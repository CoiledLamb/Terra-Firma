using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Spall sandals (C04, user 2026-10-01: wall-adjacent move speed, +10 at tier 2).
	/// A copy of base MovespeedInLiquid with the liquid check swapped for a wall check: the
	/// wearer's cell is rechecked on EnteredCell/EndTurn, and while any of the eight neighbors
	/// holds a qualifying wall the wearer gets MoveSpeed -MS (lower is faster) through the
	/// part's StatShifter. StoneOnly counts any wall whose MaterialType is "stone" (Merges.xml),
	/// natural or built, so concrete and crypt walls count and metal or wood do not (user 2026-10-01).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_SpallSandals : IPart
	{
		public int MS = 10;

		public bool StoneOnly = true;

		public bool ShowInShortDescription = true;

		public int CurrentBonus;

		public override bool SameAs(IPart p)
		{
			Cleo_TerraFirma_SpallSandals other = p as Cleo_TerraFirma_SpallSandals;
			if (other.MS != MS || other.StoneOnly != StoneOnly || other.ShowInShortDescription != ShowInShortDescription)
				return false;
			return base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == EquippedEvent.ID
				|| ID == UnequippedEvent.ID
				|| ID == GetShortDescriptionEvent.ID;
		}

		public override bool HandleEvent(EquippedEvent E)
		{
			E.Actor.RegisterPartEvent(this, "EndTurn");
			E.Actor.RegisterPartEvent(this, "EnteredCell");
			CurrentBonus = 0;
			CheckWalls();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnequippedEvent E)
		{
			if (CurrentBonus > 0)
			{
				if (E.Actor != null)
					StatShifter.RemoveStatShift(E.Actor, "MoveSpeed");
				CurrentBonus = 0;
			}
			E.Actor.UnregisterPartEvent(this, "EndTurn");
			E.Actor.UnregisterPartEvent(this, "EnteredCell");
			return base.HandleEvent(E);
		}

		// Rules line on base MovespeedInLiquid's shape, "while adjacent to" from ModSprayFire.
		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			if (ShowInShortDescription && MS != 0)
			{
				E.Postfix.Append("\n&C");
				if (MS > 0)
					E.Postfix.Append('+');
				E.Postfix.Append(MS).Append(" move speed while adjacent to ")
					.Append(StoneOnly ? "a stone wall" : "a wall");
			}
			return base.HandleEvent(E);
		}

		private bool HasQualifyingWallAdjacent(Cell cell)
		{
			foreach (Cell adjacent in cell.GetAdjacentCells())
			{
				for (int i = 0, count = adjacent.Objects.Count; i < count; i++)
				{
					GameObject obj = adjacent.Objects[i];
					if (!obj.IsWall())
						continue;
					if (!StoneOnly || obj.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>()?.MaterialType == "stone")
						return true;
				}
			}
			return false;
		}

		private void CheckWalls()
		{
			GameObject equipped = ParentObject.Equipped;
			Cell cell = equipped?.CurrentCell;
			if (cell == null)
				return;
			if (HasQualifyingWallAdjacent(cell))
			{
				if (CurrentBonus == 0)
				{
					CurrentBonus = MS;
					StatShifter.SetStatShift(equipped, "MoveSpeed", -MS);
				}
			}
			else if (CurrentBonus > 0)
			{
				StatShifter.RemoveStatShift(equipped, "MoveSpeed");
				CurrentBonus = 0;
			}
		}

		public override bool AllowStaticRegistration()
		{
			return false;
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "EnteredCell" || E.ID == "EndTurn")
				CheckWalls();
			return base.FireEvent(E);
		}
	}
}
