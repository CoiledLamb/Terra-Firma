using System;

namespace XRL.World.Effects
{
	[Serializable]
	public class Cleo_TerraFirma_ChalkedHands : Effect
	{
		public const string NAME = "{{Y|chalked hands}}";
		public const string LOST_MESSAGE = "The chalk on your hands washes away.";
		public const int DURATION = 100;
		public const int RANGE = 2;
		public const int TO_HIT = 1;

		public int AppliedRange;
		public int AppliedHit;

		public Cleo_TerraFirma_ChalkedHands()
		{
			Duration = DURATION;
			DisplayName = NAME;
		}

		public override int GetEffectType()
		{
			return TYPE_CONTACT | TYPE_MINOR | TYPE_REMOVABLE | TYPE_VOLUNTARY;
		}

		public override bool UseStandardDurationCountdown()
		{
			return true;
		}

		public override string GetDetails()
		{
			return "+" + RANGE + " throwing range\n+" + TO_HIT + " to hit";
		}

		public override bool Apply(GameObject Object)
		{
			Cleo_TerraFirma_ChalkedHands existing = Object.GetEffect<Cleo_TerraFirma_ChalkedHands>();
			if (existing != null)
			{
				existing.Duration = Math.Max(existing.Duration, Duration);
				return false;
			}
			Object.ModIntProperty("ThrowRangeBonus", RANGE, RemoveIfZero: true);
			Object.ModIntProperty("HitBonus", TO_HIT, RemoveIfZero: true);
			AppliedRange = RANGE;
			AppliedHit = TO_HIT;
			return true;
		}

		public override void Remove(GameObject Object)
		{
			if (AppliedRange != 0)
				Object.ModIntProperty("ThrowRangeBonus", -AppliedRange, RemoveIfZero: true);
			if (AppliedHit != 0)
				Object.ModIntProperty("HitBonus", -AppliedHit, RemoveIfZero: true);
			AppliedRange = 0;
			AppliedHit = 0;
			base.Remove(Object);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == EnteredCellEvent.ID;
		}

		public override bool HandleEvent(EnteredCellEvent E)
		{
			if (Duration > 0 && E.Cell != null && !Object.IsFlying && E.Cell.HasOpenLiquidVolume())
			{
				Duration = 0;
				if (Object.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage(LOST_MESSAGE);
				}
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", "chalked hands lost to liquid");
			}
			return base.HandleEvent(E);
		}
	}
}
