using System;
using System.Collections.Generic;
using System.Linq;
using Cleo.TerraFirma.Scripts;
using Genkit;
using Qud.API;
using XRL;
using XRL.Language;
using XRL.World;
using XRL.World.Parts;
using XRL.World.WorldBuilders;

namespace XRL.CharacterCreation
{
	public class Cleo_TerraFirma_PorterKit : ICustomChargenClass
	{
		public void BuildCharacterBody(GameObject body)
		{
			body?.RequirePart<Cleo_TerraFirma_PorterErrand>();
		}

		public IEnumerable<string> GetChargenInfo()
		{
			yield break;
		}
	}
}

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_PorterErrand : IPart
	{
		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == SingletonEvent<BeginTakeActionEvent>.ID;
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			string zoneID = ParentObject.IsPlayer() ? ParentObject.CurrentZone?.ZoneID : null;
			if (Cleo_TerraFirma_PorterErrands.WorldLocation(zoneID) != null)
			{
				GameObject player = ParentObject;
				player.RemovePart(this);
				try
				{
					Cleo_TerraFirma_PorterErrands.Start(player);
				}
				catch (Exception x)
				{
					MetricsManager.LogException("Cleo_TerraFirma_PorterErrand", x);
				}
			}
			return base.HandleEvent(E);
		}
	}
}

namespace XRL
{
	[Serializable]
	public class Cleo_TerraFirma_PorterErrandSystem : IQuestSystem
	{
		public override void ShowStartPopup()
		{
			Quest quest = Quest;
			if (quest != null)
				XRL.Messages.MessageQueue.AddPlayerMessage("You have received a new quest, " + quest.DisplayName + "!");
		}
	}
}

namespace Cleo.TerraFirma.Scripts
{
	public static class Cleo_TerraFirma_PorterErrands
	{
		public const string SENDER_PLACE = "the Sunderlies";
		public const string SENDER = "the guild";

		public const string PARCEL = "Cleo_TerraFirma_PorterStone";

		public class Site
		{
			public string ZoneID, Name, SecretID;
		}

		public static List<Site> InUse()
		{
			var result = new List<Site>();
			string all = The.Game?.GetStringGameState(Cleo_TerraFirma_WarrenQuests.IN_USE_STATE, null);
			if (string.IsNullOrEmpty(all))
				return result;
			foreach (string line in all.Split('\n'))
			{
				string[] f = line.Split('|');
				if (f.Length == 3 && !string.IsNullOrEmpty(f[0]))
					result.Add(new Site { ZoneID = f[0], Name = f[1], SecretID = f[2] });
			}
			return result;
		}

		public static Location2D WorldLocation(string ZoneID)
		{
			if (string.IsNullOrEmpty(ZoneID))
				return null;
			string[] f = ZoneID.Split('.');
			if (f.Length != 6 || f[0] != "JoppaWorld"
				|| !int.TryParse(f[1], out int px) || !int.TryParse(f[2], out int py)
				|| !int.TryParse(f[3], out int zx) || !int.TryParse(f[4], out int zy))
				return null;
			return Location2D.Get(px * 3 + zx, py * 3 + zy);
		}

		public static Site Pick(string FromZoneID, List<Site> Sites = null)
		{
			if (WorldLocation(FromZoneID) == null)
				return null;
			List<Site> open = Candidates(FromZoneID, Sites ?? InUse());
			return open.Count == 0 ? null : open.GetRandomElement();
		}

		public static List<Site> Candidates(string FromZoneID, List<Site> Sites)
		{
			return Sites.Where(s => s.ZoneID != FromZoneID && WorldLocation(s.ZoneID) != null && !The.ZoneManager.IsZoneBuilt(s.ZoneID)).ToList();
		}

		public static GameObject Parcel()
		{
			GameObject item = GameObject.Create(PARCEL);
			Cleo_TerraFirma_WarrenQuests.Paint(item);
			item.SetIntProperty("norestock", 1);
			item.SetIntProperty("QuestItem", 1);
			item.SetStringProperty("NeverStack", "1");
			Cleo_TerraFirma_QuestBurden.Roll(item, "heavy");
			item.SetStringProperty("HasPregeneratedName", "yes");
			item.Render.DisplayName = Cleo_TerraFirma_WarrenQuests.ADJECTIVE + " " + item.Render.DisplayName;
			return item;
		}

