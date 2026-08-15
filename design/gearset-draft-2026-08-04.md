# The earth-manipulation gearset — consolidated draft list, 2026-08-04

**Status: working draft. Consolidates every item pitched for the gearset across the
2026-08-02/03 jam (`design/jam-2026-08-02-rock-items.md`), TODO's jam-commitments entry, the
2026-08-04 skills-audit session, and the user's lore notes (morestonecarver.txt). Provenance
tagged per item; all tiers, numbers, and elaborations below a concept line are CLAUDE
PROPOSAL, UNRULED unless marked. Post-beta by standing decision — nothing here is beta scope.**

**Session rulings folded in (USER, 2026-08-04):** the skills audit produced no adopted
changes ("nothing from here jumps out") — no calling-grant edit, no bespoke skill tree; the
audit doc stands as reference. Carving is ITEM-GATED: "the idea of carving a message into
rocks with the mallet and chisel is awesome but i wouldn't lock it behind a skill, just
having the items would be enough."

---

## The slot map (the shape of the set at a glance)

One accidental but pleasing property of everything pitched so far: **no two items compete for
an equipment slot.** The set tiles the body.

| Slot | Item | Concept provenance | Tier lean |
|---|---|---|---|
| Hands | Catch gloves | USER, 2026-08-02 ("definitely leaning gloves") — **LANDED 2026-08-13 as Cobblefur Gloves (cobble baboon butchery); see TODO roster entry** | LOW (USER LEAN) |
| Feet | Wall-kick boots | USER RULED gear-not-rider, 2026-08-02 | mid (Claude, unruled) |
| Body | Socket armor (the feedable breastplate) | USER, 2026-08-04 shape | scales (unruled) |
| Arms | Baboon-skin throw bracers | USER, 2026-08-04 — **LANDED 2026-08-13 as Cobblefur Bracer (cobble baboon butchery, buff shape RULED close-range accuracy); see TODO roster entry** | low (Claude, unruled) |
| Weapon | Pulling weapon | USER, 2026-08-02 | mid-high (Claude, unruled) |
| Missile weapon | Gravel shotgun | USER, 2026-08-02 | gated on mill's return |
| Tools (carried) | Chisel + mallet carving verb | USER, 2026-08-04 (item-gated ruling) | tier 0, already ships |
| Tools (carried) | Moss boxes | USER lore notes (morestonecarver.txt) | low (Claude, unruled) |

Connective tissue (Claude observation, unruled, from the jam): rubble/rocks as first-class
objects are the set's currency — Fissure and the catalog produce, the puller gathers, the
armor and shotgun consume, the gloves and EB recycle, the chisel inscribes.

---

## Item entries

### 1. Catch gloves
**USER LEAN 2026-08-02: gloves; tier LOW.** Rocks-only catch scope; reactive trigger
(incoming thrown rock → save → into EB reserve/inventory) over a stance.
Claude elaboration, unruled: catching is scaling-neutral (value = what enemies throw, which
deepens with depth), so low tier is safe and maximizes time with the verb. Wild-baboon
scavengers throwing your own ring salvage back makes the gloves a comedy engine at zero
extra cost.
**Open fork:** for non-EB wearers, where does a caught rock go — inventory only, or
auto-ready into the thrown slot (StoneScavenger's re-ready plumbing already exists)?

### 2. Wall-kick boots
**USER RULED 2026-08-02: gear, not a mutation rider. The Calcify combo is the point** (raise
wall, kick wall — EB's earliest design ancestor returning as gear). Ruins metal refuses the
kick (connection doctrine, same law the redoubt wave now enforces via the `Natural` flag —
the boots can ride the exact same gate, already built).
Claude riffs, unruled: "kick any adjacent wall" makes corridors ammo; kicked wall terminates
as rubble (feeds the currency loop); feet slot.
**Open fork:** kick scope — any natural wall / only walls you or your mutations raised.
The `Natural`-flag gate makes the first option nearly free to implement and doctrinally
consistent.

### 3. Socket armor — the feedable breastplate
**USER 2026-08-04 (shape, supersedes-or-merges the jam's slotted-rock sketch):** chest armor
"with a hollow maw in the center that can fit a boulder, but that rock will steadily lose
health as you're attacked, until you need to find a new one." Lives in the GEARSET (settled
2026-08-04 — not skills, not a mutation).
Standing constraints that bind this item: **no new riders casually** (USER 2026-08-04,
rider-density concern) — which cuts against the jam's maximalist per-rock-rider fork and
favors the simple shape: AV while fed, boulder HP as the ablative pool, visible wear stages
(intact → cracked → crumbles).
Unruled attachments, listed not endorsed:
- Omnivorous maw (pebbles as top-off fuel for the socketed boulder) — sketched as the
  volume-sink alternative; the pickup-cost objection from the pebble discussion applies to
  it too.
- Chisel-shaping a rock to fit (slower cracking) — originally the jam's "calling as best
  wearer" hook; with carving now ruled item-gated, this would be a chisel verb, no skill
  needed. Still unruled as a feature.
**Open fork:** what feeds the maw — boulders only (clean, scarce, the user's stated image) /
any rock with tier scaling the AV / plus pebble top-offs.

### 3b. Baboon-skin throw bracers — NEW, added this session
**USER 2026-08-04:** arm-slot items that give a throw buff, flavored as baboon skin and
**butcherable from corpses.**
Claude notes, unruled:
- Flavor coherence is free: baboons are already the mod's (and Qud's) resident rock-throwers —
  wild baboons carry Small Stones and the StoneScavenger part, and the baboon figurine is a
  ring-salvage keeper. The throwers' hide teaching you to throw closes a loop the mod
  already drew.
- Acquisition via butchery means the base game gates it behind Cooking and Gathering →
  Butchery (50 sp) — a nice quiet answer to the skills audit's "throwing is skill-free"
  finding: the throw buff enters through a skill after all, just wearing gear's clothes.
  Baboons are a starting-area population, so low tier self-enforces.
- Buff shape options (pick at ruling time, engine support verified in the audit's pipeline
  reading): throw range (the `ThrowRangeBonus` idiom Geomancy already stacks with) /
  accuracy (the `CloseThrowRangeAccuracyBonus` property the mod itself already lends in
  Part_FallenRock) / to-hit or penetration (no base-game skill occupies this, so tread
  lightly).
- One-per-arm-slot question: Qud arm items pair (two arms); does the buff stack across both
  bracers, or is it a set piece where one is flavor and two are the buff?
**Open fork:** buff shape (range / accuracy / both-small) and whether two bracers stack.

### 4. Pulling weapon
**USER 2026-08-02:** hypertractor-adjacent sand-grab; **pulls loose objects too** — harvest
fallen rocks, drag Lithovore dinner (post-beta), restock the EB reserve from range. Combat
item that doubles as a gathering tool.
Claude elaboration, unruled: base-game hypertractor proves the pull plumbing; emergent play
is pulling enemies across your own dropped hazards (hexstone, cracked floor, tar).
**Open fork (the item's one real decision):** shape — activated-beam artifact copy (safe,
derivative) vs heavy melee weapon (maul/gauntlet family, Cudgel-adjacent so
Stonecarver-friendly) with an activated cone/line vacuum on cooldown.

### 5. Gravel shotgun
**USER 2026-08-02:** breaks a rock apart, fires it as a pebble blast. **It is a handheld
mill with a barrel** — inherits the mill law when the mill returns (mill + cooking are cut
for beta; future home = separate mod or stonecarver merchants, USER).
Claude guards, unruled: the spread does NOT carry the rock's exotic payload (grinding
destroys identity, same as the powder step); it's the aggressive consumer in the rock
economy and the non-mutant's way in.
**Open fork:** does the shotgun wait for the mill's future home to resolve (inheriting the
full law), or ship earlier with a simplified two-rule law (grindable = ammo, refused = jam)?
Waiting is the conservative read; the item is explicitly the mill's dependent.

