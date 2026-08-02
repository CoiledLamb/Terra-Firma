# Fallout sweep — redoubt-pivot gating inventory (2026-08-02)

Produced by a read-only sweep of the whole repo against the sign-off-round rulings (TODO.md,
REDOUBT PIVOT entry). Classifications: **GATE** (spawn/chargen/population entry to disable) ·
**RE-POINT** (live reference from surviving content into held content, must redirect) ·
**KEEP** (serves a keeper) · **DIES-WITH-OWNER** (only referenced by gated content).

Summary counts: GATE 68 · RE-POINT 71 · KEEP ~145 · DIES-WITH-OWNER ~34.
Ten ambiguous cases needing rulings are listed at the end.

---

## manifest.json

| Line | What | Class | Action |
|---|---|---|---|
| 4 | Description advertises "Seven earth mutations: … and the Lithovore defect" | RE-POINT | Swap in the beta sentence (ACCEPTED AS BETA DRAFT 2026-08-02). |

## Mutations.xml

| Line | What | Class | Action |
|---|---|---|---|
| 4, 7–10 | Geomancy, Calcify, Earthen Barrage, Stoneshaper, Gorgon's Gaze | KEEP | — |
| 12 | Fissure chargen entry | KEEP | Mutation survives the pivot. |
| 15–17 | Lithovore design comment block | RE-POINT | Annotate beta-held so the gate self-documents. |
| 18–20 | `<category Name="PhysicalDefects">` wrapping the Lithovore `<mutation>` entry | **GATE** | Comment out line 19; category tag goes with it (only member). Class stays compiled. |
| 19 | `Exclusions="Carnivorous,Photosynthetic Skin"` | DIES-WITH-OWNER | Inert once the entry is gated. |

## Creatures.xml

| Line | What | Class | Action |
|---|---|---|---|
| 3–11 | Burrowing Urchin + Aculei | KEEP | — |
| 13–18 | Malformed cragmensch design comment | RE-POINT | Mark beta-held. |
| 19–32 | `Cleo_TerraFirma_Malformed Cragmensch` blueprint | **GATE** | Comment out the whole object. Wish-spawnable only. |
| 25/27/28 | FabricateFromSelf / DropOnDeath / inventory → Rock_Strange; 29 Cragmensch Fist | DIES-WITH-OWNER | — |

**Finding: the cragmensch has ZERO population/encounter entries anywhere in the repo** —
gating the blueprint is the whole job.

## Merges.xml

| Line | What | Class | Action |
|---|---|---|---|
| 4–5, 9–11 | IsAnyStone/IsGemstone tags + GeomancyStonePart on BaseStone/Gemstone | KEEP | Geomancy + scavenger read these. |
| **6** | `StoneMill` part on **BaseStone** | **GATE** | Rock-side grind front door. |
| **12** | `StoneMill` part on **Gemstone** | **GATE** | Same. |
| 13–18 | Gemstone ThrownWeapon 1d2 | KEEP | Geomancy ammo ruling. |
| 20–151 | All StoneshaperWallProperties / NoStoneshape merges | KEEP | Stoneshaper. |
| 157–161 | Milling ruling comment | RE-POINT | Mark beta-held. |
| **162–164** | Campfire merge → StoneMill | **GATE** | Removes campfire grind action. |
| **165–167** | Pestle merge → StoneMill | **GATE** | Removes anywhere-grind. |
| 168–172 | Hopper ruling comment | RE-POINT | Mark beta-held. |
| **173–175** | Millstone merge → StoneHopper | **GATE** | Removes powered-mill grind. |
| 152–156 | Baboon ruling comment | RE-POINT | Now half-true (part stays, pockets go) — rewrite. |
| 176–177 | Baboon merge → StoneScavenger | **KEEP** | Explicitly retained (RULED 2026-08-02). |
| **178** | Baboon → `@ThrowableRocks1` pockets 1-2 | **GATE** | Remove this line only; leave 177. |
| **180–182** | Hulking Baboon merge (sole content = pockets 2-3) | **GATE** | Whole block. |
| **183–185** | Shrewd Baboon merge (sole content = pockets 2-3) | **GATE** | Whole block. |

Note: post-gate, wild baboons have StoneScavenger but no starting ammo — see ambiguity 4.

## PopulationTables.xml

Entire file (Stonecarver starting gear + maquette): KEEP, no held content. The Random
Copper/Silver Figurine entries are base game, not the baboon figurine.

## Populations.xml

