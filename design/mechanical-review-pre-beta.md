# Pre-beta mechanical review: Fissure + Lithovore

**2026-07-27 · Claude analysis, nothing here is a ruling.** Directive: "going over them
mechanically and making sure everything makes sense before I hand it to another player. for
lithovore this means all the stones make sense to eat and cook. for fissure this means the
throw, cast, damage line up, it's useable as a mutation at early levels, and doesn't
completely break into the midgame." Ground truth: `Rocks.xml`, `Populations.xml`,
`Mutation_Fissure.cs`, `Mutation_Lithovore.cs` as deployed 2026-07-27.

---

## 1. Fissure: the curve

| Lv | Radius | Falls | Cooldown | Tiers (surface, eff=Lv) | Tiers (Z50+, eff=Lv+8) |
|----|--------|-------|----------|--------------------------|-------------------------|
| 1  | 1      | 6     | 97       | 1–3                      | 4–7                     |
| 4  | 2      | 10    | 73       | 1–4                      | 5–8                     |
| 7  | 3      | 16    | 49       | 3–6                      | 7–9                     |
| 10 | 4      | 24    | 30       | 4–7                      | 8–9                     |

Tier roll: `ceil(eff/2) + rand(-1..+2)`, clamped 1–9. Crush: rock's die + Level + 1d6 per
step inside the rim. Party gate protects the caster's side from step triggers; enemies
trigger spenders at 100%.

**Early game (Lv1–3) verdict: usable, genuinely good.** One cast at Lv1 drops 6 rocks in
the 3×3 with ~75% adjacent odds; an adjacent snapjaw eats 2d4–2d6 +1 +1d6 (avg ~10) —
kills or cripples most starter enemies outright, and the floor is now ammo plus mines.
Tier 1–3 stock is coherent starter kit: a weapon rock (shivshale 1d4/p5 + bleed), heals,
water, terror, oil. Cooldown 97 makes it a once-per-encounter nuke plus ammo economy,
which matches the Burgeoning-parity cost ruling. No early bald spot found in the math.

**Midgame (Lv5–8, depth in play) verdict: scales, doesn't break.** Cooldown eases 65→41,
falls 10→16, and depth feeds tier quality instead of quantity (falls/radius/cooldown are
depth-flat by ruling, only spill and tier rise). The power spikes that ARE there — wall
falls at 15% crush-instakill (tiers 7–9 generics), quarrelquartz, the deep four — arrive
where the tier ladder says they should. Free-ally falls are capped (living cap 3–5/cast).
The discharge law caps hoard power: spenders die on use, so the midgame doesn't snowball
a grenade warehouse. Verdict: intact.

### Fissure flags, ranked (rulings yours)

**F1 · Stale level text lies about the dull clock — [text] [cheap].**
`GetLevelText` still ends the ammo paragraph with "Pocketed too long, a fissure rock
crumbles to plain stone." The dull clock was CUT (2026-07-25 fragility sitting, DullAfter
0 mod-wide). A beta player reads this, plans around decay that doesn't exist. One-line
deletion (or rewrite to the discharge law's one-breath rule). Check `Abilities.xml` for a
sibling line while in there.

**F2 · The strange pool is depth-flat and now NPC-deliverable — [design].**
`StrangeRocksPool` is one flat table (~37 slots, everything weight 10, deep four at 5).
Strange rock seasons generics from tier 2 up, so a Lv2 character's strange throw is ~2.7%
a Space-Time Vortex, ~2.7% a 50-turn charm, ~1.3% each deep-four payload (BreakGear 100
included). That out-of-depth jackpot may be exactly the lottery's charm (very Qud), but it
was never explicitly ruled as such, and two things sharpened it since: the held
strange-table cleanup mandate ("some of the things it has don't really make sense"), and
the malformed cragmensch now firing strange rocks AT players. Options if it grates:
tier-banded strange pools (pool follows the tier the strange fell from), or weight decay
on the top rungs. If the flat lottery is the point, rule it KEPT and the flag closes.

**F3 · Shattered stalactite outguns rocks four tiers up — [design] [numbers].**
Tier-3 headliner, 1d6/p6 thrown plus ShatteredArmor ×100 turns on EVERY throw-hit
(StepChance 0, pure weapon). Compare: tier-7 headliners sit at 1d3/p4; vitriolite's etch
rider was deliberately ruled to 50% chance while the stalactite crack is 100%; nothing
else beats p6 until the deep four (2d4/p6, tier 9). Its identity is "the simple spike you
throw hard," and tier 3 needs a weapon rock — but as written it stays the best pure
projectile in the catalog until tier 9, and the best gear-cracker, period. Dials if
wanted: pen 6→5, or crack chance 100→50 (vitriolite parity), or both. If "the humble
stalactite is secretly the best spear forever" is the joke, rule it kept.

**F4 · Cracked floor at tier 2 vs the chasm cut — [design] [flag-lite].**
Crack (15% fall-through per crossing, permanent zoner) is a tier-2 HEADLINER, so
level-2 casts mint floor holes while the chasm was cut as "too spicy to lock in now."
The difference is real (crack is a gamble per crossing, chasm was a guaranteed pit), and
crack's rate is legible zoner behavior under the discharge law. Probably fine — but if
any beta player rages about a surprise basement trip at level 2, this is where it came
from. Rule keep / move up a tier / dial the 15.

**F5 · Wall instakill, note only — no action proposed.** 8d8 + 15% instakill rides tiers
7–9 generics. It kills things standing next to a deep-strata caster, which is the
fantasy working as designed; the player can also stand a wall up as terrain. Documented
so nobody rediscovers it as a surprise.

---

## 2. Lithovore: the eat / cook audit

Method: every catalog rock checked for (a) an eaten channel that matches its thrown/step
identity, (b) a cooking story consistent with the mill law, (c) deliberate-and-recorded
gaps vs accidental ones.

**The table is in strong shape.** 30+ rocks eat as themselves: soupstone hydrates, salt
is always tasty, shivshale cuts on the way down, caromcobble hops you, farflint
teleports, mullstone refreshes, the deep four bite back, glowing twins gamble, strange
mimics a doer, armed stones detonate in the gut per the mill law's mouth station.
Grind-side, the mill law's severity split is implemented as ruled (HE forfeits powder,
gas/thermal/EMP detonate AND yield). The audit found no wrong-feeling effect on any
covered rock. The flags are all gaps, not wrongness:

