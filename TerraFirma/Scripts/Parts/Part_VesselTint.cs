using System;
using XRL.Liquids;

namespace XRL.World.Parts
{
	/// <summary>
	/// Husangeion's mood-ring glaze (user tweak 2026-08-07: "what if boar vessel's detail
	/// color was dependent on the majority liquid stored inside it? so cider would be red,
	/// wine would be purple, and so on"): each end of turn the DetailColor re-reads the
	/// tank's PRIMARY liquid and wears that liquid's engine color (BaseLiquid.GetColor —
	/// water B, wine m, blood r, honey w...). Water's B IS the blueprint's shipped
	/// DetailColor, so the default look is unchanged and reads retroactively as the water
	/// showing through the glaze. OverrideFor holds the user's own mappings where the
	/// engine's canonical char disagrees with the ask (cider's GetColor is tawny "w"; the
	/// ask names it red — its secondary shade). Empty tank falls back to EmptyColor (XML
	/// dial, default B — CLAUDE DIAL; a dry-clay "y" is the one-line alternative). Generic
	/// by construction — nothing boar-specific — but only Pets.xml mounts it.
	/// Save note: pre-existing saved boars serialized without this part keep their fixed
	/// glaze (same stale-cosmetic class as the rename); new spawns tint live.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_VesselTint : IPart
	{
		public string EmptyColor = "B";

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<EndTurnEvent>.ID
				|| ID == ObjectCreatedEvent.ID;
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			Retint();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ObjectCreatedEvent E)
		{
			Retint();
			return base.HandleEvent(E);
		}

		/// <summary>User-ruled color mappings that outrank the liquid's canonical char.</summary>
		private static string OverrideFor(string id)
		{
			switch (id)
			{
				case "cider": return "r"; // user's own example 2026-08-07; engine canon is tawny "w"
				default: return null;
			}
		}

		public void Retint()
		{
			Render render = ParentObject?.Render;
			if (render == null)
				return;
			string color = EmptyColor;
			BaseLiquid primary = ParentObject.LiquidVolume?.GetPrimaryLiquid();
			if (primary != null)
				color = OverrideFor(primary.ID) ?? primary.GetColor();
			if (!color.IsNullOrEmpty() && render.DetailColor != color)
				render.DetailColor = color;
		}
	}
}
