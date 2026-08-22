# Qud cave-gen survey — 2026-08-07

**Provenance:** Claude recon, decompiled ground truth (ilspycmd 8.2 vs the current Steam
install at `C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud`). Read before
doing external procgen research for the camp carve pass. Context: the two-phase painter
re-ruling (carve first, decorate second) — this survey feeds both phases.
Note: the csproj still points QudLibPath at `D:\Steam\...` — stale; the install is on C:.

## The headline

**The engine already contains a carve-then-decorate pipeline.** `CaveCity` runs after
`Cave` has finished carving, partitions the *already-open* space with an influence map,
claims qualifying regions, walls them into buildings, dresses them from population
tables, and connects them with cost-shaped tunnels. Our two-phase order isn't just
defensible — it's the base game's own idiom for "people moved into a cave."

## Phase-1 material (carve)

### Cave.cs — layered fields + persistent connectivity
- One **global** Perlin field (1200x375, five octaves: scale/amp 4/1.0 · 8/0.86 ·
  64/0.6 · 300/0.36 · 1200/0.06, then smoothed) sampled by world coordinates — zones get
  cross-zone cave continuity for free, no storage. If we ever want multi-zone quarry
  districts, this is the trick: one seeded field, indexed by (wX*240 + X*80, wY*75 + Y*25).
- AND'd with per-zone **cellular automata** (`CellularGrid`: 2 passes, SeedChance 60,
  seeded borders depth 2). Cell opens if CA says open OR noise ≤ 0.47.
- Inter-zone tunnel topology = one persistent **RecursiveBacktrackerMaze3D** (240x75x30,
  whole world); each zone looks up its maze cell for N/S/E/W/U/D exits.
- Tunnels between exits: pathfind a **Drillbot with AddNoise=true**, carve the path, then
  **stochastic widening** — each path cell clears adjacent cells at radius 1d2, 75%
  chance each. That wobble+widen combo is the entire "wormy tunnel" look.
- 8-in-1000: the cave zone hosts a SultanDungeon; 2-in-1000: a CaveCity.

### CanyonBuilder.cs / CanyonReacher.cs — our host terrain
- Same Drillbot+noise idiom between canyon mouth connections; widening radius is
  terrain-typed (Mountains/Hills 1d3, else 1d1).
- **Every corridor cell gets a `CanyonMarker` object** (path cells AND widened cells).
  Our mouth-search may be able to find the corridor by markers instead of the
  reachability heuristic — VERIFY at build time whether markers still exist when our
  post-builder runs (they may be swept by a later cleanup builder).
- CanyonReacher rebuilds the reachable map from Canyon-typed connections only — the
  "reachable = the corridor" assumption ZonePainter already leans on, confirmed.

### ShugBurrowBuilder.cs — the "dug by a creature" signature (best single steal)
- Pathfinder cost = `|Simplex(x,y)| * 190` (FastNoise, 4 octaves, freq 0.1); walls add
  only +20. Tunnels meander along noise contours and punch walls freely — reads as
  something that dug where digging was easy, not A→B optimal. **This is the divot-tunnel
  voice**: our spines are dead straight (surveyed, chalk-lined — right for masons);
  the mole-beast's work should use THIS instead. Two tunnel voices = free storytelling.
- Carve: circular radius 2 around each step. Then a radius-3 shell pass: surviving walls
  are REPLACED with the themed wall blueprint — the burrow is *lined*, walls read as
  worked. This is the base idiom for our planned tool-marked pocket walls (Calcify
  material-matching precedent composes here).
- 1% of shell cells roll `IsolatedPocketDecoration` — sparse ambient dressing baked into
  the carve, a nice touch worth copying for dust/rubble.

### SurfaceCave.cs — rejection sampling is engine-sanctioned
- NoiseMap one-shot, then hard validation: reachable flood-fill < 400 cells → builder
  returns false and the zone rebuilds. Matches our ruled "no-thick-wall fallback = spawn
  nothing," and licenses the stronger form: carve pass may simply fail.

### TunnelMaker.cs — drunkard's walk with a direction alphabet
- Directions string as weighted alphabet ("NSEE" biases east), 10-step patience, 100
  restarts, per-cell direction strings as output. Cheapest possible wandering tunnel;
  fine for tiny test digs if we don't want pathfinding.