**L1 · Healstone has NO eaten effect — [design] [the one that will get noticed].**
The healing rock heals on throw and on step, but eaten it is a flavorless half-snack: no
`EatenHeal`, no `EatenEffect` marker (correctly absent from the strange-eat pool, which
proves the gap is load-bearing). Every lithovore beta player will eat a healstone first —
it is the single most intuitive eat in the catalog — and get nothing. Cheapest fix shape
if wanted: `EatenHeal 2d4` (the rock's own payload die, snack-grade) + the EatenEffect
marker; C# channel likely already exists (Heal payload machinery). Or rule it deliberate
("healing is the thrown verb") and the flag closes — but then the description text should
carry the hint, because silence here reads as a bug, not a choice.

**L2 · Oilstone and tanglelith eat as plain rations — [design] [flag-lite].**
Both lack eaten channels. Defensible as plain food (not every rock needs a trick, and
"you ate the tar rock, it was filling" has its own comedy), but unlike streakschorl
(plain BY RULING, power-budget provenance recorded) these two are plain by omission.
One-word rulings close them: "plain" or an effect each (oil slick underfoot? stuck-self
comedy? the gamble-food investigation may want them as both-ways candidates).

**L3 · Ground_Gyrestone is a dead blueprint under the mill law — [cleanup] [cheap].**
The mill law forfeits spacetime powder (gyrestone GrindSpawns at the millstone, no
yield), but `Cleo_TerraFirma_Ground_Gyrestone` still exists with the Spacetime domain
mapping. Nothing can mint it now. Harmless, but it is exactly the kind of ghost the
strange-audit's purity standard hunts: strike it (and check whether the Spacetime
IngredientMapping has any other consumer) or leave a dated dormant comment.

**L4 · Rocks with no ground twin, all accounted for — note only.** Shivshale and
shattered stalactite mill to plain flour (no bespoke domain — acceptable, the knife rock
has no kitchen story to tell); oxidian and lodestone are RULED open slots; quarrelquartz
forfeits by law. No accidental gaps found.

**L5 · Economy check — no flag.** Lv1 Fissure feeds ~3 snacks per 97-round cast
(6 falls × half-snack); hunger stays trivially solved for a Fissure lithovore, which is
the ruled intent ("food literally rains from the ceiling" priced into the −2). The
defect's real cost lives in the cooking restriction and it survives: plant/meat bans
bite exactly as hard regardless of rock abundance. Devour's full-clear is famished-gated.
Nothing degenerate found.

---

## 3. What this review did NOT cover

Live feel (the 100%-step-trigger ratify is still owed a play session, per the discharge
ruling); NPC-caster balance (parked with the creatures workstream); text voice (separate
sweep); the held strange-table CONTENT cleanup (F2 is about its power shape, not its
roster).
