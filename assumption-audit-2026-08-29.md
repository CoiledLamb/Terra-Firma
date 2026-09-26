# Engine-assumption audit, all six mutations (2026-08-29)

**CLAUDE FINDINGS, UNRULED.** Six parallel auditors, one per mutation, each reading the mod C#
in full and verifying every engine-behavior assumption against the decompiled Assembly-CSharp
(current Steam build) and shipped Base XML. Every VERIFIED below has a citation the auditor
actually read; nothing was marked verified from inference. Fix decisions are the user's.

Tally: ~130 assumptions checked. 6 live defects, 7 wrong-but-currently-harmless maps,
6 unverified edges, the rest verified with citations (full per-mutation tables preserved in
the session transcript; the load-bearing ones are summarized here).

## Live defects (would bite in play), worst first

1. **Stoneshaper: statue weapon buffs wiped on body-structure change.**
   `Mutation_Stoneshaper.cs:610-627` customizes DefaultBehavior fist instances in place
   (formula BaseDamage, Slot, muse tile/name). `Body.RegenerateDefaultEquipment()` obliterates
   and recreates any instance whose `Blueprint == DefaultBehaviorBlueprint`, and it runs on
   dismemberment/regrowth (`Body.UpdateBodyParts`), implant, and unimplant. First limb the
   statue loses, every fist reverts to blueprint `1d3+1`. Same failure class as the TakenEvent
   regression. Fix direction: re-apply via `RegenerateDefaultEquipmentEvent` /
   `DecorateDefaultEquipmentEvent` (the engine's own hook, used by Horns), or a per-statue
   blueprint. A `TemporaryDefaultBehavior` guard does NOT help; that tag also triggers replacement.

2. **Stoneshaper: carving is a free action.** Neither `HandleEvent(CommandEvent)` nor
   `CarveFrom` calls `UseEnergy`. The engine charges nothing for ability commands
   (`CommandEvent.Send` / `ActivatedAbilities` contain no energy code; every base mutation
   charges itself, e.g. BurrowingClaws `UseEnergy(1000, ...)`). Player and NPC shapers cast and
   still act the same turn; only the 120-turn cooldown limits it.

3. **Gorgon's Gaze: death-veto leaves the victim permanently drop-less.**
   `Mutation_GorgonsGaze.cs:413-417` hands loot back when `Die()` returns false but never clears
   `SuppressCorpseDrops=1`. That property is persistent and gates corpse (Corpse part), inventory
   (Inventory.cs:2648), AND equipped gear (BodyPart.cs:3586) on every future death. Engine's own
   unwind idiom: `Consumer.cs:106/128` treats it as a counter,
   `ModIntProperty("SuppressCorpseDrops", -1, RemoveIfZero: true)`. One-line fix in the else branch.

4. **Earthen Barrage: dead guard, possible retrieve/refund loop.**
   `Body.GetUnequippedPart("Thrown Weapon")` never returns null (always allocates a list; null
   only if no Body part). The `thrownSlot == null` guards at `Mutation_EarthenBarrage.cs:450-457`
   and `:547` are dead. With no usable empty slot: CanGetStone passes, charge spent, AutoEquip
   silently fails, stone lands in inventory, end-of-turn reabsorb refunds it; message loop every
   turn the toggle is on. Fix: check `.Count == 0` (or `GetUnequippedPartCount`).

5. **Calcify: NPC wall line can cross zone edges; repaint uses wrong rectangle.**
   `GetCellFromDirection` walks into any live adjacent zone (`GetCellFromDirectionGlobal`,
   `bLocalOnly: false`); the anchor is guarded (`Mutation_Calcify.cs:157`) but the flank walk is
   not, so an NPC cast at a zone edge places walls next door (base ForceWall does the same walk,
   so placement is base-blessed). The bite: `Mutation_Calcify.cs:311` then feeds the other zone's
   local X/Y into `ZoneManager.PaintWalls` on the CASTER's zone; wrong rectangle repainted.
   `Fx_StoneRise.PlayWallRise` coordinates also wrong there, mitigated by the `IsVisible()` gate.

6. **Fissure: rim eviction ignores `DirectMoveTo`'s return.** `Mutation_Fissure.cs:1121-1135`.
   `Physics.ProcessTargetedMove` fails silently for a non-player mover against a non-hostile
   occupant (slam damage already dealt, "bounced clear" log line lies, victim stays on rim) and
   stacks the mover in against all-hostile occupants (engine-legal, Push-chain parity). The
   `!StillHoldsCombatant` recheck downgrades to debris, so nothing crashes or vanishes; contained.

7. **Stoneshaper: `Trifling` argument is a silent no-op.**
   `Brain.BecomeCompanionOf(GameObject, bool Trifling, bool Involuntary)` never reads Trifling
   (body is `SetAlliedLeader<AllyDefault>` + `StopFight`). Temporary and permanent statues get
   identical companion status; `Mutation_Stoneshaper.cs:685` encodes a distinction that does not exist.

## Wrong maps, currently harmless (comments/deprecations)

- **`CombatJuice.cameraShake(x)` takes a DURATION in seconds, not a magnitude.** Three sightings:
  `Mutation_Calcify.cs:231`, `Mutation_Fissure.cs:553-554`, `Fx_StoneRise.cs` ShakeTimes. The
  ruled "three descending shakes" survives because shorter reads as subtler; the dial's model
  is wrong. Citation: `CombatJuice.cameraShake` -> `CameraShake.shakeDuration += duration`.
- **Geomancy: AfterThrownEvent is not exclusive to PerformThrow.** `Part_GeomancyStone.cs:9-10`
  doc comment. `GameObject.WasThrown` also fires from MissileWeapon projectile landing
  (MissileWeapon.cs:3457). No base launcher fires stones today; zero current impact.
- **Earthen Barrage: "same formula as Light Manipulation" is not live-true.** The copied math is
  verbatim (`LightManipulation.cs:440-454`) but base gates it on
  `GlobalConfig.GetBoolSetting("LightManipulationWillpowerRecharge")`, default false, no
  GlobalConfig.json ships. Live base regrowth is flat 15; the mod always scales. Behavior fine,
  precedent claim wrong.
- **Stoneshaper: anatomies DO carry DefaultBehaviors.** `Mutation_Stoneshaper.cs:592-595` comment
  is wrong (`AnatomyPart.DefaultBehavior` exists, `Bodies.xml:10` Hand -> DefaultFist). Code
  outcome unaffected; the two loops handle both cases.
- **Fissure: `idmatch` is `[Obsolete]`, removal flagged "after Q1 2024".** `Mutation_Fissure.cs:976`.
  One-line rename to `IDMatch`.
- **Geomancy: Write/Read skips the mod's own SERIAL_SENTINEL convention** documented at
  `_Helpers.cs:36-44`. Mod-internal doc/code mismatch, not an engine error.
- **Earthen Barrage: dead `CheckExistenceSupportEvent` registration** (`Mutation_EarthenBarrage.cs:241`,
  no handler); **Gorgon's Gaze: dead `EndTurnEvent` registration** (`Mutation_GorgonsGaze.cs:109-118`).

## Unverified edges (left open, not claimed either way)

- **Stoneshaper cross-zone statues:** sculptor's EndTurn ticks a remote statue's lifetime;
  freeze/thaw semantics of the held reference unaudited; `IsInGraveyard/IsNowhere` guard does
  not cover the frozen-zone case.
- **Lovesick muse is a DEEP COPY**, not the live creature (`Lovesick.Apply` copies Beauty, with
  effects and ID, including our StatueCopyGen property). Carving it works; copy path unplayed.
- **Fissure liquid-pool relocation** (`Mutation_Fissure.cs:455-499`): no base precedent moves a
  pool via `RemoveFromContext` + `AddObject`; re-settle behavior untraced. Worst case cosmetic.
  Stays log-confirmed-until-seen.
- **Earthen Barrage ReplaceThrownWeaponEvent window:** returning true lets the engine also scan
  inventory for a same-blueprint stone and contest the just-filled slot; transient, no repro run.
- **Gorgon's Gaze `Duration + 1` display fudge** remains unexplained (mechanical shift is
  correct; likely a countdown-vs-EndTurn phase offset).
