# Asset-debt tracker — quarriers / camps / side content

Created 2026-07-24 at user request: **inventory every planned blueprint and the sprite + text
debt it incurs, BEFORE building anything.** TODO.md stays the ruling record; this file is the
design/planning ledger (leanings, ideas, reuse notes — the forward-looking half).

**As of 2026-07-25, `asset-debt-tracker.html` is GENERATED GROUND TRUTH, not hand-maintained**
(user: the tool must be "accurate and informative rather than having its head in the clouds").
It's produced by `generate-asset-report.ps1` scanning the actual mod files: sprites on disk,
what references them, missing/unwired, source↔deploy drift, Cleo_* blueprint inventory, plus
the hand-curated `rooms.json` manifest. Rerun the script to refresh; never edit the HTML.
The §7 sprite tick-list below is SUPERSEDED — a sprite is "done" when the file exists and the
report shows it wired; no ticking required.

**Text policy (user ruling 2026-07-24):** every string Claude produces is a DRAFT. Text debt
clears at **human sign-off** (rewrite or approval), not at generation — generating text moves a
row from "owed" to "staged," never to "done." Text debt weighs the same as sprite debt.

**Status:** `RULED` committed · `LIKED` user-endorsed, not formally ruled · `IDEA` on the table ·
`CUT` removed (kept for the record)
**Sprite:** `NEW` needs bespoke tile · `VAR` variant/recolor of an existing tile · `REUSE` existing
base-game or mod tile as-is · `CHECK` verify a base-game object/tile exists to reuse · `—` no sprite
**Text:** `desc` look-description · `convo` conversation lines · `table` generated-text table
(litany/spice) · `—` none

## Summary (hand-maintained — update when rows change)

| | count |
|---|---|
| Bespoke NEW tiles | ~11 (+5 VAR) |
| Tiles pending a reuse CHECK | ~4 (hay · dung · tent · fence — likely in Walls/Items files, not Furniture) — **Furniture.xml sweep 2026-07-24 confirmed: `Hookah`, `Floor Cushion`, `Bench`/`Ornate Bench`, `Millstone`, `Wine Pitcher` (+Merchant variants), `Brazier`/`Campfire`, `Village Monument Stele`, `Statue`/`Random Stone Statue` family, `FossilizedRemains` chest, `Urn` family, `Catchbasin`, `Woven Basket`** |
| Descriptions owed | ~24 |
| Greeting lines owed | 6 (one per profession — basic greeting + "let's trade" option, base-game minor-NPC idiom; RULED 2026-07-24, no bespoke dialogue trees) |
| Generated-text tables owed | 3 (stele litany · overseer-statue litany · musing-stone markov/spice) |
| Big text pieces owed | faction display text · anchor book · quarry chants · HistorySpice entries (Pass 3 REQUIRED) |

---

## 1. NPC roster (all RULED 2026-07-22)

**SPRITE DEBT COLLAPSED 2026-08-11 (user: "the two sprites for them are done and can be thrown into the game whenever"):** under the compressed faction shape (TODO.md tile-condensing entry + banked color map, same day), the roster runs on TWO base sprites + DetailColor per role — and both bases are DRAWN. The six NEW rows below are SUPERSEDED as the operative debt (kept for the eventual bespoke-six bar, which stands per the "for the time being" framing). **MAPPING CONFIRMED same day (user): the two bodies ARE the species split — human base = REUSE of the player Stonecarver calling tile (`stonecarver.png`, "we'll just use the player icon that's already made!"), mole-folk base = user sprite — LANDED same day, evening drop: `TerraFirma/Textures/stonerat.png` (16x24; user: "a bit more ratlike than expected, yeag" — no consequences: descs are ruled body-agnostic, so mole-vs-rat lives only in the pixels).** Net new files owed: ZERO — the faction's personnel art is fully in hand. **Gear consequence banked (user, same message): sharing the calling's face "handily answers some 'what items would they have' questions" — NPC carver inventories seed from the Stonecarver calling kit; USER DIRECTION: "we can keep their packs light for now."** (Claude note, factual: the calling kit includes chisel + mallet, so pack carvers pass the `Cleo_TerraFirma_EtchTool` gate by construction — etched stones stop being player-only artifacts; loot-side, downed carvers drop etch-capable tools.)

