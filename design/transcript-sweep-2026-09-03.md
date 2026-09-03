# Transcript sweep, 2026-09-03

**CLAUDE PROPOSAL, UNRULED.** Every user message in every project transcript (2024 messages, 2026-07-20 to 2026-09-03) was checked for a five-word run already present in the trackers, design docs, CLAUDE.md or memory. The 1093 with no match went to classifiers with the preceding Claude message as context; they copied the user's words, and the merge verified every quote is a verbatim substring of its source. The 243 non-tweak statements were then checked by meaning against the same records.

Tweaks found and set aside: 208. They stay inline in the tracker if wanted at all.

## Counts (non-tweak statements)

| status | count |
|---|---|
| missing | 25 |
| recorded-differently | 24 |
| recorded | 124 |
| dead | 66 |
| session-artifact | 4 |
| **all** | **243** |

## Missing (21)

Nothing in the records says this, in any wording. Grouped by the home the checker proposed; both the grouping and the inclusion are proposals.

### ground-truth (2)

| when | nature | governs | gist | user's words |
|---|---|---|---|---|
| 2026-08-13 | fact | rubble and boulders | You cannot share a cell with rubble or boulders | "that won't work for rubble or boulders since you can't share a space with them" |
| 2026-08-16 | fact | faction join flag | join=false means members won't join the player, not vice versa | "so join=false actually means *they* won't join you, rather than you not being able to join them" |

### guidelines/warrens (3)

| when | nature | governs | gist | user's words |
|---|---|---|---|---|
| 2026-08-19 | guideline | warren rooms | Fully define each room before building; no rooms hidden behind rare size gates | "can we make sure we're locking down what each room is fully before building this? don't wanna get surprised because some are locked behind rare size requirements" |
| 2026-08-22 | guideline | warren procgen | Draw on the Broken Earth series for warren procgen ideas | "i'm thinking about if there's anything we could utilize from that for our warren procgen" |
| 2026-08-23 | guideline | terra firma 2 | Warrens are the biggest meat of Terra Firma 2; will take tweaking | "it definitely is gonna take a bit of tweaking. this is the biggest meat of terra firma 2 imo" |

### guidelines/text (1)

| when | nature | governs | gist | user's words |
|---|---|---|---|---|
| 2026-08-24 | guideline | quarrier voice | Capture diaspora/Sunderlies element: Qud subtly as their true home | "the thing i want to capture more is like the diaspora/sunderlies element. like, this subtle element of qud being their true homem" |

### guidelines/workflow (1)

| when | nature | governs | gist | user's words |
|---|---|---|---|---|
| 2026-08-19 | guideline | brainstorm ideas | Creature and item ideas must tie into real Qud mechanics | "make sure they actually tie into qud mechanics" |

### guidelines/mutations (3)

| when | nature | governs | gist | user's words |
|---|---|---|---|---|
| 2026-07-21 | guideline | gorgon's gaze | Physical Gorgon's Gaze needs to be stronger than Ego-scaling Sunder Mind | "as a mental mutation, sunder mind scales naturally with a player's ego stat. while gorgon's gaze is a physical mutation, so it will need to be a bit stronger to compensate" |
| 2026-08-04 | guideline | mutation kit | gap vs Atlas: no secondary utility for the rocks you make | "the biggest thing his kit has that this doesn't is a secondary utility for all the rocks you're making" |
| 2026-08-04 | guideline | mutation roster | role map: sense/CC+protection/hard protection/allies/blasting/petrify | "sense - geomancy fissure - crowd control aoe + protection calcify - hard protection/control stoneshaper - making allies to defend you earthen barrage - blasting chunks of earth at your foes gorgon's gaze - petrifying foes into statues" |

### decisions (2)

| when | nature | governs | gist | user's words |
|---|---|---|---|---|
| 2026-08-12 | decision | stonecarver roster | Cut the lamp-carrier role if it creates tension | "hmmm... we could just cut it if there's tension, it's not a huge deal" |
| 2026-08-22 | decision | carver families | Families are flavor data, not engine factions (simmering) | "yeag, i lean flavor data families as well. just simmering for now" |

### tracker-inline (9)

