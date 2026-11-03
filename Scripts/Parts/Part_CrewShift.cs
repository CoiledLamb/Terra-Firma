using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_CrewShift : IPart
	{
		public const string RESERVED = "Cleo_TerraFirma_QuestReserved";

		public string Job = "Dig";
		public string ShiftTurns = "300-500";
		public string BreakTurns = "100-200";
		public string SpotTurns = "20-40";
		public int ChipEvery = 3;
		public int ChipsToBreak = 20;
		public int DigCap = 0;
		public int DropChance = 15;
		public int EtchChance = 25;
		public int LegTimeout = 60;

		public string Spots = "";

		public bool OnShift;
		public int Spot = -1;
		public long ShiftEnds;
		public long BreakEnds;
		public long SpotEnds;
		public long NextChip;
		public long LegStarted;
		public int Chips;

		[NonSerialized] public int RestStart = 400;
		[NonSerialized] public int RestEnd = 760;
		[NonSerialized] public string IdleTurns = "60-120";
		[NonSerialized] public int StoneChance = 35;
		[NonSerialized] public int BoulderChance = 15;
		[NonSerialized] public long PlanSkipUntil;

		public const string PLAN_X = "Cleo_TerraFirma_CrewPlanX";
		public const string PLAN_Y = "Cleo_TerraFirma_CrewPlanY";
		public int PlanX
		{
			get => ParentObject?.GetIntProperty(PLAN_X, -1) ?? -1;
			set => SetPlan(PLAN_X, value);
		}
		public int PlanY
		{
			get => ParentObject?.GetIntProperty(PLAN_Y, -1) ?? -1;
			set => SetPlan(PLAN_Y, value);
		}

		private void SetPlan(string name, int value)
		{
			if (ParentObject == null)
				return;
			if (value < 0)
				ParentObject.RemoveIntProperty(name);
			else
				ParentObject.SetIntProperty(name, value);
		}

		private const int STALL_TURNS = 5;
		private Cell StallCell;
		private int StallTicks;

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == PooledEvent<AIBoredEvent>.ID || ID == ZoneActivatedEvent.ID || ID == ZoneThawedEvent.ID;
			return true;
		}

		public override bool HandleEvent(ZoneActivatedEvent E)
		{
			if (Job == "Dig" && E.Zone != null)
				Cleo_TerraFirma_WarrenDig.CatchUp(E.Zone, StoneChance, BoulderChance);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ZoneThawedEvent E)
		{
			if (Job == "Dig" && E.Zone != null)
				Cleo_TerraFirma_WarrenDig.CatchUp(E.Zone, StoneChance, BoulderChance);
			return base.HandleEvent(E);
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
			Zone zone = ParentObject.CurrentZone;
			if (brain == null || zone == null || The.Game == null)
				return false;
			if (!Cleo_TerraFirma_DevHolds.On)
			{
				if (OnShift)
					EndShift(brain, 0);
				return false;
			}
			bool digging = Job == "Dig" && Cleo_TerraFirma_WarrenDig.HasPlan(zone);
			if (string.IsNullOrEmpty(Spots) && !digging)
				return false;
			if (ParentObject.IsPlayerControlled() || brain.PartyLeader != null)
			{
				if (OnShift)
					EndShift(brain, 0);
				return false;
			}
			long now = The.Game.TimeTicks;
			if (Resting())
			{
				if (OnShift)
				{
					EndShift(brain, now + Stat.Random(0, 60));
					Helpers.VerifyLog("SHIFT", $"{ParentObject.Blueprint} ({ParentObject.ID}) stops for the midday rest or hands off to the other crew (crew {Crew()})");
				}
				return false;
			}
			if (!OnShift)
			{
				if (BreakEnds == 0)
					BreakEnds = now + Stat.Random(0, 60);
				if (now < BreakEnds)
					return false;
				OnShift = true;
				ParentObject.SetIntProperty("PreventIdleBehavior", 1);
				PickSpot(zone, now);
				Helpers.VerifyLog("SHIFT", $"{ParentObject.Blueprint} ({ParentObject.ID}) starts a {Job} shift, spot {Spot}");
			}
			Cell wall = digging && now >= PlanSkipUntil ? PlanCell(zone, now) : null;
			if (wall != null)
			{
				Cleo_TerraFirma_WarrenDig.Seen(zone);
				brain.Wanders = false;
				if (ParentObject.CurrentCell.IsAdjacentTo(wall))
				{
					SetHome(brain, ParentObject.CurrentCell);
					DigPlan(zone, wall, now);
					return false;
				}
				if (now - LegStarted > LegTimeout || Stalled())
				{
					PlanSkipUntil = now + 50;
					PlanX = PlanY = -1;
					return false;
				}
				return WalkTo(brain, StandCell(wall));
			}
			if (PlanX >= 0)
			{
				PlanX = PlanY = -1;
				PickSpot(zone, now);
			}
			wall = SpotCell(zone, Spot);
			if (!SpotUsable(wall) && !PickSpot(zone, now))
			{
				EndShift(brain, now + IdleTurns.RollCached());
				return false;
			}
			wall = SpotCell(zone, Spot);
			brain.Wanders = false;
			if (ParentObject.CurrentCell.IsAdjacentTo(wall))
			{
				SetHome(brain, ParentObject.CurrentCell);
				Work(zone, wall, now);
				return false;
			}
			if (now - LegStarted > LegTimeout || Stalled())
			{
				WalkDiag($"re-picks: {(now - LegStarted > LegTimeout ? "leg timeout" : "stalled")} on spot {Spot}");
				PickSpot(zone, now);
				wall = SpotCell(zone, Spot);
				if (wall == null)
					return false;
			}
			Cell stand = StandCell(wall);
			bool pushed = WalkTo(brain, stand);
			WalkDiag(pushed
				? $"walks to spot {Spot} (wall {wall.X},{wall.Y}, stand {stand?.X},{stand?.Y})"
				: $"no walk for spot {Spot} (wall {wall.X},{wall.Y}, stand {(stand == null ? "none" : stand.X + "," + stand.Y)}, has MoveTo {brain.HasGoal("MoveTo")})");
			return pushed;
		}

		private static readonly bool WALK_DIAG = true;

		private void WalkDiag(string what)
		{
			if (!WALK_DIAG)
				return;
			Cell here = ParentObject.CurrentCell;
			Helpers.VerifyLog("SHIFTWALK", $"{ParentObject.Blueprint} ({ParentObject.ID}) at {here?.X},{here?.Y} {what} | {DiagState()}");
		}

		public string DiagState()
		{
			long now = The.Game?.TimeTicks ?? 0;
			return $"shift {(OnShift ? "on" : "off")}, job {Job}, spot {Spot}, leg {now - LegStarted} turns (timeout {LegTimeout}), stall {StallTicks}/{STALL_TURNS}, wanders {ParentObject.Brain?.Wanders}, resting {Resting()}";
		}

		private Cell PlanCell(Zone zone, long now)
		{
			Cell cur = PlanX >= 0 ? zone.GetCell(PlanX, PlanY) : null;
			if (cur != null && cur.GetFirstWall() != null && Cleo_TerraFirma_WarrenDig.Frontier(zone).Contains(cur))
				return cur;
			Cell next = Cleo_TerraFirma_WarrenDig.Nearest(zone, ParentObject);
			if (next == null)
				return null;
			PlanX = next.X;
			PlanY = next.Y;
			Chips = 0;
			NextChip = 0;
			LegStarted = now;
			StallCell = null;
			StallTicks = 0;
			return next;
		}

		private void DigPlan(Zone zone, Cell wall, long now)
		{
			if (now < NextChip)
				return;
			NextChip = now + Math.Max(1, ChipEvery);
			if (++Chips < ChipsToBreak)
			{
				Chip(wall);
				return;
			}
			if (Cleo_TerraFirma_WarrenDig.Break(zone, wall, ParentObject, StoneChance, BoulderChance, live: true))
				Helpers.VerifyLog("SHIFT", $"{ParentObject.Blueprint} ({ParentObject.ID}) breaks plan cell {wall.X},{wall.Y}");
			PlanX = PlanY = -1;
		}

		private void Work(Zone zone, Cell wall, long now)
		{
			if (SpotEnds == 0)
				SpotEnds = now + SpotTurns.RollCached();
			if (Job == "Dig" && Cleo_TerraFirma_DevHolds.On)
			{
				if (now >= NextChip)
				{
					NextChip = now + Math.Max(1, ChipEvery);
					Chips++;
					Chip(wall);
				}
			}
			if (now < SpotEnds)
				return;
			if (Job == "Etch" && Stat.Random(1, 100) <= EtchChance)
				Etch(wall);
			NextSpot(zone, now);
		}

		private void Chip(Cell wall)
		{
			if (wall.IsVisible())
			{
				ParentObject.PlayWorldSound("Sounds/Damage/sfx_damage_stone");
			}
		}

		private void Etch(Cell wall)
		{
			GameObject face = wall.GetFirstWall();
			if (face == null || face.IsImportant() || face.HasProperty(RESERVED))
				return;
			Cleo_TerraFirma_Etchable part = face.RequirePart<Cleo_TerraFirma_Etchable>();
			if (!part.IsEtchableSurface())
				return;
			string text = MarkovLine();
			if (text != null && part.NpcEtch(ParentObject, text))
				Helpers.VerifyLog("SHIFT", $"{ParentObject.Blueprint} ({ParentObject.ID}) etches {wall.X},{wall.Y}: \"{part.Inscription}\"");
		}

		private static string MarkovLine()
		{
			MarkovBook.EnsureCorpusLoaded(Cleo_TerraFirma_Etchable.CORPUS);
			if (!MarkovBook.CorpusData.TryGetValue(Cleo_TerraFirma_Etchable.CORPUS, out MarkovChainData data))
				return null;
			for (int attempt = 0; attempt < 5; attempt++)
			{
				string text = MarkovChain.GenerateShortSentence(data)?.Trim();
				if (string.IsNullOrWhiteSpace(text) || text.Contains("=") || text.Contains("{") || text.Contains("}"))
					continue;
				return text.Replace('—', ',');
			}
			return null;
		}

		private bool SpotUsable(Cell wall)
		{
			GameObject face = wall?.GetFirstWall();
			if (face == null)
				return false;
			if (Job != "Etch")
				return true;
			if (face.IsImportant() || face.HasProperty(RESERVED))
				return false;
			Cleo_TerraFirma_Etchable part = face.GetPart<Cleo_TerraFirma_Etchable>();
			return part == null || string.IsNullOrEmpty(part.Inscription);
		}

		private void NextSpot(Zone zone, long now)
		{
			PickSpot(zone, now);
		}

		private bool PickSpot(Zone zone, long now)
		{
			string[] all = Spots.Split(';');
			List<int> open = new List<int>();
			for (int i = 0; i < all.Length; i++)
				if (i != Spot && SpotUsable(SpotCell(zone, i)))
					open.Add(i);
			SpotEnds = 0;
			Chips = 0;
			NextChip = 0;
			LegStarted = now;
			StallCell = null;
			StallTicks = 0;
			if (open.Count == 0)
			{
				if (!SpotUsable(SpotCell(zone, Spot)))
					Spot = -1;
				return Spot >= 0;
			}
			Spot = open[Stat.Random(0, open.Count - 1)];
			return true;
		}

		public const string CREW = "Cleo_TerraFirma_Crew";

		private int Crew()
		{
			if (!Cleo_TerraFirma_DevHolds.On)
				return 0;
			int c = ParentObject.GetIntProperty(CREW, 0) - 1;
			if (c == 0 || c == 1)
				return c;
			int a = 0, b = 0;
			ParentObject.CurrentZone?.ForeachObjectWithPart("Cleo_TerraFirma_CrewShift", o =>
			{
				if (o == ParentObject || o.GetPart<Cleo_TerraFirma_CrewShift>()?.Job != Job)
					return;
				int oc = o.GetIntProperty(CREW, 0) - 1;
				if (oc == 0) a++;
				else if (oc == 1) b++;
			});
			c = a <= b ? 0 : 1;
			ParentObject.SetIntProperty(CREW, c + 1);
			return c;
		}

		private bool Resting()
		{
			long t = Calendar.TotalTimeTicks % 1200;
			if (t >= RestStart && t < RestEnd)
				return true;
			if (!Cleo_TerraFirma_DevHolds.On)
				return false;
			long work = 1200 - (RestEnd - RestStart);
			long since = (t - RestEnd + 1200) % 1200;
			int block = (int)Math.Min(3, since * 4 / Math.Max(1, work));
			return block % 2 != Crew();
		}

		private void EndShift(Brain brain, long breakEnds)
		{
			OnShift = false;
			Spot = -1;
			SpotEnds = 0;
			BreakEnds = breakEnds;
			ParentObject.RemoveIntProperty("PreventIdleBehavior");
			brain.Wanders = true;
		}

		private Cell StandCell(Cell wall)
		{
			Cell best = null;
			foreach (Cell c in wall.GetLocalAdjacentCells())
			{
				if (c.IsSolid() || (c != ParentObject.CurrentCell && c.HasObjectWithPart("Combat")))
					continue;
				if (best == null || ParentObject.DistanceTo(c) < ParentObject.DistanceTo(best))
					best = c;
			}
			return best;
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
			if (target == null)
				return false;
			SetHome(brain, target);
			if (ParentObject.CurrentCell == target || brain.HasGoal("MoveTo"))
				return false;
			brain.PushGoal(new MoveTo(target, careful: false));
			return true;
		}

		private static void SetHome(Brain brain, Cell c)
		{
			brain.StartingCell ??= new GlobalLocation();
			brain.StartingCell.SetCell(c);
		}

		private Cell SpotCell(Zone zone, int i)
		{
			if (i < 0 || string.IsNullOrEmpty(Spots))
				return null;
			string[] all = Spots.Split(';');
			if (i >= all.Length)
				return null;
			string[] xy = all[i].Split(',');
			if (xy.Length != 2 || !int.TryParse(xy[0], out int x) || !int.TryParse(xy[1], out int y))
				return null;
			return zone.GetCell(x, y);
		}
	}
}
