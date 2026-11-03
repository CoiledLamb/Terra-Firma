using System;
using System.Collections.Generic;
using System.Linq;
using Cleo.TerraFirma.Scripts;
using ConsoleLib.Console;
using HistoryKit;
using Qud.API;
using XRL;
using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World.Conversations;
using XRL.World.Effects;
using XRL.World.Parts;
using XRL.World.ZoneBuilders;

namespace XRL.World.WorldBuilders
{
	[Serializable]
	public class Cleo_TerraFirma_WarrenShapeQuestTemplate : BaseDynamicQuestTemplate
	{
		public override void init(DynamicQuestContext context)
		{
			var ctx = context as Cleo_TerraFirma_WarrenQuestContext;
			if (ctx == null)
				return;
			ctx.QuestID ??= Guid.NewGuid().ToString();
			string giverZone = ctx.getQuestGiverZone();
			if (ctx.Verb == "deliver")
			{
				GameObject item = ctx.getQuestDeliveryItem();
				item.SetStringProperty("HasPregeneratedName", "yes");
				item.Render.DisplayName = ctx.getQuestItemNameMutation(item.Render.DisplayName);
				ctx.ItemID = zoneManager.CacheObject(item);
				zoneManager.AddZoneBuilder(ctx.TargetZoneID, 6000, "Cleo_TerraFirma_WarrenQuestReceiver", "questContext", ctx);
			}
			else if (ctx.Verb == "carve")
			{
				GameObject muse = Cleo_TerraFirma_WarrenQuestText.CarveMuse(ctx);
				ctx.MuseID = zoneManager.CacheObject(muse);
				GameObject spike = GameObject.Create(Cleo_TerraFirma_PreordainedSpike.BLUEPRINT);
				var part = spike.RequirePart<Cleo_TerraFirma_PreordainedSpike>();
				part.QuestID = ctx.QuestID;
				part.TargetZoneID = ctx.TargetZoneID;
				part.MuseID = ctx.MuseID;
				spike.SetIntProperty("norestock", 1);
				spike.SetStringProperty("NeverStack", "1");
				spike.SetIntProperty("QuestItem", 1);
				ctx.ItemID = zoneManager.CacheObject(spike);
			}
			else if (ctx.Verb == "clearout")
			{
				GameObject leader = GameObject.Create(Cleo_TerraFirma_WarrenQuests.SQUATTER);
				try
				{
					leader = HeroMaker.MakeHero(leader) ?? leader;
				}
				catch (Exception x)
				{
					MetricsManager.LogException("Cleo_TerraFirma_WarrenQuests clear-out leader", x);
				}
				leader.AddPart(new Cleo_TerraFirma_WarrenQuestFoe { QuestID = ctx.QuestID, Leader = true });
				ctx.ItemID = zoneManager.CacheObject(leader);
				zoneManager.AddZoneBuilder(ctx.TargetZoneID, 6000, "Cleo_TerraFirma_WarrenQuestPlacer", "deliveryItemID", ctx.ItemID, "Mode", "leader", "Owner", ctx.QuestID);
			}
			else if (ctx.Verb == "defend")
			{
				zoneManager.AddZoneBuilder(ctx.TargetZoneID, 6000, "Cleo_TerraFirma_WarrenQuestApproach", "QuestID", ctx.QuestID);
			}
			else if (ctx.Verb == "fetch")
			{
				GameObject item = ctx.getQuestDeliveryItem();
				item.AddPart(new DynamicQuestTarget());
				item.SetStringProperty("HasPregeneratedName", "yes");
				item.Render.DisplayName = ctx.getQuestItemNameMutation(item.Render.DisplayName);
				ctx.ItemID = zoneManager.CacheObject(item);
				zoneManager.AddZoneBuilder(ctx.TargetZoneID, 6000, "Cleo_TerraFirma_WarrenQuestPlacer", "deliveryItemID", ctx.ItemID, "Mode", "fetch", "Owner", "");
			}
			else if (ctx.Verb == "desecrate")
			{
				GameObject statue = ctx.getQuestRemoteInteractable();
				statue.AddPart(new DynamicQuestTarget());
				statue.AddPart(new Cleo_TerraFirma_WarrenQuestDeed { QuestID = ctx.QuestID, StepID = "b_desecrate", EventID = statue.GetStringProperty("QuestEvent") });
				statue.SetIntProperty("NoAIEquip", 1);
				statue.Render.DisplayName = ctx.getQuestItemNameMutation(statue.Render.DisplayName);
				statue.SetStringProperty("HasPregeneratedName", "yes");
				statue.RequirePart<Interesting>().Radius = 1;
				statue.FireEvent("SpecialInit");
				ctx.ItemID = zoneManager.CacheObject(statue);
				string owner = ctx.TargetOccupancy == "in use" ? Cleo_TerraFirma_WarrenQuests.FACTION : "";
				zoneManager.AddZoneBuilder(ctx.TargetZoneID, 6000, "Cleo_TerraFirma_WarrenQuestPlacer", "deliveryItemID", ctx.ItemID, "Mode", "statue", "Owner", owner);
			}
			zoneManager.AddZoneBuilder(giverZone, 6000, "Cleo_TerraFirma_WarrenShapeQuestGiver", "questContext", ctx);
		}
	}

	public static class Cleo_TerraFirma_WarrenQuestText
	{
		public static readonly string[] BAND = { "band", "pack" };
		public static readonly string[] HARASSING = { "plaguing", "harassing", "pestering" };
		public static readonly string[] DEAL_WITH = { "ply your bloody trade", "fend them off", "slay them" };
		public static readonly string[] MUST_DO = { "break bread", "make peace", "build trust" };
		public static readonly string[] DELIVER = { "deliver", "transport" };
		public static readonly string[] EFFECT = { "ensure peace lasts between our kin", "remind us of home when melancholy takes us", "bring us comfort when we are weary" };
		public static readonly string[] HOMES = { "quarry", "warren", "cavehome", "burrow" };
		public static readonly string[] SEEK_IT = { "seek it out", "search for it" };
		public static readonly string[] ON_OUR_BEHALF = { "in our stead", "on our behalf" };
		public static readonly string[] OCCUPIED = { "inhabited", "occupied" };
		public static readonly string[] STATION = { "station", "dispatch", "assign" };
		public static readonly string[] CARVE_VERB = { "carve a statue of", "sculpt" };

		public const string REWARD_LINE = "For your service, you may choose a boon from our guild.";

		public static string Pick(params string[] options) => options.GetRandomElement();

		public static string Spice(string path) => HistoricStringExpander.ExpandString("<spice." + path + ".!random>");

