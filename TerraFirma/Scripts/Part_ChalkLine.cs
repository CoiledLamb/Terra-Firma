using System;
using XRL.UI;

namespace XRL.World.Parts
{
	/// <summary>
	/// A drawn chalk line on the floor (box-of-chalk sitting, 2026-08-22; all four forks user-
	/// ruled). Carries its direction sprite (the eight chalkline*.png user sprites) and a "smear"
	/// action open to anyone adjacent: player-drawn lines are unowned and smear clean, while
	/// camp-painted lines are expected to ship with Physics Owner set, so smearing one calls the
	/// base ownership machinery (BroadcastForHelp) and the guild answers - "smearing a chalk line
	/// in a stonecarver's camp is a hostile action, but your own are fine", all through native
	/// ownership, zero new crime code.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_ChalkLine : IPart
	{
		public const string SMEAR_COMMAND = "Cleo_TerraFirma_SmearChalkLine";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID;
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			E.AddAction("Smear", "smear away", SMEAR_COMMAND, null, 's', FireOnActor: false);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == SMEAR_COMMAND && E.Actor != null)
			{
				string owner = ParentObject.Physics?.Owner;
				if (!owner.IsNullOrEmpty())
				{
					// owned chalk: the smear is vandalism and the owner hears about it
					ParentObject.Physics.BroadcastForHelp(E.Actor);
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"owned chalk line ({owner}) smeared by {E.Actor.Blueprint}, help broadcast");
				}
				else
				{
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"chalk line smeared by {E.Actor.Blueprint}");
				}
				if (E.Actor.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage("You smear the chalk line away."); // DRAFT
				}
				E.Actor.UseEnergy(1000, "Item SmearChalkLine");
				ParentObject.Destroy();
				E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
		}
	}
}
