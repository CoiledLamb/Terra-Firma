using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// The mossy-wall name adjective (USER RULED 2026-08-23: "shale that has grown
	/// mossy should be prefixed green|mossy shale"). Rides beside the stock
	/// Harvestable that carries the moss itself (same ruling: "the moss should be
	/// harvestable, reverting the wall to its basic form"). The adjective asks
	/// Ripe on every name build, so harvesting un-prefixes with no name surgery;
	/// Etchable's "etched" adjective is the house precedent for the idiom.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_MossyWall : IPart
	{
		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == PooledEvent<GetDisplayNameEvent>.ID;
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			if (ParentObject.GetPart<Harvestable>()?.Ripe == true)
				E.AddAdjective("{{green|mossy}}");
			return base.HandleEvent(E);
		}
	}
}
