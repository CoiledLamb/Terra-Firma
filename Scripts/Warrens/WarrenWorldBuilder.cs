using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.UI;

namespace XRL.World.WorldBuilders
{
	[JoppaWorldBuilderExtension]
	public class Cleo_TerraFirma_WarrenPlacement : IJoppaWorldBuilderExtension
	{
		public const int WARRENS_PER_WORLD = 25;
		public const int ASSIGN_FL = 9;
		public const int ASSIGN_CAMP = 8;

		public const int DEAL_ABANDONED = 5;
		public const int DEAL_RECLAIMED = 5;

		private readonly List<Cleo_TerraFirma_WarrenSite> sites = new List<Cleo_TerraFirma_WarrenSite>();

		public const string NAME_SPECIAL = "Cleo_TerraFirma_Warren";
		public const string NAME_SUFFIX = ", quarry warren";
		public const string NAME_FALLBACK = "a quarry warren";

		public static string RollName(HashSet<string> Used = null)
		{
			Cleo_TerraFirma_NameTemplateSyntax.Fix();
			for (int i = 0; i < 100; i++)
			{
				string root = XRL.Names.NameMaker.MakeName(Special: NAME_SPECIAL, Type: "Site", FailureOkay: true);
				if (string.IsNullOrEmpty(root)) break;
				if (Used == null || Used.Add(root)) return root + NAME_SUFFIX;
			}
			return NAME_FALLBACK;
		}

		public override void OnAfterMutableInit(JoppaWorldBuilder Builder)
		{
			var stages = new List<string>();
			for (int i = 0; i < ASSIGN_FL; i++) stages.Add("flourishing");
			for (int i = 0; i < ASSIGN_CAMP; i++) stages.Add("camp");
			while (stages.Count < WARRENS_PER_WORLD) stages.Add("working");
			var rng = new Random(The.Game.GetWorldSeed("TFWARRENASSIGN"));
			for (int i = stages.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (stages[i], stages[j]) = (stages[j], stages[i]); }
			var occupancies = new List<string>();
			for (int i = 0; i < DEAL_ABANDONED; i++) occupancies.Add("abandoned");
			for (int i = 0; i < DEAL_RECLAIMED; i++) occupancies.Add("reclaimed");
			while (occupancies.Count < WARRENS_PER_WORLD) occupancies.Add("in use");
			var orng = new Random(The.Game.GetWorldSeed("TFWARRENOCCUPANCY"));
			for (int i = occupancies.Count - 1; i > 0; i--) { int j = orng.Next(i + 1); (occupancies[i], occupancies[j]) = (occupancies[j], occupancies[i]); }
			sites.Clear();
			The.Game.RequireSystem<Cleo_TerraFirma_WarrenMapTiles>();
			int placed = 0;
			var usedNames = new HashSet<string>();
			Builder.AddMutableEncounterToTerrain("DesertCanyon", WARRENS_PER_WORLD,
				delegate(string zoneID, Genkit.Location2D location, XRL.World.Parts.TerrainTravel pTravel)
				{
					string stage = stages[Math.Min(placed, stages.Count - 1)];
					string occupancy = occupancies[Math.Min(placed, occupancies.Count - 1)];
					if (Options.ShowOverlandEncounters && pTravel != null)
					{
						pTravel.ParentObject.Render.RenderString = "Q";
						pTravel.ParentObject.Render.SetForegroundColor('y');
					}
					The.ZoneManager.AddZoneBuilder(zoneID, 6000, "Cleo_TerraFirma_WarrenMaker", "Stage", stage, "Occupancy", occupancy);
					WarrenSurvey.Register(zoneID);
					string name = RollName(usedNames);
					The.ZoneManager.SetZoneName(zoneID, name, Proper: true);
					string secretID = Builder.AddSecret(zoneID, name,
						new string[3] { "stonecarver", "settlement", "humanoid" }, "Settlements");
					Builder.AddLocationFinder(zoneID, secretID);
					pTravel?.AddEncounter(new XRL.World.Parts.EncounterEntry(
						"You hear the ring of hammers on stone nearby. Would you like to investigate?",
						zoneID, "", secretID, Optional: true));
					Builder.worldInfo.friendlySettlements.Add(new GeneratedLocationInfo
					{
						name = name,
						targetZone = zoneID,
						zoneLocation = location,
						secretID = secretID
					});
					Builder.mutableMap.SetMutable(location, 0);
					sites.Add(new Cleo_TerraFirma_WarrenSite
					{
						ZoneID = zoneID, Name = name, SecretID = secretID,
						Stage = stage, Occupancy = occupancy, Location = location
					});
					placed++;
				});
			Cleo_TerraFirma_WarrenStart.Pick(sites);
			Helpers.VerifyLog("WARREN", $"placement (blind-claim): {placed} claims sited " +
				$"(assigned {ASSIGN_FL} fl / {ASSIGN_CAMP} camp / {WARRENS_PER_WORLD - ASSIGN_FL - ASSIGN_CAMP} working; attainment settles at visit; " +
				$"dealt {WARRENS_PER_WORLD - DEAL_ABANDONED - DEAL_RECLAIMED} in use / {DEAL_ABANDONED} abandoned / {DEAL_RECLAIMED} reclaimed)");
		}

		public override void OnAfterBuild(JoppaWorldBuilder Builder)
		{
			try
			{
				Cleo_TerraFirma_WarrenQuests.RollAll(sites);
			}
			catch (Exception x)
			{
				MetricsManager.LogException("Cleo_TerraFirma_WarrenQuests", x);
			}
		}
	}
}
