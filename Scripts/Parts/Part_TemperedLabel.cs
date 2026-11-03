using System;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_TemperedLabel : IPart
	{
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != PooledEvent<GetDisplayNameEvent>.ID)
				return ID == GetShortDescriptionEvent.ID;
			return true;
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			if (E.Understood() && !E.Object.HasProperName && E.Object.HasPart<Cleo_TerraFirma_ModTempered>())
				E.AddAdjective("{{W|tempered}}");
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			Cleo_TerraFirma_ModTempered mark = E.Object.GetPart<Cleo_TerraFirma_ModTempered>();
			if (mark != null)
				E.Postfix.AppendRules("Tempered: " + Cleo_TerraFirma_Tempering.Describe(mark.Kind, mark.Amount) + ".");
			return base.HandleEvent(E);
		}
	}
}
