# Warren-facing text sitting (playtest week, compiled 2026-09-05)

Index only. Every current string below is a CLAUDE DRAFT unless it says otherwise, and the
user's words replace it verbatim. Order is the order a playtester meets the text.
Current work and completion live in [TODO.md](../TODO.md), T01; this sheet is the string inventory.
Historical context lives in [JOURNAL.md](../JOURNAL.md). Do not maintain a second completion list here.

Two kinds of edit, marked per row:
- **XML**: swap the string in place, deploy, no recompile.
- **C#**: swap the literal, compile, deploy.

## 1. Reaching a warren (C#)

| what | current string | where |
|---|---|---|
| map secret name | `a quarry warren` | `Scripts/WarrenWorldBuilder.cs:56` and `:69` (two sites, same string) |
| pass-over rumor popup | `You hear the ring of hammers on stone nearby. Would you like to investigate?` | `Scripts/WarrenWorldBuilder.cs:64` |

The journal's "Scouting canyons..." / "Raising quarry warrens..." loading lines (JOURNAL.md ~1559)
were never built; nothing in `Scripts/` carries them, so there is no string to swap.

## 2. Talking to the crew (XML, `XML/Conversations.xml`)

Banks are tilde-delimited barks in one `<text>` node, picked at random. Measured target
7-9 lines per bank (Stilt trade banks). Current lines are the user's own (2026-08-16).

| bank | lines now | file |
|---|---|---|
| `Cleo_TerraFirma_SnapjawMiner` | 2 | `Conversations.xml:32` |
| `Cleo_TerraFirma_Outrider` | 2 | `Conversations.xml:64` |
| `Cleo_TerraFirma_Mason` | 4 | `Conversations.xml:79` |
| `Cleo_TerraFirma_Hewer` | 2 | `Conversations.xml:102` |
| overseer | 0, no conversation exists | new `<conversation>` in the same file |

Overseer wiring is NOT text and is owed by Claude once the bank exists: a `ConversationScript`
part on `Cleo_TerraFirma_Overseer` (`XML/Creatures.xml:181`) and a `GivesRep` part (Snapjaw
Hero 0 idiom; nothing in the tree carries `GivesRep` today). Without both the overseer is
unclickable and the water ritual never fires. Farewell on the three carver roles is ruled
("Rust and earth."), the miner keeps "...live and drink." on purpose.

## 3. Looking at the crew (XML)

| blueprint | field | where |
|---|---|---|
| `Cleo_TerraFirma_Stonecarver` (chassis) | Description | `Creatures.xml:143` |
| `Cleo_TerraFirma_Hewer` | Description | `Creatures.xml:187` |
| `Cleo_TerraFirma_Mason` | Description | `Creatures.xml:205` |
| `Cleo_TerraFirma_Outrider` | Description | `Creatures.xml:226` |
| `Cleo_TerraFirma_Snapjaw Miner` | Description | `Creatures.xml:104` |
| `Cleo_TerraFirma_Rockhound` | Description | `Creatures.xml:34` |
| `Cleo_TerraFirma_AnimateFigurine` | Description | `Creatures.xml:279` |
| Quarriers' Guild | faction Description, nothing drafted on purpose | `Factions.xml` (comment at :16) |

Mole variants inherit their role's description; nothing to write there.

## 4. Looking at the site (XML, `XML/ObjectBlueprints.xml` unless noted)

| blueprint | field | current | where |
|---|---|---|---|
| `Cleo_TerraFirma_Yard Board` | ArrivedLine tag | `arrived the =day= of =month=` | `:230` |
| same | DepartedLine tag | `departed the =day= of =month=` | `:231` |
| same | TallyLine tag | `=blocks= blocks raised` | `:232` |
| `Cleo_TerraFirma_QuarryStones1/2/3` | DisplayName | `stack of quarried stone` (all three) | `:158` `:164` `:168` |
| same | Description | ABSENT, held for this sitting | add a `Description` part to `QuarryStones1`; 2 and 3 inherit |
| `Cleo_TerraFirma_MossClump` | DisplayName | `clump of {{K\|cave}}{{green\|moss}}` (user format, ruled) | `:151` |
| same | Description | ABSENT | add a `Description` part |
| mossy wall adjective | `{{green\|mossy}}` | user-ruled word | `Scripts/Part_MossyWall.cs:18` (C#, only if rewording) |
| `Cleo_TerraFirma_FireDish` | Description | `A wide basin of dressed stone cradles a bed of embers.` | `:106` |
| `Cleo_TerraFirma_FireDishCold` | Description | `A wide basin of dressed stone cradles a bed of cold ash.` | `:131` |
| `Cleo_TerraFirma_Quarry Chest` | DisplayName | `quarry chest` | `:247` |
| `Cleo_TerraFirma_AnimationTable` | Description | `placeholder description >:)` | `XML/Objects.xml:224` |
| `Cleo_TerraFirma_ChalkLine` | Description | `A stroke of white lime lies across the earth.` | `XML/Objects.xml:240` |
| chalkboard look-text frame | `Chalk is scrawled across the surface. It reads:` | base graffiti line, one word swapped | `Scripts/Part_ChalkboardWritable.cs:172` (C#) |

Cairn and toppled-cairn descriptions carry no DRAFT mark and are left alone here.

## 5. Things that happen while you are there (C#)

| beat | current | where |
|---|---|---|
| stepping on a cairn | `XDidY(stepper, "topple", ...)` reads "X topples the cairn!" | `Scripts/Part_Cairn.cs:79` |
| your boot smears a chalk line | `Your steps smear the chalk line.` | `Scripts/Part_ChalkLine.cs:84` |
| someone else's boot | `<walker> smears the chalk line underfoot.` | `Scripts/Part_ChalkLine.cs:88` |
| deliberate smear | `You smear the chalk line away.` | `Scripts/Part_ChalkLine.cs:122` |
| rockhound eats a stone | verb `crunch` | `Scripts/Part_RockSweeper.cs:171` |
| writing on a board, prompt | `The slate waits under a film of old dust.` | `Scripts/Part_ChalkboardWritable.cs:93` |
| board write options | `Write a message.` / `Write from memory.` | `Scripts/Part_ChalkboardWritable.cs:94` |
| board write popup | `You chalk "..." onto <board>.` | `Scripts/Part_ChalkboardWritable.cs:132` |

## Not on this sheet

The item-lane message strings (box of chalk ability text, lantern load/unload/burnout,
grinder feed/finish/stop, cricket perish, glowsphere break) are draft but functional and
ride the release text pass; `design/writing-sittings-1.0.md` still indexes them. Rock
catalog and Lithovore text are quarantined. Tombstone epitaphs belong to the unbuilt
graveyard room.

## Record correction

The 2026-09-05 status read claimed `warren-port` held five unmerged commits. It does not:
`warren-port` is behind `items` (154 commits), and its five unique hashes (moss, fire dish
VFX, pass-over encounter, lighting rebalance) all reached `items` by other paths, verified
by the strings above existing in the `items` tree. Nothing on that branch is owed.
