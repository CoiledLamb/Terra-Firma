# World-builder cooperativeness recon — 2026-08-23

**Provenance: CLAUDE RECON, decompile ground truth (not design; nothing here is ruled).**
Question scouted (user docket 2026-08-23): "how cooperative the world builder will be with
making warrens in the first place." Sources: `ilspycmd` decompiles of `Assembly-CSharp.dll`
and the Base XML tree. C# citations are Type.Method (decompile-relative line numbers where
given); XML paths are under `CoQ_Data\StreamingAssets\Base\`.

**Headline: highly cooperative. Two viable doors, one fork to rule** (see the SPAWN RATE
section of TODO.md for the fork entry).

---

## 1. Canyon zones: what they are and how they build

**World map blueprint:** `TerrainDesertCanyon` (`ObjectBlueprints\WorldTerrain.xml:35`), plus
variants `TerrainDesertCanyon2/3/4`.

**Cell blueprint:** `Worlds.xml:599` — `<cell Name="DesertCanyon" Inherits="DefaultJoppaCell"
ApplyTo="TerrainDesertCanyon">`. Variants at lines 618-620 (`DesertCanyon2/3/4`) are empty
cells inheriting `DesertCanyon`. Matching is by terrain object blueprint name:
`ZoneManager.GetCellBlueprints(string, int, int)` looks up
`WorldBlueprint.CellBlueprintsByApplication[terrainObject.Blueprint]`, with an additional
`"x.y"` positional key.

**Surface canyon zone (Level=10, Worlds.xml:603-608):**
- `<builder Class="Hills">` (priority 4000)
- `<builder Class="FactionEncounters" Population="GenericFactionPopulation">` (4000)
- `<music Track="Music/Deep Dawn">` → builder class `Music` (4000)
- `<postbuilder Class="ZoneTemplate:DesertCanyon">` (5000)

Tag→priority mapping is `WorldFactory.LoadBuilderNode`: `prebuilder`=3999, `builder`=4000,
`postbuilder`=5000, explicit `Priority` attribute honored. Constants in
`XRL.World.ZoneBuilderPriority` (OVERRIDE=-1000 … EXTREMELY_LATE=7000).

**The canyon shape itself is NOT in the cell blueprint.** At world build,
`JoppaWorldBuilder.BuildCanyonSystems` walks all 240x75 surface zone slots; wherever the
parasang terrain blueprint name contains `"Canyon"` (also Hills, Asphalt,
TerrainRustedArchway, TerrainBethesdaSusa, TerrainJoppaRedrockChannel) it consults the
pre-generated maze `The.Game.WorldMazes["QudCanyonMaze"]` (built in
`JoppaWorldBuilder.BuildMazes`, 240x75) and calls
`The.ZoneManager.AddZoneMidBuilder(zoneID, "CanyonStartMouth"/"CanyonNorthMouth"/…/
"CanyonBuilder")` (priority 4500) and `AddZonePostBuilder(zoneID, "CanyonReacher")` (5000).
Builder classes: `XRL.World.ZoneBuilders.CanyonBuilder`, `Canyon{Start,North,South,East,
West}Mouth`, `CanyonReacher`.

**Effective run order for a surface canyon zone** (single priority-sorted list,
`ZoneBuilderCollection.Add` is a stable insertion sort): Hills → FactionEncounters → Music
(4000) → Canyon mouths + CanyonBuilder (4500) → ZoneTemplate:DesertCanyon + CanyonReacher
(5000) → any world-build POI builders like `SnapjawStockadeMaker` (6000). Afterward,
hardcoded in `ZoneManager.GenerateZone`: `ForceConnections`, reachability check (failure →
full rebuild loop, per-builder RNG reseeded from `"ZONESEED"+ZoneID`), `zone.Built = true`,
`BiomeManager.MutateZone`, `SanityCheck`, then `BeforeZoneBuiltEvent.Send` →
`ZoneBuiltEvent.Send` → `PaintWalls`/`PaintWater` → `AfterZoneBuiltEvent.Send`.

Special mechanism: any builder with **negative priority** switches the collection into
override mode — `ZoneBuilderCollection.ApplyTo` runs the negative-priority block and then
stops (`ZoneManager.AddZoneBuilderOverride` inserts at -1000). Bey Lah instead uses
`ClearZoneBuilders` + an explicit stack (`JoppaWorldBuilder.AddHindrenVillage`).

## 2. Mod injection points

### a. Worlds.xml IS mod-overlayable — with cell-granularity replace semantics

`WorldFactory.Init()` loads **every XML file with root `<worlds>`** via
`DataManager.YieldXMLStreamsWithRoot("Worlds")`. `DataManager.CheckXMLInitialize` caches base
files first, then every active mod's XML files, sorted by a `LoadPriority` root attribute —
so a mod can ship its own `Worlds.xml`.

Merge semantics, `WorldFactory.LoadWorldsNode`:
- Same world `Name` → merged into the existing world, not replaced.
- World-level `<builder>` entries merge per-class: same `Class` replaces; a `Class` starting
  with `-` removes the named builder.
- `<cell>` entries merge **by Name, whole-cell replace**:
  `value.CellBlueprintsByName[item.Key] = item.Value`, with `ApplyTo` re-registration.
- `Inherits` resolves at parse time against the cross-file `Cells` registry
  (`WorldFactory.LoadCellNode`, `CopyFrom`), so a mod cell can inherit the base cell of the
  same name: `<cell Name="DesertCanyon" Inherits="DesertCanyon"
  ApplyTo="TerrainDesertCanyon">`.
- Inside a cell, a `<zone>` node **replaces the whole zone slot**
  (`cellBlueprint.LevelBlueprint[j,i,k] = zoneBlueprint`) — builder lists are not merged
  per-zone. To add one builder to the Level=10 canyon zone you must restate the stock four
  builders plus yours.
- **Trap:** base `DesertCanyon2/3/4` copied the **base** DesertCanyon at load time; a mod
  override of `DesertCanyon` does not retro-propagate. A mod must override all four cells
  (three are one-liners inheriting the mod's own cell).

### b. ZoneTemplates.xml: mod-mergeable, but templates do not self-select zones

`ZoneTemplateManager.LoadTemplates` iterates
`DataManager.YieldXMLStreamsWithRoot("ZoneTemplates")`; same `Name` → **whole-template
replace**. Cache is `[ModSensitiveStaticCache]`, rebuilt on mod changes.

Templates fire **only where referenced**: `ZoneBuilderBlueprint.ApplyTo` special-cases
`Class.StartsWith("ZoneTemplate:")` → `ZoneTemplateManager.Templates[name].Execute(Zone)`.
There is no terrain-constraint matching in the template itself; the Worlds.xml cell blueprint
is the selector. Inside a template: `<global>`, `<single>`, `<eachregion>` roots; nodes
`population`/`group`/`builder`/`exit`/`cellfilterout`/`graph`/`branch` with `Criteria`,
`Chance`, `Weight`, `Style`, `MaxApplications`, `Hint` (`ZoneTemplateManager.LoadNode`).
`Criteria` vocabulary (`ZoneTemplateNode.TestCriteria`): `ZoneHasSemanticTag:X`,
`RegionHasSemanticTag:X`, `connection`/`!connection`, `isolated`/`!isolated`, `furthest`,
`pocket`, `deadend`. Regions are an influence-map partition of the already-carved zone
(`ZoneTemplate.Execute`, `RegionSize` attribute). Note `<builder>` is a legal template node —
a template can invoke a C# builder per region.

**Goatfolk villages / snapjaw forts are NOT placed via templates.** The canyon template's
per-region table only spawns loose `SnapjawParty1` groups (`PopulationTables.xml:6453`
`DesertCanyonPerSector`: Nothing 8400 / SnapjawParty1 1500). The POIs are world-build
placements (see 2c and section 4).

### c. World-builder extension hooks — real, mod-facing, unconditionally invoked

- Generic: `WorldFactory.BuildWorld` runs
  `ModManager.GetInstancesWithAttribute<IWorldBuilderExtension>(typeof(WorldBuilderExtension))`
  and calls `OnBeforeBuild(world, builder)` / `OnAfterBuild` around every world builder.
- Joppa-specific: `JoppaWorldBuilder.BuildWorld` (decompile line 3655) loads
  `ModManager.GetInstancesWithAttribute<IJoppaWorldBuilderExtension>(typeof(JoppaWorldBuilderExtension))`;
  hooks `OnBeforeBuild`, `OnAfterBuild`, `OnBeforeMutableInit`, `OnAfterMutableInit`
  (`XRL.World.WorldBuilders.IJoppaWorldBuilderExtension` — a class with virtual no-op
  methods; you subclass it AND tag `[JoppaWorldBuilderExtension]`).
- From a hook you get the builder instance, i.e. the exact toolkit the base game uses for
  POIs: `AddMutableEncounterToTerrain("DesertCanyon", n, callback)` (picks n random mutable
  canyon zones, hands you zoneID + TerrainTravel), `AddSecret(...)`,
  `AddLocationFinder(...)`, `The.ZoneManager.AddZoneBuilder(zoneID, 6000, "YourBuilder",
  params...)`, `mutableMap`, `worldInfo`.
- **Negative finding:** the base game ships zero classes carrying either attribute (full
  assembly class list scanned) — the hooks are mod-facing only, but their call sites are
  unconditional in the live build path, so they are not vestigial.

### d. Zone-build events a C# mod can listen to

- `BeforeZoneBuiltEvent` / `ZoneBuiltEvent` (post-builders, pre-paint) /
  `AfterZoneBuiltEvent` (post-paint) — sent in all three build paths of `ZoneManager`
  (`GenerateZone`, `GenerateFactoryZone`, world-map zone path). `Send` dispatches to
  `The.Game.HandleEvent` and `Zone.HandleEvent`.
- Receiver mechanism: subclass `XRL.IGameSystem`, which has virtual `HandleEvent(...)`
  overloads for `BeforeZoneBuiltEvent`/`ZoneBuiltEvent`/`AfterZoneBuiltEvent`/
  `ZoneActivatedEvent`/`ZoneThawedEvent`; register with
  `The.Game.RegisterEvent(this, ZoneBuiltEvent.ID)`; install via
  `XRLGame.RequireSystem<T>()`. At `ZoneBuiltEvent` time builders have finished, so a system
  can carve on top and gate itself on `Z.GetZoneWorld()`/terrain.
- `ZoneActivatedEvent` fires from `Zone.Activated()` **every** activation, not once — wrong
  hook for one-time carving.
- `XRL.World.Parts.ApplyZoneBuilder` — an `IPart` for widget objects: on `EnteredCellEvent`
  it runs `ZoneManager.ApplyBuilderToZone(ZoneBuilder, zone)` exactly once (`Executed` flag).
  **Base-game load-bearing:** `ObjectBlueprints\Widgets.xml:124-132` (`StairsDownBuilder`,
  `WaterloggedBuilder`, `SlimePoolsBuilder`). This is the bridge that lets a population table
  or zone template trigger a full C# builder.
- Custom builder classes: everything resolves through
  `ModManager.ResolveType("XRL.World.ZoneBuilders." + Class)`, which searches mod assemblies.
  A mod class in namespace `XRL.World.ZoneBuilders` with `public bool BuildZone(Zone Z)` is
  directly referenceable from Worlds.xml, `AddZoneBuilder`, templates, and
  `ApplyZoneBuilder`; XML attributes / `AddZoneBuilder` params are injected into same-named
  fields/properties by reflection (`ZoneBuilderBlueprint.Create`).
- Also present: `GetZoneEvent.TryGetFor` is checked before the cache in
  `ZoneManager.GetZone` — a full zone-supply interception point.

## 3. Lazy zone build vs world build — confirmed

`ZoneManager.GetZone(string)`: `GetZoneEvent` → `CachedZones` dict → `TryThawZone` (frozen
zones live in the save's SQLite cache, `SELECT Data, FrozenTick FROM FrozenZone`;
`FreezeZone` writes `REPLACE INTO FrozenZone`) → **`GenerateZone`**. So zones are built on
first request and then persist in the save (`ZoneManager.Save` serializes `CachedZones` and
the `FrozenZones` set; `IsZoneBuilt` = cached-or-frozen).

At build time, `GenerateZone` merges two builder sources into one collection:
1. `ZoneBuilders` dictionary (per-ZoneID, populated by `AddZoneBuilder`/`AddZoneMid/
   PostBuilder` at world build or anytime) — **serialized into the save**
   (`SaveZoneBuilders`/`LoadZoneBuilders`).
2. The Worlds.xml cell/zone blueprints via `request.World.GetBlueprintsFor(request)` — **read
   live from the XML-loaded `WorldFactory`**, fresh each session.

Consequence, verified: a newly installed mod's Worlds.xml overlay, zone templates, and
builder classes WILL fire in any not-yet-built zone of an existing save. Anything done at
world build (JoppaWorldBuilder extensions, `AddSecret` map notes, `ZoneBuilders` entries like
`GoatfolkYurts`) happens once at embark and never reaches existing saves.

## 4. Frequency control

- **World-build POIs (the goatfolk/fort model):** counts and chances are hardcoded in
  `JoppaWorldBuilder`. Goatfolk villages: per mutable **Jungle** surface zone, `3.in100()` →
  `AddZoneBuilder(zoneID, 6000, "GoatfolkYurts")` (+name, secret, encounter popup); qlippoth
  haunts: DeepJungle, `15.in1000()`. Snapjaw forts: `BuildForts` calls
  `AddMutableEncounterToTerrain("DesertCanyon", 25, ...)` → exactly 25 stockades
  (`SnapjawStockadeMaker` at 6000), plus 25 pig farms, 25 starapple farms and 1 Stopsvalinn
  hero, across all mutable DesertCanyon zones. `GoatfolkYurts.BuildZone` is one line
  delegating to `VillageMaker`.
- **Zone-build path (available to a lazy mod builder):** (i) `Chance`/`Weight` attributes on
  zone-template nodes (e.g. `SurfaceRuinsTechInfrastructure` uses Chance="20"/"80"); (ii)
  population-table weights (`DesertCanyonPerSector` 8400 Nothing vs 1500 SnapjawParty1);
  (iii) roll your own in the builder — per-zone RNG is deterministically seeded per builder
  from `"ZONESEED"+ZoneID` (`GenerateZone` + `ZoneBuilderCollection.ApplyTo`), so "X% of
  canyon zones" is stable per world seed and immune to visit order.

## 5. Red Rock precedent

Red Rock is a Worlds.xml hand-authoring, not a world-builder feature: `Worlds.xml:2080`
`<cell Name="Redrock" Inherits="DesertCanyon" ApplyTo="TerrainRedRock" Mutable="false">`
(terrain object `WorldTerrain.xml:501`). Surface zones override the inherited canyon zone
with `Plains` + population `Redrock Surface` + widgets; the 1,1 zone adds `RedrockOutcrop`.
Strata 11-14 (x=1,y=1 only) each run the dedicated C# builder
`XRL.World.ZoneBuilders.Redrock` (a `ZoneBuilderSandbox` subclass: fill with Shale, carve via
`NoiseMap` with per-depth seed densities, stairs as forced nodes) plus depth-specific
`ZoneTemplate:Redrock 1/2/3` and `Bottom Redrock` postbuilders (`ZoneTemplates.xml:1270-1313`).
`Mutable="false"` exempts the parasang from all mutable-encounter placement
(`JoppaWorldBuilder.BuildMutableEncounters` checks `CellBlueprint.Mutable`). So the precedent
is: inherit DesertCanyon, mark immutable, swap in bespoke builders + templates per stratum.

## 6. Landmarks / secrets

API: `Qud.API.JournalAPI.AddMapNote(string ZoneID, string text, string category = "general",
string[] attributes = null, string secretId = null, bool revealed = false, bool sold = false,
long time = -1, bool silent = false)`, plus `GetMapNote(secretId)` and
`RevealMapNote(note, silent, LearnedFrom)`. `JoppaWorldBuilder.AddSecret` is just a thin
wrapper that appends terrain-derived attributes before calling `AddMapNote`. Nothing in it
depends on world-build context — it appends to game-state collections and **can be called
lazily** from a zone builder or a `ZoneBuiltEvent` handler at first build (create unrevealed
with a stable `secretId`, e.g. derived from the ZoneID, then `RevealMapNote` for the
"oh neat" moment). Base game itself calls `JournalAPI` functions long after world build
(e.g. `JournalAPI.AddAccomplishment` inside `ZoneManager.SetActiveZone`).

## Cooperativeness verdict

The engine is highly cooperative; ranked most to least mod-friendly for this specific goal:

1. **Worlds.xml overlay on the DesertCanyon cells** (add a `postbuilder` for our own class or
   template to the Level=10 zone) — pure data + one C# builder class; fires lazily, works in
   existing saves, deterministic per-seed frequency inside the builder. Costs: restate the
   stock builder chain in the replaced zone node; override all four DesertCanyon cell
   variants; whole-cell replace means collision with any other mod touching those cells.
2. **Custom `XRL.World.ZoneBuilders.*` class** — the engine's native currency
   (reflection-resolved from mod assemblies, parameter injection, retry contract via
   `BuildZone` return). Needed by #1 anyway; also triggerable from population tables via the
   base-game-proven `ApplyZoneBuilder` widget part.
3. **`IGameSystem` + `HandleEvent(ZoneBuiltEvent)`** — zero XML collisions, full post-build
   carve access, easy terrain filtering; slightly less idiomatic (must self-install via
   `RequireSystem` and dedupe), and it runs after ForceConnections/reachability, so carving
   must preserve connectivity itself.
4. **ZoneTemplates.xml overlay** — fine for defining our own template referenced from #1;
   replacing the stock `DesertCanyon` template is collision-prone (whole-template replace,
   no partial merge).
5. **`[JoppaWorldBuilderExtension]`** — the most powerful (exact goatfolk/fort toolkit:
   mutable map, secrets at embark, per-zone builder stamping) but world-build-only: new
   saves only, and no base-game implementor exists to crib from.

## The fork this opens (UNRULED — recorded in TODO.md § SPAWN RATE)

The 08-18 lab model says "a world-build-time placement pass, not a zone roll"
(TODO.md, SPAWN RATE section). This recon shows that pass is door 5: exact count, native
mutable-pool dedup against farms/forts, `AddSecret` at embark — **but new worlds only.**
The zone-build door (1+2) preserves the existing-saves reach recorded under the Quarriers
lore direction, reproduces the lab's own measured binomial spread (22-on-25, σ≈5), and can
lazily place secrets — but must hand-roll farm/fort avoidance. Dedup note for that door:
world-build POI builders are stamped into the per-ZoneID `ZoneBuilders` dictionary
(serialized in the save), so a warren builder can check the zone's stamped builder list for
`SnapjawStockadeMaker`/farm makers and decline the zone. Both doors share the same C#
builder class; only the trigger differs, so the fork does not block the carve port.
