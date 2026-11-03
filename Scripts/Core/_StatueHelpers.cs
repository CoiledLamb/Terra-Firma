using System.Collections.Generic;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

namespace Cleo.TerraFirma.Scripts
{
	public static class StatueHelpers
	{
		public static void EntombPossessions(GameObject Owner, GameObject Statue)
		{
			if (Owner == null || Statue == null)
				return;
			if (!HasPossessions(Owner))
				return;
			Statue.RequirePart<Container>();
			Statue.RequirePart<Inventory>();
			if (Owner.Body != null)
			{
				foreach (BodyPart part in new List<BodyPart>(Owner.Body.LoopParts()))
				{
					GameObject equipped = part.Equipped;
					if (equipped == null || equipped.IsNatural())
						continue;
					if (!part.ForceUnequip(Silent: true) && part.Equipped == equipped)
					{
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

		public static bool HasPossessions(GameObject Owner)
		{
			if (Owner.Body != null)
			{
				foreach (BodyPart part in Owner.Body.LoopParts())
				{
					if (part.Equipped != null && !part.Equipped.IsNatural())
						return true;
				}
			}
			return Owner.Inventory != null && Owner.Inventory.GetObjectsDirect().Count > 0;
		}

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
