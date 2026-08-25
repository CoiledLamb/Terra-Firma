using System;
using System.Collections.Generic;
using System.Linq;
using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// Warren v2 carve engine - FIRST PORT PASS 2026-08-23, ported line-faithful from
	/// design/warren-lab-v2.html (the sim leads by ruling 2026-08-07; this file catches
	/// up, never the reverse). Box layout only in this pass: blockMode is the sim's
	/// non-default A/B lane and rides a later port pass, as do the dress tables
	/// (registration is in; dressSite is not - roles are stamped and logged so the
	/// dress pass lands on the contract later with zero re-derivation).
	/// The REAL zone replaces genCanyon: host/water/tree masks are read from the
	/// built zone, so the carve answers actual canyon geometry, rivers included.
	/// All decisions log as [TF-VERIFY] [WARREN].
	/// </summary>
	public static class WarrenCarver
	{
		// ---------------------------------------------------------------- knobs
		// The sim's DEFAULTS, frozen at port time. Sim knob ids in comments.
		public const int ABANDONED_PCT = 25;      // abandonedPct
		public const int RECLAIMED_PCT = 20;      // reclaimedPct
		public const int AQUIFER_MIN = 0;         // aquiferMin
		public const int AQUIFER_MAX = 2;         // aquiferMax
		public const int POOL_MIN = 6;            // poolMin
		public const int POOL_MAX = 18;           // poolMax
		public const int VEIN_CHANCE = 70;        // veinChance
		public const int BREACH_CHANCE = 45;      // breachChance
		public const int BREACH_MAX = 2;          // breachMax
		public const int BREACH_REACH = 5;        // breachReach
		public const int MOSS_PCT = 35;           // mossPct (ported + dialed 2026-08-23)
		public const int MOMENTUM = 55;           // momentum
		public const int WANDER = 22;             // wander
		public const int MOUTH_RUN = 6;           // mouthRun
		public const int BOX_W0 = 6, BOX_W1 = 15; // boxW0/boxW1
		public const int BOX_H0 = 6, BOX_H1 = 9;  // boxH0/boxH1
		public const int PLAIN_W = 5;             // plainW
		public const int HALL_W = 9;              // hallW
		public const int COMMONS_PCT = 95;        // commonsPct (box layout figure)
		public const int MAX_SPAN = 3;            // maxSpan
		public const int LAT_Y = 2;               // latY
		public const int CLEARANCE = 1;           // clearance
		public const int PILLAR_FILL = 62;        // pillarFill
		public const int COLUMN_CHANCE = 25;      // columnChance
		public const int SPUR_CHANCE = 35;        // spurChance
		public const int PART_CHANCE = 55;        // partChance
		public const int PART_MAX = 2;            // partMax
		public const int STRADDLE = 70;           // straddle
		public const int PINCH_COST = 400;        // pinchCost
		public const int HUG_COST = 260;          // hugCost
		public const int EMPTY_BASE = 200;        // emptyBase (Redrock's own figure)
		public const int ATTEMPTS = 4;            // attempts
		public const int MIN_SITE = 70;           // minSite ("worth the trip")
		// RULED 2026-08-23 ("like all three of these fixes"): the face gate is a
		// PREDICTOR, not the bar - licensing (MIN_SITE + 2 rooms) stays the ruled
		// judge of what got built. 400 (the smallest stage's area budget) was
		// over-predicting refusal on lobes that could honestly build 80+ floor.
		public const int FACE_PREDICTOR = 300;
		public const int FACE_SAMPLE = 40;        // faces tried per attempt (was 25; serrated skins hide clean throats)
		// blockMode (fix 2, same ruling): claim-and-subdivide, the sim's A/B lane,
		// ported as the snake-country specialist. Boxes stay the DEFAULT look (the
		// user's 08-18 A/B lean); blocks run when a face's box carve under-builds
		// below licensing (retry-with-blocks policy: CLAUDE proposal in the ruling).
		public const int BLOCK_MIN = 50;          // blockMin
		public const int BLOCK_MAX = 95;          // blockMax
		public const int DEN_MAX = 24;            // denMax (FillRoom's own number)
		public const int CAMP_FLOOR = 130;        // campFloor
		public const int FLOUR_FLOOR = 210;       // flourFloor
		public const int DRIFT_MAX = 10;          // driftMax (unused in box mode's tunnels)

		private class Stage
		{
			public string Name;
			public int P0, P1;        // pockets (box layout)
			public int C0, C1;        // claims (block layout)
			public int Cross;
			public int Wp0, Wp1;      // waypoints
			public int Area;
			public int Fill0, Fill1;
		}
		private static readonly Stage[] STAGES = {
			new Stage{ Name="working",     P0=3, P1=5, C0=1, C1=2, Cross=2, Wp0=1, Wp1=2, Area=400,  Fill0=1, Fill1=2 },
			new Stage{ Name="camp",        P0=4, P1=7, C0=2, C1=3, Cross=4, Wp0=2, Wp1=3, Area=680,  Fill0=1, Fill1=3 },
			new Stage{ Name="flourishing", P0=6, P1=9, C0=3, C1=4, Cross=7, Wp0=3, Wp1=4, Area=1000, Fill0=2, Fill1=4 },
		};

		// ---------------------------------------------------------------- rng
		// mulberry32, ported bit-exact so a sim seed and a port seed can be compared.
		private static uint _state;
		private static double Rng()
		{
			unchecked
			{
				uint z = (_state += 0x6D2B79F5u);
				uint t = (z ^ (z >> 15)) * (1u | z);
				t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
				return (t ^ (t >> 14)) / 4294967296.0;
			}
		}
		private static int R(int lo, int hi) => lo + (int)(Rng() * (Math.Max(lo, hi) - lo + 1));
		private static bool Chance(double p) => Rng() < p;
		private static T Pick<T>(IReadOnlyList<T> a) => a[(int)(Rng() * a.Count)];

		/// <summary>Stable FNV-1a over the zone id: the port's ZONESEED.</summary>
		public static int SeedFor(string zoneID)
		{
			unchecked
			{
				uint h = 2166136261u;
				foreach (char c in zoneID) { h ^= c; h *= 16777619u; }
				return (int)h;
			}
		}

		// ---------------------------------------------------------------- grid
		private const int SOLID = 0, FLOOR = 1;
		private class Grid
		{
			public int W, H;
			public byte[] C, Art, Host, Water, Aqua, Tree, Region, Marker;
			public short[] Pocket;
			public string[] Item;           // the dress layer: sim item ids, translated at placement
			public string YardBoardText;    // the yard chalkboard's ledger lines (user concept 2026-08-23)
			public int MouthIdx = -1;
			public int[] MouthDist;
			public Grid(int w, int h)
			{
				W = w; H = h;
				int n = w * h;
				C = new byte[n]; Art = new byte[n]; Host = new byte[n]; Water = new byte[n];
				Aqua = new byte[n]; Tree = new byte[n]; Marker = new byte[n];
				Pocket = new short[n];
				for (int i = 0; i < n; i++) Pocket[i] = -1;
			}
			public int Idx(int x, int y) => y * W + x;
			public bool Inb(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
		}
		private static readonly int[][] N4 = { new[]{1,0}, new[]{-1,0}, new[]{0,1}, new[]{0,-1} };
		private static readonly int[][] N8 = { new[]{1,0}, new[]{-1,0}, new[]{0,1}, new[]{0,-1}, new[]{1,1}, new[]{1,-1}, new[]{-1,1}, new[]{-1,-1} };

		private static bool Wf(Grid g, int x, int y) => g.C[g.Idx(x, y)] == FLOOR && g.Host[g.Idx(x, y)] == 0;

		// A corridor is ONE cell wide and never runs alongside floor it is not joining.
		private static bool SideClear(Grid g, int x, int y, int fx, int fy)
		{
			foreach (var d in N4)
			{
				int nx = x + d[0], ny = y + d[1];
				if (nx == fx && ny == fy) continue;
				if (g.Inb(nx, ny) && g.C[g.Idx(nx, ny)] == FLOOR) return false;
			}
			return true;
		}

		private class Box { public int X0, Y0, X1, Y1; }
		private class Pt { public int X, Y; }
		private class PocketRec
		{
			public int Id;
			public Box BoxOut, In;
			public int Through, Partitions, Pillars, Spurs, Entrances;
			public int W, H;
			public string Character, Role;
			public bool Live, Infill;
			public int Depth, DepthRank;
			public List<int> Footprint;   // blockyard only: actual floor cells (idx)
			public List<int[]> PillarCells = new List<int[]>();   // the commons tables land on the carve's own articulation
			public List<int[]> SpurCells = new List<int[]>();
		}
		private class Artery
		{
			public Pt Mouth;
			public List<Pt> Path;
			public Box Foot;
			public byte[] Mask;
		}

		// ---------------------------------------------------------------- host read
		// The real zone replaces genCanyon. host = every already-open cell (canyon
		// floor, river, clearings); water = open liquid; tree = solid non-wall
		// objects standing on open ground (dogthorn and kin). Walls that are not
		// natural stone are not ours to cut: marked host AND water so no guard
		// targets them, no mouth opens onto them, no tunnel eats them.
		private static Grid ReadHost(Zone Z)
		{
			var g = new Grid(Z.Width, Z.Height);
			for (int y = 0; y < g.H; y++)
				for (int x = 0; x < g.W; x++)
				{
					Cell c = Z.GetCell(x, y);
					int i = g.Idx(x, y);
					if (c == null) { g.Host[i] = 1; g.Water[i] = 1; continue; }
					if (c.HasWall())
					{
						// the house stone call (harmonized 2026-08-04): natural stone
						// answers, everything constructed refuses - same gate as the
						// Fissure carve and Calcify's wall borrowing
						GameObject wall = c.GetWalls().FirstOrDefault();
						bool natural = XRL.World.Parts.Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(
							wall?.GetPart<XRL.World.Parts.Cleo_TerraFirma_StoneshaperWallProperties>());
						if (!natural)
						{
							g.Host[i] = 1; g.Water[i] = 1;   // hard wall: not rock, not walkable, never a mouth
						}
						continue;                             // natural rock: SOLID, ours
					}
					g.C[i] = FLOOR; g.Host[i] = 1;
					if (c.HasObject("CanyonMarker")) g.Marker[i] = 1;   // the canyon's own breadcrumbs (CanyonBuilder stamps every corridor cell)
					if (c.HasObjectWithPart("LiquidVolume")) g.Water[i] = 1;
					else if (c.IsSolid()) g.Tree[i] = 1;      // solid non-wall on open ground
				}
			// census, because the first live zone refused with "no face opens onto
			// enough rock" (2026-08-23) and the numbers settle every host-read theory
			int rock = 0, hard = 0, open = 0, water = 0, trees = 0;
			for (int i = 0; i < g.W * g.H; i++)
			{
				if (g.C[i] == FLOOR) { open++; if (g.Water[i] != 0) water++; if (g.Tree[i] != 0) trees++; }
				else if (g.Host[i] != 0) hard++;
				else rock++;
			}
			Helpers.VerifyLog("WARREN", $"host census: {rock} natural rock, {hard} hard walls, {open} open ({water} water, {trees} trees)");
			return g;
		}

		// ---------------------------------------------------------------- band
		private class Band { public int Mass; public List<int> Cells; }
		private static Band LargestBand(Grid g)
		{
			var lab = new int[g.W * g.H];
			for (int i = 0; i < lab.Length; i++) lab[i] = -1;
			var comps = new List<List<int>>();
			for (int y = 0; y < g.H; y++)
				for (int x = 0; x < g.W; x++)
				{
					int i0 = g.Idx(x, y);
					// PORT FIX 2026-08-23: the band is CARVABLE rock only. The sim
					// had no host-marked solids; the real zone does (hard walls),
					// and counting them inflated the band and seated mouth
					// candidates on cells the region growth then refused.
					if (g.C[i0] == FLOOR || g.Host[i0] != 0 || lab[i0] >= 0) continue;
					int id = comps.Count;
					var acc = new List<int>();
					var st = new Stack<int>(); st.Push(i0); lab[i0] = id;
					while (st.Count > 0)
					{
						int ci = st.Pop(); acc.Add(ci);
						int cx = ci % g.W, cy = ci / g.W;
						foreach (var d in N4)
						{
							int nx = cx + d[0], ny = cy + d[1];
							if (!g.Inb(nx, ny)) continue;
							int ni = g.Idx(nx, ny);
							if (lab[ni] < 0 && g.C[ni] != FLOOR) { lab[ni] = id; st.Push(ni); }
						}
					}
					comps.Add(acc);
				}
			if (comps.Count == 0) return null;
			var best = comps.OrderByDescending(c => c.Count).First();
			return new Band { Mass = best.Count, Cells = best };
		}

		// region = every band cell reachable from the mouth out to a budget, keeping
		// a one-cell rind against the canyon everywhere except the mouth (N8: a
		// diagonal slip into the canyon is a second mouth the N4 count never saw).
		// ROUND 4 (2026-08-23, RULED at the user's posted archipelago zone: "i could
		// see it being a valid quarry spot, we just have to get smarter about how
		// we're placing them"): real rock is shore-heavy and pinch-riddled, and the
		// sim conflated NOT CARVABLE with NOT TRAVERSABLE. Two classes are now
		// pass-through-but-unclaimable for the flood: the rind (rock beside the
		// EDGE-CONNECTED canyon - isolated bubbles no longer poison a ring) and the
		// bubbles themselves (pre-carved interior air; they can never be a mouth,
		// so crossing them risks nothing). The one-mouth law holds untouched: the
		// carve still only opens cells the mask claims, and the mask never holds a
		// rind or bubble cell. Sim catch-up flagged in TODO for all three parts.
		private class RegionRec { public byte[] Mask; public int Size; public List<int> Cells; }
		private static RegionRec GrowRegion(Grid g, Pt mouth, int budget, byte[] connected)
		{
			bool OnRind(int x, int y) => N8.Any(d => g.Inb(x + d[0], y + d[1]) && connected[g.Idx(x + d[0], y + d[1])] != 0);
			var mask = new byte[g.W * g.H];
			var seen = new byte[g.W * g.H];
			var q = new List<int> { g.Idx(mouth.X, mouth.Y) };
			seen[q[0]] = 1; mask[q[0]] = 1;             // the mouth is exempt by construction
			var cells = new List<int> { q[0] };          // claimable cells in flood order = depth
			int n = 1, head = 0;
			while (head < q.Count && n < budget)
			{
				int ci = q[head++];
				int x = ci % g.W, y = ci / g.W;
				foreach (var d in N4)
				{
					int nx = x + d[0], ny = y + d[1];
					if (!g.Inb(nx, ny)) continue;
					int ni = g.Idx(nx, ny);
					if (seen[ni] != 0) continue;
					if (nx < 1 || ny < 1 || nx > g.W - 2 || ny > g.H - 2) continue;
					if (connected[ni] != 0) continue;          // the canyon proper: hard stop
					bool bubble = g.C[ni] == FLOOR;            // open but not edge-connected
					bool rind = !bubble && OnRind(nx, ny);
					seen[ni] = 1;
					// ROUND 7 (2026-08-23, the 1-of-1000 starfish): the rind is
					// impassable again as well as unclaimable - SIM-FAITHFUL, the
					// sim's flood never crossed its skin; round 4's pinch-crossing
					// was port over-reach that let a face with a 1-cell lobe size
					// itself on mass it could never walk to, building mouth-anchored
					// starfish. Faces with tiny lobes now lose to the next face in
					// the try order. Bubbles stay crossable: the one real terrain
					// class the sim lacks, and they can never be a mouth.
					if (rind) continue;
					q.Add(ni);                                 // rock and bubbles are traversable
					if (bubble) continue;                      // pre-carved air: crossed, never claimed
					mask[ni] = 1; cells.Add(ni); n++;
					if (n >= budget) break;
				}
			}
			return new RegionRec { Mask = mask, Size = n, Cells = cells };
		}

		// ---------------------------------------------------------------- artery
		private static Artery LayArtery(Grid g, Stage S, Pt mouth, Box foot, byte[] mask, List<int> regionCells)
		{
			int nWp = R(S.Wp0, S.Wp1);
			var bands = Enumerable.Range(0, nWp).ToList();
			for (int i = bands.Count - 1; i > 0; i--) { int j = R(0, i); (bands[i], bands[j]) = (bands[j], bands[i]); }
			var wps = new List<Pt>();
			// the region flooded OUT from the mouth, so its cell order IS depth:
			// one waypoint per shell pulls the artery along the seam.
			int nC = regionCells.Count;
			foreach (int b in bands)
			{
				int lo = (int)Math.Floor((b + 0.35) * nC / nWp);
				int hi = Math.Min(nC - 1, (int)Math.Floor((double)(b + 1) * nC / nWp) - 1);
				int wi = R(Math.Min(lo, hi), Math.Max(lo, hi));
				// ROUND 6 (2026-08-23, the rebuild-loop dialog): a tiny lobe makes hi
				// -1 and wi negative. The SIM survived this - JS cs[-1] is undefined
				// and its if(c) guard swallowed it - so the guard WAS the behavior,
				// and dropping it in the port turned a skipped waypoint into a
				// deterministic throw, which the zone-seeded rebuild replays forever.
				if (wi < 0 || wi >= nC) continue;
				int ci = regionCells[wi];
				wps.Add(new Pt { X = ci % g.W, Y = ci / g.W });
			}
			if (wps.Count == 0 && nC > 0)
				wps.Add(new Pt { X = regionCells[nC - 1] % g.W, Y = regionCells[nC - 1] / g.W });

			var path = new List<Pt>();
			var cur = new Pt { X = mouth.X, Y = mouth.Y };
			int[] lastDir = null, wanderDir = null;
			int wander = 0;
			g.C[g.Idx(cur.X, cur.Y)] = FLOOR; g.Art[g.Idx(cur.X, cur.Y)] = 1;
			path.Add(new Pt { X = cur.X, Y = cur.Y });
			foreach (var wp in wps)
			{
				int guard = 0;
				while ((cur.X != wp.X || cur.Y != wp.Y) && guard++ < 400)
				{
					int dx = Math.Sign(wp.X - cur.X), dy = Math.Sign(wp.Y - cur.Y);
					int[] dir = null;
					// a JOG is a committed run at right angles to the goal - it winds.
					// The stretch you use most gets straightened: no jogs out of the mouth.
					if (wander > 0) { dir = wanderDir; wander--; }
					else if (path.Count >= MOUTH_RUN && Chance(WANDER / 100.0))
					{
						wanderDir = dx != 0 ? new[] { 0, Chance(.5) ? 1 : -1 } : new[] { Chance(.5) ? 1 : -1, 0 };
						wander = R(1, 3); dir = wanderDir;
					}
					if (dir == null && lastDir != null && Chance(MOMENTUM / 100.0))
					{
						int nx0 = cur.X + lastDir[0], ny0 = cur.Y + lastDir[1];
						if (g.Inb(nx0, ny0) && Math.Abs(nx0 - wp.X) + Math.Abs(ny0 - wp.Y) < Math.Abs(cur.X - wp.X) + Math.Abs(cur.Y - wp.Y))
							dir = lastDir;
					}
					if (dir == null)
					{
						var opts = new List<int[]>();
						if (dx != 0) opts.Add(new[] { dx, 0 });
						if (dy != 0) opts.Add(new[] { 0, dy });
						if (opts.Count == 0) break;
						dir = opts.Count == 1 ? opts[0] : (Chance(.6) ? opts[0] : opts[1]);
					}
					// a jog that would leave the region is abandoned, not clamped
					if (wander > 0)
					{
						int tx = cur.X + dir[0], ty = cur.Y + dir[1];
						if (!g.Inb(tx, ty) || mask[g.Idx(tx, ty)] == 0) { wander = 0; continue; }
					}
					int nx = Math.Min(g.W - 1, Math.Max(0, cur.X + dir[0]));
					int ny = Math.Min(g.H - 1, Math.Max(0, cur.Y + dir[1]));
					if (g.Host[g.Idx(nx, ny)] != 0) break;     // the haul route never eats the canyon
					if (mask[g.Idx(nx, ny)] == 0) break;       // and stays inside the site's own rock
					if (!SideClear(g, nx, ny, cur.X, cur.Y))
					{
						int[] alt = dir[0] != 0
							? new[] { 0, Math.Sign(wp.Y - cur.Y) != 0 ? Math.Sign(wp.Y - cur.Y) : (Chance(.5) ? 1 : -1) }
							: new[] { Math.Sign(wp.X - cur.X) != 0 ? Math.Sign(wp.X - cur.X) : (Chance(.5) ? 1 : -1), 0 };
						int ax = Math.Min(g.W - 1, Math.Max(0, cur.X + alt[0]));
						int ay = Math.Min(g.H - 1, Math.Max(0, cur.Y + alt[1]));
						if (SideClear(g, ax, ay, cur.X, cur.Y)) { nx = ax; ny = ay; dir = alt; }
					}
					cur = new Pt { X = nx, Y = ny }; lastDir = dir;
					g.C[g.Idx(nx, ny)] = FLOOR; g.Art[g.Idx(nx, ny)] = 1;
					path.Add(new Pt { X = nx, Y = ny });
				}
			}
			return new Artery { Mouth = mouth, Path = path, Foot = foot, Mask = mask };
		}

		// ---------------------------------------------------------------- pockets
		private static List<PocketRec> FindPockets(Grid g, Artery art, int budget, bool wantHall)
		{
			var pockets = new List<PocketRec>();
			bool Attempt(int w, int h, Pt anchor, bool straddleIt)
			{
				int ox, oy;
				if (straddleIt)
				{
					ox = anchor.X - R(1, Math.Max(1, w - 2));
					oy = anchor.Y - R(1, Math.Max(1, h - 2));
				}
				else
				{
					ox = anchor.X + R(-w + 1, 0) + (Chance(.5) ? 0 : R(-2, 2));
					oy = anchor.Y + (Chance(.5) ? R(-h, -1) : 1);
				}
				int x0 = Math.Max(1, Math.Min(g.W - 2 - w, ox)), y0 = Math.Max(1, Math.Min(g.H - 2 - h, oy));
				var box = new Box { X0 = x0, Y0 = y0, X1 = x0 + w - 1, Y1 = y0 + h - 1 };
				if (box.X1 >= g.W - 1 || box.Y1 >= g.H - 1) return false;
				for (int y = box.Y0; y <= box.Y1; y++)
					for (int x = box.X0; x <= box.X1; x++)
						if (g.Host[g.Idx(x, y)] != 0 || art.Mask[g.Idx(x, y)] == 0) return false;
				foreach (var p in pockets)
					if (!(box.X1 + 1 < p.BoxOut.X0 - 1 || box.X0 - 1 > p.BoxOut.X1 + 1 || box.Y1 + 1 < p.BoxOut.Y0 - 1 || box.Y0 - 1 > p.BoxOut.Y1 + 1)) return false;
				int through = 0;
				for (int y = box.Y0; y <= box.Y1; y++)
					for (int x = box.X0; x <= box.X1; x++)
						if (g.Art[g.Idx(x, y)] != 0) through++;
				if (through == 0)
				{
					bool nearRoad = art.Path.Any(q => q.X >= box.X0 - 3 && q.X <= box.X1 + 3 && q.Y >= box.Y0 - 3 && q.Y <= box.Y1 + 3);
					if (!nearRoad) return false;
				}
				pockets.Add(new PocketRec { BoxOut = box, Through = through, Id = pockets.Count });
				return true;
			}
			// COMMONS GUARANTEE (ruled: present 50-75%, never 100%): dedicated pass
			// before the budget loop, front 40% of the haul road preferred, whole
			// road accepted; width is the hall's identity and never shrinks. If 1500
			// tries cannot seat one, the site has no commons - honest failure.
			if (wantHall && art.Path.Count > 6)
			{
				int frontEnd = Math.Min(art.Path.Count - 1, Math.Max(8, (int)(art.Path.Count * 0.4)));
				for (int t = 0; t < 3000 && pockets.Count == 0; t++)
				{
					int sq = Math.Min(2, t / 800);
					int hi = t < 1500 ? frontEnd : art.Path.Count - 1;
					Attempt(t < 2000 ? R(HALL_W + 2, HALL_W + 4) : HALL_W + 2,
							t < 2000 ? R(Math.Max(5, BOX_H0 - sq), Math.Max(6, BOX_H1 - sq * 2)) : 5,
							art.Path[R(2, hi)], t % 3 > 0);
				}
			}
			int tries = 0;
			while (pockets.Count < budget && tries++ < 900)
			{
				// the first pocket straddles an anchor near the MOUTH: the site
				// starts where you walk in (rule 3, rooms sit ON the route)
				bool early = pockets.Count == 0 && art.Path.Count > 6 && tries < 300;
				Pt anchor = early ? art.Path[R(2, Math.Min(8, art.Path.Count - 1))]
								  : art.Path[R(0, art.Path.Count - 1)];
				int sq = Math.Min(3, tries / 150);
				Attempt(R(Math.Max(5, BOX_W0 - sq), Math.Max(5, BOX_W1 - sq * 3)),
						R(Math.Max(5, BOX_H0 - sq), Math.Max(5, BOX_H1 - sq * 2)),
						anchor, early || Chance(STRADDLE / 100.0));
			}
			return pockets;
		}

		private static void ResolvePocket(Grid g, PocketRec p)
		{
			// the box's outer ring is the room's WALL and stays solid
			int x0 = p.BoxOut.X0 + 1, y0 = p.BoxOut.Y0 + 1, x1 = p.BoxOut.X1 - 1, y1 = p.BoxOut.Y1 - 1;
			p.In = new Box { X0 = x0, Y0 = y0, X1 = x1, Y1 = y1 };
			int w = x1 - x0 + 1, h = y1 - y0 + 1;
			for (int y = y0; y <= y1; y++)
				for (int x = x0; x <= x1; x++) { g.C[g.Idx(x, y)] = FLOOR; g.Pocket[g.Idx(x, y)] = (short)p.Id; }
			p.W = w; p.H = h;
			p.Character = w <= PLAIN_W ? "plain" : (w >= HALL_W ? "hall" : "worked");

			// RULE 7 - partitions are PARTIAL: stubs off a wall that stop short
			p.Partitions = 0;
			if (w >= 6 && h >= 4 && Chance(PART_CHANCE / 100.0))
			{
				int n = R(1, PART_MAX);
				for (int i = 0; i < n; i++)
				{
					if (Chance(.6))
					{
						int px = R(x0 + 2, Math.Max(x0 + 2, x1 - 2));
						bool fromTop = Chance(.5);
						int len = R(2, Math.Max(2, h - 2));
						for (int k = 0; k < len; k++)
						{
							int py = fromTop ? y0 + k : y1 - k;
							if (py < y0 || py > y1 || g.Art[g.Idx(px, py)] != 0) break;
							g.C[g.Idx(px, py)] = SOLID;
						}
					}
					else
					{
						int py = R(y0 + 1, Math.Max(y0 + 1, y1 - 1));
						bool fromLeft = Chance(.5);
						int len = R(2, Math.Max(2, w - 3));
						for (int k = 0; k < len; k++)
						{
							int px = fromLeft ? x0 + k : x1 - k;
							if (px < x0 || px > x1 || g.Art[g.Idx(px, py)] != 0) break;
							g.C[g.Idx(px, py)] = SOLID;
						}
					}
					p.Partitions++;
				}
			}

			// RULE 8 - pillars by SPAN on a sparsely sampled lattice, clearance off
			// every wall, never on the haul lane (RULE 10)
			p.Pillars = 0; p.Spurs = 0;
			int cl = CLEARANCE;
			bool Free(int x, int y)
			{
				if (x < x0 + cl || x > x1 - cl || y < y0 + cl || y > y1 - cl) return false;
				if (g.C[g.Idx(x, y)] != FLOOR || g.Art[g.Idx(x, y)] != 0) return false;
				foreach (var d in N8)
				{
					int nx = x + d[0], ny = y + d[1];
					if (nx >= x0 && nx <= x1 && ny >= y0 && ny <= y1 && g.C[g.Idx(nx, ny)] == SOLID) return false;
				}
				return true;
			}
			if (p.Character != "plain")
			{
				int sx = MAX_SPAN + 1, sy = LAT_Y;
				int ox = x0 + cl + R(0, Math.Min(sx - 1, 2)), oy = y0 + cl + R(0, Math.Min(sy - 1, 1));
				for (int py = oy; py <= y1 - cl; py += sy)
					for (int px = ox; px <= x1 - cl; px += sx)
					{
						if (!Chance(PILLAR_FILL / 100.0)) continue;
						if (!Free(px, py)) continue;
						g.C[g.Idx(px, py)] = SOLID; p.Pillars++; p.PillarCells.Add(new[] { px, py });
						if (Chance(COLUMN_CHANCE / 100.0))
						{
							int gy = py + (Chance(.5) ? 1 : -1);
							if (Free(px, gy)) { g.C[g.Idx(px, gy)] = SOLID; p.Pillars++; p.PillarCells.Add(new[] { px, gy }); }
						}
					}
				// a lattice point inside the clearance margin merges into the wall as a SPUR
				for (int px = ox; px <= x1 - cl; px += sx)
				{
					if (!Chance(SPUR_CHANCE / 100.0)) continue;
					bool fromTop = Chance(.5);
					int len = R(1, 2);
					bool did = false;
					int[] tip = null;
					for (int k = 0; k < len; k++)
					{
						int sy2 = fromTop ? y0 + k : y1 - k;
						if (sy2 < y0 || sy2 > y1 || g.Art[g.Idx(px, sy2)] != 0 || g.C[g.Idx(px, sy2)] != FLOOR) break;
						g.C[g.Idx(px, sy2)] = SOLID; did = true; tip = new[] { px, sy2 };
					}
					if (did) { p.Spurs++; p.SpurCells.Add(tip); }
				}
			}
			p.Entrances = p.Through > 0 ? 1 : 0;
		}

		// ------------------------------------------------ LAYER 2 ALT: BLOCKS
		// Claim-and-subdivide, ported from the sim (carveBlocks/subdivide/finishRoom;
		// the BuildingTemplate/FillRoom voice and the organblock drawing's rule 7).
		// Stamped overlapping rectangles claim mass, hollow where fully surrounded,
		// FillRoom-style partition down to den size, deliberate 1-2 doorways onto
		// the road. Room count falls out of the division instead of being begged
		// from 900 failed box placements - the snake-country specialist.
		private static void FinishRoom(Grid g, List<int> comp, Box bb, List<PocketRec> pockets)
		{
			var p = new PocketRec
			{
				Id = pockets.Count,
				BoxOut = new Box { X0 = Math.Max(0, bb.X0 - 1), Y0 = Math.Max(0, bb.Y0 - 1), X1 = Math.Min(g.W - 1, bb.X1 + 1), Y1 = Math.Min(g.H - 1, bb.Y1 + 1) },
				In = new Box { X0 = bb.X0, Y0 = bb.Y0, X1 = bb.X1, Y1 = bb.Y1 },
			};
			p.W = bb.X1 - bb.X0 + 1; p.H = bb.Y1 - bb.Y0 + 1;
			p.Character = p.W <= PLAIN_W ? "plain" : (p.W >= HALL_W ? "hall" : "worked");
			foreach (int i in comp) g.Pocket[i] = (short)p.Id;
			if (p.Character != "plain")
			{
				int sx = MAX_SPAN + 1, sy = LAT_Y;
				int ox = bb.X0 + 1 + R(0, Math.Min(sx - 1, 2)), oy = bb.Y0 + 1 + R(0, Math.Min(sy - 1, 1));
				bool Free2(int x, int y)
				{
					if (!g.Inb(x, y)) return false;
					int i = g.Idx(x, y);
					if (g.Pocket[i] != p.Id || g.C[i] != FLOOR || g.Art[i] != 0) return false;
					foreach (var d in N8)
					{
						int nx = x + d[0], ny = y + d[1];
						if (g.Inb(nx, ny) && g.C[g.Idx(nx, ny)] == SOLID) return false;
					}
					return true;
				}
				for (int py = oy; py <= bb.Y1 - 1; py += sy)
					for (int px = ox; px <= bb.X1 - 1; px += sx)
					{
						if (!Chance(PILLAR_FILL / 100.0)) continue;
						if (!Free2(px, py)) continue;
						g.C[g.Idx(px, py)] = SOLID; p.Pillars++; p.PillarCells.Add(new[] { px, py });
						if (Chance(COLUMN_CHANCE / 100.0))
						{
							int gy = py + (Chance(.5) ? 1 : -1);
							if (Free2(px, gy)) { g.C[g.Idx(px, gy)] = SOLID; p.Pillars++; p.PillarCells.Add(new[] { px, gy }); }
						}
					}
			}
			pockets.Add(p);
		}

		private static void Subdivide(Grid g, List<int> comp, List<PocketRec> pockets, Func<bool> takeHall)
		{
			int x0 = g.W, y0 = g.H, x1 = 0, y1 = 0;
			foreach (int i in comp)
			{
				int x = i % g.W, y = i / g.W;
				if (x < x0) x0 = x; if (x > x1) x1 = x;
				if (y < y0) y0 = y; if (y > y1) y1 = y;
			}
			int w = x1 - x0 + 1, h = y1 - y0 + 1;
			var bb = new Box { X0 = x0, Y0 = y0, X1 = x1, Y1 = y1 };
			// the commons guarantee, block flavor: the first wide-enough region
			// on the mouth side stops dividing and stands as the hall
			if (w >= HALL_W && comp.Count >= HALL_W * 3 && takeHall())
			{ FinishRoom(g, comp, bb, pockets); return; }
			if (comp.Count <= DEN_MAX || (w <= 4 && h <= 4))
			{ FinishRoom(g, comp, bb, pockets); return; }
			bool vert = w > h; if (Chance(.25)) vert = !vert;   // FillRoom's own dice
			for (int attempt = 0; attempt < 8; attempt++)
			{
				int line = vert ? R(x0 + 2, Math.Max(x0 + 2, x1 - 2)) : R(y0 + 2, Math.Max(y0 + 2, y1 - 2));
				var wallCells = comp.Where(i => vert ? i % g.W == line : i / g.W == line).ToList();
				if (wallCells.Count < 3) continue;
				// the door is the GAP the wall leaves, biased to the ends
				int gap = wallCells[Chance(.6) ? (Chance(.5) ? 0 : wallCells.Count - 1) : R(1, wallCells.Count - 2)];
				int gap2 = -1;
				// rule 5, loops beat trees: 40% leave the second end open too
				if (Chance(.6) && wallCells.Count >= 4)
				{
					var ends = new List<int>();
					if (wallCells[0] != gap) ends.Add(wallCells[0]);
					if (wallCells[wallCells.Count - 1] != gap) ends.Add(wallCells[wallCells.Count - 1]);
					if (ends.Count > 0) gap2 = ends[R(0, ends.Count - 1)];
				}
				foreach (int i in wallCells) g.C[i] = SOLID;
				var rest = comp.Where(i => g.C[i] == FLOOR).ToList();
				var restSet = new HashSet<int>(rest);
				var seen = new HashSet<int>();
				var parts = new List<List<int>>();
				foreach (int i in rest)
				{
					if (seen.Contains(i)) continue;
					var part = new List<int> { i };
					var st = new Stack<int>(); st.Push(i); seen.Add(i);
					while (st.Count > 0)
					{
						int ci = st.Pop(); int cx = ci % g.W, cy = ci / g.W;
						foreach (var d in N4)
						{
							int nx = cx + d[0], ny = cy + d[1];
							if (!g.Inb(nx, ny)) continue;
							int ni = g.Idx(nx, ny);
							if (restSet.Contains(ni) && !seen.Contains(ni)) { seen.Add(ni); part.Add(ni); st.Push(ni); }
						}
					}
					parts.Add(part);
				}
				// a split that would orphan a CLOSET is retried - a den is a room, not a shelf
				if (parts.Count < 2 || parts.Any(pt => pt.Count < 8))
				{ foreach (int i in wallCells) g.C[i] = FLOOR; continue; }
				g.C[gap] = FLOOR;
				if (gap2 >= 0) g.C[gap2] = FLOOR;
				foreach (var part in parts) Subdivide(g, part, pockets, takeHall);
				return;
			}
			FinishRoom(g, comp, bb, pockets);
		}

		private static List<PocketRec> CarveBlocks(Grid g, Artery art, Stage S, bool wantHall)
		{
			var pockets = new List<PocketRec>();
			var claimed = new byte[g.W * g.H];
			int nClaims = R(S.C0, S.C1);
			bool hallLeft = wantHall;
			Func<bool> takeHall = () => { if (hallLeft) { hallLeft = false; return true; } return false; };
			for (int c = 0; c < nClaims; c++)
			{
				// seeds spread along the haul road, mouth outward (rule 1 + accretion)
				double t = Math.Min(.95, Math.Max(.05, (c + .5) / nClaims + R(-15, 15) / 100.0));
				var a = art.Path[Math.Min(art.Path.Count - 1, (int)(art.Path.Count * t))];
				int budget = R(BLOCK_MIN, BLOCK_MAX);
				var cells = new List<int[]>();
				int sx2 = a.X, sy2 = a.Y;
				for (int r2 = 0; r2 < 4 && cells.Count < budget; r2++)
				{
					int rw = R(6, 12), rh = R(4, 8);
					int rx = sx2 - R(1, rw - 2), ry = sy2 - R(1, rh - 2);
					for (int y = ry; y < ry + rh; y++)
						for (int x = rx; x < rx + rw; x++)
						{
							if (x < 1 || y < 1 || x >= g.W - 1 || y >= g.H - 1) continue;
							int ni = g.Idx(x, y);
							if (claimed[ni] != 0 || g.Host[ni] != 0) continue;
							if (art.Mask[ni] == 0) continue;
							bool wet = false;   // the wet-pocket guard holds
							foreach (var d in N8)
							{
								int wx = x + d[0], wy = y + d[1];
								if (g.Inb(wx, wy) && g.Aqua[g.Idx(wx, wy)] != 0) { wet = true; break; }
							}
							if (wet) continue;
							if (g.C[ni] != FLOOR) { cells.Add(new[] { x, y }); claimed[ni] = 1; }
						}
					// the next rect anchors inside this one, so the union stays one mass
					sx2 = rx + R(0, rw - 1); sy2 = ry + R(0, rh - 1);
				}
				if (cells.Count < BLOCK_MIN * 0.5) continue;
				// hollow: a claim cell opens only when everything around it is CLAIM -
				// the block walls itself against rock AND against the road
				var open = new List<int>();
				foreach (var cxy in cells)
				{
					bool ok = true;
					foreach (var d in N8)
					{
						int nx = cxy[0] + d[0], ny = cxy[1] + d[1];
						if (!g.Inb(nx, ny) || claimed[g.Idx(nx, ny)] == 0) { ok = false; break; }
					}
					if (ok) open.Add(g.Idx(cxy[0], cxy[1]));
				}
				foreach (int i in open) g.C[i] = FLOOR;
				var openSet = new HashSet<int>(open);
				var compSeen = new HashSet<int>();
				foreach (int i in open)
				{
					if (compSeen.Contains(i)) continue;
					var comp = new List<int> { i };
					var st = new Stack<int>(); st.Push(i); compSeen.Add(i);
					while (st.Count > 0)
					{
						int ci = st.Pop(); int cx = ci % g.W, cy = ci / g.W;
						foreach (var d in N4)
						{
							int nx = cx + d[0], ny = cy + d[1];
							if (!g.Inb(nx, ny)) continue;
							int ni = g.Idx(nx, ny);
							if (openSet.Contains(ni) && !compSeen.Contains(ni)) { compSeen.Add(ni); comp.Add(ni); st.Push(ni); }
						}
					}
					if (comp.Count >= 8) Subdivide(g, comp, pockets, takeHall);
					else foreach (int ci in comp) g.C[ci] = SOLID;   // slivers go back to rock
				}
				// DELIBERATE THRESHOLDS: 1-2 doorways where a block wall stands
				// between a room and the road
				var doors = new List<int[]>();
				foreach (var cxy in cells)
				{
					int i = g.Idx(cxy[0], cxy[1]);
					if (g.C[i] == FLOOR) continue;
					foreach (var d in new[] { new[]{1,0}, new[]{0,1} })
					{
						int ax = cxy[0] + d[0], ay = cxy[1] + d[1], bx2 = cxy[0] - d[0], by2 = cxy[1] - d[1];
						if (!g.Inb(ax, ay) || !g.Inb(bx2, by2)) continue;
						int a1 = g.Idx(ax, ay), b1 = g.Idx(bx2, by2);
						bool roomA = g.C[a1] == FLOOR && g.Pocket[a1] >= 0, roadB = g.C[b1] == FLOOR && g.Pocket[b1] < 0;
						bool roomB = g.C[b1] == FLOOR && g.Pocket[b1] >= 0, roadA = g.C[a1] == FLOOR && g.Pocket[a1] < 0;
						if ((roomA && roadB) || (roomB && roadA)) { doors.Add(cxy); break; }
					}
				}
				int want = R(1, 2);
				while (want-- > 0 && doors.Count > 0)
				{
					var dxy = doors[R(0, doors.Count - 1)];
					doors.Remove(dxy);
					g.C[g.Idx(dxy[0], dxy[1])] = FLOOR;
					for (int k = doors.Count - 1; k >= 0; k--)
						if (Math.Abs(doors[k][0] - dxy[0]) + Math.Abs(doors[k][1] - dxy[1]) < 4) doors.RemoveAt(k);
				}
			}
			// registration facts the box path gets from connectPocket
			foreach (var p in pockets)
			{
				for (int y = p.In.Y0; y <= p.In.Y1; y++)
					for (int x = p.In.X0; x <= p.In.X1; x++)
					{
						int i = g.Idx(x, y);
						if (g.Pocket[i] != p.Id || g.C[i] != FLOOR) continue;
						foreach (var d in N4)
						{
							int nx = x + d[0], ny = y + d[1];
							if (!g.Inb(nx, ny)) continue;
							int ni = g.Idx(nx, ny);
							if (g.C[ni] != FLOOR) continue;
							if (g.Art[ni] != 0) p.Through++;
							if (g.Pocket[ni] != p.Id) p.Entrances = 1;
						}
					}
			}
			return pockets;
		}

		// ---------------------------------------------------------------- tunneling
		// The engine's own cost field (ZoneBuilderSandbox.TunnelTo, decompiled
		// 2026-08-18) plus the two terms it was missing: hug and diagonal pinch.
		private static int _noiseSeed;
		private static double VNoise(double x, double y)
		{
			double Hsh(int a, int b)
			{
				unchecked
				{
					// JS Math.imul semantics; >>> is unsigned, so shift through uint
					int n = (a * 374761393) ^ (b * 668265263) ^ (_noiseSeed * (int)2246822519u);
					n = (n ^ (int)((uint)n >> 13)) * 1274126177;
					return ((uint)(n ^ (int)((uint)n >> 16)) / 4294967296.0) * 2 - 1;
				}
			}
			int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
			double fx = x - x0, fy = y - y0;
			double sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
			double a2 = Hsh(x0, y0), b2 = Hsh(x0 + 1, y0), c2 = Hsh(x0, y0 + 1), d2 = Hsh(x0 + 1, y0 + 1);
			double t = a2 + (b2 - a2) * sx, u = c2 + (d2 - c2) * sx;
			return t + (u - t) * sy;
		}

		private static double TunnelCost(Grid g, int x, int y, int emptyBase, bool noiseOn)
		{
			if (x == 0 || y == 0 || x == g.W - 1 || y == g.H - 1) return 2000;
			if (g.Host[g.Idx(x, y)] != 0) return 1e6;          // never the canyon
			double n = noiseOn ? Math.Abs(VNoise(x / 3.0, y)) * 160 : 0;
			// the SKIN against the canyon is priced as a last resort: finite, so a
			// boxed-in room can still get out, but dear enough to detour the zone
			if (N4.Any(d => g.Inb(x + d[0], y + d[1]) && g.Host[g.Idx(x + d[0], y + d[1])] != 0)) return 900 + n;
			if (g.C[g.Idx(x, y)] == FLOOR) return n + emptyBase;
			double hug = 0;
			foreach (var d in N4) if (g.Inb(x + d[0], y + d[1]) && Wf(g, x + d[0], y + d[1])) hug += HUG_COST;
			bool Open(int a, int b) => g.Inb(a, b) && g.C[g.Idx(a, b)] == FLOOR;
			foreach (var d in new[] { new[]{1,1}, new[]{1,-1}, new[]{-1,1}, new[]{-1,-1} })
				if (Open(x + d[0], y + d[1]) && !Open(x + d[0], y) && !Open(x, y + d[1])) hug += PINCH_COST;
			return 80 + n + hug;
		}

		private static List<int> Tunnel(Grid g, int ax, int ay, int bx, int by, int emptyBase, bool noiseOn, Func<int, int, double> costFn = null)
		{
			int SZ = g.W * g.H;
			var dist = new double[SZ]; for (int i = 0; i < SZ; i++) dist[i] = double.PositiveInfinity;
			var prev = new int[SZ]; for (int i = 0; i < SZ; i++) prev[i] = -1;
			var done = new byte[SZ];
			var heap = new List<(double c, int i)> { (0, ay * g.W + ax) };
			dist[ay * g.W + ax] = 0;
			void Push((double c, int i) v)
			{
				heap.Add(v);
				int i = heap.Count - 1;
				while (i > 0) { int p = (i - 1) >> 1; if (heap[p].c <= heap[i].c) break; (heap[p], heap[i]) = (heap[i], heap[p]); i = p; }
			}
			(double c, int i) Pop()
			{
				var t = heap[0]; var l = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1);
				if (heap.Count > 0)
				{
					heap[0] = l;
					int i = 0;
					while (true)
					{
						int a2 = 2 * i + 1, b2 = a2 + 1, m = i;
						if (a2 < heap.Count && heap[a2].c < heap[m].c) m = a2;
						if (b2 < heap.Count && heap[b2].c < heap[m].c) m = b2;
						if (m == i) break;
						(heap[m], heap[i]) = (heap[i], heap[m]); i = m;
					}
				}
				return t;
			}
			int goal = by * g.W + bx;
			while (heap.Count > 0)
			{
				var (c, cur) = Pop();
				if (done[cur] != 0) continue;
				done[cur] = 1;
				if (cur == goal) break;
				int cx = cur % g.W, cy = cur / g.W;
				// shuffled ties are where the organic look comes from
				var dirs = new List<int[]>(N4);
				for (int i = dirs.Count - 1; i > 0; i--) { int j = R(0, i); (dirs[i], dirs[j]) = (dirs[j], dirs[i]); }
				foreach (var d in dirs)
				{
					int nx = cx + d[0], ny = cy + d[1];
					if (!g.Inb(nx, ny)) continue;
					int ni = ny * g.W + nx;
					if (done[ni] != 0) continue;
					double w = costFn != null ? costFn(nx, ny) : TunnelCost(g, nx, ny, emptyBase, noiseOn);
					if (w >= 1e6) continue;
					double nc = c + w;
					if (nc < dist[ni]) { dist[ni] = nc; prev[ni] = cur; Push((nc, ni)); }
				}
			}
			if (prev[goal] < 0 && goal != ay * g.W + ax) return null;
			var path = new List<int>();
			int cur2 = goal;
			while (cur2 >= 0) { path.Add(cur2); cur2 = prev[cur2]; }
			path.Reverse();
			return path;
		}

		private static int CarveTunnel(Grid g, List<int> path)
		{
			int cut = 0;
			foreach (int i in path) if (g.C[i] != FLOOR) { g.C[i] = FLOOR; cut++; }
			return cut;
		}

		private static int ConnectPocket(Grid g, PocketRec p, Artery art, int? forceWant = null)
		{
			int want = forceWant ?? (p.Through > 0 ? (Chance(.4) ? 1 : 0) : R(1, 2));
			if (want <= 0) return 0;
			var mine = new List<int[]>();
			for (int y = p.In.Y0; y <= p.In.Y1; y++)
				for (int x = p.In.X0; x <= p.In.X1; x++)
					if (g.C[g.Idx(x, y)] == FLOOR) mine.Add(new[] { x, y });
			if (mine.Count == 0) return 0;
			var targets = art.Path.Where(q => g.C[g.Idx(q.X, q.Y)] == FLOOR).ToList();
			if (targets.Count == 0) return 0;
			int made = 0;
			for (int k = 0; k < want; k++)
			{
				var from = mine[R(0, mine.Count - 1)];
				var to = targets[R(0, targets.Count - 1)];
				var path = Tunnel(g, from[0], from[1], to.X, to.Y, EMPTY_BASE, true);
				if (path == null) continue;
				if (CarveTunnel(g, path) >= 0) { made++; p.Entrances++; }
			}
			return made;
		}

		// RULE 4/5 - crosscuts make circuits: close in space, far on the graph
		private static int AddCrosscuts(Grid g, int n)
		{
			int made = 0, tries = 0;
			var floors = new List<int[]>();
			for (int y = 1; y < g.H - 1; y++)
				for (int x = 1; x < g.W - 1; x++)
					if (Wf(g, x, y)) floors.Add(new[] { x, y });
			if (floors.Count < 4) return 0;
			while (made < n && tries++ < 60)
			{
				var a = floors[R(0, floors.Count - 1)];
				var b = floors[R(0, floors.Count - 1)];
				if (Math.Abs(a[0] - b[0]) + Math.Abs(a[1] - b[1]) < 6) continue;
				int d = GraphDist(g, a[0], a[1], b[0], b[1], 60);
				if (d >= 0 && d < 14) continue;
				var path = Tunnel(g, a[0], a[1], b[0], b[1], EMPTY_BASE, true);
				if (path == null) continue;
				if (CarveTunnel(g, path) > 0) made++;
			}
			return made;
		}

		// late pass: claim leftover mass as extra single-entrance rooms
		private static int Infill(Grid g, Artery art, List<PocketRec> pockets, int n)
		{
			int made = 0, tries = 0;
			while (made < n && tries++ < 500)
			{
				int w = R(6, 10), h = R(5, 8);
				int lo = Math.Max(1, art.Foot.X0), hi = Math.Min(g.W - 2 - w, art.Foot.X1 - w + 1);
				int lo2 = Math.Max(1, art.Foot.Y0), hi2 = Math.Min(g.H - 2 - h, art.Foot.Y1 - h + 1);
				if (hi < lo || hi2 < lo2) continue;
				var box = new Box { X0 = R(lo, hi), Y0 = R(lo2, hi2) };
				box.X1 = box.X0 + w - 1; box.Y1 = box.Y0 + h - 1;
				if (box.X1 >= g.W - 1 || box.Y1 >= g.H - 1) continue;
				bool ok = true;
				for (int y = box.Y0; y <= box.Y1 && ok; y++)
					for (int x = box.X0; x <= box.X1; x++)
						if (g.C[g.Idx(x, y)] != SOLID || art.Mask[g.Idx(x, y)] == 0) { ok = false; break; }
				if (!ok) continue;
				bool near = false;
				for (int y = box.Y0 - 2; y <= box.Y1 + 2 && !near; y++)
					for (int x = box.X0 - 2; x <= box.X1 + 2; x++)
						if (g.Inb(x, y) && g.C[g.Idx(x, y)] == FLOOR) { near = true; break; }
				if (!near) continue;
				var p = new PocketRec { BoxOut = box, Through = 0, Id = pockets.Count, Infill = true };
				pockets.Add(p);
				ResolvePocket(g, p);
				p.Entrances = 0;
				if (ConnectPocket(g, p, art, 1) == 0)
				{
					for (int y = p.In.Y0; y <= p.In.Y1; y++)
						for (int x = p.In.X0; x <= p.In.X1; x++)
						{ g.C[g.Idx(x, y)] = SOLID; g.Pocket[g.Idx(x, y)] = -1; }
					pockets.RemoveAt(pockets.Count - 1);
					continue;
				}
				made++;
			}
			return made;
		}

		// ---------------------------------------------------------------- geology
		private class AquiferRec { public int Pools, Veins; }
		private static AquiferRec PlaceAquifers(Grid g, Band band)
		{
			int n = R(AQUIFER_MIN, AQUIFER_MAX);
			var rec = new AquiferRec();
			if (n == 0) return rec;
			var interior = band.Cells.Where(i =>
			{
				int x = i % g.W, y = i / g.W;
				if (x <= 4 || y <= 3 || x >= g.W - 5 || y >= g.H - 4) return false;
				return !N8.Any(d => g.Inb(x + d[0], y + d[1]) && g.C[g.Idx(x + d[0], y + d[1])] == FLOOR);
			}).ToList();
			if (interior.Count == 0) return rec;
			void Wet(int x, int y)
			{
				if (!g.Inb(x, y) || x < 1 || y < 1 || x > g.W - 2 || y > g.H - 2) return;
				int i = g.Idx(x, y);
				g.C[i] = FLOOR; g.Host[i] = 1; g.Water[i] = 1; g.Aqua[i] = 1;
			}
			var nodes = new List<int[]>();
			for (int i = 0; i < n; i++)
			{
				int ci = interior[R(0, interior.Count - 1)];
				int cx = ci % g.W, cy = ci / g.W;
				if (g.Water[g.Idx(cx, cy)] != 0) continue;
				int want = R(POOL_MIN, POOL_MAX);
				var q = new List<int[]> { new[] { cx, cy } };
				var seen = new HashSet<int> { g.Idx(cx, cy) };
				int placed = 0;
				while (q.Count > 0 && placed < want)
				{
					int pi = R(0, q.Count - 1);
					var c = q[pi]; q.RemoveAt(pi);
					Wet(c[0], c[1]); placed++;
					foreach (var d in N4)
					{
						int nx = c[0] + d[0], ny = c[1] + d[1];
						if (!g.Inb(nx, ny) || seen.Contains(g.Idx(nx, ny)) || g.C[g.Idx(nx, ny)] == FLOOR) continue;
						seen.Add(g.Idx(nx, ny));
						if (Chance(.65)) q.Add(new[] { nx, ny });
					}
				}
				if (placed > 0) { rec.Pools++; nodes.Add(new[] { cx, cy }); }
			}
			// veins: each pool bores toward a random rock cell within 30 and fills it
			double Geology(int x, int y)
			{
				if (x == 0 || y == 0 || x == g.W - 1 || y == g.H - 1) return 2000;
				if (g.C[g.Idx(x, y)] == FLOOR && g.Aqua[g.Idx(x, y)] == 0) return 1e6;
				return 80 + Math.Abs(VNoise(x / 3.0, y)) * 160;
			}
			foreach (var node in nodes)
			{
				if (!Chance(VEIN_CHANCE / 100.0)) continue;
				var cands = new List<int[]>();
				for (int y = 1; y < g.H - 1; y++)
					for (int x = 1; x < g.W - 1; x++)
						if (g.C[g.Idx(x, y)] != FLOOR && Math.Abs(x - node[0]) + Math.Abs(y - node[1]) < 30) cands.Add(new[] { x, y });
				if (cands.Count == 0) continue;
				var t = cands[R(0, cands.Count - 1)];
				var path = Tunnel(g, node[0], node[1], t[0], t[1], 0, true, Geology);
				if (path == null) continue;
				foreach (int i in path) Wet(i % g.W, i / g.W);
				rec.Veins++;
			}
			return rec;
		}

		// sometimes the dig DID hit one: a short heading ending in standing water
		private static int BreachAquifers(Grid g)
		{
			if (!Chance(BREACH_CHANCE / 100.0)) return 0;
			var targets = new List<int[]>();
			for (int y = 1; y < g.H - 1; y++)
				for (int x = 1; x < g.W - 1; x++)
				{
					if (g.Aqua[g.Idx(x, y)] == 0) continue;
					foreach (var d in N4)
					{
						int rx = x + d[0], ry = y + d[1];
						if (!g.Inb(rx, ry) || g.C[g.Idx(rx, ry)] == FLOOR) continue;
						for (int dd = 1; dd <= BREACH_REACH; dd++)
						{
							int fx = rx + d[0] * dd, fy = ry + d[1] * dd;
							if (!g.Inb(fx, fy)) break;
							if (g.Host[g.Idx(fx, fy)] != 0) break;
							if (Wf(g, fx, fy)) { targets.Add(new[] { rx, ry, -d[0], -d[1], dd }); break; }
						}
					}
				}
			if (targets.Count == 0) return 0;
			int made = 0, want = R(1, BREACH_MAX);
			for (int k = 0; k < targets.Count && made < want; k++)
			{
				var t = targets[R(0, targets.Count - 1)];
				bool ok = true;
				for (int i = 0; i < t[4]; i++)
				{
					int cx = t[0] + t[2] * i, cy = t[1] + t[3] * i;
					if (!g.Inb(cx, cy) || g.Host[g.Idx(cx, cy)] != 0) { ok = false; break; }
				}
				if (!ok) continue;
				for (int i = 0; i < t[4]; i++) g.C[g.Idx(t[0] + t[2] * i, t[1] + t[3] * i)] = FLOOR;
				made++;
			}
			return made;
		}

		// ---------------------------------------------------------------- graph
		private static int GraphDist(Grid g, int ax, int ay, int bx, int by, int cap)
		{
			var seen = new byte[g.W * g.H];
			var q = new Queue<int[]>();
			q.Enqueue(new[] { ax, ay, 0 });
			seen[g.Idx(ax, ay)] = 1;
			while (q.Count > 0)
			{
				var c = q.Dequeue();
				if (c[0] == bx && c[1] == by) return c[2];
				if (c[2] >= cap) continue;
				foreach (var d in N4)
				{
					int nx = c[0] + d[0], ny = c[1] + d[1];
					if (g.Inb(nx, ny) && seen[g.Idx(nx, ny)] == 0 && g.C[g.Idx(nx, ny)] == FLOOR)
					{ seen[g.Idx(nx, ny)] = 1; q.Enqueue(new[] { nx, ny, c[2] + 1 }); }
				}
			}
			return -1;
		}

		// NAVIGATIONAL loops: contract each pocket blob to one node first
		private class Measure0 { public int Loops, Comps, Floor; }
		private static Measure0 MeasureG(Grid g)
		{
			var node = new int[g.W * g.H];
			for (int i = 0; i < node.Length; i++) node[i] = -1;
			int next = 0, floor = 0;
			for (int y = 0; y < g.H; y++)
				for (int x = 0; x < g.W; x++)
				{
					if (!Wf(g, x, y)) continue;
					floor++;
					if (node[g.Idx(x, y)] >= 0) continue;
					int pid = g.Pocket[g.Idx(x, y)], id = next++;
					if (pid < 0) { node[g.Idx(x, y)] = id; continue; }
					var st = new Stack<int[]>(); st.Push(new[] { x, y });
					node[g.Idx(x, y)] = id;
					while (st.Count > 0)
					{
						var c = st.Pop();
						foreach (var d in N4)
						{
							int nx = c[0] + d[0], ny = c[1] + d[1];
							if (g.Inb(nx, ny) && node[g.Idx(nx, ny)] < 0 && Wf(g, nx, ny) && g.Pocket[g.Idx(nx, ny)] == pid)
							{ node[g.Idx(nx, ny)] = id; st.Push(new[] { nx, ny }); }
						}
					}
				}
			int V = next;
			var edges = new HashSet<long>();
			for (int y = 0; y < g.H; y++)
				for (int x = 0; x < g.W; x++)
				{
					if (!Wf(g, x, y)) continue;
					int a = node[g.Idx(x, y)];
					foreach (var d in new[] { new[]{1,0}, new[]{0,1} })
					{
						int nx = x + d[0], ny = y + d[1];
						if (!g.Inb(nx, ny) || !Wf(g, nx, ny)) continue;
						int b = node[g.Idx(nx, ny)];
						if (a != b) edges.Add(a < b ? ((long)a << 20) | (uint)b : ((long)b << 20) | (uint)a);
					}
				}
			var adj = new Dictionary<int, List<int>>();
			foreach (long e in edges)
			{
				int a = (int)(e >> 20), b = (int)(e & 0xFFFFF);
				if (!adj.TryGetValue(a, out var la)) adj[a] = la = new List<int>();
				la.Add(b);
				if (!adj.TryGetValue(b, out var lb)) adj[b] = lb = new List<int>();
				lb.Add(a);
			}
			var seen = new HashSet<int>();
			int C = 0;
			for (int i = 0; i < V; i++)
			{
				if (seen.Contains(i)) continue;
				C++;
				var st = new Stack<int>(); st.Push(i); seen.Add(i);
				while (st.Count > 0)
				{
					int n2 = st.Pop();
					if (adj.TryGetValue(n2, out var l))
						foreach (int m in l) if (!seen.Contains(m)) { seen.Add(m); st.Push(m); }
				}
			}
			return new Measure0 { Loops = edges.Count - V + C, Comps = C, Floor = floor };
		}

		private static int ComponentCount(Grid g)
		{
			var seen = new byte[g.W * g.H];
			int C = 0;
			for (int y = 0; y < g.H; y++)
				for (int x = 0; x < g.W; x++)
				{
					if (!Wf(g, x, y) || seen[g.Idx(x, y)] != 0) continue;
					C++;
					var st = new Stack<int[]>(); st.Push(new[] { x, y }); seen[g.Idx(x, y)] = 1;
					while (st.Count > 0)
					{
						var c = st.Pop();
						foreach (var d in N4)
						{
							int nx = c[0] + d[0], ny = c[1] + d[1];
							if (g.Inb(nx, ny) && seen[g.Idx(nx, ny)] == 0 && Wf(g, nx, ny))
							{ seen[g.Idx(nx, ny)] = 1; st.Push(new[] { nx, ny }); }
						}
					}
				}
			return C;
		}

		// OUTSIDE A ROOM, NOTHING IS 2 WIDE. Give the excess back where it strands nothing.
		private static int TrimSlack(Grid g)
		{
			int trimmed = 0;
			for (int y = 0; y < g.H - 1; y++)
				for (int x = 0; x < g.W - 1; x++)
				{
					var q = new[] { new[]{x,y}, new[]{x+1,y}, new[]{x,y+1}, new[]{x+1,y+1} };
					if (!q.All(c => Wf(g, c[0], c[1]))) continue;
					var cand = q.Where(c => g.Pocket[g.Idx(c[0], c[1])] < 0 && g.Idx(c[0], c[1]) != g.MouthIdx).ToList();
					if (cand.Count == 0) continue;   // all four belong to a room: legitimate
					foreach (var c in cand)
					{
						g.C[g.Idx(c[0], c[1])] = SOLID;
						if (ComponentCount(g) == 1) { trimmed++; break; }
						g.C[g.Idx(c[0], c[1])] = FLOOR;
					}
				}
			return trimmed;
		}

		// Nothing may be stranded (EnsureAllVoidsConnected, ported)
		private static int RepairConnectivity(Grid g)
		{
			int repairs = 0;
			for (int guard = 0; guard < 16; guard++)
			{
				var lab = new int[g.W * g.H];
				for (int i = 0; i < lab.Length; i++) lab[i] = -1;
				var regions = new List<(List<int[]> cells, double cx, double cy)>();
				for (int y = 0; y < g.H; y++)
					for (int x = 0; x < g.W; x++)
					{
						if (!Wf(g, x, y) || lab[g.Idx(x, y)] >= 0) continue;
						int id = regions.Count;
						var cells = new List<int[]>();
						var st = new Stack<int[]>(); st.Push(new[] { x, y }); lab[g.Idx(x, y)] = id;
						while (st.Count > 0)
						{
							var c = st.Pop(); cells.Add(c);
							foreach (var d in N4)
							{
								int nx = c[0] + d[0], ny = c[1] + d[1];
								if (g.Inb(nx, ny) && lab[g.Idx(nx, ny)] < 0 && Wf(g, nx, ny))
								{ lab[g.Idx(nx, ny)] = id; st.Push(new[] { nx, ny }); }
							}
						}
						regions.Add((cells, cells.Average(c => (double)c[0]), cells.Average(c => (double)c[1])));
					}
				if (regions.Count <= 1) return repairs;
				regions.Sort((a, b) => b.cells.Count.CompareTo(a.cells.Count));
				var joined = new List<(List<int[]> cells, double cx, double cy)> { regions[0] };
				bool progressed = false;
				for (int i = 1; i < regions.Count; i++)
				{
					var r = regions[i];
					joined.Sort((a, b) =>
					{
						double da = (a.cx - r.cx) * (a.cx - r.cx) + (a.cy - r.cy) * (a.cy - r.cy);
						double db = (b.cx - r.cx) * (b.cx - r.cx) + (b.cy - r.cy) * (b.cy - r.cy);
						return da.CompareTo(db);
					});
					var near = joined[0];
					var A = r.cells[R(0, r.cells.Count - 1)];
					var B = near.cells[R(0, near.cells.Count - 1)];
					var path = Tunnel(g, A[0], A[1], B[0], B[1], EMPTY_BASE, false);
					if (path != null && CarveTunnel(g, path) >= 0) { repairs++; progressed = true; }
					joined.Add(r);
				}
				if (!progressed) return repairs;
			}
			return repairs;
		}

		// ---------------------------------------------------------------- roles
		private class RolesRec { public string Tally; public string YardDesc; public Box YardRect; public string YardState; }
		private static RolesRec RegisterRoles(Grid g, Artery art, List<PocketRec> pockets, int si, string occupancy)
		{
			// depth = walk distance from the mouth over finished floor
			var dist = new int[g.W * g.H];
			for (int i = 0; i < dist.Length; i++) dist[i] = -1;
			var q = new List<int[]> { new[] { art.Mouth.X, art.Mouth.Y } };
			dist[g.MouthIdx] = 0;
			while (q.Count > 0)
			{
				var nq = new List<int[]>();
				foreach (var c in q)
					foreach (var d in N4)
					{
						int nx = c[0] + d[0], ny = c[1] + d[1];
						if (g.Inb(nx, ny) && dist[g.Idx(nx, ny)] < 0 && g.C[g.Idx(nx, ny)] == FLOOR)
						{ dist[g.Idx(nx, ny)] = dist[g.Idx(c[0], c[1])] + 1; nq.Add(new[] { nx, ny }); }
					}
				q = nq;
			}
			g.MouthDist = dist;
			// entrance recheck on the FINISHED floor (repair bores doors after carve counts)
			foreach (var p in pockets)
			{
				if (p.Entrances > 0) continue;
				for (int y = p.In.Y0; y <= p.In.Y1 && p.Entrances == 0; y++)
					for (int x = p.In.X0; x <= p.In.X1; x++)
					{
						if (g.Pocket[g.Idx(x, y)] != p.Id || g.C[g.Idx(x, y)] != FLOOR) continue;
						bool found = false;
						foreach (var d in N4)
						{
							int nx = x + d[0], ny = y + d[1];
							if (g.Inb(nx, ny) && g.C[g.Idx(nx, ny)] == FLOOR && g.Pocket[g.Idx(nx, ny)] != p.Id) { found = true; break; }
						}
						if (found) { p.Entrances = 1; break; }
					}
			}
			foreach (var p in pockets)
			{
				int dmin2 = int.MaxValue;
				for (int y = p.In.Y0; y <= p.In.Y1; y++)
					for (int x = p.In.X0; x <= p.In.X1; x++)
					{
						int dd = dist[g.Idx(x, y)];
						if (dd >= 0 && dd < dmin2) dmin2 = dd;
					}
				p.Depth = dmin2 == int.MaxValue ? 9999 : dmin2;
				p.Role = null;
			}
			var ranked = pockets.OrderBy(p => p.Depth).ToList();
			for (int i = 0; i < ranked.Count; i++) ranked[i].DepthRank = i;
			var reach = ranked.Where(p => p.Depth < 9999).ToList();
			int dmin = reach.Count > 0 ? reach[0].Depth : 0;
			int dmax = reach.Count > 0 ? reach[reach.Count - 1].Depth : 0;
			int span = Math.Max(1, dmax - dmin);
			bool Near(PocketRec p) => p.Depth < 9999 && (double)(p.Depth - dmin) / span < 0.34;
			// commons: flourishing only, the hall closest to the mouth (front is a
			// lean carried by depth-sort, not a gate - ruled 2026-08-18)
			if (si == 2)
			{
				var c = reach.FirstOrDefault(p => p.Character == "hall");
				if (c != null) c.Role = "commons";
			}
			// block yard: largest clear rectangle in a near, off-route, non-hall
			// pocket (5x5 up to 9x7); else a patch of canyon floor from the doorstep
			string yardDesc = "none";
			Box yardRect = null;
			bool ClearAt(int i) => g.C[i] == FLOOR && g.Art[i] == 0 && g.Water[i] == 0 && g.Tree[i] == 0 && i != g.MouthIdx;
			Box BestRectMin(HashSet<int> cl, int minW, int minH)
			{
				if (cl.Count == 0) return null;
				int bx0 = g.W, by0 = g.H, bx1 = 0, by1 = 0;
				foreach (int i in cl)
				{
					int x = i % g.W, y = i / g.W;
					if (x < bx0) bx0 = x; if (x > bx1) bx1 = x;
					if (y < by0) by0 = y; if (y > by1) by1 = y;
				}
				if (bx1 < bx0) return null;
				int hw = bx1 - bx0 + 1;
				var hist = new int[hw];
				Box best = null; int bestScore = -1;
				for (int y = by0; y <= by1; y++)
				{
					for (int xi = 0; xi < hw; xi++) hist[xi] = cl.Contains(g.Idx(bx0 + xi, y)) ? hist[xi] + 1 : 0;
					for (int a = 0; a < hw; a++)
					{
						int mh = int.MaxValue;
						for (int b = a; b < hw; b++)
						{
							mh = Math.Min(mh, hist[b]);
							if (mh == 0) break;
							int w2 = b - a + 1;
							if (w2 < minW || mh < minH) continue;
							int cw2 = Math.Min(w2, 9), ch2 = Math.Min(mh, 7), score = cw2 * ch2;
							if (score <= bestScore) continue;
							bestScore = score;
							int rx0 = bx0 + a, rx1 = bx0 + b, ry0 = y - mh + 1, ry1 = y;
							if (w2 > cw2) { if (art.Mouth.X < (rx0 + rx1) / 2.0) rx1 = rx0 + cw2 - 1; else rx0 = rx1 - cw2 + 1; }
							if (mh > ch2) { if (art.Mouth.Y < (ry0 + ry1) / 2.0) ry1 = ry0 + ch2 - 1; else ry0 = ry1 - ch2 + 1; }
							best = new Box { X0 = rx0, Y0 = ry0, X1 = rx1, Y1 = ry1 };
						}
					}
				}
				return best;
			}
			Box BestRect(HashSet<int> cl) => BestRectMin(cl, 5, 5) ?? BestRectMin(cl, 5, 4) ?? BestRectMin(cl, 4, 5);
			string yardState = null;
			// BOTH candidates measured, AREA decides (USER 2026-08-25 "what if we improved
			// blockyard placement a bit when there's lots of open space outside?", nodded
			// to port after the sim measured inside share 22% -> 8.2%). The old order took
			// any qualifying pocket unconditionally and never measured the doorstep; now
			// the doorstep wins when STRICTLY bigger, inside keeps ties.
			PocketRec insidePocket = null; Box insideRect = null;
			foreach (var p in reach)
			{
				if (p.Role != null || p.Through != 0 || p.Character == "hall" || !Near(p)) continue;
				var cl = new HashSet<int>();
				for (int y = p.In.Y0; y <= p.In.Y1; y++)
					for (int x = p.In.X0; x <= p.In.X1; x++)
					{
						int i = g.Idx(x, y);
						if (g.Pocket[i] == p.Id && ClearAt(i)) cl.Add(i);
					}
				var r2 = BestRect(cl);
				if (r2 == null) continue;
				insidePocket = p; insideRect = r2;
				break;
			}
			Box outsideRect = null;
			{
				// outside: canyon floor from the doorstep; trees walkable for the
				// flood and claimable by the rectangle (the drape stamps over brush)
				var cl = new HashSet<int>();
				var seen2 = new byte[g.W * g.H];
				var q2 = new List<int[]> { new[] { art.Mouth.X, art.Mouth.Y } };
				seen2[g.MouthIdx] = 1;
				int visited = 0;
				while (q2.Count > 0 && visited < 320)
				{
					var nq = new List<int[]>();
					foreach (var c in q2)
						foreach (var d in N4)
						{
							int nx = c[0] + d[0], ny = c[1] + d[1];
							if (!g.Inb(nx, ny) || seen2[g.Idx(nx, ny)] != 0 || g.Host[g.Idx(nx, ny)] == 0 || g.Water[g.Idx(nx, ny)] != 0) continue;
							seen2[g.Idx(nx, ny)] = 1; visited++; nq.Add(new[] { nx, ny });
							cl.Add(g.Idx(nx, ny));
						}
					q2 = nq;
				}
				outsideRect = BestRect(cl);
			}
			int AreaOf(Box r2) => (r2.X1 - r2.X0 + 1) * (r2.Y1 - r2.Y0 + 1);
			if (insideRect != null && (outsideRect == null || AreaOf(insideRect) >= AreaOf(outsideRect)))
			{
				insidePocket.Role = "blockyard";
				insidePocket.Footprint = new List<int>();
				for (int y = insideRect.Y0; y <= insideRect.Y1; y++)
					for (int x = insideRect.X0; x <= insideRect.X1; x++) insidePocket.Footprint.Add(g.Idx(x, y));
				yardDesc = $"inside pocket {insidePocket.Id} ({insideRect.X1 - insideRect.X0 + 1}x{insideRect.Y1 - insideRect.Y0 + 1})";
				yardRect = insideRect;
			}
			else if (outsideRect != null)
			{
				yardDesc = $"outside ({outsideRect.X1 - outsideRect.X0 + 1}x{outsideRect.Y1 - outsideRect.Y0 + 1})";
				yardRect = outsideRect;
			}
			// yard state stamped at registration (the contract: dress never classifies)
			if (yardDesc != "none")
			{
				if (occupancy == "abandoned") yardState = Chance(.3) ? "robbed" : "abandoned";
				else if (occupancy == "reclaimed") yardState = "robbed";
				else
				{
					int[][] w3 = { new[]{30,60,10}, new[]{15,65,20}, new[]{5,55,40} };
					int roll3 = R(1, 100);
					yardState = roll3 <= w3[si][0] ? "fresh" : roll3 <= w3[si][0] + w3[si][1] ? "working" : "full";
				}
				yardDesc += " state=" + yardState;
			}
			// face: deepest reachable pocket, then every unclaimed hall
			var deepP = reach.Count > 0 ? reach[reach.Count - 1] : null;
			if (deepP != null && deepP.Role == null) deepP.Role = "face";
			foreach (var p in pockets) if (p.Role == null && p.Character == "hall") p.Role = "face";
			// dwellings take the remaining off-route pockets; the rest stay plain
			foreach (var p in pockets) if (p.Role == null) p.Role = p.Through == 0 ? "dwelling" : "plain";
			var tally = pockets.GroupBy(p => p.Role).ToDictionary(gr => gr.Key, gr => gr.Count());
			string tstr = string.Join(" ", new[] { "commons", "blockyard", "face", "dwelling", "plain" }
				.Select(role => role + ":" + (tally.TryGetValue(role, out int v) ? v : 0)));
			return new RolesRec { Tally = tstr, YardDesc = yardDesc, YardRect = yardRect, YardState = yardState };
		}

		// ---------------------------------------------------------------- moss
		// The 2026-08-23 sim port: candidacy at min distance, one roll per cell,
		// v1 grading (full beside the water, x0.4 at the reach edge), guaranteed
		// tell. Returns wall cells to recolor TileColor K / DetailColor g.
		private static List<int> MossCells(Grid g)
		{
			var cand = new byte[g.W * g.H];
			for (int i = 0; i < g.W * g.H; i++)
			{
				if (g.Aqua[i] == 0) continue;
				int wx = i % g.W, wy = i / g.W;
				for (int dy = -2; dy <= 2; dy++)
					for (int dx = -2; dx <= 2; dx++)
					{
						int nx = wx + dx, ny = wy + dy;
						if (!g.Inb(nx, ny) || g.C[g.Idx(nx, ny)] == FLOOR) continue;
						bool faces = false;
						foreach (var a in N8)
						{
							int fx = nx + a[0], fy = ny + a[1];
							if (g.Inb(fx, fy) && g.C[g.Idx(fx, fy)] == FLOOR && g.Water[g.Idx(fx, fy)] == 0) { faces = true; break; }
						}
						if (!faces) continue;
						int d = Math.Max(Math.Abs(dx), Math.Abs(dy)); if (d == 0) d = 1;
						int j = g.Idx(nx, ny);
						if (cand[j] == 0 || d < cand[j]) cand[j] = (byte)d;
					}
			}
			var outCells = new List<int>();
			int first = -1;
			for (int i = 0; i < cand.Length; i++)
			{
				if (cand[i] == 0) continue;
				if (first < 0) first = i;
				if (Rng() < (MOSS_PCT / 100.0) * (cand[i] <= 1 ? 1 : 0.4)) outCells.Add(i);
			}
			if (outCells.Count == 0 && first >= 0) outCells.Add(first);
			return outCells;
		}

		// ---------------------------------------------------------------- dress
		// The full sim dress pass, ported 2026-08-23 on the user's green light
		// ("this is our first honest to goodness good patch that feels like the
		// sim, so i feel like we can do dressing"). Tables, laws, weights and
		// dials are the sim's verbatim (warren-lab-v2.html dressSite; v1
		// provenance markers live there). Deliberately NOT ported, flagged loud:
		// commons pre-etched walls (Part_Etchable integration gets its own look),
		// basket contents poptables (baskets place empty), catch basin (no confirmed
		// base blueprint name; dropped from the liquid pool), cold-brazier and
		// cold-firedish art (light parts stripped; sprite may still read lit).
		private class Row { public int Wt; public string Id, Hint; public Row(int w, string id, string hint = null) { Wt = w; Id = id; Hint = hint; } }
		// nugget row weight 1 (USER RULED 2026-08-25, weights delegated): quarry
		// product tumbled free at the work face - rare on purpose, ~1 in 110 rolls
		private static readonly Row[] T_SOCKET = { new Row(55,null), new Row(18,"stack1"), new Row(8,"straytorch","face"), new Row(5,"pickaxe","face"), new Row(5,"trash"), new Row(2,"chest"), new Row(4,"bg.floor_cushion"), new Row(3,"bg.bedroll"), new Row(2,"pitcher_wine"), new Row(4,"basket_stones"), new Row(2,"bg.vase"), new Row(2,"bg.clay_jug"), new Row(1,"nugget") };
		private static readonly Row[] T_GALLERYBAY = { new Row(50,null), new Row(18,"bg.random_stone_statue"), new Row(12,"largeboulder"), new Row(10,"stack2"), new Row(5,"chest"), new Row(5,"straytorch"), new Row(6,"pitcher_wine") };
		private static readonly Row[] T_GALLERYPILLAR = { new Row(62,null), new Row(20,"bg.random_stone_statue"), new Row(10,"straytorch"), new Row(8,"stack1"), new Row(8,"bg.bench"), new Row(6,"bg.floor_cushion") };
		private static readonly Row[] T_GALLERYFLOOR = { new Row(47,null), new Row(25,"largeboulder","center"), new Row(15,"bg.random_stone_statue","center"), new Row(5,"bg.village_monument_stele","center"), new Row(8,"firedish","center") };
		private static readonly Row[] T_NATURAL = { new Row(55,null), new Row(10,"stone"), new Row(15,"loosestone"), new Row(20,"pebble") };
		private static readonly Row[] T_PLAINTRACE = { new Row(74,null), new Row(10,"straytorch","face"), new Row(7,"trash"), new Row(5,"pickaxe","face"), new Row(3,"bg.floor_cushion"), new Row(2,"pitcher_cider") };
		private static readonly Row[] T_HALLTABLE = { new Row(74,null), new Row(10,"straytorch","face"), new Row(7,"trash"), new Row(5,"pickaxe","face"), new Row(4,"chalkmark") };
		private static readonly HashSet<string> SOLID_ITEMS = new HashSet<string> { "bg.random_stone_statue", "bg.village_monument_stele", "largeboulder" };
		private static readonly Dictionary<string, string[]> ROOMPOOLS = new Dictionary<string, string[]>
		{
			// brazier REMOVED (USER RULED 2026-08-23: "remove chrome braziers with
			// the blue flames from the roll" - base Brazier is chrome + blue flame,
			// wrong hands entirely). Fire dish substituted: CLAUDE dial, the mod's
			// own light furniture, strikeable.
			{ "light", new[]{ "firedish", "straytorch" } },
			{ "storage", new[]{ "bg.chest", "bg.woven_basket", "basket_food" } },
			{ "seating", new[]{ "bg.floor_cushion", "bg.canvas_folding_chair", "kline", "bg.bench", "stool" } },
			{ "sleep", new[]{ "bg.bed", "bg.bedroll", "bg.folding_cot" } },
			{ "centre", new[]{ "bg.table", "bg.workbench" } },
			// vases + clay jugs joined 2026-08-24 (USER: "let's add vases and
			// clay jugs for variety"). Base Vase's own description places it in
			// a canyon dweller's home-kiln; both carry RandomColors, so glaze
			// variety rolls per spawn. Catch basin still out (no confirmed name).
			{ "liquid", new[]{ "bg.wine_pitcher", "pitcher_wine", "pitcher_cider", "bg.vase", "bg.clay_jug" } },
			{ "crafts", new[]{ "bg.workbench", "bg.anvil", "pickaxe", "bg.random_stone_statue", "stack1", "stack2", "stack3", "stonefig", "basket_stones", "kiln" } },
			{ "hearth", new[]{ "bg.oven", "bg.campfire" } },
			{ "comfort", new[]{ "bg.hookah" } },
			// keepsake joined 2026-08-25 (USER: bracelets + nuggets as rare table
			// spawns, leans delegated): a personal effect in a dwelling. Base
			// Bracelet1/2/3 are the beaded trade goods bartered up the Svy.
			{ "keepsake", new[]{ "bracelet1", "bracelet2", "bracelet3" } },
		};
		private class RowSpec { public int N0, N1, Chance; public string Pool, Hint; public bool First; }
		private class RoomType { public int Wt; public string Name; public RowSpec[] Rows; public double Density; }
		private static RowSpec RS(int n0, int n1, string pool, string hint, int chance = 0, bool first = false)
			=> new RowSpec { N0 = n0, N1 = n1, Pool = pool, Hint = hint, Chance = chance, First = first };
		private static readonly RoomType[] ROOMTYPES = {
			new RoomType{ Wt=40, Name="house", Density=0.3, Rows=new[]{ RS(1,1,"light","corner"), RS(1,2,"storage","wall"), RS(0,1,"seating","inside"), RS(0,1,"sleep","wall"), RS(1,1,"centre","wall",30), RS(1,1,"liquid","wall",40), RS(1,1,"keepsake","inside",12) } },
			new RoomType{ Wt=20, Name="storage", Density=0.3, Rows=new[]{ RS(1,1,"light","corner"), RS(1,3,"liquid","wall"), RS(3,4,"storage","wall",0,true) } },
			new RoomType{ Wt=15, Name="crafts", Density=0.3, Rows=new[]{ RS(1,1,"light","corner"), RS(1,2,"seating","wall"), RS(2,4,"crafts","corner",0,true), RS(0,2,"storage","wall") } },
			new RoomType{ Wt=10, Name="seating", Density=0.3, Rows=new[]{ RS(1,1,"light","corner"), RS(2,4,"seating","wall",0,true), RS(0,1,"centre","inside"), RS(1,1,"comfort","inside",35,true), RS(1,1,"liquid","wall",40) } },
			new RoomType{ Wt=8, Name="hearth", Density=0.42, Rows=new[]{ RS(1,1,"light","corner"), RS(1,2,"hearth","wall",0,true), RS(1,2,"liquid","wall"), RS(1,2,"storage","wall"), RS(1,1,"centre","inside",60), RS(0,2,"seating","inside") } },
		};
		private const double DWELLDENSITY = 0.3;   // v1's engine-faithful number

		private static string DressSite(Grid g, Artery art, List<PocketRec> pockets, RolesRec roles, string occupancy, int siteFloor)
		{
			g.Item = new string[g.W * g.H];
			var placed = new Dictionary<string, int>();
			int items = 0, tableItems = 0, refused = 0, topups = 0;
			var misses = new List<(Row[] tbl, PocketRec p, List<int[]> cs)>();
			bool SolidItem(int i) => g.Item[i] != null && SOLID_ITEMS.Contains(g.Item[i]);
			bool IsFree(int i) => g.C[i] == FLOOR && g.Item[i] == null && g.Art[i] == 0 && g.Water[i] == 0 && g.Tree[i] == 0 && i != g.MouthIdx;
			// v1's articulation guard: a solid item only lands where removing the
			// cell keeps its floor neighbours mutually reachable
			bool SolidOK(int x, int y)
			{
				int home = g.Idx(x, y);
				var ns = new List<int>();
				foreach (var d in N4)
				{
					int nx = x + d[0], ny = y + d[1];
					if (g.Inb(nx, ny) && g.C[g.Idx(nx, ny)] == FLOOR && !SolidItem(g.Idx(nx, ny))) ns.Add(g.Idx(nx, ny));
				}
				if (ns.Count <= 1) return true;
				var seen = new HashSet<int> { home, ns[0] };
				var st = new Stack<int>(); st.Push(ns[0]);
				int found = 1;
				while (st.Count > 0 && found < ns.Count)
				{
					int c = st.Pop(); int cx = c % g.W, cy = c / g.W;
					foreach (var d in N4)
					{
						int nx = cx + d[0], ny = cy + d[1];
						if (!g.Inb(nx, ny)) continue;
						int ni = g.Idx(nx, ny);
						if (seen.Contains(ni) || g.C[ni] != FLOOR || SolidItem(ni)) continue;
						seen.Add(ni); st.Push(ni);
						if (ns.Contains(ni)) found++;
					}
				}
				return found == ns.Count;
			}
			void Put(int i, string id) { g.Item[i] = id; placed[id] = (placed.TryGetValue(id, out int v) ? v : 0) + 1; items++; }
			string WRoll(Row[] tbl)
			{
				int t = 0; foreach (var r in tbl) t += r.Wt;
				double x = Rng() * t;
				foreach (var r in tbl) { x -= r.Wt; if (x < 0) return r.Id == null ? null : (r.Hint != null ? r.Id + "|" + r.Hint : r.Id); }
				return null;
			}
			List<int[]> CellsOf(PocketRec p)
			{
				var cs = new List<int[]>();
				for (int y = p.In.Y0; y <= p.In.Y1; y++)
					for (int x = p.In.X0; x <= p.In.X1; x++)
						if (g.Pocket[g.Idx(x, y)] == p.Id && g.C[g.Idx(x, y)] == FLOOR) cs.Add(new[] { x, y });
				return cs;
			}
			// the doorway rule: furniture stays off any cell open to floor outside the pocket
			bool Threshold(PocketRec p, int x, int y)
			{
				foreach (var d in N4)
				{
					int nx = x + d[0], ny = y + d[1];
					if (g.Inb(nx, ny) && g.C[g.Idx(nx, ny)] == FLOOR && g.Pocket[g.Idx(nx, ny)] != p.Id) return true;
				}
				return false;
			}
			bool Seat(PocketRec p, string rolled, List<int[]> cs)
			{
				if (rolled == null) return false;
				string item = rolled, hint = null;
				int bar = rolled.IndexOf('|');
				if (bar >= 0) { hint = rolled.Substring(bar + 1); item = rolled.Substring(0, bar); }
				bool solid = SOLID_ITEMS.Contains(item);
				bool Legal(int[] c2) => IsFree(g.Idx(c2[0], c2[1])) && !Threshold(p, c2[0], c2[1]) && (!solid || SolidOK(c2[0], c2[1]));
				int[] c = null;
				if (hint == "face")
				{
					int best = -1;
					foreach (var cc in cs)
					{
						int dd = g.MouthDist != null ? g.MouthDist[g.Idx(cc[0], cc[1])] : -1;
						if (dd > best && Legal(cc)) { best = dd; c = cc; }
					}
				}
				else if (hint == "center")
				{
					double cx = 0, cy = 0;
					foreach (var cc in cs) { cx += cc[0]; cy += cc[1]; }
					cx /= cs.Count; cy /= cs.Count;
					double best = 1e9;
					foreach (var cc in cs)
					{
						if (!Legal(cc)) continue;
						double dd = (cc[0] - cx) * (cc[0] - cx) + (cc[1] - cy) * (cc[1] - cy);
						if (dd < best) { best = dd; c = cc; }
					}
				}
				if (c == null)
				{
					var ok = cs.Where(Legal).ToList();
					if (ok.Count == 0) { if (solid) refused++; return false; }
					c = ok[R(0, ok.Count - 1)];
				}
				Put(g.Idx(c[0], c[1]), item); tableItems++;
				if (item == "trash" && !Chance(.55))
				{
					var pile = new List<int[]> { c };
					int extra = R(1, 3);
					for (int e = 0; e < extra; e++)
					{
						var ps = pile[R(0, pile.Count - 1)];
						var adj = cs.Where(a => Math.Abs(a[0] - ps[0]) <= 1 && Math.Abs(a[1] - ps[1]) <= 1 && IsFree(g.Idx(a[0], a[1]))).ToList();
						if (adj.Count == 0) continue;
						var nc = adj[R(0, adj.Count - 1)];
						Put(g.Idx(nc[0], nc[1]), "trash"); pile.Add(nc);
					}
				}
				return true;
			}
			// FACE: one socket roll per ~30 floor cells, 1-3. A FACE IS NEVER
			// STERILE (2026-08-23, user's overlit/bare contrast flag): the first
			// roll skips the Nothing row - work stopped here, something is always
			// left. Echoes the plain-rooms ruling's own words ("it just isn't
			// sterile"). CLAUDE dial, strikeable; sim catch-up flagged.
			void DressFace(PocketRec p)
			{
				var cs = CellsOf(p); if (cs.Count == 0) return;
				int rolls = Math.Max(1, Math.Min(3, (int)Math.Round(cs.Count / 30.0)));
				for (int i = 0; i < rolls; i++)
				{
					string rolled = i == 0 ? WRollNo(T_SOCKET) : WRoll(T_SOCKET);
					if (!Seat(p, rolled, cs)) misses.Add((T_SOCKET, p, cs));
				}
			}
			// COMMONS: gallery tables on the carve's own articulation
			void DressCommons(PocketRec p)
			{
				var cs = CellsOf(p); if (cs.Count == 0) return;
				foreach (var pc in p.PillarCells)
				{
					var adj = cs.Where(a => Math.Abs(a[0] - pc[0]) <= 1 && Math.Abs(a[1] - pc[1]) <= 1).ToList();
					if (adj.Count > 0 && !Seat(p, WRoll(T_GALLERYPILLAR), adj)) misses.Add((T_GALLERYPILLAR, p, adj));
				}
				foreach (var sc in p.SpurCells)
				{
					var adj = cs.Where(a => Math.Abs(a[0] - sc[0]) <= 1 && Math.Abs(a[1] - sc[1]) <= 1).ToList();
					if (adj.Count > 0 && !Seat(p, WRoll(T_GALLERYBAY), adj)) misses.Add((T_GALLERYBAY, p, adj));
				}
				if (!Seat(p, WRoll(T_GALLERYFLOOR), cs)) misses.Add((T_GALLERYFLOOR, p, cs));
				// pre-etched commons walls: NOT PORTED yet (Part_Etchable markov
				// integration gets its own sitting) - flagged in TODO
			}
			// DWELLING: v1's typed-room grammar whole
			void DressDwelling(PocketRec p)
			{
				var cs = CellsOf(p); if (cs.Count == 0) return;
				int tot = 0; foreach (var t in ROOMTYPES) tot += t.Wt;
				double x = Rng() * tot; RoomType type = null;
				foreach (var t in ROOMTYPES) { x -= t.Wt; if (x < 0) { type = t; break; } }
				if (type == null) return;
				int WallsAround(int[] c2)
				{
					int n = 0;
					foreach (var d in N4)
					{
						int nx = c2[0] + d[0], ny = c2[1] + d[1];
						if (!g.Inb(nx, ny) || g.C[g.Idx(nx, ny)] != FLOOR) n++;
					}
					return n;
				}
				int budget = Math.Max(1, (int)Math.Round(cs.Count * type.Density));
				int roomChests = 0;   // one chest per room (user flag: "too many chests")
				var rows = type.Rows.ToList();
				var fixedRows = new List<RowSpec> { rows[0] };
				rows.RemoveAt(0);
				for (int i = rows.Count - 1; i >= 0; i--) if (rows[i].First) { fixedRows.Add(rows[i]); rows.RemoveAt(i); }
				for (int i = rows.Count - 1; i > 0; i--) { int j = R(0, i); (rows[i], rows[j]) = (rows[j], rows[i]); }
				foreach (var row in fixedRows.Concat(rows))
				{
					if (budget <= 0) break;
					if (row.Chance > 0 && !Chance(row.Chance / 100.0)) continue;
					int want = Math.Min(R(row.N0, row.N1), budget);
					if (row.Pool == "light") want = Math.Min(want, Math.Max(1, budget >> 1));   // a room is lit once
					for (int w2 = 0; w2 < want && budget > 0; w2++)
					{
						var pool = ROOMPOOLS[row.Pool];
						// ONE OVEN PER SITE - an oven is built masonry, a camp builds one
						if (pool.Contains("bg.oven") && placed.ContainsKey("bg.oven")) pool = pool.Where(i2 => i2 != "bg.oven").ToArray();
						string item = pool[R(0, pool.Length - 1)];
						if (item == "bg.chest") { if (roomChests >= 1) item = "bg.woven_basket"; else roomChests++; }
						bool solid = SOLID_ITEMS.Contains(item);
						bool Legal(int[] c2) => IsFree(g.Idx(c2[0], c2[1])) && !Threshold(p, c2[0], c2[1]) && (!solid || SolidOK(c2[0], c2[1]));
						var cands = cs.Where(c2 =>
						{
							if (!Legal(c2)) return false;
							if (row.Hint == "corner") return WallsAround(c2) >= 2;
							if (row.Hint == "wall") return WallsAround(c2) >= 1;
							return true;
						}).ToList();
						if (cands.Count == 0) cands = cs.Where(Legal).ToList();
						if (cands.Count == 0) { if (solid) refused++; break; }
						var c = cands[R(0, cands.Count - 1)];
						Put(g.Idx(c[0], c[1]), item); budget--; tableItems++;
						// cluster stamps riding a centerpiece roll: the hookah-circle
						// (user sketch, ruled) and the kiln's brick piles
						if (item == "bg.hookah" || item == "kiln")
						{
							string orbit = item == "bg.hookah" ? "bg.floor_cushion" : "quarrybricks";
							int kmax = item == "bg.hookah" ? R(2, 4) : R(1, 2);
							var slots = new List<int[]>();
							foreach (var d in N4)
							{
								int sxx = c[0] + d[0], syy = c[1] + d[1];
								if (cs.Any(a => a[0] == sxx && a[1] == syy) && IsFree(g.Idx(sxx, syy)) && !Threshold(p, sxx, syy))
									slots.Add(new[] { sxx, syy });
							}
							int k = Math.Max(1, Math.Min(kmax, slots.Count));
							while (k-- > 0 && slots.Count > 0)
							{
								int si2 = R(0, slots.Count - 1);
								var s2 = slots[si2]; slots.RemoveAt(si2);
								Put(g.Idx(s2[0], s2[1]), orbit); tableItems++;
							}
						}
					}
				}
			}
			// BLOCK YARD: the chalked rectangle with contents inside, five states,
			// grades ascending from the bench (the yard is the site's status board)
			void DressYard(Box rect, string state, bool outside)
			{
				int x0 = rect.X0, y0 = rect.Y0, x1 = rect.X1, y1 = rect.Y1;
				bool FreeI(int i) => g.C[i] == FLOOR && g.Item[i] == null;
				for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) g.Tree[g.Idx(x, y)] = 0;   // the drape clears its ground
				// GAP + TRAIL, TRAIL-FIRST (USER FIX 2026-08-25 after an in-game
				// yard rolled ringed but trail-less: "draw the path first and make
				// the hole last, on the first tile where there's room"). The trail
				// walks out from beside the mouth and the ring opens where the
				// walk arrives, so gap and path agree BY CONSTRUCTION. The rank
				// picker survives as fallback only: inside yards, yards no route
				// reaches, and the mouth-inside-the-ring edge (sim audit found 1
				// in 117: the ring encloses the mouth, so no trail is owed).
				int gmx = art.Mouth.X, gmy = art.Mouth.Y;
				int gapI = -1, gapX = 0, gapY = 0;
				List<int> tpath = null;
				string trailNote = null;
				if (outside)
				{
					bool IsGapCell(int x, int y)
					{
						if (x < x0 || x > x1 || y < y0 || y > y1) return false;
						if (x > x0 && x < x1 && y > y0 && y < y1) return false;
						return !((x == x0 || x == x1) && (y == y0 || y == y1));   // corners never door
					}
					bool PassAt(int i2, bool trees)
					{
						int px3 = i2 % g.W, py3 = i2 / g.W;
						return g.C[i2] == FLOOR && g.Host[i2] != 0 && g.Water[i2] == 0 && (trees || g.Tree[i2] == 0)
							&& !(px3 >= x0 && px3 <= x1 && py3 >= y0 && py3 <= y1);
					}
					int mxc = g.MouthIdx % g.W, myc = g.MouthIdx / g.W;
					// THE DOORSTEP CASE (sim audit 2026-08-25: 12 of 13 failures):
					// a yard flush against the mouth leaves the search no seeds. If
					// the mouth already touches a legal gap cell, that cell is the
					// door and the trail is zero cells long.
					foreach (var dd in N4)
					{
						int nx = mxc + dd[0], ny = myc + dd[1];
						if (g.Inb(nx, ny) && IsGapCell(nx, ny))
						{ gapI = g.Idx(nx, ny); gapX = nx; gapY = ny; tpath = new List<int>(); trailNote = "doorstep"; break; }
					}
					int[] prevA = null; int gapFound = -1;
					int Bfs(bool trees)
					{
						var pv = new int[g.W * g.H]; for (int k = 0; k < pv.Length; k++) pv[k] = -2;
						var ql = new List<int>();
						foreach (var dd in N4)
						{
							int nx = mxc + dd[0], ny = myc + dd[1]; if (!g.Inb(nx, ny)) continue;
							int ni = g.Idx(nx, ny);
							if (PassAt(ni, trees) && pv[ni] == -2) { pv[ni] = -1; ql.Add(ni); }
						}
						int head = 0;
						while (head < ql.Count)
						{
							int c2 = ql[head++], cx2 = c2 % g.W, cy2 = c2 / g.W;
							foreach (var dd in N4)
							{
								int nx = cx2 + dd[0], ny = cy2 + dd[1]; if (!g.Inb(nx, ny)) continue;
								if (IsGapCell(nx, ny)) { prevA = pv; gapFound = g.Idx(nx, ny); return c2; }
								int ni = g.Idx(nx, ny);
								if (pv[ni] != -2 || !PassAt(ni, trees)) continue;
								pv[ni] = c2; ql.Add(ni);
							}
						}
						return -1;
					}
					if (gapI < 0)
					{
						// strict first; a blocked route retries THROUGH brush and
						// tramples what it crosses (the drape precedent)
						int hitI = Bfs(false); if (hitI < 0) hitI = Bfs(true);
						if (hitI >= 0)
						{
							gapI = gapFound; gapX = gapI % g.W; gapY = gapI / g.W;
							tpath = new List<int>();
							for (int c2 = hitI; c2 >= 0; c2 = prevA[c2]) tpath.Add(c2);
							tpath.Reverse();   // beside-the-mouth first, gap-side last
						}
					}
				}
				if (gapI < 0)
				{
					// FALLBACK rank picker (the 08-24 primary, demoted): outward
					// step walkable first, then nearness to the mouth. The gap
					// always exists even when the trail honestly does not.
					int gapBest = int.MaxValue, gapRank = 9;
					for (int x = x0; x <= x1; x++)
						for (int y = y0; y <= y1; y++)
						{
							if (x > x0 && x < x1 && y > y0 && y < y1) continue;
							if ((x == x0 || x == x1) && (y == y0 || y == y1)) continue;   // corners never door
							int odx = x == x0 ? -1 : x == x1 ? 1 : 0, ody = y == y0 ? -1 : y == y1 ? 1 : 0;
							int gox = x + odx, goy = y + ody;
							int rank = 2;
							if (g.Inb(gox, goy))
							{
								int goi = g.Idx(gox, goy);
								if (g.C[goi] == FLOOR && g.Host[goi] != 0 && g.Water[goi] == 0)
									rank = g.Tree[goi] != 0 ? 1 : 0;
							}
							int gd = (x - gmx) * (x - gmx) + (y - gmy) * (y - gmy);
							if (rank < gapRank || (rank == gapRank && gd < gapBest))
							{ gapRank = rank; gapBest = gd; gapI = g.Idx(x, y); gapX = x; gapY = y; }
						}
				}
				for (int x = x0; x <= x1; x++)
					for (int y = y0; y <= y1; y++)
					{
						if (x > x0 && x < x1 && y > y0 && y < y1) continue;
						int i = g.Idx(x, y); if (i == gapI || !FreeI(i)) continue;
						string id = y == y0 ? (x == x0 ? "chalklineNW" : x == x1 ? "chalklineNE" : "chalklineN")
								 : y == y1 ? (x == x0 ? "chalklineSW" : x == x1 ? "chalklineSE" : "chalklineS")
								 : x == x0 ? "chalklineW" : "chalklineE";
						Put(i, id);
					}
				// TRAIL + CAIRNS paint: the worn line renders as dirt path, the
				// mouth cell stays raw (the doorstep law), cairns on the shoulder
				// every R(3,6), side rolled.
				if (tpath != null && tpath.Count > 0)
				{
					var trailSet = new HashSet<int>(tpath);
					foreach (int c2 in tpath) { g.Tree[c2] = 0; if (g.Item[c2] == null) Put(c2, "dirtpath"); }
					int cairns = 0, until = R(3, 6);
					for (int pi2 = 1; pi2 < tpath.Count; pi2++)
					{
						if (--until > 0) continue;
						until = R(3, 6);
						int c2 = tpath[pi2], cx2 = c2 % g.W, cy2 = c2 / g.W;
						int px3 = tpath[pi2 - 1] % g.W, py3 = tpath[pi2 - 1] / g.W;
						int tdx = cx2 - px3, tdy = cy2 - py3;
						int cside = Chance(.5) ? 1 : -1;
						int cox = cx2 + (-tdy) * cside, coy = cy2 + tdx * cside;
						if (!g.Inb(cox, coy)) continue;
						int coi = g.Idx(cox, coy);
						if (g.C[coi] == FLOOR && g.Host[coi] != 0 && g.Item[coi] == null && g.Water[coi] == 0 && g.Tree[coi] == 0
							&& !trailSet.Contains(coi) && coi != g.MouthIdx)
						{
							// tense pass: a living camp keeps its waystones stacked; a
							// dead site lets some fall (50% toppled, CLAUDE dial)
							Put(coi, occupancy != "in use" && Chance(.5) ? "cairn_toppled" : "cairn");
							cairns++;
						}
					}
					Helpers.VerifyLog("WARREN", $"trail: {tpath.Count} cells, {cairns} cairns, gap {gapX},{gapY}");
				}
				else if (outside)
					Helpers.VerifyLog("WARREN", $"trail: {(trailNote ?? "none (no route)")}, gap {gapX},{gapY}");
				int ix0 = x0 + 1, iy0 = y0 + 1, ix1 = x1 - 1, iy1 = y1 - 1;
				if (ix1 < ix0 || iy1 < iy0) return;
				int wx = Math.Abs(ix0 - art.Mouth.X) <= Math.Abs(ix1 - art.Mouth.X) ? ix0 : ix1;
				bool working = state == "working" || state == "full";
				if (state == "fresh" || working)
				{
					int benchY = -1;
					if (Chance(.75)) { Put(g.Idx(wx, iy0), "bg.workbench"); benchY = iy0; }
					if (working && benchY >= 0 && iy0 + 1 <= iy1 && Chance(.7)) Put(g.Idx(wx, iy0 + 1), "bg.canvas_folding_chair");
					if (working && Chance(.75) && FreeI(g.Idx(wx, iy1))) Put(g.Idx(wx, iy1), "basket_stones");
				}
				// THE YARD LEDGER (USER 2026-08-23: a chalkboard as a likely roll,
				// "chalked with a date of arrival, and if they've left, a date of
				// departure, styled in the games own vernacular. as well as
				// potentially saying how much stone was quarried"). Chance 70 and
				// the 60%-tally roll are CLAUDE dials; the LINES ARE DRAFT (user
				// voice owed). Dates ride Calendar.GetDay/GetMonth - the engine's
				// own marginalia vernacular; the tally is the site's true carved
				// floor, so the board never lies about the dig it stands in.
				if (Chance(.7))
				{
					int bi = -1;
					for (int y = iy0; y <= iy1 && bi < 0; y++)
						for (int x = ix0; x <= ix1; x++)
							if (FreeI(g.Idx(x, y))) { bi = g.Idx(x, y); break; }
					if (bi >= 0)
					{
						long now = Calendar.TotalTimeTicks;
						long Wrap(long t) { long y2 = 438000L; t %= y2; if (t < 0) t += y2; return t; }
						long arrival = Wrap(now - (long)R(45, 320) * 1200);
						var lines = new List<string> { "arrived the " + Calendar.GetDay(arrival) + " of " + Calendar.GetMonth(arrival) };
						if (occupancy != "in use")
						{
							long depart = Wrap(now - (long)R(8, 40) * 1200);
							lines.Add("departed the " + Calendar.GetDay(depart) + " of " + Calendar.GetMonth(depart));
						}
						if (Chance(.6))
							lines.Add(siteFloor + " blocks raised");
						g.YardBoardText = string.Join("\n", lines);
						Put(bi, "yardboard");
					}
				}
				if (state == "fresh")
				{
					// never an EMPTY ring: one short stack1 run, "just getting started"
					int x = wx + (wx == ix0 ? 1 : -1);
					if (x >= ix0 && x <= ix1)
					{
						int len = R(1, 2);
						for (int k = 0; k < len; k++) { int i = g.Idx(x, iy0 + k); if (FreeI(i)) Put(i, "stack1"); }
					}
					return;
				}
				int dir = wx == ix0 ? 1 : -1, avail = ix1 - ix0, ih = iy1 - iy0 + 1;
				int nCols = state == "full" ? avail : R(1, Math.Min(3, Math.Max(1, avail)));
				bool gaps = state == "robbed";
				for (int c2 = 0; c2 < nCols; c2++)
				{
					int x = wx + dir * (c2 + 1); if (x < ix0 || x > ix1) break;
					int gi = state == "full" ? Math.Min(3, c2 + 2) : Math.Min(3, c2 + 1);
					int len = state == "full" || gi == 3 ? ih
							: gi == 2 ? R(Math.Max(1, ih - 1), ih)
							: R(1, Math.Max(1, ih - 1));
					for (int k = 0; k < len; k++)
					{
						int i = g.Idx(x, iy0 + k); if (!FreeI(i)) continue;
						if (gaps && Chance(.5)) { if (Chance(.4)) Put(i, "loosestone"); continue; }
						Put(i, "stack" + gi);
					}
				}
			}
			foreach (var p in pockets)
			{
				if (p.Role == "face") DressFace(p);
				else if (p.Role == "commons") DressCommons(p);
				else if (p.Role == "dwelling") DressDwelling(p);
				else if (p.Role == "plain")
				{
					// SIZE-SCALED TRACES (user feel flag in game 2026-08-24,
					// "still a bit bare"): one roll per den-threshold of floor
					// (24, the engine's own number), cap 3 - the scaling shape
					// faces already use. Nothing weights per roll untouched;
					// a plain room is still allowed to be plain, it just is
					// not forever one-roll regardless of size. Sim-measured:
					// big plains 2.9 items avg, site totals unmoved.
					var cs = CellsOf(p);
					int rolls = Math.Max(1, Math.Min(3, cs.Count / 24 + 1));
					for (int ri = 0; ri < rolls; ri++)
						if (cs.Count > 0 && !Seat(p, WRoll(T_PLAINTRACE), cs)) misses.Add((T_PLAINTRACE, p, cs));
				}
			}
			if (roles.YardRect != null) DressYard(roles.YardRect, roles.YardState ?? "working", roles.YardDesc != null && roles.YardDesc.StartsWith("outside"));
			// WARREN FLOOR: only the tail is cut - Nothing-rolled spaces re-roll
			// with the Nothing row removed until the site clears its floor
			int eligible = pockets.Count(p => p.Role == "face" || p.Role == "commons" || p.Role == "dwelling");
			int wantFloor = Math.Max(2, (int)Math.Round(eligible * 0.45));
			string WRollNo(Row[] tbl)
			{
				int t = 0; foreach (var r in tbl) if (r.Id != null) t += r.Wt;
				if (t == 0) return null;
				double x = Rng() * t;
				foreach (var r in tbl) { if (r.Id == null) continue; x -= r.Wt; if (x < 0) return r.Hint != null ? r.Id + "|" + r.Hint : r.Id; }
				return null;
			}
			int fguard = 0;
			while (tableItems < wantFloor && misses.Count > 0 && fguard++ < 12)
			{
				int mi = R(0, misses.Count - 1);
				var (tbl, p, cs) = misses[mi]; misses.RemoveAt(mi);
				if (Seat(p, WRollNo(tbl), cs)) topups++;
			}
			// CORRIDOR TRACES: the sparsest table on non-artery segments
			var segSeen = new byte[g.W * g.H];
			bool Corr(int i2) => g.C[i2] == FLOOR && g.Host[i2] == 0 && g.Water[i2] == 0 && g.Art[i2] == 0 && g.Pocket[i2] < 0 && i2 != g.MouthIdx;
			for (int i0 = 0; i0 < g.W * g.H; i0++)
			{
				if (segSeen[i0] != 0 || !Corr(i0)) continue;
				var seg = new List<int> { i0 };
				var st = new Stack<int>(); st.Push(i0); segSeen[i0] = 1;
				while (st.Count > 0)
				{
					int c = st.Pop(); int cx = c % g.W, cy = c / g.W;
					foreach (var d in N4)
					{
						int nx = cx + d[0], ny = cy + d[1];
						if (!g.Inb(nx, ny)) continue;
						int ni = g.Idx(nx, ny);
						if (segSeen[ni] != 0 || !Corr(ni)) continue;
						segSeen[ni] = 1; seg.Add(ni); st.Push(ni);
					}
				}
				if (seg.Count < 3) continue;
				string rolled = WRoll(T_HALLTABLE);
				if (rolled == null) continue;
				string item = rolled, hint = null;
				int bar = rolled.IndexOf('|');
				if (bar >= 0) { hint = rolled.Substring(bar + 1); item = rolled.Substring(0, bar); }
				var open = seg.Where(i2 => g.Item[i2] == null).ToList();
				if (open.Count == 0) continue;
				if (item == "chalkmark")
				{
					// a claim mark chalked against the passage wall
					var walled = new List<(int i, string id)>();
					foreach (int i2 in open)
					{
						int cx = i2 % g.W, cy = i2 / g.W;
						foreach (var (dd, dx, dy) in new[] { ("N", 0, -1), ("S", 0, 1), ("E", 1, 0), ("W", -1, 0) })
							if (g.Inb(cx + dx, cy + dy) && g.C[g.Idx(cx + dx, cy + dy)] != FLOOR) { walled.Add((i2, "chalkline" + dd)); break; }
					}
					if (walled.Count == 0) continue;
					var pick2 = walled[R(0, walled.Count - 1)];
					Put(pick2.i, pick2.id); continue;
				}
				int ci;
				if (hint == "face")
				{
					int best = -1; ci = open[0];
					foreach (int i2 in open)
					{
						int dd2 = g.MouthDist != null ? g.MouthDist[i2] : -1;
						if (dd2 > best) { best = dd2; ci = i2; }
					}
				}
				else ci = open[R(0, open.Count - 1)];
				Put(ci, item);
				if (item == "trash" && !Chance(.55))
				{
					int px2 = ci % g.W, py2 = ci / g.W;
					int extra = R(1, 3);
					for (int e = 0; e < extra; e++)
					{
						int nx = px2 + R(-1, 1), ny = py2 + R(-1, 1);
						if (!g.Inb(nx, ny)) continue;
						int ni = g.Idx(nx, ny);
						if (g.C[ni] == FLOOR && g.Host[ni] == 0 && g.Item[ni] == null && g.Water[ni] == 0 && ni != g.MouthIdx)
						{ Put(ni, "trash"); px2 = nx; py2 = ny; }
					}
				}
			}
			// NATURAL SCATTER: geology showing through the floor, corridors included
			var natFloors = new List<int>();
			for (int i = 0; i < g.W * g.H; i++)
				if (g.C[i] == FLOOR && g.Host[i] == 0 && g.Water[i] == 0 && g.Item[i] == null && i != g.MouthIdx) natFloors.Add(i);
			int natWant = R(2, 5) + (int)Math.Round(natFloors.Count * 0.03);
			for (int na = 0; na < natWant && natFloors.Count > 0; na++)
			{
				string it = WRoll(T_NATURAL); if (it == null) continue;
				for (int t = 0; t < 8 && natFloors.Count > 0; t++)
				{
					int pi = R(0, natFloors.Count - 1), ci = natFloors[pi];
					if (SOLID_ITEMS.Contains(it) && !SolidOK(ci % g.W, ci / g.W)) { refused++; continue; }
					natFloors.RemoveAt(pi);
					if (g.Item[ci] == null) Put(ci, it);
					break;
				}
			}
			// LIGHTING: sconces flank the commons entrances from the inside, dens
			// stay dark, one post at the mouth - the light you steer by
			{
				// OVERLIT COMMONS FIX (2026-08-23, user in game: "galleries are
				// spawning pretty overlit"): v2 commons straddle the haul road, so
				// they carry far more openings than v1's galleries - every cluster
				// double-flanked read as a landing strip. ONE flank per doorway,
				// spacing 3, hard cap 4 posts per commons. Dials CLAUDE, sim
				// catch-up flagged; "underground the glow is choreography" intact.
				var posts = new List<int[]>();
				bool PostOK(int px, int py) => posts.All(q => Math.Max(Math.Abs(q[0] - px), Math.Abs(q[1] - py)) >= 3);
				foreach (var p in pockets)
				{
					if (p.Role != "commons") continue;
					int pocketPosts = 0;
					var cs = CellsOf(p);
					var inC = new HashSet<int>(cs.Select(c2 => g.Idx(c2[0], c2[1])));
					var cand = new List<int[]>();   // [ex, ey, gx, gy]
					var seenD = new HashSet<int>();
					foreach (var c2 in cs)
						foreach (var d in N4)
						{
							int ex = c2[0] + d[0], ey = c2[1] + d[1];
							if (!g.Inb(ex, ey)) continue;
							int ei = g.Idx(ex, ey);
							if (seenD.Contains(ei) || inC.Contains(ei) || g.C[ei] != FLOOR) continue;
							int ox = ex + d[0], oy = ey + d[1];
							if (g.Inb(ox, oy) && g.Pocket[g.Idx(ox, oy)] >= 0 && g.Pocket[g.Idx(ox, oy)] != p.Id) continue;   // den door: dark
							seenD.Add(ei); cand.Add(new[] { ex, ey, c2[0], c2[1] });
						}
					var used = new HashSet<int>();
					foreach (var e in cand)
					{
						int ek = g.Idx(e[0], e[1]);
						if (used.Contains(ek)) continue;
						var cq = new List<int[]> { e }; used.Add(ek);
						for (int ci2 = 0; ci2 < cq.Count; ci2++)
							foreach (var o in cand)
							{
								int ok3 = g.Idx(o[0], o[1]);
								if (!used.Contains(ok3) && Math.Abs(o[0] - cq[ci2][0]) <= 2 && Math.Abs(o[1] - cq[ci2][1]) <= 2) { used.Add(ok3); cq.Add(o); }
							}
						if (pocketPosts >= 4) break;
						int ax = e[2] - e[0], ay = e[3] - e[1];
						foreach (var f in new[] { new[] { e[2] - ay, e[3] - ax }, new[] { e[2] + ay, e[3] + ax } })
						{
							if (!g.Inb(f[0], f[1])) continue;
							if (!inC.Contains(g.Idx(f[0], f[1])) || !IsFree(g.Idx(f[0], f[1]))) continue;
							if (!PostOK(f[0], f[1])) continue;
							Put(g.Idx(f[0], f[1]), "bg.torchpost"); posts.Add(new[] { f[0], f[1] });
							pocketPosts++;
							break;   // one flank per doorway - a lone flank is fine, the pair was the floodlight
						}
					}
				}
				// the mouth post
				var mq = new Queue<int[]>(); mq.Enqueue(new[] { art.Mouth.X, art.Mouth.Y, 0 });
				var mseen = new HashSet<int> { g.MouthIdx };
				while (mq.Count > 0)
				{
					var m = mq.Dequeue();
					int mi = g.Idx(m[0], m[1]);
					if (g.C[mi] == FLOOR && g.Host[mi] == 0 && g.Item[mi] == null && mi != g.MouthIdx)
					{
						bool hug = false;
						foreach (var d in N4) if (g.Inb(m[0] + d[0], m[1] + d[1]) && g.C[g.Idx(m[0] + d[0], m[1] + d[1])] != FLOOR) hug = true;
						if (hug) { Put(mi, "bg.torchpost"); break; }
					}
					if (m[2] < 2)
						foreach (var d in N4)
						{
							int nx = m[0] + d[0], ny = m[1] + d[1];
							if (g.Inb(nx, ny) && !mseen.Contains(g.Idx(nx, ny)) && g.C[g.Idx(nx, ny)] == FLOOR)
							{ mseen.Add(g.Idx(nx, ny)); mq.Enqueue(new[] { nx, ny, m[2] + 1 }); }
						}
				}
			}
			// TENSE: a dead site goes dark (per-space, so the held retreat gradient
			// stays a one-function change); reclaimed = TAKEN, the whole abandoned
			// base plus fresh snapjaw fires beside the cold hearths
			if (occupancy != "in use")
			{
				var flip = new Dictionary<string, string> {
					{ "bg.campfire", "campfire_remains" },
					{ "bg.torchpost", "torchpost_unlit" }, { "firedish", "firedish_cold" }, { "straytorch", null } };
				for (int i = 0; i < g.W * g.H; i++)
				{
					string it = g.Item[i];
					if (it == null || !flip.ContainsKey(it)) continue;
					int pid = g.Pocket[i];
					if (pid >= 0 && pid < pockets.Count && pockets[pid].Live) continue;
					string to = flip[it];
					g.Item[i] = to;
					if (to == null) items--;
				}
			}
			if (occupancy == "reclaimed")
			{
				// 1-2 crude fires, deep-biased - the newcomers den in the back
				var dens = pockets.OrderByDescending(p => p.DepthRank).Take(Math.Min(3, pockets.Count)).ToList();
				int fires = R(1, 2);
				foreach (var p in dens)
				{
					if (fires <= 0) break;
					var cs = CellsOf(p).Where(c2 => IsFree(g.Idx(c2[0], c2[1]))).ToList();
					if (cs.Count == 0) continue;
					var c = cs[R(0, cs.Count - 1)];
					Put(g.Idx(c[0], c[1]), "bg.campfire"); fires--;
				}
			}
			// trash scatter, all tenses; a dead site stops sweeping its road
			bool deadSite = occupancy != "in use";
			var tCells = new List<int>();
			for (int i = 0; i < g.W * g.H; i++)
				if (g.C[i] == FLOOR && g.Host[i] == 0 && g.Water[i] == 0 && g.Item[i] == null && i != g.MouthIdx
					&& (deadSite || g.Art[i] == 0)) tCells.Add(i);
			int tWant = (int)Math.Round(tCells.Count * (deadSite ? 0.048 : 0.012)) + (deadSite ? R(2, 4) : R(0, 2));
			for (int n = 0; n < tWant && tCells.Count > 0; n++)
			{
				int pi = R(0, tCells.Count - 1), ci = tCells[pi]; tCells.RemoveAt(pi);
				if (g.Item[ci] != null) continue;
				Put(ci, "trash");
				if (!Chance(.55))
				{
					int px2 = ci % g.W, py2 = ci / g.W;
					int extra = R(1, 3);
					for (int e = 0; e < extra; e++)
					{
						int nx = px2 + R(-1, 1), ny = py2 + R(-1, 1);
						if (!g.Inb(nx, ny)) continue;
						int ni = g.Idx(nx, ny);
						if (g.C[ni] == FLOOR && g.Host[ni] == 0 && g.Item[ni] == null && g.Water[ni] == 0 && ni != g.MouthIdx)
						{ Put(ni, "trash"); px2 = nx; py2 = ny; }
					}
				}
			}
			// ROOM FLOOR - the warren-floor law one level down (user docket
			// 2026-08-24: rooms should spawn with *some* kind of decoration).
			// Runs LAST so natural scatter and tense trash count as decoration:
			// only a room that ends the whole dress with ZERO items re-rolls
			// its own table with the Nothing row removed - same cells, same
			// hints, same guards. Per-space Nothing weights stay as ruled;
			// count variance survives, only the zero collapses to one.
			// Dwellings never need it (their budget floors at 1); a room with
			// no legal cell stays bare and counts refused. Sim-verified
			// 2026-08-24: 497 sites / 1651 rooms, bare 109 -> 0, density unmoved.
			int roomFloored = 0;
			foreach (var p in pockets)
			{
				Row[] tbl = p.Role == "face" ? T_SOCKET
					: p.Role == "plain" ? T_PLAINTRACE
					: p.Role == "commons" ? T_GALLERYFLOOR : null;
				if (tbl == null) continue;
				var cs = CellsOf(p); if (cs.Count == 0) continue;
				bool hasItem = false;
				foreach (var c2 in cs) if (g.Item[g.Idx(c2[0], c2[1])] != null) { hasItem = true; break; }
				if (hasItem) continue;
				if (Seat(p, WRollNo(tbl), cs)) roomFloored++;
			}
			return $"{items} items, {tableItems} table, {topups} topups, {roomFloored} room-floored, {refused} solid-refused";
		}

		// sim item id -> real blueprint. Every name here is verified against Base
		// or the mod's own XML; an unverified name is a MODERROR at spawn, so the
		// unconfirmed ones were dropped or substituted (flags in the TODO entry).
		private static readonly Dictionary<string, string> BP = new Dictionary<string, string>
		{
			{ "straytorch", "Torch" }, { "pickaxe", "Pickaxe" }, { "trash", "Garbage" },
			{ "chest", "Chest" }, { "bg.chest", "Chest" },
			{ "stack1", "Cleo_TerraFirma_QuarryStones1" }, { "stack2", "Cleo_TerraFirma_QuarryStones2" }, { "stack3", "Cleo_TerraFirma_QuarryStones3" },
			{ "quarrybricks", "Cleo_TerraFirma_QuarriedBlock" },
			{ "bg.random_stone_statue", "Random Stone Statue" }, { "bg.village_monument_stele", "Village Monument Stele" },
			{ "bg.campfire", "Campfire" }, { "campfire_remains", "Campfire Remains" },
			{ "bg.woven_basket", "Woven Basket" }, { "basket_stones", "Woven Basket" }, { "basket_food", "Woven Basket" },
			{ "bg.floor_cushion", "Floor Cushion" }, { "bg.canvas_folding_chair", "Canvas Folding Chair" },
			{ "bg.bed", "Bed" }, { "bg.bedroll", "Bedroll" }, { "bg.folding_cot", "Folding Cot" },
			{ "bg.table", "Table" }, { "bg.workbench", "Workbench" },
			{ "bg.animation_table", "Cleo_TerraFirma_AnimationTable" }, // sketch vocab reserved 2026-08-24; no sketch uses it yet

			{ "bg.wine_pitcher", "Wine Pitcher" }, { "pitcher_wine", "Wine Pitcher" }, { "pitcher_cider", "Cider Vase" },
			{ "bg.vase", "Vase" }, { "bg.clay_jug", "Jug" },
			{ "bg.anvil", "Anvil" }, { "bg.oven", "Oven" }, { "bg.hookah", "Hookah" }, { "bg.bench", "Bench" },
			{ "kline", "Kline" }, { "stool", "Stool" }, { "kiln", "Kiln" },
			{ "bg.torchpost", "Torchpost" }, { "torchpost_unlit", "Unlit Torchpost" },
			{ "firedish", "Cleo_TerraFirma_FireDish" }, { "firedish_cold", "Cleo_TerraFirma_FireDish" },
			{ "stonefig", "Cleo_TerraFirma_Random Stone Figurine" }, { "figurine", "Random Figurine" },
			{ "largeboulder", "LargeBoulder" },
			// fallen rock OUT of dress (USER RULED 2026-08-25 "replace it with
			// basegame rocks"): stone = the takeable pocket rock, loosestone =
			// the robbed-yard block remnant, so it reads chunkier. Picks CLAUDE
			// dials, both verified (Items.xml:3016 / 3049).
			{ "stone", "Small Stone" }, { "loosestone", "SmallBoulder" }, { "pebble", "Cleo_TerraFirma_Rock_Pebbles" },
			{ "cairn", "Cleo_TerraFirma_Cairn" }, { "cairn_toppled", "Cleo_TerraFirma_CairnToppled" }, { "dirtpath", "DirtPath" },   // trail port 2026-08-25; DirtPath IS the register (ZoneTerrain.xml:937)
			{ "nugget", "Copper Nugget" },
			{ "bracelet1", "Bracelet1" }, { "bracelet2", "Bracelet2" }, { "bracelet3", "Bracelet3" },
		};

		private static int PlaceItems(Zone Z, Grid g)
		{
			if (g.Item == null) return 0;
			int placed = 0;
			for (int i = 0; i < g.W * g.H; i++)
			{
				string id = g.Item[i];
				if (id == null) continue;
				Cell c = Z.GetCell(i % g.W, i / g.W);
				if (c == null || c.HasWall()) continue;
				GameObject obj;
				if (id.StartsWith("chalkline"))
				{
					obj = GameObject.Create("Cleo_TerraFirma_ChalkLine");
					if (obj.Render != null) obj.Render.Tile = id + ".png";   // the directional piece
				}
				else if (id == "yardboard")
				{
					obj = GameObject.Create("Chalkboard");
					if (g.YardBoardText != null)
						obj.RequirePart<XRL.World.Parts.Cleo_TerraFirma_ChalkWriting>().Text = g.YardBoardText;
				}
				else
				{
					if (!BP.TryGetValue(id, out string bp)) continue;
					obj = GameObject.Create(bp);
					if (id == "firedish_cold")
					{
						// the cold flip: light and heat stripped (sprite may still
						// read lit - art polish flagged in TODO)
						obj.RemovePart<XRL.World.Parts.LightSource>();
						obj.RemovePart<XRL.World.Parts.Campfire>();
					}
				}
				c.AddObject(obj);
				placed++;
			}
			return placed;
		}

		// ---------------------------------------------------------------- build
		public class Result
		{
			public bool Built;
			public string Reason = "";
			public string Stage = "", Occupancy = "", Roles = "", Yard = "";
			public int StageIdx = -1;   // index into STAGES; the scout's capacity read
			public string Layout = "boxes";
			public int Floor, Pockets, Degraded, Attempt, Seed;
		}

		/// <summary>One complete attempt (buildOnce). Grid-side only; ApplyToZone lands it.</summary>
		private static Result BuildOnce(Zone Z, int seed, int? forced, int attempt, bool blockLayout, out Grid gridOut, out List<PocketRec> pocketsOut)
		{
			_state = (uint)seed; _noiseSeed = seed;
			gridOut = null; pocketsOut = null;
			var g = ReadHost(Z);
			int si = forced ?? R(0, STAGES.Length - 1);
			int wanted = si, degraded = 0;

			var band = LargestBand(g);
			if (band == null) return new Result { Built = false, Reason = "no band", Seed = seed };
			var aq = PlaceAquifers(g, band);
			band = LargestBand(g);   // pools ate rock; re-measure
			if (band == null) return new Result { Built = false, Reason = "no band after geology", Seed = seed };

			// PORT FIX round 3 (2026-08-23, first sealed warren): the mouth must open
			// onto host floor CONNECTED to the zone's traversal network, not any open
			// cell - the real zone has isolated clearings inside the rock mass that
			// the sim's canyon host never had. Connected = reachable from a zone-edge
			// open cell (surface canyon travel enters at the edges).
			// ROUND 10 (2026-08-23, the sealed-from-the-canyon warren: "doesn't open
			// out into the canyon"): "connected" now means THE CANYON, not the zone
			// edge. Edge-flooding accepted any open sliver touching the border, and
			// a mouth legally opened onto one - reachable only from the next
			// parasang, sealed from this zone's own network. The canyon announces
			// itself: CanyonBuilder stamps a CanyonMarker widget on every corridor
			// cell (stair spurs included), so the flood seeds from markers and
			// spreads over open ground. Edge seeding survives only as the fallback
			// for markerless zones (tfslab tests, oddball terrain).
			var connected = new byte[g.W * g.H];
			{
				var cq = new Queue<int>();
				int seeds = 0;
				for (int i = 0; i < g.W * g.H; i++)
					if (g.Marker[i] != 0 && g.C[i] == FLOOR && g.Water[i] == 0)
					{ connected[i] = 1; cq.Enqueue(i); seeds++; }
				bool fallback = seeds == 0;
				if (fallback)
				{
					for (int x = 0; x < g.W; x++)
						foreach (int yy in new[] { 0, g.H - 1 })
							if (g.C[g.Idx(x, yy)] == FLOOR && g.Water[g.Idx(x, yy)] == 0 && connected[g.Idx(x, yy)] == 0)
							{ connected[g.Idx(x, yy)] = 1; cq.Enqueue(g.Idx(x, yy)); }
					for (int y = 0; y < g.H; y++)
						foreach (int xx in new[] { 0, g.W - 1 })
							if (g.C[g.Idx(xx, y)] == FLOOR && g.Water[g.Idx(xx, y)] == 0 && connected[g.Idx(xx, y)] == 0)
							{ connected[g.Idx(xx, y)] = 1; cq.Enqueue(g.Idx(xx, y)); }
				}
				while (cq.Count > 0)
				{
					int ci = cq.Dequeue();
					int cx = ci % g.W, cy = ci / g.W;
					foreach (var d in N4)
					{
						int nx = cx + d[0], ny = cy + d[1];
						if (!g.Inb(nx, ny)) continue;
						int ni = g.Idx(nx, ny);
						if (connected[ni] != 0 || g.C[ni] != FLOOR || g.Water[ni] != 0) continue;
						connected[ni] = 1; cq.Enqueue(ni);
					}
				}
				int connCount = 0; for (int i = 0; i < connected.Length; i++) if (connected[i] != 0) connCount++;
				Helpers.VerifyLog("WARREN", $"host connectivity: {connCount} canyon-connected open cells ({(fallback ? "edge fallback" : seeds + " marker seeds")})");
				// degenerate zone (no markers AND no edge-open floor): any host, so
				// a fully-walled test slab still carves under the wish
				if (connCount == 0)
					for (int i = 0; i < connected.Length; i++) if (g.C[i] == FLOOR && g.Water[i] == 0) connected[i] = 1;
			}
			var skin = band.Cells.Where(i =>
			{
				int x = i % g.W, y = i / g.W;
				if (x < 1 || y < 1 || x > g.W - 2 || y > g.H - 2) return false;
				return N4.Any(d => g.Inb(x + d[0], y + d[1]) && connected[g.Idx(x + d[0], y + d[1])] != 0);
			}).ToList();
			if (skin.Count == 0) return new Result { Built = false, Reason = "band never meets the canyon", Seed = seed };
			for (int i = skin.Count - 1; i > 0; i--) { int j = R(0, i); (skin[i], skin[j]) = (skin[j], skin[i]); }
			// each attempt starts from a different face (same canyon, same seed)
			if (attempt > 0 && skin.Count > 1)
			{
				int rot = (attempt * 5) % skin.Count;
				var moved = skin.Take(rot).ToList();
				skin.RemoveRange(0, rot);
				skin.AddRange(moved);
			}

			RegionRec region = null; Pt mouth = null;
			foreach (int mi in skin.Take(FACE_SAMPLE))
			{
				var cand = new Pt { X = mi % g.W, Y = mi / g.W };
				var reachR = GrowRegion(g, cand, int.MaxValue, connected);
				if (reachR.Size < FACE_PREDICTOR) continue;   // predictor only; licensing stays the bar
				int sTry = si, deg = 0;
				while (sTry > 0 && STAGES[sTry].Area > reachR.Size) { sTry--; deg++; }
				region = GrowRegion(g, cand, STAGES[sTry].Area, connected);
				si = sTry; degraded = deg; mouth = cand;
				break;
			}
			if (region == null) return new Result { Built = false, Reason = "no face opens onto enough rock", Seed = seed };
			int bx0 = g.W, by0 = g.H, bx1 = 0, by1 = 0;
			foreach (int ci in region.Cells)
			{
				int x = ci % g.W, y = ci / g.W;
				if (x < bx0) bx0 = x; if (x > bx1) bx1 = x;
				if (y < by0) by0 = y; if (y > by1) by1 = y;
			}
			var foot = new Box { X0 = bx0, Y0 = by0, X1 = bx1, Y1 = by1 };
			g.Region = region.Mask;

			// occupancy: whole-site, three states (ruled 2026-08-18); per-pocket
			// live flags kept so the held retreat gradient stays a one-function change
			int oRoll = R(1, 100);
			string occupancy = oRoll <= ABANDONED_PCT ? "abandoned"
				: oRoll <= ABANDONED_PCT + RECLAIMED_PCT ? "reclaimed" : "in use";

			// ROUND 5 (2026-08-23, the stubby-site watch item fired: band 1051 built
			// only 71 floor): waypoints must live in the MOUTH'S LOBE. Round 4 lets
			// the region span pinches, but the artery walks only claimed cells, so
			// far-lobe waypoints stalled it at the first pinch - breaking RULE 1
			// (RULED: the artery traces most of the compound). The artery now fully
			// works its own lobe; cross-pinch mass stays in the region for tunnels
			// and infill, which already pay the sim-sanctioned dear-skin cost to
			// cross. Lobe share logged for the stubby-site watch.
			var lobeCells = new List<int>();
			{
				var lseen = new byte[g.W * g.H];
				var lq = new Queue<int>();
				int m0 = g.Idx(mouth.X, mouth.Y);
				lseen[m0] = 1; lq.Enqueue(m0); lobeCells.Add(m0);
				while (lq.Count > 0)
				{
					int ci = lq.Dequeue();
					int lx = ci % g.W, ly = ci / g.W;
					foreach (var d in N4)
					{
						int nx = lx + d[0], ny = ly + d[1];
						if (!g.Inb(nx, ny)) continue;
						int ni = g.Idx(nx, ny);
						if (lseen[ni] != 0 || region.Mask[ni] == 0) continue;
						lseen[ni] = 1; lq.Enqueue(ni); lobeCells.Add(ni);
					}
				}
				Helpers.VerifyLog("WARREN", $"mouth lobe: {lobeCells.Count} of {region.Size} claimable cells");
			}
			var S = STAGES[si];
			var art = LayArtery(g, S, mouth, foot, region.Mask, lobeCells);
			g.MouthIdx = g.Idx(art.Mouth.X, art.Mouth.Y);
			bool wantHall = si == 2 && Chance(COMMONS_PCT / 100.0);
			List<PocketRec> pockets;
			if (blockLayout)
			{
				pockets = CarveBlocks(g, art, S, wantHall);
			}
			else
			{
				pockets = FindPockets(g, art, R(S.P0, S.P1), wantHall);
				foreach (var p in pockets) ResolvePocket(g, p);
				foreach (var p in pockets) ConnectPocket(g, p, art);
			}
			foreach (var p in pockets) p.Live = occupancy == "in use";
			int crosscuts = AddCrosscuts(g, S.Cross);
			int filled = Infill(g, art, pockets, R(S.Fill0, S.Fill1));
			int breaches = BreachAquifers(g);
			int repairs = RepairConnectivity(g);
			int trimmed = TrimSlack(g);
			var m = MeasureG(g);
			// worth the trip: the bar sits on what actually got BUILT
			if (m.Floor < MIN_SITE || pockets.Count < 2)
				return new Result { Built = false, Reason = $"too little got built ({m.Floor} floor, {pockets.Count} rooms)", Seed = seed };
			// stage honesty: a stage that under-builds its floor DEMOTES in place
			int[] floorMin = { 0, CAMP_FLOOR, FLOUR_FLOOR };
			while (si > 0 && m.Floor < floorMin[si]) { si--; degraded++; }
			var roles = RegisterRoles(g, art, pockets, si, occupancy);
			string dressStats = DressSite(g, art, pockets, roles, occupancy, m.Floor);
			Helpers.VerifyLog("WARREN", $"dress: {dressStats}");
			Helpers.VerifyLog("WARREN", $"carve: band {band.Mass}, mouth ({mouth.X},{mouth.Y}), stage {STAGES[si].Name} (wanted {STAGES[wanted].Name}, degraded {degraded}), " +
				$"{pockets.Count} pockets, floor {m.Floor}, loops {m.Loops}, comps {m.Comps}, crosscuts {crosscuts}, infill {filled}, " +
				$"aquifers {aq.Pools}p/{aq.Veins}v, breaches {breaches}, repairs {repairs}, trimmed {trimmed}, layout {(blockLayout ? "blocks" : "boxes")}");
			Helpers.VerifyLog("WARREN", $"roles: {roles.Tally}; yard {roles.YardDesc}; occupancy {occupancy}");
			gridOut = g; pocketsOut = pockets;
			return new Result
			{
				Built = true, Stage = STAGES[si].Name, StageIdx = si, Occupancy = occupancy,
				Roles = roles.Tally, Yard = roles.YardDesc, Layout = blockLayout ? "blocks" : "boxes",
				Floor = m.Floor, Pockets = pockets.Count, Degraded = degraded, Attempt = attempt, Seed = seed,
			};
		}

		/// <summary>The retry contract (build): same canyon every attempt, first
		/// attempt that clears the bar wins, and no-warren stays a legal outcome
		/// (a rumour that does not pay off is fine - ruled, abandoned villages).
		/// RETRY-WITH-BLOCKS (ruled 2026-08-23 "like all three of these fixes"):
		/// a face whose BOX carve under-builds below licensing retries once with
		/// the block layout before the next face burns - boxes stay the default
		/// look per the 08-18 A/B lean; blocks are the snake-country specialist.</summary>
		/// <summary>apply=false is the SCOUT'S DRY RUN (scout-and-quota port
		/// 2026-08-25): the full pipeline runs - degrade honesty, worth-the-trip,
		/// retry-with-blocks - but the zone is never touched. Forced-flourishing
		/// dry runs read a zone's CAPACITY: the scorer IS the builder, so score
		/// and build cannot disagree, and a chosen flourishing carve is an exact
		/// replay of its own probe (same seed, same forced stage).</summary>
		public static Result Build(Zone Z, int seed, int? forcedStage = null, bool apply = true)
		{
			Result last = null;
			for (int a = 0; a < ATTEMPTS; a++)
			{
				var r = BuildOnce(Z, seed, forcedStage, a, false, out Grid g, out List<PocketRec> pockets);
				if (r.Built)
				{
					if (apply) ApplyToZone(Z, g);
					return r;
				}
				last = r;
				if (r.Reason.StartsWith("too little got built"))
				{
					var r2 = BuildOnce(Z, seed, forcedStage, a, true, out Grid g2, out List<PocketRec> pockets2);
					if (r2.Built)
					{
						Helpers.VerifyLog("WARREN", $"blocks rescued the face (attempt {a}, boxes built {r.Reason})");
						if (apply) ApplyToZone(Z, g2);
						return r2;
					}
					last = r2;
				}
				if (r.Reason == "no band" || r.Reason == "no band after geology" || r.Reason == "band never meets the canyon") break;
			}
			Helpers.VerifyLog("WARREN", $"no site: {last?.Reason ?? "?"} (zone {Z.ZoneID}, seed {seed})");
			return last ?? new Result { Built = false, Reason = "no attempt ran", Seed = seed };
		}

		// (ProspectMark REMOVED same day it landed - the prospect fallback was
		// superseded by the self-erasing ruling ("just stopped existing"). The
		// mechanism lives in git history at c27bf12 if abandoned prospects ever
		// return as deliberate content rather than as an apology.)

		// ---------------------------------------------------------------- apply
		// Grid to zone: final FLOOR cells that are ours lose their walls; aquifer
		// cells become deep brackish pools (the Redrock water idiom); moss walls
		// recolor in place (TileColor K / DetailColor g, the v1 user picks).
		private static void ApplyToZone(Zone Z, Grid g)
		{
			int carved = 0, pools = 0, mossed = 0, padded = 0;
			for (int y = 0; y < g.H; y++)
				for (int x = 0; x < g.W; x++)
				{
					int i = g.Idx(x, y);
					if (g.Aqua[i] != 0)
					{
						Cell c = Z.GetCell(x, y);
						if (c == null) continue;
						foreach (GameObject wall in c.GetWalls().ToList()) wall.Obliterate();
						if (!c.HasObjectWithPart("LiquidVolume")) { c.AddObject(GameObject.Create("DeepBrackishPool")); pools++; }
						continue;
					}
					if (g.C[i] == FLOOR && g.Host[i] == 0)
					{
						Cell c = Z.GetCell(x, y);
						if (c == null) continue;
						foreach (GameObject wall in c.GetWalls().ToList()) wall.Obliterate();
						carved++;
						// PADDED EARTH (user ruled 2026-08-12, whole-warren scope;
						// gap user-flagged in game 2026-08-24; base parent DIRECT
						// per user ruling same day, no rename blueprint): every
						// carved cell gets the stock DirtFloor, the strict
						// VillageMaker hut idiom. The mouth stays raw, a doorstep
						// worn back to rock; water keeps its pools.
						if (i != g.MouthIdx && g.Water[i] == 0)
						{
							c.AddObject(GameObject.Create("DirtFloor"));
							padded++;
						}
					}
				}
			// MOSS AS A LIVING COAT (USER RULED 2026-08-23: "shale that has grown
			// mossy should be prefixed green|mossy shale. the moss should be
			// harvestable, reverting the wall to its basic form"). Stock
			// Harvestable carries the whole loop - DestroyOnHarvest=false keeps
			// the wall, Unripe* holds the wall's ORIGINAL palette captured here,
			// harvest drops the moss clump and the render reverts natively; the
			// MossyWall part adds the name adjective only while Ripe. Regen
			// deliberately off (RegenTime unset) - regrowth is an unruled float.
			foreach (int i in MossCells(g))
			{
				Cell c = Z.GetCell(i % g.W, i / g.W);
				GameObject wall = c?.GetWalls().FirstOrDefault();
				if (wall?.Render == null) continue;
				var h = wall.RequirePart<XRL.World.Parts.Harvestable>();
				h.Ripe = true;
				h.DestroyOnHarvest = false;
				h.OnSuccess = "Cleo_TerraFirma_MossClump";
				h.OnSuccessAmount = "1";
				h.RipeColor = "&K"; h.RipeTileColor = "&K"; h.RipeDetailColor = "g";
				h.UnripeColor = wall.Render.ColorString ?? "";
				h.UnripeTileColor = wall.Render.TileColor ?? "";
				h.UnripeDetailColor = wall.Render.DetailColor ?? "";
				wall.RequirePart<XRL.World.Parts.Cleo_TerraFirma_MossyWall>();
				wall.Render.TileColor = "&K";
				wall.Render.ColorString = "&K";
				wall.Render.DetailColor = "g";
				mossed++;
			}
			int dressed = PlaceItems(Z, g);
			Helpers.VerifyLog("WARREN", $"applied: {carved} cells carved ({padded} padded), {pools} pool cells, {mossed} mossy walls, {dressed} dress items");
			// mouth forensics (round 3): the first live warren carved green and was
			// then found sealed. Name the mouth's neighbors at apply time so a
			// post-builder seal (biome mutation is the suspect - it runs after every
			// builder) shows up as a diff between this line and what the player sees.
			if (g.MouthIdx >= 0)
			{
				int mx = g.MouthIdx % g.W, my = g.MouthIdx / g.W;
				var names = new List<string>();
				foreach (var d in N8)
				{
					Cell nc = Z.GetCell(mx + d[0], my + d[1]);
					if (nc == null) continue;
					GameObject top = nc.GetWalls().FirstOrDefault() ?? nc.GetFirstObjectWithPart("LiquidVolume");
					names.Add($"({mx + d[0]},{my + d[1]}) {(top != null ? top.Blueprint : (nc.IsSolid() ? "solid?" : "open"))}");
				}
				Helpers.VerifyLog("WARREN", $"mouth ({mx},{my}) neighbors at apply: {string.Join(" | ", names)}");
			}
		}
	}
}
