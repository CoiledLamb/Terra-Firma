using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_VesselDrink : AIBehaviorPart
	{
		public float Trigger = 0.5f;

		public int SipDrams = 4;

		public int Radius = 20;

		public int Cooldown = 50;

		public int CooldownLeft;

		[NonSerialized] public string DrinkSound = "Sounds/Interact/sfx_interact_liquidContainer_drink";
		[NonSerialized] public float DrinkSoundVolume = 0.3f;
		[NonSerialized] private bool Sipping;

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
			bool wasSipping = Sipping;
			Sipping = false;
			LiquidVolume basin = ParentObject.LiquidVolume;
			Cell cell = ParentObject.CurrentCell;
			Brain brain = ParentObject.Brain;
			if (basin == null || cell == null || brain == null || basin.MaxVolume <= 0)
				return false;
			if (brain.PartyLeader != null || brain.Target != null)
				return false;

			GameObject pool = cell.GetFirstObjectWithPart("LiquidVolume", GO => GO != ParentObject && IsDrinkable(GO));
			if (pool != null && basin.Volume < basin.MaxVolume)
			{
				int amount = Math.Min(SipDrams, Math.Min(basin.MaxVolume - basin.Volume, pool.LiquidVolume.Volume));
				if (amount > 0 && basin.MixWith(pool.LiquidVolume, PouredFrom: pool, PouredBy: ParentObject, Amount: amount))
				{
					Sipping = true;
					if (!wasSipping && !DrinkSound.IsNullOrEmpty())
						ParentObject.PlayWorldSound(DrinkSound, DrinkSoundVolume);
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
			List<Cell> cells = zone.GetCells(C => ParentObject.DistanceTo(C) <= Radius && C.GetFirstObjectWithPart("LiquidVolume", GO => GO != ParentObject && IsDrinkable(GO)) != null);
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
