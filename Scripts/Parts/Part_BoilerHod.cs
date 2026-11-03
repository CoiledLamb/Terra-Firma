using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_BoilerHod : IPoweredPart
	{
		public string Liquid = "water";

		public int ChargePerDram = 55;

		public int ChargeCounter;

		public Cleo_TerraFirma_BoilerHod()
		{
			ChargeUse = 0;
			IsBootSensitive = false;
			IsEMPSensitive = false;
			WorksOnSelf = true;
		}

		public override bool SameAs(IPart p)
		{
			Cleo_TerraFirma_BoilerHod other = p as Cleo_TerraFirma_BoilerHod;
			if (other.Liquid != Liquid || other.ChargePerDram != ChargePerDram || other.ChargeCounter != ChargeCounter)
				return false;
			return base.SameAs(p);
		}

		public int FuelDrams()
		{
			LiquidVolume volume = ParentObject?.LiquidVolume;
			if (volume == null || volume.Volume <= 0)
				return 0;
			return volume.Amount(Liquid);
		}

		public int GetCharge()
		{
			int drams = FuelDrams();
			if (drams <= 0)
				return 0;
			return drams * ChargePerDram - ChargeCounter;
		}

		public void UseCharge(int Amount)
		{
			if (FuelDrams() <= 0)
				return;
			ChargeCounter += Amount;
			while (ChargeCounter >= ChargePerDram)
			{
				ParentObject.LiquidVolume.UseDrams(Liquid, 1);
				if (FuelDrams() <= 0)
				{
					ChargeCounter = 0;
					break;
				}
				ChargeCounter -= ChargePerDram;
			}
		}

		public override bool GetActivePartLocallyDefinedFailure()
		{
			if (FuelDrams() <= 0)
				return true;
			return base.GetActivePartLocallyDefinedFailure();
		}

		public override string GetActivePartLocallyDefinedFailureDescription()
		{
			if (FuelDrams() <= 0)
				return "ProcessInputMissing";
			return base.GetActivePartLocallyDefinedFailureDescription();
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == QueryChargeEvent.ID
				|| ID == TestChargeEvent.ID
				|| ID == UseChargeEvent.ID
				|| ID == GetShortDescriptionEvent.ID
				|| ID == SingletonEvent<GetDebugInternalsEvent>.ID;
		}

		private bool Ready()
		{
			return IsReady(UseCharge: false, IgnoreCharge: false, IgnoreLiquid: false, IgnoreBootSequence: false, IgnoreBreakage: false, IgnoreRust: false, IgnoreEMP: false, IgnoreRealityStabilization: false, IgnoreSubject: false, IgnoreLocallyDefinedFailure: false, 1, null, UseChargeIfUnpowered: false, 0L);
		}

		public override bool HandleEvent(QueryChargeEvent E)
		{
			if (!E.LiveOnly && Ready())
			{
				int left = GetCharge() - ChargeUse * E.Multiple;
				if (left > 0)
					E.Amount += left;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(TestChargeEvent E)
		{
			if (!E.LiveOnly && Ready())
			{
				int left = GetCharge() - ChargeUse * E.Multiple;
				if (left > 0)
				{
					E.Amount -= left;
					if (E.Amount <= 0)
						return false;
				}
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UseChargeEvent E)
		{
			if (E.Pass == 2 && !E.LiveOnly && Ready())
			{
				int own = ChargeUse * E.Multiple;
				int take = Math.Min(E.Amount, GetCharge() - own);
				if (take > 0)
				{
					UseCharge(take + own);
					E.Amount -= take;
					if (E.Amount <= 0)
						return false;
				}
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			Suspensor suspensor = ParentObject.GetPart<Suspensor>();
			if (suspensor != null && suspensor.Force > 0)
				E.Postfix.AppendRules("When powered, reduces carried weight by " + suspensor.Force + " lbs.");
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetDebugInternalsEvent E)
		{
			E.AddEntry(this, "ChargePerDram", ChargePerDram);
			E.AddEntry(this, "ChargeCounter", ChargeCounter);
			E.AddEntry(this, "FuelDrams", FuelDrams());
			return base.HandleEvent(E);
		}
	}
}
