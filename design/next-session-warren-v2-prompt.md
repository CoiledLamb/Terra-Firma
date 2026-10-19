# Next-session prompt: warren v2 — close the carve, open the dressing (2026-08-18)

New track. Does **not** supersede `next-session-warren-lab-prompt.md` (that one hands off warren
**v1**, which is still live and still the comparison track). Copy the block below into a fresh
session.

---

We're continuing the **warren v2 branch** of Terra Firma (Caves of Qud mod, `CoQ_TerraFirma-dev`).
The rig is `design/warren-lab-v2.html` — standalone, no build step, open it from disk. Own
localStorage keys (`tfw2*`), so v1's lab and its saved canvases are untouched.

**Read this first and do not skip it: `TODO.md` § "WARREN v2 — GOAL STATEMENT + BRANCH CHARTER"
(currently ~line 410) through the end of the occupancy section (~line 700).** That is roughly
300 lines and it is the entire design record for this branch — the goal statement, the stage axis,
the payload model, the ten rules extracted from my drawings, the three-layer architecture, the
canyon census, the engine ports, the spawn ruling and the occupancy ruling. Everything below
assumes you have read it.

## 0. Where the work actually stands

The **carve layer is finished except for one item.** Verified over 200–400 roll batches: every
site has exactly one mouth, zero disconnected sites, zero doorless rooms, zero breaches into
canyon floor or river, corridor slack ~0.09 per site, ~1.25 diagonal pinches per site. The canyon
host, the river/marsh, the tree scatter, the Dirty floor paint, padded earth and the Redrock
aquifer port are all in and measured. 25 warrens per world, three stages × three occupancy states.

**The one open build item is the REGISTRATION CONTRACT** (§1 below). After that the carve is
closed and the work is dress tables.

## 1. Build this first: the registration contract

The 2026-08-12 v1 ruling stands and is the whole point: **carvers register each space's identity
at birth, so the dress phase never classifies.** Right now a pocket only carries `character`
(`plain` / `worked` / `hall`), derived from **width alone**, which is not an identity. Dressing
built against that would have to infer role from geometry — exactly the machinery the ruling
exists to avoid.

Stamp a `role` on each pocket at carve time, one of:

| role | geometry that produces it | dress table it feeds |
|---|---|---|
| **haulage** | the artery | *none — the rule is "stays clear"* |
| **face** | deepest pocket; pillared halls generally | work evidence (v1 dress pass 1) |
| **dwelling** | off-route pocket, single door | communal blocks (v1 dress pass 3) |
| **commons** | pillared hall near the mouth, **flourishing only** | gallery (v1 dress pass 2) + statues, etching |
| **block yard** | off the haulage way near the entrance | the quarry-yard recipe in `design/recipes.md` |

Each pocket also carries, alongside `role`: `live` (already built — the occupancy flag, uniform
per site today, per-space **by design**), a **depth rank** from the mouth, and the geometry it
already records (`character`, `pillars`, `spurs`, `entrances`, `through`, `in`, `box`).

**Block yard needs one extra field: a FOOTPRINT, not a box.** The chalk border is computed from
the footprint — that is precisely why it was ruled a recipe and not an `.rpm` — so it can compute
around an L or a shape clipped by its chamber. Hand the recipe a footprint so irregular yards stay
possible. Much cheaper now than after the tables exist.

**Two things the user settled that shape this, both 2026-08-18:**
- **The gallery IS the commons.** The user described v1's gallery as *"a decorative/historical
  space more than an active one"* — statues, pre-etched walls. Same role, so v1's gallery pass is
  a table already half-written for it. Do not build them as two things.
- **Claude read, accepted in direction, worth keeping because it explains the placement:** a
  commons uses pillared-hall geometry, and room-and-pillar *is* extraction geometry — so a commons
  is a worked-out face that got repurposed. First chamber cut is first exhausted, and the dig
  advanced away from it, which is why the social space sits near the mouth. That is the accretion
  axis stated literally rather than asserted.

## 2. Then: block-yard state (the user's live question, nothing built)

User, 2026-08-18: *"i think it'd be good if the recipe had more variance than my blueprints?"*

Read `design/recipes.md` § quarry-yard before proposing anything — it **already** varies row count
1–3 with a scaling border, orientation flip, per-cell art from each stage's `{base, a, b}` set,
chair at ~70%, and the ruled ascending-from-bench fill ordering so yard size tells a time story.

Three axes it does **not** vary, in the order I pitched them and the user did not object to:

1. **The yard's STATE — the big one, and it needs no new art.** The same pieces make: *fresh
   claim* (border and bench, nothing stacked — the border **is** the claim), *working* (the recipe
   as written), *full* (no room left, stock waiting on haulage), *abandoned* (stacks standing,
   bench bare), *robbed* (gaps torn where stone was taken). The last two map onto the ruled
   occupancy states, **which makes the yard the site's status board** — you read whether a warren
   is live, dead or snapjaw-held from its yard before meeting anybody.
2. **Break the rectangle** (see the footprint field above). This is what kills the stamped look.
3. **Row geometry** — runs of 2–4 rather than always 3. Cheapest, smallest payoff.