| when | nature | governs | gist | user's words |
|---|---|---|---|---|
| 2026-07-22 | fact | stone blueprints | Existing stone blueprints were cleared with the commissioned author; Claude may edit XML | "you can do the XML if you want, it's not that big of a deal - it'd just be modifying the existing ones we already have that have been cleared with the person i commissioned this from" |
| 2026-07-22 | decision | clutter stone | Delete the clutter stone | "you can delete the clutter stone" |
| 2026-08-17 | decision | sifrah | Sifrah won't be relevant much longer; leave it out of the analysis | "take sifrah out of the equation here since it won't be relevant much longer" |
| 2026-08-21 | decision | item focus | Focus on scaffold frame rather than unwritten armor | "actually rather than the unwritten armor, let's hone in on scaffold frame" |
| 2026-08-22 | decision | carver grudges | Mild inter-family grudges via quests/dialogue, tied to legendary naming (simmering) | "maybe I should make the stonecarvers have like, mild grudges against each other. I was thinking about maybe having quests but that could be a good way to like, illustrate that background. it could also tie into legendary naming where certain families dislike other families maybe?" |
| 2026-08-23 | decision | branch process | Merge the 1.2.2 changes into the new-items branch | "we should take 1.2.2's changes into our new items branch i think?" |
| 2026-08-25 | decision | shipped Textures | .aseprite source (stoneandbees) moved out of the shipped dev folder | "moved stoneandbees out of the dev folder" |
| 2026-08-26 | decision | docket artifact | Squatters belong to warren tab, provisioner to creatures tab | "squatters can go to warren, provisioner can go to creatures" |
| 2026-08-26 | decision | credits | Credits go in the Steam forum, not GitHub/manifest | "nah it's fine, no one should care about the github for this, credits in the steam forum should do" |

## Filed after the user's look

- 2026-09-03: "read the books and get familiar with the lore and tone of the game as closely as possible before suggesting anything" (2026-07-23) filed into `design/guidelines/text.md`. User: "reading the lore before proposing things are great".

## Culled by the user

- 2026-09-03: "quarrier dialogue uses elements of the broken earth trilogy" (2026-08-23) dropped. User: "the quarrier dialogue was just an experiment, not really a strict guideline".
- 2026-09-03: "procgen produces something that feels or looks like the drawings, without trying to necessarily be the drawing" (2026-08-18) dropped. User: "procgen about drawing is pretty vague".
- 2026-09-03: "qud tiles need to face a certain direction" dropped from ground-truth. User: "mostly irrelevant for you and also kind of vague".

## Recorded differently (24)

The records hold a later, conflicting or narrower version. Worth a look in case the record is the one that is wrong.

