using System;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	/// <summary>
	/// Merged onto all stones (BaseStone/Gemstone via Merges.xml).
	/// When a stone lands from a successful throw, relays to the thrower's Geomancy mutation.
	/// Uses the native <see cref="AfterThrownEvent"/> (fired to the item, after placement in the landing
	/// cell, only on PerformThrow's success path) -- replaces the old Harmony postfix on PerformThrow.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GeomancyStonePart : IPart
	{
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == AfterThrownEvent.ID;
			return true;
		}

		public override bool HandleEvent(AfterThrownEvent E)
		{
			if (E.Actor != null && E.Actor.TryGetPart(out Cleo_TerraFirma_Geomancy geomancy))
				geomancy.OnStoneThrown(E.Item);
			return base.HandleEvent(E);
		}
	}
}
