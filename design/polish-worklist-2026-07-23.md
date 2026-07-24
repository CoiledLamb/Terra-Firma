# Polish Worklist — compiled 2026-07-23 at verification-session wrap

Every open flag from the 2026-07-22/23 verification session plus all pre-existing design debt,
deduplicated from TODO.md and the design docs (241 raw extractions → the list below).
**TODO.md remains the source of truth** — entries here are the index for the mutation-by-mutation
polishing pass; full context lives in the referenced TODO sections.

Tags: 🆕 flagged this session · 📜 older debt · `[design]` needs a ruling · `[ratify]` a Claude
number/text awaiting blessing · `[polish]`/`[text]` writing or cleanup work · `[bug]` behavior call.

---

## Pass 1 — Earthen Barrage + Gravel Whorl

- [x] 🆕 `[bug]` **Deflected-thrown-item placement + fragility** — RULED mark-the-projectile + BUILT +
  DEPLOYED 2026-07-23 (new `Cleo_TerraFirma_WhorlDeflectedPart`; details in TODO.md).
- [x] 🆕 `[design]` **Devoured non-stone real items obliterated** — RULED KEPT 2026-07-23 ("you deserve
  to find out"); rider BUILT: ALL devoured grenades now detonate at the defender, not just mod stones.
- [x] 🆕 `[polish]` **Whorl verb conjugation** — FIXED + DEPLOYED 2026-07-23 (all nine call sites,
  verbs pre-conjugated + `Actor:` → `Source:`; full diagnosis in TODO.md). `[verify]` in-game reads.
- [x] 🆕 `[design]` **Ricochet VFX** — RULED cheap-particle-line + BUILT + DEPLOYED 2026-07-23
  (staggered gravel blips + endpoint dust puff); richer animations flagged for a later pass.
- [x] 🆕 `[ratify]` **Whorl DRAFT texts — ALL RATIFIED 2026-07-23** ("The whorl thickens.", ricochet
  range 5, verb "scatter", reabsorb message; description tails reworded to "Lasts while stones
  remain in reserve."). **Carry-forward flag (WIDENED 2026-07-23): ALL mutation and description
  text mod-wide is earmarked for the base-game-voice standard at the final text pass** (trigger
  was the resonant dashes, but the mandate is "as close to base game language as possible" everywhere).
- [x] 📜 `[design]` **Resonant whorl DV bonus** — RULED +2/+4 + BUILT + DEPLOYED 2026-07-23
  ("DV is less valuable than AV so you need more to compensate").
- [x] 🆕 `[design]` **Mullstone refresh dial** — RULED guarantee-exactly-ONE + BUILT + DEPLOYED
  2026-07-23 (new `Cleo_TerraFirma_RefreshOneCooldownOnEat` part; 100% "vastly overcorrecting").
- [x] 📜 `[polish]` **Bespoke Attune Stones icon — LANDED + WIRED + DEPLOYED 2026-07-23** (user art,
  `abil_attunestone.png`, 16x24, house black-main/white-detail palette; moved into `Textures/Abilities/`,
  `UITile` at `Abilities.xml:35` repointed, cyan C/c mapping kept). Single sprite for now — user may
  double back with a second state; if so, adopt the Take Stones two-line `State="ToggleOn"` idiom.
  `[verify]` icon renders on the ability bar next boot (flip to `Foreground="c" Detail="C"` if the
  bright/dark emphasis reads inverted).
- [x] 🆕 `[ratify]` **GasBelly cadence drift — RATIFIED AS OBSERVED 2026-07-23** (SpewEvery=8 stays;
  "1 turn only roughly equates to 1 turn" is accepted base-game physics; no text promises a number).

## Pass 2 — Fissure + the Rock Catalog *(the thickest folder)*

**Geometry / cast — the 9-tier Burgeoning realignment RULED + BUILT + DEPLOYED 2026-07-23**
(architecture adopted; primes retired + seam batching in; deep four tier-9 plain; filler trio
live; mullstone→T6; pits deferred; full record + sim + verify checklist in TODO.md):**
- [~] 🆕 `[design]` **Depth QUANTITY buff — RULED 2026-07-23, build held with the restructure:**
  spill scaling (a) YES (20%+5%/step, freeze at 50%); +falls (b) HELD as future dial; quality cap
  (c) +8 reached at Z50, flat below (old "engine floor Z=50" decomp claim struck — wrong).
- [x] 📜 `[design]` **Prime falls scale?** — ABSORBED by the restructure (moot under the
  Burgeoning ±spread; primes' own fate is an open restructure ruling).
- [x] 📜 `[ratify]` **Carried-over v1 numbers — RULED 2026-07-24:** cost 3 MP ratified; cooldown
  RESHAPED to `105 − 8×Lv, floor 30` ("2x as slow at max instead of 4x" vs Burgeoning's 15) —
  BUILT + DEPLOYED, level text renders the new value automatically.
- [ ] 📜 `[polish]` **Multi-fall sound stack** — if per-cell slams are too loud, keep only the cast slam.

**Strange rock (one-at-a-time bundle, user-held):**
- [ ] 🆕 `[design]` **Band placement** — ~29% of Lv1 casts produce one; move to Band2New or cut
  band-1 weight to ~2. *(2026-07-23: ABSORBED by the 9-tier restructure if it lands — every
  placement gets re-decided there; revives as-written only if the restructure is vetoed.)*
- [ ] 📜 `[design]` **Audit 1: mimic fix** — place-then-fire repairs the three dud mimics
  (lodestone/thrown-drainstone/crack) vs a curated strange-specific table.
- [ ] 📜 `[design]` **Audit 2: deep reach** — keep the ~5% band-4 trickle in strange's pool or cut it.
- [ ] 📜 `[design]` **Audit 3: deep sting** — mimicked deep effects currently cost the thrower nothing.
- [ ] 📜 `[design]` **Audit 4: wild spawns** — strange-spawned creatures arrive unallied
  (ape neutral, statuette baboon-troop). Keep wild / ally / cut creatures from the pool.

**Rock-catalog design calls:**
- [ ] 🆕 `[design]` **Chasm → visible pit** — user LEANING yes, not locked ("a lot of moving
  parts"). If adopted, holes must show the terrain below (PitMaterial/DropPit recipe reconned,
  in TODO drawing board); 3x3 native patch vs 1-tile subclass at build.
- [ ] 🆕 `[design]` **Deep stones want heft** — bigger rock, higher damage, weight back up (e.g. 5),
  size/description reading larger, crush optionally up.
- [ ] 🆕 `[design]` **Pool-volume audit — AUDITED 2026-07-23, decision package ready.** Findings:
  SaltyWater/Slime/Acid/Salt puddles all spawn at a flat 500 drams (hence "pool" renders — one
  pebble births a water source; a waterskin holds 32); but base `OilPuddle` already ships
  `StartVolume="10d10"` (10–100 drams, renders puddle) and the deep byproduct patches use 15–20.
  **The engine's own fix pattern is StartVolume.** Proposal to rule: mod child puddles (pure XML)
  with StartVolume dice — your floated step=splash vs break=full-spill maps to two dice (e.g.
  ~2d10 step, ~10d10 throw), and the pour-into-container park item should draw from the same
  per-rock dram budget. Numbers yours.
- [x] 🆕 `[design]` **Aegischist thrown = impassable cage — BUILT + DEPLOYED 2026-07-23.** Hits cage
  the victim (bubble-shard ring, shared `PlaceForceRing`); misses keep the wall line. Both recon
  questions answered by decomp: `Unwalkable` forcefields reject EVERYONE (no owner-pass leak —
  the prisoner can't slip out), and partial cages against walls/bystanders accepted (the wall is
  part of the cage). Ratify rider: cage duration = the bubble shard's 12 rounds (step parity;
  arguably the full-strength channel deserves more — DRAFT). `[verify]` thrown hit → ring closes,
  victim trapped; miss → wall line at landing.
- [x] 🆕 `[design]` **Drainstone vs inorganics — BUILT + DEPLOYED 2026-07-23** per the user lean:
  attach-but-no-leech on inorganics (detection = the engine's own `LifeDrain.IsValidTarget`
  IsOrganic gate — better than NoBleed, robots bleed oil), rides out its rounds, then distinct
  endings: gorged "bursts like a blood balloon, finally sated" vs starved "withers and crumbles
  away, dry and unfed" (both DRAFT, phrases from the ruling). `[verify]` latch a robot → no drain,
  dry crumble; latch flesh → drain + burst.
- [x] 🆕 `[design]` **Caromcobble trip-on-step — BUILT + DEPLOYED 2026-07-23** (StepChance 25 +
  Prone via the StepPayload override; HoldChance 100 = a permanent banana peel, intended).
  Numbers ride the ratify pile. `[verify]` walk an enemy over it → 25% prone, rock survives.
- [ ] 🆕 `[design]` **StoneStep coverage** — three candidates, decide together: ape statue (the
  acid-bane A/B evidence), Stoneshaper statues (C# — runtime creation path), future stonefrog.
- [ ] 🆕 `[design]` **Stone creatures catch fungal infections** — keep (stone grows gills) vs immunize.
- [ ] 🆕 `[design]` **Allied geyser hit the player once** — watch item; audit target selection if it recurs.
- [ ] 📜 `[design]` **Mutating no-ops silently on robots/purestock** — "(acceptable?)" needs an answer.

**Rock-catalog ratifications — SWEEP RUN 2026-07-24, everything ratified except two revisions:**
- [x] 📜 `[ratify]` RATIFIED: HoldChance 25 · AcidResistance −50 · omnigeyser stats · figurine
  +5/50 · drainstone dice/durations (band placement moot post-realignment) · spiderweb 3/12 ·
  phosphorescent 25% step proc · fungal tints · caromcobble full set · farflint 25 · pebble
  weight 0 · half-snack satiety · aegischist cage duration 12. **REVISED + BUILT + DEPLOYED:
  ape statue PERMANENT → lifespan 500–1000 then crumbles to devourable rubble; baboon statuette
  gains the same lifespan and stiffens into the NEW `BaboonFigurineItem` (AddsRep Baboons 200
  DRAFT, Commerce 30) — the catalog's original rep half finally lands via new generic
  `Part_TimedForm` (pocket stones spill on expiry). The pocket-form pass still owes item→creature.**
  `[verify]` ape crumbles to rubble at expiry; statuette stiffens to figurine + spills stones;
  figurine grants baboon rep.
- [ ] 📜 `[design]` **PARKED CONVERSATION (user, 2026-07-24): hold rate / fragility / natural
  expiry as one system** — "interested in a conversation about hold rate, fragility, and natural
  expiry at a certain point, but that can wait." The dials: HoldChance 25, DullAfter 1000–2500,
  TimedForm 500–1000, healstone HoldChance 0, caromcobble 100.
- [ ] 🆕 `[design]` **Stale +100 throw accuracy on the playtest character** — keep the god-arm or
  strip via one-line wish next session.

**Geysers (their own mini-pass):**
- [ ] 📜 `[design]` **Body plan + stats** — Turret anatomy vs custom Bodies.xml vent/maw; strip the
  unneeded melee; ratify draft stats. Fold in: 🆕 `[polish]` **rules text showing** (strip per
  grenade idiom) and 🆕 `[design]` **visible equip-before-throw feel** (accept vs silence).
- [ ] 📜 `[polish]` **Placeholder stalagmite tile + Fungus-anatomy naming wart.**

**Text (rides the whole catalog):**
- [ ] 📜 `[text]` **Every rock description is DRAFT** — v1 fifteen + all waves + deep four; plus the
  Fissure level-text final pass and 🆕 the muse/statue text items in Pass 4.

## Pass 3 — Lithovore + Kitchen

- [x] 🆕 `[polish]` **Devour pronoun bug — FIXED + DEPLOYED 2026-07-23.** `.them` was already the
  correct objective property; the statue's pronoun PROVIDER is what yielded "its". Fix: literal
  "it" — a statue is an object whatever its muse was, so hardcoded neuter is always right.
- [x] 🆕 `[design]` **Universal statue devourability — AUDITED + FIXED + DEPLOYED 2026-07-23.**
  Suspicion confirmed: the base "Statue" blueprint family carries NO Statue part (Eater statues,
  Oboroqoru, Keresh, all six Stilt shrine statues — pure Render+Physics). Detection broadened via
  blueprint descent (the Rubble/BaseBoulder idiom); gold Eater statues ride along — universal
  means universal. `[verify]` a famished lithovore can devour a Stilt shrine statue.
- [ ] 📜 `[polish]` **HistorySpice.json entries — REQUIRED; merge path VERIFIED 2026-07-23, fully
  de-risked.** Decomp ground truth: the engine natively merges any mod file named
  `historyspice.json` (JSON union merge, `HistoricSpice.Init`); the recipe-namer's lookup key is
  the **`AggregatedIngredient` tag value** — already set on every powder ("BrineGeode",
  "Mullstone", "Healstone"…); entry shape = `adj`/`adj1`/`adj2`/`noun`/`nounPlural` phrase lists.
  All that remains is the writing itself (~33 keys) — a pure phrase-drafting sitting, zero
  technical risk. Carbide Chef is in `kitchenrock` for live preview.
- [ ] 📜 `[design]` **Eat-pass 2 worklist** — mutation-grant eats (sludgabbro→Slime Glands,
  urchinite→Quills, galvena→Electrical Generation), ash's true choke, fungal eat matching the
  lottery's own infection, drainstone inner latch, gemstone eats (earmark: transmute-enemy-into-
  gemstone), SecretsOnEat shelf.
- [ ] 📜 `[design]` **Pass-2 bespoke cooking parts** — tanglelith major-stability, ash weak-armor
  (+1 AV unit), shivshale weak-agility, oxidian rust domain (needs real design), waxshroom's wax
  definition (blocked on you inventing what eating wax does), shieldstone force-bubble action;
  examine gyrestone-spacetime / drainstone-HP / gossamarl-Spinnerets keep-or-upgrade calls.
- [ ] 📜 `[design]` **Cooking umbrellas** — the mutation-domain slate (which mutations, slot shapes,
  packaging 2–3 domains vs one, carriers) and the Tier-2 `stone` domain with the ceiling-drop
  signature action; Minor-vs-full tiering lever; six flags on the pass-1 assignment (twins'
  pot-gamble, electric-no-Minor wart, redundant pairs, eaten/cooked unification, grenade-in-pot,
  grind fork); stone-soup named recipe (park unless wanted).
- [x] 📜 `[ratify]` **Round-dial sweep — RUN 2026-07-24, mostly resolved.** RATIFIED: mumbles 25%
  secrets ("okay ratio"), roiling innards name (language flyover owed at the text pass), quill 1d4,
  lovesick nearest-visible + self-love-when-alone ("exactly as expected"), vitriolite/pulsestone
  warns (Qud-standard, acid-container precedent), soupstone 5000 / temps ±150 / devour-as-full-meal.
  CHANGED + BUILT + DEPLOYED: siphonyx eat → new `StoneSiphon` effect (drain goes NOWHERE, flat
  2/round for 7, full heal-burst at natural expiry; early removal returns nothing) · urchinite eat
  10% self-prick (+ snapshot-loop fix, geyser-MODERROR class) · blurring eat 10% second clone ·
  shivshale bleed 1→1d2, stalactite 1→1d8 · millstone grinding now REQUIRES the mill to be
  powered/turning ("The millstone is still." DRAFT; pestle = the unpowered fallback; user lean,
  vetoable at verify).
- [ ] 📜 `[ratify]` **Sweep residue (the escapees):** 🔁 **galvena arc dice/range — SKIPPED BY THE
  USER A SECOND TIME** (13-for-14 again; the stone is dodging ratification) · thunderclap eat —
  FLAGGED FOR REVISIT (user cooled on it now that dazestone is the canon daze rock; consider a new
  streakschorl eat at eat-pass 2) · static-bubble + aegis self-cage — deferred to their OWN pass
  (user) · per-rock powders — explanation owed in another pass before ruling · streakschorl leg
  range 6 — elaboration delivered, ruling pending.
- [ ] 📜 `[design]` **Accepted deviations — revisit only if they grate** — banned learned recipes
  still nourish; gas immunity is full-type not partial; pantry rules text stays commented out
  (grenade idiom); pestle-in-inventory mystery (moot since rock-side grinding).
- [ ] 📜 `[design]` **Lithovore scope fork residue** — is the edible set all-IsAnyStone (Small
  Stones/EB pebbles as rations) or curated; deep-four eaten pulse numbers; statue-eating detail
  polish (satiation value, whole-vs-bites).
- [ ] 📜 `[text]` **Lithovore texts** — BearerDescription "lithovores", defect tile art, all
  session-DRAFT strings (grind messages, warn prompts, GasBelly details).

## Pass 4 — Stoneshaper

- [x] 🆕 `[design]` **Muse-weapon naming — RESOLVED VERBATIM 2026-07-23** (user in-game check post
  muse-copy fix: "seem fine on a cursory glance"). The "stone " prefix idea stays pocketed if a
  future statue's features grate.
- [x] 🆕 `[design]` **Fungal growths in the muse copy — RULED KEEP 2026-07-23** ("carved as the muse
  appeared, gills and all"); leans the Pass 1 stone-creatures-catch-infections flag toward keep too.
- [x] 🆕 `[design]` **Pre-Lv10 no-second-statue friction — RULED EXTEND + BUILT + DEPLOYED
  2026-07-23** ("cooldown gates it most of the time anyways"): casting now swaps at ANY level —
  prompt guards every living statue, old one de-animates into the lootable container, normal
  cooldown applies. Lifetime-as-cooldown display RETIRED with its three normalization branches;
  accepted cost: the ability card no longer counts down statue lifetime (flag at text pass if it
  grates). `[verify]` low-level recast prompts + swaps; expiry/kill no longer touch the cooldown.
- [ ] 📜 `[design]` **Taunt balance watch** — your note at revival: "hopefully not too strong."
- [ ] 🆕 `[design]` **Lifetime-as-cooldown retirement — REVIEW WATCH (user flag, 2026-07-23):** the
  swap-down build removed the pinned cooldown display + its three normalization branches; watch
  for unexpected side effects (cooldown oddities after statue death/expiry, save/load of mid-life
  statues, anything that read `StatueLifetimeRemaining` assuming the old rhythm).
- [ ] 📜 `[text]` **Stoneshaper text pass** + Ctesiphus "phosphorescent" description quirk.
- [x] 📜 *(housekeeping)* **DONE 2026-07-23** — the four ruled-and-built checkboxes closed (natural-
  weapons ruling, Lv10 permanence, container-statue spec, Distraction revival) and the
  contradictory "user ruling needed" note struck (answered by the Lv10-permanence ruling).

## Pass 5 — Gorgon's Gaze

- [x] 📜 `[design]` **Damage-breaks-gaze held lever — RULED KEEP no-break 2026-07-23** (it's the
  low-level survival mechanism: tank hits while channeling; a damage break would worsen exactly
  the low-level weakness the user flagged).
- [x] 📜 `[design]` **Per-tick LOS-break dial — RULED PARK 2026-07-23** with the creatures/quarriers
  workstream (only matters for wild gorgon channels).
- [x] 🆕 `[design]` **Save-DC curve REBALANCED + DEPLOYED 2026-07-23** (user find: low-level kill
  security + the 26%→91% Lv1→4 cliff). Formula `8 + 2×Lv` → **`11 + Lv`** (one-line change; riders
  — mark 4→2 fails, AV swap — VETOED, mark unchanged). Simmed (gaze_curves.py): vs equals
  56/83/97/99 by Lv1/4/7/10 (was 26/91/100/100); trash at Lv1 ~70%; boss-tier 16/40/70/83 becomes
  the true difficulty curve (was saturating at 99% by Lv7). `[verify]` show-save-rolls: opening DC
  = 12+Lv+WIL in the log.
- [ ] 📜 `[text]` **Gaze text already house-voice** — confirm no residue at the text pass.

## Pass 6 — Calcify

- [x] 📜 `[design]` **NPC-caster gap — RULED batch-now + BUILT + DEPLOYED 2026-07-23** ("let's
  knock calcify off the list"). AI advertises on DEFENSIVE + RETREAT channels only — never
  offensive, since calcified stone is opaque both ways and Force Wall's owner-shoots-through
  trick doesn't apply; threat = event target or Force Wall's nearest-visible-hostile scan.
  NPC cell-picking = perpendicular line across the approach, anchored adjacent to the caster
  (honoring Calcify's own contiguity rule); occupied cells skipped by the placer, partial lines
  accepted. `[verify]` wish Calcify onto a creature, hurt it, watch it wall you off (unlocks the
  existing NPC-caster appendix item).
- [ ] 📜 `[text]` **Calcify text pass.**

## Pass 7 — Geomancy

- [ ] 📜 `[text]` **Geomancy text pass.** *(Mechanically closed this session — reveals, mask fix,
  EB-pebble synergy all confirmed; only gemstone-scan + save/load verify remain.)*

## Cross-cutting / release logistics

- [ ] 📜 `[ratify]` **Manifest description** — two drafts benched 2026-07-23 (see TODO.md note);
  needs a fresh angle, placeholder still ships.
- [ ] 📜 `[polish]` **Statue Maker item description** — "placeholder description >:)".
- [ ] 🆕 `[polish]` **Strip `[TF-VERIFY]` instrumentation before release** — kill switch
  `Helpers.VERIFY_WATCH`, or delete the call sites (now ~15 files after the session's additions).
- [x] 📜 `[release]` **git init — DONE 2026-07-23** (fresh repo, no fork linkage, no trailer; history live since `b4ed68e`).
- [ ] 📜 `[release]` **Workshop publishing plan + credits.**
- [ ] 📜 `[design]` **Stale `Cleo_TerraFirma_Ephemeral Stone` blueprint** — deletion recommended,
  awaiting your ruling.
- [x] 📜 *(housekeeping)* **Reconcile stale TODO state — DONE 2026-07-23** (first-boot/DISABLED/
  deploy-hold lines corrected, deploy states verified against the Mods folder; the "duplicate
  whorl entries" turned out to be compile-side only, nothing to dedupe). Stoneshaper's four
  ruled-and-built boxes closed the same sweep.

## Side content (committed / parked workstreams — rule when you get there)

- [ ] 📜 **Stabilizer bands** — deploy-state check, world wiring (Stonecarver gear? merchants?
  loot?), description + Commerce 15/Tier 2 ratify.
- [ ] 📜 **Chalk** (damage vs accuracy + resource model) · **Sling fork** (worn booster vs true
  MissileWeapon) · **Visor** (binned, revive later) · **accuracy-stacking ceiling** ruling.
- [ ] 📜 **Quarriers** — how stone leaves Qud (dromad ride-alongs / mole-beast), mole-beast
  weirdness axis, drover's design gap, faction name + display text, mutation-to-type assignment
  (commits EB/Stoneshaper/Gorgon AI handlers; keep pack NPCs sub-Lv10), 3x3 lump tech (rpm chunk
  vs C# painter), camp commit/cut, malformed cragmensch (commit, name, placement, band), Stilt
  merchant identity, non-expiring merchant stones mechanism.
- [ ] 📜 **Stonefrog** — six calls queued (disguise depth, numbers, MentalShield, corpse-rock,
  cold-weakness, wild tables).
- [ ] 📜 **Figurine pocket-form pass** — item↔creature transformation + AddsRep, after the creature
  form verifies (it has).
- [ ] 📜 **Parked design budget** — ceiling creatures (your shortlist), wall-growth harvestables,
  stonecarver static content (the lithovore's restaurant), lore items/books, wall kick, Geomancy
  consumable, transmute-gemstone concept, throw-augmenting late-game items; 🆕 geode family
  (beyond brine geode), 🆕 pour-into-container on the liquid rocks (ties to the pool-volume audit).

## Appendix — remaining verify items (testing leftovers, not polish)

Kitchen round (gyrestone mill safe, Inspired dish name, cooked domains firing, boulder 5–6,
picker bans, bespoke eats: thunderclap/self-arc/siphonyx/hexstone-tether) · staged-fix smoke test
next boot (gravelly haymakers on humanoid statues, stone reabsorb + charge refund, green surface
message, urchinite-crowd no-crash, wall-source carve log) · resonant physical-passthrough + Lase
beam scatter + ÷-pen numbers · whorl dangerous-stone devour · surge field arcing (`[SURGE]` has
never fired) · scavenger `[SCAV]` fetch (never fired) · baboon pocket counts (Hulking 2–3 /
Quartz 0) · wall-crush 15% instakill · NPC Fissure/Gorgon casters (unlocks dead-gorgon +
stunned-caster) · crack walk-over + chasm swallow · lovestone/fugue reverts (self-logging) ·
show-save-rolls WIL glance · gemstone scan + Geomancy save/load · Stoneshaper weird anatomies +
Young Ivory fail + Lovesick-egg re-check on purpose · Calcify AV-on-look + fallback blueprint ·
EB sounds at 1/5/10 + daze visual · stabilizer bands checklist (post-deploy) · full-chaos
save/load gauntlet · fresh-character session: Stonecarver chargen, Boar Vessel, Burrowing Urchin
(+ encounter-table question), Lithovore chargen slot · sound-pass listen-through.
