using System.Collections.Generic;
using System.Threading;
using ConsoleLib.Console;
using UnityEngine;
using XRL.Core;
using XRL.UI;
using XRL.World.Capabilities;

namespace XRL.World.Parts.Mutation
{
	public class Cleo_TerraFirma_Calcify : BaseMutation
	{
		#region Fields
		private const string COMMAND_ID = "Cleo_TerraFirma_CommandCalcify";

		private const float BASE_WALL_HP = 500f;
		private const int BASE_WALLS_CREATED = 4; // 4 at Lv1, exactly 9 at Lv10 -- Force Wall's count at full investment, in permanent stone

		private const int BASE_COOLDOWN = 90;
		#endregion

		#region Basic data
		public override string GetDescription()
		{
			// A' PICKED at the 2026-07-26 voice sitting ("A' sounds fine honestly"): Force Wall's
			// concrete register; "out of thin air" appears nowhere in base text. Built 2026-07-27.
			return "You raise walls of solid stone.";
		}

		public override string GetLevelText(int Level)
		{
			// 2026-07-26 voice sitting: flat constants unwrap (0 AV / -10 DV bare, values ratified
			// 2026-07-25), Cooldown lands last per the ~100%-of-base convention (was missing
			// entirely). Built 2026-07-27.
			return "Creates a contiguous line of permanent walls in empty tiles starting adjacent to you\n" +
				"Number created: {{rules|" + GetWallsCreated(Level) + "}}\n" +
				"Hit points per wall: {{rules|" + GetWallHp(Level) + "}}\n" +
				"Created walls have 0 AV and -10 DV\n" +
				"Cooldown: " + BASE_COOLDOWN + " rounds";
		}

		public override bool Mutate(GameObject GO, int Level = 1)
		{
			ActivatedAbilityID = AddMyActivatedAbility("Calcify", COMMAND_ID, "Physical Mutations");
			return base.Mutate(GO, Level);
		}

		public override bool Unmutate(GameObject GO)
		{
			RemoveMyActivatedAbility(ref ActivatedAbilityID);
			return base.Unmutate(GO);
		}

