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
| `TerraFirma/Scripts/Part_DrainLatch.cs` | 2026-09-30 | The drainstone's cursed latch that drinks life (blueprint on `quarantine-hold`) |
| `TerraFirma/Scripts/Part_SurgeField.cs` | 2026-09-30 | The deep galvena's arcing field (blueprint on `quarantine-hold`); class `Cleo_TerraFirma_SurgePart` |
| `TerraFirma/Scripts/_TestHarness-held.cs` | 2026-09-30 | `tfspore`, `SetupQuickApe`, `SetupForm` from the dev harness |
| `TerraFirma/Scripts/Part_FallenRock.cs` | 2026-09-30 | The v1 stone engine: step payloads, the payload switch, pathfinding asymmetry, fragility, throw sting, ricochet, dull clock. Pebbles were the last live carrier and used none of it |
| `TerraFirma/Scripts/_LiveFileFragments-held.cs` | 2026-09-30 | Code cut from live files: Fissure's keeper salvage, crack gates and ring figure (held since 2026-08-03) with their constants and band tables; FallenRock's `FallThrough`, `Latch` and surge-owner lines |
| `TerraFirma/XML/Rocks.xml` | 2026-09-30 | Baboon figurine item, ape statue, stone fist/bite, baboon statuette, chasm, redoubt figure, crack |
| `design/warren-lab-v2.html` | 2026-09-30 | The warren carve sim. Retired because warrens now generate in the base game; `WarrenCarver.cs` no longer ports from it |

Left in the mod on purpose, because live code still uses them: the Fissure spine in `Rocks.xml`
(base fallen rock, pebbles, rubble, crumbled wall, wall). The code cut from live files is in
`_LiveFileFragments-held.cs`, each fragment labeled with where it goes back.

## Saves

One save-compat stub: `TerraFirma/Scripts/SaveCompatStubs.cs` keeps `Cleo_TerraFirma_FallenRockPart`,
because 0.1.2.5 Fissure dropped pebbles carrying it. The stub skips the saved fields without a load
error (the engine reads a part's public fields in order, so a trimmed class would desync the save).
Every other moved class shipped in 0.1.2.5 but had no spawn path in normal play there: Fissure dropped only the spine (generic, pebbles, rubble, crack, walls), and the
catalog tables were reachable only through Lithovore (not in that build's `Mutations.xml`), the
statuette's own inventory, or wishes. A save that does hold one loads anyway: `IPart.Load` skips
the part's block and the game shows its load-error popup once. Objects whose blueprint moved load
from their saved parts; the engine logs "Unknown Blueprint" on lookup and falls back to `Object`.

## Restoring

Move the files back to the same paths under the mod, paste the XML objects back into
`TerraFirma/XML/Rocks.xml`, and paste the harness methods back into `TestHarnessWishes`. Restoring
`Part_FallenRock.cs` means deleting its stub in `SaveCompatStubs.cs`. Both builds must stay 0 errors.
