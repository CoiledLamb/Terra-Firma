# Fissure rock catalog — "fissure roundup 2026"

User's design list, received 2026-07-22 (verbatim in the second section). Working rules from the user:

- **Rocks don't discriminate** — every effect hits players and enemies alike.
- **Positive effects: throw at yourself. Negative: throw at your enemies.**
- Ordered weakest → strongest. All names WIP.
- Fiction: **the ceilings we pass under have these unidentified rocks in them** — Fissure shakes them
  loose. The rocks could also appear elsewhere in the game later (other spawns, loot, mineable walls).
- Extensible: as the mod grows creatures, they join this table; later, walls with cooking-ingredient
  growths could spawn as table entries too.
- Presentation over volume (ruling 2026-07-22): the cast should land **a solid slam (rock-wall
  collapse sound) + smoke particles where rocks fall**, with few actual objects. Light C# for the
  moment is acceptable; no bespoke animation system.

## Triage: engine mechanism, cost, wave

Legend — **Cost:** XML (blueprint only) · reuse (existing engine/mod part, light glue) · C#-small
(shared new part or one small part) · C#-real (novel logic) . **Wave:** v1 ships with the mutation
rebuild; v1.5 fast-follow; v2 later; parked = needs its own design pass. Mechanisms marked
*(verify)* are high-confidence but must be decompile-checked before build, per house norms.

Three shared pieces power most of the list:

