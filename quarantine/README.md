# quarantine/

Held content, kept out of the mod since 2026-09-30. Nothing here compiles: `TerraFirma.csproj`
excludes `quarantine\**`, `TerraFirma2.csproj` compiles an explicit list, and `publish-beta.ps1`
packages only `TerraFirma/` and `TerraFirma2/`. Paths mirror the mod tree, so a file returns to
the same relative path it left.

Older held content is parked on the `quarantine-hold` branch (2026-08-22): the v1 rock catalog's
population tables, payload stones and kitchen, Lithovore, and the mill/cooking suite. The design
record for all of it is `TODO-quarantine.md`.

## What is here

| File | Held | Notes |
|---|---|---|
| `TerraFirma/Scripts/Part_TimedForm.cs` | 2026-09-30 | Lifespan for the ape statue and baboon statuette |
| `TerraFirma/Scripts/Part_FungalLottery.cs` | 2026-09-30 | Per-save spore shuffle for the fungal rocks (their blueprints are on `quarantine-hold`) |
| `TerraFirma/Scripts/_TestHarness-held.cs` | 2026-09-30 | `tfspore`, `SetupQuickApe`, `SetupForm` from the dev harness |
| `TerraFirma/XML/Rocks.xml` | 2026-09-30 | Baboon figurine item, ape statue, stone fist/bite, baboon statuette, chasm |

Left in the mod on purpose, because live code still uses them: `Part_FallenRock.cs` (Fissure,
Earthen Barrage's whorl, the TF2 cairn), and through it `Part_DrainLatch.cs` and
`Part_SurgeField.cs`; the Fissure spine in `Rocks.xml` (base fallen rock, pebbles, rubble,
crumbled wall, wall, redoubt figure, crack).

## Saves

Both moved classes shipped in 0.1.2.5, so `TerraFirma/Scripts/SaveCompatStubs.cs` keeps an inert
stub under each name that skips its saved data without a load error. Objects whose blueprint
moved still load from their saved parts; the engine logs "Unknown Blueprint" when something looks
the blueprint up, and falls back to `Object`.

## Restoring

Move the files back to the same paths under the mod, paste the XML objects back into
`TerraFirma/XML/Rocks.xml`, paste the harness methods back into `TestHarnessWishes`, and delete the
matching stubs from `SaveCompatStubs.cs`. Both builds must stay 0 errors.
