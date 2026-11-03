using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cleo.TerraFirma.Scripts;
using Genkit;
using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World.Conversations;
using XRL.World.Parts;

namespace XRL.World.WorldBuilders
{
	[Serializable]
	public class Cleo_TerraFirma_WarrenSite
	{
		public string ZoneID;
		public string Name;
		public string SecretID;
		public string Stage;
		public string Occupancy;
		[NonSerialized]
		public Location2D Location;
	}

	public static class Cleo_TerraFirma_WarrenQuests
	{
		public static readonly string[] VERBS = { "fetch", "desecrate", "carve", "deliver", "defend", "clearout" };

		private static List<Cleo_TerraFirma_WarrenSite> PoolFor(string verb, List<Cleo_TerraFirma_WarrenSite> givers,
			List<Cleo_TerraFirma_WarrenSite> abandoned, List<Cleo_TerraFirma_WarrenSite> reclaimed)
		{
			if (verb == "fetch" || verb == "desecrate")
				return abandoned;
			if (verb == "clearout")
				return reclaimed;
			if (verb == "carve")
				return givers.Concat(abandoned).ToList();
			return givers;
		}

		public const string PRESET_STATE = "Cleo_TerraFirma_PresetOverseer@";

		internal static GameObject PresetOverseer(string zoneID)
		{
			string id = The.Game.GetStringGameState(PRESET_STATE + zoneID);
			GameObject go = string.IsNullOrEmpty(id) ? null : The.ZoneManager.peekCachedObject(id);
			if (go != null)
				return go;
			go = GameObject.Create(OVERSEER);
			if (go == null)
				return null;
			The.Game.SetStringGameState(PRESET_STATE + zoneID, The.ZoneManager.CacheObject(go));
			return go;
		}

		public static GameObject TakePresetOverseer(string zoneID)
		{
			string id = The.Game?.GetStringGameState(PRESET_STATE + zoneID);
			if (string.IsNullOrEmpty(id))
				return null;
			The.Game.SetStringGameState(PRESET_STATE + zoneID, "");
			return The.ZoneManager.PullCachedObject(id);
		}

		public const string TARGET_STATE = "Cleo_TerraFirma_WarrenQuestTarget@";

		public const string IN_USE_STATE = "Cleo_TerraFirma_WarrenSitesInUse";

		public static readonly string[] BANDS = { "close", "mid", "far" };

		public const int BEARING_CHANCE = 50;

