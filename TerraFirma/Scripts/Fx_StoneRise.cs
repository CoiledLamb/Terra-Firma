using System;
using System.Collections.Generic;
using ConsoleLib.Console;
using Kobold;
using UnityEngine;
using XRL.Rules;
using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// The redoubt wave's emergence sprite (animation pass v2, user spec 2026-08-03: "a rock
	/// wall sprite sliding into the earth and briefly emerging" -- never fully hidden, no
	/// burrow idiom). A pooled sprite of the wall's own tile starts mostly buried below its
	/// cell, slides partway up, holds, and sinks back; rim instances rise near-full and hold
	/// until the real fabric takes over. Chassis = CombatJuiceEntryJump's pooled-sprite
	/// pattern (the thrown-object animation), tween shape ours. Pre-delay, the sprite parks
	/// BEHIND the map (z +100) so staggered cells stay unseen without ever "burrowing".
	/// </summary>
	public class Cleo_TerraFirma_JuiceStoneRise : CombatJuiceEntry
	{
		private IRenderable Tile;
		private ex3DSprite2 Target;
		private Vector3 Base;
		private float Delay;
		private float Rise;
		private float Hold;
		private float Sink;
		private float BuriedY;
		private float EmergeY;

		private bool StayUp; // rim pieces hold at full emergence until the real fabric takes over -- no dip before the handoff

		public Cleo_TerraFirma_JuiceStoneRise(IRenderable tile, Vector3 basePos, float delay, float rise, float hold, float sink, float buriedDepth, float emergeHeight, bool stayUp = false)
		{
			Tile = tile;
			Base = basePos;
			Delay = delay;
			Rise = Mathf.Max(0.01f, rise);
			Hold = hold;
			Sink = Mathf.Max(0.01f, sink);
			BuriedY = -buriedDepth;
			EmergeY = -buriedDepth + emergeHeight;
			StayUp = stayUp;
			duration = delay + this.Rise + hold + (stayUp ? 0f : this.Sink);
		}

		public override void start()
		{
			// OPAQUE (feel round 3: transparent backing let water/floor show through the tile
			// while it rose -- "the occluding bit"): the sprite renders as a solid block, the
			// way standing walls do, so the rising stone reads as mass, not a cutout.
			Target = SpriteManager.GetPooledSprite(Tile, Transparent: false);
			Transform transform = Target.gameObject.transform;
			transform.SetParent(GameManager.Instance.TileRoot.transform);
			transform.position = Base + new Vector3(0f, BuriedY, 100f); // parked behind the map until its cue
		}

		public override void update()
		{
			if (Target == null)
				return;
			float p = t - Delay;
			if (p < 0f)
				return;
			float y;
			if (p <= Rise)
				y = Mathf.Lerp(BuriedY, EmergeY, Easing.SineEaseOut(Mathf.Clamp01(p / Rise)));
			else if (StayUp || p <= Rise + Hold)
				y = EmergeY;
			else
				y = Mathf.Lerp(EmergeY, BuriedY, Easing.SineEaseInOut(Mathf.Clamp01((p - Rise - Hold) / Sink)));
			Target.transform.position = Base + new Vector3(0f, y, -10f);
		}

		public override void finish()
		{
			if (Target != null)
			{
				SpriteManager.Return(Target);
				Target = null;
			}
			base.finish();
		}
	}

	/// <summary>A self-authored renderable for the emergence sprite -- no RenderForUI middleman
	/// (walls are painted at zone level and a fresh sample carries no tile; feel notes
	/// 2026-08-03, two failed attempts). Colors follow the dominant material (feel round 3:
	/// hardcoded sandstone red made every rising stone red in non-red zones).</summary>
	public class Cleo_TerraFirma_StoneTileRenderable : IRenderable
	{
		private string TilePath;
		private char Fg;
		private char Detail;
		private char Bg;

		public Cleo_TerraFirma_StoneTileRenderable(string tilePath, char fg, char detail, char bg)
		{
			TilePath = tilePath;
			Fg = fg;
			Detail = detail;
			Bg = bg;
		}

		public string getTile() => TilePath;
		public string getRenderString() => "#";
		public string getColorString() => "&" + Fg + "^" + Bg;
		public string getTileColor() => "&" + Fg + "^" + Bg;
		public char getDetailColor() => Detail;
		public ColorChars getColorChars() => new ColorChars { foreground = Fg, background = Bg, detail = Detail };
		public bool getHFlip() => false;
		public bool getVFlip() => false;
	}

	/// <summary>Enqueues the whole cast's emergence choreography and reports its total length
	/// (the game thread sleeps that long while the Unity side plays it out).</summary>
	public static class Cleo_TerraFirma_StoneRiseFX
	{
		/// <summary>Harness kill switch (tfredoubt): true routes scripted casts through the
		/// unseen-cast branch -- deposits land synchronously, no frame pump, no sprites.
		/// Set (and always restored) only by the test harness; rides the TF-VERIFY strip.</summary>
		public static bool HarnessMute;

		/// <summary>Tile resolution, generalized to any painted-wall material (feel round 2:
		/// the ring wears the zone's dominant wall). Candidates probed against the sprite
		/// manager's atlas, verdict logged, result cached per material value. Naming forms
		/// per the brinestalk blueprint literal (Walls/wall_brinestalk-00000000.png) plus the
		/// atlas-tag variants seen in the wall families.</summary>
		private static readonly Dictionary<string, string> ResolvedTiles = new Dictionary<string, string>();

		private static string ResolveTile(string paintValue)
		{
			if (paintValue.IsNullOrEmpty())
				paintValue = "wall_rock";
			if (ResolvedTiles.TryGetValue(paintValue, out string cached))
				return cached;
			string[] candidates =
			{
				"Walls/" + paintValue + "-00000000.png",
				"Assets_Content_Textures_Walls_" + paintValue + "-00000000.png",
				"Assets_Content_Textures_Walls_" + paintValue + "-00000000.bmp",
				"Assets_Content_Textures_Tiles_" + paintValue + "-00000000.bmp",
				"Walls/wall_rock-00000000.png", // the rock fallback so exotic materials still rise as stone
			};
			string resolved = null;
			foreach (string candidate in candidates)
			{
				if (SpriteManager.TryGetTextureInfo(candidate, out _))
				{
					resolved = candidate;
					Helpers.VerifyLog("FIS", $"anim tile RESOLVED for '{paintValue}': {candidate}");
					break;
				}
				Helpers.VerifyLog("FIS", $"anim tile rejected (not in atlas): {candidate}");
			}
			if (resolved == null)
				Helpers.VerifyLog("FIS", $"anim tile UNRESOLVED for '{paintValue}' -- emergence sprites skipped");
			ResolvedTiles[paintValue] = resolved;
			return resolved;
		}

		/// <summary>Everything the mutation's frame-pump loop needs to settle the cast in
		/// sync with the sprites: per-cell moments (seconds from choreography start) and the
		/// total run length.</summary>
		public class Schedule
		{
			public float Total;
			public Dictionary<Cell, float> SettleTimes = new Dictionary<Cell, float>(); // rim fabric materializes here
			public Dictionary<Cell, float> GapTimes = new Dictionary<Cell, float>();    // gap sprites sink back here (dust cue)
		}
		// tempo dials (seconds)
		public const float SHELL_SEC = 0.10f;    // wave-front travel per shell (public: the wake scatter keys off it)
		public const float TRAVEL_RISE = 0.09f;
		private const float TRAVEL_HOLD = 0.03f;
		private const float TRAVEL_SINK = 0.10f;
		private const float RIM_STAGGER = 0.18f; // breakthrough spread ("softly", staggered)
		private const float RIM_RISE = 0.14f;
		private const float RIM_HOLD = 0.10f;    // held emergence before the real fabric takes over
		private const float RIM_SINK = 0.06f;
		// tile geometry (world units; a tile is ~24 tall -- floatingText's +12 half-tile offset)
		private const float BURIED_DEPTH = 20f;
		private const float TRAVEL_EMERGE = 9f;  // a low shoulder of stone
		private const float RIM_EMERGE = 20f;    // near-full breakthrough

		/// <summary>Enqueues the emergence choreography and returns the settle schedule for
		/// the mutation's frame-pump loop; null = nothing to animate (unseen cast). Gap cells
		/// rise and SINK BACK (the sections the earth couldn't hold); everything else on the
		/// rim rises, holds, and hands off to the real fabric at its own settle moment.</summary>
		public static Schedule Play(Cell origin, int radius, ICollection<Cell> reached, ICollection<Cell> gaps, string paintValue, char paintFg, char paintDetail, char paintBg, Func<int, int, int, bool> onRing)
		{
			if (origin == null)
				return null;
			GameManager gm = GameManager.Instance;
			if (gm == null)
				return null;
			string tilePath = ResolveTile(paintValue);
			if (tilePath == null)
				return null;
			IRenderable wallTile = new Cleo_TerraFirma_StoneTileRenderable(tilePath, paintFg, paintDetail, paintBg);
			float rimStart = radius * SHELL_SEC;
			Schedule schedule = new Schedule();
			schedule.Total = rimStart + RIM_STAGGER + RIM_RISE + RIM_HOLD + 0.04f;
			int enqueued = 0;
			foreach (Cell c in reached)
			{
				if (!c.IsVisible())
					continue;
				int dx = c.X - origin.X;
				int dy = c.Y - origin.Y;
				bool rim = onRing(dx, dy, radius);
				Vector3 basePos = gm.getTileCenter(c.X, c.Y);
				if (rim)
				{
					float delay = rimStart + 0.01f * Stat.Random(0, (int)(RIM_STAGGER * 100f));
					if (gaps != null && gaps.Contains(c))
					{
						// couldn't hold: rise, falter, sink back
						CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(wallTile, basePos, delay, RIM_RISE, 0.04f, RIM_SINK, BURIED_DEPTH, RIM_EMERGE * 0.7f), async: true);
						schedule.GapTimes[c] = delay + RIM_RISE + 0.04f;
					}
					else
					{
						CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(wallTile, basePos, delay, RIM_RISE, RIM_HOLD, RIM_SINK, BURIED_DEPTH, RIM_EMERGE, stayUp: true), async: true);
						schedule.SettleTimes[c] = delay + RIM_RISE + RIM_HOLD;
					}
					enqueued++;
					continue;
				}
				for (int r = 1; r < radius; r++)
				{
					if (onRing(dx, dy, r))
					{
						float delay = r * SHELL_SEC + 0.01f * Stat.Random(0, 4);
						CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(wallTile, basePos, delay, TRAVEL_RISE, TRAVEL_HOLD, TRAVEL_SINK, BURIED_DEPTH, TRAVEL_EMERGE), async: true);
						enqueued++;
						break;
					}
				}
			}
			return enqueued > 0 ? schedule : null;
		}
	}
}
