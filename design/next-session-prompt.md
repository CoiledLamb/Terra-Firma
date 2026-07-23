# Next-session prompt — food & rocks continuation (draft 2026-07-22 evening, edit freely before use)

---

I'm continuing work on **Terra Firma**, a Caves of Qud mod I commissioned (by "Cleo, Ceres et al.")
and now own outright. The previous session was the big **food-and-rocks session**: it built the
**Lithovore** physical defect (eat rocks, only rocks — 2 MP, Carnivorous parity), a full
raw-eating layer (~36 eaten effects on the base `*OnEat` register), cooking-domain tags for the
whole catalog, **grind-first milling** (rocks are never direct ingredients; the pantry opens only
through the mill — pestle anywhere, campfire menu, millstone furniture, or interacting with the
rock itself), per-rock **Ground_\*** powder blueprints, the mod's **first two bespoke cooking
domains** (Web on gossamarl: Spinnerets grant / on-stuck / webs-erupt; Spacetime on myriad
gyrestone: borrowed teleport units + a vortex-opens action), fungal **cuisine tells** (the lottery
restamps each fungal rock's cooking domains — hoarshroom rock = base hoarshrooms' exact
`cold,fungus`; mumbles = 25% secrets — with the item label forced neutral so the tell stays
experiential), famished **devouring** (statues incl. living Stoneshaper statues, rubble,
boulders — full hunger clear; container statues spill their entombed gear), **rename pass 2**
(siphonyx, lullabaster, vertigneiss, quarrelquartz, lattice euclase, conchalk, brine geode,
dotagate, aegischist, hushmica, manymarble, sludgabbro, lodestone trim), and a refinement round:
weight-scaled satiety (≤1 lb = half snack), the **roiling innards** status (eating a gas stone =
50–250 turns of periodic venting **plus full immunity to the gas currently being vented** — via
the engine's own `CheckGasCanAffectEvent` gate; conchalk's protection rotates per random venting),
nuanced pains (shivshale/stalactite = internal Bleeding via the engine's `Internal` flag; siphonyx
= LifeDrain with a null drainer, so the native random-drainer fallback feeds *someone*), and the
grenade action on stones relabeled **"crush"**. Cooking policy for lithovores = **C**: plants and
meats banned (picker-hidden; preset meals punish), stones/liquids/fungus/exotics legal. Rocks stay
**out of village meal generation** (ruled deliberately; future static stonecarver content only).
Test wishes: `kitchenrock` (millstone + pestle + 3× every rock + boulder + cooking skill line incl.
Carbide Chef + optional Lithovore prompt) and `grindcheck` (mill diagnostics).

**Everything is BUILT, DEPLOYED, and was live-tested through the session** (several compile-fix
rounds landed same-day; the old hold-until-verify rule was superseded by continuous deploy+test).

## Read these first
- `CoQ_TerraFirma-dev\TODO.md` — the single source of truth. This session's records live under
  **"Lithovore (rock-eater defect)"** (ruling rounds 1–10 + addenda), **"Stone milling"**, and the
  cooking section's meal-generation ground truth. Verify checklists for every round are itemized
  there — I've been live-testing, so open by asking me what's already confirmed.
- `CoQ_TerraFirma-dev\design\rock-eaten-effects.md` — the eaten-effects + cooking-domain table
  with all rulings, plus the **pass-2 bespoke cooking-parts worklist**.
