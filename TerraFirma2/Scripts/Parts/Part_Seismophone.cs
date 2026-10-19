using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// The seismophone (name RULED 2026-08-22, wall-ear retired; description USER TEXT FINAL
	/// 2026-08-22). User shape: "grants heightened hearing when you're adjacent to a wall and
	/// it's powered." Head slot 0 AV / 1 DV, tier 3, level 2 hearing = radius 7 (3 + 2 x Level),
	/// all per the 2026-08-23 pre-build rulings.
	///
	/// Build is a subclass of base MutationOnEquip on purpose: that part already re-checks its
	/// grant on cell change, power flip, effect changes, and every end of turn, and IActivePart
	/// exposes a locally-defined-failure hook that folds straight into its IsReady gate. So the
	/// whole conditional is one override: no adjacent wall = not ready = grant silently removed
	/// (silence ruled fine 2026-08-23; base ApplyBonus/UnapplyBonus emit no messages). Wall test
	/// is Cell.HasWall(), the engine's own predicate - ANY wall by ruling ("i think any wall is
	/// fine"), earth or otherwise. The aquifer/liquid-ping half of the original wall-ear pitch
	/// was ruled SKIPPED at the same sitting.
	///
	/// MutationOnEquip's constructor zeroes the powered profile (its base users are freebie
	/// telepathy relics), so ours restores the IPoweredPart defaults NightVision runs on:
	/// 1 charge/turn, EMP and power-switch sensitive, tech-scannable.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_Seismophone : MutationOnEquip
	{
		/// <summary>Shadow of MutationWasAdded, so verify logging fires once per transition
		/// instead of every turn.</summary>
		public bool WasGranted;

		/// <summary>Instrumentation shadow (2026-08-24 beep hunt): whether the wearer carried
		/// the hidden HeightenedHearingEffect last turn. Diagnosis under test: the mutation's
		/// EndTurn sweep stamps the effect on every Combat object in radius INCLUDING the
		/// listener at distance 0, and that self-effect's removal on grant loss is what plays
		/// sfx_statusEffect_stop (Effect.RemoveSound fires for effects dying on the player).</summary>
		public bool HadSelfEffect;

		// Since the 2026-08-29 ladder pass the blueprint's <part> line is the tuning seam
		// (Objects.xml sets the same attributes); these ctor values are fail-safe fallbacks
		// for a bare mount, restoring the IPoweredPart profile MutationOnEquip's ctor zeroes.
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

		// Beep fix (user flag 2026-08-24, "it plays a sound when you leave the walls radius";
		// no-Harmony per the user, same sitting). Root cause: HeightenedHearing's EndTurn sweep
		// stamps its hidden effect on every Combat object in radius INCLUDING the listener at
		// distance 0, and Effect.RemoveSound plays sfx_statusEffect_stop for any effect dying
		// on the player - so the self-stamp's removal beeped on every wall-departure (repro'd
		// in-log + by ear). Fix: veto the self-stamp so it never exists. ApplyEffectEvent is
		// cascade 0 and never reaches equipment, so we register on the wearer directly - the
		// CyberneticsAirCurrentMicrosensor idiom, Serialize:true so it survives save/load.
		// Another creature's hearing stamping ITS effect on our wearer has Listener != wearer
		// and passes untouched.

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

		// Lattice compute scaling (user 2026-10-19, base step size). Base compute-scaled sensors
		// (CyberneticsPenetratingRadar) take range x (100 + compute) / 100 via
		// GetAvailableComputePowerEvent.AdjustUp. Hearing takes a level, not a radius
		// (HeightenedHearing.GetRadius: 3 + 2 x Level), so the radius is scaled the base way and
		// the highest level that fits is granted: 29+ compute = level 3, 58+ = 4, 86+ = 5 at the
		// XML level 2. No new saved field (save layout holds): the XML Level stays the base, and the
		// live grant is moved by retuning the existing equipment tracker's bonus in place, so the
		// mutation is never removed and re-added (no flicker, no repeat of the 2026-08-24 beep).
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
			// the base "Grants you Heightened Hearing at level X." line reads Level; show the live level
			int baseLevel = Level;
			Level = GetComputeLevel(ParentObject.Equipped);
			bool result = base.HandleEvent(E);
			Level = baseLevel;
			E.Postfix.AppendRules("Compute power on the local lattice increases this item's range.");
			return result;
		}
	}
}
