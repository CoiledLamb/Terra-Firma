# Lithovore eaten-table comb — pre-beta (2026-07-31)

Commissioned at the L2 sitting (user: "i want to comb over the entire eaten effects table
… and make sure everything tracks before this beta cut"). Governing principle for the
even-out work, user-stated same sitting: **the "witchwood" design principle — stones can
have a positive effect with a chance to do something negative** (base witchwood bark:
the heal with the confusion tax). In-catalog precedents already using the shape:
urchinite (outward quill burst, 10% self-prick, RULED 2026-07-24) and manymarble
(allied clone, 10% chance of a second — the inverted, all-positive version).

Everything below is CLAUDE ANALYSIS; per-entry status is tagged. All proposal numbers
DRAFT. Sources: `Rocks.xml` eaten tags + stock *OnEat parts, `Mutation_Lithovore.cs`
`FireEatChannels`.

---

## A. Tracks — positive or neutral, no action proposed

| Rock | Eaten | Note |
|---|---|---|
| brine geode | +5000 water (half-drink) | RULED 2026-07-22 |
| healstone | heal 2d4 | RULED 2026-07-27 (L1) |
| chunk of salt | always tasty (random minor buff) | RULED, salt-liquid precedent |
| mullstone | refresh one cooldown | the inversion survives |
| aegischist | force shards close around you | |
| phasalt | Phased 1d6+4 | escape/wall-walk button |
| manymarble | allied fugue clone ×10, 10% second | witchwood-inverted |
| farflint | random teleport | chaotic escape button |
| caromcobble | harmless hop, radius 2 | RULED |
| galvena | arc at nearest visible creature (companions included), crackle if alone | indiscriminate rider IS its witchwood bite |
| urchinite | quill burst spares eater, 10% self-prick | **the witchwood template**, RULED |
| drainstone | StoneSiphon 10 rds, pooled 1d3/rd bursts back as healing | delayed-gratification gamble, RE-RULED 2026-07-24 |
| streakschorl | plain | **BY RULING** 2026-07-25 (thunderclap cut, power budget) |
| glowing twins | lightbulb snack vs poison+Mutating, identical warns | the 50/50 gamble IS a witchwood pair (Mutating = the jackpot) |
| strange | eats as a surrogate doer | purity-ruled pool |
| pebbles / fallen rock / etc. | plain ration | fine |

## B. Negative but identity-true — the warn-gated lessons (keep)

| Rock | Eaten | Why it stays |
|---|---|---|
| shivshale | internal bleed 1d2 | small tax, warned |
| shattered stalactite | internal bleed 1d8 | RATIFIED "emphatically" 2026-07-24 |
| quarrelquartz | HE grenade detonates in you | the mill law's mouth station |
| hushmica | EMP detonates in you | artifact users' lesson, trivial otherwise |
| gyrestone | Space-Time Vortex at your cell | "You knew what this was." |
| deep four | 2d3 typed harm + detonate / surge field | "the deep bite back," ruled identity |
| fungal four | random infection incubates | the fungal lottery is its own economy |
| vitriolite | 1d3 acid + acid pool underfoot | T1-scale numbers, warned; cheap lesson |
| sludgabbro | slime pool at your feet | comedy-lite, RULED round 6 |
| dotagate | Lovesick 50, nearest visible | comedy-negative but identity-perfect; flag-lite only |
| lodestone | magnet pulse centered on EATER — own gear rips free | warned; **borderline** — optional witchwood shape below |

Lodestone — **RULED 2026-07-31 ("fetch sounds good"), build queued:** the eaten pulse
becomes a FETCH — loose metal items in radius 3 yank toward the eater (vacuum-loot),
25% (house rate) the pulse grips the eater's own gear too (old rip-free behavior as the
bite). Robot-drag half: keep if it composes naturally with the fetch rewrite (surface
at build if it doesn't). The biggest personality change of the comb — lesson → utility —
made knowingly. Move its row out of the B-keep list at build time.

## C. "Just terrible to eat" — the witchwood candidates (the sitting's target class)

**C1 · grimslate — RULED 2026-07-31 ("sounds good"), build queued.** Witchwood invert:
dread radiates outward — adjacent creatures terrified (urchinite loop shape), ~25% the
dread turns inward and the eater flees (old behavior as the bite). Numbers DRAFT.
*Build note (dreadroot check, user-directed):* decompiled `FearOnEat` is a SAVE-GATED
mental attack (`PerformMentalAttack` → `Terrified.Attack`, Strength 1d8 / difficulty 8 /
Duration 2d4, flee anchored on the eater's cell; player forfeits turn on failed save).
Both halves keep those semantics — outward = per-neighbor Terrified.Attack (attacker =
eater, so Willpower saves + credit thread correctly); inward 25% = dreadroot's exact
self-attack. The old entry was save-gated all along, so "guaranteed punishment" was
overstated — ruling stands on the zero-upside ground.

**C2 · vantabsidian — RULED 2026-07-31 ("this sounds fine to me"), build queued.**
*Your eyes drink the dark:* see in darkness ~100 turns, ~25% the dark drinks back —
Blind ~10 (raw application, no save; the asymmetry vs C1's save-gated fear is correct
per the dreadroot check). **Blind duration flagged by the user as potentially
revisitable post-build.** Numbers DRAFT. *Build recon (done same sitting):* skulk tonic
is the engine precedent — timed effect calls `Mutations.AddMutationMod("DarkVision", 1)`
(Guid stored, `RemoveMutationMod` on expiry, radius bumpable, tonic-tested across
save/load); build = a small DarkAdapted-style effect in that exact shape.

**C3 · vertigneiss — RULED 2026-07-31 ("yep"), build queued.** Keep the belly untouched
(the half-tool: vented gas zones your own square), chance-gate the immediate
self-confusion at **25%** — the user unified the bite rate ("sure, we can just keep
using 25%"), making **25% the HOUSE WITCHWOOD RATE** for these bites. The 10% mild-rider precedents
(urchinite prick, manymarble second clone) are NOT frozen — user lean same sitting:
"we can update mild riders as well potentially, just communicate each one during the
pass" — each gets surfaced individually at the build, none changed silently.
**Surfaced + RULED KEPT AT 10% 2026-07-31 ("sounds good to me"):** urchinite prick
(token tax on an outward-value entry) and manymarble second clone (a bonus roll, not a
bite; 25% would cheapen the good surprise) both stay.
User lean recorded same exchange: this belly-keep + chance-gated-swallow shape is
"a pretty good framework for the other belly parts" (C4, and C6/C7 if wanted).
Confirm ConfuseOnEat's save semantics at build (expect dreadroot-style mental attack).

**C4 · lullabaster — RULED 2026-07-31 ("sounds good to me!"), build queued.** Framework
applied: SleepGas belly stays untouched, immediate nap (Asleep 1d6+2, raw) chance-gated
at the house 25%. Accepted charm: a landed nap somewhere safe is nearly a rest button
(sleep speeds natural healing) — that's flavor, not a flaw.

**C5 · lattice euclase (hexstone) — RULED 2026-07-31 ("alright, sounds fine"), build
queued.** SUBTRACTION, not witchwood: the `EatenDetonate` tag is CUT (the gratuitous
third hit); RealityStabilized 50 + NormalityGas belly stay as-is. The entry's native
two-facedness carries it — the tether mutes your tricks AND everything
reality-distortion-based aimed at you (vortices, espers, spacetime trouble).

**C6 · conchalk — RULED KEPT AS-IS 2026-07-31 ("sounds fine to me").** Roulette is the
whole personality; the gas table sets the odds, some rolls favorable — already a gamble
entry. No gate, no change.

**C7 · compressed ash** — MiasmaticAsh belly only. Belly-as-tool reading applies
(ash zones around you); mild. Lean keep. But see D3 — its bookkeeping is broken.

## D. Gaps and wrinkles

**D1 · oilstone — RE-RULED same sitting 2026-07-31 ("confirmed for this"; supersedes
the slick-only ruling minutes earlier): WITCHWOOD, user's own shape.** Positive (75%):
*sure-greased* — can't slip for ~50 turns (user: "an effect where you can't slip for x
amount of turns"); sibling to tanglelith's sure-footed (the two sticky rocks =
complementary footing buffs). Bite (25%, house rate): the oil passes through — slick
underfoot (the superseded ruling demoted to the bite). Drink-oil dead. Add the
`EatenEffect` marker. Slip-save recon at build; numbers DRAFT.

**D2 · tanglelith — RULED 2026-07-31 ("1 sounds fine"), build queued: SURE-FOOTED.**
The tar grips inward — anchored ~50 turns (save bonus vs forced movement / knockdown /
prone; mirrors its stabilityMinor cooking domain, the eat-mirrors-domain house pattern),
25% (house rate) the tar grips too well: briefly Stuck. Build = small save-modifier
effect (SaveModifier idiom); numbers DRAFT. Plain-by-ruling and stuck-only-comedy both
dead; the user pushed past plain ("we could probably rack our brains and come up with
something positive").

**D3 · compressed ash missing `EatenEffect` marker — NODDED 2026-07-31 ("add it"),
build queued.** One-line tag add; restores the strange-eat pool's purity standard
(RULED 2026-07-24, "everything it does should actually happen" — pool = all doers).

**D4 · holographic rock — RULED 2026-07-31, build queued: the joke completes.** Zero
satiety; message is the USER'S OWN LINE, verbatim: "You bite down on the holographic
rock. It flickers to nothingness in your mouth." (Checked same sitting: "flicker" is
the base game's hologram verb — ConcealedHologramMaterial "flickers subtly," flicker-
frame rendering — and the line bridges our rock's existing deadpan "nothing" register.)
Build note: the line REPLACES the standard eat popup incl. the satiety readout (the
readout would step on the joke); player-only, NPCs silent no-op.

**D5 · warn-gate consistency — RULED KEPT AS-IS 2026-07-31 ("keep them as is, it's
fine"):** the harm/status line stands — harm warns, statuses never do, no new
`DangerousToEat` tags. (The witchwood pass had already dissolved most of the original
inconsistency: the former harsh statuses are now 25% bites on positive gambles, which
shouldn't warn. Hexstone and dotagate stay unwarned by this ruling.)

---

## BUILD RECORD — BUILT + DEPLOYED 2026-07-31 (compiles clean, 0 errors; active next restart)

Every ruling above shipped in one batch: **XML** (Rocks.xml: grimslate/vertigneiss/
lullabaster/vantabsidian lost their base OnEat parts for channel tags, hexstone lost
`EatenDetonate`, ash gained its `EatenEffect` marker, tanglelith + oilstone gained their
witchwood tags, lodestone comment reruled) · **new effects** (`Effect_DarkAdapted.cs` =
skulk-tonic-shape temporary DarkVision; `Effect_StoneFooting.cs` = SureFooted +4 vs
Move/Knockdown and SureGreased +100 vs Slip, both renew-not-stack) · **six new channels
+ fetch rewrite + holographic zero-satiety branch** in `Mutation_Lithovore.cs`
(`WITCHWOOD_BITE = 25` const; dread and confuse keep dreadroot save semantics;
vertigneiss buildup mechanic cut in port, recorded). All player-facing message strings
DRAFT, owed the text signoff. Deploy hash-verified (Rocks.xml + 3 scripts).

**Verify checklist (next boot, LOG unless noted):** grimslate eat → "Dread" channel line,
either `radiates outward: N routed` or `the bite`; EYES: adjacent enemies flee ·
vantabsidian eat → dark-adapted status + EYES dark cells visible, or Blind 10 ·
vertigneiss/lullabaster eat → `swallowed clean` most eats, belly lines still follow ·
hexstone eat → RealityStabilized + belly, NO detonation · lodestone eat near loose metal
→ items skitter adjacent (EYES), or full pulse on the bite · tanglelith eat →
sure-footed status; save/load with it active (new serialized effects: DarkAdapted Guid,
footing effects) · oilstone eat → sure-greased; walk an oil/slime puddle → no slip ·
holographic eat → user's line, NO satiety change, rock gone · ash eat via strange
surrogate now possible (`strange surrogate=CompressedAsh`) · Stuck bite: tanglelith 25%.

**Not built (queued):** tfeat harness rows for the new channels (each fires exactly one
of two logged branches, so either-line asserts are cheap) — add at the next harness
sitting before the hardening boot.

## Suggested ruling order

C1 grimslate → C2 vantabsidian → C3 vertigneiss → C4 lullabaster → C5 hexstone →
C6 conchalk → B lodestone (optional) → D1 oilstone → D2 tanglelith → D4 holographic →
D5 warn line → D3 ash marker (nod). One at a time, per house style.
