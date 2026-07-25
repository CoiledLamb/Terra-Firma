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

---

*Add new entries at the tail. If a scenario's assertion changes shape (harness edits), note the
version delta here — a PASS from a weaker assertion doesn't inherit the stronger claim.*
