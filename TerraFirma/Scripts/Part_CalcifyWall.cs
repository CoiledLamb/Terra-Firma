using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Used on walls created by Calcify.
	/// Scales HP and AV, and gives a unique description and display name to differentiate them from regular walls.
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
			StatShifter.SetStatShift("Hitpoints", CurHp, true);
			StatShifter.SetStatShift("AV", -AvReduction, true);
			if (ParentObject.TryGetPart(out Description d))
				d.Short = "Dust and grit were strung into cubic costume and set to bar passage.";
		}

		public override void Remove()
		{
			StatShifter.RemoveStatShifts();
			if (ParentObject.TryGetPart(out Description d))
				d.Short = ParentObject.GetBlueprint().GetPartParameter("Description", "Short", d.Short); // restore if restorable; otherwise leave the calcified text in place
		}
	}
}
