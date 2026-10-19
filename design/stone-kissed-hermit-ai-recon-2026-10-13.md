# stone-kissed hermit: AI recon (2026-10-13)

Source: `Assembly-CSharp.dll` from `CoQ_TerraFirma-dev/bin/Debug/netstandard2.0` (the build reference copy),
decompiled with ilspycmd 9.1; base `ObjectBlueprints/Creatures.xml`; TF `Mutation_GorgonsGaze.cs`.

## The ask

Start neutral. Move away from the player inside some range. If the player gets closer, turn hostile and
start petrifying.

## What base already does

### CryptSitterBehavior: the closest precedent (neutral until alerted, then hostile, then calms down)
`XRL.World.Parts.CryptSitterBehavior`, used by `Crypt Sitter Special`
(`<part Name="Brain" Hostile="false" Wanders="false" Feelings="All:0" />`).
- `HandleEvent(GetFeelingEvent)`: while `!Alerted`, forces `E.Feeling = 0`. Nobody reads as hostile and
  faction rep doesn't matter.
- `Alert()`: `Alerted = true; Brain.Allegiance.Hostile = true;` recolors the detail and runs `DidX("stir")`.
  Triggered by `TookDamageEvent` or the `AICryptHelpBroadcast` event.
- `Unalert()`: the reverse. It fires when the sitter is more than 10 path cells from `Brain.StartingCell`, then
  `MoveTo(home)`.
- While unalerted and away from home, it walks back to `StartingCell`. At home it burns its turn (`UseEnergy(1000)`).

Swap the alert trigger from "took damage" to "player within N" and this is most of the hermit. The
"go home" piece also suits a hermit who lives inside a statue ring.

### Flee goal: the avoidance primitive
`XRL.World.AI.GoalHandlers.Flee(GameObject Target, int Duration, bool Panicked, bool LocalOnly)`, public.
`AITryKeepDistance` pushes `new Flee(target, Stat.Roll(Duration), Panicked: false, LocalOnly)`.

### Why the stock keep-distance parts don't fit
- `AITryKeepDistance` (`AttackThreshold`, `Duration`, `NotStuck`, `LocalOnly`) only acts on `AICantAttackRange`
  for its current `Target`. A neutral creature has no target, so it never fires.
- `AITryKeepSteadyDistance` (`Distance`) is the same family: hostile kiting.
- `Brain.MinKillRadius/MaxKillRadius` (defaults 5/15) set how far a hostile brain looks for prospective
  targets (`FindProspectiveTarget`). That's a hostile-acquisition radius, not neutral avoidance.

Conclusion: there's no XML-only route to "neutral but backs away". The architecture ladder lands on one small C# part.

### Base hermits for reference
`Hide-Sheathed Hermit`: `Brain Hostile="false" Factions="Hermits-100"`, Jungle + Hills tables.
`Scrap-clad Hermit`: `Factions="Hermits-100"`. Neither has special AI.

## TF gorgon's gaze as an NPC weapon (already wired)
`Mutation_GorgonsGaze.cs`:
- `:127` `AIGetOffensiveAbilityListEvent`: adds the gaze when it has no current target, the target is in range,
  the ability is usable, and the target isn't already petrifying. There's no ally check, which is fine since
  statues aren't creatures.
- `:238` `GetRange` = 80 ("sight").
- `:137` `BeforeTakeActionEvent`: while channeling, an NPC caster `PassTurn()`s every action. **It stands still
  for the whole gaze.**

Consequences for the design:
1. Once hostile, the hermit can gaze from anywhere it can see you. The aggro range only decides *when*,
   not *from where*. Stepping back out of the aggro radius won't break the gaze (zone exit does).
2. While gazing the hermit doesn't flee, so "backs away, then plants its feet and stares" falls out
   for free.

## Codex PROPOSAL: shape of the part (not built)

`Cleo_TerraFirma_StoneKissedRecluse` (name TBD), modeled on CryptSitterBehavior:
- XML dials: `AvoidRadius` (e.g. 8), `AlertRadius` (e.g. 3), `FleeDuration` ("1d3"), `CalmDistance`
  (path cells from home before unalerting, base uses 10).
- `GetFeelingEvent`: Feeling 0 while unalerted (CryptSitter idiom).
- `BeginTakeActionEvent`: if unalerted and the player is visible within `AvoidRadius`, push
  `Flee(player, roll, Panicked:false, LocalOnly:true)`. If within `AlertRadius`, `Alert()`.
- `Alert()`: `Allegiance.Hostile = true`, a stir message. The gaze AI takes over from there.
- Unalert: when far from home or the player is out of sight for X turns, back to neutral and `MoveTo(home)`.

## Open questions
- Ranges: what avoid and alert radii?
- Is aggro one-way, or does the hermit calm down and go home (CryptSitter style)?
- Does Hermits faction rep matter? The CryptSitter feeling override ignores rep. Alternatively, the override
  could apply only when the player isn't loved by Hermits.
- Should getting hit also alert it (CryptSitter does)? Probably yes, or ranged players can pick it off freely.
- Mountain terrain is cramped, so fleeing gets cornered fast. That may be a feature.

## Aside for the salturan
Base has `XRL.World.Parts.Mimic` (`Mimic`, `Horned Chameleon`). It copies the look of what it's standing on
and caps hostility recognition distance (`MaxHostilityRecognitionDistanceMimickingColor` = 3,
`...MimickingShape`). That's a likely precedent for the rock disguise. Not read in depth yet.

## Follow-up 2026-10-13: user's first numbers (thinking out loud, not closed)

- Back away at distance <= 4 (9x9), aggro at distance <= 2 (5x5). Revised by user 2026-10-13 from 7x7 / 3x3.
- Calms down when the player backs off, unless the player engaged it in combat. Hook: `TookDamageEvent`
  (CryptSitter's own alert trigger) sets a sticky `Provoked` flag that blocks calming. Misses don't deal damage,
  so they wouldn't count.
- High Hermits rep: `The.Game.PlayerReputation.GetLevel("Hermits")` returns -2..2 (`Reputation.cs:160`),
  where 1 is liked and 2 is loved (RuleSettings thresholds). The part skips avoid/aggro at loved (2), per the user 2026-10-13.
