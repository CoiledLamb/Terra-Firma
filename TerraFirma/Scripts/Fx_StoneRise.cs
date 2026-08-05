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

		/// <summary>Tile resolution, generalized to any painted-wall material (feel round 2)
		/// AND any connectivity mask (feel round 5: fractured plates). The mask is the zone
		/// painter's own 8-digit suffix -- N,NE,E,SE,S,SW,W,NW, 1 per connected neighbor,
		/// decomp-verified in ZoneManager.PaintWall -- so a plate's sprites wear the same
		/// variants standing walls would. Candidates probed against the atlas, cached per
		/// material+mask; a missing mask variant falls back to the isolated tile (an extra
		/// fracture, never a hole), a missing material falls back to wall_rock.</summary>
		private static readonly Dictionary<string, string> ResolvedTiles = new Dictionary<string, string>();

		public const string ISOLATED_MASK = "00000000";

		private static string ResolveTile(string paintValue, string mask)
		{
			if (paintValue.IsNullOrEmpty())
				paintValue = "wall_rock";
			string key = paintValue + "-" + mask;
			if (ResolvedTiles.TryGetValue(key, out string cached))
				return cached;
			string[] candidates =
			{
				"Walls/" + paintValue + "-" + mask + ".png",
				"Assets_Content_Textures_Walls_" + paintValue + "-" + mask + ".png",
				"Assets_Content_Textures_Walls_" + paintValue + "-" + mask + ".bmp",
				"Assets_Content_Textures_Tiles_" + paintValue + "-" + mask + ".bmp",
			};
			string resolved = null;
			foreach (string candidate in candidates)
			{
				if (SpriteManager.TryGetTextureInfo(candidate, out _))
				{
					resolved = candidate;
					break;
				}
			}
			if (resolved != null)
				Helpers.VerifyLog("FIS", $"anim tile RESOLVED for '{key}': {resolved}");
			else if (mask != ISOLATED_MASK)
			{
				// the atlas doesn't stock this variant: fracture the plate rather than skip the cell
				Helpers.VerifyLog("FIS", $"anim tile: no '{key}' variant, isolated fallback");
				resolved = ResolveTile(paintValue, ISOLATED_MASK);
			}
			else if (paintValue != "wall_rock")
			{
				Helpers.VerifyLog("FIS", $"anim tile: no isolated tile for '{paintValue}', wall_rock fallback");
				resolved = ResolveTile("wall_rock", ISOLATED_MASK);
			}
			else
				Helpers.VerifyLog("FIS", "anim tile UNRESOLVED for 'wall_rock' -- emergence sprites skipped");
			ResolvedTiles[key] = resolved;
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
		// plate dials (feel round 5, user: fractured-plate connectivity -- arcs of each shell
		// fuse into slabs that heave TOGETHER; the breaks between them are the fractures)
		// re-dialed 2026-08-04 (user: "plates could be a bit less fractured") -- was 2/4/15
		private const int PLATE_MIN = 3;
		private const int PLATE_MAX = 6;
		private const int SHARD_CHANCE = 8;       // % of cuts that come off as a lone shard
		private const int PLATE_JITTER_STEPS = 5; // travel plates offset 0..0.05s within their shell
		// tile geometry (world units; a tile is ~24 tall -- floatingText's +12 half-tile offset)
		private const float BURIED_DEPTH = 20f;
		private const float TRAVEL_EMERGE = 9f;  // a low shoulder of stone
		private const float RIM_EMERGE = 20f;    // near-full breakthrough

		/// <summary>Enqueues the emergence choreography and returns the settle schedule for
		/// the mutation's frame-pump loop; null = nothing to animate (unseen cast). FEEL
		/// ROUND 5 (user): each shell and the rim are partitioned into contiguous ARCS --
		/// plates -- whose sprites wear connected wall-tile variants and heave as one slab
		/// (shared delay); the breaks between plates are the fractures. Rim plates punch
		/// through together and hold; gap cells stay lone shards that rise and SINK BACK
		/// (the sections the earth couldn't hold). Shells never fuse radially -- they move
		/// at different moments, and the offset IS the outward travel.</summary>
		public static Schedule Play(Cell origin, int radius, ICollection<Cell> reached, ICollection<Cell> gaps, string paintValue, char paintFg, char paintDetail, char paintBg, Func<int, int, int, bool> onRing)
		{
			if (origin == null)
				return null;
			GameManager gm = GameManager.Instance;
			if (gm == null)
				return null;
			if (ResolveTile(paintValue, ISOLATED_MASK) == null)
				return null;
			float rimStart = radius * SHELL_SEC;
			Schedule schedule = new Schedule();
			schedule.Total = rimStart + RIM_STAGGER + RIM_RISE + RIM_HOLD + 0.04f;
			// bucket the visible cells: rim fabric / rim gaps / travel shells 1..radius-1
			List<Cell> rimFabric = new List<Cell>();
			List<Cell> rimGaps = new List<Cell>();
			List<Cell>[] shells = new List<Cell>[radius];
			foreach (Cell c in reached)
			{
				if (!c.IsVisible())
					continue;
				int dx = c.X - origin.X;
				int dy = c.Y - origin.Y;
				if (onRing(dx, dy, radius))
				{
					if (gaps != null && gaps.Contains(c))
						rimGaps.Add(c);
					else
						rimFabric.Add(c);
					continue;
				}
				for (int r = 1; r < radius; r++)
				{
					if (onRing(dx, dy, r))
					{
						(shells[r] ?? (shells[r] = new List<Cell>())).Add(c);
						break;
					}
				}
			}
			// PLAN before PLAY: plates first, then every tile variant this cast needs resolved
			// while the juice queue is still quiet. TryGetTextureInfo's miss path loads the
			// texture under the atlas WRITE lock -- probing new variants while the Unity side
			// was fetching our already-enqueued sprites deadlocked into a hard freeze
			// (2026-08-04, third live cast). After this warmup, enqueue-time ResolveTile
			// calls are pure cache hits and never touch the atlas.
			List<List<Cell>>[] shellPlates = new List<List<Cell>>[radius];
			for (int r = 1; r < radius; r++)
			{
				if (shells[r] != null)
					shellPlates[r] = CutPlates(SortByAngle(shells[r], origin));
			}
			List<List<Cell>> rimPlates = CutPlates(SortByAngle(rimFabric, origin));
			for (int r = 1; r < radius; r++)
			{
				if (shellPlates[r] == null)
					continue;
				foreach (List<Cell> plate in shellPlates[r])
					for (int i = 0; i < plate.Count; i++)
						ResolveTile(paintValue, MaskFor(plate, i));
			}
			foreach (List<Cell> plate in rimPlates)
				for (int i = 0; i < plate.Count; i++)
					ResolveTile(paintValue, MaskFor(plate, i));
			int enqueued = 0;
			// travel shells: fractured plates surging outward, one shared delay per plate
			for (int r = 1; r < radius; r++)
			{
				if (shellPlates[r] == null)
					continue;
				foreach (List<Cell> plate in shellPlates[r])
				{
					float delay = r * SHELL_SEC + 0.01f * Stat.Random(0, PLATE_JITTER_STEPS);
					enqueued += EnqueuePlate(plate, gm, paintValue, paintFg, paintDetail, paintBg,
						delay, TRAVEL_RISE, TRAVEL_HOLD, TRAVEL_SINK, TRAVEL_EMERGE, StayUp: false, schedule, Settle: false);
				}
			}
			// the rim: plates of rampart punching through together, held until the fabric lands
			foreach (List<Cell> plate in rimPlates)
			{
				float delay = rimStart + 0.01f * Stat.Random(0, (int)(RIM_STAGGER * 100f));
				enqueued += EnqueuePlate(plate, gm, paintValue, paintFg, paintDetail, paintBg,
					delay, RIM_RISE, RIM_HOLD, RIM_SINK, RIM_EMERGE, StayUp: true, schedule, Settle: true);
			}
			// gaps: lone shards, their own stagger, rise-falter-sink
			string gapTile = ResolveTile(paintValue, ISOLATED_MASK);
			foreach (Cell c in rimGaps)
			{
				float delay = rimStart + 0.01f * Stat.Random(0, (int)(RIM_STAGGER * 100f));
				IRenderable tile = new Cleo_TerraFirma_StoneTileRenderable(gapTile, paintFg, paintDetail, paintBg);
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(tile, gm.getTileCenter(c.X, c.Y), delay, RIM_RISE, 0.04f, RIM_SINK, BURIED_DEPTH, RIM_EMERGE * 0.7f), async: true);
				schedule.GapTimes[c] = delay + RIM_RISE + 0.04f;
				enqueued++;
			}
			return enqueued > 0 ? schedule : null;
		}

		/// <summary>One plate: every cell's sprite wears the tile variant connected toward its
		/// arc-neighbors and shares the plate's delay -- the slab heaves as one piece.</summary>
		private static int EnqueuePlate(List<Cell> plate, GameManager gm, string paintValue, char fg, char detail, char bg,
			float Delay, float Rise, float Hold, float Sink, float Emerge, bool StayUp, Schedule schedule, bool Settle)
		{
			int enqueued = 0;
			for (int i = 0; i < plate.Count; i++)
			{
				string tilePath = ResolveTile(paintValue, MaskFor(plate, i));
				if (tilePath == null)
					continue;
				Cell c = plate[i];
				IRenderable tile = new Cleo_TerraFirma_StoneTileRenderable(tilePath, fg, detail, bg);
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(tile, gm.getTileCenter(c.X, c.Y), Delay, Rise, Hold, Sink, BURIED_DEPTH, Emerge, StayUp), async: true);
				if (Settle)
					schedule.SettleTimes[c] = Delay + Rise + Hold;
				enqueued++;
			}
			return enqueued;
		}

		/// <summary>The painter's 8-digit suffix (N,NE,E,SE,S,SW,W,NW) for a plate member:
		/// connected only toward its immediate arc-neighbors, so plate interiors fuse and
		/// plate ends keep their broken faces.</summary>
		private static string MaskFor(List<Cell> plate, int idx)
		{
			if (plate.Count == 1)
				return ISOLATED_MASK;
			char[] mask = ISOLATED_MASK.ToCharArray();
			Cell c = plate[idx];
			SetMaskBit(mask, c, idx > 0 ? plate[idx - 1] : null);
			SetMaskBit(mask, c, idx < plate.Count - 1 ? plate[idx + 1] : null);
			return new string(mask);
		}

		private static void SetMaskBit(char[] mask, Cell c, Cell neighbor)
		{
			if (neighbor == null)
				return;
			int ddx = neighbor.X - c.X;
			int ddy = neighbor.Y - c.Y;
			if (ddx < -1 || ddx > 1 || ddy < -1 || ddy > 1 || (ddx == 0 && ddy == 0))
				return; // not touching -- no seam to draw
			int bit;
			if (ddx == 0) bit = ddy < 0 ? 0 : 4;              // N / S
			else if (ddx > 0) bit = ddy < 0 ? 1 : (ddy == 0 ? 2 : 3);  // NE / E / SE
			else bit = ddy < 0 ? 7 : (ddy == 0 ? 6 : 5);      // NW / W / SW
			mask[bit] = '1';
		}

		/// <summary>Ring cells in circular order around the origin.</summary>
		private static List<Cell> SortByAngle(List<Cell> cells, Cell origin)
		{
			List<Cell> sorted = new List<Cell>(cells);
			sorted.Sort((a, b) => Math.Atan2(a.Y - origin.Y, a.X - origin.X).CompareTo(Math.Atan2(b.Y - origin.Y, b.X - origin.X)));
			return sorted;
		}

		/// <summary>Partition an angle-ordered ring into plates: first split at every spatial
		/// break (cells not touching -- unreached ground, walls, the gaps), rotating the walk
		/// to start at a natural break so no run straddles the list seam; then cut each
		/// contiguous run into arcs of PLATE_MIN..PLATE_MAX with the occasional lone shard.
		/// A fully unbroken circle simply gets one arbitrary fracture where the walk began.</summary>
		private static List<List<Cell>> CutPlates(List<Cell> ring)
		{
			List<List<Cell>> plates = new List<List<Cell>>();
			int n = ring.Count;
			if (n == 0)
				return plates;
			bool Touching(Cell a, Cell b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)) == 1;
			int seam = 0;
			for (int i = 0; i < n; i++)
			{
				if (!Touching(ring[i], ring[(i + n - 1) % n]))
				{
					seam = i;
					break;
				}
			}
			List<List<Cell>> runs = new List<List<Cell>>();
			List<Cell> run = new List<Cell> { ring[seam] };
			for (int k = 1; k < n; k++)
			{
				Cell c = ring[(seam + k) % n];
				if (Touching(c, run[run.Count - 1]))
					run.Add(c);
				else
				{
					runs.Add(run);
					run = new List<Cell> { c };
				}
			}
			runs.Add(run);
			foreach (List<Cell> r in runs)
			{
				int i = 0;
				while (i < r.Count)
				{
					int len = Stat.Random(1, 100) <= SHARD_CHANCE ? 1 : Stat.Random(PLATE_MIN, PLATE_MAX);
					len = Math.Min(len, r.Count - i);
					plates.Add(r.GetRange(i, len));
					i += len;
				}
			}
			return plates;
		}
	}
}
