using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_FigurineDepiction : IPart
	{
		public double LegendaryValueMultiplier = 1.5;

		public override bool SameAs(IPart p) => true;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == AfterObjectCreatedEvent.ID
				|| ID == GetIntrinsicValueEvent.ID;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("CustomRender");
			base.Register(Object, Registrar);
		}

		GameObjectBlueprint Depicted
		{
			get
			{
				string name = ParentObject.GetPart<RandomFigurine>()?.Creature;
				return name.IsNullOrEmpty() ? null : GameObjectFactory.Factory.GetBlueprintIfExists(name);
			}
		}

		public static bool IsLegendary(GameObjectBlueprint Model) => Model?.GetTag("Role") == "Hero";

		public static bool IsInvisible(GameObjectBlueprint Model) => Model?.Mutations?.ContainsKey("Invisibility") == true;

		public static bool RevealsInvisible(Cell C)
		{
			if (C == null)
				return false;
			LightLevel light = C.GetLight();
			return light == LightLevel.Darkvision || light == LightLevel.LitRadar || light == LightLevel.Radar
				|| light == LightLevel.Dimvision || light == LightLevel.Interpolight || light == LightLevel.Omniscient;
		}

		public override bool HandleEvent(AfterObjectCreatedEvent E)
		{
			if (IsInvisible(Depicted) && ParentObject.Render != null)
				ParentObject.Render.CustomRender = true;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetIntrinsicValueEvent E)
		{
			if (IsLegendary(Depicted))
				E.Value *= LegendaryValueMultiplier;
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "CustomRender")
			{
				Cell cell = ParentObject.CurrentCell;
				if (cell != null && ParentObject.Render != null && IsInvisible(Depicted))
					ParentObject.Render.Visible = RevealsInvisible(cell);
			}
			return base.FireEvent(E);
		}

		public override bool Render(RenderEvent E)
		{
			if (E.UI)
			{
				GameObjectBlueprint model = Depicted;
				if (IsInvisible(model) && !RevealsInvisible(ParentObject.GetCellContext()))
				{
					E.Tile = null;
					E.RenderString = " ";
					return base.Render(E);
				}
				if (model != null && E.Context != null && E.Context.StartsWith("Look"))
				{
					string tile = model.GetPartParameter<string>("Render", "Tile");
					if (!tile.IsNullOrEmpty())
						E.Tile = Cleo.TerraFirma.Scripts.Cleo_TerraFirma_PedestalTile.Get(tile) ?? tile;
				}
			}
			return base.Render(E);
		}
	}
}
