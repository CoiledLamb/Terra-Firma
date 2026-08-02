using System;
using Cleo.TerraFirma.Scripts;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects
{
	/// <summary>
	/// The vantabsidian eaten-raw positive (eaten-table comb C2, RULED 2026-07-31): your eyes
	/// drink the dark -- see in darkness for the duration. Built in the skulk tonic's exact
	/// shape (decomp-verified): a temporary DarkVision mutation mod, Guid-tracked, removed at
	/// expiry; radius bumped like the tonic does so the grant feels generous. The 25% bite
	/// (Blind, short) lives in the eat channel, not here. Texts DRAFT.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_DarkAdapted : Effect
	{
		public Guid DarkVisionModID;

		/// <summary>Skulk-tonic parity: base DarkVision radius 5, tonic runs it at 8.</summary>
		public const int RADIUS_BONUS = 3;

		public Cleo_TerraFirma_DarkAdapted()
		{
			DisplayName = "{{K|dark-adapted}}"; // DRAFT name
		}

		public Cleo_TerraFirma_DarkAdapted(int Duration) : this()
		{
			base.Duration = Duration;
		}

		public override string GetDetails()
		{
			return "Can see in the dark."; // DRAFT (DarkVision's own description line)
		}

		public override bool UseStandardDurationCountdown() => true;

		public override bool Apply(GameObject Object)
		{
			// renew instead of stack: a second helping extends the first
			Cleo_TerraFirma_DarkAdapted existing = Object.GetEffect<Cleo_TerraFirma_DarkAdapted>();
			if (existing != null)
			{
				existing.Duration += Duration;
				Helpers.VerifyLog("LITH", $"darksight: renewed on {Object.Blueprint}, now {existing.Duration}");
				return false;
			}
			Mutations mutations = Object.RequirePart<Mutations>();
			DarkVisionModID = mutations.AddMutationMod("DarkVision", null, 1, Mutations.MutationModifierTracker.SourceType.Cooking);
			DarkVision dv = Object.GetPart<DarkVision>();
			if (dv != null)
				dv.Radius += RADIUS_BONUS;
			Helpers.VerifyLog("LITH", $"darksight: DarkVision mod granted to {Object.Blueprint} for {Duration}");
			if (Object.IsPlayer())
				IComponent<GameObject>.AddPlayerMessage("The dark resolves into shapes."); // DRAFT
			return base.Apply(Object);
		}

		public override void Remove(GameObject Object)
		{
			// skulk-tonic unwind order: radius first (while the part still exists), then the mod
			DarkVision dv = Object.GetPart<DarkVision>();
			if (dv != null)
				dv.Radius -= RADIUS_BONUS;
			if (DarkVisionModID != Guid.Empty)
				Object.RequirePart<Mutations>().RemoveMutationMod(DarkVisionModID);
			Helpers.VerifyLog("LITH", $"darksight: expired on {Object.Blueprint}");
			if (Object.IsPlayer())
				IComponent<GameObject>.AddPlayerMessage("The dark closes back over your eyes."); // DRAFT
			base.Remove(Object);
		}
	}
}
