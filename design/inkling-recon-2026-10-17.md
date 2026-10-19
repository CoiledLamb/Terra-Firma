# inkling: recon (2026-10-17)

Source: `Assembly-CSharp.dll` build reference copy (2026-10-12 snapshot), decompiled with ilspycmd 9.1;
base `ObjectBlueprints/Creatures.xml`. Concept and dials are Cleo's (creatures thread, 2026-10-17).

## The concept

Small, very weak prey animal of early cave areas. Leaves a puddle of ink when killed. Behaviors:
- scared (base impulse): hides in a wall as graffiti
- likes you / petted: hides on your body as a tattoo
- wants: eats your tattoos, removing or garbling them (the Mark of Death included, no guard; user 2026-10-17)

## Ink on death
Base urchins already do it: `BaseUrchin` carries `<tag Name="BleedLiquid" Value="ink-1000" />`
(`Creatures.xml:611`); `Ink` is a base liquid (`Liquids.xml:543`). XML only.

## Hiding in walls
- Entering a solid cell is rejected unless the mover's `Brain.WallWalker` is set
  (`Physics.HandleEvent(ObjectEnteringCellEvent)`). The `WallWalker` mutation only sets that flag.
- `AIWallWalker.WalkToNearbyWall` (livid creeper, jilted lover) picks an adjacent wall that touches a
  passable cell and `DirectMoveTo`s into it. That is the hide move almost verbatim.
- Shape: normal walker; when scared, set `Brain.WallWalker`, step into the nearest wall; to leave, step to an
  adjacent floor cell and clear the flag. While the flag is set its pathing uses walls only, so a spooked
  inkling could creep along the wall (open: feature or tell).
- No stash and no phasing. Phasing was rejected: a phased creature can't touch or be touched and renders in
  phase colors.

## Graffiti disguise
- Base `Mimic` never edits the Render part: it copies an adjacent object's color/tile/glyph into its own
  fields and applies them in `Render(RenderEvent)`, so clearing the fields restores the creature. It also caps
  `GetHostilityRecognitionLimitsEvent` distance (3 when copying color). It does not touch name or description.
- Base `Graffitied` adds the "graffitied" adjective via `GetDisplayNameEvent` and appends "Graffiti is scrawled
  across the surface. It reads: ..." via `GetShortDescriptionEvent`.
- Shape: Mimic-style render override copying the wall in its *own* cell; Graffitied-style name; the graffiti
  line followed by the creature's description (user 2026-10-17; text is Cleo's).

## Tattoos
- `XRL.World.Parts.Tattoos`: a tattoo is a string per body part in public `Descriptions`
  (`Dictionary<int, List<string>>`), capped by `RuleSettings.MAXIMUM_TATTOOS_PER_BODY_PART`.
  `ApplyTattoo(Subject, CanTattoo, CanEngrave, Desc, Color, Detail)` picks a random tattooable part; a color
  tints the whole creature (`Tattoos.Render`).
- Base `RemoveTattoo(BodyPartID)` wipes every tattoo on that part; eating one tattoo edits the list directly.
- Mark of Death: `GameObject.FindMarkOfDeath` scans tattoo text for the `MarkOfDeath` game state string. Eating
  it removes the mark. Re-tattooing it with the tattoo gun is presumably the way back (inferred, unchecked).

## Petting
`Pettable.Pet` sends `AfterPetEvent` after a successful pet; `OnlyAllowIfLiked`, `PettableIfPositiveFeeling`.

## Riding as a tattoo: holding the whole creature
- Companions carry inventory, skills and levels, so the creature itself is held, not rebuilt.
- Base `LocalCache` holds live `GameObject`s off the map with their x/y and saves them in its own `Write`/`Read`
  via `Writer.WriteGameObject`; `Stopsvaalinn` does the same. The non-reference `WriteGameObject`
  (`SerializationWriter.cs:2382`) queues the whole object into the save.
- Shape: a part on the carrier holds the inkling object plus the tattoo line; on hop-off it returns to an
  adjacent cell. Prove with the serialization harness.

## Spawn and factions (closed 2026-10-17)
- Base has no cave DynamicObjectsTable. Biome cave populations (e.g. `DesertCanyonCavePopulation`) roll creatures from
  the shared `Tier2CaveCreatures` (`PopulationTables.xml:8366`); `Tier5CaveCreatures` is the next step up. One merge
  entry there covers low-level caves everywhere.
- Grazing hedonists is faction id `Prey` (`Factions.xml:73`), on 22 base creatures. Newly Sentient Beings has 5 with it
  as sole faction. Dual factions are a base idiom (`Factions="Prey-100,Insects-25"`).

## Art and text (2026-10-17)
Sprite `TerraFirma2/Textures/inkling.png`. Description (user): "Pitch spilled forth from the primordial brush, and congealed into a skittish, trembling creature."

## Factions weight (closed 2026-10-17)
`Prey-100,Newly Sentient Beings-50`. Feeling is the weighted average of faction feelings
(`AllegianceSet.GetBaseFeeling`), so NSB is a third of the vote; `GivesRep.GetLovedFactions` counts membership 25+.
