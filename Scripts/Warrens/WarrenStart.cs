using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL;
using XRL.World;
using XRL.World.WorldBuilders;

namespace XRL.World.WorldBuilders
{
	public static class Cleo_TerraFirma_WarrenStart
	{
		public const string WARREN_START = "Cleo_TerraFirma_WarrenStart";
		public const string START_LOCATION = "Cleo_TerraFirma_WarrenStartLocation";
		public const string START_ZONE = "Cleo_TerraFirma_WarrenStartZone";
		public const string START_NAME = "Cleo_TerraFirma_WarrenStartName";
		public const string LOCATION_ID = "Cleo_TerraFirma_QuarryWarren";
		public const string TAUGHT_SKILL = "Tactics_Throwing";
		public const string SURVEYOR = "Cleo_TerraFirma_WarrenSurveyor";
		public const string QUEST_DONE = "Cleo_TerraFirma_WarrenStartQuestDone";
		public const string OVERSEER = "Cleo_TerraFirma_Overseer";

		public const int JOPPA_X = 34, JOPPA_Y = 67;
		public const int WORKING_PENALTY = 30;

		public static bool Wanted => The.Game?.GetStringGameState(WARREN_START) == "Yes";

		public static void Pick(List<Cleo_TerraFirma_WarrenSite> sites)
		{
			if (!Wanted) return;
			Cleo_TerraFirma_WarrenSite best = null;
			int bestScore = int.MaxValue;
			foreach (var s in sites)
			{
				if (s == null || s.Location == null || s.Occupancy != "in use") continue;
				int score = Math.Abs(s.Location.X - JOPPA_X) + Math.Abs(s.Location.Y - JOPPA_Y)
					+ (s.Stage == "working" ? WORKING_PENALTY : 0);
				if (score < bestScore) { bestScore = score; best = s; }
			}
			if (best == null)
			{
				Helpers.VerifyLog("WARRENSTART", "no in-use warren to start at; base falls back to Joppa's cell");
				MetricsManager.LogError("TerraFirma: warren start picked but no in-use warren was placed");
				return;
			}
			The.Game.SetStringGameState(START_ZONE, best.ZoneID);
			The.Game.SetStringGameState(START_LOCATION, best.ZoneID + "@37,22");
			string suffix = Cleo_TerraFirma_WarrenPlacement.NAME_SUFFIX;
			string bare = best.Name != null && best.Name.EndsWith(suffix) ? best.Name.Substring(0, best.Name.Length - suffix.Length) : best.Name;
			The.Game.SetStringGameState(START_NAME, bare);
			The.Game.SetStringGameState("villageZeroName", bare);
			The.ZoneManager.AddZonePostBuilder(best.ZoneID, "IsCheckpoint", "Key", best.ZoneID);
			Qud.API.JournalMapNote note = best.SecretID == null ? null : Qud.API.JournalAPI.GetMapNote(best.SecretID);
			if (note != null)
			{
				if (The.Player == null)
				{
					if (!note.Revealed)
					{
						note.LearnedFrom = "Exploration";
						note.Revealed = true;
						SecretVisibilityChangedEvent.Send(note);
					}
				}
				else
					Qud.API.JournalAPI.RevealMapNote(note, silent: true, LearnedFrom: "Exploration");
				note.Tradable = false;
			}
			Helpers.VerifyLog("WARRENSTART", $"start warren: {best.Name} ({best.ZoneID}), stage {best.Stage}, score {bestScore}");
		}

		public static void NoteQuestTurnedIn(string GiverZoneID)
		{
			string start = The.Game?.GetStringGameState(START_ZONE, null);
			if (string.IsNullOrEmpty(start) || GiverZoneID != start) return;
			The.Game.SetIntGameState(QUEST_DONE, 1);
			Helpers.VerifyLog("WARRENSTART", $"start warren quest turned in at {GiverZoneID}: the surveyor's main quest is open");
		}

		public static bool SurveyorOpen()
		{
			if (The.Game == null) return true;
			if (string.IsNullOrEmpty(The.Game.GetStringGameState(START_ZONE, null))) return true;
			return The.Game.GetIntGameState(QUEST_DONE) == 1;
		}
	}
}

