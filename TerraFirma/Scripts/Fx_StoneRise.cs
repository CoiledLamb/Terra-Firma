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

		public Cleo_TerraFirma_JuiceStoneRise(IRenderable tile, Vector3 basePos, float delay, float rise, float hold, float sink, float buriedDepth, float emergeHeight)
		{
			Tile = tile;
			Base = basePos;
			Delay = delay;
			Rise = Mathf.Max(0.01f, rise);
			Hold = hold;
			Sink = Mathf.Max(0.01f, sink);
			BuriedY = -buriedDepth;
			EmergeY = -buriedDepth + emergeHeight;
			duration = delay + this.Rise + hold + this.Sink;
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
			else if (p <= Rise + Hold)
				y = EmergeY;
			else
			{
				float q = Mathf.Clamp01((p - Rise - Hold) / Sink);
				if (q >= 1f)
				{
					// spent: park BEHIND the map immediately rather than waiting on finish().
					// The juice manager finishes entries on turn cadence, which can lag well
					// past the handoff -- and a lagging OPAQUE held sprite over the real wall
					// was the "error shale": a flat grid-aligned stone block wearing sprite
					// tiles, healing only when the sweep finally ran (2026-08-04 diagnosis;
					// the walls beneath were painted correctly the whole time).
					Target.transform.position = Base + new Vector3(0f, BuriedY, 100f);
					return;
				}
				y = Mathf.Lerp(EmergeY, BuriedY, Easing.SineEaseInOut(q));
			}
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

		public const string ISOLATED_MASK = "00000000";
		private const string DEFAULT_ATLAS = "Assets_Content_Textures_Tiles_"; // the painter's own default (ZoneManager.PaintWall)
		private const string DEFAULT_EXT = ".bmp";

		/// <summary>Tile path composed the zone painter's own way -- PaintedWallAtlas prefix +
		/// paint value + 8-digit connectivity suffix + extension -- with NO atlas probing.
		/// TryGetTextureInfo's miss path loads textures under the atlas WRITE lock, and
		/// calling it from the game thread deadlocked against the Unity side twice
		/// (2026-08-04; the second freeze hit during cold-cache pre-resolve with only ambient
		/// juice running). The base game never touches the atlas from the game thread: paths
		/// resolve to textures Unity-side inside GetPooledSprite, and a variant the atlas
		/// doesn't stock renders as the engine's blank InvalidInfo with a one-time warning
		/// in Player.log ("No texture found by ID") -- annoying, never fatal.</summary>
		private static string ComposeTile(string paint, string atlas, string ext, string mask)
		{
			if (paint.IsNullOrEmpty())
				paint = "wall_rock";
			if (atlas.IsNullOrEmpty())
				atlas = DEFAULT_ATLAS;
			if (ext.IsNullOrEmpty())
				ext = DEFAULT_EXT;
			return atlas + paint + "-" + mask + ext;
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
		// re-dialed 2026-08-04 twice (user: "plates could be a bit less fractured", then "bit
		// more fragmentation would be good on the wave also") -- the axes split: the TRAVELING
		// wave breaks busier, the rim's rampart slabs stay big
		private const int TRAVEL_PLATE_MIN = 2;
		private const int TRAVEL_PLATE_MAX = 4;
		private const int TRAVEL_SHARD_CHANCE = 12; // % of cuts that come off as a lone shard
		private const int RIM_PLATE_MIN = 3;
		private const int RIM_PLATE_MAX = 6;
		private const int RIM_SHARD_CHANCE = 8;
		// Calcify's raise (feel 2026-08-04: "rising of stones matching the order in which you
		// selected tiles, with a slight offset"). The lead-in also gives the Unity juice queue
		// its spin-up time -- the all-at-once slab lived inside 0.24s and startup latency ate
		// it ("the walls aren't rendering during the animation").
		private const float CALC_LEAD_SEC = 0.10f;
		private const float CALC_STEP_SEC = 0.07f; // per-wall offset down the picked line
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
		public static Schedule Play(Cell origin, int radius, ICollection<Cell> reached, ICollection<Cell> gaps, string paintValue, string paintAtlas, string paintExt, char paintFg, char paintDetail, char paintBg, Func<int, int, int, bool> onRing)
		{
			if (origin == null)
				return null;
			GameManager gm = GameManager.Instance;
			if (gm == null)
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
			List<List<Cell>>[] shellPlates = new List<List<Cell>>[radius];
			for (int r = 1; r < radius; r++)
			{
				if (shells[r] != null)
					shellPlates[r] = CutPlates(SortByAngle(shells[r], origin), TRAVEL_PLATE_MIN, TRAVEL_PLATE_MAX, TRAVEL_SHARD_CHANCE);
			}
			List<List<Cell>> rimPlates = CutPlates(SortByAngle(rimFabric, origin), RIM_PLATE_MIN, RIM_PLATE_MAX, RIM_SHARD_CHANCE);
			Helpers.VerifyLog("FIS", $"anim tiles composed painter-style: {(paintAtlas.IsNullOrEmpty() ? DEFAULT_ATLAS : paintAtlas)}{(paintValue.IsNullOrEmpty() ? "wall_rock" : paintValue)}-<mask>{(paintExt.IsNullOrEmpty() ? DEFAULT_EXT : paintExt)}");
			int enqueued = 0;
			// travel shells: fractured plates surging outward, one shared delay per plate
			for (int r = 1; r < radius; r++)
			{
				if (shellPlates[r] == null)
					continue;
				foreach (List<Cell> plate in shellPlates[r])
				{
					float delay = r * SHELL_SEC + 0.01f * Stat.Random(0, PLATE_JITTER_STEPS);
					enqueued += EnqueuePlate(plate, gm, paintValue, paintAtlas, paintExt, paintFg, paintDetail, paintBg,
						delay, TRAVEL_RISE, TRAVEL_HOLD, TRAVEL_SINK, TRAVEL_EMERGE, schedule, Settle: false);
				}
			}
			// the rim: plates of rampart punching through together, held until the fabric lands
			foreach (List<Cell> plate in rimPlates)
			{
				float delay = rimStart + 0.01f * Stat.Random(0, (int)(RIM_STAGGER * 100f));
				enqueued += EnqueuePlate(plate, gm, paintValue, paintAtlas, paintExt, paintFg, paintDetail, paintBg,
					delay, RIM_RISE, RIM_HOLD, RIM_SINK, RIM_EMERGE, schedule, Settle: true);
			}
			// gaps: lone shards, their own stagger, rise-falter-sink
			string gapTile = ComposeTile(paintValue, paintAtlas, paintExt, ISOLATED_MASK);
			foreach (Cell c in rimGaps)
			{
				float delay = rimStart + 0.01f * Stat.Random(0, (int)(RIM_STAGGER * 100f));
				IRenderable tile = new Cleo_TerraFirma_StoneTileRenderable(gapTile, paintFg, paintDetail, 'k');
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(tile, gm.getTileCenter(c.X, c.Y), delay, RIM_RISE, 0.04f, RIM_SINK, BURIED_DEPTH, RIM_EMERGE * 0.7f), async: true);
				schedule.GapTimes[c] = delay + RIM_RISE + 0.04f;
				enqueued++;
			}
			return enqueued > 0 ? schedule : null;
		}

		/// <summary>Calcify's wall raise (VFX pass, flagged + user-confirmed 2026-08-04; ripple
		/// re-spec same day): one connected slab -- full-adjacency masks so an L-bend still
		/// fuses -- rising in PICK ORDER, each wall a slight beat after the last (the stone
		/// follows your gesture; an NPC's line ripples outward from its anchor). Every tile
		/// is pre-resolved before the first enqueue (the freeze lesson); each cell dresses
		/// from its OWN blueprint (the sticky borrow can change stone mid-line). Returns the
		/// settle schedule -- per-cell handoff moments -- or null if nothing animates; the
		/// caller places each real wall as its moment arrives.</summary>
		public static Schedule PlayWallRise(List<Cell> cells, List<string> blueprints)
		{
			GameManager gm = GameManager.Instance;
			if (gm == null || cells == null || cells.Count == 0)
				return null;
			string[] tiles = new string[cells.Count];
			char[] fgs = new char[cells.Count];
			char[] details = new char[cells.Count];
			char[] bgs = new char[cells.Count];
			for (int i = 0; i < cells.Count; i++)
			{
				ResolveWallDressing(i < blueprints.Count ? blueprints[i] : null, out string paint, out string atlas, out string ext, out fgs[i], out details[i], out bgs[i]);
				tiles[i] = ComposeTile(paint, atlas, ext, MaskForSet(cells, i));
			}
			Schedule schedule = new Schedule();
			int enqueued = 0;
			for (int i = 0; i < cells.Count; i++)
			{
				if (!cells[i].IsVisible())
					continue;
				float delay = CALC_LEAD_SEC + i * CALC_STEP_SEC;
				// black backing always (user call 2026-08-04, "put a black mask on the bottom
				// layer of the animation": the material caret backing -- shale's ^w light
				// brown -- made every holding sprite a light-tan impostor over the dark wall)
				IRenderable tile = new Cleo_TerraFirma_StoneTileRenderable(tiles[i], fgs[i], details[i], 'k');
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(tile, gm.getTileCenter(cells[i].X, cells[i].Y), delay, RIM_RISE, RIM_HOLD, RIM_SINK, BURIED_DEPTH, RIM_EMERGE), async: true);
				schedule.SettleTimes[cells[i]] = delay + RIM_RISE + RIM_HOLD;
				schedule.Total = Mathf.Max(schedule.Total, delay + RIM_RISE + RIM_HOLD);
				enqueued++;
			}
			return enqueued > 0 ? schedule : null;
		}

		/// <summary>Mask connected toward EVERY set-member within reach, unlike the arcs'
		/// chain masks: a calcified line is one deliberate rampart, bends included.</summary>
		private static string MaskForSet(List<Cell> set, int idx)
		{
			char[] mask = ISOLATED_MASK.ToCharArray();
			for (int j = 0; j < set.Count; j++)
			{
				if (j != idx)
					SetMaskBit(mask, set[idx], set[j]);
			}
			return new string(mask);
		}

		/// <summary>Paint family, atlas prefix + extension, and color triple straight off a
		/// wall blueprint: PaintedWall tag (first comma variant) and Render colors,
		/// inheritance already baked into the blueprint tree; the shared gray (&y^K over
		/// wall_rock) where a blueprint offers nothing -- the same fallback the settled
		/// fabric wears.</summary>
		private static void ResolveWallDressing(string blueprint, out string paint, out string atlas, out string ext, out char fg, out char detail, out char bg)
		{
			paint = "wall_rock";
			atlas = null;
			ext = null;
			fg = 'y';
			detail = 'K';
			bg = 'K';
			GameObjectBlueprint b = blueprint == null ? null : GameObjectFactory.Factory.GetBlueprintIfExists(blueprint);
			if (b == null)
				return;
			string p = b.GetTag("PaintedWall");
			if (!p.IsNullOrEmpty())
				paint = p.Contains(",") ? p.Split(',')[0] : p;
			atlas = b.GetTag("PaintedWallAtlas", null);
			ext = b.GetTag("PaintedWallExtension", null);
			string tc = b.GetPartParameter<string>("Render", "TileColor");
			if (tc.IsNullOrEmpty())
				tc = b.GetPartParameter<string>("Render", "ColorString");
			if (!tc.IsNullOrEmpty())
			{
				int amp = tc.IndexOf('&');
				if (amp >= 0 && amp + 1 < tc.Length)
					fg = tc[amp + 1];
			}
			string dc = b.GetPartParameter<string>("Render", "DetailColor");
			if (!dc.IsNullOrEmpty())
				detail = dc[0];
			string cs = b.GetPartParameter<string>("Render", "ColorString");
			if (!cs.IsNullOrEmpty())
			{
				int caret = cs.IndexOf('^');
				if (caret >= 0 && caret + 1 < cs.Length)
					bg = cs[caret + 1];
			}
		}

		/// <summary>One plate: every cell's sprite wears the tile variant connected toward its
		/// arc-neighbors and shares the plate's delay -- the slab heaves as one piece.</summary>
		private static int EnqueuePlate(List<Cell> plate, GameManager gm, string paintValue, string atlas, string ext, char fg, char detail, char bg,
			float Delay, float Rise, float Hold, float Sink, float Emerge, Schedule schedule, bool Settle)
		{
			int enqueued = 0;
			for (int i = 0; i < plate.Count; i++)
			{
				string tilePath = ComposeTile(paintValue, atlas, ext, MaskFor(plate, i));
				Cell c = plate[i];
				IRenderable tile = new Cleo_TerraFirma_StoneTileRenderable(tilePath, fg, detail, 'k'); // black backing, same call as above
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(tile, gm.getTileCenter(c.X, c.Y), Delay, Rise, Hold, Sink, BURIED_DEPTH, Emerge), async: true);
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
		/// contiguous run into arcs of plateMin..plateMax with the occasional lone shard.
		/// A fully unbroken circle simply gets one arbitrary fracture where the walk began.</summary>
		private static List<List<Cell>> CutPlates(List<Cell> ring, int plateMin, int plateMax, int shardChance)
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
					int len = Stat.Random(1, 100) <= shardChance ? 1 : Stat.Random(plateMin, plateMax);
					len = Math.Min(len, r.Count - i);
					plates.Add(r.GetRange(i, len));
					i += len;
				}
			}
			return plates;
		}
	}
}
