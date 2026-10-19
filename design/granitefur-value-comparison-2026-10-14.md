# Granitefur value comparison, 2026-10-14

**CLAUDE PROPOSAL (research only).** No prices changed. Data for the user's next pass on the
granitefur commerce values; the user decides.

Source: shipped Base XML, `CoQ_Data/StreamingAssets/Base/ObjectBlueprints/` (every base row
below is in `Items.xml`; the corpses are in `Creatures.xml`), values resolved through
`Inherits`. Commerce is the base `Commerce.Value` (before trade modifiers). Rows with value under
1 (natural weapons, placeholders) are left out of the stats.

## Our three items (TF2 `XML/ObjectBlueprints.xml`)

| Blueprint | Name | Tier | Slot | Stats | Value |
|---|---|---|---|---|---|
| `Cleo_TerraFirma_Cobblefur Bracer` | granitefur bracer | 2 | Arm | AV0/DV0, +2 throw range | 40 |
| `Cleo_TerraFirma_Cobblefur Gloves` | granitefur gloves | 2 | Hands | AV0/DV1, catch chance 50 | 50 |
| `Cleo_TerraFirma_Cobblefur Cloak` | granitefur cloak | 2 | Back | AV1/DV0, conditional DV+2 | 45 |

All three: organic fur, Baboons -100 rep per piece. Obtained by butchering the granite baboon
(TF2 `Creatures.xml`, `Butcherable OnSuccess="@Cleo_TerraFirma_Cobblefur Items"`: one of the
three at equal weight), plus the hewer/mason MiningGear gloves row and two loot tables
(TF2 `PopulationTables.xml`).

## Where each sits (percentile of base values)

| | Same slot, all tiers | Same slot, tiers 1-3 | Butchered wearables | Tier 2 wearables |
|---|---|---|---|---|
| bracer 40 | 45th | about 54th | 14th | 89th |
| gloves 50 | 32nd | about 67th | 14th | 89th |
| cloak 45 | 33rd | about 75th | 14th | 89th |

## 1. Same slot

### Arm (bracer). Full set: 30 items, min 5, median 50, max 33333 (Otherpearl)

| Blueprint | Name | Tier | Stats | Value |
|---|---|---|---|---|
| LeatherBracer | leather bracer | 3 | AV1/DV0 | 5 |
| Wrist Fan | wrist fan | 3 | AV0/DV0 | 20 |
| Compass Bracelet | compass bracelet | 2 | AV0/DV0 | 25 |
| Snakeskin Armlet | snakeskin armlet | 3 | AV0/DV2 | 35 |
| **granitefur bracer** | | 2 | +2 throw range | **40** |
| Precinct Navigator | precinct navigator | 4 | AV0/DV0 | 40 |
| Homoelectric Wrist Warmer | homoelectric wrist warmer | 4 | AV0/DV0 | 50 |
| Fidget Cell | fidget cell | 1 | AV0/DV0 | 60 |
| Bracelet1-3 | beaded bracelet | 1 | AV0/DV0 | 80 |
| Hologram Bracelet | hologram bracelet | 4 | AV0/DV0 | 125 |

### Hands (gloves). Full set: 19 items, min 5, median 195, max 1750

| Blueprint | Name | Tier | Stats | Value |
|---|---|---|---|---|
| Leather Gloves | leather gloves | 1 | AV1/DV0 | 5 |
| Boar-skin gloves | boar-skin gloves | 1 | AV1/DV0 | 5 |
| Steel Gauntlets | steel gauntlets | 3 | AV2/DV-3 | 20 |
| Elastyne Gloves | elastyne gloves | 3 | AV0/DV2 | 25 |
| Chain Gauntlets | chain gauntlets | 3 | AV2/DV-3 | 25 |
| **granitefur gloves** | | 2 | AV0/DV1, catch | **50** |
| Ape Fur Gloves | ape fur gloves | 3 | AV1/DV0 | 95 |
| Ulnar Stimulators | ulnar stimulators | 3 | AV1/DV0 | 115 |
| Carbide Gauntlets | carbide gauntlets | 4 | AV3/DV-5 | 150 |
| Quartzfur Gloves | quartzfur gloves | 5 | AV2/DV-1 | 250 |

### Back (cloak). Full set: 36 items, min 4, median 225, max 6666

