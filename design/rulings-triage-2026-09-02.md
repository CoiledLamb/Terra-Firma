# Rulings triage, 2026-09-02

**CLAUDE PROPOSAL, UNRULED.** Every `kind` and `status` below is a guess for the user to cull. Nothing here changes a tracker. Source: every line containing `RULED` (not `UNRULED`) across the three trackers, one row per distinct ruling.

Kinds: **law** = invariant over many things; **scope** = in/out of a release, mod, branch; **tweak** = one item's number, name, sprite, trigger, wording; **meta** = the word appears but no decision is recorded.

Proposal for the ledger: only `law` and `scope` with status `live` get a file in `rulings/`. Tweaks stay inline in the tracker and die with their line.

**Agreed 2026-09-02 (user: "agree with this", to the five-point read):** RULED in the trackers stays pure provenance; weight lives in this folder. Only live law and scope get files, target about 30. CLAUDE.md keeps enforcement summaries and links here, no duplication. A quarantined tracker should hold zero rulings, only a pointer to the scope ruling that quarantined it.

**Date sweep 2026-09-02:** 3 of 69 law/scope entries were undated; all three dated from git history or the adjacent heading, source noted in the date column.

**RULED 2026-09-02, cull round one (user, verbatim):** "the first four i can vouch on" (the four quote-less laws: spawn-layer gating, warren goal statement, worth-the-trip bar, IDs mechanical / flavor in DisplayName are the user's own). "all the old fissure stuff - the rocks, lithovore, eating them, are irrelevant for the project now." (drops the three discharge laws, deep-X naming, the witchwood principle, the six keeper rocks, and every rock-cooking scope row.) "the moss is it's own thing free of lithovore's eating requirements, so don't worry about it." The scope-governs-a-set refinement was sent back for a self-audit, not ruled.

## Counts

| kind | total | live | superseded | dead-with-mechanic | unclear |
|---|---|---|---|---|---|
| law | 27 | 22 | 0 | 5 | 0 |
| scope | 42 | 28 | 7 | 4 | 3 |
| tweak | 496 | 326 | 36 | 132 | 2 |
| meta | 21 | 18 | 1 | 2 | 0 |
| **all** | **586** | | | | |

## law (27)

| ref | date | status | governs | gist | user's words |
|---|---|---|---|---|---|
| `TODO-archive.md:196` | 2026-07-23 | live | all blueprints | Blueprint IDs are mechanical; flavor lives in DisplayName only |  |
| `TODO-archive.md:196` | 2026-07-23 | live | all blueprints | Every mod blueprint carries the Cleo_TerraFirma_ prefix; save-compat waived for the sweep |  |
| `TODO-archive.md:267` | 2026-07-25 | dead-with-mechanic | rock catalog discharge | Discharge consumes the stone 100%, no lucky-survivor roll | "if a payload goes off, the stone should be destroyed" |
| `TODO-archive.md:267` | 2026-07-25 | dead-with-mechanic | rock catalog discharge | Spender stones trigger 100% on step; zoners keep their rates | "the annoying thing about step chance is... it feels really inconsistent and unclear in practice" |
| `TODO-archive.md:267` | 2026-07-25 | dead-with-mechanic | rock catalog discharge | Stones never trigger on their owner or the owner's party | "b makes sense" |
| `TODO-archive.md:872` | 2026-07-27 | live | whole mod | No em dashes in shipped text |  |
| `TODO-archive.md:875` | 2026-07-21 | live | commits | No Co-Authored-By trailer on commits |  |
| `TODO-archive.md:878` | 2026-08-05 | live | publish pipeline | Published build strips all code comments; repo keeps everything | "i'd lean full strip... i just don't want them to be obviously ai" |
| `TODO-archive.md:892` | 2026-08-02 | live | verification instrumentation | TF-VERIFY stays on through the friends beta; full strip at the 1.0 pass | "sounds good" |
| `TODO-archive.md:929` | 2026-08-07 | live | all display names | DisplayNames must be plain ASCII; classic font indexes by codepoint |  |
| `TODO-quarantine.md:19` | 2026-07-27 | live | mutation level text | Mutation level text must stay as close to base-game voice as possible | "we need to make sure the level text is as close to the base game voice as possible. this is potentially a conversation as big as descriptions" |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | eaten table | Witchwood principle: eaten effects are positive with a chance of negative | "i want to comb over the entire eaten effects table … and make sure everything tracks before this beta cut" |
| `TODO-quarantine.md:89` | 2026-07-22 | dead-with-mechanic | rock catalog names | 'deep X' is the universal T2 naming pattern for the catalog |  |
| `TODO.md:83` | 2026-07-21 | live | commits | No Co-Authored-By trailer on any commit |  |
| `TODO.md:83` | 2026-07-27 | live | shipped text | No em dashes in shipped text (manifest description onward) |  |
| `TODO.md:118` | 2026-08-02 | live | held content | Spawn-layer gating: held blueprints stay compiled as inert wish-doors, never mass comment-outs |  |
| `TODO.md:118` | 2026-08-02 | live | verification | TF-VERIFY instrumentation stays on through the beta |  |
| `TODO.md:458` | 2026-08-06 | live | all mutations | Every mutation level must increase something | "I need to make sure every level up increases something on the mutations, including on calcify" |
| `TODO.md:458` | 2026-08-06 | live | all mutations | 1-cost and defect mutations don't level (Geomancy, Lithovore exempt) | "1 cost and defect mutations don't level" |
| `TODO.md:520` | 2026-07-26 | live | quarrier art | Art bar: reuse base tiles/items by default, bespoke sprites as the user makes them | "i'm fine reusing tiles and items for the quarriers. some things will need their own sprites, but i'm gradually ticking away at those" |
| `TODO.md:633` | 2026-08-17 | live | quarry warrens | Warren goal statement: halfway between procgen and goatfolk villages; every v2 decision argues from it |  |
| `TODO.md:705` | 2026-08-18 | live | warren verification | Judging bar: roll two sites; can the user tell them apart and name each | "i totally agree with that metric, actually. it's something i've internally been trying to do on the last one" |
| `TODO.md:769` | 2026-08-18 | live | quarry warrens | Worth-the-trip bar: a rumoured warren must have something to do |  |
| `TODO.md:2046` | 2026-08-26 | live | whole mod | Architecture doctrine: XML > C# > Harmony, copy base idioms first |  |
| `TODO.md:2500` | 2026-07-22 | live | roster mutations | Any mutation a stonecarver NPC carries, its AI must actually cast | "gotta be able to fight back" |
| `TODO.md:2593` | 2026-07-24 | live | shipped text | All AI-produced text is draft until the user rewrites or signs off |  |
| `TODO.md:2599` | 2026-08-07 | live | warren workflow | Sim-first workflow: the carve sim is the design target, the game ports in passes | "it's fine if the sim gets ahead — wanna have the sim be what we're aiming for, and then iterate in game in passes" |

## scope (42)

| ref | date | status | governs | gist | user's words |
|---|---|---|---|---|---|
| `TODO-archive.md:47` | 2026-07-23 | live | gorgon NPC channel | Per-tick LOS-break dial parked with the creatures/quarriers workstream |  |
| `TODO-archive.md:267` | 2026-07-25 | dead-with-mechanic | rock dulling | Dulling cut mod-wide: DullAfter 0; weight is the cap | "maybe not even necessary in the long run (burgeoning gives unlimited cooking ingredients)... since they have weight, it's not like you want to carry an unlimited amount of them" |
| `TODO-archive.md:845` | 2026-07-25 | superseded | fissure NPC caster | Wild Fissure AI handler parked to build with the creatures workstream | "park with workstream" |
| `TODO-archive.md:878` | 2026-08-06 | live | beta distribution | Distribution = unlisted Steam Workshop item via a standing second mod folder TerraFirmaBeta | "wouldn't we just make a stripped copy on the workshop?" |
| `TODO-archive.md:879` | 2026-08-05 | superseded | friends beta release | Friends beta target Friday 2026-08-07, hard latest Sunday 2026-08-09 | "my goal is to have the friends beta ready by friday, with the latest it could come out being sunday" |
| `TODO-archive.md:881` | 2026-08-05 | live | statue maker | Statue Maker cut from the beta | "we cut it for beta" |
| `TODO-archive.md:885` | 2026-08-02 | live | fissure | Friends beta ships the redoubt Fissure (wave, push, stun, permanent ring), not v2 | "creating a rough form of cover and area control for yourself" |
| `TODO-archive.md:885` | 2026-08-02 | live | rock catalog + lithovore | Full grenade rock table and Lithovore held from the beta; Lithovore not benched forever |  |
| `TODO-archive.md:887` | 2026-08-02 | unclear | beta rock spawns | Six keeper rocks stay as occasional spawns/ring salvage; everything else gates off | "these sound fine for additional small spawns" |
| `TODO-archive.md:1035` | 2026-07-21 | live | calcify whorling stones | Calcify's whorling stones cut entirely; deflection is EB's identity |  |
| `TODO-archive.md:1102` | 2026-08-05 | live | etching | Etching ships in the friends beta | "i'd like it to be in since i think it fits in well with the stonecarver calling" |
| `TODO-archive.md:1145` | 2026-08-22 | live | etch tools | Steel/carbide chisel+mallet quartet removed; knives and hammers serve as etch tools | "let's remove the added chisels and mallets from the mod and just use knives and hammers in their place. the existing blunted chisel and splintered mallet is fine" |
| `TODO-archive.md:1193` | 2026-08-15 | live | quarrier roster | Provisioner cut; trade belongs to the vine merchant |  |
| `TODO-archive.md:1193` | 2026-08-15 | live | quarrier roster | Drover out of this build pass; roster = hewer/mason/outrider/overseer |  |
| `TODO-archive.md:1197` | 2026-08-15 | live | quarriers faction text | Faction description and display text deferred to their own text pass | "description and display text can be resolved in it's own pass" |
| `TODO-quarantine.md:23` | 2026-07-31 | unclear | beta text | Draft eat strings ship as-is for friends beta; vigilant signoff at release pass | "I can accept draft text for the friends pass, full release I want to be more vigilant on" |
| `TODO-quarantine.md:24` | 2026-07-31 | dead-with-mechanic | spacetime cooking domain | Spacetime cooking domain fully cut, including gyrestone powder and VortexOpen | "cut it" |
| `TODO-quarantine.md:27` | 2026-08-11 | live | strange rock | Strange-table cleanup sitting moved into quarantine, returns only by explicit reopen | "strange table sitting should be in quarantine" |
| `TODO-quarantine.md:41` | 2026-07-22 | dead-with-mechanic | geyser | Geyser build held, not scheduled | "hold off for now" |
| `TODO-quarantine.md:124` | 2026-07-22 | superseded | strange rock | All four strange-stone audit decisions held, worked one at a time | "hold off on all these, want to work through them slowly" |
| `TODO-quarantine.md:246` | 2026-07-22 | superseded | rock cooking | Cooking direction: mutation-themed effects, not rock-domain tagging |  |
| `TODO-quarantine.md:247` | 2026-07-22 (heading one line up: 'later same day') | dead-with-mechanic | rock cooking | Pass 1 ties into existing base domains only; rock-domain tagging reinstated as pass 1 |  |
| `TODO.md:34` | 2026-08-16 | superseded | Part 2 release | Two-mod split: base keeps everything shipped, expansion adds only never-shipped content | "i'm thinking about having the current mod be a mutations only one at the end of the day, and releasing a new one with the stonecarvers proper as it's own one" |
| `TODO.md:36` | 2026-08-26 | live | Part 2 release | Full move: base becomes mutations-only, everything else goes to Terra Firma 2 | "scope is everything *not* mutations for the second mod. so all items, behaviors, chargen characters, pets, creatures, etc go in terra firma 2" |
| `TODO.md:36` | 2026-08-26 | live | Part 2 release | Transition: base keeps shipped content through TF2 beta, strip once at TF2 1.0 | "that sounds fine" |
| `TODO.md:38` | 2026-08-15 | live | creatures branch | Creatures branch opened; staging wish first, spawn wiring after | "set up a branch for the creatures, including stonecarvers, so i can generate them in game" |
| `TODO.md:137` | 2026-08-14 | live | discharge law ratify | Discharge-law final ratify cut from the 0.1.2 queue; rides catalog return | "cut this from the queue as fissure rocks are not spawnable at the moment" |
| `TODO.md:138` | 2026-08-11 | live | strange table sitting | Strange-table cleanup sitting moved to quarantine with the catalog | "strange table sitting should be in quarantine" |
| `TODO.md:283` | 2026-08-29 | live | warren power grid | Warren power grid flagged for a game-side port pass | "flag this down for wanting a port later" |
| `TODO.md:327` | 2026-08-05 | live | statue maker item | Statue Maker item benched to phase 2 |  |
| `TODO.md:517` | 2026-07-26 | superseded | quarriers workstream | Quarriers in scope for 1.0 as a release pillar | "unfortunately i'd like to fit quarriers in the 1.0 release, it's just going to be tricky to implement, i think." |
| `TODO.md:595` | 2026-07-26 | superseded | canyon painter | Canyon painter system ships complete in 1.0; content library trails | "ideally we could have the canyon painter fully realized in 1.0 with additional items and scenes for flavor added in later on." |
| `TODO.md:920` | 2026-08-23 (git 44f97a9 journals the ruling) | live | cave moss | Moss clump cooking effect is planned future work, not quarantine-blocked | "only the lithovore cooking conversation is benched, this will get an effect later on" |
| `TODO.md:2329` | 2026-08-25 (git 4c0f42c, same-sitting delete) | live | fallen rock blueprints | Fallen rock blueprints deleted outright from the mod | "remo ve em" |
| `TODO.md:2534` | 2026-07-22 | unclear | mole-beast | Mole-beast committed: no interior, burrowing provisional |  |
| `TODO.md:2544` | 2026-08-15 | live | drover | Drover out of the roster color pass | "drover we don't need to include in this pass" |
| `TODO.md:2567` | 2026-07-22 | live | carver roster | Roster: hewer/mason/outrider + overseer by epithet; provisioner cut, drover parked |  |
| `TODO.md:2594` | 2026-07-24 | live | NPC conversations | No bespoke dialogue trees; one greeting line plus standard trade | "same amount of work, presented differently" |
| `TODO.md:2689` | 2026-08-11 | live | quarantined content | Design quarantine: fissure v1, its rock catalog, and Lithovore out of design conversations | "can we quarantine the first iteration of fissure, all the rocks it added, and lithovore, so they're not counted for conversations like these?" |
| `TODO.md:2707` | 2026-08-26 | live | Part 2 creatures | Part 2 creatures: miner, rockhound, granite baboon, boar vessels in; five on the maybe list | "rockslime, burrowbee, burrowing urchin, stone eating centipede, trapdoor spider all go on the maybe list. if i can fit any in, they go in, if i can't, they don't. so miner, rockhound, granite baboons, and boar vessels are confirmed in for right now" |
| `TODO.md:2733` | 2026-08-14 | live | granite baboon | Cobble baboon does not ship in 0.1.2; no spawn wiring before the stonecarver pass | "cobble baboon shouldn't ship in 0.1.2 btw" |
| `TODO.md:2763` | 2026-08-11 | live | stilt merchant | Stilt merchant benched until later | "the stilt merchant can be benched till a later date?" |

## tweak (496)

| ref | date | status | governs | gist | user's words |
|---|---|---|---|---|---|
| `TODO-archive.md:15` | 2026-08-05 | live | carve reveal smoke | Carve smoke fires after the reveal completes, not relocated foot dust | "instead of that, can we just have the smoke spawn at the end or something?" |
| `TODO-archive.md:15` | 2026-08-05 | live | carve reveal smoke | Carve and Calcify fire a random 1-4 smoke puffs at pump end | "a random amount of puffs... between 1-4" |
| `TODO-archive.md:16` | 2026-08-05 | superseded | geomancy pulse | Feel round 7: revert Geomancy pulse to the round-1 classic glyph front | "let's go back to original, not feeling this one" |
| `TODO-archive.md:16` | 2026-08-05 | live | geomancy pulse | Gemstone-scan cyan pulse color cut; no unspoken color rule | "unspoken rules like that can add confusion" |
| `TODO-archive.md:17` | 2026-08-06 | live | gorgon petrify VFX | Petrification VFX = rising stone band clipped from the target's own tile | "i lean more 2!" |
| `TODO-archive.md:17` | 2026-08-06 | live | gorgon petrify VFX | Band height tracks NET state (fails minus successes); statue still counts cumulative fails |  |
| `TODO-archive.md:17` | 2026-08-06 | live | gorgon petrify VFX | Partial-petrify stone mark = detail-channel gray via Effect.Render hook, no overlay |  |
| `TODO-archive.md:17` | 2026-08-06 | live | gorgon petrify VFX | Per-fail beat accent deferred to a feel round; band lurch is the beat |  |
| `TODO-archive.md:17` | 2026-08-06 | live | gorgon petrify VFX | Sub-4-fail escape: band slides off with no residue |  |
| `TODO-archive.md:17` | 2026-08-06 | live | gorgon petrify VFX | Statue completion: band tops out, no separate gray pulse |  |
| `TODO-archive.md:17` | 2026-08-06 | live | gorgon petrify VFX | Temporal dither recorded as fallback if the band fails at sprite scale |  |
| `TODO-archive.md:46` | 2026-07-23 | live | gorgon gaze channel | Damage taken does not break the gaze channel; keep |  |
| `TODO-archive.md:74` | 2026-07-23 | dead-with-mechanic | mullstone | Mullstone eat guarantees exactly one cooldown reset, not 25% or all | "all cooldowns clear is vastly overcorrecting; the most it could do would be like guarantee 1" |
| `TODO-archive.md:107` | 2026-07-23 | live | stoneshaper statue weapons | Fungal-infected muse statues keep gill weapons; carved as the muse appeared |  |
| `TODO-archive.md:108` | 2026-07-23 | live | stoneshaper recast | Stoneshaper recast swaps the living statue at any level; lifetime-as-cooldown retired | "cooldown gates it most of the time anyways" |
| `TODO-archive.md:175` | 2026-07-22 | dead-with-mechanic | fissure v1 cast | Fissure v1 cast shape = self-ring, no targeting, radius 1-4 by level | "self ring sounds cooler to me," |
| `TODO-archive.md:175` | 2026-07-22 | dead-with-mechanic | fissure v1 cast | Falls per cast = radius + 3, tuned up from radius + 1 | "a few more rocks," |
| `TODO-archive.md:194` | 2026-07-23 | dead-with-mechanic | fissure v1 rock tiers | 9-tier rock table adopted; primes retired, seam mechanic adopted |  |
| `TODO-archive.md:194` | 2026-07-23 | dead-with-mechanic | fissure v1 rock tiers | Deep four ride tier 9 plain, no depth gate; 50/50 coin-flip port overruled |  |
| `TODO-archive.md:194` | 2026-07-23 | dead-with-mechanic | fissure v1 rock tiers | Pits deferred; chasm ships as built |  |
| `TODO-archive.md:208` | 2026-07-23 | dead-with-mechanic | fissure v1 rock tiers | Filler trio (healstone/dazestone/oilstone) ruled into tiers 1-3; demotions undone |  |
| `TODO-archive.md:208` | 2026-07-23 | dead-with-mechanic | fissure v1 rock tiers | Shattered stalactite stays tier 3 | "a sharp rock is honest tier-3 material" |
| `TODO-archive.md:208` | 2026-07-23 | dead-with-mechanic | fissure v1 rock tiers | Rusty rock moves down a tier, 4 to 3 |  |
| `TODO-archive.md:210` | 2026-07-23 | dead-with-mechanic | fissure v1 rock catalog | Three new filler stones ruled in, names WIP, landing with the restructure |  |
| `TODO-archive.md:215` | 2026-07-23 | dead-with-mechanic | healstone | Healstone eaten = regen over time; cooked = simple healing effects |  |
| `TODO-archive.md:215` | 2026-07-23 | dead-with-mechanic | dazestone | Dazestone eaten = dazes the eater; cooked effect open |  |
| `TODO-archive.md:215` | 2026-07-23 | dead-with-mechanic | oilstone | Oilstone eaten = drink-oil event; cooked = heat effects, tentative |  |
| `TODO-archive.md:221` | 2026-07-23 | dead-with-mechanic | fissure v1 depth | Depth spill scaling yes: 20% + 5%/step, frozen at 50% |  |
| `TODO-archive.md:221` | 2026-07-23 | dead-with-mechanic | fissure v1 depth | Quality cap +8, reached at Z50, flat below; Z=50 floor claim struck |  |
| `TODO-archive.md:267` | 2026-07-25 | dead-with-mechanic | stonestep tag | StoneStep retired as an individual inclusion; party immunity covers it | "we could kill the stonestep part as an individual inclusion... since all creatures that spawn from seam (right now) are friendly, they would already inherit it. so no carveout just for baboons, no conversation about apes needing it in the future" |
| `TODO-archive.md:267` | 2026-07-25 | dead-with-mechanic | glowing twins | Glowing twins are spenders; the Luminous mark is the residue |  |
| `TODO-archive.md:267` | 2026-07-25 | live | baboon scavenging | Fetch/harvest by reach: baboons stand beside the stone, not on it | "fetch should check *adjacent* tiles, rather than tiles underneath" |
| `TODO-archive.md:267` | 2026-07-25 | dead-with-mechanic | tanglelith | Tanglelith classified at its own redesign; sits out the batch |  |
| `TODO-archive.md:372` | 2026-07-25 | dead-with-mechanic | glowing twins | Glowing twins moved from tier 9 to tier 6 | "let's move the glowing twins down to tier 6 - they're really not that dangerous, and it pads out the roster more" |
| `TODO-archive.md:381` | 2026-07-25 | dead-with-mechanic | tanglelith | Tanglelith is a spender that buys a permanent flammable tar zone; downs fliers |  |
| `TODO-archive.md:381` | 2026-07-25 | dead-with-mechanic | tanglelith | Tanglelith name not locked; pitchstone stays a candidate, more names owed | "i like the name tanglelith better than pitchstone. pitchstone can stay a potential name, but run 4-5 more by me before we lock it in" |
| `TODO-archive.md:398` | 2026-07-25 | dead-with-mechanic | stone mill | Mill law: armed stones detonate at the millstone instead of yielding flour |  |
| `TODO-archive.md:398` | 2026-07-25 | dead-with-mechanic | stone mill | Gas rocks keep their flours; detonation and yield coexist | "instead of them being in conflict, they can coexist" |
| `TODO-archive.md:398` | 2026-07-25 | dead-with-mechanic | stone mill | Severity split: HE and spacetime forfeit powder; gas/thermal/EMP detonate and yield | "stronger reactions, no powder. minor things like gas? they can coexist... if hushmica's detonation in the mine makes it so there's no output, then we no longer have an artifacts entry" |
| `TODO-archive.md:398` | 2026-07-25 | dead-with-mechanic | stone mill | Hand tools refuse armed stones; the millstone is the only station |  |
| `TODO-archive.md:398` | 2026-07-25 | dead-with-mechanic | stone mill | The mill pays +1 serving on every grind | "since it requires power and using a mill, could it give 1 extra flour when milling? this can offset some of the danger by adding a reward as well" |
| `TODO-archive.md:420` | 2026-07-25 | dead-with-mechanic | holographic rock | Holographic rock designed and built as a joke rock | "it seems both easy and fun" |
| `TODO-archive.md:420` | 2026-07-25 | dead-with-mechanic | holographic rock | Int save DC 12 | "something like 12" |
| `TODO-archive.md:420` | 2026-07-25 | dead-with-mechanic | holographic rock | Placement = generics of tiers 2-3 at weight 5; never picked for seams (NoSeam tag) | "it's just a joke rock, so it's pretty weak... it should also not get picked for seams, which maybe points more to being a generic" |
| `TODO-archive.md:420` | 2026-07-25 | dead-with-mechanic | holographic rock | Belief is sticky per character, one roll ever |  |
| `TODO-archive.md:420` | 2026-07-25 | dead-with-mechanic | holographic rock | MentalShield/mindless auto-pass; machines cannot be fooled or hit |  |
| `TODO-archive.md:420` | 2026-07-25 | dead-with-mechanic | holographic rock | Believed hits can kill; generic-rock dice and the stone is consumed |  |
| `TODO-archive.md:420` | 2026-07-25 | dead-with-mechanic | holographic rock | Inert underfoot; excluded from the strange-rock pool |  |
| `TODO-archive.md:447` | 2026-08-05 | live | fissure wave carve | Fissure wave attacks any wall; supersedes the corridor ruling's natural-only silent refusal |  |
| `TODO-archive.md:448` | 2026-08-05 | live | fissure wave carve | Wave attacks every wall; hard walls damaged but never destroyed, soft walls fall | "really i'd have it just make the attack on any wall - machinery still gets hurt by the earth, it just doesn't get destroyed ... it'd maybe be less effective on constructed stone in the sense of 'can't fully break it', but that would apply less to things like brinestalk or fungus walls." |
| `TODO-archive.md:448` | 2026-08-05 | live | fissure wave carve | No wall-feedback messages; chip dust is the only signal | "popup spam" |
| `TODO-archive.md:515` |  | superseded | stone mill | Grinding UX = batch campfire menu (preserve-exotic shape), not per-stone actions |  |
| `TODO-archive.md:529` | 2026-07-22 | dead-with-mechanic | lithovore cooking | Lithovores banned from cooking with plants and meats; stones and liquids legal |  |
| `TODO-archive.md:529` | 2026-07-22 | dead-with-mechanic | lithovore cooking | Fungus stays legal for lithovores (honorary minerals) |  |
| `TODO-archive.md:529` | 2026-07-22 | dead-with-mechanic | lithovore cooking | Plain no-ingredient meals nourish lithovores |  |
| `TODO-archive.md:530` | 2026-07-22 | dead-with-mechanic | rock eaten effects | Rock eaten-effects reuse base cooking effects wherever possible |  |
| `TODO-archive.md:535` | 2026-07-22 | dead-with-mechanic | lithovore | Defect name: Lithovore |  |
| `TODO-archive.md:535` | 2026-07-22 | dead-with-mechanic | lithovore | Lithovore cost 2 MP, Carnivorous parity |  |
| `TODO-archive.md:535` | 2026-07-22 | dead-with-mechanic | lithovore | Lithovore scope: all IsAnyStone plus gemstones |  |
| `TODO-archive.md:535` | 2026-07-22 | dead-with-mechanic | lithovore | Lithovore excludes Carnivorous and Photosynthetic Skin |  |
| `TODO-archive.md:535` | 2026-07-22 | dead-with-mechanic | lithovore | Dangerous rocks are eatable with a warning prompt |  |
| `TODO-archive.md:535` | 2026-07-22 | dead-with-mechanic | lithovore | Living Stoneshaper statues count as edible for famished lithovores | "eating your buddy because you're starving makes thematic sense" |
| `TODO-archive.md:541` | 2026-07-22 | dead-with-mechanic | deep four rocks | Deep four eaten = pure danger, elemental pulse centered on the eater |  |
| `TODO-archive.md:543` | 2026-07-22 | dead-with-mechanic | strange rock | Strange rock cooked = random rock domain; raw = only rocks with real eaten effects |  |
| `TODO-archive.md:543` | 2026-07-22 | dead-with-mechanic | strange rock | Strange rock mimic table needs a cleanup pass | "some of the things it has don't really make sense" |
| `TODO-archive.md:544` | 2026-07-22 | dead-with-mechanic | rock eaten effects | Raw-eat effects = stock OnEat parts at snack dice; rocks carry no Food part |  |
| `TODO-archive.md:544` | 2026-07-22 | dead-with-mechanic | lithovore cooking | Liquids stay legal cooking partners for lithovores |  |
| `TODO-archive.md:544` | 2026-07-22 | dead-with-mechanic | lithovore | Famished lithovores can eat statues, like carnivores eat corpses; details open |  |
| `TODO-archive.md:872` | 2026-07-21 | live | manifest | Manifest author field stays "Cleo, Ceres et al." |  |
| `TODO-archive.md:878` | 2026-08-05 | live | publish pipeline | Date tokens scrubbed from published .cs text; repo untouched | "scrub is fine for the published version" |
| `TODO-archive.md:878` | 2026-08-06 | superseded | boar vessel name | Boar Vessel renamed Hûskévos (DisplayName only) | "ONE change before i send it live" |
| `TODO-archive.md:881` | 2026-08-05 | live | gravel whorl DV | Whorl DV kept as shipped, with a phase-2 note |  |
| `TODO-archive.md:881` | 2026-08-05 | live | fugue clone stone | Fugue-clone tether built: clone-cast stone dies with the clone |  |
| `TODO-archive.md:888` | 2026-08-02 | live | wild baboons | Wild baboons keep StoneScavenger; pocket-stone merges out | "scavenging is cool and we can keep that as long as it works" |
| `TODO-archive.md:889` | 2026-08-02 | live | fissure wave loot | The wave never destroys items; v1 ignores loose items entirely | "agree with your lean" |
| `TODO-archive.md:890` | 2026-08-02 | live | redoubt fissure numbers | Shape ruled, Claude drafts the redoubt numeric table, user re-dials in game | "this sounds fine, i'll test numbers in game" |
| `TODO-archive.md:898` | 2026-08-02 | live | fissure wave carve | The wave carves; depth scales with level; carved mass feeds the ring; breaches welcome | "i agree with all of this and await to see how it feels in practice" |
| `TODO-archive.md:898` | 2026-08-02 | superseded | fissure wave carve | Natural stone only; constructed/metal walls stop the wave |  |
| `TODO-archive.md:900` | 2026-08-04 | live | fissure wave carve | Carve is a penetration roll vs wall AV; some walls are left standing | "rather than having it autonomously destroy walls, could we have it make an attack against the walls armor? so the end result is similar but some walls are leftover" |
| `TODO-archive.md:902` | 2026-08-04 | live | calcify fallback wall | Calcify and Fissure share the gray no-geology fallback wall look |  |
| `TODO-archive.md:902` | 2026-08-04 | live | calcify VFX | Calcify rise animation = pick-order ripple with a slight per-wall offset | "i think i like the idea of the rising of stones matching the order in which you selected tiles, with a slight offset" |
| `TODO-archive.md:902` | 2026-08-04 | live | natural stone classifier | Shared IsNaturalStone classifier gates Fissure dressing and Calcify borrowing | "we should harmonize the stone call as well" |
| `TODO-archive.md:906` | 2026-08-06 | live | fugue clone stone | Fugue-clone Fissure deposits and Calcify walls vanish when the clone dies | "calcify/fissure stone can disappear when the fugue clone dies" |
| `TODO-archive.md:907` | 2026-08-06 | live | NPC advertisement | NPCs keep rolling Terra Firma mutations; build AI advertisement instead of excluding | "rather than removing why don't we just work the advertisement out?" |
| `TODO-archive.md:907` | 2026-08-06 | live | stoneshaper NPC muse | NPC shaper's muse = strongest creature in LOS, judged by level | "pick the strongest monster nearby... determine strongest by level, since that's the easiest indicator" |
| `TODO-archive.md:908` | 2026-08-06 | live | fissure wave riders | Wall-standing creatures get save-or-slam from the wave, no push attempt | "since they can't be pushed off walls, they should just get slammed if they fail the save" |
| `TODO-archive.md:910` | 2026-08-06 | live | boar vessel anatomy | Boar vessel uses boar anatomy with stone-category parts, not limbless pottery | "boar vessel should inherit it's anatomy from boar but still have vessel like physical traits" |
| `TODO-archive.md:910` | 2026-08-06 | superseded | boar vessel bleed | Boar vessel bleeds water | "yeag have it bleedwater" |
| `TODO-archive.md:911` | 2026-08-06 | live | geomancy ammo | Nuggets and figurines work as Geomancy ammo; IsAnyStone deliberately not applied | "trade goods (nuggets and figurines) should work for geomancy. it's a great tutorial for the mechanic when you start stonecarver" |
| `TODO-archive.md:912` | 2026-08-06 | live | etching | Etching a bodied statue opens a limb picker like tattoo guns | "etching a statue should bring up 'what limb do you want to etch' like tattoo guns" |
| `TODO-archive.md:920` | 2026-08-06 | live | fissure recast | Recast fells the caster's own ring fabric freely; recycled ring seals with no gaps | "fissure doesn't recast well on itself due to minimum open spaces rules" |
| `TODO-archive.md:921` | 2026-08-06 | live | fissure text | Fissure description = audit option B: wave through the earth raising a broken-stone ring | "what about B" |
| `TODO-archive.md:921` | 2026-08-06 | live | fissure text | Fissure level text opens on stat lines; opener sentence cut; final as deployed | "i'm fine with this batch of fissure text" |
| `TODO-archive.md:927` | 2026-07-24 | live | ephemeral stone blueprint | Stale with-space Ephemeral Stone blueprint removed (already absent) | "you can remove, that's fine" |
| `TODO-archive.md:929` | 2026-08-07 | live | boar vessel name | Boar vessel named Husangeion (hus + aggeion) |  |
| `TODO-archive.md:930` | 2026-08-07 | live | boar vessel glaze | Boar vessel detail color follows its majority stored liquid; cider forced red | "what if boar vessel's detail color was dependent on the majority liquid stored inside it? so cider would be red, wine would be purple, and so on" |
| `TODO-archive.md:930` | 2026-08-07 | live | boar vessel glaze | Empty vessel detail color = negative space k (third round) | "negative space to imply the channels fluid runs through" |
| `TODO-archive.md:930` | 2026-08-07 | live | boar vessel pronouns | Boar vessel pronouns it/its via neuterperson gender tag |  |
| `TODO-archive.md:931` | 2026-08-07 | live | boar vessel bleed | Bleed liquid water-900,salt-100; producer and initial liquid stay pure water | "make sure the water it bleeds has a tiny bit of salt in it" |
| `TODO-archive.md:935` | 2026-07-25 | live | gemstone equip | Gemstones gain a ThrownWeapon part so default equip lands in the thrown slot | "we would have to add the behavior for them to be thrown slot, but i think that's a good idea. i think default equip slot is XML anyways" |
| `TODO-archive.md:971` | 2026-08-14 | live | whorl ricochet VFX | Rock ricochet = bounce on the whorl then a thrown arc toward the carom end | "i want it to 'bounce' on us and then do a thrown animation in the direction it's ricocheting." |
| `TODO-archive.md:971` | 2026-08-14 | live | whorl ricochet VFX | Laser ricochet redraws the beam line along the carom leg | "i want to redraw the animation of the laser line when it ricochets" |
| `TODO-archive.md:971` | 2026-08-14 | live | whorl ricochet VFX | Elbow chip-burst retired; bounce/beam redraw is the whole beat | "bounce redraw carries the whole beat imo" |
| `TODO-archive.md:971` |  | live | whorl ricochet VFX | Standalone in-place hop cut; the outgoing arc is the bounce | "there's one extra bounce on the player's tile that's unnecessary - it just needs to draw the bounce" |
| `TODO-archive.md:971` |  | live | whorl ricochet VFX | Dust at the player/whorl cell is fine; landing-cell poof stays banned | "dust puff on player is fine for rocks i think" |
| `TODO-archive.md:992` | 2026-07-22 | superseded | resonant stone penetration | Resonant stones: level pen bonus reduced by 5 flat |  |
| `TODO-archive.md:992` | 2026-07-22 | live | resonant stone penetration | Attuned stones keep one fifth of the level-scaled pen bonus (final form) |  |
| `TODO-archive.md:995` | 2026-07-21 | live | gravel whorl | Whorl retunes with the stones: resonant deflects Light projectiles, physical pass |  |
| `TODO-archive.md:995` | 2026-07-23 | live | gravel whorl DV | Resonant whorl AV bonus becomes +2/+4 DV | "DV is less valuable than AV so you need more to compensate" |
| `TODO-archive.md:1006` | 2026-07-23 | dead-with-mechanic | fissure v1 depth | Spill chance 20% + 5%/depth step, frozen at 50% |  |
| `TODO-archive.md:1006` | 2026-07-23 | dead-with-mechanic | fissure v1 depth | Depth quality bonus caps at +8 at Z50; +1 fall per 2 steps held |  |
| `TODO-archive.md:1014` | 2026-07-23 | live | EB sculpted stones | Sculpted stones left in inventory are reabsorbed at end of turn | "return back to the take stones option, or be destroyed" |
| `TODO-archive.md:1016` | 2026-07-23 | live | whorl deflection | Whorl-deflected thrown items marked so placement holds and fragility is skipped |  |
| `TODO-archive.md:1023` | 2026-07-21 | live | stoneshaper statue taunt | Distraction fossil revived as a taunting tank statue | "hopefully not too strong" |
| `TODO-archive.md:1026` | 2026-07-21 | live | stoneshaper statue weapons | Statue natural weapons copy muse tile/name/slot only; damage on mutation formula |  |
| `TODO-archive.md:1027` | 2026-07-21 | live | inert container statue | Unified inert lootable container statue spec serving three paths |  |
| `TODO-archive.md:1040` | 2026-08-12 | live | whorl message verbs | Resonant deflection verbs: snuffs / diffuses / refracts; scatter unchanged | "ricochet can be refract" |
| `TODO-archive.md:1102` | 2026-08-05 | live | etching | Etch targets = statues and walls, metal walls excluded, held rocks out | "not held rocks - instead, statues, and walls (metal walls excluded)" |
| `TODO-archive.md:1102` | 2026-08-05 | live | etching | Re-etch allowed, flavored as breaking off a sheet | "with the flavor of breaking off a sheet from it" |
| `TODO-archive.md:1102` | 2026-08-05 | live | etching text | The two etch menu option labels are the user's own voice, not Claude drafts | "i wanna use my own voice for the two options offered when etching" |
| `TODO-archive.md:1102` | 2026-08-07 | live | etching VFX | Etch VFX = full stoneshaper carve reveal over the recolored face | "the same visual flair of carving away the top layer to see the recolored item underneath" |
| `TODO-archive.md:1102` | 2026-08-07 | live | etching garble | Garbled etch text uses the graffiti Obfuscate law | "i want the garbled text to work like graffiti - check it out before implementing" |
| `TODO-archive.md:1102` | 2026-08-07 | live | etching recolor | Etch recolor = flip main to detail; no random graffiti recolor | "it should just flip the color of main to detail like it already does" |
| `TODO-archive.md:1125` |  | live | glowsphere lantern | Light-economy concern waved off; lantern may exceed radius 8 | "light economy isn't really a thing in qud so i'm not worried about this breaking it" |
| `TODO-archive.md:1126` |  | live | glowsphere lantern | Day/night fuel behavior vetoed | "burning fuel only at night/dimming under open sun is overcomplicating things" |
| `TODO-archive.md:1127` |  | live | glowsphere lantern | Lantern must beat a bare shard; axis and numbers open | "lanterns should be better than bare glowsphere shards though!" |
| `TODO-archive.md:1128` |  | live | glowsphere lantern | Shards are loadable into lanterns | "glowsphere shards should be loadable into lanterns" |
| `TODO-archive.md:1129` |  | live | glowsphere fragments | Fragment count 2-3 per broken sphere |  |
| `TODO-archive.md:1137` | 2026-08-26 | live | floating glowsphere | Floating glowspheres do not break; the unmerge is permanent | "floating glowsphere does not break like the handheld one, so we can tick that off now" |
| `TODO-archive.md:1141` |  | live | glowsphere fragment combat | Shard applies phosphorescent on any hit; thrown penetrating hits may bleed | "shard can apply phosphorescent on hit, with a chance to bleed whatever it hits when thrown" |
| `TODO-archive.md:1142` |  | live | glowsphere fragments | Player-facing name is glowsphere fragment, not glowstone; internals keep Glowstone | "make sure they go by 'glowsphere fragments' and not glowstone, which i invented, oops" |
| `TODO-archive.md:1142` |  | live | glowsphere break verb | Break verb shortened to break; confirm text is the user's | "Really break your glowsphere?" |
| `TODO-archive.md:1142` |  | live | glowsphere lantern | Lantern color qud dark grey &K with Y glow detail |  |
| `TODO-archive.md:1142` |  | superseded | glowsphere lantern | Lantern Commerce 120 to 40 |  |
| `TODO-archive.md:1142` |  | live | thrown glowsphere | Thrown glowspheres break: 1d4 impact, phosphorescent on hit, scatter 1-3 lit fragments | "dial can be 1-3 instead of 1-2" |
| `TODO-archive.md:1143` | 2026-08-22 | live | tinkerable items | Tinkerability: grinder and future seismophone only; lantern not tinkerable |  |
| `TODO-archive.md:1145` | 2026-08-22 | live | glowsphere commerce | Lantern Commerce 40 to 25; fragment Commerce 15 to 5 |  |
| `TODO-archive.md:1145` | 2026-08-22 | live | etch tools | Blunted chisel and splintered mallet kept as the starter kit |  |
| `TODO-archive.md:1155` | 2026-08-12 | live | EB take stones label | Take Stones label reads (x/y+1) while a sculpted stone is equipped | "0/3+1 makes sense as like 'i have no stones in reserve and 1 equipped'" |
| `TODO-archive.md:1155` | 2026-08-12 | live | EB re-arm | Ground pickup of an own sculpted stone re-arms; deliberate unequip still reabsorbs |  |
| `TODO-archive.md:1159` | 2026-08-25 | live | EB re-arm | Empty thrown slot with toggle on re-arms at end of turn, whatever emptied it | "when you run out, it'd be ideal if your pebbles started equipping again, so you didn't have to manually do it yourself" |
| `TODO-archive.md:1171` | 2026-07-23 | live | calcify NPC caster | Calcify NPC AI advertises defensively/retreat only; perpendicular wall line | "let's knock calcify off the list" |
| `TODO-archive.md:1184` | 2026-08-15 | superseded | etch tools | Proper etch tools: steel, carbide as ceiling | "it occured to me currently all stonecarvers are using blunted chisels and splintered mallets... maybe copper or steel?" |
| `TODO-archive.md:1184` | 2026-08-15 | live | etch tools | Player stonecarver keeps the blunted starter pair |  |
| `TODO-archive.md:1188` | 2026-08-15 | live | quarrier kits | Quarrier kits roll tier-appropriate gear via {ownertier} with biased spine items | "rather than use player base armor, we could do something like generate tier appropriate armor with a few items biased to roll each time." |
| `TODO-archive.md:1188` | 2026-08-15 | live | quarrier kits | Mason's role tell = maquette figurine, not a bubble level | "give the mason a figurine" |
| `TODO-archive.md:1193` | 2026-07-22 | superseded | quarrier roster | Six-role quarrier roster: hewer/mason/outrider/drover/provisioner/overseer |  |
| `TODO-archive.md:1193` | 2026-07-22 | superseded | quarrier roster | Provisioner spawns with tradeable goods |  |
| `TODO-archive.md:1193` | 2026-07-22 | live | quarrier overseer | Overseer carries 2 mutations with a chance for 3 |  |
| `TODO-archive.md:1197` | 2026-08-15 | live | quarriers faction | Faction name = Quarriers' Guild, over Stonecarvers' |  |
| `TODO-archive.md:1197` | 2026-08-15 | live | quarriers faction | Faction period post-sultanate (Old=false) | "post-sultanate" |
| `TODO-archive.md:1197` | 2026-08-15 | live | quarriers faction | Emblem = stonecarver.png human body, y over K | "just use the regular stonecarver.png rather than stonerat." |
| `TODO-archive.md:1197` | 2026-08-15 | live | quarriers faction | Rep jingles = the cragmensch pair | "definitely cragsmensch" |
| `TODO-archive.md:1197` | 2026-08-15 | live | quarriers faction | No waterritual node; follows Farmers' shape, quarriers recruitable | "join on water ritual should also follow the same pattern" |
| `TODO-archive.md:1197` | 2026-08-15 | live | quarriers faction | Interests: sell desertcanyons + underground, buy ruins | "buying ruins is good, selling desertcanyons and underground works" |
| `TODO-archive.md:1210` | 2026-08-15 | live | stonecarver calling rep | Stonecarver calling starts with 200 rep with the Quarriers' Guild | "starting rep should be the same as farmer's and merchants" |
| `TODO-quarantine.md:14` | 2026-08-06 | dead-with-mechanic | discharge law dials | Feel session ratified redoubt/discharge-law step-trigger numbers as shipped, no re-dials | "everything feels right" |
| `TODO-quarantine.md:14` | 2026-08-06 | dead-with-mechanic | fissure v1 wave | Wave stays indiscriminate and hits party members; party gate covers ring rocks only | "leave it the same, that's fine behavior" |
| `TODO-quarantine.md:17` |  | dead-with-mechanic | healstone eat | Healstone eaten heal is a flat 2d4 payload plus strange-eat pool marker | "easy to approve" |
| `TODO-quarantine.md:18` |  | dead-with-mechanic | fissure v1 level text | Stale crumble sentence deleted from Fissure level text before beta | "we have to update those before beta" |
| `TODO-quarantine.md:19` | 2026-07-27 | dead-with-mechanic | fissure v1 level text | Fissure level text Burgeoning-shaped; earth-neutral flavor line; persist-line rider stays out |  |
| `TODO-quarantine.md:19` | 2026-07-27 | live | mutation level text | All six mutation level texts signed off final as built | "oh, they're all ready to go" |
| `TODO-quarantine.md:20` | 2026-07-30 | dead-with-mechanic | strange rock | Strange rock pool power shape kept as is | "I'm fine with the power shape as it is" |
| `TODO-quarantine.md:20` | 2026-07-30 | dead-with-mechanic | strange rock | Deep four stay in strange pool at weight 5 | "as well as deep four presence" |
| `TODO-quarantine.md:20` | 2026-07-30 | dead-with-mechanic | strange rock | Strange fall distribution is a bell across nine tiers, endpoints at 1 | "most common in the middle levels but less likely at the top and bottom" |
| `TODO-quarantine.md:20` | 2026-07-30 | dead-with-mechanic | strange rock | Throwable strange rock mirrors the fall bell across mirror bands | "fix this to mirror, that's its purpose" |
| `TODO-quarantine.md:20` | 2026-07-30 | dead-with-mechanic | strange rock | Mimicked deep payloads free of SelfHarm; grenade-half-only mimicry fine, no change | "it's fine" |
| `TODO-quarantine.md:21` | 2026-07-30 | dead-with-mechanic | shattered stalactite | Shattered stalactite stays 1d6/p6 with 100% crack | "keep as is" |
| `TODO-quarantine.md:21` | 2026-07-25 | dead-with-mechanic | vitriolite | Regular T7 vitriolite carries a 50% etch rider, stalactite as parity reference |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | grimslate eat | Grimslate eat becomes dread-invert |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | vantabsidian eat | Vantabsidian eat becomes dark-sight, blind duration revisitable |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | vertigneiss/lullabaster eat | Vertigneiss and lullabaster belly-keep with 25%-gated swallows |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | hexstone eat | Hexstone eaten detonate cut |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | conchalk eat | Conchalk eat kept |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | lodestone eat | Lodestone eat becomes fetch |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | oilstone eat | Oilstone eat becomes sure-greased, user's shape |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | tanglelith eat | Tanglelith eat becomes sure-footed |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | holographic eat | Holographic rock eat is a zero-satiety joke, user's line verbatim |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | compressed ash eat | Compressed ash gets its EatenEffect marker |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | eaten table | Eat warn line kept as is; mild riders surfaced and kept at 10% |  |
| `TODO-quarantine.md:23` | 2026-07-31 | dead-with-mechanic | eaten table | 25% is the house witchwood rate |  |
| `TODO-quarantine.md:24` | 2026-07-22 | superseded | spacetime cooking domain | Spacetime cooking domain judged worth building | "worth it" |
| `TODO-quarantine.md:26` | 2026-07-27 | dead-with-mechanic | Lithovore description | Lithovore description final; devour and satiety stay undocumented | "this is a complete description" |
| `TODO-quarantine.md:39` |  | dead-with-mechanic | spiderweb stone | Spiderweb stone is one-use on throw |  |
| `TODO-quarantine.md:39` |  | dead-with-mechanic | melting rock | Melting rock gear-damage half cut; pure acid puddle |  |
| `TODO-quarantine.md:41` | 2026-07-22 | superseded | geyser | Geyser design: per-stone variants plus a stronger omnigeyser rolling its stone |  |
| `TODO-quarantine.md:42` | 2026-07-22 | dead-with-mechanic | stalagmite pair | Stalagmite pair: surface spawns stalagmites only, caves stalactite-heavy |  |
| `TODO-quarantine.md:42` | 2026-07-22 | dead-with-mechanic | stalagmite | Stalagmite fragility HP 1 AV 0, drops stalactite item on break | "plant-wall" |
| `TODO-quarantine.md:47` |  | dead-with-mechanic | geyser | Three tiered omnigeysers replace per-stone geyser variants |  |
| `TODO-quarantine.md:49` | 2026-07-22 | dead-with-mechanic | baboon loadout | Baboon rock counts: basic 1-2, Hulking/Shrewd 2-3 | "more rocks to show they're stronger" |
| `TODO-quarantine.md:49` | 2026-07-22 | dead-with-mechanic | baboon scavenger | Scavenger priority: enemy at distance <= stone wins; only strictly nearer stone detours |  |
| `TODO-quarantine.md:56` | 2026-07-22 | superseded | baboon figurine | Figurine: plain-stone shots, active in hand/thrown/ground; transforming pet earmarked; parked |  |
| `TODO-quarantine.md:90` |  | dead-with-mechanic | fungal rocks | Fungal rock to infection mapping random per run, consistent within run |  |
| `TODO-quarantine.md:105` | 2026-07-22 | dead-with-mechanic | drainstone | Drainstone latch displaces worn gear, rerolling natural-weapon slots | "cursed equipment" |
| `TODO-quarantine.md:105` | 2026-07-22 | dead-with-mechanic | drainstone | Drainstone latch has fixed duration then crumbles |  |
| `TODO-quarantine.md:105` | 2026-07-22 | dead-with-mechanic | drainstone | Step latches at half duration; drainer is owner; unowned falls back to random drainer |  |
| `TODO-quarantine.md:105` | 2026-07-22 | dead-with-mechanic | drainstone | Drainstone leech is mineral, works in normality |  |
| `TODO-quarantine.md:112` | 2026-07-22 | dead-with-mechanic | baboon figurine | Figurine is creature-first: ape-statue pattern, weaker but ranged, baboon loadout/scavenge, permanent |  |
| `TODO-quarantine.md:112` | 2026-07-22 | dead-with-mechanic | figurine/ape statue | Melee fallback is stone mimicry of natural weapons, same for ape statue |  |
| `TODO-quarantine.md:124` | 2026-07-24 | dead-with-mechanic | strange rock | Strange mimic must really perform its rolled effect, place-then-fire, no null | "everything it does should actually happen, no null" |
| `TODO-quarantine.md:124` | 2026-07-30 | dead-with-mechanic | strange rock | Deep four kept in strange pool at weight 5 |  |
| `TODO-quarantine.md:143` | 2026-07-22 | dead-with-mechanic | caromcobble | Caromcobble, the bouncing rubber rock, ruled and built as rock #56 | "a rubber rock that bounces" |
| `TODO-quarantine.md:146` | 2026-07-22 | dead-with-mechanic | stalactite item | Stalactites exempted from anti-hoarding decay |  |
| `TODO-quarantine.md:146` | 2026-07-22 | dead-with-mechanic | rock decay | Dull clock re-ruled 1000-2500 turns, per-rock expiry rolled once |  |
| `TODO-quarantine.md:146` | 2026-07-22 | dead-with-mechanic | rock decay | Decay product is weightless pebbles instead of plain rock | "functionally the same end result" |
| `TODO-quarantine.md:148` | 2026-07-22 | dead-with-mechanic | rock weights | BaseFallenRock weight 5 to 1 | "fissure stones should be lighter" |
| `TODO-quarantine.md:148` | 2026-07-22 | dead-with-mechanic | pebbles | Pebbles weigh 0 (user asked 0.1, int engine) |  |
| `TODO-quarantine.md:148` | 2026-07-22 | dead-with-mechanic | stalactite item | Stalactite keeps weight 5 by explicit override |  |
| `TODO-quarantine.md:148` | 2026-07-24 | dead-with-mechanic | Lithovore satiety | All fissure rocks eat as half snacks, blessed knock-on of weight 1 | "half-snack satiety" |
| `TODO-quarantine.md:162` | 2026-07-25 | dead-with-mechanic | seam living falls | Per-cast living-fall cap 3-5, rest of seam rerolls tier table | "i think capping somewhere like, 3-5 and then letting the rest of the seam roll something of that tier is fair" |
| `TODO-quarantine.md:163` | 2026-07-24 | dead-with-mechanic | millstone grind | Menu grind on the mill ruled out; mill home becomes hopper-only | "sounds great - menu probably goes" |
| `TODO-quarantine.md:164` | 2026-07-25 | dead-with-mechanic | caromcobble | Caromcobble StepChance 25 to 50 | "50 was my leaning also" |
| `TODO-quarantine.md:166` | 2026-07-25 | dead-with-mechanic | phasalt | Phasalt Phased payload withheld from walls, logged honestly; wall-walk parked as slim potential | "agree, put it in design budget as a slim potential" |
| `TODO-quarantine.md:167` | 2026-08-01 | dead-with-mechanic | strange/drainstone mimic | Mimic surrogates stamped; early unlatch obliterates them instead of minting siphonyx loot | "b works" |
| `TODO-quarantine.md:188` | 2026-07-25 | dead-with-mechanic | oxidian eat | Oxidian eat stamps RustOnHit on natural weapons for 50 turns | "i'm fine with oxidian getting rust on hit" |
| `TODO-quarantine.md:203` | 2026-07-22 | superseded | streakschorl eat | Streakschorl eat is a thunderclap dazing adjacent creatures 1d2+1 |  |
| `TODO-quarantine.md:204` | 2026-07-25 | dead-with-mechanic | streakschorl eat | Streakschorl thunderclap eat cut for now after revisit |  |
| `TODO-quarantine.md:317` | 2026-07-22 | dead-with-mechanic | sludgestone eat | Sludgestone eat spawns slime pool underfoot |  |
| `TODO-quarantine.md:317` | 2026-07-22 | dead-with-mechanic | urchinite eat | Urchinite eat is a quill burst sparing the eater |  |
| `TODO-quarantine.md:317` | 2026-07-22 | dead-with-mechanic | vitriolite eat | Vitriolite eat is 1d3 acid plus acid puddle |  |
| `TODO-quarantine.md:317` | 2026-07-22 | superseded | hexstone eat | Hexstone eat is RealityStabilized 50 turns plus its normality grenade bursting |  |
| `TODO-quarantine.md:317` | 2026-07-22 | superseded | vantabsidian eat | Vantabsidian eat is Blind 50 |  |
| `TODO-quarantine.md:317` | 2026-07-22 | dead-with-mechanic | lovestone eat | Lovestone eat is Lovesick 50, beloved = nearest visible creature | "you fall for the next thing you see" |
| `TODO-quarantine.md:317` | 2026-07-22 | dead-with-mechanic | shieldstone eat | Shieldstone eat is a static force bubble, about 10 turns |  |
| `TODO-quarantine.md:317` | 2026-07-22 | dead-with-mechanic | phasalt eat | Phasalt eat is Phased 1d6+4 |  |
| `TODO-quarantine.md:317` | 2026-07-22 | superseded | streakschorl eat | Hypersonic eat is thunderclap (round 11) |  |
| `TODO-quarantine.md:317` | 2026-07-22 | dead-with-mechanic | pulsestone eat | Pulsestone eat is its EMP grenade bursting in you |  |
| `TODO-quarantine.md:317` | 2026-07-22 | dead-with-mechanic | ajistone eat | Ajistone eat is a random conch-roll gas cloud, density 40 |  |
| `TODO-quarantine.md:317` | 2026-07-22 | dead-with-mechanic | blurring eat | Blurring eat is one allied fugue copy of you |  |
| `TODO-quarantine.md:317` | 2026-07-22 | dead-with-mechanic | gossamarl eat | Gossamarl eat spills 3 webs around you |  |
| `TODO-quarantine.md:326` |  | dead-with-mechanic | ground powders | Powder split: per-rock ground blueprints found by name convention | "i lean the latter" |
| `TODO-quarantine.md:326` | 2026-07-24 | dead-with-mechanic | ground powders | Per-rock (not per-domain) powders blessed; per-rock spice uniqueness is wanted | "fine by me! i was looking forwards to the history spice for each rock feeling a bit unique" |
| `TODO-quarantine.md:353` | 2026-07-22 | dead-with-mechanic | galvena eat | Galvena eat is a self-discharge arc at nearest visible creature within 5, indiscriminate |  |
| `TODO-quarantine.md:353` | 2026-07-22 | superseded | streakschorl eat | Hypersonic eat is thunderclap, Dazed 1d2+1 adjacent, eater spared |  |
| `TODO-quarantine.md:353` | 2026-07-22 | dead-with-mechanic | streakschorl | Hypersonic rock named streakschorl |  |
| `TODO-quarantine.md:358` |  | dead-with-mechanic | streakschorl | Streakschorl shape: seek and bounce, no return, single use | "seek and bounce, no return. single use disk?" |
| `TODO-quarantine.md:359` |  | dead-with-mechanic | streakschorl | Streakschorl to-hit hybrid: aimed target auto-hits, bounces roll vs DV |  |
| `TODO-quarantine.md:360` |  | dead-with-mechanic | streakschorl | Streakschorl bounces 2 flat |  |
| `TODO-quarantine.md:360` | 2026-07-24 | dead-with-mechanic | streakschorl | Streakschorl per-leg range is base throw range plus 6, ratified | "streakschorl leg range fine" |
| `TODO-quarantine.md:368` | 2026-07-22 | dead-with-mechanic | pantry rules text | Pantry rules text commented out for now; rocks stay name plus flavor | "comment it back out for now" |
| `TODO-quarantine.md:372` | 2026-07-22 | dead-with-mechanic | stone milling | Milling numbers: 2-3 servings per rock, 5-6 per boulder, 1000 energy |  |
| `TODO-quarantine.md:372` | 2026-07-22 | dead-with-mechanic | stone milling | Manual pestle grinding works anywhere, no campfire, no skill gate |  |
| `TODO-quarantine.md:374` | 2026-07-22 | dead-with-mechanic | stone milling | Milling is safe: all grind warnings removed |  |
| `TODO-quarantine.md:374` | 2026-07-22 | superseded | gyrestone grind | Gyrestone grind vortex release removed; powder is the whole yield |  |
| `TODO-quarantine.md:395` | 2026-07-27 | dead-with-mechanic | malformed cragmensch | Malformed cragmensch committed: frailer than cragmensch, fabricates strange rocks | "let's knock out one of the creature spawns" |
| `TODO-quarantine.md:395` | 2026-07-27 | dead-with-mechanic | malformed cragmensch | Malformed cragmensch Toughness Boost 1, AV 10 |  |
| `TODO-quarantine.md:395` | 2026-07-27 | dead-with-mechanic | malformed cragmensch | Malformed cragmensch HP 15 and strange-rock death-drop ratified | "both are fine" |
| `TODO.md:40` | 2026-08-15 | live | tfcarverpack wish | Carver pack wish = one legendary leader plus rank-and-file mix from a poptable | "i imagined the pack would spawn a mix of each type from a poptable, with one leader" |
| `TODO.md:63` | 2026-08-29 | live | stoneshaper carve | Stoneshaper carve costs 1000 energy, one standard turn | "action cost should be 1000 right? just 1 turn" |
| `TODO.md:84` | 2026-08-11 | live | credits text | Credits roster lives in manifest.json description; lowercase names, Ava thanks cut | "credits roster should be in the manifest.json/description" |
| `TODO.md:93` | 2026-08-14 | live | Calcify VFX | Calcify cast gets a small screen shake | "let's add a small screen shake to calcify" |
| `TODO.md:96` | 2026-08-12 | live | gravel whorl devour VFX | Devour swirl renders brazier-style, a mass of pixels around the caster | "like how the fire on braziers does it's texture - a mass of pixels masquerading as the effect" |
| `TODO.md:96` | 2026-08-12 | live | gravel whorl devour VFX | Devour swirl lingers 1-2 revolutions |  |
| `TODO.md:96` | 2026-08-12 | superseded | gravel whorl devour VFX | True laser interception ruled out (would need Harmony); overdraw instead |  |
| `TODO.md:103` |  | live | wall-kick boots | Wall-kick boots are gear, not a mutation rider; Calcify combo is the point |  |
| `TODO.md:103` | 2026-08-04 | live | etch carving verb | Carving verb on chisel+mallet is item-gated, no skill required | "just having the items would be enough" |
| `TODO.md:118` | 2026-08-02 | superseded | Fissure | Redoubt pivot: Fissure reshaped into the redoubt ring form |  |
| `TODO.md:118` | 2026-08-02 | live | Fissure | The fissure wave never destroys items (chasm precedent) |  |
| `TODO.md:131` | 2026-08-09 | live | fissure pulse | Fissure epicenter = depleting sequence |  |
| `TODO.md:131` | 2026-08-09 | live | fissure pulse | Fissure animation = concept B (three ripple beats then rim) |  |
| `TODO.md:131` | 2026-08-09 | live | fissure pulse | v5 staircase damage/stun numbers per pulse and level |  |
| `TODO.md:132` | 2026-08-10 | live | fissure pulse | Fissure ring radius 4 to 3 | "i think the ring radius should be 3" |
| `TODO.md:133` | 2026-08-10 | live | fissure pulse | Allies and own statues take fissure damage; caster-only exemption | "i don't think there's much precedent for damaging effects not hurting your allies in qud... not super interested in carving out an exception" |
| `TODO.md:151` | 2026-08-22 | superseded | mudskin girdle | Unwritten armor renamed mudskin carapace | "renamed the unwritten armor" |
| `TODO.md:152` |  | live | mudskin girdle | Carapace statline 1 AV / +1 DV bare, bleed cap +2 AV | "i could see 1AV/1DV for the carapace even tbh, and then have +2 as the bleed cap so situationally it can give more AV while innately having more DV" |
| `TODO.md:153` |  | live | mudskin girdle | Shed is timed decay; hardening flakes off stepping down -1 per interval | "step down is fine" |
| `TODO.md:153` |  | live | mudskin girdle | Hardening accrual shown as a status effect for legibility | "we could have it as a status for legibility?" |
| `TODO.md:155` |  | live | flint-spike pauldrons | Doublet statline 2 AV / -1 DV | "i lean 2AV/-1DV and 0AV/2DV" |
| `TODO.md:156` |  | live | flint-spike pauldrons | Doublet bleed trigger = any adjacent melee | "adjacent melee reads fine to me" |
| `TODO.md:159` |  | live | pauldrons / girdle | Names: flint-spike pauldrons and mudskin girdle (girder typo fixed) | "lets have them be flint-spike pauldrons, and mudskin girder. this reduces overlap with basegame items i think" |
| `TODO.md:160` |  | live | pauldrons / girdle | Sprites: girdle = bark armor tile, pauldrons = studded leather tile | "for sprites, mudskin should use bark armor while pauldrons can use studded leather armor" |
| `TODO.md:168` | 2026-08-22 | live | seven item descriptions | Seven item description strings locked as final user text | "okay, done, log these" |
| `TODO.md:229` | 2026-08-22 | live | mole cricket | Mole cricket: destroy on unequip; digs walls, maybe etches, pecks 1d2 | "yes it inherits destroy on unequip - and yes my idea is that it sometimes digs a wall, *maybe* sometimes etches a wall, and sometimes attacks for a small dice roll (thinking something like 1d2)" |
| `TODO.md:229` | 2026-08-22 | live | mole cricket | Cricket AddsRep +200 Insects and +200 Quarriers | "+200 rep with insects, +200 rep with stonecarvers" |
| `TODO.md:229` | 2026-08-22 | live | mole cricket | Cricket dig is idle-gated (wearer standing still, no target) | "idle gating works i think" |
| `TODO.md:237` | 2026-08-23 | live | seismophone | Seismophone wall gate = any wall | "i think any wall is fine" |
| `TODO.md:237` | 2026-08-23 | live | seismophone | Seismophone grants mutation level 2 / radius 7 | "this is fine" |
| `TODO.md:237` | 2026-08-23 | live | seismophone | Seismophone is tier 3 | "tier 3 probably" |
| `TODO.md:237` | 2026-08-23 | live | seismophone | Liquid-ping/aquifer half skipped, retired not deferred | "fine to skip this, we don't need it" |
| `TODO.md:237` | 2026-08-23 | live | seismophone | Grant toggles silently; status via examine state | "sure, sounds fine" |
| `TODO.md:250` |  | live | seismophone | Name is seismophone (wall-ear retired) unless a better one turns up | "seismophone is wall-ear unless we come up with a better name, i was just thinking that'd be good" |
| `TODO.md:254` | 2026-08-22 | live | piezoelectric cell | Grinder grinds anywhere, trickle not burst; mill law inapplicable | "invalid as no armed stones made it in!" |
| `TODO.md:254` | 2026-08-22 | superseded | piezoelectric cell | Initial dials ratified: MaxCharge 8-10k, weight x20, trickle 10/turn, no size cap | "all this is fine, no worries" |
| `TODO.md:254` | 2026-08-22 | superseded | piezoelectric cell | MaxCharge 50000, spawns 1/5 to 1/2 full | "make the max charge something like 50k and have it usually spawn 1/5th to half full" |
| `TODO.md:254` | 2026-08-22 | live | piezoelectric cell | Cell carries the fed stone's weight, decrementing as it grinds | "grinder cell should probably preserve the weight of what its grinding and steadily decrement that over time?" |
| `TODO.md:254` | 2026-08-23 | live | piezoelectric cell | Feed picker also offers stones in adjacent tiles, ground in place | "piezoelectric should pull stones from adjacency rather than just your inventory, so selecting feed a stone knows if a boulder is in the tile next to you" |
| `TODO.md:254` | 2026-08-23 | live | piezoelectric cell | Dials: StartCharge 2500-10000, cap 25000, 50 charge per pound | "lower the starting charge to 2500-10000, lower the cap to 25000, and raise the grind to *50 charge" |
| `TODO.md:254` | 2026-08-23 | live | piezoelectric cell | Stop-grinding action added; remaining charge forfeited | "there should probably be a stop grinding option in case you cell fills up but its still grinding away at rocks and weighing a ton" |
| `TODO.md:256` | 2026-08-22 | live | item tinkerability | Only seismophone and grinder cell are tinkerable | "only one i'd want tinkerable are the seismophone and the grinder cell" |
| `TODO.md:258` | 2026-08-22 | live | piezoelectric cell | Grinder cell renamed piezoelectric cell | "piezoelectric makes sense, yeag" |
| `TODO.md:262` |  | live | animation table | Table output is a bonded creature carrying the figurine's rep feeling | "bonded creature - shares a positive faction feeling copying the reputation part of the figurine." |
| `TODO.md:263` |  | live | animation table | Walker echoes the depicted creature per stoneshaper statue rules | "stoneshaper rules" |
| `TODO.md:264` |  | live | animation table | Any figurine material accepted as input |  |
| `TODO.md:265` |  | live | animation table | Mason-only exclusivity deliberately ended | "it's because its mason exclusive that i'm broadening it. good to spread these things around so they feel intentional." |
| `TODO.md:271` |  | live | animation table | Walkers are stoneshaper level 1 or 2 (Claude picked 1), statues in miniature | "we can just have the figurines be like, lvl 1 or 2 (your pick) using stoneshaper tech so they're just statues in miniature." |
| `TODO.md:271` |  | live | animation table | Flat charge cost per animation | "charge could fluctuate but it probably doesn't need to" |
| `TODO.md:278` |  | live | animation table | Table runs on an internal cell; mill/electrical variety parked not dead | "for now, the table with an internal cell makes sense." |
| `TODO.md:281` | 2026-08-29 | live | warren power grid | Universal Charging Station ruled out of the warren grid | "nay on the universal charging station - it doesn't work in base game" |
| `TODO.md:282` | 2026-08-29 | live | warren power grid | Rock Tumbler and Kiln both included as grid consumers | "include both" |
| `TODO.md:283` | 2026-08-29 | live | piezoelectric mill | Mill seating law: wall/corner, never blocking paths |  |
| `TODO.md:283` | 2026-08-29 | live | warren power grid | Wired-rate knob for flourishing warrens greenlit; number unruled | "i was wondering if we should have the wired rate be something like 50-75% for flourishing warrens, so not all of them have them. but that would also mean some worlds would spawn without that infrastructure" |
| `TODO.md:284` |  | live | animation table | Walker keeps the Swarmer part | "keeping swarmer is fine" |
| `TODO.md:285` |  | live | animation table | Walker carries the rep or a faction feeling (Claude picked faction feeling) | "walker should carry the rep itself, or have a faction feeling about whatever the rep was" |
| `TODO.md:287` | 2026-08-24 | live | animation table | Walker description copies the figurine's description | "the description should copy the figurine description" |
| `TODO.md:289` |  | live | animation table | Table spawns one animate figurine adjacent when created | "the table should try to spawn the figurine creature in an adjacent tile when created" |
| `TODO.md:294` | 2026-08-22 | live | box of chalk | Chalk strokes auto-orient | "agree" |
| `TODO.md:294` | 2026-08-22 | live | box of chalk | Chalk draws on adjacent cells |  |
| `TODO.md:294` | 2026-08-22 | live | box of chalk | Chalkboard writing is both typed and markov | "both" |
| `TODO.md:294` | 2026-08-22 | live | box of chalk | Box of chalk has 8-12 uses | "sounds fine!" |
| `TODO.md:304` | 2026-08-23 | live | chalk draw VFX | Chalk draw VFX = floor mask dispelled by a white puff; stock flameburst ruled out | "applying an opaque background mask over the tile, having a sort of small puff of white particles, like what's used for fires, which reveals/dispels the mask in a dynamic motion - this would be for drawing it on the ground. transitions from one state to another would still be messy but that's fine. we could then reuse the puff effects in a slightly different way for the chalkboard, but let's build ground first" |
| `TODO.md:308` |  | live | chalk smear VFX | Smear = the draw reveal inverted; motes mixed grey and white | "let's have smear be the same animation but inverted - also, since we've fixed the bug, we can have the pixels be a mix of grey and white" |
| `TODO.md:314` | 2026-08-22 | live | chalkboard | Space on a chalkboard triggers the write prompt | "pressing space on a chalkboard should trigger the writing prompt" |
| `TODO.md:314` | 2026-08-22 | live | chalkboard | Chalkboard look-text mirrors the graffiti frame verbatim in structure | "agreed on all these" |
| `TODO.md:315` | 2026-08-26 | superseded | box of chalk | Chalk stick count shown stack-style (x9) | "chalk stick presentation should shift to stack style" |
| `TODO.md:315` | 2026-08-26 | live | box of chalk | Chalk stick count uses ingredient-servings grammar: [9 sticks] | "stacker language for the amount of sticks left is confusing. let's do something more like how it describes liquids or ingredient servings" |
| `TODO.md:361` | 2026-08-06 | live | Earthen Barrage | Daze rider cut from Earthen Barrage | "cutting the daze rider from earthen barrage" |
| `TODO.md:362` | 2026-08-06 | live | gravel whorl | Whorl reserve-size AV and resonant DV stat shifts cut | "cut stat shift from whorl" |
| `TODO.md:363` | 2026-08-06 | live | Earthen Barrage | EB penetration bonus Lv/2 to Lv/3; dice untouched | "I lean penetration cut, yeag" |
| `TODO.md:364` | 2026-08-15 | live | Earthen Barrage | EB damage tuned lower (resolved by ladder F) | "note: tune earthen barrage damage lower" |
| `TODO.md:373` | 2026-08-15 | live | Earthen Barrage | Ladder F: hand-authored EB damage table L1-10 ending 4d6+1 | "we can go with F" |
| `TODO.md:380` | 2026-07-23 | superseded | gravel whorl resonant DV | Resonant whorl grants +2/+4 DV in place of AV as compensation | "DV is less valuable than AV so you need more to compensate" |
| `TODO.md:389` | 2026-08-05 | superseded | gravel whorl resonant DV | Resonant DV swap kept as shipped for the beta; phase-2 look item | "keep as shipped, note it might need looking at during phase 2" |
| `TODO.md:407` |  | superseded | gravel whorl outcomes | Whorl outcome split = even thirds |  |
| `TODO.md:407` |  | live | gravel whorl outcomes | Whorl outcome split back to 50/30/20 devour/swat/ricochet |  |
| `TODO.md:408` | 2026-07-22 | live | gravel whorl devour | Dangerous stones devoured are torn apart and their effects trigger on the defender |  |
| `TODO.md:408` | 2026-07-23 | live | gravel whorl devour | Non-stone real items devoured are obliterated | "throwable items are almost always disposable… if you were throwing expensive items at an enemy's whorl to see what happens, you deserve to find out" |
| `TODO.md:408` | 2026-07-23 | live | gravel whorl devour | All devoured grenades explode at the defender, not just mod stones | "as long as when a grenade hits the whorl, it explodes" |
| `TODO.md:410` |  | live | gravel whorl ricochet | Ricochet = random-angle line trace, range 5, projectile's own damage, no payload fire |  |
| `TODO.md:412` | 2026-07-23 | live | gravel whorl ricochet VFX | Ricochet line renders staggered gravel-blip trace plus endpoint dust puff | "we could mess around with animations later on" |
| `TODO.md:425` | 2026-08-07 | live | Stoneshaper statue text | Rough statues read 'crudely depicts'; other tiers keep base text | "we could go even simpler and *just* do tier 1" |
| `TODO.md:427` | 2026-08-12 | live | Stoneshaper copy decay | Statue-of-statue copies decay a random stat, but not on the first copy | "i don't think it should happen the *first* time you make a copy, but every time you copy the statue after that, the stats should get dinged" |
| `TODO.md:429` | 2026-07-21 | live | Stoneshaper | Stoneshaper Lv10 permanence kept as capstone |  |
| `TODO.md:453` |  | live | Calcify | Calcify balance target: par with Force Wall, a bit stronger for investment |  |
| `TODO.md:453` |  | live | Calcify | Calcify BASE_WALLS_CREATED 3 to 4 (9 at Lv10) |  |
| `TODO.md:455` | 2026-08-05 | live | Calcify | Fugue-clone Calcify walls dissipate with the clone (ExistenceSupport) | "sounds fine" |
| `TODO.md:456` | 2026-07-23 | live | Calcify | NPC Calcify casting built (NPC-caster gap closed) |  |
| `TODO.md:457` | 2026-08-06 | live | Calcify | Calcify walls are true copies of the adjacent wall; no HP scaling per level | "calcify should fully copy the wall it's adjacent to if possible, including description and stats. it should no longer gain HP scaling per level" |
| `TODO.md:458` | 2026-08-06 | live | Calcify | Calcify cooldown scales down on odd levels (try-out) | "we can try out cool down" |
| `TODO.md:471` | 2026-08-06 | superseded | Fissure | Fissure radius fixed at 4; damage/stun ladders steepen by band instead | "instead of fissure range expanding with level, it could get a bit more damage and stun increment" |
| `TODO.md:474` | 2026-08-07 | live | Stoneshaper statue | Statue death: popup on combat death and natural expiry, log line only on swap carve | "popup on natural expiry - yes. popup on swap carve? stay quiet" |
| `TODO.md:545` | 2026-08-27 | live | rockhound | Rockhound actively seeks loose stones within radius 8 | "option 2 is fine" |
| `TODO.md:548` | 2026-08-16 | live | beast dialogue | Baboon and rockhound stay mute (no conversation script) | "if basedog is mute, then we're good on rockhounds also" |
| `TODO.md:554` |  | live | carver conversations | Carver farewell line is 'Rust and earth.' | "it could be rust and earth, or just the default live and drink" |
| `TODO.md:555` |  | live | Quarriers' Guild rep | Quarriers' Guild InitialPlayerReputation -200 (Bey Lah precedent) | "pack opens wary - maybe 100 rep with player. check precedent, something like bey lah could be good" |
| `TODO.md:556` | 2026-08-29 | live | Quarriers' Guild rep | Base rep stays -200; no genotype split; partreputation on mutations declined | "i wouldn't use partrep here, it would scale too high with all the mutations" |
| `TODO.md:557` |  | live | overseer | Overseer same water ritual as rank and file; own dialogue bank | "overseer doesn't differ. uses own dialogue bank" |
| `TODO.md:562` | 2026-08-22 | live | etch tools | Steel/carbide chisels and mallets removed; base knives and hammers etch instead | "oh I actually wanted to remove steel/carbide chisels and mallets and just use base knives and hammers, note that down" |
| `TODO.md:570` | 2026-08-22 | live | outrider kit | Outrider kit locked (dagger+hammer/shield, baboon item, chest list); quarrier spine lightened | "let's lighten up the quarrier kit spine so it just has what's absolutely essential/misc items and then lock in some specifics for the outrider ... dagger + hammer, or dagger/hammer + shield. dagger + hammer could also have a buckler. a random baboon table item with a bias towards bracers for synergy" |
| `TODO.md:578` | 2026-08-22 | live | quarrier kit spine | Spine: 80% tier-1 shoes; torches vary to lanterns with shards, rarely glowspheres | "for the spine - lets have an 80% chance for tier 1 shoes and vary torches so they can also be lanterns with shards and rarely glowspheres." |
| `TODO.md:586` | 2026-08-22 | live | outrider kit | Outrider kit loosened with curated variance, not tier tables | "do you think they're too restrictive? i can broaden what they have or just have it roll a tier table if that would be more game accurate" |
| `TODO.md:588` |  | live | carver arms tables | Carver knife/hammer material ladder: blunted/splintered, steel, carbide | "blunted/splintered - steel - carbide is my lean" |
| `TODO.md:588` |  | live | etching tool gate | Base tools etch: any cudgel-skill striker plus short-blade edge is an etch kit | "broad is fine" |
| `TODO.md:590` |  | live | outrider kit | Ring Mail and Chain Mail join the chest pickone as tier-2 highrolls | "some tier 2 highrolls could be good - something like ring and chain mail make sense to me" |
| `TODO.md:641` | 2026-08-17 | live | quarry warrens | Stage axis: work roots always visible, camp then leisure as the dig succeeds |  |
| `TODO.md:649` | 2026-08-17 | live | quarry warrens | Payloads 0-2 per warren, riding the stage axis |  |
| `TODO.md:751` | 2026-08-18 | live | warren organ blocks | Organ blocks need their own entrances | "organ blocks *do* need entrances also." |
| `TODO.md:769` | 2026-08-18 | live | warren stages | Young dig stage cut; stages are working/camp/flourishing |  |
| `TODO.md:839` | 2026-08-18 | live | warren spawn rate | 25 warrens per world |  |
| `TODO.md:868` | 2026-08-18 | live | warren occupancy | Occupancy: three whole-site states, in use / abandoned / reclaimed |  |
| `TODO.md:920` |  | live | cave moss | Cave moss is harvestable; wall reverts to plain form | "the moss should be harvestable, reverting the wall to its basic form" |
| `TODO.md:920` |  | live | cave moss | Moss item named 'clump of cavemoss', split-colored dark grey / green | "clump of (dark grey)cave(green)moss" |
| `TODO.md:958` |  | live | warren carve placement | Archipelago zones are valid quarry country; rind/bubble/pinch fixes landed | "i could see it being a valid quarry spot, we just have to get smarter about how we're placing them" |
| `TODO.md:970` | 2026-08-23 | live | warren refusal handling | Three-fix bundle for warren refusal accepted (rumors, retry policy, overclaim) | "like all three of these fixes" |
| `TODO.md:974` |  | live | warren refusal handling | Refused warren zones erase their rumor outright instead of a prospect fallback | "i'd prefer if the 10 or so failed carves just stopped existing, so that we end up with 25 somewhat real warrens" |
| `TODO.md:978` | 2026-08-23 | live | warren carve | Box-starvation fork resolved: port blockMode plus retry-with-blocks policy |  |
| `TODO.md:986` | 2026-08-23 | live | warren refusal handling | Refusal fork resolved by the three-fix bundle | "like all three of these fixes" |
| `TODO.md:1099` | 2026-08-18 | live | warren hearth dress | At most one oven per warren site | "gen 3 here resulted in 2 ovens - they should be capped at one" |
| `TODO.md:1136` | 2026-08-18 | live | warren carve layout | Carve reopened: claim-and-subdivide block layout as an A/B (default on) | "i would be open for reopening carve to try implementing that" |
| `TODO.md:1190` |  | live | warren dress tables | Two basket poptables: basket_stones and basket_food (canyon preserves) |  |
| `TODO.md:1417` | 2026-08-24 | live | warren floor | Padded-earth blueprint killed; warren floors use base DirtFloor | "we don't need to do a rename, just use the base game parent for the floor" |
| `TODO.md:1441` | 2026-08-24 | live | warren dress tables | Vases and clay jugs added to dwelling dress for variety | "let's add vases and clay jugs for variety" |
| `TODO.md:1451` |  | live | warren dress tables | Cider pitcher debt retired (Cider Vase); vase and jug rows join the socket table | "we could retire that + we could add vases and jugs to work tables" |
| `TODO.md:1466` |  | live | fire dish | Fire dish is brown (&w body / r detail), superseding its grey | "fire dish could be brown also?" |
| `TODO.md:1504` |  | live | warren tunnels | Between-room tunnels take the straight-run cap; a few dead ends added | "between room tunnels seems worth trying out, yeag" |
| `TODO.md:1620` | 2026-08-25 | live | warren placement probe | Placement timesaves 1 and 3: dry runs skip dress, SCOUT_N 50 to 35 | "sure, we can do 1 and 3" |
| `TODO.md:1760` | 2026-08-26 | live | warren dress tables | Seating and sleep pool weights ratified | "agree with your weights on that" |
| `TODO.md:1788` | 2026-08-26 | live | cold fire dish | Cold fire dish keeps the brown; ash-grey recolor struck | "no need for the ash grey recolor" |
| `TODO.md:1807` | 2026-08-26 | live | warren hearth dress | 'A kitchen keeps one fire' law repealed; hearth picks unfiltered beyond one oven | "we can repeal that rule, it's probably too strict and ineffective" |
| `TODO.md:1830` | 2026-08-26 | live | warren garden breach | Garden breach built game-side directly, no sim first | "game side directly works" |
| `TODO.md:2071` | 2026-08-25 | live | warren placement | Blind-claim placement ported game-side; scout-and-quota retired | "alright, i'll say go, let's try it" |
| `TODO.md:2135` | 2026-08-25 | live | warren augment ladder | Build both contributor suggestions: wipe-and-rebuild rung plus the second | "can we tried implementing these 2?" |
| `TODO.md:2201` |  | live | warren carver code | Contributor vocabulary integrated into sim and C# (touches4/8, isOuterOf, isInnerOf) | "can we integrate this into our own code to clean it up a bit? fine if it causes some behavior to change" |
| `TODO.md:2236` |  | live | cairns | Abandoned-site cairns pre-toppled at 50/50 stays | "if in abandoned sites cairns are pre toppled with a 50/50 chance, that's fine" |
| `TODO.md:2247` | 2026-08-25 | live | warren dress | Tandem docket: cairn pass, bracelets and nuggets to tables, squatters sub-roll | "i figure we can add the other low impact ones in tandem, like building the cairn pass (sprites should be in now), and adding bracelets and nuggets to tables, with your lean being fine there" |
| `TODO.md:2323` | 2026-08-25 | live | warren dress tables | Fallen rock blueprint out of the dress table; base rocks instead | "we should remove the fallen rock blueprint from the dress table and replace it with basegame rocks" |
| `TODO.md:2343` |  | live | warren squatters | Squatters as a sub-roll within abandoned sites; game-side placement held |  |
| `TODO.md:2343` | 2026-08-26 | live | warren squatters | Phase spider cut from the squatter draw; Cave Spider only | "i believe it should only be cave, phase spiders don't spawn in the desert canyons as far as i know" |
| `TODO.md:2359` | 2026-08-29 | live | warren plazas | Plaza register = commons/leisure at every stage; density rides the stage axis | "i think working sites can have leisure too, it kind of flattens them to be *just* work" |
| `TODO.md:2367` | 2026-08-26 | live | cairns | Cairn restacking ruled in as build work | "add cairn restacking as something to be built on here" |
| `TODO.md:2409` |  | live | warren statues | Warren statues depict canyon creatures, stonecarvers, or generic shrine shapes | "statues should be of creatures in the canyons, stonecarvers, sometimes just generic shrine shapes." |
| `TODO.md:2424` | 2026-08-18 | live | warren abandoned tense | Abandoned tense: no funerary urns, campfire remains, much more trash; base warrens more trash | "for abandoned - no on funerary urns, yes to campfire remains - they should have substantially more trash than a living warren - and base warrens should have more trash in general." |
| `TODO.md:2429` |  | live | warren dress tables | Funerary urn's abandoned-tense home overruled; do not resurrect |  |
| `TODO.md:2501` | 2026-07-22 | superseded | Quarriers' Guild rep | Reception neutral at baseline, trending positive; trade at minimum |  |
| `TODO.md:2502` | 2026-07-22 | live | overseer | Water ritual: yes, via GivesRep on the legendary |  |
| `TODO.md:2503` | 2026-07-22 | live | carver packs | Pack structure = legendary-pack shape (distinct types, one legendary) |  |
| `TODO.md:2504` | 2026-08-14 | live | legendary naming | Legendary naming: bespoke Naming.xml, etruscan-chopped name + english craft byname | "i feel like this works and we can bank it" |
| `TODO.md:2515` | 2026-08-15 | live | legendary naming | Two-axis byname: material half grants a perk, craft half grants the kit | "steal the combinative part of goatfolk heroes where the clan has an effect, for our prefixes" |
| `TODO.md:2519` | 2026-08-15 | live | legendary naming | Byname delivered as a hero epithet via the goatfolk-builder structure |  |
| `TODO.md:2520` | 2026-08-15 | live | legendary naming | Name-drives-kit: the epithet bank is the mutation pool manifest | "sounds fine to bank" |
| `TODO.md:2521` | 2026-08-15 | live | legendary naming | Word-to-grant mapping as drafted is the bank; Gorgon word deferred | "the word mapping is fine" |
| `TODO.md:2538` | 2026-08-07 | unclear | mole-beast | Mole-beast is a single tile, star-kraken idiom; no multi-object body | "that's what qud would do" |
| `TODO.md:2541` | 2026-08-15 | live | mole carvers | PathAsBurrower stays on for mole carvers; revisit if it reads badly | "i'm not super precious about quarry design and giving some pickaxes and burrowing claws pathasburrower part is pretty much fine, unless i see it in game and it works out badly." |
| `TODO.md:2544` | 2026-08-15 | live | carver colors | Mason role color = grey | "mason grey is fine" |
| `TODO.md:2544` |  | live | carver colors | Base carver colors fine as shipped: grey body, dark grey detail | "these are fine for base colors" |
| `TODO.md:2555` | 2026-08-24 | superseded | fissure stone flavor | Fissure stone flavor: Claude's gut, user edits later (clay flavor was mis-aimed) | "oh sorry, i meant figurine grenade can be lump of primordial clay - not sure on the flavor for the fissure one, just go with your gut and i'll edit it later however i desire" |
| `TODO.md:2556` |  | live | figurine grenade | Figurine grenade named 'lump of primordial clay', copper nugget tile placeholder |  |
| `TODO.md:2570` | 2026-07-22 | superseded | overseer | Overseer mutation pool = the roster three, 2 with chance for 3 |  |
| `TODO.md:2571` | 2026-08-15 | live | carver roster | Carver types distinguished by profession noun first | "profession first is fine, it's a qud idiom" |
| `TODO.md:2577` | 2026-07-22 | live | outrider | Thrower type carries the real EB mutation (no Boulderer mimic) |  |
| `TODO.md:2578` | 2026-08-15 | live | Quarriers' Guild | Faction named Quarriers' Guild |  |
| `TODO.md:2592` | 2026-07-24 | superseded | canyon painter | Painter architecture: recursive recipe primitive, depth cap 3, no backtracking | "genius — solves a problem I've had since their inception" |
| `TODO.md:2595` | 2026-07-24 | live | warren infirmary | Dust sickness is flavor only, no disease mechanic |  |
| `TODO.md:2596` | 2026-07-25 | superseded | warren architecture | Warrens are node-pockets joined by divot tunnels; chambers stay den-sized |  |
| `TODO.md:2597` | 2026-08-07 | live | canyon painter | Two-phase painter: carve first, decorate second |  |
| `TODO.md:2599` | 2026-08-07 | live | warren bore | Bore smoothed-wall lining cut for now | "remove bore smoothed wall for now" |
| `TODO.md:2599` | 2026-08-07 | live | carve sim canyon | Sim canyon generation made faithful to Qud's CanyonBuilder | "as close to how qud's will be as possible" |
| `TODO.md:2623` |  | live | cave moss | Moss is wall-mounted; the wall tile recolors, harvested bit is an item | "i think the rock should spawn on the wall? that way we can just cheaply recolor it when it's time to harvest? and then just have an item for the harvested bit" |
| `TODO.md:2624` | 2026-08-11 | live | warren canyon attachment | One mouth per warren plus ~30% back door | "agree with this lean" |
| `TODO.md:2624` | 2026-08-11 | live | warren canyon attachment | A warren never crosses the canyon corridor | "this sounds fine" |
| `TODO.md:2624` | 2026-08-11 | live | warren canyon attachment | Wet pockets and deep sites fail honestly in thin bands; no shrink-to-fit | "agreed" |
| `TODO.md:2624` | 2026-08-11 | live | warren canyon attachment | Spurs are attachment surface; spur mouths mildly preferred | "i like the shape of that ruling" |
| `TODO.md:2625` | 2026-08-12 | live | warren mouth picker | Mouth candidates weighted by carvable mass behind them | "let's try that first fix" |
| `TODO.md:2626` | 2026-08-12 | live | warren mouth picker | Class-aware mouth fitting: size class rolls before the mouth, degrades to fit | "i'll go with your lean" |
| `TODO.md:2632` | 2026-08-12 | live | warren dress | Dress pass starts lived-in tense, work evidence first, gallery/communal second | "work evidence first. i think gallery/communal comes second?" |
| `TODO.md:2632` | 2026-08-12 | live | warren dress tables | Natural rocks scatter inside the warren, not canyon-only | "i'd rather have the natural rocks be *in* the warren" |
| `TODO.md:2632` | 2026-08-12 | live | warren dress tables | Boulders cut from natural and worked tables; stones, small stones, pebbles only | "cut boulders from the table and just do stones, small stones, and pebbles" |
| `TODO.md:2634` | 2026-08-12 | live | warren floor | Padded earth on the whole warren, every carved cell | "actually - let's do whole warren" |
| `TODO.md:2635` | 2026-08-12 | live | wet pocket halo | Muddy soil cut from blueprints and halo rolls | "let's remove muddy soil from the blueprints and rolls?" |
| `TODO.md:2635` | 2026-08-12 | live | wet pocket halo | Seeps/runnels replaced with puddles in the wet halo | "let's replace the runnels/seeps with puddles instead" |
| `TODO.md:2636` | 2026-08-14 | live | figurine material ladder | Floor figurines roll stone (rep same, low value) instead of always copper | "figurines on the floor have a chance to spawn stone (or wood?) instead of copper - still giving rep, but very little value" |
| `TODO.md:2636` | 2026-08-14 | live | warren dress tables | Trash included in the work dressing pool | "include trash in the work dressing pool" |
| `TODO.md:2641` | 2026-08-14 | live | warren lighting | Torchposts flank gallery entrances from inside plus one at the canyon mouth; rest dark | "i'm fine with your shape here" |
| `TODO.md:2641` | 2026-08-14 | live | carve sim lighting | Sim glow rendering cut; game renders the light | "don't bother rendering the glow of the lights - we'll see it all in game" |
| `TODO.md:2643` | 2026-08-14 | unclear | warren chest loot | Warren chest loot = work dressing table minus trash, rare copper nugget, agate whisper | "have it use our work dressing table (approximately, ignoring trash and things that'd be weird to be in a chest) as a population table of loot — rarely something nice like a copper nugget" |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Funerary urns out of dwelling pools; benches in | "because it has connotations in the tomb of the eaters" |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Chairs replaced by scrapasan and floor cushions |  |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Only generic Table and Workbench in the centre pool; trade-specific tables out |  |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Woven baskets stay |  |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Bookshelves out; scholarly room type retired |  |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Canvas folding chairs back in; hookah reinstated on its own comfort row | "so is hookah!" |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Anvil stays in crafts pool; millstone cut | "anvil is fine millstone is not necessary" |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Pickaxe and stone statue added to the crafts pool | "let's add pickaxe and statues to it" |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Quarried slabs and stone figurines join the crafts pool | "quarried slabs and figurines are also fine for work rooms" |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Tall ornate braziers out of generation | "let's take out the tall ornate braziers from generation, there's plenty of light rolling" |
| `TODO.md:2649` | 2026-08-17 | live | warren dwelling dress | Every room type's light row drops to a flat 1 | "we could roll fewer lights, yeag" |
| `TODO.md:2650` | 2026-08-12 | live | warren sealDiag | Seal accidental diagonal chamber entrances from the room side | "seal accidental diagonal entrances into chambers from the ROOM side — the hall keeps its line, the room goes quiet behind its real door" |
| `TODO.md:2665` | 2026-08-23 | live | role texture | Hewer stones level 2, 2-3 each, no redoubt ring; mason figurines 2-4 | "stone level should probably be 2, amount should be 2-3, figurine count can be 2-4. no redoubt ring" |
| `TODO.md:2667` |  | live | mason figurine swarm | Mason figurine swarm count 2-4 |  |
| `TODO.md:2668` | 2026-08-23 | live | hewer fissure stones | Fissure stone payload = pulses only, never the rim | "no redoubt ring" |
| `TODO.md:2668` | 2026-08-23 | live | hewer fissure stones | Fissure stone level 2, count 2-3 |  |
| `TODO.md:2671` | 2026-08-23 | live | quarrier kit | Digging-tool veto reversed; quarriers may carry digging tools | "i don't like that veto, i think it makes sense for quarriers to have digging tools especially if it means they dig their environment" |
| `TODO.md:2671` | 2026-08-23 | live | carver names | Display names: quarry hewer / quarry mason / quarry outrider | "close out the quarry x naming lean" |
| `TODO.md:2673` | 2026-08-23 | live | quarrier gear | Gear sitting: jackhammer chance, mining headgear table, glove expansion, shared weapon pool | "let's have all 3 quarriers use the same weapon pool. we'll have bucklers/shields as a special get for outriders" |
| `TODO.md:2680` | 2026-08-23 | live | MiningGear table | MiningGear generalized as a loot table merchants and chests can roll | "yeag i think making it a generalized loot table that merchant tables and chests can roll makes sense" |
| `TODO.md:2681` | 2026-08-26 | live | stonecarver starting gear | Stonecarver starting gear second MiningGear roll (Chance 15) cut | "flag the second roll as needing to get cut in journal? so we don't clobber ongoing workstreams" |
| `TODO.md:2709` | 2026-08-27 | live | Part 2 art | Rockhound and miner palettes final; cistern keeps the waterskin sprite | "rockhound and snapjaw miner palettes fine, cistern can keep using the waterskin sprite" |
| `TODO.md:2721` | 2026-08-22 | live | granite baboon | Cobble baboon renamed granite baboon; pelts are granitefur | "for sprites I'm leaning recolored quartz baboon - grey and brown. renamed to granite baboon. sprites are already done thanks to a friend. gloves need slight mechanical change, bracers need to be thoroughly examined" |
| `TODO.md:2721` | 2026-08-22 | live | granitefur pelts | Pelt family palette rust red / grey detail; cloak = recolored panther cloak tile | "make the detail color grey for the family" |
| `TODO.md:2721` | 2026-08-22 | live | granite baboon | Baboon numbers sitting: EB 2, HP 18, L6, AV 2, Str +2, corpse 15% | "let's lower base AV to 2" |
| `TODO.md:2721` | 2026-08-22 | live | granite baboon | Granite baboon spawns in Hills, DesertCanyon and Baboons pools plus troop merges | "i think it should spawn in all 3" |
| `TODO.md:2721` | 2026-08-27 | live | granite baboon | Recolored quartz baboon tile is the final sprite; art debt zero | "sprites are fine for granite baboon" |
| `TODO.md:2724` | 2026-08-22 | live | item spawn tables | Item spawn wiring: lantern, piezo cell, cricket, chalk, granitefur join base pick pools |  |
| `TODO.md:2731` | 2026-08-22 | live | granitefur bracer | Bracer grants +2 throw range instead of the cap-aware accuracy lend | "swap the bonus to +2 throw range and dodge the problem altogether" |
| `TODO.md:2731` | 2026-08-22 | live | granitefur gloves | Gloves catch any thrown item (not just stones), never launched projectiles | "rather than catch only thrown rocks, it could work on all thrown items mayhaps" |
| `TODO.md:2731` | 2026-08-22 | live | granitefur gloves | Caught grenade gives one turn to throw it back (hot potato) | "catching a grenade could allow you a turn before it explodes, so you can throw it back" |
| `TODO.md:2733` |  | live | baboon naming | Naming structure: creature plain, '-fur' reserved for pelt items |  |
| `TODO.md:2733` | 2026-08-13 | superseded | granite baboon | Name locked 'cobble baboon' (gravel rejected for whorl collision) |  |
| `TODO.md:2733` | 2026-08-15 | live | granite baboon | Habitat: desert canyon | "it's fine for the cobble baboon to be in desert canyons" |
| `TODO.md:2733` | 2026-08-13 | live | granitefur cloak | Cloak +2 DV counts loose floor litter/rubble, adjacent-inclusive | "loose floor litter, rubble?" |
| `TODO.md:2733` | 2026-08-13 | live | granitefur cloak | Cloak adjacent scope confirmed over Yurtmat same-tile | "i agree with that thinking" |
| `TODO.md:2733` | 2026-08-13 | live | granitefur cloak | Base Rubble heaps count as cloak cover via a narrow tag |  |
| `TODO.md:2737` | 2026-08-15 | live | carver packs | Carver packs sometimes include a rockhound | "once we make packs spawn in base game, they should include a rockhound sometimes" |
| `TODO.md:2738` | 2026-08-13 | live | rockhound | Wild stray rockhounds ruled in; table wiring deferred | "wild strays are a good inclusion. not sure we need to add them now" |
| `TODO.md:2740` | 2026-08-13 | live | rockslime | Baetyls ruled out as rockslime host faction | "baetyls are computers" |
| `TODO.md:2745` | 2026-08-11 | live | boar vessel | Inverted hierarchy: wild boar vessel is the parent, Husangeion descends from it | "just because it was made first doesn't mean it needs to be first" |
| `TODO.md:2745` | 2026-08-11 | live | boar vessel | Name 'boar vessel', plain common noun |  |
| `TODO.md:2745` | 2026-08-11 | live | boar vessel | Rain intake is flavor; basin fills via LiquidProducer | "the rain that fills it up doesn't need to be a mechanical thing — it can just be flavor that uses waterproducer" |
| `TODO.md:2745` | 2026-08-11 | live | boar vessel | Fill rate is slow | "they'd probably fill up pretty slow" |
| `TODO.md:2745` | 2026-08-11 | live | gut cistern | Gut cistern in v1 | "i would lean implement this on v1 but it depends on tech debt. since we already verified mostly everything works with husangeion, it's cheap to add something here" |
| `TODO.md:2745` | 2026-08-11 | live | boar vessel | Glaze may lie: brackish vessels glaze water-blue | "it's fine for this to lie" |
| `TODO.md:2745` | 2026-08-11 | live | boar vessel | Herd structure follows base rules: pens packs, wild solo, legendary retinues | "basegame rules pretty much" |
| `TODO.md:2745` | 2026-08-11 | live | boar vessel | Wild stats retuned from the pet's L1/AV5/HP100 | "definitely need to retune these" |
| `TODO.md:2745` | 2026-08-26 | live | boar vessel | Name locked 'boar vessel' over 'pig vessel' | "i think i'll go with boar over pig, after that debunked etruscan boar vessel thing" |
| `TODO.md:2751` | 2026-08-26 | superseded | gut cistern | Cistern ships as plain removable body-slot gear | "i lean 2" |
| `TODO.md:2759` | 2026-08-26 | live | Husangeion | Husangeion pet carries no gut cistern | "the pet gut cistern i'm less keen on ... as is you can just drain husangeion and it accumulates water naturally. if you don't want it to drink it, you can just seal it. if it has a gut cistern, suddenly unequipping it from a companion becomes a lot easier, and that cursed wrinkle reveals itself again" |
| `TODO.md:2760` | 2026-08-27 | live | gut cistern | Cistern delivered by butchering a brown/azure corpse, not equipment or inventory | "corpse recolored brown and azure, butcherable for a gut cistern" |
| `TODO.md:2760` | 2026-08-27 | live | boar vessel | Wild vessels spawn in base Boar's four biomes | "same biomes as boar, honestly" |
| `TODO.md:2760` | 2026-08-27 | live | boar vessel | Wild vessels keep the boar-tempered brain; farm child is docile | "they can be hostile unless farmed? since they're boar vessels" |
| `TODO.md:2760` | 2026-08-27 | live | boar vessel | Pig farms roll one boar vessel at Chance 15 (draft) | "boar farms should have a small chance to roll 1 boar vessel amongst their herds" |
| `TODO.md:2761` | 2026-08-27 | live | gut cistern | Gut cisterns start unsealed, ManualSeal kept | "gut cisterns can start unsealed now" |
| `TODO.md:2762` | 2026-08-27 | live | boar vessel | Boar vessel CorpseChance 50 confirmed | "sounds fine" |
| `TODO.md:2762` | 2026-08-27 | live | boar vessel | Corpse detail color = the cistern's blue (family azure) | "make the corpse detail color the same blue as the gut cistern's" |
| `TODO.md:2763` | 2026-08-11 | live | vine merchant | Trade relocates to the vine merchant; wandering packs neutral-no-trade | "i agree that it's a stronger take for sure, so pro your lean" |
| `TODO.md:2763` | 2026-08-11 | live | vine merchant | Chassis = base Jilted Lover as placeholder | "something like a jilted lover as a placeholder could be fine" |
| `TODO.md:2763` | 2026-08-11 | live | vine merchant | Wares: generic canyon trade goods plus rocks and stonecarver miscellany | "generic canyon tier trade goods, + some rocks and stonecarver miscellany (rocks, chisels, pickaxes, figurines)" |
| `TODO.md:2816` | 2026-07-25 | live | baboon stone fetch AI | Baboon fetch skips unstandable cells and gives up after 3 detours; reach-stoop rejected | "completely agree" |

## meta (21)

| ref | date | status | governs | gist | user's words |
|---|---|---|---|---|---|
| `TODO-archive.md:6` |  | live | tracker provenance | Archive header: RULED markers in this file still count as provenance |  |
| `TODO-quarantine.md:357` | 2026-07-22 | dead-with-mechanic | streakschorl | Round 12 header: streakschorl becomes seek-and-bounce flight; dials on following lines | "work more like geomagnetic disks" |
| `TODO-quarantine.md:368` | 2026-07-26 | dead-with-mechanic | pantry rules text | Provenance audit: the grenade/house idiom is Claude framing, no standing idiom is ruled |  |
| `TODO.md:5` |  | live | trackers | RULED markers in archive and quarantine still count as provenance; grep all three files |  |
| `TODO.md:20` |  | live | trackers | Description of the RULED/LEAN/PROPOSAL status-tag convention |  |
| `TODO.md:22` |  | live | trackers | Citing precedent requires finding its RULED marker; no marker = proposal |  |
| `TODO.md:151` | 2026-08-22 | live | mudskin girdle | Summary: every fork for carapace/doublet ruled at the sitting (see following lines) |  |
| `TODO.md:215` |  | live | seven items | Statement that USER-marked shapes below are ruled as direction |  |
| `TODO.md:260` | 2026-08-24 | live | animation table | Summary: figurine animation table pitched, four forks ruled on following lines | "a table that spawns in quarry warrens (and in some other tables potentially) that allows you to exchange item figurines for animate creature figurine, using stoneshaper technology. the unresolved tension is that the quarries don't have electricity, and i figure this would probably work off power cells or something? but maybe not" |
| `TODO.md:474` | 2026-08-07 | live | Stoneshaper statue | Provenance note: statue no-popup tag rationale was Claude-authored, never ruled |  |
| `TODO.md:552` | 2026-08-29 | live | Quarriers' Guild | Summary: faction docket of six items ruled (see following lines) |  |
| `TODO.md:657` |  | live | legendary overseer | Table row citing name-drives-kit (08-15) and the faction entry as ruled |  |
| `TODO.md:866` | 2026-08-23 | live | warren placement hook | Lean, not ruling: warrens placed at world build, new saves only; stamp when hedge dropped | "new saves only probably? since this will be in a new mod you should start a new save for that" |
| `TODO.md:962` |  | live | warren artery | Round 5 waypoint fix implements the ruled artery law; no new ruling |  |
| `TODO.md:982` |  | live | warren spawn rate | Cites the ruled 25-target; claims re-dialed to 27 as a Claude dial |  |
| `TODO.md:1866` | 2026-08-17 | live | warren occupancy | Cites the payloads ruling: an abandoned warren may hold nobody | "an abandoned warren could have nothing" |
| `TODO.md:2541` | 2026-08-16 | superseded | mole carvers | Species-fixed-per-profession reshape struck; it was a gloss on a sprite-budget remark | "well the tracker is wrong on this point" |
| `TODO.md:2561` | 2026-08-24 | live | hewer fissure stones | Correction: hewer fissure stones already exist; move to dowser is a real move | "i swore it got built in the stoneshaper spawning with figurines pass" |
| `TODO.md:2562` |  | live | figurine grenade | Cites the figurine grenade flavor ruling |  |
| `TODO.md:2650` | 2026-08-16 | live | warren sealDiag | User call owed on the wall-beside-door silhouette (keep, knob to 0, or narrow) |  |
| `TODO.md:2652` | 2026-08-17 | live | warren host | Red Rock host lab pass; nothing ruled, one-mouth ruling kept on a dial | "i'm looking to potentially rework the warren quarries to be structured more like red rock" |

## Sort by nature, 2026-09-02

**Reframe agreed by the user ("that sounds fine"):** the RULED tag sorts by who said it, which is a trust axis, not a description of the project. Survivors of the cull are re-sorted here by what kind of thing they are. Provenance becomes a field. Bin placement is CLAUDE PROPOSAL, UNRULED. Rows dropped under the fissure ruling, superseded rows, and per-item benches are not listed.

### Facts (true or false, settled by decompile, stale when the game patches)

| survivor | source | note |
|---|---|---|
| 1-cost and defect mutations don't level | `TODO.md:458`, user: "1 cost and defect mutations don't level" | base engine behavior the mod matches. The Geomancy exemption riding on the same line is a decision, not a fact; Lithovore's exemption is gone. |
| classic font indexes DisplayName glyphs by codepoint | `TODO-archive.md:929` | the fact under the ASCII constraint below. |

Facts want a ground-truth note pinned to a game version. The archive already holds entries shaped like that (the "decomp ground truth" records); the two above are the only facts that were wearing a ruling tag.

### Constraints (enforced, fail closed)

| survivor | source | where it lives now |
|---|---|---|
| no em dashes in shipped text | `TODO.md:83`, `TODO-archive.md:872` | CLAUDE.md + hook + `tools/check-text-rules.ps1` |
| no Co-Authored-By trailer | `TODO.md:83`, `TODO-archive.md:875` | CLAUDE.md |
| XML > C# > Harmony | `TODO.md:2046` | CLAUDE.md, RULED 2026-08-26 |
| TerraFirmaBeta is the distribution path; dev deploys never touch it | `TODO-archive.md:878`, user: "wouldn't we just make a stripped copy on the workshop?" | CLAUDE.md deploy layout |
| every mod blueprint carries the `Cleo_TerraFirma_` prefix; IDs are mechanical, flavor lives in DisplayName only | `TODO-archive.md:196` | **nowhere outside the tracker** |
| DisplayNames are plain ASCII | `TODO-archive.md:929` | **nowhere outside the tracker**; hookable, same shape as the em dash check |
| held blueprints stay compiled as inert wish-doors, never mass comment-outs | `TODO.md:118`, vouched 2026-09-02 | **nowhere outside the tracker** |
| published build strips all code comments; repo keeps everything | `TODO-archive.md:878`, user: "i'd lean full strip... i just don't want them to be obviously ai" | `tools/publish-beta.ps1`, verified: it carries a C# comment lexer (`TfCommentStripper`) and strips on publish |
| TF-VERIFY instrumentation stays on through beta, full strip at 1.0 | `TODO.md:118`, `TODO-archive.md:892`, user: "sounds good" | memory only; expires at 1.0 |

The three bolded gaps are the actionable output of this sort: constraints that exist only as tracker prose. Candidate CLAUDE.md lines, and the ASCII one is a candidate hook.

### Guidelines (directional, meant to bend, argued from)

Proposed home: prose docs in `design/`, grouped by area, each guideline carrying its date and the user's words as a field.

| survivor | source | proposed doc |
|---|---|---|
| warren goal: halfway between procgen and goatfolk villages | `TODO.md:633`, vouched 2026-09-02 | warrens |
| judging bar: roll two sites; can the user tell them apart and name each | `TODO.md:705`, user: "i totally agree with that metric, actually. it's something i've internally been trying to do on the last one" | warrens |
| worth-the-trip: a rumoured warren must have something to do | `TODO.md:769`, vouched 2026-09-02 | warrens |
| art bar: reuse base tiles and items by default; bespoke sprites as the user makes them | `TODO.md:520`, user: "i'm fine reusing tiles and items for the quarriers. some things will need their own sprites, but i'm gradually..." | warrens (or creatures, user's call) |
| mutation level text stays as close to base-game voice as possible | `TODO-quarantine.md:19`, user: "we need to make sure the level text is as close to the base game voice as possible" | text; moves out of the quarantine file per the zero-rulings-in-quarantine agreement |
| all AI-produced text is draft until the user rewrites or signs off | `TODO.md:2593` | text |
| sim-first: the carve sim is the design target, the game ports in passes | `TODO.md:2599`, user: "it's fine if the sim gets ahead, wanna have the sim be what we're aiming for, and then iterate in game in passes" | workflow |
| every mutation level must increase something | `TODO.md:458`, user: "I need to make sure every level up increases something on the mutations, including on calcify" | mutations; checkable, could graduate to a constraint |
| any mutation a stonecarver NPC carries, its AI must actually cast | `TODO.md:2500`, user: "gotta be able to fight back" | mutations (or creatures) |

Four docs, not three: warrens, text, workflow, mutations.

### Decisions (dated, point-in-time, expire with what they govern)

One file each. These are the only survivors where "ruled on date X" is the natural form.

| survivor | date | source | expires with |
|---|---|---|---|
| full move: base mod is mutations only, everything else goes to Terra Firma 2; base keeps shipped content through the TF2 beta, strip once at TF2 1.0 | 2026-08-26 | `TODO.md:36`, user: "scope is everything *not* mutations for the second mod..." / "that sounds fine" | TF2 1.0 (the transition clause) |
| design quarantine of fissure v1, its rock catalog, and Lithovore; escalated 2026-09-02 to irrelevant for the project | 2026-08-11 / 2026-09-02 | `TODO.md:2689`, user: "can we quarantine the first iteration of fissure, all the rocks it added, and lithovore..." / "all the old fissure stuff - the rocks, lithovore, eating them, are irrelevant for the project now." | nothing; terminal |
| Part 2 creature roster: miner, rockhound, granite baboon, boar vessels in; rockslime, burrowbee, burrowing urchin, stone-eating centipede, trapdoor spider on the maybe list | 2026-08-15 (line also carries 2026-08-26) | `TODO.md:2707`, user: "rockslime, burrowbee, burrowing urchin, stone eating centipede, trapdoor spider all go on the maybe list..." | the Part 2 creature pass |
| carver roster: hewer, mason, outrider, overseer by epithet; provisioner cut (trade belongs to the vine merchant); drover parked | 2026-08-15 (line carries 07-22 and 08-29 too; order needs a read) | `TODO.md:2567`, `TODO-archive.md:1193` | the stonecarver pass |
| NPC conversations: no bespoke dialogue trees, one greeting line plus standard trade | 2026-07-24 | `TODO.md:2594`, user: "same amount of work, presented differently" | the creatures workstream |

Kept inline, not filed: the creatures branch contract (`TODO.md:38`, a working agreement that expired when creatures spawned in game), redoubt fissure ships with catalog held (moot under the full move and the fissure ruling).

### What this does to the folder

`rulings/` as proposed this morning dissolves. Five decision files, four guideline docs in `design/`, three constraint lines for CLAUDE.md plus one hook candidate, two facts to a ground-truth note. Where the five decision files live (a `decisions/` folder, or inside `design/`) is open.
