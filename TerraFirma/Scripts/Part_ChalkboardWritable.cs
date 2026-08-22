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
				|| ID == InventoryActionEvent.ID;
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (E.Actor != null && Cleo_TerraFirma_ChalkBox.FindCarriedBox(E.Actor) != null)
			{
				E.AddAction("Write", "write with chalk", WRITE_COMMAND, null, 'w', FireOnActor: false);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == WRITE_COMMAND && E.Actor != null && E.Actor.IsPlayer())
			{
				Cleo_TerraFirma_ChalkBox box = Cleo_TerraFirma_ChalkBox.FindCarriedBox(E.Actor);
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
				E.Actor.PlayWorldSound("Sounds/Interact/sfx_interact_tattoo"); // PLACEHOLDER sound - sound pass flagged
				Popup.Show("You chalk \"" + text + "\" onto " + ParentObject.t() + "."); // DRAFT
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"chalkboard written ({(choice == 0 ? "typed" : "markov")})");
				E.Actor.UseEnergy(1000, "Item WriteChalkboard");
				box.ConsumeUse(E.Actor);
				E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
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
