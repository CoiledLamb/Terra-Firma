using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Rides every sculpted stone (subscriber report 2026-08-12: throw until dry, pick a stone
	/// back up off the ground, and nothing re-arms -- the pickup path missed every ruled re-arm
	/// trigger, so the stone sat in the pack until end-of-turn reabsorbed it, and cycling the
	/// toggle was the only way back to armed). TakenEvent fires only on the take-from-world flow
	/// (Inventory.cs take path; our own sculpts arrive via ReceiveObject, which sends
	/// AddedToInventoryEvent instead), so this part hears exactly "picked up" and hands the
	/// stone straight back to an empty thrown slot. A DELIBERATE unequip into the pack fires no
	/// TakenEvent: the 2026-07-23 reabsorb ruling keeps governing that path untouched.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneRearmOnTakePart : IPart
	{
		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == TakenEvent.ID;
		}

		public override bool HandleEvent(TakenEvent E)
		{
			E.Actor?.GetPart<Mutation.Cleo_TerraFirma_EarthenBarrage>()?.OnStoneTaken(ParentObject);
			return base.HandleEvent(E);
		}
	}
}
