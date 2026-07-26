# HistorySpice sitting — prep sheet (compiled 2026-07-24)

Everything technical is done; what remains is the writing sitting. This sheet is the scaffolding
so the sitting is pure phrase-drafting. Merge path VERIFIED 2026-07-23 (TODO.md has the decomp
record): any mod file named `historyspice.json` union-merges natively; the dish-namer's lookup
key is the `AggregatedIngredient` tag value; entry shape = `adj` / `adj1` / `adj2` / `noun` /
`nounPlural` phrase lists under `spice.cooking.recipeNames.ingredients`.

## The key list (pulled from Rocks.xml 2026-07-24 — 36 unique keys)

Shared keys (deliberate merges): **FungalRock** (all four fungal grounds — no lottery tell in
dish names), **GlowingRock** (both twins — the gamble survives into the pot).

Sitting mandate (user, 2026-07-24, from the per-rock blessing): "i was looking forwards to the
history spice for each rock feeling a bit unique" — write for per-rock distinctiveness; the two
shared keys above stay the only merges.

| Group | Keys |
|---|---|
| Liquids & food | BrineGeode · SaltChunk · Sludgabbro · Oilstone · Mullstone |
| CC / mental | Grimslate · Vertigneiss · Lullabaster · Hushmica · Dotagate |
| Combat / hazard | Urchinite · Pyroclase · Frostspar · Vitriolite · Galvena · Streakschorl |
| Utility / defense | Farflint · Gossamarl · Tanglelith · Aegischist · Phasalt · Healstone · Siphonyx · Euclase · Conchalk · Caromcobble |
| Weird | StrangeRock · Manymarble · Vantabsidian · Gyrestone · GlowingRock (shared) · FungalRock (shared) |
| Deep four | DeepGalvena · DeepPyroclase · DeepFrostspar · DeepVitriolite |

Not on the list, decide at the sitting:
- **StoneMeal** (the fallback flour): one blueprint covers every domainless grind — needs either
  an entry under its blueprint name or its own AggregatedIngredient tag. Known cosmetic wart
  recorded in TODO.md (stamped powders name as generic stone meal).
- **Dazestone**: ~~no ground twin yet by design~~ **CUT 2026-07-25 (intervention resolved by
  subtraction; blueprint dormant). No key, no twin, edge case closed.**
- **Compressed ash**: ~~if built~~ **BUILT 2026-07-25** — `Ground_CompressedAsh` is live with
  key **CompressedAsh** (the armorMinor domain). The key list is now 37; it needs a spice
  entry at the writing sitting like the rest.

## Entry shape (base-game example for register)

Base entries are short lists of evocative culinary fragments; the namer slots them into
generated dish names ("&lt;adj&gt; &lt;noun&gt; of ...", "... with &lt;nounPlural&gt;"). Keep
fragments lowercase, food-register, no full sentences.

## Seed examples (Claude DRAFTS — register samples to react against, not commitments)

```json
"Galvena": {
  "adj": ["galvanic", "crackling", "charged"],
  "noun": ["galvena crumble", "live-current grit"],
  "nounPlural": ["galvena shavings", "charged grits"]
},
"BrineGeode": {
  "adj": ["briny", "sea-hearted", "geode-cured"],
  "noun": ["brine crystal", "salt heart"],
  "nounPlural": ["brine crystals", "geode salts"]
},
"FungalRock": {
  "adj": ["sporulent", "mycelial", "gilled"],
  "noun": ["fungal grit", "spore meal"],
  "nounPlural": ["fungal grindings", "spore meals"]
}
```

Sitting shape: ~36 entries × 3 lists × 2–3 phrases ≈ 250 short fragments, in your voice; the
Carbide Chef `kitchenrock` wish previews Inspired names live.
