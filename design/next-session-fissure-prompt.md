# Next-session prompt: the Fissure redoubt (written 2026-08-04)

Paste or point a fresh agent here to continue the Fissure redesign for the Terra Firma
Caves of Qud mod (repo: `C:\Users\venob\terra firma revival\CoQ_TerraFirma-dev`).

## Non-negotiable working norms

- **TODO.md is the source of truth.** Read its REDOUBT PIVOT entry (friends-beta section)
  before touching anything. Every design-shaping statement carries a provenance tag:
  RULED (user's words, dated) / LEAN (user's, not a ruling) / CLAUDE PROPOSAL, UNRULED.
  Cite rules only by finding their RULED marker. Delegated code needs no ceremony.
- The Stonecarver calling is a **Mutated Human** calling, NOT True Kin.
- **No Co-Authored-By trailer on commits** (RULED). Commit early and often; the repo is
  git-initialized, active branch `master`.
- **Text is the user's domain**: mutation descriptions, level text, names, lore. Draft
  freely, mark drafts, never sign off text yourself. **No em dashes in shipped text.**
  Beta-draft text ships under the standing 2026-07-31 ruling; the vigilant signoff is a
  release-pass concern.
- The user rules on design one item at a time; propose, don't harden proposals into rules.

## State of the redoubt (as of 2026-08-04)

Fissure was rebuilt 2026-08-02 from "grenade-catalog ceiling collapse" (v2, preserved at
git tag `fissure-v2-grenade`) into THE REDOUBT: a wave radiates from the caster, damages +
pushes creatures outward (engine force-vs-weight; stun only-if-moved, Toughness DC 16+L),
CARVES natural stone (level-scaled depth; the `Cleo_TerraFirma_StoneshaperWallProperties`
part = carvable, its absence = the wave stops; connection doctrine), and deposits a
non-expiring Euclidean ring of fabric (rubble/crumbled/wall; wall share 10..75% by band
with rolled variance, user's numbers) with 6..3 gaps by band, plus loose-stone wake
scatter across the interior. The ring dresses as the zone's DOMINANT natural-stone wall
(modal PaintedWall among StoneshaperWallProperties bearers -> stamped on deposits, zone
painter renders connectivity; sandstone fallback). Cooldown 100 flat (Force Wall/Bubble
parity, precedent-dug).

Animation (several feel rounds, all user-reviewed): custom CombatJuice entry
(`Fx_StoneRise.cs`) slides OPAQUE pooled sprites of the local wall tile partway out of the
earth per travel shell, staggers the rim breakthrough, holds until each cell's real fabric
materializes inside a frame-pump settle loop (world renders live; per-cell smoke, thinned).
Sprites never fully hide (no burrow idiom -- user rule). Tile resolved at runtime by
probing atlas candidates (`FIS anim tile` log lines); colors + background from the dominant
material. Walls deposited onto liquid DISPLACE the pool to a dry neighbor (water is never
destroyed) or downgrade to rubble if undammable.

BETA-HELD inside the cast (all in marked comment blocks, user's words attached): keeper
salvage, geyser-in-gap, the stone figure, crack-gates under gaps. The ring is pure fabric
for the beta. Also held mod-wide: Lithovore (chargen gated; kitchenrock wish = ruled dev
door), the mill/cooking suite, malformed cragmensch, the deep four (dormant blueprints).

Key files: `TerraFirma/Scripts/Mutation_Fissure.cs` (cast), `Scripts/Fx_StoneRise.cs`
(animation), `XML/Populations.xml` (trimmed tables), `design/redoubt-numbers-draft.md`
(dials + feel-round history), `design/vfx-survey-2026-08-03.md`, 
`design/fallout-sweep-2026-08-02.md`, `design/jam-2026-08-02-rock-items.md` (the jam
record), TODO.md (canon).

## Build / deploy / verify recipe

- Compile: `dotnet build TerraFirma.csproj -p:QudLibPath="C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed"`
  (NO trailing backslash). Any error is a real error; MSB3277 warning is benign.
- Strict-validate XML after edits (XmlDocument.Load; the `--`-in-comment trap is chronic).
- Deploy: copy changed files to `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma\`,
  hash-verify. Changes activate on game restart only.
- Verification: TF-VERIFY instrumentation is ON through the beta (strip at 1.0). `[FIS]`
  log lines cover cast/carve/wave/ring/anim-tile. Harness wishes: tfbatch (trimmed chain),
  tffis (ring-piece census), tfcarom/tfcrack/tfquick/tfform/tfscav live; held scenarios
  remain compiled as hand-run dev doors.

## Owed / open work (check TODO for current status before starting)

1. Feel rounds continue: the user casts, gives notes, you dial. All tempo/geometry dials
   sit at the top of Fx_StoneRise.cs; mechanics dials in Mutation_Fissure's Scaling region.
2. Redoubt-specific harness scenarios DO NOT EXIST yet (wave push/stun asserts, carve
   depth, ring census by band, water displacement) -- tffis only counts ring pieces.
3. Still owed from the gate-verify: clean `rockwineverytime` character tfbatch (tfscav +
   tfshape), plus save->quit->load with a ring standing (serialization bald spot).
4. Release text sitting: Fissure's GetDescription/GetLevelText are DRAFTS; the level text
   no longer fully matches mechanics (falls/area/depth lines are gone -- rewrite wanted at
   the sitting, user signs off).
5. Beta packaging after the user calls the mutation done: see TODO's beta checklist.

Ask the user what they want first; default to responding to their latest feel notes.
