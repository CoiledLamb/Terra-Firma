using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Granitefur bracer (renamed from cobblefur 2026-08-22): +2 THROW RANGE.
	/// REWORKED same day by user ruling ("swap the bonus to +2 throw range and dodge the
	/// problem altogether") after the accuracy exam found an order-dependent overflow: the
	/// old CloseThrowRangeAccuracyBonus lend could be pushed past 100 by the Parabolic
	/// Muscular Subroutine's flat non-cap-aware +100, flipping the scatter multiplier
	/// negative. ThrowRangeBonus has no such cliff - GetThrowProfileEvent reads the actor's
	/// int property straight into Range (decomp line 62), linear sum, safe to stack.
	/// The whole cap-aware lend/return receipt system is deleted with the problem.
	/// Same property family as the Parabolic Muscular Subroutine's ThrowRangeBonus:2, so
	/// the bracer remains its low-tech ancestor, now on the range half instead.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_CobblefurBracer : IPart
	{
		public const string PROPERTY = "ThrowRangeBonus";

		/// <summary>Throw range granted. USER NUMBER 2026-08-22 ("+2 throw range").</summary>
		public int Bonus = 2;

		public override bool SameAs(IPart p)
		{
			return p is Cleo_TerraFirma_CobblefurBracer o && o.Bonus == Bonus && base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != EquippedEvent.ID && ID != UnequippedEvent.ID)
				return ID == GetShortDescriptionEvent.ID;
			return true;
		}

		public override bool HandleEvent(EquippedEvent E)
		{
			if (E.Item.IsEquippedProperly())
			{
				E.Actor.ModIntProperty(PROPERTY, Bonus);
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("BRAC", $"{E.Actor.Blueprint} equips bracer: throw range +{Bonus} (property now {E.Actor.GetIntProperty(PROPERTY)})");
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnequippedEvent E)
		{
			E.Actor.ModIntProperty(PROPERTY, -Bonus, RemoveIfZero: true);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("BRAC", $"{E.Actor.Blueprint} unequips bracer: throw range -{Bonus} (property now {E.Actor.GetIntProperty(PROPERTY)})");
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			// the Parabolic Muscular Subroutine's own BehaviorDescription phrasing
			E.Postfix.AppendRules("Your throw range is increased by " + Bonus + ".");
			return base.HandleEvent(E);
		}
	}
}
