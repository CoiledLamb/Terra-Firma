using System;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_ModTempered : IModification
	{
		public string Kind = "";

		public int Amount;

		public Cleo_TerraFirma_ModTempered()
		{
		}

		public Cleo_TerraFirma_ModTempered(string Kind, int Amount)
		{
			this.Kind = Kind;
			this.Amount = Amount;
		}

		public override int GetModificationSlotUsage() => 0;

		public override bool ModificationApplicable(GameObject Object) => false;

		public override bool SameAs(IPart p)
		{
			if (!(p is Cleo_TerraFirma_ModTempered other) || other.Kind != Kind || other.Amount != Amount)
				return false;
			return base.SameAs(p);
		}
	}
}
