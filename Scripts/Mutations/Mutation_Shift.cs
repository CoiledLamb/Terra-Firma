using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using ConsoleLib.Console;
using XRL.Core;
using Cleo.TerraFirma.Scripts;
using XRL.UI;

namespace XRL.World.Parts.Mutation
{
	[Serializable]
	public class Cleo_TerraFirma_Shift : BaseMutation
	{
		public const string COMMAND_ID = "Cleo_TerraFirma_CommandShift";
		public const int COOLDOWN = 50;

		public override bool CanLevel() => false;

		public override string GetDescription()
		{
			return "You heave a wall of stone aside.\n\n" +
				"Pushes an adjacent natural stone wall into a free tile next to it.\n" +
				"Cooldown: " + COOLDOWN + " rounds";
		}

		public override string GetLevelText(int Level) => "";

		public override bool Mutate(GameObject GO, int Level = 1)
		{
			ActivatedAbilityID = AddMyActivatedAbility("Shift", COMMAND_ID, "Physical Mutations");
			return base.Mutate(GO, Level);
		}

		public override void CollectStats(Templates.StatCollector stats, int Level)
		{
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), COOLDOWN);
		}

		public override bool Unmutate(GameObject GO)
		{
			RemoveMyActivatedAbility(ref ActivatedAbilityID);
			return base.Unmutate(GO);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == PooledEvent<CommandEvent>.ID;
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command != COMMAND_ID)
				return base.HandleEvent(E);
			string none = NothingToShift(ParentObject);
			if (none != null)
				return ParentObject.Fail(none);
			Cell wallCell, landing;
			GameObject wall;
			while (true)
			{
				wallCell = PickDirection(ForAttack: false, Label: "Shift which stone?");
				if (wallCell == null)
					return false;
				wall = CheckWall(ParentObject, wallCell, out string fail);
				if (wall == null)
				{
					ParentObject.Fail(fail);
					continue;
				}
				List<Cell> landings = Landings(ParentObject, wallCell);
				if (landings.Count == 0)
				{
					ParentObject.Fail("There's nowhere to shift " + wall.t() + ".");
					continue;
				}
				landing = landings[0];
				if (landings.Count == 1)
					break;
				Cell straightOn = wallCell.GetCellFromDirection(ParentObject.CurrentCell.GetDirectionFromCell(wallCell));
				Cell start = landings.Contains(straightOn) ? straightOn : landings[0];
				while (true)
				{
					landing = PickTarget.ShowPicker(PickTarget.PickStyle.EmptyCell, 0, 9999, start.X, start.Y, Locked: false, AllowVis.Any, Label: "Shift it where?");
					if (landing == null)
						return false;
					if (landings.Contains(landing))
						break;
					ParentObject.Fail(CantShiftThere(landing));
				}
				break;
			}
			if (!PlayShift(ParentObject, wall, wallCell, landing))
				Move(ParentObject, wall, wallCell, landing);
			UseEnergy(1000, "Physical Mutation Shift");
			CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
			PlayWorldSound("Sounds/Throw/sfx_throwing_stone_large_impact", 1f);
			DidXToY("shift", wall);
			return base.HandleEvent(E);
		}

		public static bool IsShiftable(GameObject Wall)
		{
			if (Wall == null || !Wall.IsWall())
				return false;
			if (Wall.HasPart<HologramMaterial>() || Wall.HasPart<Walltrap>() || Wall.HasPart<Forcefield>())
				return false;
			return Wall.HasPart<Cleo_TerraFirma_CalcifyWallPart>()
				|| Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(Wall.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>());
		}

		public static GameObject Blocker(Cell Cell)
		{
			foreach (GameObject o in Cell.Objects)
			{
				if (o.IsCombatObject() || o.IsWall() || o.ConsiderSolid() || o.IsTakeable()
					|| (o.Render != null && o.Render.RenderLayer > 5)
					|| o.HasPart<Door>() || o.HasPart<StairsUp>() || o.HasPart<StairsDown>())
					return o;
			}
			return null;
		}

		public static GameObject CheckWall(GameObject Actor, Cell WallCell, out string Fail)
		{
			Fail = null;
			Cell here = Actor?.CurrentCell;
			GameObject wall = here == null || WallCell == null || WallCell.ParentZone != here.ParentZone || here.OnWorldMap() ? null : WallCell.GetFirstWall();
			if (wall == null)
				Fail = "There's nothing there to shift.";
			else if (!IsShiftable(wall))
				Fail = "You can't budge " + wall.t() + ".";
			if (Fail != null)
				Helpers.VerifyLog("SHIFT", $"refused at ({WallCell?.X},{WallCell?.Y}): {Fail} objects: {(WallCell == null ? "none" : string.Join(", ", System.Linq.Enumerable.Select(WallCell.Objects, o => o.Blueprint)))}");
			return Fail == null ? wall : null;
		}

		public static List<Cell> Landings(GameObject Actor, Cell WallCell)
		{
			List<Cell> list = new List<Cell>();
			foreach (Cell c in WallCell.GetLocalAdjacentCells())
				if (c.ParentZone == WallCell.ParentZone && Blocker(c) == null)
					list.Add(c);
			return list;
		}

		public static bool TryShift(GameObject Actor, Cell WallCell, Cell Landing, out GameObject Wall, out string Fail)
		{
			Wall = CheckWall(Actor, WallCell, out Fail);
			if (Wall == null)
				return false;
			if (Landing == null || !Landings(Actor, WallCell).Contains(Landing))
			{
				Fail = CantShiftThere(Landing);
				return false;
			}
			Move(Actor, Wall, WallCell, Landing);
			return true;
		}

		private const float LEAD_SEC = 0.10f;
		private const float MOVE_SEC = 0.14f;
		private const float HOLD_SEC = 0.10f;
		private const float STONE_DEPTH = 20f;

		public static bool PlayShift(GameObject Actor, GameObject Wall, Cell From, Cell To)
		{
			GameManager gm = GameManager.Instance;
			Zone zone = From?.ParentZone;
			if (Cleo_TerraFirma_StoneRiseFX.HarnessMute || gm == null || zone == null || !zone.IsActive() || Actor == null || !Actor.IsPlayer()
				|| (!From.IsVisible() && !To.IsVisible()) || Wall.Render == null || Wall.Render.Tile.IsNullOrEmpty())
				return false;
			string tile = Regex.Replace(Wall.Render.Tile, "-[01]{8}(\\.[A-Za-z]+)$", "-" + Cleo_TerraFirma_StoneRiseFX.ISOLATED_MASK + "$1");
			string color = Wall.Render.TileColor.IsNullOrEmpty() ? Wall.Render.ColorString : Wall.Render.TileColor;
			int amp = color?.IndexOf('&') ?? -1;
			char fg = amp >= 0 && amp + 1 < color.Length ? color[amp + 1] : 'y';
			char detail = Wall.Render.DetailColor.IsNullOrEmpty() ? 'K' : Wall.Render.DetailColor[0];
			IRenderable sprite = new Cleo_TerraFirma_StoneTileRenderable(tile, fg, detail, 'k');
			float riseStart = LEAD_SEC + MOVE_SEC;
			float land = riseStart + MOVE_SEC + HOLD_SEC;
			if (From.IsVisible())
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(sprite, gm.getTileCenter(From.X, From.Y), 0f, 0.01f, LEAD_SEC, MOVE_SEC, STONE_DEPTH, STONE_DEPTH, clipToFloor: true), async: true);
			if (To.IsVisible())
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(sprite, gm.getTileCenter(To.X, To.Y), riseStart, MOVE_SEC, HOLD_SEC, 0.01f, STONE_DEPTH, STONE_DEPTH, vanishAfterHold: true, clipToFloor: true), async: true);
			ScreenBuffer buf = ScreenBuffer.GetScrapBuffer1(bLoadFromCurrent: true);
			TextConsole console = Popup._TextConsole;
			bool lifted = false;
			bool landed = false;
			System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
			for (float t = 0f; !landed; t = (float)clock.Elapsed.TotalSeconds)
			{
				if (!lifted && t >= LEAD_SEC)
				{
					From.RemoveObject(Wall);
					ZoneManager.PaintWalls(zone, From.X - 1, From.Y - 1, From.X + 1, From.Y + 1);
					lifted = true;
				}
				if (t >= land)
				{
					To.AddObject(Wall);
					ZoneManager.PaintWalls(zone, Math.Min(From.X, To.X) - 1, Math.Min(From.Y, To.Y) - 1, Math.Max(From.X, To.X) + 1, Math.Max(From.Y, To.Y) + 1);
					Cleo_TerraFirma_Fissure.SmokePuff(To);
					landed = true;
				}
				XRLCore.Core.RenderBaseToBuffer(buf);
				console.DrawBuffer(buf);
				if (!landed)
					Thread.Sleep(15);
			}
			Helpers.VerifyLog("SHIFT", $"{Actor.Blueprint} shifted {Wall.Blueprint} ({From.X},{From.Y}) -> ({To.X},{To.Y}), calcify back+forth, planned {land:0.00}s, took {clock.Elapsed.TotalSeconds:0.00}s, sprite {tile}");
			return true;
		}

		public static string NothingToShift(GameObject Actor)
		{
			Cell here = Actor?.CurrentCell;
			if (here == null || here.OnWorldMap())
				return "There's no stone nearby to shift.";
			bool anyStone = false;
			foreach (Cell c in here.GetLocalAdjacentCells())
			{
				if (!IsShiftable(c.GetFirstWall()))
					continue;
				anyStone = true;
				if (Landings(Actor, c).Count > 0)
					return null;
			}
			return anyStone ? "There's nowhere to shift the stone nearby." : "There's no stone nearby to shift.";
		}

		public static string CantShiftThere(Cell Landing)
		{
			GameObject blocker = Landing == null ? null : Blocker(Landing);
			return blocker != null ? "You can't shift that there, " + blocker.t() + " is in the way." : "You can't shift that there.";
		}

		public static void Move(GameObject Actor, GameObject Wall, Cell From, Cell To)
		{
			From.RemoveObject(Wall);
			To.AddObject(Wall);
			Helpers.VerifyLog("SHIFT", $"{Actor?.Blueprint} shifted {Wall.Blueprint} ({From.X},{From.Y}) -> ({To.X},{To.Y})");
		}
	}
}