### Throwable tables (feed geyser shots AND baboon pockets — top-priority RE-POINT surface)

| Line | Table | Class | Detail |
|---|---|---|---|
| 8–13 | Header comment incl. strange-bell ruling | RE-POINT | Rewrite; bell removed. |
| **14–35** | ThrowableRocksBand2 (13 entries) | **RE-POINT** | Survivors: 19 caromcobble, 28 shattered stalactite. Cut: 16 whorlstone, 17 torpor, 18 volatile, 20–25 fungal group, 26 ash, 27 spiderweb, 29 rusty, 30 scary, 31 bola, 32 spiny, 33 strange (w8, bell). |
| **36–63** | ThrowableRocksBand3 (20 entries) | **RE-POINT** | Survivors: 42 ajistone, 43 vantabsidian, 45 shunting. Cut: 38 voltaic, 39 scorching, 40 icy, 41 pulsestone, 44 melting, 46 hypersonic, 47–50 glowing twins, 51 mullstone, 52 hexstone, 53 lovestone, 54 shieldstone, 55 drifting, 56 gyrestone, 57 drainstone, 58 lodestone, 59 blurring, 60 strange (w3, bell). |
| **64–78** | ThrowableRocks1 (geyser-1 ammo + every baboon pocket target) | **RE-POINT** | All seven direct entries held: 66 pebbles, 67 generic, 68 soupstone, 69 sludgestone, 70 salt, 71 strange (w1), 72 pointy; 73–76 nest Band2/3. Rebuild from keepers or it resolves to nothing. |
| **79–87** | ThrowableRocks2 (geyser-2 ammo) | RE-POINT | Nests 1/Band2/Band3. |
| **88–95** | ThrowableRocksBand4 — deep four (90–93) | **GATE** | Nothing survives; drop its two callers (101, 108). |
| **96–104** | ThrowableRocks3 | RE-POINT | 101 → Band4. |
| **105–110** | ThrowableRocks4 (geyser-3 ammo) | **RE-POINT** | 108 → Band4; must re-point or top geyser fires nothing. |

### Fissure fall tables (live refs from the surviving mutation)

| Line | Table | Detail |
|---|---|---|
| 112–122 | 9-tier header comment (bell 1/2/4/6/8/6/4/2/1, deep four, mullstone) | RE-POINT — rewrite to keeper roster. |
| 123–137 | FissureTier1 | Held: 126 soupstone, 127 salt, 128 pointy, 129 healstone, 132 pebbles, 133 strange. ZERO keepers — empty after gating. |
| 138–158 | FissureTier2 | Held: 141 sludgestone, 147 scary, 150 pebbles, 151 generic, 152 strange, 153 holographic. Keeper: 142 crack. |
| 159–178 | FissureTier3 | Held: 162 oilstone, 167 rusty, 170 generic, 172 strange, 173 holographic. Keepers: 165 stalactite, 166 stalagmite, 171 rubble. |
| 179–200 | FissureTier4 | Held: 182 whorlstone, 183 torpor, 185–190 fungal group, 195 strange. Keepers: 193 rubble, 194 stalagmite. |
| 201–219 | FissureTier5 | Held: 204 bola, 205 spiderweb, 206 spiny, 208 ash, 214 strange (peak). Keepers: 207 caromcobble, 209 baboon statuette, 212 stalagmite, 213 crumbled wall. |
| 220–241 | FissureTier6 | Held: 223 volatile, 224 mullstone, 229–232 glowing twins, 236 strange. Keepers: 225 Omnigeyser1, 235 crumbled wall. |
| 242–261 | FissureTier7 | Held: 245 voltaic, 246 scorching, 247 icy, 248 melting, 251 ape statue, 256 strange. Keepers: 249 shunting, 250 vantabsidian, 254 crumbled wall, 255 wall. |
| 262–281 | FissureTier8 | Held: 265 drainstone, 266 hexstone, 267 hypersonic, 269 pulsestone, 270 shieldstone, 271 lodestone, 276 strange. Keepers: 268 ajistone, 272 Omnigeyser2, 275 wall. |
| 282–307 | FissureTier9 | Held: 285 blurring, 286 lovestone, 287 drifting, 289 gyrestone, 291–294 deep four, 303 strange. Keepers: 290 Omnigeyser3, 302 wall. |
| 308–317 | StrangeRocksPool header | DIES-WITH-OWNER | — |
| **318–370** | StrangeRocksPool (43 entries) | DIES-WITH-OWNER — only consumers are strange rock's payload + Lithovore EatenStrange, both gated. **Exception: harness `tfstrange` rolls it directly (_TestHarness.cs:1373).** |
| 372–461 | FissureTable_Generics1–8Plus (legacy Burgeoning plant copies) | KEEP — already retired from roll path. |
| 462–633 | FissureTable1–9 (legacy plant tables) | KEEP — dormant. |

