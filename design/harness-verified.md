# Harness-verified ledger

Every fact established by the automated verification harness (`_TestHarness.cs`), one entry per
verified claim. **Purpose (user, 2026-07-24): "keep a tab on anything solved that way so if
things go funny in the future, we know where to look."** If a behavior on this list goes funny,
the first move is to RE-RUN the listed wish and compare against the recorded result — a changed
verdict localizes the regression to whatever shipped in between; an unchanged PASS points the
hunt elsewhere.

House rules for entries: record what was PROVEN (the exact assertion), what was explicitly NOT
covered (the limits are load-bearing — a re-run can't clear a claim the scenario never made),
and the re-run command. Direct engine-state assertions only; log-inference results don't get
ledger entries (log inference is not proof).

---

## 2026-07-24 — first live run (game 2.0.211.50, character Karuukat's save)

### `tfstreak` PASS — streakschorl bag re-equip after flight (player path)
- **Proven:** with the last-held streakschorl thrown via `PerformThrow` (the real player
  pipeline; hijack path bypasses the engine's own wrapper, so the mod's replicated flow in
  `Part_StreakFlight` is the only re-equip in play), the thrown slot was re-armed with a
  DIFFERENT streakschorl object drawn from the bag: slot = Hypersonic x1 after flight, bag 2→1.
  Setup ruled out stack-remainder mimicry (one equipped BEFORE spares granted).
- **Not covered:** whorl-vs-flight interaction, both render modes, save/load of a mid-state
  stone, NPC-thrower path (separately confirmed live 2026-07-23 by log), EB auto-sculpt owning
  the slot (Check() veto branch untested — no handler refused in-arena).
- **Re-run:** `tfstreak` (instant; self-cleaning as of harness v2).

### `tfhopper` PASS — millstone hopper grind mechanics + power gate
- **Proven:** `Part_StoneHopper` grinds one hopper stone per EndTurn while `Mill.IsReady` is
  true and does nothing while it's false: force-powered mill (ChargeUse=0) turned 2 salt rocks
  into 5 powder servings in 5 turns (2–3/rock rate honored, products accumulated in the mill);
  force-dead mill (unpayable ChargeUse) still held 2 stones, 0 powders. Tick-1 readiness line
  confirmed the forcing itself (live=True, dead=False).
- **Not covered:** Joppa's actual river/wheel power delivery (the world-wiring eyeball is still
  owed), boulder rate (5–6), non-salt payload rocks in the hopper, hopper + pestle interaction.
- **Re-run:** `tfhopper` (5 turns standing still).

### `tfscav` — SCAV free re-ready + fetch loop (verdict from the run's logs; the v1 counter's
### 8 "starved" turns were the harness's own sampling phantom, fixed in v2)
- **Proven:** across 118 baboon-turns, every `slot->EMPTY` transition was answered by
  "pocket re-ready ... (free action)" within the same turn, with ZERO `REFUSED` lines — H1
  (re-ready failure) refuted in this arena. The dry-fetch loop ran end-to-end live: pockets-empty
  baboons detoured to loose stones (d=5–8), stooped, re-armed. Cadence measured: 34 throws /
  118 baboon-turns = 0.29 throws/baboon-turn (4 baboons, 30 player turns, mixed Small Stone +
  granted rocks — baboons carry native stones on top of harness grants).
- **Not covered:** the ≥vanilla cadence comparison (no baseline exists under the mod's global
  merges — needs a mod-disabled control boot, Tier 2), scoot-loop behavior under real pursuit
  (the player stood still), H2/H3 (stone-merge AI scoring / scavenger-part side effects) are
  UNTESTED — only H1 is refuted.
- **Re-run:** `tfscav` (30 turns; run on a character that can absorb the pelting; v2 counter
  counts starvation only when emptiness survives a full turn).

## 2026-07-24 night — round 2 first run

