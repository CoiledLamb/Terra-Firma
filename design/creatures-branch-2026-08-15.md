# The `creatures` branch — contract + state (opened 2026-08-15)

Branched off `master` at `185a98c`, immediately after the 0.1.2 rig. Home for the creature
and stonecarver-NPC workstream so none of it can leak into a patch push by accident.

**Why a branch at all:** the 08-14 ruling ("cobble baboon shouldn't ship in 0.1.2") is
satisfied on master by construction — every mod creature carries
`ExcludeFromDynamicEncounters` and has zero table entries, so they ship as inert wish-doors.
The moment real spawn wiring lands, that stops being true, and a patch cut from master would
carry it. The wiring lives here instead.

## What must not go back to master without a ruling

- Any lift of `ExcludeFromDynamicEncounters` on a mod creature.
- Any population-table entry, party-table merge, or encounter placement for one.
- The stonecarver roster blueprints (their color map still has a CLAUDE LEAN and a CLAUDE
  FLOAT in it — mason grey, drover brown).
- `tfcreatures` itself. It is a dev tool, not patch content.

## `tfcreatures` — the staging wish (this branch's first landing)

User's framing 2026-08-15: a branch "for the creatures, including stonecarvers, so i can
generate them in game," staging-wish-first, real wiring after.

One wish lines the whole roster up east of the player, brains frozen and pacified, godmode on,
so they can be walked past and looked at rather than fought (the `tfgazewall` idiom). Wish
again and every brain goes back exactly as it was — the wish snapshots `Mobile`/`Wanders`/
`Hostile` per creature rather than assuming defaults. Each spawn logs its `Render.Tile`, so
art the deploy never carried shows up in the log as well as on screen.

Staged today:

| # | subject | what to look at |
|---|---|---|
| 1 | cobble baboon | EB 4 — orbiting pebbles, thrown stones, a visible whorl; butchers into cobblefur |
| 2 | rockhound | the RockSweeper gut: swallow and spill |
| 3 | snapjaw miner | the pickaxe IS the AI (Digging installed, `PathAsBurrowerEvent` answered) |
| 4 | burrowing urchin | wall-walker, Burrowing 5, aculei |
| 5 | Husangeion | passive, 64-dram tank, glaze tracking the liquid |
| 6 | stonecarver sprite | **art only** |
| 7 | stonerat sprite | **art only** |

**6 and 7 are sprite checks, not creatures.** No stonecarver NPC blueprint exists yet — the
roster build is this branch's actual job — so the wish paints `stonecarver.png` and
`stonerat.png` onto plain `WatervineFarmer` bodies at runtime. Nothing about the roster is
committed by that. It is the art standing in the world at true scale, and it ships no design.

Not in the roster: the malformed cragmensch. It has no blueprint in the shipped XML at all —
it lives in the quarantined v1 catalog, and the 08-12 derived tracker's "built + deployed" line
for it is stale.

## Deploy discipline while this branch is checked out

Two mods cannot share an id, so there is exactly one live dev copy and it is whichever branch
was deployed last.

- `Mods\TerraFirma` — the live dev mod. **Currently running the `creatures` branch build**
  (deployed 2026-08-15), so `tfcreatures` is available in game next launch.
- `Mods\TerraFirmaBeta` — the 0.1.2 workshop upload source, comment-stripped, staged by
  `tools\publish-beta.ps1` and **independent of any further dev deploy**. Pushing 0.1.2 does
  not need master checked out again.

To go back to a clean patch build: `git checkout master`, then re-mirror `TerraFirma\` into
`Mods\TerraFirma`.

## Next on this branch

Spawn wiring, once the user has looked at them (their own order, from the tracker): snapjaw
party merges and canyon tables for the miner, dog/stray placement for the rockhound, canyon
wild for the baboon, dynamic-pool-vs-explicit-cave-table for the urchin. Then the stonecarver
roster on the compressed two-body shape, and the epithet-driven overseer builder that the
08-15 name-drives-kit ruling defined.
