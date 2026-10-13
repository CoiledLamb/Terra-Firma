using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	/// <summary>
	/// Outrider trail watch (2026-09-30): the posted outrider walks a beat around the warren
	/// mouth and hands the watch to an off-shift outrider when the shift ends; with nobody to
	/// hand it to, it takes a break inside and the mouth stands empty until it walks back out.
	///
	/// Stock movement does all the walking. With Wanders off, base Bored.TakeAction runs its own
	/// target scan first and then MoveTo's Brain.StartingCell whenever the creature is away from
	/// it, idling 1-10 turns once there. This part only moves that home point: each waypoint in
	/// turn, the rest cell when heading in, and Wanders back on when off shift (the drunk walk
	/// the chassis inherits from base Creature). AIBoredEvent is observed, never vetoed, so
	/// combat, party leaders and idle behavior all stay stock.
	///
	/// WarrenCarver stamps Beat, Rest and the first OnDuty at build; a blueprint spawned any
	/// other way has an empty Beat and behaves exactly as before.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_OutriderPatrol : IPart
	{
		/// <summary>Beat ring around the mouth, in cells (read by WarrenCarver). CLAUDE PROPOSAL.</summary>
		public int BeatRadius = 12;

		/// <summary>Closest a waypoint sits to the mouth. CLAUDE PROPOSAL.</summary>
		public int BeatInner = 3;

		/// <summary>Waypoints on the ring, one per angular sector. CLAUDE PROPOSAL.</summary>
		public int BeatPoints = 6;

		/// <summary>Turns spent looking around at each waypoint. CLAUDE PROPOSAL.</summary>
		public string PauseTurns = "3-8";

		/// <summary>Turns on the beat before handing it over. CLAUDE PROPOSAL.</summary>
		public string ShiftTurns = "400-600";

		/// <summary>Turns the post stands empty when no one can take over. CLAUDE PROPOSAL.</summary>
		public string BreakTurns = "100-200";

		/// <summary>Turns before a walk that cannot arrive is given up. CLAUDE PROPOSAL.</summary>
		public int LegTimeout = 80;

		/// <summary>Waypoints as "x,y;x,y;..." in walking order; stamped by WarrenCarver.</summary>
		public string Beat = "";

		/// <summary>An inside floor cell "x,y" to head for off shift; stamped by WarrenCarver.</summary>
		public string Rest = "";

		public bool OnDuty;
		public bool HeadingIn;
		public int Leg;
		public long ShiftEnds;
		public long BreakEnds;
		public long PauseEnds;
		public long LegStarted;
		public long NextLook;

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == PooledEvent<AIBoredEvent>.ID;
			return true;
		}

		public override bool HandleEvent(AIBoredEvent E)
		{
			Tend();
			return base.HandleEvent(E);
		}

		private void Tend()
		{
			Brain brain = ParentObject.Brain;
			Cell here = ParentObject.CurrentCell;
			Zone zone = here?.ParentZone;
			if (brain == null || zone == null || string.IsNullOrEmpty(Beat) || The.Game == null)
				return;
			if (ParentObject.IsPlayerControlled() || brain.PartyLeader != null)
			{
				// recruited: the watch is no longer theirs to keep
				if (OnDuty || HeadingIn)
				{
					OnDuty = false;
					HeadingIn = false;
					brain.Wanders = true;
				}
				return;
			}
			long now = The.Game.TimeTicks;
			if (OnDuty)
				WalkBeat(brain, zone, now);
			else
				OffShift(brain, zone, now);
		}

		private void WalkBeat(Brain brain, Zone zone, long now)
		{
			if (ShiftEnds == 0)
				ShiftEnds = now + ShiftTurns.RollCached();
			if (now >= ShiftEnds)
			{
				EndShift(brain, zone, now);
				return;
			}
			List<Cell> beat = Waypoints(zone);
			if (beat.Count == 0)
			{
				OnDuty = false;
				brain.Wanders = true;
				return;
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
					// home is where they stand, so stock Bored idles instead of nudging onto an occupied waypoint
					SetHome(brain, ParentObject.CurrentCell);
				}
				if (now < PauseEnds)
					return;
				NextLeg(brain, beat, now);
			}
			else if (now - LegStarted > LegTimeout)
				NextLeg(brain, beat, now);
			else
				SetHome(brain, target);
		}

		private void NextLeg(Brain brain, List<Cell> beat, long now)
		{
			Leg = (Leg + 1) % beat.Count;
			PauseEnds = 0;
			LegStarted = now;
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

		private void OffShift(Brain brain, Zone zone, long now)
		{
			if (HeadingIn)
			{
				Cell rest = RestCell(zone);
				if (rest != null && ParentObject.DistanceTo(rest) > 1 && now - LegStarted <= LegTimeout)
				{
					SetHome(brain, rest);
					return;
				}
				HeadingIn = false;
				brain.Wanders = true;
			}
			if (now < BreakEnds || now < NextLook)
				return;
			NextLook = now + 10;
			// an empty post gets filled: after a break, or when the watch died or joined someone
			if (FindColleague(zone, onDuty: true, idleOnly: false) == null)
			{
				TakeDuty(now);
				Helpers.VerifyLog("PATROL", $"{ParentObject.Blueprint} ({ParentObject.ID}) takes the empty post");
			}
		}

		public void TakeDuty(long now)
		{
			OnDuty = true;
			HeadingIn = false;
			BreakEnds = 0;
			PauseEnds = 0;
			LegStarted = now;
			ShiftEnds = now + ShiftTurns.RollCached();
			Brain brain = ParentObject.Brain;
			Zone zone = ParentObject.CurrentZone;
			if (brain == null || zone == null)
				return;
			List<Cell> beat = Waypoints(zone);
			if (beat.Count == 0)
				return;
			// join the beat at the nearest waypoint rather than cross the ring
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

		/// <summary>Moves the stock home point. Brain.StartingCell can be null (base null-checks it
		/// in Brain.Stay and in the Wanders-off setup, Brain.cs:2159/2612), so this copies that
		/// guard: make one before setting it. Player log 2026-10-13: the unguarded write threw in TakeDuty during
		/// the warren build and in WalkBeat every bored turn.</summary>
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