| Blueprint | Status | Sprite | Text | Notes |
|---|---|---|---|---|
| Hewer (Fissure) | RULED | ~~NEW~~ base ×2 + rust DetailColor (see note) | desc + greeting | |
| Mason (Stoneshaper) | RULED | ~~NEW~~ base ×2 + grey DetailColor (Claude lean, unruled) | desc + greeting | strange-mood variant reuses this tile |
| Outrider (EB) | RULED | ~~NEW~~ base ×2 + warden blue | desc + greeting | stationed at camp mouth |
| Drover (beast-tender) | RULED | ~~NEW~~ base ×2 + brown (Claude float, unruled) | desc + greeting | design meat still open (beast-bound) |
| Provisioner (trade) | RULED (cut pending — compression ruling) | — | — | trade role moved to vine merchant 2026-08-11; formal cut rides the open compression ruling |
| Overseer (legendary) | RULED | ~~NEW~~ same blueprint + hero builder (engine `&M` pink, free) | desc + greeting | water ritual = engine (GivesRep); hero builder adds name |

## 2. Creatures

| Blueprint | Status | Sprite | Text | Notes |
|---|---|---|---|---|
| Mole-beast | RULED (interior cut) | NEW | desc | name OPEN; weirdness axis OPEN (tuning-stone steering = leading idea) |
| Malformed cragmensch | IDEA (commitment OPEN) | VAR (cragmensch recolor) | desc | pure-XML build per recon |
| Stonefrog | PARKED | — | — | six calls queued; not this pass |
| Burrowing Urchin | DONE (companion mod) | REUSE | — | verify encounter-table Q only |
| Snapjaw miner | IDEA (art-first, confirm owed) | DONE — `snapjaw miner.png` on disk 2026-08-12, unwired | desc | snapjaw variant idiom = pure XML; see TODO roster entry |
| Rockhound | IDEA (art-first, confirm owed) | DONE — `rockhound.png` on disk 2026-08-12, unwired | desc | quarrier-kept pet; Herding Dog dual-faction precedent; see TODO roster entry |

## 3. Camp recipes — furniture & fixtures by room

| Object | Room | Status | Sprite | Text | Notes |
|---|---|---|---|---|---|
| Carved fire bowl | communal | LIKED | VAR (`Brazier`/`Campfire` confirmed) | desc | "they cut everything they own" |
| Hookah | communal | LIKED | REUSE (`Hookah` confirmed) | — | straight from the Yd salon |
| Cushions | communal | LIKED | REUSE (`Floor Cushion` confirmed) | — | |
| Half-finished projects | communal | LIKED | REUSE (mod statues/figurines) | desc (1 shared) | marker table |
| Lithophone | communal | IDEA [Claude] | NEW | desc | one object, big implication |
| Geode-splitting slab | communal | IDEA [Claude] | CHECK (table/slab reuse) | desc | |
| Feed pile (hay + worms) | sty | LIKED | CHECK (base hay) | desc | |
| Mineral lick | sty | IDEA [Claude] | NEW | desc | lithovore may lick; no comment |
| Carved runnel / seep | sty | LIKED | REUSE (water cell) + maybe VAR | desc | wet corner grows moss |
| Gastrolith droppings | sty | IDEA [Claude] | CHECK (base dung) or NEW | desc | polished-pebble forage table |
| Pen posts | sty | LIKED | CHECK (fence reuse) | — | |
| Tent fixture | overseer tent | LIKED | REUSE (base tents) | — | |
| Region map (lootable) | overseer tent | IDEA | CHECK (map scrap reuse) | desc | reveal mechanic = later call |
| Goods chest | overseer tent | LIKED | REUSE | — | contents table only |
| Predecessor-overseer statues | overseer tent | IDEA [Claude] | REUSE (mod statue tech) | **table** (litany lines) | pack history as furniture |
| Slab bench | workshop | LIKED | REUSE (`Bench`/`Ornate Bench` confirmed) | desc | marking bench = same tile |
| Tool rack | workshop | LIKED | VAR (no low-tech rack in base — only `Energy Cell Rack`) | desc | chisel/mallet visible in world |
| Unpowered millstone | workshop | LIKED | REUSE (**base-game `Millstone` confirmed**, MountedFurniture) | — | kitchen workstream likely already uses it |
| Apprentice-corner rejects | workshop | LIKED | REUSE (cracked viewing stone etc.) | — | second source, different sadness |
| Quarried blocks (raw→blocked→dressed→marked) | yard | LIKED | NEW ×1 + VAR ×3 | desc (1 shared or 4 short) | graded rows read as pipeline |
| Spent-stone midden | yard | LIKED [Claude] | REUSE (rubble) | desc | DullAfter given an address |
| Sleeping niche + effects | niches | LIKED [Claude] | REUSE (bedroll, figurines) | desc (niche flavor) | 1-cell honeycomb carves |
| Shrine plinth | shrine | LIKED [Claude] | REUSE/VAR (`SultanShrine` family = shrine-tech precedent; `Urn` for offerings) | desc | |
| Boundary-eye stele | shrine + mouth + wild | LIKED [Claude] | VAR (**`Village Monument Stele` confirmed** — retint/re-desc) | desc | one tile serves 3 uses |
| Powder jars / rations | mill-pantry | LIKED | REUSE (mod powders exist) | — | stock tables only |
| Infirmary bedroll scene | marker | RULED flavor-only | REUSE | desc | dust-sickness TEXT ONLY (2026-07-24); sap-mask grounding |

