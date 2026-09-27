using Cleo.TerraFirma.Scripts;
using XRL;
using XRL.Messages;
using XRL.Wish;

namespace XRL.World.ZoneBuilders
{
	/// <summary>
	/// Restored staged warren builder, registered at priority 6000.
	/// Layout is zone-seeded. Failed sites are recorded and their map notes erased.
	/// Always return true so deterministic carve errors cannot cause rebuild loops.
	/// </summary>
	public class Cleo_TerraFirma_WarrenMaker
	{
		/// <summary>Assigned stage from world placement. Staged claims use the
		/// augmentation ladder; empty retains the original unstaged build path.</summary>
		public string Stage;

		public bool BuildZone(Zone Z)
		{
			// The belt (round 6): our carve is zone-seeded, so any throw is
			// DETERMINISTIC - the engine's rebuild loop replays it forever and the
			// player gets the "zone isn't building properly" dialog. An exception
			// must therefore degrade to an honest no-warren, never a loop.
			try
			{
				int? forced = Stage == "flourishing" ? 2 : Stage == "camp" ? 1 : Stage == "working" ? 0 : (int?)null;
				var r = forced.HasValue
					? WarrenCarver.BuildAugmented(Z, WarrenCarver.SeedFor(Z.ZoneID), forced.Value)
					: WarrenCarver.Build(Z, WarrenCarver.SeedFor(Z.ZoneID));
				WarrenSurvey.Record(Z.ZoneID, r);
				if (r.Built)
					Helpers.VerifyLog("WARREN", $"builder done: {Z.ZoneID} -> {r.Stage}, {r.Occupancy}, attempt {r.Attempt}, {r.Layout}");
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
				SettleRumor(Z);
			}
			return true;
		}

		/// <summary>Remove a refused site's map note and orphaned finder.
		/// Other settlement/encounter references remain a separate audit edge.</summary>
		private static void SettleRumor(Zone Z)
		{
			Qud.API.JournalMapNote note = null;
			foreach (var n in Qud.API.JournalAPI.MapNotes)
				if (n.ZoneID == Z.ZoneID && n.Attributes != null && n.Attributes.Contains("stonecarver")) { note = n; break; }
			if (note == null) return;   // wish carves and unnoted zones settle nothing
			Qud.API.JournalAPI.DeleteMapNote(note);
			// the finder builder (priority 5000, ran before us) left a widget at
			// (0,0) pointing at the dead secret - sweep any orphaned finder
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

	/// <summary>Dev wish: run the v2 carve on the CURRENT zone (random seed each
	/// call, so re-wishing rerolls; the world-build path uses the ZoneID seed
	/// instead). Rides the VERIFY_WATCH strip like the rest of the tf* harness.</summary>
	[HasWishCommand]
	public static class WarrenMakerTest
	{
		[WishCommand(Command = "tfwarrenv2")]
		public static void CarveHere()
		{
			Zone Z = The.Player?.CurrentZone;
			if (Z == null) return;
			int seed = XRL.Rules.Stat.Random(1, int.MaxValue - 1);
			var r = WarrenCarver.Build(Z, seed);
			MessageQueue.AddPlayerMessage(r.Built
				? $"tfwarrenv2: {r.Stage} warren, {r.Occupancy}, {r.Floor} floor, {r.Pockets} pockets (seed {r.Seed}, attempt {r.Attempt}). Roles in the log."
				: $"tfwarrenv2: {{{{R|no site}}}} - {r.Reason} (seed {r.Seed}).");
		}

		/// <summary>Testing wish (user ask 2026-08-23): reveal every warren secret on
		/// the world map at once, so verifying does not mean walking the desert.
		/// FORCE-BUILDS each claimed zone first (round 9 refinement, closing the
		/// first-visit hole for the test loop): duds erase their own notes during
		/// the build sweep, so every note this wish reveals is a REAL warren by
		/// construction. Keys on the "stonecarver" secret attribute.</summary>
		[WishCommand(Command = "tfwarrens")]
		public static void RevealAll()
		{
			// snapshot first: the build sweep deletes dud notes out from under us
			var claims = new System.Collections.Generic.List<Qud.API.JournalMapNote>();
			foreach (var note in Qud.API.JournalAPI.MapNotes)
				if (note.Attributes != null && note.Attributes.Contains("stonecarver")) claims.Add(note);
			int built = 0;
			foreach (var note in claims)
			{
				if (!The.ZoneManager.IsZoneBuilt(note.ZoneID))
				{
					The.ZoneManager.GetZone(note.ZoneID);   // builds; a refusal erases its note
					built++;
				}
			}
			int revealed = 0, real = 0;
			foreach (var note in claims)
			{
				if (Qud.API.JournalAPI.GetMapNote(note.ID) == null) continue;   // erased in the sweep
				real++;
				if (note.Revealed) continue;
				Qud.API.JournalAPI.RevealMapNote(note, silent: true);
				revealed++;
			}
			Helpers.VerifyLog("WARREN", $"tfwarrens: {claims.Count} claims, {built} zones built in the sweep, {real} real warrens, {revealed} newly revealed");
			MessageQueue.AddPlayerMessage(claims.Count > 0
				? $"tfwarrens: {real} real warrens on the map ({claims.Count} were claimed; the difference erased itself). Every note is a warren."
				: "tfwarrens: {{R|no warren secrets in this world}} - was it embarked before the placement extension?");
		}
	}
}
