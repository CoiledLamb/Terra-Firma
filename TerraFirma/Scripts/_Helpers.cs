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

		/// <summary>
		/// Save-format sentinel (2026-08-07, the save-compat shim): parts with hand-rolled
		/// Write/Read lead their block with this magic int + a format-version int, so a reader
		/// can tell a current block from any legacy layout at a glance. Legacy blocks fail the
		/// sentinel check, throw before consuming more than 4 bytes, and land in the part's
		/// ReadError, which re-parses or resets and seeks cleanly to the block end -- never
		/// bleeding into a neighboring part's stream. NOT part of the VERIFY_WATCH strip:
		/// this is shipping serialization machinery.
		/// </summary>
		public const int SERIAL_SENTINEL = unchecked((int)0xC1E0E7C4);
	}
}