- **Calcify comment numbers** (Stunning Force flat 50, Burgeoning/Teleport Other/Belcher ladders)
  unchecked; comment-only, no code depends on them.

## Notable clean bills

- **Fissure's PV pipeline is fully engine-true post-fix**: mechanical PV has no +4 anywhere
  (`RuleSettings.VISUAL_PENETRATION_BONUS` is display-only), `RollDamagePenetrations` fed
  `Stat("AV")` exactly as Combat.cs:1087-1144 does, damage rolled once per pen, MakeSave
  argument shape byte-identical to StunningForce's own call.
- **Earthen Barrage thrown PV** likewise has zero +4 exposure; resonant/vibro stones verified
  end to end (`VibroWeapon` sets Vorpal; pen = defender AV). Engine floors thrown StrMod at 1,
  so level text slightly under-promises for negative-Str throwers.
- **Gorgon's Gaze channel/pin**: verbatim SunderMind + MemberOfPsychicBattle + Stuck idioms,
  entomb-before-Die ordering confirmed load-bearing (the flag kills all three drop paths).
- **Calcify**: ExistenceSupport inverted contract, fugue-clone support, ForceWall AI recipe all
  cited; ShaleVines post-build `GrowVines()` requirement confirmed real.
- **Geomancy**: Clairvoyance save idiom byte-for-byte; particle front's ascending-delay ordering
  confirmed load-bearing (`ParticleManager.Render` stops at first pending delay).

