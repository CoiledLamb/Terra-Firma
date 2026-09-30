# Save-format versioning: branch on ModVersions; the sentinel parts stay until TF2 1.0

- status: closed 2026-09-30, in effect
- expires with: the TF2 1.0 strip (the save break that retires both legacy read paths)

## In effect

- **New layout changes** branch on `Reader.ModVersions`, the mod ID to manifest version map the
  engine writes into every save header (see `ground-truth.md`, "Saves record each active mod's
  version"). No new magic ints. A check must look up every ID the mod saves under
  (`Cleo_TerraFirma`, `Cleo_TerraFirmaBeta`, `Cleo_TerraFirma2`), and a layout change needs a
  manifest version bump to be told apart.
- **The `SERIAL_SENTINEL` parts** (`Cleo_TerraFirma_EarthenBarrage`, `Cleo_TerraFirma_Etchable`) stay as they are:
  saves since 2026-08-07 carry the stamp, so their read paths must stay. Switching them to
  `ModVersions` was weighed and not taken; it trades one branch for another.
- **At the TF2 1.0 strip** both legacy read paths and the sentinel retire with the save break.

The `publish-beta.ps1` save-layout guard still blocks unplanned layout changes; this covers
deliberate ones.
