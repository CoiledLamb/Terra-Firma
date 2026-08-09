using System;

namespace XRL.World.Effects
{
	/// <summary>
	/// The lingering mark left on creatures that escape Gorgon's Gaze after accruing enough failed saves.
	/// Part of them stays stone: slower, stiffer, harder.
	/// Applied by <see cref="XRL.World.Parts.Mutation.Cleo_TerraFirma_GorgonsGaze"/>.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_PartiallyPetrified : Effect
	{
		public const int DURATION = 500;
		public const int MOVE_SPEED_PENALTY = 20;
		public const int QUICKNESS_PENALTY = 10;
		public const int AV_BONUS = 2;

		public Cleo_TerraFirma_PartiallyPetrified()
		{
			Duration = DURATION;
			DisplayName = "{{y|partially petrified}}";
		}

		public override string GetDetails()
		{
			return $"Partly stone. -{MOVE_SPEED_PENALTY} move speed. -{QUICKNESS_PENALTY} Quickness. +{AV_BONUS} AV.";
		}

		public override bool UseStandardDurationCountdown() => true;

		/// <summary>
		/// The mark's faint steady cast (ruled 2026-08-06 with the rising-band VFX): the sprite's
		/// DETAIL channel goes statue-gray for the mark's whole duration -- a subset of the
		/// creature's own pixels stays stone, so the escape handoff (band slides off, this takes
		/// over) never snaps back to full flesh while the debuff stands. Recomputed every rendered
		/// frame, non-destructive, save-safe -- the engine's own tint idiom (BasiliskPoison
		/// precedent). "y" matches the Random Statue dressing (&amp;y/y); creatures whose detail
		/// is already 'y' show no cast -- accepted, the effect tag still carries the state.
		/// </summary>
		public override bool Render(RenderEvent E)
		{
			if (Duration > 0)
				E.DetailColor = "y";
			return base.Render(E);
		}

		public override bool Apply(GameObject Object)
		{
			// refresh rather than stack
			if (Object.TryGetEffect<Cleo_TerraFirma_PartiallyPetrified>(out var existing))
			{
				existing.Duration = DURATION;
				return false;
			}
			return base.Apply(Object);
		}

		public override void Remove(GameObject Object)
		{
			StatShifter.RemoveStatShifts();
			base.Remove(Object);
		}

		public override bool WantEvent(int ID, int Cascade)
		{
			if (!base.WantEvent(ID, Cascade))
				return ID == SingletonEvent<EndTurnEvent>.ID;
			return true;
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			// idempotent; internal MoveSpeed runs inverted (displayed move speed = 200 - stat), so +20 here is -20 displayed
			StatShifter.SetStatShift("MoveSpeed", MOVE_SPEED_PENALTY);
			StatShifter.SetStatShift("Speed", -QUICKNESS_PENALTY);
			StatShifter.SetStatShift("AV", AV_BONUS);
			return base.HandleEvent(E);
		}
	}
}
