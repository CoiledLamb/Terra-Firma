# Next-session prompt: warren quests (D10), 2026-10-02

Copy the block below into a fresh session.

---

We're picking up **D10, overseer quests that lead to other warrens**, in Terra Firma (Caves of Qud
mod, `CoQ_TerraFirma-dev`, branch `items`). TF2 work (`TerraFirma2/`).

**Read first:** the D10 entry in `TODO.md` (warren planning docket). It holds the closed shape and
the remaining open questions. Do not re-derive it. The warren guideline note is in
`design/guidelines/warrens.md` (goal statement, "bend, closed 2026-10-02").

## Where it stands

Design talk only; no code yet. Shape closed 2026-10-02:

- Quests are rolled **upfront at worldgen**, base's way (`JoppaWorldBuilder.BuildDynamicQuests`,
  the "Generating dynamic quests" step). Our hook: `Cleo_TerraFirma_WarrenPlacement` in
  `TerraFirma2/Scripts/Warrens/WarrenWorldBuilder.cs`, which claims all 25 warrens with their stage.
- Six verbs: locate, find an item, etch a message, pray, desecrate, defend from snapjaws.
  Attacking another warren is off the list (one shared Quarriers faction).
- Givers are overseers. Overseers roll at build time (`Chance=` 15/40/75 on the WarrenCrew tables,
  `TerraFirma2/XML/PopulationTables.xml:111`), so a quest whose giver never seats never appears
  (base precedent). Measure the stray-shrine count in a real world before tuning.
- Targets may be abandoned sites. Quests can chain but don't have to.
- Quest statues are made upfront, cached, and seated by hint into one of the carver's own statue
  slots; only quest statues carry base `Shrine`.
- Figurines first as quest items, marked base's way (`VillageDynamicQuestContext.getQuestDeliveryItem`:
  palette recolor, `itemNameMutation` rename, `QuestItem`/`NeverStack`/`norestock`, `DynamicQuestTarget`).
- Rewards: our own table twisting base's (`StepXP = tier x 1000`, +100 rep, pick 1 of 3 from a
  population): MiningGear, gems/nuggets, rep from a faction shortlist.

## Base pieces to reuse (decompiled 2026-10-02)

- `FindASiteDynamicQuestManager`: finishes the locate step when the target map note is revealed or
  its zone visited. Warren notes carry the `"stonecarver"` attribute.
- `InteractWithAnObjectDynamicQuestManager` + `InteractQuestTarget`: finishes a step when the object
  whose **ID** matches receives the string event named by its `QuestEvent` property. Base `Shrine`
  fires `"Prayed"` and `"Desecrated"`; our etch code can fire its own.
- `FindASpecificItemDynamicQuestTemplate_FabricateQuestItem`: zone builder that hides a cached item
  on a `LairOwner`, else a `NamedVillager`, else in a relic chest.
- `DynamicQuestConversationHelper`: accept/reject/turn-in conversation nodes.
- Not reusable as-is: the find-a-site giver builder, typed to `VillageDynamicQuestContext` (history
  village; `IsValidQuestDestination` not virtual). Copy its shape (`fabricateFindASpecificSiteQuest`,
  including the path/direction/radius directions text) into our own builder.

## Blockers cleared 2026-10-02

- **Spice:** any mod file named `historyspice.json` is `JObject.Merge`d into base with arrays
  union-merged (`HistoryKit.HistoricSpice.Init`; also verified 2026-07-23, see
  `design/historyspice-prep.md`). Keep our quest lines in our own subtree: unioning into base keys
  leaks them into base village quests.
- **Blasphemy:** `BlasphemyPerformedEvent.Send` resolves a being through
  `Factions.FindWorshippable`, which is null unless the object has a `Worshippable` property/tag and a
  `WorshipFaction` (or primary faction) that registers that being. Null means no rep and no
  tracking: an untagged quest statue only costs the local `BroadcastForHelp`. Opting in sends the
  player through `Reputation.BlasphemyPerformed` (gated by global config `WorshipReputation`);
  the first blasphemy against a faction applies every faction's blasphemy attitude toward it, set
  in faction XML. Prayer mirrors this through `WorshipPerformedEvent`.

## Still open (none block locate)

Quest-item adjective and palette source (CLAUDE PROPOSAL: the target warren's name, not
"sacred"); snapjaw miner inherits base Snapjaw factions, so it sides with raiders on the defend
quest; whether quest statues opt into worship rep; small guard for a target whose build fails
(`WarrenMaker.SettleRumor` deletes its note; generation has run 25/25, so low priority).

## Next step

Build **locate** first: our worldgen quest roll plus a giver zone builder modeled on
`fabricateFindASpecificSiteQuest`, using `FindASiteDynamicQuestManager` unchanged. Then find-item,
etch, pray/desecrate, defend. Follow CLAUDE.md: ground truth before design, XML > C# > Harmony,
quest text is the user's (draft until signoff), wishes go in `Scripts\Dev\`.
