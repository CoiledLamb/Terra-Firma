# Per-rock powders — the owed explanation (written 2026-07-24)

> **RULED same day — BLESSED as built:** "fine by me! i was looking forwards to the history
> spice for each rock feeling a bit unique." Rider recorded in TODO.md: per-rock uniqueness in
> spice entries is a wanted feature — keep shared keys to the two deliberate no-tell cases.
> This doc stays as the rationale record.

Context: at ruling round 7 (2026-07-22) you ruled the powder split with "i lean the latter,"
and your word was **per-domain**. What got built is **per-ROCK**: 36 `Cleo_TerraFirma_Ground_*`
blueprints, one per cookable rock. This is the explanation owed before you bless or veto that
interpretation. (TODO.md round 7 records the build; this doc is the *why*.)

## Why per-rock instead of per-domain

**1. Inspired dish names key off the blueprint, not the domain.** Carbide Chef's dish-namer
(decompiled) dereferences the spice dictionary by the ingredient's blueprint identity and samples
a fresh object of it for the display name. With per-domain powders, every inspired rock dish
would name itself off a generic shared blueprint — "stone meal soup" forever. That generic-naming
wart is the exact thing the powder split was created to fix; per-domain powders would rebuild it.
Per-rock gives "ground galvena" dishes with true names.

**2. Per-rock is a superset of per-domain.** Where shared flavor is right, the
`AggregatedIngredient` tag already merges blueprints into one spice key (base-game precedent: all
jerkies share "Jerky"). We use it: the fungal four share `FungalRock`, the glowing twins share
`GlowingRock`. So per-rock can still collapse to per-domain flavor wherever you want — but a
per-domain blueprint can never recover a rock's individual name. One direction is reversible,
the other isn't.

**3. Some powders carry per-rock behavior.** The fungal grounds carry the lottery part and
restamp their own cooking type per rolled infection; the glowing twins deliberately share a
DisplayName so the gamble survives milling; strange's ground keeps its union list whole. A single
shared per-domain blueprint has nowhere to put per-rock parts — these designs only work with
individual twins.

## The cost

36 XML objects and a standing maintenance rule (new cookable rock = new `Ground_` twin + spice
key). The mill finds twins automatically by the `_Rock_` → `_Ground_` name convention, so the
cost is purely authorship, not plumbing. Dazestone deliberately has no twin yet (cooking domain
OPEN — the Rocks.xml comment records it); it joins when its intervention pass resolves.

## The ruling asked

- **Bless per-rock as built** (recommendation — everything above), or
- **Revert to per-domain** and accept generic inspired-dish naming + losing the lottery/twin
  behaviors, or name a third shape.

One knock-on either way: the HistorySpice sitting writes one phrase entry per spice KEY (36 under
per-rock with the shared tags, fewer under per-domain) — see `design/historyspice-prep.md`.
