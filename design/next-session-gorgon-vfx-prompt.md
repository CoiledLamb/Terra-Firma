# Next-session prompt: Gorgon's Gaze petrification VFX

**SUPERSEDED 2026-08-06 by `next-session-beta-close-prompt.md`** — this doc's "propose your build shape" stage completed 2026-08-05 (the hybrid proposal now lives at TODO's Gorgon wishlist entry, awaiting the four dial rulings), and the parallel-session warnings below no longer apply. Use the beta-close prompt instead.

Copy-paste the block below into a fresh session.

---

We're doing the Gorgon's Gaze VFX pass for Terra Firma (the Caves of Qud mod in `CoQ_TerraFirma-dev` — TODO.md there is the source of truth; read the "Animation wishlist" section first — the Gorgon's Gaze entry records my concept, my stated lean, AND the recon on how to build it — plus the "Gorgon's Gaze" section for the mutation's mechanics).

**The concept (mine, from the wishlist):** a pixel filter on the target that slowly shows the buildup of petrification.

**The design state (also in that TODO entry):** a true per-pixel filter on another object's sprite isn't reachable from mod land. Two approximations were laid out: (a) a turn-based Effect that walks the target's TileColor/DetailColor toward stone-gray per buildup stage, or (b) a realtime overlay of the target's own tile in stone colors, dithering in as buildup grows. My lean, recorded 2026-08-05: (b) "sounds the most accurate to what i meant but also the most complex and prone to bugs" — that's a LEAN, not a ruling. The recorded mitigation shape for (b)'s risk: don't build one persistent overlay tracking the channel (buildup advances in game turns, juice entries live in realtime seconds — they fight); instead, each channel tick enqueues a fresh SHORT-LIVED overlay at the target's current cell at that turn's tint level. Self-expiring, so a moving/dying/phasing target never strands a sprite, and it still reads continuous. Propose your build shape against this before writing code — including whether a hybrid ((a)'s effect driving (b)'s overlays) beats either alone.

**PARALLEL-WORK WARNING — this matters:** two other sessions are working VFX passes concurrently — Stoneshaper (actively editing `TerraFirma/Scripts/Fx_StoneRise.cs`) and Geomancy (in its own new Fx file). Do NOT touch `Fx_StoneRise.cs` beyond reading it — put your juice entries in a NEW file (e.g. `Fx_GorgonPetrify.cs`). Re-read shared files (TODO.md especially) immediately before editing, keep TODO edits scoped to your own lines, and only deploy files YOU changed — all three sessions deploy into the same live mod folder.

**Chassis lessons (read `Fx_StoneRise.cs`'s comments before any sprite code — Calcify/Fissure pass, signed off 2026-08-05):** re-apply colors every frame with an equality-defeating detailcolor nudge (the mesh-normals stomp turns the detail channel black on first-use pooled sprites); park spent sprites behind the map (z +100) immediately, never wait on `finish()`; pre-resolve tile paths before the first enqueue (game-thread atlas calls deadlock — two on record); keep new FX behind a `HarnessMute`-style kill switch. Note your subject is a CREATURE tile, not a painted wall — the target's own Render carries a real tile path directly (no painter-mask composition needed), but check how the base game snapshots creature renderables (`CombatJuiceEntryJump` takes an `IRenderable`; `AscendAnimation.SetClimber` is another reference) and mind HFlip. For the dither ramp, remember the overlay can be `Transparent: true` — the gaze overlay wants the target visible beneath it, the opposite call from the wall sprites' opaque backing.

**Where the mechanics live:** `Mutation_GorgonsGaze.cs` (`TerraFirma/Scripts/`). Recon the channel/buildup structure first — what ticks, what the buildup counter is, what interrupts it, and what already logs `[TF-VERIFY]` — before proposing the tint cadence.

**Working agreements (standing):**
- Compile check: `dotnet build TerraFirma.csproj -p:QudLibPath="C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed"` — NO trailing backslash. Any error is a real error.
- Deploy = copy changed files to `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma\`; fixes only activate on game restart (I test live and report back — remember which build my session is actually running, and that the parallel sessions deploy to the same folder).
- Validate XML with a strict XmlDocument.Load before deploying (`--` in comments is a repeat offender).
- `[TF-VERIFY]` instrumentation via `Helpers.VerifyLog`; stripped before release. Use a distinct tag (whatever the gaze code already uses, or GAZE) so your lines don't tangle with the other sessions'.
- No Co-Authored-By trailer on commits. Don't commit unless I ask.
- All player-facing text is DRAFT until I sign off. Design decisions are mine; tag your proposals as proposals.
- Iterate by feel rounds: build → I test → I describe what's off → adjust. Expect several rounds; the calcify pass took ~5.

Start by reading the wishlist entry, the Gorgon's Gaze TODO section, and `Mutation_GorgonsGaze.cs`'s channel flow, then propose the overlay shape (tick cadence, tint ramp, dither/alpha semantics, interrupt behavior) before writing code.
