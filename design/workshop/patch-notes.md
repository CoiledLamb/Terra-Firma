# terra firma patch notes

<!--
Running patch notes, newest version on top. Each finished thing gets one line under the
version it ships in, written for players: what changed, plainly. No lore, no flavor
(flavor is Cleo's). Ship text rules apply: no em dashes, plain ASCII.

The "not landed yet" list under the next version holds work that is built but held,
half-verified, or not built. It never ships; move a line up when the thing is done.

Keeping it current:
  python tools/patch-notes.py              list commits since the last release not yet reviewed
  python tools/patch-notes.py skip-noise   mark tracker/dev-wish/diagnostic commits as reviewed
  python tools/patch-notes.py note <hash>  mark commits as covered by a line here
  python tools/patch-notes.py skip <hash>  mark commits as nothing a player sees
  python tools/patch-notes.py release      print the next version's notes, ready to paste
The ledger is design/workshop/patch-notes-reviewed.txt.
-->

## 0.2.8 (next)

new creatures:
- inkling: hides in cave walls as graffiti, and rides along as a tattoo if you pet it. it gets hungry, eats tattoo ink, and takes on the color of what it ate. found in caves and in the tomb of the eaters, and sometimes kept as a village pet.
- stone-kissed hermit: lives alone in the mountains and hills, ringed by statues of the local creatures. it backs away if you come close and turns on you with gorgon's gaze if you get too close. back off and it calms down and goes home, unless you've hurt it. hermits who love you leave you alone.

changes:
- boar vessels now turn up in the wild, in the same four places boars live, at half a boar's odds.
- stilt pig farmers sometimes keep a whole pen of boar vessels.

fixes:
- fixed an error with petrification on the game's lang-experimental beta branch.

### not landed yet (not in the notes until it's done)
- refractive buckler and refractive shield: built but held (commented out); still owe names, descriptions, sprites and where you find them.
- refractive gear reflecting gorgon's gaze: decided, not built.

## 0.2.7.1 (live 2026-10-16)

changes:
- claysculpting is held out of random encounters until it's finished.

fixes:
- wording fixes across the mod.

## 0.2.7 (2026-10-14)

new:
- a confused stoneshaper carves a random creature near your level.

changes:
- while frozen, you can't use your hands for most actions, and you can't cast fissure.
- granitefur cloak, gloves and bracer are worth 45 each.
- granite baboons leave a corpse 10% of the time (was 15%).
- mason escort size, carbide weight and mining gear odds adjusted; granitefur cloak recolored.
- updated credits and a new workshop cover.

fixes:
- hewers now notice the player before using fissure.
- outriders on patrol walk past their allies instead of stopping.
- grammar and spacing fixes in several messages.