		public static void RollAll(List<Cleo_TerraFirma_WarrenSite> sites)
		{
			if (sites == null || sites.Count == 0)
				return;
			var rng = new Random(The.Game.GetWorldSeed("TFWARRENQUESTS"));
			Ledger.Clear();
			var givers = sites.Where(s => s.Occupancy == "in use").ToList();
			The.Game.SetStringGameState(IN_USE_STATE, string.Join("\n", givers.Select(s => s.ZoneID + "|" + s.Name + "|" + s.SecretID)));
			var abandoned = sites.Where(s => s.Occupancy == "abandoned").ToList();
			var reclaimed = sites.Where(s => s.Occupancy == "reclaimed").ToList();
			var cleared = new HashSet<string>();
			var verbs = new List<string>();
			for (int i = 0; i < givers.Count; i++)
				verbs.Add(VERBS[i % VERBS.Length]);
			for (int i = verbs.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (verbs[i], verbs[j]) = (verbs[j], verbs[i]); }
			var used = new Dictionary<string, int>();
			int rolled = 0;
			for (int i = 0; i < givers.Count; i++)
			{
				var giver = givers[i];
				string verb = verbs[i];
				var pool = PoolFor(verb, givers, abandoned, reclaimed).Where(s => s != giver && !(verb == "clearout" && cleared.Contains(s.ZoneID))).ToList();
				if (pool.Count == 0 && verb == "clearout")
				{
					verb = "defend";
					pool = givers.Where(s => s != giver).ToList();
				}
				if (pool.Count == 0)
					pool = sites.Where(s => s != giver && s.Occupancy != "reclaimed").ToList();
				if (pool.Count == 0)
					continue;
				var target = PickTarget(giver, pool, used, rng, out string band);
				used[target.ZoneID] = (used.TryGetValue(target.ZoneID, out int n) ? n : 0) + 1;
				The.Game.SetIntGameState(TARGET_STATE + target.ZoneID, 1);
				var context = new Cleo_TerraFirma_WarrenQuestContext
				{
					GiverZoneID = giver.ZoneID,
					GiverName = giver.Name,
					TargetZoneID = target.ZoneID,
					TargetName = target.Name,
					TargetSecretID = target.SecretID,
					TargetOccupancy = target.Occupancy,
					Verb = verb,
					questNumber = 0,
					tier = Math.Max(1, The.ZoneManager.GetZoneTier(giver.ZoneID))
				};
				if (verb == "clearout")
					cleared.Add(target.ZoneID);
				context.QuestID = Guid.NewGuid().ToString();
				context.PackKind = PACK_KINDS[rng.Next(PACK_KINDS.Length)];
				context.CarveCelebrate = rng.Next(4) == 0;
				context.UseBearing = rng.Next(100) < BEARING_CHANCE;
				if (verb == "deliver")
				{
					GameObject from = PresetOverseer(giver.ZoneID);
					GameObject to = PresetOverseer(target.ZoneID);
					context.GiverOverseerID = from?.ID;
					context.GiverOverseerName = from?.DisplayNameOnlyDirectAndStripped;
					context.GiverOverseerBareName = from?.GetDisplayName(Stripped: true, WithoutTitles: true, BaseOnly: true);
					context.ReceiverOverseerID = to?.ID;
					context.ReceiverOverseerName = to?.DisplayNameOnlyDirectAndStripped;
				}
				try
				{
					DynamicQuestFactory.fabricateQuestTemplate(typeof(Cleo_TerraFirma_WarrenShapeQuestTemplate), context);
					rolled++;
					Helpers.VerifyLog("QUEST", $"{giver.Name} ({giver.ZoneID}) gives {verb} -> {target.Name} ({target.ZoneID}, {target.Occupancy}, {band}, {Distance(giver, target)} zones away)");
					Ledger.Add(verb + "|" + giver.ZoneID + "|" + giver.Name + "|" + target.ZoneID + "|" + target.Name);
				}
				catch (Exception x)
				{
					MetricsManager.LogException("Cleo_TerraFirma_WarrenQuests " + verb + " at " + giver.ZoneID, x);
				}
			}
			The.Game.SetStringGameState(LEDGER_STATE, string.Join("\n", Ledger));
			Helpers.VerifyLog("QUEST", $"rolled {rolled} warren quests from {givers.Count} in-use warrens ({abandoned.Count} abandoned, {reclaimed.Count} reclaimed)");
		}

		public const string LEDGER_STATE = "Cleo_TerraFirma_WarrenQuestLedger";
		private static readonly List<string> Ledger = new List<string>();

		private static Cleo_TerraFirma_WarrenSite PickTarget(Cleo_TerraFirma_WarrenSite giver, List<Cleo_TerraFirma_WarrenSite> pool,
			Dictionary<string, int> used, Random rng, out string band)
		{
			int Used(Cleo_TerraFirma_WarrenSite s) => used.TryGetValue(s.ZoneID, out int n) ? n : 0;
			var sorted = pool.OrderBy(s => Distance(giver, s)).ToList();
			int b = rng.Next(BANDS.Length);
			band = BANDS[b];
			int start = sorted.Count * b / BANDS.Length;
			int end = Math.Max(start + 1, sorted.Count * (b + 1) / BANDS.Length);
			var inBand = sorted.GetRange(start, end - start);
			int least = inBand.Min(Used);
			var picks = inBand.Where(s => Used(s) == least).ToList();
			return picks[rng.Next(picks.Count)];
		}

		private static int Distance(Cleo_TerraFirma_WarrenSite a, Cleo_TerraFirma_WarrenSite b)
		{
			Location2D la = a.Location ?? Zone.zoneIDTo240x72Location(a.ZoneID);
			Location2D lb = b.Location ?? Zone.zoneIDTo240x72Location(b.ZoneID);
			if (la == null || lb == null)
				return int.MaxValue;
			return Math.Max(Math.Abs(la.X - lb.X), Math.Abs(la.Y - lb.Y));
		}

		public const string FETCH_TABLE = "Cleo_TerraFirma_WarrenQuest FetchItems";
		public const string ANCESTOR_TABLE = "Cleo_TerraFirma_WarrenQuest Ancestors";
		public const string FIGURINE_TABLE = "Cleo_TerraFirma_WarrenQuest Figurines";
		public const string JEWEL_TABLE = "Cleo_TerraFirma_WarrenQuest Jewels";
		public const string SACRED_TAG = "Cleo_TerraFirma_SacredThings";
		public const string OVERSEER = "Cleo_TerraFirma_Overseer";
		public const string FACTION = "Cleo_TerraFirma_Quarriers";
		public const string SQUATTER = "Cleo_TerraFirma_Snapjaw Miner";
		public const string DEFEND_SNAPJAWS = "Cleo_TerraFirma_WarrenQuest DefendSnapjaws";
		public const string DEFEND_BABOONS = "Cleo_TerraFirma_WarrenQuest DefendBaboons";
		public const string DEFEND_SNAPJAWS_2 = "Cleo_TerraFirma_WarrenQuest DefendSnapjaws2";
		public const string DEFEND_BABOONS_2 = "Cleo_TerraFirma_WarrenQuest DefendBaboons2";
		public const string DEFEND_BURROWBEES = "Cleo_TerraFirma_WarrenQuest DefendBurrowbees";
		public const string DEFEND_BURROWBEES_2 = "Cleo_TerraFirma_WarrenQuest DefendBurrowbees2";