## Rocks.xml

### Held catalog blueprints — GATE each object block
Lines: 42 pebbles · 51 fallen rock (generic — see ambiguity 1) · 69 holographic · 109 brine
geode · 120 sludgabbro · 134 healstone · 154 dazestone (already dormant) · 166 oilstone ·
184 pointy · 196 salt · 208 strange · 234 vertigneiss · 250 lullabaster · 286 quarrelquartz ·
298/318/322/326 fungal four · 361 gossamarl · 380 oxidian · 396 grimslate · 410 tanglelith ·
451 urchinite · 462 compressed ash · 487 galvena · 504 pyroclase · 517 rimeholm · 529
hushmica · 576 vitriolite · 631 ape statue · 714 streakschorl · 791 mullstone · 805 lattice
euclase · 824 dotagate · 838 aegischist · 852 phasalt · 864 gyrestone · 903 chasm (already
dormant) · 910 lodestone · 928 manymarble · 943 siphonyx · 970/990/1011/1028 deep four ·
1074/1091 glowing twins.

### Keeper blueprints + their cross-refs

| Line | What | Class | Detail |
|---|---|---|---|
| 18–38 | BaseFallenRock | KEEP | Six keepers inherit. |
| 79–85, 226–232, 331–342, 344–359, 479–485, 887–901 | Rubble, CrumbledWall, ShatteredStalactite, Stalagmite, Wall, Crack | KEEP | Structural. Stalagmite 352/358 → stalactite = keeper→keeper. |
| 266–284 | Rock_Rubber (caromcobble) | KEEP | 281 EatenHop inert with Lithovore gated. |
| 544–558 | Rock_Ajistone | KEEP | 553–554 eat channels inert. |
| 560–574 | Rock_Vantabsidian | KEEP | 571 inert. |
| 595–608 | Rock_Shunting (farflint) | KEEP | 605 inert. |
| 618–629 | BaboonFigurineItem | KEEP | — |
| **631–657** | Rock_ApeStatue | **GATE** | 650 TimedForm → Rubble ref dies with it; 661 StoneApeFist dies-with-owner. |
| 664–666 | StoneBaboonBite | KEEP | Statuette weapon. |
| 676–712 | Rock_BaboonFigurine (statuette) | KEEP | 691 Small Stone ×5 base-game fallback ammo survives. |
| **692** | Statuette pockets → `@ThrowableRocks1` | **RE-POINT** | Follows whatever ThrowableRocks1 becomes. |
| 697 | TimedForm → FigurineItem | KEEP | keeper→keeper. |
| 744–765 / 766–774 / 775–787 | Omnigeysers 1/2/3 | KEEP (collapse pending) | **755 / 773 / 786 RockTable refs = RE-POINT; 755 FabricateBlueprint fallback = Rock_Generic (held — ambiguity 1).** |

### Mill / cooking

| Line | What | Class |
|---|---|---|
| 1105–1111, 1120–1126, 1346–1350 | Milling comments | RE-POINT — mark beta-held. |
| **1112–1118** | StoneMeal | **GATE** |
| **1127–1133** | BaseGroundRock | **GATE** |
| **1135–1344** | All 46 Ground_* powders | **GATE** — incl. the four keeper-sourced (1193 Shunting, 1235 Rubber, 1254 Ajistone, 1274 Vantabsidian; see ambiguity 9). |
| **1351–1358** | CookingIngredient _Web | **GATE** |
| **1364–1369** | CookingIngredient _ArmorMinor | **GATE** |
| 1294–1300, 1359–1360 | Spacetime cut records | KEEP — historical comments. |

### Support blueprints for held rocks
93–107 puddle overrides (OilPuddle also named in Mutation_Lithovore.cs:734 — leave, harmless) ·
438–449 TarPatch (also harness) · 1050–1059 SurgeField · 1061–1072 force shards — all
DIES-WITH-OWNER.

## _TestHarness.cs (largest RE-POINT surface)

