using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using XRL;
using XRL.Messages;
using XRL.UI;
using XRL.Wish;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>Codex PROPOSAL: saved, per-zone measurements for the terrain-led
	/// experiment. Null means registered but unmeasured. Failed claims remain here
	/// after their journal notes disappear; repeat surveys never carve a built zone.
	/// Uses XRLGame.Get/SetStringGameState, serialized by XRLGame.SaveGame.</summary>
	[HasWishCommand]
	public static class WarrenSurvey
	{
		private const string StateKey = "Cleo_TerraFirma_WarrenSurvey_Terrain_v1";

		private static Dictionary<string, WarrenCarver.Result> Read()
		{
			string json = The.Game.GetStringGameState(StateKey);
			return string.IsNullOrEmpty(json)
				? new Dictionary<string, WarrenCarver.Result>()
				: JsonConvert.DeserializeObject<Dictionary<string, WarrenCarver.Result>>(json);
		}

		private static void Write(Dictionary<string, WarrenCarver.Result> records)
			=> The.Game.SetStringGameState(StateKey, JsonConvert.SerializeObject(records));

		public static void Register(string zoneID)
		{
			var records = Read();
			if (records.ContainsKey(zoneID)) return;
			records.Add(zoneID, null);
			Write(records);
		}

		public static void Record(string zoneID, WarrenCarver.Result result)
		{
			var records = Read();
			records[zoneID] = result;
			Write(records);
		}

		internal static string Summarize(Dictionary<string, WarrenCarver.Result> records)
		{
			var measured = records.Values.Where(r => r != null).ToList();
			var built = measured.Where(r => r.Built).ToList();
			var failed = measured.Where(r => !r.Built).ToList();
			var text = new StringBuilder("Warren survey: terrain-led, no rock augmentation\n");
			text.AppendLine($"Registered claims: {records.Count}; measured: {measured.Count}; unmeasured: {records.Count - measured.Count}");
			text.AppendLine($"Built: {built.Count}; failed: {failed.Count}" +
				(measured.Count > 0 ? $"; success: {100.0 * built.Count / measured.Count:0.0}%" : ""));
			if (built.Count > 0)
			{
				text.AppendLine($"Stages: working {built.Count(r => r.Stage == "working")}, camp {built.Count(r => r.Stage == "camp")}, flourishing {built.Count(r => r.Stage == "flourishing")}");
				text.AppendLine($"Layouts: boxes {built.Count(r => r.Layout == "boxes")}, blocks {built.Count(r => r.Layout == "blocks")}");
				text.AppendLine($"Floor cells: mean {built.Average(r => r.Floor):0.0}, min {built.Min(r => r.Floor)}, max {built.Max(r => r.Floor)}");
				text.AppendLine($"Rooms: mean {built.Average(r => r.Pockets):0.0}, min {built.Min(r => r.Pockets)}, max {built.Max(r => r.Pockets)}");
				text.AppendLine($"Aquifer sites: {built.Count(r => r.AquiferPools > 0)}/{built.Count}; pools {built.Sum(r => r.AquiferPools)}, veins {built.Sum(r => r.AquiferVeins)}");
				text.AppendLine("Aquifer gate: 20% by default in XML; terrain and failed sites affect observed frequency.");
			}
			foreach (var group in failed.GroupBy(r => r.Reason.Split('(')[0].Trim()).OrderByDescending(g => g.Count()))
				text.AppendLine($"Failure: {group.Key}: {group.Count()}");
			text.AppendLine("Unmeasured claims are excluded from rates. Existing unrecorded zones are never recarved.");
			text.Append("Use a fresh world for a complete sample. Generation counts do not verify playability.");
			return text.ToString();
		}

		[WishCommand(Command = "tfwarrenstats")]
		public static void Survey()
		{
			if (The.Game == null) return;
			// Import remaining old-world claims without pretending already-built sites
			// have terrain-led results. New worlds register every claim during placement.
			foreach (var note in Qud.API.JournalAPI.MapNotes.ToList())
				if (note.Attributes != null && note.Attributes.Contains("stonecarver")) Register(note.ZoneID);
			var records = Read();
			if (records.Count == 0)
			{
				Popup.Show("tfwarrenstats: no registered claims or stonecarver map notes in this world.");
				return;
			}
			MessageQueue.AddPlayerMessage("tfwarrenstats: building unvisited claims. This may take a moment.");
			int generated = 0, errors = 0;
			var timer = System.Diagnostics.Stopwatch.StartNew();
			foreach (var pair in records.OrderBy(p => p.Key, StringComparer.Ordinal))
			{
				if (pair.Value != null || The.ZoneManager.IsZoneBuilt(pair.Key)) continue;
				try
				{
					The.ZoneManager.GetZone(pair.Key);
					generated++;
				}
				catch (Exception ex)
				{
					errors++;
					MetricsManager.LogException("TerraFirma warren survey: " + pair.Key, ex);
				}
			}
			timer.Stop();
			records = Read();
			foreach (var pair in records.OrderBy(p => p.Key, StringComparer.Ordinal))
				Helpers.VerifyLog("WARREN-STATS", pair.Key + " " + JsonConvert.SerializeObject(pair.Value));
			string report = Summarize(records) + $"\nThis survey: {generated} zones loaded, {errors} load errors, {timer.Elapsed.TotalSeconds:0.0}s. Details in Player.log.";
			Helpers.VerifyLog("WARREN-STATS", report);
			Popup.Show(report);
		}
	}
}
