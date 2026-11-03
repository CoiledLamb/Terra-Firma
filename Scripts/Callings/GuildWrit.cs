using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_GuildWrit : IPart
	{
		public const string RULES = "You may give this to any overseer to receive one free tempering.";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == GetShortDescriptionEvent.ID;
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			E.Postfix.AppendRules(RULES);
			return base.HandleEvent(E);
		}
	}
}
