using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Cobblefur cloak: DV while amid loose rubble (user ruling 2026-08-13: "loose floor
	/// litter"). Chassis = base Yurtmat verbatim (user pointer, same sitting: "check how the
	/// grassy yurtmat works") -- item part registers onto the wearer's movement, StatShifter
	/// applies/removes a named DV shift -- with two swaps: PlantProperties becomes the mod's
	/// IsAnyStone tag, and same-tile widens to ADJACENT-INCLUSIVE (the user's "adjacent to
	/// rocks"; rubble scatters across cells in a way rooted foliage never does).
	/// One deliberate addition over Yurtmat: an EndTurn re-check on the wearer, because in
	/// this mod rubble ARRIVES around a stationary fighter constantly (spent barrage, fissure
	/// fallout) -- movement-only checks would lag the battlefield.
	/// Design line: the litter ecology as your cover; EB's own spent ammo makes you harder
	/// to hit. Third body on the accumulation signature.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_CobblefurCloak : IPart
	{
		/// <summary>Conditional DV while amid rubble. DRAFT 2 (Yurtmat parity; numbers sitting owed).</summary>
		public int Bonus = 2;

		public override bool SameAs(IPart p)
		{
			return false; // carries StatShifter state
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != EquippedEvent.ID && ID != UnequippedEvent.ID)
				return ID == GetShortDescriptionEvent.ID;
			return true;
		}

		public override bool HandleEvent(EquippedEvent E)
		{
			if (E.Item.IsEquippedProperly())
			{
				E.Actor.RegisterEvent(this, EnteredCellEvent.ID, 0, Serialize: true);
				E.Actor.RegisterEvent(this, EndTurnEvent.ID, 0, Serialize: true);
				CheckRubble();
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnequippedEvent E)
		{
			E.Actor.UnregisterEvent(this, EnteredCellEvent.ID);
			E.Actor.UnregisterEvent(this, EndTurnEvent.ID);
			base.StatShifter.RemoveStatShifts(E.Actor);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EnteredCellEvent E)
		{
			CheckRubble();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			CheckRubble();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			E.Postfix.AppendRules(Bonus.Signed() + " DV while amid loose rubble");
			return base.HandleEvent(E);
		}

		private static bool CellHasRubble(Cell C)
		{
			if (C == null)
				return false;
			foreach (GameObject obj in C.Objects)
			{
				// loose stones/boulders carry IsAnyStone (BaseStone merge); base Rubble heaps are
				// wall-layer objects OUTSIDE the BaseStone tree, so they carry their own narrow
				// cover tag instead of widening the load-bearing IsAnyStone (trade-goods precedent)
				if (obj.HasTag("Cleo_TerraFirma_IsAnyStone") || obj.HasTag("Cleo_TerraFirma_IsRubbleCover"))
					return true;
			}
			return false;
		}

		private void CheckRubble()
		{
			GameObject wearer = ParentObject.Equipped;
			Cell cell = wearer?.CurrentCell;
			if (cell == null)
				return;
			bool amidRubble = CellHasRubble(cell);
			if (!amidRubble)
			{
				foreach (Cell c in cell.GetLocalAdjacentCells())
				{
					if (CellHasRubble(c))
					{
						amidRubble = true;
						break;
					}
				}
			}
			if (amidRubble)
			{
				base.StatShifter.DefaultDisplayName = "rubble cover";
				base.StatShifter.SetStatShift(wearer, "DV", Bonus);
			}
			else
			{
				base.StatShifter.RemoveStatShifts(wearer);
			}
		}
	}
}
