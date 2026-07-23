using XRL.Messages;
using XRL.UI;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// Contains mod-specific utility functions.
	/// </summary>
	public class Helpers
	{
		/// <summary>
		/// Outputs a message to the log using the provided data. Only functions when debug logging is enabled in the settings.
		/// </summary>
		/// <param name="Msg">The content of the message.</param>
		/// <param name="Context">A short string that will be appended before the message. For best results, limit to 3-4 characters at most.</param>
		public static void DebugLog(string Msg, string Context, string Color = null, bool Capitalize = true)
		{
			if (Options.GetOption("Cleo_TerraFirma_DebugLog").EqualsNoCase("Yes"))
				MessageQueue.AddPlayerMessage($"[{Context}] {Msg}", Color, Capitalize);
		}

		/// <summary>
		/// TEMPORARY verification instrumentation (2026-07-22): writes an "INFO - [TF-VERIFY] ..." line
		/// to Player.log so the checklist watcher can confirm mechanical behavior from outside the game.
		/// Invisible in-game. Strip the call sites (or flip VERIFY_WATCH off) before release.
		/// </summary>
		public const bool VERIFY_WATCH = true;

		public static void VerifyLog(string Context, string Msg)
		{
			if (VERIFY_WATCH)
				MetricsManager.LogInfo($"[TF-VERIFY] [{Context}] {Msg}");
		}
	}
}