		public override void CollectStats(Templates.StatCollector stats, int Level)
		{
			stats.Set("WallNum", GetWallsCreated(Level));
			stats.Set("WallHp", GetWallHp(Level));
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), GetCooldown(Level));
		}
		#endregion

		#region Events
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != AIGetDefensiveAbilityListEvent.ID && ID != AIGetRetreatAbilityListEvent.ID)
				return ID == PooledEvent<CommandEvent>.ID;
			return true;
		}

		// ---- AI advertisement (NPC gap closed 2026-07-23; the quarriers ruling had already
		// committed this work). DEFENSIVE and RETREAT channels only, never offensive: unlike
		// Force Wall, whose owner can shoot out through their own fields, calcified stone is
		// opaque to everyone -- an offensive advertisement would teach NPCs to wall off their
		// own attacks. ----
		public override bool HandleEvent(AIGetDefensiveAbilityListEvent E)
		{
			GameObject threat;
			if (IsMyActivatedAbilityAIUsable(ActivatedAbilityID) && (threat = FindWallThreat(E.Target)) != null)
				E.Add(COMMAND_ID, 1, null, Inv: false, Self: false, threat);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(AIGetRetreatAbilityListEvent E)
		{
			GameObject threat;
			if (IsMyActivatedAbilityAIUsable(ActivatedAbilityID) && (threat = FindWallThreat(E.Target)) != null)
				E.Add(COMMAND_ID, 1, null, Inv: false, Self: false, threat);
			return base.HandleEvent(E);
		}

		/// <summary>The wall-off target: the event's target when valid, else the nearest visible
		/// hostile (Force Wall's retreat-scan recipe -- the retreat event's Target is often null).</summary>
		private GameObject FindWallThreat(GameObject given)
		{
			if (GameObject.Validate(ref given))
				return given;
			GameObject nearest = null;
			int nearestDistance = int.MaxValue;
			ParentObject.CurrentZone?.ForeachObject(delegate(GameObject obj)
			{
				if (obj.IsCreature && obj != ParentObject && ParentObject.IsHostileTowards(obj)
					&& ParentObject.HasLOSTo(obj, IncludeSolid: true, BlackoutStops: false, UseTargetability: true))
				{
					int d = ParentObject.DistanceTo(obj);
					if (d < nearestDistance)
					{
						nearest = obj;
						nearestDistance = d;
					}
				}
			});
			return nearest;
		}

		/// <summary>NPC cell-picking (the other half of the gap): a contiguous line across the
		/// approach -- the cell adjacent to the caster toward the threat, extended along both
		/// orthogonals. Force Wall's perpendicular-line brain, anchored caster-side to honor
		/// Calcify's own walls-start-adjacent-to-you rule. Occupied cells stay in the list;
		/// the placer below skips them (partial lines accepted, same as partial player casts).</summary>
		private List<Cell> PickNpcWallLine(GameObject threat)
		{
			Cell here = ParentObject.CurrentCell;
			Cell there = threat?.CurrentCell;
			if (here == null || there == null || here.ParentZone != there.ParentZone)
				return null;
			string dir = here.GetDirectionFromCell(there);
			Cell anchor = here.GetCellFromDirection(dir);
			if (anchor == null)
				return null;
			int walls = GetWallsCreated(Level);
			List<Cell> line = new List<Cell>(walls) { anchor };
			string[] flanks = XRL.Rules.Directions.GetOrthogonalDirections(dir);
			Cell left = anchor, right = anchor;
			while (line.Count < walls && (left != null || right != null))
			{
				left = left?.GetCellFromDirection(flanks[0]);
				if (left != null && line.Count < walls)
					line.Add(left);
				right = right?.GetCellFromDirection(flanks[1]);
				if (right != null && line.Count < walls)
					line.Add(right);
			}
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CALC", $"NPC cast: {ParentObject.Blueprint} walls off {threat.Blueprint} ({dir}ward), {line.Count} candidate cells");
			return line;
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command != COMMAND_ID)
				return base.HandleEvent(E);
			List<Cell> targeted = null;
			if (ParentObject.IsPlayer())
				targeted = PickFieldAdjacent(GetWallsCreated(Level), ParentObject);
			else
				targeted = PickNpcWallLine(E.Target ?? ParentObject.Target);
			if (targeted.IsNullOrEmpty())
				return false;
			UseEnergy(1000, $"Physical Mutation {Name}");
			CooldownMyActivatedAbility(ActivatedAbilityID, GetCooldown(Level));
			int wallHp = GetWallHp(Level);
			// the stone call, harmonized with Fissure's carve gate (RULED 2026-08-04: "we should
			// harmonize the stone call as well"): only TRUE GEOLOGY answers. The zone's default
			// wall must itself be natural stone, neighbor borrowing demands the Natural flag,
			// and where nothing natural stands, the shared gray fallback rises.
			string bp = ParentObject.CurrentZone.GetDefaultWall();
			if (bp != null)
			{
				GameObjectBlueprint defaultWall = GameObjectFactory.Factory.GetBlueprintIfExists(bp);
				if (defaultWall == null
					|| defaultWall.GetPartParameter<string>("Cleo_TerraFirma_StoneshaperWallProperties", "MaterialType") != "stone"
					|| !defaultWall.GetPartParameter("Cleo_TerraFirma_StoneshaperWallProperties", "Natural", false))
					bp = null;
			}
			// plan first, place after the rise (VFX pass 2026-08-04, user-confirmed: the
			// Fissure emergence machinery, one connected slab): the borrow stays sticky and
			// order-dependent exactly as before, but no wall exists until the stone has risen
			List<Cell> planCells = new List<Cell>();
			List<string> planBps = new List<string>();
			foreach (Cell cell in targeted)
			{
				if (!cell.IsEmpty())
					continue;
				foreach (Cell c in cell.GetCardinalAdjacentCellsWhere(x => x.HasWall()))
				{
					var wallProps = c.GetFirstWall().GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
					if (Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(wallProps))
					{
						bp = wallProps.ParentObject.Blueprint;
						break;
					}
				}
				planCells.Add(cell);
				planBps.Add(bp ?? "Cleo_TerraFirma_WallCalcify");
			}
			PlayWorldSound("Sounds/Throw/sfx_throwing_stone_large_impact", 1f, Combat: true); // the slam covers the rise
			void PlaceWall(int i)
			{
				GameObject wall = GameObject.Create(planBps[i]);
				wall.AddPart(new Cleo_TerraFirma_CalcifyWallPart(wallHp - wall.GetStatValue("Hitpoints"), wall.GetStatValue("AV")));
				Phase.carryOver(ParentObject, wall);
				planCells[i].AddObject(wall);
				// a fresh painted-family wall carries NO tile until the zone painter runs --
				// it renders as its ASCII glyph through the pump (user flag 2026-08-04).
				// Paint it (and its neighbors' connectivity) the moment it lands.
				planCells[i].CheckPaintWallsAround(wall);
			}
			Cleo.TerraFirma.Scripts.Cleo_TerraFirma_StoneRiseFX.Schedule schedule = null;
			Zone zone = ParentObject.CurrentZone;
			if (!Cleo.TerraFirma.Scripts.Cleo_TerraFirma_StoneRiseFX.HarnessMute && zone != null && zone.IsActive() && ParentObject.CurrentCell?.IsVisible() == true)
				schedule = Cleo.TerraFirma.Scripts.Cleo_TerraFirma_StoneRiseFX.PlayWallRise(planCells, planBps);
			if (schedule != null)
			{
				// Fissure's settle idiom: the world renders live while the stones come up in
				// pick order, and each real wall lands exactly at its own sprite's handoff
				ScreenBuffer buf = ScreenBuffer.GetScrapBuffer1(bLoadFromCurrent: true);
				TextConsole console = Popup._TextConsole;
				float t = 0f;
				float end = schedule.Total + 0.08f;
				int placed = 0;
				while (t < end)
				{
					while (placed < planCells.Count
						&& (!schedule.SettleTimes.TryGetValue(planCells[placed], out float settleAt) || settleAt <= t))
						PlaceWall(placed++);
					XRLCore.Core.RenderBaseToBuffer(buf);
					console.DrawBuffer(buf);
					Thread.Sleep(40);
					t += 0.04f;
				}
				while (placed < planCells.Count)
					PlaceWall(placed++);
			}
			else
			{
				for (int i = 0; i < planCells.Count; i++)
					PlaceWall(i);
			}
			DidX("calcify", "loose matter", ColorAsGoodFor: ParentObject, Source: ParentObject);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CALC", $"cast Lv{Level}: {planCells.Count} wall(s) raised of {targeted.Count} targeted (formula {GetWallsCreated(Level)}), wall HP {GetWallHp(Level)}, blueprint={bp ?? "FALLBACK Cleo_TerraFirma_WallCalcify"}, animated={(schedule != null ? schedule.Total.ToString("0.00") + "s pick-order ripple" : "no")}");
			return base.HandleEvent(E);
		}
		#endregion

		#region Helpers
		public static int GetWallsCreated(int Lv) => (int)(BASE_WALLS_CREATED + Mathf.Floor(Lv / 2f));

		public static int GetWallHp(int Lv) => (int)(BASE_WALL_HP * Mathf.Ceil(Lv / 2f));

		public static int GetCooldown(int Lv) => BASE_COOLDOWN;
		#endregion
	}
}