### 6. Chisel + mallet carving — NEW, ruled this session
**USER 2026-08-04: item-gated, no skill.** "Just having the items would be enough." The
calling's starting tools gain the verb; anyone who finds a chisel and mallet can carve.
Claude sketch, unruled: an inventory-menu verb on the chisel requiring a mallet in inventory
(or either tool requiring the other); target = a held rock, an adjacent natural wall, or an
adjacent statue; output = a player-written inscription readable on look, in the engraving /
graffiti idiom the engine already ships. Pure flavor surface, zero combat contact — text is
the user's domain and the player's here; the mod just holds the chisel.
Adjacency (not scope creep, just noting the door): this is the cheapest on-ramp to the
deferred "geology that responds / carved commemorations" thread, and it composes with the
redoubt (carve the rim), Gorgon statues (sign your work), and Calcify (carve the wall you
raised).
**Open fork:** carve targets for v1 — rocks only (the user's literal words) / rocks + walls
/ rocks + walls + statues.

### 7. Moss boxes — surfaced from the lore notes
**USER (morestonecarver.txt, undated):** netsuke-adorned boxes containing clumps of dried
moss of a given color; used wet to **paint or infill chisel marks**, and edible as a meal
for a slight taste effect.
Placement here, Claude proposal: with carving now item-gated, the moss box is the carving
verb's natural companion item — carve with the chisel, infill with the moss, and the
inscription gains color. The eat-a-meal clause is a charming small verb with no system
weight (a taste effect, not an effect table).
**Open fork:** in or out of the gearset's first pass — it's the most ornamental item on the
list, and also the smallest.

### Fringe / explicitly parked
- **Active stone form as gear** — USER HUNCH 2026-08-04 (skill: no — affirmed by the audit's
  no-precedent finding; gear: "potentially"). No slot claimed, no shape pitched. Parked
  until wanted.
- **Rubble-ring item** — MOOTED: the jam's "fissure meditation lands item-side" fallback
  died when the redoubt became the mutation itself. Recorded so nobody re-pitches it.
- **Per-rock wear-riders on the socket armor** — the maximalist fork, parked HARD (jam
  wording) and further cut against by the rider-density concern.

---

## Where-it-lives, settled and open

Settled by rulings to date: wall-kick = boots (gear, RULED) · socket/feedable = body armor
(settled 2026-08-04) · carving = tools, item-gated (RULED 2026-08-04) · no bespoke skill
tree (declined 2026-08-04) · no new mutations preferred (seventh-mutation debt note, USER).

Still homeless or unresolved: puller's shape (fork above) · shotgun's timing (tied to the
mill's future home) · moss boxes in/out · active stone form (parked).

## Suggested ruling order (one at a time, cheapest-to-deepest)

1. Moss boxes in or out of the set's first pass.
2. Carving v1 targets (rocks only / + walls / + statues).
3. Bracers: buff shape (range / accuracy / both-small) and two-bracer stacking.
4. Gloves: caught-rock destination for non-EB wearers.
5. Boots: kick scope (any natural wall vs own-raised only).
6. Puller shape (beam artifact vs heavy melee vacuum).
7. Armor maw diet (boulders only / tiered rocks / pebble top-offs).
8. Shotgun timing (wait for the mill's home vs simplified early law).
