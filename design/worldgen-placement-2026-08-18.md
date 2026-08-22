# Worldgen placement: scout-and-quota (deep dive, pre-implementation)

USER CONCEPT 2026-08-18: *"instead of only trying to place 25, we could place more, and then
trim down to 25 warrens that fit our tables. this relies heavily on seeing how worldgen works."*
This document is the seeing. Status: **design verified against decomp + sim, NOT implemented.**
Every unattributed decision below is a CLAUDE call awaiting the user's strike.

## The problem it solves

Blind placement (25 random canyon zones) inherits the terrain's stage distribution: bands
degrade the rolled stage, flourishing lands at ~2/world, and five of the nine stage x occupancy
cells fall under one per world (flourishing/reclaimed measured ZERO in five worlds — flagged in
the occupancy section). Distribution was ruled NOT the anti-groan lever, but the empty grid
corners are a content loss the census keeps flagging.

## Engine ground truth (all decompiled 2026-08-18, ilspycmd)

- **Zones build lazily on visit.** `AddMutableEncounterToTerrain` stamps a builder name on a
  random mutable zone and unflags it — nothing generates at worldgen. Therefore any
  trim-to-25 selection must SCORE bands at worldgen, before any zone exists.
- **Zone build RNG is derivable at worldgen.** `ZoneManager.cs:3307-3316`:
  `request.Seed = "ZONESEED"+ZoneID; Stat.ReseedFrom(GetWorldSeed(seedString)+buildTries)`.
  `XRLGame.GetWorldSeed(Key)` (XRLGame.cs:1505) is a pure function of the stored world seed.
- **BUT the ambient stream drifts:** every builder that runs before ours consumes draws, and
  `buildTries` increments on zone-build retry — a pre-roll against the ambient stream lies.
  **THE TWIST THAT MAKES SCORING EXACT: our zone builder seeds its canyon from its own derived
  per-zone key** (e.g. `GetWorldSeed("TFWARREN"+ZoneID)`), not the ambient stream. Worldgen
  pre-roll and visit-time build then produce the same canyon BY CONSTRUCTION. The sim already
  relies on canyon-regeneration determinism (the retry-per-zone system).
- **Mod hook exists and is first-class:** a `[JoppaWorldBuilderExtension]` class implementing
  `IJoppaWorldBuilderExtension` gets `OnAfterBuild(builder)` — fires AFTER
  `BuildMutableEncounters()` (farms/forts already claimed, no collision), with the builder
  instance in hand (`JoppaWorldBuilder.cs:3655-3700`).
- **Builder params exist:** `AddZoneBuilder(ZoneID, Priority, Class, Key1, Value1, ...)` up to
  six pairs (`ZoneManager.cs:1110-1140`) — the assigned stage stamps as `"Stage","flourishing"`.
- **`AddSecret` and `AddLocationFinder` are public** on the builder
  (`JoppaWorldBuilder.cs:1978, 2673`) — rumour wiring happens for the CHOSEN 25 only.
- **TRAP: `GetMutableLocationWithTerrain` is `GetRandomElement()` WITHOUT removal**
  (`MutabilityMap.cs:35-46`) — repeated scouting calls return duplicates. The scout must
  enumerate `mutableLocationsByTerrainType["DesertCanyon"]` directly (dedupe by construction)
  and unflag only the chosen 25. Rejected candidates stay mutable.
- `AdjustZoneGenerationTierTo` is called by the farm flow per chosen zone — mimic for the
  chosen 25, never for rejected scouts.

## The design

1. **Scout** ~50 candidate canyon zones in `OnAfterBuild` (enumerate the mutable pool, sample
   without replacement).
2. **Score each exactly**: run the canyon from the zone's own derived seed, then the builder's
   OWN first phase — faces → `growRegion` sizes → capacity = max stage whose area budget fits.
   The scorer IS the builder's degrade logic, so score and build cannot disagree.
3. **Select 25 against a stage quota** (see open call below), biggest-capacity zones taking
   flourishing. Stamp each chosen zone's builder with its assigned stage; unflag it;
   `AddSecret(..., "Settlements")` + `AddLocationFinder`, the farm idiom.
4. **Zone-side keeps every failsafe**: the stamped stage is a CEILING, not a command — the
   degrade-honesty and worth-the-trip bars stay, so a scorer bug yields a smaller warren, never
   a broken promise.

## Sim validation (2026-08-18, warren-lab-v2 console, 5 worlds x 50 scouts)

Flourishing-capable scouts per world: **15 / 11 / 15 / 12 / 7** against a quota of 5 — every
world filled the full 5 fl / 9 camp / 11 working quota with headroom. Corner math: 5 flourishing
x 14% reclaimed ≈ **0.7 flourishing/reclaimed warrens per world**, up from ~0.28 blind (and the
quota is the knob if ≥1 is wanted).

## Costs and risks, honestly

- **Worldgen cost:** ~50 canyon pre-rolls + growRegion scoring. Sim-side the full carve runs
  4ms; the scorer runs less. Estimate well under a second of worldgen; measure in-engine anyway.
- **Determinism risk:** if the C# canyon port accidentally reads ANY ambient RNG, score and
  build silently diverge. Mitigation: the builder asserts at build time that the regenerated
  band matches the scored capacity, and logs loudly (TF-VERIFY) when it does not.
- **Quota rigidity:** a fixed quota makes every world's stage mix identical — a sameness risk at
  the WORLD scale. Mitigation option: roll the quota itself in a band (fl 4-6 etc.). CLAUDE
  lean: roll it.
- **Two seeds, one canyon:** the zone's OTHER content still uses the ambient stream; only the
  canyon (and anything the warren derives from it) moves to the derived seed. The visible
  consequence is zero — the canyon was already regenerated identically per attempt.

## Open calls (the user's)

1. **The quota itself** — 5/9/11 mirrors today's ratios with flourishing pushed to 5. Corner
   math: fl-reclaimed/world ≈ quota_fl x 0.14. Wanting ≥1/world means quota_fl ≈ 7.
2. **Fixed quota vs rolled-in-a-band** (CLAUDE lean: rolled).
3. **Scout count** (50 scouts the sim number; more = better tails, slower worldgen).
4. **Secret naming** — the rumour's display name is stamped at worldgen, before the site exists.
   Stage-flavored names ("a flourishing quarry-warren") vs generic; text is the user's.

## Sim-first obligation

Per the 2026-08-07 ruling the sim leads: the lab's World census should gain the scout-and-quota
model BEFORE any C# exists, so the design target stays judgeable in the rig. That is the first
implementation step whenever this is greenlit.
