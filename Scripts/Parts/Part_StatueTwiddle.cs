using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_StatueTwiddlePart : IPart
	{
		public override bool AllowStaticRegistration() => true;

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("CanSmartUse");
			Registrar.Register("CommandSmartUse");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "CanSmartUse")
				return false;
			if (E.ID == "CommandSmartUse")
			{
				ParentObject.Twiddle();
				return false;
			}
			return base.FireEvent(E);
		}
	}
}
