using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Used on walls created by Calcify. Since the 2026-08-06 rebalance (user: "calcify should
	/// fully copy the wall it's adjacent to if possible, including description and stats") this is
	/// a MARKER part: the wall keeps its source blueprint's own HP, AV, DV, and description. The
	/// part still prepends "calcified" to the display name (how you tell your wall from the world's)
	/// and anchors the fugue tether (Calcify's CheckExistenceSupportEvent handler keys on it).
	/// The stat-shift fields and Attach/Remove shift logic survive for OLD SAVES only: walls placed
	/// before the rebalance carry nonzero values and their StatShifter state, and must keep them.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_CalcifyWallPart : IPart
	{
		public int CurHp;
		public int AvReduction;

		public override bool WantEvent(int ID, int Cascade)
		{
			if (!base.WantEvent(ID, Cascade) &&
				ID != PooledEvent<GetDisplayNameEvent>.ID)
				return ID == SingletonEvent<GetDebugInternalsEvent>.ID;
			return true;
		}

		public override bool HandleEvent(GetDebugInternalsEvent E)
		{
			E.AddEntry(this, nameof(CurHp), CurHp);
			return base.HandleEvent(E);
		}

		public Cleo_TerraFirma_CalcifyWallPart() { }

		public Cleo_TerraFirma_CalcifyWallPart(int curHP, int curAv)
		{
			CurHp = curHP;
			AvReduction = curAv;
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			E.AddBase("calcified", -5);
			return base.HandleEvent(E);
		}

		public override void Attach()
		{
			// legacy path only: rebalanced casts attach with both fields zero and shift nothing
			if (CurHp != 0)
				StatShifter.SetStatShift("Hitpoints", CurHp, true);
			if (AvReduction != 0)
				StatShifter.SetStatShift("AV", -AvReduction, true);
		}

		public override void Remove()
		{
			StatShifter.RemoveStatShifts();
			// old walls had their Description overridden at attach; restore if restorable
			if ((CurHp != 0 || AvReduction != 0) && ParentObject.TryGetPart(out Description d))
				d.Short = ParentObject.GetBlueprint().GetPartParameter("Description", "Short", d.Short);
		}
	}
}
