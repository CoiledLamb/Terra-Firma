using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// The omnigeyser's throat: FabricateFromSelf that re-rolls its blueprint from a rock
	/// population table before every fabrication. The tables carry the mutation's own band
	/// escalation ("Deeper" groups), so the chance to spit a rank up comes from data, not code.
	/// Base part handles everything else verbatim: HP cost per shot (collapse at threshold 0),
	/// AI throwing, auto-equip to the Thrown Weapon slot, cooldown.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_OmniFabricate : FabricateFromSelf
	{
		/// <summary>Rock table rolled per shot (the throwable-only mirrors of the fissure bands).</summary>
		public string RockTable = "Cleo_TerraFirma_ThrowableRocks1";

		public override bool FireEvent(Event E)
		{
			if (E.ID == "ActivateFabricateFromSelf")
				RerollBlueprint(); // base Activate() reads FabricateBlueprint fresh each time
			return base.FireEvent(E);
		}

		private void RerollBlueprint()
		{
			for (int tries = 0; tries < 8; tries++)
			{
				string bp = PopulationManager.RollOneFrom(RockTable)?.Blueprint;
				if (bp.IsNullOrEmpty())
					continue;
				GameObjectBlueprint gbp = GameObjectFactory.Factory.GetBlueprint(bp);
				// belt and braces: spit only takeable, brainless stone
				if (gbp != null && !gbp.HasPart("Brain") && gbp.GetPartParameter("Physics", "Takeable", Default: true))
				{
					FabricateBlueprint = bp;
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GEYSER", $"{ParentObject?.Blueprint ?? "?"} spits: rolled {bp} from {RockTable}");
					return;
				}
			}
			FabricateBlueprint = "Cleo_TerraFirma_Rock_Generic";
		}
	}
}
