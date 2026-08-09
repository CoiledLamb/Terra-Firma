# Base-game mutation numbers (decompiled 2026-08-06)

Ground truth from ilspycmd 8.2 over Assembly-CSharp.dll, extracted for the beta rebalance pass.
L = mutation level. Integer division unless noted. Compiled from six parallel verbatim-extraction
reads; decomp sources live in the session scratchpad (qud-decomp/XRL/World/Parts/Mutation/).

## Direct comparables for Terra Firma

**Stone Gaze (lithofex)** — delayed-line gaze. Range 7+L, cooldown 50 flat, 3-turn telegraphed
warmup (1000 energy/turn, targets pushed a FleeLocation goal). One save: Toughness DC 20 FLAT
(attacker stat Ego), fail = instant death + statue (`Random Stone Statue`, Shrine part, 50%
pray/desecrate). No per-level scaling except range. Refractable projectile, blackout stops beam.

**Sunder Mind** — the Gorgon's Gaze cooldown/range model. Cooldown 80 flat, range 80 (sight),
10-round channel; any non-pass action breaks it, mental/psionic damage taken breaks it. Damage
dice ladder: L odd <3 "1d3", L even <3 "1d4", L>=3 odd "1d3+L/2", even "1d4+(L-1)/2"; attack is
vs MA with bonus max(EgoMod, L-2), rolled per penetration, accrues; final round bursts the total
again as unavoidable. Nosebleed chip: 1/turn to BOTH parties in the last 5 rounds.

**Force Wall** — Calcify's ruled par. Mental, cooldown 100, 9 cells (PickField(9)), duration
14+2L displayed / 15+2L actual (Temporary +1). Forcefield HP/AV live in blueprint XML, not code.
Reality-distortion gated.

**Force Bubble** — cooldown 100, duration 10+3L (9+3L+1), 8 fields ringing the caster, move with
owner, push force 5000+500L (distance 4). ExistenceSupport validated every turn (the fugue-tether
precedent).

**Stunning Force** — Fissure's cooldown precedent (Fissure = 2x at 100). Cooldown 50 flat, range 8,
area 7x7 (radius 3). Damage 1d3 (<3) / 1d3+(L-1)/2, rolled 3/2/1 times at distance <=1/2/3,
Concussion. Push force L*1000 distance 4. Stun: Toughness save DC 15+L, fail = Stun 3 rounds.
50% chance the blast center displaces one random adjacent cell.

**Light Manipulation** — Earthen Barrage's ruled regen model. Charges = max light radius =
4 + L/2; lase spends 1, regen 1 per 15 turns Willpower-scaled (-5%/pt over 16, clamp [3,75]) —
EB copies this exactly (15 base). Damage ladder ~1d3→1d5+(L-6); pen bonus 4+(L-1)/2. Reflect
chance 10+3L% (RefractLight, angle+180).

**Burrowing Claws** — Fissure's carve-PV precedent. Wall pen "1d6+3L" (bonus 3L), wall damage
25% of max HP per dig hit (50% gigantic). Claw damage from blueprint VariantDamage ladder
(default 1d2). AV formula exists (1/2/3 at L<5/<9/9+) but is dead code.

**Temporal Fugue** — cooldown 200 (Quantum Fugue 100), duration 20+2*(L/2) displayed (+1 actual),
copies (L-1)/2+1. Copies: XP 0, Temporary, FugueCopy string prop (Calcify/Fissure tether key),
HostileCopyChance default 0.

**Quills** — reserve-economy comparable. 300+ quill pool (grows ~100/level), fling 10% of pool,
1d3/quill per pen, pen min(6,(L-1)/2), regen L/4-ish per round Willpower-scaled (same -5%/pt
formula family). Passive AV 2 (L<=2) else L/3+2, halved as penalty when pool below half.
Melee reflect: ~1% of pool breaks, damage*3% per quill back at attacker.

**Stinger** — save-DC scaling model: poison save Toughness DC 14+2L. Damage ladder 1d6→2d8 by
bands of 3. Pen 3 (L<2) else min(9,(L-2)/3+4). Cooldown 25 flat. 20% proc as offhand, 100% on
charge/lunge.

