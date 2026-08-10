# Next-session prompt: Fissure pulse VFX feel pass (written 2026-08-10)

Point a fresh agent here for the Fissure pulse ANIMATION feel session (repo:
`C:\Users\venob\terra firma revival\CoQ_TerraFirma-dev`). Construction is DONE — the
concept B ripple choreography was built, compiled, and deployed at the 2026-08-09 port
pass, but it has NEVER BEEN SEEN. This session is eyes-on dialing: the user casts, gives
notes, you turn dials in `TerraFirma/Scripts/Fx_StoneRise.cs`. Do not rebuild the
choreography and do not re-litigate ruled shape.

## Read order

1. TODO.md — working norms + canon (the pulse-pass entry is in the friends-beta section).
2. `design/fissure-pulse-rework-2026-08-08.md` — the spec, including the v5 staircase,
   the L20 amendments, and the concept B ruling (Downstream section, RULED 2026-08-09).
3. `TerraFirma/Scripts/Fx_StoneRise.cs` — the built choreography (Play() + the dial block).
4. `TerraFirma/Scripts/Mutation_Fissure.cs` — the cast that drives it (deposits, shakes
   fired from the settle pump, wake-scatter timing keyed to BEAT_SEC/BOB_RISE).
5. `design/redoubt-numbers-draft.md` — fabric/plate history (feel rounds 1-5); the
   fractured-plate grammar carried into the beats unchanged.

## Non-negotiable working norms (unchanged)

- Provenance discipline: RULED / LEAN / CLAUDE PROPOSAL; cite rules via their markers.
- Stonecarver = Mutated Human calling, NOT True Kin.
- No Co-Authored-By trailer on commits. Commit early and often, branch `master`.
- Text is the user's domain; TF-VERIFY stays ON through the beta.
- Compile: `dotnet build TerraFirma.csproj -p:QudLibPath="C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed"`
  (no trailing backslash; MSB3277 benign). Deploy = copy changed files to
  `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma\`,
  hash-verify, game restart to activate. Player.log watch per the verify-watch workflow.

## What is RULED about the animation (concept B, 2026-08-09 — do not move)

- Three ripple beats of DECAYING amplitude; the ratio 100%/50%/25% is the ruled grammar
  ("amplitude belongs to the PULSE").
- The tall motion stays INTERIOR (concept B over A, confirmed): beat 1's full-height
  churn plays at ring 1; every later, wider beat plays lower.
- Churn BOBS (up-crest-settle round trip); rim fabric RISES AND STAYS.
- Interior rings re-bob each beat at the CURRENT beat's amplitude, SAMPLED, as fractured
  plates ("not all ~50 cells — density is a dial").
- Three camera shakes, descending, one per beat.
- The rim punch-through at radius 4 stays the signature closing beat — nothing crowds it.

## The dials (all numeric values are FEEL DIALS; top of Fx_StoneRise.cs)

| Dial | Value | What it does |
|---|---|---|
| BEAT_SEC | 0.25 | beat spacing; rim starts at 3×BEAT_SEC (0.75s) |
| BOB_RISE / BOB_HOLD / BOB_SINK | 0.09 / 0.03 / 0.10 | one churn bob (0.22s total) |
| PULSE1_EMERGE | 14 px | beat 1's full churn height (old travel was 9 — the strong beat is deliberately taller) |
| BEAT_AMP | 1 / 0.5 / 0.25 | the RULED ratio — dial only with a new ruling |
| REBOB_PCT | 50 | % of interior plates that re-bob on beats 2-3 |
| SHAKE_MAG_1 / STEP | 0.3 / 0.1 | shakes 0.3 → 0.2 → 0.1 |
| RIM_STAGGER/RISE/HOLD/SINK, RIM_EMERGE | 0.18/0.14/0.10/0.06, 20 | punch-through, UNCHANGED from the redoubt feel rounds |
| TRAVEL_PLATE_MIN/MAX, SHARD | 2/4, 12% | beat-plate fracture (re-dialed twice in feel round 5 — history in redoubt-numbers-draft) |
| RIM_PLATE_MIN/MAX, SHARD | 3/6, 8% | rim slabs stay big |
| PLATE_JITTER_STEPS | 5 | 0-0.05s per-plate offset inside a beat |

Wake scatter timing lives in Mutation_Fissure (deposits keyed to
`(ringIdx-1)*BEAT_SEC + BOB_RISE`); the three shakes fire from the settle pump off
`schedule.ShakeTimes`.

## Known watch-items for the first eyes-on casts (CLAUDE flags, unverified visually)

1. **Beat-3 legibility**: 25% of 14px = 3.5px — the third beat may be invisible in
   motion. If so, the dial is PULSE1_EMERGE (raise the base) before touching the ruled
   ratio.
2. **Beat overlap edge**: worst-case jitter (0.05) + bob (0.22) = 0.27 > BEAT_SEC 0.25 —
   a plate can still be settling when its next-beat sprite starts rising, ~0.02s of
   doubled sprite near the buried position. Predicted invisible; if it shimmers, raise
   BEAT_SEC to 0.30.
3. **Re-bob density**: REBOB_PCT 50 is a guess — too busy reads as noise, too sparse
   breaks the "whole interior churns" story.
4. **Shake feel**: three descending shakes in 0.5s may judder; magnitudes and whether
   beat 3 deserves one at all are open.
5. **Rim timing**: the last bob ends ~0.72s, rim starts 0.75s — confirm the closing beat
   still reads as ITS OWN event after three interior beats.
6. The unseen-cast and HarnessMute branches skip all of this — only live visible casts
   exercise the choreography.

## Context you should NOT touch (state as of 2026-08-10)

- **Mechanics numbers are UNDER USER TEST** (v5 staircase, per-level dice/stun/DC; carve
  clamp removed; sealed ring at L19). The user is running their own casts against tfpen
  tables. If a feel note is about DAMAGE or NUMBERS, log it and defer — this sitting is
  motion only.
- Harness state: `tfbatch` last ran 22 pass / 2 fail; the Directed-kill fix for fis/scav
  (commit d4a8d6d, decomp Kill.cs:506 root cause) is deployed but AWAITS a confirming
  boot. redoubt.carve needs open ground east of the player to run. `tfpen` has not yet
  been run — nudge for it once.
- Owed text signoffs (user's domain, drafts marked in code): EB's two pen-cap lines
  (level text + Abilities.xml tooltip).
- Other feel flags parked with their owners: EB dice behind the Str cap, the vibro-less
  Stoneshaper statue, machinery casts under the removed clamp.

Default first move: ask the user to cast Fissure a few times somewhere visible and give
their first motion notes; dial from there, one note at a time, deploy + verify each round.
