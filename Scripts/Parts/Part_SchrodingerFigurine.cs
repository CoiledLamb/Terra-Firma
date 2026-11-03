using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;
using XRL.UI;
using XRL.World.WorldBuilders;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_SchrodingerFigurine : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_CollapseFigurine";

		public string Materials = "Random Copper Figurine,Random Silver Figurine,Random Gold Figurine,Random Agate Figurine,Random Topaz Figurine,Random Jasper Figurine,Random Amethyst Figurine,Random Sapphire Figurine,Random Emerald Figurine,Random Peridot Figurine";

		public bool QuestReward;

		public override bool SameAs(IPart p) => (p as Cleo_TerraFirma_SchrodingerFigurine)?.QuestReward == QuestReward;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID;
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (E.Actor != null && E.Actor.IsPlayer())
				E.AddAction("Collapse", "collapse", COMMAND, null, 'c', FireOnActor: false, Default: 5);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == COMMAND && E.Actor != null && E.Actor.IsPlayer() && Collapse(E.Actor))
				E.RequestInterfaceExit();
			return base.HandleEvent(E);
		}

		private bool Collapse(GameObject Actor)
		{
			string subject = Cleo_TerraFirma_CarvingPicker.Pick("");
			if (subject == null)
				return false;
			List<string> materials = new List<string>();
			foreach (string m in Materials.Split(','))
			{
				string name = m.Trim();
				if (GameObjectFactory.Factory.GetBlueprintIfExists(name) != null)
					materials.Add(name);
			}
			if (materials.Count == 0)
				return false;
			string material = materials[Stat.Random(0, materials.Count - 1)];
			GameObject figurine = GameObject.Create(material, BeforeObjectCreated: o =>
			{
				RandomFigurine rf = o.GetPart<RandomFigurine>();
				if (rf != null)
					rf.Creature = subject;
			});
			if (figurine == null)
				return false;
			if (QuestReward)
				Cleo_TerraFirma_WarrenQuestReward.MakeRewardFigurine(figurine);
			figurine.MakeUnderstood();
			Helpers.VerifyLog("QUEST", $"schrodinger figurine collapsed: {subject} in {material}");
			string was = ParentObject.T();
			ParentObject.RemoveOne().Obliterate();
			Actor.ReceiveObject(figurine);
			Popup.Show(was + " settles into " + figurine.an() + ".");
			return true;
		}
	}
}
