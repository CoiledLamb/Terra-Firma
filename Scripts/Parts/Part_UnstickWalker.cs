using System;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_UnstickWalker : IPart
	{
		public const int STILL_TURNS = 8;

		[NonSerialized] private Cell LastCell;
		[NonSerialized] private int Still;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == SingletonEvent<BeforeTakeActionEvent>.ID;
		}

		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			Check();
			return base.HandleEvent(E);
		}

		private void Check()
		{
			GameObject who = ParentObject;
			Brain brain = who?.Brain;
			Cell here = who?.CurrentCell;
			if (brain == null || here == null || who.IsPlayerControlled())
				return;
			if (here != LastCell)
			{
				LastCell = here;
				Still = 0;
				return;
			}
			if (brain.Target != null || who.HasEffect<XRL.World.Effects.Asleep>() || !brain.HasGoal("MoveTo"))
			{
				Still = 0;
				return;
			}
			if (++Still < STILL_TURNS)
				return;
			Still = 0;
			string why = Diag(who, brain, here);
			brain.Goals.Clear();
			Helpers.VerifyLog("UNSTICK", $"{who.Blueprint} ({who.ID}) stood {STILL_TURNS} turns on a walk at {here.X},{here.Y}; goals cleared | {why}");
		}

		private static string Diag(GameObject who, Brain brain, Cell here)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			var items = brain.Goals.Items;
			sb.Append("goals:");
			for (int i = items.Count - 1; i >= 0; i--)
				sb.Append(' ').Append(items[i]?.GetDescription());
			for (int i = items.Count - 1; i >= 0; i--)
			{
				if (items[i] is XRL.World.AI.GoalHandlers.MoveTo mt)
				{
					sb.Append($" | to {mt.dCx},{mt.dCy} (d={Math.Max(Math.Abs(mt.dCx - here.X), Math.Abs(mt.dCy - here.Y))}, careful={mt.careful})");
					break;
				}
			}
			for (int i = items.Count - 1; i >= 0; i--)
			{
				if (items[i] is XRL.World.AI.GoalHandlers.Step st)
				{
					Cell next = here.GetCellFromDirection(st.Dir, !st.allowUnbuilt);
					if (next == null)
					{
						sb.Append($" | next {st.Dir}: off map");
						break;
					}
					sb.Append($" | next {st.Dir} {next.X},{next.Y} weight {next.GetNavigationWeightFor(who)} careful={st.careful} solid={next.IsSolidFor(who)}{(next == The.Player?.CurrentCell ? " PLAYER" : "")}:");
					int k = 0;
					foreach (GameObject o in next.Objects)
					{
						if (o == null || o.HasTag("Floor") || o.Render == null || !o.Render.Visible && o.Brain == null)
							continue;
						k++;
						sb.Append(' ').Append(o.Blueprint);
						if (o.Brain != null)
						{
							string kind = o.IsHostileTowards(who) ? "hostile"
								: o.Brain.PartyLeader != null && o.Brain.PartyLeader == who ? "my follower"
								: o.Brain.PartyLeader != null && o.Brain.PartyLeader == who.Brain.PartyLeader ? "same leader"
								: o.IsAlliedTowards(who) ? "ally"
								: "non-enemy";
							sb.Append($" ({o.ID}, {kind}{(o.IsMobile() ? "" : ", immobile")}, top {o.Brain.Goals.Peek()?.GetType().Name ?? "none"})");
						}
					}
					if (k == 0)
						sb.Append(" empty");
					break;
				}
			}
			sb.Append(" | beside:");
			int n = 0;
			foreach (Cell c in here.GetLocalAdjacentCells())
				foreach (GameObject o in c.GetObjectsWithPart("Combat"))
				{
					n++;
					sb.Append($" {o.Blueprint} ({o.ID}) at {c.X},{c.Y}{(o.IsHostileTowards(who) ? " hostile" : "")}{(o.Brain?.Goals.Items.Count > 0 ? " busy:" + o.Brain.Goals.Peek()?.GetType().Name : "")}");
				}
			if (n == 0)
				sb.Append(" none");
			Cleo_TerraFirma_CrewShift shift = who.GetPart<Cleo_TerraFirma_CrewShift>();
			if (shift != null)
				sb.Append(" | ").Append(shift.DiagState());
			return sb.ToString();
		}
	}
}
