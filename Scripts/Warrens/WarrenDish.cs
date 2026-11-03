using System;
using System.Collections.Generic;
using XRL;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Skills.Cooking;

namespace Cleo.TerraFirma.Scripts
{
	public static class Cleo_TerraFirma_WarrenDish
	{
		public const string MOSS = "Cleo_TerraFirma_CrushedCaveMoss";
		public const string TABLE = "DynamicObjectsTable:DesertCanyon_Ingredients";
		public const int REROLLS = 25;

		public static bool IsMoss(string blueprint) =>
			blueprint != null && blueprint.IndexOf("moss", StringComparison.OrdinalIgnoreCase) >= 0;

		public static string RollPartner(int tier)
		{
			for (int i = 0; i < REROLLS; i++)
			{
				string bp = PopulationManager.RollOneFrom(TABLE)?.Blueprint;
				if (!string.IsNullOrEmpty(bp) && !IsMoss(bp))
					return bp;
			}
			for (int i = 0; i < REROLLS; i++)
			{
				string bp = CookingRecipe.RollOvenSafeIngredient("Ingredients" + Math.Max(0, Math.Min(8, tier)));
				if (!string.IsNullOrEmpty(bp) && !IsMoss(bp))
					return bp;
			}
			return "Starapple Preserves";
		}

		private readonly struct DishIngredients
		{
			private readonly List<GameObject> list;
			public DishIngredients(List<GameObject> list) { this.list = list; }
			public static implicit operator List<GameObject>(DishIngredients i) => i.list;
			public static implicit operator ReadOnlySpan<GameObject>(DishIngredients i) => i.list.ToArray();
		}

		public static CookingRecipe Roll(int tier)
		{
			var ingredients = new List<GameObject>(2);
			try
			{
				foreach (string bp in new[] { MOSS, RollPartner(tier) })
				{
					GameObject o = GameObject.Create(bp);
					if (o != null)
						ingredients.Add(o);
				}
				return CookingRecipe.FromIngredients(new DishIngredients(ingredients), null, null, "generic");
			}
			finally
			{
				foreach (GameObject o in ingredients)
					o.Obliterate();
			}
		}

		public static string Text(string warrenName) =>
			"What a savory smell! Teach me to cook the favorite dish of " + warrenName + ".\n";

		public static TeachesDish Teach(GameObject overseer, string warrenName, int tier)
		{
			if (overseer == null)
				return null;
			if (overseer.TryGetPart(out TeachesDish had))
				return had;
			CookingRecipe recipe = Roll(tier);
			if (recipe == null)
				return null;
			var part = new TeachesDish(recipe, Text(warrenName));
			overseer.AddPart(part);
			Helpers.VerifyLog("WARREN", $"dish: {recipe.GetDisplayName()} ({string.Join(", ", recipe.Components.ConvertAll(c => c.getIngredientId()))}) taught by {overseer.GetReferenceDisplayName()} of {warrenName}");
			return part;
		}

		public static string WarrenName(Zone Z)
		{
			string name = Z == null ? null : The.ZoneManager.GetZoneBaseDisplayName(Z.ZoneID, Mutate: false);
			if (string.IsNullOrEmpty(name))
				return XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenPlacement.NAME_FALLBACK;
			string suffix = XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenPlacement.NAME_SUFFIX;
			return name.EndsWith(suffix) ? name.Substring(0, name.Length - suffix.Length) : name;
		}
	}
}
