using System;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	/// <summary>
	/// Standing trail cairn (user docket 2026-08-25): starts upright, topples when a
	/// creature steps on it, and toppling an OWNED cairn is a hostile act against the
	/// owner. Mechanism is the engine's own ownership idiom, verified in the decompile:
	/// Physics.Owner (the Barathrumite-furniture pattern) gates damage via
	/// OkayToDamageEvent (Physics.cs HandleEvent returns false when Owner is set), and
	/// Physics.BroadcastForHelp sends AIHelpBroadcastEvent with HelpCause.Trespass at
	/// radius 20 - nearby members of the owner's faction come running. Step channel
	/// mirrors Cleo_TerraFirma_FallenRockPart (ObjectEnteredCellEvent, combat-object +
	/// phase/flight gates). Members of the owner faction never topple their own
	/// waystones. Owner assignment WIRED 2026-08-29 (user: "please fix the trespass, it
	/// was working off outdated information"; the old deferral note predated the guild,
	/// which has existed since 2026-08-15): WarrenCarver stamps
	/// Physics.Owner = "Cleo_TerraFirma_Quarriers" on warren-placed cairns at dress time.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_CairnTopple : IPart
	{
		/// <summary>What the cairn becomes underfoot.</summary>
		public string ToppledBlueprint = "Cleo_TerraFirma_CairnToppled";

		/// <summary>Sound on topple (user note 3, 2026-08-25: base SmallBoulder's own impact
		/// sound, medium fits a knee-high stack). Played via PlayWorldSound in the topple
		/// handler, so the stock ImpactSound tag (a thrown-weapon seam) would never fire here;
		/// a field is the honest XML dial. Settable since the 2026-08-29 ladder pass.</summary>
		public string ToppleSound = "Sounds/Throw/sfx_throwing_stone_medium_impact";

		/// <summary>Pathfinding weight advertised to smart actors (7 = FallenRockPart's own
		/// mine-grade "route around", user docket 2026-08-25): nobody wanders through a
		/// waystone by accident, so wildlife stops toppling them in the site's off-hours;
		/// a deliberate step still topples.</summary>
		public int AvoidWeight = 7;

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) &&
				ID != ObjectEnteredCellEvent.ID &&
				ID != GetNavigationWeightEvent.ID)
				return ID == PooledEvent<InterruptAutowalkEvent>.ID;
			return true;
		}

		public override bool HandleEvent(GetNavigationWeightEvent E)
		{
			if (E.Smart && E.PhaseMatches(ParentObject))
				E.MinWeight(AvoidWeight);
			return base.HandleEvent(E);
		}

		// a step that would anger somebody should not happen on autopilot
		public override bool HandleEvent(InterruptAutowalkEvent E)
		{
			E.IndicateObject = ParentObject;
			return false;
		}

		public override bool HandleEvent(ObjectEnteredCellEvent E)
		{
			GameObject stepper = E.Object;
			if (GameObject.Validate(ref stepper) && stepper != ParentObject &&
				stepper.IsCombatObject() && stepper.PhaseAndFlightMatches(ParentObject))
			{
				string owner = ParentObject.Physics?.Owner;
				// the owner's people know their own waystones
				if (!string.IsNullOrEmpty(owner) && stepper.GetPrimaryFaction() == owner)
					return base.HandleEvent(E);
				Cell here = ParentObject.CurrentCell;
				if (here == null)
					return base.HandleEvent(E);
				GameObject toppled = GameObject.Create(ToppledBlueprint);
				if (!string.IsNullOrEmpty(owner) && toppled.Physics != null)
					toppled.Physics.Owner = owner;   // the scatter is still theirs
				if (Visible())
					XDidY(stepper, "topple", ParentObject.the + ParentObject.ShortDisplayName, "!", ColorAsBadFor: stepper);   // draft, approved 2026-09-30
				ParentObject.PlayWorldSound(ToppleSound, 1f);
				if (!string.IsNullOrEmpty(owner))
					ParentObject.Physics?.BroadcastForHelp(stepper);   // the hostile act, engine's own trespass channel
				Helpers.VerifyLog("CAIRN", $"toppled by {stepper.Blueprint}{(string.IsNullOrEmpty(owner) ? "" : $" (owner {owner}, help broadcast)")}");
				here.AddObject(toppled);
				ParentObject.Destroy(null, Silent: true);
			}
			return base.HandleEvent(E);
		}
	}

	/// <summary>
	/// Restacking (B09, user 2026-09-29: "just tidying"): the inverse of the topple, carried
	/// by the toppled scatter. An inventory action swaps the standing blueprint back in with
	/// the owner carried over. Deliberately no reputation effect; the topple's help broadcast
	/// has no native undo and nothing here tries to fake one. Action idiom copies base Chair
	/// (GetInventoryActionsEvent / InventoryActionEvent) and our own animation table.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_CairnRestack : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_RestackCairn";

		/// <summary>What the scatter becomes when restacked.</summary>
		public string StandingBlueprint = "Cleo_TerraFirma_Cairn";

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) &&
				ID != GetInventoryActionsEvent.ID &&
				ID != CanSmartUseEvent.ID &&
				ID != CommandSmartUseEvent.ID)
				return ID == InventoryActionEvent.ID;
			return true;
		}

		// Space restacks: the Harvestable precedent, a ground object whose one use changes it.
		// Same routing as our animation table: smart-use forwards to the inventory action.
		public override bool HandleEvent(CanSmartUseEvent E) => false;

		public override bool HandleEvent(CommandSmartUseEvent E)
		{
			InventoryActionEvent.Check(ParentObject, E.Actor, ParentObject, COMMAND);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			E.AddAction("Restack", "restack", COMMAND, null, 'r', FireOnActor: false, Default: 10);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == COMMAND)
			{
				if (E.Actor != null && !E.Actor.CanMoveExtremities(null, ShowMessage: true, Involuntary: false, AllowTelekinetic: true)) // frozen: base hand-action gate
					return false;
				Cell here = ParentObject.CurrentCell;
				if (here == null)
					return false;
				// a stack under someone's feet would topple the moment they stepped back on.
				// Wording follows base Door.cs:657 ("cannot be closed with X in the way").
				GameObject blocker = here.GetFirstObject(o => o.IsCombatObject());
				if (blocker != null)
					return E.Actor.ShowFailure(ParentObject.T() + " cannot be restacked with " + (blocker.IsPlayer() ? "you" : blocker.t()) + " in the way.");
				GameObject standing = GameObject.Create(StandingBlueprint);
				string owner = ParentObject.Physics?.Owner;
				if (!string.IsNullOrEmpty(owner) && standing.Physics != null)
					standing.Physics.Owner = owner;
				if (E.Actor.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage("You stack the stones back into a cairn."); // draft, approved 2026-09-30
				Helpers.VerifyLog("CAIRN", $"restacked by {E.Actor.Blueprint}{(string.IsNullOrEmpty(owner) ? "" : $" (owner {owner} kept)")}");
				here.AddObject(standing);
				ParentObject.Destroy(null, Silent: true);
				E.Actor.UseEnergy(1000, "Item RestackCairn");
				E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
		}
	}
}
