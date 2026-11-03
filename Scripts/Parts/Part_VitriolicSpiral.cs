using System;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_VitriolicSpiral : IPart
	{
		public string Liquid = "acid";

		public string Damage = "1d3";

		public int Every = 2;

		public int Counter;

		public override bool SameAs(IPart p)
		{
			Cleo_TerraFirma_VitriolicSpiral other = p as Cleo_TerraFirma_VitriolicSpiral;
			if (other.Liquid != Liquid || other.Damage != Damage || other.Every != Every || other.Counter != Counter)
				return false;
			return base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == EquippedEvent.ID
				|| ID == UnequippedEvent.ID
				|| ID == GetShortDescriptionEvent.ID
				|| ID == SingletonEvent<GetDebugInternalsEvent>.ID;
		}

		public override bool HandleEvent(EquippedEvent E)
		{
			if (E.Item.IsEquippedProperly())
				E.Actor.RegisterEvent(this, EndTurnEvent.ID, 0, Serialize: true);
			Counter = 0;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnequippedEvent E)
		{
			E.Actor.UnregisterEvent(this, EndTurnEvent.ID);
			Counter = 0;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			Tick();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			E.Postfix.AppendRules("While you're petrifying, drips acid on you every other turn.");
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetDebugInternalsEvent E)
		{
			E.AddEntry(this, "Counter", Counter);
			E.AddEntry(this, "AcidDrams", AcidDrams());
			return base.HandleEvent(E);
		}

		public int AcidDrams()
		{
			LiquidVolume volume = ParentObject?.LiquidVolume;
			if (volume == null || volume.Volume <= 0)
				return 0;
			return volume.Amount(Liquid);
		}

		public static bool IsPetrifying(GameObject Who)
		{
			Cleo_TerraFirma_Petrifying fx = Who?.GetEffect<Cleo_TerraFirma_Petrifying>() ?? Who?.GetEffect<Cleo_TerraFirma_PetrifyingReflected>();
			return fx != null && fx.Duration > 0;
		}

		public bool Tick()
		{
			GameObject wearer = ParentObject?.Equipped;
			if (wearer == null || !IsPetrifying(wearer))
			{
				Counter = 0;
				return false;
			}
			if (Counter++ % Every != 0 || AcidDrams() <= 0)
				return false;
			ParentObject.LiquidVolume.UseDrams(Liquid, 1);
			wearer.TakeDamage(Damage.RollCached(), "from acid hissing out of " + ParentObject.t() + ".", "Acid", Source: ParentObject, Accidental: true);
			return true;
		}
	}
}
