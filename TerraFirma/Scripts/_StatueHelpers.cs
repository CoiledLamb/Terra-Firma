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
				foreach (BodyPart part in Owner.Body.LoopParts())
				{
					if (part.Equipped != null && !part.Equipped.IsNatural())
						part.Unequip();
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
