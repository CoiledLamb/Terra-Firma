using Cleo.TerraFirma.Scripts;
using XRL.UI;

namespace XRL.World.WorldBuilders
{
	/// <summary>
	/// Codex PROPOSAL: claim locations without assigning stages or scouting zones.
	/// Terrain determines the finished stage when each zone builds. The survey
	/// retains all claims, including failures whose map notes are later erased.
	/// </summary>
	[JoppaWorldBuilderExtension]
	public class Cleo_TerraFirma_WarrenPlacement : IJoppaWorldBuilderExtension
	{
		public const int WARRENS_PER_WORLD = 25;   // claims, not a guaranteed success count

		public override void OnAfterMutableInit(JoppaWorldBuilder Builder)
		{
			int placed = 0;
			Builder.AddMutableEncounterToTerrain("DesertCanyon", WARRENS_PER_WORLD,
				delegate(string zoneID, Genkit.Location2D location, XRL.World.Parts.TerrainTravel pTravel)
				{
					if (Options.ShowOverlandEncounters && pTravel != null)
					{
						pTravel.ParentObject.Render.RenderString = "Q";
						pTravel.ParentObject.Render.SetForegroundColor('y');
					}
					The.ZoneManager.AddZoneBuilder(zoneID, 6000, "Cleo_TerraFirma_WarrenMaker");
					WarrenSurvey.Register(zoneID);
					// secret name is player-facing text: DRAFT, rides the release
					// voice pass (user owns shipped strings)
					string secretID = Builder.AddSecret(zoneID, "a quarry warren",
						new string[3] { "stonecarver", "settlement", "humanoid" }, "Settlements");
					Builder.AddLocationFinder(zoneID, secretID);
					// Existing encounter copy; a rejected carve erases its map note.
					pTravel?.AddEncounter(new XRL.World.Parts.EncounterEntry(
						"You hear the ring of hammers on stone nearby. Would you like to investigate?",
						zoneID, "", secretID, Optional: true));
					// non-hostile by goal statement -> friendlySettlements
					Builder.worldInfo.friendlySettlements.Add(new GeneratedLocationInfo
					{
						name = "a quarry warren",
						targetZone = zoneID,
						zoneLocation = location,
						secretID = secretID
					});
					Builder.mutableMap.SetMutable(location, 0);
					placed++;
				});
			Helpers.VerifyLog("WARREN", $"placement (terrain-led): {placed} claims sited; stages and success settle at build");
		}
	}
}
