using System;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	/// <summary>
	/// Paralyzes on hit (user 2026-10-18: the stillvine stinger should really paralyze). Base has
	/// PoisonOnHit and ConfuseOnHit for weapons but no paralysis twin: the only base paralysis on
	/// hit is StingerParalysisProperties, which fires only for a wielder with the Stinger
	/// mutation. This is base PoisonOnHit's shape (same events, same special-effect chance hook,
	/// a Toughness save) applying base Paralyzed the way the stinger does.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_ParalyzeOnHit : IPart
	{
		public int Chance = 100;

		/// <summary>Toughness save difficulty.</summary>
		public string Strength = "15";

		public string Duration = "1d3";

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
				if (GameObject.Validate(ref defender)
					&& GetSpecialEffectChanceEvent.GetFor(attacker, ParentObject, "Part Cleo_TerraFirma_ParalyzeOnHit Activation", Chance, defender, projectile).in100()
					&& !defender.MakeSave("Toughness", Strength.RollCached(), null, null, "Stinger Injected Poison Paralysis", IgnoreNaturals: false, IgnoreNatural1: false, IgnoreNatural20: false, IgnoreGodmode: false, ParentObject))
				{
					defender.ApplyEffect(new Paralyzed(Duration.RollCached(), -1));
				}
			}
			return base.FireEvent(E);
		}
	}
}
