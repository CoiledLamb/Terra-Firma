# Mutation guidelines

Directional. Meant to bend, argued from, not enforced. Provenance is a field on each entry.

## Every mutation level must increase something

- ruled: 2026-08-06
- user: "I need to make sure every level up increases something on the mutations, including on calcify"
- source: `JOURNAL.md:458`
- applies to: every leveled mod mutation. At the ruling, Calcify was the only one with dead levels; fixed with a stepping cooldown the same day.
- note: this is checkable by an audit over the level tables and could graduate to a constraint. Not yet.

## Any mutation an NPC carries, its AI must actually cast

- ruled: 2026-07-22
- user: "gotta be able to fight back"
- source: `JOURNAL.md:2500`
- applies to: stonecarver types and any wild creature carrying a mod mutation. Whichever mutations land on a roster commit their AI handler work; the handlers are shared infrastructure.

## Riding along, a fact rather than a guideline

1-cost and defect mutations do not level. Base engine behavior the mod matches, stated by the
user 2026-08-06 ("1 cost and defect mutations don't level"). Geomancy is exempt from the
every-level rule above by design; Lithovore is gone. Recorded in `design/ground-truth.md`.
