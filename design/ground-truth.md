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

## Ability-card templates: Duration defaults to rounds, and `<switch>` branches on a stat

- checked: 2026-09-30, decompile of `XRL.Templates` (`StatNode.DefaultUnits`, `SwitchNode`, `StatCollector.Render`)
- `Cooldown` and `Duration` get the unit "round" automatically (pluralized for integers, appended raw to text values). A statline or stat whose key was never set renders the key name in gray.
- `<switch Name="X"><case Value="...">...</case><default>...</default></switch>` picks text by the stat's string value (base uses it on the PowerSkate card).
- consequence for the mod: the Stoneshaper card switches on Duration's "until destroyed"; Fissure's statlines must match the keys `CollectStats` sets.

## Cooked meals never pair one ingredient type with itself

- checked: 2026-09-30, decompile of `XRL.World.Parts.Campfire` (type list built with `!list2.Contains(type)`, then `GenerateEffectFromTypeList` permutes it) and `ProceduralCookingEffect` (Duration 1, counted down only for non-players)
- The trigger, the action and the three-part base unit all come from different ingredient types. For the player, a meal effect lasts until the next meal.
- Base triggers on repeatable events carry a chance in their text (dealing cold/electric damage 25%, fire 10%, reflect and stuck 50%); rare events fire every time.
- consequence for the mod: the cave moss trigger can never feed its own pebble action; it rolls 50% because Earthen Barrage keeps stones coming for whatever action it pairs with.

## Mod dependency ranges and in-mod load order

- checked: 2026-09-30, decompile of `XRL.ModInfo.ResolveDependencies`, `XRL.Version.EqualsSemantic`, `XRL.World.Loaders.ObjectBlueprintLoader.ReadObjectsNode`
- Manifest dependency values accept `*`/`x`, `^`, `>=`/`>`/`<=`/`<`, `a - b` and `||`. A mismatch logs "[Required] ... MISSING" and marks the mod as missing its dependency.
- A `Load="Merge"` object whose target has not loaded yet is discarded with "unknown blueprint, node discarded".
- consequence for the mod: TF2 depends on `>=0.2.0`; the fused testing build concatenates same-named XML files TF1-first.

## Small engine behaviors used by the 2026-09-29 audit fixes

- checked: 2026-09-29/30, decompile
- `GameObject.Fail(msg)` shows `Popup.ShowFail` only for the player and returns false.
- Melee hits pass their weapon into the damage events (`Combat`, the "TakeDamage" event's Weapon parameter; `TookDamageEvent.Weapon`). Not individually checked: that breath, blast and quill-retaliation damage leave Weapon null.
- Base `Creature` carries `<tag Name="Creature" />`, inherited by every creature; animated items get `IsCreature` from a property on a non-creature blueprint.

## Saved parts are read field by field, in order, with no end check

- checked: 2026-09-30, decompile of `IComponent<T>.Read`/`Write` (default path walks `GetCachedFields()`: public instance fields, declaration order, skipping `[NonSerialized]`), `IPart.Load` and `Effect.Load` (each part is a length-prefixed block, but a successful `LoadData` never seeks to the block's end), `SerializationReader.SkipBlock` and `XRLGame` (`Popup.DisplayLoadError` when `Reader.Errors > 0`).
- A class missing at load: `ReadTokenizedType` throws, `SkipBlock` skips the part and counts an error, and the load-error popup shows. A class with **more** saved fields than the save holds reads past its block into the next part and corrupts it (seen 2026-09-30: a 0.1.2.5 statue's added `StoneshaperStatueStatsPart` fields broke the next part, Wings, and the statue was lost). Fewer fields leave the stream mid-block.
- `IComponent.ReadError(Exception, Reader, Start, Length)` runs before `SkipBlock`; returning true after `Reader.Stream.Position = Start + Length` skips the part without counting an error (the stub pattern in `SaveCompatStubs.cs`).
- The level cap is applied after food and gear bonuses (`BaseMutation.CalcLevel`), so a cooked mutation boost does nothing at the cap.
- consequence for the mod: once a class has shipped, its saved public fields are frozen. New per-object data goes in `[NonSerialized]` fields backed by object string properties, or the class ships a stub. Compare every shipped class's field list against the staged build before a release.
