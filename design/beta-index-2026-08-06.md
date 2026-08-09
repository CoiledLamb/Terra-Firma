# Beta content index — everything in the Terra Firma beta (for patch notes)

**Compiled 2026-08-06** from TODO.md (source of truth), the full git history (236 commits,
`Initial commit` → `Publish pipeline`), `design/fallout-sweep-2026-08-02.md`, and the shipped
artifact `publish/CoQ_TerraFirma_beta_2026-08-05.zip` (XML verified directly). This is an
index of facts, not draft notes — the wording of the patch notes is yours.

**Two things to know before writing:**

1. **The 08-05 zip is stale.** Work marked BUILT + DEPLOYED on 2026-08-05/06 postdates it:
   Gorgon petrification-band VFX, Fissure text signoff, fugue-tether fixes, the NPC
   advertisement pass, etching completion popup. The tracker's own instruction is to re-run
   `tools/publish-beta.ps1` on send-out day. Everything below assumes the re-run artifact.
2. **The redoubt pivot (RULED 2026-08-02) frames the whole beta.** Beta ships the REDOUBT
   Fissure; the grenade rock catalog, Lithovore, and stone milling are BETA-HELD (spawns
   gated, blueprints dormant as dev doors; grenade version recoverable at git tag
   `fissure-v2-grenade`). Patch notes should not advertise held content — Part 2 lists it
   so you know where the line is.

Manifest: id `Cleo_TerraFirma`, title "Terra Firma", version 0.1, author "Cleo, Ceres et al."
Description sentence (beta-final): "Six earth mutations: Gorgon's Gaze, Earthen Barrage,
Stoneshaper, Geomancy, Calcify, and Fissure, plus the Stonecarver calling and assorted
stone-flavored extras."

---

## PART 1 — WHAT SHIPS

### Headline roster
- Six mutations: **Gorgon's Gaze, Earthen Barrage, Stoneshaper, Geomancy, Calcify, Fissure**
  (all with finished text, bespoke ability icons, cast VFX, and NPC support where ruled).
- **Stonecarver calling** (Mutated Human calling — NOT True Kin).
- **Boar Vessel** starting pet, **Burrowing Urchin** creature.
- **Etching** system (chisel + mallet on walls and statues).
- Per-mutation **mutation-pool toggle options**.

