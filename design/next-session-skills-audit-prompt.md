# Next-session prompt: skills audit + set proposal (written 2026-08-04)

Paste or point a fresh agent here to run a skills audit for the Terra Firma Caves of Qud
mod (repo: `C:\Users\venob\terra firma revival\CoQ_TerraFirma-dev`).

## Non-negotiable working norms

- **TODO.md is the source of truth.** Read its REDOUBT PIVOT entry (friends-beta section)
  before proposing anything. Every design-shaping statement carries a provenance tag:
  RULED (user's words, dated) / LEAN (user's, not a ruling) / CLAUDE PROPOSAL, UNRULED.
  Cite rules only by finding their RULED marker. Delegated code needs no ceremony.
- The Stonecarver calling is a **Mutated Human** calling, NOT True Kin.
- **No Co-Authored-By trailer on commits** (RULED).
- **Text is the user's domain**: descriptions, names, lore. Draft freely, mark drafts,
  never sign off text yourself. **No em dashes in shipped text.**
- The user rules on design one item at a time; propose, don't harden proposals into rules.
- This is a DESIGN session: no code changes unless the user asks. Deliverable is a doc.

## The assignment (user's words, 2026-08-04)

"do an audit of skills, and examine the mechanics and synergies of terra firma, and see
what they come up with as far as a set"

Deliberately open. Two readings; develop BOTH unless the user narrows it at session start:

1. **A curated base-game skill set** — which base skills a Terra Firma / Stonecarver
   build actually wants, and whether the calling's granted-skill list should change.
   Current grant (Callings.xml): Cudgel, ShortBlades, Customs, Discipline,
   Survival_RuinsSurvival, plus +2 Willpower.
2. **A bespoke mod skill set/tree** — new skills as a home for earth-manipulation verbs
   that shouldn't cost mutation points.

Everything you produce is CLAUDE PROPOSAL, UNRULED, until the user says otherwise.

## Audit scope

- Read the base game's skills data directly — find Skills.xml under the game install
  (`C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\StreamingAssets\Base\`).
  Verify against the XML; do not trust model memory of Qud's skill trees.
- Catalog which trees/powers touch Terra Firma's verbs. Known contact points to chase:
  - **Thrown weapons.** Earthen Barrage stones occupy the thrown slot; Geomancy gives
    +2 thrown range and sonar-on-impact. Find what actually governs thrown attacks and
    whether any skills feed them.
  - **Cudgel** (calling grant) — melee identity next to a mostly-caster kit.
  - **Self-discipline / Willpower** — calling grants +2 WIL; Gorgon's Gaze channels by
    passing turns; check Meditate-adjacent powers against the stand-ground identity.
  - **Tactics / Acrobatics / Shield** — DV-and-positioning skills; note where they
    FIGHT the kit (mobility skills vs a planted-feet caster) as well as where they help.
  - **Endurance, Wayfaring, Customs** — survivability and the existing grants.
- Map each of the six mutations' mechanics to skills that amplify or conflict. The kit
  (one-liners): Geomancy = thrown-stone sonar sense; Calcify = anchored wall line;
  Earthen Barrage = sculpted thrown-rock reserve; Stoneshaper = animate a statue ally
  from creature+wall; Gorgon's Gaze = channeled staged petrification; Fissure (redoubt) =
  center-out wave, push+stun, persistent broken-stone ring. Full state: TODO.md and
  `design/next-session-fissure-prompt.md`.

## Jam record 2026-08-04 (context this prompt exists to carry — not yet in the jam doc)

Session was a design jam on earth-manipulation kits (Warframe's Atlas as main reference).
Provenance-tagged outcomes that bound your design space:

- **Soft-synergy frame (USER, 2026-08-04):** the mod's synergies are deliberately SOFT —
  "making cover, throwing things" — and the user accepted **standing still / holding
  ground** as "a cool potential add to the list of soft synergies," explicitly as a soft
  synergy, NOT a hard mechanic. Claude's supporting read (unruled): five of six mutations
  are defense/control; Gorgon's channel, Fissure's self-centered ring, and Calcify's
  anchoring all reward a planted-feet playstyle. A skill set that leans stand-your-ground
  fits; one that demands kiting fights the kit.
- **No Calcify rider for now (USER, 2026-08-04):** "too many of the mutations in my mod
  already have one." Rider-density is a live concern — don't propose new riders casually.
- **Seventh mutation (USER, 2026-08-04):** "not a strict no, it's just more debt down the
  line." Prefer homes that aren't new mutations.
- **Active stone form (USER HUNCH, 2026-08-04, not a ruling):** as a skill "my hunch is
  no"; as gear "potentially." If the audit surfaces evidence for the skill reading (e.g.
  a base-game precedent for transformation-like skill powers), present it — the hunch is
  re-openable with evidence, but the burden of proof is yours.
- **Socket armor = the feedable breastplate (USER, 2026-08-04):** chest armor "with a
  hollow maw in the center that can fit a boulder, but that rock will steadily lose
  health as you're attacked, until you need to find a new one." Merges with jam-doc item
  #2 (rock-socket armor). This lives in the GEARSET, not skills — don't re-home it.
- **Pebble volume-sink (USER, 2026-08-04):** temporary AV from crushing lots of Fissure
  pebbles is "tantalizing but maybe just bait." Claude analysis (unruled): pickup-based
  volume sinks contradict the planted-feet identity in a turn-based game (collection
  costs turns and movement). Alternatives sketched, both unruled: an omnivorous maw
  (pebbles as top-off fuel for the socketed boulder) or density-where-it-lies (loose
  rock in adjacent tiles passively protects). Nothing ruled; nothing built.
- **The gearset direction (USER LEANS, 2026-08-02, in `design/jam-2026-08-02-rock-items.md`):**
  gloves (catch), boots (wall-kick), puller weapon, socket armor. New verbs have been
  landing gear-side, deliberately. A skills proposal must justify why a verb belongs in
  a skill instead of the gearset or a mutation — "where does this verb live" is THE
  recurring fork of this design phase.

## Deliverable

Write `design/skills-audit-2026-XX-XX.md` (today's date) containing:

1. The base-skill audit: what exists, verified against game XML, filtered to what touches
   Terra Firma at all (a full dump of every skill is noise; the contact map is the value).
2. Synergy findings: amplifiers, anti-synergies, and anything surprising (best finds are
   interactions nobody designed — e.g. what happens between existing skill powers and
   walls/rocks/statues the mod creates).
3. The proposed set(s), both readings, every design-shaping line provenance-tagged.
   For reading 1: a concrete revised calling grant list + a "buy order" for a Stonecarver
   run, with reasoning. For reading 2: named skills with costs, prerequisites, and which
   jam-thread verbs (if any) they'd absorb — and an honest assessment of whether a skill
   tree is worth the debt at all, given the gearset absorbs verbs cheaply.
4. Open questions for the user, phrased as forks they can rule on one at a time.

Ask the user which reading they care about first; if no preference, audit first (reading
1 grounds reading 2). Present findings before writing the proposal doc if the session is
interactive.
