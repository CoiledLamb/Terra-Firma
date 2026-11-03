using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_AnimateFigurineDepiction : IPart
	{
		public string Creature;

		public string Figurine;

		public override bool SameAs(IPart p) => p is Cleo_TerraFirma_AnimateFigurineDepiction o && o.Creature == Creature && o.Figurine == Figurine;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == BeforeDeathRemovalEvent.ID;
		}

		public GameObject MakeFigurine()
		{
			if (Figurine.IsNullOrEmpty() || GameObjectFactory.Factory.GetBlueprintIfExists(Figurine) == null)
				return null;
			string creature = Creature;
			GameObject figurine = GameObject.Create(Figurine, BeforeObjectCreated: o =>
			{
				RandomFigurine rf = o.GetPart<RandomFigurine>();
				if (rf != null && !creature.IsNullOrEmpty())
					rf.Creature = creature;
			});
			figurine?.MakeUnderstood();
			return figurine;
		}

		public override bool HandleEvent(BeforeDeathRemovalEvent E)
		{
			Corpse corpse = ParentObject.GetPart<Corpse>();
			IInventory drop = ParentObject.GetDropInventory();
			if (corpse != null && drop != null && corpse.CorpseChance.in100())
			{
				GameObject figurine = MakeFigurine();
				if (figurine != null)
					drop.AddObjectToInventory(figurine, null, Silent: false, NoStack: false, FlushTransient: true, null, E);
			}
			return base.HandleEvent(E);
		}

		public static void Record(GameObject Walker, GameObject Figurine)
		{
			string creature = Figurine?.GetPart<RandomFigurine>()?.Creature;
			if (Walker == null || creature.IsNullOrEmpty())
				return;
			Cleo_TerraFirma_AnimateFigurineDepiction part = Walker.RequirePart<Cleo_TerraFirma_AnimateFigurineDepiction>();
			part.Creature = creature;
			part.Figurine = Figurine.Blueprint;
			Walker.SetIntProperty("SuppressCorpseDrops", 1);
			if (Walker.Render != null && Figurine.Render != null)
			{
				Walker.Render.ColorString = Figurine.Render.ColorString;
				Walker.Render.TileColor = Figurine.Render.TileColor;
				Walker.Render.DetailColor = Figurine.Render.DetailColor;
			}
		}

		public override bool Render(RenderEvent E)
		{
			if (E.UI && E.Context != null && E.Context.StartsWith("Look") && !Creature.IsNullOrEmpty())
			{
				string tile = GameObjectFactory.Factory.GetBlueprintIfExists(Creature)?.GetPartParameter<string>("Render", "Tile");
				if (!tile.IsNullOrEmpty())
					E.Tile = Cleo.TerraFirma.Scripts.Cleo_TerraFirma_PedestalTile.Get(tile) ?? tile;
			}
			return base.Render(E);
		}
	}
}
