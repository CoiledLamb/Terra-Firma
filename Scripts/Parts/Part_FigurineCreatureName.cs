using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_FigurineCreatureName : IPart
	{
		public const string TOKEN = "*creature*";
#if !VERSION_1_0
		public const string STONE = "stone";
		public const string STAND_IN = "copper";

		[NonSerialized]
		private bool Lent;
#endif

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == ObjectCreatedEvent.ID
#if !VERSION_1_0
				|| ID == BeforeObjectCreatedEvent.ID
#endif
				;
		}

#if !VERSION_1_0
		public override bool HandleEvent(BeforeObjectCreatedEvent E)
		{
			RandomFigurine figurine = ParentObject.GetPart<RandomFigurine>();
			if (figurine != null && figurine.Material == STONE)
			{
				figurine.Material = STAND_IN;
				Lent = true;
			}
			return base.HandleEvent(E);
		}
#endif

		public override bool HandleEvent(ObjectCreatedEvent E)
		{
			RandomFigurine figurine = ParentObject.GetPart<RandomFigurine>();
#if !VERSION_1_0
			if (Lent && figurine != null)
			{
				Lent = false;
				figurine.Material = STONE;
				Description description = ParentObject.GetPart<Description>();
				string standIn = figurine.MaterialDisplayName(STAND_IN);
				string text = description?._Short;
				int at = string.IsNullOrEmpty(standIn) || text == null ? -1 : text.IndexOf(standIn, StringComparison.Ordinal);
				if (at >= 0)
					description._Short = text.Substring(0, at) + STONE + text.Substring(at + standIn.Length);
			}
#endif
			string name = ParentObject.Render?.DisplayName;
			string creature = figurine?.Creature;
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
