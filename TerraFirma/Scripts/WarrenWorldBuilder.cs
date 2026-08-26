using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.UI;

namespace XRL.World.WorldBuilders
{
	/// <summary>
	/// BLIND-CLAIM warren placement (user ruled 2026-08-25, adopting the outside
	/// contributor's model: "pick the zones at random during worldgen, don't check
	/// if they have enough space, when the zone is found by the player, force the
	/// zone into a state that works for the procgen"). Replaces scout-and-quota,
	/// which lives in git history: zones are claimed blind at pig-farm frequency
	/// (the exact BuildForts idiom), each stamped with an assigned stage, and the
	/// carver's augment ladder makes every claim stand at visit - a claim cannot
	/// lie because the zone is made to fit. Worldgen cost: ~zero (scout ran
	/// 12-22s of zone builds; the ladder's cost moved to zone visits, where it
	/// hides inside normal zone-build time).
	///
	/// Stage assignment is OVER-ASSIGNMENT against sim-measured attainment
	/// (fl ~57%, camp ~87% at stage; misses stand one step down): assign ~9
	/// flourishing for ~5 expected, the ruled mix in expectation with binomial
	/// per-world spread - the sameness-mitigation the rolled quota bought, now
	/// free. Dials CLAUDE's, tunable after real-Hills rates land in the log.
	/// </summary>
	[JoppaWorldBuilderExtension]
	public class Cleo_TerraFirma_WarrenPlacement : IJoppaWorldBuilderExtension
	{
		public const int WARRENS_PER_WORLD = 25;   // ruled 2026-08-18; pig-farm frequency
		public const int ASSIGN_FL = 9;            // CLAUDE dial: ~5 expected at sim attainment
		public const int ASSIGN_CAMP = 8;          // CLAUDE dial: ~9 expected with fl spill

		public override void OnAfterMutableInit(JoppaWorldBuilder Builder)
		{
			// assigned stages, shuffled by a derived key so the ambient stream is
			// untouched and a world's assignment map is stable
			var stages = new List<string>();
			for (int i = 0; i < ASSIGN_FL; i++) stages.Add("flourishing");
			for (int i = 0; i < ASSIGN_CAMP; i++) stages.Add("camp");
			while (stages.Count < WARRENS_PER_WORLD) stages.Add("working");
			var rng = new Random(The.Game.GetWorldSeed("TFWARRENASSIGN"));
			for (int i = stages.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (stages[i], stages[j]) = (stages[j], stages[i]); }
			int placed = 0;
			Builder.AddMutableEncounterToTerrain("DesertCanyon", WARRENS_PER_WORLD,
				delegate(string zoneID, Genkit.Location2D location, XRL.World.Parts.TerrainTravel pTravel)
				{
					string stage = stages[Math.Min(placed, stages.Count - 1)];
					if (Options.ShowOverlandEncounters && pTravel != null)
					{
						pTravel.ParentObject.Render.RenderString = "Q";
						pTravel.ParentObject.Render.SetForegroundColor('y');
					}
					The.ZoneManager.AddZoneBuilder(zoneID, 6000, "Cleo_TerraFirma_WarrenMaker", "Stage", stage);
					// secret name is player-facing text: DRAFT, rides the release
					// voice pass (user owns shipped strings)
					string secretID = Builder.AddSecret(zoneID, "a quarry warren",
						new string[3] { "stonecarver", "settlement", "humanoid" }, "Settlements");
					Builder.AddLocationFinder(zoneID, secretID);
					// PASS-OVER ENCOUNTER (user ask 2026-08-23, the village/lair
					// idiom). LINE IS DRAFT, user voice owed. The first-visit dud is
					// structurally gone: the ladder guarantees the visited zone holds
					// a warren, so the popup can no longer lie.
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
			Helpers.VerifyLog("WARREN", $"placement (blind-claim): {placed} claims sited " +
				$"(assigned {ASSIGN_FL} fl / {ASSIGN_CAMP} camp / {WARRENS_PER_WORLD - ASSIGN_FL - ASSIGN_CAMP} working; attainment settles at visit)");
		}
	}
}
