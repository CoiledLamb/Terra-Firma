using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_SilicateFieldResonator : IPart
	{
		public int PerWall = 5;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == SingletonEvent<GetAvailableComputePowerEvent>.ID;
		}

		public override bool HandleEvent(GetAvailableComputePowerEvent E)
		{
			if (E.Actor != null && E.Actor == ParentObject.Implantee)
				E.Amount += PerWall * CountWalls(E.Actor.CurrentCell);
			return base.HandleEvent(E);
		}

		public static int CountWalls(Cell Center)
		{
			if (Center == null || Center.OnWorldMap())
				return 0;
			int n = 0;
			foreach (Cell c in Center.GetLocalAdjacentCells())
			{
				if (c.HasObject(IsResonantWall))
					n++;
			}
			return n;
		}

		public static bool IsResonantWall(GameObject Wall)
		{
			if (Wall == null || !Wall.IsWall())
				return false;
			if (Wall.HasPart<HologramMaterial>() || Wall.HasPart<Forcefield>())
				return false;
			return Wall.HasPart<Cleo_TerraFirma_CalcifyWallPart>()
				|| Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(Wall.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>());
		}
	}
}