**Beguiling/Domination attack math** — 1d8 + Level + max(EgoMod, mutation L) vs defender
Level (+MA for Domination Sifrah). Beguiling cooldown 50, bonus HP to target 5*L. Domination
duration 100*(L+1), cooldown 75, adjacent only.

## Actives, by shape

- **Breathers (all)**: cooldown 15 flat, cone 4+L long, 20+2L degrees. Fire/Ice/Freeze damage
  Ld4+faceCount; temp delta +310+25L (fire) / -120-7L (ice) / -20-60L (freeze breath, range 30,
  cd 30). Passive resist 2L where present.
- **Gas generation (all)**: toggle, cooldown 40 flat, density 800 split over adjacent cells,
  duration base 1+L/2; corrosive/poison/sleep L+2; confusion/normality/glitter (L+2)*3/2;
  defoliant/fungicide (L+2)*2.
- **Flaming/Freezing Ray**: range 9, cooldown 10/20, damage Ld4/Ld3 (+hand count), on-hit rider
  2Ld8 heat / -Ld4 cold.
- **Pyrokinesis**: 3x3 at range 8, 3 rounds of Ld3/Ld4/Ld6 halved, cooldown 50. Cryokinesis:
  same but Ld2/Ld3/Ld4 halved.
- **Confusion**: cone 4+L/3 long, 29+L wide, cooldown 75. 1d8+max(EgoMod,L) mental attack;
  confusion level & mental penalty min(10, (L-1)/2+3); duration (L/2+10)*0.8..1.2.
- **Disintegration**: 7x7 self-centered, Ld10+2L (structural Ld100+20), cooldown 75, then
  Exhausted 3.
- **Sunder Mind / Stone Gaze / Stunning Force / Force Wall / Force Bubble / Light Manip**: above.
- **EMP mutation**: radius 2/5/9 (L<5/<9/else), cooldown 200, actual duration 6+L..15+L
  (text claims 4+2L..13+2L — known text mismatch).
- **Teleportation**: cooldown max(103-3L,5), scatter radius max(13-L,2). Teleport Other:
  cooldown max(115-10L,5), adjacent, no save. Phasing: duration L+7 actual, cooldown 103-3L.
- **Burgeoning**: cooldown max(115-10L,5), 3x3 at range 8, plant tier ceil(L/2)+rand(-1,2)
  clamp 1-9, 20% adjacent spillover.
- **Belcher**: 1d2+L/4 creatures, cooldown 210-10L, range 3+L radius 4; also bursts on death.
- **Time Dilation**: radius 9, cooldown 150, 15 rounds; speed penalty = baseSpeed *
  (9-dist)^2 * (0.0005L+0.0085).
- **Will Force**: +max(2*EgoMod,1) to STR/AGI/TOU for 16+2L..20+2L rounds, cooldown 200 shared.
- **Life Drain**: melee-range, 20 rounds, L damage/round if d8+max(EgoMod,L) > MA+4, heals
  drainer, cooldown 200.
- **Waveform Worm**: dash 5+L/2, Ld4 crushing everything traversed, cooldown 20.
- **Sticky Tongue**: pull range 10+2L, cooldown 22-L, 1-turn windup.
- **Adrenal Control**: +9+L quickness 20 rounds, cooldown 200; +L/3+1 to all other physical
  mutation ranks (property "AdrenalLevelModifier").
- **Electrical Generation**: charge L*100/turn (Willpower-scaled +-5%/pt, clamp base/5..base*5),
  max 2000+2000L, discharge (charge/1000)d4 at voltage charge/1000.
- **Magnetic Pulse**: radius L+1, cooldown 100, 1-round warmup.
- **Spinnerets**: web save 13+3L (phase web 18+2L), duration 5+L moves, cooldown 80; +5+L on
  Move saves.
- **Repelling Force**: Concussion push only (L*1000, distance 4), 7x7, cooldown 30.

## Passive stat mutations

