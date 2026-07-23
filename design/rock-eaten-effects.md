# Rock eaten-effects — draft table (2026-07-22)

Companion to the **Rock-eater defect** section in TODO.md. Direction ruled by user 2026-07-22:
**reuse base cooking effects wherever possible** (soupstone = weak hydration, sludgestone = weak
slime, chunk of salt = always tasty). Everything below is a **Claude draft for the ruling pass** —
per-rock assignments, the section a rock lands in, and all magnitudes are open until ruled.

## RULING ROUND 2 (2026-07-22) — raw-eat model re-based on base raw-ingredient eating

User directive: *"check whatever happens when a player eats a basic cooking ingredient raw, and
work from your understanding off that."* Findings (Foods.xml + decomp, verified):

- **Base preserved ingredients ARE raw-edible and do NOTHING** — `Preservable` carries
  `Food Satiation="None"`: pickles eaten raw give zero satiation, zero effect. An ingredient's
  magic lives only in the pot.
- **When base wants a raw-eat effect it uses a dedicated `*OnEat` part — twenty exist:**
  `EffectOnEat` (generic named-effect), `FearOnEat` (Dreadroot Tuber, `Strength=3d8 Duration=4d6`
  — **the magnitude register for raw eats**), `PoisonOnEat`, `ConfuseOnEat`, `TeleportOnEat`,
  `TemperatureOnEat`, `BreatheOnEat`, `StatOnEat`, `HealOnEat`/`GeometricHealOnEat`,
  `MutationInfectionOnEat`, `SecretsOnEat`, `TongueOnEat`, `JumpOnEat`,
  `RefreshAllCooldownsOnEat`/`RefreshCooldownsOnEat`, `MPOnEat`, `MutationPointsOnEat`,
  `PermuteMutationBuysOnEat`, `SifrahInsightOnEat`.

**Consequences — these supersede the mechanism notes further down:**
1. Eaten-effects = **stock OnEat parts on rock blueprints, snack-grade dice, immediate** — NOT
   meal-grade domain units lasting until next hunger. Sub-fork 1 (strength dial) dissolves; the
   old domain-unit unification idea is dead.
2. The defect's eat channel **replays Food's own event sequence on the rock** ("OnEat"/"Eating"/
   "Eaten" + AddFood + stack-destroy) so stock OnEat parts fire natively. Rocks carry no live
   `Food` part — nobody without the defect can eat them. Per-part event registration verified at
   build.
3. **Pure-XML mappings now available:** fungal four → `MutationInfectionOnEat` · gossamarl →
   `TongueOnEat` · whorlstone → `ConfuseOnEat` · farflint → `TeleportOnEat` · pyroclase/frostspar
   → `TemperatureOnEat` · radioactive twin → `PoisonOnEat` (+ Mutating via `EffectOnEat`) ·
   grimslate → `FearOnEat` (Dreadroot's own part — the fear rock scares your gut; honest, replaces
   the +MA inversion) · mullstone → `RefreshAllCooldownsOnEat` (the inversion survives) ·
   `SecretsOnEat` on the shelf for anything geomancy-adjacent · `BreatheOnEat` strengthens the
   burp family.
