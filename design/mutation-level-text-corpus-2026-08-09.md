# Base-game mutation text corpus (pulled 2026-08-09, decomp 2.0.211.50 via ilspycmd)

Every mutation's GetDescription + GetLevelText, flattened from the decompiled classes,
roster and order from the game's `Mutations.xml`. Pulled for the **Fissure desc pass
v3** sitting; the house mutations' current text sits at the bottom for side-by-side.

**Notation** (artifacts of flattening code into text — the game renders these resolved):

- `{expr}` — a code expression (usually a number or dice string at render time).
- `[cond ? «A» : «B»]` — a branch; the game shows one side. The common one is the
  level-preview idiom: `Level != base.Level` means "you are previewing a different
  rank than your current one," and vanilla swaps prose for terse
  `{{rules|Increased ...}}` delta lines there.
- Known flattening warts, verified harmless: builder-chain conditions are dropped
  (conditional lines print unconditionally here); Quills carries two stray glyph
  escapes; Horns' variant plumbing leaks a few tokens. Nothing else hand-edited —
  what you read is what the assembly says.

## Conventions observed (CLAUDE ANALYSIS, for the pass — not rulings)

- **Descriptions**: second person, present tense, ONE sentence, zero numbers, zero
  mechanics vocabulary ("You invoke a concussive force in a nearby area, throwing
  enemies back and stunning them."). Our current Fissure line already obeys this.
- **Level text is a stat block, not prose**: `Label: value` lines with numbers wrapped
  in `{{rules|...}}`; behavior lines lead with a bare verb ("Emits a...", "Creates 9
  contiguous squares...", "Releases gas..."); restriction and reputation lines come
  last ("Cannot wear helmets"); cooldown is a bare `Cooldown: N rounds` line.
- **THE parallel for the pulse mechanic — Stunning Force**: its behavior line is
  "Creatures are pushed away from center of blast, stunned, and dealt crushing damage
  in up to 3 increments." with a separate `Damage increment: {{rules|...}}` stat line.
  The verb-cousin already ships the exact "up to 3 hits" grammar the churn needs; the
  question for the sitting is whether "increment" fits when our three dice differ.
- **The level-preview idiom we don't use**: Stunning Force and Horns swap their prose
  line for `{{rules|Increased stun save difficulty}}`-style delta lines when previewing
  a non-current rank. Adoptable for Fissure (dice + stun rounds both climb); would be
  new machinery in GetLevelText (the `Level != base.Level` check).

## Fissure v3 candidates (CLAUDE DRAFTS for the sitting — react, don't adopt)

All keep the ruled silences silent (engulf catch, pinning, eviction, flyers, loot,
moved-only stun nuance) and the signed description line unchanged. No em dashes.

**A — Stunning Force's shape, adapted:**

    Ring radius: 4 squares
    Creatures are churned outward and dealt crushing damage in up to 3 pulses, strongest first.
    Pulse damage: {{rules|2d3/1d3/1d3}} per penetration
    Creatures moved by the churn must make a Toughness save or be stunned for {{rules|1}} round.
    Each pulse batters the walls it reaches.
    Cooldown: 100 rounds

**B — the current deployed draft (spread spelled out):**

    Ring radius: 4 squares
    Damage: {{rules|2d3}}, then {{rules|1d3}}, then {{rules|1d3}} as the churn spreads to 1, 2, and 3 squares
    Creatures caught in its path are pushed outward and must make a Toughness save or be stunned for {{rules|1}} round.
    Each pulse batters the walls it reaches.
    Cooldown: 100 rounds

**C — tersest legal stat block (mechanics fully implied):**

    Ring radius: 4 squares
    Damage: {{rules|2d3/1d3/1d3}} in three spreading pulses
    Creatures caught in its path are pushed outward and must make a Toughness save or be stunned for {{rules|1}} round.
    Cooldown: 100 rounds

Drafts are shown as L1 renders; the rules-wrapped numbers ride the existing getters.

Open questions the corpus sharpens: (1) how to print the PV, if at all — vanilla has
three precedents: Stinger's inline weapon-stat glyph pair `(♦PV ♥dmg)`, Light
Manipulation's bare `Laser penetration:` label line (and our own Earthen Barrage's
`Penetration bonus:` follows it), or Burrowing Claws' scoped `penetration vs. walls:`;
(2) "pulse" vs Stunning Force's "increment" vs spelling out 1/2/3 squares; (3) adopt
the level-preview delta idiom (`{{rules|Increased ...}}` lines) or stay static.

---

## The base-game corpus

### Chimera  —  Morphotypes (`Chimera`)

**Description:** You only manifest physical mutations, and all of your mutation choices when manifesting a new mutation are physical.

Whenever you manifest a new mutation, one of your choices will also cause you to grow a new limb at random.

**Level text:** *(none)*

### Esper  —  Morphotypes (`Esper`)

**Description:** You only manifest mental mutations, and all of your mutation choices when manifesting a new mutation are mental.

**Level text:** *(none)*

### Unstable Genome  —  Morphotypes (`UnstableGenome`)

**Description:** You gain one extra mutation each time you buy this, but the mutations don't manifest right away.
Whenever you gain a level, there's a 33% chance that your genome destabilizes and you get to choose from 3 random mutations.

**Level text:** *(none)*

### Adrenal Control  —  Physical (`AdrenalControl2`)

**Description:** You regulate your body's release of adrenaline.

**Level text:**

```
You can increase your body's adrenaline flow for {GetQuicknessDuration(Level)} rounds.
While it's flowing, you gain +{{C|{GetQuicknessBonus(Level)}}} quickness and other physical mutations gain +{{C|{GetMutationBonus(Level)}}} rank.
Cooldown: {GetCooldown(Level)} rounds
```

### Beak  —  Physical (`Beak`)

**Description:** *(none)*

**Level text:** *(none)*

### Burrowing Claws  —  Physical (`BurrowingClaws`)

**Description:** {Blueprint.GetTag("VariantDescription").Coalesce("You bear spade-like claws that can burrow through the earth.")}

**Level text:**

```
{cachedDisplayNameStrippedTitleCase} penetration vs. walls: {{rules|{wallBonusPenetration.Signed()}}}
{value} destroy walls after {wallHitsRequired} penetrating hits.
Can dig passages up or down when outside of combat
{value} are also a {GetWeaponClass()} class natural weapon that deal {{rules|{GetClawsDamage(Level)}}} base damage to non-walls.{Event.FinalizeString(stringBuilder)}
```

### Carapace  —  Physical (`Carapace`)

**Description:** {Blueprint.GetTag("VariantDescription").Coalesce("You are protected by a durable carapace.")}

**Level text:**

```
AV
 DV
 Heat Resistance
 Cold Resistance
You may tighten your {cachedDisplayNameStripped} to receive double the AV bonus at a -2 DV penalty as long as you remain still.
Cannot wear body armor.{Event.FinalizeString(stringBuilder)}
```

### Corrosive Gas Generation  —  Physical (`CorrosiveGasGeneration`, text inherited from `GasGeneration`)

**Description:** {Description}

**Level text:**

```
Releases gas for {{rules|{GetReleaseDuration(Level)}}} rounds
```

### Double-muscled  —  Physical (`HeightenedStrength`)

**Description:** You are possessed of hulking strength.

**Level text:**

```
+{{C|{GetStrengthBonus(Level)}}} Strength
{{C|{GetDazedChance(Level)}%}} chance to daze your opponent on a successful melee attack for 2-3 rounds
```

### Electrical Generation  —  Physical (`ElectricalGeneration`)

**Description:** You accrue electrical charge that you can use and discharge to deal damage.

**Level text:**

```
Maximum charge: {{C|{GetMaxCharge(Level)}}}
Accrue base {{C|{GetBaseChargePerTurn(Level, BaseChargePerTurnPercent)}}} charge per turn
Can discharge all held charge for 1d4 damage per {1000} charge
Discharge can arc to adjacent targets dealing reduced damage, up to 1 target per {1000} charge
EMP causes involuntary discharge (difficulty 18 Willpower save)
You can drink change from electrical power sources.
You can drink charge from energy cells and capacitors.
You gain {100} charge per point of electrical damage taken.
You can provide charge to equipped devices that have integrated power systems.{stringBuilder.ToString()}
```

### Electromagnetic Pulse  —  Physical (`ElectromagneticPulse`)

**Description:** You generate an electromagnetic pulse that disables nearby artifacts and machines.

**Level text:**

```
Area: {{rules|{num}x{num}}} centered around yourself
Duration: {{rules|{(4 + Level * 2)}-{(13 + Level * 2)}}} rounds
Cooldown: {GetCooldown(Level)} rounds
```

### Flaming Ray  —  Physical (`FlamingRay`)

**Description:** *(none)*

**Level text:**

```
Emits a {rANGE}-square ray of flame in the direction of your choice.
Damage: {{rules|{ComputeDamage(level)}}}
Cooldown: 10 rounds
Melee attacks heat opponents by {{rules|{GetHeatOnHitAmount(level)}}} degrees
```

### Freezing Ray  —  Physical (`FreezingRay`)

**Description:** *(none)*

**Level text:**

```
Emits a {rANGE}-square ray of frost in the direction of your choice.
Damage: {{rules|{ComputeDamage(Level)}}}
Cooldown: 20 rounds
Melee attacks cool opponents by {{rules|{GetCoolOnHitAmount(Level)}}} degrees
```

### Heightened Hearing  —  Physical (`HeightenedHearing`)

**Description:** You are possessed of unnaturally acute hearing.

**Level text:**

```
You detect the presence of creatures within a radius of {{rules|{GetRadius(Level)}}}.
```

### Heightened Quickness  —  Physical (`HeightenedSpeed`)

**Description:** You are gifted with tremendous speed.

**Level text:**

```
+{{rules|{GetSpeedBonus(Level)}}} Quickness
```

### Horns  —  Physical (`Horns`)

**Description:** {blueprint.GetPropertyOrTag("Gender")}

**Level text:**

```
20% chance on melee attack to gore your opponent
Damage increment: {{rules|{baseDamage}}}
To-hit bonus: {{rules|{HornsProperties.GetToHitBonus(Level)}}}
[(Level == base.Level) ? «Goring attacks may cause bleeding
» : «[(Level % 4 != 1) ? «{{rules|Increased bleeding save difficulty}}
» : «{{rules|Increased bleeding save difficulty and intensity}}
»]»]plural{blueprint.GetPropertyOrTag("Gender")}+{{rules|{aV} AV}}
Cannot wear helmets
+100 reputation with {{w|antelopes}} and {{w|goatfolk}}
```

### Metamorphosis  —  Physical (`Metamorphosis`)

**Description:** You assume the form of any creature you touch.

**Level text:**

```
May only assume the form of creatures level {GetMaxLevel(Level)} or lower.
Cooldown: {GetCooldown(Level).ToString()} rounds
```

### Multiple Arms  —  Physical (`MultipleArms`)

**Description:** You have an extra set of arms.

**Level text:**

```
{{rules|{GetAttackChance(Level)}%}} chance for each extra arm to deliver an additional melee attack whenever you make a melee attack
```

### Multiple Legs  —  Physical (`MultipleLegs`)

**Description:** You have an extra set of legs.

**Level text:**

```
+{{rules|{GetMoveSpeedBonus(Level)}}} move speed
+{{rules|{GetCarryCapacityBonus(Level).ToString()}%}} carry capacity
```

### Night Vision  —  Physical (`DarkVision`)

**Description:** You see in the dark.

**Level text:** *(none)*

### Phasing  —  Physical (`Phasing`)

**Description:** You may phase through solid objects for brief periods of time.

**Level text:**

```
Duration: {{rules|{GetDuration(Level)}}} rounds
Cooldown: {{rules|{GetBaseCooldown(Level).ToString()}}} rounds
```

### Photosynthetic Skin  —  Physical (`PhotosyntheticSkin`)

**Description:** You replenish yourself by absorbing sunlight through your hearty green skin.

**Level text:**

```
You can bask in the sunlight instead of eating a meal to gain a special metabolizing effect for {{rules|{GetBonusDurationString(Level)}}}: +{{rules|{GetBonusRegeneration(Level)}%}} to natural healing rate and +{{rules|{GetBonusQuickness(Level)}}} Quickness
While in the sunlight, you accrue starch and lignin that you can use as ingredients in meals you cook (max {{rules|{GetStarchServings(Level)}}} of each).
+{{rules|{GetBonusCamouflage(Level)}}} DV while occupying the same space as foliage
+200 reputation with {{w|roots}}, {{w|trees}}, {{w|vines}}, and {{w|the Consortium of Phyta}}
```

### Quills  —  Physical (`Quills`)

**Description:** {Blueprint.GetTag("VariantDescription").Coalesce("Hundreds of needle-pointed quills cover your body.")}

**Level text:**

```
{{rules|{nMaxQuills}}} {objectName}{'\n'}+{{rules|80-120}} {objectName}{'\n'}May expel 10% of your {objectName} in a burst around yourself ({{c|\u001a}}{{rules|{value}}} {{r|\u0003}}1d3)
Regenerate {objectName} at the approximate rate of {{rules|{(float)Level / 4f}}} per round
+{{rules|{GetAV(Level)}}} AV as long as you retain half your {objectName} (+{{rules|{GetAV(Level) - aVPenalty}}} AV otherwise)
Creatures attacking you in melee may impale themselves on your {objectName}, breaking roughly 1% of them and reflecting 3% damage per {ObjectNameSingular} broken.
Cannot wear body armor
Immune to other creatures' {objectName}{Event.FinalizeString(stringBuilder)}
```

### Regeneration  —  Physical (`Regeneration`)

**Description:** Your wounds heal very quickly.

**Level text:**

```
Your full natural healing rate applies in combat.
+{{rules|{(int)(100f * GetRegenerationBonus(Level))}%}} faster natural healing rate
{{rules|{GetRegenerationChance(Level)}%}} chance to regrow a missing limb each round
```

### Sleep Gas Generation  —  Physical (`SleepGasGeneration`, text inherited from `GasGeneration`)

**Description:** {Description}

**Level text:**

```
Releases gas for {{rules|{GetReleaseDuration(Level)}}} rounds
```

### Slime Glands  —  Physical (`SlimeGlands`)

**Description:** You produce a viscous slime that you can spit at things.

Covers an area with slime
Range: 8
Area: 3x3
Cooldown: 40 rounds
You can walk over slime without slipping.

**Level text:** *(none)*

### Spinnerets  —  Physical (`Spinnerets`)

**Description:** You can spin sticky silk webs.

**Level text:**

```
{GetDuration(Level)}}} move actions{stringBuilder.ToString()}
```

### Stinger (Confusing Venom)  —  Physical (`Stinger`)

**Description:** {StingerProperties.GetDescription()}

**Level text:**

```
20% chance on melee attack to sting your opponent ({{c|\u001a}}{{rules|{GetPenetration(Level)}{RuleSettings.VISUAL_PENETRATION_BONUS}}} {{r|\u0003}}{{rules|{GetDamage(Level)}}})
Stinger is a long blade and can only penetrate once.
Always sting on charge or lunge.
Stinger applies venom on damage (only 20% chance if Stinger is your primary weapon).
May use Sting activated ability to strike with your stinger and automatically hit and penetrate.
Sting cooldown: {GetCooldown(Level)}{'\n'}+200 reputation with {{w|arachnids}}{Event.FinalizeString(stringBuilder)}
```

### Stinger (Paralyzing Venom)  —  Physical (`Stinger`)

**Description:** {StingerProperties.GetDescription()}

**Level text:**

```
20% chance on melee attack to sting your opponent ({{c|\u001a}}{{rules|{GetPenetration(Level)}{RuleSettings.VISUAL_PENETRATION_BONUS}}} {{r|\u0003}}{{rules|{GetDamage(Level)}}})
Stinger is a long blade and can only penetrate once.
Always sting on charge or lunge.
Stinger applies venom on damage (only 20% chance if Stinger is your primary weapon).
May use Sting activated ability to strike with your stinger and automatically hit and penetrate.
Sting cooldown: {GetCooldown(Level)}{'\n'}+200 reputation with {{w|arachnids}}{Event.FinalizeString(stringBuilder)}
```

### Stinger (Poisoning Venom)  —  Physical (`Stinger`)

**Description:** {StingerProperties.GetDescription()}

**Level text:**

```
20% chance on melee attack to sting your opponent ({{c|\u001a}}{{rules|{GetPenetration(Level)}{RuleSettings.VISUAL_PENETRATION_BONUS}}} {{r|\u0003}}{{rules|{GetDamage(Level)}}})
Stinger is a long blade and can only penetrate once.
Always sting on charge or lunge.
Stinger applies venom on damage (only 20% chance if Stinger is your primary weapon).
May use Sting activated ability to strike with your stinger and automatically hit and penetrate.
Sting cooldown: {GetCooldown(Level)}{'\n'}+200 reputation with {{w|arachnids}}{Event.FinalizeString(stringBuilder)}
```

### Thick Fur  —  Physical (`ThickFur`)

**Description:** You are covered in a thick coat of fur, which protects you from the elements.

+5 Heat Resistance
+5 Cold Resistance
+100 reputation with {{w|apes}}, {{w|baboons}}, {{w|bears}}, and {{w|grazing hedonists}}

**Level text:** *(none)*

### Triple-jointed  —  Physical (`HeightenedAgility`)

**Description:** Your joints stretch much further than usual.

**Level text:**

```
+{{rules|{(2 + (Level - 1) / 2)}}} Agility
{{rules|{GetCooldownCancelChance(Level).ToString()}%}} chance that Sprint and skills with Agility prerequisites don't go on cooldown after use
```

### Two-headed  —  Physical (`TwoHeaded`)

**Description:** You have two heads.

**Level text:**

```
Mental actions have {{rules|{GetReducedMentalActionCost(Level)}%}} lower action costs
{{rules|{GetShakeOff(Level).ToString()}%}} chance initially and each round to shake off a negative mental status effect
```

### Two-hearted  —  Physical (`TwoHearted`)

**Description:** You have two hearts.

**Level text:**

```
+{{rules|{2}{(Level - 1) / 2}}} Toughness
You can sprint for {{rules|{GetSprintBonus(Level)}%}} longer.{stringBuilder.ToString()}
```

### Wings  —  Physical (`Wings`)

**Description:** {Blueprint.GetTag("VariantDescription").Coalesce("You fly.")}

**Level text:**

```
You travel on the world map at {{rules|{1.5}{0.5 * (double)Level}x}} speed.
{{rules|{(36 + Level * 4)}%}} reduced chance of becoming lost
While outside, you may fly. You cannot be hit in melee by grounded creatures while flying.
{{rules|{num}%}} chance of falling clumsily to the ground
{{rules|{((int)(SprintingMoveSpeedBonus(Level) * 100f)).Signed()}%}} move speed while sprinting
You can jump {{rules|{GetJumpDistanceBonus(Level)}[(GetJumpDistanceBonus(Level) == 1) ? «}} square» : «}} squares»] farther.
You can charge {{rules|{GetChargeDistanceBonus(Level)}[(GetChargeDistanceBonus(Level) == 1) ? «}} square» : «}} squares»] farther.
+300 reputation with {{w|birds}} and {{w|winged mammals}}{stringBuilder.ToString()}
```

### Albino  —  PhysicalDefects (`Albino`)

**Description:** Your skin, hair, and eyes are absent of pigment.

You regenerate hit points at one-fifth the usual rate in the daylight.

**Level text:** *(none)*

### Amphibious  —  PhysicalDefects (`Amphibious`)

**Description:** Your skin must be kept moist with fresh water.

You pour water on yourself rather than drinking it to quench your thirst.
You require about two-thirds more water than usual.
+100 reputation with {{w|frogs}}

**Level text:** *(none)*

### Brittle Bones  —  PhysicalDefects (`BrittleBones`)

**Description:** Your bones are brittle.

You suffer 50% more damage from bludgeoning attacks, falling, and other sources of concussive damage.

**Level text:** *(none)*

### Nerve Poppy  —  PhysicalDefects (`Analgesia`)

**Description:** You lack a developed sense of pain.

You only know your general state of health and not your precise number of hit points.

**Level text:** *(none)*

### Carnivorous  —  PhysicalDefects (`Carnivorous`)

**Description:** You eat meat exclusively.

You get no satiation from foods that aren't meat.
If you eat raw food that isn't meat, there's a 50% chance you become ill for 2 hours.
You can't cook with plant or fungus ingredients.
You don't get ill when you eat raw meat.
You can eat raw meat without being famished.


**Level text:** *(none)*

### Cold-Blooded  —  PhysicalDefects (`ColdBlooded`)

**Description:** Your vitality depends on your temperature

**Level text:** *(none)*

### Electromagnetic Impulse  —  PhysicalDefects (`ElectromagneticImpulse`)

**Description:** You involuntarily release electromagnetic pulses, deactivating robots and artifacts around yourself.

Small chance each round you're in combat that you release an electromagnetic pulse with radius 3, deactivating robots and artifacts (including those you carry) for 11-20 rounds.

**Level text:** *(none)*

### Hooks for Feet  —  PhysicalDefects (`HooksForFeet`)

**Description:** {Blueprint.GetTag("VariantDescription")}

**Level text:** *(none)*

### Irritable Genome  —  PhysicalDefects (`IrritableGenome`)

**Description:** Your genome is irritable and unpredictable.

Whenever you spend a mutation point, the next mutation point you gain will be spent randomly.
Whenever you buy a new mutation, you get a random one instead of a choice of three.

**Level text:** *(none)*

### Myopic  —  PhysicalDefects (`Myopia`)

**Description:** You are nearsighted.

You can only see up to a radius of 10.

**Level text:** *(none)*

### Spontaneous Combustion  —  PhysicalDefects (`SpontaneousCombustion`)

**Description:** You spontaneously erupt into flames.

Small chance each round you're in combat that you spontaneously erupt into flames.

**Level text:** *(none)*

### Tonic Allergy  —  PhysicalDefects (`TonicAllergy`)

**Description:** You are allergic to tonics.

The chance your mutant physiology reacts adversely to a tonic is increased to 33%.
If you react adversely this way to a salve or ubernostrum tonic, the adverse reaction effect is chosen randomly from among other tonic effects. You will still heal.

**Level text:** *(none)*

### Beguiling  —  Mental (`Beguiling`)

**Description:** You beguile a nearby creature into serving you loyally.

**Level text:**

```
Mental attack versus a creature with a mind
Success roll: {{rules|mutation rank}} or Ego mod (whichever is higher) + character level + 1d8 VS. Defender MA + character level
Range: 1
Beguiled creature: +{{rules|{(Level * 5).ToString()}}} bonus hit points
Cooldown: 50 rounds
```

### Burgeoning  —  Mental (`Burgeoning`)

**Description:** You cause plants to spontaneously grow in a nearby area, hindering your enemies.

**Level text:**

```
Range: 8
Area: 3x3 + growth into adjacent tiles
Cooldown: {{rules|{num}}} rounds
```

### Clairvoyance  —  Mental (`Clairvoyance`)

*(no text methods anywhere in the class chain)*

### Confusion  —  Mental (`Confusion`)

**Description:** You confuse nearby enemies.

**Level text:**

```
Affected creatures act semi-randomly and receive a {{rules|-{GetMentalPenalty(Level)}}} penalty to their mental abilities.
Cone angle: {{rules|{GetConeAngle(Level).ToString()}}} degrees
Cone length: {{rules|{GetConeLength(Level).ToString()}}}
Duration: {{rules|{GetDuration(Level)}}} rounds
Cooldown: {GetCooldown(Level).ToString()} rounds
```

### Cryokinesis  —  Mental (`Cryokinesis`)

**Description:** You chill a nearby area with your mind.

**Level text:**

```
[(Level == base.Level) ? «Chills affected area over {Duration.Things("round")}, dealing damage and freezing things
» : «[(Level <= base.Level) ? «{{rules|Decreased chill temperature intensity}}
» : «{{rules|Increased chill temperature intensity}}
»]»]Range: {Range}
Area: {num}x{num}
```

### Disintegration  —  Mental (`Disintegration`)

**Description:** You disintegrate nearby matter.

**Level text:**

```
Area: 7x7 around self
Damage to non-structural objects: {{rules|{GetNonStructuralDamage(Level)}}}
Damage to structural objects: {{rules|{GetStructuralDamage(Level)}}}
You are exhausted for 3 rounds after using this power.
Cooldown: {GetCooldown(Level).ToString()} rounds
```

### Domination  —  Mental (`Domination`)

**Description:** You garrote an adjacent creature's mind and control its actions while your own body lies dormant.

**Level text:**

```
Mental attack versus creature with a mind
Success roll: {{rules|mutation rank}} or Ego mod (whichever is higher) + character level + 1d8 VS. Defender MA + character level
Range: 1
Duration: {{rules|{GetDuration(Level).ToString()}}} rounds
Cooldown: 75 rounds
```

### Ego Projection  —  Mental (`WillForce`)

**Description:** Through sheer force of will, you perform uncanny physical feats.

**Level text:**

```
Augments one physical attribute by an amount equal to twice your Ego bonus
Duration: {{rules|{GetLowDuration(Level)}-{GetHighDuration(Level)}}} rounds
Cooldown: 200 rounds
```

### Force Bubble  —  Mental (`ForceBubble`)

**Description:** You generate a forcefield around yourself.

**Level text:**

```
Creates a 3x3 forcefield centered on yourself
Duration: {{rules|{GetDuration(Level)}}} rounds
Cooldown: 100 rounds
You may fire missile weapons through the forcefield.
```

### Force Wall  —  Mental (`ForceWall`)

**Description:** You generate a wall of force that protects you from your enemies.

**Level text:**

```
Creates 9 contiguous squares of immobile forcefield.
Duration: {{rules|{GetDuration(Level)}}} rounds
Cooldown: {cooldown.ToString()} rounds
You may fire missile weapons through the forcefield.
```

### Kindle  —  Mental (`Kindle`)

**Description:** You ignite a small fire with your mind.

Range: 12
Cooldown: 50

**Level text:** *(none)*

### Light Manipulation  —  Mental (`LightManipulation`)

**Description:** You manipulate light to your advantage.

**Level text:**

```
You produce ambient light within a radius of {{rules|{GetMaxLightRadius(Level)}}}.
You may focus the light into a laser beam, temporarily reducing the radius of your ambient light by 1.
Laser damage increment: {{rules|{GetDamage(Level)}}}
Laser penetration: {{rules|{(GetLasePenetrationBonus(Level) + RuleSettings.VISUAL_PENETRATION_BONUS).ToString()}}}
Ambient light recharges at a rate of 1 unit every {GetRadiusRegrowthTurns().ToString()} rounds until it reaches its maximum value.
{{rules|{GetReflectChance(Level).ToString()}%}} chance to reflect light-based damage
```

### Mass Mind  —  Mental (`MassMind`)

**Description:** You tap into the aggregate mind and steal power from other espers.

**Level text:**

```
Refreshes all mental mutations
Cooldown: {{rules|{GetCooldown(Level)}}} rounds
Cooldown is not affected by Willpower.
Each use attracts slightly more attention from psychic interlopers.
[(Level != base.Level) ? «{{rules|Decreased chance for another esper to steal your powers}}
» : «{{rules|Small chance each round for another esper to steal your powers}}
»]-200 reputation with {{w|the Seekers of the Sightless Way}}
```

### Mental Mirror  —  Mental (`MentalMirror`)

**Description:** You reflect mental attacks back at your attackers.

**Level text:**

```
When you suffer a mental attack while Mental Mirror is off cooldown, you gain +{{rules|{GetMABonus(Level)}}} mental armor (MA).
If the attack then fails to penetrate your MA, it's reflected back at your attacker.
Cooldown: {GetCooldown(Level).ToString()}
```

### Precognition  —  Mental (`Precognition`)

**Description:** You peer into your near future.

**Level text:**

```
You may activate this power and then later revert to the point in time when you activated it.
Duration between use and reversion: {{rules|{GetDuration(Level)}}} rounds
Cooldown: {GetCooldown(Level).ToString()} rounds
```

### Psychometry  —  Mental (`Psychometry`)

**Description:** You read the history of artifacts by touching them, learning what they do and how they were made.

**Level text:**

```
{GetIdentifiableComplexity(Level)}}}.{GetLearnableComplexity(Level)}}} (must have the appropriate Tinker skill).{stringBuilder.ToString()}
```

### Pyrokinesis  —  Mental (`Pyrokinesis`)

**Description:** You heat a nearby area with your mind.

**Level text:**

```
[(Level == base.Level) ? «Toasts affected area over {Duration.Things("round")}
» : «[(Level <= base.Level) ? «{{rules|Decreased toast temperature intensity}}
» : «{{rules|Increased toast temperature intensity}}
»]»]Range: {Range}
Area: {num}x{num}
```

### Sense Psychic  —  Mental (`SensePsychic`)

**Description:** You can sense other mental mutants through the psychic aether.

You detect the presence of psychic enemies within a radius of {Radius.ToString()}.
There's a chance you identify detected enemies.

**Level text:** *(none)*

### Spacetime Vortex  —  Mental (`SpacetimeVortex`)

**Description:** You sunder spacetime, sending things nearby careening through a tear in the cosmic fabric.

**Level text:**

```
Summons a vortex that swallows everything in its path.
You may enter the vortex to teleport to a random location in Qud.
+200 reputation with {{w|highly entropic beings}}
```

### Stunning Force  —  Mental (`StunningForce`)

**Description:** You invoke a concussive force in a nearby area, throwing enemies back and stunning them.

**Level text:**

```
Range: {rANGE}
Area: 7x7
[(Level != base.Level) ? «{{rules|Increased stun save difficulty}}
» : «Creatures are pushed away from center of blast, stunned, and dealt crushing damage in up to 3 increments.
»]Damage increment: {{rules|{GetDamageIncrement(Level)}}}
Cooldown: 50 rounds
```

### Sunder Mind  —  Mental (`SunderMind`)

**Description:** You sunder the mind of an enemy, leaving them reeling in pain.

**Level text:**

```
For up to 10 rounds, you engage in psychic combat with an opponent, dealing damage each round.
Taking any action other than passing the turn will break the connection.
Each round you make a mental attack vs mental armor (MA).
Damage increment: {{rules|{GetDamageDice(Level)}}}
After the tenth round, you deal bonus damage equal to the total amount of damage you've done so far.
Range: sight
Cooldown: {GetCooldown(Level).ToString()} rounds
```

### Syphon Vim  —  Mental (`LifeDrain`)

**Description:** You bond with a nearby organic creature and leech its life force.

**Level text:**

```
Mental attack versus an organic creature
Drains {{rules|{Level}}} hit [(Level == 1) ? «point» : «points»] per round
Target gets a mental save to resist damage each round
Duration: 20 rounds
Cooldown: 200 rounds
```

### Telepathy  —  Mental (`Telepathy`)

**Description:** You may communicate with others through the psychic aether.

Chat with anyone in vision
Takes you much less time to issue orders to companions

**Level text:** *(none)*

### Teleportation  —  Mental (`Teleportation`)

**Description:** You teleport to a nearby location.

**Level text:**

```
Teleport to a random location within a designated area.
Uncertainty radius: {{rules|{GetRadius(Level)}}}
Cooldown: {{rules|{GetCooldown(Level).ToString()}}} rounds
```

### Teleport Other  —  Mental (`TeleportOther`)

**Description:** You teleport an adjacent creature to a random nearby location.

**Level text:**

```
Cooldown: {{rules|{GetCooldownTurns(Level)}}} rounds
```

### Time Dilation  —  Mental (`TimeDilation`)

**Description:** You distort time around your person in order to slow down your enemies.

**Level text:**

```
Creatures within {Range} tiles are slowed according to how close they are to you.
Distance 1: creatures receive a {{rules|{((int)(CalculateQuicknessPenaltyMultiplier(1.0, Range, Level) * 100.0)).ToString()}%}} Quickness penalty
Distance 4: creatures receive a {{rules|{((int)(CalculateQuicknessPenaltyMultiplier(4.0, Range, Level) * 100.0)).ToString()}%}} Quickness penalty
Distance 7: creatures receive a {{rules|{((int)(CalculateQuicknessPenaltyMultiplier(7.0, Range, Level) * 100.0)).ToString()}%}} Quickness penalty
Duration: 15 rounds
Cooldown: {GetCooldown(Level).ToString()} rounds
```

### Temporal Fugue  —  Mental (`TemporalFugue`)

**Description:** You quickly pass back and forth through time creating multiple copies of yourself.

**Level text:**

```
Duration: {{rules|{GetTemporalFugueDuration(Level)}}} rounds
Copies: {{rules|{GetTemporalFugueCopies(Level).ToString()}}}
Cooldown: {{rules|{GetCooldown(Level).ToString()}}} rounds
```

### Amnesia  —  MentalDefects (`Amnesia`)

**Description:** You forget things and places.

Whenever you learn a new secret, there's a small chance you forget a secret.
Whenever you return to a map you previously visited, there's a small chance you forget the layout.

**Level text:** *(none)*

### Blinking Tic  —  MentalDefects (`BlinkingTic`)

**Description:** You teleport about uncontrollably.

Small chance each round you're in combat that you randomly teleport to a nearby location.

**Level text:** *(none)*

### Dystechnia  —  MentalDefects (`Dystechnia`)

**Description:** You are befuddled by technological complexity.

You're much worse at examining artifacts.
You can't have artifacts identified for you because you don't understand their explanations.
When you fail severely during artifact examination, the artifact explodes.


**Level text:** *(none)*

### Evil Twin  —  MentalDefects (`EvilTwin`)

**Description:** Acting on some inscrutable impulse, a parallel version of yourself travels through space and time to destroy you.

Each time you embark on a new location, there's a small chance your evil twin has tracked you there and attempts to kill you.

**Level text:** *(none)*

### Narcolepsy  —  MentalDefects (`Narcolepsy`)

**Description:** You fall asleep involuntarily from time to time.

Small chance each round you're in combat that you fall asleep for 20-29 rounds.

**Level text:** *(none)*

### Psionic Migraines  —  MentalDefects (`PsionicMigraines`)

**Description:** You suffer from powerful psionic migraines that render your head extremely sensitive.

You can't wear hats or helmets.

**Level text:** *(none)*

### Quantum Jitters  —  MentalDefects (`QuantumJitters`)

**Description:** Your willful acts sometimes dent spacetime.

Whenever you use an activated ability, there's a small chance your focus slips and you dent spacetime in the local region, causing 1-2 spacetime vortices to appear. This chance increases the longer you go without using an activated ability.

**Level text:** *(none)*

### Socially Repugnant  —  MentalDefects (`SociallyRepugnant`)

**Description:** Others find it difficult to tolerate you in social settings.

-50 reputation with every faction

**Level text:** *(none)*


## House mutations (current text, from the mod sources)

### Gorgon's Gaze (`Mutation_GorgonsGaze.cs`)

**Description:** Your unblinking gaze stops creatures in their tracks.

**Level text:**

```
You transfix an opponent with your unblinking gaze and slowly turn them to stone.
Taking any action other than passing the turn will break your gaze.
Each round your target makes a Toughness save. Each failure advances petrification by one stage. Each success reverses one stage.
Save difficulty: {{rules|{GetSaveDifficulty(Level)}}} + your Willpower modifier, +1 per failed save
Each failed save holds your target in place and applies -{QUICKNESS_LOSS_PER_SAVE} Quickness.
Three successful saves break the gaze.
Targets that fail {NUM_OF_SAVES} saves are permanently turned to stone.
Targets that escape after {STONE_MARK_MIN_FAILS} or more failures are left partially petrified for {Cleo_TerraFirma_PartiallyPetrified.DURATION} rounds.
Partial petrification: -{Cleo_TerraFirma_PartiallyPetrified.MOVE_SPEED_PENALTY} move speed, -{Cleo_TerraFirma_PartiallyPetrified.QUICKNESS_PENALTY} Quickness, +{Cleo_TerraFirma_PartiallyPetrified.AV_BONUS} AV
Range: sight
Cooldown: {BASE_COOLDOWN} rounds
```

### Earthen Barrage (`Mutation_EarthenBarrage.cs`)

**Description:** You form hefty rocks suitable for throwing.

**Level text:**

```
You sculpt a stone every {{rules|{GetTurnsPerCharge()}}} rounds, up to a reserve of {{rules|{MaxCharges(Level)}}}.
Sculpted stones occupy your thrown weapon slot.
Stones not held as a thrown weapon crumble to dust after {ROCK_LIFETIME_MIN}-{ROCK_LIFETIME_MAX} rounds.
Damage increment: {{rules|{GetThrowDamage(Level)}}}
Penetration bonus: {{rules|+{GetThrowPenetrations(Level)}}}
Stones in reserve whorl about you and deflect physical projectiles.
Lasers and other energy attacks pass through the whorl.
Deflect chance: 3% per stone in reserve, +1% per mutation level[Level >= RESONANT_UNLOCK_LEVEL ? «
You may attune your stones to {{C|resonance}}.
Resonant stones' penetration value is equal to the defender's armor value.
While attuned, your whorl deflects light instead of matter.» : «
At level {RESONANT_UNLOCK_LEVEL}, you learn to attune your stones to {{C|resonance}}.»]
```

### Stoneshaper (`Mutation_Stoneshaper.cs`)

**Description:** You animate earth to fight for you.

**Level text:**

```
You can gaze at a creature, then at a wall, to carve an animate statue of that creature.
The statue inherits the creature's body plan and physique (Strength, Agility, Toughness).
It does not inherit mutations, skills, or equipment.
Nearby enemies must make a Willpower save (difficulty {{rules|{GetDistractionSaveDifficulty(Level)}}}) or direct their attacks at the statue.
Can target {{rules|[wt == WallType.Stone ? «stone» : «[wt == WallType.StoneAndMetal ? «stone and metal» : «almost any»]»] walls}}
Statue hit points: {{rules|{GetHP(Level)}}}
Statue AV: {{rules|{GetAV(Level)}}}
Statue damage increment: {{rules|{GetNaturalDamage(Level)}}}
Statue to-hit bonus: +{{rules|{GetToHitBonus(Level)}}}
[duration == INFINITE_DURATION_PLACEHOLDER ? «The statue lasts {{rules|until destroyed}}. Carving another releases it.
» : «The statue lasts for {{rules|{duration}}} rounds.
»]Cooldown: {BASE_COOLDOWN} rounds
```

### Geomancy (`Mutation_Geomancy.cs`)

**Description:** You sense minute details through earthly vibrations.

Stones you throw reveal the area around their point of impact.
Power equals the stone's sell value or weight, whichever is higher.
Vision radius: 1 tile per {(int)POWER_PER_TILE} power (minimum {(int)MIN_RADIUS_TILES})
Vision duration: 1 round per {(int)POWER_PER_TURN} power (minimum {(int)MIN_DURATION_TURNS})
Gemstones also apply full scanning to creatures and walls in the revealed area.
+2 thrown weapon range

**Level text:** *(none)*

### Calcify (`Mutation_Calcify.cs`)

**Description:** You raise walls of solid stone.

**Level text:**

```
Creates a contiguous line of permanent walls in empty tiles starting adjacent to you
Number created: {{rules|{GetWallsCreated(Level)}}}
Walls are true copies of the local stone
Cooldown: {{rules|{GetCooldown(Level)}}} rounds
```

### Fissure (current DRAFT) (`Mutation_Fissure.cs`)

**Description:** You send a wave through the earth, raising a ring of broken stone.

**Level text:**

```
Ring radius: {GetRingRadius(Level)} squares
Damage: {{rules|{GetPulseDice(Level, 1)}}}, then {{rules|{GetPulseDice(Level, 2)}}}, then {{rules|{GetPulseDice(Level, 3)}}} as the churn spreads to 1, 2, and 3 squares
Creatures caught in its path are pushed outward and must make a Toughness save or be stunned for {{rules|{GetStunRounds(Level)}}} [GetStunRounds(Level) == 1 ? «round» : «rounds»].
Each pulse batters the walls it reaches.
Cooldown: {GetCooldown(Level)} rounds
```

### Lithovore (`Mutation_Lithovore.cs`)

**Description:** You eat stone exclusively.

Some stones have additional effects when eaten.
You can't cook with plant or meat ingredients.

**Level text:** *(none)*

