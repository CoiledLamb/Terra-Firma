using System;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_PetrifyOnHit : IPart
	{
		public int Chance = 100;

		public int FallbackLevel = 10;

		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			if (Object.IsCreature)
			{
				Registrar.Register("AttackerHit");
				return;
			}
			if (Object.IsProjectile)
			{
				Registrar.Register("ProjectileHit");
				return;
			}
			Registrar.Register("WeaponHit");
			Registrar.Register("WeaponThrowHit");
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID.EndsWith("Hit"))
			{
				GameObject attacker = E.GetGameObjectParameter("Attacker");
				GameObject defender = E.GetGameObjectParameter("Defender");
				GameObject projectile = E.GetGameObjectParameter("Projectile");
				if (GameObject.Validate(ref defender) && defender != attacker
					&& GetSpecialEffectChanceEvent.GetFor(attacker, ParentObject, "Part Cleo_TerraFirma_PetrifyOnHit Activation", Chance, defender, projectile).in100())
				{
					Cleo_TerraFirma_GorgonsGaze.TouchPetrify(attacker, defender, FallbackLevel);
				}
			}
			return base.FireEvent(E);
		}
	}
}
