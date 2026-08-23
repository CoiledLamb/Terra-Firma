using Cleo.TerraFirma.Scripts;
using XRL;
using XRL.Messages;
using XRL.Wish;

namespace XRL.World.ZoneBuilders
{
	/// <summary>
	/// Warren v2 zone builder (first port pass 2026-08-23). Stamped per-zone by the
	/// placement extension (WarrenWorldBuilder) at priority 6000 - the same slot
	/// SnapjawStockadeMaker carves its fort in, i.e. after the canyon, the river and
	/// the zone template have all finished. Seed derives from the ZoneID so a zone
	/// carves identically however many times it rebuilds.
	/// ALWAYS returns true: a zone whose band refuses every attempt yields no warren,
	/// and that is a legal outcome by ruling (2026-08-18, "a rumor leading to
	/// something that doesn't pay off is fine" - Qud has abandoned villages). The
	/// refusal logs loudly instead.
	/// </summary>
	public class Cleo_TerraFirma_WarrenMaker
	{
		public bool BuildZone(Zone Z)
		{
			// The belt (round 6): our carve is zone-seeded, so any throw is
			// DETERMINISTIC - the engine's rebuild loop replays it forever and the
			// player gets the "zone isn't building properly" dialog. An exception
			// must therefore degrade to an honest no-warren, never a loop.
			try
			{
				var r = WarrenCarver.Build(Z, WarrenCarver.SeedFor(Z.ZoneID));
				if (r.Built)
					Helpers.VerifyLog("WARREN", $"builder done: {Z.ZoneID} -> {r.Stage}, {r.Occupancy}, attempt {r.Attempt}, {r.Layout}");
				else
					SettleRumor(Z);
			}
			catch (System.Exception x)
			{
				Helpers.VerifyLog("WARREN", $"EXCEPTION in builder for {Z.ZoneID}: {x}");
				MetricsManager.LogException("Cleo_TerraFirma_WarrenMaker", x);
			}
			return true;
		}

		/// <summary>SELF-SETTLING RUMORS (ruled 2026-08-23 "like all three of these
		/// fixes"): a refused zone settles its own promise, zone-locally. Rock to
		/// chip -> the prospect mark lands and the map note REWRITES to the
		/// prospect (DRAFT string; user voice owed at the release pass). Bare
		/// lawn -> an unrevealed note deletes (the rumor never gets told) and the
		/// orphaned LocationFinder widget is swept; a note the player already
		/// holds rewrites instead of vanishing mid-journal.</summary>
		private static void SettleRumor(Zone Z)
		{
			Qud.API.JournalMapNote note = null;
			foreach (var n in Qud.API.JournalAPI.MapNotes)
				if (n.ZoneID == Z.ZoneID && n.Attributes != null && n.Attributes.Contains("stonecarver")) { note = n; break; }
			if (note == null) return;   // wish carves and unnoted zones settle nothing
			int marks = WarrenCarver.ProspectMark(Z, WarrenCarver.SeedFor(Z.ZoneID) + 1);
			if (marks > 0 || note.Revealed)
			{
				note.Text = "an abandoned prospect";   // DRAFT: player-facing, user voice owed
				Helpers.VerifyLog("WARREN", $"rumor settled: rewritten to prospect ({marks} pieces) for {Z.ZoneID}");
			}
			else
			{
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
				Helpers.VerifyLog("WARREN", $"rumor settled: unrevealed note deleted for {Z.ZoneID}");
			}
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
		/// the world map at once, so verifying the 25 does not mean walking the
		/// desert. Keys on the "stonecarver" secret attribute the placement stamps.</summary>
		[WishCommand(Command = "tfwarrens")]
		public static void RevealAll()
		{
			int revealed = 0, total = 0;
			foreach (var note in Qud.API.JournalAPI.MapNotes)
			{
				if (note.Attributes == null || !note.Attributes.Contains("stonecarver")) continue;
				total++;
				if (note.Revealed) continue;
				Qud.API.JournalAPI.RevealMapNote(note, silent: true);
				revealed++;
			}
			Helpers.VerifyLog("WARREN", $"tfwarrens: revealed {revealed} of {total} warren secrets");
			MessageQueue.AddPlayerMessage(total > 0
				? $"tfwarrens: revealed {revealed} warren locations ({total} exist). Check the world map."
				: "tfwarrens: {{R|no warren secrets in this world}} - was it embarked before the placement extension?");
		}
	}
}
