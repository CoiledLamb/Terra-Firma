using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// Keeps autoexplore from "autogetting" furniture that carries a base EnergyCell part (the
	/// animation table and the piezoelectric mill). Player log 2026-10-13: autoexplore deleted
	/// an animation table.
	///
	/// Base gates autoget in GameObject.HandleEvent(AutoexploreObjectEvent), but that check runs
	/// when the event hits the ACTOR, and AutoexploreObjectEvent.Check sends it to the actor
	/// first and the object second. EnergyCell then sets Command "Autoget" on the object pass
	/// (Options.AutogetAmmo), after the takeable check has already passed. TakeObject pulls the
	/// object out of its cell, Inventory.AddObject refuses it as untakeable ("Attempting to add
	/// untakeable object"), and the object is left nowhere. Base furniture never meets this
	/// because it stores charge in Capacitor, not EnergyCell.
	///
	/// Works whichever side of EnergyCell the calling part sits on: it clears an Autoget that
	/// is already set, and returning false stops the dispatch before EnergyCell can set one.
	/// Saved tables and mills keep their EnergyCell, so this guard is needed for them anyway.
	/// </summary>
	public static class FurnitureCellGuard
	{
		public static bool Block(AutoexploreObjectEvent E, GameObject Furniture)
		{
			if (E.Item != Furniture || Furniture.IsTakeable())
				return true;
			if (E.Command == "Autoget")
				E.Command = null;
			return false;
		}
	}
}