namespace XRL.World.Conversations
{
	[HasConversationDelegate]
	public static class Cleo_TerraFirma_WarrenStartDelegates
	{
		[ConversationDelegate(Key = "Cleo_TerraFirma_IfWarrenStartQuestDone", InverseKey = "Cleo_TerraFirma_IfNotWarrenStartQuestDone")]
		public static bool IfWarrenStartQuestDone(DelegateContext Context) => XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenStart.SurveyorOpen();
	}
}

namespace XRL.CharacterBuilds
{
	public class Cleo_TerraFirma_WarrenStartEmbark : AbstractEmbarkBuilderModule
	{
		public override bool shouldBeEnabled() => false;
		public override bool IncludeInBuildCodes() => false;

		public override void Init()
		{
#if VERSION_1_0
			try
			{
				if (!EmbarkBuilderConfiguration.modules.TryGetValue("XRL.CharacterBuilds.Qud.QudChooseStartingLocationModule", out var m)
					|| !(m is XRL.CharacterBuilds.Qud.QudChooseStartingLocationModule loc))
				{
					MetricsManager.LogError("TerraFirma: no starting location module; the quarry warren start is missing");
					return;
				}
				if (loc.startingLocations.ContainsKey(Cleo_TerraFirma_WarrenStart.LOCATION_ID)) return;
				var d = new XRL.UI.Framework.StartingLocationData
				{
					Id = Cleo_TerraFirma_WarrenStart.LOCATION_ID,
					Name = "{{w|quarry}}\nwarren",
					Location = "StringGameState:" + Cleo_TerraFirma_WarrenStart.START_LOCATION,
					Description = DESCRIPTION
				};
				d.stringGameStates[Cleo_TerraFirma_WarrenStart.WARREN_START] = "Yes";
				d.stringGameStates["RuinedJoppa"] = "Yes";
				d.reputations.Add(new XRL.UI.Framework.StartingLocationReputation { Faction = "Cleo_TerraFirma_Quarriers", Modifier = REPUTATION });
				foreach (var g in GRID)
					d.grid[g[0]] = new XRL.UI.Framework.StartingLocationGridElement { Tile = g[1], Foreground = g[2], Detail = g[3], Background = "k" };
				loc.startingLocations.Add(d.Id, d);
			}
			catch (Exception x)
			{
				MetricsManager.LogException("Cleo_TerraFirma_WarrenStartEmbark", x);
			}
#endif
		}

		public const string DESCRIPTION =
			"{{c|\u2219}} Random quarry warren in the desert canyons.\n" +
			"{{c|\u2219}} Warren teaches Deft Throwing.\n" +
			"{{c|\u2219}} +100 reputation with the Quarriers' Guild.";
		public const int REPUTATION = 100;

		public static readonly string[][] GRID =
		{
			new[] { "00", "Terrain/sw_hills2.bmp", "w", "w" },
			new[] { "10", "Terrain/sw_canyon.bmp", "w", "w" },
			new[] { "20", "Terrain/sw_canyon.bmp", "w", "w" },
			new[] { "30", "Terrain/sw_hills2.bmp", "y", "y" },
			new[] { "40", "Terrain/sw_hills_2.bmp", "y", "y" },
			new[] { "01", "Terrain/sw_marsh_worldmap_3.bmp", "g", "g" },
			new[] { "11", "Terrain/sw_hills2.bmp", "w", "w" },
			new[] { "21", "Terrain/tf_quarry_embark.png", "w", "r" },
			new[] { "31", "Terrain/sw_canyon.bmp", "w", "w" },
			new[] { "41", "Terrain/sw_hills.bmp", "y", "y" },
			new[] { "02", "Terrain/sw_marsh_worldmap_1.bmp", "g", "b" },
			new[] { "12", "Terrain/sw_marsh_worldmap_4.bmp", "g", "g" },
			new[] { "22", "Terrain/sw_canyon.bmp", "w", "w" },
			new[] { "32", "Terrain/sw_canyon.bmp", "w", "w" },
			new[] { "42", "Terrain/sw_hills2.bmp", "w", "w" },
		};
	}
}

