using System;
using XRL.Rules;

namespace XRL.World.Parts
{
	/// <summary>
	/// Cave moss rides base shale's water vines (2026-09-29, replacing the warren-only
	/// placement). Base ShaleVines repaints shale beside wading-depth water as vineshale1
	/// on ZoneBuiltEvent; this part, merged onto Shale beside it, turns some of those vine
	/// walls into ripe moss using the warren's existing recolor rules: ripe wears the moss
	/// palette, and harvesting reverts to the vine palette ShaleVines painted.
	/// GrowVines is called first (idempotent) so the result never depends on part order.
	/// StartRipeChance copies base harvestable plants (Watervine 1:20, Starapple Tree 1:5).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_ShaleMoss : IPart
	{
		public const string VINE_PAINT = "vineshale1";

		/// <summary>Chance a vine wall carries ripe moss. Base Harvestable dice syntax.</summary>
		public string StartRipeChance = "1:20";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == ZoneBuiltEvent.ID;
		}

		public override bool HandleEvent(ZoneBuiltEvent E)
		{
			ParentObject.GetPart<ShaleVines>()?.GrowVines();
			if (ParentObject.Render != null
				&& ParentObject.GetStringProperty("PaintedWall") == VINE_PAINT
				&& !ParentObject.HasPart<Harvestable>()
				&& Stat.Chance(StartRipeChance))
				Grow();
			return base.HandleEvent(E);
		}

		private void Grow()
		{
			// Same stock-Harvestable loop the warren used: Unripe holds the wall's current
			// (vine) palette, so a harvest reverts to vine shale and drops one clump.
			var h = ParentObject.RequirePart<Harvestable>();
			h.DestroyOnHarvest = false;
			h.OnSuccess = "Cleo_TerraFirma_MossClump";
			h.OnSuccessAmount = "1";
			h.RipeColor = "&K"; h.RipeTileColor = "&K"; h.RipeDetailColor = "g";
			h.UnripeColor = ParentObject.Render.ColorString ?? "";
			h.UnripeTileColor = ParentObject.Render.TileColor ?? "";
			h.UnripeDetailColor = ParentObject.Render.DetailColor ?? "";
			ParentObject.RequirePart<Cleo_TerraFirma_MossyWall>();
			h.UpdateRipeStatus(newRipeStatus: true);
		}
	}
}
