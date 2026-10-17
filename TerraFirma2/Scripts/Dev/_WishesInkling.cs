using XRL;
using XRL.Messages;
using XRL.Wish;
using XRL.World;
using XRL.World.Parts;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>Inkling staging wishes (creatures branch 2026-10-17). Spawn one with the base
	/// blueprint wish (Cleo_TerraFirma_Inkling); these skip the parts that wait on luck or time.</summary>
	[HasWishCommand]
	public class InklingWishes
	{
		/// <summary>The nearest inkling in the zone rides on you, skipping the "likes you" gate.</summary>
		[WishCommand(Command = "tfinkride")]
		public static void Ride()
		{
			GameObject ply = The.Player;
			GameObject best = null;
			int bestDist = int.MaxValue;
			foreach (GameObject obj in ply.CurrentZone.GetObjectsWithPart("Cleo_TerraFirma_Inkling"))
			{
				int d = ply.DistanceTo(obj);
				if (d < bestDist)
				{
					best = obj;
					bestDist = d;
				}
			}
			bool ok = best?.GetPart<Cleo_TerraFirma_Inkling>().RideOn(ply) ?? false;
			MessageQueue.AddPlayerMessage(ok ? "tfinkride: riding." : "tfinkride: no inkling in the zone, or no room for a tattoo.", Capitalize: false);
		}

		/// <summary>The riding inkling eats now instead of waiting on its hunger timer.</summary>
		[WishCommand(Command = "tfinkfeed")]
		public static void Feed()
		{
			Cleo_TerraFirma_InklingTattoo rider = The.Player.GetPart<Cleo_TerraFirma_InklingTattoo>();
			if (rider == null || !rider.Feed())
				MessageQueue.AddPlayerMessage("tfinkfeed: no inkling riding, or no other tattoos to eat.", Capitalize: false);
		}

		/// <summary>The riding inkling hops off, unfrightened.</summary>
		[WishCommand(Command = "tfinkoff")]
		public static void Off()
		{
			Cleo_TerraFirma_InklingTattoo rider = The.Player.GetPart<Cleo_TerraFirma_InklingTattoo>();
			if (rider == null || !rider.HopOff(Frightened: false, Silent: false))
				MessageQueue.AddPlayerMessage("tfinkoff: no inkling riding, or no open cell beside you.", Capitalize: false);
		}
	}
}
