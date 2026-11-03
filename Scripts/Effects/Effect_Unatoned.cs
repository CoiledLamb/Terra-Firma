using System;
using System.Collections.Generic;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects
{
	[Serializable]
	public class Cleo_TerraFirma_UnatonedEffect : Effect
	{
		public const string NAME = "unatoned";

		public Cleo_TerraFirma_UnatonedEffect()
		{
			Duration = DURATION_INDEFINITE;
			DisplayName = "{{K|" + NAME + "}}";
		}

		public override int GetEffectType()
		{
			return TYPE_MENTAL | TYPE_NEGATIVE;
		}

		private Cleo_TerraFirma_CapriciousDevotion Devotion => Object?.GetPart<Cleo_TerraFirma_CapriciousDevotion>();

		public override string GetDescription()
		{
			var devotion = Devotion;
			if (devotion == null)
				return DisplayName;
			var parts = new List<string>();
			if (devotion.MentalPenalty > 0)
				parts.Add("mental -" + devotion.MentalPenalty);
			if (devotion.PhysicalPenalty > 0)
				parts.Add("physical -" + devotion.PhysicalPenalty);
			return parts.Count == 0 ? DisplayName : DisplayName + " (" + string.Join(", ", parts) + ")";
		}

		public override string GetDetails()
		{
			var devotion = Devotion;
			if (devotion == null)
				return "";
			var lines = new List<string>();
			if (devotion.MentalPenalty > 0)
				lines.Add($"-{devotion.MentalPenalty} to mental saves until you pray.");
			if (devotion.PhysicalPenalty > 0)
				lines.Add($"-{devotion.PhysicalPenalty} to physical saves until you desecrate a statue.");
			lines.Add(NextLine("mental", Cleo_TerraFirma_CapriciousDevotion.DaysUntilNext(devotion.LastPrayed)));
			lines.Add(NextLine("physical", Cleo_TerraFirma_CapriciousDevotion.DaysUntilNext(devotion.LastDesecrated)));
			return string.Join("\n", lines);
		}

		private static string NextLine(string Kind, int Days)
		{
			if (Days < 0)
				return $"Your {Kind} penalty is at its maximum.";
			return $"Next {Kind} penalty in {Days} {(Days == 1 ? "day" : "days")}.";
		}
	}
}
