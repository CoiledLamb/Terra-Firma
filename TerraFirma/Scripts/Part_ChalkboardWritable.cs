using System;
using XRL.UI;

namespace XRL.World.Parts
{
	/// <summary>
	/// The write action on base Chalkboards (merged on via Merges.xml). Lives on the BOARD, not
	/// the chalk, per the user's ergonomics ruling 2026-08-22 ("the action should be on the thing
	/// you're doing the action on ... etch is a good example, where you select the wall to etch").
	/// Shown only when the actor carries a chalk box with strokes left; typed AND markov paths
	/// (both user-ruled), etch charset, one use consumed from the fullest carried box. Words land
	/// in Cleo_TerraFirma_ChalkWriting below - one string, overwritten by the next writer.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_ChalkboardWritable : IPart
	{
		public const string WRITE_COMMAND = "Cleo_TerraFirma_WriteChalkboard";
		public const string CORPUS = "LibraryCorpus.json";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID
				|| ID == CanSmartUseEvent.ID
				|| ID == CommandSmartUseEvent.ID;
		}

		// space/interact on the board = the write prompt while chalk is carried (user ruling
		// 2026-08-22; first build wired only the menu Default, but the space path is the
		// SmartUse event pair - Chair's Sit is the decomp precedent, gates included)
		private bool CanWrite(GameObject actor)
		{
			return actor != null && actor != ParentObject && ParentObject.Understood()
				&& actor.PhaseMatches(ParentObject) && actor.FlightCanReach(ParentObject)
				&& Cleo_TerraFirma_ChalkBox.FindCarriedBox(actor) != null;
		}

		public override bool HandleEvent(CanSmartUseEvent E)
		{
			if (CanWrite(E.Actor))
			{
				return false;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(CommandSmartUseEvent E)
		{
			if (CanWrite(E.Actor))
			{
				AttemptWrite(E.Actor);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (E.Actor != null && Cleo_TerraFirma_ChalkBox.FindCarriedBox(E.Actor) != null)
			{
				// Default 5 = the Chair "Sit" precedent: pressing space on the board while
				// holding chalk fires the write prompt directly (user ruling 2026-08-22)
				E.AddAction("Write", "write with chalk", WRITE_COMMAND, null, 'w', FireOnActor: false, Default: 5);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == WRITE_COMMAND && E.Actor != null)
			{
				if (AttemptWrite(E.Actor))
				{
					E.RequestInterfaceExit();
				}
			}
			return base.HandleEvent(E);
		}

		private bool AttemptWrite(GameObject actor)
		{
			if (actor != null && actor.IsPlayer())
			{
				Cleo_TerraFirma_ChalkBox box = Cleo_TerraFirma_ChalkBox.FindCarriedBox(actor);
				if (box == null)
				{
					Popup.ShowFail("You have no chalk."); // DRAFT
					return false;
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
						return false;
					}
				}
				else if (choice == 1)
				{
					MarkovBook.EnsureCorpusLoaded(CORPUS);
					if (!MarkovBook.CorpusData.TryGetValue(CORPUS, out MarkovChainData data))
					{
						IComponent<GameObject>.AddPlayerMessage("Nothing comes."); // DRAFT
						return false;
					}
					// reroll template leftovers, MarkovBook's own page-loop idiom
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
					return false;
				}
				Cleo_TerraFirma_ChalkWriting writing = ParentObject.RequirePart<Cleo_TerraFirma_ChalkWriting>();
				writing.Text = text;
				// no write sfx by ruling 2026-08-22 (tattoo-gun placeholder removed; the sound
				// pass picks a real one if any)
				Popup.Show("You chalk \"" + text + "\" onto " + ParentObject.t() + "."); // DRAFT
				// write beat AFTER the modal (bug catch 2026-08-23 "puffs are almost invisible":
				// fired before Popup.Show, the whole 0.3s beat played out behind the popup);
				// board profile throws full radius so the dust clears the board's own sprite.
				// escape from either prompt never gets here
				Cleo.TerraFirma.Scripts.Cleo_TerraFirma_ChalkRevealFX.PlayBoardScuff(ParentObject.CurrentCell);
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"chalkboard written ({(choice == 0 ? "typed" : "markov")})");
				actor.UseEnergy(1000, "Item WriteChalkboard");
				box.ConsumeUse(actor);
				return true;
			}
			return false;
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
				// Graffitied.AddGraffiti's frame verbatim (ruled 2026-08-22): infix prose, blank
				// line, quote alone on its own line. One word swapped in the frame line (DRAFT,
				// user's to reword); words fixed chalk-white where graffiti uses the spray color
				if (E.Infix.Length > 0)
				{
					E.Infix.Append("\n");
				}
				E.Infix.Append("\n").Append("Chalk is scrawled across the surface. It reads: \n\n\"")
					.Append("{{Y|").Append(Text).Append("}}").Append("\"\n");
			}
			return base.HandleEvent(E);
		}
	}
}
