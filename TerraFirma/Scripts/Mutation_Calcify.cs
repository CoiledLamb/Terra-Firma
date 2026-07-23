using System.Collections.Generic;
using UnityEngine;
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
			return "Through intense focus, you may cause solid walls to appear out of thin air.";
		}

		public override string GetLevelText(int Level)
		{
			return "Creates a contiguous line of permanent walls in empty tiles starting adjacent to you\n" +
				"Number created: {{rules|" + GetWallsCreated(Level) + "}}\n" +
				"Hit points per wall: {{rules|" + GetWallHp(Level) + "}}\n" +
				"Created walls have {{rules|0}} AV and {{rules|0}} DV";
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
			if (!base.WantEvent(ID, cascade))
				return ID == PooledEvent<CommandEvent>.ID;
			return true;
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command != COMMAND_ID)
				return base.HandleEvent(E);
			List<Cell> targeted = null;
			if (ParentObject.IsPlayer())
				targeted = PickFieldAdjacent(GetWallsCreated(Level), ParentObject);
			if (targeted.IsNullOrEmpty())
				return false;
			UseEnergy(1000, $"Physical Mutation {Name}");
			CooldownMyActivatedAbility(ActivatedAbilityID, GetCooldown(Level));
			int wallHp = GetWallHp(Level);
			var bp = ParentObject.CurrentZone.GetDefaultWall();
			foreach (Cell cell in targeted)
			{
				if (!cell.IsEmpty())
					continue;
				foreach (Cell c in cell.GetCardinalAdjacentCellsWhere(x => x.HasWall()))
				{
					var wallProps = c.GetFirstWall().GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
					if (wallProps?.MaterialType == "stone")
					{
						bp = wallProps.ParentObject.Blueprint;
						break;
					}
				}
				GameObject wall = GameObject.Create(bp ?? "Cleo_TerraFirma_WallCalcify");
				wall.AddPart(new Cleo_TerraFirma_CalcifyWallPart(wallHp - wall.GetStatValue("Hitpoints"), wall.GetStatValue("AV")));
				Phase.carryOver(ParentObject, wall);
				cell.AddObject(wall);
			}
			PlayWorldSound("Sounds/Throw/sfx_throwing_stone_large_impact", 1f, Combat: true);
			DidX("calcify", "loose matter", ColorAsGoodFor: ParentObject, Source: ParentObject);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CALC", $"cast Lv{Level}: {targeted.Count} cells targeted (formula {GetWallsCreated(Level)}), wall HP {GetWallHp(Level)}, blueprint={bp ?? "FALLBACK Cleo_TerraFirma_WallCalcify"}");
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
