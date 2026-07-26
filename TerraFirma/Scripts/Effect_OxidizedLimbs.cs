using System;
using Cleo.TerraFirma.Scripts;
using XRL.World.Anatomy;
using XRL.World.Parts;

namespace XRL.World.Effects
{
	/// <summary>
	/// The oxidian eaten-raw effect (RULED 2026-07-25, the oxidian conversation: "i'm fine with
	/// oxidian getting rust on hit"). The rust-stone's description was the design all along --
	/// "what it touches, it teaches to decay" -- eating it is being taught: the eater's NATURAL
	/// weapons carry the engine's own RustOnHit (qudzu frond / girshling fang / pseudopod
	/// precedent) for the duration. Discoverability rides the named-visible-effect stack: eat
	/// message + this status entry + RustOnHit's own proc lines. Duration and chance DRAFT.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_OxidizedLimbs : Effect
	{
		/// <summary>Rust chance per connected hit (DRAFT; engine spread runs qudzu unconditional / girshling 50 / pseudopod 100).</summary>
		public const int RUST_CHANCE = 50;

		public Cleo_TerraFirma_OxidizedLimbs()
		{
			DisplayName = "{{r|oxidized}}"; // DRAFT name
		}

		public Cleo_TerraFirma_OxidizedLimbs(int Duration) : this()
		{
			base.Duration = Duration;
		}

		public override string GetDetails()
		{
			// first sentence = the user's own line (2026-07-25), generic-limbs variant
			return "Layers of oxidized metal flake from your limbs. Natural weapons rust what they strike (" + RUST_CHANCE + "% per hit).";
		}

		public override bool UseStandardDurationCountdown() => true;

		public override bool Apply(GameObject Object)
		{
			int stamped = 0;
			Body body = Object?.Body;
			if (body != null)
			{
				foreach (BodyPart bp in body.GetParts())
				{
					GameObject weapon = bp.DefaultBehavior;
					if (weapon != null && weapon.HasPart("MeleeWeapon") && !weapon.HasPart("RustOnHit"))
					{
						weapon.AddPart(new RustOnHit { Chance = RUST_CHANCE });
						stamped++;
					}
				}
			}
			Helpers.VerifyLog("LITH", $"oxidize: RustOnHit ({RUST_CHANCE}%) stamped on {stamped} natural weapon(s) of {Object?.Blueprint} for {Duration}");
			if (stamped == 0 && Object != null && Object.IsPlayer())
				IComponent<GameObject>.AddPlayerMessage("The rust finds nothing of yours to cling to."); // DRAFT (no natural weapons; effect still ticks out harmlessly)
			return base.Apply(Object);
		}

		public override void Remove(GameObject Object)
		{
			// strip by re-enumeration rather than stored refs (simpler and save/load-proof).
			// Accepted edge, recorded: a creature with NATIVE RustOnHit limbs would lose it here --
			// no such player-reachable body plan exists (qudzu are plants).
			Body body = Object?.Body;
			if (body != null)
			{
				foreach (BodyPart bp in body.GetParts())
				{
					GameObject weapon = bp.DefaultBehavior;
					if (weapon != null && weapon.HasPart("RustOnHit"))
						weapon.RemovePart("RustOnHit");
				}
			}
			base.Remove(Object);
		}
	}
}
