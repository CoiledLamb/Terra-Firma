# Workflow guidelines

Directional. Meant to bend, argued from, not enforced. Provenance is a field on each entry.

## The carve sim is the design target; the game ports in passes

- ruled: 2026-08-07
- user: "it's fine if the sim gets ahead — wanna have the sim be what we're aiming for, and then iterate in game in passes"
- source: `JOURNAL.md:2599`
- applies to: quarry warren generation. The current sim is `design/warren-lab-v2.html`; `WarrenCarver.cs` catches up in deliberate port passes. The source entry describes the earlier tile-sketch/ZonePainter pair.
- shape: drift is expected and directional, sim leads. "Port-pending" flags are the normal state of the pipeline, not debt. A port pass closes them in batches when the user calls for one. Never reconcile the sim backward to match the game.
- supersedes: the "C# is ground truth" framing from earlier the same day.
