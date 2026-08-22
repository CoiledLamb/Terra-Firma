using System;
using XRL.Language;
using XRL.Rules;
using XRL.UI;

namespace XRL.World.Parts
{
	/// <summary>
	/// Merged onto base Glowsphere (Merges.xml). Adds a deliberate "break" inventory action that
	/// destroys the sphere and yields 2-3 glowstone shards (user ruling 2026-08-22: "2-3 fragments
	/// per sphere"; break-as-verb over a passive breaking condition was the delegated Claude call,
	/// flagged in TODO). Floating Glowsphere deliberately NOT merged: it inherits LightSource
	/// directly, not Glowsphere, and whether it breaks too is an open fork.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GlowsphereBreak : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_BreakGlowsphere";
		public const string SHARD_BLUEPRINT = "Cleo_TerraFirma_GlowstoneShard";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID;
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			E.AddAction("Break", "break into shards", COMMAND, null, 'b', FireOnActor: false);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == COMMAND && E.Actor != null)
			{
				// destructive and irreversible; a permanent light becomes depleting shards
				if (E.Actor.IsPlayer()
					&& Popup.ShowYesNo("Really shatter " + ParentObject.t() + "? The shards will not last forever.") != DialogResult.Yes) // DRAFT
				{
					return false;
				}
				GameObject sphere = ParentObject.SplitFromStack() ?? ParentObject;
				int count = Stat.Random(2, 3);
				for (int i = 0; i < count; i++)
				{
					E.Actor.ReceiveObject(GameObject.Create(SHARD_BLUEPRINT));
				}
				if (E.Actor.IsPlayer())
				{
					Popup.Show("The glowsphere shatters into " + Grammar.Cardinal(count) + " glowing shards."); // DRAFT
				}
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHARD", $"glowsphere broken into {count} shards by {E.Actor.Blueprint}");
				E.Actor.UseEnergy(1000, "Item BreakGlowsphere");
				sphere.Destroy();
				E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
		}
	}
}