Constants 35–57: KEEP = STATUETTE_BP, FIGURINE_ITEM_BP, RUBBLE_BP, CAROM_BP, CRACK_BP.
RE-POINT = STREAK, HOPPER_STOCK, SCAV_AMMO (Rock_Generic), STRANGE_BP, STRANGE_POOL, APE,
PEBBLES, HEALSTONE, AEGISCHIST, DRAINSTONE, GEODE, GENERIC, SCARY, TANGLE. TAR_PATCH dies.

| Line | Scenario | Verdict |
|---|---|---|
| 63, 110–121 | tfverify help + tfbatch chain (121 queue string) | RE-POINT — prune to surviving set; drop quickape+drain from queue. |
| **146–154** | quick.pockets (asserts Hulking Baboon ≥1 mod rock) | **RE-POINT — asserts the exact merge being gated; will FAIL. Delete or invert.** |
| 158–167 | quick.rep | KEEP |
| 171–192 | quick.heal | RE-POINT — drop (healstone held). |
| 197–238 | quick.cage | RE-POINT — drop (aegischist held). |
| 245–279 | tfcarom | KEEP |
| 287–350 | tftar | RE-POINT — drop (tanglelith held). |
| 356–402 | tfmine (discharge law) | RE-POINT — re-point to farflint (StepChance=100, Rocks.xml:603) — tests the fragility SYSTEM, which survives. |
| 406–438 | tfcrack | KEEP |
| 442–463, 470–509 | tfspill, tfpuddle | RE-POINT — drop (geode held). |
| 549–667 | tfdevour (557 Lithovore gate) | RE-POINT — reachable only via kitchenrock post-gate; mark debug-only or drop from tfbatch. |
| 669–758 | tfshape | KEEP |
| 769–1020 | tfeat (777 gate; rows 800–830: 21 of 27 held; keeper rows 805 stalactite, 808 carom, 809 farflint, 824 vantabsidian; 991 mullstone assert) | RE-POINT — prune with Lithovore gate. |
| 1051–1093 | tffis | KEEP — depends on re-pointed tier tables. |
| **1097–1115** | SetupQuickApe | **RE-POINT — drop.** |
| 1117–1144 | tfdull/SetupDull (already retired) | RE-POINT — drop. |
| **1146–1245** | SetupDrain (4 asserts) | **RE-POINT — drop.** |
| 1346–1355 | CountModRocks | KEEP (only caller is gated quick.pockets). |
| **1363–1390** | tfstrange (rolls StrangeRocksPool ×50) | **RE-POINT — sole live pool consumer; see ambiguity 8.** |
| **1398–1461** | tfstreak | **RE-POINT — drop.** |
| **1468–1493, 1549–1563** | SetupHopper/SpawnMill | **RE-POINT — drop (mill held).** |
| 1500–1527 | SetupForm | KEEP |
| **1536–1546** | RunNext switch (hopper/quickape/dull/drain cases) | RE-POINT — remove dropped cases. |
| **1596** | SetupScav arms baboons with Rock_Generic | **RE-POINT — swap ammo; scenario survives.** |
| 1798–1969 | Finishers: FinishForm (1934, tag-driven) KEEP · FinishScav KEEP · TestThrowCounter KEEP · QuickApe/Dull/Drain/Hopper finishers + MillTurning log DIES-WITH-OWNER. |

## _Wishes.cs

| Line | Wish | Verdict |
|---|---|---|
| 15–36 | rockwineverytime (grants five keepers + Fissure; NOT Lithovore) | KEEP |
| **42–96** | kitchenrock (Millstone + Pestle + 3× every Rock_* + cooking skills + Lithovore grant) | **GATE or keep as deliberate dev door — ambiguity 7.** |
| **100–115** | grindcheck | DIES-WITH-OWNER (reports false forever post-gate). |
| **119–154** | fissurerocks (spawns one of EVERY Rock_* by prefix — would mint ~50 held rocks in beta) | **RE-POINT — gate or filter to keepers.** |

## Other Scripts