### `tfstrange` PASS ×2 — strange-pool purity (the audit invariant as a standing assertion)
- **Proven:** 100 rolls total across two runs (50 + 50; 32 and 28 distinct blueprints), zero
  null rolls, zero bad: every rolled blueprint is creature-free and either grenade-carrying or
  payload-mimicking, never strange itself, never a payloadless fizzle — the 2026-07-24 audit
  ruling ("everything it does should actually happen, no null") holds at the pool level.
  **This is the re-run to reach for whenever a new rock joins the catalog or the pool table.**
- **Not covered:** live fire-through (place-then-fire at a real victim — the "visibly does
  something" eyeball), pool WEIGHTS (deep-four-at-half-weight is not asserted, only membership),
  Lithovore strange-eat path.
- **Re-run:** `tfstrange` (instant, zero zone side effects).

### Round-1 re-confirmations (same run)
`tfstreak` and `tfhopper` re-PASSed under harness v2 in-batch — the round-1 ledger entries stand,
now with the v2 slot-normalization setup exercised on a fresh save.

## 2026-07-24 night — round 2 second run ("all green from tests!")

### `tfform` PASS ×2 — statuette expiry mechanics (batch item 5's assertable half)
- **Proven:** a statuette whose TimedForm clock runs out is GONE within the window and replaced
  by exactly one `Cleo_TerraFirma_BaboonFigurineItem`, with its pocket stones spilled loose
  beside it (5 and 10 counted across the two runs — count varies with the statuette's rolled
  inventory). Asserted by zone-count delta + creature invalidation, two independent runs.
- **Not covered:** the death-POPUP suppression half — and note the structural limit: the
  harness statuette is wish-spawned, NOT a player follower, so the companion-death popup (and
  its suppression) is never exercised by this scenario at all. That half belongs to an organic
  follower-statuette expiry, user eyeball, as sheeted. Also not covered: ape-statue → rubble
  (same part, different blueprint wiring).
- **Re-run:** `tfform` (5 turns).

### `tfscav` v2 PASS — starvation counter, fixed definition
- **Proven:** with starvation counted only when emptiness survives a full turn, 87 baboon-turns
  produced **0 starved turns** — the round-1 "every EMPTY heals same-turn" log reading is now a
  counted assertion, not an interpretation. Cadence 0.31 throws/baboon-turn (0.29 in round 1 —
  stable across runs; the metric is trustworthy as a future regression baseline).
- **Still not covered:** the ≥vanilla comparison (unchanged — needs a mod-disabled boot).
- **Re-run:** `tfscav` (30 turns).

### Infrastructure: `tfbatch` chain proven
Full five-scenario chain ran hands-free (streak → strange → hopper → form → scav), zero mod
errors. `tfbatch` is now the standing one-wish regression sweep for everything in this ledger.
(tfstrange third PASS: 30 distinct blueprints; streak + hopper re-confirmed again in-chain.)

## 2026-07-24 night — round 3 first run

### `tfcarom` PASS — caromcobble trip statistics + unbreakability
- **Proven:** 40 forced crossings → 13 trips (inside the 3–22 band for StepChance 25) and the
  peel survived every proc (HoldChance 100 — it is genuinely the permanent banana peel).
- **Not covered:** organic pathing (creatures may route around; crossings were forced), throw
  behavior (separate ricochet channel). **Re-run after the pending StepChance re-ruling** with
  the band adjusted to the new number.
- **Re-run:** `tfcarom` (instant).

### `tfcrack` PASS — FallThrough pull-down
- **Proven:** a creature crossing the cracked floor fell to the zone below on crossing 9
  (StepChance 15); the victim genuinely changed zones (JoppaWorld.11.22.1.2.**11**).
- **Not covered:** fall damage on landing, the chasm variant (CUT from tier 9, blueprint dormant).
- **Re-run:** `tfcrack` (instant).

### `tfspill` PASS — liquid-rock destruction spill
- **Proven:** a brine geode destroyed by damage (not obliteration) spilled 14 drams — inside
  the 4–32 band of the 4d8 pool-volume audit.
- **Not covered:** the "puddle not pool" RENDER (eyeball), the no-spill paths (obliteration,
  decay, use-path suppression), other liquid rocks.
- **Re-run:** `tfspill` (instant).

### `tfdull` PASS — the anti-hoarding clock, first end-to-end verification ever
- **Proven:** a carried rock with its dull clock wound to 2 crumbled out of the bag on schedule,
  leaving pebbles (+1), payload lost with the dull.
- **Not covered:** the 1000–2500 staggered-roll distribution (only the collapsed clock), floor
  rocks never aging (unasserted), save/load of a mid-clock rock (Tier 2).
- **Re-run:** `tfdull` (5 turns).

### `tfquick` shelf — pockets PASS · heal PASS · ape PASS (rep = verified bug, below; cage pending)
- **Proven:** Hulking Baboon spawns carrying mod rocks (3), Quartz Baboon carries none (merge
  inheritance boundary honored) · healstone step-proc healed a wounded creature (35→38) and
  `ConsumedOnStep` ate the stone · ape statue expired to devourable rubble (+1) on schedule.
- **Not covered:** heal's throw path and self-throw UX (popup), exact pocket dice (2–3 spec —
  only ≥1 asserted).
