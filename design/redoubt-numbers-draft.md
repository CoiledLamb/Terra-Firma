# Redoubt-Fissure numbers draft (2026-08-02)

Status: **Claude draft under the ruled shape** (delegation RULED 2026-08-02: "this sounds
fine, i'll test numbers in game"). Every value is a dial for the in-game feel session.
Ruled shape it implements: wave radiates from caster · pushes everything away · stun ·
non-expiring rough ring at the wave's edge · size/strength/stun scale with level · carve
(natural stone only, level-scaled depth, carved mass feeds fabric, breaches are features) ·
never destroys items (v1 ignores loose items) · walls of constructed/metal stop the wave.

## Dial table

| Dial | L1–3 | L4–6 | L7–9 | L10 | Notes |
|---|---|---|---|---|---|
| Ring radius | 2 | 3 | 4 | 5 | rim at radius; wave sweeps outward to it |
| Wave damage | 1d3+1 | 1d3+2 | 1d3+3 | 1d3+5 | crush; control-first, ring is the payoff |
| Push | to just outside the rim | — | — | — | resisted push = 1-cell shove |
| Push resist | Strength save, DC 18+Level | — | — | — | size bonus applies — the thunderdome sort |
| Stun on wave contact | 1 rd | 1–2 | 2 | 3 | Toughness save DC 16+Level negates; only creatures the wave MOVES are stunned (kept your feet = kept your wits) |
| Carve depth (natural stone) | 1 cell | 2 | 3 | full radius | constructed/metal stops the wave at all levels |
| Gaps in rim | 3 fixed | 3 | 3 | 3 | fixed count + growing circumference = ruled proportional completeness |
| Crack-gate under gaps | 50% per gap | — | — | — | doors made of dice |
| Fabric mix (rubble/crumbled/wall) | 70/25/5 | 55/30/15 | 40/35/25 | 30/35/35 | corridor casts skew denser (carved mass joins the rim) |
| Keeper salvage per cast | 0–1 | 0–1 | 0–2 | 0–2 | rolled among rim cells ("additional small spawns," ruled) |
| Geyser in a gap | — | 10% (tier 1) | 15% (tier 2) | 20% (tier 3) | the door that shoots back |
| **Stone figure in rim** | — | 2% | 4% | 6% | **USER RULED IN for v1 (2026-08-02: "figurine can come in first pass").** Blocks LOS like a wall; inert; Stoneshaper-animatable. What the figures ARE = user's lore surface, mechanics only here. |
| Cooldown | 100 flat | — | — | — | see precedent section |

## Cooldown precedent (decompiled ground truth, 2026-08-02)

Base-game physicals: Flaming Ray 10 · Freezing Ray 20 · Slime Glands / Corrosive Gas /
Sleep Gas 40 · **Stunning Force 50 (closest verb-cousin: AoE radial knockback + stun)** ·
Spinnerets 80. Mental neighbors: **Force Wall 100 flat (duration 14+2L) · Force Bubble 100
flat (duration 10+3L)** · Teleportation 103−3L (floor 5) · Burgeoning 115−10L (floor 5) ·
Temporal Fugue 200. House range: Gorgon 80 · Calcify 90 · Fissure v2 105−8L (floor 30) ·
Stoneshaper 120.

Two base-game idioms: duration-products get FLAT cooldowns (level buys duration — the Force
family); instant-payoff casts get DECLINING cooldowns (level buys frequency — Burgeoning,
which Fissure v2 copied). The redoubt's product is PERMANENT: no duration for level to buy,
and level already buys size/strength/stun/carve — a declining cooldown would double-dip.

**Recommendation: 100 flat** — exact Force Wall/Bubble parity (the ruled analogue), inside
the house range, 2× Stunning Force for "Stunning Force plus a permanent fortress."
Feel-session lever: if high-level casters should remodel more freely, the Burgeoning idiom
(e.g. 120−4L) is the alternative; open at flat.

## Feel round 1 (2026-08-02, user, first live casts — all built + deployed same day)

1. **"The ring feels a bit too square"** → disk and rim are now EUCLIDEAN CIRCLES, not
   Chebyshev squares (integer tests: disk d² ≤ r²+r, ring r²−r < d² ≤ r²+r).
2. **"The walls feel too solid at lower levels - need more gaps"** → gap count is now a
   falling ladder, **6/5/4/3 by band** (was 3 fixed). Proportional completeness runs on both
   axes: circumference grows AND gaps close as you level.
3. **"We can comment out cracks for now"** → crack-gates under gaps BETA-HELD (code
   commented in the cast, `CRACK_GATE_CHANCE` const held with it; the crack floor object
   itself stays live elsewhere — tfcrack still covers it).

## Feel round 2 (2026-08-03, user, all built + deployed same day)

1. Frame-pump settle: choreography and world render now interleave (the rays' draw-and-
   sleep idiom) -- each rim piece materializes at its own breakthrough moment with its own
   smoke; gap sprites rise and SINK BACK. Fixes "rocks just appear / smoke all at the end".
2. Interior wake scatter: ~15% of interior cells get loose stone in the wave's wake
   (10% plain stone / 3% pebbles / 2% MediumBoulder, timed to shell passage). Floor-painting
   version noted as possible future dial, not built.
3. **Salvage + geyser + THE STONE FIGURE all BETA-HELD ("all salvage + baboon and geyser,
   for now" -- corrected same day: "that's what i meant by the baboon" = the figure).
   The ring is pure fabric for the beta; reconsider once the mutation is functioning.**
4. **Wall share 10 -> 75 by band with growing variance (user numbers: 10/30/55/75 base,
   +/-5/8/12/15 rolled per cast)**; remainder splits 55/45 rubble/crumbled. **The ring
   dresses as the zone's dominant painted wall** (modal PaintedWall value stamped on
   deposited walls, zone painter renders connectivity; emergence sprite resolves the same
   material's isolated tile; sandstone fallback in wall-less zones).

## Carve-as-attack (2026-08-04, RULED)

User's words: "rather than having it autonomously destroy walls, could we have it make an
attack against the walls armor? so the end result is similar but some walls are leftover."

The bite is now a penetration roll vs the wall's AV, resolved once per wall (the first ray
to reach it; later rays reuse the outcome). Grammar borrowed from Burrowing Claws (wall-pen
bonus + percent-of-max-HP damage), so granite's 26,000 HP means granite ENDURANCE, not
26,000 points of bookkeeping. Numbers are Claude drafts under the standing delegation:

| Dial | Value | Notes |
|---|---|---|
| Wave PV vs walls | 6 + 3×Level | Burrowing Claws' 3×level wall-pen precedent. L1 = 9, L4 = 18, L7 = 27, L10 = 36 |
| Damage per penetration | 34% of the wall's MAX hit points | 3+ pens fell any wall; 1–2 leave it standing damaged (the leftover) |
| Zero penetrations | no damage, wave breaks on the wall | "the stone shrugs" — a leftover at full health |
| Mass credit | only walls that FELL feed ring-fabric upgrades | survivors are stops, not fuel |

Anchors against base wall AVs: soft sedimentary stone (shale/sandstone, AV 6–10) answers
from L1 but occasionally shrugs a low-level wave; marble (AV 25) wants band 3; granite
(AV 40, 26k HP) shrugs at any level a mortal reaches — the deep hard stone is now a real
material boundary instead of a carve like any other. Carve depth still caps how many walls
one ray can chew THROUGH; a surviving wall stops the ray regardless of budget left.

## Deliberate v1 exclusions

- Item-plowing (wave ignores loose items — RULED, revisit post-beta if the wave feels like
  it passes through furniture).
- Recast-plows-old-ring concentric growth (jam riff, unruled — v1 recast simply raises new
  fabric where the new rim lands; old rubble stays put).
