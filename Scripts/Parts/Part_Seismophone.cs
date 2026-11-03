using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_Seismophone : MutationOnEquip
	{
		public bool WasGranted;

		public bool HadSelfEffect;

		public Cleo_TerraFirma_Seismophone()
		{
			ClassName = "HeightenedHearing";
			Level = 2;
			ChargeUse = 1;
			IsEMPSensitive = true;
			IsPowerSwitchSensitive = true;
			IsTechScannable = true;
		}

		public override bool GetActivePartLocallyDefinedFailure()
		{
			GameObject wearer = ParentObject?.Equipped;
			Cell cell = wearer?.CurrentCell;
			if (cell == null)
				return true;
			foreach (Cell adj in cell.GetLocalAdjacentCells())
			{
				if (adj.HasWall())
					return false;
			}
			return true;
		}

		public override string GetActivePartLocallyDefinedFailureDescription()
		{
			return "NoWallContact";
		}

		public override bool HandleEvent(EquippedEvent E)
		{
			E.Actor.RegisterEvent(this, ApplyEffectEvent.ID, 0, Serialize: true);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnequippedEvent E)
		{
			E.Actor.UnregisterEvent(this, ApplyEffectEvent.ID);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ApplyEffectEvent E)
		{
			if (E.Effect is XRL.World.Effects.HeightenedHearingEffect fx
				&& fx.Listener == ParentObject.Equipped)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SEISMO", "self-stamp vetoed");
				return false;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			bool result = base.HandleEvent(E);
			SyncComputeLevel();
			if (MutationWasAdded != WasGranted)
			{
				WasGranted = MutationWasAdded;
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SEISMO",
					WasGranted ? "hearing granted (powered, wall contact)" : "hearing removed");
			}
			GameObject wearer = ParentObject.Equipped;
			if (wearer != null && wearer.IsPlayer())
			{
				bool selfFx = wearer.HasEffect(typeof(XRL.World.Effects.HeightenedHearingEffect));
				if (selfFx != HadSelfEffect)
				{
					HadSelfEffect = selfFx;
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SEISMO", selfFx
						? "self-effect STAMPED on wearer (hidden HeightenedHearingEffect, distance 0)"
						: "self-effect REMOVED from wearer <- if the beep lands here, diagnosis confirmed");
				}
			}
			return result;
		}

		public int GetComputeLevel(GameObject Wearer)
		{
			if (Wearer == null)
				return Level;
			int radius = GetAvailableComputePowerEvent.AdjustUp(Wearer, 3 + 2 * Level);
			return Math.Max(Level, (radius - 3) / 2);
		}

		private void SyncComputeLevel()
		{
			if (!MutationWasAdded)
				return;
			GameObject wearer = ParentObject.Equipped;
			Mutations mutations = wearer?.GetPart<Mutations>();
			Mutations.MutationModifierTracker tracker = mutations?.MutationMods.Find(m => m.id == Tracker);
			if (tracker == null)
				return;
			int want = GetComputeLevel(wearer);
			if (tracker.bonus == want)
				return;
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SEISMO", $"compute level {tracker.bonus} -> {want} (lattice compute {GetAvailableComputePowerEvent.GetFor(wearer)})");
			tracker.bonus = want;
			wearer.SyncMutationLevelAndGlimmer();
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			int baseLevel = Level;
			Level = GetComputeLevel(ParentObject.Equipped);
			bool result = base.HandleEvent(E);
			Level = baseLevel;
			E.Postfix.AppendRules("Compute power on the local lattice increases this item's range.");
			return result;
		}
	}
}
