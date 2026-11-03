using System;

namespace XRL.World.Effects
{
	[Serializable]
	public class Cleo_TerraFirma_PetrifyingReflected : Cleo_TerraFirma_Petrifying
	{
		public int GazerFails;

		public Cleo_TerraFirma_PetrifyingReflected() : base()
		{
		}

		public Cleo_TerraFirma_PetrifyingReflected(GameObject Initiator) : base(Initiator)
		{
		}
	}
}
