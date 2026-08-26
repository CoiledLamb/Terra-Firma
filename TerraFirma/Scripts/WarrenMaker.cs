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
		/// <summary>Assigned stage from the scout pass (AddZoneBuilder key/value,
		/// scout-and-quota port 2026-08-25). Under scout placement the warren is
		/// carved AT WORLDGEN and this builder only runs if the engine ever
		/// REBUILDS the zone - same seed, same stage, same warren. Empty = the
		/// wish/legacy path, which rolls its own stage.</summary>
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
				// BLIND-CLAIM (user ruled the port 2026-08-25): a staged claim runs
				// the augment ladder - the zone is made to fit, so the claim cannot
				// lie. The wish/legacy path (no stage) stays pure canyon truth.
				var r = forced.HasValue
					? WarrenCarver.BuildAugmented(Z, WarrenCarver.SeedFor(Z.ZoneID), forced.Value)
					: WarrenCarver.Build(Z, WarrenCarver.SeedFor(Z.ZoneID));
				if (r.Built)
					Helpers.VerifyLog("WARREN", $"builder done: {Z.ZoneID} -> {r.Stage}, {r.Occupancy}, attempt {r.Attempt}, {r.Layout}");
				else
					SettleRumor(Z);   // under blind-claim this should be unreachable; kept as the belt
			}
			catch (System.Exception x)
			{
				Helpers.VerifyLog("WARREN", $"EXCEPTION in builder for {Z.ZoneID}: {x}");
				MetricsManager.LogException("Cleo_TerraFirma_WarrenMaker", x);
				// the belt's belt (demoted from placement to exception-only,
				// 2026-08-25): an exception is the one path left where a claim
				// could stand over nothing, and a silent lie is ruled invalid
				SettleRumor(Z);
			}
			return true;
		}

		/// <summary>SELF-ERASING RUMORS (RULED 2026-08-23 evening, superseding the
		/// same-day prospect fallback: "i'd prefer if the 10 or so failed carves
		/// just stopped existing, so that we end up with 25 somewhat real
		/// warrens"): a refused zone erases its own promise outright - note
		/// deleted whether or not the player holds it, orphaned LocationFinder
		/// widget swept, no physical mark. With the 35-claim overclaim, the world
		/// converges on ~25 real sites and the journal never lied.</summary>
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