| Blueprint | Name | Tier | Stats | Value |
|---|---|---|---|---|
| Leather Cloak | leather cloak | 2 | AV0/DV1 | 5 |
| Grassy Yurtmat | grassy yurtmat | 3 | AV0/DV1 | 5 |
| Panther's Cloak | panther's cloak | 2 | AV0/DV1 | 8 |
| Albino Monkey Braid | albino monkey braid | 1 | AV0/DV1 | 12 |
| Worn Burnoose | worn burnoose | 1 | AV0/DV1 | 12 |
| Scarlet Shawl | scarlet shawl | 1 | AV0/DV0 | 18 |
| **granitefur cloak** | | 2 | AV1/DV0, cond. DV+2 | **45** |
| Molly Netting | molly netting | 4 | AV0/DV1 | 75 |
| Portable Beehive | portable beehive | 3 | AV0/DV2 | 95 |
| Ape Fur Cloak | ape fur cloak | 3 | AV0/DV1 | 100 |
| Ironweave Cloak | ironweave cloak | 4 | AV1/DV0 | 200 |
| Quartzfur Cloak | quartzfur cloak | 5 | AV1/DV-1 | 250 |

## 2. Butchered items

Everything a base `Butcherable` part yields, directly or through its population table. Of 70
drops, 21 are wearable (min 10, median 250, max 1600); the rest are meat, glands, fangs and shells.
The fur sets are the direct analogs:

| Blueprint | Name | Tier | Slot | Stats | Value | From |
|---|---|---|---|---|---|---|
| Ape Fur Gloves | ape fur gloves | 3 | Hands | AV1/DV0 | 95 | Albino ape corpse |
| Ape Fur Hat | ape fur hat | 3 | Head | | 95 | Albino ape corpse |
| Ape Fur Cloak | ape fur cloak | 3 | Back | AV0/DV1 | 100 | Albino ape corpse |
| Albino Ape Pelt | albino ape pelt | 2 | (none) | | 100 | Albino ape corpse |
| Quartzfur Gloves / Hat / Cloak | quartzfur set | 5 | Hands/Head/Back | | 250 each | Quartz Baboon Corpse |
| Ogre Fur Gloves / Hat / Cloak | ogre fur set | 6 | Hands/Head/Back | | 300 each | Ogre ape corpse |
| SlogTrophy | Slog's annunclus | | Arm | | 250 | SlogCorpse |
| Knollworm Skull | knollworm skull | 2 | Head | AV1/DV0 | 20 | Knollworm Corpse |
| Electric Snail Shell | electric snail shell | 3 | Head | | 25 | Electric Snail Corpse |
| EyelessCrabShell | eyeless crab shell | 2 | (none) | | 50 | Eyeless Crab Corpse |

Granitefur (tier 2) sits under ape fur (tier 3) on base's own fur ladder: ape fur 95-100 at
tier 3, quartzfur 250 at tier 5, ogre fur 300 at tier 6. Every piece in a base fur set has the
same value, except ape fur's cloak (100 against 95).

## 3. Tier 2 wearables (all slots)

18 items, min 5, median 12.5, max 500 (25th to 75th percentile: 5 to 25).

| Blueprint | Name | Slot | Stats | Value |
|---|---|---|---|---|
| Leather Cloak | leather cloak | Back | AV0/DV1 | 5 |
| Vine-Weave Tunic | vine-weave tunic | Body | AV1/DV2 | 5 |
| Panther's Cloak | panther's cloak | Back | AV0/DV1 | 8 |
| Leather Boots | leather boots | Feet | AV1/DV0 | 10 |
| Ring Mail | ring mail | Body | AV3/DV-2 | 20 |
| Knollworm Skull | knollworm skull | Head | AV1/DV0 | 20 |
| Chain Mail | chain mail | Body | AV3/DV-1 | 25 |
| Goggles | goggles | Face | | 25 |
| Compass Bracelet | compass bracelet | Arm | | 25 |
| **granitefur set** | | Arm/Hands/Back | | **40-50** |
| Symbiotic Firefly | symbiotic firefly | Floating Nearby | | 400 |
| Mechanical Wings | mechanical wings | Back | | 500 |

## Readings (no recommendation)

- Against tier 2, granitefur is already high: 40-50 beats every tier-2 base wearable except
  the two tech items. Its effects are what set it apart from the 5-25 crowd.
- Against base fur, it sits under ape fur: 40-50% of the tier-3 ape fur prices, which fits
  the ladder if granitefur is meant to be the step below.
- Base fur sets price every piece alike. Granitefur's 40/50/45 split breaks that pattern. That's
  fine if the split is meant to reflect the gloves' catch and the cloak's DV; it's a choice, not
  a base precedent.
