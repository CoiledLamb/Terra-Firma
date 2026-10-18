using System.Collections.Generic;
using XRL;
using XRL.Messages;
using XRL.Wish;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>Stone-kissed hermit spawn check (creatures branch 2026-10-18): rolls the live
	/// mountain table the way a fresh mountain zone does, so the merge can be seen working
	/// without walking dozens of parasangs.</summary>
	[HasWishCommand]
	public class HermitWishes
	{
		/// <summary>Roll MountainsZoneGlobals-Reachable 1000 times and count stone-kissed hermits.</summary>
		[WishCommand(Command = "tfhermitroll")]
		public static void Roll()
		{
			const string table = "MountainsZoneGlobals-Reachable";
			const string hermit = "Cleo_TerraFirma_StoneKissedHermit";
			int zones = 0, goats = 0;
			for (int i = 0; i < 1000; i++)
			{
				bool seen = false;
				foreach (PopulationResult result in PopulationManager.Generate(table))
				{
					if (result.Blueprint == hermit)
						seen = true;
					else if (result.Blueprint == "Goat")
						goats++;
				}
				if (seen)
					zones++;
			}
			string line = $"tfhermitroll: hermit in {zones}/1000 rolls of {table} (goat rolls {goats}, a base control).";
			MessageQueue.AddPlayerMessage(line, Capitalize: false);
			Helpers.VerifyLog("HERMIT", line);
		}

		/// <summary>What this screen is and what the mountain table put in it: terrain, and counts
		/// of stone-kissed hermits, goats, cannibals and dogs (the table's own roster).</summary>
		[WishCommand(Command = "tfhermitzone")]
		public static void Zone()
		{
			XRL.World.Zone zone = The.Player.CurrentZone;
			int hermits = 0, goats = 0, cannibals = 0, dogs = 0;
			foreach (XRL.World.GameObject obj in zone.GetObjects())
			{
				switch (obj.Blueprint)
				{
					case "Cleo_TerraFirma_StoneKissedHermit": hermits++; break;
					case "Goat": goats++; break;
					case "Cannibal": cannibals++; break;
					case "Dog": dogs++; break;
				}
			}
			string line = $"tfhermitzone: {zone.ZoneID} terrain '{zone.GetTerrainDisplayName()}' region '{zone.GetTerrainRegion()}': hermits {hermits}, goats {goats}, cannibals {cannibals}, dogs {dogs}.";
			MessageQueue.AddPlayerMessage(line, Capitalize: false);
			Helpers.VerifyLog("HERMIT", line);
		}
	}
}