### Gorgon's Gaze
- **Composite channel redesign** (the mutation's core loop rebuilt): cumulative failed saves
  with +1 DC per fail (death spiral); each fail pins the victim, −10 Quickness; success
  ladder — 1st frees movement, 2nd ends the slow, 3rd breaks the gaze; **10 fails = permanent
  statue** with possessions entombed in a lootable container statue; **escaping at 4+ fails
  leaves a partially-petrified mark** (−20 MS, −10 QN, +2 AV, 500 rounds, refresh-not-stack).
- Save DC curve rebalanced `8+2×Lv` → **`11+Lv`** (opening DC 12+Lv+WIL); attacker stat is
  Willpower; cooldown 69 → 80; range 80 with LOS required.
- Channel now **breaks when any caster is Stunned/Confused/Asleep/Paralyzed** (live-verified).
- Dead-gorgon trap fixed (effect self-recedes if the initiator is gone); freshly petrified
  victims no longer get a spurious escapee mark under the statue swap.
- **Petrification buildup VFX** (2026-08-06): a rising stone band clipped from the target's
  own tile, tracking net fails-minus-successes (12%→92% ladder), serrated edge, porous band
  (detail pixels show through), a 2-row shimmering "waterline" transition, rock-debris pops
  per failed save (10-rock burst on the statue swap); stone-marked escapees render in
  statue-gray including the look-UI portrait.
- Level text FINAL (2026-07-27 voice sitting).

### Earthen Barrage
- **Gravel Whorl** — deflection is now exclusively EB's identity (cut from Calcify): deflect
  chance 3%×charges + 1%×level (46% at Lv10 full reserve); +1 AV at 3+ stones, +2 at 9+;
  works whenever stones remain in reserve (decoupled from the Take Stones toggle).
- **Whorl outcome scale** (50/30/20 Devour/Swat/Ricochet): devoured plain stones feed the
  reserve +1 charge; dangerous devoured items trigger at the defender (grenades detonate
  there, blame to the thrower — includes base-game grenades); swat places the projectile in
  an adjacent cell (loot preserved); ricochet returns it to sender (range-5 line, d20 vs DV,
  the projectile's own damage) with gravel-blip trace VFX.
- **Resonant stones** (Lv10 "Attune Stones" toggle): vibro (PV=AV) cyan stones with free
  in-place retune; the resonant whorl flips to deflecting light — Light-tagged projectiles,
  **+2/+4 DV** in place of AV, and **RefractLight** scatters laser beams in random directions.
  Resonant penetration double-dip fixed. Bespoke Attune Stones icon (user art).
- **Willpower-scaled charge regen** (base 15 turns, −5% per point over WIL 16, clamp 3–75).
- Take Stones QoL: auto-equip re-arms after throwing ANY stone; unequipped sculpted stones
  reabsorb at end of turn (charge refunded); new characters auto-arm once by default; new
  option "Take Stones starts toggled on" (default Yes).
- Fixes: whorl flicker/sound spam, thrown sound, message conjugation, pebble weight 0.
- Level text FINAL (constants bare, vibro line recast).

### Stoneshaper
- **Statue swap at ANY level** (was Lv-gated) with confirm popup; lifetime-as-cooldown
  display retired; **Lv10 permanence kept as the capstone**.
- **Natural expiry unified**: a statue that runs out freezes into a lootable container
  statue where it stands — entombed gear recoverable.
- **Taunt revived** (statue as tank; save grants long immunity, never affects the player).
- **Cosmetic muse weapons**: the statue copies the muse's natural-weapon tile/name/slot
  (damage stays on the mutation formula); multi-weapon muses yield multi-weapon statues;
  fungal growths carve in.
- Anatomy-less muses (plants) fail cleanly — no cooldown or wall spent; killed-statue
  cooldown lock fixed; "-69420" ability card reads "until destroyed".
- **Carve reveal VFX** (signed off): a cover block wearing the wall's own tile shrinks by
  thirds; debris chunks carry the literal pixel bands broken off; crack sounds per break;
  smoke only after full reveal.
- Full rock-creature VO on statues; de-animation sound.
- Level text FINAL (opener teaches targeting order, code-verified).

### Geomancy
- **Pulse VFX** (8 feel rounds, signed off): the TD warp wave — 360 transparent lens-ripple
  particles per ring warping terrain out to the revealed rim, plus a slower echo ring;
  darkness and automap recede with the ring on the same sweep; classic ø-glyph front kept
  behind a const.
- **Gemstones are first-class throwing ammo**: default-equip to the thrown slot (1d2).
- Harmony dependency removed for the throw path (native `AfterThrownEvent` relay part);
  NPC geomancers route through the same path — but Geomancy itself is **deliberately
  player-only** (no AI advertisement).
- Per-cell reality-distortion filter fix (stabilized cells individually excluded); duration
  text constant fix.
- Text restructured non-leveled, FINAL.

### Calcify
- **Balance: 3 → 4 base walls** (4 at Lv1, exactly 9 at Lv10 — Force Wall parity in
  permanent stone; walls 500×⌈Lv/2⌉ HP, cooldown 90).
- **Whorling stones CUT from Calcify** (deflection moved wholesale to EB).
- **NPC caster support**: defensive/retreat AI advertisement, perpendicular wall-line picks.
- **Cast VFX** (signed off): pick-order ripple — stones rise one after another along the
  picked line, per-cell dressing from the raised blueprint, slam sound at cast start,
  smoke after the line finishes.
- Shared **IsNaturalStone** classifier with Fissure: ruins/fulcrete zones raise a shared
  gray "calcified stone" fallback wall; real geology still borrows the local blueprint.
- Fixes: missing fallback blueprint, description-restore fallback, cooldown constant
  unification, "0 DV" text corrected to −10.
- Level text FINAL (flavor line picked at sitting).

### Fissure — the redoubt (complete redesign)
- **New cast**: a wave of earth radiates from the caster — ray-based reach; damage + outward
  push via engine force-vs-weight physics; **stun only if moved** (Toughness save DC 16+L);
  a **non-expiring rim** of broken stone at radius 2/3/4/5 by band with 3 gaps; interior
  plain-stone wake scatter; flyers exempt; loose items never destroyed; world-map cast
  blocked. Positioning: Fissure is the Force Bubble analogue to Calcify's Force Wall.
- **The wave carves**: natural stone walls answer the wave — a penetration roll vs wall AV
  (wave PV 6+3×Level, 34% max-HP bites, once per wall); carve depth scales from a 1-cell
  bite to full-chamber excavation; carved mass feeds denser ring fabric; breaches into
  occupied spaces are a feature.
- **Attack-any-wall** (supersedes silent refusal): Stone walls fall and feed the ring; Soft
  walls (plant/fungus/wood) can fall but give no ring credit; Hard walls (metal/machine/
  constructed) are clamped to 1 HP and always stop the wave; chip dust on every hit, zero
  popups.
- **Walls over water**: the heave displaces pools to adjacent dry cells (water never
  destroyed); undammable deep water downgrades the wall to rubble breaking the surface.
- **Fugue tether** (also Calcify): a temporal fugue clone's deposits/walls vanish when the
  clone dissipates; the player's own stone is untouched.
- **Cast VFX**: rising-stone rim choreography, sprite-slide emergence wearing the real
  rock-wall tile, fractured-plate slabs (travel wave breaks busier, rim slabs stay big),
  ring dressed as the zone's dominant true geology (gray fallback), camera shake, opaque
  emergence sprites, two atlas-deadlock freezes definitively fixed.
- Surface cast message "The earth heaves and convulses!"; underground keeps "brings the
  ceiling down."
- **Feel session ratified as shipped 2026-08-06** — all redoubt numbers stand, zero
  re-dials; indiscriminate wave kept (party gate covers ring rocks only).
- Text SIGNED OFF 2026-08-06 ("You send a wave through the earth, raising a ring of broken
  stone."; ring permanence deliberately undocumented).
- BETA-HELD inside the cast: keeper salvage rolls, geyser-in-gap, the stone figure,
  crack-gates. The shipped ring is pure fabric + gaps + scatter.

### Etching (new mod-wide feature, fully verified 2026-08-06)
- Carry both the **blunted chisel and splintered mallet** → an Etch verb on statues and
  walls (metal walls excluded, held rocks excluded).
- Typed path (60-char message) or markov path (one library-corpus sentence); re-etch
  allowed; inscription shows in look text plus an "etched" name adjective; 1000 energy;
  completion popup echoes the words.
- Menu strings are user-voice FINAL ("Etch a message." / "Write from memory."); other
  etching strings are DRAFT pending the release voice pass.

### NPC / legendary support (2026-08-06, live-verified)
- Legendaries can roll Terra Firma mutations; all five combat mutations now function on
  NPCs: Stoneshaper (offensive ad, wall scan radius 8, strongest-muse-by-level pick), EB
  (quiet auto-arm, base AI throws natively), Gorgon (offensive ad + channel maintenance),
  Calcify and Fissure (already advertised). Geomancy stays player-only by design.

### Options (Terra Firma category)
- **Six per-mutation pool toggles** (default Yes): removes the mutation from chargen and
  mid-run draws; existing characters unaffected. (DisplayText strings DRAFT.)
- "Take Stones starts toggled on" (default Yes) and "Enable debug logging".

### Stonecarver calling + companion content (merged companion mod)
- **Stonecarver**: +2 Willpower; Cudgel, Short Blades, Customs, Discipline, Ruins Survival;
  custom gear table with the chisel + mallet (gear-table prefix bug fixed — it would have
  spawned toolless), Vinewood Sap Mask, carved fire dish, quarried stone; own tile.
- **Boar Vessel** starting pet: pottery anatomy, produces water, properly passive (was
  picking fights — fixed and confirmed live).
- **Burrowing Urchin**: wall-walking creature, Burrowing 5, drops boring aculei.
- Items: blunted chisel, splintered mallet, sharpened polyp (urchin spine), boring aculei,
  carved fire dish, quarried stone.
- Skills audit outcome: no changes (Customs kept, no bespoke tree).

### Rocks in play (the keeper roster)
The full ~56-rock catalog is built but held (see Part 2). Live spawn tables in the shipped
XML are pruned to keepers + structural:
- **caromcobble** — rubber rock: never breaks, 2 ricochet legs per throw, 50% trip-on-step.
- **shattered stalactite** — 1d6/pen 6 throw + 100% armor-crack.
- **conchalk** (warped ajistone) — a different random gas per emission.
- **vantabsidian** — radius-5 darkness field; thrown blinds.
- **farflint** (shunting) — teleports the victim 8–16 tiles.
- **baboon statuette** — allied stone baboon skirmisher; expires into a rep-granting
  figurine trinket (rep works while carried, no ID needed).
- **Omnigeysers 1/2/3** — allied immobile turrets fabricating and throwing rocks from
  their own HP (three-wide until the phase-2 collapse).
- Structural: pebbles, plain fallen rock, rubble, crumbled wall, fallen wall, stalagmite/
  stalactite, cracked floor (in FissureTier2 of the shipped tables).
- **Wild baboons keep the StoneScavenger part** — they spawn with base Small Stones, then
  loot and throw rocks they find (pocket merges of mod rocks gated).
- **OPEN QUESTION for the notes** (tracker never reconciles this): the trim ruling says
  keepers appear "as occasional small spawns/ring salvage," but ring salvage and
  geyser-in-gap are beta-held in the cast — the shipped tables exist and are pruned
  correctly, yet no live consumer of the exotic keepers is clearly documented. Worth a
  30-second in-game check (or a ruling) before the notes promise findable caromcobbles.

### Mod-wide fixes and infrastructure
- Sound pass: deflected items swatted with loot preserved, statue VO, Calcify stone-impact
  cast, Gorgon escape-stage sounds, attune gemstone sound.
- Bitrot fixes from the original mod: Pigskin→PigskinWall (leather-wall statues work
  again), Temporary.Silent removal, PopulationTables group-name fix, binary-formatter
  MODERROR fixed (serialization-safe effects with post-load re-acquisition).
- Blueprint naming normalized: mechanical IDs + `Cleo_TerraFirma_` prefix everywhere
  (save-compat with pre-revival saves waived).
- Mullstone eat refreshes exactly ONE random cooldown (was all).
- tfserial serialization harness 12/12 green through QuickSave/QuickLoad + cold load.
- Publish pipeline: comment-stripped artifact (BETA-HELD blocks invisible), strict XML
  validation, compile check, date-token leak guard, zip.
- **Dev tooling ships ON by ruling** (through the friends beta, for remote diagnosis):
  TF-VERIFY logging, the tf* wish harness (tfbatch/tfredoubt/tfserial/tfnpc/tfspore),
  dev-door wishes (kitchenrock, fissurerocks, rockwineverytime). Strip is a 1.0 item —
  do not list as a bug if a tester finds a wish.

---

## PART 2 — BUILT BUT NOT IN BETA (don't advertise)

- **Grenade-rock Fissure + the full ~56-rock catalog** (held at the redoubt pivot;
  recoverable at git tag `fissure-v2-grenade`): shivshale, brine geode, salt, healstone,
  sludgabbro, grimslate, oilstone, oxidian, vertigneiss, lullabaster, the fungal four,
  compressed ash, tanglelith, gossamarl, urchinite, quarrelquartz, mullstone spawns,
  glowing twins, galvena, pyroclase, frostspar, vitriolite, ape statue, siphonyx, lattice
  euclase, streakschorl, hushmica, lodestone, aegischist, manymarble, dotagate, phasalt,
  gyrestone, chasm, strange rock, holographic rock, the deep four — plus the discharge
  law / fragility system riding them.
- **Lithovore defect** (chargen gated; kitchenrock dev door) — 18 eat channels, devour,
  GasBelly, cooking bans, all dormant.
- **Stone milling / cooking suite** (cut as one unit: mill, hopper, powders, domains).
- **Statue Maker claysculpting loop** (benched to phase 2; acquisition surfaces gated,
  dev door stays).
- **Stabilizer bands** — compiled and wish-spawnable but no world wiring (effectively
  dev-only in beta).
- **Malformed cragmensch** (blueprint dormant, zero spawn entries).
- Redoubt held-ring content: salvage, geyser-in-gap, stone figure, crack-gates.
- All 61 bespoke rock descriptions (BETA-HELD comments; spawnables use base text).
- Jam commitments parked post-beta: earth-manipulation gearset (gloves/boots/pulling
  weapon/socket armor/gravel shotgun/moss boxes/bracers), seismic sense, statue stance,
  granite gaol, "geology that responds".
- Quarriers workstream, Stonefrog, geyser body-plan pass, creatures groundwork, HistorySpice
  entries, Calcify screen shake, whorl-refract VFX pass, and the pending design budget.

---

## PART 3 — pre-send-out cross-checks (from the tracker)

1. Re-run `tools/publish-beta.ps1` for the final artifact (the 08-05 zip lacks the 08-06 work).
2. Leak-guard invariants to spot-check in play: no crack/geyser/salvage/figure from a
   redoubt cast; no Lithovore at chargen; baboon pockets are base Small Stones only; no
   mill grind options anywhere; Statue Maker absent from tinker recipes/trinkets.
3. Known harmless residue (accepted): orphaned fx_geomancy_spark textures, EB's vestigial
   WantEvent registration, dormant legacy FissureTable data.
4. DRAFT text surfaces still standing (fine for beta, flagged for the release pass):
   etching non-menu strings, option DisplayText labels, stabilizer bands description.