- Heightened STR/AGI/TOU(TwoHearted)/INT/WIL/EGO: all 2+(L-1)/2 to the stat. STR adds
  13+2L% daze-on-damage (2-3 rounds); AGI adds 7+3L% cooldown-cancel on Agility abilities;
  INT costs -1/-2/-3 Ego (L<5/<9/else) and ~(2+L)*IntMod/10000 auto-explore proc; TwoHearted
  adds 20+10L% sprint duration.
- Heightened Speed: +13+2L Quickness. Multiple Legs: +20L move speed, +L+5% carry.
  Multiple Arms: 7+3L% offhand chance.
- Carapace: AV 3+L/2, DV -2, resists 5+5L heat/cold; Tighten doubles AV, -2 more DV.
- Quills/Horns: above; Horns AV 1+(L-1)/3, damage 2d(3+L/3), to-hit L/2+1, 20% gore proc,
  bleed save 20+2L.
- Regeneration: heal bonus +10+10L%, debuff purge 1+L/3%/round, limb regrow 10L%/round,
  decap-immune at L10.
- Two-Headed: mental action cost -15-5L%, 50% shake-off flat.
- Photosynthetic Skin: in sun +13+2L quickness, +20+10L% regen, camo DV min((L-1)/4+1, 6).
- Wings: fall 6-L%, travel +50+50L%, jump +1+L/3, charge +2+L/3.
- Chromatophores: Scintillating 5 rounds, confusion rank L, cooldown 200.
- Absorptions (heat/cold): immune to type, heal 10%*(L-1) of absorbed.
- Clairvoyance: radius 3+L (whole zone at L15), duration 19+L, cooldown 100.
- Precognition: duration 4L+12, cooldown 500.
- Ego Projection etc. not present as classes; several stubs (Empathy, Gills, Heightened Touch,
  Energy Metamorphosis) carry no code.

## Defects with numbers

Narcolepsy 0.2%/round sleep 20-29 (combat-gated for player); Spontaneous Combustion 0.2%/round
+401..700 temp; Electromagnetic Impulse 0.2%/round self-EMP radius 3, 11-20 rounds; Blinking Tic
0.1%/round random teleport; Skittish 0.3%/round loses turn to AI; Evil Twin 5%/zone; Amnesia
5% secret loss + 5%/zone map wipe; Brittle Bones x1.5 crushing/cudgel/concussion; Albino regen/5
in daylight; Cold-Blooded ~-10 speed at 25C scaling with temperature; Quantum Jitters up to 25%
vortex-on-cooldown; Unwelcome Germination 0.2%/round arms a 30-39-round germination window at
25%/round; Weak Heart -5 poison/disease saves + ~1/10000 cardiac arrest; Hemophilia bandage/3 +
bleeding saves at half natural roll; Dystechnia -20 inspect, complexity*3000 crit-fail explosion;
Pack Rat must stay >=90% carry weight or -1 HP/round; Tonic Allergy 7-entry overdose table;
Myopia vision radius 10; Socially Repugnant -50 all visible rep.

## Known base-game text-vs-code mismatches (for calibration — base ships these)

- Acid Slime Glands: text says cooldown 10, code applies 40. Liquid Spitter: text 10, code 40.
- EMP: duration text 4+2L..13+2L, code 6+L..15+L; CollectStats area RxR vs true (2R+1)^2.
- Magnetic Pulse: text radius L, code L+1.
- Phasing: text duration 6+L, code 6+L+1.
- Time Dilation: text 15 rounds, code sets 16.
- Cold Absorption: text "0.{L-1}%" heal, code 10%*(L-1) (100x).
- Multi Horns: text damage table off by one at L3; advertises a gore/bleed proc that has no code.
- Heightened Ego/Willpower: advertised specials tagged [unimplemented], only the stat is real.
- Defensive Chromatophores: CollectStats says cooldown 5, code 200.
- Metamorphosis: the max-level gate (5*(L+1)) is displayed but never enforced.
- Force Wall/Temporal Fugue/Force Bubble: durations run +1 over displayed.
