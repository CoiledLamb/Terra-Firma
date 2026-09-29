using System;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	/// <summary>
	/// Mudskin girdle (C01, shaped 2026-08-22): each bleed tick the wearer takes hardens the
	/// mud one step, shown as the scabbed status. Bleed ticks are recognized by the damage
	/// attribute base Bleeding stamps on its own ticks ("Bleeding Unavoidable",
	/// Bleeding.DamageAttributes), so internal bleeding and liquid variants count too.
	/// TookDamageEvent cascades to equipment (cascade 17 = equipment, except thrown), so no
	/// wearer registration is needed.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_MudskinGirdle : IPart
	{
		/// <summary>Most AV the hardening can add (08-22 shape: +2).</summary>
		public int Cap = 2;

		/// <summary>Turns between flake steps, restarted by every tick. CLAUDE PROPOSAL 20.</summary>
		public int FlakeInterval = 20;

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != TookDamageEvent.ID && ID != UnequippedEvent.ID)
				return ID == GetShortDescriptionEvent.ID;
			return true;
		}

		public override bool HandleEvent(TookDamageEvent E)
		{
			GameObject wearer = ParentObject.Equipped;
			if (wearer != null && E.Object == wearer && ParentObject.IsEquippedProperly()
				&& E.Damage != null && E.Damage.Amount > 0 && E.Damage.HasAttribute("Bleeding"))
			{
				if (!wearer.TryGetEffect<Cleo_TerraFirma_Scabbed>(out var scab))
				{
					scab = new Cleo_TerraFirma_Scabbed { Cap = Cap, FlakeInterval = FlakeInterval };
					if (!wearer.ApplyEffect(scab))
						return base.HandleEvent(E);
				}
				scab.Harden();
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnequippedEvent E)
		{
			E.Actor?.RemoveEffect<Cleo_TerraFirma_Scabbed>();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			// rules line approved 2026-09-29
			E.Postfix.AppendRules("+1 AV each turn you take bleeding damage, up to +" + Cap + ". Loses 1 AV for every " + FlakeInterval + " turns you go without bleeding.");
			return base.HandleEvent(E);
		}
	}
}
