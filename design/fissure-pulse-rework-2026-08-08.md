# Fissure pulse rework draft (2026-08-08)

Status: **ruled shape, numbers pending.** The redoubt (rim at radius 4, fabric/gap/carve-
credit/cooldown machinery, recast-refresh) is untouched — this rework replaces only how the
wave itself resolves: one uniform disk hit becomes THREE PULSES in the same action. Numbers
stay under the standing delegation (RULED 2026-08-02: "this sounds fine, i'll test numbers
in game"); dice ladders below are open until a numbers sitting. Supersedes the wave-damage
row of redoubt-numbers-draft.md when built; everything else in that doc stands.

## The ruled shape (sitting of 2026-08-08)

- **Three pulses, one action** (user: "the idea would be it hits in 3 rings in the same
  turn - adjacent to the user, 1 further out, then one further out again").
- **Expanding-churn coverage** (Claude mechanism draft implementing the user's ruled
  outcome, ratified in the same sitting): pulse t hits EVERYTHING within ring t, then
  pushes hit creatures one ring outward. One rule, no special cases. Hit totals it
  produces, all ruled outcomes:
  - start at ring 1: 3 hits — whether churned outward pulse by pulse or standing firm
    (user: "hit 3 times when adjacent... to catch anything that lingers in them")
  - start at ring 2: 2 hits · start at ring 3: 1 hit
  - weight decides where you END, not how much you take: the brute is churned in place,
    the chaff is churned outward and ejected
  - **Vanilla precedent, found at numbers time (base-mutation-numbers-2026-08-06.md):
    STUNNING FORCE — Fissure's ruled cooldown cousin — already rolls its damage 3/2/1
    times at distance ≤1/2/3.** The 3/2/1 proximity gradient is the native grammar of
    the exact mutation this one is measured against; the pulse rework independently
    reconstructs it and adds the push-chaining mechanism underneath.
- **Origin tile in coverage all 3 pulses, caster exempt** (RULED 2026-08-08 "agreed"):
  the engulf catch (user: "reflavored to hit the user's tile first, though not damaging
  them in the blast. this is a catch for killing engulfing enemies with it"). An
  unpushable engulfer eats all 3 pulses at the DEPLETING dice — same total as any ring-1
  occupant; the dice belong to the pulse, never the position. (Epicenter equity question
  raised by user 2026-08-08 — "keep the same damage or diminish after each hit as though
  the target was pushed" — RULED 2026-08-09, port-pass sitting: DEPLETING SEQUENCE, the
  Claude recommendation taken; the dice belong to the pulse, never the position, matching
  the pulse-owned FX amplitude grammar.) Quills is the vanilla precedent for the
  self-burst-as-engulf-answer grammar.
- **Attenuating pulses** (RULED 2026-08-08 "agreed"): pulse 1 strongest, pulse 3 weakest —
  the wave spends itself as it travels. Adjacent = strong+medium+weak; a ring-3 bystander
  takes only the weakest tail (user's companion reasoning: "the final hit would be the
  lowest damage").
- **Push clamp: one ring per pulse** (RULED 2026-08-08: "i like the clamp more", over
  force-scaled fling). The weight-vs-force thunderdome sort still applies to WHETHER a
  creature moves; the clamp caps how far.
- **Damage budget** (direction RULED 2026-08-08: "we would just cut the +7 from the
  damage budget and then allow more variance and dice rolls with what's left"): flat
  bonuses out, the band's budget redistributed into three decaying dice pools. Ladder =
  numbers sitting.
- **Stun: one check per cast** (RULED 2026-08-08 "sounds good"), on the last pulse that
  moved the creature. Kept-your-feet grammar unchanged; no chain-stuns from three pulses.
- **Pinning** (RULED 2026-08-08: "slamming enemies into walls sounds good"): pushed into
  a standing wall = slam damage (existing wall-rider grammar), each remaining pulse the
  creature sits inside coverage. Pinned against stone is now the worst place to be.
- **Wall riders** (RULED 2026-08-08 "that's fine to me"): the save-or-slam check runs
  per pulse for climbers/burrowers on walls inside coverage, same beat as pinning.
- **Companions**: accepted — they ride the same churn (user: "i think this might be
  better for companions, honestly - the final hit would be the lowest damage, and as you
  progress through the game, you're normally outspeeding companions, so they'll be
  further away. worth seeing in testing with other players"). Explicit playtest item.

## Rim eviction (RULED 2026-08-08, feel-pass confirm reserved)

The problem it solves: with the clamp, a creature churned from ring 1 lands on exactly
ring 4 — the rim line — so a good cast used to seed its own ring with debris downgrades
(StillHoldsCombatant). Ruled in two beats (same sitting: "pushes them *against* the wall
for damage if it would spawn behind them, if not flings them away", then the landing-spot
resolution: "they could be deposited inside the ring with you (hence the slam) or have
the rubble/wall appear under them and bounce/fling them away? that'd be my resolution to
the tension of the flag"). The fabric ALWAYS rises — the ring completes, no debris
downgrade from cascade-ejected creatures. The occupant bounces off the emergence, and
which way is geometry (the case split is Claude's formalization of the ruling):

- **Outward open** → the rubble/wall erupts under them and BOUNCES/FLINGS them away —
  ejected clear of the ring (no damage, or a token — dial).
- **Outward blocked by a standing wall** → slammed against it (pin grammar, the slam
  dice) and DEPOSITED INSIDE THE RING with the caster — the wall rejects them back into
  the thunderdome. The almost-escaped are thrown back in.
- Micro-fallback (Claude draft, rare): both directions blocked (corridor rim cell
  walled on both faces) → that one spot keeps the debris downgrade —
  crushed-but-not-buried, the loot rule's cousin.

## Carve: per-pulse bites REPLACE the depth ladder (RULED 2026-08-08)

User: "probably replacing, it's more natural - could hit like pickaxes and dig through
them with repeated damage."

Every wall inside a pulse's coverage takes one bite (the carve-as-attack penetration roll,
2026-08-04 grammar unchanged: PV vs AV, percent-of-max per pen, hard-class clamped to
1 HP). A ring-1 wall takes up to 3 bites; a ring-3 wall takes 1. Carve depth stops being
a separate band ladder — flesh and rock share the attenuation geometry. GetCarveDepth and
the per-ray bite budget (the anti-corridor bookkeeping) retire; the natural bound is the
radius itself. Mass credit unchanged: only walls that FELL feed fabric upgrades. Own-
fabric-yields (recast-refresh, 2026-08-06) unchanged — the maker's pieces still kneel
without a roll.

Open carve items for the numbers sitting:
- **Low-band governor**: 3 bites at ring 1 out-carves the old depth-1 budget at L1. The
  PV ladder (6+3L) is now the ONLY carve scaler — anchors need a recheck (does L1 vs
  sandstone AV 6-10 with three tries fell too reliably? does attenuation also apply to
  bite PV or damage, pulse 3 biting softer?).
- **Ring-4 walls**: pulses cover rings 1-3; the rim rises at 4. Does the dissipating
  front bite ring-4 walls once, or not at all? (The wave must still REACH ring-4 cells
  for rim placement — blocked rays still shadow the rim as today.)
- **Corridor recheck**: the old budget stopped unspent bites chaining through breaches
  (the full-row corridor bug). Coverage-based biting is geometry-bounded instead —
  verify the corridor cast produces a sane result before retiring the bookkeeping.

## Numbers draft (2026-08-08 sitting; user shape, Claude draft under the standing delegation)

User's proposed shape, verbatim: "damage die depleting (4d3 on top, 1d3 at furthest band) /
flat 4 pv - maybe making the shape closer to burrowing claws?"

**Decomp anchor (base-mutation-numbers-2026-08-06.md): Stunning Force's damage is
"1d3 (<3) / 1d3+(L-1)/2, rolled 3/2/1 times at distance <=1/2/3."** The verb-cousin already
runs proximity-multiplied dice. Calibration ceiling it sets: SF L10 point-blank averages ~18
unmitigated on a 50 cooldown with no fortress; Fissure's draft below stays under that on a
100 cooldown WITH the redoubt.

### The mitigation fork (RULED 2026-08-08: "i lean option b")

"Flat 4 pv" invites the attack grammar, and Burrowing Claws is the instructive precedent
because BC SPLITS its penetration: claws hit flesh as a natural weapon (blueprint weapon,
base PV 4 family), while wall-digging uses a dedicated wall-pen ladder ("1d6+3L", bonus 3L).
"Closer to Burrowing Claws" therefore reads as: **pulse hits on creatures are attacks at
flat PV 4 (AV mitigates, pens multiply the damage roll), while wall bites keep their own
scaling wall-pen** — flat 4 must NOT replace the wall PV, or carving dies (PV 4 vs sandstone
AV 6–10 mostly shrugs; marble/granite become permanently immune at every level).

- (a) Stunning Force parity: depleting dice, unmitigated, no PV anywhere on the flesh
  side. Exact vanilla grammar of the cousin; never whiffs. NOT taken.
- **(b) BC split — RULED**: creature pulses are PV-4 attacks — AV joins weight in the
  thunderdome sort (the armored shrug the churn, the unarmored chaff is shredded),
  variance widens (pens × dice, "more variance and dice rolls" ✓), and the whole
  mutation speaks ONE grammar: the wave makes attacks, flesh or stone. Accepted cost:
  0-pen pulses exist — vs a high-AV brute the cast is control + fortress, not damage,
  which the original draft already embraced ("control-first, ring is the payoff").
  Push/stun are untouched by pens.

PV evolution within the sitting: flat 4 (user's opener) → 4+(L-1)/2 lase-style bonus
(Claude, answering the late-game armor curve) → REJECTED (user: overpen slippery slope)
→ **4 + Strength mod, RULED** — the armor curve is answered by build investment instead
of a level ladder. Details in the PV row below.

### Pulse dice ladder (draft v3; per-PULSE dice)

Ladder history: v1 pinned the tail at 1d3 all levels ("furthest ring should do 1d3");
SUPERSEDED same sitting ("it doesn't need to be pinned at all levels"). v2 grew back-to-
front but was flagged low once mitigation was weighed ("i feel like stunning force dmg
might be a bit low for this - especially if it's rolling against armor"). v3 below: dice
are THE level-scaling lever (PV rides Strength, not level — see PV row), totals lifted
per the user ("i think they can be a bit higher than in your ladder as well"), die size
stepping d3→d4 at band 3 so the late wave hits HARDER, not just more. Shape invariant
unchanged: the wave deepens from the front back, p1 ≥ p2 ≥ p3.

| Band | Pulse 1 | Pulse 2 | Pulse 3 | Ring-1 total (3 hits) | Ring-2 total | Ring-3 total |
|---|---|---|---|---|---|---|
| L1–3 | 2d3 | 1d3 | 1d3 | 4d3 (avg 8) | 2d3 (4) | 1d3 (2) |
| L4–6 | 3d3 | 2d3 | 1d3 | 6d3 (12) | 3d3 (6) | 1d3 (2) |
| L7–9 | 3d4 | 2d4 | 1d4 | 6d4 (15) | 3d4 (7.5) | 1d4 (2.5) |
| L10+ | 4d4 | 2d4 | 2d4 | 8d4 (20) | 4d4 (10) | 2d4 (5) |

Raw point-blank 8/12/15/20 (v2 was 8/10/12/16; old flat ladder 3/5/7/9 uniform). These
are PER-PEN rolls: real output = dice × penetrations, so a Str-invested caster (PV 7-10
by mid-game) shreds soft targets well past these averages while an Ego-lean caster sits
near 1 pen and casts for control + fortress — build-coupled damage is the point of the
Str ruling, not a side effect. Stunning Force (unmitigated 6/12/18, cd 50, ranged) stays
a reference point, not a ceiling: self-centered melee-range churn earns its premium.
Tail stays 1d3 through band 2, grows with the die-size step after — companion graze
story holds where companions actually keep pace. Every cell is a feel dial; the
harness pen-distribution pass (build-time item) puts measured expected-damage numbers
under all of this before the feel session.

### The rest of the table

| Dial | Draft | Notes |
|---|---|---|
| Creature-hit PV | 4 + Strength mod (RULED 2026-08-08) | the melee-weapon grammar itself — BC's claws take Str mod as PV like any weapon. Level-scaled pen BONUS rejected same sitting (user: "penetration bonus can be a slippery slope due to how armor overpens work") — bonus-pen ladders are hidden damage multipliers that peak against the SQUISHIEST targets; Str-scaling bounds pens by build investment, the price every other physical attacker pays. Cap (weapon-style MaxStrengthBonus) reserved as a feel dial if overpen shows vs chaff |
| Wall-bite PV | keep 6+3L | BC's split; the low-band governor question stands (3 bites at ring 1 vs old depth-1 budget — feel-test whether 6+3L needs softening now that repetition digs) |
| Stun | 1/2/3/4 rounds by band, Toughness DC 16+L | carried unchanged; one check per cast (ruled). SF anchor: flat 3 rounds, DC 15+L |
| Pin/rider slam | the current pulse's die (fork-matching) | per-pulse slams as ruled |
| Rim eviction | bounced-inside = slam die; flung-away = 0 | eviction ruled; token fling damage stays a dial |

## Downstream (not blocking design)

- **Level text**: new stat lines for the pulse mechanic — user's voice, draft-until-signoff.
- **FX**: StoneRiseFX sells one traveling front; three pulses want three readable beats.
  How a player reads "three hits, one action" is the design-adjacent half; tempo dials
  are port-pass work. Animation concept RULED 2026-08-09 (port-pass sitting): CONCEPT B,
  confirmed over A — amplitude belongs to the PULSE (100%/50%/25% by beat), the tall
  motion stays interior rather than riding the front outward; interior cells re-bob each
  beat at the current pulse's smaller amplitude (sampled, density a dial); churn BOBS
  (up-crest-settle) while rim fabric RISES AND STAYS; three camera shakes, descending;
  the rim punch-through at radius 4 stays the signature closing beat.
- **AI**: the cast is now much stronger point-blank; the AI's worth-casting check
  (currently distance ≤ radius) may want to prefer adjacency.
- **Companion feel**: parked for playtests with other players (ruled above).
