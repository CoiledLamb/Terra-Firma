# Recipe ledger — sketches received and encoded

User-authored sketches (TFMAP verbatim) + the extracted spec each one rules. The painter builds
from the SPEC; the TFMAP is the authoritative reference drawing. Started 2026-07-26.

---

## hookah-circle (cluster, 3×3) — user sketch 2026-07-26

```
TFMAP v1 (excerpt)
legend {"o":"cushion","H":"hookah"}
 o
oHo
 o
```

**Spec:** hookah fixed at center; four cushion slots at N/E/S/W as a **choose-k group**, k = 2–4
(sketch shows the maximum state). Slots landing on wall/occupied cells are excluded *before* the
draw — a wall-adjacent hookah fans its cushions on the open sides. Never zero cushions (count-first,
not per-slot chance — a bare hookah reads wrong).

**Consequences:** replaces DressCommunal's random cushion scatter (cushions orbit the hookah, they
don't scatter). Establishes the **choose-k-of-group slot mode** for all future clusters.

---

## quarry-yard (exterior recipe, drape) — user sketch 2026-07-26

```
TFMAP v1 36x18 (content excerpt)
legend {"|":"chalkline","Q":"bg.workbench","=":"stack1","q":"stack2","!":"stack3","k":"bg.canvas_folding_chair","p":"bg.woven_basket"}
|||||||||
|Q = q !|
|k = q !|
|  = q !|
|p      |
|||||||||
```

**Spec:**
- rows = roll **1–3**; footprint = (3 + 2·rows) × 6; horizontal variant = transpose
- **chalk line: full 1-cell rectangle border, computed from footprint** (why this is a recipe,
  not an .rpm — the border scales with the roll)
- work column on the **road side** (faces mouth/corridor): workbench fixed (top), chair ~70%
  chance below it, woven basket fixed at bottom corner **with loot table** (loose stones; rare
  cracked viewing stone as an apprentice's discard)
- each stack row = vertical run of 3 stack tiles, **stage-uniform but art-varied** (2026-07-26
  amendment: user added quarrystones1a/1b–3a/3b — each cell in a row rolls its art from that
  stage's variant set {base, a, b}, so rows keep batch identity without clone-stamping)
- row fill-stage ordering **RULED 2026-07-27 (user): ascending-from-bench** — filling happens near
  the bench, finished stock migrates outward; a 1-row yard shows one partial row ("just getting
  started"), full pallets only appear in bigger yards. Yard size tells a time story.
- user floated .rpm chunk; ruled recipe instead (row-count roll + orientation flip + scaling
  chalk border = parameterized territory; the ROW is the rigid stencil)

**Reuse:** works standalone as a wilderness work-site POI (chalk rect + 1 row + bench) — carver
presence without carvers.
