using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	/// <summary>
	/// The figurine animation table (design sitting 2026-08-24, all forks user-ruled; see the
	/// journal entry). Exchange verb on the TABLE per the mod-wide target-side ergonomics
	/// ruling: offer "Animate" when the interactor carries any-material figurine, pick one,
	/// pay a flat charge from the table's INTERNAL EnergyCell (ruled: internal cell, no
	/// socket - chargeable via tinker recharge, unstealable because no removal verb exists),
	/// destroy the figurine, wake a bonded walker beside the table.
	///
	/// The walker is a "statue in miniature" (ruled): the mason-escort chassis re-statted by
	/// the stoneshaper mutation's own law at STONESHAPER_LEVEL 1 (level 1 = CLAUDE PICK,
	/// delegated from "lvl 1 or 2 (your pick)"), Swarmer kept by ruling, display name echoing
	/// the figurine, and Brain.Allegiance entries copied from the depicted creature (the
	/// faction-feeling reading, delegated pick: same list base RandomFigurine feeds its
	/// AddsRep from). Fists stay chassis default rather than the statue-fist body swap;
	/// flagged as a dial in the journal.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_AnimationTablePart : IPart
	{
		public const string ANIMATE_COMMAND = "Cleo_TerraFirma_AnimateFigurine";

		/// <summary>The walker chassis, previously a literal in two method bodies (creation
		/// spawn + BuildWalker). XML-settable since the 2026-08-29 ladder pass; coincidentally
		/// the same string as ANIMATE_COMMAND, which stays a command id.</summary>
		public string WalkerBlueprint = "Cleo_TerraFirma_AnimateFigurine";

		/// <summary>Flat charge per animation (RULED flat; number is a DIAL).</summary>
		public int AnimationCost = 1000;

		/// <summary>The stoneshaper level the table works at. CLAUDE PICK 1 (delegated):
		/// statue law makes 1-vs-2 purely 25 vs 50 HP, and "miniature" argues the floor.</summary>
		public int StoneshaperLevel = 1;

		/// <summary>One-shot guard for the creation spawn (RULED 2026-08-24, "the table should
		/// try to spawn the figurine creature in an adjacent tile when created"): a stock
		/// animate figurine placed beside the table on its first cell entry, HasFigurines
		/// grammar. UNBONDED on purpose - its blueprint is already quarriers-100, so it
		/// loiters the warren as the table's product on display rather than following a
		/// piece of furniture around.</summary>
		public bool CreationSpawnDone;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| (ID == EnteredCellEvent.ID && !CreationSpawnDone)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == CanSmartUseEvent.ID
				|| ID == CommandSmartUseEvent.ID
				|| ID == InventoryActionEvent.ID;
		}

		public override bool HandleEvent(EnteredCellEvent E)
		{
			if (!CreationSpawnDone)
			{
				CreationSpawnDone = true;
				Cell spot = ParentObject.CurrentCell?.GetFirstEmptyAdjacentCell(1, 2);
				if (spot != null)
				{
					spot.AddObject(GameObject.Create(WalkerBlueprint)).MakeActive();
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("FIGTABLE", "creation spawn: ambient figurine placed beside the table");
				}
				else
				{
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("FIGTABLE", "creation spawn: no open adjacent tile, skipped");
				}
			}
			return base.HandleEvent(E);
		}

		// Codex PROPOSAL: native furniture smart-use route, sharing the menu handler.
		private bool CanAnimate(GameObject actor)
		{
			return actor != null && actor != ParentObject && ParentObject.Understood()
				&& actor.PhaseMatches(ParentObject) && actor.FlightCanReach(ParentObject)
				&& FindCarriedFigurines(actor).Count > 0;
		}

		public override bool HandleEvent(CanSmartUseEvent E)
		{
			if (CanAnimate(E.Actor)) return false;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(CommandSmartUseEvent E)
		{
			if (CanAnimate(E.Actor))
				InventoryActionEvent.Check(ParentObject, E.Actor, ParentObject, ANIMATE_COMMAND);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (FindCarriedFigurines(E.Actor).Count > 0)
			{
				E.AddAction("Animate", "animate a figurine", ANIMATE_COMMAND, null, 'a', FireOnActor: false, Default: 10);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == ANIMATE_COMMAND)
			{
				List<GameObject> figurines = FindCarriedFigurines(E.Actor);
				if (figurines.Count == 0)
				{
					if (E.Actor.IsPlayer())
						Popup.ShowFail("You carry no figurine to animate."); // DRAFT
					return false;
				}
				if (ParentObject.QueryCharge() < AnimationCost)
				{
					if (E.Actor.IsPlayer())
						Popup.ShowFail(ParentObject.Does("lie") + " dormant; its cell wants for charge."); // DRAFT
					return false;
				}
				GameObject pick;
				if (E.Actor.IsPlayer())
				{
					List<string> options = new List<string>();
					foreach (GameObject fig in figurines)
						options.Add(fig.DisplayName);
					int choice = Popup.PickOption(
						Intro: "Animate which figurine?", // DRAFT
						Options: options.ToArray(),
						AllowEscape: true);
					if (choice < 0)
						return false;
					pick = figurines[choice];
				}
				else
				{
					pick = figurines[0];
				}
				pick = pick.SplitFromStack() ?? pick;
				Cell spot = ParentObject.CurrentCell?.GetFirstEmptyAdjacentCell(1, 2);
				if (spot == null)
				{
					if (E.Actor.IsPlayer())
						Popup.ShowFail("There is no open ground beside " + ParentObject.t() + "."); // DRAFT
					return false;
				}
				if (!ParentObject.UseCharge(AnimationCost))
					return false;

				GameObject walker = BuildWalker(pick, E.Actor);
				spot.AddObject(walker).MakeActive();
				if (E.Actor.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage("You set " + pick.t() + " on the table. Stone hums against stone, and it steps down animate."); // DRAFT
				}
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("FIGTABLE",
					$"animated {pick.Blueprint} -> {walker.DisplayNameOnlyStripped} (charge left {ParentObject.QueryCharge()})");
				pick.Destroy();
				E.Actor.UseEnergy(1000, "Item AnimateFigurine");
				E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
		}

		private static List<GameObject> FindCarriedFigurines(GameObject actor)
		{
			List<GameObject> found = new List<GameObject>();
			if (actor?.Inventory != null)
			{
				foreach (GameObject obj in actor.Inventory.GetObjectsDirect())
				{
					if (obj.HasPart<RandomFigurine>())
						found.Add(obj);
				}
			}
			return found;
		}

		/// <summary>Wake the walker: escort chassis + statue law at StoneshaperLevel + the
		/// figurine's identity and faction feeling + bond to the animator.</summary>
		private GameObject BuildWalker(GameObject figurine, GameObject animator)
		{
			GameObject walker = GameObject.Create(WalkerBlueprint);

			// statue law, the mutation's own formulas - "statues in miniature"
			walker.GetStat("Hitpoints").BaseValue = Cleo_TerraFirma_Stoneshaper.GetHP(StoneshaperLevel);
			walker.GetStat("AV").BaseValue = Cleo_TerraFirma_Stoneshaper.GetAV(StoneshaperLevel);

			// identity echo: "animate stone snapjaw figurine"
			walker.Render.DisplayName = "animate " + figurine.DisplayNameOnlyStripped;

			// description echo (RULED 2026-08-24, "the description should copy the figurine
			// description"): the walker wears the item's own text verbatim, material and
			// depicted-creature passage included, replacing its generated ambient identity.
			Description figDesc = figurine.GetPart<Description>();
			Description walkerDesc = walker.GetPart<Description>();
			if (figDesc != null && walkerDesc != null)
				walkerDesc._Short = figDesc._Short;

			// faction feeling: the same allegiance list base RandomFigurine feeds its AddsRep
			// from, copied onto the walker's own brain (quarriers membership kept from the
			// blueprint; only new keys added)
			RandomFigurine figPart = figurine.GetPart<RandomFigurine>();
			if (figPart?.Creature != null && walker.Brain != null)
			{
				GameObject sample = GameObjectFactory.Factory.CreateSampleObject(figPart.Creature);
				if (sample?.Brain != null)
				{
					foreach (KeyValuePair<string, int> kv in sample.Brain.Allegiance)
					{
						if (!walker.Brain.Allegiance.ContainsKey(kv.Key))
							walker.Brain.Allegiance.Add(kv.Key, kv.Value);
					}
				}
			}

			walker.AddPart(new Cleo_TerraFirma_BondedFigurinePart(animator));
			return walker;
		}
	}
}
