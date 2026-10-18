using System;
using System.Collections.Generic;
using XRL.Rules;
using XRL.World.AI.GoalHandlers;
using XRL.World.Capabilities;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	/// <summary>
	/// The burrowbee's forage loop (C15, designed with the user 2026-10-17/18). When idle it picks
	/// a flower within ForageRadius, burrows, travels under the ground to it, surfaces beside it
	/// and lingers a few turns, then repeats. Everything else is base: the Burrowing mutation does
	/// the burrow (CommandBurrow) and the Burrowed effect the rest (invisible, can't attack, forced
	/// up when hit, Burrowed.Emerge to surface). Gated the way base AIUrnDuster gates its
	/// errands: not busy, not player-led, CanAIDoIndependentBehavior, and no target, so a provoked
	/// bee just fights.
	///
	/// Radius and turn counts are Codex PROPOSAL dials; the flower list is XML.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_BurrowbeeForager : IPart
	{
		/// <summary>Flower blueprints it forages, comma separated (default base Lahbloom).</summary>
		public string Flowers = "Lahbloom";

		/// <summary>How far it looks for its next flower. DIAL.</summary>
		public int ForageRadius = 8;

		/// <summary>Turns it lingers beside a flower after surfacing. DIAL.</summary>
		public string Linger = "3-6";

		public int LingerLeft;

		/// <summary>The flower it's heading for, by cell.</summary>
		public int FlowerX = -1;
		public int FlowerY = -1;

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<BeginTakeActionEvent>.ID;
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			Brain brain = ParentObject.Brain;
			Cell here = ParentObject.CurrentCell;
			if (brain == null || here == null || ParentObject.IsPlayerControlled() || ParentObject.IsBusy()
				|| ParentObject.Target != null || !ParentObject.FireEvent("CanAIDoIndependentBehavior"))
				return base.HandleEvent(E);

			// the loop does its own burrowing; base Burrowing would also burrow whenever bored
			Mutation.Burrowing burrowing = ParentObject.GetPart<Mutation.Burrowing>();
			if (burrowing == null)
				return base.HandleEvent(E);
			burrowing.bBurrowWhenBored = false;

			Burrowed burrowed = ParentObject.GetEffect<Burrowed>();

			if (burrowed == null && LingerLeft > 0)
			{
				LingerLeft--;
				return base.HandleEvent(E);
			}

			Cell flower = FlowerCell(here);
			if (flower == null)
			{
				flower = PickFlower(here);
				if (flower == null)
				{
					// nothing to forage here: surface and live like any bee
					if (burrowed != null)
						burrowed.Emerge();
					return base.HandleEvent(E);
				}
				FlowerX = flower.X;
				FlowerY = flower.Y;
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("BURROWBEE", $"{ParentObject.Blueprint} picked a flower at {flower.X},{flower.Y}");
			}

			if (here.IsAdjacentTo(flower) || here == flower)
			{
				if (burrowed != null)
				{
					burrowed.Emerge();
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("BURROWBEE", $"{ParentObject.Blueprint} surfaced at {flower.X},{flower.Y}");
				}
				FlowerX = FlowerY = -1;
				LingerLeft = Linger.RollCached();
				brain.Goals.Clear();
				ParentObject.UseEnergy(1000, "Forage");
				return false;
			}

			if (burrowed == null)
			{
				// winged (user 2026-10-18): base Wings keeps it aloft whenever idle, so land first
				if (Flight.IsFlying(ParentObject))
					Flight.StopFlying(ParentObject, ParentObject);
				CommandEvent.Send(ParentObject, "CommandBurrow");
				if (!ParentObject.HasEffect<Burrowed>())
				{
					// couldn't burrow (body position, movement mode): spend the turn, try again next
					ParentObject.UseEnergy(1000, "Forage");
					return false;
				}
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("BURROWBEE", $"{ParentObject.Blueprint} burrowed toward {flower.X},{flower.Y}");
				return false;
			}

			// one route at a time: MoveTo spends its first turn pathing and queues Steps
			if (!brain.HasGoal<MoveTo>() && !brain.HasGoal<Step>())
			{
				brain.Goals.Clear();
				brain.PushGoal(new MoveTo(flower, shortBy: 1));
			}
			return base.HandleEvent(E);
		}

		Cell FlowerCell(Cell here)
		{
			if (FlowerX < 0)
				return null;
			Cell cell = here.ParentZone.GetCell(FlowerX, FlowerY);
			if (cell == null || !HasFlower(cell))
			{
				FlowerX = FlowerY = -1;
				return null;
			}
			return cell;
		}

		bool HasFlower(Cell cell)
		{
			foreach (GameObject obj in cell.Objects)
			{
				if (Flowers.CachedCommaExpansion().Contains(obj.Blueprint))
					return true;
			}
			return false;
		}

		Cell PickFlower(Cell here)
		{
			List<Cell> found = new List<Cell>();
			for (int dx = -ForageRadius; dx <= ForageRadius; dx++)
			{
				for (int dy = -ForageRadius; dy <= ForageRadius; dy++)
				{
					if (Math.Max(Math.Abs(dx), Math.Abs(dy)) < 2)
						continue; // the flower it's beside doesn't count
					Cell cell = here.ParentZone.GetCell(here.X + dx, here.Y + dy);
					if (cell != null && HasFlower(cell))
						found.Add(cell);
				}
			}
			return found.Count > 0 ? found.GetRandomElement() : null;
		}
	}
}
