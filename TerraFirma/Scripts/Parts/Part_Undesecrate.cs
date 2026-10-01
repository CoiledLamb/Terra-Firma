using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Lets the Repair skill lift desecration off a carved statue (B19, user 2026-10-01).
	/// Carved statues inherit base Random Statue, which carries Shrine, so they can be
	/// desecrated (the Desecrate action, or Shrine dropping below 75% HP). Base never removes
	/// ModDesecrated. This copies the Broken effect's repair idiom: answer IsRepairableEvent
	/// with false (repairable) while desecrated, and drop the mod on RepairedEvent, healing to full.
	/// Cost and skill tier come from base Tinkering_Repair unchanged.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_UndesecratePart : IPart
	{
		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == PooledEvent<IsRepairableEvent>.ID
				|| ID == PooledEvent<RepairedEvent>.ID;
		}

		public override bool HandleEvent(IsRepairableEvent E)
		{
			if (ParentObject.HasPart<ModDesecrated>())
				return false;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(RepairedEvent E)
		{
			ParentObject.RemovePart<ModDesecrated>();
			// Full HP too (user 2026-10-01), or Shrine's under-75% rule re-desecrates on the
			// next hit. Only the HP line of base RestorePristineHealth, which also clears effects.
			Statistic hp = ParentObject.GetStat("Hitpoints");
			if (hp != null)
				hp.Penalty = 0;
			return base.HandleEvent(E);
		}
	}
}
