using System;
using System.Collections.Generic;
using XRL.Rules;
using XRL.World.AI.GoalHandlers;
using XRL.World.Capabilities;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_BurrowbeeForager : IPart
	{
		public string Flowers = "Lahbloom";

		public int ForageRadius = 8;

		public string Linger = "3-6";

		public int LingerLeft;

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
				if (Flight.IsFlying(ParentObject))
					Flight.StopFlying(ParentObject, ParentObject);
				CommandEvent.Send(ParentObject, "CommandBurrow");
				if (!ParentObject.HasEffect<Burrowed>())
				{
					ParentObject.UseEnergy(1000, "Forage");
					return false;
				}
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("BURROWBEE", $"{ParentObject.Blueprint} burrowed toward {flower.X},{flower.Y}");
				return false;
			}

#if !VERSION_1_0
			if (!brain.HasGoal<MoveTo>() && !brain.HasGoal<Step>())
#else
			if (!brain.HasGoal("MoveTo") && !brain.HasGoal("Step"))
#endif
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
						continue;
					Cell cell = here.ParentZone.GetCell(here.X + dx, here.Y + dy);
					if (cell != null && HasFlower(cell))
						found.Add(cell);
				}
			}
			return found.Count > 0 ? found.GetRandomElement() : null;
		}
	}
}
