# Carving: gaslight gauntlets, lathe, subject picker (spec, 2026-09-29)

Closed 2026-09-29, in effect. The "Open at build, mine" items at the end stay CLAUDE PROPOSAL. Beta top-ten heading #4 ("lathe carving and
crysteel gauntlets") is this work; "crysteel" was a slip for gaslight.

Concept (user): carving builds off etching by adding base-game verbs that do interesting
things. The lathe carves figurines from nuggets and gemstones; gaslight gauntlets carve
statues out of walls. Both open the same subject picker.

## Subject picker (shared, build first)

A `Popup.PickOption` with `Icons`, the same call and shape as base item naming
(`XRL.World.Capabilities.ItemNaming`, "Rename" menu). Each creature row shows its sprite.

1. **Last looked at.** The creature the player last looked at. Base `XRL.UI.Look` fires
   `LookedAt` on the player with `Object` (Look.cs ~304); a small player part keeps the
   blueprint. Hidden until a creature has been looked at.
2. **Local fauna.** Creatures from the current zone's spawn tables, shown as a list.
   Table lookup to be confirmed against the decompile at build.
3. **Search by name.** `Popup.AskString`, then a list of matching creatures by display name.
   Covers every creature. (Folds the old "pick from a list" and "type in blueprint".)
4. **Random.** Base `EncountersAPI.GetASampleCreature`, as `RandomFigurine` does.

Every option skips creatures tagged `NoFigurine`, as base does for random figurines.
Escape cancels and spends nothing. Testable alone through a wish.

## Gaslight gauntlets (new item, TF2)

- Hands, tier 4, **1 AV / 0 DV**, `Metal`. Tile `Items/sw_gloves_skintight.bmp` (base
  plastifer gloves), green `&g` with yellow `W` detail.
- Gaslight conventions (base `Gaslight Chisel`, Items.xml:910): charged name
  `{{gaslight|gaslight}} gauntlets`, uncharged plain; `EnergyCellSocket`;
  `LightSource OnlyIfCharged Radius=4`; Tinker bits `0004`, commerce 120. The base
  `Gaslight` part is weapon-only, so a small part of ours copies the name swap.
- **Weapon:** while worn, each hand's fist is swapped for a gaslight fist, mirroring base
  `CyberneticsFistReplacement` (swap `DefaultBehavior`, restore originals, re-apply on
  `RegenerateDefaultEquipmentEvent`) but driven by equip/unequip. Fist **1d4**. Charged:
  +5 PV and 100 charge per swing from the gauntlets' cell (Gaslight chisel/kris numbers).
  Uncharged: plain 1d4.
- **Carving:** an action on the gauntlets targets an adjacent natural stone wall, opens the
  picker and spends **1000 charge**. The wall becomes an inert, decorative statue through
  TF1's `MakeStatueOfCreatureFromWall` (made public for TF2), with material read from the
  wall as the Stoneshaper does. No animation, expiry or rough tiers; those stay the
  calling's.
- Acquisition: tinkering and quarrier loot.
- Description: user text, owed.

## Lathe (base `Lathe`, Furniture.xml:1350, merged)

- New interaction on every base lathe: pick a nugget or gemstone from inventory, open the
  picker, consume one, create a figurine through base `RandomFigurine` with `Creature` and
  `Material` set from the input (copper, silver, gold; agate through peridot, whose rep
  values base already defines).
- Needs power, matching the base lathe's existing `ChargeSink`.
- Reputation stays as base figurines have it (it only applies while held).

## Open at build, mine

- Time cost of a carve (turns).
- Exact spawn-table lookup for local fauna.
- Unpowered or out-of-charge refusal wording follows the 2026-09-29 failure-message
  conventions.
