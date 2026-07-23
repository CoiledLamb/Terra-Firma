using Cleo.TerraFirma.Scripts;
using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Simple part that handles HP and AV offsets for statues created by Stoneshaper.
	/// The numbers passed to this are calculated in <see cref="Mutation.Cleo_TerraFirma_Stoneshaper.ApplyStatueBuffs(GameObject, GameObject, GameObject, int, Cleo_TerraFirma_StoneshaperWallProperties)"/>.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneshaperStatueStatsPart : IPart
	{
		public int CurHp;
		public int CurAv;

		public Cleo_TerraFirma_StoneshaperStatueStatsPart() { }

		public Cleo_TerraFirma_StoneshaperStatueStatsPart(int CurHp, int CurAv)
		{
			Helpers.DebugLog($"initializing stoneshaper statue: {CurHp}, {CurAv}", "STU");
			this.CurHp = CurHp;
			this.CurAv = CurAv;
		}

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
			E.AddEntry(this, nameof(CurAv), CurAv);
			return base.HandleEvent(E);
		}

		public override void Attach()
		{
			StatShifter.SetStatShift("Hitpoints", CurHp, true);
			StatShifter.SetStatShift("AV", CurAv, true);
		}

		public override void Remove()
		{
			StatShifter.RemoveStatShifts();
		}
	}
}
