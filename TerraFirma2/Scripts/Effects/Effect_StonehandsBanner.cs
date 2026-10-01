using System;

namespace XRL.World.Effects
{
	/// <summary>
	/// What the warren banner bestows (user 2026-10-01: +2 Willpower to quarriers who can see
	/// it). Base Banner applies it by type name with Duration 7 and refreshes it every 5 turns,
	/// so it never stacks. Shape copied from base WarTrance with one stat shift.
	/// Name "rooted" picked by the user 2026-10-01 ("the opposite of homesick").
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StonehandsBannerEffect : Effect
	{
		public const string NAME = "{{w|rooted}}";

		public Cleo_TerraFirma_StonehandsBannerEffect()
		{
			Duration = 7;
			DisplayName = NAME;
		}

		public override int GetEffectType()
		{
			return 83886082; // WarTrance's type flags
		}

		public override string GetStateDescription()
		{
			return NAME;
		}

		public override string GetDetails()
		{
			return "+2 Willpower";
		}

		public override bool Apply(GameObject Object)
		{
			StatShifter.SetStatShift("Willpower", 2);
			return true;
		}

		public override void Remove(GameObject Object)
		{
			StatShifter.RemoveStatShifts();
			base.Remove(Object);
		}
	}
}