## Phase-2 material (decorate) — ZoneBuilderSandbox statics

The decorator toolkit already exists as engine statics (ZonePainter can call these
without subclassing):

- **`GenerateInfluenceMap(Z, seeds, strategy, maxRegionSize)`** / `...NRegions` —
  partitions open space into regions. Run it over our finished carve with node centers
  as seeds → the **space inventory falls out for free**: per-region `Size`,
  `BoundingBox`, `Center`, `Cells`, `BorderCells`/`NonBorderCells`, `AdjacentRegions`
  (adjacency graph!), `minRadius`, `IsEdgeRegion`. CostMap = distance-from-seed rings.
- **`InfluenceMapRegion.Tags` + `ConnectsToTag`** — regions carry string tags and can
  query graph connectivity by tag. Role assignment (communal/tent/sty...) = tagging;
  "is the block yard deeper than the work rooms?" = graph queries we don't have to write.
- **`PlacePopulationInRegion(Z, region, table, hint)`** — dress a region from a
  population table. The weighted-blank system, engine-native.
- **`PlacePrefab(Z, mapName, ...)`** — .rpm chunks with PreAction/PostAction/Replace
  hooks per cell. The rare authored tableau (strange-mood mason) drops in as a prefab
  with material-replace hooks, inside a claimed region.
- **`TunnelTo(zone, start, end, pathWithNoise, windyness, emptyBase, carveAction,
  weightFunc)`** — generic tunnel with pluggable carve action AND cost function.
  CaveCity's weightFunc is instructive: flat wall faces cost 20, wall corners 80, floor
  0 → connections punch through walls at sensible door spots without any door logic.
- **`EnsureAllVoidsConnected(zone, ...)`** — safety net: if a carve accident isolates a
  pocket, the engine reconnects it. Lets the carve pass stay greedy and dumb.
- **Oracle ints** (`GetOracleIntColumn` etc.) — per-zone deterministic rolls; same zone
  always rolls the same camp. Good for debugging, harmless otherwise.
- **Caveat:** `FilterPoints(hint, region, valid)` decompiles to an EMPTY body — the
  region-level hint path is dead code. Real hint handling lives in
  `PlaceObjectInBuilding`/`AddPointWithHint`/`PlaceObjectInRegionRect` (FrontSide etc.).
  Don't design against hint strings without checking the specific call path first.

### CaveCity.cs — the claim-and-dress loop worth mirroring
- Region qualification: `Size ≥ 9`, bbox ≥ 3x3, `AdjacentRegions.Count ≤ 1`, no stairs,
  per-region chance roll. Claim = qualify, not solve. Skip on failure. (Exactly our
  no-backtracking creed.)
- Building = wall the cost-ring at radius R (interior = cost < R), first ring cell
  becomes the door. Round-ish structures that hug the carved space — no rect-fitting.
- First claimed region gets the boss population, the rest get the creature population —
  a two-tier claim priority in six lines.

## Mapping onto the camp plan (proposals, nothing ruled)

1. **Keep our primitives for the pockets** (blob/satellite/spine/face — the authored
   look survives); **steal the ShugBurrow noise-cost tunnel as the mole-beast's voice**
   and the Drillbot wobble+widen for the weathered quarry mouth. Straight spine = mason
   work; noise tunnel = beast work; the player reads who dug what.
2. **Replace the hand-rolled manifest with an influence map** over the finished carve:
   seeds at node centers, regions ≈ our spaces, adjacency + depth rings free.
3. **Decorate = claim loop in the CaveCity shape**: required roles sorted by priority,
   each claims its best-fit qualifying region (tag it), skip-if-none; leftovers roll
   flavor tables via PlacePopulationInRegion; tableaus via PlacePrefab.
4. **Wall-lining shell pass** after carve: pocket walls swapped to tool-marked /
   palette-matched material (ShugBurrow idiom + Calcify machinery).
5. **Fail-hard licensing**: carve returns false → no camp this zone (SurfaceCave
   precedent) — already our ruled fallback.

## Ruin/encounter freshness recon (2026-08-07, second sitting — user asked how Red Rock / ruins stay fresh)

