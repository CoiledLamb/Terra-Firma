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
- [ ] 📜 `[ratify]` **Carried-over v1 numbers** — cooldown 105−5×Lv floor 40, cost 3 MP — never
  explicitly signed.
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
- [ ] 🆕 `[design]` **Pool-volume audit** — brine geode's `SaltyWaterPuddle` is 500 drams (renders
  "pool"); audit SlimePuddle + deep-stone pools; user floated step = splash vs break = full spill.
- [ ] 🆕 `[design]` **Aegischist thrown = impassable cage** — bubble ring around the victim on hits
  (step recipe), wall line demoted to misses; check forcefield owner-pass semantics + partial-cage gaps.
- [ ] 🆕 `[design]` **Drainstone vs inorganics** — attach-but-no-leech (user lean), still withers dry
  at duration's end; distinct endings: gorged burst ("popping like a blood balloon" / "finally
  bursting, sated") vs starved dry crumble. Detection: NoBleed / mirror Syphon Vim's gating.
- [ ] 🆕 `[design]` **Caromcobble trip-on-step** — ruled, unbuilt: StepChance 25 + `Effect/Prone`,
  HoldChance stays 100. Numbers to ratify at build.
- [ ] 🆕 `[design]` **StoneStep coverage** — three candidates, decide together: ape statue (the
  acid-bane A/B evidence), Stoneshaper statues (C# — runtime creation path), future stonefrog.
- [ ] 🆕 `[design]` **Stone creatures catch fungal infections** — keep (stone grows gills) vs immunize.
- [ ] 🆕 `[design]` **Allied geyser hit the player once** — watch item; audit target selection if it recurs.
- [ ] 📜 `[design]` **Mutating no-ops silently on robots/purestock** — "(acceptable?)" needs an answer.

**Rock-catalog ratifications (Claude numbers awaiting blessing):**
- [ ] 📜 `[ratify]` **HoldChance 25** (the one fragility dial) · **AcidResistance −50** stone-body
  weakness · **ape statue PERMANENT** (timed 500–1000 fallback if vetoed) · **omnigeyser stats**
  (L8/16/24, HP 50/100/175, AV 4/6/8…) · **figurine range numbers** (+5 range / 50 accuracy) ·
  **drainstone numbers** (1d3/round, 15/7 rounds, band 3 w6) · **spiderweb numbers** (3 webs,
  step-Stuck 12) · **phosphorescent twin's undocumented 25% Luminous step proc** · **fungal tint
  drafts** · **caromcobble full number/text set** · 🆕 **farflint StepChance 25** · 🆕 **pebble
  weight 0 blessing** · 🆕 **half-snack satiety knock-on** (bless or re-anchor part-based).
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

- [ ] 🆕 `[polish]` **Devour pronoun bug** — "devour **its**" (objective-pronoun property or
  restructure the sentence).
- [ ] 🆕 `[design]` **Universal statue devourability** — audit which statue blueprints slip
  `IsDevourableMass` (pure-Render shrine statuary suspected); broaden detection.
- [ ] 📜 `[polish]` **HistorySpice.json entries — REQUIRED** — `spice.cooking.recipeNames.ingredients`
  for the ~30 `Ground_*` powders (+ StoneMeal + rock flour); phrase lists in your voice; verify the
  mod-side spice merge path first. Carbide Chef is in `kitchenrock` for live preview.
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
- [ ] 📜 `[ratify]` **Round-dial sweep** — mumbles 25% secrets, GasBelly name "roiling innards" +
  density 30, siphonyx 7 rounds, quill 1d4, clone duration 10, lovesick nearest-visible reading
  (+ self-love-when-alone keep?), static force bubble (vs following), galvena arc dice/range,
  thunderclap dice, streakschorl leg range +6, warns on vitriolite/pulsestone (vetoable),
  unpowered millstone grinding, per-rock powders (vs your "per-domain" word), Lithovore pass-1
  magnitudes (soupstone 5000, temps ±150, harm dice, devour 2000 energy).
- [ ] 📜 `[design]` **Accepted deviations — revisit only if they grate** — banned learned recipes
  still nourish; gas immunity is full-type not partial; pantry rules text stays commented out
  (grenade idiom); pestle-in-inventory mystery (moot since rock-side grinding).
- [ ] 📜 `[design]` **Lithovore scope fork residue** — is the edible set all-IsAnyStone (Small
  Stones/EB pebbles as rations) or curated; deep-four eaten pulse numbers; statue-eating detail
  polish (satiation value, whole-vs-bites).
- [ ] 📜 `[text]` **Lithovore texts** — BearerDescription "lithovores", defect tile art, all
  session-DRAFT strings (grind messages, warn prompts, GasBelly details).

## Pass 4 — Stoneshaper

- [ ] 🆕 `[design]` **Muse-weapon naming** — "gravelly cherubic foot"? Options: simple prefix /
  adjective surgery (strip mechanical/metal/chrome…) / verbatim.
- [ ] 🆕 `[design]` **Fungal growths in the muse copy** — fickle-gill statue weapons: keep or filter
  infection-derived natural equipment.
- [ ] 🆕 `[design]` **Pre-Lv10 no-second-statue friction** — confirm-as-intended vs extend the swap
  down (DeanimateStatue path exists; cost: lifetime-as-cooldown stops gating low levels).
- [ ] 📜 `[design]` **Taunt balance watch** — your note at revival: "hopefully not too strong."
- [ ] 📜 `[text]` **Stoneshaper text pass** + Ctesiphus "phosphorescent" description quirk.
- [ ] 📜 *(housekeeping)* Close the four ruled-and-built checkboxes still showing open (natural-
  weapons ruling, Lv10 permanence, container-statue spec, Distraction revival) — and the
  contradictory line-168 "user ruling needed" note that line-170 already answered.

## Pass 5 — Gorgon's Gaze

- [ ] 📜 `[design]` **Damage-breaks-gaze held lever** — testing has happened (the channel is
  escapable but sticky); rule keep-or-add the damage break.
- [ ] 📜 `[design]` **Per-tick LOS-break dial** — NPC-channel question, explicitly unruled.
- [ ] 📜 `[text]` **Gaze text already house-voice** — confirm no residue at the text pass.

## Pass 6 — Calcify

- [ ] 📜 `[design]` **NPC-caster gap** — confirm parking with the innate-creatures workstream, or
  batch the AI handler + cell-picking now (quarriers ruling already commits the AI work).
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
- [ ] 📜 `[release]` **git init** (fresh, no fork linkage; no Co-Authored-By).
- [ ] 📜 `[release]` **Workshop publishing plan + credits.**
- [ ] 📜 `[design]` **Stale `Cleo_TerraFirma_Ephemeral Stone` blueprint** — deletion recommended,
  awaiting your ruling.
- [ ] 📜 *(housekeeping)* Reconcile stale TODO state: "NEXT UP: first boot" (superseded), "Fissure
  still DISABLED" status line (Phase D shipped), "v1.5 batch NOT deployed" hold (later waves say
  deployed), Lithovore/stabilizer deployment holds (overtaken by events), duplicate whorl-typo/
  placement entries from this compile.

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
