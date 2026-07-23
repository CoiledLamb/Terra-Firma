using System;
using System.Collections.Generic;
using XRL.World.AI.GoalHandlers;
using XRL.World.Anatomy;

namespace XRL.World.Parts
{
	/// <summary>
	/// Keeps a thrower in the fight: when the creature has nothing left to throw, it fetches a
	/// loose stone nearby (usually its own spent ammo) and readies it again instead of settling
	/// for melee. Merged onto baboons (user request 2026-07-22); harmless on anything with a
	/// Thrown Weapon slot. Stones are recognized by the mod's Cleo_TerraFirma_IsAnyStone tag,
	/// which the Merges already stamp on all of BaseStone's descendants.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneScavenger : IPart
	{
		/// <summary>How far afield the creature notices a loose stone (Chebyshev).</summary>
		public int Radius = 8;

		public override bool SameAs(IPart p)
		{
			return p is Cleo_TerraFirma_StoneScavenger o && o.Radius == Radius && base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == SingletonEvent<BeforeTakeActionEvent>.ID;
			return true;
		}

		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			TryScavenge(E);
			return base.HandleEvent(E);
		}

		private static bool IsLooseStone(GameObject obj)
		{
			return obj != null
				&& obj.HasTag("Cleo_TerraFirma_IsAnyStone")
				&& obj.Physics != null && obj.Physics.Takeable
				&& obj.InInventory == null && obj.Equipped == null;
		}

		private static bool CellHasLooseStone(Cell C)
		{
			foreach (GameObject obj in C.Objects)
			{
				if (IsLooseStone(obj))
					return true;
			}
			return false;
		}

		private void TryScavenge(BeforeTakeActionEvent E)
		{
			GameObject who = ParentObject;
			if (who == null || who.IsPlayerControlled() || who.Brain == null || who.CurrentCell == null)
				return;
			if (who.Brain.HasGoal("MoveTo"))
				return; // already going somewhere on purpose (possibly ours)
			BodyPart slot = who.GetFirstBodyPart("Thrown Weapon");
			if (slot == null || slot.Equipped != null)
				return; // no slot, or armed already
			List<GameObject> pack = who.Inventory?.GetObjectsDirect();
			if (pack != null)
			{
				foreach (GameObject item in pack)
				{
					if (item.HasTag("Cleo_TerraFirma_IsAnyStone"))
						return; // still carrying ammo; the brain readies it on its own
				}
			}
			// nearest loose stone, own cell included
			Cell here = who.CurrentCell;
			Cell best = null;
			int bestDist = Radius + 1;
			if (CellHasLooseStone(here))
			{
				best = here;
				bestDist = 0;
			}
			else
			{
				foreach (Cell c in here.GetLocalAdjacentCells(Radius))
				{
					int dist = Math.Max(Math.Abs(c.X - here.X), Math.Abs(c.Y - here.Y));
					if (dist < bestDist && CellHasLooseStone(c))
					{
						best = c;
						bestDist = dist;
					}
				}
			}
			if (best == null)
				return;
			// priority rule (user, 2026-07-22): an enemy as close as the stone -- or closer -- wins the
			// baboon's attention; only a stone strictly nearer than every threat is worth the detour
			GameObject target = who.Brain.Target;
			if (GameObject.Validate(ref target) && who.DistanceTo(target) <= bestDist)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SCAV", $"{who.Blueprint}: enemy {target.Blueprint} (d={who.DistanceTo(target)}) outranks stone at d={bestDist} -> attacks instead");
				return;
			}
			if (bestDist == 0)
			{
				// underfoot: stoop for it (costs the turn), then let the brain ready it
				foreach (GameObject obj in here.Objects)
				{
					if (IsLooseStone(obj))
					{
						if (who.TakeObject(obj, NoStack: false, Silent: false, EnergyCost: 1000))
						{
							Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SCAV", $"{who.Blueprint}: stooped for {obj.Blueprint} underfoot (turn spent)");
							who.Brain.PerformReequip();
							E.PreventAction = true;
						}
						return;
					}
				}
				return;
			}
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SCAV", $"{who.Blueprint}: detours to loose stone at d={bestDist}");
			who.Brain.PushGoal(new MoveTo(best, careful: false, overridesCombat: true));
		}
	}
}
