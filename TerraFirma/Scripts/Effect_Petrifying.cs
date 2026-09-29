using System;
using XRL.World.Capabilities;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects
{
	/// <summary>
	/// Applied solely by <see cref="Cleo_TerraFirma_GorgonsGaze"/>. All updates to this effect's duration (and the actual "turning to stone" thing) happen there, not here.<br/><br/>
	/// If <see cref="Receding"/> is false: Prevents all movement. Reduces quickness by remaining duration. Does not count down.<br/>
	/// Otherwise: Only reduces quickness. Counts down normally.<br/><br/>
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_Petrifying : Effect
	{
		#region Fields
		private const string STUCK_MESSAGE = "You are turning to stone and stuck in place!";

		/// <summary>
		/// The <see cref="GameObject"/> that caused this effect.
		/// </summary>
		public GameObject Initiator;

		/// <summary>
		/// If true, this effect will count down its duration normally and has reduced downsides.
		/// </summary>
		public bool Receding;

		/// <summary>
		/// Whether the target is held in place. Reasserted by every failed save; the first successful save releases it.
		/// </summary>
		public bool Pinned = true;

		/// <summary>
		/// Whether the quickness drain has been shaken off (second successful save). A later failed save reasserts it.
		/// </summary>
		public bool SlowCleared;

		/// <summary>
		/// Wrapper that checks if any duration is remaining and the action is not receding.
		/// </summary>
		public bool Active => Duration > 0 && !Receding;
		#endregion

		#region Basic info
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
				return $"Turning to stone.{(Pinned ? " Held in place." : "")}{(SlowCleared ? "" : $" -{Duration + 1} QN")}"; // I have no idea why I need to use a +1 here either
			return $"Recovering from partial petrification.{(SlowCleared ? "" : $" -{Duration + 1} QN")}";
		}

		public override bool UseStandardDurationCountdown() => !Active;
		#endregion

		#region Logic
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
			base.Remove(Object);
		}
		#endregion

		#region Events
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

		public override bool HandleEvent(EndTurnEvent E)
		{
			// if whatever was petrifying us is dead or gone mid-gaze, nothing will ever release us -- release ourselves.
			// "Gone" includes another zone (audit 2026-09-29): a victim teleported away leaves the gazer's zone
			// suspended, so its gaze tick never runs again to let go.
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
						AddPlayerMessage(STUCK_MESSAGE);
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
		#endregion

		#region Helpers
		/// <summary>
		/// Updates this effect's data with new parameters.
		/// </summary>
		/// <param name="NewDuration">The new duration that should override the existing one.</param>
		/// <param name="ShouldRecede"><see cref="Receding"/> will be set to this value.</param>
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

		/// <summary>
		/// A failed save reasserts the full petrification: pinned, slowing, and deeper in.
		/// </summary>
		public void ApplyFailure()
		{
			Pinned = true;
			SlowCleared = false;
			Duration = Math.Min(100, Duration + 10);
			UpdateStatShifts();
		}

		/// <summary>First successful save: movement wrenches free; the slow remains.</summary>
		public void ReleasePin()
		{
			Pinned = false;
		}

		/// <summary>Second successful save: the stiffness shakes off; quickness restored.</summary>
		public void ClearSlow()
		{
			SlowCleared = true;
			UpdateStatShifts();
		}

		private void RefreshDisplayName()
		{
			DisplayName = "{{y|petrifying}} (" + (!Active ? "receding" : (Pinned ? "held" : "slipping")) + (SlowCleared ? "" : ", -" + Duration + " QN") + ")";
		}

		private void UpdateStatShifts()
		{
			StatShifter.SetStatShift("Speed", SlowCleared ? 0 : -Duration);
		}

		private void RemoveStatShifts()
		{
			StatShifter.RemoveStatShifts();
		}
		#endregion
	}
}
