using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Cobblefur bracer: close-range throwing accuracy, the low-tech ancestor of the Parabolic
	/// Muscular Subroutine (base's ONLY granter of this property; same Arm slot, 100 vs our
	/// draft 25). Engine truth (decomp 2026-08-13, banked in TODO): within throw range the
	/// property scales aim scatter by (100-total)/100 AND skips the fall-one-short distance
	/// variance branch entirely; the engine takes max(item-line, skill-line) so no skill
	/// double-dip; totals past 100 flip the multiplier NEGATIVE.
	///
	/// CAP-AWARE LEND/RETURN (user catch 2026-08-13: "chimeras can have potentially unlimited
	/// arms" -- arm count is NOT a natural cap): on equip this adds only min(Bonus, 100-current)
	/// and REMEMBERS what it added (serialized); unequip returns exactly the receipt. Any equip
	/// order, any limb count, any mid-fight dismemberment unwinds cleanly; the total never
	/// crosses 100. A limb-stacked chimera reaching exactly 100 through sheer dead monkey is
	/// the intended joke -- the per-piece Baboons rep hit is the price.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_CobblefurBracer : IPart
	{
		public const string PROPERTY = "CloseThrowRangeAccuracyBonus";
		public const int CAP = 100;

		/// <summary>Accuracy granted by one bracer. DRAFT 25 (numbers sitting owed; safe band
		/// 25-35 -- see cap logic, which protects the ceiling regardless).</summary>
		public int Bonus = 25;

		/// <summary>The receipt: exactly how much this bracer actually lent (0 if equipped at
		/// the cap). Serialized so save/load keeps the books straight.</summary>
		public int AppliedAmount;

		public override bool SameAs(IPart p)
		{
			return false; // stateful (per-instance receipt)
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != EquippedEvent.ID && ID != UnequippedEvent.ID)
				return ID == GetShortDescriptionEvent.ID;
			return true;
		}

		public override bool HandleEvent(EquippedEvent E)
		{
			if (E.Item.IsEquippedProperly() && AppliedAmount == 0)
			{
				int current = E.Actor.GetIntProperty(PROPERTY);
				int room = Math.Max(0, CAP - current);
				AppliedAmount = Math.Min(Bonus, room);
				if (AppliedAmount > 0)
					E.Actor.ModIntProperty(PROPERTY, AppliedAmount);
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("BRAC", $"{E.Actor.Blueprint} equips bracer: lent {AppliedAmount} (property now {E.Actor.GetIntProperty(PROPERTY)})");
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnequippedEvent E)
		{
			if (AppliedAmount > 0)
			{
				E.Actor.ModIntProperty(PROPERTY, -AppliedAmount, RemoveIfZero: true);
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("BRAC", $"{E.Actor.Blueprint} unequips bracer: returned {AppliedAmount} (property now {E.Actor.GetIntProperty(PROPERTY)})");
				AppliedAmount = 0;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			E.Postfix.AppendRules($"+{Bonus} close-range throwing accuracy (multiple sources stack up to +{CAP}: within your throw range, aim scatter shrinks by the total percentage and throws never fall short)");
			return base.HandleEvent(E);
		}
	}
}