Sources: `Ruins`/`Ruiner`/`RuinPowerGrids` decompiles; `Worlds.xml` Redrock cell;
`ZoneTemplates.xml` Redrock 1-3; `PopulationTables.xml` Redrock tables.

**The Red Rock pipeline:** zone = `Redrock` carve builder + `PossibleCryotube`
(chance-gated one-off) + `postbuilder ZoneTemplate:Redrock N`. The template
partitions open space into ~RegionSize-cell regions and rolls population tables
globally + per region. All the freshness is TABLE ARCHITECTURE:

1. **Placement hints ARE real** — `Hint="InsideCorner"`, `AlongWall`, `Center`,
   `Adjacent:25`, `AdjacentToBlueprint:Campfire`, `AdjacentToStartsWith:Sultan`,
   `LivesOnWalls` — used everywhere in base tables and resolved by the
   zone-template placement path (`PlaceObjectInArea` Hints). **This corrects the
   earlier caveat:** only the `FilterPoints` helper is dead code; the hint system
   itself is alive and is the engine's placement grammar. Objects declare
   relationships, tables stay layout-agnostic — same table dresses any shape.
2. **Nothing is weighted content.** `<table Weight="200" Name="Nothing"/>` beside
   Weight-200 snapjaws; features table leads with Weight-30 Nothing. Most regions
   get silence or one small thing — density variance IS the freshness.
3. **Rarity ladders in one table:** SultanShrine at Weight 1 beside Rubble/Bones/
   Trash/Campfire at Weight 20 — mundane floor, rare jackpot, one roll.
4. **Relational micro-tableaus:** campfire at Center + garbage adjacent-to-campfire;
   shrine + torchposts adjacent + flowers Adjacent:100 + wine pitcher. Scenes are
   composed by relation, not coordinates (validates the recipes.md hookah-circle).
5. **Tense variants as pickone siblings:** `Campfire Remains` vs `Campfire` — one
   weight roll flips a scene from past to present.
6. **Depth ladders = dial-twin tables:** Redrock1/2/3 differ only in dice
   (4d4→6d6 dreadroot), creature mix, and RegionSize 100→150→200. Same grammar,
   turned dials.
