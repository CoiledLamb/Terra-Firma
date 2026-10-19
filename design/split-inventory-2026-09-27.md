**CLAUDE PROPOSAL** working inventory, 2026-09-27: file/blueprint buckets for the TF1/TF2 split, produced read-only from items vs master. Bucket calls are proposals; the dependency arrangement itself is closed in design/decisions/2026-08-26-full-move.md.

# TF1 / TF2 split inventory (items @ 44a3b4a vs master @ 8915cad)

Read-only survey, 2026-09-27. Paths are relative to `CoQ_TerraFirma-dev/TerraFirma/`. Line numbers are on
`items` unless marked `master:`. "Status" is items vs master. Merge base is `a4b8db8` (0.1.2.2 state); master
has 6 commits items lacks (0.1.2.3 re-arm fix, which items has ported, and the 0.1.2.5 pull of the granite baboon
and cobblefur gear), so an "A" for the cobblefur scripts means "pulled from master in 0.1.2.5, kept on items".

Bucket key:
- **TF1**: six mutations, Stonecarver calling, Husangeion, and whatever those need.
- **TF2**: never-shipped content (items, creatures, warrens, chalk, and so on).
- **TF2@1.0**: shipped in master, moves to TF2 at the TF2 1.0 strip (per `design/decisions/2026-08-26-full-move.md`:
  etching, vanilla baboon scavenging, prototype assets, rockhound, snapjaw miner).
- **Shared**: both mods need it at runtime.
- **Q**: quarantined v1 content (fissure v1 rocks, Lithovore, kitchen). It is still compiled into shipped master
  and has been removed from items (parked on `quarantine-hold`).

---

## 1. Scripts (`Scripts/*.cs`, 71 on items plus 12 master-only)

### 1a. TF1 (31)

| File | Types | Status | Reason / change on items |
|---|---|---|---|
| Effect_EarthyVibrations.cs | `XRL.World.Effects.Cleo_TerraFirma_EarthlyVibrations` | unchanged | Geomancy scan effect |
| Effect_GravelWhorl.cs | `XRL.World.Effects.Cleo_TerraFirma_GravelWhorl` | **modified** | EB whorl. Change: `E.Done`/Validate guard at :176-182 against the double-resolve with the **cobblefur gloves** (TF2). The guard itself is harmless in TF1; it was added for TF2. |
| Effect_PartiallyPetrified.cs | `XRL.World.Effects.Cleo_TerraFirma_PartiallyPetrified` | unchanged | Gorgon |
| Effect_Petrifying.cs | `XRL.World.Effects.Cleo_TerraFirma_Petrifying` | unchanged | Gorgon |
| Fx_GeomancyPulse.cs | `Cleo.TerraFirma.Scripts.Cleo_TerraFirma_GeomancyPulseFX` | unchanged | Geomancy VFX |
| Fx_GorgonPetrify.cs | `Cleo.TerraFirma.Scripts.Cleo_TerraFirma_{PetrifyBandState,CreatureSnapshotRenderable,JuicePetrifyBand,JuicePetrifyChunk,GorgonPetrifyFX}` | unchanged | Gorgon VFX; also used by Fx_GravelWhorlFX |
| Fx_GravelWhorlFX.cs | `Cleo.TerraFirma.Scripts.Cleo_TerraFirma_{GravelWhorlFX,JuiceWhorlFleck,JuiceStoneBounce,JuiceSparkJack,JuiceWhorlArc}` | unchanged | EB VFX |
| Mutation_Calcify.cs | `XRL.World.Parts.Mutation.Cleo_TerraFirma_Calcify` | **modified** | TF1 fix: zone-edge clamp (:156-178); description text trimmed (matches Abilities.xml edit) |
| Mutation_EarthenBarrage.cs | `XRL.World.Parts.Mutation.Cleo_TerraFirma_EarthenBarrage` | **modified** | TF1: master's 0.1.2.3 empty-slot re-arm ported (:345-358); `GetUnequippedPart` count fix; NeverStack moved to blueprint tag (needs Objects.xml:27). Slot heal at :279-298 is a TF1 fix for a bug found through the **gloves/grinder** (TF2) |
| Mutation_Fissure.cs | `XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure`, `+WaveWallClass`, `+PulseWake` | **modified** | TF1: debris `Rock_Generic` changed to `SmallBoulder` (the blueprint was deleted); rim eviction `DirectMoveTo` fix. **Serves TF2:** pulse/sweep code pulled out into `public static RunPulses/Sweep/GetPulseDice/GetCreaturePVFor` + `PulseWake` so that `Part_FissureStone` (TF2) can call it |
| Mutation_Geomancy.cs | `â€¦Mutation.Cleo_TerraFirma_Geomancy`, `Cleo_TerraFirma_GeomancyCellData` | unchanged | |
| Mutation_GorgonsGaze.cs | `â€¦Mutation.Cleo_TerraFirma_GorgonsGaze` | **modified** | TF1 fix: unwinds `SuppressCorpseDrops` (:416-418) |
| Mutation_Stoneshaper.cs | `â€¦Mutation.Cleo_TerraFirma_Stoneshaper`, `+WallType` | **modified** | TF1 audit fixes: `UseEnergy` on carve, `BecomeCompanionOf` arg, muse-weapon re-arm data; `DEFAULT_BEHAVIOR_BP` changed to internal |
| Part_CalcifyWall.cs | `XRL.World.Parts.Cleo_TerraFirma_CalcifyWallPart` | unchanged | Calcify |
| Part_DazeOnThrowPens.cs | `â€¦Parts.Cleo_TerraFirma_DazeOnThrowPensPart` | unchanged | EB legacy (daze rider was cut 2026-08-06). **Nothing references it**; it may still be needed to deserialize old saves (see open questions) |
| Part_EarthlyVibrations.cs | `â€¦Parts.Cleo_TerraFirma_PartGeomancyScan` | unchanged | Geomancy |
| Part_GeomancyStone.cs | `â€¦Parts.Cleo_TerraFirma_GeomancyStonePart` | unchanged | Geomancy; merged onto base stones |
| Part_StoneRearmOnTake.cs | `â€¦Parts.Cleo_TerraFirma_StoneRearmOnTakePart` | unchanged | EB pebble |
| Part_StoneshaperStatueStats.cs | `â€¦Parts.Cleo_TerraFirma_StoneshaperStatueStatsPart` | **modified** | TF1 fix: re-arms natural weapons on `RegenerateDefaultEquipmentEvent` |
| Part_VesselTint.cs | `â€¦Parts.Cleo_TerraFirma_VesselTint` | unchanged | Husangeion glaze. On items it is carried by BaseBoarVessel (Creatures.xml:335), so TF2's wild/farm vessels use it too |
| Part_WhorlDeflected.cs | `â€¦Parts.Cleo_TerraFirma_WhorlDeflectedPart` | unchanged | EB whorl (also used by Part_FallenRock) |
| Part_FallenRock.cs | `â€¦Parts.Cleo_TerraFirma_FallenRockPart` | unchanged | Fissure spine (Rocks.xml:41, :226; Mutation_Fissure.cs:1054). Still contains inert v1-catalog code that pulls in the next four files |
| Part_TimedForm.cs | `â€¦Parts.Cleo_TerraFirma_TimedForm` | unchanged | Rocks.xml ring pieces (:156, :203), held and inert |
| Effect_Enamored.cs | `XRL.World.Effects.Cleo_TerraFirma_Enamored` | unchanged | Compiled only because of Part_FallenRock.cs:1007 (v1, inert) |
| Part_DrainLatch.cs | `â€¦Parts.Cleo_TerraFirma_DrainLatch` | unchanged | Compiled only because of Part_FallenRock.cs:993 (v1, inert) |
| Part_SurgeField.cs | `â€¦Parts.Cleo_TerraFirma_SurgePart` | unchanged | Compiled only because of Part_FallenRock.cs:781 (v1, inert) |
| Part_FungalLottery.cs | `â€¦Parts.Cleo_TerraFirma_FungalLottery` | unchanged | v1, inert; only the `tfspore` harness wish touches it |
| _HarmonyPatches.cs | `Cleo.TerraFirma.Scripts.Patches.HarmonyPatches` | **modified** | The gemstone-scan postfix stays (Geomancy). The three Lithovore patches were removed (parked in Q) |
| _MutationPoolOptions.cs | `Cleo.TerraFirma.Scripts.MutationPoolOptions` | unchanged | Options pool toggles |
| _StatueHelpers.cs | `Cleo.TerraFirma.Scripts.StatueHelpers` | unchanged | Gorgon and Stoneshaper |
| _SerializationHarness.cs | `Cleo.TerraFirma.Scripts.SerializationHarnessWishes` | unchanged | Dev harness (`tfserial`), TF1 subjects only. Compile-depends on `_TestHarness.cs` (`TestHarnessWishes`, `Cleo_TerraFirma_TestDriver`) |

