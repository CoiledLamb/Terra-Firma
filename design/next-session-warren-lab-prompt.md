# Next-session prompt: warren lab v2 improvements (Monday 2026-08-11)

New track — does not supersede any existing next-session prompt. The fresh-slate warren work
in TODO ("FRESH-SLATE RESET, 2026-08-08") now has a dedicated rig; this hands it off.
Copy-paste the block below into a fresh session.

**Unrecorded corrections warning:** everything in section 0 currently lives ONLY in
`design/warren-lab.html` code comments. By my own standing lesson (corrections land in TODO
the DAY they happen, never only in code comments) these owe a TODO entry. That is task 1.

---

We're improving the warren procgen lab for Terra Firma (the Caves of Qud mod in
`CoQ_TerraFirma-dev` — TODO.md there is the source of truth). The rig is
`design/warren-lab.html`, a standalone carve-focused fork of `design/tile-sketch.html` built
2026-08-11. Open it in a browser; no build step. It writes its own localStorage keys
(`tfwarren*`), so tile-sketch's saved canvases are untouched. Read the fresh-slate reset entry
and the calibration-round entries in TODO before touching the generator — ten hand-drawings
of mine are encoded in those numbers and I don't want them re-litigated by accident.

**0. What the lab already is, so you don't rebuild it.**
- **46 knobs, 7 groups**, right-hand panel — every number the warren and canyon generators used
  to hardcode. Live reroll on change; "Reset all" returns to the 2026-08-08 tuned baseline
  (verified byte-identical); "Copy diff" writes only the changed knobs to the text box.
  Part-table weights are real weights now, not a cumulative `R(1,100)` if-chain.
- **Roll history** — rolls live in their own list that navigation never truncates, fixing the
  v1 bug where any brush stroke wiped the redo branch and a roll you'd flipped past was gone.
  `◀ 7/12 ▶`, clickable tick strip, `[`/`]` or Alt+arrows, Space rerolls, K pins to the canvas
  library. Each entry stores its seed AND the knob values it rolled under, so landing on a roll
  you like gives you a reproducible roll, not just a picture. Painting edits the roll in place.
- **Canyon**, ported from the decompiled `CanyonBuilder` — see section 0b.
- **Vocabulary gallery** (second view) — 23 cards, one per primitive, each rendered by running
  the generator's OWN carve function on a scratch grid, so a card cannot drift from the code.
  Role tags filter; that's the overlap view. Per-card reference slot takes a dropped Aseprite
  export or screenshot, stored locally, persistent.
- **Qud tiles** toggle (default on) — walls drawn with the real extracted sprites, masked off
  their neighbours exactly as `ZoneManager.PaintWall` does; floors rolling one of four
  `sw_ground_dots` variants. Kept walls (standing cores, the phase-2 slots) get an amber tint,
  because painted rock makes a deliberate pillar look identical to untouched mass.
- Legacy camp-chain buttons (Zone/Quarry/Face/Gallery/Bore/Sprawl/Comb/Ranks/Digger) are still
  there behind a collapsed "legacy track" disclosure, for comparison.

**0b. Ground truth corrected 2026-08-11 (re-decompiled against the current Steam install).**
These change generated shape and are NOT yet in TODO:
- **The Drillbot's noise is not a smooth cost field.** `AddNoise` builds
  `NoiseMap(80,25,10, 3,3, 4, 80,80, 6, 3,-3, 1)` and then sets `weight = 98` for every cell
  whose noise exceeds 1 — a FLAT 98 over ~76% of the zone, with sub-cutoff cells keeping their
  terrain weight (solid wall, i.e. real obstacles). So a canyon is a shortest path through a
  field of ~21% blob obstacles: long clean runs that bend hard around blobs. The 2026-08-07
  reconstruction's `1 + white*6` produced fine-grained wobble that does not occur in game.
- **Stairs count as connections.** CanyonBuilder paths to every Stairs connection as well as
  every Canyon one, and those endpoints are interior — real canyons grow spurs to a staircase
  in the middle of the zone. The sim never modelled this.