| File:Line | What | Verdict |
|---|---|---|
| **Mutation_Fissure.cs:33** | `DEBRIS_BLUEPRINT = Rock_Generic` — surviving mutation's hard-coded fallback into held content | **RE-POINT — highest-risk single line in the C#. Ambiguity 1.** |
| Mutation_Fissure.cs:38/40 | Stalagmite/stalactite blueprints | KEEP |
| Mutation_Fissure.cs:117 | GetTierTable → FissureTier1–9 | RE-POINT (via tier tables). |
| Mutation_Fissure.cs:201–206, 330–336 | NoSeam gate (only holographic bears the tag) | DIES-WITH-OWNER (code stays). |
| **Part_OmniFabricate.cs:16** | RockTable default = ThrowableRocks1 | **RE-POINT** |
| **Part_OmniFabricate.cs:41** | FabricateBlueprint fallback = Rock_Generic | **RE-POINT (ambiguity 1).** |
| Part_FallenRock.cs:282 | Dull-crumble → Rock_Pebbles (dormant path, live string) | RE-POINT |
| Part_FallenRock.cs:751/805/978–1007 | TarPatch, StrangeRocksPool, Blurring/DrainLatch/Enamored hooks | DIES-WITH-OWNER |
| Part_StoneMill.cs (all) | Grind system | **GATE** (unreferenced once Merges 6/12/163/166 gone; stays compiled). |
| Part_StoneHopper.cs (all) | Hopper | **GATE** (unreferenced once Merges 174 gone). |
| CookingDomains_Rocks.cs (all) | Web/ArmorMinor domains | **GATE** (only reachable via gated mappings). |
| Part_FungalLottery / Part_Holographic / Part_DrainLatch / Part_StreakFlight / Part_SurgeField / Effect_Enamored / Effect_OxidizedLimbs / Effect_StoneSiphon | Held-rock support | DIES-WITH-OWNER |
| Part_StoneScavenger.cs | Pure IsAnyStone tag matching | **KEEP — survives the pocket gate cleanly.** |
| Mutation_Lithovore.cs (848 lines) | All eat machinery | KEEP compiled; unreachable without chargen/wish. |
| _HarmonyPatches.cs (Lithovore meal patches) | Self-disabling (PlayerIsLithovore() false) | KEEP |
| Mutation_Geomancy.cs:101, ZonePainter.cs, Effect_GravelWhorl.cs:251, Part_RefreshOneCooldownOnEat.cs:13 | Tag reads / comments | KEEP |

---

## Ambiguities needing rulings (with Claude recommendations, UNRULED)

1. **Rock_Generic ("fallen rock") is unclassified — THE blocking decision.** It's Fissure's
   debris blueprint, the geyser fabricate fallback, harness scav ammo, and a generics-ladder
   entry in five tiers. RECOMMEND: reclassify as STRUCTURAL/KEEPER — it's inert plain stone,
   exactly what the redoubt wave produces and the plain-stone geyser fires. Solves four
   re-points at once.
2. **Rock_Pebbles** — held by ruling but bottom rung of the inert crush ladder + dull-crumble
   product. RECOMMEND: keep alongside generic as inert structural filler (zero identity cost).
3. **Geyser three-to-one collapse** — which blueprint survives, what tier placement, what ammo?
   RECOMMEND for Phase 1: re-point all three RockTables at the rebuilt ThrowableRocks1
   (keepers + plain stones) and leave all three live; do the collapse in Phase 2 when the
   redoubt's level-scaled spawn logic replaces tier placement entirely.
4. **Wild baboons: truly unarmed, or a base-game Small Stone line?** RECOMMEND: truly unarmed —
   scavenging IS the ruled fantasy; empty-handed baboons melee like base-game until they find
   rocks. The new tfbatch row proves pickup-and-throw works ("as long as it works").
5. **Held-list completeness confirmations:** healstone, oilstone, pointy, holographic,
   Rock_Hypersonic (= streakschorl) all HELD; Rock_Rubber = caromcobble KEEPER. RECOMMEND:
   confirm as stated.
6. **Dazestone + chasm (already dormant).** RECOMMEND: leave as-is; the tag preserves them,
   markers would be noise.
7. **kitchenrock wish = the only remaining Lithovore path post-gate.** RECOMMEND: keep as the
   deliberate dev door — wishes are opt-in dev tooling, TF-VERIFY is staying on anyway, and it
   preserves a live test path for the compiled Lithovore code.
8. **tfstrange is the sole live StrangeRocksPool consumer.** RECOMMEND: drop the scenario with
   the others; leave the pool defined-but-unreferenced (inert data, dies-with-owner).
9. **Keeper-sourced powders (Ground_Shunting/Rubber/Ajistone/Vantabsidian).** RECOMMEND: gate
   with the mill but under a distinct marker so they un-gate first when the mill returns.
10. **Marker convention.** RECOMMEND: reuse the exact BETA-HELD marker string from the
    description hold for every gate in this pass — one grep finds the entire hold.