		public static Quest Start(GameObject Player)
		{
			string here = Player?.CurrentZone?.ZoneID;
			Site target = Pick(here);
			if (target == null)
			{
				int noted = InUse().Count;
				string why = noted == 0 ? "no in-use warrens noted (world made before this build?)"
					: WorldLocation(here) == null ? $"can't place the player's zone {here ?? "none"} on the world map"
					: "every in-use warren is already built";
				Helpers.VerifyLog("PORTER", $"no errand: {why} ({noted} noted)");
				return null;
			}
			string questID = Guid.NewGuid().ToString();
			GameObject receiver = Cleo_TerraFirma_WarrenQuests.PresetOverseer(target.ZoneID);
			int tier = Math.Max(1, The.ZoneManager.GetZoneTier(target.ZoneID));
			var ctx = new Cleo_TerraFirma_WarrenQuestContext
			{
				GiverZoneID = null,
				GiverName = SENDER_PLACE,
				GiverOverseerName = SENDER,
				GiverOverseerBareName = SENDER,
				TargetZoneID = target.ZoneID,
				TargetName = target.Name,
				TargetSecretID = target.SecretID,
				TargetOccupancy = "in use",
				Verb = "deliver",
				QuestID = questID,
				ReceiverOverseerID = receiver?.ID,
				ReceiverOverseerName = receiver?.DisplayNameOnlyDirectAndStripped,
				questNumber = 0,
				tier = tier
			};
			GameObject parcel = Parcel();
			ctx.ItemID = parcel.ID;
			parcel.MakeUnderstood();
			Player.ReceiveObject(parcel);
			The.Game.SetIntGameState(Cleo_TerraFirma_WarrenQuests.TARGET_STATE + target.ZoneID, 1);
			The.ZoneManager.AddZoneBuilder(target.ZoneID, 6000, "Cleo_TerraFirma_WarrenQuestReceiver", "questContext", ctx);

			string targetName = ConsoleLib.Console.ColorUtility.StripFormatting(target.Name);
			string itemName = parcel.DisplayNameOnlyDirectAndStripped;
			string receiverName = ctx.ReceiverOverseerName ?? ("the overseer of " + targetName);
			var quest = new Quest { ID = questID, StepsByID = new Dictionary<string, QuestStep>(), SystemType = typeof(XRL.Cleo_TerraFirma_PorterErrandSystem) };
			quest.Name = Grammar.MakeTitleCase("Deliver to " + receiverName);
			var step = new QuestStep
			{
				ID = "a_deliver",
				Name = Grammar.MakeTitleCase("Deliver to " + receiverName),
				Text = "Bring " + parcel.the + itemName + " to " + receiverName + " at " + targetName + ".",
				XP = 100,
				Finished = false
			};
			quest.StepsByID.Add(step.ID, step);
			quest.Manager = new XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuestManager
			{
				MyQuestID = questID,
				Verb = "deliver",
				TargetZoneID = target.ZoneID,
				TargetSecretID = target.SecretID
			};
			quest.dynamicReward = new DynamicQuestReward
			{
				StepXP = tier * 1000,
				rewards = { new Cleo_TerraFirma_WarrenQuestReward(target.ZoneID, target.Name) }
			};
			DynamicQuestsGameState.Add(quest);
			The.Game.StartQuest(questID, SENDER, SENDER_PLACE);

			JournalMapNote note = JournalAPI.GetMapNote(target.SecretID);
			if (note != null && !note.Revealed)
				JournalAPI.RevealMapNote(note, silent: true);
			Helpers.VerifyLog("PORTER", $"errand: {itemName} to {receiverName} at {targetName} ({target.ZoneID}, tier {tier}, {questID})");
			return The.Game.TryGetQuest(questID, out Quest started) ? started : null;
		}
	}
}
