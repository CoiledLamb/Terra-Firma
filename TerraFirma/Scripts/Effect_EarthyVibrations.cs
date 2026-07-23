using System;

namespace XRL.World.Effects
{
	/// <summary>
	/// Data-only part used to indicate that a creature is affected by Geomancy's scanning effect.
	/// Has no functionality on its own; only serves as a visible indicator.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_EarthlyVibrations : Effect
	{
		public Cleo_TerraFirma_EarthlyVibrations()
		{
			Duration = 2;
			DisplayName = "{{y|earthly vibrations}}";
		}

		public Cleo_TerraFirma_EarthlyVibrations(int duration) : this()
		{
			Duration = duration;
		}

		public override string GetDetails()
		{
			return "Shaking from inside. Exact statistics visible to all creatures.";
		}

		public override bool UseStandardDurationCountdown() => true;
	}
}
