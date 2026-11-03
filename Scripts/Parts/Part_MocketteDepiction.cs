using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_MocketteDepiction : IPart
	{
		public override bool SameAs(IPart p) => true;

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("AfterLookedAt");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "AfterLookedAt")
			{
				string depicted = ParentObject.GetPart<RandomStatue>()?.BaseBlueprint ?? ParentObject.GetPart<RandomFigurine>()?.Creature;
				if (!depicted.IsNullOrEmpty() && GameObjectFactory.Factory.Blueprints.TryGetValue(depicted, out GameObjectBlueprint model)
					&& model.HasPart(nameof(Cleo_TerraFirma_Mockette)))
				{
					Cleo_TerraFirma_Mockette.ConfuseLooker(ParentObject, E.GetGameObjectParameter("Looker"),
						model.GetPartParameter(nameof(Cleo_TerraFirma_Mockette), "ConfusionLevel", 10),
						model.GetPartParameter(nameof(Cleo_TerraFirma_Mockette), "BuildupTimeout", 15),
						model.GetPartParameter(nameof(Cleo_TerraFirma_Mockette), "BuildupPerLook", 3));
				}
			}
			return base.FireEvent(E);
		}
	}
}
