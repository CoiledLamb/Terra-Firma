using Cleo.TerraFirma.Scripts;

namespace XRL.World.Conversations
{
	[HasConversationDelegate]
	public static class Cleo_TerraFirma_TemperingDelegates
	{
		[ConversationDelegate(Key = "Cleo_TerraFirma_IfTemperOwed", InverseKey = "Cleo_TerraFirma_IfNotTemperOwed")]
		public static bool IfTemperOwed(DelegateContext Context)
		{
			string zoneID = The.Speaker?.CurrentZone?.ZoneID;
			return Cleo_TerraFirma_Tempering.Available(Context.Target, zoneID) > 0;
		}
	}
}

namespace XRL.World.Conversations.Parts
{
	public class Cleo_TerraFirma_TemperReward : IConversationPart
	{
		public override bool WantEvent(int ID, int Propagation)
		{
			return base.WantEvent(ID, Propagation) || ID == GetChoiceTagEvent.ID || ID == EnterElementEvent.ID;
		}

		public override bool HandleEvent(GetChoiceTagEvent E)
		{
			E.Tag = "{{W|[Temper an item]}}";
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EnterElementEvent E)
		{
			Cleo_TerraFirma_Tempering.Offer(The.Player, The.Speaker, The.Speaker?.CurrentZone?.ZoneID);
			return base.HandleEvent(E);
		}
	}
}
