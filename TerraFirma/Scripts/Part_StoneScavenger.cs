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

		/// <summary>Sparse gate diagnostics for the scavenger experiment (user flag 2026-07-24:
		/// "baboons don't appear to scavenge for rocks"): one snapshot every 50th tick per
		/// creature names which early-return gate is starving the behavior, without flooding
		/// a log that sees this part every turn on every baboon.</summary>
		[NonSerialized]
		private int DiagTick;

		/// <summary>State-transition trail (experiment round 2, user: "we might need better hooks
		/// here so you can verify what's actually happening"): log ONLY when the observed state
		/// changes -- armed blueprint, pocket-stone count, or MoveTo-goal presence. Catches the
		/// events the sparse snapshots miss (the throw that empties the slot, the pickup that
		/// fills it, the moment pockets hit zero) at a handful of lines per fight, and makes the
		/// baboons' otherwise-invisible base-stone throws show up as armed->EMPTY transitions.</summary>
		[NonSerialized]
		private string LastSlotState;
		[NonSerialized]
		private int LastPocketCount = -1;
		[NonSerialized]
		private bool LastHadMoveGoal;

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
			bool diag = ++DiagTick % 50 == 1;
			// transition trail: fires only on state CHANGES (cheap; a few lines per fight)
			BodyPart diagSlot = who.GetFirstBodyPart("Thrown Weapon");
			string slotState = diagSlot == null ? "NO-SLOT" : (diagSlot.Equipped?.Blueprint ?? "EMPTY");
			int pocketCount = 0;
			List<GameObject> diagPack = who.Inventory?.GetObjectsDirect();
			if (diagPack != null)
				foreach (GameObject item in diagPack)
					if (item.HasTag("Cleo_TerraFirma_IsAnyStone"))
						pocketCount += item.Count;
			bool hasMoveGoal = who.Brain.HasGoal("MoveTo");
			if (slotState != LastSlotState || pocketCount != LastPocketCount || hasMoveGoal != LastHadMoveGoal)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SCAV", $"{who.Blueprint} STATE: slot {LastSlotState ?? "?"}->{slotState}, pockets {LastPocketCount}->{pocketCount}, moveGoal {LastHadMoveGoal}->{hasMoveGoal}");
				LastSlotState = slotState;
				LastPocketCount = pocketCount;
				LastHadMoveGoal = hasMoveGoal;
			}
			if (who.Brain.HasGoal("MoveTo"))
			{
				if (diag)
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SCAV", $"{who.Blueprint} gate: MoveTo goal active (scoot loop suspect) -> skipped");
				return; // already going somewhere on purpose (possibly ours)
			}
			BodyPart slot = who.GetFirstBodyPart("Thrown Weapon");
			if (slot == null || slot.Equipped != null)
			{
				if (diag)
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SCAV", $"{who.Blueprint} gate: {(slot == null ? "NO Thrown Weapon body part" : "slot already armed with " + slot.Equipped.Blueprint)} -> skipped");
				return; // no slot, or armed already
			}
			List<GameObject> pack = who.Inventory?.GetObjectsDirect();
			if (pack != null)
			{
				foreach (GameObject item in pack)
				{
					if (item.HasTag("Cleo_TerraFirma_IsAnyStone"))
					{
						if (diag)
							Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SCAV", $"{who.Blueprint} gate: still has pocket ammo ({item.Blueprint}) -> skipped");
						return; // still carrying ammo; the brain readies it on its own
					}
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
			{
				if (diag)
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SCAV", $"{who.Blueprint} gate: all clear but NO loose stone within {Radius} -> nothing to fetch");
				return;
			}
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
