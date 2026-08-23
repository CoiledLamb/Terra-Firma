using Cleo.TerraFirma.Scripts;
using XRL.UI;

namespace XRL.World.WorldBuilders
{
	/// <summary>
	/// Warren placement - DOOR A (user lean 2026-08-23: "new saves only probably?
	/// since this will be in a new mod you should start a new save for that").
	/// The exact pig-farm/snapjaw-fort idiom from JoppaWorldBuilder.BuildForts,
	/// decompiled 2026-08-18: pop N mutable DesertCanyon zones, stamp the zone
	/// builder, register the secret + location finder so gossip can point at it.
	/// Runs at OnAfterMutableInit, which fires BEFORE BuildForts inside
	/// BuildMutableEncounters - both draw from the same pool and both remove
	/// their claims, so warrens, farms and forts never share a zone.
	/// Fires once, at embark: existing saves get no warrens by design (ruled
	/// consequence of the door-A lean; the base mod is untouched by this file).
	/// </summary>
	[JoppaWorldBuilderExtension]
	public class Cleo_TerraFirma_WarrenPlacement : IJoppaWorldBuilderExtension
	{
		public const int WARRENS_PER_WORLD = 25;   // ruled 2026-08-18; pig-farm frequency

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
					// secret name is player-facing text: DRAFT, rides the release
					// voice pass (user owns shipped strings)
					string secretID = Builder.AddSecret(zoneID, "a quarry warren",
						new string[3] { "stonecarver", "settlement", "humanoid" }, "Settlements");
					Builder.AddLocationFinder(zoneID, secretID);
					// non-hostile by goal statement -> friendlySettlements
					// (CLAUDE call, delegated plumbing; forts use enemySettlements)
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
			Helpers.VerifyLog("WARREN", $"placement: {placed} warrens sited at embark ({WARRENS_PER_WORLD} asked)");
		}
	}
}