### 1b. Shared (4)

| File | Types | Status | Reason |
|---|---|---|---|
| _Helpers.cs | `Cleo.TerraFirma.Scripts.Helpers` | unchanged | Every script uses `Helpers.VerifyLog`. It reads the Options.xml option `Cleo_TerraFirma_DebugLog` |
| Fx_StoneRise.cs | `Cleo.TerraFirma.Scripts.Cleo_TerraFirma_{JuiceStoneRise,JuiceCarveBlock,JuiceStoneChunk,StoneTileRenderable,StoneRiseFX}` (+nested `Schedule`) | unchanged | The TF1 mutations use it; TF2 uses `PlayEtchReveal` (:713, called from Part_Etchable.cs:480) and `StoneTileRenderable`/`CloneSubRect` (Fx_ChalkReveal.cs:108-308) |
| Part_StoneshaperWallProperties.cs | `â€¦Parts.Cleo_TerraFirma_StoneshaperWallProperties` | unchanged | Used by the TF1 mutations. TF2 reads `IsNaturalStone` (:26) in WarrenCarver.cs:236 and Part_MoleCricket.cs:111, and Part_Etchable.cs:182 reads the part. It only works together with the Merges.xml wall table |
| Part_StoneScavenger.cs | `â€¦Parts.Cleo_TerraFirma_StoneScavenger` | unchanged | Placed in TF2@1.0 by decision (vanilla baboon scavenging, Merges.xml:180), but the TF1 Rocks.xml `Rock_BaboonFigurine` (:199) carries it. Shared, unless that held blueprint drops the part |

### 1c. TF2, new on items (30)

