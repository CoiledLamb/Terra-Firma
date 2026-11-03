using System;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;

namespace XRL.World.Parts.Mutation
{
	[Serializable]
	public class Cleo_TerraFirma_Iconoclast : BaseMutation
	{
		public const int WAKE_CHANCE = 25;
		public const int WAKE_RADIUS = 3;
		public const string ROLLED = "Cleo_TerraFirma_IconoclastRolled";

		public string ScannedZoneID;

		public override bool CanLevel() => false;

		public override string GetDescription()
		{
			return "You have broken a covenant of the stone, and statues remember your sins.\n\n" +
				"Statues and figurines have a " + WAKE_CHANCE + "% chance to be animate and hostile to you.\n" +
				"-100 reputation with the Quarriers' Guild.";
		}

		public override string GetLevelText(int Level) => "";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == SingletonEvent<EndTurnEvent>.ID;
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (ParentObject.IsPlayer())
			{
				Zone zone = ParentObject.CurrentZone;
				if (zone != null && !zone.IsWorldMap())
				{
					if (zone.ZoneID != ScannedZoneID)
					{
						ScannedZoneID = zone.ZoneID;
						ScanZone(zone);
					}
					RollCarvedStatue();
					WakeNearby();
				}
			}
			return base.HandleEvent(E);
		}

		public static bool IsStatue(GameObject Object) => Object.HasPart<Shrine>() && !Object.IsCreature && !Object.IsTakeable();

		public static bool IsFigurine(GameObject Object) => Object.HasPart<RandomFigurine>() && !Object.IsCreature;

		private void ScanZone(Zone Z)
		{
			int rolled = 0, primed = 0;
			foreach (GameObject o in Z.GetObjects(o => o.GetIntProperty(ROLLED) == 0 && o.CurrentCell != null && (IsStatue(o) || IsFigurine(o))))
			{
				rolled++;
				if (Roll(o))
					primed++;
			}
			ParentObject.Inventory?.ForeachObject(o =>
			{
				if (IsFigurine(o))
					o.SetIntProperty(ROLLED, 1);
			});
			if (rolled > 0)
				Helpers.VerifyLog("ICONOCLAST", $"{Z.ZoneID}: rolled {rolled}, primed {primed}");
		}

		public static bool Roll(GameObject Object, int Chance = WAKE_CHANCE)
		{
			Object.SetIntProperty(ROLLED, 1);
			if (Stat.Random(1, 100) > Chance)
				return false;
			Object.RequirePart<Cleo_TerraFirma_IconoclastStatue>();
			Helpers.VerifyLog("ICONOCLAST", $"primed {Object.Blueprint} ({Object.DisplayNameOnlyStripped}) at {Object.CurrentCell?.X},{Object.CurrentCell?.Y}");
			return true;
		}

		private void RollCarvedStatue()
		{
			if (!ParentObject.TryGetPart<Cleo_TerraFirma_Stoneshaper>(out var shaper))
				return;
			GameObject statue = shaper.CurStatue;
			if (statue == null || statue.GetIntProperty(ROLLED) != 0)
				return;
			statue.SetIntProperty(ROLLED, 1);
			if (Stat.Random(1, 100) > WAKE_CHANCE)
				return;
			statue.RemovePart<Distraction>();
			Cleo_TerraFirma_IconoclastStatue.MakeHostile(statue, ParentObject);
			Cleo_TerraFirma_IconoclastStatue part = statue.RequirePart<Cleo_TerraFirma_IconoclastStatue>();
			part.Dormant = false;
			Helpers.VerifyLog("ICONOCLAST", $"carved statue {statue.ID} turned on its carver");
		}

		private void WakeNearby()
		{
			Cell origin = ParentObject.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (zone == null)
				return;
			for (int x = origin.X - WAKE_RADIUS; x <= origin.X + WAKE_RADIUS; x++)
			{
				for (int y = origin.Y - WAKE_RADIUS; y <= origin.Y + WAKE_RADIUS; y++)
				{
					Cell cell = zone.GetCell(x, y);
					if (cell == null || !cell.IsVisible())
						continue;
					foreach (GameObject o in cell.GetObjectsWithPart("Cleo_TerraFirma_IconoclastStatue"))
					{
						Cleo_TerraFirma_IconoclastStatue part = o.GetPart<Cleo_TerraFirma_IconoclastStatue>();
						if (part != null && part.Dormant)
							part.Wake(ParentObject, "bearer at " + ParentObject.DistanceTo(o));
					}
				}
			}
		}
	}
}
