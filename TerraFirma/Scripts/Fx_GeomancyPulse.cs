using System;
using System.Collections.Generic;
using System.Diagnostics;
using XRL.Core;
using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// Geomancy's landing pulse (2026-08-05, user concept: "some kind of pulse/sonar effect
	/// that shows the radius where it falls"). Two halves, one sweep clock:
	/// (1) the WARP WAVE here -- rings of the engine's '@' distortion particles expanding
	///     from the landing cell, dying exactly at the revealed rim. Feel round 8, user:
	///     "time dilation/electromagnetic pulse - the wave it sends out is a *great*
	///     precedent." Decomp ground truth: TimeDilation.Render fires 360 "@" particles
	///     per ring with per-degree velocities, and ParticleManager.Render's '@' branch
	///     draws NO glyph -- it copies the neighboring cell's char/tile/colors into the
	///     particle's cell, a transparent LENS RIPPLE through whatever is on screen.
	///     Thematically exact for a vibration passing through the earth: the wave warps
	///     the world instead of painting on it. (EMP's wave was the other candidate --
	///     rejected: it is a Thread.Sleep frame-pumped band, wrong for a per-throw
	///     effect; TD's is fire-and-forget.) Accessibility note: the engine itself
	///     swaps '@' for '*' when OptionDisableTextWarpEffects is on.
	/// (2) the UNVEILING -- Mutation_Geomancy's per-frame lighting gates each cell on
	///     RevealAtMS stamped from the same clock, so darkness recedes with the front.
	/// The lighting carries the honesty (distortion-masked cells never light); the wave
	/// carries the shape. Particle-only: no pooled sprites, no juice chassis.
	/// Feel-round history (all 2026-08-05): 1 per-cell ø blips (the CLASSIC_FRONT
	/// fallback) -> 2 tangent strokes, then smooth radial dots -> 3 torch squares ->
	/// 4 dense text shimmer -> 5 pixel-cluster tile particles -> 6 brazier-style
	/// emitter renewal -> 7 ruled back to classic -> 8 the TD warp wave (this build).
	/// Rounds 3-6 machinery deleted; recoverable from the TODO record if ever wanted.
	/// The fx_geomancy_spark1-3.png textures they used were pruned 2026-08-12.
	/// </summary>
	public static class Cleo_TerraFirma_GeomancyPulseFX
	{
		// tempo: ms of sweep per ring of distance. SHELL_SEC (0.10s) is the redoubt's
		// heaving-earth pace; the sonar ping reads right a touch quicker.
		public const int RING_MS = 80;

		// warp-wave dials. TD fires 360/ring (base-game-proven particle budget; the
		// text list caps at 1000); the echo ring at reduced speed is the sonar
		// double-ping, arriving behind the main front and dying at the same rim.
		public const int WARP_PARTICLES = 360;
		public const int WARP_RINGS = 2;
		private static readonly float[] RING_SPEED_FACTORS = { 1f, 0.65f };

		// FALLBACK RETAINED (ruled in round 7, superseded by round 8 but kept one flip
		// away): true = the round-1 front verbatim -- one delayed text glyph per
		// revealed cell (delay = dist * RING_MS, the ø ring char, &W), per-cell honest
		// to the distortion mask. Gemstone cyan stays cut by ruling either way.
		public const bool CLASSIC_FRONT = false;
		private const string CLASSIC_GLYPH = "&Wø"; // 'ø' = CP437 248, the degree-ring
		private const int CLASSIC_LIFE = 6;         // ~0.1s per cell

		public const string CENTER_GLYPH = "*"; // impact ping at the landing cell
		public const string CENTER_COLOR = "&Y";

		// kill switch for the wave alone (the unveiling rides the mutation's RevealAtMS
		// stamps and is muted separately via the HarnessMute/inactive-zone branch at the
		// call site)
		public static bool GlyphsEnabled = true;

		/// <summary>Game-thread-safe ms clock (Unity's Time API is main-thread-only;
		/// ParticleManager itself paces on a Stopwatch).</summary>
		private static readonly Stopwatch Clock = Stopwatch.StartNew();
		public static long NowMS => Clock.ElapsedMilliseconds;

		/// <summary>Launch the wave. Warp mode (default): TD-style '@' distortion rings.
		/// CLASSIC_FRONT mode: the round-1 per-cell glyph front over
		/// <paramref name="cells"/>. Either way the wave dies at <paramref name="radius"/>
		/// (the farthest cell the reveal actually reached, so a mask-trimmed reveal gets
		/// a correspondingly shorter wave). Fire-and-forget; purely cosmetic. Returns
		/// the particle count for the verify line.</summary>
		public static int Play(Cell center, float radius, List<(Cell cell, float dist)> cells)
		{
			if (!GlyphsEnabled || center == null)
				return 0;
			XRLCore.ParticleManager.Add(CENTER_COLOR + CENTER_GLYPH, center.X, center.Y, 0f, 0f, 8, 0f, 0f, 0L);
			if (radius < 1.5f)
				return 0; // nothing meaningful to sweep
			if (CLASSIC_FRONT)
			{
				if (cells == null || cells.Count == 0)
					return 0;
				// ascending delay order is LOAD-BEARING (the particle render loop walks
				// add-order and stops at the first pending delay -- decomp-verified)
				cells.Sort((a, b) => a.dist.CompareTo(b.dist));
				foreach ((Cell cell, float dist) in cells)
					XRLCore.ParticleManager.Add(CLASSIC_GLYPH, cell.X, cell.Y, 0f, 0f, CLASSIC_LIFE, 0f, 0f, (long)(dist * RING_MS));
				return cells.Count;
			}
			// TD's idiom, our radius semantics: per-degree velocities from the landing
			// cell (TD: Add("@", x, y, sin(j)/n, cos(j)/n) with life 999 -- ours die at
			// the rim instead). Uniform speed within a ring keeps the ripple coherent;
			// no delays, so no sort needed.
			float baseSpeed = 16f / RING_MS; // cells per ~16ms engine tick
			int spawned = 0;
			for (int r = 0; r < WARP_RINGS && r < RING_SPEED_FACTORS.Length; r++)
			{
				float speed = baseSpeed * RING_SPEED_FACTORS[r];
				int life = (int)(radius / speed);
				if (life <= 0)
					continue;
				for (int j = 0; j < WARP_PARTICLES; j++)
				{
					double ang = j * (2.0 * Math.PI / WARP_PARTICLES);
					XRLCore.ParticleManager.Add("@", center.X, center.Y,
						(float)Math.Sin(ang) * speed, (float)Math.Cos(ang) * speed, life, 0f, 0f, 0L);
					spawned++;
				}
			}
			return spawned;
		}
	}
}
