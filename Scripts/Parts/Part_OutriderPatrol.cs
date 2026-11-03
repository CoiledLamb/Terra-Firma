using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_OutriderPatrol : IPart
	{
		public int BeatRadius = 12;

		public int BeatInner = 3;

		public int BeatPoints = 6;

		public string PauseTurns = "3-8";

		public string ShiftTurns = "400-600";

		public string BreakTurns = "100-200";

		public int LegTimeout = 80;

		public string Beat = "";

		public string Rest = "";

		public bool OnDuty;
		public bool HeadingIn;
		public int Leg;
		public long ShiftEnds;
		public long BreakEnds;
		public long PauseEnds;
		public long LegStarted;
		public long NextLook;

		private const int STALL_TURNS = 5;

		private Cell StallCell;
		private int StallTicks;

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == PooledEvent<AIBoredEvent>.ID;
			return true;
		}

		public override bool HandleEvent(AIBoredEvent E)
		{
			if (Tend())
				return false;
			return base.HandleEvent(E);
		}

		private bool Tend()
		{
			Brain brain = ParentObject.Brain;
			Cell here = ParentObject.CurrentCell;
			Zone zone = here?.ParentZone;
			if (brain == null || zone == null || string.IsNullOrEmpty(Beat) || The.Game == null)
				return false;
			if (ParentObject.IsPlayerControlled() || brain.PartyLeader != null)
			{
				ParentObject.RemoveIntProperty("PreventIdleBehavior");
				if (OnDuty || HeadingIn)
				{
					OnDuty = false;
					HeadingIn = false;
					brain.Wanders = true;
				}
				return false;
			}
			long now = The.Game.TimeTicks;
			if (OnDuty)
			{
				ParentObject.SetIntProperty("PreventIdleBehavior", 1);
				return WalkBeat(brain, zone, now);
			}
			ParentObject.RemoveIntProperty("PreventIdleBehavior");
			return OffShift(brain, zone, now);
		}

		private bool WalkBeat(Brain brain, Zone zone, long now)
		{
			if (ShiftEnds == 0)
				ShiftEnds = now + ShiftTurns.RollCached();
			if (now >= ShiftEnds)
			{
				EndShift(brain, zone, now);
				return false;
			}
			List<Cell> beat = Waypoints(zone);
			if (beat.Count == 0)
			{
				OnDuty = false;
				brain.Wanders = true;
				return false;
			}
			brain.Wanders = false;
			Leg %= beat.Count;
			if (LegStarted == 0)
				LegStarted = now;
			Cell target = beat[Leg];
			if (ParentObject.DistanceTo(target) <= 1)
			{
				if (PauseEnds == 0)
				{
					PauseEnds = now + PauseTurns.RollCached();
					SetHome(brain, ParentObject.CurrentCell);
				}
				if (now < PauseEnds)
					return false;
				NextLeg(brain, beat, now);
				return WalkTo(brain, beat[Leg]);
			}
			if (now - LegStarted > LegTimeout || Stalled())
			{
				NextLeg(brain, beat, now);
				target = beat[Leg];
			}
			return WalkTo(brain, target);
		}

		private bool Stalled()
		{
			Cell here = ParentObject.CurrentCell;
			if (here != StallCell)
			{
				StallCell = here;
				StallTicks = 0;
				return false;
			}
			return ++StallTicks >= STALL_TURNS;
		}

		private bool WalkTo(Brain brain, Cell target)
		{
			SetHome(brain, target);
			if (target == null || ParentObject.CurrentCell == target || brain.HasGoal("MoveTo"))
				return false;
			brain.PushGoal(new MoveTo(target, careful: false));
			return true;
		}

		private void NextLeg(Brain brain, List<Cell> beat, long now)
		{
			Leg = (Leg + 1) % beat.Count;
			PauseEnds = 0;
			LegStarted = now;
			StallCell = null;
			StallTicks = 0;
			SetHome(brain, beat[Leg]);
		}

		private void EndShift(Brain brain, Zone zone, long now)
		{
			OnDuty = false;
			ShiftEnds = 0;
			PauseEnds = 0;
			GameObject relief = FindColleague(zone, onDuty: false, idleOnly: true);
			if (relief != null)
			{
				relief.GetPart<Cleo_TerraFirma_OutriderPatrol>().TakeDuty(now);
				BreakEnds = 0;
				Helpers.VerifyLog("PATROL", $"{ParentObject.Blueprint} ({ParentObject.ID}) hands the watch to {relief.Blueprint} ({relief.ID})");
			}
			else
			{
				BreakEnds = now + BreakTurns.RollCached();
				Helpers.VerifyLog("PATROL", $"{ParentObject.Blueprint} ({ParentObject.ID}) has no relief; post empty until turn {BreakEnds}");
			}
			Cell rest = RestCell(zone);
			if (rest != null)
			{
				HeadingIn = true;
				LegStarted = now;
				brain.Wanders = false;
				SetHome(brain, rest);
			}
			else
				brain.Wanders = true;
		}

		private bool OffShift(Brain brain, Zone zone, long now)
		{
			if (HeadingIn)
			{
				Cell rest = RestCell(zone);
				if (rest != null && ParentObject.DistanceTo(rest) > 1 && now - LegStarted <= LegTimeout && !Stalled())
					return WalkTo(brain, rest);
				HeadingIn = false;
				StallCell = null;
				StallTicks = 0;
				brain.Wanders = true;
			}
			if (now < BreakEnds || now < NextLook)
				return false;
			NextLook = now + 10;
			if (FindColleague(zone, onDuty: true, idleOnly: false) == null)
			{
				TakeDuty(now);
				Helpers.VerifyLog("PATROL", $"{ParentObject.Blueprint} ({ParentObject.ID}) takes the empty post");
			}
			return false;
		}

		public void TakeDuty(long now)
		{
			OnDuty = true;
			HeadingIn = false;
			BreakEnds = 0;
			PauseEnds = 0;
			LegStarted = now;
			StallCell = null;
			StallTicks = 0;
			ShiftEnds = now + ShiftTurns.RollCached();
			Brain brain = ParentObject.Brain;
			Zone zone = ParentObject.CurrentZone;
			if (brain == null || zone == null)
				return;
			List<Cell> beat = Waypoints(zone);
			if (beat.Count == 0)
				return;
			int best = 0;
			for (int i = 1; i < beat.Count; i++)
				if (ParentObject.DistanceTo(beat[i]) < ParentObject.DistanceTo(beat[best]))
					best = i;
			Leg = best;
			brain.Wanders = false;
			SetHome(brain, beat[Leg]);
		}

		private GameObject FindColleague(Zone zone, bool onDuty, bool idleOnly)
		{
			List<GameObject> found = new List<GameObject>();
			zone.ForeachObjectWithPart("Cleo_TerraFirma_OutriderPatrol", o =>
			{
				if (o == ParentObject || !o.IsValid() || o.IsPlayerControlled() || o.Brain == null || o.Brain.PartyLeader != null)
					return;
				var p = o.GetPart<Cleo_TerraFirma_OutriderPatrol>();
				if (p == null || p.OnDuty != onDuty || p.Beat != Beat)
					return;
				if (idleOnly && o.Target != null)
					return;
				if (idleOnly && o.HasEffect<XRL.World.Effects.Asleep>())
					return;
				found.Add(o);
			});
			return found.Count == 0 ? null : found[XRL.Rules.Stat.Random(0, found.Count - 1)];
		}

		private List<Cell> Waypoints(Zone zone)
		{
			List<Cell> cells = new List<Cell>();
			foreach (string pt in Beat.Split(';'))
			{
				Cell c = ParseCell(zone, pt);
				if (c != null)
					cells.Add(c);
			}
			return cells;
		}

		private Cell RestCell(Zone zone) => ParseCell(zone, Rest);

		private static void SetHome(Brain brain, Cell c)
		{
			brain.StartingCell ??= new GlobalLocation();
			brain.StartingCell.SetCell(c);
		}

		private static Cell ParseCell(Zone zone, string pt)
		{
			if (string.IsNullOrEmpty(pt))
				return null;
			string[] xy = pt.Split(',');
			if (xy.Length != 2 || !int.TryParse(xy[0], out int x) || !int.TryParse(xy[1], out int y))
				return null;
			return zone.GetCell(x, y);
		}
	}
}