		public static string Sentences(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;
			char[] c = text.ToCharArray();
			bool cap = true;
			for (int i = 0; i < c.Length; i++)
			{
				if (cap && char.IsLetter(c[i]))
				{
					c[i] = char.ToUpperInvariant(c[i]);
					cap = false;
				}
				else if (c[i] == '.' || c[i] == '?' || c[i] == '!')
					cap = true;
				else if (cap && !char.IsWhiteSpace(c[i]))
					cap = false;
			}
			return Grammar.ConvertAtoAn(new string(c));
		}

		public class Bearing
		{
			public JournalMapNote Landmark;
			public string Where;
			public string LandmarkText => Landmark == null ? null : Grammar.InitLowerIfArticle(ColorUtility.StripFormatting(Landmark.Text));
		}

		public static Bearing FindBearing(string TargetZoneID, string TargetSecretID)
		{
			var result = new Bearing();
			Genkit.Location2D at = Zone.zoneIDTo240x72Location(TargetZoneID);
			if (at == null)
				return result;
			ZoneID.Parse(TargetZoneID, out string world, out int px, out int py, out int _, out int _, out int tz);
			bool Other(JournalMapNote n) => n != null && n.ID != TargetSecretID && n.ZoneID != TargetZoneID && !string.IsNullOrEmpty(n.Text);
			List<JournalMapNote> cardinal = JournalAPI.GetMapNotesInCardinalDirections(TargetZoneID) ?? new List<JournalMapNote>();
			cardinal = cardinal.Where(n => Other(n) && n.ZoneZ == tz).ToList();
			for (int broaden = 0; broaden <= 240 && cardinal.Count > 0; broaden += 3)
			{
				var ring = cardinal.Where(n => n.ResolvedLocation.Distance(at) >= 12 - broaden && n.ResolvedLocation.Distance(at) <= 18 + broaden).ToList();
				if (ring.Count == 0)
					continue;
				JournalMapNote lm = ring.GetRandomElement();
				int min = Math.Max(1, lm.ResolvedLocation.Distance(at) - Stat.Random(0, 12));
				int max = min + 12;
				string direction = "";
				if (lm.ResolvedX > at.X) direction = "west";
				if (lm.ResolvedX < at.X) direction = "east";
				if (lm.ResolvedY < at.Y) direction = "south";
				if (lm.ResolvedY > at.Y) direction = "north";
				result.Landmark = lm;
				result.Where = Math.Max(min / 3, 1) + "-" + Math.Max(max / 3, 1) + " parasangs " + direction + " of " + result.LandmarkText;
				return result;
			}
			for (int radius = 1; radius <= 80; radius++)
			{
				var near = JournalAPI.GetMapNotes(n => Other(n) && n.WorldID == world
					&& Math.Abs(n.ParasangX - px) <= radius && Math.Abs(n.ParasangY - py) <= radius
					&& !string.Equals(n.Category, "Miscellaneous"));
				if (near.Count == 0)
					continue;
				result.Landmark = near.GetRandomElement();
				result.Where = "within " + radius + " parasangs of " + result.LandmarkText;
				return result;
			}
			return result;
		}

		public static GameObject CarveMuse(Cleo_TerraFirma_WarrenQuestContext ctx)
		{
			if (ctx.CarveCelebrate)
			{
				string blueprint = PopulationManager.RollOneFrom(Cleo_TerraFirma_WarrenQuests.CARVE_CREATURE_TABLE)?.Blueprint;
				GameObject creature = string.IsNullOrEmpty(blueprint) ? null : GameObject.Create(blueprint);
				if (creature != null)
					return creature;
				ctx.CarveCelebrate = false;
			}
			string ancestor = PopulationManager.RollOneFrom(Cleo_TerraFirma_WarrenQuests.ANCESTOR_TABLE)?.Blueprint ?? "Cleo_TerraFirma_Stonecarver";
			GameObject muse = GameObject.Create(ancestor);
			StonecarverEpithet.Build(muse, Legendary: false);
			return muse;
		}

