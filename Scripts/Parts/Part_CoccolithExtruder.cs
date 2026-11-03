using System;
using XRL.UI;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_CoccolithExtruder : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_CommandCoccolithDraw";
		public const string THROW_PROPERTY = "ThrowRangeBonus";

		public int Sticks = -1;
		public int MaxSticks = 12;
		public int RegrowTurns = 100;
		public int SticksPerRange = 6;
		public int MaxRange = 2;
		public int RegrowCounter;
		public int Applied;

		public string commandId = "";
		public Guid ActivatedAbilityID = Guid.Empty;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool WantTurnTick()
		{
			return true;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ObjectCreatedEvent.ID
				|| ID == ImplantedEvent.ID
				|| ID == UnimplantedEvent.ID
				|| ID == PooledEvent<CommandEvent>.ID;
		}

		public override bool HandleEvent(ObjectCreatedEvent E)
		{
			if (Sticks < 0)
				Sticks = MaxSticks;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ImplantedEvent E)
		{
			if (Sticks < 0)
				Sticks = MaxSticks;
			ActivatedAbilityID = E.Implantee.AddDynamicCommand(out commandId, COMMAND, AbilityName(), "Cybernetics",
				Description: "Draw a chalk line on an adjacent square.",
				Icon: "*");
			Sync(E.Implantee);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnimplantedEvent E)
		{
			E.Implantee.RemoveActivatedAbility(ref ActivatedAbilityID);
			if (Applied != 0)
				E.Implantee.ModIntProperty(THROW_PROPERTY, -Applied, RemoveIfZero: true);
			Applied = 0;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command == commandId && !commandId.IsNullOrEmpty() && E.Actor != null && E.Actor == ParentObject.Implantee)
				AttemptDraw(E.Actor);
			return base.HandleEvent(E);
		}

		public override void TurnTick(long TimeTick, int Amount)
		{
			GameObject who = ParentObject.Implantee;
			if (who == null || Sticks >= MaxSticks)
			{
				RegrowCounter = 0;
				return;
			}
			RegrowCounter += Amount;
			if (RegrowCounter < RegrowTurns)
				return;
			Sticks = Math.Min(MaxSticks, Sticks + RegrowCounter / RegrowTurns);
			RegrowCounter %= RegrowTurns;
			Sync(who);
		}

		private void AttemptDraw(GameObject actor)
		{
			if (!actor.CanMoveExtremities(null, ShowMessage: true, Involuntary: false, AllowTelekinetic: true))
				return;
			if (Sticks <= 0)
			{
				if (actor.IsPlayer())
					Popup.ShowFail("You have no chalk.");
				return;
			}
			if (Cleo_TerraFirma_ChalkBox.DrawStroke(this, actor, Sticks - 1, "Sounds/Interact/sfx_interact_crayon_draw", 0.5f))
				Spend(actor);
		}

		public void Spend(GameObject actor, int n = 1)
		{
			Sticks = Math.Max(0, Sticks - n);
			Sync(actor);
		}

		public int RangeBonus => Math.Min(MaxRange, Math.Max(0, Sticks) / SticksPerRange);

		private string AbilityName() => "Draw with Chalk (" + Math.Max(0, Sticks) + ")";

		public void Sync(GameObject who)
		{
			if (who == null || who != ParentObject.Implantee)
				return;
			ActivatedAbilityEntry ent = who.GetActivatedAbility(ActivatedAbilityID);
			if (ent != null)
				ent.DisplayName = AbilityName();
			int want = RangeBonus;
			if (want != Applied)
			{
				who.ModIntProperty(THROW_PROPERTY, want - Applied, RemoveIfZero: true);
				Applied = want;
			}
		}
	}
}
