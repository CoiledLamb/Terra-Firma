using System;
using Cleo.TerraFirma.Scripts;
using XRL.World.Capabilities;

namespace XRL.World.Effects
{
	/// <summary>
	/// The sibling footing buffs (eaten-table comb, RULED 2026-07-31): the two sticky rocks
	/// grant complementary footing. Tanglelith = SURE-FOOTED (modest broad anchor: save bonus
	/// vs forced movement and knockdown); oilstone = SURE-GREASED (absolute narrow one: slip
	/// saves auto-pass -- "oil recognizes its own"). Both ride ModifyDefendingSaveEvent with
	/// SavingThrows.Applicable token matching (the engine SaveModifier idiom). Known accepted
	/// bleed: slip saves carry the "Move" token, so sure-footed's +4 also nudges slips --
	/// sticky feet resisting slips a little is fiction-coherent, recorded, not fought.
	/// The 25% bites (Stuck / slick underfoot) live in the eat channels. Texts DRAFT.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_SureFooted : Effect
	{
		public const int SAVE_BONUS = 4;
		public const string VS = "Move,Knockdown";

		public Cleo_TerraFirma_SureFooted()
		{
			DisplayName = "{{g|sure-footed}}"; // DRAFT name
		}

		public Cleo_TerraFirma_SureFooted(int Duration) : this()
		{
			base.Duration = Duration;
		}

		public override string GetDetails()
		{
			return "+" + SAVE_BONUS + " on saves vs. forced movement and knockdown."; // DRAFT
		}

		public override bool UseStandardDurationCountdown() => true;

		public override bool Apply(GameObject Object)
		{
			Cleo_TerraFirma_SureFooted existing = Object.GetEffect<Cleo_TerraFirma_SureFooted>();
			if (existing != null)
			{
				existing.Duration += Duration;
				Helpers.VerifyLog("LITH", $"surefooted: renewed on {Object.Blueprint}, now {existing.Duration}");
				return false;
			}
			Helpers.VerifyLog("LITH", $"surefooted: anchored {Object.Blueprint} for {Duration} (+{SAVE_BONUS} vs {VS})");
			if (Object.IsPlayer())
				IComponent<GameObject>.AddPlayerMessage("The tar settles into your stride."); // DRAFT
			return base.Apply(Object);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == ModifyDefendingSaveEvent.ID;
		}

		public override bool HandleEvent(ModifyDefendingSaveEvent E)
		{
			if (E.Defender == base.Object && SavingThrows.Applicable(VS, E))
				E.Roll += SAVE_BONUS;
			return base.HandleEvent(E);
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_SureGreased : Effect
	{
		/// <summary>Guaranteed pass: the ruling is "can't slip", not "slips less".</summary>
		public const int SAVE_BONUS = 100;
		public const string VS = "Slip";

		public Cleo_TerraFirma_SureGreased()
		{
			DisplayName = "{{w|sure-greased}}"; // DRAFT name
		}

		public Cleo_TerraFirma_SureGreased(int Duration) : this()
		{
			base.Duration = Duration;
		}

		public override string GetDetails()
		{
			return "Cannot slip on liquids."; // DRAFT
		}

		public override bool UseStandardDurationCountdown() => true;

		public override bool Apply(GameObject Object)
		{
			Cleo_TerraFirma_SureGreased existing = Object.GetEffect<Cleo_TerraFirma_SureGreased>();
			if (existing != null)
			{
				existing.Duration += Duration;
				Helpers.VerifyLog("LITH", $"suregreased: renewed on {Object.Blueprint}, now {existing.Duration}");
				return false;
			}
			Helpers.VerifyLog("LITH", $"suregreased: {Object.Blueprint} slip-proof for {Duration}");
			if (Object.IsPlayer())
				IComponent<GameObject>.AddPlayerMessage("Oil beads on your skin. Your steps hold."); // DRAFT
			return base.Apply(Object);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == ModifyDefendingSaveEvent.ID;
		}

		public override bool HandleEvent(ModifyDefendingSaveEvent E)
		{
			if (E.Defender == base.Object && SavingThrows.Applicable(VS, E))
				E.Roll += SAVE_BONUS;
			return base.HandleEvent(E);
		}
	}
}
