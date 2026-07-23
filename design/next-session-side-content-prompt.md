# Session prompt: Terra Firma side content

I'm continuing work on **Terra Firma**, a Caves of Qud mod I commissioned (by "Cleo, Ceres
et al.") and now own outright. The core is done and live: seven earth mutations audited,
redesigned, and playing well, and the Fissure rock catalog is fully core-built (54/54
living entries — deep elementals, cracks and chasms, the drainstone, the lot). This
session is **side content**: the world around the mutations, fleshed out through design
conversation. Fissure has its own follow-up queue (strange-stone decisions, stonefrog,
geyser body plan, ratification/rename/text passes) recorded in TODO.md — that queue is NOT
today's focus unless I say otherwise or open with verification findings.

## Read these first
- `C:\Users\venob\terra firma revival\CoQ_TerraFirma-dev\TODO.md` — the single source of
  truth. For this session especially: **"Throw-augmenting items"** (engine ground truth on
  the whole thrown system + open forks), **"Cooking effects"** (recon done, direction
  ruled, slate awaiting my rulings), **"Pending design budget"** and **"Creature/NPC
  groundwork"** (verified engine recon for wild creatures/NPCs), and the Mod-wide section
  (manifest, publishing). Read thoroughly before saying anything substantive.
- `CoQ_TerraFirma-dev\design\fissure-rock-list.md` — the finished catalog, for reference
  (side content can hook into the fall tables, but the catalog itself is done).
- Mod source: `CoQ_TerraFirma-dev\TerraFirma\`. Deployed copy the game loads:
  `C:\Users\venob\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma` — sync every
  change (`robocopy <source> <deployed> /E`). Test wishes: `fissurerocks` (one of every
  rock), `rockwineverytime` (full debug character).

## The workstreams — this is what I'm interested in (no order committed; we pick together)

1. **Throw-enhancing items and gear.** Stabilizer bands are built (blueprint ships with
   the mod; world wiring — merchants, gear tables, loot — is an open ruling, and the text/
   commerce numbers await ratification). Still open from my original four concepts: the
   **chalk container** (damage-or-accuracy fork + resource model) and the **sling**
   (worn-booster vs true MissileWeapon fork); the **visor** is binned for later. The full
   thrown-system decompile (scatter vs to-hit, the dead ThrowToHitBonus knob, the real
   CloseThrowRangeAccuracyBonus lever, range math, benchmarks) is recorded in TODO — trust
   it, re-verify only what we touch. New gear concepts are welcome in this lane too.

2. **Cooking ingredients and effects.** Direction already ruled: domains themed on the
   mod's MUTATIONS, not on rock-tagging. The drafted mutation-domain slate (Fissure
   ceiling-drop actions, Calcify wall-eruptions, gaze stone-flesh, statue grants, gravel
   whorl, geomancy reveal) awaits my rulings on which mutations make the cut and in what
   slot shapes; packaging (how many domains) and carriers (what items hold them — rocks,
   new ingredients, harvestable wall growths) are mine to decide in conversation. The
   engine recon (PreparedCookingIngredient, ProceduralCookingIngredient_{type}, the
   mutation-grant generic, PreservableItem grind step) is done and recorded.

3. **Quarriers out in the wild.** People who work the stone: stonecutter NPCs, camps or
   villages, what they dig, sell, and believe. The Stonecarver calling and companion items
   (chisel, mallet) are already merged and give the profession a foothold; extend from
   there. Nothing else is designed — concepts are mine, engine legwork is yours (villages,
   merchants, conversation systems, faction wiring: recon before we commit shapes).

4. **New creatures.** Two lanes: creatures that carry the mod's mutations innately (wild
   gorgons, calcifiers, boulder-throwers — the AI-advertisement/NPC-targeting/save-math
   groundwork is verified and recorded; known gap: Calcify has no NPC cell-picking) and
   original earth-creatures (the stonefrog already has a drafted spec in TODO's settle
   queue — it can fold into this conversation if I want). Ceiling creatures for the fall
   tables stay sparse and deliberate; I have a shortlist.

5. **Lore and worldbuilding.** Books, relics, inscriptions, lore hooks tying the mutations
   into Qud's history; the manifest description (still placeholder); Workshop publishing
   plan + credits. Base-game voice throughout; the words are mine, the delivery mechanisms
   (book items, secrets, conversation text) are yours to recon.

## How we work — read carefully, this is the important part
- **Design is collaborative. Do NOT invent creature concepts, names, stats, flavor text,
  or descriptions on your own — not even as "suggestions" in your opening message.** Your
  job is engine facts, decompiled precedents, benchmarks, feasibility, and implementation;
  the concepts and words are mine. Exception (standing ruling): when I explicitly ask for
  name/text candidates, offer them (portmanteau formula, ration "-ite", vantabsidian-style).
- **Discuss then implement**: specs with concrete numbers get my sign-off before code.
  Small unambiguous fixes can just be done. When I say work through something slowly, one
  decision at a time — no batched question panels.
- **Ground truth over memory**: verify every API and blueprint name by decompiling or
  grepping. The previous session's decompile dump lived in a session scratchpad and is
  GONE — re-derive with `ilspycmd` (installed) against `C:\Program Files (x86)\Steam\
  steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.dll` (`-p -o <dir>` for
  the greppable full dump, ~a minute, worth it). Game XML at `CoQ_Data\StreamingAssets\Base\`.
- **Design themes to honor**: "the earth rewards the disciplined" (Willpower recurs);
  hoard-or-spend tensions; rocks don't discriminate; stone bodies = +AV but acid eats
  stone (standing ruling); base-game voice in all player-facing text, DRAFT-marked in XML
  for my text pass.
- **Balance via comparison**: benchmark against decompiled base-game equivalents.
- **Constraints**: the seven mutations and the rock catalog are settled — no refactoring
  except where a task needs a hook. Update TODO.md as decisions land; sync to the Mods
  folder after builds. Git commits (if any) carry NO Co-Authored-By trailer. Practical
  gotcha that has bitten before: XML comments must not contain `--`.
- I playtest on a rolling basis. If I open with findings, fixing those comes first.

Start by reading TODO.md (the sections named above), then confirm what you see and ask
which workstream we're opening. Don't propose content in your opening — show me you have
the context, surface anything that needs my attention, and ask where we're starting.
