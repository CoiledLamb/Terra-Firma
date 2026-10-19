using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts
{
	/// <summary>
	/// Codex PROPOSAL (2026-10-19, boar vessel water thread; DRAFT, not yet compiled):
	/// the wild boar vessel refills its own basin. Shape copied from base AISeekHealingPool
	/// (AITakingAction, nearest qualifying cell, push MoveTo), minus its Goals.Clear(): this
	/// only fires when the brain is idle, so it never interrupts fleeing or a fight.
	/// Once standing in a drinkable pool it pulls up to SipDrams into the basin per turn
	/// (LiquidVolume.MixWith with Amount splits that much off the pool). The basin stays
	/// brackish on purpose (user 2026-10-19: the gut cistern is the desalinator).
	/// Drinkable = open ground volume, not dangerous to touch, and only water and salt in it,
	/// so a vessel never wades into acid, lava or someone's spilled wine. Party members skip
	/// it (the Husangeion's tank holds whatever its owner pours in). All numbers DRAFT.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_VesselDrink : AIBehaviorPart
	{
		/// <summary>Seek water when the basin is at or below this share of MaxVolume.</summary>
		public float Trigger = 0.5f;

		/// <summary>Drams pulled from the pool per turn while standing in it.</summary>
		public int SipDrams = 4;

		/// <summary>Ignore pools farther than this many cells.</summary>
		public int Radius = 20;

		/// <summary>Turns to wait after a search finds nothing, so penned vessels don't re-scan every turn.</summary>
		public int Cooldown = 50;

		public int CooldownLeft;

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("AITakingAction");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "AITakingAction" && TryDrink())
				return false;
			return base.FireEvent(E);
		}

		private bool TryDrink()
		{
			LiquidVolume basin = ParentObject.LiquidVolume;
			Cell cell = ParentObject.CurrentCell;
			Brain brain = ParentObject.Brain;
			if (basin == null || cell == null || brain == null || basin.MaxVolume <= 0)
				return false;
			if (brain.PartyLeader != null || brain.Target != null)
				return false;

			// Standing in a pool: sip until full, then let the brain carry on.
			GameObject pool = cell.GetFirstObjectWithPart("LiquidVolume", IsDrinkable);
			if (pool != null && basin.Volume < basin.MaxVolume)
			{
				int amount = Math.Min(SipDrams, Math.Min(basin.MaxVolume - basin.Volume, pool.LiquidVolume.Volume));
				if (amount > 0 && basin.MixWith(pool.LiquidVolume, PouredFrom: pool, PouredBy: ParentObject, Amount: amount))
				{
					ParentObject.UseEnergy(1000, "Drinking");
					return true;
				}
				return false;
			}

			if (CooldownLeft > 0)
			{
				CooldownLeft--;
				return false;
			}
			if (basin.Volume > basin.MaxVolume * Trigger || brain.IsBusy())
				return false;

			Zone zone = cell.ParentZone;
			if (zone == null)
				return false;
			List<Cell> cells = zone.GetCells(C => ParentObject.DistanceTo(C) <= Radius && C.GetFirstObjectWithPart("LiquidVolume", IsDrinkable) != null);
			if (cells.Count == 0)
			{
				CooldownLeft = Cooldown;
				return false;
			}
			cells.Sort(new XRLCore.SortCellBydistanceToObject(ParentObject));
			brain.PushGoal(new MoveTo(cells[0], careful: true, MaxTurns: Radius * 2));
			if (brain.Goals.Count > 0)
			{
				brain.Goals.Peek().TakeAction();
				return true;
			}
			return false;
		}

		private static bool IsDrinkable(GameObject GO)
		{
			LiquidVolume v = GO.LiquidVolume;
			if (v == null || !v.IsOpenVolume() || v.Volume <= 0 || v.ConsiderLiquidDangerousToContact())
				return false;
			foreach (string id in v.ComponentLiquids.Keys)
				if (id != "water" && id != "salt")
					return false;
			return true;
		}
	}
}
