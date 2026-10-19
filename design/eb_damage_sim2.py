"""EB damage re-tune sim (2026-08-15 pass -- "tune earthen barrage damage lower").

Extends eb_damage_sim.py with (a) EARLY-WEIGHTED ladders and (b) the electrofuge
one-shot comparable, so the two halves of the friend's note can be read together:
"less damage to start" without making the lategame falloff worse.

Pen engine is the same faithful reimpl of decompiled Stat.RollDamagePenetrations:
rounds of 3 dice, die = 1d10-2 exploding on natural 10 (accumulate 8, reroll),
+ min(Bonus,Max); >= 1 die over AV -> 1 pen; all 3 over -> another round; Bonus -2
each round. Effective thrown PV = min(StrMod, ThrownWeapon.Penetration=1) + bonus
= 1 + PenetrationBonus for any Str-positive thrower. MECHANICAL pen, no visual +4.
Damage is rolled once per penetration.

Electrofuge (Base/ObjectBlueprints/Creatures.xml): AV 2, Level 5, Hitpoints 18,
Inherits BaseSpider -> Toughness sValue "10,1d3,(t-1)d1".

HP BAND = 18-22, BACK-SOLVED FROM THE 2026-08-06 RUN, NOT RE-DERIVED. That pass
recorded P(one-shot | hit) at L4 as 8-21% post-cut; this engine reproduces exactly
that at HP 18 (21.3%) and HP 22 (8.3%), i.e. blueprint HP through blueprint + 1 per
level above 1. Using the same band keeps today's candidates comparable to the ruling
they are being weighed against. CAVEAT, UNRESOLVED: read straight off the blueprints,
BaseSpider Toughness is 10+1d3 = 11-13, whose modifier is NEGATIVE (-2/-3), which
under TouMod x (Lv-1) would put effective HP at 6-10 and roughly triple every
one-shot number below. Settling that needs Leveler.cs; no decomp on disk this
session. It shifts all rows together, so it does not reorder the candidates.
"""
import random

TRIALS = 200_000

def pen_die(rng):
    total = 0
    r = rng.randint(1, 10) - 2
    while r == 8:
        total += 8
        r = rng.randint(1, 10) - 2
    return total + r

def roll_pens(av, bonus, rng):
    pens = 0
    while True:
        over = sum(1 for _ in range(3) if pen_die(rng) + bonus > av)
        if over >= 1:
            pens += 1
        bonus -= 2
        if over < 3:
            return pens

def roll_hit(av, pv_bonus, nd, ds, fl, rng):
    """Total damage from one landed throw (pens already multiplied in)."""
    tot = 0
    for _ in range(roll_pens(av, 1 + pv_bonus, rng)):
        tot += sum(rng.randint(1, ds) for _ in range(nd)) + fl
    return tot

def avg_damage(av, pv_bonus, nd, ds, fl, rng):
    return sum(roll_hit(av, pv_bonus, nd, ds, fl, rng) for _ in range(TRIALS)) / TRIALS

def p_oneshot(hp, av, pv_bonus, nd, ds, fl, rng):
    hits = sum(1 for _ in range(TRIALS) if roll_hit(av, pv_bonus, nd, ds, fl, rng) >= hp)
    return 100.0 * hits / TRIALS

# ---- ladders: L -> (dice, die size, flat, pen bonus) --------------------------
# Every candidate must tick SOMETHING at all ten levels (the every-level lattice
# requirement carried over from the 2026-08-06 ruling).
def cur(L):   return (1 + L // 3, 6, L % 3, L // 3)          # SHIPPED, post-pen-cut
def pre(L):   return (1 + L // 3, 6, L % 3, L // 2)          # pre-08-06, for scale
def d4(L):    return (1 + L // 3, 4, L % 3, L // 3)          # the REJECTED dice cut
def shift(L): return (1 + (L - 1) // 3, 6, (L - 1) % 3, L // 3)   # E: whole ladder one notch left

_F_DICE = [1, 1, 1, 1, 2, 2, 3, 3, 4, 4]
_F_FLAT = [0, 1, 2, 3, 0, 1, 0, 1, 0, 1]
def slow(L):
    i = min(L, 10) - 1
    return (_F_DICE[i], 6, _F_FLAT[i], L // 3)               # F: slow start, L10 UNCHANGED

_G_DICE = [1, 1, 1, 1, 1, 2, 2, 3, 3, 4]
_G_FLAT = [0, 1, 2, 3, 4, 0, 1, 0, 1, 0]
def slower(L):
    i = min(L, 10) - 1
    return (_G_DICE[i], 6, _G_FLAT[i], L // 3)               # G: slower start, L10 -1

LADDERS = [
    ("PRE   pre-08-06 (pen L/2)", pre),
    ("CUR   shipped   (pen L/3)", cur),
    ("D4    rejected dice cut  ", d4),
    ("E     ladder shifted -1L ", shift),
    ("F     slow start, L10 same", slow),
    ("G     slower start, L10 -1", slower),
]

AVS = [0, 3, 6, 10]
LEVELS = [1, 4, 7, 10]
LASE_L10_AV3 = 28   # Light Manipulation parity line, carried from the 2026-08-06 sim

rng = random.Random(20260815)

print("=" * 78)
print("AVERAGE DAMAGE PER LANDED THROW   (Light Manipulation lase at L10 = %d vs AV 3)" % LASE_L10_AV3)
print("=" * 78)
for name, f in LADDERS:
    print(name)
    for L in LEVELS:
        nd, ds, fl, pb = f(L)
        row = "   L%-2d %dd%d+%-2d pv+%d |" % (L, nd, ds, fl, pb)
        row += "".join("  AV%-2d %6.1f" % (av, avg_damage(av, pb, nd, ds, fl, rng)) for av in AVS)
        print(row)
    print()

print("=" * 78)
print("P(ONE-SHOT AN ELECTROFUGE | THE THROW LANDS)   AV 2, HP band 18-22 (see docstring)")
print("=" * 78)
for name, f in LADDERS:
    cells = []
    for L in (4, 5, 7):
        nd, ds, fl, pb = f(L)
        hi = p_oneshot(18, 2, pb, nd, ds, fl, rng)
        lo = p_oneshot(22, 2, pb, nd, ds, fl, rng)
        cells.append("L%-2d %2.0f-%2.0f%%" % (L, lo, hi))
    print("%-28s %s" % (name, "   ".join(cells)))
