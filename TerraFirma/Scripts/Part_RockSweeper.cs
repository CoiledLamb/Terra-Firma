using System;
using System.Collections.Generic;

namespace XRL.World.Parts
{
	/// <summary>
	/// The rockhound's gut: a rock-only Sweeper (base beetlebum part; user pointer 2026-08-13
	/// "check out beetlebums"). On entering a cell it silently swallows any loose stone into
	/// inventory, so floors get grazed clean while it lives and death spills the hoard by
	/// native inventory drop -- the beetlebum loot-back grammar, and the roster's
	/// stone-creatures-die-into-stone-objects corpse line. Stones are recognized by the mod's
	/// Cleo_TerraFirma_IsAnyStone tag (the Merges stamp it on all BaseStone descendants);
	/// the filter is the whole point, since an unfiltered Sweeper would eat figurines.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_RockSweeper : IPart
	{
		/// <summary>Mirrors base Sweeper's guard; keeps boulder-scale objects out of the gut.</summary>
		public int MaxWeight = 100;

		public override bool SameAs(IPart p)
		{
			return p is Cleo_TerraFirma_RockSweeper o && o.MaxWeight == MaxWeight && base.SameAs(p);
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("EnteredCell");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "EnteredCell")
				TrySweep();
			return base.FireEvent(E);
		}

		private void TrySweep()
		{
			GameObject who = ParentObject;
			// player-controlled (dominated/companion-driven) hounds keep their manners: the
			// player stays in charge of which rocks are food -- base Sweeper has no such guard,
			// but base never planned a kept pet either
			if (who == null || who.CurrentCell == null || who.IsPlayerControlled())
				return;
			foreach (GameObject item in who.CurrentCell.GetObjectsWithPart("Physics"))
			{
				if (item == who || item.IsCombat)
					continue;
				if (!item.HasTag("Cleo_TerraFirma_IsAnyStone"))
					continue;
				if (!item.IsTakeable() || item.Weight >= MaxWeight)
					continue;
				if (who.FireEvent(Event.New("CommandTakeObject", "Object", item).SetSilent(Silent: true)))
				{
					// consume verb is a Claude DRAFT (one string to swap at text signoff)
					DidXToY("crunch", item);
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("HOUND", $"{who.Blueprint} swallowed {item.Blueprint} (gut now {GutCount(who)})");
				}
			}
		}

		private static int GutCount(GameObject who)
		{
			int n = 0;
			List<GameObject> pack = who.Inventory?.GetObjectsDirect();
			if (pack != null)
				foreach (GameObject item in pack)
					if (item.HasTag("Cleo_TerraFirma_IsAnyStone"))
						n += item.Count;
			return n;
		}
	}
}