| File | Types | Reason / TF1 types it uses |
|---|---|---|
| Builder_AnimateFigurineIdentity.cs | `XRL.World.ObjectBuilders.Cleo_TerraFirma_AnimateFigurineIdentity` | Mason figurines |
| Builder_StonecarverEpithet.cs | `Cleo.TerraFirma.Scripts.StonecarverEpithet` (+nested `Roll`, `StatGrant`, `Definition`), `XRL.World.ObjectBuilders.Cleo_TerraFirma_StonecarverLegendary` | Overseer. **Instantiates the TF1 mutations** at :101-104 |
| Fx_ChalkReveal.cs | `Cleo.TerraFirma.Scripts.Cleo_TerraFirma_{ChalkRevealFX,JuiceChalkMaskFrag,JuiceChalkMote}` | Chalk; uses Fx_StoneRise (Shared) |
| Fx_SeismicRise.cs | `XRL.World.Parts.Cleo_TerraFirma_SeismicArrival`, `Cleo.TerraFirma.Scripts.SeismicRise` | Seismic shufflers |
| Part_AnimationTable.cs | `â€¦Parts.Cleo_TerraFirma_AnimationTablePart` | Uses `Cleo_TerraFirma_Stoneshaper.GetHP/GetAV` (:195-196), which are **TF1** |
| Part_Cairn.cs | `â€¦Parts.Cleo_TerraFirma_CairnTopple` | Warren trail |
| Part_ChalkBox.cs | `â€¦Parts.Cleo_TerraFirma_ChalkBox` | Chalk |
| Part_ChalkLine.cs | `â€¦Parts.Cleo_TerraFirma_ChalkLine` | Chalk; builds tile names at :169 |
| Part_ChalkboardWritable.cs | `â€¦Parts.Cleo_TerraFirma_ChalkboardWritable`, `Cleo_TerraFirma_ChalkWriting` | Chalk; uses `Etchable.TYPED_CHARS/TYPED_MAX` (:100) |
| Part_CobblefurCloak.cs | `â€¦Parts.Cleo_TerraFirma_CobblefurCloak` | Granitefur. Shipped in 0.1.2.2/0.1.2.3 and pulled in 0.1.2.5; reads the `IsAnyStone`/`IsRubbleCover` tags (:82) |
| Part_CobblefurGloves.cs | `â€¦Parts.Cleo_TerraFirma_CobblefurGloves`, `Cleo_TerraFirma_CaughtGrenade` | Granitefur; pulled in 0.1.2.5. Also modified on items after the merge base (all thrown items, grenade fuse) |
| Part_FissureStone.cs | `â€¦Parts.Cleo_TerraFirma_FissureStonePart` | Calls **TF1** `Cleo_TerraFirma_Fissure.RunPulses/GetPulseDice` (:44-45) |
| Part_GlowsphereBreak.cs | `â€¦Parts.Cleo_TerraFirma_GlowsphereBreak` | Glowsphere merge |
| Part_GlowstoneLantern.cs | `â€¦Parts.Cleo_TerraFirma_GlowstoneLantern` | Lantern |
| Part_GlowstoneShardOnHit.cs | `â€¦Parts.Cleo_TerraFirma_GlowstoneShardOnHit` | Shard |
| Part_GravelAmmoLoader.cs | `â€¦Parts.Cleo_TerraFirma_GravelAmmoLoader` | Grinder shotgun |
| Part_GrinderCell.cs | `â€¦Parts.Cleo_TerraFirma_GrinderCell` | Reads the `IsAnyStone` tag (:27) |
| Part_HasFigurines.cs | `â€¦Parts.Cleo_TerraFirma_HasFigurinesPart`, `Cleo_TerraFirma_BondedFigurinePart` | Mason escort; faction string at :69 |
| Part_MoleCricket.cs | `â€¦Parts.Cleo_TerraFirma_MoleCricket` | Uses StoneshaperWallProperties (Shared) at :111 |
| Part_MossyWall.cs | `â€¦Parts.Cleo_TerraFirma_MossyWall` | Warren moss |
| Part_PiezoelectricMill.cs | `â€¦Parts.Cleo_TerraFirma_PiezoelectricMill` | Warren power |
| Part_SeismicShufflers.cs | `â€¦Parts.Cleo_TerraFirma_SeismicShufflers` | Boots |
| Part_Seismophone.cs | `â€¦Parts.Cleo_TerraFirma_Seismophone` | Helmet |
| WarrenCarver.cs | `Cleo.TerraFirma.Scripts.WarrenCarver` (partial; nested Stage/Grid/Box/Pt/PocketRec/Artery/Band/RegionRec/AquiferRec/Measure0/RolesRec/Row/RowSpec/RoomType/Result) | Warrens; uses StoneshaperWallProperties (Shared) at :236 |
| WarrenCommons.cs | `WarrenCarver` (partial) | Pre-etched walls through `Cleo_TerraFirma_Etchable` (:41-52) |
| WarrenFurnishingComparison.cs | `WarrenCarver` (partial; nested RoomPair/FurnishingPair) | |
| WarrenMaker.cs | `XRL.World.ZoneBuilders.Cleo_TerraFirma_WarrenMaker`, `WarrenMakerTest` | Wishes `tfwarrenv2`, `tfwarrens` |
| WarrenPower.cs | `WarrenCarver` (partial) | |
| WarrenSurvey.cs | `Cleo.TerraFirma.Scripts.WarrenSurvey` | Wish `tfwarrenstats` |
| WarrenWorldBuilder.cs | `XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenPlacement` (`[JoppaWorldBuilderExtension]`) | Places warrens during live worldgen |

### 1d. TF2@1.0, shipped in master (3)

