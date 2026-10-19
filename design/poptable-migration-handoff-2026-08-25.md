# Handoff: dress tables to engine population tables (2026-08-25)

**For a fresh agent.** USER commissioned this handoff 2026-08-25 after ruling the
migration worth doing; treat the migration itself as greenlit, every dial below as
CLAUDE-proposed unless the user strikes it. Read `CLAUDE.md` at repo root FIRST and
follow it exactly (text rules, provenance discipline, verification, deploy layout,
no Co-Authored-By). The design ledger is `TODO.md`; this sitting's context lives in
the entries dated 2026-08-25 (blind-claim port, interior-checking vocabulary).

## Mission

Move the warren dress system's TABLE DATA (which blueprint, what weight, how many,
what spatial hint) out of C# constants into the mod's `PopulationTables.xml`, using
the ENGINE'S OWN hint vocabulary, while keeping every placement LAW in code. The
goal is authorship and moddability: table contents are content calls and content is
the user's lane; XML edits should never require a recompile.

## The correcting insight this rides on (receipts)

Claude wrongly claimed engine poptables cannot express placement. The user
challenged it; the grep settled it: base `PopulationTables.xml` carries 1000+
`Hint=` attributes with a rich spatial vocabulary, consumed by village furnishing.
Measured survey (2026-08-25): `AlongWall` 231 · `AlongInsideWall` 124 ·
`Adjacent:90` 77 · `Adjacent` 61 · `InsideCorner` 59 · `OutsideDoor:1` 57 ·
`Aquatic` 53 · `RadialFromCenter:1` 37 · `LivesOnWalls` 36 · `Center` 25.

**Your first recon obligation: find the engine consumer of these hints** (decompile
with `ilspycmd`, path in CLAUDE.md; grep the village/furnishing zone builders for
`Hint` / `AlongWall`) **and cite class+method in your journal entry before writing
the interpreter.** Do not guess parameterized hint semantics (`Adjacent:90`,
`OutsideDoor:1`); read them.

## Current architecture (all in `TerraFirma/Scripts/WarrenCarver.cs`)

Search anchors, not line numbers - the file moves often:

- `T_SOCKET`, `T_GALLERYBAY`, `T_GALLERYPILLAR`, `T_GALLERYFLOOR`, `T_NATURAL`,
  `T_PLAINTRACE`, `T_HALLTABLE` - weighted scatter tables, `Row(weight, id, hint)`
  with hints `face`/`center` and a heavy `Nothing` row (null id).
- `ROOMPOOLS` - uniform-pick blueprint pools (`light`, `storage`, `seating`,
  `sleep`, `centre`, `liquid`, `crafts`, `hearth`, `comfort`, `keepsake`).
- `ROOMTYPES` / `RowSpec` - per-room-type recipes: pool + count range + chance +
  hint (`corner`/`wall`/`inside`) + first-flag. Read `DressDwelling` to see how
  hints resolve (`WallsAround` counts: corner >= 2 walls, wall >= 1).
- `BP` dictionary - sim item id -> real blueprint. Every name verified against
  Base or mod XML; an unverified name is a MODERROR at spawn.
- `DressSite` / `Seat` / `PlaceItems` - the placement engine and the zone writer.
- The sim mirror is `design/warren-lab-v2.html` (`DRESS`, `ROOMPOOLS`, `ROOMTYPES`,
  `PLAINTRACE`, `NATURAL`, `ITEMS` art table). THE SIM LEADS (memory + TODO:
  sim-first ruling); it keeps its JS copies, and your migration must leave a
  comment on both sides naming the other as the mirror.

Existing XML precedent: `TerraFirma/XML/PopulationTables.xml` already holds the
basket poptables and MiningGear - follow its idiom and naming
(`Cleo_TerraFirma_*`).

## What migrates

1. Every `T_*` scatter table -> one mod poptable each. `Nothing` rows: engine
   tables express nothing-weight natively (verify idiom in base tables; villages
   use it) - keep the exact current weights.
2. Every `ROOMPOOLS` pool -> one poptable each (uniform weights).
3. Hint TRANSLATION, ours -> engine vocabulary (verify against the consumer
   recon before committing to it): `corner` -> `InsideCorner` · `wall` ->
   `AlongWall` · `center` -> `Center` · `inside` -> whatever the engine's
   interior hint is (candidate: absence of hint, or `AlongInsideWall` - RECON) ·
   `face` (ours: the work face) has no engine equivalent - keep as a
   `Cleo_TerraFirma_Face` hint string that OUR interpreter handles; the engine
   never reads our tables, so custom hint strings are legal data.

## What does NOT migrate (the laws - they are invariants over rolls, not rows)

- Per-room budgets and density (`RoomType.Density`, size-scaled trace rolls).
- One oven per site; one chest per room (downgrade to basket).
- The articulation guard (`SolidOK`) and threshold checks.
- WARRENFLOOR and ROOM FLOOR re-roll laws (re-roll with Nothing removed).
- Cluster stamps (hookah orbit, kiln bricks) - shape-adaptive code.
- `DressYard`, chalk ring, gap+trail+cairns; occupancy/tense passes.
- The `BP` id->blueprint map dies where tables carry real blueprint names
  directly - a simplification, but the sim still needs its art ids: decide
  whether tables carry blueprint names (recommended) and the SIM keeps its own
  id layer, or keep BP. Flag your choice in the journal.

## The determinism decision (present to the user if in doubt; recommendation below)

Warren carves are seed-deterministic (`SeedFor(ZoneID)`); a zone rebuild
reproduces its warren exactly. Rolling via `PopulationManager` swaps dress onto
the engine's ambient RNG and rebuilds would re-dress differently (layout stable,
items drifting). RECOMMENDED ARCHITECTURE: **parse the XML tables, roll with the
carver's own seeded R()** - data-driven tables AND intact determinism. Use
`PopulationManager` (or its loader) only to READ table definitions; verify the
API surface in the decompile (do not guess method names). If parsing proves
hostile, fall back to engine rolls and log the determinism change loudly in
TODO.md as a flagged behavior delta.

## Verification protocol (house rules, do not skip)

1. Compile: `dotnet build TerraFirma.csproj -p:QudLibPath="C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed"`
   - NO trailing backslash (phantom CS0518 storm otherwise). 0 errors required.
2. XML: strict `XmlDocument.Load` + `tools/check-text-rules.ps1` (no `--` in XML
   comments - it will not parse; no em dashes in shipped text).
3. Deploy: copy changed files to
   `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma`,
   hash-verify each. Never touch `...\Mods\TerraFirmaBeta`.
4. Behavior check: run the sim's dress census before and after any sim-side
   change; game-side, compare `[TF-VERIFY] [WARREN] dress:` lines over a
   `tfwarrens` sweep against current distributions (items/site median ~40).
   Distribution-equal is the bar; seed-exact is only expected under the
   recommended parse-and-roll-ourselves architecture.
5. Record the verdict in TODO.md with status tags per the provenance discipline
   (weights stay their CLAUDE-dial/USER-RULED provenance - copy the existing
   tables' comments across, they carry rulings). Commit without Co-Authored-By.
6. Status ends at `log-confirmed, awaiting in-game verification` - the user's
   eyeball closes it, never you.

## Explicitly out of scope

RPM prefab setpieces (logged separately as the payload channel, unruled), the
figurine animation table, any change to placement laws or dress LOOK. If a weight
seems wrong, flag it; do not retune.
