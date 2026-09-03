# Ground truth

Facts of the engine or the code. True or false, settled by the decompile or the shipped Base XML,
never by a ruling. Stale when the game patches, so each carries the date it was checked.
The archive holds older ground-truth records inline; this file collects the ones that were
wearing a ruling tag when the trackers were triaged on 2026-09-02.

## 1-cost and defect mutations do not level

- checked: 2026-08-06 (user statement; matches base mutation behavior)
- source: `TODO.md:458`
- consequence for the mod: Geomancy is exempt from the every-level guideline by design.

## The classic font indexes its glyph atlas by raw Unicode codepoint

- checked: 2026-08-07, screenshot-confirmed. `Hûskévos` drew as `H√sk0vos`: û (251) drew √, é (233) drew Θ. The CP437 slots those glyphs occupy are untypeable C1 controls.
- source: `TODO-archive.md:929`
- consequence for the mod: DisplayNames must be plain ASCII. That is a constraint, held in CLAUDE.md and checked by the text-rules hook and scanner.
