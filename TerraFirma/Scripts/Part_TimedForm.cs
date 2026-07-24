using System;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;

namespace XRL.World.Parts
{
	/// <summary>
	/// A living fall with a lifespan (RULED 2026-07-24, revising the ape-statue PERMANENT ratify:
	/// "ape can have cooldown - same for baboon"). Counts down a rolled duration; at expiry the
	/// creature announces itself, spills anything it was carrying (loot preserved, house rule),
	/// and either becomes its ReplacementBlueprint (ape -> rubble, baboon -> the rep-granting
	/// figurine item) or simply goes. The replacement inherits nothing -- it is a fresh object,
	/// the way a statue is not the ape it was.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_TimedForm : IPart
	{
		/// <summary>Lifespan die, rolled once on the first tick. DRAFT: 500-1000 (the recorded fallback range).</summary>
		public string Duration = "500-1000";

		/// <summary>What remains where the creature stood. Empty = nothing remains.</summary>
		public string ReplacementBlueprint = "";

		/// <summary>Expiry message verb (conjugated via Does) + the rest of the sentence.</summary>
		public string ExpireVerb = "crumble";
		public string ExpireRest = "away.";

		public int TurnsLeft;

		public override bool SameAs(IPart p) => false;

		public override bool WantTurnTick() => true;

		public override void TurnTick(long TimeTick, int Amount)
		{
			if (TurnsLeft <= 0)
				TurnsLeft = Math.Max(1, Duration.RollCached());
			TurnsLeft -= Amount;
			if (TurnsLeft > 0)
				return;
			GameObject self = ParentObject;
			Cell cell = self.CurrentCell;
			Helpers.VerifyLog("FORM", $"{self.Blueprint} lifespan spent -> {(ReplacementBlueprint.IsNullOrEmpty() ? "nothing" : ReplacementBlueprint)}");
			if (IComponent<GameObject>.Visible(self))
				IComponent<GameObject>.AddPlayerMessage(self.Does(ExpireVerb) + " " + ExpireRest);
			if (cell != null)
			{
				// spill possessions (the baboon's pocket stones): what it carried stays in the world
				var carried = self.Inventory?.GetObjectsDirect();
				if (carried != null)
					foreach (GameObject item in new System.Collections.Generic.List<GameObject>(carried))
					{
						item.RemoveFromContext();
						cell.AddObject(item);
					}
				if (!ReplacementBlueprint.IsNullOrEmpty())
					cell.AddObject(GameObject.Create(ReplacementBlueprint));
			}
			self.Obliterate();
		}
	}
}