| when | governs | gist | user's words | where | note |
|---|---|---|---|---|---|
| 2026-07-22 | process | Claude lists options; user picks or counter-proposes | "i think we should list out a bunch of possible things we could do for channel fragility and then i pick from the list, or suggest something else" | CoQ_TerraFirma-dev/TODO.md:2548 | only as the candidate-slate/naming-session idiom, not a general process rule |
| 2026-07-22 | deploy layout | One new mod folder merging both versions' architecture plus the day's changes | "i'd like you to set up a new mod folder in the game directory that has all the architecture from both versions already in there, plus all the changes we've done today" | CLAUDE.md (Deploy layout); CoQ_TerraFirma-dev/TODO-archive.md:871 | superseded: one live dev copy Mods\TerraFirma plus TerraFirmaBeta staging |
| 2026-07-22 | throw visor | Visor binned to pick up later | "let's bin visor as a "to pick up later on" thing, since it's not naturally coming together." | CoQ_TerraFirma-dev/TODO-archive.md:1050 | bin later folded into a formal CUT 2026-08-13 |
| 2026-07-22 | baboons | Baboons spawn with 1-2 fissure stones (roll-up tiering) and re-fetch thrown stones | "1) baboons should sometimes spawn with fissure stones, using the same rolling up tier effect the mutation uses. i imagine like, 1 stone from fissure in each baboon's inventory, maybe 2, would be nice 2) would it be possible to make baboons try and pick up stones they've thrown that are nearby, re equip them, and ready them to throw again? so that they can keep engaging in thrown combat rather than swapping to melee only once their stones are exhausted." | CoQ_TerraFirma-dev/TODO.md:545 | fissure-stone spawn dead; re-fetch lives as cobble baboon active seek 08-27 |
| 2026-07-22 | carver roster | Legendary overseer draws from the roster-three mutation pool | "roster 3 pool is fine" | CoQ_TerraFirma-dev/TODO.md:2568 | pool boundary superseded 08-15 by the epithet bank |
| 2026-07-24 | whole mod | Mod is not public; save compatibility is not a concern | "the mod is not out publicly so this isn't a concern." | CoQ_TerraFirma-dev/TODO-archive.md:874 | superseded once the beta shipped: save compat became a real concern 2026-08-07 |
| 2026-07-25 | canyon zones | canyon zones are full of thick large stone walls covering huge chunks | "the canyon is an area full of walls, thick large stone ones that cover huge chunks of the zone" | CoQ_TerraFirma-dev/TODO.md:952 | later census: real canyon zones run far more open than assumed |
| 2026-07-25 | camp rooms | five camp room concepts: communal, sty, overseer tent, workshop, stone blocks | "1) communal area with stove/campfire, hookah with cushions, rubble/half finished projects 2) feeding sty for mole creature, has hay mixed with worms and bugs, water trough/natural drinking spout 3) overseer tent with map of region, chest, statues 4) open air workshop where stone is carved 5) blocks of stone quarried and set aside" | design/recipes.md | communal (hookah) and stone blocks (quarry-yard) encoded; sty/tent/workshop became v2 roles |
| 2026-08-06 | fissure | Fissure valued as a wall-probing, path-carving tool through caves | "with fissure, i was able to probe 1 wide walls and carve out my own path through the caves" | CoQ_TerraFirma-dev/TODO-archive.md:900 | carve-as-attack mechanic recorded; the path-carving valuation is not |
| 2026-08-06 | deploy process | Deploys stay dev-only; beta ships all fixes at once | "it shouldn't be, let's just keep it dev for now and ship beta all at once" | CLAUDE.md:55 | dev/beta deploy separation is standing; ship-all-at-once timing not recorded |
| 2026-08-07 | boar vessel name | The name is a portmanteau of "hus" and "skeuos" | "also the name is in this case a portmanteau of "hus" and "skeuos"" | CoQ_TerraFirma-dev/TODO-archive.md:929 | name replaced by Husangeion (hus + aggeion); Hûskévos etymology gone |
| 2026-08-07 | canyon sim | Base texture needs more variety: canyons past zone borders, active rivers | "i feel like more varied generation on the base texture is necessary potentially - variants for canyons that stretch beyond zone borders or have rivers that run through them actively" | CoQ_TerraFirma-dev/TODO.md:2484 | river reintegration parked then landed in v2; cross-border canyons not recorded |
| 2026-08-10 | earthen barrage | High-tier PV bonus and throw damage read too high | "i'm quite worried about the PV bonus and throw damage at higher tiers. it reads as quite high from what i'm seeing here" | CoQ_TerraFirma-dev/TODO.md:373 | resolved by Ladder F 2026-08-15; the 08-10 flag itself not quoted |
| 2026-08-12 | stonefrog | Lithofexes spawn with husks and hide among them (hedged) | "i *think* lithofexes spawn with a bunch of lithofex husks or something of that nature, and try to remain hidden within them until something happens?" | TODO.md:485 | record corrects it: husk-hiding pattern is the Molting Basilisk, not lithofex |
| 2026-08-12 | release versions | Mutations release is 0.1; creatures/items/canyons update is 1.0 | "oops, I meant 1.0, with mutations being 0.1" | TODO.md:36; TODO-archive.md:1094 | superseded 08-26: non-mutation content is Terra Firma 2, not base 1.0 |
| 2026-08-15 | 0.1.2 release | 0.1.2 contents: fissure rework, Str-scaled PV, stoneshaper nerfs, calcify ladder, canyon wayfaring | "terra firma beta 0.1.2 Balance fissure has been reworked: it now hits in a radius around the player 3 times, expanding it's ring each time it no longer deals flat damage, and instead tries to penetrate armor, scaling with strength earthen barrage now uses strength scaling for PV  stoneshaper no longer has to-hit bonuses, or vibro weapons copies of statues made with stoneshaper now have stat maluses that increase for each copy you make levelling calcify reduces cooldown by -5 instead of -10 every other level  QOL new vfx for earthen barrage automatically taking stones is more accurate (thanks euclid!) how many stones you have available with earthen barrage should be clearer (thanks euclid!) etching/stoneshaper carve animation and description should be slightly more accurate to the tile you're carving stoneshaper cast now snaps to walls after you select a creature stonecarvers now start with canyon wayfaring instead of ruins updated credits" | TODO-archive.md:1094 | ship list logged item by item; the user's patch-notes text not kept |
| 2026-08-17 | release scope | Tempted to pull calling, pet, baboon AI, etching and non-mutation blueprints at release | "when release happens, i would be tempted to pull the calling, pet, baboon ai behavior, and etching as a concept + any blueprints outside of those necessary for the mutation" | TODO.md:36 | superseded by the 08-26 full move: strip once at TF2 1.0 |
| 2026-08-17 | warren rooms | Room vocabulary: kitchen, provisioner dwelling, gallery, common, work, recreation, overseer quarters | "kitchen provisioner (plant) dwelling gallery common area work area recreational zones overseers quarters" | TODO.md:1663; TODO.md:2649 | vocabulary landed as 10 room pools (hearth/crafts/etc.), not this list |
| 2026-08-22 | stonecarver loot | Loot tables too generic; hone the edge, find slots wanting custom items | "the current ones are a bit too generic - not strictly against them, but I want to hone that edge a bit. but it's mainly to identify slots that might want a custom addition" | TODO.md:2680 | later ruling generalized MiningGear as loot table; the too-generic critique not logged |
| 2026-08-22 | furniture drafts | Tool rack and (corrected to) dolmen picked out of furniture drafts | "tool rack has potential. other than that, the nomon seemed interesting" | design/asset-debt-tracker.md:83 | tool rack logged LIKED; the dolmen pick absent |
| 2026-08-22 | item roster | Two new chestplates: flint-spiked doublet and mudskin carapace | "i want to sketch out two chestplates - the flint-spiked doublet, and the mudskin carapace" | TODO.md:151; TODO.md:159 | names superseded same day: flint-spike pauldrons, mudskin girdle |
| 2026-08-24 | warren popup | Lean: drop the investigate popup when a warren cannot be guaranteed | "do you think a safer option is to not advertise via the popup if we can't guarantee a warren will be there?" | TODO.md:922 | record accepts the refuse-on-arrival hole rather than dropping the popup |
| 2026-08-26 | docket artifact | Tracker reworked: per-branch tabs of what should be there; art/name/code/text tags; one box per item | "rework the tracker so it has like tabs at the top that are separated by branch, but rather than each branch containing what is actually on it, it has what *should* be on it. so box of chalk is on the items branch, creatures on creatures, and warren gets it own tab and then have tags for art, name, code, and text, based on what they need. then we go granular with each creature, item, etc getting its own box" | memory/part2-beta-target.md | artifact named as the derived view; tab/tag structure not described |
| 2026-08-30 | beta mutations | The beta ships six mutations, not seven | "there's only 6 mutations, so some assumptions are certainly happening," | TODO.md:34; TODO.md:2580 | tracker counts 7 mutations (Lithovore included); live set is 6 |

