# Mutation voice audit — 2026-08-06 (beta close-out)

**Status: CLAUDE ANALYSIS + PROPOSALS, awaiting rulings.** Requested by the user in place of a
straight signoff on the two drafted Fissure lines: "do an audit of all mutation language, and make
sure you're getting as close as possible to the base game voice."

Basis: the 2026-07-26 voice sitting's recorded conventions (`design/mutation-voice-comparison.html`)
re-checked against fresh decomp samples (Stunning Force, Force Wall, Force Bubble, Flaming Ray,
Burrowing Claws). The conventions held:

- Flavor line: one declarative "You/Your" sentence, 4–8 words home register, no numbers for leveled
  mutations; non-leveled mutations append mechanic lines after a blank line.
- Level text: mechanism sentence(s) first (subjectless third person "Creates…/Emits…" or "You
  may/can…"), then one-fact-per-line stat labels, **Cooldown last**; `{{rules|…}}` wraps scaling
  values only, flat constants bare; ~zero semicolons in the base corpus; no editorializing.

## Verdicts

| Mutation | Status | Notes |
|---|---|---|
| Gorgon's Gaze | **GREEN** | Signed off 2026-07-27; VFX pass added no mechanical claims; every text claim re-checked accurate. |
| Earthen Barrage | **GREEN** | Signed off; whorl-DV line stays accurate by the kept-as-shipped ruling; crumble line rides live constants. |
| Calcify | **GREEN** | Signed off; shape matches Force Wall exactly (mechanism first, HP wrapped, cooldown bare). Post-signoff changes were visual only. |
| Stoneshaper | **GREEN** | Signed off; wall-type wrap is legitimately scaling; carve reveal changed no text claim. |
| Geomancy | **GREEN** | Signed off; non-leveled Carnivorous pattern; +2 range claim verified. |
| Lithovore | **FINAL by ruling** (2026-07-27), beta-held from chargen — out of scope. |
| **Fissure** | **THE ONE DRAFT — five findings below.** Text is 08-02 redoubt draft; predates carve-as-attack. |

## Fissure findings (F1–F5)

**F1 — Wrapped flat constant.** `GetCooldown` is a flat 100 (`=> 100`), but the text emits
`Cooldown: {{rules|100}} rounds`. The sitting's rule (and FlamingRay's "Cooldown: 10 rounds"): flat
constants go bare. *(If the feel session re-dials cooldown to scale by level, the wrap returns —
this finding is about matching the code as it stands.)*

**F2 — Semicolons.** The live stun line AND both driver-prompt replacement drafts carry semicolons;
the base corpus has exactly one across 131 mutations. Both proposals below use period splits.

**F3 — Carve accuracy (the known flag).** "The wave carves through N squares" states the carve as
automatic; carve-as-attack (RULED 2026-08-04) makes it a penetration roll vs wall AV with leftovers.
"Up to" + a resist sentence fixes it.

**F4 — No mechanism sentence; permanence as an orphan line.** Base activated mutations open level
text with a mechanism sentence (Force Wall: "Creates a wall of force…"; Flaming Ray: "Emits a…").
Fissure's opens on a stat label, and "The ring of stone persists." floats free — base folds
permanence into the mechanism as an adjective (Calcify's own signed text: "permanent walls").

**F5 — Description length + accuracy.** 19 words (home register 4–8; earned tail exists but is
spectacle-earned) and "hurls your surroundings outward" implies loot moves — the loot rule says
items stay put; only creatures push.

## Proposed Fissure text (DRAFT, awaiting ruling/re-voice)

**Description — three options:**

- (A) `You drive a violent wave through the earth.` *(8 words, home register)*
- (B) `You send a wave through the earth, raising a ring of broken stone.` *(13 — carries the
  ring identity; recommended)*
- (C) Keep the current long line but fix accuracy: `You drive a wave through the earth that hurls
  creatures outward and raises a ring of broken stone.`

**Level text (full block, mechanism-first per Force Wall):**

```
Drives a wave through the earth around you, leaving a permanent ring of broken stone where it dissipates.
Ring radius: {{rules|N}} squares
Damage: {{rules|1d3+1}}
Creatures are pushed outward. Those moved must make a Toughness save or be stunned for {{rules|N}} rounds.
The wave batters natural stone walls in its path, tearing through up to {{rules|N}} squares. The hardest stone withstands it.
Cooldown: 100 rounds
```

Line notes: "Ring radius:" is a new label but fixed-vocab-shaped (cf. "Cone length:"); "Damage:"
is the FlamingRay label and the wrap is correct (the die scales by band); the resist sentence has a
flatter alternative — `Harder stone resists.` — if "the hardest stone withstands it" reads too
flavored for rules text; ruled silences stay silent (flyer exemption, loot, gap ladder, ring-density
credits, salvage/geyser rolls).

**Deletion:** "The ring of stone persists." (folded into the mechanism sentence as "permanent…ring").
