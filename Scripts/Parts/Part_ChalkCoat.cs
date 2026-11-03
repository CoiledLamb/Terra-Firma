using System;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_ChalkCoat : IPart
	{
		public const string ADJECTIVE = "{{Y|chalked}}";
		public const string RULES = "Chalked: This item cannot rust until it gets wet.";
		public const string WASH_MESSAGE = "The chalk washes off ";

		public override bool SameAs(IPart p)
		{
			return p is Cleo_TerraFirma_ChalkCoat;
		}

		public static bool CanCoat(GameObject item)
		{
			return item != null
				&& item.HasPart<Metal>()
				&& !item.HasPart<NoRust>()
				&& !item.HasPart<ModLacquered>()
				&& !item.HasPart<Cleo_TerraFirma_ChalkCoat>()
				&& !item.HasPart<CyberneticsBaseItem>()
				&& !item.IsNatural();
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == PooledEvent<GetDisplayNameEvent>.ID
				|| ID == GetShortDescriptionEvent.ID;
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			if (!E.Understood() || !E.Object.HasProperName)
			{
				E.AddAdjective(ADJECTIVE, -5);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			E.Postfix.AppendRules(RULES);
			return base.HandleEvent(E);
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("ApplyRusted");
			Registrar.Register("ApplyLiquidCovered");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "ApplyRusted")
			{
				return false;
			}
			if (E.ID == "ApplyLiquidCovered")
			{
				LiquidVolume liquid = E.GetParameter("Liquid") as LiquidVolume;
				if (liquid != null && liquid.ContainsLiquid("water"))
				{
					WashOff();
				}
				return true;
			}
			return base.FireEvent(E);
		}

		public void WashOff()
		{
			GameObject item = ParentObject;
			GameObject holder = item.Equipped ?? item.InInventory;
			if (holder != null && holder.IsPlayer())
			{
				IComponent<GameObject>.AddPlayerMessage(WASH_MESSAGE + item.the + item.ShortDisplayName + ".");
			}
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"coat washed off {item.DebugName}");
			item.RemovePart(this);
		}
	}
}
