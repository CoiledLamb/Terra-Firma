# Next-session prompt: Geomancy pulse VFX

Copy-paste the block below into a fresh session.

---

We're doing the Geomancy VFX pass for Terra Firma (the Caves of Qud mod in `CoQ_TerraFirma-dev` — TODO.md there is the source of truth; read the "Animation wishlist" section first, my concept is recorded there, plus the "Geomancy" section for what the mutation actually does).

**The concept (mine, from the wishlist):** some kind of pulse/sonar effect that shows the radius where it falls.

**PARALLEL-WORK WARNING — this matters:** a second session is concurrently doing the Stoneshaper carve VFX, and it is actively editing `TerraFirma/Scripts/Fx_StoneRise.cs` (it has already added a chunk-fall juice entry there) and TODO.md. Do NOT add your code to `Fx_StoneRise.cs` — if you need a juice entry, create a NEW file (e.g. `Fx_GeomancyPulse.cs`) so the two sessions never touch the same script. Re-read any shared file (TODO.md especially) immediately before editing it, keep your TODO edits scoped to your own lines, and only deploy files YOU changed.

**Start with recon, not code:** read `Mutation_Geomancy.cs` to pin down what "where it falls" concretely means in the mechanics (my wishlist phrasing is loose — figure out what event has a landing cell and a radius, then propose). The cheap build per the wishlist recon is an expanding glyph ring via `XRLCore.ParticleManager` at the landing cell — Fissure's `SmokePuff` (public, in `Mutation_Fissure.cs`) shows the particle idiom, and the redoubt's `SHELL_SEC` (0.10s/ring in `Fx_StoneRise.cs`) is the tempo precedent for expanding-ring pacing. A particle-only build may not need the juice chassis at all, which also keeps you clear of the parallel session's file.

**If you DO end up needing pooled-sprite juice entries**, the hard-won chassis lessons live in `Fx_StoneRise.cs`'s comments (Calcify/Fissure pass, signed off 2026-08-05) — read them before writing any sprite code. Headlines: re-apply colors every frame with an equality-defeating detailcolor nudge (mesh-normals stomp turns detail black on first-use pooled sprites); park spent sprites behind the map (z +100) immediately, never wait on `finish()`; pre-resolve every tile path before the first enqueue (game-thread atlas calls deadlock — two on record); opaque sprites with black `'k'` backing; keep new FX behind the `Fx_StoneRise.HarnessMute` kill switch pattern.

**Working agreements (standing):**
- Compile check: `dotnet build TerraFirma.csproj -p:QudLibPath="C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed"` — NO trailing backslash. Any error is a real error.
- Deploy = copy changed files to `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma\`; fixes only activate on game restart (I test live and report back — remember which build my session is actually running, and that the parallel session deploys to the same folder).
- Validate XML with a strict XmlDocument.Load before deploying (`--` in comments is a repeat offender).
- `[TF-VERIFY]` instrumentation via `Helpers.VerifyLog` for anything worth verifying in Player.log; it's stripped before release. Use a GEO tag so your lines don't tangle with the stoneshaper session's.
- No Co-Authored-By trailer on commits. Don't commit unless I ask.
- All player-facing text is DRAFT until I sign off. Design decisions are mine; tag your proposals as proposals.
- Iterate by feel rounds: build → I test → I describe what's off → adjust. Expect several rounds; the calcify pass took ~5.

Start by reading the wishlist entry, the Geomancy TODO section, and `Mutation_Geomancy.cs`, then propose the pulse shape (trigger moment, ring glyphs/colors, tempo, radius semantics) before writing code.
