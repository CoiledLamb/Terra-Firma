using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Rides every sculpted stone (subscriber report 2026-08-12: throw until dry, pick a stone
	/// back up off the ground, and nothing re-arms -- the pickup path missed every ruled re-arm
	/// trigger, so the stone sat in the pack until end-of-turn reabsorbed it, and cycling the
	/// toggle was the only way back to armed). Hands a stone the carrier just picked up OFF THE
	/// GROUND straight back to an empty thrown slot.
	///
	/// CORRECTED SAME DAY (user live-catch: swapping another thrown weapon over a held stone
	/// deleted the stone): TakenEvent alone is NOT pickup-only -- the unequip-to-inventory flow
	/// ends in eCommandFreeTakeObject, a CommandTakeObject with EnergyCost 0, so displaced and
	/// deliberately-unequipped equipment ALSO arrives via PerformTake/TakenEvent. Reacting to
	/// those re-equipped the stone into the very slot the engine was clearing and the swap fight
	/// destroyed it. The discriminator is BeginBeingTaken, which fires on the item BEFORE it
	/// leaves its old context: in a cell = true ground pickup (act); equipped or in a container
	/// = a shuffle, not a stoop (stay out -- the 2026-07-23 reabsorb ruling governs those).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneRearmOnTakePart : IPart
	{
		/// <summary>True while a take that started from a cell is in flight. Transient by
		/// design: the BeginBeingTaken/TakenEvent pair resolves within one action.</summary>
		[NonSerialized]
		private bool CameFromCell;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == TakenEvent.ID;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("BeginBeingTaken");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "BeginBeingTaken")
				CameFromCell = ParentObject.CurrentCell != null;
			return base.FireEvent(E);
		}

		public override bool HandleEvent(TakenEvent E)
		{
			if (CameFromCell)
			{
				CameFromCell = false;
				E.Actor?.GetPart<Mutation.Cleo_TerraFirma_EarthenBarrage>()?.OnStoneTaken(ParentObject);
			}
			return base.HandleEvent(E);
		}
	}
}
