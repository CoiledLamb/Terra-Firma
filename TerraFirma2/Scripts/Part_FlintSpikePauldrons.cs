using System;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	/// <summary>
	/// Flint-spike pauldrons (C01, shaped 2026-08-22): any adjacent melee attacker that lands
	/// damage must save or start bleeding. Dials are ModSpiked's own (1d3, save 20 + 2 x Tier);
	/// bloodless attackers shrug it off through Bleeding.Apply's Bleeds gate. Hook is Quills'
	/// TookDamageEvent retaliation, cascaded to equipment. Melee gate: attacker adjacent, not
	/// indirect (bleed ticks), no projectile, not thrown (the engine stamps "Thrown" on
	/// thrown-item damage), so the mod's rock throwers are never taxed by their own armor.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_FlintSpikePauldrons : IPart
	{
		public string Damage = "1d3";

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != TookDamageEvent.ID)
				return ID == GetShortDescriptionEvent.ID;
			return true;
		}

		public int GetSaveTarget()
		{
			return 20 + 2 * ParentObject.GetTier();
		}

		public override bool HandleEvent(TookDamageEvent E)
		{
			GameObject wearer = ParentObject.Equipped;
			GameObject attacker = E.Actor;
			if (wearer != null && E.Object == wearer && ParentObject.IsEquippedProperly()
				&& attacker != null && attacker != wearer && GameObject.Validate(attacker)
				&& !E.Indirect && E.Projectile == null
				&& E.Damage != null && E.Damage.Amount > 0
				&& !E.Damage.HasAttribute("Thrown") && !E.Damage.HasAttribute("reflected")
				&& attacker.CurrentCell != null && wearer.CurrentCell != null
				&& attacker.CurrentCell.IsAdjacentTo(wearer.CurrentCell))
			{
				attacker.ApplyEffect(new Bleeding(Damage, GetSaveTarget(), wearer));
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			// DRAFT rules line, user text owed
			E.Postfix.AppendRules("Adjacent attackers who strike you in melee must make a Toughness save (difficulty " + GetSaveTarget() + ") or start bleeding.");
			return base.HandleEvent(E);
		}
	}
}
