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

		public override bool HandleEvent(EndTurnEvent E)
		{
			bool result = base.HandleEvent(E);
			if (MutationWasAdded != WasGranted)
			{
				WasGranted = MutationWasAdded;
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SEISMO",
					WasGranted ? "hearing granted (powered, wall contact)" : "hearing removed");
			}
			return result;
		}
	}
}
