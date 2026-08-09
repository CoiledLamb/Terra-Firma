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

## Decompile dumps

Raw sources cached this session at
`scratchpad/qud-builders/` (Cave, CaveCity, CanyonBuilder, CanyonReacher,
ShugBurrowBuilder, TunnelMaker, SurfaceCave, BasicLair, BasicRoomHall, NoiseMap,
InfluenceMap, InfluenceMapRegion, ZoneBuilderSandbox). Scratchpad is session-temporary —
re-decompile on demand; ilspycmd is a global dotnet tool.