## 4. Cairn tableaus (wilderness recipes)

| Recipe | Status | Sprite | Text | Notes |
|---|---|---|---|---|
| Grave (bones + flowers + stele) | RULED-ish (user spec) | REUSE + shared stele | **table** (memorial litany) | "X raised this stone for Y" |
| Gathering spot (graffiti + wine) | RULED-ish (user spec) | REUSE (`Wine Pitcher` confirmed, + Merchant variant) | — | |
| Musing spot (carved markov text + half-formed statues) | RULED-ish (user spec) | REUSE (stele tile + mod statues) | **table/corpus** (markov source) | LibraryCorpus first, own corpus later |

## 5. Items

| Item | Status | Sprite | Text | Notes |
|---|---|---|---|---|
| Viewing stone | LIKED | NEW | desc | valuable curio; rare geode jackpot |
| Cracked viewing stone | LIKED (user's item) | VAR (of viewing stone) | desc | user's line is already the desc seed |
| Sealed geode | LIKED | NEW | desc | + contents population table |
| Fossils (curio family) | LIKED ("missed spot") | NEW (1–3) or VAR | desc | base `FossilizedRemains` chest = tile/precedent; fossil-theology book page candidate |
| Moss box (netsuke clasp) | IDEA (user notes) | NEW | desc | base `Clump of Grave Moss`/`Crushed Grave Moss` = ready-made contents |
| Mason-marked lumps | IDEA (tier undecided) | — | desc interpolation | legendary-stamp = Claude lean |
| ~~Unfinished One colossus~~ | **CUT 2026-07-24** | — | — | too much work right now |

## 6. Text-only debt (no sprites)

| Piece | Status | Blocked on |
|---|---|---|
| Faction name + display text | OPEN ruling | naming slate session |
| ~~Calling chargen description~~ | **NOT A THING — user was right (verified 2026-07-24):** base Subtypes.xml has NO description element for any caste/calling; chargen renders from stats/skills/gear. Only optional flavor surface = `<extrainfo>` one-liners (base idiom: "Starts with trade goods") — e.g. "Starts with a maquette" is available if ever wanted, zero debt | — |
| Anchor book (outsider-scholar) | pitched | homeland dial |
| Quarry chants (litany) | IDEA | nothing — writable anytime |
| HistorySpice entries (~30 powders + StoneMeal + rock flour) | **REQUIRED (Pass 3)** | spice merge-path verify |
| Profession greeting lines ×6 | counted in §1 — one line + trade option each (RULED 2026-07-24: no dialogue trees) | doctrine rulings enrich but don't block |
| Doctrine rulings (consent / funerary / lithovore schism) | OPEN | user — these feed all convo/desc voice |

## 7. Sprite checklist — tick as drawn (the durable record; the .html mirrors it clickably)

**NPC roster**
- [ ] hewer tile
- [ ] mason tile
- [ ] outrider tile
- [ ] drover tile
- [ ] provisioner tile
- [ ] overseer tile (or VAR of a base type)

**Creatures**
- [ ] mole-beast tile
- [ ] malformed cragmensch (VAR — only if committed)

**Camp fixtures**
- [ ] carved fire bowl (VAR of Brazier/Campfire)
- [ ] lithophone
- [ ] mineral lick
- [ ] quarried block (base tile)
- [ ] quarried block grades ×3 (VAR)
- [ ] boundary-eye stele (VAR of Village Monument Stele)
- [ ] tool rack (VAR)
- [ ] gastrolith droppings (pending dung CHECK — may be free)

**Items**
- [ ] viewing stone
- [ ] cracked viewing stone (VAR)
- [ ] sealed geode
- [ ] fossil curio (1, optionally up to 3)
- [ ] moss box

## 8. Pre-existing debt (cross-ref — lives in polish-worklist/TODO, listed here for one view)

Attune Stones icon (cyan placeholder) · stalagmite tile placeholder · Lithovore defect tile ·
Statue Maker description ("placeholder description >:)") · manifest description · **every rock
description is DRAFT** (v1 fifteen + waves + deep four) · Lithovore session-DRAFT strings ·
Stoneshaper/Gorgon/Calcify/Geomancy text passes · Ctesiphus "phosphorescent" quirk.