- **Re-run:** `tfquick`.

### `quick.rep` — VERIFIED BUG (a fact, recorded as one): figurine AddsRep is inert
- **Proven (by assertion, delta 0):** taking the figurine changes Baboons rep not at all —
  `AddsRep` without `WorksOnCarrier` is equipper-only, and the figurine is never equipped. The
  rep half of the figurine has never functioned. Fix queued (one attribute); this standing red
  flips green as the fix's verification.

### `tfdrain` — oneshot PASS; flesh lifecycle proven; robot half OPEN
- **Proven:** the one-shot path (1-HP victim dies to the throw → corpse can't latch → stone
  rests as loot) is now a direct assertion, not a log reading. Flesh lifecycle (standalone run):
  latch + LifeDrain asserted at setup; drain measured (97→56), LifeDrain expired, gorged burst
  on schedule — composite verdict FAILed only on the robot half.
- **OPEN, not leddered:** the robot latch never attaches (victimless dispatch, both runs) — under
  investigation with upgraded diagnostics; the dry-crumble ending remains unverified. The batch
  run's silent flesh dispatch likewise.
- **Re-run:** `tfdrain` (25 turns).
- **SUPERSEDED same night by the lend-build run — see the full-lifecycle entry below.**

## 2026-07-24 late night — the lend-build run (drainstone lifecycle complete)

### `tfdrain` — the WHOLE drainstone lifecycle, all four paths verified
- **Proven (direct assertions + the named-victim dispatch lines):**
  · flesh latch: stone riding the victim, LifeDrain applied (drain.latch PASS, 2nd confirmation);
  · **robot latch: "LATCHED onto Scrapbot … STARVED (inorganic, no leech)" — the ruled
    attach-but-no-leech EXECUTED for the first time ever**; stone attached, LifeDrain correctly
    withheld (drain.robot PASS);
  · one-shot: corpse can't latch, stone loose as loot (3rd confirmation, throwat HP 1→−1);
  · **both endings fired on schedule: gorged burst (flesh: drained 98→90, LifeDrain expired,
    stone gone) and dry crumble (robot: never drained, stone gone).** The composite FAILed only
    on an over-strict harness assert — the Scrapbot REGENERATED the throw's 2 scratch damage
    (98→100), which proves no-drain; assert fixed to >= baseline.
- **The robot-latch "bug" is CLOSED: it was throw misses all along** (the miss saga). Worklist
  item "Drainstone vs inorganics" `[verify]` line is fully closed by this run.
- **Not covered:** the step-proc latch path (at-rest immediate channel via a real step), latch
  on the player, save/load of a latched stone (Tier 2).
- **Re-run:** `tfdrain` (25 turns; composite goes green from the next run on).

### Engine fact worth its own line (third distinct miss mode)
Even with perfect accuracy (the lend property), the engine keeps an **agility-based connect
roll** on thrown attacks — the cage whiffed once with zero scatter and no interposer. Harness
throws that need a guaranteed connect must RETRY (cage now throws up to 4). This also refines
the never-miss ledger note: the mod's NeverMiss + lend kills scatter and fall-short, not the
connect roll.

