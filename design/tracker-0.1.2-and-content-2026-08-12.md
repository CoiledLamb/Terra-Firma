# Tracker — 0.1.2 patch lane + content buckets (snapshot 2026-08-12)

**Derived view, not a ruling record.** TODO.md stays the source of truth; every item here
points back at its home entry (TODO:line refs are as of today's file). Regenerate on request.
Scope per the user's framing: **0.1.2 = mutation balance pass + small QoL tweaks**; then the
three content buckets (stonecarvers · creatures · items) that ride toward 1.0.
Quarantine boundary respected throughout (v1 fissure catalog / cooking / Lithovore excluded).

---

## 1. The 0.1.2 lane (mutation balance + small QoL)

0.1.1 is live on the workshop (pushed 2026-08-07). The patch-1 lane is legitimate per the
standing beta ruling; TF-VERIFY stays ON through it (strip gates the full release, not this).

### 1a. Verify backlog — the next boot(s) pay this down

Nearly everything balance-flavored is BUILT + DEPLOYED and waiting on eyes/log. Grouped per
system; run `tfbatch` first (one `BATCH COMPLETE: N pass / M fail` verdict line), then the
eyes-on items.

**Harness / batch**
- [ ] `tfbatch` green under the new geometry (redoubt battery expects radius 3 now) — TODO:88-89
- [ ] `tfpen` output sane (measured pen/damage distributions for the feel session) — TODO:88
- [ ] quick.cage re-run on the fixed harness (≥7 open sides) decides real-vs-terrain — TODO:534

**Fissure (pulse rework + radius 3 + PV fix)**
- [ ] Live cast reads as three beats then the rim; ring lands at radius 3; three shakes — TODO:88-89
- [ ] [FIS] hit lines show pens VARYING (0-pens possible vs armor); chaff survives pulse 1 — TODO:90
- [ ] Engulf-catch / pin / eviction lines in the log — TODO:88
- [ ] Recast refresh: in-place recast seals, offset recast no dead arcs — TODO:86

**Earthen Barrage (take-stones + whorl)**
- [ ] **USER'S OWN REMINDER 2026-08-12: SWAT + RICOCHET beats** — `tfwhorlswat` / `tfwhorlrico`
      (grit blips at landing, chip-burst elbow), `tfwhorlattune` for diffuses/refracts verbs — TODO:51
- [ ] Swap-over-held-stone RE-RUN after the BeginBeingTaken correction: swap → stone lands in
      pack and reabsorbs; manual unequip same; ground pickup still re-arms — TODO:128
- [ ] Counter states: "(0/3+1)" armed-dry, "+1" drops on dry throw, "(3/3+1)" full — TODO:128
- [ ] Balance cuts visible: L4 stones 2d6+1 pv+1 · no daze anywhere · no whorl AV/DV on sheet
      at 9+ reserve · resonant stones show no pen bonus — TODO:132-134
- [ ] Ember-catch laser devour + spark-jack first-boot gate (mod compiler resolves the
      renderer types) — TODO:52
- [ ] Outcome scale spread over a fight; devoured plain stone feeds reserve; beam scatter
      (resonant) fires + no double-dip on laser pistols — TODO:187, 189
- [ ] EverArmed QoL: new character starts with sculpted stone equipped turn one — TODO:79
- [ ] Take Stones after world-map descent (lease or first-turn auto-retrieve) — TODO:81
- [ ] Wave wall-rider save-or-slam both branches — TODO:80

**Stoneshaper**
- [ ] Copy-decay chain: gen 2 clean → gen 3 dings one stat 1-3, message + [SHAPE] line — TODO:206
- [ ] Wall-picker snap: cursor seeds on a legal wall near the muse; no sidebar-target hijack — TODO:206
- [ ] "Crudely depicts" on Lv1-2 carves; Lv3+ keeps "intricately"; survives expiry — TODO:202
- [ ] `tfstatueai` v2 (kill + follow phases) — all green closes the inert-statue report as a
      watch item; follow-red reopens it — TODO:91
- [ ] Weird-anatomy feel check (Slog / swarm / no-handed muses) — TODO:205
- [ ] Companion-death matrix: combat kill = popup · natural expiry = "expired" popup ·
      swap-carve = log line · statuette quiet · Husangeion popup — TODO:260

**Calcify**
- [ ] True-copy look: shale vs limestone show source stats/desc under "calcified"; fallback
      zone gray at 150 HP — TODO:243
- [ ] Ladder 1→10: cooldown 90/90/80/…/50 while walls 4→9 — TODO:244
- [ ] Fugue-tether: clone's walls vanish on dissipate; normal casts persist — TODO:241

**Gorgon's Gaze**
- [ ] Willpower mod lands in save DC (show-save-rolls) — TODO:119
- (dead-gorgon trap + stunned-caster verifies stay GATED on an NPC gorgon existing —
  they land with the creatures workstream, not 0.1.2 — TODO:117-118)

**Cross-cutting**
- [ ] Etch residual eyes: low-tier block glyphs · tier-6 clean carve · statue limb etch with
      the carve reveal — TODO:61, 85
- [ ] Carve-reveal on gray fallback walls breaks gray (no red/yellow flash) — TODO:92
- [ ] Save-compat: pre-update save shows [SERIAL] recovery lines, zero deserialization
      exceptions — TODO:43
- [ ] Stonecarver chargen end-to-end + Wilderness Lore: Canyons on the skills screen — TODO:97-98
- [ ] Smoke-at-end: reveals stay smoke-free until done, then 1-4 puffs — TODO:54
- [ ] Boar body list + "wet" bleed prefix eyes; trade goods pulse as Geomancy ammo, no EB
      auto-sculpt — TODO:82-83
- [ ] Sound pass audible sweep — TODO:101
- [ ] Statue Maker absent from tinker pool / trinket drops (beta gate holding) — TODO:107

### 1b. Open balance calls in the lane

- ~~**Discharge-law final ratify**~~ — CUT from the queue 2026-08-14 (user: fissure rocks not
      spawnable); rides with the catalog's return — TODO:94
- [ ] **Fissure feel session** — the v5 staircase ladder + the flagged CLAUDE dials
      (dissipating-front rim bite, AI coverage gate) judged against tfpen + live casts — TODO:88
- [ ] **Calcify cooldown try-out revisit** — the scaling cooldown was framed try-out;
      feel-session revisit expected — TODO:244
- [ ] Copy-decay dial (1d3, floor 1) — flag if decay rate feels off — TODO:206
- [ ] Salvage-not-reset lease on picked-up EB stones — USER LEAN, recorded not scheduled;
      pick up only if dry-thrower recycling reads too generous — TODO:129
- [ ] Resonant whorl DV swap — RULED kept-as-shipped for beta; phase-2 look item
      (+2 AV / +4 DV intent on record) — TODO:145
- [ ] Distraction-taunt strength watch ("hopefully not too strong") — TODO:203
- [ ] Stone creatures catching fungal infections — keep (Qud-weird) vs immunize; decide at
      the creature pass — TODO:174
- [ ] Statue weight 10000 (inherited, never re-dialed) — unruled note in passing — TODO:91

### 1c. Small QoL / polish candidates

- [ ] Calcify screen shake (user flag; one-liner per recon) — TODO:49
- [ ] Credits: confirm workshop page reads right after next push, then close — TODO:40
- [ ] Recall story wiring — waiting on the user retrieving the text off-machine; one property
      line + Books.xml entry when it lands; DO NOT draft — TODO:99
- [ ] EB pickup-equip silent vs "you ready the stone" line — unruled dial — TODO:128
- [ ] Whorl attuned-normal-stone no-auto-retune — unruled dial — TODO:128

### 1d. Bugs / watch items in the lane

- [ ] **Geomancy wall-HP readout regression** — USER FLAG 2026-08-12, examine later: likely
      died with the deleted Harmony throw patch; git history has the old patch, then find a
      native seam (or rule the readout unwanted) — TODO:228
- [ ] SCAV give-up verify (no d=1 oscillation; one give-up line per wedge) — TODO:552
- Watch, no action unless they recur: TurnTick NRE (third sighting → capture + consider
  upstream report) — TODO:42 · Torpor-adjacency NRE — TODO:95 · allied-geyser friendly fire
  (moot if the geyser cut confirms) — TODO:172
- Quarantine-adjacent, MOOT until the catalog returns: aegischist thrown force-bubble
  (TODO:184) · gyrestone null-cell tolerance check (TODO:173)

### 1e. Text debts (ride the release text pass, not 0.1.2 — listed for visibility)

Home: `design/writing-sittings-1.0.md`. All Claude strings DRAFT until signoff; the two etch
menu options are USER VOICE, FINAL. Top of the pile: etch strings (intro/prompt/slip/frames) ·
statue decay message · gaze wall-refusal ShowFail · "wet" bleed prefix · stabilizer bands desc ·
boar vessel wild description (USER CLAIMED, their task) · mod-wide base-game-voice sweep.

---

## 2. Stonecarvers (quarriers workstream — release pillar, rides to 1.0)

User's own remaining-path read (2026-08-11): sprites droppable anytime → **"then it's just
text passes, and iterating on the warren until they're about ready to ship."**

### NPC build (the next big construction)
- [ ] Role blueprints under the compressed two-body shape: human base = `stonecarver.png`
      reused, mole base = `stonerat.png`; DetailColor carries role; thin mole child per role
      adds BurrowingClaws (variant-blueprint idiom, ~6 lines each) — TODO:370, 373
- [ ] Formal roster compression ruling (provisioner cut rides it) — TODO:372, 378
- [ ] Color map: overseer pink (engine-free) + outrider blue RULED-ish; mason grey = CLAUDE
      LEAN, drover brown = CLAUDE FLOAT — settle at build — TODO:373
- [ ] Overseer mutation picker (2-with-chance-for-3 from the roster three; bespoke ~20 lines
      default) — TODO:381
- [ ] Pack tables (SnapjawParty1 idiom) + legendary overseer (hero builder + GivesRep) — TODO:358
- [ ] Gear: inventories seed from the calling kit, kept LIGHT (chisel+mallet = etch gate
      passes by construction) — TODO:373
- [ ] Gorgon landing strip: if the roster never places Gorgon, the two gated verifies have no
      home — cheapest = overseer's chance-for-3 slot (CLAUDE flag, unruled) — TODO:373
- [ ] Floats awaiting user ruling: light-manipulation human (icon exists, engine AI free;
      lore tension on file) — TODO:375 · idle-etch brain part (parked to NPC build) — TODO:380
- [ ] Drover meat (bounded by the centipede design) — TODO:379

### Faction (deliberately AFTER the NPC build)
- [ ] Factions.xml entry + display text + name (Stonecarvers'-vs-Quarriers' lean on file) +
      calling starting-rep + possible calling rename — settle as one piece — TODO:384-399

### Warren / painter (sim leads, C# catches up)
- [ ] Stage-2 re-judge on the fixed economy (ex3's "too early to tell" is the open verdict) — TODO:445
- [ ] Stage-3 dress continues: gallery/communal tables next (room-jam vocabulary), then
      sleeping/communal; tense variants (abandoned flip) after lived-in reads right — TODO:453
- [ ] **C# port pass** (the port-pending batch, one deliberate sitting): diagonal seal · shard
      sweep · wet pockets + moss walls · canyon warren + tripwires · attach-probe/threading/
      border-ring guard fixes · flora + padded earth · dress registry · comb/ranks fixes
      already part-ported to tile-sketch — TODO:442-457
- [ ] Port-time rulings owed: buried-grass repaint call · natural-stones EB-forage economy ·
      pebble blueprint · wet-pocket final rarity · pre-breached variant float — TODO:449, 453, 443
- [ ] Warren verify list (tfquarry end-to-end etc.) — TODO:461

### Camp content
- [ ] Vine merchant build: Jilted Lover chassis strip, wares table, max-one decorate-phase
      claim; name/faction/greeting open — TODO:504
- [ ] 3x3 lumps + camp spawn wiring (painter-vs-chunk leans painter, unruled) — TODO:401-402
- [ ] Chalk-border wire-up (8-direction sprites landed; blueprint Render + yard recipe at the
      painter port) — TODO:320
- [ ] Moss walls: XML-only per recon (stock Harvestable, native recolor); rides the wet-pocket
      port; Harvestry-skill gate to accept or route around — TODO:444
- [ ] Trail (post-1.0 by design): cairn/musing tableaus, geode lottery, dromad ride-alongs — TODO:334

### The beast
- [ ] Stone-eating centipede: mouth/weapon fork (labium vs claws) + name — both OPEN, user's — TODO:366-369

---

## 3. Creatures

### Committed — build order
1. [ ] **Boar/pig vessel** — RULED FIRST TINKER TARGET: base wild blueprint → verify → re-parent
       Husangeion; cistern in v1 (zero C# — two LiquidProducers); flee brain; stats retune;
       Stilt pen group; name lock owed ("pig vessels" float supersedes "boar vessel" if
       locked) — TODO:503
2. [ ] Vine merchant (see stonecarvers) — TODO:504
3. [ ] Burrowbee — base Burrowing committed; pollen vs fissure-pulse vs both/castes = USER'S
       CALL (Claude lean: fissure bee) — TODO:494
4. [ ] Stoneskin baboon — EB AI inherited free; habitat + name open — TODO:496
5. [ ] Burrowing urchin — placement only (dynamic pools vs explicit cave tables) — TODO:495
6. [ ] Stonefrog settle sitting: disguise depth (lean: a+ palette + basilisk flip, no Hidden) ·
       numbers signoff · MentalShield · corpse-rock · cold call · wild tables — TODO:266-276
7. [ ] Malformed cragmensch — built + deployed; SPAWN PLACEMENT still open (ties to lump/camp
       tech); desc DRAFT + sprite = recolored placeholder — TODO:403

### Floated — confirm owed (concepts user's)
- [ ] Rock-covered ooze (litter-grazer; accretion armor) — TODO:498
- [ ] Trapdoor spider (Calcify-only vs both shapes; decides the burrowbee lean pairing) — TODO:499
- [ ] Rock bird (throw-system fauna; tell differentiation vs stonefrog owed) — TODO:501
- [ ] Stone-backed tortoise (first etchable ITEM; saltback-overlap concern on file) — TODO:502
- [ ] **Snapjaw miner** — sprite IN HAND 2026-08-12, art-first; everything else open — TODO:roster
- [ ] **Rockhound** — sprite IN HAND 2026-08-12; Herding Dog dual-faction precedent; drover/sty
      threads noted — TODO:roster

### Cuts / gates
- [ ] Geyser cut — signal on file, CONFIRM OWED (bookkeeping; already unreachable) — TODO:264
- Deferred verifies that land WITH creatures: dead-gorgon trap · stunned-caster · fis-npc /
  NPC casters in the wild — TODO:117-118, 95

---

## 4. Items

### Phase-2 gearset (the jam commitments — design doc: `design/gearset-draft-2026-08-04.md`)
- [ ] Catch-and-return gloves · wall-kick boots · pulling weapon · rock-socket armor (park the
      per-rock riders) · gravel shotgun (waits on the mill's return) — TODO:60
- [ ] Chisel+mallet carving verb (RULED item-gated, no skill) — TODO:60
- [ ] Moss boxes (in/out unruled; supply chain = the wall moss) — TODO:60, 444
- [ ] Baboon-skin throw bracers (buff shape + stacking unruled) — TODO:60
- [ ] Statue Maker restore (phase-2 gate lift + real description + verify) — TODO:103-107

### Throw-augmenting set (older, half-built)
- [ ] Stabilizer bands — BUILT + DEPLOYED; owed: desc/name ratify, Commerce/Tier ratify,
      WORLD WIRING (gear table? merchant? loot?), the equip verify — TODO:293-296
- [ ] Chalk container — damage-or-accuracy fork + resource model, user's to rule — TODO:291
- [ ] Sling — fork (a)/(b) open — TODO:292
- [ ] Visor — BINNED ("pick up later"), recon preserved — TODO:297

### New item debts from other workstreams
- [ ] Pebble blueprint (no base blueprint exists — mod's own at warren port; sprite = asset
      debt if bespoke wanted) — TODO:453
- [ ] Moss item (wall harvest product; doubles as moss-boxes supply) — TODO:444
- [ ] Cistern item (rides the pig-vessel build) — TODO:503
- [ ] Viewing stones / cracked viewing stone (Fissure flavor drop; camp/merchant/geode
      surfaces) — TODO:465
- Etching feature itself: DONE + verified green; only strings ride the text pass — TODO:61

---

## Reading the lanes

- **0.1.2 is mostly a verify-and-feel patch**: one or two boots through §1a, a real play
  session for the discharge law + fissure feel, the geomancy readout examine, and whatever
  small QoL lands easily. Very little new construction.
- **Stonecarvers' critical path**: NPC build → faction trio → warren port pass → camp content.
  Text passes run alongside (user's own framing).
- **Creatures' critical path**: pig vessel first (ruled), then the settle sittings; the two
  gated Gorgon verifies unlock only when an NPC gorgon exists somewhere.
- **Items are all phase-2 EXCEPT stabilizer bands**, which are one ratify + one wiring call +
  one verify from done.
