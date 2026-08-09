# Next-session prompt: the Fissure PULSE PASS (written 2026-08-09)

Point a fresh agent here to build the Fissure pulse rework for the Terra Firma Caves of
Qud mod (repo: `C:\Users\venob\terra firma revival\CoQ_TerraFirma-dev`). The design
sitting is DONE — the spec is complete and quote-tagged; this session is a port + VFX
pass, not a design session. Do not re-litigate ruled items.

## Read order (before touching anything)

1. TODO.md — working norms + current canon (source of truth).
2. **`design/fissure-pulse-rework-2026-08-08.md` — THE SPEC.** Complete, every ruling
   carries the user's dated words. What it says, you build.
3. `design/next-session-fissure-prompt.md` (2026-08-04) — the redoubt-era handoff: build
   recipe, animation history, held features. Still accurate for everything the pulse
   spec doesn't supersede.
4. `design/redoubt-numbers-draft.md` — still governs everything non-wave (ring fabric,
   gaps, wall share, dressing, cooldown 100 flat).
5. `TerraFirma/Scripts/Mutation_Fissure.cs` + `Scripts/Fx_StoneRise.cs` — current state.

## Non-negotiable working norms (unchanged)

- Provenance discipline: RULED (user's words, dated) / LEAN / CLAUDE PROPOSAL. Cite
  rules only via their RULED marker; propose, don't harden proposals into rules.
- Stonecarver = **Mutated Human** calling, NOT True Kin.
- **No Co-Authored-By trailer on commits.** Commit early and often, branch `master`.
- **Text is the user's domain** — level text/descriptions are drafts until signoff.
  No em dashes in shipped text.
- TF-VERIFY instrumentation stays ON through the beta (strip at 1.0).

## TWO RULINGS WERE PENDING AT HANDOFF — get them from the user FIRST

1. **Epicenter equity**: does a stationary target at the caster's tile / ring 1 take the
   depleting pulse sequence (Claude recommendation, matches the FX amplitude grammar) or
   full leading dice every beat? Marked AWAITING RULING in the spec's engulf-catch bullet.
2. **Animation concept B confirm**: user sketched A (tall wall-motion rides the front
   outward) vs B (tall stays interior, shrinking toward the edge). Claude recommended B
   — amplitude belongs to the PULSE (100%/50%/25% by beat), interior re-bobs at the
   current pulse's smaller amplitude, churn BOBS (up-crest-settle) while rim fabric
   RISES AND STAYS. User had not yet said the word. Confirm before building FX.

## What this pass builds (mechanics first, FX second)

**Mechanics — `Mutation_Fissure.cs`:**
- Replace the single-hit wave with the 3-pulse expanding churn: pulse t hits everything
  within ring t (origin tile included, caster exempt), then pushes hit creatures ONE
  ring outward (clamp ruled; the weight-vs-force sort still decides IF they move).
- Damage per pulse = attack: dice ladder (spec draft v3, d3→d4 step at band 3) rolled
  per penetration, **PV = 4 + Strength mod** (level-scaled pen bonus REJECTED — overpen
  slippery slope, user's words in spec). USER FLAG ON RECORD: "not sure about these
  dice numbers" — every ladder cell is a feel dial; dial, don't defend.
- Stun: ONE check per cast, on the last pulse that moved the creature (1/2/3/4 rounds
  by band, Toughness DC 16+L unchanged).
- Pinning: pushed into a standing wall = slam (wave die of the current pulse), each
  remaining pulse it sits in coverage. Wall riders: save-or-slam PER PULSE.
- Rim eviction (ruled, spec has the case split): fabric ALWAYS rises; occupant bounced
  outward if open, slammed + deposited INSIDE the ring if blocked; both-blocked corridor
  case keeps the debris downgrade.
- Carve: per-pulse bites REPLACE GetCarveDepth + the per-ray bite budget. Every wall in
  a pulse's coverage takes one carve-as-attack bite (grammar unchanged: wall PV 6+3L,
  34%-of-max per pen, hard-class clamped). Ring-1 walls get up to 3 bites. KEEP: rays/
  shadowing for rim placement (blocked rays still shadow the rim), own-fabric-yields,
  mass credits (fallen walls only), stone classifier. OPEN at port time: whether the
  dissipating front bites ring-4 walls once — pick, log, flag for feel.
- Flyers stay exempt (unchanged dial). Loot rules, water displacement, fugue tether,
  recast-refresh/seal: ALL UNCHANGED — do not touch.
- AI: prefer casting at adjacency (the damage mode); current check is distance ≤ radius.

**FX — `Fx_StoneRise.cs` (after B is confirmed):**
- Three ripple beats of decaying amplitude (100/50/25), churn = round-trip bob
  (up-crest-settle), rim = rise-and-stay. Interior re-bobs each beat at the current
  pulse's amplitude, SAMPLED (not all ~50 cells — density is a dial) with fractured
  plates bobbing as slabs. Three camera shakes, descending. The rim punch-through at
  radius 4 stays the signature closing beat — nothing may crowd it.

**Harness — before the feel session:**
- Pen-distribution wish (e.g. `tfpen`): roll `Stat.RollDamagePenetrations` a few
  thousand times across PV 4..10 × AV 0/4/8/12, log expected pens + expected damage
  under the v3 ladder. The feel session dials against MEASUREMENTS, not estimates.
- New `[FIS]` verify lines: per-pulse coverage size, per-victim hits/pens/damage,
  eviction events, per-pulse carve bites.

## Build / deploy / verify recipe (unchanged from 2026-08-04 prompt)

- Compile: `dotnet build TerraFirma.csproj -p:QudLibPath="C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed"`
  (NO trailing backslash). MSB3277 warning benign; any error is real.
- Strict-validate XML after edits (the `--`-in-comment trap is chronic).
- Deploy: copy to `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma\`,
  hash-verify, game restart to activate. Player.log watcher per the verify-watch workflow.

## After mechanics land

- Level text: GetDescription/GetLevelText need a rewrite for the pulse mechanic — DRAFT
  ONLY, user signs off (voice conventions: design/mutation-voice-audit-2026-08-06.md).
- Feel rounds: user casts, gives notes, you dial. Companion churn is an explicit
  playtest-with-other-players item (ruled).
- Preserve the pre-pulse cast at a git tag before the rewrite (idiom precedent:
  `fissure-v2-grenade`) — suggest `fissure-v3-redoubt`.

Default first move: ask the user for the two pending rulings, then port mechanics.
