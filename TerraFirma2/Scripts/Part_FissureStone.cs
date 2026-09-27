using System;
using Cleo.TerraFirma.Scripts;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	/// <summary>
	/// The hewer's throwable charge (role-texture pitch 2026-08-23; payload RULED same day:
	/// pulses only, never the redoubt ring; Level 2, stones 2-3 per hewer). Grammar is the
	/// goatfolk sower's seed exactly (base Creatures.xml:4663): BaseThrownWeapon plus an
	/// IGrenade part riding in plain inventory, and the stock combat AI does the throwing.
	/// IGrenade's AfterThrownEvent hands DoDetonate the landing cell; the pulse core is
	/// Mutation_Fissure's own RunPulses, extracted for this second caller, so the stone and
	/// the mutation can never drift apart. The stone is spent in the act.
	/// Inherited IGrenade behaviors, tracker-flagged rather than chosen: an in-inventory
	/// Detonate action for whoever holds one, and sympathetic detonation when the stone
	/// itself takes damage. Indirect detonations exempt nobody and credit nobody.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_FissureStonePart : IGrenade
	{
		public int Level = 2;

		[NonSerialized]
		private bool detonating;

		public override bool SameAs(IPart p)
		{
			if (p is Cleo_TerraFirma_FissureStonePart other && other.Level != Level)
				return false;
			return base.SameAs(p);
		}

		protected override bool DoDetonate(Cell C, GameObject Actor = null, GameObject ApparentTarget = null, bool Indirect = false)
		{
			if (detonating)
				return true;
			detonating = true;
			try
			{
				PlayWorldSound(GetPropertyOrTag("DetonatedSound"), 1f, 0f, Combat: true);
				DidX("shatter", null, "!");
				GameObject actor = Indirect ? null : Actor;
				Helpers.VerifyLog("FSTONE", $"detonation @({C.X},{C.Y}): thrower={Actor?.Blueprint ?? "none"}, indirect={Indirect}, Lv{Level}, dice {Cleo_TerraFirma_Fissure.GetPulseDice(Level, 1)}/{Cleo_TerraFirma_Fissure.GetPulseDice(Level, 2)}/{Cleo_TerraFirma_Fissure.GetPulseDice(Level, 3)}");
				Cleo_TerraFirma_Fissure.RunPulses(C, actor, Actor ?? ParentObject, Level, "FSTONE");
				ParentObject.Destroy(null, Silent: true);
			}
			finally
			{
				detonating = false;
			}
			return true;
		}
	}
}
