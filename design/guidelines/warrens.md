# Quarry warren guidelines

Directional. Meant to bend, argued from, not enforced. Provenance is a field on each entry.
Every v2 warren decision argues from the goal statement; the bars below are how it gets judged.

## Goal statement

- ruled: 2026-08-17
- user: "the quarry warrens are a sort of halfway point between procgen villages and goatfolk villages. they are part settlement, part camp, part work area. they sometimes might contain a recipe you haven't had before, that you can only eat in this one camp. or a legendary stonecarver and their entourage, that you can encourage to join you. or a few fragments of history or gossip from faraway lands and those close to their home. sometimes, they've cultivated a provisioner in the walls - a jilted lover and consortium representative that will trade with you and restock periodically. they don't assign quests, nor are they immediately hostile to the player. learning the location of a quarry warren should be an 'oh neat!' moment earlygame, while goatfolk villages turn to groans extremely quickly."
- source: `JOURNAL.md:633`
- shape: a contrast bar, so the named failure mode is part of the spec. Goatfolk villages groan because every instance is the same instance. "Halfway between the two" needs an identity layer above the carve.
- bend, closed 2026-10-02: the "don't assign quests" line gives way to overseer-given quests that lead to other warrens, once those are built (D10 in `TODO.md`).

### Stage axis, same sitting

- user: "the more large/dense a warren is, the most it expands outwards into settlement territory. the roots of it being a work area are always visible, and as they progress it becomes more of a camp. and then finally it fleshes out to have more decorative areas devoted to making people happy rather than producing more stone - the sign of a successful dig"
- source: `JOURNAL.md:641`
- one axis governs size, register, payload count and dress. After the young-dig cut below it runs working, camp, flourishing.

## Judging bar

- ruled: 2026-08-18, replacing v1's criterion
- user: "i totally agree with that metric, actually. it's something i've internally been trying to do on the last one"
- source: `JOURNAL.md:705`
- the bar: roll two sites. Can the user tell them apart and say what each one is? If two rolls both read as just a warren, the identity layer failed. Stated before the first roll on purpose, because v1's rubric was never written down and every judgement drifted toward it.
- note: v1's bar ("does the language hold over thirty rolls") was a sameness test, the goatfolk groan mechanism. This one is its deliberate inverse.

## Worth-the-trip bar

- ruled: 2026-08-18
- user: "if we're going to have these be rumored information (like lairs or farms in the canyons) they are better off making sure each has something to do in them, if that makes sense. and a young dig is the least likely to be worth the player getting notified of in that way, which makes me wonder if we should cut it and just do working/camp/flourishing?"
- source: `JOURNAL.md:769`
- shape: warrens are rumoured locations, the way lairs and farms are. A rumour is a promise, so a site the player is sent to has to be worth the walk. The young dig is cut and not replaced: a band too thin for the smallest real stage yields no warren at all.
- refinement accepted in direction (Claude): the bar is about built size, not occupancy. An abandoned camp is worth walking through; an abandoned young dig is a hole. Enforced after the carve as a floor on built extent, not on available rock.

## Art bar

- ruled: 2026-07-2x, the quarriers scope exchange (exact day on the source line's parent entry)
- user: "i'm fine reusing tiles and items for the quarriers. some things will need their own sprites, but i'm gradually ticking away at those"
- source: `JOURNAL.md:520`
- shape: reuse of base tiles and items is the default; bespoke sprites land as the user produces them. Debt is tracked in `design/asset-debt-tracker.md`.
