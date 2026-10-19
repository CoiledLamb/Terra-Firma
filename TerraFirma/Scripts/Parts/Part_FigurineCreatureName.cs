using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Fills the creature into the stone figurine's name on lang-experimental. The blueprint name
	/// carries stable's token: stable RandomFigurine.HandleEvent(ObjectCreatedEvent) does a literal
	/// Replace("*creature*", ...), while lang 2.0.212 runs the name through ReplaceBuilder, which
	/// only knows "=creature.name=" and leaves "*creature*" standing ("animate stone *creature*
	/// figurine", user report 2026-10-19). One name can't satisfy both, so this part mops up after
	/// RandomFigurine (it sits after it on the blueprint): on stable there is nothing left to replace.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_FigurineCreatureName : IPart
	{
		public const string TOKEN = "*creature*";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == ObjectCreatedEvent.ID;
		}

		public override bool HandleEvent(ObjectCreatedEvent E)
		{
			string name = ParentObject.Render?.DisplayName;
			string creature = ParentObject.GetPart<RandomFigurine>()?.Creature;
			if (name != null && name.Contains(TOKEN) && !creature.IsNullOrEmpty())
			{
				GameObject sample = GameObjectFactory.Factory.CreateSampleObject(creature);
				if (sample != null)
					ParentObject.Render.DisplayName = name.Replace(TOKEN, sample.DisplayNameOnlyStripped);
			}
			return base.HandleEvent(E);
		}
	}
}