		public static readonly string[] PACK_KINDS = { "snapjaws", "baboons", "burrowbees" };
		public const string CARVE_CREATURE_TABLE = "Cleo_TerraFirma_WarrenQuest CarveCreatures";
		public static readonly string[] CARVE_SUBJECT_TABLES = { "Cleo_TerraFirma_WarrenQuest CarveSubjects1", "Cleo_TerraFirma_WarrenQuest CarveSubjects2", "Cleo_TerraFirma_WarrenQuest CarveSubjects3" };

		public static string CarveSubjectTable(int Level) => CARVE_SUBJECT_TABLES[Level >= 20 ? 2 : Level >= 10 ? 1 : 0];
		public const string SETTLER_CREW = "Cleo_TerraFirma_WarrenQuest SettlerCrew";
		public const string SETTLER_DRESSING = "Cleo_TerraFirma_WarrenQuest SettlerDressing";

		public const string ADJECTIVE = "ceremonial";

		public static void Paint(GameObject GO)
		{
			if (GO?.Render == null)
				return;
			GO.Render.SetForegroundColor('W');
			GO.Render.DetailColor = GO.Blueprint == "Cleo_TerraFirma_QuestHookah" ? "y" : "w";
		}

		public const int MONUMENT_CHANCE = 35;

		public static bool IsMonument(GameObject GO) => Array.IndexOf(WarrenCarver.MONUMENTS, GO?.Blueprint) >= 0;

		public static GameObject Monument()
		{
			GameObject monument = GameObject.Create(WarrenCarver.MONUMENTS.GetRandomElement());
			Paint(monument);
			return monument;
		}

		public const string SUBJECT_PROPERTY = "Cleo_TerraFirma_QuestStatueSubject";

		public const int CREATURE_IDOL_CHANCE = 80;

		public static GameObject CreatureIdol()
		{
			GameObject statue = GameObject.Create("Random Stone Statue");
			string blueprint = statue.GetPart<RandomStatue>()?.BaseBlueprint;
			GameObject creature = blueprint == null ? null : GameObject.CreateSample(blueprint);
			if (creature != null)
			{
				statue.SetStringProperty(SUBJECT_PROPERTY, creature.an(Stripped: true, Short: true, BaseOnly: true));
				creature.Obliterate();
			}
			statue.RemovePart<Shrine>();
			Paint(statue);
			return statue;
		}

