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