---

# Fleet two: non-mutation C# (same sitting, six lanes)

**CLAUDE FINDINGS, UNRULED. No fixes applied; all held for ruling.** Same method: every engine
assumption verified against the decompile + Base XML, citations read, not inferred. Full per-lane
tables in the session transcript; the actionable core below.

## Live defects, worst first

1. **`Part_FallenRock.cs` corrupts the engine's adjacency cache, four sites** (:614, :754, :787,
   :968). `Cell.GetLocalAdjacentCells()` returns `_LocalAdjacentCellCache` BY REFERENCE and the
   payload code calls `RemoveAll` on it, permanently deleting then-solid neighbors from the
   cell's cache for the life of the zone. `Part_GrinderCell.cs:135-137` documents and fixes this
   exact class ("the 2026-08-23 reach=9/10 log lines were that cache growing"); FallenRock never
   got the copy. Fix: `new List<Cell>(...)` at all four sites. SHIPPED code.
2. **Enamored never actually ends** (`Effect_Enamored.cs:52-58`). `SetAlliedLeader` takes
   allegiance AND sets a non-transient PartyLeader (adds to PartyMembers, Brain.cs:892-951);
   `Remove` only pops the allegiance stack, so the victim stays a follower after expiry. Base
   precedent for the missing step: `Beguiled.Remove` nulls `PartyLeader` and clears `Goals`
   before removing the allegiance (Beguiled.cs:103-115).