		public static GameObject CarveSubject(int Level)
		{
			string blueprint = PopulationManager.RollOneFrom(Cleo_TerraFirma_WarrenQuests.CarveSubjectTable(Level))?.Blueprint;
			return string.IsNullOrEmpty(blueprint) ? null : GameObject.Create(blueprint);
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_WarrenQuestManager : QuestManager
	{
		public string Verb;
		public string TargetZoneID;
		public string TargetSecretID;
		public string GiveItemID;
		public string PackKind;
		public bool Locate;
		public const string FETCH_STATE = "Cleo_TerraFirma_WarrenQuestFetch@";
		public const string STUDY_STATE = "Cleo_TerraFirma_WarrenQuestStudy@";
		public string FetchItemID
		{
			get => MyQuestID == null ? null : The.Game?.GetStringGameState(FETCH_STATE + MyQuestID, null);
			set
			{
				if (MyQuestID == null || The.Game == null) return;
				if (string.IsNullOrEmpty(value)) The.Game.RemoveStringGameState(FETCH_STATE + MyQuestID);
				else The.Game.SetStringGameState(FETCH_STATE + MyQuestID, value);
			}
		}
		public string StudyBlueprint
		{
			get => MyQuestID == null ? null : The.Game?.GetStringGameState(STUDY_STATE + MyQuestID, null);
			set
			{
				if (MyQuestID == null || The.Game == null) return;
				if (string.IsNullOrEmpty(value)) The.Game.RemoveStringGameState(STUDY_STATE + MyQuestID);
				else The.Game.SetStringGameState(STUDY_STATE + MyQuestID, value);
			}
		}

		public const string SPAWNED_STATE = "Cleo_TerraFirma_WarrenQuestPack@";

		private FindASiteDynamicQuestManager Locator()
			=> new FindASiteDynamicQuestManager(TargetZoneID, TargetSecretID, MyQuestID, "a_locate");

		private FindASpecificItemDynamicQuestManager Finder()
			=> new FindASpecificItemDynamicQuestManager(FetchItemID, MyQuestID, "b_find");

		public override void OnQuestAdded()
		{
			if (Locate)
				Locator().OnQuestAdded();
			if (!string.IsNullOrEmpty(FetchItemID))
				Finder().OnQuestAdded();
			if (!string.IsNullOrEmpty(GiveItemID))
			{
				GameObject item = The.ZoneManager.PullCachedObject(GiveItemID);
				if (item != null && The.Player != null)
				{
					item.MakeUnderstood();
					The.Player.ReceiveObject(item);
					Popup.Show("You receive " + item.an() + ".");
				}
			}
			The.Game.RequireSystem<Cleo_TerraFirma_WarrenQuestSystem>();
		}

		public override void AfterQuestAdded()
		{
			if (Locate)
				Locator().AfterQuestAdded();
			if (!string.IsNullOrEmpty(FetchItemID))
				Finder().AfterQuestAdded();
			OnZone(The.Player?.CurrentZone);
		}

		public override void OnQuestComplete()
		{
			if (Verb == "clearout")
				The.Game.SetIntGameState(Cleo_TerraFirma_WarrenQuestSystem.RECLAIM_STATE + TargetZoneID, 1);
		}

		public override GameObject GetQuestInfluencer()
			=> GameObject.Find(o => o.GetStringProperty("GivesDynamicQuest") == MyQuestID);

		public override string GetQuestZoneID() => TargetZoneID;

		private bool Done(string step) => The.Game.HasFinishedQuestStep(MyQuestID, step);

		public void OnZone(Zone Z)
		{
			if (Z == null || Z.ZoneID != TargetZoneID || !The.Game.HasQuest(MyQuestID))
				return;
			if (Verb == "defend")
			{
				if (!Locate && !Done("a_travel"))
					The.Game.FinishQuestStep(MyQuestID, "a_travel");
				if (The.Game.GetIntGameState(SPAWNED_STATE + MyQuestID) == 0)
				{
					The.Game.SetIntGameState(SPAWNED_STATE + MyQuestID, 1);
					SpawnPack(Z);
				}
				CheckFoes(Z, null);
			}
			else if (Verb == "clearout")
				CheckFoes(Z, null);
		}

		public void CheckStudy(Zone Z)
		{
			if (Z == null || string.IsNullOrEmpty(StudyBlueprint) || !The.Game.HasQuest(MyQuestID) || Done("a_study"))
				return;
			GameObject seen = Z.FindObject(o => o.Blueprint == StudyBlueprint && o.IsVisible());
			if (seen == null)
				return;
			Helpers.VerifyLog("QUEST", $"carve: saw {seen.DisplayNameOnlyDirectAndStripped} in {Z.ZoneID} ({MyQuestID})");
			The.Game.FinishQuestStep(MyQuestID, "a_study");
		}

		public void CheckFoes(Zone Z, GameObject Dying)
		{
			if (Z == null || Z.ZoneID != TargetZoneID || !The.Game.HasQuest(MyQuestID) || Done("b_defeat"))
				return;
			if (Verb == "defend" && The.Game.GetIntGameState(SPAWNED_STATE + MyQuestID) == 0)
				return;
			int alive = Z.GetObjects(o => o != Dying && o.GetPart<Cleo_TerraFirma_WarrenQuestFoe>() is Cleo_TerraFirma_WarrenQuestFoe f
				&& f.QuestID == MyQuestID && (Verb != "clearout" || f.Leader)).Count;
			if (alive > 0)
				return;
			Helpers.VerifyLog("QUEST", $"{Verb} won in {Z.ZoneID} ({MyQuestID})");
			The.Game.FinishQuestStep(MyQuestID, Locate ? "a_locate~b_defeat" : "b_defeat");
			if (Verb == "clearout")
			{
				foreach (GameObject miner in Z.GetObjects(o => o.Blueprint == Cleo_TerraFirma_WarrenQuests.SQUATTER && o != Dying))
					miner.ApplyEffect(new Terrified(50, The.Player));
			}
		}

		private void SpawnPack(Zone Z)
		{
			GameObject player = The.Player;
			bool here = player?.CurrentZone == Z;
			List<Cell> open = Z.GetEmptyReachableCells();
			if (open.Count == 0)
				open = Z.GetEmptyCells();
			var reach = new HashSet<Cell>(open);
			List<Cell> outside = Cleo_TerraFirma_WarrenQuestApproach.Cells(Z, MyQuestID).Where(reach.Contains).ToList();
			string from = "doorstep";
			if (outside.Count < 4)
			{
				outside = open.Where(c => c.X <= 2 || c.X >= Z.Width - 3 || c.Y <= 2 || c.Y >= Z.Height - 3).ToList();
				from = "zone edge";
			}
			if (outside.Count < 4)
			{
				outside = open;
				from = "anywhere";
			}
			Cell center = outside.Where(c => !here || c.DistanceTo(player) >= 10).GetRandomElement() ?? outside.GetRandomElement();
			if (center == null)
				return;
			var spots = outside.Where(c => !here || c.DistanceTo(player) >= 5)
				.OrderBy(c => c.DistanceTo(center))
				.ToList();
			int tier = The.ZoneManager.GetZoneTier(Z.ZoneID);
			bool tough = tier >= 2;
			string table = PackKind == "baboons" ? (tough ? Cleo_TerraFirma_WarrenQuests.DEFEND_BABOONS_2 : Cleo_TerraFirma_WarrenQuests.DEFEND_BABOONS)
				: PackKind == "burrowbees" ? (tough ? Cleo_TerraFirma_WarrenQuests.DEFEND_BURROWBEES_2 : Cleo_TerraFirma_WarrenQuests.DEFEND_BURROWBEES)
				: (tough ? Cleo_TerraFirma_WarrenQuests.DEFEND_SNAPJAWS_2 : Cleo_TerraFirma_WarrenQuests.DEFEND_SNAPJAWS);
			bool hero = PackKind == "burrowbees";
			int placed = 0;
			foreach (PopulationResult result in PopulationManager.Generate(table))
				for (int i = 0; i < result.Number && placed < spots.Count; i++)
				{
					GameObject foe = GameObject.Create(result.Blueprint);
					if (foe == null)
						continue;
					if (hero)
					{
						hero = false;
						try
						{
							foe = HeroMaker.MakeHero(foe) ?? foe;
						}
						catch (Exception x)
						{
							MetricsManager.LogException("Cleo_TerraFirma_WarrenQuests hero burrowbee", x);
						}
					}
					foe.AddPart(new Cleo_TerraFirma_WarrenQuestFoe { QuestID = MyQuestID });
					spots[placed++].AddObject(foe);
				}
			Helpers.VerifyLog("QUEST", $"defend pack: {placed} {PackKind} (tier {tier}, {table}) in {Z.ZoneID} near {center.X},{center.Y} ({from})");
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_WarrenQuestSystem : IGameSystem
	{
		public const string RECLAIM_STATE = "Cleo_TerraFirma_WarrenReclaimed@";
		public const string RECLAIM_DONE_STATE = "Cleo_TerraFirma_WarrenReclaimDone@";

		public override void Register(XRLGame Game, IEventRegistrar Registrar)
		{
			Registrar.Register(ZoneActivatedEvent.ID);
			Registrar.Register(EndTurnEvent.ID);
		}

		public static int EndTurnsSeen;

		public override bool HandleEvent(EndTurnEvent E)
		{
			EndTurnsSeen++;
			try
			{
				Zone Z = The.Player?.CurrentZone;
				if (Z != null && !Z.IsWorldMap())
				{
					var studying = new List<Cleo_TerraFirma_WarrenQuestManager>();
					foreach (Quest quest in The.Game.Quests.Values)
						if (!quest.Finished && quest.Manager is Cleo_TerraFirma_WarrenQuestManager manager && manager.StudyBlueprint != null)
							studying.Add(manager);
					foreach (var manager in studying)
						manager.CheckStudy(Z);
				}
			}
			catch (Exception x)
			{
				MetricsManager.LogException("Cleo_TerraFirma_WarrenQuestSystem study", x);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ZoneActivatedEvent E)
		{
			try
			{
				OnZone(E.Zone);
			}
			catch (Exception x)
			{
				MetricsManager.LogException("Cleo_TerraFirma_WarrenQuestSystem", x);
			}
			return base.HandleEvent(E);
		}

		public static void OnZone(Zone Z)
		{
			if (Z == null)
				return;
			var managers = new List<Cleo_TerraFirma_WarrenQuestManager>();
			foreach (Quest quest in The.Game.Quests.Values)
				if (!quest.Finished && quest.Manager is Cleo_TerraFirma_WarrenQuestManager manager)
					managers.Add(manager);
			foreach (var manager in managers)
				manager.OnZone(Z);
			Payoff(Z);
		}

		public static void Payoff(Zone Z)
		{
			if (The.Game.GetIntGameState(RECLAIM_STATE + Z.ZoneID) == 0 || The.Game.GetIntGameState(RECLAIM_DONE_STATE + Z.ZoneID) != 0)
				return;
			The.Game.SetIntGameState(RECLAIM_DONE_STATE + Z.ZoneID, 1);
			int gone = 0;
			Cell den = null;
			foreach (GameObject miner in Z.GetObjects(o => o.Blueprint == Cleo_TerraFirma_WarrenQuests.SQUATTER))
			{
				den ??= miner.CurrentCell;
				miner.Obliterate();
				gone++;
			}
			Cell heart = den
				?? Z.GetObjects(o => o.HasPart<RandomStatue>() || Cleo_TerraFirma_WarrenQuests.IsMonument(o)).GetRandomElement()?.CurrentCell;
			List<Cell> open = Z.GetEmptyReachableCells();
			if (heart != null)
			{
				var near = open.Where(c => c.DistanceTo(heart) <= 10).ToList();
				if (near.Count >= 6)
					open = near;
			}
			int placed = 0;
			foreach (string table in new[] { Cleo_TerraFirma_WarrenQuests.SETTLER_DRESSING, Cleo_TerraFirma_WarrenQuests.SETTLER_CREW })
				foreach (PopulationResult result in PopulationManager.Generate(table))
					for (int i = 0; i < result.Number && open.Count > 0; i++)
					{
						GameObject go = GameObject.Create(result.Blueprint);
						if (go == null)
							continue;
						if (table == Cleo_TerraFirma_WarrenQuests.SETTLER_DRESSING && go.Physics != null && string.IsNullOrEmpty(go.Physics.Owner)
							&& (go.Physics.Takeable || go.HasPart<XRL.World.Parts.Inventory>() || go.HasTag("Vessel")))
							go.Physics.Owner = "Cleo_TerraFirma_Quarriers";
						Cell cell = open.GetRandomElement();
						open.Remove(cell);
						cell.AddObject(go);
						placed++;
					}
			Helpers.VerifyLog("QUEST", $"reclaimed {Z.ZoneID}: {gone} squatters cleared, {placed} settlers and dressing placed");
		}
	}
}

namespace XRL.World.ZoneBuilders
{
	using XRL.World.WorldBuilders;

	public class Cleo_TerraFirma_WarrenShapeQuestGiver : ZoneBuilderSandbox
	{
		public DynamicQuestContext questContext;

		public bool BuildZone(Zone zone)
		{
			base.zone = zone;
			var ctx = questContext as Cleo_TerraFirma_WarrenQuestContext;
			if (ctx == null)
				return true;
			var filter = ctx.getQuestGiverFilter();
			var candidates = zone.GetObjects(o => filter(o));
			GameObject giver = candidates.FirstOrDefault(o => o.IDMatch(ctx.GiverOverseerID)) ?? candidates.GetRandomElement();
			if (giver == null)
			{
				Helpers.VerifyLog("QUEST", $"{ctx.Verb} at {zone.ZoneID}: no overseer to give it");
				return true;
			}
			try
			{
				Fabricate(ctx, giver);
			}
			catch (Exception x)
			{
				MetricsManager.LogException("Cleo_TerraFirma_WarrenShapeQuestGiver " + ctx.Verb + " at " + zone.ZoneID, x);
			}
			return true;
		}

		private static QuestStep Step(Quest quest, string id, string name, string text)
		{
			var step = new QuestStep { ID = id, Name = Grammar.MakeTitleCase(name), Text = text, XP = 100, Finished = false };
			quest.StepsByID.Add(id, step);
			return step;
		}

		private void Fabricate(Cleo_TerraFirma_WarrenQuestContext ctx, GameObject giver)
		{
			string target = ColorUtility.StripFormatting(ctx.TargetName);
			string origin = ctx.getQuestOriginZone();
			string giverName = giver.DisplayNameOnlyDirectAndStripped;
			var quest = new Quest { ID = ctx.QuestID ?? Guid.NewGuid().ToString(), StepsByID = new Dictionary<string, QuestStep>() };
			var manager = new Cleo_TerraFirma_WarrenQuestManager
			{
				MyQuestID = quest.ID,
				Verb = ctx.Verb,
				TargetZoneID = ctx.TargetZoneID,
				TargetSecretID = ctx.TargetSecretID,
				PackKind = ctx.PackKind
			};
			string pitch, accept, complete = null, incomplete, report = null, gate = null, reveal = ctx.TargetSecretID;
			string returnSteps = null;
			string takeItemID = null;
			bool locate = ctx.Verb == "clearout"
				|| ((ctx.Verb == "carve" || ctx.Verb == "fetch" || ctx.Verb == "desecrate") && ctx.TargetOccupancy != "in use")
				|| (ctx.TargetOccupancy == "in use" && ctx.UseBearing);
			var bearing = Cleo_TerraFirma_WarrenQuestText.FindBearing(ctx.TargetZoneID, ctx.TargetSecretID);
			string where = bearing.Where ?? ("at " + target);
			string landmark = bearing.LandmarkText ?? target;
			if (locate)
			{
				reveal = bearing.Landmark?.ID ?? ctx.TargetSecretID;
				manager.Locate = true;
				Step(quest, "a_locate", "Find " + target, "Locate " + target + ", located " + where + ".");
			}
			string returnName = "Return to " + origin;
			string returnText = "Return to " + origin + " and speak to " + giverName + ".";
			DynamicQuestReward reward = ctx.getQuestReward();
			switch (ctx.Verb)
			{
				case "defend":
				{
					string pack = ctx.PackKind == "baboons" || ctx.PackKind == "burrowbees" ? ctx.PackKind : "snapjaws";
					quest.Name = "Safeguard " + target;
					if (!locate)
						Step(quest, "a_travel", "Travel to " + target, "Travel to " + target + ".");
					Step(quest, "b_defeat", "Defeat the " + pack, "Defeat the " + pack + " at " + target + ".");
					Step(quest, "c_return", returnName, returnText);
					pitch = "a " + Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.BAND) + " of "
						+ Cleo_TerraFirma_WarrenQuestText.Spice("commonPhrases.wild") + " " + pack + " is "
						+ Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.HARASSING) + " our kin, " + where
						+ ". could you " + Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.DEAL_WITH)
						+ " for us? You will be rewarded for your work.";
					accept = "Yes. I will safeguard " + target + " as you ask.";
					complete = "I've defeated the " + pack + " at " + target + ".";
					incomplete = "I haven't defeated the " + pack + " yet.";
					report = Cleo_TerraFirma_WarrenQuestText.Spice("quests.thanks") + ". " + Cleo_TerraFirma_WarrenQuestText.REWARD_LINE;
					gate = "b_defeat";
					returnSteps = (locate ? "a_locate~" : "a_travel~") + "b_defeat~c_return";
					break;
				}
				case "clearout":
				{
					string creatures = "snapjaw miners";
					quest.Name = "Reclaim " + target;
					Step(quest, "b_defeat", "Defeat the " + creatures, "Defeat the " + creatures + " at " + target + ".");
					Step(quest, "c_return", returnName, returnText);
					string wordReached = Stat.Random(0, 1) == 0 ? "Word has reached us that" : Cleo_TerraFirma_WarrenQuestText.Spice("gossip.leadIns");
					pitch = "Greetings, wayfarer. we've heard tell of a " + Cleo_TerraFirma_WarrenQuestText.Spice("commonPhrases.abandoned") + " "
						+ Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.HOMES) + ", near " + landmark + ". Would you "
						+ Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.SEEK_IT) + " "
						+ Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.ON_OUR_BEHALF) + "? "
						+ wordReached + " it may be " + Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.OCCUPIED) + " by "
						+ creatures + ", so we request your " + Cleo_TerraFirma_WarrenQuestText.Spice("commonPhrases.services") + ".";
					accept = "Yes. I will reclaim " + target + " as you ask.";
					complete = "I've cleared out " + target + ".";
					incomplete = "I haven't cleared out " + target + " yet.";
					report = "we are " + Cleo_TerraFirma_WarrenQuestText.Spice("commonPhrases.thankful") + ", wayfarer. we will "
						+ Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.STATION) + " a group of quarriers there. "
						+ Cleo_TerraFirma_WarrenQuestText.REWARD_LINE;
					gate = "b_defeat";
					returnSteps = "a_locate~b_defeat~c_return";
					break;
				}
				case "carve":
				{
					GameObject subject0 = Cleo_TerraFirma_WarrenQuestText.CarveSubject(The.Player?.Level ?? 1);
					if (subject0 != null)
					{
						if (!string.IsNullOrEmpty(ctx.MuseID))
							The.ZoneManager.UncacheObject(ctx.MuseID);
						ctx.MuseID = The.ZoneManager.CacheObject(subject0);
						ctx.CarveCelebrate = true;
						var spikePart = The.ZoneManager.peekCachedObject(ctx.ItemID)?.GetPart<Cleo_TerraFirma_PreordainedSpike>();
						if (spikePart != null)
							spikePart.MuseID = ctx.MuseID;
						manager.StudyBlueprint = subject0.Blueprint;
					}
					GameObject muse = The.ZoneManager.peekCachedObject(ctx.MuseID);
					string subject = muse == null ? "a stonecarver" : muse.an(Stripped: true);
					string occasion = ctx.CarveCelebrate
						? Cleo_TerraFirma_WarrenQuestText.Spice("commonPhrases.celebrate") + " " + (muse?.t(Stripped: true) ?? "the stonecarver")
						: Cleo_TerraFirma_WarrenQuestText.Pick("remember our past", "memorialize our ancestors",
							"honor our " + Cleo_TerraFirma_WarrenQuestText.Spice("commonPhrases.bygone") + " kin");
					quest.Name = "Sculpt a statue of " + subject;
					if (manager.StudyBlueprint != null)
						Step(quest, "a_study", "Learn to carve " + subject, "See " + subject + " in the wild to learn how to carve it.");
					Step(quest, "b_carve", "Sculpt a statue of " + subject, "Use the preordained spike on a stone wall at " + target
						+ " to carve " + subject + ".");
					Step(quest, "c_return", returnName, returnText);
					pitch = "wayfarer, it has come time to " + occasion + ". Would you go to " + target + ", and "
						+ Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.CARVE_VERB) + " " + subject + "?";
					accept = "Yes. I will sculpt " + subject + " as you ask.";
					complete = "I've sculpted " + subject + " at " + target + ".";
					incomplete = "I haven't sculpted " + subject + " yet.";
					report = Cleo_TerraFirma_WarrenQuestText.Spice("quests.thanks") + ". " + Cleo_TerraFirma_WarrenQuestText.REWARD_LINE;
					gate = "b_carve";
					returnSteps = (locate ? "a_locate~" : "") + (manager.StudyBlueprint != null ? "a_study~" : "") + "b_carve~c_return";
					manager.GiveItemID = ctx.ItemID;
					break;
				}
				case "fetch":
				{
					GameObject item = The.ZoneManager.peekCachedObject(ctx.ItemID);
					string itemName = item?.DisplayNameOnlyDirectAndStripped ?? "ceremonial relic";
					string the = item?.the ?? "the ";
					quest.Name = "Recover " + the + itemName;
					Step(quest, "b_find", "Find " + the + itemName, "Find " + the + itemName + " at " + target + ".");
					Step(quest, "c_return", "Return " + the + itemName + " to " + origin, "Return " + the + itemName + " to " + origin + " and speak to " + giverName + ".");
					pitch = "wayfarer, we seek your aid in "
						+ Cleo_TerraFirma_WarrenQuestText.Pick("recovering our lost " + itemName, "finding a missing " + itemName, "searching for our beloved " + itemName) + ". "
						+ Cleo_TerraFirma_WarrenQuestText.Pick("It is dear to us", "It is highly valued", "It is sacred to us") + ". "
						+ Cleo_TerraFirma_WarrenQuestText.Pick("We last heard", "The crows mentioned", "The stones tell us") + " it is " + where + ".";
					accept = "Yes. I will find " + the + itemName + " as you ask.";
					complete = "I've found " + the + itemName + ".";
					incomplete = "I don't have " + the + itemName + " yet.";
					report = Cleo_TerraFirma_WarrenQuestText.Spice("quests.thanks") + ". " + Cleo_TerraFirma_WarrenQuestText.REWARD_LINE;
					takeItemID = ctx.ItemID;
					returnSteps = (locate ? "a_locate~" : "") + "b_find~c_return";
					manager.FetchItemID = ctx.ItemID;
					break;
				}
				case "desecrate":
				{
					GameObject statue = The.ZoneManager.peekCachedObject(ctx.ItemID);
					string statueName = statue?.DisplayNameOnlyDirectAndStripped ?? "ceremonial statue";
					string the = statue?.the ?? "the ";
					quest.Name = "Desecrate " + the + statueName;
					Step(quest, "b_desecrate", "Desecrate " + the + statueName, "Desecrate " + the + statueName + " at " + target + ".");
					Step(quest, "c_return", returnName, returnText);
					string subject = statue?.GetStringProperty(Cleo_TerraFirma_WarrenQuests.SUBJECT_PROPERTY) ?? statueName;
					pitch = Cleo_TerraFirma_WarrenQuestText.Pick("Wayfarer! We have heard rumors", "Wayfarer! Whispers have travelled through the vine", "Wayfarer! A nonsense bird sang to us")
						+ " that an idol to " + subject + " has been " + Cleo_TerraFirma_WarrenQuestText.Pick("built", "erected", "shaped") + " " + where + ". Would you "
						+ Cleo_TerraFirma_WarrenQuestText.Pick("desecrate", "profane", "defile") + " it for us, so that " + subject
						+ " may know our revilement, and that we may spread the salt of the earth?";
					accept = "Yes. I will desecrate " + the + statueName + " as you ask.";
					complete = "I've desecrated " + the + statueName + ".";
					incomplete = "I haven't desecrated " + the + statueName + " yet.";
					report = Cleo_TerraFirma_WarrenQuestText.Spice("quests.thanks") + ". " + Cleo_TerraFirma_WarrenQuestText.REWARD_LINE;
					gate = "b_desecrate";
					returnSteps = (locate ? "a_locate~" : "") + "b_desecrate~c_return";
					break;
				}
				case "deliver":
				{
					GameObject item = The.ZoneManager.peekCachedObject(ctx.ItemID);
					string itemName = item?.DisplayNameOnlyDirectAndStripped ?? "gift";
					string receiver = ctx.ReceiverOverseerName ?? ("the overseer of " + target);
					quest.Name = "Deliver to " + receiver;
					Step(quest, "a_deliver", "Deliver to " + receiver, "Bring " + (item?.the ?? "the ") + itemName + " to " + receiver + " at " + target + ".");
					pitch = "wayfarer, we must " + Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.MUST_DO)
						+ " with our kin. would you " + Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.DELIVER)
						+ " this " + itemName + " in our stead?";
					accept = "Yes. I will deliver " + (item?.the ?? "the ") + itemName + " as you ask.";
					incomplete = "I haven't delivered " + (item?.the ?? "the ") + itemName + " yet.";
					reward = new DynamicQuestReward
					{
						StepXP = ctx.tier * 1000,
						rewards = { new Cleo_TerraFirma_WarrenQuestReward(ctx.TargetZoneID, ctx.TargetName) }
					};
					manager.GiveItemID = ctx.ItemID;
					break;
				}
				default:
					return;
			}
			quest.Name = Grammar.MakeTitleCase(quest.Name);
			quest.Manager = manager;
			quest.dynamicReward = reward;
			DynamicQuestsGameState.Add(quest);

			giver.SetStringProperty("GivesDynamicQuest", quest.ID);
			giver.RequirePart<Interesting>();
			giver.SetIntProperty("QuestGiver", 1);
			if (giver.Brain != null)
			{
				giver.Brain.Wanders = false;
				giver.Brain.WandersRandomly = false;
			}
			ConversationXMLBlueprint conv = Cleo_TerraFirma_WarrenQuestGiverPrep.Own(giver);
			if (conv == null)
				return;
			ConversationXMLBlueprint intro = ConversationsAPI.AddNode(conv, null, Cleo_TerraFirma_WarrenQuestText.Sentences(pitch));
			ConversationXMLBlueprint yes = DynamicQuestConversationHelper.fabricateIntroAcceptChoice(accept, intro, quest);
			if (!string.IsNullOrEmpty(reveal))
				yes["RevealMapNote"] = reveal;
			DynamicQuestConversationHelper.fabricateIntroRejectChoice("No, I will not.", intro);

			var hold = new ConversationXMLBlueprint { ID = "Cleo_TerraFirma_QuestHold", Name = "Node" };
			ConversationXMLBlueprint open = ConversationsAPI.AddChoice(hold, null, intro, HistoricStringExpander.ExpandString("<spice.quests.intro.!random.capitalize>"));
			open["IfNotHaveQuest"] = quest.ID;
			open["IfNotFinishedQuest"] = quest.ID;
			if (report != null)
			{
				ConversationXMLBlueprint done = ConversationsAPI.AddNode(conv, null, Cleo_TerraFirma_WarrenQuestText.Sentences(report));
				ConversationXMLBlueprint live = ConversationsAPI.AddChoice(done, null, "End", "Live and drink.");
				if (takeItemID != null)
				{
					ConversationXMLBlueprint take = ConversationsAPI.AddPart(live, "TakeItem");
					take.SetAttribute("IDs", takeItemID);
					take.SetAttribute("ClearQuest", "true");
				}
				ConversationXMLBlueprint finish = ConversationsAPI.AddPart(live, "QuestHandler");
				finish.SetAttribute("Action", "Step");
				finish.SetAttribute("QuestID", quest.ID);
				finish.SetAttribute("StepID", returnSteps);
				ConversationXMLBlueprint back = ConversationsAPI.AddChoice(hold, null, done, complete);
				back["IfHaveQuest"] = quest.ID;
				back["IfNotFinishedQuest"] = quest.ID;
				ConversationXMLBlueprint notYet = ConversationsAPI.AddChoice(hold, null, "End", incomplete);
				notYet["IfHaveQuest"] = quest.ID;
				notYet["IfNotFinishedQuest"] = quest.ID;
				if (takeItemID != null)
				{
					back["IfHaveItemWithID"] = takeItemID;
					notYet["IfHaveItemWithID"] = "!" + takeItemID;
				}
				else
				{
					back["IfFinishedQuestStep"] = quest.ID + "~" + gate;
					notYet["IfNotFinishedQuestStep"] = quest.ID + "~" + gate;
				}
			}
			else
			{
				ConversationXMLBlueprint notYet = ConversationsAPI.AddChoice(hold, null, "End", incomplete);
				notYet["IfHaveQuest"] = quest.ID;
				notYet["IfNotFinishedQuest"] = quest.ID;
			}
			Cleo_TerraFirma_WarrenQuestGiverPrep.AddToStart(conv, hold.Children);
			Helpers.VerifyLog("QUEST", $"{giver.GetReferenceDisplayName()} in {giver.CurrentZone?.ZoneID} gives \"{quest.Name}\" ({ctx.Verb}, {quest.ID})");
		}
	}

	public class Cleo_TerraFirma_WarrenQuestApproach : ZoneBuilderSandbox
	{
		public const string STATE = "Cleo_TerraFirma_WarrenQuestApproach@";
		public string QuestID;

		public bool BuildZone(Zone zone)
		{
			base.zone = zone;
			if (string.IsNullOrEmpty(QuestID) || !WarrenCarver.CarvedByZone.TryGetValue(zone.ZoneID, out HashSet<int> carved) || carved.Count == 0)
			{
				Helpers.VerifyLog("QUEST", $"defend doorstep in {zone.ZoneID}: no carved floor, the pack will use the zone edge");
				return true;
			}
			int w = zone.Width;
			var dist = new Dictionary<int, int>();
			var queue = new Queue<int>();
			foreach (int i in carved)
			{
				dist[i] = 0;
				queue.Enqueue(i);
			}
			var found = new List<Cell>();
			while (queue.Count > 0 && found.Count < 120)
			{
				int i = queue.Dequeue();
				int d = dist[i];
				if (d >= 10)
					continue;
				Cell c = zone.GetCell(i % w, i / w);
				foreach (Cell n in c.GetAdjacentCells())
				{
					int ni = n.X + n.Y * w;
					if (dist.ContainsKey(ni) || n.IsSolid())
						continue;
					dist[ni] = d + 1;
					queue.Enqueue(ni);
					if (d + 1 >= 3)
						found.Add(n);
				}
			}
			The.Game.SetStringGameState(STATE + QuestID, string.Join(";", found.Select(c => c.X + "," + c.Y)));
			Helpers.VerifyLog("QUEST", $"defend doorstep in {zone.ZoneID}: {found.Count} canyon cells noted ({QuestID})");
			return true;
		}

		public static List<Cell> Cells(Zone Z, string questID)
		{
			var cells = new List<Cell>();
			string s = The.Game.GetStringGameState(STATE + questID);
			if (string.IsNullOrEmpty(s))
				return cells;
			foreach (string xy in s.Split(';'))
			{
				string[] p = xy.Split(',');
				if (p.Length == 2 && int.TryParse(p[0], out int x) && int.TryParse(p[1], out int y) && Z.GetCell(x, y) is Cell c && c.IsEmpty())
					cells.Add(c);
			}
			return cells;
		}
	}

	public class Cleo_TerraFirma_WarrenQuestReceiver : ZoneBuilderSandbox
	{
		public DynamicQuestContext questContext;

		public bool BuildZone(Zone zone)
		{
			base.zone = zone;
			var ctx = questContext as Cleo_TerraFirma_WarrenQuestContext;
			if (ctx == null)
				return true;
			try
			{
				Build(zone, ctx);
			}
			catch (Exception x)
			{
				MetricsManager.LogException("Cleo_TerraFirma_WarrenQuestReceiver at " + zone.ZoneID, x);
			}
			return true;
		}

		private void Build(Zone zone, Cleo_TerraFirma_WarrenQuestContext ctx)
		{
			var overseers = zone.GetObjects(o => o.Blueprint == Cleo_TerraFirma_WarrenQuests.OVERSEER);
			GameObject receiver = overseers.FirstOrDefault(o => o.IDMatch(ctx.ReceiverOverseerID)) ?? overseers.FirstOrDefault();
			if (receiver == null)
			{
				receiver = Cleo_TerraFirma_WarrenQuests.TakePresetOverseer(zone.ZoneID) ?? GameObject.Create(Cleo_TerraFirma_WarrenQuests.OVERSEER);
				Cell cell = zone.GetEmptyReachableCells().GetRandomElement() ?? zone.GetEmptyCells().GetRandomElement();
				if (cell == null)
					return;
				cell.AddObject(receiver);
				Helpers.VerifyLog("QUEST", $"deliver: no overseer seated in {zone.ZoneID}, placed {receiver.GetReferenceDisplayName()}");
			}
			receiver.RequirePart<Interesting>();
			ConversationXMLBlueprint conv = Cleo_TerraFirma_WarrenQuestGiverPrep.Own(receiver);
			if (conv == null)
				return;
			GameObject item = The.ZoneManager.peekCachedObject(ctx.ItemID) ?? The.Player?.Inventory?.Objects.FirstOrDefault(o => o.IDMatch(ctx.ItemID));
			string itemName = item?.DisplayNameOnlyDirectAndStripped ?? "gift";
			string giver = ctx.GiverOverseerName ?? ("the overseer of " + ctx.GiverName);
			string giverBare = ctx.GiverOverseerBareName ?? giver;
			string origin = ctx.getQuestOriginZone();
			string text = "Thank you for " + Cleo_TerraFirma_WarrenQuestText.Pick("bringing this here", "delivering this to me", "transporting this " + itemName)
				+ ", wayfarer. "
				+ Cleo_TerraFirma_WarrenQuestText.Pick(
					"We have awaited this gift from " + giver + " for many nights under the Beetle Moon.",
					"We have toiled for many days under the high salt sun, yearning for " + Grammar.MakePossessive(giverBare) + " " + itemName + ".",
					"It has been too long since we have heard from " + origin + ". I hope " + giver + " is well.")
				+ " It shall " + Cleo_TerraFirma_WarrenQuestText.Pick(Cleo_TerraFirma_WarrenQuestText.EFFECT) + ". "
				+ Cleo_TerraFirma_WarrenQuestText.REWARD_LINE;
			ConversationXMLBlueprint node = ConversationsAPI.AddNode(conv, null, text);
			ConversationXMLBlueprint live = ConversationsAPI.AddChoice(node, null, "End", "Live and drink.");
			ConversationXMLBlueprint take = ConversationsAPI.AddPart(live, "TakeItem");
			take.SetAttribute("IDs", ctx.ItemID);
			take.SetAttribute("ClearQuest", "true");
			ConversationXMLBlueprint step = ConversationsAPI.AddPart(live, "QuestHandler");
			step.SetAttribute("Action", "Step");
			step.SetAttribute("QuestID", ctx.QuestID);
			step.SetAttribute("StepID", ctx.UseBearing && ctx.TargetOccupancy == "in use" ? "a_locate~a_deliver" : "a_deliver");
			var hold = new ConversationXMLBlueprint { ID = "Cleo_TerraFirma_QuestHold", Name = "Node" };
			ConversationXMLBlueprint give = ConversationsAPI.AddChoice(hold, null, node, "I bring " + (item?.the ?? "the ") + itemName + " from " + origin + ".");
			give["IfHaveQuest"] = ctx.QuestID;
			give["IfNotFinishedQuest"] = ctx.QuestID;
			give["IfHaveItemWithID"] = ctx.ItemID;
			Cleo_TerraFirma_WarrenQuestGiverPrep.AddToStart(conv, hold.Children);
			Helpers.VerifyLog("QUEST", $"deliver: {receiver.GetReferenceDisplayName()} in {zone.ZoneID} takes {itemName} ({ctx.QuestID})");
		}
	}
}

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_WarrenQuestDeed : IPart
	{
		public string QuestID;
		public string StepID;
		public string EventID;

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			if (!string.IsNullOrEmpty(EventID))
				Registrar.Register(EventID);
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == EventID && The.Game.HasQuest(QuestID) && !The.Game.HasFinishedQuestStep(QuestID, StepID))
			{
				Helpers.VerifyLog("QUEST", $"{EventID} {ParentObject.DisplayNameOnlyDirectAndStripped} ({QuestID})");
				The.Game.FinishQuestStep(QuestID, StepID);
			}
			return base.FireEvent(E);
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_WarrenQuestFoe : IPart
	{
		public string QuestID;
		public bool Leader;

		public override bool WantEvent(int ID, int cascade)
			=> base.WantEvent(ID, cascade) || ID == BeforeDeathRemovalEvent.ID;

		public override bool HandleEvent(BeforeDeathRemovalEvent E)
		{
			Zone zone = ParentObject.CurrentZone;
			if (zone != null && The.Game.Quests.TryGetValue(QuestID ?? "", out Quest quest)
				&& quest.Manager is XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuestManager manager)
				manager.CheckFoes(zone, ParentObject);
			return base.HandleEvent(E);
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_PreordainedSpike : IPart
	{
		public const string BLUEPRINT = "Cleo_TerraFirma_PreordainedSpike";
		public const string COMMAND = "Cleo_TerraFirma_DriveSpike";

		public string QuestID;
		public string TargetZoneID;
		public string MuseID;

		public override bool WantEvent(int ID, int cascade)
			=> base.WantEvent(ID, cascade) || ID == GetInventoryActionsEvent.ID || ID == InventoryActionEvent.ID;

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			E.AddAction("Activate", "activate", COMMAND, null, Key: 'a', Default: 10);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == COMMAND && E.Actor != null && E.Actor.IsPlayer() && ForHere(E.Actor).Drive(E.Actor))
				E.RequestInterfaceExit();
			return base.HandleEvent(E);
		}

		private Cleo_TerraFirma_PreordainedSpike ForHere(GameObject Actor)
		{
			string here = Actor.CurrentZone?.ZoneID;
			if (here == null || here == TargetZoneID || Actor.Inventory == null)
				return this;
			foreach (GameObject o in Actor.Inventory.GetObjects())
			{
				var other = o.GetPart<Cleo_TerraFirma_PreordainedSpike>();
				if (other != null && other != this && other.TargetZoneID == here
					&& The.Game.HasQuest(other.QuestID) && !The.Game.HasFinishedQuestStep(other.QuestID, "b_carve"))
				{
					Helpers.VerifyLog("QUEST", $"spike for {QuestID} used in {here}: the spike for {other.QuestID} answers instead");
					return other;
				}
			}
			return this;
		}

		private bool Refuse(GameObject Actor)
		{
			return Actor.Fail("Your " + ParentObject.ShortDisplayName + " bounces off the wall, inert.");
		}

		public bool NotYetLearned()
			=> The.Game.TryGetQuest(QuestID, out Quest quest) && quest.StepsByID.ContainsKey("a_study") && !The.Game.HasFinishedQuestStep(QuestID, "a_study");

		public bool Eligible(GameObject Wall, Zone Z)
		{
			if (Wall == null || Z == null || Z.ZoneID != TargetZoneID)
				return false;
			if (!The.Game.HasQuest(QuestID) || The.Game.HasFinishedQuestStep(QuestID, "b_carve"))
				return false;
			if (NotYetLearned())
				return false;
			if (Wall.IsImportant() || Wall.HasProperty(Cleo_TerraFirma_CrewShift.RESERVED))
				return false;
			if (!string.IsNullOrEmpty(Wall.GetPart<Cleo_TerraFirma_Etchable>()?.Inscription))
				return false;
			var props = Wall.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			return props == null || props.MaterialType != "metal";
		}

		private bool Drive(GameObject Actor)
		{
			if (!Actor.CanMoveExtremities("Activate", ShowMessage: true, Involuntary: false, AllowTelekinetic: true))
				return false;
			if (Actor.OnWorldMap())
				return Refuse(Actor);
			Cell cell = Actor.Physics.PickDestinationCell(1, RequireCombat: false, Label: "Choose any adjacent wall.");
			if (cell == null)
				return false;
			if (!cell.HasWall() || !cell.IsAdjacentTo(Actor.CurrentCell))
				return Actor.Fail("You must choose an adjacent wall.");
			GameObject wall = cell.GetFirstWall();
			if (NotYetLearned())
			{
				Helpers.VerifyLog("QUEST", $"spike refused: a_study not done ({QuestID})");
				GameObject subject = The.ZoneManager.peekCachedObject(MuseID);
				return Actor.Fail("You have not yet seen " + (subject == null ? "your subject" : subject.an(Stripped: true)) + ".");
			}
			if (!Eligible(wall, cell.ParentZone))
			{
				Helpers.VerifyLog("QUEST", $"spike refused: wall {wall?.Blueprint} in {cell.ParentZone?.ZoneID}, target {TargetZoneID} ({QuestID})");
				return Refuse(Actor);
			}
			GameObject muse = The.ZoneManager.peekCachedObject(MuseID);
			bool madeMuse = muse == null;
			if (madeMuse)
				muse = GameObject.Create("Cleo_TerraFirma_Stonecarver");
			var props = wall.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			GameObject statue = Mutation.Cleo_TerraFirma_Stoneshaper.MakeStatueOfCreatureFromWall(muse, wall, string.Empty, props);
			if (statue == null)
				return Refuse(Actor);
			statue.SetIntProperty("norestock", 1);
			statue.SetIntProperty(Cleo_TerraFirma_CrewShift.RESERVED, 1);
			statue.SetImportant(true);
			Mutation.Cleo_TerraFirma_Stoneshaper.PlayCarveReveal(cell, wall, statue, () =>
			{
				wall.Obliterate();
				cell.AddObject(statue, true, Silent: true);
			}, Actor.IsPlayer());
			IComponent<GameObject>.XDidYToZ(Actor, "carve", "a statue of", muse, EndMark: "!", ColorAsGoodFor: Actor, IndefiniteObject: true, Source: Actor);
			if (madeMuse)
				muse.Obliterate();
			else
				The.ZoneManager.UncacheObject(MuseID);
			Helpers.VerifyLog("QUEST", $"carve: statue of {muse.DisplayNameOnlyDirectAndStripped} in {cell.ParentZone.ZoneID} at {cell.X},{cell.Y} ({QuestID})");
			The.Game.FinishQuestStep(QuestID, "b_carve");
			IComponent<GameObject>.AddPlayerMessage("Your " + ParentObject.ShortDisplayName + " fulfills its purpose and crumbles to dust.");
			Actor.UseEnergy(1000, "Item Preordained Spike");
			ParentObject.Destroy();
			return true;
		}
	}
}
