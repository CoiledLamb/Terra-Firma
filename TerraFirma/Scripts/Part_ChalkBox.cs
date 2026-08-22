using System;
using System.Collections.Generic;
using XRL.Language;
using XRL.Rules;
using XRL.UI;

namespace XRL.World.Parts
{
	/// <summary>
	/// The box of chalk (design sitting 2026-08-22, all four forks user-ruled same day):
	/// (1) draw chalk lines on ADJACENT floor cells, sprite AUTO-ORIENTED - a new stroke looks
	/// for neighboring chalk and takes the connecting direction, else lies along the stroke the
	/// player drew; (2) write on base Chalkboards, typed AND markov (both ruled in), same prose
	/// whitelist as etching, no tool gate - chalk IS the tool; (3) 8-12 uses rolled at creation
	/// ("sounds fine!"), one per stroke or board-write, smearing free (that verb lives on the
	/// line, Part_ChalkLine). Spent box crumbles. Marks are permanent until smeared by ruling
	/// ("i don't really think it needs to wear off").
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_ChalkBox : IPart
	{
		public const string DRAW_COMMAND = "Cleo_TerraFirma_DrawChalk";
		public const string WRITE_COMMAND = "Cleo_TerraFirma_WriteChalk";
		public const string LINE_BLUEPRINT = "Cleo_TerraFirma_ChalkLine";
		public const string CORPUS = "LibraryCorpus.json";

		/// <summary>Strokes left in the box; rolled 8-12 at creation (user number).</summary>
		public int Uses = -1;

		public override bool SameAs(IPart p)
		{
			if ((p as Cleo_TerraFirma_ChalkBox).Uses != Uses)
			{
				return false;
			}
			return base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ObjectCreatedEvent.ID
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID
				|| ID == PooledEvent<GetDisplayNameEvent>.ID;
		}

		public override bool HandleEvent(ObjectCreatedEvent E)
		{
			if (Uses < 0)
			{
				Uses = Stat.Random(8, 12);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			if (Uses >= 0 && ParentObject.IsReal && ParentObject.IsTakeable())
			{
				E.AddTag("{{y|(" + Uses + ")}}");
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			E.AddAction("Draw", "draw a chalk line", DRAW_COMMAND, null, 'd', FireOnActor: false);
			if (FindAdjacentChalkboard(E.Actor) != null)
			{
				E.AddAction("Write", "write on the chalkboard", WRITE_COMMAND, null, 'w', FireOnActor: false);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == DRAW_COMMAND && E.Actor != null)
			{
				AttemptDraw(E);
			}
			else if (E.Command == WRITE_COMMAND && E.Actor != null)
			{
				AttemptWrite(E);
			}
			return base.HandleEvent(E);
		}

		private void AttemptDraw(InventoryActionEvent E)
		{
			if (Uses <= 0)
			{
				if (E.Actor.IsPlayer())
				{
					Popup.ShowFail("The box is empty."); // DRAFT
				}
				return;
			}
			Cell target = PickDirection("Draw where?", E.Actor); // DRAFT prompt (IComponent helper returns the cell)
			if (target == null || target == E.Actor.CurrentCell)
			{
				return;
			}
			string dir = E.Actor.CurrentCell.GetDirectionFromCell(target);
			if (target.HasWall() || target.HasObjectWithPart(nameof(Cleo_TerraFirma_ChalkLine)))
			{
				if (E.Actor.IsPlayer())
				{
					Popup.ShowFail("There is no clear floor there to chalk."); // DRAFT
				}
				return;
			}
			GameObject line = GameObject.Create(LINE_BLUEPRINT);
			// auto-orient (RULED): take the direction that connects to neighboring chalk if any,
			// else lie along the stroke the player just drew
			string facing = dir;
			foreach (string d in Directions.DirectionList)
			{
				Cell neighbor = target.GetCellFromDirection(d);
				if (neighbor != null && neighbor.HasObjectWithPart(nameof(Cleo_TerraFirma_ChalkLine)))
				{
					facing = d;
					break;
				}
			}
			Render render = line.GetPart<Render>();
			if (render != null)
			{
				render.Tile = "chalkline" + facing + ".png";
			}
			target.AddObject(line);
			Uses--;
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"line drawn {dir}, facing {facing}, {Uses} uses left");
			E.Actor.UseEnergy(1000, "Item DrawChalk");
			ConsumeIfSpent(E.Actor);
			E.RequestInterfaceExit();
		}

		private void AttemptWrite(InventoryActionEvent E)
		{
			if (Uses <= 0)
			{
				if (E.Actor.IsPlayer())
				{
					Popup.ShowFail("The box is empty."); // DRAFT
				}
				return;
			}
			GameObject board = FindAdjacentChalkboard(E.Actor);
			if (board == null || !E.Actor.IsPlayer())
			{
				return;
			}
			// typed-or-markov pick, the etch menu's shape; these two OPTION LINES are DRAFT
			// (etching's own pair is USER VOICE and stays etching's)
			int choice = Popup.PickOption(
				Intro: "The slate waits under a film of old dust.", // DRAFT
				Options: new string[] { "Write a message.", "Write from memory." }, // DRAFT
				AllowEscape: true);
			string text = null;
			if (choice == 0)
			{
				text = Popup.AskString("Chalk your message. For example: \"live and drink\".", "", "Sounds/UI/ui_notification",
					Cleo_TerraFirma_Etchable.TYPED_CHARS, null, Cleo_TerraFirma_Etchable.TYPED_MAX); // DRAFT
				if (text.IsNullOrEmpty())
				{
					return;
				}
			}
			else if (choice == 1)
			{
				MarkovBook.EnsureCorpusLoaded(CORPUS);
				if (!MarkovBook.CorpusData.TryGetValue(CORPUS, out MarkovChainData data))
				{
					IComponent<GameObject>.AddPlayerMessage("Nothing comes."); // DRAFT
					return;
				}
				for (int i = 0; i < 5; i++)
				{
					text = MarkovChain.GenerateShortSentence(data).TrimEnd(' ');
					if (!text.Contains("="))
					{
						break;
					}
				}
			}
			else
			{
				return;
			}
			Cleo_TerraFirma_ChalkWriting writing = board.RequirePart<Cleo_TerraFirma_ChalkWriting>();
			writing.Text = text;
			Uses--;
			Popup.Show("You chalk \"" + text + "\" onto " + board.t() + "."); // DRAFT
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"chalkboard written ({(choice == 0 ? "typed" : "markov")}), {Uses} uses left");
			E.Actor.UseEnergy(1000, "Item WriteChalk");
			ConsumeIfSpent(E.Actor);
			E.RequestInterfaceExit();
		}

		private void ConsumeIfSpent(GameObject actor)
		{
			if (Uses <= 0)
			{
				if (actor.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage("The last stick of chalk crumbles to dust."); // DRAFT
				}
				ParentObject.Destroy();
			}
		}

		private static GameObject FindAdjacentChalkboard(GameObject actor)
		{
			if (actor?.CurrentCell == null)
			{
				return null;
			}
			foreach (Cell cell in actor.CurrentCell.GetLocalAdjacentCells())
			{
				GameObject board = cell.FindObject("Chalkboard");
				if (board != null)
				{
					return board;
				}
			}
			return null;
		}
	}

	/// <summary>
	/// Chalk words on a chalkboard: one string, shown in look text, overwritten by the next
	/// writer. The Graffitied/etch storage idiom (plain field, serialization-safe by construction).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_ChalkWriting : IPart
	{
		public string Text;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetShortDescriptionEvent.ID;
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			if (!Text.IsNullOrEmpty())
			{
				E.Postfix.AppendRules("Chalked across it: \"" + Text + "\""); // DRAFT frame
			}
			return base.HandleEvent(E);
		}
	}
}
