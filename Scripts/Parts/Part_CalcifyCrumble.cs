using System;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_CalcifyCrumble : IPart
	{
		public int Duration;

		public long LastTurn = long.MaxValue;

		public Cleo_TerraFirma_CalcifyCrumble() { }

		public Cleo_TerraFirma_CalcifyCrumble(int Duration)
		{
			this.Duration = Duration;
		}

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != ZoneActivatedEvent.ID && ID != ZoneThawedEvent.ID)
				return ID == SingletonEvent<GetDebugInternalsEvent>.ID;
			return true;
		}

		public override bool HandleEvent(GetDebugInternalsEvent E)
		{
			E.AddEntry(this, nameof(Duration), Duration);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ZoneActivatedEvent E)
		{
			CatchUp();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ZoneThawedEvent E)
		{
			CatchUp();
			return base.HandleEvent(E);
		}

		private void CatchUp()
		{
			int missed = (int)Math.Max(The.Game.Turns - LastTurn, 0L);
			LastTurn = The.Game.Turns;
			if (Duration > 0 && (Duration -= missed) <= 0)
				Crumble();
		}

		public override bool WantTurnTick() => true;

		public override void TurnTick(long TimeTick, int Amount)
		{
			LastTurn = TimeTick;
			if (Duration > 0 && (Duration -= Amount) <= 0)
				Crumble();
		}

		private void Crumble()
		{
			GameObject wall = ParentObject;
			Cell cell = wall.CurrentCell;
			Helpers.VerifyLog("CALC", $"wall crumbled @({cell?.X},{cell?.Y}): {wall.Blueprint}");
			if (cell != null && cell.IsVisible())
			{
				IComponent<GameObject>.AddPlayerMessage(wall.Does("crumble") + ".");
				Mutation.Cleo_TerraFirma_Fissure.SmokePuff(cell, XRL.Rules.Stat.Random(1, 2));
			}
			wall.Obliterate(Silent: true);
			if (cell?.ParentZone != null)
				ZoneManager.PaintWalls(cell.ParentZone, cell.X - 1, cell.Y - 1, cell.X + 1, cell.Y + 1);
		}
	}
}