7. **Ruiner = time SIMULATED, not painted:** 0-3 + Level/5 real physics explosions
   (walls crumble via the engine's own destruction), then grammar-police passes —
   `CleanNarrowConnections` (kills diagonal pinches), `CleanOrphanDoors` (doors
   that lost their wall pair die) — then hard validation: ≥400 reachable cells or
   the builder FAILS and the zone rebuilds.
8. **Ruins semantic tags:** carve tags every cell (Inside/Wall/Door/Outer/Isolated);
   a THEME rolls from a population table onto the zone; per-tag Replace tables
   mutate it; dressing interprets tags. Building templates persist per zone
   COLUMN and span 1d3 x 1d2 ZONES — large-scale coherence for free.
9. **RuinPowerGrids:** infrastructure rolled ALMOST-working (30% missing 1d3
   producers / 1d6 consumers, rolled damage) — broken machines are implicit puzzles.

**Terra Firma applications (proposals):** decorate-phase tables should lead with
heavy Nothing weights + one rare jackpot (strange-mood mason at Weight 1 is the
SultanShrine of this mod); tableaus via relational hints (engine-native, works
today); tense variants for workings (active face / abandoned face, lit fire dish /
cold one); camp-size ladder as dial-twin tables; sprawl workings roll a tiny
feature table each (bones/junk/puddle/nothing); adopt `CleanNarrowConnections`
into the painter (bite merging can create diagonal pinches); keep the ≥N-reachable
hard validation. Ruiner-style real-explosion weathering: tempting for a "collapsed
working" variant, note the mod already owns wall→rubble machinery.

## Ruins SHAPING recon (2026-08-12 — full decompile of Ruins/Ruiner/BuildingTemplate, pre-dress-phase; the 08-07 section covered the table side, this is the geometry side)

**How a ruin actually gets its shape (`BuildingTemplate`):**
1. **Mass-first, hollow-second.** N random rectangles stamp SOLID wall mass (overlapping
   freely), an 8-dir flood from the zone border classifies Outside vs OutsideWall (the
   skin), then interior cells hollow to Inside. The building's footprint is whatever the
   overlaps made — irregular silhouettes for free, no outline drawing anywhere.
2. **Recursive partition with validated doors (`FillRoom`).** Any room over 24 cells
   (and >3 per axis) gets a partition across its narrower axis (75%; flipped 25%),
   placement validated so the partition butts open cells at both ends, ONE door punched
   at a validated spot in it, then RECURSE into both halves. Big spaces subdivide until
   den-sized organically. This is the suite's grown-up cousin.
3. **Doors at perimeter intervals:** a walk along OutsideWall punches area/n doors with
   random phase — 1-2 usually, 10% chance of 1d2 more.
4. **`RoomList` falls out of the shaper** — every room flood-enumerated with bbox +
   size DURING generation. The engine's own idiom is the warren-lab dress registry:
   spaces know what they are at birth; nothing is inferred later.
5. Multi-zone coherence: the template is a zone-COLUMN property spanning 1d3 x 1d2
   zones. (No warren application yet — single-zone — noted for someday.)

**How it gets its age (`Ruiner`) — the tense machine:**
- 0-3 + RuinLevel/5 REAL explosions (`LegacyApplyExplosion`) at random points — decay is
  SIMULATED, not painted; walls crumble through the engine's own destruction physics.
- Then the grammar passes: `CleanNarrowConnections` (diagonal pinch opener — already in
  the legacy police) and **`CleanOrphanDoors`** (a door not flanked by exactly one
  parallel wall PAIR dies — doors whose walls were blown away don't linger as noise).
- Then HARD validation: >=400 cells reachable from a connection or the builder returns
  false and the whole zone rebuilds. Honest failure at zone scale.

**Semantic-tag theme layer (`Ruins.BuildZone`):** every cell tagged from its template
tile (Inside/Room/Perimeter · Wall/Inner · Door · Wall/Outer · Connection/Stairs ·
Void→Inside+Isolated — sealed voids are TAGGED for deliberate treatment, the engine's
own endorsement of the wet-pocket breach-reward idea), then a THEME rolls once from a
population table onto the zone column and per-tag Replace tables mutate it. One roll
recolors the whole site's dressing; the geometry never changes.

**Steal verdict for the warren (proposals, unruled):**
- **NOW (dress phase): the theme layer.** A warren-level theme roll at dress time
  swapping table VARIANTS per space-kind while the registry stays fixed — one roll
  makes a prosperous dig vs a lean one vs a fresh cut. Cheap (a lookup key), and it is
  exactly how the game gets site-level personality without new geometry.
- **LATER (the "progress from there" tense flip): Ruiner's simulate-don't-paint.** The
  abandoned-warren variant should be collapse EVENTS (rubble splats + wall crumble —
  the mod already owns wall→rubble machinery from Fissure) followed by the police and
  a reach validation, not hand-placed decay items. Geometry-level past tense.
- **C# PORT: `CleanOrphanDoors`** joins the police when real Door objects exist.
- Confirmations, no action: dress registry ≈ RoomList (engine-idiomatic); spawn-nothing
  ≈ the >=400 hard fail; FillRoom subdivision available if communal wants a
  multi-room block grown from one footprint instead of suite stamps.

## Red Rock SHAPING recon (2026-08-17 — full decompile of `Redrock`, `NoiseMap`, `TunnelTo`, `EnsureAllVoidsConnected`; the 08-07 section above covered the table side, this is the geometry side)

**Provenance:** Claude recon, decompiled ground truth (ilspycmd vs the current Steam install).
Context: the user's "rework the warren quarries to be structured more like red rock" question.

**The whole carve, in order (`Redrock.BuildZone`):**
1. **Every cell gets a Shale wall.** Solid zone, no exceptions.
2. **The doors become seeds.** Every `ZoneConnection` and every cell tagged Stairs is
   collected into a `List<NoiseMapNode>` and handed to the noise map as ExtraNodes, each
   spiked to `MaxSeedDepth`. **There is no mouth search anywhere in the file** — open space
   grows around the real entrances by construction. This is the base game's entire answer to
   the attachment problem our `mouthMassR` / `mouthMassPerPart` fitting solves the hard way.
3. **`NoiseMap(80,25, 10, 3,2, seedsPerSector, 80,240, 0, 4, -3, 1, nodes, -1, maxSeeds)`**,
   then clear walls wherever `Noise >= 1`, border ring guarded. Param names confirmed off
   `CreateNoiseMap`: (W, H, MaxDepth, SectorsWide, SectorsHigh, SeedsPerSector, MinSeedDepth,
   MaxSeedDepth, BaseNoise, FilterPasses, BorderWidth, CutoffDepth, ExtraNodes, FilterBorder,
   MaximumSeeds). Two details that shape the result:
   - **BaseNoise 0.** With no background field the ONLY open space is seed blobs — which is
     why Red Rock reads as discrete chambers rather than sponge. The drillbot's obstacle map
     passes 6 for exactly the opposite reason.
   - **Seed depth 80-240, blurred 4 passes.** Depth IS blob size, so chamber size varies per
     seed for free. `MaximumSeeds` decrements across a SHUFFLED sector list and breaks out
     mid-run, so a cap starves random sectors, not the last ones.
   - The ExtraNode guard skips anything on the border ring (`x > 0 && x < Width-1`).
4. **Water (NOT PORTED).** A second NoiseMap seeded from a SUBSET of the first's `PlacedSeeds`
   becomes `DeepBrackishPool`; then each pool node runs `TunnelTo` to a random WALL cell
   within 30, carving pool as it goes — **water veins that run off into the mass and stop.**
   The opposite shape from our wet pockets (sealed, shelled, conspicuously avoided). Left out
   of the lab port deliberately; ours already owns the water fiction.
5. **0-4 long hauls.** `TunnelTo` from a random placed seed to the seed furthest from it.
   **The sharp bit: `emptyBase` is 200 while a wall costs 80** — open floor is more than twice
   as expensive as solid rock, so the hauls bore fresh passage instead of threading the
   chambers that already exist. (`TunnelTo` cost, full: border 2000, wall `80 + |simplex((x +
   wX*80)/3, y + wY*25)| * 160`, open `emptyBase + noise`, cardinal-only, shuffled directions.)
6. **`EnsureAllVoidsConnected`.** Regions sorted by distance from the first, each linked to its
   nearest already-connected neighbour by a noisy cardinal path.
7. **Level set pieces.** Z12 sorts regions by summed distance from the stairs, takes the top
   1-3, fills them with `InfluenceMapBlocker` and gives them their own encounter table — **a
   vault you have to dig into, and the nearest base-game relative of our wet pocket.** Z13
   rolls one of Stockade / CaveCity / SnapjawFort. Z14 gets the river builders.

**Depth ladder = the same builder with different dice, nothing else:** seeds/sector `1-3`
(capped at 3 total) → `0-5` → `0-6` → `0-7`; pool nodes 0 → 1 → 2-3 → 3-4; hauls 0 → 1 → 2-3
→ 3-4. Region size on the dressing side moves with it (100 → 150 → 200). Same grammar,
turned dials — the idiom our warren size classes already run at zone scale.

**The structural verdict (proposal, unruled): Red Rock has no carve language.** Its geometry
carries zero intent; intent arrives as a TENANT layered onto finished blobs. That is exactly
inverted from the warren, whose whole bet is that the geometry itself reads as made. Steals
worth having without giving that up: the doors-as-seeds trick (if attachment ever gets
expensive), depth-ranked regions as a placement law (far from the entrance = where the good
thing goes — we have no such law), and the layered structure itself, i.e. giving the quarry
natural void to cut into so the seam between worked and unworked is visible. Lab pass for the
last one built 2026-08-17 — see the TODO entry; the numbers are there.

## Decompile dumps

Raw sources cached this session at
`scratchpad/qud-builders/` (Cave, CaveCity, CanyonBuilder, CanyonReacher,
ShugBurrowBuilder, TunnelMaker, SurfaceCave, BasicLair, BasicRoomHall, NoiseMap,
InfluenceMap, InfluenceMapRegion, ZoneBuilderSandbox). Scratchpad is session-temporary —
re-decompile on demand; ilspycmd is a global dotnet tool.
