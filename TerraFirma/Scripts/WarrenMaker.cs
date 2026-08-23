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
			var r = WarrenCarver.Build(Z, WarrenCarver.SeedFor(Z.ZoneID));
			if (r.Built)
				Helpers.VerifyLog("WARREN", $"builder done: {Z.ZoneID} -> {r.Stage}, {r.Occupancy}, attempt {r.Attempt}");
			return true;
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
	}
}