- **The 4×4 hub clearing only happens on the Start branch**, and with a Canyon Start connection
  the hub sits AT that connection (an edge cell), not at a random interior point.
- **`GetLocalAdjacentCells(r)` is a filled Chebyshev box clamped to 0..W-1 / 0..H-1** — widening
  reaches the zone edge, it does not stop at the border ring.
- `Stat.Random(1, high)` rolled once per path cell: confirmed, unchanged.
- **`PaintWall` mask order is N NE E SE S SW W NW**, off-map neighbours read 0 via `bEdgeFlag`.
  A neighbour connects on same-blueprint, same `PaintWith`, `PaintWith == "*"`, or having the
  `PaintPart` part. Same convention as `Fx_StoneRise.ComposeTile` / `ISOLATED_MASK`.
- **All 256 `wall_rock` masks are stocked.** Counted in the tile index. No carve shape we can
  produce will hit the blank-`InvalidInfo` path that `Fx_StoneRise`'s comment warns about.
- **Village hut floors: `DirtPath` on every interior cell**, hardcoded in both `VillageMaker`
  and `VillageBase` for every faction — goatfolk get no distinctive floor, their identity is
  `ThatchedWall` + `RoundBuildings` + the population table. `DirtPath` inherits `DirtFloor`,
  which carries `RandomTileOnMove` over four `sw_ground_dots` variants. **So Qud's own idiom is:
  walls autotile off neighbours, floors roll one of N.** Relevant to any floor work we do.

**0c. Three bugs found and fixed 2026-08-11 — two of them are in the SIM's design record.**
- **The mini comb had never once fired in a warren.** `carveComb` requires its entire footprint
  to be solid wall, but the call site placed that footprint centred on `(ex,ey)` — the cell the
  hall had just carved — so the wall check failed on the hall itself and it returned null every
  time. Twenty warrens with the comb as the ONLY part in the table produced zero combs. Now
  placed ahead of the hall end like the suite; fires in about one warren in three.
  **This bug is also live in `tile-sketch.html` (identical call, line 1208).**
- **66% of rank units in a warren were sealed.** `carveRanks` reports whether its lane reached
  open floor and `growWarren` was ignoring the flag — measured over 15 unit-only warrens, 57 of
  86 accepted units were ones carveRanks had itself logged UNATTACHED. Now reverts, matching the
  comb. Cost: units drop from ~6 per warren to ~1. **Any judgement I made about ranks before
  2026-08-11 was made looking at two-thirds broken output.**
- The gallery's ring-unit demo had its lane and depth axes parallel, so it drew a bar instead of
  a unit. Gallery-only, no generator impact.

**1. Record section 0b and 0c in TODO.** Before any new work. Corrections belong in the tracker
the day they happen; these are three days late already because the lab was built in one sitting.
Tag them ground truth (decompiled), not proposal.

**2. Ranks — I rule, you build.** My standing complaint is "the filled spaces and non-linking
aspect make them feel weird." The non-linking half was the bug above and is fixed. The filled
half is a live design question and the recorded CLAUDE PROPOSAL is: the rank unit is the only
piece in the vocabulary whose interior is not space — everything else carves a room, the unit
carves a ring and keeps the middle, and in stage 1 with nothing decorated a kept core is just a
lump. It may be a phase-3 primitive being judged in phase 1 (the core was designed as a slot).
Against that: we already have three standing-core shapes (mini wrap, island notch, comb
pillar-rooms) and ranks are the fourth; what they uniquely add over the wrap is repetition at
regular spacing, but trunks already carry the planned register and the suite carries packed
domestic architecture. Three options, mine to pick — **ask me, do not choose**:
(a) set `ring unit` weight to 0, roll twenty warrens, see if I miss it — free and reversible,
recommended first; (b) hollow the core into a doored chamber, spending the phase-2 slot;
(c) leave rare and re-judge now the sealed ones are gone. Use the gallery's `pillar` filter when
I judge — the five standing-core pieces line up side by side there.

