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
	/// waystones. Owner assignment at spawn is DEFERRED: no warren faction exists yet
	/// (population lane unbuilt), so the hostility channel is latent until one does.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_CairnTopple : IPart
	{
		/// <summary>What the cairn becomes underfoot.</summary>
		public string ToppledBlueprint = "Cleo_TerraFirma_CairnToppled";

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
					XDidY(stepper, "topple", ParentObject.the + ParentObject.ShortDisplayName, "!", ColorAsBadFor: stepper);   // DRAFT line, voice pass owed
				// rocks coming down (user note 3, 2026-08-25): base SmallBoulder's own
				// impact sound, verified in Items.xml; medium fits a knee-high stack
				ParentObject.PlayWorldSound("Sounds/Throw/sfx_throwing_stone_medium_impact", 1f);
				if (!string.IsNullOrEmpty(owner))
					ParentObject.Physics?.BroadcastForHelp(stepper);   // the hostile act, engine's own trespass channel
				Helpers.VerifyLog("CAIRN", $"toppled by {stepper.Blueprint}{(string.IsNullOrEmpty(owner) ? "" : $" (owner {owner}, help broadcast)")}");
				here.AddObject(toppled);
				ParentObject.Destroy(null, Silent: true);
			}
			return base.HandleEvent(E);
		}
	}
}
