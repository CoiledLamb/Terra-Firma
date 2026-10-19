using System;

namespace XRL.World.Effects
{
	/// <summary>
	/// C09 (built 2026-10-19): the gazer's own petrification when a refractor bounces its
	/// Gorgon's Gaze back. Same stages as <see cref="Cleo_TerraFirma_Petrifying"/> (pin, -10
	/// Quickness per fail); the subclass exists only to hold the gazer's own fail tally, kept
	/// apart from the target's count on the mutation. A new class rather than a new field on
	/// the mutation, so the shipped save layout holds. Base HasEffect/GetEffect match the exact
	/// type, so target-side lookups of the parent class never see this one.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_PetrifyingReflected : Cleo_TerraFirma_Petrifying
	{
		/// <summary>Saves the gazer has failed against its own reflected gaze, this gaze.</summary>
		public int GazerFails;

		public Cleo_TerraFirma_PetrifyingReflected() : base()
		{
		}

		public Cleo_TerraFirma_PetrifyingReflected(GameObject Initiator) : base(Initiator)
		{
		}
	}
}
