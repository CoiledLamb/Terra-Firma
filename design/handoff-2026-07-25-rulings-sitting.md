# Handoff — the rulings-and-fixes sitting (written 2026-07-25, at the harness marathon's close)

You are picking up Terra Firma (`Cleo_TerraFirma`), a commissioned Caves of Qud mod the user
(full rights holder) is finishing. **Your job this session: initiate the design conversation on
everything the verification harness cleared up that now needs rulings or code changes.** The
verification itself is DONE — Tier 1 of the automated harness is complete, the board is green,
and what remains is deciding and building.

Read in this order before anything substantive: your memory directory (auto-loaded — note
`stonecarver-is-mutated-human`, `verify-watch-workflow`, `text-voice-and-ownership`), **TODO.md
at the repo root — the single source of truth** (start at § Automated verification harness for
this week's record), `design/polish-worklist-2026-07-23.md` (the index),
**`design/harness-verified.md` (NEW: the ledger of harness-proven facts — every entry lists the
exact assertion, its LIMITS, and the re-run wish; if anything verified goes funny, re-run its
wish FIRST)**, and `design/verify-pass.html` (the sheet). Run `python design/progress.py` after
any checkbox change — the user watches the dashboard.

House rules that bite (full versions in memory/TODO): compile with
`dotnet build TerraFirma.csproj -p:QudLibPath="C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\"`
(any error is real) · never `--` inside XML comments; strict-parse XML before deploying · deploy
= copy to `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma\`, activates
on game restart only · commits per logical unit, NO Co-Authored-By trailer, `git commit -F` with
ascii for quoted messages · record every ruling in BOTH trackers with dates and the user's words
(vetoes and retractions included) · tracker edits anchor on entry TAILS · log inference is not
proof — but in-code assertion IS · watcher invocation EXACTLY:
`powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\venob\.claude\watch-player-log.ps1 -TimeoutMinutes 9`
(background, standalone, never chained).

Style that works: discuss-then-act; present rulings BATCH-STYLE and compactly (they answer "all
fine except X"); push back with reasons (they welcome it and are usually right); build
immediately once ruled; give them concrete experiments; keep watcher windows narrated in
one-liners. Text is theirs (draft-until-signoff); code is delegated.

---

## THE DOCKET — organized by decision weight

### A. Build-ready, no ruling needed (open with these; one fix batch, one deploy)

1. **The figurine pair** — two confirmed defects, two one-attribute XML fixes on
   `Cleo_TerraFirma_BaboonFigurineItem`:
   - `<part Name="Examiner" Complexity="0" />` override — it inherits base Trinket's
     identify-me behavior (Items.xml:11811); user flag: "needed to be identified (unintended)".
   - `WorksOnCarrier="true"` on the AddsRep part — **harness-proven INERT** (quick.rep: rep
     −300→−300 on take, delta 0; the part is equipper-only without the attribute; the rep half
     has never functioned).
   While in there: sweep other Trinket-inheriting mod items for the same two warts.
   **Verification is free: the standing RED `quick.rep` assert flips green on the fixed build.**

2. **(Conditional) fungal lottery storage** — ASK FIRST: did the user run the save-quit-reload
   experiment? (Note the SPORE permutation → menu Save and Quit → reload the SAME save → mint a
   fungal rock → compare.) Diagnosis so far: persistence works all session, dies at the boot
   boundary; the two suspects are SetStringGameState-not-surviving vs sessions-ending-unsaved.
   Same permutation after a real save = code innocent, CLOSE the bug. Different = design real
   storage (then the fix). Do NOT redesign before the experiment.

### B. One-line rulings (present as one compact batch, build the winners same sitting)

1. **Caromcobble StepChance 25 → higher?** (user: "could be higher"; the peel never breaks, so
   the proc rate is pure zoning strength — 50? 100?). One XML number. After ruling: update
   `tfcarom`'s statistical band to the new rate and re-run as the verifier.
2. **Seam squad-drops** — nine living baboons in one Fissure cast (machinery flawless; scale is
   the question): keep as jackpot comedy / exclude creatures from seam eligibility / cap living
   falls.
3. **Phasalt vs walls** — log claims Phased applies to walls, game shows nothing (user tested, 8
   throws): confirm the no-op and gate the payload, or make walk-through-walls real; either way
   stop the log overpromising.
4. **Healstone wall-repair** — healstones repair walls (mason's medkit for Calcify): bless or gate.
5. **Gemstone default equip → thrown slot** — they're first-class ammo now; recon the engine's
   default-slot pick at build.
6. **SCAV dry-fetch WEDGE** (harness find, watched across two full windows): a pockets-empty
   baboon oscillates "detours to loose stone at d=1" forever — stoop only fires at d=0, so a
   stone in a cell it can't/won't enter wedges the loop. Shapes: reach-stoop at d=1 / give up a
   target after N failed detours / exclude occupied cells from `CellHasLooseStone`.
7. **Fissure AI advertisement** (harness-proven ABSENT — tffis: a hostile-locked Fissure L5
   creature cast NOTHING in 20 turns): build the Calcify-pattern AI handler NOW, or park it for
   the quarriers/creatures workstream where it already sits on the owed list
   (EB / Stoneshaper / Gorgon / Fissure)? Note what it blocks: fis-npc plus the deferred
   dead-gorgon and stunned-caster sheet items.
8. **The ratify pile** (DRAFT numbers awaiting bless): hopper throughput 1 stone/turn · Calcify
   level text "−10 DV" (lean (b) text-follows-reality; veto = creation-time DV override
   instead) · figurine AddsRep 200 (the NUMBER, now that the part will actually work) ·
   **farflint dials ONLY if the survival still grates** (log-proven working as designed — a 25%
   HOLDS roll): (a) as-is · (b) `ConsumedOnStep="true"` · (c) the flavor third way — on a step
   proc the stone shunts ITSELF elsewhere too.

### C. Real conversations (the user explicitly wants these AS conversations — do not rush them)

1. **Hold rate / fragility / natural expiry as one system** (parked 2026-07-24, now with a
   step-half LEAN on file — NOT a ruling, their words: "consumed on step is a great baseline
   for stones that get destroyed when walked over… i've also been considering making them break
   100% of the time when stepped on… i'm honestly not sure. it's a design conversation thing").
   Seeds already recorded in TODO (§ the farflint entry): the live system already breaks 3 of 4
   step-procs (HoldChance 25 = 25% SURVIVE), so the real question is whether the "lucky stone
   keeps holding on" whimsy (their own 2026-07-22 ruling) survives at 0%; a blanket 100% needs
   instant exemptions (caromcobble's permanent peel IS its identity; phosphorescent's glow-mark
   becomes single-use zoning) — which points at the cleaner principle: **a stone is consumed by
   DISCHARGING its payload; persistent-zoning stones keep their holds** — no exemption list
   needed because the distinction does the work. Dials on file: HoldChance 25 default, DullAfter
   1000–2500, TimedForm 500–1000, healstone 0, caromcobble 100. Build shapes if ruled:
   ConsumedOnStep on the base blueprint with per-stone overrides (pure XML) vs a separate
   StepHoldChance (C#-small).
2. **Do NOT fold in** the weak-rock docket, the name-rewrite slate, or the holographic rock —
   those are their own queued sittings (entries in TODO/worklist); just know they exist so
   nothing gets double-booked.

---

## After the fix batch: the harness IS the verifier

Restart the game (mods compile at launch), then ONE wish — `tfbatch` (~100 turns standing
still, 18 scenarios) — and read the `[AUTO]` lines. Expected on the fixed build: `quick.rep`
GREEN for the first time (that's the figurine verification), the sludgestone eat row GREEN
(pool-merge pre-sweep already deployed), everything else green, `tffis` still the no-handler
answer unless you built B7. Transcribe results into `design/harness-verified.md` (assertion +
limits + re-run wish — never ledger a claim the assertion didn't make) and the sheet's VERDICTS
block (update LAST_UPDATED). If StepChance was re-ruled, fix `tfcarom`'s accept-band BEFORE
re-running or it will red honestly.

Standing obligations to keep visible: the `[TF-VERIFY]`/harness strip before release (one kill
switch: `Helpers.VERIFY_WATCH`) · Tier 2 (hands-off boot cycle) stays parked until the lottery
verdict or the scav-vs-vanilla baseline actually blocks a decision · the stale +100
throw-accuracy god-arm on the old playtest character is still strippable by one-line wish (it
confounded the miss saga once already).
