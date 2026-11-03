using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_OverlandPassage : IPart
	{
		public const string BLOCKED = "ObjectEnteringCellBlockedBySolid";

		public string Terrains = "TerrainMountains,TerrainMoonStair";

		public string Except = "TerrainEynRoj";

		public int SpeedPenalty = 75;

		public override bool SameAs(IPart p)
		{
			Cleo_TerraFirma_OverlandPassage other = p as Cleo_TerraFirma_OverlandPassage;
			return other != null && other.Terrains == Terrains && other.Except == Except && base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ImplantedEvent.ID
				|| ID == UnimplantedEvent.ID
				|| ID == TravelSpeedEvent.ID;
		}

		public override bool HandleEvent(ImplantedEvent E)
		{
			E.Implantee.RegisterPartEvent(this, BLOCKED);
			E.Implantee.RegisterEvent(this, TravelSpeedEvent.ID, Serialize: true);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnimplantedEvent E)
		{
			E.Implantee.UnregisterPartEvent(this, BLOCKED);
			E.Implantee.UnregisterEvent(this, TravelSpeedEvent.ID);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(TravelSpeedEvent E)
		{
			GameObject who = ParentObject?.Implantee;
			if (who != null && E.Actor == who && SlowsIn(who.CurrentCell, who.OnWorldMap()))
				E.PercentageBonus -= SpeedPenalty;
			return base.HandleEvent(E);
		}

		public bool SlowsIn(Cell C, bool WorldMap) => WorldMap && C != null && C.HasObject(Passes);

		public override bool FireEvent(Event E)
		{
			if (E.ID == BLOCKED)
			{
				GameObject who = ParentObject?.Implantee;
				if (who != null && who.OnWorldMap() && Passes(E.GetGameObjectParameter("Object")))
					return false;
			}
			return base.FireEvent(E);
		}

		public bool Passes(GameObject Terrain)
		{
			if (Terrain == null)
				return false;
			GameObjectBlueprint bp = Terrain.GetBlueprint();
			foreach (string x in Except.Split(','))
			{
				string name = x.Trim();
				if (Terrain.Blueprint == name || (bp != null && bp.InheritsFrom(name)))
					return false;
			}
			foreach (string t in Terrains.Split(','))
			{
				string name = t.Trim();
				if (Terrain.Blueprint == name || (bp != null && bp.InheritsFrom(name)))
					return true;
			}
			return false;
		}
	}
}
