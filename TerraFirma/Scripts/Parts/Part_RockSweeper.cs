using System;
using System.Collections.Generic;
using XRL.World.AI.GoalHandlers;

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
	///
	/// ACTIVE SEEK (USER RULED 2026-08-27, "option 2 is fine", after live-fire: "most wandered
	/// off and ignored the stones" -- v1's passive trot-and-snack was invisible in play): the
	/// shelved Part_StoneScavenger bones, hound gate. Idle hounds beeline to the nearest loose
	/// stone in SeekRadius and the EnteredCell sweep eats it on arrival; no reach-fetch and no
	/// throw slot (the hound WANTS to stand on its food -- no discharge law applies to a dog).
	/// Kept from the baboon: enemy-outranks-stone priority rule, MoveTo-goal yield, and the
	/// 3-strikes wedge backstop with a 20-turn rest.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_RockSweeper : IPart
	{
		/// <summary>Mirrors base Sweeper's guard; keeps boulder-scale objects out of the gut.</summary>
		public int MaxWeight = 100;

		/// <summary>How far afield the hound smells a loose stone (Chebyshev; scavenger default).</summary>
		[NonSerialized] public int SeekRadius = 8;   // out of the save (2026-09-30): 0.1.2.5 saved only MaxWeight, and fields load by position

		[NonSerialized]
		private Cell LastSeekTarget;
		[NonSerialized]
		private int SeekTries;
		[NonSerialized]
		private int SeekRest;

		public override bool SameAs(IPart p)
		{
			return p is Cleo_TerraFirma_RockSweeper o && o.MaxWeight == MaxWeight && o.SeekRadius == SeekRadius && base.SameAs(p);
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("EnteredCell");
			base.Register(Object, Registrar);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == SingletonEvent<BeforeTakeActionEvent>.ID;
			return true;
		}

		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			TrySeek();
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "EnteredCell")
				TrySweep();
			return base.FireEvent(E);
		}

		private bool IsEdibleStone(GameObject obj)
		{
			return obj != null
				&& obj.HasTag("Cleo_TerraFirma_IsAnyStone")
				&& obj.Physics != null && obj.Physics.Takeable
				&& obj.InInventory == null && obj.Equipped == null
				&& !obj.IsCombat && obj.Weight < MaxWeight;
		}

		private bool CellHasEdibleStone(Cell C)
		{
			foreach (GameObject obj in C.Objects)
			{
				if (IsEdibleStone(obj))
					return true;
			}
			return false;
		}

		private void TrySeek()
		{
			GameObject who = ParentObject;
			if (who == null || who.CurrentCell == null || who.Brain == null || who.IsPlayerControlled())
				return;
			if (SeekRest > 0)
			{
				SeekRest--;
				return;
			}
			if (who.Brain.HasGoal("MoveTo"))
				return; // already going somewhere on purpose (possibly ours)
			Cell here = who.CurrentCell;
			// nearest edible stone; own cell excluded, EnteredCell already ate anything here
			Cell best = null;
			int bestDist = SeekRadius + 1;
			foreach (Cell c in here.GetLocalAdjacentCells(SeekRadius))
			{
				if (c.IsSolid())
					continue;
				int dist = Math.Max(Math.Abs(c.X - here.X), Math.Abs(c.Y - here.Y));
				if (dist < bestDist && CellHasEdibleStone(c))
				{
					best = c;
					bestDist = dist;
				}
			}
			if (best == null)
				return;
			// priority rule (baboon precedent, user 2026-07-22): an enemy as close as the stone
			// or closer wins the hound's attention
			GameObject target = who.Brain.Target;
			if (GameObject.Validate(ref target) && who.DistanceTo(target) <= bestDist)
				return;
			// wedge backstop (baboon precedent): three pushes at the same stone without eating
			// it means the path is not working -- rest the nose for a while
			if (best == LastSeekTarget)
				SeekTries++;
			else
			{
				LastSeekTarget = best;
				SeekTries = 1;
			}
			if (SeekTries > 3)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("HOUND", $"{who.Blueprint} gave up on stone at d={bestDist} (3 failed seeks) -> nose rests 20 turns");
				SeekRest = 20;
				LastSeekTarget = null;
				SeekTries = 0;
				return;
			}
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("HOUND", $"{who.Blueprint} beelines to loose stone at d={bestDist}");
			who.Brain.PushGoal(new MoveTo(best, careful: false, overridesCombat: true));
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
					// gut-mark (user live-fire catch 2026-08-27: the hound ate a stone, then the
					// brain's reequip pass slotted it as a thrown weapon and threw it -- the
					// beetlebum-wears-the-hat behavior, but it leaks the hoard). NoAIEquip is the
					// engine's own skip: every AI equip branch honors it and players are exempt.
					// Set per-instance so floor stones stay equipable by snapjaws; accepted wart:
					// the mark rides spilled stones after death, so scavengers won't arm them.
					item.SetIntProperty("NoAIEquip", 1);
					// consume verb draft, approved 2026-09-30
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
