using System.Collections.Generic;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// Shared logic for rendering creatures down into inert, lootable statues.
	/// Consumers: Gorgon's Gaze (petrify kills), Stoneshaper (natural expiry and permanent-statue swaps).
	/// </summary>
	public static class StatueHelpers
	{
		/// <summary>
		/// Moves every possession (equipped and carried) of <paramref name="Owner"/> into <paramref name="Statue"/>
		/// and ensures the statue is a lootable container (Container + Inventory, same recipe as the base game's Chest).
		/// Natural equipment (claws, fangs) is left alone.
		/// </summary>
		public static void EntombPossessions(GameObject Owner, GameObject Statue)
		{
			if (Owner == null || Statue == null)
				return;
			Statue.RequirePart<Container>();
			Statue.RequirePart<Inventory>();
			if (Owner.Body != null)
			{
				// USER REPORT + FIX 2026-08-15 ("armor should be lootable"): this used to call
				// part.Unequip(), which is the LOW-LEVEL SLOT CLEARER, not an unequip. Decomp
				// BodyPart.Unequip(): its whole body is FindPartsEquipping then ClearEquipped on
				// each part. No command fires and the item is handed to nobody. Equipped objects
				// live on the body part, not in Inventory, so every worn or wielded item was left
				// referenced by nothing while the sweep below moved only the pack. Worn gear was
				// being DESTROYED by petrification, not entombed.
				// ForceUnequip fires CommandForceUnequipObject, the engine's own forced path: it
				// routes the item into the owner's inventory (so the sweep below carries it) and
				// handles multi-slot and cursed gear correctly. Parts list is snapshotted because
				// the unequip event can restructure the body underneath a live enumeration.
				foreach (BodyPart part in new List<BodyPart>(Owner.Body.LoopParts()))
				{
					GameObject equipped = part.Equipped;
					if (equipped == null || equipped.IsNatural())
						continue;
					if (!part.ForceUnequip(Silent: true) && part.Equipped == equipped)
					{
						// Belt: if the engine refuses, clear the slot the old way but hand the
						// item straight to the statue in the same breath, so it can never be
						// orphaned again. (ClearEquipped is not public; Unequip() IS that call.)
						part.Unequip();
						Statue.ReceiveObject(equipped);
					}
				}
			}
			Inventory inv = Owner.Inventory;
			if (inv != null)
			{
				foreach (GameObject item in new List<GameObject>(inv.GetObjectsDirect()))
				{
					inv.RemoveObject(item);
					Statue.ReceiveObject(item);
				}
			}
		}

		/// <summary>
		/// Returns everything held by <paramref name="Statue"/> to <paramref name="Owner"/>'s inventory.
		/// Unwinds <see cref="EntombPossessions"/> when a petrification is cancelled (e.g. the death was prevented).
		/// </summary>
		public static void ReturnPossessions(GameObject Statue, GameObject Owner)
		{
			if (Owner == null || Statue == null)
				return;
			Inventory inv = Statue.Inventory;
			if (inv != null)
			{
				foreach (GameObject item in new List<GameObject>(inv.GetObjectsDirect()))
				{
					inv.RemoveObject(item);
					Owner.ReceiveObject(item);
				}
			}
		}
	}
}