1. **Step-trigger part** (one new C# part): fires the rock's payload at reduced strength when a
   creature enters/steps on the cell. Written once, parameterized per rock.
2. **Falling-impact routine** (in the cast code): crush damage + smoke + slam at each landing cell,
   scaled by what fell (pebble 1 dmg → wall massive + instakill chance).
3. ~~Unidentified-rock naming via `Examiner`~~ — **CUT (user ruling 2026-07-22):** unknown-forms
   are tech-item language in base Qud; rocks using them would overstep. The glowing-rock twins
   need no system (two blueprints sharing DisplayName + tile is the gamble); strange rock is
   random-per-throw. "Unidentified ceiling rocks" is description flavor only.

| # | Rock | Tier band | Mechanism | Cost | Wave |
|---|------|-----------|-----------|------|------|
| 1 | pebbles | 1–3 | ThrownWeapon 1 dmg (color variants CUT, playtest ruling 2026-07-22) | XML | v1 |
| 2 | soupstone | 1–3 | LiquidVolume splash on shatter (phial pattern *(verify)*), salty water | reuse | v1 |
| 3 | sludgestone | 1–3 | same, slime liquid (native slip) | reuse | v1 |
| 4 | cracked floor | 1–3 | dormant StairsDown + FallThrough channel: 15% fall-through underfoot, mine asymmetry kept | C#-small | **built** |
| 5 | chasm | 7–9 | OpenShaft child: always-open hole, swallows what it lands on (loot too) | XML | **built** |
| 6 | generic rocks | 1–3 | throwable, crush dmg via falling-impact | XML | v1 |
| 7 | rubble | 1–3 | impassable, not takeable, bigger crush dmg (base-game rubble precedent *(verify)*) | XML | v1 |
| 8 | crumbled walls | 4–6 | partial-cover wall object, crush dmg between rubble and wall | XML | v1 |
| 9 | walls | 7–9 | full wall object lands; heavy crush + instakill chance on the cell it crushes | XML + impact rule | v1 |
| 10 | stalagmite | 4–6 | one-hit breakable floor spike (HP 1/AV 0, plant-wall fragility ruled), drops the stalactite; surface-only substitution rule | reuse | **v1.5 built** |
| 11 | shattered stalactite | 4–6 | thrown blade rock (1d6/pen 6 ShortBlades) + ShatteredArmor −1 AV 100 rds on hit; cave-weighted 3:1 over stalagmite | reuse | **v1.5 built** |
| 12 | fungal rock | 4–6 | fungal spore gas puff on hit/step (puffer gas *(verify)*) | reuse | **v1.5 built** |
| 13 | compressed ash | 4–6 | ash gas cloud on throw, smaller on step *(verify gas name)* | reuse | **v1.5 built** |
| 14 | rusty rock | 4–6 | Rusted effect on hit; chance rust attack on step | C#-small | **v2 built** |
| 15 | scary rock | 4–6 | Terrified/fear effect on hit, weaker on step | C#-small | **v2 built** |
| 16 | bola rock | 4–6 | Stuck/immobilize on hit; prone chance on step | C#-small | **v2 built** |
| 17 | pointy rock | 1–3 | `BleedingOnHit` (already used on the mod's urchin spine) | reuse | v1 |
| 18 | whorlstone | 4–6 | confusion gas payload (grenade part *(verify)*) | reuse | v1 |
| 19 | torpor stone | 4–6 | sleep gas payload (grenade part *(verify)*) | reuse | v1 |
| 20 | spiderweb stone | 4–6 | web objects on burst + StickyOnHit (one-use, ruled) | reuse | **v1.5 built** |
| 21 | baboon statuette | 4–6 | CREATURE-first (ruled 2026-07-22): BaseBaboon child, AIThrowAndScoot + StoneScavenger + fissure pocket stones, range via actor properties, stone body (+2 AV, acid-weak); pocket-form/rep = open auxiliary pass | XML | **built (creature core)** |
| 22 | spiny rock | 4–6 | quill burst on throw/step | C#-small | **v2 built** |
| 23 | chunk of salt | 1–3 | thirst reduction on hit (throw at self!); salt puddle on break | C#-small | **v1.5 built** |
| 24 | volatile rock | 4–6 | HE-grenade payload, weaker on step | reuse | v1 |
| 25 | spitting geyser | 4–6 | **tier-1 omnigeyser** (ruled 2026-07-22): rolls ThrowableRocks1 per shot w/ Deeper roll-up; FabricateFromSelf subclass, HP-per-shot to collapse | C#-small | **v1.5 built** |
| 26 | shunting rock | 7–9 | random teleport on hit, scatter ≤12 (ruled); ModDisplacer call shape unpowered; no step effect | C#-small | **v1.5 built** |
| 27 | voltaic rock | 7–9 | `DischargeOnHit` (catalogued in the statue survey) | reuse | v1 |
| 28 | scorching rock | 7–9 | thermal-grenade payload *(verify)* | reuse | v1 |
| 29 | icy rock | 7–9 | freeze-grenade payload *(verify)* | reuse | v1 |
| 30 | melting rock | 7–9 | acid splash on throw/step (gear-damage half CUT — ruling 2026-07-22) | reuse | **v1.5 built** |
| 31 | ape statue | 7–9 | falls as a LIVING allied creature (albino-ape stats on BaseApe, ape fists, no XP); **permanent** per band-3 ruling — no animate/de-animate tech needed | reuse | **v1.5 built** |
| 32 | mullstone | 7–9 | cooldown-lock all abilities 10 turns (ActivatedAbility API exists) | C#-small | **v2 built** |
| 33 | spewing geyser | 7–9 | **tier-2 omnigeyser** (rolls ThrowableRocks2) | C#-small | **v1.5 built** |
| 34 | drainstone | 7–9 | cursed equipment: latch displaces gear (natural-weapon reroll), Cursed + native LifeDrain feeds thrower, 15/7 rounds then crumbles | C#-small | **built** |
| 35 | lattice hexstone | 7–9 | normality gas payload *(verify)*; anchor on hold/step | reuse+ | **v2 built** |
| 36 | streakschorl *(was hypersonic rock — NAMED 2026-07-22)* | 7–9 | **Seek-and-bounce flight (RULED 2026-07-22, round 12): geomagnetic-disc adaptation** — flies to the aimed hostile (auto-hit), bounces to up to 2 more (d20, can flinch), spends itself at flight's end (single use, no return). Fallback with no hostile reachable: the original NeverMiss + vibro normal throw; any step still breaks it | C#-medium (`Part_StreakFlight.cs`) | **v1.5 built + flight built** |
| 37 | warped ajistone | 7–9 | random gas blueprint at emit time | C#-small | **v1.5 built** |
| 38 | pulsestone | 7–9 | EMP-grenade payload *(verify)*; metal | reuse | v1 |
| 39 | strange rock | any | meta: rolls the full pool per throw/step — grenade rocks Detonate, payload rocks fire, plain things (pebbles, stalagmites, apes) simply appear | C#-small | **v1.5 built** |
| 40 | blurring rock | 8–9 | CreateFugueCopyOf hostile-path + re-led: copies of victim SERVE THE THROWER (2/20 thrown, 1/10 step) | C#-small | **built** |
| 41 | vantabsidian | 7–9 | darkness field (Vantabloom precedent) + temp blind on hit | reuse | **v1.5 built** |
| 42 | lovestone | 8–9 | temp-ally on throw (hostile act); Lovesick on step (mod already touches Lovesick) | C#-small | **v2 built** |
| 43 | mindstone | 8–9 | ~~domination on throw; consciousness-shunt on step~~ | — | **CUT (user ruling 2026-07-22)** |
| 44 | shieldstone | 8–9 | force walls on throw / force bubble on step (Force tech exists) | C#-small | **v2 built** |
| 45 | drifting rock | 8–9 | omniphase; phase on hit (phase-grenade pattern *(verify)*) | reuse | **v2 built** |
| 46 | glowing rock (radioactive) | 8–9 | poison radius + Mutating + LightSource glow | C#-small | **v1.5 built** |
| 47 | glowing rock (phosphorescent) | 8–9 | **identical name+sprite to #46** — identification gamble; benign glow | XML | **v1.5 built** |
| 48 | magnetic lodestone | 8–9 | `MagneticPulse.EmitMagneticPulse(rock, radius)` — rips worn metal, drags robots; thrown r6/step r3 | C#-small | **built** |
| 49 | myriad gyrestone | 9 | space-time vortex objects (exist, spawnable) | reuse+ | **v2 built** |
| 50 | heaving geyser | 9 | **tier-3 omnigeyser** (rolls ThrowableRocks3, most HP) | C#-small | **v1.5 built** |
| 51 | deep galvena | band 4 | DischargeOnHit 2d4/2d6 + lingering surge field (SurgePart, Temporary 4) | C#-small | **built** |
| 52 | deep pyroclase | band 4 | ThermalGrenade +900 + lava-patch byproducts; base Temperature 500 (radiance + AI avoidance) | reuse+ | **built** |
| 53 | deep frostspar | band 4 | ThermalGrenade −800 + convalessence-patch byproducts; base Temperature −250 | reuse+ | **built** |
| 54 | deep vitriolite | band 4 | AcidGas 80 + acid-pool byproducts + BreakGear channel (Broken + ShatteredArmor) | C#-small | **built** |
| 55 | (T2 rule) | band 4 | SelfHarm 1d3 typed sting per throw + 1 dmg/5 turns equipped (holding burn, user add) | shared rule | **built** |
| 56 | caromcobble *(NEW 2026-07-22 — the rubber rock, user concept out of the whorl-deflection thread)* | 4–6 | **Ricochet throw:** after every throw resolves — hit or miss, "the rock never lands where you aimed, ever" — it bounces 2 fully-random-angle legs (range 4, flat d20 vs DV, 1d4 per hit, thrower attribution), lands intact; **HoldChance 100, rubber never breaks**. Eaten (lithovore) = involuntary harmless hop. Ground form = base `rubberLowTier` (congealed rubbergum parity) | C#-small (generic `Payload="Ricochet"` channel) | **built** |

## v1.5 engine recon (2026-07-22, decompiled/grepped ground truth — all *(verify)* marks for wave v1.5 resolved)

Verified against game XML (`StreamingAssets\Base\ObjectBlueprints\`) and `ilspycmd` decompiles
(class dumps in session scratchpad; type list confirmed against the full 7,071-class listing).

| # | Rock | Ground truth |
|---|------|--------------|
| 10/11 | stalagmite / shattered | `BaseStalagmite` (ZoneTerrain.xml:881) is terrain: Takeable=false, Solid, AV 10, HP 5000, Flyover, AllowMissiles — a landing-object precedent, not a weapon; blade dice are plain MeleeWeapon XML. **No `Cracked` effect exists in the engine** (only unrelated `Crackling`) — the "cracked on penetration(?)" TBD needs a user ruling: bespoke effect, or reinterpret. Break-to-take drop mechanism: verify `Corpse`-part-on-object vs. small on-destroy handler at build. |
| 12 | fungal rock | `FungalSporeGasPuff`/`Luminous`/`Wax`/`Mumbles` (+`80` density variants) — `GasFungalSpores` part, one infection per variant. "Variety of colors" maps to infection variants. Rides the mod's existing GasGrenade/Gas payload channels: **XML-only**. |
| 13 | compressed ash | Gas is `MiasmaticAsh` (+`80`/`200`) — "osseous ash" DisplayName confirmed, `GasAsh` asphyxiation. The ashy stalagmite (`MiasmaticStalagmite`) already demos it via Walltrap jet. **XML-only.** |
| 20 | spiderweb stone | Web objects are `Web`/`FrostWeb`/`PhaseWeb` (ZoneTerrain.xml:1227+, `Sticky` part). Bonus find: **`StickyOnHit` part** = stuck-on-throw out of the box (`ProjectileHit`→`Stuck` effect, then destroys the rock — one-use semantics, a design choice to keep or drop). Web *spawning* on burst = small SpawnObject payload in the mod part. |
| 23 | chunk of salt | Native thirst API: Stomach registers **`AddWater` event** (`FireEvent(Event.New("AddWater", "Amount", N))`; drink quantum in code = 10000). Salt puddle reuses the soupstone liquid-pour tech. C#-small confirmed. |
| 25 | spitting geyser | **Cragmensch recipe is the whole mechanic** (Creatures.xml:4986): `FabricateFromSelf ChargeUse="0" FabricateBlueprint="MediumBoulder" BatchSize="1" HitpointsPer="1" AIUseForThrowing="true" HitpointsThreshold="5" Cooldown="1d2+1"` — HitpointsPer IS the shoot-self-damage, threshold is the collapse floor. Immobile-emplacement brain per `TinkerTurret` (Brain Mobile=false). Single fixed blueprint per part instance — "random rocks" needs a tiny subclass rolling a population table, or per-variant geysers (user choice). |
| 26 | shunting rock | `ModDisplacer` natively registers `WeaponThrowHit` — but it's a powered mod (ChargeUse 250, cell socket, EMP-sensitive): wrong chassis for a rock. The core is the **public `GameObject.RandomTeleport(...)` API** — small unpowered on-hit part copying ModDisplacer's trigger recipe. Cost is C#-small, not pure reuse. |
| 30 | melting rock | Acid splash/puddles: `AcidPuddle`/`AcidPool` + mod liquid tech; `AcidGas` = "corrosive gas". **Open:** its `GasDamaging` has `ExcludeTag="Item"` — corrosive gas spares gear, so no native acid-wrecks-gear precedent found yet; the gear-damage half needs its own dig at build time. |
| 31 | ape statue | "ape fist" natural-weapon blueprints exist (albino/ogre ape, Creatures.xml:7407/8765); mod's animate/de-animate statue tech covers the rest. Reuse confirmed. |
| 36 | hypersonic rock | No native never-miss for thrown weapons found — C#-small stands (vibro = mod's thrown-Vorpal tech). |
| 37 | warped ajistone | AjiConch decompiled: one call — `PopulationManager.GenerateOne("DynamicObjectsTable:AjiConch")` — rolls a random gas blueprint from the dynamic table (every gas tagged `DynamicObjectsTable:AjiConch`: poison, osseous ash, corrosive, glitter, …), then Density=800/cells, Level=5. The table is shareable as-is. C#-small confirmed. |
| 39 | strange rock | Pure mod-side meta-roll; nothing to verify. |
| 41 | vantabsidian | Vantabloom's darkness is the **`Blackout` part** (Radius param; plain IPart, works on any object — field is XML-only). Temp blind = `XRL.World.Effects.Blind` (standard duration effect), applied via a small on-hit payload. |
| 46 | glowing rock (radioactive) | `PoisonGas`(+`80`) blueprint; **`XRL.World.Effects.Mutating`** confirmed (Duration; rolls `MutatingResults` pop table at expiry: mutation/defect/MP; halfway permute). **Gate: only mutants and True Kin can get Mutating** — robots/purestock beasts shrug it off; balance fact to design around. Glow = `LightSource` part. |
| 47 | glowing rock (phosphorescent) | `XRL.World.Effects.Luminous` renders exactly as "{{m|phosphorescent}}" (duration effect, radius-3 glow) — the user's wording is already the engine's. Twin-name gamble stays systemless XML. |

**Shared-tech implication:** most v1.5 payloads reduce to two new generic channels on `Part_FallenRock`
(SpawnObject: webs; ApplyEffect: Blind/Luminous/Mutating/Stuck) plus three bespoke one-offs
(AddWater, RandomTeleport, random-gas). The 7 shipped channels already cover the rest.

## Verbatim list (user, 2026-07-22)

```
alright rock stuff time! fissure roundup 2026 lets gooo!

for the below effects, they should activate on players and enemies. rocks don't discriminate!
if the effect is positive, throw it at yourself. if it's negative, throw it at your enemies!

generally starting from the weakest and going to the strongest. all names wip!
pebbles: largely decorative. deals 1 damage to any creature it hits directly. comes in a variety of colors!
soupstone: splashes salty water on a creature it hits. creates a puddle of salty water when stepped on.
sludgestone: splashes slime on a creature it hits. creates a puddle of slime when stepped on. has a chance to slip.
cracks: small chance for anyone standing on them to fall into the next floor
holes: if possible, the kind used in rustwells/chavvah, but i have no idea how reasonable this is
generic rocks: damages anything in the tile directly under it, can be thrown
rubble: damages anything in the tile directly under it, but more. can't be thrown (or passed through, follow basegame!)
crumbled walls: midpoint between rubble and walls for damage
walls: even higher damage to the tile directly under it, has a chance of instantly killing any units it hits
stalagmite: deals short/long blade damage, must be broken to throw it. inflicts cracked on penetration(?)
shattered stalagtite: deals short long blade damage, inflicts cracked on penetration(?)
fungal rock: emits spores when thrown or stepped on. comes in a variety of colors
compressed ash: emits a cloud of osseous ash when thrown, and a smaller cloud if stepped on
rusty rock: applies rust on hit. has a chance to apply a rusting attack to anything that steps on it
scary rock: applies fear on hit. has a chance to apply a weaker fear effect if stepped on
bola rock: applies clinging to a creature, immobilizing them in a tile for a while. has a chance to make a creature fall prone if stepped on
pointy rock: applies bleed on hit. has a chance to apply a weak bleed effect if stepped on
whorlstone: emits confusion in a radius when thrown. has a chance to confuse nearby creatures if stepped on.
torpor stone: emits sleep gas in radius when thrown. has a chance to put nearby creatures to sleep if stepped on.
spiderweb stone: emits spiderwebs and makes the target stuck when thrown. has a chance to makes creatures stuck if stepped on.
baboon figurine: a small, sentient figure that throws stones at enemies. gives as much rep as a neck ring/figure when held in hand
spiny rock: emits a burst of quills when thrown, and a weaker one when stepped on by a creature
chunk of salt: reduces the thirst of the creature you throw it at. spawns a puddle of salt when broken/stepped on?
volatile rock: explodes when thrown, explodes in a weaker square when stepped on.
spitting geyser: spawns a geyser that shoots random rocks at enemies. each rock shot deals damage to it, until it collapses.
shunting rock: teleports a creature hit to a random tile when hit. does not have a step effect(?)
voltaic rock: emits a burst of electricity that arcs between creatures, and a weaker surge when stepped on.
scorching rock: emits a blast of fire when thrown, and a weaker blast when stepped on.
icy rock: emits a blast of frost when thrown, and a weaker blast when stepped on.
melting rock: splashes acid on a creature when thrown or stepped on. cracks and breaks gear if it hits.
ape statue: summons an animate statue with an ape's natural equipment that shares player affiliations and "dies" after some amount of turns
mullstone: puts all abilities on a 10 turn cooldown of any creature it hits. puts one ability on a ten turn cooldown when stepped on.
spewing geyser: spawns a geyser that shoots random elemental rocks at enemies. each rock shot deals damage to it, until it collapses. has more health
drainstone: enters an enemies inventory when thrown, and absorbs some HP as long as it's there. clings to an enemy and absorbs their life force for a few turns if stepped on. (HP gains go to the player if it's easy)
lattice hexstone: emits a field of normality (maybe just gas?) when thrown, and a weaker one when stepped on. anchors anyone holding or stepping on it.
hypersonic rock: never misses, has vibro. breaks if stepped on (maybe also fixed break chance on use)
warped ajistone: emits a random cloud of gas when thrown, and a smaller cloud when stepped on
pulsestone: emits an electromagnetic pulse when thrown. electromagnetically pulses when stepped on. metal.
strange rock: has a random effect pulled from any entry on the table! unknown until you throw it.
blurring rock: summons temporal fugue clones of target hit, summons a clone if stepped on.
vantabsidian: creates a field of darkes around itself, gives a temporary blind status if thrown at a unit
lovestone: temporarily allies an enemy when thrown. this is a hostile action. if stepped on, inflicts lovesick.
mindstone: temporarily dominates an enemy when thrown. if stepped on, shunts the creatures consciousness to a random creature on the map (can be abstracted for enemies)
shieldstone: attempts to create a line of force walls when thrown. creates a force bubble around the target for a short time if stepped on.
drifting rock: omniphase. applies phase to anything it hits, and for a shorter period of if stepped on
glowing rock: inflicts long lasting poison in a radius. applies mutating when thrown or stepped on. glows in a radius.
glowing rock: glows in a radius. applies phosphorescent when thrown. has the same name and sprite as radioactive glowing rock
magnetic lodestone: emits a pulsed field magnet effect when thrown, and one with a weaker radius when stepped on. metal.
myriad gyrestone: warps an enemy a la space time vortex when thrown. spawns space time vortexes when stepped on.
heaving geyser: spawns a geyser that shoots random T2 elemental rocks at enemies. each rock shot deals damage to it, until it collapses. has even more health
T2 elemental stones - all slightly hurt the thrower when equipped/used
voltaic rock 2: shoots a strong burst of arcing electricity when thrown, and continues to surge out electricity for several turns afterwards. emits electricity when stepped on
scorching rock 2: emits a large blast of fire when thrown, and splits into patches of lava. emits a smaller burst if stepped on, and splits into a patch or two of lava. has a very high base temperature
icy rock 2: emits a large blast of ice when thrown, and splits into patches of convalessence. emits a smaller burst if stepped on, and splits into a patch or two of convalessence. has a very low base temperature.
melting rock 2: emits a blast of corrosive gas, and splashes 3-5 pools of acid when thrown. breaks and cracks armor on hit. emits 1-3 tiles of acid if stepped on, and breaks a random piece of gear, splashing acid in the process
```
