using Cleo.TerraFirma.Scripts;
using XRL;
using XRL.Messages;
using XRL.Wish;

namespace XRL.World.ZoneBuilders
{
	public class Cleo_TerraFirma_WarrenMaker
	{
		public string Stage;

		public string Occupancy;

		public const string MUSIC_TRACK = "Music/Cleo_TerraFirma_WarrenSong";

		public bool BuildZone(Zone Z)
		{
			try
			{
				int? forced = Stage == "flourishing" ? 2 : Stage == "camp" ? 1 : Stage == "working" ? 0 : (int?)null;
				WarrenCarver.Result r;
				WarrenCarver.ForcedOccupancy = string.IsNullOrEmpty(Occupancy) ? null : Occupancy;
				try
				{
					r = forced.HasValue
						? WarrenCarver.BuildAugmented(Z, WarrenCarver.SeedFor(Z.ZoneID), forced.Value)
						: WarrenCarver.Build(Z, WarrenCarver.SeedFor(Z.ZoneID));
				}
				finally
				{
					WarrenCarver.ForcedOccupancy = null;
				}
				WarrenSurvey.Record(Z.ZoneID, r);
				if (r.Built)
				{
					Z.SetMusic(MUSIC_TRACK);
					Helpers.VerifyLog("WARREN", $"builder done: {Z.ZoneID} -> {r.Stage}, {r.Occupancy}, attempt {r.Attempt}, {r.Layout}");
				}
				else
					SettleRumor(Z);
			}
			catch (System.Exception x)
			{
				Helpers.VerifyLog("WARREN", $"EXCEPTION in builder for {Z.ZoneID}: {x}");
				MetricsManager.LogException("Cleo_TerraFirma_WarrenMaker", x);
				WarrenSurvey.Record(Z.ZoneID, new WarrenCarver.Result
				{
					Reason = "exception: " + x.GetType().Name,
					Seed = WarrenCarver.SeedFor(Z.ZoneID)
				});
				if (Z.HasZoneProperty(WarrenCarver.APPLIED_PROPERTY))
					Helpers.VerifyLog("WARREN", $"note kept: {Z.ZoneID} threw after the carve was applied");
				else
					SettleRumor(Z);
			}
			return true;
		}

		private static void SettleRumor(Zone Z)
		{
			Qud.API.JournalMapNote note = null;
			foreach (var n in Qud.API.JournalAPI.MapNotes)
				if (n.ZoneID == Z.ZoneID && n.Attributes != null && n.Attributes.Contains("stonecarver")) { note = n; break; }
			if (note == null) return;
			if (The.Game.GetIntGameState(XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuests.TARGET_STATE + Z.ZoneID) > 0)
			{
				Helpers.VerifyLog("WARREN", $"rumor kept: {Z.ZoneID} was refused but a warren quest targets it");
				return;
			}
			XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenMapTiles.Forget(note);
			Qud.API.JournalAPI.DeleteMapNote(note);
			Cell c00 = Z.GetCell(0, 0);
			if (c00 != null)
			{
				foreach (GameObject o in new System.Collections.Generic.List<GameObject>(c00.GetObjectsWithPart("LocationFinder")))
				{
					var lf = o.GetPart<XRL.World.Parts.LocationFinder>();
					if (lf != null && Qud.API.JournalAPI.GetMapNote(lf.ID) == null)
						o.Obliterate();
				}
			}
			Helpers.VerifyLog("WARREN", $"rumor erased: {Z.ZoneID} stops existing as a destination");
		}
	}

}
