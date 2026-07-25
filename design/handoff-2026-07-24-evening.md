# Handoff — the verify marathon + build batch of 2026-07-24 (evening; write date same day)

You are picking up Terra Firma (`Cleo_TerraFirma`), a commissioned Caves of Qud mod the user
(full rights holder) is finishing. Read in this order before anything substantive: your memory
directory (auto-loaded — note especially `stonecarver-is-mutated-human` and the updated
`verify-watch-workflow`), then **TODO.md at the repo root — the single source of truth**, then
`design/polish-worklist-2026-07-23.md` (the working index), then run `python design/progress.py`
(regenerate after ANY checkbox change; the user relies on the dashboard). The interactive verify
sheet is `design/verify-pass.html` — see "The verify sheet" below before touching it.

## Where things stand

Today was the project's biggest day: a **verify marathon** (fresh character "Karuukat", Mutated
Human Stonecarver, save is healthy) that took the 70-item sheet from unknown to nearly all
green/amber/deferred, followed by a **post-session build batch — BUILT + DEPLOYED, UNVERIFIED**
(activates on next game launch). Full record: TODO.md § "POST-SESSION BUILD BATCH" and
§ "Verify session 2 findings".

**The batch awaiting next-boot verification (8 pieces, expected signals):**
1. **Streakschorl re-equip re-fix** (`Part_StreakFlight`) — first fix FAILED user testing
   (Check() notifies handlers only; the engine's bag-search lives in a protected wrapper we now
   replicate). Verify: fly the last held streakschorl with spares in the bag → next lands in
   hand; STRK logs "bag re-equip: ... readied".