- Mod source: `CoQ_TerraFirma-dev\TerraFirma\`. Deployed copy:
  `C:\Users\venob\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma` — sync every change
  (`robocopy <source> <deployed> /E`).

## The work (owed / open, in rough priority)

1. **Galvena's eaten-effect** — it was skipped in my eaten-effects list (13 entries for 14 rocks)
   and is still plain. Floated candidate: a small self-discharge arc, snack-grade. My ruling owed.
2. **Hypersonic rock** — the cursed slot: still needs its eaten-effect ("hurts(???)" was flagged
   as needing more flavor; candidates floated: quickness hum, thunderclap daze via EB's part) AND
   its portmanteau name (0-for-11 across two sessions; rejected: machite, velocite, swiftspar,
   howlite, whistleflint, truechert, keenflint, arrowagate, screamscree, machmarble, dartgneiss).
3. **HistorySpice pass** — REQUIRED before Inspired dish-naming looks right: entries in
   `spice.cooking.recipeNames.ingredients` for the ~30 powder blueprints only (grind-first means
   rocks never hit the namer; friendly `AggregatedIngredient` keys are already on every powder —
   FungalRock and GlowingRock shared). Phrase lists (adj/noun/nounPlural) are MY voice — this is a
   writing session. Verify the mod-side spice JSON merge path first (212.17 modding notes suggest
   support; find precedent or test in the Modding Toolkit). Carbide Chef is in `kitchenrock`
   precisely so we can preview dish names live.
4. **Eat-pass 2 (deferred eaten-effects, all marked in XML):** mutation-grant eats (sludgabbro →
   Slime Glands, urchinite → Quills, galvena → Electrical Generation — the generic grant base
   makes each ~6 lines); ash's true Asphyxiating choke; **fungal eats matching the lottery's own
   infection** (currently random via base MutationInfectionOnEat — known deviation); drainstone's
   true inner latch; gemstone eaten-effects (Ego blocked by dotagate — the earmarked concept:
   tie to the relic-style transmute-enemy-into-gemstone effect, serious cost/value).
5. **Pass-2 bespoke cooking domains remaining (worklist in the design file):** compressed ash
   weak-armor (+1 AV unit), shivshale weak-agility, oxidian rust domain (needs a design pass, not
   just code), shieldstone/aegischist force-bubble domain upgrade (borrow armor's OnPenetration
   trigger; bubble code exists), tanglelith major stability (now optional — it holds
   stabilityMinor since gossamarl moved to Web). **Waxshroom's "acts like you ate wax" is blocked
   on ME defining what eating wax does** — no such effect exists anywhere in base (verified).
6. **The big pass-2 umbrellas, parked:** the mutation-themed cooking domain slate (TODO cooking
   section — ranked, unruled) and the Tier-2 `stone` domain with the ceiling-drop signature
   action. Same build style as the Web/Spacetime domains just shipped.
7. **Text pass debt (large):** everything new is DRAFT-marked — Lithovore description, powder/
   stone-meal descriptions, "roiling innards" (ruled only NOT-"burp"; name is a placeholder),
   GasBelly details text, grind messages, warn prompts. Also the mod-wide description pass that
   predates this session.
8. **Draft numbers awaiting my ratification** (all flagged in XML/TODO): mumbles secrets weight
   (25% via comma-list duplicates), spew cadence 8 / density 30, siphonyx drain 7 rounds,
   quill-burst die 1d4, fugue-clone duration 10, lovesick beloved = nearest-visible reading,
   force bubble static + Temporary 12, satiety weight threshold (≤1 = half snack), warns added to
   vitriolite/pulsestone (vetoable), millstone grinding ignoring mechanical power (flagged), the
   gyrestone grind being 3 vortices PER STONE ground, HoldChance 25 (older).
9. **Known deviations accepted-for-now (revisit only if they grate):** lithovore-banned learned
   recipes still nourish (no buff + Ill risk; the ClearHunger call is transpiler-only); roiling
   immunity is full type-immunity rather than partial resistance (the clean seam); whether the
   pestle's own inventory menu entry ever worked is unresolved (rock-side grinding made it moot).
10. **Strange-table cleanup** — mandate reaffirmed this session ("some of the things it has don't
    really make sense"), still held for one-at-a-time decisions (four audit items in the Fissure
    section).

## Engine ground truth — practical notes
- The decompile dump lives in a session scratchpad and is GONE. Re-derive with `ilspycmd`
  (installed) against `C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\
  Managed\Assembly-CSharp.dll` — full dump `-p -o <dir>` takes ~a minute and is worth it.
  Game XML at `CoQ_Data\StreamingAssets\Base\`.
- Hard-won facts this session (trust, but they're re-derivable): all base `*OnEat` parts fire on
  the string event `"OnEat"` sent to the FOOD object; every gas behavior gates through
  `CheckGasCanAffectEvent` (the immunity seam); `ProceduralCookingEffect.CreateSpecific` builds
  effects from class-name strings; PreparedCookingIngredient comma-lists pick ONE random unused
  option per cook (duplicates = weights — that's the mumbles 25%); `GetCookingActionsEvent` is
  sent to actor AND campfire but does NOT cascade into inventory; base preserved ingredients are
  `Satiation="None"` (raw ingredients do nothing — our raw-eating layer deliberately exceeds base).
- Two recurring mod-side hazards, both bit us repeatedly: **XML comments cannot contain `--`**
  (validate with a PowerShell `[xml]` cast before every deploy), and **base MUTATION classes can
  shadow effect classes** inside `XRL.World.Parts.Mutation` files (LifeDrain did; fully qualify).
- Lithovore architecture: eat/devour lives in `Mutation_Lithovore.cs` (channel tags documented in
  its header comment); milling in `Part_StoneMill.cs` (homes: Pestle, Campfire, Millstone via
  Merges.xml + rock-side via BaseStone/Gemstone); meal policy = 3 Harmony patches in
  `_HarmonyPatches.cs`; venting in `Effect_GasBelly.cs`; cuisine tells in `Part_FungalLottery.cs`
  (ObjectCreated restamp — part ORDER matters, PCI must precede the lottery part).

## How we work — unchanged, read carefully
- **Design is collaborative.** Engine facts, decompiled precedents, benchmarks, and feasibility
  are your job; concepts, names, numbers sign-off, and flavor are mine. (Standing exception: when
  I explicitly ask for name/text suggestions, offer candidate slates — formula: effect-root fused
  with a stone-morpheme, vantabsidian-style; ration "-ite" [budget SPENT: urchinite/mycolite/
  vitriolite], limit morpheme reuse; they don't all have to be fused — see "brine geode".)
- **Discuss then implement:** specs with concrete numbers get my sign-off before code. Small
  unambiguous fixes can just be done. Ground truth over memory — verify every API and blueprint
  name by decompiling or grepping; never trust recall of Qud internals.
- All player-facing text you draft is marked DRAFT in the XML for my text pass.
- Update TODO.md as decisions land. Sync to the Mods folder after builds (robocopy above), and
  validate XML before every sync. Git commits (if any) carry NO Co-Authored-By trailer.

Start by reading TODO.md's Lithovore section and the design file, then ask me two things: what my
live-testing has already confirmed or broken from the round 1–10 verify checklists, and whether we
open with fixes, the galvena/hypersonic loose ends, or the HistorySpice writing session.
