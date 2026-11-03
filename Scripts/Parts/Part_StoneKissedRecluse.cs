using System;
using System.Collections.Generic;
using XRL.Rules;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_StoneKissedRecluse : IPart
	{
		public int AvoidRadius = 4;

		public int AggroRadius = 2;

		public int SkipAtReputation = 2;

		public int FleeTurns = 3;

		public string StatueTable = "DynamicObjectsTable:Mountains_Creatures";

		public string StatueCount = "2-4";

		public int StatueMinDistance = 2;
		public int StatueMaxDistance = 5;

		public bool Aggroed;

		public bool Provoked;

		public bool StatuesPlaced;

		public override bool SameAs(IPart p) => true;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<BeginTakeActionEvent>.ID
				|| ID == TookDamageEvent.ID
				|| ID == EnteredCellEvent.ID
				|| ID == ZoneBuiltEvent.ID;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register(PooledEvent<GetFeelingEvent>.ID);
			base.Register(Object, Registrar);
		}

		static bool IsPlayerSide(GameObject Obj)
		{
			return Obj != null && (Obj.IsPlayer() || Obj.IsPlayerLed());
		}

		bool Ignores()
		{
			return The.Game?.PlayerReputation != null
				&& The.Game.PlayerReputation.GetLevel("Hermits") >= SkipAtReputation;
		}

		public override bool HandleEvent(GetFeelingEvent E)
		{
			if (IsPlayerSide(E.Target) || IsPlayerSide(E.TargetLeader))
			{
				if (Provoked)
					return base.HandleEvent(E);
				E.Feeling = (Aggroed && !Ignores()) ? -100 : 0;
				return false;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(TookDamageEvent E)
		{
			if (!Provoked && IsPlayerSide(E.Actor))
			{
				Provoked = true;
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("HERMIT", $"provoked by {E.Actor.Blueprint}");
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			Brain brain = ParentObject.Brain;
			if (brain == null || ParentObject.IsPlayer() || brain.PartyLeader != null || Provoked)
				return base.HandleEvent(E);

			GameObject player = The.Player;
			bool near = player != null && player.CurrentZone == ParentObject.CurrentZone && !Ignores();
			int dist = near ? ParentObject.DistanceTo(player) : int.MaxValue;

			if (near && dist <= AggroRadius)
			{
				if (!Aggroed)
				{
					Aggroed = true;
					brain.Goals.Clear();
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("HERMIT", $"aggro at distance {dist}");
				}
				return base.HandleEvent(E);
			}

			if (Aggroed)
			{
				if (dist <= AvoidRadius)
					return base.HandleEvent(E);
				Calm();
			}

			if (near && dist <= AvoidRadius)
			{
				if (!brain.HasGoal("Flee"))
				{
					brain.Goals.Clear();
					brain.PushGoal(new Flee(player, FleeTurns, Panicked: false, LocalOnly: true));
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("HERMIT", $"backing away at distance {dist}");
				}
				return base.HandleEvent(E);
			}

			Cell cell = ParentObject.CurrentCell;
			if (cell != null && !brain.StartingCell.IsClear()
				&& brain.StartingCell.PathDistanceTo(cell) > 2
#if !VERSION_1_0
				&& !brain.Goals.ContainsDescendedFrom<IMovementGoal>())
#else
				&& !HasMovementGoal(brain))
#endif
			{
				brain.Goals.Clear();
				brain.PushGoal(new MoveTo(brain.StartingCell));
			}
			return base.HandleEvent(E);
		}

#if VERSION_1_0
		static bool HasMovementGoal(Brain brain)
		{
			foreach (XRL.World.AI.GoalHandler goal in brain.Goals.Items)
				if (goal is IMovementGoal)
					return true;
			return false;
		}
#endif

		void Calm()
		{
			Aggroed = false;
			ParentObject.GetPart<Mutation.Cleo_TerraFirma_GorgonsGaze>()?.CancelGaze(Silent: true);
			ParentObject.Brain.Goals.Clear();
			ParentObject.Brain.Target = null;
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("HERMIT", "calmed; heading home");
		}

		public override bool HandleEvent(ZoneBuiltEvent E)
		{
			PlaceStatues();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EnteredCellEvent E)
		{
			if (ParentObject.CurrentZone != null && ParentObject.CurrentZone.Built)
				PlaceStatues();
			return base.HandleEvent(E);
		}

		void PlaceStatues()
		{
			if (StatuesPlaced)
				return;
			Cell home = ParentObject.CurrentCell;
			if (home == null || home.ParentZone == null)
				return;
			StatuesPlaced = true;

			List<Cell> spots = new List<Cell>();
			Dictionary<Cell, int> steps = new Dictionary<Cell, int> { [home] = 0 };
			Queue<Cell> frontier = new Queue<Cell>();
			frontier.Enqueue(home);
			while (frontier.Count > 0)
			{
				Cell at = frontier.Dequeue();
				int d = steps[at];
				if (d >= StatueMinDistance && at.IsEmpty())
					spots.Add(at);
				if (d >= StatueMaxDistance)
					continue;
				foreach (Cell next in at.GetLocalAdjacentCells())
				{
					if (steps.ContainsKey(next) || next.IsSolid())
						continue;
					steps[next] = d + 1;
					frontier.Enqueue(next);
				}
			}

			int want = StatueCount.RollCached();
			int placed = 0;
			while (placed < want && spots.Count > 0)
			{
				Cell cell = spots.GetRandomElement();
				spots.Remove(cell);
				if (!cell.IsEmpty())
					continue;
				string bp = RollStatueCreature();
				if (bp == null)
					break;
				GameObject model = GameObject.Create(bp);
				if (model == null)
					continue;
				cell.AddObject(Mutation.StoneGaze.CreateStatueOf(model));
				placed++;
			}
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("HERMIT", $"statue ring: {placed}/{want} placed");
		}

		string RollStatueCreature()
		{
			for (int i = 0; i < 6; i++)
			{
				string bp = PopulationManager.RollOneFrom(StatueTable)?.Blueprint;
				if (!string.IsNullOrEmpty(bp) && bp != ParentObject.Blueprint)
					return bp;
			}
			return null;
		}
	}
}