3. **Streakschorl NeverMiss is conditional on the THROWER** (`Part_FallenRock.cs:83-87, :228).
   `GetThrowProfileEvent.Process` dispatches to the object only when `Actor.WantEvent` passes;
   nothing base-side wants it except Telekinesis, so live verification passed only because
   Geomancy wants it on the caster. NPCs and no-Geomancy players get no lend silently. Related:
   the Kill goal fires the event with a real TargetCell during throw CONSIDERATION (Kill.cs:120),
   so a declined throw can leave the +100 accuracy lend armed.
4. **Cobblefur/granitefur gloves: the catch is undone by the engine** (`Part_CobblefurGloves.cs`
   :57-83). After `E.Done`, PerformThrow unconditionally re-places the weapon (AddObject clears
   InInventory/Equipped), dumping the caught rock at the wearer's feet; if it stack-merged the
   original dies and the catch sticks (nondeterministic). Grenades survive only via the
   CaughtGrenade detonation-cancel rescue. TF2-BOUND (items branch; family pulled from master
   in 0.1.2.5), fix before TF2.
5. **Stonecarver epithet perks: `BoostStat` is PERCENTAGE, not additive**
   (`Builder_StonecarverEpithet.cs:149-162`). `BoostStat(x)` = `BaseValue += ceil(Base*0.25*x)`.
   So DV/Heat/Cold/Electric perks on zero-base template stats are SILENT NO-OPS, and "+2 Ego"
   on base ~16 is actually ~+8. The calls mirror GoatfolkHero1 exactly; the additive reading in
   comments/DescribePerk is what's false. Also `SetIntProperty("Hero")`/`Role` do not make the
   engine read it as legendary (rep hooks key on GivesRep; Role reprices XP only in
   FinalizeStats, which runs BEFORE builders). TF2-BOUND, unshipped.
6. **DrainLatch runs on two desynced clocks** (`Part_DrainLatch.cs:139` vs `:173-186`). The
   stone's `RoundsLeft` ticks per world turn; the LifeDrain effect ticks per victim ACTION, so
   hasted victims outrun the stone and slowed ones drain past it. Also the ctor's `10` is
   LEVEL, feeding a per-turn resist contest (LifeDrain.cs:193): the drain is contested, not
   guaranteed, and it self-terminates early if the drainer dies.
7. **Grinder cell pauses while socketed in a device** (`Part_GrinderCell.cs:225-257`).
   EndTurnEvent's cascade (352) lacks the SLOTS bit and `EnergyCellSocket` forwards only
   TurnTick, so "grinds anywhere" excludes installed. Design call: document the quirk or move
   the trickle to `TurnTick`.
8. **Rock sweeper never sweeps its own cell** (`Part_RockSweeper.cs:103`). Adjacency excludes
   self and "EnteredCell" fires only on the hound's own movement, so a stone arriving under the
   hound sits until it re-enters.

## Caveats and wrong maps, currently harmless

- **Warren settled-rumor erasure leaves the `worldInfo.friendlySettlements` pointer**
  (`WarrenMaker.cs:67-87`): a settled warren can still be issued as a dynamic quest destination;
  null-safe, no crash.
- **Warren water mask** (`WarrenCarver.cs:243`): `HasObjectWithPart("LiquidVolume")` also matches
  ground-placed liquid CONTAINERS; engine idiom is `HasOpenLiquidVolume()`. Population builders
  run before the warren, so a table-placed container can poison the mask. Low odds, real.
- **Moss harvest color revert** (`WarrenCarver.cs:3315-3327`): `Harvestable` restores Unripe
  channels only when non-empty; a wall with a null original channel keeps the moss tint after
  harvest. Shale is safe (all three channels set).
- **Rebuild determinism comment** (`WarrenCarver.cs:3362`): the ambient-RNG fear is over-cautious;
  `ZoneBuilderCollection.ApplyTo` reseeds per builder with NO attempt counter, so builder rolls
  replay identically per rebuild. Flip side, verified: a deterministic connection-stranding
  augment would infinite-loop into the "zone isn't building properly" dialog, and
  ForceConnections will bore repair tunnels through warren interior (walls cost only 80).
- **Gloves catch nuance** aside, the throw-event family (WeaponThrowHit before damage,
  ThrownProjectileHit after, AfterThrown skipped when destroyed mid-flight) all verified.
- **Enamored/DrainLatch aside, effects countdown contracts verified** (brained = per action,
  brainless = per turn, 9999 special-cased).
- **_TestHarness "HP finalized from Toughness at zone entry" has NO engine citation**
  (finalization happens at factory creation) and the harness contradicts itself (tfcrack and
  statueai set HP pre-AddObject with no guard). Self-guarded by the WARN probe; stop citing the
  mechanism as fact.
- Minor: Seismophone restores the IPoweredPart profile one flag short (`IsBootSensitive`);
  GlowstoneLantern uses ObjectCreatedEvent where the cited idiom is AfterObjectCreated
  (equivalent in practice); `SplitFromStack() ?? x` fallbacks are dead code; chalk box's
  "Destroy leaves a corpse in inventory" belt guards a veto-only path and would obliterate
  through the veto; ChalkBox EnsureGrant runs twice per pickup (idempotent); WeatherMouth's
  75/35 wobble numbers are the mod's own, not CanyonBuilder's; `WarrenMaker.cs:22` "carved AT
  WORLDGEN" comment stale under blind-claim.

## Clean bills

- **Chalk/etching lane: zero WRONG verdicts** across all five files; Sticky/Graffitied/Tattoos/
  Chair/TattooGun/MarkovBook precedents verbatim; Grammar.Obfuscate tier-6 skip verified clause
  by clause; the external-registration save/load rebind (ExternalEventBind) verified.
- **Infrastructure lane: everything verified except the HP-at-zone-entry ghost above.** Wish
  regex machinery, cache-init attributes, quicksave round-trip, Paralyzed energy-zero idiom,
  Kill.cs:506 / Brain.cs:512 / ActionManager.cs:743 / Body.cs:2427 citations all still land
  exactly; two comment line-numbers drifted (GameObject.cs:15333/15389 -> ~15345/15400).
- **Worldgen plumbing: the whole builder chain verified** — extension discovery, priority
  ordering (mid 4500 / post 5000 / warren 6000), builder param reflection, TunnelTo cost
  parity with Redrock's own figures, population table machinery, journal/secret APIs.
- **Creature lane leans on no TakenEvent-on-unequip myth anywhere** (the old regression class);
  scavenger/sweeper use CommandTakeObject, the safe channel. Thrown-slot equip being a free
  action: engine-default, matching the ruling.

---

# REACHABILITY CORRECTION (USER CATCH 2026-08-30: "is enamored for some of the fissure/lithovore rocks? all of those are quarantined")

The fleet-two "shipped, fix first" tier was WRONG about reachability. Ground-truthed against
shipped `Rocks.xml` + TODO-quarantine.md:

- `Effect_Enamored` fires only from `Part_FallenRock.cs:1007`, the `TempAlly` payload = the
  LOVESTONE, v1 catalog, quarantined 2026-08-11.
- The four adjacency-cache corruption sites sit in `SpawnByproducts` (:614), `Tar` (:754),
  `SpawnObject` (:787), `FugueClones` (:968) = byproduct rocks / tanglelith / spiderweb stone /
  blurring rock, all catalog. Shipped `Rocks.xml` wires exactly ONE payload (`FallThrough`,
  line 226) and sets no Byproduct die anywhere, so the corrupting paths are DORMANT in shipped
  play. The bugs are real in the code but nothing reaches them until the catalog reopens.
- Streakschorl (NeverMiss thrower-gate) and drainstone (DrainLatch clocks): both catalog,
  quarantined. The quarantine tracker's own live-verify notes (line 156) even show the NPC
  streakschorl test passing via a Geomancy-wanting thrower, consistent with the gate finding.

Corrected tiers: fleet two found ZERO defects reachable in shipped play today. Everything lands
in either the QUARANTINE lane (fix when the v1 catalog reopens: cache corruption x4, Enamored
party leak, streakschorl gate, drainstone clocks) or the TF2 lane (gloves catch, epithet
BoostStat math, warren caveats). The grinder-cell socket pause and sweeper own-cell blind spot
ride their items' own status. The mutation fixes from fleet one are unaffected; those were and
are shipped-live.
