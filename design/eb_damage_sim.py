"""EB throw-damage rebalance sim (2026-08-06 beta pass).
Faithful reimpl of decompiled Stat.RollDamagePenetrations (qud-decomp/XRL/Rules/Stat.cs:160):
rounds of 3 dice, die = 1d10-2 exploding on natural 10 (accumulate 8, reroll), + min(Bonus,Max);
>= 1 die over AV -> 1 pen; all 3 over -> another round; Bonus -2 every round.
Effective thrown PV = min(StrMod, ThrownWeapon.Penetration=1) + PenetrationBonus  (ThrownWeapon.cs:122)
=> 1 + stone PenetrationBonus for any Str-positive thrower. Damage rolled once per pen.
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

def avg_damage(av, pv_bonus, n_dice, d_size, flat, rng):
    tot = 0
    for _ in range(TRIALS):
        pens = roll_pens(av, 1 + pv_bonus, rng)
        for _ in range(pens):
            tot += sum(rng.randint(1, d_size) for _ in range(n_dice)) + flat
    return tot / TRIALS

# ladders: name -> (dice(L), die size(L), flat(L), penBonus(L))
LADDERS = {
    "CUR  (1+L/3)d6+(L%3), pen L/2": lambda L: (1 + L // 3, 6, L % 3, L // 2),
    "A    (1+L/3)d4+(L%3), pen L/2": lambda L: (1 + L // 3, 4, L % 3, L // 2),
    "B    (1+L/4)d6+(L%4), pen L/2": lambda L: (1 + L // 4, 6, L % 4, L // 2),
    "C    (1+L/4)d6+(L%4), pen L/3": lambda L: (1 + L // 4, 6, L % 4, L // 3),
}
AVS = [3, 6, 10, 14]
LEVELS = [1, 4, 7, 10]

rng = random.Random(20260806)
for name, f in LADDERS.items():
    print(name)
    for L in LEVELS:
        nd, ds, fl, pb = f(L)
        row = "  L%-2d %dd%d+%d pv+%d | " % (L, nd, ds, fl, pb)
        row += "  ".join("AV%-2d %5.1f" % (av, avg_damage(av, pb, nd, ds, fl, rng)) for av in AVS)
        print(row)
    print()