| File | Types | Status | Change on items |
|---|---|---|---|
| Part_Etchable.cs | `â€¦Parts.Cleo_TerraFirma_Etchable` | **modified** | All three changes serve TF2: `TYPED_CHARS/TYPED_MAX` changed to internal for chalk; `SetGeneratedInscription` added for warren generation; an improvised-tool tier penalty (an etching tweak). It uses **TF1/Shared** `StoneshaperWallProperties` (:182) and `StoneRiseFX.PlayEtchReveal` (:480) |
| Part_RockSweeper.cs | `â€¦Parts.Cleo_TerraFirma_RockSweeper` | **modified** | Serves TF2 only: rockhound active seek and the NoAIEquip gut-mark |
| ZonePainter.cs | `Cleo.TerraFirma.Scripts.ZonePainterTest`, `Carve` | unchanged | Prototype painter wishes (`tfpaint`, `tfquarry`, â€¦) that create FireDish and QuarriedBlock |

### 1e. Split per wish (2)

| File | Types | Status | TF1 wishes | TF2 wishes |
|---|---|---|---|---|
| _Wishes.cs | `Cleo.TerraFirma.Scripts.WishHandler` | **modified** (`kitchenrock`, `grindcheck` and `fissurerocks` moved to Q; 11 TF2 wishes added) | rockwineverytime (but :35 gives the prototype `StatueMaker`), tfpen, tfwhorl{fx,gym,laser,swat,rico,devour,attune,outcome}, tfgeoscan | tfcreatures (roster also includes the TF1 `BoarVessel`), tfcarverpack (uses `StonecarverEpithet`), tfjingle(neg), tfgravel, tflantern, tfgrinder, tfcricket, tfseismophone, tffigtable, tfchalk |
| _TestHarness.cs | `Cleo.TerraFirma.Scripts.TestHarnessWishes`, `XRL.World.Parts.Cleo_TerraFirma_TestDriver`, `Cleo_TerraFirma_TestThrowCounter` | **modified** (-1120 lines: v1 rock/kitchen tests removed) | tfverify, tfnpc, tfbatch, tfquick, tffeedback (includes the Husangeion VesselTint check), tfgazewall (but :351 creates the TF2 `QuarriedBlock`), tfcrack, tfshape, tfstatueai, tffis, tfredoubt, tfdull (retired), tfform, tfspore (v1) | tfscav (baboon scavenger) |

### 1f. Unclear (1, plus one flagged)

| File | Types | Status | Why |
|---|---|---|---|
| Part_StatueMaker.cs | `â€¦Parts.Cleo_TerraFirma_StatueMakerPart` | unchanged (in master) | The "claysculpting loop" tool (Objects.xml:83), placeholder description, spawned by no table; only `rockwineverytime` gives it. It looks like a prototype asset (so TF2@1.0), but it depends on the TF1 `Stoneshaper` type |
| (Part_DazeOnThrowPens.cs) | listed in 1a | | Dead code; only possible reason to keep it is save deserialization |

### 1g. Deleted on items, still shipped in master (Q, 12)

`CookingDomains_Rocks.cs` (`XRL.World.Effects.Cleo_TerraFirma_CookingDomainArmorMinor_UnitAV`, `â€¦CookingDomainWeb_UnitSpinnerets`, `â€¦CookingDomainWeb_WebsErupt_ProceduralCookingTriggeredAction`), `Effect_DarkAdapted.cs` (`Cleo_TerraFirma_DarkAdapted`), `Effect_GasBelly.cs` (`â€¦GasBelly`), `Effect_OxidizedLimbs.cs` (`â€¦OxidizedLimbs`), `Effect_StoneFooting.cs` (`â€¦SureFooted`, `â€¦SureGreased`), `Effect_StoneSiphon.cs` (`â€¦StoneSiphon`), `Mutation_Lithovore.cs` (`XRL.World.Parts.Mutation.Cleo_TerraFirma_Lithovore`), `Part_Holographic.cs` (`â€¦HolographicPart`), `Part_RefreshOneCooldownOnEat.cs`, `Part_StoneHopper.cs`, `Part_StoneMill.cs`, `Part_StreakFlight.cs` (`â€¦StreakFlightPart`). All of these are parked on `quarantine-hold`. `Part_CobblefurBracer.cs` was deleted on both branches: items replaced it with a stock `IntPropertyChanger`.

**Script counts (items, 71):** TF1 31 (DazeOnThrowPens included but flagged), Shared 4, TF2 30, TF2@1.0 3, split 2, unclear 1 (StatueMaker). Q (master only): 12.

---

## 2. XML, per file