2. **Fungal lottery diagnostic** (`Part_FungalLottery`) — persistence via SetStringGameState
   EXISTS and fails silently (four different permutations across one save's boots). The SPORE
   line now says "ROLLED FRESH - stored state was empty" vs "loaded from game state". Verify:
   reload the save; ROLLED FRESH on a load = GetStringGameState returns empty post-load → then
   design the real fix.
3. **SCAV pocket re-ready** (`Part_StoneScavenger`) — slot-empty + pocket stone → forced
   CommandEquipObject as a **FREE action (user ruling: thrown-weapon equips cost nothing)**.
   REFUSED outcomes log (the smoking-gun detector for the cadence regression's H1). Verify:
   baboons re-arm every throw; cadence ≥ vanilla (the user's vanilla control PROVED the mod
   regresses baboon throwing — hypotheses H1/H2/H3 in TODO; this fix may mask or reveal).
4. **Millstone HOPPER** (`Part_StoneHopper` NEW, Merges swap; menu grind on the mill REMOVED
   per ruling "menu probably goes") — powered mill grinds one hopper stone/turn (throughput 1
   = DRAFT) into shared-recipe servings that accumulate in the mill. Verify: rocks into
   Joppa's mill → powders while the wheel turns; dead mill grinds nothing; GRIND "hopper:" lines.
5. **Statuette popup suppression** (`Part_TimedForm`) — stamps NoFollowerDeathPopup (property;
   engine gate is HasTagOrProperty, decomp-verified) pre-Obliterate. Verify: expiry shows the
   stiffens line with NO "Your companion ... died." popup; combat deaths still popup.
6. **Calcify DV text** — "0 DV" → "−10 DV" (lean (b) text-follows-reality, DRAFT — user may
   veto toward a creation-time DV override; AV 0 confirmed INTENDED, the HP pool is the defense).
7. **Drainstone victim-died log** (`Part_DrainLatch`) — the last silent link speaks.
8. (No code) Healstone "double-fire" was RETRACTED — two interleaved throws misread; the
   wall-heal synergy (healstones repair walls — mason's medkit for Calcify) awaits bless/gate.

**Also unverified-by-log but user-confirmed today:** Boar Vessel passivity (`Brain
Passive="true"` — the NoCombat tag was never a brain mechanism), puddle renders, figurine rep,
per-rock powder display names, "Engagement style: defensive".

## Hard workflow rules (several learned expensively TODAY)

1. **Compile**: `dotnet build TerraFirma.csproj -p:QudLibPath="C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\"`
   — fully clean builds; ANY error is real; ignore MSB3277/MSB3106 warnings.
2. **The `--` XML-comment curse** (10+ strikes): never put `--` inside XML comments; strict
   `XmlDocument.Load` in try/catch BEFORE deploying any XML.
3. Deploy = copy changed files to `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma\`;
   the game compiles mods at launch only.
4. Commits: **no Co-Authored-By trailer, ever**; commit per logical unit; `git commit -F` (ascii
   encoding — utf8 Out-File leaks a BOM into the subject) for messages with quotes.
5. **Record every ruling in BOTH trackers with dates and the user's words** — including vetoes,
   ruled-as-fine, and RETRACTIONS (report those honestly; two retractions today).
6. **During live verify sessions: FLAG, don't fix** (user rule, in memory). Fixes batch
   post-session. Mid-batch adjustments on user ruling are fine.
7. **Log inference is not proof** (memory note; three overconfident calls today): distinguish
   log-half from eyeball-half; "consistent with" ≠ "proven"; NPC paths don't prove player paths
   (slot-stack remainders mimic bag re-equips); interleaved throws mimic double-fires.
8. **Tracker edits: anchor insertions on entry TAILS, never heads** — three entry-fusion
   accidents today (headers eaten, bodies fused). If grep shows a fused line, repair it and say so.
9. **Stonecarver is a MUTATED HUMAN calling — NOT True Kin** (thrice-repeated error; the source
   was TODO.md itself, now fixed; callings ARE the mutant category, True Kin pick castes).
10. **Watcher**: invoke EXACTLY
    `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\venob\.claude\watch-player-log.ps1 -TimeoutMinutes 9`
    (run_in_background, STANDALONE — never chained with git; chaining breaks the allowlist
    match). The user runs auto mode now which should keep it prompt-free; if prompts recur,
    fall back to direct `Get-Content`/`Select-String` reads with your own position file.
    Restart on each completion; read the output file on the completion notification.
11. Decomp ground truth over memory: the full dump survives at
    `...\Temp\claude\C--Users-venob-terra-firma-revival\9c2ffaa8-*\scratchpad\decomp` (5400+
    files — grep it before decompiling fresh; `ilspycmd` also installed). Game 2.0.211.50.

## The verify sheet (`design/verify-pass.html`)

70 items, machine-maintained `VERDICTS` block between `===VERDICTS-START/END===` markers —
Claude writes pass/partial/fail/**deferred** verdicts with evidence notes as the log confirms
items; user checkboxes live in their browser's localStorage and the page renders the union.
Update `LAST_UPDATED` when writing verdicts. Keep favicon/format; the user watches it live.
Remaining open on the sheet: healstone self-throw, drainstone step-proc, streakschorl tails
(render modes, whorl-vs-flight, save/load — now unblocked by batch item 1), eb-beam's true-Lase
scatter half, the deferred NPC-gorgon pair (dead-gorgon + stunned-caster — deferred until the
creatures/quarriers workstream mints NPC gorgons), and the attune icon carry-forward (re-verify
only after the user's planned attune-stone art edit).

## Open rulings queue (present compactly, batch-style)

- **Caromcobble StepChance** — user: "could be higher"; re-opens ratified 25; peel never breaks
  so proc rate is pure zoning strength (50? 100?). One XML number.
- **Seam squad-drops** — a statuette seam dropped NINE living baboons in one Fissure cast
  (machinery flawless; scale is the question): keep as jackpot comedy / exclude creatures from
  seam eligibility / cap living falls.
- **Phasalt vs walls** — log claims Phased applies to walls, game shows nothing (user tested,
  8 throws): confirm the no-op and gate the payload, or make walk-through-walls real, and either
  way stop the log overpromising.
- **Healstone wall-repair** — bless (mason's medkit) or gate.
- **Gemstone default equip → thrown slot** — they're first-class ammo now; recon the engine's
  default-slot pick.
- **Hopper throughput** (1/turn DRAFT) + **Calcify DV lean (b)** — veto windows.
- **AddsRep 200** (figurine) — DRAFT number, ratify.
- Long-standing: the "roiling innards" rename TALK (release-gated, candidates recorded),
  manifest description (3 fresh drafts were presented: deadpan-inventory / verb-forward /
  epigraph registers), claysculpting loop description (2 drafts presented).

## Sittings queued (creative, with the user)

HistorySpice (fully prepped: `design/historyspice-prep.md`, 36 keys, per-rock uniqueness is
WANTED — user blessed the powder split precisely for it) · weak-rock docket + dazestone
intervention + deep-galvena stronger-electric (one cooking-rocks sitting) · **deflection VFX
sitting** (user: refract needs a visible light-track; stone ricochet wants a clearer carom) ·
**resonant verb set** (user: attuned deflections shouldn't use gravel verbs — mass grinds,
resonance refracts; branch the nine XDidY calls on Resonant) · statuette expiry message
strengthening + figurine description connect-back · mod-wide text pass (base-game voice,
everything DRAFT) · geyser mini-pass (needs the rules-text observation, still unmade).

## Style that works with this user

Discuss-then-act; batch rulings compactly ("all fine except X"); build immediately once ruled;
push back with reasons (they welcome it and are usually right — their one-shot-kill drainstone
diagnosis and vanilla baboon control were the day's best debugging); they test WITH you (give
them concrete experiments); regenerate the dashboard so they SEE the bar move; keep watcher
windows narrated briefly (one-liners for quiet windows).
