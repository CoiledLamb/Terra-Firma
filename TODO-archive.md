# Terra Firma TODO archive (resolved history)

Moved verbatim out of TODO.md on 2026-08-06 to keep the live tracker scannable.
Nothing here is deleted history: build records, verify-session logs, superseded
specs kept for reference, decomp ground truth, and the automated-harness saga.
Provenance discipline applies here too: RULED markers in this file still count
when citing precedent - grep BOTH files before calling anything a rule.
Headings are preserved, so sections keep their original names.

---

## Animation wishlist (2026-08-05 — recorded, not scheduled at the time; three of four riffs built + signed off 2026-08-05/06)
*(closed items lifted from the live block of the same name; open flags stayed in TODO.md)*

- [x] `[design]` **Stoneshaper carve** (USER): split the rock we're cutting into 6 chunks; each break drops the top two chunks, progressively revealing the statue tile underneath. *(Claude recon, unruled: chunk sprites = the same pooled-sprite chassis with falling motion instead of rising; "reveal underneath" = draw the statue tile on the cell while overlay chunk sprites cover it, shedding a pair per break event. ~~Carve progress events already exist code-side.~~ **CORRECTED 2026-08-05 on code read: FALSE — the carve is one instantaneous action in `CarveFrom` (wall obliterated + statue placed in a single cast, no progress ticks); the breaks must be realtime choreography beats inside the cast, Calcify-ripple style.**)* **BUILT + DEPLOYED 2026-08-05 (shape user-approved same day: "this sounds perfect"; compiles clean, hash-verified — active next restart):** 2x3 chunk mosaic over the carved wall (isolated-mask wall tile at 8x8 chunk scale, wall's own dressing, opaque + black backing), wall→statue swap at 0.20s HIDDEN behind the mosaic (lead doubles as juice spin-up cover), breaks at 0.35/0.65/0.95s each dropping the top remaining pair (gravity-eased fall, slight outward drift, falls in front of standing rows, park-behind-map exit), SmokePuff + `sfx_damage_stone` per break, ~1.3s total pump. New `Cleo_TerraFirma_JuiceStoneChunk` + `PlayCarveReveal` in Fx_StoneRise.cs; pump in `CarveFrom`; unseen/harness-muted casts place instantly as before. Dials CARVE_*/CHUNK_* in Fx_StoneRise.cs. NEW HAZARD GUARDED: sprite localScale is pool state — captured in start(), restored in finish(). ~~FLAGGED FEEL ITEMS: non-uniform squash of the 16x24 tile into 8x8 chunks; the t=0 wall→mosaic discontinuity (intended as the fracture read).~~ **FEEL ROUND 1 (2026-08-05, user: the mosaic cover read as "6 miniature walls"; want "chunks of a whole stone" that "bounce up a bit before falling") — REBUILT + DEPLOYED same day, compiles clean, hash-verified:** the cover is now ONE block sprite (`Cleo_TerraFirma_JuiceCarveBlock`) wearing the wall's OWN Render.Tile — pixel-identical to the standing wall at t=0, both round-1 flags mooted — shrinking bottom-anchored by a third per break (the carve-down read; vertical squash of the tile art is the new feel watch-item); debris chunks are now small uniform-scale (0.4) bits on a ballistic arc (hop ~5px up over 0.1s, gravity down, slight outward drift, 0.40s airtime), hidden until their break, popping off the block's current top edge. **FEEL ROUND 2 (2026-08-05, user: round-1 debris were "just miniature walls"; asked to "literally break apart the sprite" with the broken parts falling off it) — REBUILT + DEPLOYED same day, compiles clean, hash-verified. NEW MECHANISM, decomp-verified first (ex3DSprite2/exTextureInfo/SpriteBuilder/exSpriteUtility read end-to-end): sub-rect atlas clones.** `exTextureInfo` is a plain ScriptableObject holding an atlas rect; `CloneSubRect` (new, Fx_StoneRise.cs) clones the pooled sprite's info down to a fraction of its rect, and assigning the clone resizes the mesh to the sub-region (SetTextureInfo queues Vertex+UV; SpriteBuilder UV math confirmed y-up, textureInfo.y = tile's BOTTOM edge). The block now shows the wall tile's ACTUAL surviving bottom pixels at natural size (squash watch-item mooted), each debris chunk wears the LITERAL half-band of pixels that broke off. Safety rails: the cached info is shared with world rendering — never mutated, only cloned; clones destroyed at finish(); textureInfo restored (alongside scale) before pool Return; all atlas-info work Unity-side in entry start() (game thread still only composes strings — freeze lesson intact); rotated/diced atlas entries fall back to round-1 squash/scaled looks. ~~`[verify]` eyes-on recast~~ **EYES-ON PASSED + SIGNED OFF 2026-08-05 (user: "oh my god that's awesome! it's just like how i envisioned it") — the carve reveal is DONE as built.** Residual low-stakes watch items for any later session: grep Player.log for "No texture found" (composed-path gaps) and keep half an eye out for any shrunken/mis-textured later juice effect (would implicate the pool guards; none expected). **DUST RELOCATED 2026-08-05 (user, post-signoff: smoke during the carve hid the statue, "calcify also has this issue"): new `DustAtBase` beside SmokePuff in Mutation_Fissure.cs** — foot-of-tile spawn (y+0.55, just below the face), gentle down-and-outward fall, dies within ~half a cell (~150-220ms), no blip flash; the carve breaks and Calcify's per-wall landings now use it, Fissure's own puffs keep SmokePuff unchanged (nothing to see under a heaving rim). Deployed hash-verified (compile-verified via shadow build — see the concurrent-session note below). **SUPERSEDED SAME DAY BEFORE TESTING (USER RULED 2026-08-05: "instead of that, can we just have the smoke spawn at the end or something?" — i.e. keep the normal SmokePuff look but fire it AFTER the reveal completes, not relocated foot dust; impl HELD, user: "hold off on impl for a bit").** ~~The DustAtBase build stays deployed in the interim~~ **BUILT + DEPLOYED 2026-08-05 (user go-ahead + puff-count ruling: "a random amount of puffs... between 1-4"; compiles clean, hash-verified):** carve fires `SmokePuff(cell, Stat.Random(1,4))` at pump end with the statue fully revealed; Calcify rolls 1-4 puffs after its line finishes, each landing on a random cell of the line; `DustAtBase` deleted (nothing else had adopted it); SmokePuff's doc updated. Mid-animation dust is gone from both — the per-break crack sounds stay. Same sitting, infrastructure: a new `publish\` snapshot folder under the repo was double-compiling every script (CS0101 wall) — csproj gained a `DefaultItemExcludes` for `publish\**` (rides the existing trustworthy-builds cleanup note). The parallel session's Geomancy pulse work now compiles green in the same build. `[verify]` eyes-on: carve + calcify reveals stay smoke-free until done, then 1-4 puffs drift up off the finished work. **CONCURRENT-SESSION NOTE 2026-08-05 ~13:00: a parallel session's in-progress Geomancy pulse work (`Fx_GeomancyPulse.cs`, new + mid-refactor with unresolved names, plus `Mutation_Geomancy.cs` call sites) does not compile and IS DEPLOYED — the mod will MODERROR on next game restart until that session lands its fix. The dust fix here is independent (verified against the committed Geomancy via shadow build) and rides whichever restart follows.** **CLEARED 2026-08-06 (user: "the animation riffs should all be clear").**
- [x] `[design]` **Geomancy** (USER): "some kind of pulse/sonar effect that shows the radius where it falls." *(Claude recon, unruled: an expanding glyph ring via ParticleManager at the landing cell is the cheap build; the redoubt's shell timing (`SHELL_SEC`) is the tempo precedent.)* **BUILT + DEPLOYED 2026-08-05 (shape user-approved same day — pulse + gradual unveiling as one package, thin glyph layer; compiles clean, hash-verified — active next restart):** two halves on one sweep clock — (1) GLYPH FRONT: one delayed text particle per revealed cell, delay ∝ true Euclidean distance from the landing cell (80ms/ring, dial `RING_MS`; particle `delayMS` decomp-verified, so NO pump loop and NO juice chassis — new file `Fx_GeomancyPulse.cs`, zero contact with Fx_StoneRise.cs); (2) UNVEILING: the mutation's per-frame lighting now gates each cell on a `RevealAtMS` stamp from the same clock, and `SetExplored` defers to front arrival — darkness + automap genuinely recede with the ring. Distortion-masked cells never blip or light (a hole in the pulse is a hole in your senses). Gemstone scan throws pulse crystal cyan `&C` vs stone's warm `&W`. Muted under `HarnessMute`/inactive zone (instant reveal as before, incl. save/load — sweep state deliberately unserialized); deliberately NO `IsVisible()` gate (dark landing cells are the point). Engine quirk coded around: ParticleManager.Render stops at the first pending delay in add-order, so particles enqueue sorted ascending. GEO verify lines: "pulse: N cells unveiling over Xms, ring dots=K" / "pulse: skipped". **FEEL ROUND 1 (2026-08-05, user: didn't love the ø glyph, wanted "a sort of sonar ring"; gemstone cyan CUT same round, user: "unspoken rules like that can add confusion"):** rebuilt as tangent-stroke arcs (`|` `-` `/` `\` per cell). **FEEL ROUND 2 (same day, user: strokes "pretty chunky, with a lot of noise"; floated spriting a ring — Claude diagnosis: the noise is per-cell BLINKING, not the glyph, so sprite deferred as the escalation) — REBUILT + DEPLOYED, compiles clean, hash-verified: SMOOTH RADIAL DOT RING.** The front is now a fixed set of `ú` middle-dot particles (firefly's own char) gliding continuously outward on the engine's RADIAL particle mode — constant dots in constant motion, zero blinking; ~6 dots per radius (min 12, cap 80 under the engine's 100-radial cap), random phase per cast, life tuned to die exactly at the farthest revealed cell, `*` ping at the landing cell. Division of honesty: radial paths in cell space trace the same screen ellipse as the Euclidean reveal (the ring carries the SHAPE), distortion-masked cells still never light (the unveiling carries the TRUTH — a masked ring dot now glides over dark cells rather than gapping; accepted round-2 tradeoff). Unveiling half unchanged. Dials: `RING_MS` tempo, `DOTS_PER_RADIUS` density. **FEEL ROUND 3 (2026-08-05, user concept: the torch-fire read — "a wave of cyan/white/blue squares radiate out"; mechanism recon: the torchpost's flame squares are a Unity prefab imposter, `PrefabImposter Prefabs/Particles/TorchpostFlames` blueprint-verified — NOT reachable for a custom wave from mod land, so the LOOK is recreated in text particles) — REBUILT + DEPLOYED same day (FX file only, call site unchanged), compiles clean, hash-verified: TORCH-SQUARE WAVE.** Each radial particle is now an ember-square (`þ` small centered square, sparse `Û` full blocks — CP437 slots 254/219) with per-particle random color from a cyan-leaning white/blue palette (&C&C&Y&B&c), ragged front edge (start d 1.0–1.3) so it reads as fire not geometry, plus a dim wake ring (&c&b&K, half-step angular offset, 70ms behind) for wave thickness; `&Y*` impact ping. ~6 squares/radius ×2 waves, cap 40+40 under the engine's 100-radial ceiling. Motion/tempo/unveiling all unchanged from round 2. **FEEL ROUND 4 (2026-08-05, user: "more of them, and they'd be smaller, like a shimmering wave") — REBUILT + DEPLOYED same day (FX file only), compiles clean, hash-verified: DENSE SHIMMER WAVE.** Moved off the radial-particle list (hard cap 100) onto the TEXT-particle list (cap 1000) — straight-line velocity from center = radial motion — enabling 18 sparks/radius (min 30, cap 240). Sparks are tiny: mostly `ú` (CP437 250, the 2–3px dot-square; the font is square pixels, a dot IS a small square), some `þ`, full blocks gone. Shimmer = per-spark randomness in one generation: color rolled from the mixed bright/dim cyan/white/blue palette (&C&C&Y&B&c&b interleaved), ±12% speed jitter (sparks drift past each other), 0–90ms onset jitter (twinkling arrival), angular jitter. Separate wake ring deleted — the jitters make the band self-thicken. Sparks enqueue sorted by delay (the text-list early-stop quirk, load-bearing). Dials: `SPARKS_PER_RADIUS`, `DELAY_JITTER_MS`, `SPEED_JITTER_PCT`, `RING_MS`. **FEEL ROUND 5 (2026-08-05, user REALIGNMENT: the 1-thick ring line should be composed of "hundreds of the small pixels that compose the brazier's light" — a dozen per segment, sub-GLYPH scale; structure right, execution missed; rounds 1–4 could never get there because a text particle is one font glyph per cell) — REBUILT + DEPLOYED same day, compiles clean, hash-verified: PIXEL WAVE VIA TILE PARTICLES.** `ParticleManager.AddTile` (base-game precedent: `Cell.ParticleTile`, the GameObject death flash) renders an arbitrary TEXTURE per particle with the same velocity/delayMS machinery — each spark is now a 16×24 cluster texture of ~a dozen scattered 1–2px squares on transparent ground, ~7 clusters/radius (one per rim cell; a cell shows at most one tile particle), 3 texture variants × HFlip/VFlip = 12 looks, per-cluster (ColorString, DetailColor) pairs from the mixed bright/dim cyan/white/blue palette. Each texture deliberately carries pixels in BOTH channels (black + white) so every cluster is two-tone regardless of which channel the dual-color shader maps to which. **NEW ASSETS — PLACEHOLDER ART, user owns sprites: `Textures/fx_geomancy_spark1-3.png` (generated scatter; redraw freely, keep filenames).** Tile-list early-stop quirk same as text list — delay-sorted enqueue kept. Motion/tempo/jitters/unveiling unchanged from round 4. **FEEL ROUND 6 (2026-08-05, user: "this is closer — look at how brazier does the fire animation part"; recon: brazier/campfire flames = Unity ParticleSystem prefabs pooled per screen cell by the imposter system (`ImposterExtra`/`PrefabImposter`, decomp-read end-to-end; prefabs compiled + fire-orange + looping, unreachable for a custom wave — even BlueCampfire shares the orange prefab), and their life = CONTINUOUS EMISSION: short-lived pixels constantly replaced) — REBUILT + DEPLOYED same day (FX file only), compiles clean, hash-verified: EMITTER SPOKES.** Each spoke now walks outward dropping a rapid succession of short-lived cluster generations (~55ms cadence, ~64ms life, fresh texture/flip/color-pair per generation, per-emission timing scatter) — every ring position's pixels REROLL several times as the front crosses it, the brazier's renewal principle on the expanding wave. Budget ~spokes×generations, self-capped 700. New dials: `GEN_MS`, `GEN_LIFE_TICKS`, `GEN_JITTER_MS`. **Escalation still recorded, not built: pooled-sprite ring / imposter-carrier objects.** **FALLBACK PREPARED same day (user: "if this one doesn't work for me i wanna just go back to our first iteration"): the round-1 per-cell ø-glyph front is preserved VERBATIM in-file behind `CLASSIC_FRONT` (const bool, default false) in Fx_GeomancyPulse.cs — round 1 predates every commit of this work, so it lives in code, not git; restoring = flip the const, rebuild, redeploy (unveiling identical in both modes; round 1's gemstone cyan stays cut by ruling).** **FEEL ROUND 7 — RULED 2026-08-05 (user: "let's go back to original, not feeling this one"): classic front restored.** **FEEL ROUND 8 (same day, user found THE reference: "time dilation/electromagnetic pulse - the wave it sends out is a *great* precedent") — BUILT + DEPLOYED, compiles clean, hash-verified: THE TD WARP WAVE.** Decomp ground truth: TimeDilation.Render fires 360 `@` particles per ring with per-degree velocities, and the particle renderer's `@` branch draws no glyph — it COPIES the neighboring cell's char/tile/colors into its own cell: a transparent LENS RIPPLE through the terrain, warping the world instead of painting on it (thematically exact for earth-vibration sense; engine auto-swaps `@`→`*` under OptionDisableTextWarpEffects). EMP's wave evaluated too and rejected as mechanism: it's a Thread.Sleep frame-pumped band — wrong for a per-throw effect (TD's is fire-and-forget). Build: TD's idiom with our radius semantics — 360 warp particles/ring launched from the landing cell at uniform speed (16/RING_MS cells/tick), life set to die exactly at the revealed rim, plus a second echo ring at 0.65× speed (sonar double-ping; dials `WARP_PARTICLES`, `WARP_RINGS`, `RING_SPEED_FACTORS`). Unveiling unchanged. **Rounds 3–6 machinery (tile clusters, emitter spokes, palettes) DELETED from the file; `CLASSIC_FRONT` fallback (round-1 ø front) retained one flip away; `fx_geomancy_spark1-3.png` now fully orphaned — prune at a cleanup pass.** GEO pulse line: `sparks=720, classic=False`. ~~`[verify]` eyes-on: throw → a distortion ripple (not drawn glyphs) expands to the revealed rim with the unveiling behind it, echo ring trailing; feel items = tempo, echo ring keep/cut, ripple density.~~ **EYES-ON PASSED + SIGNED OFF 2026-08-05 (user: "geomancy is also good from the vfx pass") — the TD warp wave ships as built.** **CLEARED 2026-08-06 (user: "the animation riffs should all be clear").**
- [x] `[design]` **Gorgon's Gaze** (USER, ends in "?"): "a pixel filter on the target that slowly shows the buildup of petrification." *(Claude recon, unruled: a true per-pixel filter on another object's sprite isn't reachable from mod land, but the same read lands two ways: (a) turn-based — an Effect shifts the target's TileColor/DetailColor toward stone-gray per buildup stage, the engine idiom for tint effects; (b) realtime — overlay a pooled sprite of the target's own tile in stone colors and dither its alpha up as buildup grows. (a) is cheap and save-safe; (b) is prettier and rides the Fx chassis.)* **USER LEAN 2026-08-05: (b) "sounds the most accurate to what i meant but also the most complex and prone to bugs"** — lean, not a ruling. *(Claude follow-up recon: the real hazard isn't the overlay, it's the turn/realtime bridge — buildup advances in game turns, juice entries live in realtime seconds, and a single persistent overlay tracking a multi-turn channel fights the juice system's fire-and-forget design. Mitigation shape: per channel tick, enqueue a fresh SHORT-LIVED overlay at the target's current cell at that turn's tint level — self-expiring, so a moving/dying/phasing target never strands a sprite; still reads continuous. Every overlay-sprite bug of the 2026-08-05 calcify session (lingering sprite, normals stomp, backing channel) is now a solved precedent in `Fx_StoneRise.cs`.)* **CLAUDE PROPOSAL 2026-08-05 (build shape, AWAITING RULING — no code written): HYBRID, on a new recon find that redraws the (a)/(b) map.** Decomp find: `Effect.Render(RenderEvent)` is the engine's own idiom for tinting a creature's sprite non-destructively — recomputed every rendered frame, riding the target wherever it draws (RenderForUI routes through ComponentRender, so the look-UI portrait tints too); precedent is BasiliskPoison, the base game's own "stony" venom, which flashes `&K` on a frame cadence. This IS a reachable "filter on the target's own sprite" — just palette-quantized (16 color chars, no smooth lerp), which is exactly what the overlay half is for. Proposed split: (1) BASELINE CONTINUITY — a serializable `Stage` int on Effect_Petrifying plus a Render-hook ramp stepping the target's fg/detail chars toward stone grays as fails accrue (~5 visible steps over 10 fails); zero turn/realtime bridge, zero stranded sprites, save-safe by construction; a receding effect fades the ramp back out on its own countdown, and Effect_PartiallyPetrified gets the same hook for a faint steady stony cast over its 500 rounds. (2) FAIL-TICK IMPACT — per failed save, one short-lived juice pulse (NEW `Fx_GorgonPetrify.cs`, zero contact with Fx_StoneRise.cs): snapshot the target's RenderForUI tile/flip/colors INTO AN OWNED renderable (RenderForUI returns a reused buffer — never hand the live ref to a juice entry), enqueue at the target's current cell, `Transparent: true` with 'k' backing (alpha-0 backing, only the silhouette draws — the opposite call from the wall sprites, as flagged), colors smooth-lerped creature→stone-gray at that tick's fails/10, ~0.5–0.7s hold then park-behind-map; full chassis guards (per-frame normals repair, game thread composes strings only, own `HarnessMute`, GAZE verify lines). Interrupts need NO teardown anywhere: pulses self-expire, the ramp lives and dies with the effect (dead-gorgon self-recede and the statue swap both come out clean for free). Why hybrid beats (b)-alone: pure overlays carry continuity gaps (a pulse expires while the player idles mid-channel) and every pool hazard scales with hold time — the ramp carries continuity for free and shrinks overlay exposure to brief pulses. Why it beats (a)-alone: TileColor mutation is destructive and serialization-exposed, the Render hook is the idiom (a) was groping for, and the palette steps alone can't give the smooth "slowly shows the buildup" read — the pulses restore it. Open dials for the ruling: tint tracks the FAILS TALLY (monotonic; recommended — it's what the statue counts) vs stage ladder; fractional-alpha cross-fade on the pulse kept as a feel-round dial (binary alpha verified in GetPooledSprite, fractional blending unverified); temporal-dither flicker recommended AGAINST for continuity (Geomancy round-2 blinking lesson) — at most a single-frame accent on the fail moment; optional final full-gray pulse under the statue swap (cheap, recommended); stone-mark ramp in or out. **DESIGN CONVERSATION 2026-08-06 (user deferred the four dials: "we should talk through it before impl or building anything. biggest design question in the session") — the hybrid proposal was SUPERSEDED by the talk.** User reframe: "the mask/dither effect (not tint) makes sense as both the animation and the breaking out" — the dither IS the effect, running both directions, and "the effect persisting to show you still have the debuff could work." Claude mapped three reachable builds (temporal dither via the render hook — BasiliskPoison decomp-verified at frames 1-9 of a 160-frame cycle; FEET-UP BAND MASK via CloneSubRect on a stone-colored snapshot of the target's own tile — new find, the carve reveal's tech; speckle-crust overlay textures — ranked last, can't hug the silhouette). **USER RULED: option 2, the rising stone band ("i lean more 2!" + build green light "yeag we can try building it and working from there!"). Sub-rulings riding the conversation: band tracks NET state** (fails minus successes — the tug-of-war read, matches the level text's "each success reverses one stage"; the STATUE still counts cumulative fails — deliberate visual/mechanical divergence, do not bug-report it) **· stone mark = detail-channel statue-gray via Effect.Render hook** (no long-lived overlay; bridges the escape handoff so no flesh-snap while the debuff stands) **· fail-beat accent DEFERRED to a feel round** (the band lurch is the beat; try dry first) **· sub-4-fails escape = band slides off, no residue (deliberate silence) · statue completion = band tops out, no separate gray pulse · temporal dither stands as the recorded FALLBACK** if the band read fails at sprite scale (shares the effect-side plumbing). **BUILT + DEPLOYED 2026-08-06 (compiles clean 0 errors, hash-verified — active next restart):** new `Scripts/Fx_GorgonPetrify.cs` (zero contact with Fx_StoneRise.cs beyond its public CloneSubRect) — ONE long-lived juice entry per channel driven through a shared volatile-field state object (game thread writes, Unity side reads; no juice entry ever touches a live GameObject), stone-colorized snapshot of the target's own tile ('y'/'K' two-tone — statue canon is flat &y/y, feel dial) clipped to exact-pixel bottom bands via cached sub-rect clones, `Transparent: true` with 'k' backing (silhouette only), band ladder net 1→12% to net 9→92% (`FracForNet`), grow ~0.22s/step + shrink-out ~0.6s (`GROW_PER_SEC`/`SHRINK_OUT_PER_SEC` dials), bottom-anchored (Kobold atlas stores untrimmed rects — decomp-verified `trimElements=false`), rotated/diced fallback = carve-block squash. **Lifecycle finds, decomp-verified:** `CombatJuice.startTurn` force-finishes every default entry two turns after enqueue → the band overrides `canFinishUpToTurn() => false` (the engine's own escape hatch); early death = zero own `duration` (manager's per-frame loop collects same frame, no turn-cadence lag); `[CallAfterGameLoaded]` GlobalGen bump orphans pre-load entries. All chassis guards: normals-stomp repair every frame, park-behind-map on every exit, scale+textureInfo pool restore, clones destroyed at finish, own `HarnessMute`, GAZE band log lines. Wiring: `UpdateBandFx` on fail + success ticks in `TickGaze`; shrink-release in `CancelGaze` (all end paths — over a fresh statue the gray-on-gray shrink is invisible by design) + `Unmutate`; defensive instant-release in `BeginGaze`; band state `[NonSerialized]` (post-load tick re-creates within one round). Mark hook in `Effect_PartiallyPetrified.Render` (detail "y" while Duration>0; creatures already detail-'y' show no cast — accepted). Out-of-sight/text-mode/muted = instant band down, next visible tick re-creates. WATCH ITEMS for feel rounds: net-1 band is ~3px (readability at sprite scale), facing flip mid-channel vs snapshot flip, band vs statue shade mismatch (y/K vs y/y). `[verify]` eyes-on: gaze a tough target — stone climbs from the feet per fail, slides back per success, sheds on escape (≥4-fail escapees keep the faint detail-gray for 500 rounds, look-UI portrait included), statue lands under a topped-out band; save→load mid-channel re-grows the band within a round; GAZE `band (fail|success)` lines in Player.log. **FEEL ROUND 1 (2026-08-06, user: band texture "pretty cool"; change asked: "instead of * coming off the enemy as you petrify them, maybe we could have small rock textures pop out periodically?") — BUILT + DEPLOYED same day (compiles clean, hash-verified):** both asterisk sites (per-fail `Splatter`/`ParticleBlip` and the statue-moment blip-20) replaced by small rock debris — pooled sprites of base-game stone item tiles (`sw_smallstone`/`sw_stone_large`, guaranteed atlas residents, random flips, scale 0.20–0.35, stone 'y'/'K' dressing) on the carve debris's ballistic arc, popping off the body at z −11 (in front of the band): 2–4 per failed save scaling with the tally, a 10-rock hard burst under the statue swap. Same gate as the band (muted/unseen/text-mode = skip), same turn-sweep decline, scale captured/restored as pool state. Dials `CHUNK_*` in Fx_GorgonPetrify.cs. **FEEL ROUND 2 (2026-08-06, user flag: band invisible on gray-bodied creatures — feral dogs wear the band's own `&y`; talked through four levers, user chose + refined the waterline): THE MIXER WATERLINE, BUILT + DEPLOYED same day (compiles clean, hash-verified).** The band's top edge now carries a 2px line in the game's MIXER-SHADE idiom — the target's main and detail render colors mixed together, "a third color in addition to black and white" (user correction mid-build recorded: the mixer contributes NO pigment of its own, it mixes THE CREATURE'S two channels; an earlier earth-partner reading with user-supplied hex #7b6529 was SUPERSEDED before testing — the hex is retired unused). Implementation: second pooled sprite in the same juice entry wearing the tile's top-of-band slice (own clone cache, both channels flattened to the mixer shade computed once at snapshot from ColorMap, z −10.5 between band and debris, parks/restores/destroys alongside the band; squash-fallback path skips the line — no honest pixel edge to mark). Gray dog ⇒ line inherits its detail channel's warmth; a creature with BOTH channels the same gray keeps an invisible line by canon (mixer is honest, not decorative). Dials `LINE_HEIGHT_PX`/`LINE_MIX`. `[verify]` eyes-on a feral dog: the transition line sweeps up/down with the band and stays legible; look for any 1px seam between line and band. **FEEL ROUND 3 (2026-08-06, user: "can we apply some dithering to the waterline so it's not just flat yet?") — BUILT + DEPLOYED same day (compiles clean, hash-verified): THE DITHERED FRONTIER.** Three moves replace the flat 2px bar: (1) SPATIAL — the line rows no longer flatten both color channels; fg wears the mixer family and detail wears the creature's detail family, so the tile art's own pixel distribution breaks the line into two live shades for free; (2) GRADIENT — the line is now two independent 1px row sprites, the lower stepped `LINE_ROW_DARKEN` toward stone dark (a soft edge into the gray band beneath); (3) TEMPORAL — both rows breathe toward stone gray and back on a slow offset sine (`LINE_SHIMMER_SEC` 1.4s, amp 0.35, phase 0.9 rad between rows — slow enough to read as the line working, not blinking; the Geomancy lesson honored by tempo, not abstinence). Each row keeps its own clone cache/park/restore lifecycle; a 1px band shows a single row (lower row parks rather than wrapping below the tile). Dials `LINE_ROWS`/`LINE_ROW_DARKEN`/`LINE_SHIMMER_*` (`LINE_HEIGHT_PX` retired into `LINE_ROWS`). `[verify]` rides the same eyes-on: frontier should shimmer subtly and read textured, not strobed. **FEEL ROUND 4 (2026-08-06, user: "i wish the texture wasn't so solid on the band") — BUILT + DEPLOYED same day (compiles clean, hash-verified): POROUS BAND.** The band's detail channel now renders fully transparent (the engine's own 'k'-under-Transparent alpha-0 path — the exact verified mechanism the backing uses), so stone covers only the art's fg-channel pixels and the creature's detail pixels stay alive THROUGH the stone: a lattice following the sprite's own pixel structure. One-char mechanism, `BAND_POROUS` const to revert. Watch item recorded: a creature whose art is mostly detail-channel reads mostly un-stoned (the flip — stone the detail, open the fg — is the same one-char change if one surfaces). **Same round, user observation RECORDED, no change (user: mixer "can result in weird shades on creatures that have strange detail colors (but overall is a boon for visibility)"):** weird-shade cases accepted as the cost of the honest mixer; available lever if it ever grates = lerp the mixer a step toward stone gray before use (one const, not built). **FEEL ROUND 5 (2026-08-06, user clarification of the round-3/4 intent: "i was just thinking a serrated/dithered edge, but that's an interesting idea" — porous band KEPT alongside as the interesting accident, `BAND_POROUS` flips it off) — BUILT + DEPLOYED same day (compiles clean, hash-verified): THE SERRATED EDGE.** The band is now `SERRATION_COLS` (4) independent column strips, each biting 0–`SERRATION_DEPTH_PX` (2) px below the true height — teeth from a proper integer hash of (column, height) (deterministic, stable between frames, crawling as the band climbs; a linear formula was rejected in-build for producing a repeating staircase). Teeth never exceed the honest fill and never cut a column to nothing (min 1px). The waterline rows now ride EACH COLUMN'S own top edge (the line follows the jagged contour). HFlip handled: GetPooledSprite mirrors each sprite about its own center, so column POSITIONS mirror too. Sprite budget per channel: 4 band + 8 line = 12 pooled sprites, all under the existing lifecycle (per-sprite clone caches/restore/destroy, ParkAll on every exit). `[verify]` same eyes-on: edge reads jagged/broken, not slanted; teeth crawl as the band moves; flipped-facing creatures serrate correctly. **GORGON VFX FEEL PASS EFFECTIVELY CLOSED 2026-08-06 after round 5 (user: "gorgon is basically perfect now?") — shipping as built: serrated porous band + mixer waterline + rock debris + mark detail-gray. Any last look rides Friday morning per the schedule; the fourth VFX pass of the beta bar is done.** **CLEARED 2026-08-06 (user: "the animation riffs should all be clear").**

---

## Gorgon's Gaze  ← audit in progress, furthest along

### Done
- [x] `[bug]` ~~Cooldown mismatch (69 applied vs 130 declared)~~ — **fixed:** `BASE_COOLDOWN = 80`, matching Sunder Mind's decompiled ground-truth cooldown, and the hardcoded `69` at the apply site now uses the constant.
- [x] `[design]` ~~Attacker stat on save DC~~ — **done: Willpower** ("the discipline to keep your eyes open"), via `MakeSave` AttackerStat, same mechanism as the lithofex's Ego. It reads the caster's stat live: player's sheet when the player casts, the creature's Willpower for NPC gorgons (single tuning knob for the innate-creatures idea). Known interaction: Willpower also reduces cooldowns — double-dips for gorgon builds; accepted as build identity.
- [x] `[design]` ~~Range~~ — **done: 80 ("sight")**, matching Sunder Mind. Cast from the eyes: default targeting requires visibility/LOS (unlike Sunder, which ignores LOS) — the flavor is engine-enforced.
- [x] `[design]` ~~Lithofex comparison~~ — done, absorbed into ground truth below; informed the DC and range decisions.
- [x] `[design]` ~~Delete or keep alternate gaze-tick handler~~ — superseded: the composite implementation will rewrite `TickGaze` wholesale; the commented-out `EndTurnEvent` handler dies with it.

### The big one — composite channel redesign
- [x] `[design]` **IMPLEMENTED 2026-07-21** — cumulative fails w/ +1 DC escalation per fail; success ladder (`ReleasePin` → `ClearSlow` → break) on the staged `Petrifying` effect (`Pinned`/`SlowCleared` layers); stone-mark exit at 4+ fails via new `Effect_PartiallyPetrified.cs` (500 rds, −20 MS/−10 QN/+2 AV, refresh-not-stack); **dead-gorgon trap fixed** (effect self-recedes when `Initiator` invalid, own `EndTurnEvent`); level text = approved contract; ability card rewritten; alternate-tick fossil + recovery const deleted. Spec (for reference): Fails cumulative (not consecutive). Each fail: progress +1, pin, −10 QN, save DC +1 (death spiral). Successes strip stages: 1st un-pins, 2nd removes slow, 3rd breaks gaze. 10 fails before 3 successes = statue (max channel 12 rounds). Escape with 4+ fails = partial-stone mark: −20 move speed, −10 QN, +2 AV, 500 rounds. DC = 8 + 2×Lv + WIL mod + 1 per fail. **The approved level text below is the implementation contract:**

  > You transfix an opponent with your unblinking gaze and slowly turn them to stone.
  > Taking any action other than passing the turn will break your gaze.
  > Each round your target makes a Toughness save; failures stiffen them, and successes shake off one stage of petrification.
  > Save difficulty: {{rules|8 + (2×Level)}} + your Willpower modifier, +{{rules|1}} per failed save
  > Each failure: held in place, {{rules|-10}} Quickness
  > First success frees movement, second ends the slowing, third breaks the gaze entirely.
  > Targets that fail {{rules|10}} saves are permanently turned to stone.
  > Targets that escape after {{rules|4}} or more failures are left partially petrified: {{rules|-20}} move speed, {{rules|-10}} Quickness, {{rules|+2}} AV for {{rules|500}} rounds
  > Range: sight
  > Cooldown: {{rules|80}} rounds


### Held levers — ALL RESOLVED 2026-07-23 (the Pass 5 sitting)
- `[design]` **Damage taken doesn't break the gaze — RULED KEEP.** It's the low-level survival mechanism (tank hits through the channel); adding a break would have worsened the low-level kill-security problem below. The PvE counterplay note stands for wild gorgons (kill the caster / eat saves).
- `[design]` **Per-tick LOS-break dial — RULED PARK** with the creatures/quarriers workstream; it only matters for NPC gorgon channels.
- `[design]` **Save-DC curve REBALANCED (user find 2026-07-23: "tricky at low levels to secure kills... 50% to 97% doesn't feel good"): `GetSaveDifficulty` = `8 + 2×Lv` → `11 + Lv`.** One-line change; everything else (spiral +1/fail, 10-fail statue, 3-success escape, mark at 4+ fails with its +2 AV — riders explicitly VETOED) untouched. Sim rationale (gaze_curves.py, 150k trials/cell): the channel is a fails-before-successes RACE, which amplifies per-round edges into cliffs — no formula avoids saturation vs equals at high level, so the design intent is WHERE certainty lives: new curve vs equals 56/83/97/99 (was 26/91/100/100 — the cliff), Lv1 trash ~70% (kills secured), and boss-tier (+3 Tou) 16/40/70/83 as the mutation's real progression (was 99%+ by Lv7, wasting the HP-agnostic-kill endgame identity). The approved contract's "8 + (2×Level)" line is superseded on this one number; level text renders the computed value and needs no edit. `[verify]` show-save-rolls shows opening DC 12+Lv+WIL.

### Reference (decompiled ground truth, 2026-07-21)
- *Sunder Mind:* cooldown 80, range 80 (sight, ignores LOS), 10-round channel, mental attack vs MA each round +max(Ego mod, Lv−2), ~1d3–1d4+Lv/2 per penetration, round-10 payoff doubles accrued damage, **whiffed rounds don't break the channel** (return value of the attack is discarded; verified), target has NO escape roll. Breaks on: caster non-pass action, caster takes Mental/Psionic damage (physical does NOT break it), caster stun-family debuffs, target death/zone-exit. Brains only; MentalShield immune.
- *Lithofex StoneGaze:* cooldown 50, range 7+Lv, 3-turn telegraphed warmup then a refractable line; ONE Toughness save vs DC 20 + attacker's Ego mod; failure = instant death + shrine-statue. Mod already reuses `StoneGaze.CreateStatueOf()` for its petrify payoff.
- *Channel comparison table and full break-condition cross-ref:* see conversation 2026-07-21; key deltas — Gorgon's is target-rolls (Sunder is caster-rolls), melee-adjacent→now sight, pierces mental immunity, pins, and after the composite will be "escapable in stages" vs Sunder's "no escape roll."

---


---

## Earthen Barrage

### Small fixes
- [x] `[design]` **Live-fire ruling (2026-07-22): Gravel Whorl decoupled from the Take Stones toggle** — it now whorls whenever stones remain in reserve, mutation alone suffices (the toggle only governs auto-equipping). SyncWhorl condition, effect details, level text, and ability card all updated.
- [x] `[bug]` **Bitrot pair from first boot's Player.log (2026-07-22):** `Pigskin` blueprint renamed to `PigskinWall` in a game update (Merges.xml target fixed — the leather-wall statue material works again); `Temporary.Silent` property removed from the engine (attribute dropped from the pebble blueprint). Both were author-era code aging, exactly the API-drift class predicted at project start. Log otherwise clean: full compile, all 27 XML roots loading, earlier fixes confirmed.
- [x] `[bug]` **Live-fire finding #2 (Player.log):** MODERROR "Using binary formatter for type Cleo_TerraFirma_GravelWhorl" — the engine's reflection field-writer binary-formatter-warns on fields typed as custom effect classes. **Fixed:** `AssocWhorl` (EB) and `AssocEffect` (Gorgon, same latent issue) marked `[NonSerialized]` with re-acquisition after load (SyncWhorl `??=` / TickGaze+CancelGaze re-link). Also fixed companion MODWARNs: missing `Name` attributes on both `<group>` elements in PopulationTables.xml. `[verify]` clean Player.log after restart; mid-gaze save/load.
- [x] `[bug]` **First live-fire finding (2026-07-21, caught by ear):** Gravel Whorl flickered off/on every turn — the 2-turn refreshed duration lost a race with the engine's countdown phase, spamming the generic status-effect-stop sound. (The original author's whorling stones used 3, likely for this exact reason.) **Fixed:** whorl lifetime is now purely managed by `SyncWhorl` — `UseStandardDurationCountdown() => false`, no expiry race possible. `[verify]` sound stops after restart.
- [x] `[bug]` ~~`ThrownSound` set to `impactsound`~~ — **fixed:** now uses `thrownsound` — `Mutation_EarthenBarrage.cs:172`. *Recon finding stands: the cosmetic-scaling system postdates the Feb 2026 build — it's the newest, least-tested code in the mod. Give it extra attention in the verification pass (listen for distinct throw vs impact sounds at levels 1, 5, and 10).*
- [x] `[bug]` ~~Level text shows pebble lifetime as "50–50"~~ — **fixed:** second constant now `ROCK_LIFETIME_MAX` — `Mutation_EarthenBarrage.cs:43`.
- Simulation run 2026-07-21 (`scratchpad/eb_sim.py`, faithful reimpl of decompiled `RollDamagePenetrations`, 200k trials/cell, to-hit not modeled): damage/PV scaling is healthy vs level-appropriate targets; AV≥PV+8 is a hard wall (engine-intended); flat 15-turn charge regen is the weak number; mutation cost is 4 MP.


### Raised in live-fire (2026-07-22/23 verification session)
- [x] `[bug]` **Take Stones auto-equip desync — REPRO'D BY USER + DIAGNOSED BY LOG + FIXED + DEPLOYED 2026-07-23 (active next restart):** pattern was toggle on → pick up a ground rock → throw it → no auto-equip. Instrumentation showed the exact cause (`prev=Rock_Salt (ours=False) -> no action`): the replace-handler only re-armed after throwing the mutation's OWN pebbles (`IsOurStone`). Fix: re-arm after throwing ANY `IsAnyStone`-tagged stone (fissure rocks, Small Stones, gemstones); non-stone thrown weapons (javelins/grenades) deliberately still don't trigger a sculpt — the toggle promises stones-on-tap, not a hijack of every thrown workflow. **Companion feature, user-requested same message: new Options entry "Take Stones starts toggled on" (Default Yes)** — new EB mutants begin with the toggle enabled (`DefaultToggleState` at ability creation; existing characters keep their current state). ~~`[verify]` throw a ground rock with toggle on → sculpted stone auto-equips~~ **CONFIRMED 2026-07-23 (new-save session): three consecutive ground-rock throws each logged `stone=True → auto-sculpt replacement` — the desync is dead, and the toggle arrived ON at mutation gain (the new option working).** Remaining eyes-item: options entry visible in the Terra Firma settings category.
- [x] `[design]` **Mullstone eat — RULED + BUILT + DEPLOYED 2026-07-23: guarantee exactly ONE cooldown reset** (user: 25% "I just didn't know that's what it was doing" felt like a no-op; "all cooldowns clear is vastly overcorrecting; the most it could do would be like guarantee 1"). New part `Cleo_TerraFirma_RefreshOneCooldownOnEat` (mirrors the base part's OnEat shape + player message, so the Lithovore eat channel fires it natively): picks one random ability currently cooling down, refreshes it; nothing cooling = nothing happens (logged). Replaces `RefreshAllCooldownsOnEat` on the mullstone blueprint. `[verify]` eat with 2+ abilities cooling → exactly one refreshes with the "suddenly feel ready" message.


---

## Stoneshaper

### Small fixes
- [x] `[bug]` ~~Ability card shows raw `-69420` for Duration at level 10+~~ — **fixed:** `CollectStats` now shows "until destroyed" for the infinite case (string stats verified safe via Sunder Mind's `stats.Set("Range", "sight")`).
- [x] `[polish]` ~~Stale "currently unused" comment on `DEFAULT_BEHAVIOR_BP`~~ — **fixed:** now describes what it is (the statues' fists).

### New bugs (audit 2026-07-21)
- [x] `[bug]` **Silent fail on anatomy-less muses (user live-fire find, 2026-07-22):** casting on a Young Ivory died at the last step with no message — plant creatures (`Plant → ActivePlant → BasePlant` chain, verified) have **no Body part**, so `ApplyStatueBuffs`'s unguarded `Creature.Body.Anatomy` threw an NRE the engine swallowed. **Fixed four ways:** target-selection check with a proper fail prompt ("X has no body plan for you to carve", re-prompts); Lovesick redirect falls back to the real target if the beloved has no body; null-statue guard after `MakeStatueOfCreatureFromWall`; `ApplyStatueBuffs` itself now skips the rebuild for body-less muses (statue keeps its default animated anatomy) so the static helper is safe for any future caller. StatueMaker item checked — not affected (never rebuilds anatomy). `[verify]` target a Young Ivory → clean fail message, no cooldown/wall consumed; `[design]` if we ever *want* plant statues carvable, delete the target-selection check — the helper now survives it.
- [x] `[bug]` ~~Null-creature crash~~ — **fixed:** `creature == null ||` guard added, mirroring the StatueMaker item's correct version.
- [x] `[bug]` ~~Killed-statue cooldown lock~~ — **fixed:** new `else if (StatueLifetimeRemaining > 0)` branch in `EndTurnEvent` detects a statue destroyed before its time and resets the cooldown to `BASE_COOLDOWN` (120) instead of leaving the lifetime-display cooldown (400+ turns) stranded.
- [x] `[polish]` ~~Hardening~~ — **done:** Mutations/Skills strip loops null-guarded in `ApplyStatueBuffs`.
- **Batch implemented 2026-07-21:** cosmetic muse weapons (tile+name by body-part type, mutation-formula stats); taunt revived (`Distraction`, Passive line dropped); expiry + Lv10 swap both route through `DeanimateStatue()` → inert lootable container statue (`CurWallBp`/wall-restore retired); swap confirmation prompt (player-only `Popup.ShowYesNo`); level text + ability card rewritten to match.

### Natural-weapons survey (2026-07-21) — for design call 2
Scanned all base-game `NaturalWeapon` blueprints for parts that would misbehave on a statue if kept:
- **Fine as stone:** plain bites/claws/horns/hooves/mandibles/kicks — pure stat-stick MeleeWeapons, no issue.
- **Disqualifying leaks:** `Oozing Pseudopod` (100% `DiseaseOnHit` — a companion that gives you glotrot), `Gooey/Slog` (`PoisonMelee` — venom from stone), `Bilge Sphincter` (`EquipStatBoost` +6 Str/+100 AcidRes — a buff organ), `Humor/Gyrohumor/PlasmaJelly` pseudopods (`ElementalDamage` up to 1d8+20 Heat), `Sawhander_Saw` (`VibroWeapon` — free armor-piercing companion), `Waydroid/Spark Tick/ElectricSnail` (`DischargeOnHit`), `Point-Defense Laser` (`EnergyAmmoLoader`, charge-dependent), `ChromePyramid_SpacetimeDistortion` (2d6 Cosmic + 2d10 discharge).
- **Verdict:** wholesale keep does NOT work — power leaks and biological/tech nonsense. Options: (a) ratify current strip-everything + gravelly haymaker (already built, level-scaled); (b) "stone rendition" middle path — keep the weapon's *form* (name + base dice as a plain MeleeWeapon), strip all effect parts; real implementation cost, adds muse-shopping depth. **Awaiting user ruling; Distraction cut (call 1) unblocks once ruled.**

### Ctesiphus / part-propagation note (2026-07-21)
The glow is a `LightSource` part on the Ray Cat blueprint — parts never propagate to statues (only tile/name/description/Lovely, wall physics/colors, anatomy, Str/Agi/Tou; mutations stripped). So no glowing statue. Cosmetic quirk: `SetCreature` bakes the muse's full description text in, so a Ctesiphus statue's description ends with the word "phosphorescent" while emitting no light. Harmless; noted for the text pass / amusement.

### API verification (decompiled 2026-07-21) — all green
- `RandomStatue.SetCreature` ✔ exists; copies blueprint/render/description data, does NOT retain the live creature — the mod skipping StoneGaze's `DeepCopy()` is safe (StoneGaze copies only because its victim dies mid-flow). Bonus find: `SetCreature` propagates `Lovely` — a statue of a Lovely creature is itself Lovely (base-game behavior; interacts with the Lovesick easter egg).
- `Body.Rebuild(string AsAnatomy)` ✔ ; `AnimateObject.Animate(GameObject, ...)` ✔.


### Raised in live-fire (2026-07-22/23 verification session)
- [x] `[bug]` **Muse-weapon cosmetics never fired — FIXED + DEPLOYED 2026-07-23 (active next restart):** user's goat statue got a fist on its Body instead of a horn on its head. Cause: `Body.Rebuild(anatomy)` copies the body plan but NOT DefaultBehaviors (natural weapons live on creature blueprints, not anatomies), so the rebuilt statue had no weapons to strip → `partsToReplace` empty → fist-on-Body fallback, while the muse-weapon map sat unused. Fix: after the strip loop, ALSO arm every statue part whose TYPE matches a slot where the muse carried a natural weapon. Side effect (faithful to the ruling's intent): multi-weapon muses now yield multi-weapon statues (bite + claws creatures), each on the mutation's damage formula. New `[SHAPE]` log line per armed part shows muse-copy vs fallback. **VERIFIED 2026-07-23:** Eyeless Crab → two stone crab claws; Baboon → fists + vicious bite, multi-slot working. **Naming question (user, 2026-07-23, from a Mechanical Mollusk Cherub statue whose features read "fist, fist, mechanical cherubic foot"):** should copied muse-weapon names take a stone prefix — "**gravelly** cherubic foot"? Options: (1) simple prefix "gravelly "/"stone " on every copied name (zero maintenance; accepts the occasional "gravelly mechanical cherubic foot" pileup); (2) strip material adjectives (mechanical/metal/chrome…) then prefix (cleanest, but a denylist to maintain); (3) verbatim as now (the description already frames it as stone). **RESOLVED VERBATIM 2026-07-23 — user in-game check after the muse-copy fix: "seem fine on a cursory glance"; the prefix idea stays pocketed.** **Follow-up bug FIXED + DEPLOYED same session (active next restart):** humanoid muses (goatfolk) copied the engine's GENERIC `DefaultFist` as a "distinctive" weapon, so their statues punched with plain 'fist' instead of gravelly haymakers. Filter: muse weapons carrying the engine's own `UndesirableWeapon` tag (DefaultFist et al.) are skipped — statues keep the gravelly identity on generic slots, and only genuinely distinctive weapons (horns, claws, bites, named fists like the albino ape's) get carved. **Emergent case observed, flag for the pass:** a FUNGAL-INFECTED muse's statue armed its Head/Hands as "{{G|fickle gill}}" — the muse map picks up infection growths (they're natural Equipped items). Cosmetic-only (formula stats), and "carved as the muse appeared, gills and all" is defensible fiction — **RULED KEEP 2026-07-23** (and leans the stone-creatures-catch-infections flag toward keep for consistency).
- [x] `[design]` **Pre-Lv10 no-second-statue friction — RULED EXTEND + BUILT + DEPLOYED 2026-07-23** (user: "cooldown gates it most of the time anyways"; supersedes the 2026-07-21 un-swappable ruling). Casting now swaps at ANY level: the confirm prompt guards every living statue, the old one de-animates via `DeanimateStatue()` (inert lootable container, gear entombed), and every carve takes the normal `BASE_COOLDOWN`. **Lifetime-as-cooldown RETIRED** along with its three cooldown-normalization branches (permanent-destroyed / natural-expiry / killed-early — they existed only to clear the pinned display); lifetime now ticks silently to expiry. Accepted cost: the ability card no longer counts down the statue's remaining turns — flag at the text pass if the lost display grates (candidate home: the statue's own effect description). `[verify]` low-level recast prompts + swaps + entombs; natural expiry and statue death no longer touch the cooldown.


---

## Geomancy

### Small fixes
- [x] `[bug]` ~~Duration line used `POWER_PER_TILE` (90) instead of `POWER_PER_TURN` (35)~~ — **fixed.**
- [x] `[bug]` ~~All-or-nothing reality-distortion filter~~ — **fixed:** confirmed against the decompiled signature (`CheckRealityDistortionAccessibility(Object, Cell, Actor, Device, Mutation, Penetration)`) that the mod passed the constant landing cell as `Cell` for every candidate reveal-cell, so the per-cell mask either passed everything or nothing. Now checks `Cell: x` per cell — stabilized cells are individually excluded from the reveal, as intended.

### De-Harmony investigation — RESOLVED 2026-07-21: throw patch is replaceable
Decompiled ground truth: `GameObject.PerformThrow` calls `Weapon.WasThrown(By, At)` **after** placing the weapon in its landing cell, **only on the success path** — firing native `BeforeAfterThrownEvent → AfterThrownEvent → AfterAfterThrownEvent`, dispatched **to the thrown item**. Exact semantics the Harmony patch hand-rolls.
- [x] `[design]` **IMPLEMENTED 2026-07-21:** relay part `Part_GeomancyStone.cs` (native `AfterThrownEvent` → forwards to thrower's Geomancy) merged onto `BaseStone`/`Gemstone`; `PerformThrow` Harmony postfix deleted, `Event_AfterSuccessfulThrow.cs` deleted, Geomancy rewired to `OnStoneThrown(Weapon)` centered on the landing cell. Orphaned `using` in Fissure removed (compile necessity, not veto-covered work). Side benefit: NPC geomancers now work through the same path. Accepted delta: stones destroyed on impact don't trigger the reveal.
- The **second** Harmony patch (`Scanning.HasScanningFor` postfix for the gemstone scan effect) has no native hook found — **stays**, per "no Harmony is preferred but not strict."


---

## Calcify

### Small fixes
- [x] `[polish]` ~~bababooey fallback~~ — **fixed:** description restore now falls back to leaving the current text in place (restore only if the blueprint has a Short).
- [x] `[polish]` ~~Cooldown constant unification~~ — **fixed:** handler now uses `GetCooldown(Level)`.
- [x] `[polish]` ~~Whorling-stones removal ran once per created wall inside the loop and never nulled the reference~~ — **fixed:** moved after the loop, reference nulled.
- [x] `[bug]` **Missing blueprint (audit find 2026-07-21):** the code's fallback `Cleo_TerraFirma_WallCalcify` was defined nowhere — a zone with no default wall + no stone neighbor would create a phantom blueprint. **Fixed:** blueprint added to Objects.xml inheriting `Sandstone` (verified real; inherits the wall-properties merge via `BaseSedimentaryRock`).
- [x] `[polish]` **Cast animation final pass — SIGNED OFF 2026-08-05 (user: "green ... as in good to go").** Three fixes that session: (1) handoff sprite no longer sinks in front of the settled wall ("flies downwards") — vanish-at-handoff flag on the juice entry, redoubt choreography untouched; (2) detail color rendered black on first-use pooled sprites (engine mesh-normals stomp: `exMesh.FlushBuffers`'s Normal block overwrites the detail channel after the Color block wrote it) — fixed by per-frame color re-apply with an equality-defeating nudge in `Fx_StoneRise.cs`, cures the redoubt's sprites too; (3) one `SmokePuff` per wall landing, borrowed from Fissure (promoted public) for cast symmetry. Backing stays black for negative space (reaffirmed 2026-08-05).


---

## Fissure  ← **ACTIVE WORKSTREAM (2026-07-22).** Veto lifted — its own condition (every other mutation's audit complete) was met per the status board; user promoted Fissure to the session's committed work.

### Key finding (2026-07-22, decomp + table diff)
`Cleo_TerraFirma_FissureTable1–9` are **verbatim copies of Burgeoning's `PlantSummoning1–9`** — same creatures, weights, nested-generics structure, the same Generics2/3-alias quirk, same `8Plus` naming. The commissioned authors copied Burgeoning's entire reward system and never flipped the switch: the code rolls `FissureTable_Generics{Lv}` (scenery filler) where the design plainly intended `FissureTable{Lv}` (75% plant creatures / 25% generics). **The intended payoff is a one-line table-name flip.** Consequences: the Generics8/9 missing-table bug becomes moot (creature tables reference `Generics8Plus` correctly), and the differentiation-from-Burgeoning problem is the entire remaining design debt — as authored, wired Fissure would be Burgeoning's loot behind a different verb.

*Burgeoning ground truth (decompiled 2026-07-22):* Mental, 3 MP. Range 8, 3x3 burst, every non-occluding cell grows a plant (100%) + 20% spill into the adjacent ring ≈ 9–12 allied summons/cast; 50% "prime plant" batching. Tier index ceil(Lv/2)±rand(−1..+2) into `PlantSummoning{1-9}`. Cooldown 115−10×Lv (floor 5; 15 @ Lv10). Has a 6-line `AIGetOffensiveAbilityList` handler (distance ≤ 8 + LOS). Summons permanent, XP zeroed, `AllySummon` leader — the mod's existing spawn code already mirrors this exactly.

### Design rulings (user, 2026-07-22)
1. **Physical-summon niche ratified** — Qud has no Physical-category summon; that alone differentiates by stat economy. Wiring flip approved in principle.
2. **Cooldown: standard level-scaled formula, NOT the Light Manipulation charge model** — user: LM works differently from every other mutation in the game; base Fissure's cooldown on a normal mutation instead. Bonus: Willpower already reduces *all* cooldowns globally, so "the earth rewards the disciplined" is served natively with zero bespoke code.
3. **Crack visuals: reuse the game's crack art** — tile `terrain/sw_crack.bmp` (used by `PondDown`, the "small crack" pond entrance) on a new mod-side cosmetic floor-decal blueprint. The bigger work, per user: **rocks with effects that make the yield feel as relevant as Burgeoning's** — that's the differentiator.
4. **Cast shape: self-centered expanding ring** — no ranged targeting; cracks open in a ring around the caster whose radius grows at level breakpoints. (Replaces the dead inverted `GetAoE()`; kills the 8-vs-9 range-check mismatch too.)
5. Small-fixes audit list approved (dead usings incl. `System.ComponentModel.Design`, `E.Actor`/`ParentObject` consistency; note `Thread.Sleep` animation delay is base-game precedent — Burgeoning does the same — so it stays).

### Spec v1 — SUPERSEDED same day (kept for reference below); Spec v2 direction ruled 2026-07-22
User feedback that killed v1's yield model: *"this is too much for sure — the real thing is that the effect should have a solid slam, like a rock wall collapsing, and smoke particles should spawn in the area where the rocks fall, to imply them falling."* Light C# for presentation acceptable; no bespoke animation system.

**Spec v2 rulings (user, 2026-07-22):**
- **Fiction flip: ceiling collapse, not underearth venting.** The ceilings we pass under carry unidentified rocks; Fissure shakes them loose. Rocks *fall* — landing does crush damage to whatever's beneath (scaling by what fell: pebble 1 dmg → falling wall segment heavy + instakill chance).
- **Burgeoning's spill mechanic adopted** — user: the 20% spread into adjacent cells is good ("you're crashing down the ceiling and sometimes it spreads").
- **Yield volume way down** from v1's 25/25/50 per cell. Presentation carries the fantasy: slam sound (rock-wall collapse) + smoke where rocks land (candidate: short-lived `Smoke` gas objects in landing cells — engine-native, briefly occludes; *(verify blueprint name)*), few objects.
- **The reward system is the rock catalog** — user delivered a 55-entry list of rocks with paired thrown/step-on effects, weakest→strongest: **`design/fissure-rock-list.md`** (verbatim list + engine triage + tier bands + wave assignments). Rocks are *unidentified* (native `Examiner` unknown-form pattern, as on the claysculpting loop). Rocks don't discriminate: effects hit players and enemies alike; positive rocks you throw at yourself.
- **Extensibility intent:** mod creatures join the fall table as they're built; later, cooking-ingredient wall growths could spawn from it; rocks may also appear elsewhere in the game (loot, mineable ceilings).
- Shared tech that powers most of the catalog: one step-trigger C# part + one falling-impact routine in the cast code + native Examiner unidentification. Payloads mostly reuse grenade/on-hit parts.

**Carried over from v1 (not contradicted, still awaiting explicit sign-off):** cooldown `105 − 5×Lv` floor 40, cost 2→3 MP, Burgeoning-style AI handler, text pass plan.
- *v1 rock slate (stalagmite/steam vent/geode) — absorbed into the catalog triage; steam-vent tech (Walltrap + `SteamGas`) and geode idea live on as catalog implementation patterns.*

### Spec v2 second ruling round (user, 2026-07-22)
- **No crack decal** — presentation is slam sound + smoke at landing cells + the fallen rocks. Nothing painted on the floor.
- **Plant tables RETIRED from the roll path** — no allied plants unless explicitly approved. The Burgeoning-copy `FissureTable1–9` XML stays dormant (zero cost). Ceiling *creatures* come later, sparse and deliberate (user has a shortlist); friendliness is a per-entry choice — Burgeoning ground truth: mutation casts summon allied via `AllySummon` (`bFriendly: true`); a hostile allegiance path exists engine-side. The mod's spawn code already does the allied version.
- **No Examiner/unidentified system** — user: unknown-forms are reserved for tech items in base Qud; rocks speaking that language oversteps. The glowing-rock twins survive systemlessly (two blueprints sharing DisplayName + tile IS the gamble); strange rock is random-per-throw, no ID needed. "Unidentified ceiling rocks" lives on as description flavor only.
- **Spill expands outward only** — never toward/onto the epicenter; must not crush the caster.
- **v1 fifteen + tier-band mapping APPROVED** (see `design/fissure-rock-list.md`).
- **Pathfinding ground truth (decompiled 2026-07-22, answers "will enemies step on dangerous rocks?"): YES, by engine design.** Avoidance exists only if the object volunteers `GetNavigationWeightEvent` weight; "Smart" pathing = `Intelligence > 6` (near-universal); and the structural precedent `Tinkering_Mine.Avoidable()` grants avoidance **only to actors non-hostile to the mine's owner** — visible hostile-owned mines give enemies zero weight so they walk on and detonate (that's what makes mines a weapon). Our step-trigger part copies the whole recipe: enemies get no weight, caster's allies avoid (ConsiderNonHostile mirror), player autowalk/autoexplore interrupted (`InterruptAutowalkEvent`/`AutoexploreObjectEvent`), triggers via `ObjectEnteredCellEvent` with phase/flight matching (fliers pass over, phased pass through).
  - **Cache-staleness hazard (user caught it 2026-07-22; decomp confirmed):** `Cell.NavigationWeightCache` is keyed by the Nav capability bitfield only — NOT by looker — so per-actor answers get baked in and served cross-actor until the cell's contents change (base-game mines technically have this staleness). **Hard rule for our part: always set `E.Uncacheable = true` when answering nav-weight queries** — forces per-query recomputation, makes the player/enemy asymmetry airtight. Player protection is triple-layered regardless: Uncacheable per-actor weights + autoexplore's own Nav cache key (Autoexploring is bit 4 of the key) + `InterruptAutowalkEvent` hard-stopping automove independent of weights. Also corrected: harvest loop was never broken — adjacent-cell pickup exists (user), so rocks-as-loot works under any shape.
- **RULED — cast shape: SELF-RING ("self ring sounds cooler to me," 2026-07-22).** No targeting; the ceiling collapses in a ring around the caster. Ring radius 1/2/3/4 at Lv1–3/4–6/7–9/10+ (Chebyshev). Falls per cast = **radius + 3 (4/5/6/7) — user tuned up from radius+1 post-build ("a few more rocks," 2026-07-22)**; spill 20% per landing, one echo, strictly outward (never toward the epicenter — cannot crush the caster, structurally). **Early-pool weakness reviewed same day: accepted as designed** — band 1 (pebbles/rubble/puddles/pointy) serves character levels ~1–8, the count bump helps band 1 most (more crush rolls + more ammo), and the Lv4 jump to whorlstone/torpor/volatile is the intended power spike. **(Provenance audit 2026-07-26: "accepted as designed" carries no quoted words — unclear whether this was user review or CLAUDE assessment; treat as CLAUDE ASSESSMENT, UNRULED, until re-confirmed.)**
- **High-level weakness pass (user playtest feedback 2026-07-22: can't kill an ooze with drops, too few rocks for the radius, yields unthreatening):** three coordinated fixes. (1) **Prime falls** — the first `radius` falls (1/2/3/4) roll the highest reachable band's *exclusive* table (`Band2New`/`Band3New`, depth-respecting), per user's "roll the highest band a few times, then lower ones for filling"; spill echoes always roll filler. Fixes the nested-blend problem where ~⅓ of Lv10 falls were band-1 chaff. (2) **Falls now scale with area**: `2×radius+2` → 4/6/8/10 (was 4/5/6/7). (3) **Crush scales**: `+Level` flat on every crush roll (Lv10 wall = 8d8+10; falling pebble bonks for 11 — thrown pebbles keep their 1-damage identity). Level text updated. *Future dial if drops still feel weak: weight fall placement toward occupied ring cells ("the earth aims") — currently pure random.*
- **Depth modifier added (user ruling 2026-07-22, "deeper you go, the more powerful — to a point"):** band selection uses `Level + (Z−10)/5`, capped at +6 (two full bands); surface Z=10 verified from decompiled Zone (`Z > 10` = "the deep underground"). Quality-only — falls/radius/cooldown never scale with depth. Z15 → +1, Z25 → +3 (one band up), Z40+ → cap. Level text: "Deeper strata shed stronger stone." `[verify]` cast at surface vs a deep stratum, watch band shift.
- **Band escalation added (user ruling 2026-07-22, base-game loot-table mechanic):** every rock roll has a ~4.8% "Deeper" chance to escalate one band, with a nested 10% (≈0.5% overall) to jump straight to band 3 from band 1. Escalations point at band-exclusive `FissureRocksBand2New`/`Band3New` tables so an escalated roll always visibly lands higher-band content (no fall-back-down dilution). A fresh Lv1 cast (4 falls) ≈ 18% chance of at least one band-2 rock, ≈ 1-in-50 of a band-3. Restructure also deduplicated band content into the `*New` tables. *(Superseded by the self-ring ruling above; kept for reference.)* Claude's recommendation, awaiting ruling: **ring around a chosen point, range 8, self-cast preserved** — epicenter cell never receives a fall (structurally can't crush the caster on self-cast; spill radiates outward from the point in both modes). Rationale: the harvest loop shouldn't mine your own doorstep (loot the collapse site you dropped on *them*); the crush ladder only matters if it can be aimed at a crowd; the mod currently has no offensive AoE (EB is single-target, Calcify defense, Stoneshaper summon). Counter-case stated: pure self-ring is the more distinctive silhouette. Proposed numbers under targeted shape: area 3x3/5x5/7x7 at Lv1–4/5–8/9+, falls per cast 2+ceil(Lv/2) cap 7, spill 20%/landing, one echo, outward. Fall-count knob (#1) was left blank by user — numbers above are Claude's proposal, unblessed.

### Build phases — **A–D BUILT + DEPLOYED 2026-07-22** (verify pass outstanding)
- [x] **A.** `Mutation_Fissure.cs` rewritten wholesale: self-ring collapse cast (no targeting → NPC-native), falls = radius+1 with 20% outward spill, crush ladder read from blueprint tags, slam (`sfx_throwing_stone_large_impact`, Calcify's verified large-stone-impact) + smoke (gray shade-glyph sinusoidal particles, Burgeoning's exact particle call), old code fully retired (PickBurst targeting, GetAoE, plant-table roll, dead usings).
- [x] **B.** New `Part_FallenRock.cs` (`Cleo_TerraFirma_FallenRockPart`): step-trigger channel (Mine's `ObjectEnteredCellEvent` + `PhaseAndFlightMatches` recipe), 7 payload types (Liquid/Gas/Explosion/Discharge/Temperature/EMP/Bleed — every call shape copied from a decompiled precedent), per-actor nav weight with **`Uncacheable=true` always**, `InterruptAutowalkEvent` hard-stop, owner courtesy. New `XML/Rocks.xml`: the v1 fifteen (+2 pebble color variants); thrown channels ride native parts (GasGrenade/HEGrenade/ThermalGrenade/EMPGrenade/DischargeOnHit/BleedingOnHit at base-game mk-I magnitudes). Three band tables in `Populations.xml` (nested 60-weight blending; band thresholds Lv4/Lv7).
- [x] **C.** Burgeoning-style AI handler (advertise when enemy within ring radius +1); `CollectStats` (RingRadius/Falls/cooldown); level text rewritten (house-voice draft — final text pass still owed).
- [x] **D.** `Mutations.xml` re-enabled at **Cost 3**; `rockwineverytime` restored; deployed to `Mods\TerraFirma`.

**Implementation deltas + notes for the verify pass:**
- Falling **walls/rubble that fail to kill** what they land on shatter into 1–2 fallen rocks instead of standing (no wall-on-creature coexistence fights). Fallen wall = `Sandstone` child, crush 8d8 + **15% instakill**; rubble/crumbled wall are `Rubble` children with HP lowered to 250/400 so they're clearable.
- Rocks are **portable mines**: picked up and dropped elsewhere they keep step-trigger + owner. Emergent, probably delightful; watch for abuse.
- Fissure rocks inherit `BaseStone` → they carry the **Geomancy relay** (geomancers throwing them get reveals) and grenade-armed rocks detonate if destroyed (chain reactions in the ring are possible and intended).
- Per-landing sound is the slam at cell volume; if the multi-fall sound stack is too loud in-game, drop the per-cell copies and keep the single cast slam.
- **Burial ruling REVISED same day (user: "a wall exists on the base level of tiles — nothing can go under them naturally"):** solid falls now **sweep loose items (corpses + dropped gear) into one open adjacent cell before placing themselves** — the Gravel Whorl's swat-aside recipe (`GetLocalAdjacentCells` + `RemoveFromContext`/`AddObject`), living things never swept. An occluding wall tile would have hidden buried loot completely (functionally deleted for anyone not knowing the rule). Fallback when no adjacent cell is open: loot stays beneath — the earth keeps what it takes.
- `[verify]` **Fissure checklist:** compiles green; **loot spills aside from solid falls** (kill something with rubble/wall → corpse + gear appear in one adjacent open cell, nothing hidden under the wall tile); ring radii at breakpoint levels 3→4 and 6→7 (wish-set mutation level); falls=3R+1 counts (4/7/10/13 — retuned mid-playtest 2026-07-22, was 2R+2), first R (prime) falls visibly roll higher-band rocks at Lv4+ (primes deliberately NOT scaled with the fall bump — filler grows, jackpots don't; flag if it should); spill only outward, never onto caster; crush messages + wall instakill; each payload fires on step (walk an ally? no — walk an enemy over it via combat) and on throw (gas clouds, explosion, temperature, EMP, discharge, bleed, puddles); enemies path ONTO rocks (watch a snapjaw walk over a volatile rock); player autowalk interrupts at own rocks; autoexplore routes around; rocks stack in inventory; save/load with rocks on the ground (Owner GameObject field serialization); NPC fissure-user via wish-granted mutation on a creature; world-map cast blocked.

### 9-tier Burgeoning realignment — **RULED + BUILT + DEPLOYED 2026-07-23** (gates cleared same day: architecture ADOPTED; primes RETIRED + seam ADOPTED; deep four ride TIER 9 PLAIN — no depth gate, user overruled Claude's port of the old 50/50 deep-special coin-flip, which is hereby SUPERSEDED; pits DEFERRED, chasm ships as-built)

**Build record:** nine `FissureTier1-9` tables + `FissureRocksAny` replace the band tables (deleted; `ThrowableRocks*` geyser tables untouched); tier roll `ceil(eff/2)+rand(-1..+2)` clamp 1-9 per fall; seam = one blueprint rolled per cast, 50% of falls+spills shed it (stacking QoL); spill 20%+5%/depth step frozen at 50%; depth cap +6→+8 (Z50, flat below; the wrong "engine floor Z=50" decomp claim struck); filler trio live (healstone T1 w/ new Heal payload channel, dazestone T2 via new `Dazed` Effect-channel case, oilstone T3 → base `OilPuddle`); ground twins for healstone (`regenLowtier`) + oilstone (`heat`), dazestone twin-less until its domain is ruled (flour fallback); strange rock + Lithovore strange-eat re-pointed to `FissureRocksAny`; level text rewritten (seam + deeper-spreads-farther lines, primes line gone). **Wobbler resolved at build: mullstone → TIER 6** (the 6-vs-8 call, taken toward the thin roster — softens the open-T6-slot want; flag if it grates). **Naming convention RULED (user, same day) + RETROACTIVELY APPLIED (save-compat explicitly waived — mod not public):** blueprint IDs are mechanical, flavor lives in DisplayName only, and **every mod blueprint carries the `Cleo_TerraFirma_` prefix** (collision safety). Sweep executed 2026-07-23: the five pass-2 flavor IDs normalized (`Rock_Grimslate→Rock_Scary`, `Rock_Tanglelith→Rock_Bola`, `Rock_Oxidian→Rock_Rusty`, `Rock_Urchinite→Rock_Spiny`, `Rock_Phasalt→Rock_Drifting`, ground twins in lockstep — the mill's name-mapping keeps working; DisplayNames unchanged, so nothing changes in-game); the six bare-`TerraFirma_` companion blueprints prefixed (`Burrowing Urchin`, `Blunted Chisel`, `Splintered Mallet`, `Urchin Spine`, `Aculei`, `BoarVessel` + their gear-table/corpse references). Deliberately NOT touched: Merges.xml names (base-game blueprints being merged onto) and the two `ProceduralCookingIngredient_Cleo_TerraFirma_*` classes (engine ingredient-naming pattern is positional). **Consequence accepted: existing playtest saves holding renamed objects will not load them cleanly — fresh character next session was already planned.** `[verify]` Stonecarver chargen gear still spawns (chisel/mallet through the renamed gear table), urchin drops aculei, Boar Vessel selectable.

**Sim (tier_sim.py, 200k casts/row):** exact Burgeoning flat quarters — 25% each at par−1/par/+1/+2; ~7 rocks/cast Lv1 → ~29 Lv10 surface → 36 at depth cap. **Emergent, flagged-accepted: the clamp pools tier 9 at the bottom of the world** — Z40 = 50% T9, Z50+ = 75% T9 (Burgeoning's own max-tier behavior; the deepest strata are made of endgame; T9's 25% generics = walls/chasm soften it in-table).

`[verify]` **Realignment checklist (next boot):** tier spread visible over a cast (mixed tiers, ~half above par at any depth); seam dominance reads (~half of one cast is the same stone, and they stack); trio in play — healstone heals on self-throw then breaks, never survives; dazestone dazes on hit (and via strange mimic!); oilstone splashes/puddles oil, oil slips + burns; spill visibly denser at depth; T9 pooling at Z40+; strange rock now rolls the full spread (deep stones possible from a strange mimic — the audit-2 "deep reach" question is now structural, fold into the strange pass); no MODERROR from the table swap on old saves (saved rocks reference blueprints, not tables — expected safe).

### Superseded drawing-board record (kept for reference)

**The proposal (user-initiated after a Burgeoning table comparison; "we don't wanna be reinventing the wheel"):** restructure the fall tables to Burgeoning's architecture. Ground truth from decomp + base XML: Burgeoning rolls `tier = ceil(Lv/2) + rand(−1..+2)` clamp 1–9 per plant (50% of rolls land ABOVE par), tables are 75% tier creatures / 25% Generics (which bleed 10-weight into adjacent tiers ≈ 6.25% up — almost exactly our Deeper 4.8%, independently re-derived), plus per-cast prime-plant batching (50% of a cast reuses one rolled blueprint — cosmetic coherence). Our real divergences: no code-side spread (deterministic band pick), 60% recursive downward blend (chaff-heavy; primes exist to patch this), 4 coarse bands vs 9 fine tiers. Key alignment: with the depth cap ruled +8, effective level tops at 18 → `ceil(18/2)` = 9 exactly.

**Proposed shape:** tier roll per fall (formula above, effLevel = level + depth bonus); nine `FissureRocks{1-9}` tables at 75/25; **generics = the inert crush family, already a tiered ladder** (pebbles → generic rocks → rubble → stalagmite → crumbled walls → walls → chasm) — later joined by wall-growth harvestables and ceiling creatures; `Deeper` groups retire (the spread replaces them).

**Tier sheet (revised 2026-07-23 — filler trio RULED IN, demotions undone):** T1 soupstone/salt/pointy/**healstone** · T2 sludgestone/cracked-floor/**dazestone** · T3 **oilstone**/shattered-stalactite/**rusty rock** *(stalactite RULED kept at 3 — "a sharp rock is honest tier-3 material"; rusty RULED down a tier from 4, user 2026-07-23)* · T4–5 whorlstone/torpor/scary + **fungal/ash restored to range** (exact slots at lock) + bola/spiderweb/spiny/caromcobble/baboon · T6 volatile/spitting-geyser *(user 2026-07-23: wants one more T6 headliner eventually — open slot, no rush; mid-tier register: a real punch but not the elemental family's altitude. Parked stun-gas rock is a natural candidate if nothing better brews)* · T7 voltaic/scorching/icy/melting/shunting/ape + **vantabsidian restored** · T8 drainstone/hexstone/streakschorl/ajistone/pulsestone/spewing/lodestone/shieldstone · T9 blurring/lovestone/drifting/twins/gyrestone/heaving.

**The three new stones (RULED 2026-07-23, names WIP like the rest; NOT built — they land with the restructure):**
- **Healstone (T1):** heals the creature hit (throw at yourself — the catalog's positive-rock rule finally exercised) or the stepper, then ALWAYS breaks (consumable — no fragility roll, spiderweb one-use kin). Chunk of salt's self-throw payload shape; one new ~10-line Heal payload case. Open numbers: heal die (~1d4 register), step = half or full. Fiction bonus: a discarded healstone is a medkit for whoever steps on it — rocks don't discriminate.
- **Dazestone (T2):** chance-of-daze on hit, single-target, no gas. **Implementation CORRECTED at the debt audit (2026-07-23): build as a new `Dazed` case in FallenRockPart's Effect payload channel (~2 lines beside Blind/Prone/Terrified), NOT a `Part_DazeOnThrowPens` reuse** — a separate-part implementation would be invisible to strange rock's mimic (which only fires FallenRockPart payloads + grenades), adding a FOURTH dud mimic to the audit-1 list. Effect-channel gets mimicry, step-halving, and the payload plumbing free. EB overlap read as house identity ("stone rings your bell"), not collision.
- **Oilstone (T3):** soupstone/sludgestone recipe verbatim pointed at oil — splash on shatter, puddle on step. **Liquid VERIFIED at the debt audit (decomp 2026-07-23): `LiquidOil` is real — Combustibility 90, FlameTemperature 250 (ignites easily), native `SlipperyWhenWet` with its own "Oil Slip Move" save + slip message.** Slippery + flammable exactly as pitched, zero mod-side liquid work. Remaining at build: the puddle blueprint name (an OilPuddle-style object, or mint one from the SaltPuddle pattern). Completes the liquid ladder: salty water → slime → oil.
- **Stun-gas rock: PARKED** (user floated, Claude case accepted: overlaps the tier-4 CC-gas family — whorlstone/torpor — not the thin tiers; stun is the strongest of the three effects. A future third sibling if ever wanted — natural candidate for the open T6 slot.)
- **Systemic per-rock tax, ×3 (debt audit 2026-07-23 — every new rock owes these at build):** ground/powder twin blueprint + **eaten/cooked mappings RULED-direction by user same day:** healstone eaten = healing-over-time buff (regen, not instant), cooked = simple healing/regeneration effects; dazestone eaten = dazes/stuns the eater for a period (the rock doesn't care who's eating it), cooked = **OPEN — user doesn't know yet**; oilstone eaten = trigger a drink-oil event (the chunk-of-salt AddWater shape, pointed at `LiquidOil`'s own drink semantics), cooked = simple heat-related effects (heat domain), tentative. + **HistorySpice.json entries (the REQUIRED item grows to ~33)** + eat-pass 2 folds the above in + DRAFT description ×3 (mod-wide text pass) + placeholder tile ×3 (systemic art debt). **Geyser ammo call:** do the trio join `ThrowableRocks1/2` (a geyser lobbing healstones HEALS its target — "geysers don't discriminate" comedy, or unwanted; one-line table choice at build). Free rides confirmed: Geomancy relay (BaseStone inheritance), decay-to-pebbles + satiety defaults (BaseFallenRock), whorl-devour boundary (plain stones — fed to the reserve, no special casing).

**Open rulings before build:** (1) adopt the architecture at all; (2) primes: retire / vestige / replace with seam-batching (Burgeoning's batching read geologically — half the falls are one stone; stacking bonus); (3) deep four: tier 9 vs depth-gated "tier 10" beyond the spread's reach; (4) wobblers: mullstone 6-vs-8, ape statue 7→8 if permanence ratifies, caromcobble never user-ranged, glowing twins 8-vs-9 (dud at jackpot altitude — taste call), baboon 5-vs-6, chasm/walls as sometimes-destructive top generics (accepted thesis or dial chasm's weight). ~~Structural bottom-thinness~~ **RESOLVED via backfill: the filler trio above.**

**Chasm → visible PIT (user leaning 2026-07-23, NOT LOCKED — "i like them more than chasms… just a lot of moving parts there." Requirement if adopted: holes must visibly show the terrain below — the original catalog #5 asked for exactly this, "the rustwells/chavvah kind"; OpenShaft was the compromise. Decision rides with the restructure rulings; recon below stands either way).** Recon (decomp + base XML, same day): the see-through tech is the **`Pit` blueprint's `PitMaterial` part** — it renders the cell directly below (`GetCellFromDirection("D")`) onto the pit tile in darkened colors; `Air`/`ChasmMaterial` is the Chavvah open-air variant; our chasm inherits `OpenShaft`, which has NO material part (static black tile — why it doesn't show terrain). **Runtime creation precedent: the engine's own `DropPit` part** — `Create("Pit")` into a built zone + `cell.PaintWallsAround()` + zone `FirmPitEdges`/`PaintPitEdges` events + `ZoneManager.PaintWalls(zone)`; copy that call shape verbatim. **Size constraint found:** `PitMaterial.PaintPit` only renders below-terrain when ALL 8 neighbors are also pit-ish — lone pit tiles draw as crag lip, no window. So: (a) chasm fall opens a **3x3 pit patch** (native look, one see-through center, zero render code; T9 drama earns it — but 9 cells of floor gone, loot-swallowing scales up) vs (b) tiny `PitMaterial` subclass skipping the neighbor check → 1–2 tile see-through holes (compact, but no crag lip — verify it reads visually). Build with the restructure; size/shape choice at build. `LazyPit` exists if below-zone build cost ever bites; the vanilla pit-gen event-pool overflow scare (verification session) was zone-BUILD-time, distinct path.

**Rulings already made this session (build HELD with the restructure — the same code moves):** depth spill scaling **(a) RULED YES** — 20% + 5%/depth step, freeze at 50%; chaff-rolls-good confirmed already present twice over (Deeper ~4.8% + depth band-shifts filler) · **(b) +1 fall per 2 steps HELD** as a future dial · **(c) quality cap RULED +8, reached at Z50, flat below** — and the TODO's old "engine floor confirmed Z=50 by decomp" claim is WRONG (user: floor is far deeper; the same line even recorded a live Z51 cast) — strike it at build. **Absorbed by the restructure:** the primes-scale flag (moot under the spread), the strange-rock band-placement item (every placement gets re-decided), the Deeper groups' fate.

**Related parked note (same discussion):** the dormant `FissureTable1–9` Burgeoning copies = the *skeleton* for the ceiling-creatures workstream (swap plants for cave fauna; allegiance leans WILD per-entry — "you cracked the ceiling; what lived there is down here and upset"; non-allied engine path exists, `GerminatedPlants` precedent) and the Generics slots map to the wall-growth harvestables earmark. Don't delete the dormant tables; they're the stencil.


### Rock wave v1.5, batch 1 — BUILT 2026-07-22 (7 catalog entries, 9 blueprints; ~~NOT deployed, held for the verify boot~~ **hold overtaken same day — deployed with the later waves**, confirmed in `Mods\TerraFirma\XML\Rocks.xml` 2026-07-23)
*(closed items lifted from the live block of the same name; open flags stayed in TODO.md)*

- [x] `[design]` ~~**Strange rock spawns too early/often (user live-fire find, 2026-07-22 verification session)** — user: "they should be in the middle of the bands?"~~ **RESOLVED 2026-07-30 by the F2 sitting's bell ruling** (the same instinct, finished): strange now seasons all nine fissure tiers at 1/2/4/6/8/6/4/2/1 — rare at the ends, peaked mid-run. *(The old band-1-at-29%-per-cast math predated the 9-tier realignment, which had already dropped tier 1 to bleed-only.)* The Throwable-family half of the original flag is the F2 sitting's still-open mirror question (see the beta-pass F2 entry).
- [x] `[polish]` **Fissure cast message on the surface — FIXED 2026-07-22 (same session as the find):** cast line now branches on the same Z ≤ 10 rule as the stalactite swap; surface reads **"The earth heaves and convulses!"** (user wording), underground keeps "brings the ceiling down." Compiled clean + deployed mid-session — takes effect on next game restart. `[verify]` surface cast shows the new line after restart.

### Verify session 2 findings (2026-07-24, character Karuukat — live log-watch)
*(closed items lifted from the live block of the same name; open flags stayed in TODO.md)*

- [x] `[bug]` **Streakschorl no auto-equip with spares in the bag — FIXED + DEPLOYED 2026-07-24 (user find: "streakschorl doesn't auto equip if you have another in your bag"):** the flight hijack spends the stone OUTSIDE the engine's throw path, so `ReplaceThrownWeaponEvent` never fired — the engine's next-from-bag re-equip and EB's auto-sculpt both listen there and both went hungry (same root as the accuracy-lend leak, third symptom of the bypass). Fix in `Part_StreakFlight`: capture the equipped slot before the split, and after `DoFlight` fire `ReplaceThrownWeaponEvent.Check(thrower, stone, slot)` — only when the slot was really vacated (fallback throws keep the normal path). **FIX FAILED IN TESTING (user flag 2026-07-24, same session: "this fix didn't work in testing session") — root cause diagnosed:** `Check()` only dispatches the event to HANDLERS (EB's auto-sculpt etc.); the engine's actual find-in-bag-and-equip logic lives in `GameObject.EquipReplacementThrownWeapon` AROUND the Check call, and that method is `protected` — the fix fired the notification and skipped the substance. A player without EB gets nothing. (The NPC back-to-back flights read as "proof" were a slot-STACK remainder, not a bag transfer — inference error, noted.) **Corrected fix shape for the post-session batch:** after Check returns true, replicate the engine's default half in StreakFlight — capture the thrown blueprint before flight, search the thrower's inventory for a same-blueprint item (skip broken/rusted/hidden, per the decompiled engine loop), and fire `CommandEquipObject` into the vacated slot. `[verify]` player with spares in bag: next streakschorl lands in hand after a flight.
- [x] `[bug?]` **Farflint step-shunt "doesn't consume the stone" (user flag 2026-07-24 late
  evening, harness session: "farflint shunt on step works, but doesn't consume the stone?") —
  LOG-RESOLVED same minute: WORKING AS DESIGNED.** The observed proc ran the full chain: STEP
  trigger → Teleport fired (reduced, amt 8) → `fragility (step): HOLDS` — a single legitimate
  25% hold roll (farflint rides the house default HoldChance 25; "rolled fresh every use, so a
  lucky stone can keep holding on indefinitely", user ruling 2026-07-22). Optional dials if the
  survival grates, for a rulings batch: (a) keep as-is · (b) `ConsumedOnStep="true"` (the field
  exists) · (c) flavor-fit third way — on a step-proc the stone shunts ITSELF elsewhere too ("a
  stone that was somewhere else a moment ago"), zoning both ways. No change made.
  **USER LEAN RECORDED 2026-07-24 (explicitly NOT a ruling — "i'm honestly not sure. it's a
  design conversation thing"):** "consumed on step is a great baseline for stones that get
  destroyed when walked over" + considering step-breaks at 100% "rather than being sturdy."
  Folds into the PARKED hold-rate/fragility/expiry conversation (worklist Pass 2) — the step
  half of that triad now has a lean on file. Conversation seeds, Claude 2026-07-24: (1) the
  live system already breaks 3 of 4 step-procs (HoldChance 25 = 25% SURVIVE) — the real question
  isn't sturdiness, it's whether the "lucky stone keeps holding on" whimsy survives at 0%; (2) a
  100% baseline needs immediate exemptions — caromcobble's permanent peel IS its identity
  (HoldChance 100 intended), and phosphorescent's glow-mark step-proc becomes single-use zoning;
  (3) step-fragility and throw-fragility share one HoldChance today — a 100%-step rule wants
  either ConsumedOnStep stamped on the base blueprint (one attribute, whole catalog, exempt by
  override) or a separate StepHoldChance dial (C#-small).
  **CONVERSATION ADVANCED 2026-07-25 (rulings batch; still NOT a ruling — "not urgent to
  answer now, but it will be important soonish"):** farflint dials stay AS-IS for now ("all
  fine!"), and the user's lean sharpened toward the discharge principle: **"the main thing
  with farflint is i think if a payload goes off, the stone should be destroyed. but it's
  part of a larger conversation as well, around stones step chance, their dulling period,
  and fragile chance overall."** Claude's proposed frame on the table: a stone is consumed
  by DISCHARGING its payload; persistent-zoning stones (caromcobble, phosphorescent) keep
  their holds — the discharge/zoning distinction replaces any exemption list. Open fork:
  whether any discharge stone keeps a lucky-survivor roll on purpose. The sitting is owed
  SOONISH (their word), covering step chance + dulling period + fragile chance as one system.
  **THE SITTING HAPPENED — FULL SYSTEM RULED 2026-07-25 (the fragility conversation, same
  day; BUILD OWED, one batch). The rulings, each with the user's words:**
  - **Discharge consumes, 100%, no lucky roll** ("if a payload goes off, the stone should be
    destroyed" + "agree with all your leanings": the whimsy funeral is clean — no
    treasured-survivor exception; the zoners carry the charm now).
  - **Spenders trigger at 100% on step** (root complaint: "the annoying thing about step
    chance is... it feels really inconsistent and unclear in practice" — chance-per-crossing
    is illegible on one-charge stones; streakschorl's StepChance 100 + ConsumedOnStep was the
    in-catalog prototype). **Zoners keep their rates** (carom 50 per the morning ruling,
    crack 15) — rates are legible on permanent objects.
  - **Stones never trigger on their owner or the owner's party** (ruling (b) of the ladder,
    "b makes sense"; answers "the only problem(?) with this is that fissure is shaped around
    the player" — the caster would otherwise stand in a self-minefield every cast). BUILD
    NOTE: verify DropRock stamps Owner on fallen rocks; thrown/step Owner threading exists.
  - **StoneStep RETIRED as an individual inclusion** (user: "we could kill the stonestep part
    as an individual inclusion... since all creatures that spawn from seam (right now) are
    friendly, they would already inherit it. so no carveout just for baboons, no conversation
    about apes needing it in the future"). Recorded residue: UNOWNED stones now bite
    everyone including allied stone creatures (rare; accepted); wild stone creatures
    (stonefrog, quarriers) reopen the question at the deferred StoneStep-roster conversation,
    which now asks "does the fuller roster need the tag back, or is party always enough?"
  - **DULL CUT** ("maybe not even necessary in the long run (burgeoning gives unlimited
    cooking ingredients)... since they have weight, it's not like you want to carry an
    unlimited amount of them" + agreed lean): DullAfter → 0 mod-wide; discharge-consumption
    solved hoarding better; weight is the cap; Burgeoning is the precedent. Fallback if
    immortal hoards grate in play: return as a much longer fuse. tfdull retires to the
    ledger as history. (TimedForm creature lifespans are a DIFFERENT system — untouched.)
  - **Glowing twins are SPENDERS** (spend into the 300-turn Luminous mark, webs-rock
    pattern; the mark is the residue, the stone is the vessel).
  - **Fetch/harvest by REACH** ("fetch should check *adjacent* tiles, rather than tiles
    underneath"): baboons detour to a cell BESIDE the stone and reach — supersedes the
    morning wedge fix's stoop-at-d=0 shape (give-up backstop stays as insurance); players
    use the base game's get-nearby (VERIFY at build that it reaches adjacent).
  - **Tanglelith classified at its own redesign** (likely zoner; sits out this batch).
  - **Balance watch carried forward:** liquids 50→100 and house 25→100 is the largest zoning
    jump the mod has made — the one leg that wants a REAL PLAY SESSION before final ratify.
  **Player-facing law, one breath: "A stone keeps until it goes off. It always goes off
  underfoot — unless it's yours. When it goes off, it's gone."**
  **BUILT + DEPLOYED 2026-07-25, same sitting ("ship it"). The batch:** base part line →
  `HoldChance 0, DullAfter 0` + 39 spender StepChance swept to 100 (script-verified; skips:
  carom 50, crack 15, tanglelith 25 awaiting redesign, streak already 100, stalactite/
  vantabsidian 0 = throw-only) · fragility now rolls ONLY when a payload discharged (step:
  effective payload non-empty; throw: PayloadOnThrowHit + payload non-empty — payloadless
  ammo and step-only stones land intact) · party gate on step trigger + ShouldAvoid (owner's
  field is home ground: no trigger, no avoidance, no autowalk interrupt) via
  `Brain.GetFinalLeader()` (compiles, API confirmed) · **ownership follows possession**
  (TakenEvent + AfterThrown stamp Owner; DropRock stamps the Fissure caster) · StoneStep tag
  + check removed · scavenger reach-fetch (detours to a standable cell BESIDE the stone,
  takes at d<=1; wedge backstop kept as insurance; occupied-cell exclusion dropped — reach
  makes those fetchable) · harness: **`tfmine` NEW** (party gate + first-step fire + consumption,
  in tfbatch), tfdull RETIRED (wish reports the cut), chain now 6 turn-driven. Build notes:
  compile clean after 2 real errors (TakenEvent is not pooled — `TakenEvent.ID`; local name
  collision in DropRock); the `--`-in-XML-comment curse struck TWICE in the new comments
  (strikes 8-9; both caught by strict parse, clean file redeployed). Known residues recorded
  above (unowned stones bite everyone; stone stacking keeps one Owner — SameAs excludes it).
  `[verify]` next restart: tfbatch — mine.party + mine.discharge new-green, quick.rep flip
  still pending from the figurine fix, carom on the 9-31 band; then the PLAY-SESSION feel
  check on 100% triggers before final ratify.

- [x] `[bug]` **Baboon figurine, TWO confirmed defects for one batch entry (both 2026-07-24
  night, both one-attribute XML fixes on `Cleo_TerraFirma_BaboonFigurineItem`):**
  (1) **Needs identification (user flag: "unintended")** — inherits base `Trinket`'s
  `<part Name="Examiner" Unknown="UnknownOddTrinket" Complexity="1" />` (Items.xml:11811);
  fix = override `Complexity="0"` (a knee-high stone baboon is self-evident).
  (2) **AddsRep 200 is INERT — harness-CONFIRMED (quick.rep: rep −300→−300 on take, delta 0):**
  `AddsRep` is equipper-only unless `WorksOnCarrier="true"`, which the part never sets; the
  figurine's entire rep half has never functioned. Fix = add the attribute; the standing red
  quick.rep assert flips green as the verification.
  Recon note: sweep other Trinket-inheriting mod items for the same two inherited warts.
  **BOTH FIXES BUILT + DEPLOYED 2026-07-25** (`Examiner Complexity="0"` override +
  `WorksOnCarrier="true"` on the AddsRep part, Rocks.xml; XML strict-parsed, active next
  restart). **Sweep DONE, clean:** the figurine is the mod's only Trinket inheritor and only
  AddsRep carrier; the Statue Maker's `Examiner Complexity="3"` is intentional (tinkerable
  artifact, bits 0013). `[verify]` next tfbatch: quick.rep flips GREEN (rep −300→−100 on take).


### POST-SESSION BUILD BATCH — BUILT + DEPLOYED 2026-07-24 (after the verify marathon wrapped; active next launch)
Eight pieces, all from the day's flags (details live in each flag's own entry):
1. **Streakschorl re-equip RE-FIX** — replicates the engine's protected wrapper: Check() first (handlers may own the slot), then same-blueprint bag search + `CommandEquipObject` into the vacated slot, with readied/refused/no-spare logging. ~~`[verify]` throw last held streakschorl with spares → next lands in hand.~~ **VERIFIED 2026-07-24 evening — harness tfstreak PASS, direct engine-state assertion on the player path (§ Automated verification harness).**
2. **Fungal lottery diagnostic** — the SPORE line now says ROLLED-FRESH vs loaded-from-game-state (persistence was already designed in via SetStringGameState and fails silently; this names the broken link). `[verify]` reload the save: if the line says ROLLED FRESH again, GetStringGameState is returning empty post-load. **DIAGNOSTIC PAID OFF 2026-07-24 late night: first access each boot = empty, all later same-session accesses = loaded — persistence works in-session, dies across boots. Down to two suspects (engine state not persisting vs sessions ending unsaved during harness iteration); the save-quit-reload experiment in § the harness decides. No redesign until it runs.** **CLOSED 2026-07-25: experiment ran via tfspore, permutation 3201 held ×3 across save-quit-reload — sessions-ending-unsaved confirmed, code innocent, no redesign (user: "all 3 tests gave back 3201, looks good").**
3. **SCAV pocket re-ready** — slot empty + pocket stone → forced `CommandEquipObject` as a **FREE action (user ruling, same evening: "don't worry about action costs for equipping thrown weapons, it's actually a free action")**, REFUSED outcomes logged (the H1 smoking-gun detector). `[verify]` baboons re-arm every throw with no lost turn; cadence ≥ vanilla; watch for REFUSED spam naming the regression. **RE-READY VERIFIED 2026-07-24 evening (harness tfscav): every slot->EMPTY answered same-turn by the free re-ready, ZERO REFUSED across 118 baboon-turns — H1 refuted in-arena; dry-fetch loop also ran live. Cadence 0.29 throws/baboon-turn measured; only the ≥vanilla baseline comparison remains open.**
4. **Millstone HOPPER** — new `Part_StoneHopper` on the Millstone merge (menu grind removed per ruling): powered mill grinds one hopper stone per turn into servings (2-3/rock, 5-6/boulder, shared MakePowder recipe), products accumulate in the mill. ~~Throughput 1/turn DRAFT.~~ **Throughput 1/turn RATIFIED 2026-07-25 ("all fine!").** `[verify]` drop rocks in Joppa's mill → powders appear while the wheel turns; dead mill grinds nothing. **MECHANISM VERIFIED 2026-07-24 evening (harness tfhopper PASS: powered 2 stones -> 5 powders, dead mill inert); Joppa's river-powered mill still owed the world-wiring eyeball.**
5. **Statuette popup suppression** — TimedForm stamps `NoFollowerDeathPopup` (property, engine gate is HasTagOrProperty, decomp-verified) just before its own Obliterate: expiry keeps the stiffens line, loses the "died" popup; combat deaths still mourn. `[verify]` next expiry shows no companion-death popup. **Expiry MECHANICS verified 2026-07-24 night (harness tfform PASS ×2: gone + figurine + spill asserted); the popup half specifically still needs an ORGANIC follower-statuette expiry — the harness statuette is no one's companion, so the popup gate never arms in-scenario.**
6. **Calcify DV text** — "0 DV" → "−10 DV" (lean (b) text-follows-reality, ~~DRAFT; flip to a creation-time DV override if vetoed~~ **RATIFIED 2026-07-25, "all fine!"**).
7. **Drainstone victim-died log** — the last silent link in the latch chain now speaks ("pending victim died before the latch could take hold").
8. **Healstone "double-fire" RETRACTED** — batch review showed two interleaved throws, not one double-firing (use-path always-break logs no resolve line by design); no code change needed, entry corrected.


### Weak-rock flags (2026-07-24, out of the pentacle survey — design/fissure-pentacles.html)
*(closed items lifted from the live block of the same name; open flags stayed in TODO.md)*

- [x] `[design]` **Dazestone intervention — RESOLVED BY SUBTRACTION 2026-07-25: DAZESTONE CUT,
  grimslate moved down to hold tier 2** (user: "we could potentially just cut dazestone and move
  grimslate down?" → roster check showed no bald spot → "let's just go for that then"). BUILT +
  DEPLOYED same sitting: out of FissureTier2 (grimslate in from tier 4) and the strange pool;
  blueprint DORMANT (chasm precedent); tfmine's test mine swapped to grimslate (asserts
  Terrified). What closed for free: the open cooking-domain hole (never had a ground twin),
  the dazestone rename slot, one sprite debt, one historyspice edge case. Daze returns to being
  EARTHEN BARRAGE's verb exclusively — the rock was quietly redundant with the headline
  mutation. Tier 2 after: sludgabbro / crack / grimslate (terror is tier 2's CC lesson;
  confusion/sleep stay tier 4's). KNOCK-ON recorded on the thunderclap entry below: its
  "dazestone is the canon daze rock" premise evaporated with the cut.
  *Original flag (for the record):* weakest headliner (survey 6), Dazed 6 mild, cooking OPEN;
  options were (a) split-channel stunGasBreath / (b) stun-gas shunt to the tier-4 trio /
  (c) bespoke daze gas in place — all three mooted by the cut.
- [x] `[design]` **Glowing twins moved DOWN, tier 9 → tier 6 — RULED + BUILT + DEPLOYED
  2026-07-25 (user, same sitting as the dazestone cut: "let's move the glowing twins down to
  tier 6 - they're really not that dangerous, and it pads out the roster more").** The 50/50
  no-tells gamble rides one slot at weight 8, as always. Roster after both moves: T2 and T4
  at 3 headliners, T6 at 4, T9 at 8 slots. Name-slate constraint unchanged (one shared name,
  stamped twice). `[verify]` a tier-6 cast can shed a glowing rock; tier 9 no longer rolls one
  except via the ±1 bleed from tier 8.
- [x] `[design]` **Vitriolite minor gear break — SIGNED OFF + BUILT + DEPLOYED 2026-07-25 (user: "vitrolite etch and compressed ash are easy to sign off on"):** new generic throw-hit RIDER on `Cleo_TerraFirma_FallenRockPart` (`ThrowRider/Chance/Amount` — rides on top of the payload, never replaces it, throw-only, vocabulary starts at ShatteredArmor and grows on demand); vitriolite gets `ShatteredArmor 50% x100 turns` (chance from the flag's 25-50 range, duration = stalactite parity, both DRAFT). Full BreakGear stays deep-only. `[verify]` throw-hit a wearer → ~half the hits log "etch rider ... cracked armor". *Original flag:* (user: "a great addition"). Restore the CUT gear-damage half at T1 scale: chance on throw-hit (25–50, Claude numbers DRAFT) to apply the *etch* — ShatteredArmor (the −1 AV crack, shattered stalactite's own effect) — while full BreakGear stays deep-vitriolite-only. The `Effect`/`BreakGear` channels both already exist on `Cleo_TerraFirma_FallenRockPart`, so this is XML-only. Closes the band-3 weakness (post-CUT it reads as a wetter sludgabbro) without a re-band.
- [x] `[design]` **Compressed ash, both dead channels — SIGNED OFF + BUILT + DEPLOYED 2026-07-25 ("easy to sign off on"; direction ruled 2026-07-24). Built:** (a) choke CUT (`EatenHarm 1d3` + DangerousToEat removed), `EatenSpew MiasmaticAsh` — ash joins the burp family; tfeat's ash row flips harm → belly. (b) **the armorMinor domain EXISTS**: `Ground_CompressedAsh` (type `Cleo_TerraFirma_ArmorMinor`, key CompressedAsh — spice list +1) + a units-only IngredientMapping + `UnitAV` class (+1 AV passive, aegischist's shape one tier down). Draft accepted at build per the ruling ("Claude drafts at build, user veto") — the +1 AV recipe is the veto surface. NOTE: units-only mapping is a first (Web/Spacetime both carry triggers); if the loader objects, the kitchen verify names it and a trigger pair gets borrowed. `[verify]` kitchen round: cook with ground ash → "+1 AV" meal line. *Original direction:* (a) **eaten raw: cut the choke (`EatenHarm 1d3`), join the burp family** — `EatenSpew MiasmaticAsh`, the venting the gas rock always deserved (vertigneiss/lullabaster/conchalk precedent; one tag swap, XML-only). (b) **cooking: build the minor armor domain at last** — the lean-layer blocker ("no weak armor exists base-side") gets resolved by making it ourselves: bespoke `armorMinor`-tier domain on a new `Ground_CompressedAsh`, the Web/Spacetime bespoke-domain precedent. Aegischist keeps big armor; ash becomes its T1 shadow. Domain's unit/trigger/action recipe = Claude drafts at build, user veto.
- [x] `[design]` **Tanglelith tar redesign + pitching fliers down — RULED + BUILT + DEPLOYED
  2026-07-25 (the tar sitting, same day as the discharge law): the stone is a SPENDER, what it
  buys is a ZONE.** Thrown = 3 permanent tar patches (impact + web-scatter; counts DRAFT) +
  victim stuck + a flying victim forced down FIRST (the anti-air shot); underfoot = 1 patch +
  stepper stuck + stone spent (StepChance 100, discharge law). Recon jackpot: the engine's own
  `Sticky` part takes `DestroyOnBreak` as an XML param — the tar patch is the Web recipe with it
  FALSE (a web is spent by catching; tar keeps catching), FlameTemperature 100 + HotBurn 20, no
  Temporary part: permanent, fire is the eraser. Zero subclass; the C# is one `Tar` payload
  channel + `XRL.World.Capabilities.Flight.Fall` (the petrify effect's own grounding idiom,
  found in-mod). This CLOSES the discharge ruling's "tanglelith classified at its own redesign"
  rider — spender-into-zone. **NAME NOT LOCKED (user: "i like the name tanglelith better than
  pitchstone. pitchstone can stay a potential name, but run 4-5 more by me before we lock it
  in") — tanglelith stays DisplayName; mini-slate delivered, verdict owed.** Re-takeoff gate:
  Stuck's movement block is trusted to hold the downed flyer; `tftar` NEW (step discharge +
  patch-survives-catches + flyer-downing, in tfbatch) plus the user eyeball decide if a grounded
  rider is needed. Sprite + patch name/description = user's surface, DRAFT. `[verify]` tftar
  next restart; burn a patch; pyroclase-onto-tar combo. *Original flag:* FLAGGED 2026-07-24 (user: "good to flag as well"; supersedes the not-flagged triage note above). The shadow problem: its Stuck-on-throw is gossamarl's throw minus the webs, minus the domain. The direction: **(a) tar, not cords** — thrown leaves a *persistent sticky ground patch* (a ground object with the `Sticky` part — the verified Web tech minus the burst-of-objects; one-use gossamarl stays the burst, tar becomes reusable ground control) that is **flammable**, completing the floor-pair with oilstone (oil slips, tar roots, both feed pyroclase). Rename candidate for the slate: **pitchstone** — a real volcanic glass, and pitch IS tar. **(b) brings down flyers** — on-hit payload force-lands flying targets then applies the Stuck. Feasibility recon (2026-07-24, recall-grade): C#-small — the engine centralizes flight in the `Flight` part and demonstrably has a force-landing path (stunned flyers crash-land with fall damage, engine-side). *(verify at build, house norms):* exact fall API name (recall says static `Flight.Fall`-shape), whether crashing carries a native re-takeoff delay or needs a small grounded gate, whether Stuck alone blocks lift-off. Payoff bestiary: glowcrows, dragonflies, wasps, every swooper.
- [x] `[design]` **THE MILL LAW — RULED + BUILT + DEPLOYED 2026-07-25 (the mill conversation;
  reverses the 2026-07-22 safe-mill cut — the bluff is real now, and it's a CLASS, not two
  rocks):** grinding captures a stone's potency into flour; ARMED stones (all 16 grenade
  carriers + gyrestone's GrindSpawn = 17 rocks) cannot be captured — the reaction fires at the
  millstone. The user's four riders, their words: (1) gas rocks keep their flours — "instead of
  them being in conflict, they can coexist"; (2) severity split — "stronger reactions, no
  powder. minor things like gas? they can coexist... if hushmica's detonation in the mine makes
  it so there's no output, then we no longer have an artifacts entry" → HE (quarrelquartz) +
  spacetime (gyrestone) forfeit powder; gas/thermal/EMP detonate AND yield; (3) hand tools
  REFUSE armed stones ("too volatile to grind by hand" DRAFT, no turn spent) — the millstone is
  the only station violent enough; (4) **the mill pays +1 serving on every grind** ("since it
  requires power and using a mill, could it give 1 extra flour when milling? this can offset
  some of the danger by adding a reward as well") — user's own number, ruled not draft.
  The law's three stations now agree: mouth (EatenDetonate), whorl (tear-apart), mill. Build:
  hopper armed-branch (Detonate at the mill + GrindSpawn scatter + severity split), hand-grind
  refusal gate, +1 mill rate; tfhopper's yield floor moved ×2 → ×3 BEFORE any re-run. Accepted
  consequences, recorded: an HE detonation can damage the millstone itself (you did that), and
  grinding a glowing twin reveals which it was — destructive tells match the eating precedent.
  `[verify]` next boot: tfhopper green on the new floor; kitchen eyeballs — vertigneiss in the
  hopper → confusion cloud + powder; quarrelquartz → boom, no powder; pestle refuses both.
  *Original entry:* user: "i could see that happening" IF gyrestones still released vortices on grind. THEY DON'T (checked 2026-07-24): the whole mill-hazard layer was CUT in the 2026-07-22 re-ruling (live-fire showed `Destroy` never detonates grenade rocks — the warnings were a bluff; user cut the layer rather than make it real; `Part_StoneMill.cs` header records it, the "Milling it is not gentle" rules text sits in the commented-out block). `GrindSpawn` tags survive only for the Gravel Whorl's tear-apart, which is real. So this flag is now: **reopen the hazard layer and make it real this time?** The machinery exists — the whorl's tear-apart branch (grenades detonate at a cell, GrindSpawn scatters contents) is exactly the recipe the mill would port. One ruling covers both stones: grind a quarrelquartz → it goes off; grind a gyrestone → 3 vortices (restoring the Rocks.xml comment's original intent). Or stand by the safe-mill ruling and strike the stale comment.

### Holographic rock — DESIGNED + RULED + BUILT + DEPLOYED 2026-07-25 (user: "it seems both easy and fun" — it was)
**The ten forks, as ruled (2 user-adjusted, 8 accepted):** Int save **DC 12** (user: "something
like 12") · placement = **generics of tiers 2-3 at weight 5** (user: "it's just a joke rock, so
it's pretty weak... it should also not get picked for seams, which maybe points more to being a
generic" — and the seam guarantee is EXPLICIT: new `Cleo_TerraFirma_NoSeam` tag + a re-roll gate
at the Fissure seam pick, a reusable mechanism) · belief is STICKY globally per character (one
roll ever, both directions — also kills autoexplore loops and save-scumming) · MentalShield/
mindless auto-pass everywhere (machines don't believe: can't be fooled, can't be hit, the
millstone turns on nothing forever and the pestle finds "nothing to grind, however hard you
believe") · it CAN kill (the epitaph is the payoff; death message = user's surface, owed) ·
believed hits do EXACTLY generic-rock dice, 1d3/pen4 (the mundanity is the punchline) and
CONSUME the stone (discharge law: the lie collapses on contact); disbelieved throws pass through
and the rock persists · inert underfoot (StepChance 0) · **excluded from the strange pool**
(corrects the sketch: the purity invariant bars a payload that can no-op; joke mourned in the
pool comment) · the Lithovore placebo eat came FREE (only believers can hold it, so every eater
already believes — plain half-snack of nothing, structurally placebo) · HologramMaterial shimmer
= the tell; wears the generic rock's own tile on purpose. **BUILD WATCH (verify next boot): the
disbelieved-hit damage cancel rides a return-false from WeaponThrowHit — if damage lands on
disbelievers anyway, that's the seam; a `tfholo` scenario is the natural harness add. Also
verify: take-revert vs autoexplore, the shimmer reads.** Name WIP (queued behind the tanglelith
slate); description DRAFT.
*Original parked entry (2026-07-24):*
**Concept (user, verbatim mechanics):** a rock that isn't there. **If you fail an Int check, you can pick it up and throw it at an enemy. If they fail an Int check, they'll be hit by it and take damage.** A weapon that only works between the credulous — the smart see through it on both ends (your hand passes through; the target watches it pass through them). Psychosomatic ballistics.
**Sketch (Claude, all unruled):** save shape = the house `MakeSave` vs Intelligence (any stat is legal as a save stat — Gorgon's Toughness/Willpower precedent); pickup gate = Take/AutoEquip intercepted, Int save decides whether your hands believe in it; hit = target's own Int save or the damage lands (belief as penetration). Render tech *(verify at build)*: base `HologramMaterial` part is the engine's own shimmer (hologram bracelet/holographic ivory family) — the tell would be visible to the OBSERVANT, which is exactly right.
**Open forks for the pass (note, don't solve):** does MentalShield/mindless auto-pass the check (a robot's sensors don't believe in anything) or auto-FAIL (no mind to doubt with)? Can a hologram kill, or does it KO/cap at the brink ("scared to death" vs "dies of a rock that never was")? Lithovore eating one = placebo satiety on a failed check (the funniest possible interaction — flag for the ruling). Mill = presumably nothing to grind (or holographic flour, one joke deeper). Strange rock CAN mimic it (the lottery rolling a rock that isn't there is correct). Band placement wants the smart-enemy curve considered: deep-game enemies are brainy, so it may be a mid-band rock that ages out — or that's its charm.


### Carve-gate attack-any-wall — RULED + BUILT + DEPLOYED 2026-08-05 (supersedes the corridor ruling's silent refusal)
- [x] `[design]` **User flag (same day): silent no-op vs brinestalk/fulcrete "doesn't necessarily track in practice ... i can see the bug reports for that already." RULED same day: "really i'd have it just make the attack on any wall - machinery still gets hurt by the earth, it just doesn't get destroyed ... it'd maybe be less effective on constructed stone in the sense of 'can't fully break it', but that would apply less to things like brinestalk or fungus walls." Messages explicitly rejected ("popup spam") — all feedback is dust.** BUILT: three wave classes off the existing wallProps — **Stone** (natural stone: unchanged, falls, feeds ring mass) · **Soft** (MaterialType "other": plant/fungus/wood — full attack, can fall, spends carve budget, NO ring credit) · **Hard** (metal, machine walls, constructed stone, unmarked fulcrete/brick/ruins: attacked, damage clamped to leave 1 HP, always stops the wave). Every attack puffs chip dust, so the boundary is legible without a single message. Open forks, unruled: **(i)** "the force might break the internals" (user musing) — applying the engine's Broken effect to damaged machine walls, unbuilt; **(ii)** wood in the Soft class is a DRAFT boundary (user named brinestalk/fungus; wood rode the MaterialType); **(iii)** soft falls spending carve-depth squares is DRAFT semantics; **(iv)** level text still reads "carves through N squares of natural stone" — accurate for through-passage, silent on the new hurt-everything behavior; text pass owns it. **FIRST POST-RULING BOOT (2026-08-05 evening): cast code CONFIRMED live in the logs (glass-jaw wood FELL to the wave, metal attacked and held at zero pens) — but tfredoubt's carve scenario still asserted the superseded refuse contract, going red with "L1: WOOD wall attacked or felled". HARNESS REALIGNED same evening (compiles clean, deployed hash-verified — active next restart):** wood probe now a glass-jaw Soft that must FELL · cast-A metal an AV-0 Hard-clamp probe (must be damaged but stand at >=1 HP) · cast-B metal native AV 30 (must hold untouched, PV 18 can never penetrate). `[verify]` tfredoubt carve green next boot.

### Superseded / resolved by the redesign
- [x] `[bug]` ~~Generics8/9 missing → boulder fallback~~ — moot: code stops rolling `FissureTable_Generics{n}` directly (the creature tables reference `Generics8Plus` correctly).
- [x] `[design]` ~~Wire in FissureTable1–9~~ — absorbed into Spec v1 (the one-line flip + yield split).
- [x] `[design]` ~~GetAoE dead/inverted~~ — deleted; replaced by ring breakpoints.
- [x] `[design]` ~~keep-or-cut decision~~ — **kept and promoted to active work (2026-07-22).**

---


---

## Throw-augmenting items  ← opened 2026-07-22 (user concepts delivered; engine recon done; design discussion in progress)

### Thrown-system ground truth (decompiled 2026-07-22; full-assembly dump in session scratchpad)
- **There is no to-hit roll vs DV for ordinary thrown weapons.** `GameObject.PerformThrow`: a throw rolls a *hit budget* = `RollPenetratingSuccesses("1d" + Agility, 3)` (budget spent as `-= distance` per connect), and an **AimVariance** scatter roll = `Random(0,20) − 10`, pulled toward 0 by the Agility modifier. The projectile walks the trajectory; budget > 0 + a combat target in a cell = hit. Accuracy in Qud throwing = *scatter*, not a hit roll.
- **`ThrowToHitBonus`/`ThrowToHitSkillBonus` are consumed by exactly one thing in the game: GeomagneticDisc** (rolls its own Agility+properties attack). For ordinary rocks these properties are dead knobs — Deft Throwing's "+2 to hit" and the cybernetic's ThrowToHitBonus:2 only fire when throwing the disc. **Do not hang item design on ThrowToHitBonus.**
- **The real accuracy knob: `CloseThrowRangeAccuracyBonus` / `...SkillBonus`** (actor int properties, engine takes the max): when target distance ≤ throw Range, `AimVariance × (100 − bonus)/100` — at 100, zero scatter ("your throwing accuracy is 100%", the base cybernetic's own wording). Outside Range it never applies and throws also gain fall-short/long distance variance — so **range bonuses ARE accuracy bonuses** (they widen the perfect-accuracy zone).
- **Range**: base 4 + `ThrowRangeBonus` (read from BOTH actor property and thrown-item property) + `ThrowRangeSkillBonus` + Strength score modifier (`GetThrowProfileEvent.Process`).
- **Damage**: penetration modifier = `max(Strength mod, 1)`; Vorpal → pen = AV; then dice × penetrations (`RollDamagePenetrations`, the eb_sim.py core). **The damage/pen hook (`GetThrownWeaponPerformanceEvent`: DamageDieModifier/DamageModifier/PenetrationBonus fields) dispatches to the thrown OBJECT only — worn gear has no native thrown-damage hook.** Actor-side damage paths: Strength buff (boosts pen natively, crude), or mod-side EB integration (the mutation sculpts its own stones and can read actor state).
- **Benchmarks**: Deft Throwing skill = +3 range, +50 close-accuracy (50 Agi cost); **parabolic muscular subroutine** (2-point Arm cybernetic) = +2 range, 100 close-accuracy — the stabilizer concept's direct base-game cousin, mind the overlap; force knife = item-side ThrowRangeBonus +6.
- **Equipment vehicles, all verified**: `IntPropertyChanger` (generic worn-item property applicator, `WorksOnWearer` default true, IPoweredPart so `ChargeUse="0"` = unpowered — the XML-only route for stabilizers/visor passives); AjiConch = the clean template for an item-activated ability (AddDynamicCommand on equip, cooldown, AI advertisement); timed buffs = standard Effect with Duration (Luminous pattern). Slots: Arm (BaseBracelet/BaseArmlet/LeatherBracer), Face (BaseEyewear; **"Telemetric Visor" already exists** — techscan HUD, name-adjacent). Consumable/refill patterns available: stacked consumables, liquid reservoir (torch pattern), energy cells (tech flavor).
- **Sling architecture fork (user to rule):** (a) worn throw-booster (property route: range trivially, damage only via the caveats above) vs (b) a true `MissileWeapon` that fires stones as ammo — damage/accuracy become the weapon's own stats (easy, strong) but it exits the throwing system entirely (missile skills, no EB-throw synergy, Gravel Whorl deflects it as physical projectile like any bullet).
- **Visor "highlights targets"**: no native target-highlight part found; the mechanical half (hit more often) is fully covered by close-accuracy+range properties; literal highlight rendering would be bespoke C#.


---

## Cooking effects  ← opened 2026-07-22 (user request; engine recon DONE, proposal drafted, awaiting rulings)

### Cooking-system ground truth (decompiled + base XML, 2026-07-22)
- **Any item becomes a campfire ingredient by carrying `<part Name="PreparedCookingIngredient" type="X" charges="1" />`.** No food requirement — Phase Silk is the base-game precedent for a non-food ingredient. `Campfire.cs` enumerates inventory purely by presence of the part (plus unsealed liquid containers whose liquid class defines a cooking type — salt→thirst, convalessence→regenLowtier; liquid types live in the C# liquid classes, so mod liquids would define their own). Cooking **consumes** a serving/charge.
- **`type` keywords are data, not code:** each keyword resolves to a blueprint named `ProceduralCookingIngredient_{type}` inheriting `IngredientMapping` (base game: Data.xml) whose tags do all the work — `Units` / `Triggers` / `Actions` (comma-lists of effect class names), `Description` (auto-composed onto the item: "Adds X-based effects to cooked meals."), and `RandomWeight` / `RandomHighTierWeight` (nonzero = the domain joins `random`/`randomHighTier` ingredient rolls, e.g. Crusty Loaf). **New domains are pure XML** pointing at effect classes; the keyword string is arbitrary (`stone` is free).
- **Effect classes are small C#:** passive unit = `ProceduralCookingEffectUnit` (Init/Apply/Remove/GetDescription, ~30 lines — `CookingDomainArmor_UnitAV` is literally "+2 AV"); trigger = `ProceduralCookingEffectWithTrigger` (registers events, calls `Trigger()` — armor's fires on suffering a 2X+ penetration); triggered action = `ProceduralCookingTriggeredAction`. Procedural meals draw units (passive whole-day buff) or trigger+action pairs ("whenever X, Y") from the domains of the chosen ingredients.
- **Grind/preserve step is pure XML:** `<part Name="PreservableItem" Result="{blueprint}" Number="N" />` on a raw item enables the campfire **preserve** action → N product items (Starapple → 5 Starapple Preserves). Rocks could preserve into powder ingredients.
- **66 base domain keywords.** Of note for us: `thirst`, `fungus`, `heat`, `cold`, `electric`, `phase`, `teleport`, `fear` (Dried Lah Petals), `darkness` (Dried Vanta Petals), `love`, `armor` (Compacted Bone Meal — bone→stone adjacency), `tastyMinor`, `density` (**permanent +AV** unit; RandomWeight 0 / HighTier 10 — the base game gates it behind rare ingredients; if we use it, reserve for something genuinely rare).

### Meal-generation ground truth (decompiled 2026-07-22 second dig + wiki cross-check — the "middle of the campfire" gap is CLOSED)
- **Each selected ingredient contributes exactly ONE domain type** (`GetTypeInstanceAvoidingList` — multi-type ingredients carry comma-lists of type options and pick one not already used; `random`/Have-It-All resolves to a random eligible ingredient's domain). **Duplicate domains collapse** (wiki confirms: "adding the same cooking domain twice has no effect") — two heat rocks in one pot = one heat contribution.
- **Meal effect shape = shuffled permutation walk** (`GenerateEffectFromTypeList`): all permutations of the type list + a null sentinel, shuffled, first viable wins — null/1-type ⇒ `CreateJustUnits(ALL types)`; 2-type ⇒ random trigger from domain A + random action from domain B (both must have them); 3-type ⇒ unit from A + trigger from B + action from C. So 2 ingredients roll {both basics} or {whenever-X-then-Y in either order}; 3 ingredients add the base+trigger+action shape.
- **One random unit per domain per meal** (`GetTag("Units").Split(',').GetRandomElement()`), same for triggers/actions; `@`-prefixed entries indirect through a population table (another blend dial, free).
- **The `*Minor` domains are units-only** (no Triggers/Actions tags) — they can never form "whenever X, Y" combos; full domains can. **A native two-tier lever for us.**
- Counts/limits: choose **2 ingredients, 3 with `CookingAndGathering_Spicer`**; cook **up to 3× per hunger cycle** (`CookCount`); must be hungry to cook; **no Cooking and Gathering skill = no ingredient effects at all** (decomp: the unskilled random path rolls `chance = 0`, never adds types — wiki agrees).
- Inspired/Carbide Chef: rolls **3 candidate effects** from the same algorithm, player picks, result is saved + learned as a named recipe (`CookingRecipe.FromIngredients` → `LearnRecipe`). Other recipe sources: water ritual, recipe books (green names), pariah secret trades. Cooking from a recipe = deterministic stored effect, bypasses the roll.
- Tasty: 10% base (+bonus param), **forced by any `tastyMinor` ingredient** — and base-game **salt (the liquid) is already tastyMinor**, so the chunk-of-salt rock tagging is precedent-perfect. Fasting Way / Mind over Body extend metabolizing duration (wiki; not re-verified in code).


---

## Lithovore (rock-eater defect)  ← opened 2026-07-22; ALL RULINGS LANDED same day; **PASS 1 BUILT + DEPLOYED 2026-07-22** *(the "held until the Fissure verify boot" note is obsolete — the boot cleared same day; deployment confirmed in `Mods\TerraFirma` 2026-07-23, and Lithovore played live in the verification sessions)*

### Build record (2026-07-22)
*(closed items lifted from the live block of the same name; open flags stayed in TODO.md)*

- [x] `[bug]` **Streakschorl flight LEAKED the never-miss accuracy lend — CAUGHT BY LOG-WATCH + FIXED + DEPLOYED 2026-07-23 (active next restart):** the hijacked flight bypasses `WeaponThrowHit`/`AfterThrownEvent` (the lend's return points) and the stone is spent at flight's end, so the thrower kept `CloseThrowRangeAccuracyBonus 100` **permanently** (evidence: one lend line across three flights — flights 2–3 saw the property already present and skipped re-lending; this was exactly the round-12 "stale lend" caveat, now confirmed real and worse than caveated). Fix: `ReturnLentAccuracy` made public; the flight hijack calls it at commit, before SplitFromStack. **Residual: the current playtest character carries a stale permanent +100 close-throw accuracy** from the pre-fix flights — the fix can't retroactively strip it (the property is indistinguishable from the cybernetic's legit grant). User call: keep the god-arm on this character, or strip it manually next session (one-line wish-script).

### Stone milling — superseded pre-build spec (kept for reference)
User concept: preserving rocks requires carrying a **mortar and pestle**. Rulings landed same day:
1. **Tool = the base game's own Pestle** (verified: Items.xml Tier-0 stone cudgel, weight 1, "dusted by humoric powders," already spawns on apothecary NPCs + loot tables, mechanically jobless — the mod gives it its purpose). No new item.
2. **Powder profile:** weight 0, AlwaysStack, no decay/fragility, not throwable; servings per rock 2–3 (DRAFT).
3. **Powders are NOT edible** — purely cooking carriers ("a preservative on cooking"). Whole rocks stay the lithovore's food.
4. **Domainless rocks grind to plain "rock flour"** — closes the dull-rock loop with a real decision: mill before the 500-turn decay clock or lose the domain (the two systems interlock, zero new code).
5. **Grinding grenade-armed rocks warns** (the dangerous-drink prompt genre), doesn't refuse.
6. **UX RULED: batch menu, not per-stone actions** — user: per-stone grind too tedious; wants the preserve-exotic shape (choose specific stacks + quantities — you may want to keep a particular rock). **Native seam verified:** `GetCookingActionsEvent.SendToActorAndObject(player, campfire)` (Campfire.cs:1291) — a mod part can add "Grind stones" to the campfire menu with NO Harmony; `PreserveExotic()`'s decompiled loop (PickOption + AskNumber per stack) is the UI recipe to mirror.
- **Build plan:** one blueprint `Cleo_TerraFirma_StoneMeal` whose `PreparedCookingIngredient.type` + DisplayName are **stamped at grind time from the source rock** (parts are instance data — verify the runtime stamp at build; if it fails, fall back to per-rock powder blueprints + maintenance rule). One small part carrying the menu handler + grind loop. **Open wiring question for build:** where the handler lives — on the Pestle blueprint via Merges.xml *if* GetCookingActionsEvent cascades into inventory (check its CascadeLevel), else on the player (Lithovore part hosts it pass 1, generalized later — but grinding should NOT be lithovore-gated; any cook with a pestle mills).
- **DRAFT numbers awaiting sign-off:** 2–3 servings per rock; boulders grindable for more (5–6?); energy per grind ~1000; campfire required (it's where the menu lives); Cooking and Gathering required (base preserve parity) — flag if the skill gate should be dropped in favor of pestle-only.


### Engine ground truth (decompiled 2026-07-22 — fresh full dump in session scratchpad `decomp/`)
- **Defect declaration is pure XML:** base `Mutations.xml` uses `<category Name="PhysicalDefects" ... Defect="true">` with **negative Cost** entries (`Amphibious Cost="-3"`, `Carnivorous Cost="-2"`), `MaxSelected="1"`, `BearerDescription`, Tile. The mod's Mutations.xml already merges into base categories by name (Physical/Mental) — same pattern, new entry under `PhysicalDefects` with `Cost="-3"`.
- **Carnivorous (the base "you can only eat X" defect) is a hollow marker class** — every behavior is a hardcoded `HasPart<Carnivorous>()` check scattered across `Food.cs` (satiation gate, Gross-food bypass, IllnessChance 50% raw non-meat), `Stomach.cs` (`"AddFood"` string-event handler), and `Campfire.cs` (plant/fungus ingredient + recipe blocks, 50% Ill meal path). **A mod defect can ride none of it** — we build our own gates. All hooks verified native:
  - *Block normal food:* defect part handles `BeforeConsumeEvent` — verified dispatched to the **Actor** first (`IConsumeEvent.DispatchAll`), `Eat`/`Drink` flags distinguish channels, and `Food.cs` checks it before any satiation/heal lands; returning false cancels cleanly. Gate `Eat` only — drinking untouched.
  - *Eat rocks:* defect part handles `OwnerGetInventoryActionsEvent` (verified, `IInventoryActionsEvent`) → adds an "Eat" action to `Cleo_TerraFirma_IsAnyStone`-tagged items. **No Food part on rocks** — nobody without the defect can eat them, and no part-order collisions with grenade-armed rocks. On command: fire `"AddFood" Satiation=Snack` at self (Stomach handles natively; honey/algae/cider liquids are the precedent callers), apply the bonus effect, `UseEnergy(1000)`, split one off the stack and destroy (Food.cs's own sequence).
  - *Bonus effect:* `FirePayload(eater, eater, ...)` already exists (attacker param threaded for lovestone) — reusable where self-targeting makes sense; needs per-rock judgment (eating grimslate to terrify yourself is comedy, not a bonus).
- **The one wall: cooked meals.** Campfire's eat-a-meal path is direct method calls (`ClearHunger()` at 4 sites, recipe effects via `ApplyEffectsTo`) with the Carnivorous restriction hardcoded inline — no event a mod part can catch. Any meal restriction for the rock-eater = **Harmony** (policy: avoid-preferred, not strict; the Scanning postfix is the surviving precedent).

### Forks to rule (one at a time, per house style)
1. **Meal policy — RULED 2026-07-22: C, reframed as an ingredient-class ban (the Carnivorous mirror inverted).** Lithovores are banned from cooking with **plants and meats**; **stones and ALL liquids are legal**, and by class-ban logic so are the exotic ingredients that are neither (bone meal, mirror dust, spark tick plasma, dream smoke, congealed love...). Mechanical mirror of Carnivorous, per decompiled shape: banned ingredients are **hidden from the lithovore's ingredient picker** (Campfire.cs:813 pattern — can't transgress by accident), while preset oven meals and learned recipes containing a banned class hit the punishment path (no nourishment + 50% Ill, Campfire.cs:743 pattern). Harmony sites: the picker filter + preset-meal eat + the two recipe-cook paths (the four call sites already mapped). Detection: `HasTag("Plant")`/`HasTag("Meat")` on ingredients; recipes via `HasPlants()` + a meat equivalent (verify `HasMeat()` exists or compute from stored blueprints at build). **Both clarifications RATIFIED 2026-07-22:** (a) fungus stays legal — mushrooms are honorary minerals for lithovores (they grow in caves; the fungal rocks agree); (b) plain no-ingredient meals nourish (lenient falls out of class-ban logic; accepted).
2. **Eaten-effect model — DIRECTION RULED (user, 2026-07-22): reuse base cooking effects wherever possible** (soupstone = weak hydration, sludgestone = weak slime, chunk of salt = always tasty), with an "inedible/dangerous to eat" section. **Full draft table delivered: `design/rock-eaten-effects.md`** — all catalog entries mapped to verified base classes (5 sections: buff reuse / gas-rock "burp family" / plain rations / dangerous / structurally inedible) + 5 sub-forks awaiting rulings (strength dial, burp-family ratification, danger policy, deep-four risk-reward, strange-rock recursion). Key engine wins recorded there: `ProceduralCookingEffect.CreateSpecific(units)` is a public static factory taking class-name strings (mod classes resolve too), and both cooking-effect families natively expire on `BecameHungry`/`ClearFoodEffects` — eaten-effects follow food rules (until next hunger, one at a time) with zero duration code.
3. **What counts as a rock:** all `IsAnyStone` (Small Stones become trail rations; you can eat your own EB pebbles — funny, probably fine) vs fissure rocks only (tighter, needs its own tag check).
4. **Cost sanity check (user's number either way):** Carnivorous is **−2** for a harsher day-to-day restriction with zero upside; −3 for a defect that Fissure trivializes (food literally rains from the ceiling) *plus* eat-bonuses is generous unless fork 1 lands on (B)/(C). Counterweights that are real: stone weight vs food weight, and losing the cooking metagame under (B)/(C).
5. **Exclusions:** `Carnivorous` (the combo breaks our AddFood path: their handler eats the satiation and rolls 50% Ill on non-meat) and likely `Photosynthetic Skin` (the base food-economy pair already excludes Carnivorous both ways).
- **Free wins, no rulings needed:** plain fallen rock is decay-exempt (`DullAfter=0`) → the natural shelf-stable ration, finally giving dull rocks the use the cooking section wanted; hoard-or-hurl grows its third verb (*or swallow*) organically; fragility never interacts (eating consumes outright, no HoldChance roll).
- **Ruling round 3 (2026-07-22) — the defect's basics, all RULED:**
  - **Name: Lithovore.** BearerDescription proposed default "lithovores" (flag for text pass).
  - **Cost: 2 MP** (`Cost="-2"`) — Carnivorous parity; user had misremembered Carnivorous at 3.
  - **Scope: all `IsAnyStone` PLUS gemstones.** Gemstone eaten-effect deferred to edibility passes 2/3 — user concept EARMARKED there: Ego bonus is blocked (lovestone owns Ego), so instead possibly tie to the relic-style transmute-enemy-into-gemstone effect as a serious cost/value proposition.
  - **Exclusions: Carnivorous + Photosynthetic Skin** (mirrors the base PS↔Carnivorous pair).
  - **Danger policy: eatable with warning** — condition verified in decomp: base dangerous drinks prompt `Popup.WarnYesNoCancel("Are you sure you want to drink that?")` behind `Options.ConfirmDangerousLiquid` (LiquidVolume.cs:3138). Dangerous rock eats mirror the same prompt + option gate.
  - **Deep four eaten: pure danger RULED, plus approved direction — "powerful elemental pulse" on eat** (fire the stone's elemental payload centered on the eater; numbers at build).
  - **Statue-eating: living Stoneshaper statues COUNT** — "eating your buddy because you're starving makes thematic sense"; implementation shape (deanimate-then-consume vs direct) decided at build.
  - **Strange rock RULED (round 4):** cooked = one random domain **from the rock ingredient set** (comma option list of every assigned rock domain — native pick-one mechanism; maintenance rule: the list grows with the table), NOT Have-It-All's global `random`. Eaten raw = roll only rocks whose eaten entry actually does something (no fizzles). Always-warn stands. **User also reaffirmed: the strange rock's mimic table needs a cleanup pass at some point — "some of the things it has don't really make sense"; adds mandate to the held strange-stone audit (see Fissure section).**
- **Ruling round 2 (2026-07-22):** raw-eat model re-based on base raw-ingredient behavior — verified: base preserved ingredients eaten raw do NOTHING (`Preservable` = `Food Satiation="None"`), and base expresses raw-eat effects through a family of twenty stock `*OnEat` parts (Dreadroot's `FearOnEat 3d8/4d6` is the magnitude register). Eaten-effects become stock OnEat parts at snack-grade dice; the defect's eat channel replays Food's own event sequence so they fire natively; rocks carry no live Food part. Full findings + pure-XML mappings in `design/rock-eaten-effects.md` § "Ruling round 2". **Liquids stay legal cooking partners for lithovores** (constrains the meal-policy fork: whatever shape lands, liquid ingredients don't get blocked). **Statue-eating RULED as concept: famished lithovores can eat statues, the way carnivorous creatures eat corpses.** Open details, user designs: which statues qualify (base `Statue`/`RandomStatue` furniture; the mod's inert container statues — eating one should spill the entombed gear via the existing `ReturnPossessions` helper; gorgon-victim statues = eating the petrified, maximally Qud); mechanism (statues are world objects, not items — bespoke "devour" interaction on an adjacent statue from the defect part, famished-gated exactly like base Gross food, `HungerLevel >= 2`); satiation value and whole-vs-bites.
- **Extensions floated ("and more…"), parked unruled:** stonecarver NPCs carrying the defect (they snack on the quarry — free lore texture for the Quarriers workstream); wall-growth ingredients feeding the same pantry; the stone-soup named recipe (Tier 3 stretch) as the defect's signature meal.

---


---

## Quarriers in the wild (side-content workstream 3 — ACTIVE 2026-07-22)

### Engine recon (2026-07-22, decompiled + XML ground truth)
- Canyon spawns: `DesertCanyon` cells → `DesertCanyonPerSector` (pack roll: `Nothing` w8400 vs `SnapjawParty1` w1500) and `DesertCanyonZoneGlobals-Reachable`; deep-cave variants exist (`DesertCanyonCavePopulation` etc.). Mod merges weighted entries — pure XML.
- Pack idiom: party population table, `pickeach` with Chance/Number per role (`SnapjawParty1` is the template).
- Stilt merchant: `SixDayTents` builder rolls the `StiltTents` pickone table (standard tents w4, rares w1) — merge a new tent table entry; tent table places merchant + furnishings via hints; merchant = `BaseMerchant` child + `ConversationScript` + `GenericInventoryRestocker Table="..." Chance="71"`.
- Non-expiring stones dial: `DullAfter` on our own FallenRockPart.

---


---

## Pending design budget (ideas parked, not committed)

### Creature/NPC groundwork (recon 2026-07-22 — all decompiled ground truth, ready for the creatures workstream)
- **No mod mutation advertises to NPC brains except Fissure** (got its `AIGetOffensiveAbilityListEvent` handler in the rebuild). The pattern is ~10 lines: handle the event, `E.Add(COMMAND_ID)` gated on `E.Distance` / LOS / `IsMyActivatedAbilityAIUsable` — Cryokinesis, Sunder Mind, and Burgeoning are clean templates.
- **AI-fired commands carry the target**: `AICommandList.HandleCommandList` → `CommandEvent.Send(Owner, Command, Target, ...)`, so `E.Target` is populated in `CommandEvent` handlers. Player-only `PickDestinationCell` paths (Gorgon's Gaze, Stoneshaper) need an NPC branch that reads `E.Target` instead.
- **NPC channel maintenance** (for wild gorgons): Sunder Mind's caster-side `MemberOfPsychicBattle` effect forces `PassTurn()` in `BeforeTakeActionEvent` for non-player casters — mirror that in Gorgon's `BeginTakeActionEvent` (tick first, then pass). Note: `TickGaze` currently only breaks on zone-exit; a per-tick LOS-break dial was discussed 2026-07-22 and left **unruled**.
- **Blueprint `<mutation Name="X">` resolves by CLASS name** (`XRL.World.Parts.Mutation.X`, verified in GameObjectFactory) — creatures use `<mutation Name="Cleo_TerraFirma_GorgonsGaze" Level="4" />`, not the display name.
- **Save math**: defender rolls d20 + stat mod vs (base DC + attacker's AttackerStat mod). Creature Willpower is the gorgon difficulty dial: +1 DC per 2 points above 16.
- **Benchmarks**: Lithofex — L35, 175 HP, AV 12, Artillery role, one-save-or-die DC 20+Ego gaze, range 7+Lv after 3-turn telegraph. Cragmensch Boulderer — `FabricateFromSelf ... AIUseForThrowing="true"` is **native AI rock-throwing** (carves boulders out of its own HP): the zero-new-code path for wild throwers; the mod's daze part is blueprint-attachable for a wild-pebble variant.
- **Calcify NPC gap** (parked above, still open): handler is player-only cell-picking (`Mutation_Calcify.cs:65`), no AI advertisement — wild calcifiers need both built.
- **Nothing is designed.** Creature sketches floated in the 2026-07-22 session (gorgon ~L18–20 Artillery, defensive calcifier, Boulderer-pattern slinger) were reconnaissance only — user designs creatures collaboratively; no concepts, names, stats, or descriptions are committed.
- Side note: Burrowing Urchin likely already joins dynamic urchin encounter pools (inherits `BaseUrchin`, no `ExcludeFromDynamicEncounters`) — answers the old companion `[verify]` item pending in-game confirmation.

- **Modular disable options (user, 2026-07-24)** — an options menu where any mutation from the set can be individually disabled, plus (potentially) one switch to turn off all mod-added stones. Not now; park until wanted. **Mechanism RECON DONE 2026-07-24 (decomp + a friend's recipe):** the key insight (friend's, premise decomp-CONFIRMED): creature blueprints resolve mutations by C# CLASS name, never through Mutations.xml — so hiding the Mutations.xml entry disables chargen/mutation-buy while every creature keeps working. The engine primitives (MutationFactory decomp): `Hidden` attribute at file/category/entry level (the loader's own deprecation note says "set Hidden attribute instead" of removal), entries MERGE BY NAME across files, and `ExcludeFromPool` covers random-mutation rolls. The friend's two-file version (Mutations.xml + HiddenMutations.xml switched per option) needs an option-conditional file-load facility we could NOT confirm in decomp; the VERIFIED route is the engine's own Prerelease gate pattern (MutationFactory.HandleMutationNode: `if (Prerelease && !Options.EnablePrereleaseContent) { Hidden = true; ExcludeFromPool = true; }`) — a small mod hook post-Init applies the same two flags per our options. Options changes take effect on restart (factory inits once). Stones switch: option check at the Fissure roll + table entry points. Save-compat rule still owed once public: disabling mid-save orphans existing mutants/objects.
- **Visible "crumbling" status on timed falls (friend suggestion via user, 2026-07-24)** — the mechanic itself is ALREADY BUILT (`Part_TimedForm`, same day, convergent design: ape → rubble, baboon → figurine); the flag-worthy delta is presentation: a visible debuff/effect on the creature ("crumbling") so the player can see it's temporary, instead of the current silent countdown. Optional polish; a small Effect costume over the existing part if wanted.
- **"Can buy, can't sell" on fissure stones (friend suggestion via user, 2026-07-24)** — economy guard against infinite-Fissure gold farming. Probably unnecessary, as the user's own framing notes: rocks decay (DullAfter), base Commerce is 1 dram (selling spam is beggar's work), and Burgeoning's unlimited harvestables are the tolerated precedent. Park; if ever wanted, recon whether the engine has a clean no-sell facility (none known offhand) vs Commerce 0.
- **Real wall-phasing on phasalt (SLIM POTENTIAL — user 2026-07-25: "agree, put it in design budget as a slim potential")** — the deliberate successor to the gated no-op (the Effect/Phased payload now withholds itself from walls, 2026-07-25): a phasalt wall-hit that genuinely opens the wall for phase-walking. Would need real design (whose phase changes, duration, render, doorway abuse) plus decomp of whether a Phased wall actually admits normal walkers — the user's 8-throw test suggests the engine never manifests it. Slim by their word; park until wanted.
- **Geode family (user, 2026-07-23)** — "potentially adding geodes": broaden beyond the existing brine geode (né soupstone) into a crack-open family. Natural hooks: gemstone-bearing geodes (Geomancy scan synergy + the transmute-gemstone earmark), crystal/mineral yields feeding the kitchen, the crush/"Detonate reads as crush" action as the opening verb. Undesigned; park until wanted.
- **Pour option on the liquid rocks (user, 2026-07-23)** — brine geode / sludgestone / oilstone (when built) should be emptiable into containers. Two shapes at build: (a) give the rocks a real `LiquidVolume` — native Pour/Fill interactions free, but adds drink/weight semantics and interacts with the Pass 2 **pool-volume audit** (brine geode's 500-dram "pool" render — decide the dram budget once, both features use it); (b) bespoke "empty" inventory action minting N drams into a chosen container, rock crumbles/dulls after. Claude leans (a) if the audit lands on sane volumes.
- **Creatures that spawn with these mutations innately** — wild gorgons, calcifying beasts, etc. *(Gorgon's Willpower-scaled DC makes creature Willpower the difficulty knob.)*
- **Flavor items + worldbuilding** — books, relics, lore hooks tying the mutations into Qud.
- **Wall kick** — as an item, skill, or mutation; form undecided.
- **Retrofitting mutations onto one-time-use items** — won't fit most of them, but Geomancy is a good candidate (a consumable "throw this stone, reveal the area" item).

---


---

## Automated verification harness  ← NEW WORKSTREAM (user-blessed 2026-07-24: "i'm pro building this but mostly because i'm curious if it will actually work out")

### Decomp-verified foundations (all 2026-07-24, game 2.0.211.50)
- **Player-path throws**: `GameObject.PerformThrow` is public and fires `BeforeThrown` internally
  (GameObject.cs:15333) — a direct call IS the full player pipeline. On the streakschorl hijack
  path (handler returns false) PerformThrow exits BEFORE the engine's own protected
  `EquipReplacementThrownWeapon` (15340 vs 15344), so the mod's replicated flow is provably the
  only re-equip in play — the scenario tests exactly the re-fix.
- **Turn pump**: the Paralyzed idiom — `BeginTakeActionEvent` + `E.PreventAction = true` on a
  player-attached part; the engine zeroes the actor's energy itself (ActionManager.cs:743-746).
  World runs at full AI while the player provably does nothing; immune to AutoAct
  hostile-interrupts, no UI involvement.
- **Popups**: `XRL.UI.Popup.Suppress` (static bool) around scenario actions.
- **Force-powering a wish-spawned mill**: `Mill : IPoweredPart`, so `ChargeUse = 0` = trivially
  powered, unpayable ChargeUse = guaranteed dead — both hopper branches forced without a power grid.
- **Stack trap dodged (memory note "slot-stack remainders mimic bag re-equips")**: streak setup
  equips ONE stone BEFORE granting spares — three-at-once would merge into an equipped stack and
  the flight's stack-split would fake the pass.

### PoC scenarios (the current batch's riskiest items)
- `tfstreak` — synchronous: spawns a baboon on a 3-cell lane, equips 1 streakschorl + 2 bag
  spares, `PerformThrow`, asserts the slot re-armed with a DIFFERENT streakschorl object.
- `tfhopper` — 5 turns: two spawned mills (force-live/force-dead), 2 salt rocks each; asserts
  live mill = 0 stones + ≥4 powders, dead mill = 2 stones + 0 powders. Tick-1 line logs both
  mills' `IsReady` so a force-power failure names itself as a harness artifact, not a mod bug.
- `tfscav` — 30 turns: 4 spawned baboons (3 Generic stones each) vs the standing player;
  per-baboon counter parts sample the thrown slot every baboon-turn ((ref,count) shrink/swap =
  throw, cap 1/turn; slot EMPTY + stones pocketed = STARVED turn — the cadence regression's
  in-code H1 signature). PASS = 0 starved + >0 throws; cadence REPORTED not asserted (≥vanilla
  still needs the user's control-run baseline). NOTE: the player eats 30 turns of baboon
  throws — run on a character that can shrug it off.
- `tfverify` — help + kill-switch status.
- Housekeeping: spawns stamped `TFHarnessSpawn=1`, swept (Obliterate) at scenario finish;
  mid-scenario save/load aborts as INCONCLUSIVE (live state is deliberately non-serialized)
  instead of asserting against reloaded ghosts.

**→ THE LEDGER: `design/harness-verified.md`** (user request 2026-07-24: "keep a tab on anything
solved that way so if things go funny in the future, we know where to look") — one entry per
harness-proven fact with the exact assertion, the explicit NOT-covered limits, and the re-run
wish. If a listed behavior goes funny later: re-run the wish first; a flipped verdict localizes
the regression, an unchanged PASS points elsewhere. Maintain on every run that proves something.

### First live run — 2026-07-24, same evening (user: "i got pass, pass, fail, but on retrying 1, only fail")
**Verdict: the harness works.** Two scenario PASSes closed real batch verifies on first contact;
the two failures were both HARNESS bugs, both diagnosed from the log in minutes, both fixed +
redeployed same evening (fixes active next restart):
- [x] **tfstreak run 1 PASS** — "slot after flight = Hypersonic x1, bag 2->1 (re-equipped from
  bag, player path)". **Batch item 1 (re-equip re-fix) VERIFIED by direct engine-state assertion**
  — sheet updated. Remaining streakschorl tails (whorl-vs-flight, render modes, save/load) unchanged.
- [x] **tfhopper PASS** — tick-1 readiness line proved the ChargeUse forcing (live=True dead=False);
  powered mill: 2 stones -> 5 powders in 5 turns, dead mill inert. **Batch item 4's MECHANISM
  verified**; Joppa's real river-powered mill still gets an eyeball for the world-wiring half.
- [x] **tfscav "FAIL" = harness sampling artifact, and the data inside it is a PASS for batch
  item 3:** all 8 "starved" turns were phantoms — the counter samples before the scavenger's
  same-turn free re-ready; the log shows every slot->EMPTY answered by "pocket re-ready ...
  (free action)" within the turn, **ZERO REFUSED lines (H1's smoking gun stayed silent)**, plus
  the dry-fetch loop working live (detour d=5-8 -> stoop -> re-armed). Counter FIXED: starved
  now = EMPTY on two consecutive samples (emptiness that survives a whole turn). Cadence
  measured 0.29 throws/baboon-turn over 118 baboon-turns — the ≥vanilla comparison still owes
  the control baseline (vanilla can't run under the mod's global merges; Tier 2 territory or a
  mod-disabled boot). Bonus find: baboons carry native Small Stones alongside the granted rocks
  and throw both — pockets read 6 at spawn, not 3.
- [x] **tfstreak retry SETUP FAIL = harness stacking bug, FIXED:** run 1's re-equipped stone
  still occupied the slot; the retry's new stone stacked into the leftovers and the held ref
  went stale ("could not equip"). Fix: normalize the slot (`BodyPart.ForceUnequip`) then equip
  by `ForceEquipObject` — never transits the bag (both APIs decomp-verified).

### Round 2 — BUILT + DEPLOYED 2026-07-24, later the same evening (compiles clean; UNTESTED — activates next restart)
User picked the round: "the only one i don't know if is actually necessary is tfsurge because i
swear it's triggered already. we could just test it in less manufactured conditions and get an
actual outcome i feel like" — **tfsurge SKIPPED, SURGE stays an organic appendix item** (note:
zero [SURGE] lines in the two surviving Unity logs, but those only cover the last two boots —
the user's sighting may predate them; if organic play keeps coming up empty, the manufactured
scenario earns reconsideration). Built:
- **`tfstrange`** (synchronous) — the strange audit invariant as a standing assertion: 50 pool
  rolls, every blueprint must detonate or mimic (creature-free, never strange itself, no
  payloadless fizzles); prototypes created in memory + obliterated, zero zone side effects.
  This is the invariant most likely to rot when future rocks join the catalog. The live
  "visibly does something" fire-through stays an eyeball item.
- **`tfform`** (5 turns) — statuette expiry mechanics: TimedForm clock wound to 2 (`TurnsLeft`
  public, set pre-first-tick so the 500-1000 roll never runs), asserts statuette GONE +
  figurine-item zone-count +1 + pocket-stone spill counted beside it; figurine cleaned up on
  pass. The popup ABSENCE stays the user's eyeball (the property stamp is pre-obliteration,
  unobservable after the fact).
- **`tfbatch`** — all five: streak + strange synchronously, then hopper → form → scav chained
  through a driver `Queue` field (~40 turns standing still; driver removes itself BEFORE
  chaining so two drivers never coexist; a mid-chain save/load abort drops the queue too).
- Refactor: wishes are now thin wrappers over `Setup*(ply, queue)` internals; setup failures
  chain onward instead of stranding the queue.

### Round 2 first live run — 2026-07-24, same night (user: "strange and hopper both green though"; tfform "didn't return a pass")
- [x] **tfstrange PASS twice** (standalone + in-batch: 50 rolls each, 32/28 distinct blueprints,
  zero nulls, zero bad) — **the strange-pool purity invariant is now harness-verified**; ledger
  entry added. Streak + hopper re-PASSed under harness v2 (the retry fix held in-batch).
- [x] **tfform NO-VERDICT + batch chain death = ONE harness bug, root-caused from the log in
  minutes: `RemovePart` NULLS `ParentObject`.** Round 2's remove-before-finish reordering (the
  double-driver guard) broke every finisher that read ParentObject: three identical NREs — two
  tfform runs (died at `FinishForm`'s zone read, AFTER the FORM expiry line proved the mod's
  own chain fired: spill + figurine replacement) and one at hopper's post-verdict sweep, which
  is exactly where the batch chain died (form/scav never set up in-batch; the later FORM line
  was the user's manual retry). Round 1 never hit it because the old ordering finished first.
  **FIXED + DEPLOYED same night: capture the owner before removal, pass it through
  Finish/FinishForm/FinishScav/sweep/chain.** tfform's assert itself remains UNRUN — no verdict
  exists either way on the statuette scenario; next restart answers it.
- [x] `[verify]` **Next restart: tfform + tfbatch full chain — ALL GREEN 2026-07-24 night
  (user: "all green from tests!"):** tfform PASS ×2 (expiry mechanics asserted: gone + figurine
  +1 + spill counted; ledger entry with the follower-popup limit noted — the wish statuette is
  no one's companion, so the popup half stays an organic eyeball), full tfbatch chain PASS ×5
  hands-free, scav v2 counted **0 starved / 87 baboon-turns** (round 1's log reading is now an
  assertion) with cadence stable 0.31 vs 0.29. Zero mod errors. **The harness experiment is a
  clean success: 5 standing scenarios, 7 mod facts leddered, 4 harness bugs found-and-fixed by
  its own runs, 2 flags (figurine ID, farflint) resolved from its sessions' logs.**

### Round 3 — BUILT + DEPLOYED 2026-07-24 night (user: "just build as many of these as you think will be useful"; compiles clean, UNTESTED, next restart)
Six new scenarios + a key discovery-by-recon. The forced-crossing trick (`DirectMoveTo` forced
entries fire `ObjectEnteredCellEvent` synchronously) makes step-proc statistics INSTANT — no
turn pump — so four of the six are synchronous:
- **`tfquick`** — the quick shelf: baboon pocket counts (Hulking ≥1 / Quartz 0) · figurine
  rep-on-take · healstone step-heal + ConsumedOnStep (step path avoids the throw-damage
  confound) · aegischist cage ring (≥5 forcefield segments) · + ape-statue → rubble via a
  5-turn driver. **`quick.rep` is EXPECTED TO FAIL — a red test documenting a suspected real
  bug found during recon:** `AddsRep` is equipper-only by default (decomp: the TakenEvent
  carry path is gated on `WorksOnCarrier`, which our figurine XML never sets) — so the
  DRAFT AddsRep 200 likely NEVER applies to a carried trinket. Delta 0 in the run = confirmed;
  fix = `WorksOnCarrier="true"` on the part, one attribute, next batch. Delta 200 = the engine
  has a path recon missed, assert flips green, no fix needed.
- **`tfcarom`** (instant) — 40 forced crossings of the peel: trips in the 3–22 band
  (StepChance 25 binomial), peel intact through every proc (HoldChance 100). Re-run target for
  the pending StepChance re-ruling: rule the number, adjust the band, re-run.
- **`tfcrack`** (instant) — forced crossings until the 15% FallThrough procs (60 tries, miss
  odds ~0.006%); asserts the victim actually changed zones.
- **`tfspill`** (instant) — brine geode destroyed by damage (not obliteration) spills 4–32
  drams; asserts the puddle and its volume.
- **`tfdull`** (5 turns) — the anti-hoarding clock, never verified end-to-end (nobody carries a
  rock 1000 turns on purpose): DullAfter wound to 2, asserts crumble-to-pebbles in the bag.
- **`tfdrain`** (25 turns) — the full drainstone lifecycle in one scenario: player-path latch +
  LifeDrain on flesh · starved attach on a Scrapbot (no leech, IsOrganic gate) · the one-shot
  path (1-HP victim dies to the throw, corpse can't latch, stone loose as loot) · then both
  endings ride the pump: gorged burst (stone gone, drain over, HP down) vs dry crumble (robot
  HP untouched, stone gone).
- **`tfbatch` regrown**: 6 synchronous (streak, strange, quick-shelf, carom, crack, spill) +
  5 chained turn-driven (hopper → form → quickape → dull → drain → scav), ~75 turns total.
- SKIPPED this round (recorded reasons): `tffissure` tier-spread (zone chaos + cooldown
  wrangling; tier system already has a live partial) · `tfeat` + Stoneshaper anatomy sweep +
  Gorgon DC curve = round 4 candidates.


### The miss saga — SOLVED 2026-07-24 late night (watcher window + decomp; user hypothesis "it's just not penetrating?" ruled out honorably)
The watcher caught a repeat batch run on the old build and the cage throw's own lines told it:
"throw resolved: at rest; hit-payload fired=False -> miss: payload fires at the landing cell."
**ALL the round-3 throw failures (cage 3/8, drain flesh-in-batch, drain robot both runs) were
plain MISSES.** The chain of facts, each decomp- or XML-verified:
- The user's penetration theory was reasonable but the engine clears it: `WeaponThrowHit` fires
  on any CONNECTED hit regardless of penetrations (GameObject.cs:15612 — zero pens still
  delivers the Defender), so a hit-no-pen robot would still latch. The robot never connected.
- **Only streakschorl is NeverMiss** (one blueprint, Rocks.xml:633) — the catalog throws
  honestly: AimVariance scatter + the engine's fall-one-short distance variance. Misses firing
  the payload at the landing cell is WORKING AS DESIGNED mod-side.
- The verify marathons never surfaced this because **the playtest god-arm (+100
  CloseThrowRangeAccuracyBonus) masked the whole layer** (GameObject.cs:15389: the property
  zeroes AimVariance AND skips the fall-short branch) — the stale-accuracy cleanup item just
  earned its keep as an accidental test-fixture confound.
- **Harness bug #5, FIXED + DEPLOYED: ThrowAt assumed guaranteed hits.** Now it uses the mod's
  own streakschorl lend idiom — stamp the accuracy property for the throw, return it after
  (mirrors `ReturnLentAccuracy`, RemoveIfZero and all). Deterministic hits, no god-arm needed.
- Residual: drainstone-vs-robot is DOWNGRADED from bug-candidate to "probably just missed
  twice"; next tfdrain (lend + victim-named diagnostics) closes it either way.

### Fungal lottery — diagnosis SHARPENED 2026-07-24 late night (same watcher window)
Three SPORE lines in one boot: first fungal-rock creation "ROLLED FRESH - stored state was
empty" (rolled 1203, stored), next two "loaded from game state" (1203, 1203). So persistence
works ALL SESSION; the stored key is gone at the NEXT boot's first access. Exactly two suspects
left, and tonight's data cannot split them: **(a)** `SetStringGameState` genuinely not surviving
save/quit/load, or **(b)** the harness-iteration sessions END WITHOUT SAVING (quit-without-save /
killed window loses the write legitimately — the code would be innocent). **The 30-second
decider, user's hands:** note the permutation, SAVE AND QUIT properly, reload, mint any fungal
rock (a tfstrange roll usually does it), compare. Same = (b), close the bug; different = (a),
design real storage. Do this BEFORE any redesign.
**DECIDED 2026-07-25 via the tfspore pure-read wish (user: "all 3 tests gave back 3201, looks
good"): (b) CONFIRMED — 3201 held across mint, save-quit, and reload. The lottery bug is
CLOSED as not-a-bug; the storage design was right all along. Ledger entry added.**

### Lend-build run — 2026-07-24 late night: THE DRAINSTONE LIFECYCLE CLOSES
tfdrain on the lend build: **drain.latch PASS · drain.robot PASS ("LATCHED onto Scrapbot …
STARVED (inorganic, no leech)" — the ruled attach-but-no-leech executed for the FIRST TIME
EVER) · drain.oneshot PASS · both endings fired on schedule (gorged burst + dry crumble).**
The endings composite red was harness bug #7 (over-strict assert: the Scrapbot REGENERATED the
throw's 2 scratch damage 98→100, proving no-drain; fixed to >=). The robot-latch bug candidate
is CLOSED — misses all along. The drainstone-vs-inorganics `[verify]` line (worklist Pass 2) is
fully closed; four ledger entries added. Cage found a THIRD miss mode: an **agility-based
connect roll survives even perfect accuracy** (decomp: the 1d-Agility gate atop the hit branch)
— the lend kills scatter, not the connect; cage now RETRIES up to 4 throws (deployed).

### Round 4 — BUILT + DEPLOYED 2026-07-24 late night (user: "let's try em"; compiles clean, UNTESTED, next restart)
- **`tfshape`** (3-turn tail) — Stoneshaper's first harness coverage, four asserts: carve #1
  (statue stands, CurStatue tracked) · Young Ivory clean no-body fail (statue null, wall
  INTACT, cooldown not re-taken) · swap-down commit (carve #2, prior de-animated) · the
  **shape-cd retirement watch**: statue destroyed, 3 turns, cooldown may only TICK (a jump up
  = the retired reset logic resurfacing; a plunge = zero-out).
  **REQUIRED A MOD REFACTOR — the testability seam:** `Mutation_Stoneshaper.HandleEvent`'s
  interactive pickers (PickDestinationCell + goto re-prompts) cannot be wish-driven, so the
  carve core was EXTRACTED VERBATIM into `internal CarveFrom(actor, creature, wallCell)` —
  the handler now validates via UI then delegates. Behavior moved, not changed; still, the
  mutation's core was relocated, so **one human sanity-carve through the normal UI next
  session is owed** (pickers → statue → swap prompt all still route through the same core).
- **`tfpuddle`** (instant) — rock-onepuddle: a use-path break must leave exactly ONE puddle
  (death-spill suppressed against doubles); retries until a throw both connects and breaks,
  miss-breaks excluded, pre-cleans between attempts.
- **`tfdevour`** (instant, lithovore-only) — lith-devour: famished devour of an EaterStatue
  (base Statue family = the blueprint-descent case); hunger forced to famished for the bite
  and RESTORED after (the test should not feed you). SKIPs with a message on non-lithovores.
- `tfbatch` now: 8 synchronous + 6 chained (hopper → form → quickape → dull → drain → shape →
  scav), ~80 turns.

### Round 4 first run — 2026-07-24/25 boundary: 7 for 7 on the new scenarios
**shape.carve, shape.ivory (wall intact, cooldown untouched), shape.swap, shape.cd (1200→1171,
pure tick — the retirement watch is a measurement now), puddle (first throw), devour (EaterStatue
whole, hunger restored) — all PASS**; the SETUP SKIPs behaved (shape skipped a non-Stoneshaper,
devour a non-lithovore, both later re-run green on the right character). Sheet: shape-cd →
pass, shape-ivory strengthened, lith-devour + rock-onepuddle written. **LOTTERY NOTE:** this
boot's first fungal access rolled fresh AGAIN (3210, then 11 clean loads) — but the previous
session's save-provenance is unconfirmed, so the EXPLICIT experiment still decides:
note 3210 → menu Save and Quit → reload the same save → mint a fungal rock → compare.

### Round 5 — BUILT + DEPLOYED 2026-07-25 (user: "let's grab the next pass"; compiles clean, UNTESTED, next restart)
- **`tfeat`** (instant, lithovore only) — the eaten-channel sweep: 17 representative rocks
  eaten through the mutation's own `Cleo_TerraFirma_EatStone` command, 13 with real state
  asserts (water/bleed/harm/hop/teleport/thunderclap-neighbor/slime/webs/gas-belly/hot/cold)
  and 4 consume-only. Everything restored: hunger, cooking counter, HP penalty, position;
  effects removed; spawns swept. SKIPPED BY DESIGN (each a deliberate call, recorded): the
  EatenDetonate pair (a live grenade in your gut), the fungal twins (a real infection on the
  player), mullstone (needs a staged cooling ability), the deep four (bite-back + full meal,
  live-verified in the kitchen round).
- **`tffis`** (20-turn driver) — the first NPC-caster scenario: Hulking Baboon granted Fissure
  L5 via RequirePart<Mutations>, hostile-locked; PASS = the zone gained ≥3 loose fissure
  stones (a cast happened); living-fall allegiance reported (seam-dependent). **A zero-cast
  result is itself the fis-npc ANSWER (no AI advertisement on the mutation) — either outcome
  closes a question.**
- `tfbatch`: 9 synchronous + 7 chained (~100 turns).

### Round 5 first run — 2026-07-25 (user screenshots: eat 16/17, fis "FAIL" = the answer)
- [x] **tfeat 16/17 asserted** — every channel fired with correct magnitudes (the LITH lines
  read like a spec sheet: +5000 water, 1d2/1d8 internal, 1d3 Physical/Acid, hop to radius 2,
  1 neighbor dazed + eater spared, 3x Web, ConfusionGas belly, ±150 temperature, arc struck
  the neighbor at d=1). **The sludgestone "fail" was a harness artifact: liquid POOLS MERGE**
  — the slime vanished into melting's unswept acid puddle; pre-sweep fix DEPLOYED, row
  re-asserts next run. Ledger entry written.
- [x] **tffis: fis-npc ANSWERED — Fissure has NO AI advertisement.** Zero casts in 20 turns
  from a hostile-locked Fissure L5 creature (+1 stone = a thrown pocket rock). The sheet
  item's premise was false; sheet verdict set DEFERRED with the finding. **Wild Fissure
  casters need a Calcify-pattern AI handler — added to the creatures workstream's owed list
  (EB / Stoneshaper / Gorgon / Fissure).** **RULED PARK 2026-07-25 (user: "park with
  workstream"):** the handler builds with the other three in one sitting — the four share
  the Calcify pattern and the work batches. Known cost, accepted: fis-npc and the deferred
  dead-gorgon/stunned-caster sheet items stay open until that sitting; `tffis` stays the
  no-handler answer on every batch run until then.

### TIER 1 DECLARED COMPLETE — 2026-07-25 (user assessment: "is there anything left worth using
### the harness on?" → the two last cheap adds built, the rest is standing guard)
The closing adds, BUILT + DEPLOYED (next restart): **eat.mullstone** (staged refresh — exactly
one ability left cooling via the public Cooldown setter, eat, assert refreshed; every cooldown
snapshot-restored, net-neutral) and **drain.steplatch** (the last drain footnote: at-rest
drainstone latches IMMEDIATELY on its step proc, forced-crossing tech). Deliberately NOT built,
with reasons recorded: Gorgon DC statistics (build the day the formula changes), whorl-split /
eb-beam statistics (passed live; flaky AI arrangements for no new information), the remaining
eat skips (design calls). **The harness's standing duties from here: tfbatch after every build
batch (the ledger protocol) · the queued red-flips (quick.rep on the figurine fix, sludgestone
re-assert, tfcarom on the StepChance re-ruling, tffis when the AI handler lands) · pool-purity
+ eat rows for every future rock · the tffis pattern for every future NPC caster. The next
capability jump is TIER 2 (boot cycle) — start it when the lottery verdict or the vanilla
baseline actually blocks a decision, not before.**