		public static GameObject AncestorStatue()
		{
			GameObject statue = GameObject.Create("Random Stone Statue");
			string blueprint = PopulationManager.RollOneFrom(ANCESTOR_TABLE)?.Blueprint ?? "Cleo_TerraFirma_Stonecarver";
			GameObject ancestor = GameObject.Create(blueprint);
			if (ancestor != null)
			{
				StonecarverEpithet.Build(ancestor, Legendary: false);
				statue.GetPart<RandomStatue>()?.SetCreature(ancestor);
				statue.SetStringProperty(SUBJECT_PROPERTY, ancestor.DisplayNameOnlyDirectAndStripped);
				ancestor.Obliterate();
			}
			statue.RemovePart<Shrine>();
			Paint(statue);
			return statue;
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_WarrenQuestContext : DynamicQuestContext
	{
		public string GiverZoneID;
		public string GiverName;
		public string TargetZoneID;
		public string TargetName;
		public string TargetSecretID;
		public string TargetOccupancy;
		public string Verb;
		public string QuestID;
		public string ItemID;
		public string MuseID;
		public string PackKind;
		public bool CarveCelebrate;
		public const string BEARING_STATE = "Cleo_TerraFirma_WarrenQuestBearing@";
		public bool UseBearing
		{
			get => QuestID != null && The.Game != null && The.Game.GetIntGameState(BEARING_STATE + QuestID) == 1;
			set { if (QuestID != null) The.Game?.SetIntGameState(BEARING_STATE + QuestID, value ? 1 : 0); }
		}
		public string GiverOverseerID;
		public string GiverOverseerName;
		public string GiverOverseerBareName;
		public string ReceiverOverseerID;
		public string ReceiverOverseerName;

		public override HistoryKit.HistoricEntity originEntity() => null;
		public override string originZoneId() => GiverZoneID;
		public override string getQuestOriginZone() => GiverName;
		public override string getQuestGiverZone() => GiverZoneID;
		public override string questTargetZone(int minDistance, int maxDistance) => TargetZoneID;
		public override GeneratedLocationInfo getNearbyUndiscoveredLocation() => null;
		public override string assassinationTargetId() => null;
		public override GameObject getALostItem() => null;

		public override DynamicQuestReward getQuestReward()
		{
			return new DynamicQuestReward
			{
				StepXP = tier * 1000,
				rewards = { new Cleo_TerraFirma_WarrenQuestReward(GiverZoneID, GiverName) }
			};
		}

		public bool QuestGiverFilter(GameObject GO)
			=> GO.Blueprint == Cleo_TerraFirma_WarrenQuests.OVERSEER && !GO.HasProperty("GivesDynamicQuest");

		public override Func<GameObject, bool> getQuestGiverFilter() => QuestGiverFilter;

		public bool QuestActorFilter(GameObject GO)
			=> GO.IsCreature && GO.Blueprint != Cleo_TerraFirma_WarrenQuests.OVERSEER && GO.HasPart("Cleo_TerraFirma_CrewShift");

		public override Func<GameObject, bool> getQuestActorFilter() => QuestActorFilter;

		public override List<string> getSacredThings()
		{
			string tag = GameObjectFactory.Factory.GetBlueprintIfExists(Cleo_TerraFirma_WarrenQuests.OVERSEER)?.GetTag(Cleo_TerraFirma_WarrenQuests.SACRED_TAG);
			var list = string.IsNullOrEmpty(tag)
				? new List<string>()
				: tag.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
			if (list.Count == 0)
				list.Add("the stillness of stone");
			return list;
		}

		public override DynamicQuestDeliveryTarget getQuestDeliveryTarget()
		{
			return new DynamicQuestDeliveryTarget
			{
				type = "Cleo_TerraFirma_Warren",
				zoneId = TargetZoneID,
				location = Zone.zoneIDTo240x72Location(TargetZoneID),
				displayName = TargetName,
				secretId = TargetSecretID,
				attributes = new List<string>(),
				factions = new List<string>()
			};
		}

		public override GameObject getQuestRemoteInteractable()
		{
			GameObject statue = Verb == "desecrate"
				? (Stat.Random(1, 100) <= Cleo_TerraFirma_WarrenQuests.CREATURE_IDOL_CHANCE ? Cleo_TerraFirma_WarrenQuests.CreatureIdol() : Cleo_TerraFirma_WarrenQuests.AncestorStatue())
				: Stat.Random(1, 100) <= Cleo_TerraFirma_WarrenQuests.MONUMENT_CHANCE
				? Cleo_TerraFirma_WarrenQuests.Monument()
				: Cleo_TerraFirma_WarrenQuests.AncestorStatue();
			bool desecrate = Verb == "desecrate";
			statue.SetStringProperty("QuestVerb", desecrate ? "desecrate" : "pray at");
			statue.SetStringProperty("QuestEvent", desecrate ? "Desecrated" : "Prayed");
			statue.AddPart(new Shrine());
			statue.SetIntProperty("norestock", 1);
			statue.SetStringProperty("NeverStack", "1");
			return statue;
		}

		public override GameObject getQuestGenericRemoteInteractable()
		{
			GameObject statue = Cleo_TerraFirma_WarrenQuests.AncestorStatue();
			statue.SetStringProperty("QuestVerb", "etch");
			statue.SetStringProperty("QuestEvent", Cleo_TerraFirma_Etchable.ETCHED_EVENT);
			statue.SetIntProperty("norestock", 1);
			statue.SetIntProperty("QuestItem", 1);
			return statue;
		}

		public override GameObject getQuestDeliveryItem()
		{
			string blueprint = PopulationManager.RollOneFrom(Cleo_TerraFirma_WarrenQuests.FETCH_TABLE)?.Blueprint ?? "Cleo_TerraFirma_BoxOfChalk";
			GameObject item = GameObject.Create(blueprint);
			Cleo_TerraFirma_WarrenQuests.Paint(item);
			item.SetIntProperty("norestock", 1);
			item.SetIntProperty("QuestItem", 1);
			item.SetStringProperty("NeverStack", "1");
			Cleo_TerraFirma_QuestBurden.Roll(item);
			return item;
		}

		public override string getQuestItemNameMutation(string input)
			=> Cleo_TerraFirma_WarrenQuests.ADJECTIVE + " " + input;
	}

	[Serializable]
	public class Cleo_TerraFirma_WarrenFetchQuestTemplate : BaseDynamicQuestTemplate
	{
		public override void init(DynamicQuestContext context)
		{
			Array values = Enum.GetValues(typeof(QuestStoryType_FindASpecificItem));
			var qst = (QuestStoryType_FindASpecificItem)values.GetValue(Stat.Random(0, values.Length - 1));
			GameObject item = context.getQuestDeliveryItem();
			item.AddPart(new DynamicQuestTarget());
			item.SetStringProperty("HasPregeneratedName", "yes");
			item.Render.DisplayName = context.getQuestItemNameMutation(item.Render.DisplayName);
			string id = zoneManager.CacheObject(item);
			DynamicQuestDeliveryTarget target = context.getQuestDeliveryTarget();
			string giverZone = context.getQuestGiverZone();
			zoneManager.AddZoneBuilder(giverZone, 6000, "Cleo_TerraFirma_WarrenQuestGiverPrep", "Phase", "before");
			zoneManager.AddZoneBuilder(giverZone, 6000, "FindASpecificItemDynamicQuestTemplate_FabricateQuestGiver", "questContext", context, "QST", qst, "deliveryItemID", id, "deliveryTarget", target);
			zoneManager.AddZoneBuilder(giverZone, 6000, "Cleo_TerraFirma_WarrenQuestGiverPrep", "Phase", "after");
			zoneManager.AddZoneBuilder(target.zoneId, 6000, "Cleo_TerraFirma_WarrenQuestPlacer", "deliveryItemID", id, "Mode", "fetch", "Owner", "");
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_WarrenInteractQuestTemplate : BaseDynamicQuestTemplate
	{
		public override void init(DynamicQuestContext context)
		{
			var ctx = context as Cleo_TerraFirma_WarrenQuestContext;
			bool shrine = ctx?.Verb != "etch";
			var qst = shrine ? QuestStoryType_InteractWithAnObject.HolyItem : QuestStoryType_InteractWithAnObject.StrangePlan;
			GameObject statue = shrine ? context.getQuestRemoteInteractable() : context.getQuestGenericRemoteInteractable();
			statue.AddPart(new DynamicQuestTarget());
			statue.AddPart(new InteractQuestTarget { EventID = statue.GetStringProperty("QuestEvent") });
			statue.SetIntProperty("NoAIEquip", 1);
			if (!statue.HasPart<Shrine>())
			{
				statue.GiveProperName(context.getQuestItemNameMutation(statue.Render.DisplayName), Force: true);
				statue.SetStringProperty("DefiniteArticle", "the");
				statue.SetStringProperty("IndefiniteArticle", "the");
			}
			else
			{
				statue.Render.DisplayName = context.getQuestItemNameMutation(statue.Render.DisplayName);
			}
			statue.SetStringProperty("HasPregeneratedName", "yes");
			statue.RequirePart<Interesting>().Radius = 1;
			statue.FireEvent("SpecialInit");
			string id = zoneManager.CacheObject(statue);
			DynamicQuestDeliveryTarget target = context.getQuestDeliveryTarget();
			string giverZone = context.getQuestGiverZone();
			string owner = ctx?.TargetOccupancy == "in use" ? Cleo_TerraFirma_WarrenQuests.FACTION : "";
			zoneManager.AddZoneBuilder(giverZone, 6000, "Cleo_TerraFirma_WarrenQuestGiverPrep", "Phase", "before");
			zoneManager.AddZoneBuilder(giverZone, 6000, "InteractWithAnObjectDynamicQuestTemplate_FabricateQuestGiver", "questContext", context, "QST", qst, "deliveryItemID", id, "deliveryTarget", target);
			zoneManager.AddZoneBuilder(giverZone, 6000, "Cleo_TerraFirma_WarrenQuestGiverPrep", "Phase", "after");
			zoneManager.AddZoneBuilder(target.zoneId, 6000, "Cleo_TerraFirma_WarrenQuestPlacer", "deliveryItemID", id, "Mode", "statue", "Owner", owner);
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_WarrenQuestReward : DynamicQuestRewardElement
	{
		public string GiverZoneID;
		public string GiverName;

		public Cleo_TerraFirma_WarrenQuestReward()
		{
		}

		public Cleo_TerraFirma_WarrenQuestReward(string GiverZoneID, string GiverName)
		{
			this.GiverZoneID = GiverZoneID;
			this.GiverName = GiverName;
		}

		public override string getRewardConversationType() => "Choice";
		public override string getRewardAcceptQuestText() => null;

		public override void award()
		{
			Cleo_TerraFirma_WarrenStart.NoteQuestTurnedIn(GiverZoneID);
			GameObject player = The.Player;
			if (player == null)
				return;
			GameObject figurine = Figurine();
			List<GameObject> jewels = Jewels();
			var options = new List<string>();
			var hotkeys = new List<char>();
			var kinds = new List<string>();
			if (figurine != null)
			{
				options.Add(figurine.DisplayNameOnlyDirect);
				kinds.Add("figurine");
			}
			if (jewels.Count > 0)
			{
				options.Add(Describe(jewels));
				kinds.Add("jewels");
			}
			options.Add("have " + TemperingGiverName() + " temper a piece of your gear");
			kinds.Add("temper");
			for (int i = 0; i < options.Count; i++)
				hotkeys.Add((char)('a' + i));
			int index = Popup.PickOption("Choose a reward", null, "", "Sounds/UI/ui_notification", options.ToArray(), hotkeys.ToArray(), null, null, null, null, null, 1);
			if (index < 0 || index >= kinds.Count)
				index = kinds.Count - 1;
			string kind = kinds[index];
			Helpers.VerifyLog("QUEST", $"reward at {GiverZoneID}: picked {kind} ({options[index]})");
			if (kind == "figurine")
			{
				Give(player, figurine);
				jewels.ForEach(j => j.Obliterate());
			}
			else if (kind == "jewels")
			{
				jewels.ForEach(j => Give(player, j));
				figurine?.Obliterate();
			}
			else
			{
				Cleo_TerraFirma_Tempering.Grant(player, GiverZoneID);
				GameObject speaker = The.Speaker;
				if (speaker != null && speaker.CurrentZone?.ZoneID == GiverZoneID)
					Cleo_TerraFirma_Tempering.Offer(player, speaker, GiverZoneID);
				figurine?.Obliterate();
				jewels.ForEach(j => j.Obliterate());
			}
		}

		private string TemperingGiverName()
		{
			GameObject speaker = The.Speaker;
			if (speaker != null && speaker.HasProperName && speaker.CurrentZone?.ZoneID == GiverZoneID)
				return speaker.ShortDisplayNameWithoutTitlesStripped;
			return "the overseer of " + (GiverName ?? "the warren");
		}

		private static void Give(GameObject player, GameObject item)
		{
			item.MakeUnderstood();
			player.ReceiveObject(item);
		}

		private static GameObject Figurine()
		{
			string blueprint = PopulationManager.RollOneFrom(Cleo_TerraFirma_WarrenQuests.FIGURINE_TABLE)?.Blueprint;
			if (string.IsNullOrEmpty(blueprint))
				return null;
			GameObject fig = GameObject.Create(blueprint);
			if (fig == null)
				return null;
			MakeRewardFigurine(fig);
			Cleo_TerraFirma_SchrodingerFigurine schrodinger = fig.GetPart<Cleo_TerraFirma_SchrodingerFigurine>();
			if (schrodinger != null)
				schrodinger.QuestReward = true;
			return fig;
		}

		public static void MakeRewardFigurine(GameObject fig)
		{
			AddsRep rep = fig.GetPart<AddsRep>();
			if (rep != null)
			{
				rep.Value /= 2;
				if (!string.IsNullOrEmpty(rep.Faction) && rep.Faction.Contains(':'))
				{
					rep.Faction = string.Join(",", rep.Faction.Split(',').Select(f =>
					{
						int colon = f.IndexOf(':');
						if (colon < 0 || !int.TryParse(f.Substring(colon + 1), out int v))
							return f;
						return f.Substring(0, colon) + ":" + (v / 2);
					}));
				}
				rep.WorksOnCarrier = true;
				rep.WorksOnEquipper = false;
			}
			fig.Render.DisplayName = Cleo_TerraFirma_WarrenQuests.ADJECTIVE + " " + fig.Render.DisplayName;
		}

		private static List<GameObject> Jewels()
		{
			var list = new List<GameObject>();
			foreach (PopulationResult result in PopulationManager.Generate(Cleo_TerraFirma_WarrenQuests.JEWEL_TABLE))
				for (int i = 0; i < result.Number; i++)
				{
					GameObject jewel = GameObject.Create(result.Blueprint);
					if (jewel != null)
						list.Add(jewel);
				}
			return list;
		}

		private static string Describe(List<GameObject> items)
		{
			var sb = new StringBuilder();
			foreach (var group in items.GroupBy(i => i.DisplayNameOnlyDirect))
			{
				if (sb.Length > 0)
					sb.Append(", ");
				sb.Append(group.Key);
				if (group.Count() > 1)
					sb.Append(" &Wx").Append(group.Count());
			}
			return sb.ToString();
		}
	}
}

namespace XRL.World.ZoneBuilders
{
	public class Cleo_TerraFirma_WarrenQuestGiverPrep : ZoneBuilderSandbox
	{
		public const string HOOK_ID = "Cleo_TerraFirma_QuestHook";

		public string Phase;

		public bool BuildZone(Zone zone)
		{
			foreach (GameObject go in zone.GetObjects(o => o.Blueprint == XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuests.OVERSEER))
			{
				if (Phase == "after")
					After(go);
				else
					Before(go);
			}
			return true;
		}

		private static void Before(GameObject go)
		{
			ConversationXMLBlueprint copy = Own(go);
			if (copy == null || copy.Children.Any(c => c.ID == HOOK_ID))
				return;
			copy.Children.Add(new ConversationXMLBlueprint { ID = HOOK_ID, Name = "Start" });
		}

		public static ConversationXMLBlueprint Own(GameObject go)
		{
			ConversationScript script = go.GetPart<ConversationScript>();
			if (script == null)
				return null;
			if (script.Blueprint != null)
				return script.Blueprint;
			if (string.IsNullOrEmpty(script.ConversationID) || !Conversation.Blueprints.TryGetValue(script.ConversationID, out ConversationXMLBlueprint source))
				return null;
			ConversationXMLBlueprint copy = DeepCopy(source);
			copy.ID = script.ConversationID + "::" + go.ID;
			copy.Children ??= new List<ConversationXMLBlueprint>();
			script.Blueprint = copy;
			go.SetStringProperty("SuppressPowerSwitchTwiddle", "1");
			return copy;
		}

		public static bool AddToStart(ConversationXMLBlueprint conv, List<ConversationXMLBlueprint> choices)
		{
			ConversationXMLBlueprint start = conv?.Children?.FirstOrDefault(c => c.Name == "Node" && c.ID == "Start")
				?? conv?.Children?.FirstOrDefault(c => c.Name == "Start" && c.ID != HOOK_ID);
			if (start == null || choices.IsNullOrEmpty())
				return false;
			start.Children ??= new List<ConversationXMLBlueprint>();
			int at = start.Children.FindLastIndex(c => c.Name == "Choice");
			if (at < 0)
				at = start.Children.Count;
			start.Children.InsertRange(at, choices);
			return true;
		}

		private static void After(GameObject go)
		{
			ConversationXMLBlueprint conv = go.GetPart<ConversationScript>()?.Blueprint;
			if (conv?.Children == null)
				return;
			ConversationXMLBlueprint hook = conv.Children.FirstOrDefault(c => c.ID == HOOK_ID);
			if (hook == null)
				return;
			conv.Children.Remove(hook);
			if (!AddToStart(conv, hook.Children))
				return;
			Helpers.VerifyLog("QUEST", $"{go.GetReferenceDisplayName()} in {go.CurrentZone?.ZoneID} carries {hook.Children.Count} quest choices");
		}

		private static ConversationXMLBlueprint DeepCopy(ConversationXMLBlueprint source)
		{
			var copy = new ConversationXMLBlueprint(source)
			{
				Cardinal = source.Cardinal,
				Qualifier = source.Qualifier
			};
			if (!source.Children.IsNullOrEmpty())
				copy.Children = source.Children.Select(DeepCopy).ToList();
			return copy;
		}
	}

	public class Cleo_TerraFirma_WarrenQuestPlacer : ZoneBuilderSandbox
	{
		public string deliveryItemID;
		public string Mode;
		public string Owner;

		public bool BuildZone(Zone zone)
		{
			base.zone = zone;
			GameObject item = The.ZoneManager.GetCachedObjects(deliveryItemID);
			if (Mode == "leader")
			{
				WarrenCarver.FloorByZone.TryGetValue(zone.ZoneID, out Dictionary<int, bool> den);
				string seat = PlaceOnFloor(zone, item, den);
				Helpers.VerifyLog("QUEST", $"clear-out leader {item.DisplayNameOnlyDirectAndStripped} in {zone.ZoneID}: {seat ?? "NOT PLACED"}");
				return true;
			}
			item.SetIntProperty("norestock", 1);
			item.SetIntProperty(Cleo_TerraFirma_CrewShift.RESERVED, 1);
			item.SetImportant(true);
			if (!string.IsNullOrEmpty(Owner))
				item.Physics.Owner = Owner;
			WarrenCarver.FloorByZone.TryGetValue(zone.ZoneID, out Dictionary<int, bool> floor);
			string where = Mode == "fetch" ? PlaceInContainer(zone, item, floor) : PlaceAsStatue(zone, item, floor);
			if (where == null)
				where = PlaceOnFloor(zone, item, floor);
			Helpers.VerifyLog("QUEST", $"{Mode} target {item.DisplayNameOnlyDirectAndStripped} ({deliveryItemID}) in {zone.ZoneID}: {where ?? "NOT PLACED"}");
			return true;
		}

		private static bool InWarren(Cell c, Dictionary<int, bool> floor, bool roomOnly = false)
			=> c != null && (floor == null
				|| (floor.TryGetValue(c.X + c.Y * c.ParentZone.Width, out bool room) && (room || !roomOnly)));

		private static string PlaceInContainer(Zone zone, GameObject item, Dictionary<int, bool> floor)
		{
			var boxes = zone.GetObjects(o => o.HasPart<Container>() && o.Inventory != null && !o.IsCreature
				&& InWarren(o.CurrentCell, floor));
			if (boxes.Count == 0)
				return null;
			GameObject box = boxes.GetRandomElement();
			box.Inventory.AddObject(item);
			box.SetIntProperty(Cleo_TerraFirma_CrewShift.RESERVED, 1);
			return $"in {box.Blueprint} at {box.CurrentCell.X},{box.CurrentCell.Y}";
		}

		private static string PlaceAsStatue(Zone zone, GameObject item, Dictionary<int, bool> floor)
		{
			int all = 0, outside = 0, taken = 0, inscribed = 0, boxedIn = 0;
			var statues = new List<GameObject>();
			foreach (GameObject o in zone.GetObjects(o => (o.HasPart<RandomStatue>() || XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuests.IsMonument(o)) && o.CurrentCell != null))
			{
				all++;
				if (!InWarren(o.CurrentCell, floor)) { outside++; continue; }
				if (o.IsImportant() || o.HasProperty(Cleo_TerraFirma_CrewShift.RESERVED) || o.HasPart<DynamicQuestTarget>()) { taken++; continue; }
				if (!string.IsNullOrEmpty(o.GetPart<Cleo_TerraFirma_Etchable>()?.Inscription)) { inscribed++; continue; }
				if (!o.CurrentCell.GetAdjacentCells().Any(c => InWarren(c, floor) && !c.IsSolid())) { boxedIn++; continue; }
				statues.Add(o);
			}
			Helpers.VerifyLog("QUEST", $"statue swap in {zone.ZoneID}: {all} statues, {outside} outside the warren, {taken} important/reserved/quest target, {inscribed} inscribed, {boxedIn} boxed in, {statues.Count} usable");
			bool monument = XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuests.IsMonument(item);
			var pool = statues.Where(XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuests.IsMonument).ToList();
			if (!monument || pool.Count == 0)
				pool = statues.Where(o => !XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuests.IsMonument(o)).ToList();
			if (pool.Count == 0)
				return null;
			GameObject old = pool.GetRandomElement();
			Cell cell = old.CurrentCell;
			string was = old.Blueprint;
			old.Obliterate();
			cell.AddObject(item);
			return $"in place of {was} at {cell.X},{cell.Y}";
		}

		private string PlaceOnFloor(Zone zone, GameObject item, Dictionary<int, bool> floor)
		{
			List<Cell> cells = floor == null ? new List<Cell>() : zone.GetEmptyCells(c => InWarren(c, floor, roomOnly: true));
			if (cells.Count == 0 && floor != null)
				cells = zone.GetEmptyCells(c => InWarren(c, floor));
			if (cells.Count == 0)
				cells = zone.GetEmptyReachableCells();
			if (cells.Count == 0)
				cells = zone.GetEmptyCells();
			if (cells.Count == 0)
				return null;
			Cell cell = cells.GetRandomElement();
			cell.AddObject(item);
			EnsureCellReachable(zone, cell);
			return $"on the floor at {cell.X},{cell.Y}";
		}
	}
}

#if !VERSION_1_0
namespace XRL.World.Text.Delegates
{
	[XRL.World.Text.Attributes.HasVariableReplacer]
	public static class Cleo_TerraFirma_WarrenQuestReplacers
	{
		[XRL.World.Text.Attributes.VariableReplacer]
		public static string SacredThing(VariableContext Context, XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuestContext Quest)
			=> Quest.getSacredThings().GetRandomElement();

		[XRL.World.Text.Attributes.VariableReplacer]
		public static string OriginZone(VariableContext Context, XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuestContext Quest)
			=> Quest.getQuestOriginZone();
	}
}
#endif
