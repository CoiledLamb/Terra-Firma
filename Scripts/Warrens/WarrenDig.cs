using System;
using System.Collections.Generic;
using XRL;
using XRL.Rules;
using XRL.World;
using XRL.World.Parts;

namespace Cleo.TerraFirma.Scripts
{
	public static class Cleo_TerraFirma_WarrenDig
	{
		public const string PLANS = "Cleo_TerraFirma_WarrenDigPlans:";
		public const string SEEN = "Cleo_TerraFirma_WarrenDigSeen:";
		public const string DONE = "Cleo_TerraFirma_WarrenDigDone:";

		public const int CATCHUP_TURNS_PER_CELL = 120;
		public const int CATCHUP_MAX_CELLS = 12;
		public const int CATCHUP_MIN_TURNS = 100;

		public static List<List<Cell>> Plans(Zone zone)
		{
			var rooms = new List<List<Cell>>();
			string s = The.Game?.GetStringGameState(PLANS + zone.ZoneID);
			if (string.IsNullOrEmpty(s))
				return rooms;
			foreach (string room in s.Split('|'))
			{
				var cells = new List<Cell>();
				foreach (string pt in room.Split(';'))
				{
					string[] xy = pt.Split(',');
					if (xy.Length == 2 && int.TryParse(xy[0], out int x) && int.TryParse(xy[1], out int y))
					{
						Cell c = zone.GetCell(x, y);
						if (c != null)
							cells.Add(c);
					}
				}
				if (cells.Count > 0)
					rooms.Add(cells);
			}
			return rooms;
		}

		public static bool HasPlan(Zone zone) => Cleo_TerraFirma_DevHolds.On && !string.IsNullOrEmpty(The.Game?.GetStringGameState(PLANS + zone.ZoneID));

		private static bool Dug(Cell c) => c.GetFirstWall() == null;

		private static List<Cell> Current(Zone zone)
		{
			foreach (var room in Plans(zone))
				foreach (Cell c in room)
					if (!Dug(c))
						return room;
			return null;
		}

		public static List<Cell> Frontier(Zone zone)
		{
			var open = new List<Cell>();
			var room = Current(zone);
			if (room == null)
				return open;
			foreach (Cell c in room)
			{
				if (Dug(c) || !Breakable(c))
					continue;
				foreach (Cell n in c.GetCardinalAdjacentCells(bLocalOnly: true))
					if (!n.IsSolid())
					{
						open.Add(c);
						break;
					}
			}
			return open;
		}

		public static Cell Nearest(Zone zone, GameObject hewer)
		{
			Cell best = null;
			foreach (Cell c in Frontier(zone))
				if (best == null || hewer.DistanceTo(c) < hewer.DistanceTo(best))
					best = c;
			return best;
		}

		private static bool Breakable(Cell c)
		{
			foreach (GameObject o in c.Objects)
				if (o.IsImportant() || o.HasProperty(Cleo_TerraFirma_CrewShift.RESERVED)
					|| o.HasPart<ModWired>() || o.Blueprint == "PowerLine"
					|| !string.IsNullOrEmpty(o.GetPart<Cleo_TerraFirma_Etchable>()?.Inscription))
					return false;
			return true;
		}

		public static bool Break(Zone zone, Cell c, GameObject by, int stoneChance, int boulderChance, bool live)
		{
			GameObject rock = c.GetFirstWall();
			if (rock == null || !Breakable(c))
				return false;
			if (live && c.IsVisible())
			{
				rock.DustPuff();
				by?.PlayWorldSound("Sounds/Damage/sfx_damage_stone");
			}
			rock.Obliterate();
			int roll = Stat.Random(1, 100);
			if (roll <= boulderChance)
				c.AddObject("SmallBoulder");
			else if (roll <= boulderChance + stoneChance)
				c.AddObject("Small Stone");
			CheckFinished(zone);
			return true;
		}

		private static void CheckFinished(Zone zone)
		{
			var rooms = Plans(zone);
			int done = 0;
			foreach (var room in rooms)
			{
				bool all = true;
				foreach (Cell c in room)
					if (!Dug(c)) { all = false; break; }
				if (!all)
					break;
				done++;
			}
			int was = The.Game.GetIntGameState(DONE + zone.ZoneID);
			for (int i = was; i < done; i++)
				FinishRoom(zone, rooms[i], i);
			if (done > was)
				The.Game.SetIntGameState(DONE + zone.ZoneID, done);
		}

		private static void FinishRoom(Zone zone, List<Cell> room, int index)
		{
			Helpers.VerifyLog("DIG", $"room {index + 1} finished in {zone.ZoneID}: {room.Count} cells");
		}

		public static void Seen(Zone zone)
		{
			if (The.Game != null && HasPlan(zone))
				The.Game.SetStringGameState(SEEN + zone.ZoneID, The.Game.Turns.ToString());
		}

		public static void CatchUp(Zone zone, int stoneChance, int boulderChance)
		{
			if (The.Game == null || !HasPlan(zone))
				return;
			long now = The.Game.Turns;
			string key = SEEN + zone.ZoneID;
			string seen = The.Game.GetStringGameState(key);
			The.Game.SetStringGameState(key, now.ToString());
			if (string.IsNullOrEmpty(seen) || !long.TryParse(seen, out long last))
				return;
			long away = now - last;
			if (away < CATCHUP_MIN_TURNS)
				return;
			int hewers = 0;
			zone.ForeachObjectWithPart("Cleo_TerraFirma_CrewShift", o =>
			{
				var p = o.GetPart<Cleo_TerraFirma_CrewShift>();
				if (p != null && p.Job == "Dig" && !o.IsPlayerControlled() && o.Brain?.PartyLeader == null)
					hewers++;
			});
			if (hewers == 0)
				return;
			int want = (int)Math.Min(CATCHUP_MAX_CELLS, away * ((hewers + 1) / 2) / CATCHUP_TURNS_PER_CELL);
			int broke = 0;
			while (broke < want)
			{
				var frontier = Frontier(zone);
				if (frontier.Count == 0)
					break;
				if (!Break(zone, frontier[0], null, stoneChance, boulderChance, live: false))
					break;
				broke++;
			}
			if (broke > 0)
				Helpers.VerifyLog("DIG", $"caught up {broke} cell(s) in {zone.ZoneID} after {away} turns away with {hewers} hewer(s)");
		}
	}
}