| File | Status | TF1 | Shared | TF2 | TF2@1.0 / Q / unclear |
|---|---|---|---|---|---|
| Abilities.xml | modified (Calcify and EB text lines trimmed, matching the C#) | all 6 ability entries (Calcify, EarthenBarrage, AttuneStones, Stoneshaper, GorgonsGaze, Fissure) | | | |
| Bodies.xml | unchanged | anatomy `Cleo_TerraFirma_BoarVessel` (Husangeion) | also used by TF2 wild/farm vessels through BaseBoarVessel (Creatures.xml:331) | | |
| Callings.xml | modified (+`<reputations>`) | subtype Stonecarver | | | :24 references the **new faction** (see 3A) |
| Conversations.xml | modified | `Cleo_TerraFirma_ConversationStoneshaperStatue` | | `â€¦SnapjawMiner`, `â€¦Outrider`, `â€¦Mason`, `â€¦Hewer` | SnapjawMiner joins the shipped miner at 1.0 |
| Creatures.xml | modified | `Cleo_TerraFirma_BaseBoarVessel` (:329) **only because** Husangeion inherits it | | Vine Merchant, Cobble Baboon (+Corpse; pulled in 0.1.2.5), Stonecarver chassis, Overseer, Hewer, Mason, Outrider, Mole Hewer/Mason/Outrider, AnimateFigurine, Farm Boar Vessel, Boar Vessel Corpse | TF2@1.0: Rockhound, Snapjaw Miner. **Unclear:** Burrowing Urchin (shipped; not named in the placement decision) |
| Factions.xml | new | | `Cleo_TerraFirma_Quarriers` (calling rep and every quarrier NPC) | | |
| Merges.xml | modified | NoStoneshape tags (Golem/Mech/Nephilim/Chavvah/Mainframe, :154-168) | BaseStone/Gemstone/BaseNugget/Random Figurine (:3-32: GeomancyStonePart and the `IsAnyStone`/`IsGemstone`/`IsGeomancyStone` tags; TF2 parts read IsAnyStone); every `StoneshaperWallProperties` wall merge (:37-153; WarrenCarver, MoleCricket and Etchable read them) | Rubble `IsRubbleCover` (:198-200, cloak; pulled in 0.1.2.5), Glowsphere (:213-218), Chalkboard (:221-223), Torchpost/Torch ColdForm (:230-235) | TF2@1.0: Baboon/Hulking/Shrewd scavenger and stones (:179-188), Wall/Statue/Random Statue Etchable (:201-209) |
| Mutations.xml | modified (Lithovore comment only) | all 6 | | | |
| Naming.xml | new | | | namestyle `Cleo_TerraFirma_Stonecarver` | |
| ObjectBlueprints.xml | modified | Blunted Chisel (:57), Splintered Mallet (:67): calling gear | Random Stone Figurine (:257, new: calling maquette and TF2 figurines/dress). The chisel and mallet are also rows in the TF2 Carver ladders | Cobblefur Bracer/Gloves/Cloak (pulled in 0.1.2.5), FireDishCold, MossClump, QuarryStones1/2/3, Cairn, CairnToppled, Stone Basket, Food Basket, Yard Board, Quarry Chest, FissureStone | TF2@1.0: FireDish, QuarriedBlock (re-parented on items onto QuarryStones1). **Unclear:** Urchin Spine (referenced nowhere), Aculei (urchin weapon) |
| Objects.xml | modified | EphemeralStone (+NeverStack tag, which EB now relies on), EphemeralStoneResonant, WallCalcify, StoneshaperStatue, StoneshaperStatueNaturalWeapon; GutCistern (:320) only through the Husangeion parent's corpse | | Gravel Shotgun, GravelProjectile, GlowstoneShard, GlowstoneLantern(+Loaded), GrinderCell, MoleCricket, Seismophone, AnimationTable(+Dormant), PiezoelectricMill(+Dormant), ChalkLine, BoxOfChalk, CanyonStatueÃ—11, NamingMaterials, NamingCrafts | **Unclear:** StatueMaker (:83) |
| Options.xml | unchanged | TakeStonesDefaultOn, PoolÃ—6 | `Cleo_TerraFirma_DebugLog` (read by `Helpers`) | | |
| Pets.xml | modified (re-parented) | `Cleo_TerraFirma_BoarVessel` = Husangeion | | | :9 `Inherits="Cleo_TerraFirma_BaseBoarVessel"` (see 3A) |
| PopulationTables.xml | modified (27 lines on master, 923 on items) | `Cleo_StartingGear_Stonecarver` (:3-20) | `Cleo_Stonecarver_Maquette` (:29-45: calling kit and Mason Kit); `Cleo_TerraFirma_MiningGear` (:404-422, **mixed rows**, see below) | AnimateFigurineIdentity, Cobblefur Items, Stonecarver Pack, WarrenOccupancy/Aquifers/Stage, WarrenCrewÃ—3, WarrenReclaimers, Quarrier/Hewer/Mason/Outrider Kit, Carver Knife/Hammer, VineMerchantWares, merges into BaboonParty/BigBaboonParty, Utility 2, Artifact 0R-4R, Trinkets 1-8, Armor 2C/3R, WarrenDressÃ—10, WarrenPoolÃ—10, StoneBasket/FoodBasket Contents, StiltAnimalPen merge, WarrenWiring/WireDamage/EtchCount/StatueLikeness/Visitors/Hermits/SparseWebs/SpiderWebs | |
| Populations.xml | modified (emptied) | | | | master: 35 v1 rock tables = Q |
| Rocks.xml | modified (1130 lines removed) | Fissure spine: BaseFallenRock, Rock_Pebbles, Rock_Rubble, Rock_CrumbledWall, RedoubtFigure, Rock_Wall, Rock_Crack, Rock_Chasm. Held and inert: BaboonFigurineItem, Rock_ApeStatue, StoneApeFist, StoneBaboonBite, Rock_BaboonFigurine | Rock_Pebbles is also used by TF2 (dress :641, basket :836) | | master: ~100 v1 rocks and Ground_* = Q |
| SeismicShufflers.xml | new | | | SeismicShufflers, SeismicProjection | |

**The mixed table, `Cleo_TerraFirma_MiningGear` (PopulationTables.xml:404-422).** Its base-game rows (Leather Cap, Vinewood Sap Mask, Goggles, Witchwood Wreath, Miner's Helmet, Headlamp, Gas Mask, Knollworm Skull, Leather Gloves, Boar-skin gloves, Nylon Bodypack) would work in TF1. Two rows are TF2: `Cobblefur Gloves` (:418) and `SeismicShufflers` (:421). Its consumers are the TF1 calling kit (:12) and TF2's Hewer Kit (:230), Mason Kit (:257), VineMerchantWares (:396) and Quarry Chest (ObjectBlueprints.xml:241).

**The Merges.xml hard case.** A shared `BaseStone`/wall-properties block that loads from both mods would merge twice. Whether a duplicate `Load="Merge"` with an identical part is idempotent has not been verified against the engine.

---

## 3. Cross-bucket references

### 3A. TF1 content that references TF2 or new content (breaks TF1 standalone on items): 14

| # | From (TF1) | To | Note |
|---|---|---|---|
| A1 | Callings.xml:24 `reputation With="Cleo_TerraFirma_Quarriers"` | Factions.xml:67 (new) | Calling rep needs the faction. The faction's -200 initial rep and the +200 grant net to 0 by design |
| A2 | PopulationTables.xml:12 (calling kit) `table Cleo_TerraFirma_MiningGear` | :404 | This row was added on items; master's kit lacks it |
| A3 | through A2, :418 | `Cleo_TerraFirma_Cobblefur Gloves` (ObjectBlueprints.xml:33, Part_CobblefurGloves.cs, catching_gloves.png) | The 0.1.2.5 release pulled this family from TF1 |
| A4 | through A2, :421 | `Cleo_TerraFirma_SeismicShufflers` (SeismicShufflers.xml:4, Part_SeismicShufflers.cs, Fx_SeismicRise.cs, `Abilities/seismic peek.png`) | |
| A5 | PopulationTables.xml:15 to :39 Maquette | `Cleo_TerraFirma_Random Stone Figurine` (ObjectBlueprints.xml:257) | New on items; XML only; candidate for Shared |
| A6 | Pets.xml:9 Husangeion `Inherits` | `Cleo_TerraFirma_BaseBoarVessel` (Creatures.xml:329) | Re-parented 2026-08-26 |
| A7 | through A6, Creatures.xml:340 `CorpseBlueprint` | `Boar Vessel Corpse` (:370), then `Butcherable` to `GutCistern` (Objects.xml:320) | The pet sets CorpseChance 0, but the blueprints must still resolve |
| A8 | through A6, Creatures.xml:351-354 | `DynamicObjectsTable:{DesertCanyon,Jungle,Hills,Flowerfields}_Creatures` | If BaseBoarVessel moves into TF1, **wild boar vessels spawn from TF1**, which would be new creature content in the base mod |
| A9 | Rocks.xml:199 Rock_BaboonFigurine | `Cleo_TerraFirma_StoneScavenger` | Placed in TF2@1.0 by decision |
| A10 | Rocks.xml:198 | `@Cleo_TerraFirma_ThrowableRocks1` | **Already dangling on items** (the table is parked on quarantine-hold); inert while the figurine never spawns |
| A11 | _Wishes.cs:35 (rockwineverytime) | `Cleo_TerraFirma_StatueMaker` | Dev only; prototype item |
| A12 | _TestHarness.cs:351 (tfgazewall) | `Cleo_TerraFirma_QuarriedBlock` | Dev only; prototype |
| A13 | _SerializationHarness.cs | `TestHarnessWishes`, `Cleo_TerraFirma_TestDriver` (_TestHarness.cs:33, :1585) | Compile dependency if the harness is split |
| A14 | ObjectBlueprints.xml:65, :76 `Cleo_TerraFirma_EtchTool` tags on chisel and mallet | consumed only by Part_Etchable (TF2@1.0) | Soft reference: the tags are inert without Etchable |

### 3B. TF2 content that references TF1-owned types, blueprints or tags (needs Shared, or TF2 depends on TF1): 25

| # | From (TF2) | To (TF1 or Shared) |
|---|---|---|
| B1 | Creatures.xml:210 Hewer `mutation Cleo_TerraFirma_Fissure` | TF1 class |
| B2 | Creatures.xml:228 Mason `Cleo_TerraFirma_Stoneshaper` | TF1 class |
| B3 | Creatures.xml:249 Outrider `Cleo_TerraFirma_EarthenBarrage` | TF1 class |
| B4 | Creatures.xml:103 Cobble Baboon `Cleo_TerraFirma_EarthenBarrage` | TF1 class |
| B5 | Objects.xml:421-431 NamingCrafts `Mutation="Cleo_TerraFirma_{Fissure,Stoneshaper,EarthenBarrage,Geomancy}"` | TF1 classes |
| B6 | Builder_StonecarverEpithet.cs:101-104 `new Cleo_TerraFirma_{Fissure,Stoneshaper,EarthenBarrage,Geomancy}()` | TF1 classes (compile-time) |
| B7 | Part_FissureStone.cs:44-45 `Cleo_TerraFirma_Fissure.RunPulses/GetPulseDice` | TF1 statics, extracted on items for this caller |
| B8 | Part_AnimationTable.cs:195-196 `Cleo_TerraFirma_Stoneshaper.GetHP/GetAV` | TF1 statics |
| B9 | Part_MoleCricket.cs:111-112 `StoneshaperWallProperties.IsNaturalStone` | Shared |
| B10 | WarrenCarver.cs:236-237 `StoneshaperWallProperties.IsNaturalStone` | Shared |
| B11 | Part_Etchable.cs:182 (`StoneshaperWallProperties`), :480 (`StoneRiseFX.PlayEtchReveal`) | Shared |
| B12 | Fx_ChalkReveal.cs:108, 132, 161, 169, 308 (`StoneTileRenderable`, `StoneRiseFX.CloneSubRect`) | Shared (Fx_StoneRise.cs) |
| B13 | Tag `Cleo_TerraFirma_IsAnyStone` (Merges.xml:4, :8): Part_GrinderCell.cs:27; Part_RockSweeper.cs:74, 157, 183; Part_CobblefurCloak.cs:82; Part_StoneScavenger.cs:74, 102, 130 | Shared merge block |
| B14 | PopulationTables.xml:354, :361 Carver Knife/Hammer | Blunted Chisel, Splintered Mallet (TF1 calling gear) |
| B15 | PopulationTables.xml:254 Mason Kit | `Cleo_Stonecarver_Maquette` (TF1 calling table) |
| B16 | PopulationTables.xml:230, 257, 396; ObjectBlueprints.xml:241 | `Cleo_TerraFirma_MiningGear` (mixed table) |
| B17 | PopulationTables.xml:641, :836 | `Cleo_TerraFirma_Rock_Pebbles` (Rocks.xml:50, Fissure spine) |
| B18 | Creatures.xml:360 Farm Boar Vessel `Inherits BaseBoarVessel`; StiltAnimalPen merge :855 | BaseBoarVessel (TF1 if Husangeion keeps it) â†’ Bodies.xml anatomy and Part_VesselTint (TF1) |
| B19 | Creatures.xml:306 AnimateFigurine corpse; PopulationTables.xml:25, :794 | Random Stone Figurine (Shared) |
| B20 | Quarriers faction string: Creatures.xml:8, :159, :303; Objects.xml:193; Part_HasFigurines.cs:69; WarrenCarver.cs:2950, :2955 | Factions.xml (Shared) |
| B21 | Every TF2 .cs file â†’ `Helpers` | _Helpers.cs, plus the Options.xml DebugLog option |
| B22 | WarrenCarver dress over walls carrying `PaintPart StoneshaperWallProperties` | Merges.xml:37-153 (Shared) |
| B23 | _Wishes.cs tfcreatures roster | `Cleo_TerraFirma_BoarVessel` (TF1 pet) |
| B24 | _Wishes.cs:591-597 tfcarverpack | TF2 `StonecarverEpithet` and the TF2 chassis. Listed so the file split keeps the two together |
| B25 | Part_ChalkboardWritable.cs:100, WarrenCommons.cs:41-52 | `Cleo_TerraFirma_Etchable` (TF2@1.0). **During the additive beta, Etchable ships in TF1**, so TF2 would depend on a TF1 copy until the strip |

**Also relevant (no reference, but TF1 code that exists because of TF2):** the EB slot heal (Mutation_EarthenBarrage.cs:279-298), the GravelWhorl guard (:176-182) and the Fissure static extraction. All are safe to keep in TF1.

**Total cross-bucket references: 39** (14 from TF1 into TF2, 25 from TF2 into TF1 or Shared).

---

## 4. Textures

| Bucket | Textures (referencing blueprint or file) |
|---|---|
| TF1 | `Abilities/abil_{attunestone,calcify,earthenbarrage,fissure,geomancy,gorgonsgaze,stoneshaper,takestones_off,takestones_on}.png` (Mutations.xml, Abilities.xml); `blunted_chisel.png` (Blunted Chisel) |
| **Both** | `stonecarver.png`: the calling Tile (Callings.xml:4, TF1), the NPC chassis (Creatures.xml:154, TF2), and the faction emblem (Factions.xml:76, Shared). `boar vessel.png`: BaseBoarVessel Render (Creatures.xml:332), which Husangeion (TF1) and the wild/farm vessels (TF2) all inherit |
| TF2 (new) | `Abilities/seismic peek.png` (Shufflers), `animation_table.png`, `baboonskin_bracers.png`, `catching_gloves.png`, `cairn1.png`, `cairn2.png`, `chalkline{TE,TN,TS,TW,X}.png` (only through the tile names built at Part_ChalkLine.cs:169), `glowsphere_shard.png`, `grinder_cell.png`, `grinder_cell2.png` (Part_GrinderCell.cs:270), `lantern_lit.png`, `lantern_unlit.png`, `mole_cricket.png` |
| TF2@1.0 (in master) | `chalkline{E,N,NE,NW,S,SE,SW,W}.png` (shipped with no consumer in master), `firedish.png`, `quarrystones{1a,1b,2a,2b,3a,3b}.png`, `quarrybricks{1,2,3}.png` (FireDish, QuarryStones1 RandomTile), `rockhound.png`, `snapjaw miner.png`, `stonerat.png` (mole carvers) |
| Unreferenced | `pigvessel.png` (in master), `grinder_cell-export.png` (items). There is also an untracked `stoneandbees-Recovered.aseprite` in the working tree |

---

## 5. Stonecarver: calling versus NPCs

**The calling itself (TF1) needs:**

| Piece | Where | Status on items |
|---|---|---|
| Subtype | Callings.xml:4-26 | modified (+rep) |
| Tile | `stonecarver.png` | Both mods (see Â§4) |
| Starting gear table | PopulationTables.xml:3-20 `Cleo_StartingGear_Stonecarver` | modified (+MiningGear row :12) |
| Chisel and mallet | ObjectBlueprints.xml:57, :67 and `blunted_chisel.png` | unchanged; their EtchTool tags are inert without Etchable |
| Maquette table | PopulationTables.xml:29-45 | modified (+Random Stone Figurine at weight 50; table shared with Mason Kit) |
| Stone figurine | ObjectBlueprints.xml:257 | new; XML only (`RandomFigurine` is a base part) |
| MiningGear table | PopulationTables.xml:404-422 | new; mixed rows (A2-A4) |
| Guild faction for the +200 rep | Factions.xml | new |
| Geomancy "trade goods" merges (the calling's Geomancy tutorial) | Merges.xml:25-32 | unchanged |
| Skills and other gear | base game | |

The calling needs no C#.

**Only the warrens and NPCs need (TF2):**
- Creatures.xml: Stonecarver chassis (:140), Overseer, Hewer, Mason, Outrider, the three Mole variants, AnimateFigurine, Vine Merchant.
- Scripts: Builder_StonecarverEpithet.cs, Builder_AnimateFigurineIdentity.cs, Part_HasFigurines.cs, all Warren*.cs.
- XML: Naming.xml; Objects.xml NamingMaterials/NamingCrafts; Conversations Hewer/Mason/Outrider.
- PopulationTables: Stonecarver Pack, WarrenCrewÃ—3, the Quarrier/Hewer/Mason/Outrider Kits, Carver Knife/Hammer, VineMerchantWares, WarrenReclaimers, every Warren* table, the StoneBasket/FoodBasket contents.
- ObjectBlueprints: the warren furniture (QuarryStones, Cairn, baskets, Yard Board, Quarry Chest, FireDishCold, MossClump) and FissureStone.
- Texture: `stonerat.png`.

**Owned by neither, needed by both:** the Quarriers faction, Maquette, MiningGear, Random Stone Figurine, chisel and mallet (as rows in the Carver ladders), and `stonecarver.png`.

**On master, the calling had no TF2 dependencies:** there it was only the table, the chisel and mallet, the Maquette with copper and silver figurines only, and no rep. Three items-side calling edits created the coupling: the +rep, the MiningGear row, and the stone figurine in the Maquette.

---

## 6. Open questions

1. **Can TF2 run standalone at all?** Quarriers (B1-B3), the granite baboon (B4), the epithet builder (B5-B6), the fissure stone (B7) and the animation table (B8) all instantiate TF1 mutation classes. There are two options: TF2 declares a dependency on TF1, or the six mutation classes and their dependency chain become Shared, which is most of TF1. The decision's "TF2 supplies its own standalone copy" does not settle which.
2. **Calling kit on TF1:** drop the MiningGear row (and the stone figurine?) and go back to master's kit? Or keep a base-rows-only MiningGear in TF1 and let TF2 add its two rows through a `Load="Merge"`?
3. **Husangeion's parent:** keeping BaseBoarVessel in TF1 brings wild spawns (A8) and the corpse/cistern chain into the base mod. Alternatives are to revert the pet to master's standalone `Inherits="BaseAnimal"` shape, or to move the DynamicObjectsTable tags to a TF2 merge. Existing saves already hold the pet blueprint.
4. **Who owns the Quarriers faction** (never shipped)? TF1 would gain a faction whose members all live in TF2.
5. **Burrowing Urchin, Aculei and Urchin Spine** shipped in master, and the placement decision does not name them. Urchin Spine is referenced nowhere.
6. **StatueMaker** ("claysculpting loop", placeholder description, no spawn): is it a prototype asset (TF2@1.0) or TF1?
7. **Part_DazeOnThrowPens and the inert v1 code** (Part_FallenRock's DrainLatch/Enamored/SurgeField/FungalLottery/TimedForm, and the held Rocks.xml ring pieces): keep them in TF1 as compile and save-compat ballast, or trim them? Relatedly, master still ships the whole v1 catalog, Lithovore and the kitchen compiled. Is the next TF1 release cut from items (which drops them) or from master?
8. **Master-shipped TF2@1.0 files modified on items** (RockSweeper active seek, the Etchable additions, the snapjaw miner ConversationScript and conversation, the QuarriedBlock re-parent): during the additive beta, TF2 cannot redefine these classes or blueprints without duplicates. Do these changes ship in TF1 now, or wait for 1.0?
9. **The granite baboon and cobblefur gear** shipped in 0.1.2.2/0.1.2.3 and were pulled in 0.1.2.5. Existing saves may hold these blueprints and parts, which TF1 no longer defines.
10. **Duplicate classes and merges:** how the engine builds mod assemblies decides whether identical type names in the Shared folder collide when both mods are enabled, and whether a repeated `Load="Merge"` (Merges.xml Shared block, population merges) is idempotent. This needs checking against the ModManager decompile before the Shared-folder mechanism is chosen.
11. **Splitting the dev harness:** `_Wishes.cs` and `_TestHarness.cs` are mixed, and `_SerializationHarness.cs` depends on `_TestHarness.cs`. Should each mod carry its own harness, or is it all stripped at 1.0 with TF-VERIFY?
12. **Rock_Pebbles** (the Fissure spine, TF1) is used by TF2 warren dress and stone baskets. Make it Shared, or have TF2 switch to base `Small Stone`?

