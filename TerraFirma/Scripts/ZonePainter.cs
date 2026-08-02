using System;
using System.Collections.Generic;
using System.Linq;
using XRL;
using XRL.Messages;
using XRL.Rules;
using XRL.Wish;
using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// Painter test suite (2026-07-25): the three carve primitives — blob (node pocket),
	/// satellite (one-cell alcove, back-wall puncture guard), spine (1-wide tunnel, one
	/// L-bend, no pathfinding) — plus wishes that walk the permutation ladder so layouts
	/// can be critiqued before sprites exist:
	///   tfslab   — paint a 36x16 shale mass near the player (test canvas, any zone)
	///   tfpaint  — one node, communal dressing (fire dish, blocks, hookah, cushions)
	///   tfsleep  — one node + torchpost + 3 sleeping alcoves (bedroll + figurine roll)
	///   tftunnel — two nodes joined by a divot spine (sleeping divots on the tunnel)
	///   tfwarren — three nodes (communal / sleeping / yard) + two spines: the full shape
	/// All decisions log as [TF-VERIFY] [PAINT].
	/// </summary>
	[HasWishCommand]
	public static class ZonePainterTest
	{
		private const int NODE_HALF_WIDTH = 3;  // lateral radius of a node blob
		private const int NODE_HALF_DEPTH = 2;  // depth radius (into the wall)

		private class Carve
		{
			public List<Cell> Region = new List<Cell>();
			public int CX, CY, DX, DY;           // center + depth axis
			public string WallColor, WallTileColor, WallDetail;
		}

		// ============================== PRIMITIVES ==============================

		/// <summary>Blob: validate an ellipse footprint is all wall, sample its palette, carve it.</summary>
		private static Carve CarveBlob(Zone Z, int cx, int cy, int dx, int dy)
		{
			int px = dy != 0 ? 1 : 0, py = dx != 0 ? 1 : 0;
			var cells = new List<Cell>();
			for (int dep = -NODE_HALF_DEPTH; dep <= NODE_HALF_DEPTH; dep++)
			{
				for (int lat = -NODE_HALF_WIDTH; lat <= NODE_HALF_WIDTH; lat++)
				{
					double e = (double)(dep * dep) / (NODE_HALF_DEPTH * NODE_HALF_DEPTH)
							 + (double)(lat * lat) / (NODE_HALF_WIDTH * NODE_HALF_WIDTH);
					if (e > 1.3)
						continue;
					int fx = cx + dx * dep + px * lat;
					int fy = cy + dy * dep + py * lat;
					if (fx < 1 || fx > Z.Width - 2 || fy < 1 || fy > Z.Height - 2)
						return null;
					Cell f = Z.GetCell(fx, fy);
					if (f == null || !f.HasWall())
						return null;
					cells.Add(f);
				}
			}
			var carve = new Carve { CX = cx, CY = cy, DX = dx, DY = dy };
			GameObject sample = cells.Select(f => f.GetWalls().FirstOrDefault()).FirstOrDefault(w => w != null);
			carve.WallColor = sample?.Render?.ColorString;
			carve.WallTileColor = sample?.Render?.TileColor;
			carve.WallDetail = sample?.Render?.DetailColor;
			foreach (Cell f in cells)
				foreach (GameObject wall in f.GetWalls().ToList())
					wall.Obliterate();
			carve.Region = cells;
			Helpers.VerifyLog("PAINT", $"blob carved: center ({cx},{cy}), {cells.Count} cells, palette {carve.WallColor ?? "?"}/{carve.WallDetail ?? "?"}");
			return carve;
		}

		/// <summary>Satellite: carve one alcove cell off a region cell, only if the cell BEHIND
		/// the alcove is still wall (puncture guard — every alcove keeps a back).</summary>
		private static Cell CarveSatellite(Zone Z, Cell host, HashSet<Cell> forbidden)
		{
			int[,] dirs = { { 0, -1 }, { 0, 1 }, { 1, 0 }, { -1, 0 } };
			for (int d = 0; d < 4; d++)
			{
				int dx = dirs[d, 0], dy = dirs[d, 1];
				Cell alcove = Z.GetCell(host.X + dx, host.Y + dy);
				Cell behind = Z.GetCell(host.X + dx * 2, host.Y + dy * 2);
				if (alcove == null || behind == null || forbidden.Contains(alcove))
					continue;
				if (alcove.X < 1 || alcove.X > Z.Width - 2 || alcove.Y < 1 || alcove.Y > Z.Height - 2)
					continue;
				if (!alcove.HasWall() || !behind.HasWall())
					continue;
				foreach (GameObject wall in alcove.GetWalls().ToList())
					wall.Obliterate();
				Helpers.VerifyLog("PAINT", $"satellite carved @ ({alcove.X},{alcove.Y}) off ({host.X},{host.Y})");
				return alcove;
			}
			return null;
		}

		/// <summary>Spine: 1-wide L-tunnel between two points (x-leg then y-leg). Carves wall
		/// cells, passes through existing floor. Returns the path cells.</summary>
		private static List<Cell> CarveSpine(Zone Z, int x1, int y1, int x2, int y2)
		{
			var path = new List<Cell>();
			int x = x1, y = y1;
			while (x != x2) { x += Math.Sign(x2 - x); AddSpineCell(Z, x, y, path); }
			while (y != y2) { y += Math.Sign(y2 - y); AddSpineCell(Z, x, y, path); }
			Helpers.VerifyLog("PAINT", $"spine carved ({x1},{y1})->({x2},{y2}): {path.Count} cells");
			return path;
		}

		private static void AddSpineCell(Zone Z, int x, int y, List<Cell> path)
		{
			if (x < 1 || x > Z.Width - 2 || y < 1 || y > Z.Height - 2)
				return;
			Cell c = Z.GetCell(x, y);
			if (c == null)
				return;
			foreach (GameObject wall in c.GetWalls().ToList())
				wall.Obliterate();
			path.Add(c);
		}

		/// <summary>Mouth search: open floor with a deep wall run behind it, nearest the player.</summary>
		private static bool FindMouth(Zone Z, Cell near, out Cell mouth, out int mdx, out int mdy)
		{
			mouth = null; mdx = 0; mdy = 0;
			int need = NODE_HALF_DEPTH * 2 + 2;
			int[,] dirs = { { 0, -1 }, { 0, 1 }, { 1, 0 }, { -1, 0 } };
			var candidates = new List<(Cell c, int dx, int dy)>();
			for (int x = 1; x < Z.Width - 1; x++)
				for (int y = 1; y < Z.Height - 1; y++)
				{
					Cell c = Z.GetCell(x, y);
					if (c == null || c.IsSolid())
						continue;
					for (int d = 0; d < 4; d++)
					{
						int dx = dirs[d, 0], dy = dirs[d, 1], run = 0;
						while (run < need)
						{
							Cell probe = Z.GetCell(x + dx * (run + 1), y + dy * (run + 1));
							if (probe == null || !probe.HasWall())
								break;
							run++;
						}
						if (run >= need)
							candidates.Add((c, dx, dy));
					}
				}
			Helpers.VerifyLog("PAINT", $"mouth candidates: {candidates.Count}");
			if (candidates.Count == 0)
				return false;
			candidates.Sort((a, b) =>
				(Math.Abs(a.c.X - near.X) + Math.Abs(a.c.Y - near.Y))
				.CompareTo(Math.Abs(b.c.X - near.X) + Math.Abs(b.c.Y - near.Y)));
			mouth = candidates[0].c; mdx = candidates[0].dx; mdy = candidates[0].dy;
			return true;
		}

		/// <summary>Node A: carve at the nearest viable mouth. Later candidates are tried until
		/// one passes the blob footprint check.</summary>
		private static Carve CarvePrimaryNode(Zone Z, Cell near)
		{
			if (!FindMouth(Z, near, out Cell mouth, out int dx, out int dy))
				return null;
			// walk the candidate list implicitly: try progressively deeper centers on failure
			for (int extra = 0; extra < 3; extra++)
			{
				Carve c = CarveBlob(Z, mouth.X + dx * (NODE_HALF_DEPTH + 1 + extra), mouth.Y + dy * (NODE_HALF_DEPTH + 1 + extra), dx, dy);
				if (c != null)
					return c;
			}
			return null;
		}

		/// <summary>Secondary node: try offsets around an existing node until a blob fits.</summary>
		private static Carve CarveNeighborNode(Zone Z, Carve from)
		{
			int px = from.DY != 0 ? 1 : 0, py = from.DX != 0 ? 1 : 0;
			int gap = NODE_HALF_WIDTH * 2 + 7;   // wide enough that the spine reads as a tunnel, not a doorway
			var offsets = new (int ox, int oy)[] {
				(px * gap, py * gap), (-px * gap, -py * gap),
				(from.DX * (NODE_HALF_DEPTH * 2 + 7), from.DY * (NODE_HALF_DEPTH * 2 + 7)),
				(px * gap + from.DX * 3, py * gap + from.DY * 3),
				(-px * gap + from.DX * 3, -py * gap + from.DY * 3),
			};
			foreach (var (ox, oy) in offsets)
			{
				Carve c = CarveBlob(Z, from.CX + ox, from.CY + oy, from.DX, from.DY);
				if (c != null)
					return c;
			}
			Helpers.VerifyLog("PAINT", "neighbor node: no offset fit");
			return null;
		}

		// ============================== DRESSINGS ==============================

		private static void PlaceTinted(Cell cell, string blueprint, Carve palette)
		{
			GameObject obj = GameObject.Create(blueprint);
			if (obj.Render != null && !string.IsNullOrEmpty(palette.WallColor))
			{
				obj.Render.ColorString = palette.WallColor;
				obj.Render.TileColor = palette.WallTileColor;
				obj.Render.DetailColor = palette.WallDetail;
			}
			cell.AddObject(obj);
		}

		private static Cell CenterOf(Carve c)
		{
			double mx = c.Region.Average(r => r.X), my = c.Region.Average(r => r.Y);
			return c.Region.OrderBy(r => (r.X - mx) * (r.X - mx) + (r.Y - my) * (r.Y - my)).First();
		}

		private static void DressCommunal(Carve node)
		{
			var used = new HashSet<Cell>();
			Cell center = CenterOf(node);
			center.AddObject(GameObject.Create("Cleo_TerraFirma_FireDish"));
			used.Add(center);
			Helpers.VerifyLog("PAINT", $"fire dish @ ({center.X},{center.Y})");
			var edge = node.Region.Where(c => !used.Contains(c) && c.GetLocalAdjacentCells().Any(n => n != null && n.HasWall())).ToList();
			for (int i = 0; i < 3 && i < edge.Count; i++)
			{
				PlaceTinted(edge[i], "Cleo_TerraFirma_QuarriedBlock", node);
				used.Add(edge[i]);
			}
			Cell hk = edge.Skip(3).FirstOrDefault(c => !used.Contains(c));
			if (hk != null) { hk.AddObject(GameObject.Create("Hookah")); used.Add(hk); }
			var open = node.Region.Where(c => !used.Contains(c) && !c.IsSolid()).ToList();
			for (int i = 0; i < 2 && open.Count > 0; i++)
			{
				int idx = Stat.Random(0, open.Count - 1);
				open[idx].AddObject(GameObject.Create("Floor Cushion"));
				open.RemoveAt(idx);
			}
			Helpers.VerifyLog("PAINT", "communal dressing complete");
		}

		private static int DressSleepAlcoves(Zone Z, Carve node, int count)
		{
			var forbidden = new HashSet<Cell>(node.Region);
			var hosts = node.Region.Where(c => c.GetLocalAdjacentCells().Any(n => n != null && n.HasWall())).ToList();
			int made = 0;
			foreach (Cell host in hosts)
			{
				if (made >= count)
					break;
				Cell alcove = CarveSatellite(Z, host, forbidden);
				if (alcove == null)
					continue;
				forbidden.Add(alcove);
				// keep alcoves spaced: forbid the alcove's neighbors as future alcoves
				foreach (Cell n in alcove.GetLocalAdjacentCells())
					if (n != null) forbidden.Add(n);
				alcove.AddObject(GameObject.Create("Bedroll"));
				if (Stat.Random(1, 100) <= 50)
					alcove.AddObject(GameObject.Create("Random Copper Figurine"));
				made++;
				Helpers.VerifyLog("PAINT", $"sleep alcove {made}/{count} @ ({alcove.X},{alcove.Y})");
			}
			return made;
		}

		private static int DressSpineDivots(Zone Z, List<Cell> spine, int every)
		{
			var forbidden = new HashSet<Cell>(spine);
			int made = 0;
			for (int i = 1; i < spine.Count - 1; i += every)
			{
				Cell divot = CarveSatellite(Z, spine[i], forbidden);
				if (divot == null)
					continue;
				forbidden.Add(divot);
				divot.AddObject(GameObject.Create("Bedroll"));
				if (Stat.Random(1, 100) <= 50)
					divot.AddObject(GameObject.Create("Random Copper Figurine"));
				made++;
				Helpers.VerifyLog("PAINT", $"tunnel divot {made} @ ({divot.X},{divot.Y})");
			}
			return made;
		}

		// ============================== WISHES ==============================

		[WishCommand(Command = "tfslab")]
		public static void Slab()
		{
			GameObject ply = The.Player;
			Zone Z = ply?.CurrentZone;
			Cell at = ply?.CurrentCell;
			if (Z == null || at == null)
				return;
			int x0 = Math.Min(Math.Max(at.X + 3, 1), Z.Width - 38);
			int y0 = Math.Min(Math.Max(at.Y - 6, 1), Z.Height - 18);
			int filled = 0;
			for (int x = x0; x < x0 + 36; x++)
				for (int y = y0; y < y0 + 16; y++)
				{
					Cell c = Z.GetCell(x, y);
					if (c == null || c == at || c.IsSolid() || c.HasObjectWithPart("Combat"))
						continue;
					c.AddObject(GameObject.Create("Shale"));
					filled++;
				}
			Helpers.VerifyLog("PAINT", $"slab: {filled} shale cells at ({x0},{y0})..({x0 + 35},{y0 + 15})");
			MessageQueue.AddPlayerMessage($"tfslab: {filled}-cell shale canvas painted east of you. Carve away.");
		}

		[WishCommand(Command = "tfpaint")]
		public static void PaintNode()
		{
			Zone Z = The.Player?.CurrentZone;
			if (Z == null) return;
			Carve a = CarvePrimaryNode(Z, The.Player.CurrentCell);
			if (a == null) { MessageQueue.AddPlayerMessage("tfpaint: no viable wall mass here (try tfslab)."); return; }
			DressCommunal(a);
			MessageQueue.AddPlayerMessage($"tfpaint: communal node at ({a.CX},{a.CY}).");
		}

		[WishCommand(Command = "tfsleep")]
		public static void PaintSleep()
		{
			Zone Z = The.Player?.CurrentZone;
			if (Z == null) return;
			Carve a = CarvePrimaryNode(Z, The.Player.CurrentCell);
			if (a == null) { MessageQueue.AddPlayerMessage("tfsleep: no viable wall mass here (try tfslab)."); return; }
			CenterOf(a).AddObject(GameObject.Create("Torchpost"));
			int made = DressSleepAlcoves(Z, a, 3);
			MessageQueue.AddPlayerMessage($"tfsleep: node at ({a.CX},{a.CY}), {made} sleeping alcoves.");
		}

		[WishCommand(Command = "tftunnel")]
		public static void PaintTunnel()
		{
			Zone Z = The.Player?.CurrentZone;
			if (Z == null) return;
			Carve a = CarvePrimaryNode(Z, The.Player.CurrentCell);
			if (a == null) { MessageQueue.AddPlayerMessage("tftunnel: no viable wall mass here (try tfslab)."); return; }
			DressCommunal(a);
			Carve b = CarveNeighborNode(Z, a);
			if (b == null) { MessageQueue.AddPlayerMessage("tftunnel: {{R|DEGRADED}} — node B did not fit (wall mass too thin or already carved). This result is just tfpaint; use a fresh tfslab."); return; }
			var spine = CarveSpine(Z, a.CX, a.CY, b.CX, b.CY);
			int divots = DressSpineDivots(Z, spine, 3);
			var open = b.Region.Where(c => !c.IsSolid()).Take(2).ToList();
			foreach (Cell c in open)
				c.AddObject(GameObject.Create("Floor Cushion"));
			MessageQueue.AddPlayerMessage($"tftunnel: A ({a.CX},{a.CY}) -> B ({b.CX},{b.CY}), {spine.Count}-cell tunnel, {divots} divots.");
		}

		[WishCommand(Command = "tfwarren")]
		public static void PaintWarren()
		{
			Zone Z = The.Player?.CurrentZone;
			if (Z == null) return;
			Carve a = CarvePrimaryNode(Z, The.Player.CurrentCell);
			if (a == null) { MessageQueue.AddPlayerMessage("tfwarren: no viable wall mass here (try tfslab)."); return; }
			DressCommunal(a);
			Carve b = CarveNeighborNode(Z, a);
			Carve c = b != null ? CarveNeighborNode(Z, b) : null;
			if (c == null) c = CarveNeighborNode(Z, a);
			int nodes = 1, divots = 0;
			if (b != null)
			{
				nodes++;
				var spineAB = CarveSpine(Z, a.CX, a.CY, b.CX, b.CY);
				divots += DressSpineDivots(Z, spineAB, 3);
				DressSleepAlcoves(Z, b, 2);
			}
			if (c != null && c != b)
			{
				nodes++;
				var spineAC = CarveSpine(Z, a.CX, a.CY, c.CX, c.CY);
				divots += DressSpineDivots(Z, spineAC, 4);
				var edge = c.Region.Where(r => r.GetLocalAdjacentCells().Any(n => n != null && n.HasWall())).Take(3).ToList();
				foreach (Cell e in edge)
					PlaceTinted(e, "Cleo_TerraFirma_QuarriedBlock", c);
			}
			Helpers.VerifyLog("PAINT", $"warren complete: {nodes} nodes, {divots} tunnel divots");
			MessageQueue.AddPlayerMessage($"tfwarren: {nodes} nodes, {divots} tunnel divots. Communal / sleeping / yard.");
		}
	}
}
