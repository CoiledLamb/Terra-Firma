using System;
using System.Collections.Generic;
using XRL.Rules;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts
{
	/// <summary>
	/// The stone-kissed hermit's temper (TODO: stone-kissed hermit; design settled with the user
	/// 2026-10-13, recon in design/stone-kissed-hermit-ai-recon-2026-10-13.md). Modeled on base
	/// CryptSitterBehavior: a GetFeelingEvent override decides how he feels about the player, and
	/// BeginTakeActionEvent walks him home when he has nothing better to do.
	///
	/// Neutral by default and he keeps his distance: inside AvoidRadius he backs away (Flee goal,
	/// short and local). Come inside AggroRadius and he turns on you with his gaze (the TF1
	/// Gorgon's Gaze mutation on the blueprint is his whole weapon). Back off past AvoidRadius and
	/// he calms, releases the gaze silently and drifts home, unless you hurt him: damage from the
	/// player sets Provoked, which sticks. At loved Hermits reputation he pays you no mind at all.
	///
	/// On first placement he leaves a ring of statues of local creatures around himself (base
	/// StoneGaze.CreateStatueOf, so they read and behave like the base gaze's own statues).
	/// Radii, rep threshold and statue count are Codex PROPOSAL dials.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneKissedRecluse : IPart
	{
		/// <summary>Within this many cells (Chebyshev) he backs away from the player. DIAL.</summary>
		public int AvoidRadius = 4;

		/// <summary>Within this many cells he turns on the player. DIAL.</summary>
		public int AggroRadius = 2;

		/// <summary>Hermits reputation level (-2..2) at or above which he ignores the player. 2 = loved.</summary>
		public int SkipAtReputation = 2;

		/// <summary>Turns the backing-away Flee goal lasts before he re-evaluates. DIAL.</summary>
		public int FleeTurns = 3;

		/// <summary>Population the statue ring rolls its creatures from.</summary>
		public string StatueTable = "DynamicObjectsTable:Mountains_Creatures";

		/// <summary>How many statues ring him. DIAL.</summary>
		public string StatueCount = "2-4";

		/// <summary>Statues land between these many steps from him, walking through open ground. DIAL.</summary>
		public int StatueMinDistance = 2;
		public int StatueMaxDistance = 5;

		/// <summary>Turned on the player by closeness; clears when the player backs off.</summary>
		public bool Aggroed;

		/// <summary>The player hurt him. Sticky: he fights like anything else from here on.</summary>
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

			// nothing near: drift home (CryptSitterBehavior's walk-back, without its sleep)
			Cell cell = ParentObject.CurrentCell;
			if (cell != null && !brain.StartingCell.IsClear()
				&& brain.StartingCell.PathDistanceTo(cell) > 2
#if VERSION_1_1
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

#if !VERSION_1_1
		// stable's CleanStack has no ContainsDescendedFrom
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
			// a wished or late-placed hermit never sees ZoneBuiltEvent; during generation wait for it
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

			// candidates come from a flood fill through open ground from where he stands, so the
			// ring sits in his own clearing; random cells in mountain rock were mostly solid, or
			// open pockets cut off from him (seen in game 2026-10-18: 1/4 placed, one behind a wall)
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
