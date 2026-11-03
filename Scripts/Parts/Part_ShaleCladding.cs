using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_ShaleCladding : IPart
	{
		public int MaxLayers = 3;
		public int RegrowTurns = 10;

		public int Layers = -1;
		public int RegrowProgress;

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
				|| ID == ImplantedEvent.ID
				|| ID == UnimplantedEvent.ID
				|| ID == TookDamageEvent.ID
				|| ID == GetShortDescriptionEvent.ID;
		}

		public override bool HandleEvent(ImplantedEvent E)
		{
			if (Layers < 0)
				Layers = MaxLayers;
			ApplyAV(E.Implantee);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnimplantedEvent E)
		{
			StatShifter.RemoveStatShifts(E.Implantee);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(TookDamageEvent E)
		{
			GameObject who = ParentObject.Implantee;
			if (who != null && E.Object == who && E.Damage != null && E.Damage.Amount > 0 && Layers > 0
				&& (E.Damage.HasAttribute("Melee") || E.Projectile != null || E.Weapon != null))
				Flake(who);
			return base.HandleEvent(E);
		}

		public void Flake(GameObject who)
		{
			if (Layers <= 0)
				return;
			Layers--;
			RegrowProgress = 0;
			ApplyAV(who);
			if (who.IsPlayer())
				IComponent<GameObject>.AddPlayerMessage("A layer of shale flakes off your skin.");
		}

		public override void TurnTick(long TimeTick, int Amount)
		{
			GameObject who = ParentObject.Implantee;
			if (who == null || Layers >= MaxLayers)
				return;
			RegrowProgress += Amount;
			while (RegrowProgress >= RegrowTurns && Layers < MaxLayers)
			{
				RegrowProgress -= RegrowTurns;
				Layers++;
			}
			if (Layers >= MaxLayers)
				RegrowProgress = 0;
			ApplyAV(who);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			if (ParentObject.Implantee != null)
				E.Postfix.AppendRules($"Shale layers: {Math.Max(Layers, 0)} of {MaxLayers}.");
			return base.HandleEvent(E);
		}

		public void ApplyAV(GameObject who)
		{
			if (Layers > 0)
				StatShifter.SetStatShift(who, "AV", Layers);
			else
				StatShifter.RemoveStatShift(who, "AV");
		}
	}
}