*(Also this run: form, quick.ape, hopper, streak, strange re-greens; scav's third stable cadence
point, 0.28/turn.)*

## 2026-07-24/25 boundary — round 4 first run

### `tfshape` — four-for-four on Stoneshaper's first harness coverage
- **Proven (via the CarveFrom seam — behavior moved verbatim from the UI handler):**
  · carve #1: statue stands, tracked as CurStatue (Baboon muse from a spawned Sandstone wall);
  · Young Ivory: refused with no body plan, **wall INTACT, cooldown 1200→1200 untouched** — the
    clean-fail contract asserted on the checklist's own poster child;
  · swap-down: carve #2 committed, prior statue de-animated (gear entombed), CurStatue swapped;
  · **the shape-cd retirement watch: cooldown 1200→1171 segments across 3 turns after the
    statue's destruction — pure ticking, no reset, no zero-out.** The flagged review-watch is
    now a measurement instead of vigilance. Bonus: muse-weapon lines armed sensibly across body
    plans (Baboon vicious bite, Scrapbot scrap saws) and the killed-statue bookkeeping fired.
- **Not covered:** the interactive UI shell (pickers → swap prompt) — ONE HUMAN CARVE still
  owed since the seam extraction relocated the core; natural expiry (already confirmed live).
- **Re-run:** `tfshape` (3-turn tail; needs a Stoneshaper character).

### `tfpuddle` PASS — the one-puddle suppression
- **Proven:** a use-path break left exactly ONE puddle at the impact — hit-payload splash kept,
  death-spill suppressed against doubles. Scored on the first throw.
- **Not covered:** other liquid rocks' use-paths (same channel), the obliteration/decay
  no-spill paths.
- **Re-run:** `tfpuddle` (instant).

### `tfdevour` PASS — universal statue devourability, descent case
- **Proven:** a famished lithovore devoured an EaterStatue whole (base Statue family, NO Statue
  part — the exact blueprint-descent detection the audit built); hunger forced for the bite and
  restored after. Note: the InventoryActionEvent Check RETURNS false while the devour succeeds
  (the handler consumes the event) — the assert reads engine state, not the return.
- **Not covered:** living-statue devour (Stoneshaper statue as the meal), boulder-from-pack path.
- **Re-run:** `tfdevour` (instant; lithovore only).

## 2026-07-25 — the full-batch sweep (14 scenarios, live arena)

### `quick.cage` PASS — aegischist cage closed
- **Proven:** 8/8 forcefield segments around the intended victim, first throw, retries build.
  Sheet item rock-aegis CLOSED.

### `streak` — the EB auto-sculpt veto branch, observed live (assert taught to accept it)
- **Proven (by the run that "failed"):** on an Earthen Barrage character with Take Stones on,
  the replacement flow's Check() was VETOED by EB's auto-sculpt — a sculpted stone armed the
  slot (charges 3→2), bag untouched. That is the Check-first contract's OTHER branch, the one
  the round-1 ledger entry listed as not covered. The assert now passes EITHER path and labels
  which ran.
- **Re-run:** `tfstreak` (label says which branch you're on).

### Arena lessons codified (harness v6)
- **drain.endings robot HP is now report-only** — live-arena bystanders mauled the Scrapbot
  97→20 during one wait (LifeDrain still never applied; dry crumble still on schedule). The
  ending's assertable claim is the crumble, and only that is asserted.
- **scav baboons are hostile-locked at spawn** (`Brain.Target = player`) — one live run
  produced 0 throws in 118 turns because engagement never started; target acquisition is
  arena-dependent unless forced.
- **OPEN WATCH (real mod observation, 2 windows running): the dry-fetch loop can WEDGE** — a
  pockets-empty baboon oscillated "detours to loose stone at d=1" indefinitely; stoop fires
  only at d=0, so a stone in an unreachable/occupied cell loops the detour forever. Recorded
  as a SCAV polish flag in TODO.

---

*Add new entries at the tail. If a scenario's assertion changes shape (harness edits), note the
version delta here — a PASS from a weaker assertion doesn't inherit the stronger claim.*