#if !VERSION_1_0
namespace XRL.Modules.Origins
{
	public class Cleo_TerraFirma_RandomWarren : OriginModule
	{
		public override bool ModifyLocation(XRL.World.GlobalLocation Location)
		{
			string zone = The.Game.GetStringGameState(Cleo_TerraFirma_WarrenStart.START_ZONE, null);
			if (string.IsNullOrEmpty(zone))
			{
				Helpers.VerifyLog("WARRENSTART", "lang origin: no start zone picked; base falls back to Joppa");
				return true;
			}
			Location.ZoneID = zone;
			return false;
		}

		public override bool ModifyOpening(XRL.World.Text.TextBuilder Opening, XRL.World.Text.ReplaceBuilder Replace)
		{
			string name = The.Game.GetStringGameState(Cleo_TerraFirma_WarrenStart.START_NAME, null);
			if (!string.IsNullOrEmpty(name)) Replace.AddArgument("village", name);
			return base.ModifyOpening(Opening, Replace);
		}
	}
}
#endif

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_WarrenArrival : IPart
	{
		public const int SEARCH_RADIUS = 6;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == SingletonEvent<BeforeTakeActionEvent>.ID;
		}

		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			try { Arrive(); }
			catch (Exception x) { MetricsManager.LogException("Cleo_TerraFirma_WarrenArrival", x); }
			ParentObject.RemovePart(this);
			return base.HandleEvent(E);
		}

		public GameObject Arrive()
		{
			GameObject player = ParentObject;
			Cell here = player.CurrentCell;
			Zone Z = here?.ParentZone;
			string want = The.Game.GetStringGameState(Cleo_TerraFirma_WarrenStart.START_ZONE);
			if (Z == null || string.IsNullOrEmpty(want) || Z.ZoneID != want)
			{
				Helpers.VerifyLog("WARRENSTART", $"arrival skipped: player in {Z?.ZoneID}, start zone '{want}'");
				return null;
			}
			GameObject overseer = null;
			foreach (GameObject o in Z.GetObjectsWithPart("Brain"))
				if (o.Blueprint == Cleo_TerraFirma_WarrenStart.OVERSEER) { overseer = o; break; }
			overseer?.SetStringProperty("WaterRitual_Skill", Cleo_TerraFirma_WarrenStart.TAUGHT_SKILL);
			Cell anchor = overseer?.CurrentCell ?? here;
			var open = OpenCellsNear(anchor, player);
			string note = overseer != null ? "surveyor by the overseer" : "no overseer, surveyor by the start cell";
			Cell seat = open.Count > 0 ? open[0] : null;
			GameObject surveyor = seat != null ? GameObject.Create(Cleo_TerraFirma_WarrenStart.SURVEYOR) : null;
			if (surveyor != null) seat.AddObject(surveyor);
			Helpers.VerifyLog("WARRENSTART", $"arrived in {Z.ZoneID} {note}; player at {player.CurrentCell?.X},{player.CurrentCell?.Y}; surveyor {(surveyor != null ? $"at {seat.X},{seat.Y}" : "NOT placed")}");
			return surveyor;
		}

		private static List<Cell> OpenCellsNear(Cell anchor, GameObject player)
		{
			var result = new List<Cell>();
			var seen = new HashSet<Cell> { anchor };
			var frontier = new List<Cell> { anchor };
			for (int d = 0; d < SEARCH_RADIUS && result.Count < 2 && frontier.Count > 0; d++)
			{
				var next = new List<Cell>();
				foreach (Cell c in frontier)
					foreach (Cell n in c.GetLocalAdjacentCells())
					{
						if (n == null || !seen.Add(n) || n.IsSolid()) continue;
						next.Add(n);
						if (n.IsEmpty() && !n.HasObject(player) && result.Count < 2) result.Add(n);
					}
				frontier = next;
			}
			return result;
		}
	}
}

namespace Cleo.TerraFirma.Scripts
{
	[XRL.PlayerMutator]
	public class Cleo_TerraFirma_WarrenArrivalInstaller : XRL.IPlayerMutator
	{
		public void mutate(GameObject player)
		{
			if (player == null || !Cleo_TerraFirma_WarrenStart.Wanted) return;
			player.RequirePart<XRL.World.Parts.Cleo_TerraFirma_WarrenArrival>();
			Helpers.VerifyLog("WARRENSTART", $"arrival part attached; start location '{The.Game.GetStringGameState(Cleo_TerraFirma_WarrenStart.START_LOCATION)}'");
		}
	}
}
