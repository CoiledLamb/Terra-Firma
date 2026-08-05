# Next-session prompt: Stoneshaper carve VFX

Copy-paste the block below into a fresh session.

---

We're doing the Stoneshaper carve VFX pass for Terra Firma (the Caves of Qud mod in `CoQ_TerraFirma-dev` — TODO.md there is the source of truth; read the "Animation wishlist" section first, my concept is recorded there).

**The concept (mine, from the wishlist):** when carving a statue from a wall, split the rock being cut into 6 chunks — each break in the carve drops the top two chunks off, progressively revealing the statue tile underneath the rock.

**Reuse the proven chassis, don't reinvent it.** The Calcify/Fissure animation pass (signed off 2026-08-05) lives in `TerraFirma/Scripts/Fx_StoneRise.cs` and every hard-won lesson is recorded in its comments. The ones that will bite you if ignored:

- Pooled-sprite juice entries (`CombatJuiceEntry` + `SpriteManager.GetPooledSprite`) are the chassis; `CombatJuiceEntryJump` is the base-game reference.
- **Mesh-normals stomp:** a first-use pooled sprite's init flush overwrites the detail-color channel with geometric normals → black detail. Fix already in `Cleo_TerraFirma_JuiceStoneRise.update()`: re-apply colors every frame with an equality-defeating nudge. Copy that pattern.
- **Vanish-at-handoff:** when the real object takes over, park the sprite behind the map (z +100) immediately — never let a sink/fall animation play in front of the settled tile, and never wait for `finish()` (the juice manager lags it).
- **Pre-resolve all tile paths before the first enqueue** — composing painter-style strings is fine, but never call `TryGetTextureInfo` from the game thread (two deadlocks on record).
- Opaque sprites (`Transparent: false`) with black `'k'` backing for negative space; colors resolved from blueprints via Render part params.
- Frame-pump loop for game-thread sync: `RenderBaseToBuffer`/`DrawBuffer` + `Thread.Sleep(40)`, settle schedule keyed per cell — see `Mutation_Calcify.HandleEvent(CommandEvent)` for the idiom.
- `Fx_StoneRise.HarnessMute` is the test kill switch; keep any new FX behind the same mute.

**Where the carve happens:** `Mutation_Stoneshaper.cs` (`TerraFirma/Scripts/`). Check how carve progress is actually structured before designing the chunk-drop cadence — the wishlist recon claims progress events exist code-side but verify that claim against the code, don't trust it.

**Working agreements (standing):**
- Compile check: `dotnet build TerraFirma.csproj -p:QudLibPath="C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed"` — NO trailing backslash. Any error is a real error.
- Deploy = copy changed files to `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma\`; fixes only activate on game restart (I test live and report back — remember which build my session is actually running).
- Validate XML with a strict XmlDocument.Load before deploying (`--` in comments is a repeat offender).
- `[TF-VERIFY]` instrumentation via `Helpers.VerifyLog` for anything worth verifying in Player.log; it's stripped before release.
- No Co-Authored-By trailer on commits. Don't commit unless I ask.
- All player-facing text is DRAFT until I sign off. Design decisions are mine; tag your proposals as proposals.
- Iterate by feel rounds: build → I test → I describe what's off → adjust. Expect several rounds; the calcify pass took ~5.

Start by reading the wishlist entry and `Mutation_Stoneshaper.cs`'s carve flow, then propose the animation shape (timing, chunk sprites, reveal mechanics) before writing code.