4. The identity assignments in sections A/B/D below **stand as drafts**; exact part choices and
   dice get re-drafted to the OnEat register at build. Rocks with no entry = plain satiation
   (already more generous than base's Satiation="None").

**Also recorded this round:** twins pot-gamble **RULED ships** ("bit cute") · tanglelith /
compressed ash / shivshale stay **domainless in pass 1** (user: the goal was never every rock on
default options — this is the lean layer to iterate on before bespoke parts) · grind fork gains a
fact for the user's consideration: base preserves are `Weight="0"` — grinding heavy rocks into
weightless powders is the base pattern verbatim · grenade-in-pot deferred to user's own verify ·
**liquids remain legal cooking partners for lithovores** (meal-policy note) · **statue-eating
RULED as concept** — famished lithovores eat statues the way carnivores eat corpses; details in
TODO.

## Engine ground truth (decompiled 2026-07-22, session scratchpad `decomp/`)

- **`ProceduralCookingEffect.CreateSpecific(units, triggers, actions)` is a public static factory
  taking effect-class name strings** (resolved via `ModManager.ResolveType("XRL.World.Effects." + name)`
  — mod classes resolve too). One call wraps any base cooking unit into an applicable Effect; units
  carry their own description text ("@thisCreature thirst@s at half rate.").
- **Duration is free:** both `ProceduralCookingEffect` and the `BasicCookingEffect_*` (tasty) family
  natively expire on `BecameHungry`/`BecameFamished`/`ApplyWellFed`/`ClearFoodEffects` — an
  eaten-effect lasts *until you next get hungry*, exactly like a cooked meal, zero duration code.
  Fire `ClearFoodEffects` at the top of each rock-eat (Campfire's own sequence) → **one rock buff
  at a time, and eating a rock replaces a meal buff and vice versa.** Coherent and self-balancing.
- **Wiring:** per-rock blueprint tag (draft: `Cleo_TerraFirma_EatenUnits="Class,Class"` +
  `EatenSpecial="..."` for the dangerous bespokes) read by the defect part's eat channel. The whole
  table is XML data; bespoke C# only where the dangerous section says so.
- `Campfire.RollTasty` is `The.Player`-hardcoded — for the salt rule, apply the
  `RandomTastyEffect` equivalent (random pick of the 7 `BasicCookingEffect_*`) directly to the
  eater instead (NPC-safe).
- Mutation-grant units (`ProceduralCookingEffectUnitMutation<T>`) handle grant-if-absent /
  bonus-tiers-if-present natively — Slime Glands, Sticky Tongue, Quills, Blink, Electrical
  Generation below all ride this.

## The table

Sources are verified base classes (fresh decompile). "(check)" = class verified to exist, exact
in-play behavior to confirm at build.

### A. Buff on eat — straight base-unit reuse

| # | rock | eaten effect (draft) | verified source |
|---|------|----------------------|-----------------|
| 2 | soupstone | thirst at half rate | `ProceduralCookingEffectUnit_LessThirst` (thirst domain) — **user-given** |
| 3 | sludgestone | Slime Glands 1 (spit slime) | `CookingDomainLiquidSpitting_UnitSlimeGlands` — **user-given ("weak slime")** |
| 23 | chunk of salt | **always tasty**: random small buff (+HP/MA/MS/QN/stat/regen/to-hit) | `BasicCookingEffect_*` family via `RandomTastyEffect` pick — **user-given** |
| 15 | grimslate | +MA ("you digest fear") | `CookingDomainFear_UnitBonusMA` |
| 16 | tanglelith | resist forced movement (the clinging rock roots *you*) | `CookingDomainStability_MoveResistUnit` |
| 20 | gossamarl | Sticky Tongue 1 | `CookingDomainTongue_UnitStickyTongue` |
| 22 | urchinite | Quills 1 (grow quills) | `CookingDomainReflect_UnitQuills` |
| 26 | farflint | Blink 1 | `CookingDomainTeleport_UnitBlink` |
| 27 | galvena | ~~Electrical Generation 1~~ **RULED 2026-07-22 (the skipped 14th slot closes): self-discharge arc** — one arc at the nearest visible creature within 5 (voltage 1d4 / damage 1d4, its own thrown dice; indiscriminate — companions included, ruled as specced; eater unharmed; nobody in range = harmless crackle). EG grant stays eat-pass-2 and stacks on top if kept. | `EatenArc` channel; Discharge call shape from `Part_SurgeField` |
| 28 | pyroclase | +heat resist | `CookingDomainHeat_UnitResist` |
| 29 | rimestone/frostspar (T1 icy) | +cold resist | `CookingDomainCold_UnitResist` |
| 30 | vitriolite | +acid resist | `CookingDomainAcid_UnitResist` |
| 32 | mullstone | +Willpower ("quiets the mind") | `CookingDomainWillpower_UnitWillpower` |
| 36 | streakschorl *(was hypersonic rock — NAMED 2026-07-22)* | ~~+Quickness~~ **RULED 2026-07-22: thunderclap** — the speed leaves you as a crack of displaced air; all adjacent creatures Dazed 1d2+1 (EB's magnitude), eater spared (quickness-hum and both-at-once candidates declined) | `EatenThunderclap` channel; `Dazed`, quill-burst loop |
| 38 | pulsestone | EMP-flavored electric unit (check) | `CookingDomainElectric_EMPUnit` |
| 40 | blurring rock | +1 to clones you create | `CookingDomainCloning_UnitMultipleClones` |
| 41 | vantabsidian | +DV (darkness unit — Vanta Petals' own) | `CookingDomainDarkness_UnitDV` |
| 42 | lovestone | +Ego | `CookingDomainLove_UnitEgo` |
| 44 | shieldstone | +2 AV | `CookingDomainArmor_UnitAV` |
| 45 | phasalt | phase when damaged | `CookingDomainPhase_UnitPhaseOnDamage` |
| 56 | caromcobble *(NEW 2026-07-22)* | **RULED 2026-07-22: the hop** — an involuntary harmless bounce to a random open cell within 2 ("it goes down fighting") | `EatenHop` channel; `GetAdjacentCells` + `DirectMoveTo` |

### B. Gas rocks — the "burp family" rule (one rule covers five)

Eating a gas rock teaches your gut its gas: the base **Breathers** units grant "breathe X gas"
(the girshling-corpse cooking family, weak 5-tier).

| # | rock | eaten effect (draft) | verified source |
|---|------|----------------------|-----------------|
| 18 | whorlstone | breathe confusion gas | `CookingDomainBreathers_UnitBreatheConfusionGas5` |
| 19 | torpor stone | breathe sleep gas | `CookingDomainBreathers_UnitBreatheSleepGas5` |
| 35 | lattice hexstone | breathe normality gas | `CookingDomainBreathers_UnitBreatheNormalityGas5` |
| 37 | warped ajistone | a random Breathe\*Gas5 per eat (small glue) | Breathers family roll |
| — | *(radioactive glowing would be `BreathePoisonGas5` — but the twins live in the gamble section)* | | |

### C. Plain rations — satiation only, no effect

Pebbles · plain fallen rock (**the decay product — dull rocks become the shelf-stable hardtack,
closing the old "give dull rocks a use" want**) · oxidian (rust has nothing to say to a stomach) ·
Small Stones + EB ephemeral pebbles *if* the scope fork lands on all-`IsAnyStone` · gossamarl
(Sticky Tongue eat CUT by ruling 2026-07-22).

**Famished-only devour class (rulings 2026-07-22):** statues (all three kinds) **plus rubble and
boulders** — boulders inherit BaseStone/IsAnyStone and are deliberately routed out of the snack
path into the devour valve. Each is a full hunger clear.

### D. Dangerous to eat — the rock bites back

Rocks don't discriminate, including against your stomach. Mechanisms mostly reuse `FirePayload`
(eater as victim) or one-line bespokes.

| # | rock | eaten consequence (draft) |
|---|------|---------------------------|
| 17 | shivshale | internal bleeding — small `Bleeding` |
| 11 | shattered stalactite | same, plus it's a knife |
| 12 | fungal four | chance of **that rock's lottery infection** (FungalLottery part already knows it) |
| 13 | compressed ash | choking — brief `Asphyxiating`/Ill (no ash-breath unit exists) |
| 24 | volatile rock | it detonates. In you. (its own grenade at step-strength) |
| 34 | drainstone | latches inside: its drain runs on *you*, no gear displaced, crumbles sated |
| 46/47 | glowing twins | **the dinner gamble** — same name+sprite: phosphorescent = `Luminous` glow (harmless lightbulb snack); radioactive = poison + `Mutating` chance |
| 48 | magnetic lodestone | magnetic pulse centered on the eater — your own metal gear rips free |
| 49 | myriad gyrestone | a space-time vortex opens at your cell |
| 51–54 | deep four | typed internal damage (scaled from their SelfHarm dice) — **fork: pure danger, or risk-reward** (add the matching `Large*Resist` triggered-action or strong resist unit as the payoff for eating fire) |
| 39 | strange rock | rolls the full pool and applies **that rock's eaten entry** — the chaos meal (extends the existing Strange channel) |

### E. Inedible — structurally out

Never reaches a stomach, no work needed: cracked floor, chasm, rubble, crumbled wall, wall,
standing stalagmite (all terrain/non-takeable) · geysers ×3, ape statue, baboon statuette,
stonefrog-to-be (alive — it looks at you reproachfully).

## Cooking-domain assignment — pass 1 (existing domains only; Claude draft 2026-07-22, awaiting rulings)

Per the existing-domains-first ruling. One `PreparedCookingIngredient type="X"` line per rock —
pure XML. Keywords verified against the live Data.xml extract; "precedent" = the base ingredient
that owns the domain today. Tiering lever applied: **T1 elementals → `*Minor` (units-only, no
combos), deep four → full domains (combo-capable)** — pending ratification.

| # | rock | domain keyword | precedent / wiki name |
|---|------|----------------|------------------------|
| 2 | soupstone | `thirst` | pickles — "water and thirst" |
| 3 | sludgestone | `slimeSpitting` | slime |
| 23 | chunk of salt | `tastyMinor` | salt itself (native tastyMinor liquid) |
| 17 | shivshale | **OPEN** (was `agility` — user: too strong for its band; no weaker agility exists) | congealed skulk |
| 13 | compressed ash | **OPEN** (was `armor` — user leans weak-armor, which doesn't exist base-side; see flag 2) | bone meal |
| 12 | fungal four | `fungus` (all four) | pickled mushrooms |
| 15 | grimslate | `fear` | dried lah petals |
| 16 | tanglelith | **OPEN — user wants major stability; none exists base-side** (only `stabilityMinor`; see flag 1) | — |
| 18 | whorlstone | `confusionGasBreath` | mazebeard gland paste |
| 19 | torpor stone | `sleepGasBreath` | dreambeard gland paste |
| 20 | gossamarl | `stabilityMinor` — **RULED 2026-07-22** (was tongue) | asphalt — saves vs Move/Knockdown/Restraint |
| 22 | urchinite | `reflect` | spine fruit jam — quills, literal |
| 26 | farflint | `teleport` | fermented yondercane |
| 27 | galvena | `electric` — **RULED 2026-07-22: "electric needs a major, not a minor"** | spark tick plasma |
| 28 | pyroclase | `heatMinor` | (heat units, no combos) |
| 29 | rimestone/frostspar T1 | `coldMinor` | |
| 30 | vitriolite | `acidMinor` | |
| 32 | mullstone | `willpower` | mashed lag |
| 34 | drainstone | `HP` | jerky family — the life-drinker feeds you |
| 35 | lattice hexstone | `normalityGasBreath` | nullbeard gland paste |
| 36 | streakschorl *(was hypersonic rock)* | `quicknessMinor` | cider |
| 37 | warped ajistone | **RULED 2026-07-22: random gas-breath** — comma option list `fireBreath,iceBreath,corrosiveGasBreath,poisonGasBreath,sleepGasBreath,stunGasBreath,confusionGasBreath,normalityGasBreath` (verified: comma lists pick ONE random unused option per cook, `GetTypeInstanceAvoidingList`; item description auto-writes the or-list) | girsh gland pastes |
| 38 | pulsestone | `artifacts` — **RULED 2026-07-22** (tech domain for the tech-metal rock; electric overlap dissolved) | sun-dried banana — Psychometry/identify |
| 39 | strange rock | **RE-RULED 2026-07-22 (round 4, supersedes `random`): a random domain from the ROCK ingredient set, not the global pool** — implemented as a comma option list of every domain assigned in this table (the verified pick-one-random-unused mechanism, same as ajistone). **Maintenance rule: when a rock gains or changes a domain, update strange's list** (sibling of the "both table families" rule). **Eaten raw: rolls only rocks whose eaten entry actually does something** — no fizzle rolls (supersedes the fizzle proposal). Always-warn proposal stands (it's the gamble rock). | — |
| 40 | blurring rock | `cloningMinor` | cloning draught |
| 41 | vantabsidian | `darkness` | dried vanta petals |
| 42 | lovestone | `love` | congealed love |
| 44 | shieldstone | `armor` — **RULED 2026-07-22: "big armor"** (full domain; no forcefield domain exists — force-bubble flavor earmarked for a possible pass-2 bespoke) | bone meal |
| 45 | phasalt | `phase` | phase silk |
| 47 | glowing (phosphorescent) | `tastyMinor` | **the gamble cooks** — see flags |
| 46 | glowing (radioactive) | `selfPoison` | brown sludge — "poisons you" |
| 49 | myriad gyrestone | `teleport` | voider gland paste (redundant with farflint — flag) |
| 51 | deep galvena | `electric` | full domain |
| 52 | deep pyroclase | `heat` | full domain |
| 53 | deep frostspar | `cold` | full domain |
| 54 | deep vitriolite | `acid` | full domain |
| 56 | caromcobble *(NEW 2026-07-22)* | `rubberLowTier` — **RULED 2026-07-22** (base parity: extra jump, electric resist, reduced fall damage, jump-twice action; high tier reserved for a possible deep twin) | congealed rubbergum — the keyword's only other carrier |

**Deliberately NO domain (plain food or not food):** pebbles · plain fallen rock (ration, not
spice — `stabilityMinor` noted as the alternative if the staple should cook) · shattered
stalactite (weapon, not food) · oxidian (no rust domain exists) · volatile rock ·
magnetic lodestone. **Structurally excluded** (not takeable / alive): cracks, chasm, rubble,
crumbled wall, wall, standing stalagmite, geysers, ape statue, statuette.

### Flags for the ruling pass (updated after ruling round 1, 2026-07-22)

1. **Tanglelith wants major stability — none exists base-side.** The whole stability family is
   one class: `CookingDomainStability_MoveResistUnit` (`stabilityMinor`, asphalt). Options:
   (a) tanglelith shares `stabilityMinor` with gossamarl — redundant only when both are in the
   same pot; (b) tanglelith holds no domain until pass 2; (c) **pass-2 earmark:** a mod
   `stability` major domain — the keyword/mapping is pure XML and the trigger slot could borrow
   the existing whenever-you-go-prone class, but a *stronger* stability unit means one small new
   effect class (~10 lines), i.e. exactly the thing pass 1 doesn't do.
2. **No weak armor exists either** (armor domain's only unit is the full +2 AV) — same fact
   pattern for ash as flag 1: (a) keep full `armor`, (b) none, (c) pass-2 +1 AV unit subclass.
   And no weak agility for shivshale: (a) keep `agility`, (b) none, (c) pass-2. User leans
   keep-but-weaker on both, which structurally means (c) or acceptance.
3. **Pulsestone** (user dislikes the electric overlap, no answer picked): candidates —
   (a) no domain; (b) **`artifacts`** (sun-dried banana: Psychometry, identify triggers/actions)
   — the tech-flavored domain for the mod's tech-metal rock, solves the overlap sideways;
   Claude recommends (b).
4. **The twins' gamble extends to the pot** (still awaiting ruling): phosphorescent =
   `tastyMinor` (forces the tasty roll), radioactive = `selfPoison` (the meal poisons you). Same
   name, same sprite — you cannot know which you just cooked with.
5. **Eaten/cooked unification** (still open): default the defect's eat-raw effect to "one random
   unit from the rock's assigned cooking domain" instead of hand-maintaining a separate unit
   column; dangerous-section rocks keep bespoke behavior. One rule, tables can't drift.
6. **Grenade rocks in the pot — build-time verify:** ingredient consumption `Destroy()`s the
   item, and grenade-armed rocks detonate when destroyed. Cooking with a fungal rock might burst
   spores over the campfire. Suppress during consumption, or keep as an honest hazard — user call.
7. Charges: 1 per rock (base convention). The `PreservableItem` grind fork (plain rock → powder)
   from the original recon remains open and composes with all of this.

## Pass-2 bespoke cooking parts — the worklist (compiled at user request, 2026-07-22)

The rocks whose ruled or intended cooking identity needs **new effect classes** (everything pass 1
could do with existing domains is done). Mechanics reminder: a bespoke domain = one
`ProceduralCookingIngredient_X` XML mapping + small effect classes; mutation-grant units are ~6
lines via `ProceduralCookingEffectUnitMutation<T>`; **trigger classes can be borrowed across
domains** (the mapping XML just names classes — rubber's `OnProne`, tongue's `OnStuck`, armor's
`OnPenetration` are all reusable as-is).

### A. Committed needs (identity ruled, engine lacks the piece)
| rock | need | est. work | notes |
|---|---|---|---|
| tanglelith | **major stability** (ruled want) | 1 unit class + XML; trigger borrowable (`CookingDomainRubber_OnProne`) | **CHEAP PATH FIRST: if gossamarl vacates `stabilityMinor` (see B.3), tanglelith takes it NOW at zero code**, and major stability becomes optional polish |
| compressed ash | **weak armor** (+1 AV unit; user leaned weak) | 1 unit class + XML | armor's triggers/actions borrowable if it should combo |
| shivshale | **weak agility** | 1 unit class + XML | |
| oxidian | **rust domain** (none exists) | most novel of the smalls: unit (rust immunity?), trigger ("whenever your gear is rusted"?), action (rust attackers' gear?) | design pass needed, not just code |
| fungal wax rock | **"acts like you ate wax"** (ruled intent, round 7) | blocked on DEFINITION — no wax-eating exists in base at all | user invents what eating wax does; then likely 1 unit |
| shieldstone | **force-bubble domain** (user's "ehhh" on armor; earmarked) | action class ("force closes around @thisCreature when badly hit" — borrow armor's `OnPenetration` trigger; bubble spawn code exists in the mod) | upgrade, not blocker — `armor` holds until then |

### B. Weaker entries to examine (user round 8)
1. **myriad gyrestone** — currently `teleport` (redundant pair with farflint). Candidate bespoke:
   spacetime/vortex domain — trigger borrowable (`CookingDomainTeleport_OnTeleport`), action = "a
   space-time vortex opens nearby" (mod SpawnObject machinery exists). Examine whether the identity
   is distinct enough from teleport to earn classes; otherwise keep teleport and accept the pair.
2. **drainstone** — currently `HP` (jerky family, already close: on-damage heal triggers). Candidate
   bespoke: life-drain domain (unit "heal when you deal melee damage" / trigger "whenever you make a
   kill" → action "drink N HP"; native `LifeDrain` effect exists). Examine whether the vampiric
   texture is worth classes over HP's near-fit.
3. **gossamarl — the recommended first move: Spinnerets-giver domain.** `Spinnerets : BaseMutation`
   VERIFIED in decomp → grant unit ≈ 6 lines via the generic base (the slimeSpitting→SlimeGlands
   pattern exactly); trigger borrowable (`CookingDomainTongue_OnStuck`, "whenever you get stuck,");
   action candidate "webs erupt around @thisCreature" (the mod's scatter-spawn code exists). **The
   cheapest bespoke on the board, and it vacates `stabilityMinor` for tanglelith — one small domain
   resolves two entries.** (Bonus finds while verifying: `SpiderWebs` and `FrostWebs` mutations also
   exist if the domain ever wants deeper units.)

*Context: the two big pass-2 umbrellas already parked elsewhere — the mutation-themed domain slate
(TODO cooking section) and the Tier-2 `stone` domain with the ceiling-drop signature action —
would land in the same build style as this list.*

## Sub-forks to rule

1. **Strength dial:** these are the *same units real meals grant*, lasting until next hunger — a
   band-3 rock eaten raw ≈ a cooked meal's passive. Options: (a) accept — the price is spending
   ammo/loot as food and holding only one buff (Claude lean: accept for band 3, keep bands 1–2 on
   the humbler units as drafted); (b) bespoke weakened subclasses (halved numbers, real C# cost).
2. **Burp family:** ratify the Breathers rule as the uniform gas-rock treatment, or hand-pick.
3. **Danger policy:** dangerous rocks eatable-with-consequences (as drafted — the defect's flavor
   is *compulsion*, and the twins' dinner gamble is great) vs. refused with a fail message.
4. **Deep four:** pure punishment vs. risk-reward (danger + big resist payoff).
5. **Strange rock:** confirm the chaos-meal recursion (Claude lean: obviously yes).
