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
  bright/dark emphasis reads inverted). **Icon verify MARKED OFF for this pass (user 2026-07-24:
  "i might need to edit attune stone later so mark it off now but keep it on the flag for next
  verify [pass] after we modify it") — carry-forward: re-verify the icon in the NEXT verify pass
  once the planned attune-stone edit lands.**
- [x] 🆕 `[ratify]` **GasBelly cadence drift — RATIFIED AS OBSERVED 2026-07-23** (SpewEvery=8 stays;
  "1 turn only roughly equates to 1 turn" is accepted base-game physics; no text promises a number).

## Pass 2 — Fissure + the Rock Catalog *(the thickest folder)*

**Geometry / cast — the 9-tier Burgeoning realignment RULED + BUILT + DEPLOYED 2026-07-23**
(architecture adopted; primes retired + seam batching in; deep four tier-9 plain; filler trio
live; mullstone→T6; pits deferred; full record + sim + verify checklist in TODO.md):**
- [x] 🆕 `[design]` **Depth QUANTITY buff — RULED 2026-07-23 + BUILT + DEPLOYED** (closed on
  code-verify 2026-07-24: `GetSpillChance` 20%+5%/step frozen at 50%; `GetDepthBonus` cap +8 at
  Z50). +falls (b) stays HELD as a future dial — deliberately unbuilt, not a gap.
- [x] 📜 `[design]` **Prime falls scale?** — ABSORBED by the restructure (moot under the
  Burgeoning ±spread; primes' own fate is an open restructure ruling).
- [x] 📜 `[ratify]` **Carried-over v1 numbers — RULED 2026-07-24:** cost 3 MP ratified; cooldown
  RESHAPED to `105 − 8×Lv, floor 30` ("2x as slow at max instead of 4x" vs Burgeoning's 15) —
  BUILT + DEPLOYED, level text renders the new value automatically.
- [ ] 📜 `[polish]` **Multi-fall sound stack** — if per-cell slams are too loud, keep only the cast slam.

**Strange rock — AUDIT RUN 2026-07-24 ("everything it does should actually happen, no null"),
three of four closed, BUILT + DEPLOYED:**
- [x] 🆕 `[design]` **Band placement** — RESOLVED by the restructure (strange seasons the generics
  of tiers 2–8 at weight 5; its own roll pool is now separate, below).
- [x] 📜 `[design]` **Audit 1: mimic fix — BOTH remedies taken.** New curated
  `StrangeRocksPool` (payload/grenade rocks only; CUT: creatures, terrain, generics, streakschorl
  payloadless, caromcobble whose bounce can't fire in mimicry, strange itself) AND place-then-fire
  (the surrogate lands in a real cell before its payload runs) — which REPAIRS lodestone's pulse,
  crack's pull-down, and drainstone's latch into the pool; a latched strange-drainstone even
  survives on the victim instead of being cleaned up. The Latch channel itself got smarter:
  at-rest stones latch immediately, only true mid-flight throws defer.
- [x] 📜 `[design]` **Audit 2: deep reach — KEPT**, deep four at half weight in the curated pool.
- [x] 📜 `[design]` **Audit 3: deep sting — BLESSED FREE 2026-07-24** ("blessing it here"): mimicked
  deep effects cost the thrower nothing; the bite-back lives on the real stones only. The strange
  bundle is CLOSED.
- [x] 📜 `[design]` **Audit 4: wild spawns — CUT** ("cutting the weirder spawns"), no creatures in
  the pool. `[verify]` a dozen strange throws: every one visibly does something (gas, detonation,
  latch, floor opening, pulse); Lithovore strange-eat still resolves (same pool, fewer wasted rolls).

**Rock-catalog design calls:**
- [x] 🆕 `[design]` **Chasm → visible pit — SPLIT RULED 2026-07-24: chasm CUT** (out of the tier-9
  table, blueprint dormant) **+ BUILT + DEPLOYED; pits stay unlocked** ("too spicy to lock in
  now") — the visible-pit successor gets its own future pass, PitMaterial/DropPit recon banked
  in the TODO drawing board.
- [x] 🆕 `[design]` **Deep stones want heft — BUILT + DEPLOYED 2026-07-24** (numbers DRAFT for
  veto): weight 1→5 (the flag's own number; knock-on: deep four now eat as FULL meals via
  size-scaled satiety — fitting), thrown 1d3/pen4 → 2d4/pen6, crush 3d6→4d6. Descriptions
  reading larger = owed at the text pass (noted). `[verify]` heft feels right in hand + the
  full-meal eat.
- [x] 🆕 `[design]` **Pool-volume audit — RULED + BUILT + DEPLOYED 2026-07-24** (revised same day
  2d8 → **4d8**, 4–32 drams). Five mod child puddles (Brine/Slime/Oil/Salt/Acid, StartVolume
  4d8) replace the flat-500 base spawns across all eight references — thrown splashes, step
  puddles, sludgabbro/vitriolite eaten-spawns, and deep vitriolite's byproduct pools. The
  pour-into-container park item now has its dram budget: 4d8. Deep lava/convalessence patches
  (15–20) untouched — elemental residue, not held liquid. `[verify]` a brine geode splash renders
  "puddle," not "pool."
- [x] 🆕 `[design]` **Liquid rocks spill on destruction — RULED + BUILT + DEPLOYED 2026-07-24**
  (emerged from the user's "does breaking make a puddle?" question): a liquid rock destroyed by
  incidental damage releases its puddle where it dies ("X breaks open, spilling what it held."
  DRAFT). Deliberately silent on: obliteration (whorl devoured it), decay (dulls dry), and
  use-path breaks (the payload already puddled — suppressed against double-spills).
  `[verify]` burn a brine geode in a fire → puddle; throw-break → exactly ONE puddle.
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
- [ ] 🆕 `[design]` **StoneStep coverage — DEFERRED 2026-07-24 by design** ("there may be more
  creatures and i want them all to have this conversation in tandem"): the whole stone-creature
  roster rules together when it's fuller. Candidates so far: ape statue, Stoneshaper statues
  (C# carve path), stonefrog, whatever quarriers adds.
- [ ] 🆕 `[design]` **Stone creatures catch fungal infections** — keep (stone grows gills) vs immunize.
- [ ] 🆕 `[design]` **Allied geyser hit the player once** — watch item; audit target selection if it recurs.
- [x] 📜 `[design]` **Mutating no-ops silently on robots/purestock — ACCEPTED 2026-07-24** ("that's
  fine"); the engine gate stands, the radioactive twin simply wastes its lottery on the unmutable.

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
  All that remains is the writing itself — a pure phrase-drafting sitting, zero
  technical risk. Carbide Chef is in `kitchenrock` for live preview. **Prep sheet compiled
  2026-07-24: `design/historyspice-prep.md`** — the real key list is 36 (pulled from Rocks.xml;
  the ~33 estimate corrected), plus the StoneMeal/dazestone/ash edge cases and seed drafts.
- [ ] 📜 `[design]` **Eat-pass 2 worklist** — mutation-grant eats (sludgabbro→Slime Glands,
  urchinite→Quills, galvena→Electrical Generation), ash's true choke, fungal eat matching the
  lottery's own infection, drainstone inner latch, gemstone eats (earmark: transmute-enemy-into-
  gemstone), SecretsOnEat shelf.
- [ ] 📜 `[design]` **Pass-2 bespoke cooking parts** — tanglelith major-stability, ash weak-armor
  (+1 AV unit), shivshale weak-agility, oxidian rust domain (needs real design), waxshroom's wax
  definition (blocked on you inventing what eating wax does), shieldstone force-bubble action;
  examine gyrestone-spacetime / drainstone-HP / gossamarl-Spinnerets keep-or-upgrade calls;
  🆕 **deep galvena stronger electric domain (user flag 2026-07-24: "deep galvena needs a
  stronger electric cooking effect")** — its powder currently carries the same plain
  `type="electric"` as regular galvena's, so the deep stone cooks identically to its T1 cousin;
  wants a bespoke stronger-electric tier (the ash `armorMinor` precedent, inverted: base domain
  exists, the mod builds the heavy sibling).
- [ ] 📜 `[design]` **Cooking umbrellas** — the mutation-domain slate (which mutations, slot shapes,
  packaging 2–3 domains vs one, carriers) and the Tier-2 `stone` domain with the ceiling-drop
  signature action; Minor-vs-full tiering lever; six flags on the pass-1 assignment (twins'
  pot-gamble, electric-no-Minor wart, redundant pairs, eaten/cooked unification, grenade-in-pot,
  grind fork); stone-soup named recipe (park unless wanted).
- [x] 📜 `[ratify]` **Round-dial sweep — RUN 2026-07-24, mostly resolved.** RATIFIED: mumbles 25%
  secrets ("okay ratio"), quill 1d4,
  lovesick nearest-visible + self-love-when-alone ("exactly as expected"), vitriolite/pulsestone
  warns (Qud-standard, acid-container precedent), soupstone 5000 / temps ±150 / devour-as-full-meal.
  CHANGED + BUILT + DEPLOYED: siphonyx eat → new `StoneSiphon` effect (drain goes NOWHERE, flat
  2/round for 7, full heal-burst at natural expiry; early removal returns nothing) · urchinite eat
  10% self-prick (+ snapshot-loop fix, geyser-MODERROR class) · blurring eat 10% second clone ·
  shivshale bleed 1→1d2, stalactite 1→1d8 · millstone grinding now REQUIRES the mill to be
  powered/turning ("The millstone is still." DRAFT; pestle = the unpowered fallback; user lean,
  vetoable at verify).
- [ ] 🆕 `[text]` **"Roiling innards" — status updated 2026-07-24, second ruling: "undecided -
  roiling innards fine for now but talk has to be had before release."** No longer must-change —
  must-DISCUSS; the conversation is now a release-gate item (see Cross-cutting). Original flag
  (earlier 2026-07-24 — the name squicked prospective players):
  the name squicked prospective players; wanted = evocative, not grossout — shift register from
  anatomy to GEOLOGY (the stone is still doing stone things). Candidates on the table:
  **outgassing** (Claude rec — the real term for gas escaping rock, deadpan-Qud), **gas pocket**
  (miner's term), **fumarole** (you are briefly a volcanic vent). Vent message "Gas roils out of
  you." acceptable under any name; details text already clean. One rename touches DisplayName +
  comments; do at the text pass or sooner.
- [ ] 📜 `[ratify]` **Sweep residue — THREE OF FIVE CLOSED 2026-07-24:** ✅ **galvena arc 1d4/range 5
  — RATIFIED at last** ("galvena arc is fine"; the third asking landed) · ✅ **streakschorl leg
  range 6 — RATIFIED** ("streakschorl leg range fine") · ✅ **per-rock powders — BLESSED** ("fine
  by me! i was looking forwards to the history spice for each rock feeling a bit unique" — the
  blessing's rider: per-rock uniqueness in spice entries is WANTED; keep shared keys to the two
  no-tell cases). Still open: thunderclap eat — FLAGGED FOR REVISIT (user cooled on it now that
  dazestone is the canon daze rock; consider a new streakschorl eat at eat-pass 2) · static-bubble
  + aegis self-cage — deferred to their OWN pass (user).
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

- [ ] 🆕 `[design]` **Fugue-clone walls outlive the clone (user flag 2026-07-24, verify session 2)** —
  recon done, `ExistenceSupport`-on-`FugueCopy`-caster build shape ready in TODO.md; awaiting ruling.
- [x] 🆕 `[bug]` **Boar Vessel picked fights — FIXED + DEPLOYED 2026-07-24** ("passive like
  ercolano"): `Brain Passive="true"` (the engine's defensive-engagement flag; the NoCombat tag
  was never a brain mechanism). Active next restart.
- [x] 🆕 `[verify]` **NPC calcifier CONFIRMED LIVE 2026-07-24** (log: Humanoid walled off its
  threat twice, perpendicular picker chose 4 cells at Lv1, zone-default Shale material) — the
  2026-07-23 batch's log half is proven; placement eyeball rides the session.

**Verify session 2 flag docket (2026-07-24, full entries in TODO.md § Verify session 2 findings;
house rule set mid-session: FLAG mid-session, fix in the post-session batch):**
- [ ] 🆕 `[bug]` **Thrown drainstone latch vanishes** — 4 payload fires, zero attachments;
  diagnostics deployed, one throw next restart names the broken link.
- [x] 🆕 `[bug]` **Streakschorl auto-equip** — FIXED + DEPLOYED (pre-rule): flight now fires
  `ReplaceThrownWeaponEvent.Check` when it vacates the slot.
- [ ] 🆕 `[design]` Strange→FugueClones miss-dud (flag-lite) · 🆕 `[bug?]` isolated NRE after a
  torpor detonation (watch) · 🆕 `[bug?]` gyrestone null-cell post-throw (watch).
- [ ] 🆕 `[bug]` **Baboons never scavenge ([SCAV] silent all session)** — experiment resolved it:
  the fetch preconditions are nearly unreachable (5-10 pocket stones each; throw-once base
  cadence per AIThrowAndScoot decomp — Flee only, nothing re-readies). **SCAV FIXING PASS (user
  flag): extend StoneScavenger to re-ready from pockets when the slot empties (PerformReequip in
  the pocket-ammo branch) — "keep the thrower throwing"** — plus whatever the dry-fetch verdict
  demands; details in TODO.md. **ESCALATED by the user's vanilla control run: baboons throw MORE
  without the mod — the mod REGRESSES throw cadence below baseline. Hypotheses (mixed-inventory
  re-ready failure / global stone merges changing AI scoring / the scavenger part itself) +
  differential experiments recorded in TODO.md; the pass must restore ≥vanilla cadence.**
- [ ] 🆕 `[design]` **Caromcobble StepChance 25 → higher? (user flag: "could be higher")** —
  re-opens a sweep-ratified number; the peel never breaks so the proc rate is pure zoning
  strength. One XML dial, rule at the batch.
- [ ] 🆕 `[design]` **Millstone → HOPPER model — RULED (user: "sounds great - menu probably
  goes")**: mill home becomes hopper-only (stones into its inventory, native powered grind);
  the mill's menu grind is removed; pestle/campfire homes unchanged. Build at the kitchen
  pass; shape in TODO.md.

- [x] 📜 `[design]` **NPC-caster gap — RULED batch-now + BUILT + DEPLOYED 2026-07-23** ("let's
  knock calcify off the list"). AI advertises on DEFENSIVE + RETREAT channels only — never
  offensive, since calcified stone is opaque both ways and Force Wall's owner-shoots-through
  trick doesn't apply; threat = event target or Force Wall's nearest-visible-hostile scan.
  NPC cell-picking = perpendicular line across the approach, anchored adjacent to the caster
  (honoring Calcify's own contiguity rule); occupied cells skipped by the placer, partial lines
  accepted. `[verify]` wish Calcify onto a creature, hurt it, watch it wall you off (unlocks the
  existing NPC-caster appendix item).
- [ ] 🆕 `[design]` **Gemstones default-equip to thrown slots (user flag)** — they're first-class
  ammo now; recon the engine's default-slot pick at build. Details in TODO.md.
- [ ] 🆕 `[polish]` **Whorl deflection VFX (user flag): refract/scatter needs a visible light-track
  (NEW), stone ricochet wants a clearer carom (escalated from parked)** — one VFX sitting;
  details in TODO.md.
- [ ] 🆕 `[bug]` **Calcify text says walls have "0 DV" — reality is −10** (base wall DV, never
  overridden; AV 0 is intended and stated, the HP pool is the defense). Fix text or override at
  the batch; details + lean in TODO.md.
- [ ] 📜 `[text]` **Calcify text pass.**

## Pass 7 — Geomancy

- [ ] 📜 `[text]` **Geomancy text pass.** *(Mechanically closed this session — reveals, mask fix,
  EB-pebble synergy all confirmed; only gemstone-scan + save/load verify remain.)*

## Cross-cutting / release logistics

- [ ] 📜 `[ratify]` **Manifest description** — two drafts benched 2026-07-23 (see TODO.md note);
  needs a fresh angle, placeholder still ships.
- [ ] 📜 `[polish]` **Statue Maker item description** — "placeholder description >:)".
- [ ] 🆕 `[polish]` **Strip `[TF-VERIFY]` instrumentation before release** — kill switch
  `Helpers.VERIFY_WATCH`, or delete the call sites (now ~19 files after the 2026-07-24
  coverage audit: user asked "make sure everything has new TF verifies" — gaps closed with a
  new `GRIND` tag on Part_StoneMill (grinds + the power-gate refusal), per-channel `LITH`
  "eaten channel" lines across all 18 Lithovore eat channels, spill counts on `FORM` expiry,
  and a `SHAPE` no-body-fail line; everything still routes through the one kill switch).
- [x] 📜 `[release]` **git init — DONE 2026-07-23** (fresh repo, no fork linkage, no trailer; history live since `b4ed68e`).
- [ ] 📜 `[release]` **Workshop publishing plan + credits.**
- [x] 📜 `[design]` **Stale `Cleo_TerraFirma_Ephemeral Stone` blueprint — RULED remove 2026-07-24
  ("you can remove, that's fine") and found ALREADY RESOLVED:** it never survived the 2026-07-21
  companion merge — verified absent from repo and deployed mod; sole copy is in the
  `_mods_backup` archive, left as archive. Nothing to build.
- [ ] 🆕 `[release]` **"Roiling innards" rename talk — RELEASE GATE (user 2026-07-24: "undecided -
  roiling innards fine for now but talk has to be had before release").** The name ships nowhere
  until the talk happens; candidates stand (outgassing [Claude rec] / gas pocket / fumarole).
  Canonical entry with full context lives in Pass 3 below.
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
  (beyond brine geode), 🆕 pour-into-container on the liquid rocks (ties to the pool-volume audit),
  🆕 modular disable options (per-mutation off switches + a stones-off switch; recon notes in TODO).

## Appendix — remaining verify items (testing leftovers, not polish)

**→ Interactive checklist for the next boot: `design/verify-pass.html`** (built 2026-07-24 at user
request — 70 items grouped by mutation, LOG/EYES/EARS badges with the [TF-VERIFY] tag each LOG
item proves out, checkbox state persists in-browser). **Upgraded same day (user request): the file
carries a machine-maintained `VERDICTS` block between `===VERDICTS-START/END===` markers — Claude
writes pass/partial/fail verdicts with log-evidence notes there as the watcher confirms items;
user checkboxes stay in localStorage and the page renders the union.** The prose list below
remains the record; the HTML is the play-session tool.

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
