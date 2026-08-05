# Skills audit — 2026-08-04

**Status: everything in this document is CLAUDE PROPOSAL, UNRULED, unless a line carries its
own RULED/LEAN marker quoting the user.** Assignment (user's words, 2026-08-04): "do an audit
of skills, and examine the mechanics and synergies of terra firma, and see what they come up
with as far as a set." Both readings developed: (1) curated base-game set + calling-grant
review, (2) bespoke mod skill assessment.

**Verification basis:** base game `Skills.xml` read directly from the install
(`...\StreamingAssets\Base\Skills.xml`), mod behavior read from the mod's own scripts and XML,
not from model memory. Specific citations inline.

---

## 1. Base-skill audit: the contact map

The base game has 20 skill trees (plus the hidden `Nonlinearity` tree). Most have zero contact
with Terra Firma (Axe, Long Blade, Heavy Weapon, Pistol, Bow and Rifle*, Multiweapon,
Single Weapon Fighting, Physic*, Tinkering*, Cooking*, Persuasion* — asterisks get a line in
§2 for one specific interaction each). What follows is only the trees that touch the kit.

### 1.1 The thrown-weapon question (chased per the assignment)

**Finding: thrown attacks are almost entirely skill-free in the base game.** Verified against
Skills.xml: there is no dedicated Throwing tree in this build. The ONLY skill power that feeds
thrown attacks is **Tactics → Deft Throwing** (50 sp, Agi 10: "+3 throw range, throw variance
halved inside your range"). No skill improves thrown to-hit, crit, or penetration; accuracy is
raw Agility plus the item. Steady Hands/Steady Hand are bows-rifles/pistols only.

Mod-side verification of the pipeline:

- Earthen Barrage stones are genuine `ThrownWeapon` items auto-equipped to the real Thrown
  Weapon slot via the engine's own `ReplaceThrownWeaponEvent`
  (Mutation_EarthenBarrage.cs:249, :287). **Deft Throwing's range and variance bonuses apply
  to EB stones.**
- Geomancy's +2 range is a native event hook (`E.Range += 2`, Mutation_Geomancy.cs:72), so it
  **stacks additively with Deft Throwing: +5 total range** on every throw, mod rocks or not.
- Catalog rocks, ring salvage, and scavenged stones all ride the same slot and pipeline
  (Part_FallenRock.cs, Part_StoneScavenger.cs).

Consequence for both readings: the base game left throwing as a skill-light niche. A 50-sp
Tactics dip is the entire base-game ceiling for the kit's main ranged verb. That is both the
cheapest amplifier available (reading 1) and the largest open space a bespoke skill could
occupy (reading 2, with a scope caveat, see §3.2).

### 1.2 Cudgel (granted by the calling)

The grant mirrors the starting toolkit: the splintered mallet is a `BaseCudgel`
(ObjectBlueprints.xml:12) and the blunted chisel a `BaseDagger` (ShortBlades). Tool-as-weapon
identity, worth preserving.

- **Bludgeon / Conk (150 sp each, Str 17/21):** daze-stun stacking. Coherent with the kit's
  control identity (Fissure stun, Gorgon slow); a cudgel Stonecarver in melee keeps applying
  the same debuff family the mutations do.
- **Slam (150 sp, Str 25):** the sleeper. Verified text: pushes the opponent up to 3 spaces,
  "Opponents who are pushed through or against walls take extra weapon damage for each wall,"
  stun scaling with distance pushed, wall destroyed if its AV is under 5× Str mod. **The mod
  mass-produces walls.** See §2, find A.
- **Demolish (300 sp, Str 29, capstone):** 5 rounds of 100% daze + cooldown-free Slam. A
  late-game wall-crusher whose fantasy overlaps carve-as-attack Fissure; the calling can grow
  into "the earth moves when I hit things" from both the mutation and the skill side.
- Charging Strike requires `Tactics_Charge`, which pulls the build toward Tactics anyway
  (where Deft Throwing lives).

### 1.3 Self-discipline (granted) and the Willpower economy

**The calling's +2 Willpower is mechanically load-bearing three times over** (all verified in
mod code, not just flavor):

1. Willpower reduces all cooldowns (base-game global rule) — the kit is cooldown-heavy
   (Calcify 90, Gorgon 80, Stoneshaper 120, Fissure per-band).
2. EB reserve regen scales with WIL: `(WIL - 16) * 5` percent faster per point
   (Mutation_EarthenBarrage.cs:366).
3. Gorgon's save DC uses the caster's Willpower as the attacker stat
   (Mutation_GorgonsGaze.cs:209), the documented "double-dip accepted as build identity."

Discipline tree fit, power by power:

- **Meditate (0 sp, granted):** triple regen while the only legal action is Wait. This is the
  planted-feet fantasy already in skill form. Near-miss with the Gorgon channel: both are
  stand-still postures, but activating either is an action that ends the other. See §2, find D
  for a verify item on "free actions" and the channel.
- **Iron Mind / Lionheart (100 sp each, WIL 21):** shake off confusion/terror. These are the
  guards for a planted-feet character — fear and confusion are exactly the effects that force
  movement and break holds. Quietly the most on-identity purchases in the tree.
- Fasting Way / Mind over Body: economy powers, fine, not kit-relevant (rock-eating is held
  from beta anyway).
- Conatus: sprint extension, anti-thematic, skip.

### 1.4 Shield — the strongest un-granted amplifier

Soft-synergy frame (USER, 2026-08-04): standing still / holding ground is an accepted soft
synergy. The Shield tree is that synergy sold for sp:

- **Block** is passive and positional — it rewards being adjacent and staying put.
- **Staggering Block (200 sp, Str 21):** blocks can stun. Feeds the same control loop as
  Bludgeon and Fissure.
- **Shield Wall (200 sp, WIL 25):** 3 rounds of auto-block, and its attribute gate is
  **Willpower** — the one stat the calling already stacks. A shield-and-mallet Stonecarver
  standing in a redoubt gap is a coherent endgame picture: the ring is architecture, the
  shield is the door.
- Cost note: Deft/Swift Blocking want Agi 19, off-stat; the WIL/Str half of the tree is the
  Stonecarver half.

### 1.5 Tactics — cheap, and the anti-synergy line runs through it

Tree costs 50 sp; Deft Throwing another 50. That 100 sp is the single best sp-to-kit ratio
available (see §1.1). The rest of the tree splits cleanly along the soft-synergy line:

- WITH the kit: Deft Throwing; Kickback (Str 19, melee-range knockback on missile fire —
  gun/bow only by its text, so marginal here).
- AGAINST the kit: Hurdle, Charge, Juke are mobility powers; a build that leans on them is
  fighting the planted-feet identity (Charge is also the prereq tax for Charging Strike).
  They don't break anything — DV and movement powers still work while you happen to stand
  still — but sp spent there buys nothing the kit wants. Same verdict for all of Acrobatics
  (Jump, Tumble, Spry: fine, generically good, zero kit contact).

### 1.6 Endurance, Customs, Wayfaring (the survivability and granted-flavor shelf)

- **Endurance:** Shake It Off (0 sp, granted-if-bought-tree) shakes dazes/stuns; the kit
  doesn't self-stun, so this is generic defense. Calloused +1 AV, fine. No special contact.
- **Customs (granted):** Tactful is water-ritual reputation; zero mechanical contact with the
  kit. Its value is entirely flavor (a customs-and-folklore craftsman culture). Candidate for
  the swap fork in §3.1.
- **Survival_RuinsSurvival (granted):** overland ruins travel power. Base-game precedent for
  power-level grants exists (Arconaut grants exactly this), so the grant shape is idiomatic.
  `[verify]` whether the grant brings base Wayfaring (and thus Make Camp) along, or the power
  alone — matters for whether a fresh Stonecarver can even make a campfire. I did not find
  the answer in XML; it's engine grant semantics. One chargen boot answers it.

---

## 2. Synergy findings (the interactions nobody designed)

**A. Cudgel Slam vs the mod's walls — amplifier, both directions.** Slam deals extra damage
per wall the target is pushed through or against, and the mod is the game's premier wall
factory (Calcify line, redoubt rim, Stoneshaper-animatable figures). Raise a wall, Slam an
enemy into it: base-game skill, mod-made terrain, nobody designed the combo. The reverse edge
is also real: Slam "destroys a wall or door if its AV is under 5 times your Strength
modifier," so a strong cudgel wielder (player or NPC) can breach a Calcify wall or the
redoubt rim with a normal skill power. Not a bug — the rim already has gaps by design — but
worth knowing it exists as a counterplay channel, and that carve-as-attack's "granite holds"
boundary does not protect Calcify walls from Slam (different mechanic entirely,
`Part_CalcifyWall` scales HP/AV with level so high-level walls resist longer).

**B. Bow and Rifle's Flattening Fire reads the mod's terrain.** Verified text: Suppressive
Fire upgrades to Flattening Fire (adds prone + disarm) "as long as your target is surrounded
by 6 or more solid obstacles." The redoubt rim, Calcify walls, and Gorgon-made statues are
all solid obstacles. The mod can manufacture the upgrade condition on demand — a rifle-using
Stonecarver (or companion) gets a base-game power upgrade from mod terrain. Undesigned,
delightful, zero code owed.

**C. The statue is a real companion.** `Brain.BecomeCompanionOf` (Mutation_Stoneshaper.cs:495)
means Persuasion's Inspiring Presence ("your words embolden your companions") applies to an
animated statue, and Proselytize can grow the thunderdome's inside team. Off-stat (Ego) for
this calling, so a note rather than a recommendation.

**D. `[verify]` Free actions vs the Gorgon channel.** The channel breaks on any energy spend
whose type isn't a Pass (Mutation_GorgonsGaze.cs:152). Some things are free actions: Juke
moves "at no action cost," EB's auto-sculpt replacement is free, the scavenger re-ready is
free. If a free action spends no energy, it may not trip the break check — meaning Juke could
micro-reposition a channeling gorgon one cell without breaking the gaze. Either a lovely
piece of tech or a bug, and only a live test says which. Flagging, not proposing a fix.

**E. Meditate and the channel are sibling postures that can't stack.** Both say "the only
thing you may do is wait"; activating either ends the other. No action owed — but if reading
2 ever produces anything, "the stillness family" (Meditate, the gaze, the redoubt cast) is
the identity seam a bespoke power would naturally live on.

**F. Tinkering's Lay Mine is the ghost of Fissure v2.** The grenade-minefield identity the
pivot removed still exists in the base game for anyone who wants it: rock grenades (held from
beta) + Lay Mine reconstructs v2's play pattern skill-side, post-beta. Worth remembering when
the held grenade table's future is decided — the engine already sells the minefield fantasy,
which strengthens the case that redoubt-Fissure was right to stop competing for it.

---

## 3. The proposed set(s)

### 3.1 Reading 1 — curated base-game set

**Calling grant list.** Current: Cudgel, ShortBlades, Customs, Discipline,
Survival_RuinsSurvival, +2 WIL. Verdict: **four of five grants are correct and load-bearing**
(Cudgel/ShortBlades mirror the mallet/chisel toolkit; Discipline is the WIL identity; the
travel power is idiomatic Arconaut-shaped). One fork to rule:

- **FORK 1 (the only grant change proposed): swap Customs for Tactics.** CLAUDE PROPOSAL,
  UNRULED. For: Tactics is where Deft Throwing lives, throwing is the kit's primary ranged
  verb, and the tree is the cheapest in the game (50 sp) so granting it is a small mechanical
  gift that says "this calling throws rocks." Against: Customs is the culture grant — a
  folklore-keeping stonecarver people is a lore statement the user may value above mechanics,
  and text is the user's domain. A third option exists (grant both, six skills), but no base
  calling in memory is that generous — I did not verify base subtype grant counts, so treat
  the generosity ceiling as unverified. **Rule one: keep Customs / swap to Tactics / grant
  both.**

**Buy order for a Stonecarver run** (CLAUDE PROPOSAL, UNRULED; assumes Customs kept, Tactics
bought):

1. **Tactics (50) + Deft Throwing (50)** — first 100 sp. +3 range and halved variance on
   every EB stone and catalog rock; stacks with Geomancy to +5. Nothing else in the game
   touches the throwing verb.
2. **Bludgeon (150)** — the melee control loop opens.
3. **Shield tree (100) + Shield Slam (100)** — hold-the-gap identity begins; blocks work
   while channeling nothing.
4. **Iron Mind + Lionheart (100 each)** as WIL passes 21 — planted-feet insurance against
   the two effect families that force movement.
5. **Staggering Block (200, Str 21) → Conk (150)** — the stun engine matures.
6. **Shield Wall (200, WIL 25)** — the calling's stat pays off in the Shield tree, of all
   places.
7. Endgame, Str permitting: **Slam (150) → Demolish (300)** — walls become weapons (find A),
   and the character earns the wall-crusher fantasy the skill tree sells.

Anti-buys (sp that fights the kit): Charge/Juke/Hurdle, all of Acrobatics, Sprint extenders.
Not forbidden — just paying for a playstyle the six mutations don't reward.

### 3.2 Reading 2 — a bespoke skill set: honest assessment first

**The assessment the prompt asked for: a full bespoke skill tree is not worth the debt right
now.** Three grounds:

1. **The gearset is winning the "where does a verb live" fork on merit** (USER LEAN
   2026-08-02: gloves/boots/puller/socket armor, "new verbs have been landing gear-side,
   deliberately"). Gear absorbs verbs at near-zero systemic cost: no chargen surface, no
   sp-economy balance, droppable, tier-able, tradeable to the future stonecarver merchants.
   Every candidate verb I audited has a plausible gear home already sketched.
2. **The strongest mechanical opening is not Terra Firma's to fill.** §1.1 shows the real gap
   is a thrown-weapons skill tree — but a public tree benefits every thrower in Qud, which
   makes it a base-game rebalance wearing a mod's clothes. If built, it changes javelin
   builds that never touch a mutation. That's scope the mod shouldn't want.
3. **Active stone form, checked against the evidence standard the user set:** the burden was
   to find base-game precedent for transformation-like skill powers. Verified: none exists in
   Skills.xml — the closest things are stances (Long Blade) and timed buffs (Berserk,
   Demolish, Shield Wall). **The audit affirms the user's hunch (skill: no; gear: maybe).**
   No reopen argument submitted.

**What the audit DOES surface as viable, if the user wants any skill-side presence at all:**
the base game ships a working precedent for a **hidden, non-chargen skill tree** —
`Nonlinearity` (`Hidden="true"`, Skills.xml:285), a one-power tree granted by gameplay rather
than offered publicly. That mechanism fits one specific need the gearset genuinely can't
host: **calling-exclusive craft verbs that should cost sp, not mutation points and not an
equipment slot.** Sketch (CLAUDE PROPOSAL, UNRULED — named powers are illustrative, all text
is draft-until-signoff and the names are the user's to replace):

- Tree: **Stonecraft** (hidden; granted by the Stonecarver calling only; Attribute:
  Willpower).
- Power 1 (0 sp, the freebie every tree gets): chisel-and-mallet work — reshape a held rock
  to fit a socket (slower cracking in the socket armor, the jam's "calling as best wearer"
  hook, currently homeless because it's neither gear nor mutation).
- Power 2 (~150 sp, WIL 21): carve a commemoration into natural stone or a statue (the
  deferred "geology that responds" thread's cheapest on-ramp; pure flavor object, no combat
  surface).
- Deliberately NOT in the sketch: seismic sense (Geomancy owns sonar; a skill copy would
  cannibalize a mutation), anything thrown-combat (that's Deft Throwing's job or the public-
  tree scope problem), any rider on existing mutations (rider-density concern, USER
  2026-08-04).
- Debt statement, honestly: even this costs a tree, two powers, text, tiles, and a grant-path
  test, to deliver flavor verbs. It is strictly post-beta, and "do nothing skill-side" is a
  fully respectable ruling — the calling already reads as complete through its grants.

### 3.3 What this changes for the beta

Nothing. No code, no XML, no grant edits proposed for the friends beta — FORK 1 touches
chargen and would need its own verify pass; everything in 3.2 is post-beta by its own
argument. This document is design input only.

---

## 4. Forks for the user (one at a time, in suggested order)

1. **Grant swap (§3.1 FORK 1):** keep Customs / swap for Tactics / grant both. My lean, for
   what it's worth: swap — but the culture statement is yours to weigh, and lore outranks a
   50-sp discount.
2. **Skill-side presence at all (§3.2):** none (gearset carries everything) / hidden
   Stonecraft tree post-1.0 / public thrown tree (I recommend against the third on scope).
3. **Find A counterplay:** are you happy that Cudgel Slam can breach Calcify walls and the
   redoubt rim under the AV threshold? No change proposed; asking whether it needs a feel
   check at the same session as the redoubt dials.
4. **Verify items to fold into the next boot list** (cheap, all read-only): Juke vs the gaze
   channel (find D) · does the RuinsSurvival grant include Make Camp (§1.6) · Flattening
   Fire against a redoubt-ringed target, just to watch it happen once (find B).
