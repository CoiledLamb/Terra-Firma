# Mutation VFX survey (2026-08-03, decompiled ground truth)

For the redoubt wave animation pass. Decomp dumps in session scratchpad `mutdecomp/`
(Burgeoning, FlamingRay, FreezingRay, StunningForce, Quills, SlimeGlands, Teleportation,
Cryokinesis, LightManipulation, SunderMind, CombatJuice).

## The three systems

1. **Scrap-buffer scan animation** (synchronous, the workhorse): `ScreenBuffer.GetScrapBuffer1
   (bLoadFromCurrent: true)` or `RenderMapToBuffer` -> stamp glyphs over cells -> `Popup.
   _TextConsole.DrawBuffer` -> `Thread.Sleep` -> repeat. Users: FlamingRay/FreezingRay (beam
   sweep, stamps `(char)(219 + Stat.Random(0,4))` = the block glyphs, 10ms), StunningForce
   (ShockwaveAnimation, 3-cell displaced-glyph trail, 30ms), Quills (radial, 10ms),
   SlimeGlands (arc, 30ms), Burgeoning (per-cell sprout: glyph A 50ms -> glyph B 25ms).
2. **ParticleManager** (async text particles): `AddSinusoidal` (Burgeoning petals, our
   SmokePuff), polar `Add` loops (StunningForce ring), `ParticleBlip` (Teleportation),
   `obj.ParticleText(...)` per-victim flashes (the rays' burn feedback).
3. **CombatJuice** (Unity layer, gated on Options.UseOverlayCombatEffects -> garnish only):
   **`cameraShake(duration)`**, `floatingText`, `punch` lunges, `playPrefabAnimation`
   (needs Unity prefabs we don't have). Sounds via `playWorldSound`.

## Key finds

- **Burgeoning's sprout IS a two-frame rise animation** (A 50ms / B 25ms) -- the user's
  pulse concept (half rise / quarter dip) is this exact native trick swept radially.
- The rays already stamp CP437 block glyphs (219-223 = full/half blocks) as "matter" VFX.
- `CombatJuice.cameraShake` exists, one call, and no earth mutation uses it. The eruption
  wants it.
- Buffer cells carry tile info -> tile-mode animation = stamping existing wall/rubble
  TILES with tint, no sprites needed (user direction 2026-08-02: base wall textures only).

## The ruled recipe (user spec + survey, ready to build)

1. TRAVEL: buffer-scan, two beats per shell (~40ms/beat): rubble tile dim (quarter dip) /
   wall tile mid-tint (half rise), alternating -- the wave wears the local geology.
2. ERUPTION at rim: full ring stamped bright, 2 beats + camera shake + slam sound.
3. SETTLE: real fabric objects (pre-resolved before animation; replay-over-state like
   StunningForce), gaps crumble out with SmokePuff.
4. VICTIMS: ParticleText dust flash on each creature the wave strikes.

Mechanics resolve BEFORE the animation plays; the animation is cosmetic replay. Sleep
constant = user picks from the mockup speed slider.
