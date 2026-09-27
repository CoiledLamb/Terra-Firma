using System;
using XRL.Rules;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	/// <summary>
	/// Glowstone shard combat rider (user ruling 2026-08-22: "shard can apply phosphorescent on
	/// hit, with a chance to bleed whatever it hits when thrown"). "Phosphorescent" is base
	/// Effects.Luminous VERBATIM - its GetDescription() is literally "{{m|phosphorescent}}",
	/// radius-3 glow, standard duration countdown. Applied on ANY hit (melee WeaponHit + both
	/// thrown events; glow dust rubs off on contact, no penetration needed). The bleed is
	/// thrown-only, needs at least one penetration (Cleo_TerraFirma_DazeOnThrowPensPart's gate),
	/// and rides base Effects.Bleeding, which rolls its own Toughness save on top of the
	/// trigger chance. Chance/duration/save numbers are CLAUDE DIALS, unruled.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GlowstoneShardOnHit : IPart
	{
		/// <summary>Chance in 100 for a penetrating thrown hit to start bleeding. DIAL.</summary>
		public int BleedChance = 50;

		/// <summary>Bleeding effect dice + save target: BleedingOnHit's own defaults. DIAL.</summary>
		public string BleedAmount = "1d2";
		public int BleedSaveTarget = 20;

		public override bool SameAs(IPart p)
		{
			Cleo_TerraFirma_GlowstoneShardOnHit o = p as Cleo_TerraFirma_GlowstoneShardOnHit;
			if (o.BleedChance != BleedChance || o.BleedAmount != BleedAmount || o.BleedSaveTarget != BleedSaveTarget)
			{
				return false;
			}
			return base.SameAs(p);
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("WeaponHit");
			Registrar.Register("ProjectileHit");
			Registrar.Register("ThrownProjectileHit");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "WeaponHit" || E.ID == "ProjectileHit" || E.ID == "ThrownProjectileHit")
			{
				GameObject target = E.GetGameObjectParameter("Defender");
				if (GameObject.Validate(target) && target.IsCreature)
				{
					// glow marks any hit; duration DIAL (Luminous' own food-buff init is 200-700,
					// far too long for a combat debuff)
					if (!target.HasEffect<Luminous>())
					{
						target.ApplyEffect(new Luminous(Stat.Random(30, 60)));
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHARD", $"phosphorescent applied to {target.Blueprint} via {E.ID}");
					}
					if (E.ID != "WeaponHit" && E.GetIntParameter("Penetrations") > 0 && BleedChance.in100())
					{
						GameObject attacker = E.GetGameObjectParameter("Attacker");
						if (target.ApplyEffect(new Bleeding(BleedAmount, BleedSaveTarget, attacker, Stack: false)))
						{
							Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHARD", $"bleed applied to {target.Blueprint} (thrown)");
						}
					}
				}
			}
			return base.FireEvent(E);
		}
	}
}
