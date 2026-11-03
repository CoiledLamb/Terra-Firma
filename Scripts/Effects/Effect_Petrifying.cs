using System;
using XRL.World.Capabilities;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects
{
	[Serializable]
	public class Cleo_TerraFirma_Petrifying : Effect
	{
		private const string STUCK_MESSAGE = "You are turning to stone and stuck in place!";

		public const string ACID_TURN_PROPERTY = "Cleo_TerraFirma_AcidStripTurn";

		public const string ACID_BODIED_TAG = "Cleo_TerraFirma_AcidBodied";

		public const int ACID_BODY_EVERY = 3;

		public GameObject Initiator;

		public bool Receding;

		public bool Pinned = true;

		public bool SlowCleared;

		public bool Active => Duration > 0 && !Receding;

		public Cleo_TerraFirma_Petrifying()
		{
			Duration = 10;
			DisplayName = "{{y|petrifying}}";
		}

		public Cleo_TerraFirma_Petrifying(GameObject Initiator) : this()
		{
			this.Initiator = Initiator;
		}

		public override string GetDetails()
		{
			if (Active)
				return $"Turning to stone.{(Pinned ? " Held in place." : "")}{(SlowCleared ? "" : $" -{Duration + 1} QN")}";
			return $"Recovering from partial petrification.{(SlowCleared ? "" : $" -{Duration + 1} QN")}";
		}

		public override bool UseStandardDurationCountdown() => !Active;

		public override bool Apply(GameObject Object)
		{
			if (!Object.CanChangeMovementMode("Stuck", Involuntary: true, FrozenOkay: true))
				return false;
			XDidY(Object, "begin", "turning to stone", "!", ColorAsBadFor: Object, Source: Object);
			Object.ParticleText("*petrifying*", ConsequentialColorChar(Initiator ?? null, Object));
			Object.MovementModeChanged("Stuck", true);
			if (Object.IsFlying)
				Flight.Fall(Object);
			return base.Apply(Object);
		}

		public override void Remove(GameObject Object)
		{
			RemoveStatShifts();
			if (GetType() == typeof(Cleo_TerraFirma_Petrifying))
				Object?.RemoveIntProperty(Cleo_TerraFirma_GorgonsGaze.STONE_COUNT_PROPERTY);
			base.Remove(Object);
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("CanChangeBodyPosition");
			Registrar.Register("IsMobile");
			Registrar.Register("LeaveCell");
			Registrar.Register("MovementModeChanged");
			base.Register(Object, Registrar);
		}

		public override bool WantEvent(int ID, int Cascade)
		{
			if (!base.WantEvent(ID, Cascade) &&
				ID != PooledEvent<CanChangeMovementModeEvent>.ID &&
				ID != PooledEvent<GetDisplayNameEvent>.ID &&
				ID != BeforeApplyDamageEvent.ID &&
				ID != SingletonEvent<EndTurnEvent>.ID)
				return ID == GetNavigationWeightEvent.ID;
			return true;
		}

		public override bool HandleEvent(CanChangeMovementModeEvent E)
		{
			if (Active && Pinned)
			{
				if (E.ShowMessage)
					E.Object.Fail(STUCK_MESSAGE);
				return false;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetNavigationWeightEvent E)
		{
			if (!E.Flying && Active && Pinned)
			{
				E.Uncacheable = true;
				E.MinWeight(100);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			if (!E.Reference)
				E.AddTag("[{{y|petrifying}}]", 20);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeApplyDamageEvent E)
		{
			if (E.Damage != null && E.Damage.IsAcidDamage())
				StripStage("acid");
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (Duration > 0 && IsAcidBodied(base.Object) && The.CurrentTurn % ACID_BODY_EVERY == 0)
				StripStage("acid body");
			if (!Receding && (!GameObject.Validate(Initiator) || Initiator.CurrentZone != base.Object?.CurrentZone))
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GAZE", "DEAD-GORGON RELEASE: initiator invalid or in another zone mid-gaze, effect self-recedes (the trap fix firing)");
				UpdateData(null, true);
			}
			RefreshDisplayName();
			UpdateStatShifts();
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (Active && Pinned)
			{
				if (E.ID == "IsMobile")
				{
					if (!Object.IsTryingToJoinPartyLeader())
						return false;
				}
				else if (E.ID == "LeaveCell")
				{
					if (!E.HasFlag("Forced") && E.GetStringParameter("Type") != "Teleporting" && !Object.IsTryingToJoinPartyLeader())
					{
						if (Object.IsPlayer()) AddPlayerMessage(STUCK_MESSAGE);
						Object.UseEnergy(1000);
						return false;
					}

				}
				else if (E.ID == "CanChangeBodyPosition")
				{
					if (E.HasFlag("ShowMessage") && Object.IsPlayer())
						Object.Fail(STUCK_MESSAGE);
					return false;
				}

			}
			return base.FireEvent(E);
		}

		public void UpdateData(int? NewDuration = null, bool ShouldRecede = false)
		{
			if (NewDuration != null)
			{
				Duration = Math.Min(100, (int)NewDuration);
			}
			Receding = ShouldRecede;
			if (Receding)
				Pinned = false;
			UpdateStatShifts();
		}

		public void ApplyFailure()
		{
			Pinned = true;
			SlowCleared = false;
			Duration = Math.Min(100, Duration + 10);
			UpdateStatShifts();
		}

		public void ReleasePin()
		{
			Pinned = false;
		}

		public void ClearSlow()
		{
			SlowCleared = true;
			UpdateStatShifts();
		}

		public static bool IsAcidBodied(GameObject Object)
		{
			if (Object == null)
				return false;
			if (Object.HasTagOrProperty(ACID_BODIED_TAG))
				return true;
			if (Object.GetPart<CorrosiveGasGeneration>() is CorrosiveGasGeneration gas && Object.IsActivatedAbilityToggledOn(gas.ActivatedAbilityID))
				return true;
			string bleed = Object.GetPropertyOrTag("BleedLiquid");
			return bleed != null && bleed.Contains("acid");
		}

		public bool StripStage(string Why)
		{
			GameObject obj = base.Object;
			if (obj == null || Duration <= 0)
				return false;
			int turn = (int)The.CurrentTurn;
			if (obj.GetIntProperty(ACID_TURN_PROPERTY, -1) == turn)
				return false;
			obj.SetIntProperty(ACID_TURN_PROPERTY, turn);
			Duration = Math.Max(Active ? 1 : 0, Duration - 10);
			string tally;
			if (this is Cleo_TerraFirma_PetrifyingReflected self)
			{
				if (self.GazerFails > 0)
					self.GazerFails--;
				tally = $"gazer tally {self.GazerFails}";
			}
			else if (Cleo_TerraFirma_GorgonsGaze.AcidStrip(Initiator, obj))
				tally = "gaze tally down one";
			else
			{
				int count = Cleo_TerraFirma_GorgonsGaze.GetStoneCount(obj);
				if (count > 0)
					Cleo_TerraFirma_GorgonsGaze.SetStoneCount(obj, count - 1);
				tally = $"stone count {Math.Max(0, count - 1)}";
			}
			UpdateStatShifts();
			RefreshDisplayName();
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GAZE", $"ACID STRIP ({Why}): {obj.Blueprint} duration {Duration}, {tally}");
			if (obj.IsPlayer())
				AddPlayerMessage("You feel the stone's grip loosen.");
			obj.PlayWorldSound("Sounds/Damage/sfx_damage_stone", 1f, Combat: true);
			return true;
		}

		private void RefreshDisplayName()
		{
			DisplayName = "{{y|petrifying}} (" + (!Active ? "receding" : (Pinned ? "held" : "slipping")) + (SlowCleared ? "" : ", -" + Duration + " QN") + ")";
		}

		private void UpdateStatShifts()
		{
			if (!IsAttached())
				return;
			StatShifter.SetStatShift("Speed", SlowCleared ? 0 : -Duration);
		}

		private bool IsAttached()
		{
			GameObject obj = base.Object;
			if (obj == null || obj.Effects == null)
				return false;
			for (int i = 0; i < obj.Effects.Count; i++)
				if (obj.Effects[i] == this)
					return true;
			return false;
		}

		private void RemoveStatShifts()
		{
			StatShifter.RemoveStatShifts();
		}
	}

	[XRL.HasCallAfterGameLoaded]
	public static class Cleo_TerraFirma_PetrifyingShiftRepair
	{
		[XRL.CallAfterGameLoaded]
		public static void OnGameLoaded()
		{
			try
			{
				Repair(The.Player);
			}
			catch (Exception x)
			{
				MetricsManager.LogError("Terra Firma petrifying shift repair", x);
			}
		}

		public static int Repair(GameObject Who)
		{
			if (Who == null || Who.HasEffect<Cleo_TerraFirma_Petrifying>())
				return 0;
			Statistic speed = Who.GetStat("Speed");
			if (speed?.Shifts == null)
				return 0;
			int removed = 0;
			for (int i = speed.Shifts.Count - 1; i >= 0; i--)
			{
				var shift = speed.Shifts[i];
				if (shift.DisplayName != null && shift.DisplayName.Contains("petrifying"))
				{
					speed.RemoveShift(shift.ID);
					removed++;
				}
			}
			if (removed > 0)
				MetricsManager.LogInfo($"Terra Firma: removed {removed} leftover petrifying quickness shift(s) from {Who.Blueprint}");
			return removed;
		}
	}
}