## 3. Standing agreements specific to this branch

- **The ten rules came from the user's four drawings** (`design/drawings/v2/`, decoded cell-exact,
  not eyeballed). Bucket 3 of the carries-over split **starts empty**: a piece from v1 returns
  only by being argued from the goal statement. Reasoning about carve grammar from first
  principles is the failure mode this branch was created to escape.
- **The judging bar is the user's, ruled 2026-08-18:** roll two and say what each one *is*. If two
  rolls both read as "a warren", the approach failed. `Two-up` in the toolbar is that view.
- **The sim is the design target** (ruling 2026-08-07). `ZonePainter.cs` catches up in deliberate
  port passes. Drift is expected and directional — sim leads, C# follows, never reconcile backward.
- **All player-facing text is DRAFT until the user signs off**, and the dress tables are mostly
  text. From here the deliverable is drafts for the user to strike, not systems. An agent coming
  off a build session will keep building; don't.
- Design decisions are the user's. Tag proposals as proposals; do not let a Claude proposal harden
  into a rule by being cited back later.
- No `Co-Authored-By` trailer. Don't commit unless asked.
- Corrections land in TODO the day they happen, never only in code comments.

## 4. Traps this branch has already fallen into — all four were real

**Every wrong call in the 08-18 session was a measurement whose denominator went unchecked.**

- **Instrument the middle of the pipeline, not its ends.** Loop counts read 0.7 and the crosscuts
  looked broken; the real state was **109 loops before trimming, 81 cells demolished, 0.68 left** —
  two passes fighting, invisible from every headline number. One missing cost term fixed it.
- **Corridor slack registers as navigational loops.** It inflated the loop count twice and was
  nearly reported as a win both times. Contract pockets before counting cycles, and trim slack
  before believing any loop number.
- **N4 vs N8 bit twice.** "Every site has exactly one mouth, 235/235" was true of *orthogonal*
  adjacency only — half of all sites had a diagonal slip into the canyon that the count could not
  see. Same blindness in the region's skin exclusion.
- **The world builder places landmarks by terrain-targeted fixed count, and that is invisible in
  the population tables.** Three consecutive wrong spawn analogues came from reading
  `DesertCanyonZoneGlobals` / `DesertCanyonPerSector` / the `Lairs` weight. The real answer is
  `JoppaWorldBuilder.BuildForts` → `AddMutableEncounterToTerrain("DesertCanyon", 25, …)`. **The
  user refuted the analysis from five minutes of play and was right.** Observed behaviour beats a
  reading of the source.

**Mechanical:** the lab is ~148KB with 270 tiles inlined as base64 data URIs. It is edited by
python string replacement, which is easy to get wrong silently. **Syntax-check every edit** —
extract the `<script>` block and run `node --check` before reloading. Escape sequences in
generated JS are the specific thing that broke it.

**Environmental:** the tiles are inlined *because* relative `<img>` paths cannot resolve from
`file://` inside an embedded preview (served as a `data:` URL with no base directory). That also
bought live recolouring for free — same-origin, so `getImageData` never taints the canvas. Do not
"clean up" the inline bank. Licensing was raised and closed by the user 2026-08-18: the sim is
internal-only and never ships.

## 5. Do not re-litigate these — they are ruled, held, or accepted

- **Retreat gradient: HELD, not rejected** (user: *"i'm not totally sure about it"*). It is a
  generalisation of the ruled binary occupancy. Per-pocket `live` flags are already built so it
  stays a one-function change. The depth evidence is banked as measurement, not design: **79% of
  later pockets sit further from the mouth, 9.5 cells to 26.6.** Don't re-derive it and don't
  build it.
- **~1.25 diagonal pinches per site** survive the cost term. The guarded fill is designed and
  deliberately not built — at this density it would be tidy work rather than surgery, which is the
  point, since the 08-16 "walls in front of doors" complaint scales with how much filling happens.
- **flourishing / reclaimed comes out at ~0.3 per world** — five of the nine stage×occupancy cells
  land under one per world. Flagged, accepted, not solved. The bottleneck is the stage
  distribution (flourishing is ~9% of warrens), not occupancy.
- **Contested vs taken** for reclaimed warrens: taken first is the Claude lean, unruled.
- **Loops run 1.5 / 3.5 / 4.75 by stage** and are shallower than the bare-rock figures because a
  snaking band has fewer places to close a circuit. Probably inherent to the shape.

## 6. Verification idiom

Measure before changing anything, and measure the thing you actually changed. The lab exposes
`build(seed, K, forcedStage, host)`, `DEFAULTS`, `CANYON_ZONES`, `applySpawn` and `STAGES` on the
page, so a batch run in the console is three lines. The **`World census`** button runs all 1,251
canyon zones at the ruled 25/world target and reports the full stage × occupancy grid into the box
under the map — that is the right tool for anything spawn-rate shaped, and it is deliberately
**not** in the reroll loop (a 2% gate between the user and the next warren makes the rig useless).

Start by opening the lab, rolling a dozen, hitting `Two-up`, and telling the user what you see
before changing anything.