**3. Stage 2 — the warren in a canyon.** This is the actual next stage of the fresh-slate reset
(stage 1 blank slate DELIVERED, stage 2 in canyon, stage 3 painted). Now that the canyon is
faithful, constrain `growWarren` by real bands and mouths instead of a blank 80×25. Propose the
attachment rule before writing it — do not assume the legacy mouth-finding heuristic carries
over, and check whether `CanyonMarker` objects survive to our post-builder (the 2026-08-07
survey flagged this as VERIFY-at-build-time and it is still unverified).

**4. Gallery text is placeholder and I want my own voice.** Every card's "For:" and "Adds:"
line is structural description written to make the cards usable, explicitly not final. There is
a banner on the view saying so. Hand me the cards a few at a time and I'll re-voice them; swap
my lines in verbatim, no polishing. Same standing rule as all player-facing text.

**5. Lab improvements worth having, roughly in the order I'd want them.**
- **Contact sheet** — roll N warrens (20–30) as a thumbnail grid in one click. The question
  "does the language hold over thirty rolls" is the one I actually judge on, and flipping the
  roll history one at a time is the slow way to answer it.
- **Export a roll with its knobs** — TFMAP export currently drops the knob values and the seed,
  so a roll pasted into chat can't be reproduced. The history entry already carries both.
- **Two wall materials that don't fuse** — quarry brick against native shale, using the real
  `PaintWith` equality rule rather than "is wall". About five lines in `wallMask`, and it's the
  thing that would show whether a built wall reads as built.
- Knob panel wraps below the grid under ~1100px; it wants to be a collapsible sidebar.
- Sprite-coverage check for other wall sets — `wall_rock` is fully stocked at 256, nothing else
  has been counted, and `extract-qud-walls.py` takes any set name.

**6. Asset extraction, if you need more tiles.** `design/extract-qud-walls.py`. Not a
one-liner and the script exists so nobody re-derives it: Qud's tiles are packed into runtime
atlas pages (`Kobold_DynamicAtlas_Walls*`) and the name→rect index is ~27k `exTextureInfo`
MonoBehaviours whose Unity type trees are stripped from the release build. The way through is
regenerating type trees from `Assembly-CSharp.dll` via UnityPy's `TypeTreeGenerator` (works
because it's a Mono build), deserialising the index with those, then cropping each rect out of
the atlas. `python extract-qud-walls.py wall_brick wall_metal` for another set.
- Sprites are two-tone masks and the tint is BAKED IN, because the lab runs from `file://` where
  recolouring on a canvas taints it and `getImageData` throws. Re-run with
  `--body '#8c3f2a' --detail '#b09070'` to reskin.
- **UNVERIFIED (Claude inference, not ground truth):** I read opaque black as the body
  (TileColor) and white as the detail (DetailColor), because black is ~78% of pixels and shale
  should read mostly red. Not confirmed in the shader. `--swap` flips it if it looks inverted
  against the real game.
- `design/tiles/walls/` is **gitignored** — those are Freehold Games' assets, fine as a local
  design aid, not ours to commit. The repo carries the recipe, not the art. The lab probes one
  sprite at startup and disables the Qud-tiles toggle with a pointer to the script if the folder
  is empty, so a fresh clone degrades instead of breaking.

**Working agreements (standing):**
- The SIM IS THE DESIGN TARGET (my ruling 2026-08-07). Design happens in the lab at reroll
  speed; `ZonePainter.cs` catches up in deliberate port passes. Drift is expected and
  directional — sim leads, C# follows, never reconcile backward.
- Design decisions are mine. Tag proposals as proposals with status markers; do not let a
  Claude proposal harden into a "rule" by being cited back at me later.
- All player-facing text is DRAFT until I sign off.
- No Co-Authored-By trailer on commits. Don't commit unless I ask.
- Corrections land in TODO the day they happen, never only in code comments.
- The lab is a design tool, not shipped code — it does not need the TF-VERIFY treatment, and
  nothing in `design/` goes through `publish-beta.ps1`.

Start by reading the fresh-slate reset and calibration-round entries in TODO, then open
`warren-lab.html`, roll a dozen warrens, and tell me what you see before you change anything.
Then ask me for the ranks ruling.
