using System;

namespace XRL.World.Parts.Mutation
{
	[Serializable]
	public class Cleo_TerraFirma_CalcifiedWrists : BaseMutation
	{
		public const int RANGE_PENALTY = 2;
		public const int MIN_RANGE = 2;
		public const int AIM_MULTIPLIER = 2;
		public const int MELEE_TO_HIT = 1;

		public override bool CanLevel() => false;

		public override string GetDescription()
		{
			return "You have stony joints.\n\n" +
				"Your throws have -" + RANGE_PENALTY + " range.\n" +
				"Your throws are twice as inaccurate.\n" +
				"-" + MELEE_TO_HIT + " to hit with melee attacks.";
		}

		public override string GetLevelText(int Level) => "";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == PooledEvent<GetThrowProfileEvent>.ID || ID == PooledEvent<GetToHitModifierEvent>.ID;
		}

		public override bool HandleEvent(GetThrowProfileEvent E)
		{
			if (E.Actor == ParentObject)
			{
				E.Range = Math.Max(MIN_RANGE, E.Range - RANGE_PENALTY);
				E.AimVariance *= AIM_MULTIPLIER;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetToHitModifierEvent E)
		{
			if (E.Actor == ParentObject && E.Checking == "Actor" && E.Melee)
				E.Modifier -= MELEE_TO_HIT;
			return base.HandleEvent(E);
		}
	}
}
