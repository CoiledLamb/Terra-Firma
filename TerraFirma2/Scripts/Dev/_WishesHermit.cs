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
	}
}