## Recorded, in paraphrase (124)

Already in the records without the user's exact words. Listed so the quote can be attached to the record as provenance if anyone wants it.

| when | governs | gist | where |
|---|---|---|---|
| 2026-07-20 | whole mod | Original mod was commissioned work; user holds full rights | memory/terra-firma-project.md:11 |
| 2026-07-21 | tracker | Keep a persistent list: broad bugs section plus one per mutation | CoQ_TerraFirma-dev/TODO.md:1 |
| 2026-07-21 | process | Talk through fixes individually before applying; no batch breezing | memory/terra-firma-project.md:19 |
| 2026-07-21 | nunit import | Cut the NUnit import; note it in case it must return | CoQ_TerraFirma-dev/TODO-archive.md:870 |
| 2026-07-22 | gorgon's gaze | Composite channel spec accepted provisionally, flagged for test review | CoQ_TerraFirma-dev/TODO-archive.md:14 |
| 2026-07-22 | balance method | Check numbers by simulating kills across creatures and levels | CoQ_TerraFirma-dev/TODO-archive.md:69 |
| 2026-07-22 | earthen barrage | EB package approved: shield effect, Willpower regen, restyled description | CoQ_TerraFirma-dev/TODO-archive.md:990 |
| 2026-07-22 | earthen barrage | Leans option 1 (in-mutation vibro/attune) not whetstone item; rename sharp/heavy | CoQ_TerraFirma-dev/TODO-archive.md:993 |
| 2026-07-22 | stoneshaper | Statues get taunting | CoQ_TerraFirma-dev/TODO-archive.md:1023 |
| 2026-07-22 | stoneshaper | Level 10 permanence stays as capstone exception | CoQ_TerraFirma-dev/TODO.md:429 |
| 2026-07-22 | stoneshaper, gorgon's gaze | Deanimated/petrified creature inventory dropped or statue becomes lootable; shared fix | CoQ_TerraFirma-dev/TODO-archive.md:1027 |
| 2026-07-22 | calcify | Cut whorling stones from Calcify entirely | CoQ_TerraFirma-dev/TODO-archive.md:1035 |
| 2026-07-22 | calcify | Calcify on par with Force Wall, slightly stronger for the investment | CoQ_TerraFirma-dev/TODO.md:453 |
| 2026-07-22 | calcify | Wall count: accept Claude's read on the pending ruling | CoQ_TerraFirma-dev/TODO.md:453 |
| 2026-07-22 | manifest credits | Manifest author field stays as is (Cleo, Ceres et al.) | memory/terra-firma-project.md:11 |
| 2026-07-22 | commits | No Co-Authored-By Claude trailer on commits | CLAUDE.md (Commits) + memory/no-claude-commit-trailer.md |
| 2026-07-22 | stoneshaper | Stoneshaper v3 locked as specced, including monument-trail expiry | CoQ_TerraFirma-dev/TODO-archive.md:90 |
| 2026-07-22 | gravel whorl | Whorl should not be a hotbar toggle; no reason to turn it off | CoQ_TerraFirma-dev/TODO-archive.md:63 |
| 2026-07-22 | gravel whorl | Whorl stays a status effect; no toggle, no garnish (buttons for decisions, statuses for states) | CoQ_TerraFirma-dev/TODO-archive.md:63 |
| 2026-07-22 | gravel whorl | Sliding scale of outcomes: eaten, deflected to adjacent cell, angled ricochet | CoQ_TerraFirma-dev/TODO.md:406 |
| 2026-07-22 | gravel whorl | Whorl decoupled from Take Stones toggle; always on with the mutation | CoQ_TerraFirma-dev/TODO-archive.md:63 |
| 2026-07-22 | side content | Creatures are designed collaboratively; Claude must not pre-write descriptions | CoQ_TerraFirma-dev/TODO-archive.md:575 |
| 2026-07-22 | throw items | Four throw-augmenting item concepts: sling, chalk, stabilizers, visor | CoQ_TerraFirma-dev/TODO-archive.md:464 |
| 2026-07-22 | stonecarver beast | Mole-beast fits the theme | CoQ_TerraFirma-dev/TODO.md:2534 |
| 2026-07-22 | carver roster | Three basic stonecarver types on Fissure, Stoneshaper, Earthen Barrage; random-mutation layer possible | CoQ_TerraFirma-dev/TODO.md:2567 |
| 2026-07-22 | carver roster | Thrower type carries the real EB mutation, not a mimic | CoQ_TerraFirma-dev/TODO.md:2577 |
| 2026-07-22 | stonecarver beast | No walk-in interior; burrowing ok but cuttable; burrowing claws | CoQ_TerraFirma-dev/TODO.md:2534 |
| 2026-07-22 | stonecarver beast | The mole must be weird in a Qud way, not just a big animal | CoQ_TerraFirma-dev/TODO.md:2534 |
| 2026-07-22 | gravel whorl | Whorl has three outcomes: eaten, swatted, inertia-preserving ricochet | CoQ_TerraFirma-dev/TODO.md:406 |
| 2026-07-22 | gravel whorl | Add resonant RefractLight (beam scatter) upgrade; rubber rocks next | CoQ_TerraFirma-dev/TODO.md:413 |
| 2026-07-24 | blueprint naming | Mechanical blueprint IDs with flavorful display names going forward | CoQ_TerraFirma-dev/TODO-archive.md:196 |
| 2026-07-24 | blueprint naming | All blueprints prefixed Cleo_Terrafirma_ to avoid collisions | CoQ_TerraFirma-dev/TODO-archive.md:196 |
| 2026-07-24 | gorgon's gaze | Accept Claude's items 1 and 2 of the gaze package | CoQ_TerraFirma-dev/TODO-archive.md:48 |
| 2026-07-24 | gorgon's gaze | Kill-rate cliff 50% at Lv1 to 97% at Lv4 is bad; want smooth scaling | CoQ_TerraFirma-dev/TODO-archive.md:48 |
| 2026-07-24 | gorgon's gaze | DC 11+L greenlit; mark-threshold and AV-flip riders rejected | CoQ_TerraFirma-dev/TODO-archive.md:48 |
| 2026-07-24 | stone lifetime as cooldown | lifetime-as-cooldown change flagged for later review for side effects | design/handoff-2026-07-24.md:23 |
| 2026-07-24 | mutation toggles | friend's method: option-gated HiddenMutations.xml vs Mutations.xml split | CoQ_TerraFirma-dev/TODO-archive.md:578 |
| 2026-07-25 | stonecarver camps | stonecarvers set up living quarters in quarried sites | design/guidelines/warrens.md |
| 2026-07-25 | camp NPC conversations | cut conversation scripts; NPCs use basic greetings and trade | design/decisions/2026-07-24-no-dialogue-trees.md |
| 2026-07-25 | stonecarver calling | calling is a mutant term, caste is True Kin; stonecarvers are mutants | memory/stonecarver-is-mutated-human.md |
| 2026-07-25 | text ownership | all mod text is draft until human sign-off; text debt weighs like sprite debt | design/guidelines/text.md:15 |
| 2026-07-25 | warren layout | hallways with divots dug out, possibly as a bunk area | CoQ_TerraFirma-dev/TODO.md:2596 |
| 2026-07-25 | warren layout | divot hall becomes connective tunnel between carved nodes; added to list | CoQ_TerraFirma-dev/TODO.md:2596 |
| 2026-07-25 | painter testing | wish suite to walk painter permutations before sprites exist | CoQ_TerraFirma-dev/TODO.md:2597 |
| 2026-07-26 | mutation descriptions | descriptions should line up with in-house ones; this writing section could be automated | design/mutation-voice-comparison.html:137 |
| 2026-07-26 | level text conventions | flat constants: do whatever is most base-game accurate | design/mutation-voice-audit-2026-08-06.md:15 |
| 2026-07-26 | painter workflow | tile sketch tool using game tiles informs the C# painter | memory/sim-first-painter-workflow.md |
| 2026-07-27 | shipped text | hard rule: no em dashes | CLAUDE.md:23 |
| 2026-07-28 | genome preview mod | scope is in-game seed only, not character creation | memory/mutation-preview-scoping.md |
| 2026-07-28 | genome preview mod | dedicated popup copying the mutation screen | memory/mutation-preview-scoping.md:15 |
| 2026-07-28 | genome preview mod | nectar diff parked; route search for a target mutation wanted | memory/mutation-preview-scoping.md:23 |
| 2026-08-02 | post-beta ideas | jot down the three ideas (shotgun, puller, armor) | design/gearset-draft-2026-08-04.md:29 |
| 2026-08-02 | fissure v1 earmark | earmark old fissure/lithovore/stone table work for reference | CoQ_TerraFirma-dev/TODO.md:118 |
| 2026-08-05 | release phases | phase 1 is mutations only, friends beta this week | CoQ_TerraFirma-dev/TODO-archive.md:885 |
| 2026-08-05 | mutation toggles | build per-mutation option toggles | CoQ_TerraFirma-dev/TODO-archive.md:905 |
| 2026-08-05 | mutation descriptions | compare against base descriptions to match tone and feeling | design/guidelines/text.md:7 |
| 2026-08-05 | friends beta build | strip most code comments from the published beta | CoQ_TerraFirma-dev/TODO-archive.md:878 |
| 2026-08-06 | NPC mutation AI | Add NPC advertisement before beta; legendaries may generate with the mutations | CoQ_TerraFirma-dev/TODO-archive.md:907 |
| 2026-08-06 | NPC mutation AI | All mutations get NPC AI so friends and allies can use them | CoQ_TerraFirma-dev/TODO-archive.md:907 |
| 2026-08-06 | text audit | Audit items 1 and 3 (pebble pair lines) were written by the user | memory/text-voice-and-ownership.md:24 |
| 2026-08-06 | beta feel session | Feel test passed; everything feels right, Fissure the standout | design/beta-index-2026-08-06.md:143 |
| 2026-08-06 | beta distribution | Beta distributed as GitHub release or unlisted Steam Workshop page | CoQ_TerraFirma-dev/TODO.md:113 |
| 2026-08-06 | beta distribution | Go with unlisted Workshop upload staged as TerraFirmaBeta id | CoQ_TerraFirma-dev/TODO.md:113 |
| 2026-08-06 | tracker | Split TODO.md into live file plus TODO-archive.md | memory/terra-firma-project.md:13 |
| 2026-08-06 | mutation rebalance | Rebalance agent must read all mutation scaling and numbers first | design/base-mutation-numbers-2026-08-06.md:1 |
| 2026-08-06 | mutation rebalance | Rebalance must also read all base game mutations | design/base-mutation-numbers-2026-08-06.md:1 |
| 2026-08-07 | boar vessel | Do not write a recall story; the base text should already exist | CoQ_TerraFirma-dev/TODO.md:142 |
| 2026-08-07 | warren painter | Two phases: carve spaces first, decorate as a second step | CoQ_TerraFirma-dev/TODO.md:2597 |
| 2026-08-07 | cave procgen | Pull from Qud's own procgen before external research | design/cavegen-survey-2026-08-07.md:4 |
| 2026-08-07 | warren lab | Reuse the decoration editor to sim Qud worldgen and demo generations before in-game | CoQ_TerraFirma-dev/TODO.md:2599 |
| 2026-08-08 | warren generation | Carved sections must feel fully articulated before filling them | CoQ_TerraFirma-dev/TODO.md:2653 |
| 2026-08-08 | terminology | "Painted" means decorated or having items inside | CoQ_TerraFirma-dev/TODO.md:2613 |
| 2026-08-08 | warren lab v2 | v2 focuses on painting a good warren and an accurate Qud canyon sim | CoQ_TerraFirma-dev/TODO.md:2613 |
| 2026-08-08 | warren lab v2 | Gallery of hallway control elements: purpose, contribution, comparable for overlap | design/next-session-warren-lab-prompt.md:32 |
| 2026-08-08 | warren lab tiles | Extracted Qud wall sprites used in the lab (gitignored) | CoQ_TerraFirma-dev/TODO.md:2487 |
| 2026-08-08 | fissure | Rework Fissure from one-hit burst to working in waves | design/fissure-pulse-rework-2026-08-08.md |
| 2026-08-11 | creature roster | Burrowbee only fits via Burrowing Claws; fit doubted | CoQ_TerraFirma-dev/TODO.md:2719 |
| 2026-08-11 | boar vessel | Boar vessels sometimes spawn in farmer population tables | CoQ_TerraFirma-dev/TODO.md:2745 |
| 2026-08-11 | boar vessel | Gut cistern slowly purifies salty water, giving a reason to farm them | TODO.md:2745; TODO.md:2760 |
| 2026-08-11 | creature roster | Cut geysers; build boar vessel base first, Husangeion inherits from it | TODO.md:2715; TODO.md:2745 |
| 2026-08-11 | tracker | TODO trim pass approved; quarantined material gets its own file | design/decisions/2026-08-11-design-quarantine.md |
| 2026-08-11 | stonecarver roster | Compress stonecarvers to a few creatures; flesh out camp concepts | design/decisions/2026-07-22-carver-roster.md; TODO.md:2544 |
| 2026-08-11 | vine merchant placement | Bank both placements: wall face (wet-shell preferred) and dead-end chamber | TODO.md:2763 |
| 2026-08-12 | stonefrog | Basilisk-style hiding in litter recorded as the stonefrog depth option | TODO.md:488; TODO.md:489 |
| 2026-08-12 | credits | No Paige credit line in the mod | TODO.md:84 |
| 2026-08-12 | warren painter | Move on to decorating passes | TODO.md:2632 |
| 2026-08-13 | beta scope | Whorl VFX, pickup re-arm and TODO edits are beta-bound; commit them | TODO.md:356; TODO.md:96 |
| 2026-08-13 | geomancy scan | In-game: exact wall HP visible under the geomancy scan wish | TODO.md:443 |
| 2026-08-13 | base naming | In base, quartzfur names the items and quartz the baboon | TODO.md:2733 |
| 2026-08-13 | baboon and items | Creature is cobble or gravel baboon; -fur items come via baboon butcherables | TODO.md:2733 |
| 2026-08-13 | naming | Wary of the same word appearing twice in the same place | TODO.md:2733; TODO-archive.md:1197 |
| 2026-08-13 | cobblefur bracer | Re-examine bracer buff; it is invisible with a throw skill unless four equipped | TODO.md:498 |
| 2026-08-14 | carver naming | Bespoke Naming.xml built from Etruscan name pieces at mysteriousetruscans.com | TODO.md:2504 |
| 2026-08-14 | carver bynames | Bynames: material word plus craft suffix (cobblesmith, gravelaugur); lower two-name chance | TODO.md:2504; TODO.md:2519 |
| 2026-08-14 | carver naming | Scrub prefix/postfix banks before shipping; polished and base-game accurate over variety | TODO.md:2504 |
| 2026-08-15 | overseer mutations | Every pack mutation plus a few base ones rollable; line it up like goatfolk heroes | TODO.md:2520 |
| 2026-08-15 | overseer builder | Bank goatfolk-pattern builder: name drives kit, byname bank as manifest | TODO.md:2520 |
| 2026-08-15 | quarrier faction | Faction is post-sultanate; name is stonecarver's guild or quarrier's guild | TODO-archive.md:1197 |
| 2026-08-16 | text pass | Release text pass: stonecarver descriptions, dialogue, new creature descriptions | design/writing-sittings-1.0.md; TODO-archive.md:1197 |
| 2026-08-16 | carver dialogue | Player address term is a 'lack salt' equivalent: not implicitly trusted | TODO.md:550 |
| 2026-08-17 | geomancy scanning | Object-side temporary health reveal currently requires Harmony | TODO-archive.md:122; TODO.md:443 |
| 2026-08-17 | warren payload | Each site has 0-2 things worth engaging with: recipe, rep, trade, history, loot | TODO.md:649 |
| 2026-08-18 | warren primitives | Two primitives: organ block (rooms off a sweep) and artery (path through rooms) | TODO.md:721 |
| 2026-08-18 | design drawings | Drawing palette: white wall, grey floor, black background | TODO.md:713 |
| 2026-08-18 | arteries | Arteries should wind rather than run straight and long | TODO.md:1474 |
| 2026-08-18 | warren carve | Port TunnelTo weights and EnsureAllVoidsConnected; rind becomes a cost, not a wall | TODO.md:796; TODO.md:800 |
| 2026-08-18 | aquifers | Breached veins are content the dig can hit, for variety | TODO.md:952; TODO.md:1833 |
| 2026-08-18 | warren frequency | Warren spawn rate should be a sensible analogue of base dynamic canyon spawns | TODO.md:856 |
| 2026-08-20 | warren leisure | Leisure gating must not leave working sites uninteresting | TODO.md:1201 |
| 2026-08-22 | granite baboon | Cobble baboon gets a second design pass | TODO.md:2721 |
| 2026-08-22 | outrider population | Outriders are the main chestplate users; lock their population table first | TODO.md:570 |
| 2026-08-23 | warren v2 port | Begin the first real port of the new warren systems into the game | TODO.md:940; TODO.md:954 |
| 2026-08-23 | warren siting | Loosen site-rejection rules; quarries must spawn in zones with enough rock | TODO.md:952 |
| 2026-08-23 | animated figurines | Figurines reused elsewhere (spawner enemy, grenade, kit, weapon); mutation likely out | TODO.md:2684 |
| 2026-08-25 | beta hotfix | Branch off the mutations-only beta to fix the take-stones edge case | TODO.md:79; TODO.md:357 |
| 2026-08-25 | beta hotfix | Hotfix publishes from its own branch with no second-pass residue | TODO.md:79 |
| 2026-08-25 | warren maze corridors | Maze logic not ported off the A/B; confidence too low | TODO.md:1478 |
| 2026-08-25 | warren roadmap | Multilevel warrens added to the list: on the docket, not planned | TODO.md:924 |
| 2026-08-25 | warren yard routing | Build the yard/trail route fallback (sim first, port same sitting) | TODO.md:2262 |
| 2026-08-25 | warren yards | Port the inside-yard area-compare into WarrenCarver | TODO.md:2216 |
| 2026-08-26 | gut cistern | Drop NaturalEquipment; cistern ships as plain removable Body-slot gear | TODO.md:2751 |
| 2026-08-27 | pig vessel | Pig vessel spawns dynamically as a farm animal | TODO.md:2760; TODO.md:2745 |
| 2026-08-28 | text passes | Pronoun pass uses base-style pronoun tokens, not hardcoded he/she/it | TODO.md:211 |
| 2026-08-29 | warren mill | Retract figurine-table no-electricity premise; mill can be a grid producer | TODO.md:280 |

## Dead under the 2026-09-02 fissure ruling (66)

aegischist, base game UI, chasm rock, cooking rocks, cooking-table stones, deep four rocks, drainstone, filler rocks, fissure, fissure pits, fissure rocks, fissure tables, gas belly effect name, geyser, gossamarl, lithovore, lithovore cooking, mill law, pebble variants, pivot fallout, process, rock catalog, rock catalog evaluation, rock fragility, rock names, rock naming, rock payloads, spacetime ingredient, spitting geyser, stone creatures, stone discharge law, stone milling, strange rock creature, tanglelith rename, text pass process, village meals, vitriolite crack, weak rock 1, weak rock 4

