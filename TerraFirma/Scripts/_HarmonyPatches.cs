using HarmonyLib;
using System;
using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Capabilities;
using XRL.World.Effects;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Skills.Cooking;

namespace Cleo.TerraFirma.Scripts.Patches
{
	// Harmony patches, kept to the minimum the engine forces on us. The old PerformThrow postfix was
	// replaced by the native AfterThrownEvent relay (see Cleo_TerraFirma_GeomancyStonePart).
	// Patch inventory as of 2026-07-22:
	//   1. Scanning.HasScanningFor postfix (gemstone scan) -- no native hook exists.
	//   2-4. The Lithovore meal-policy patches (RULED C, 2026-07-22): the campfire's cooked-meal
	//        paths hardcode Carnivorous by name (Campfire.cs, decompiled) and expose no event, so
	//        the lithovore's plants-and-meats ban has no native seam. Three method-level patches,
	//        no transpilers:
	//        - IsValidCookingIngredient postfix: plant/meat ingredients vanish from the lithovore's
	//          picker and random rolls (liquids always legal, per ruling; fungus deliberately legal).
	//        - CookPresetMeal prefix: preset/oven meals containing a banned class = no nourishment,
	//          50% Ill (the full Carnivorous mirror).
	//        - CookingRecipe.ApplyEffectsTo prefix: learned-recipe meals with a banned class lose
	//          their buff + 50% Ill. KNOWN DEVIATION: the recipe path still clears hunger (its
	//          ClearHunger call sits mid-method with no seam short of a transpiler) -- a lithovore
	//          eating a banned learned recipe gets fed but never buffed, and risks Ill. Flagged in
	//          TODO; revisit only if playtest cares.
	[HarmonyPatch]
	class HarmonyPatches
	{
		/// <summary>
		/// Always reveals an object's stats if it has a <see cref="Cleo_TerraFirma_PartGeomancyScan"/> attached to it.
		/// </summary>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Scanning), nameof(Scanning.HasScanningFor))]
		[HarmonyPatch(new Type[] { typeof(GameObject), typeof(GameObject) })]
		static void Scanning_GetScanEpistemicStatus(GameObject who, GameObject obj, ref bool __result)
		{
			if (!__result && obj.HasPart<Cleo_TerraFirma_PartGeomancyScan>())
				__result = true;
		}

		static bool PlayerIsLithovore()
		{
			return The.Player != null && The.Player.HasPart<Cleo_TerraFirma_Lithovore>();
		}

		/// <summary>Plants and meats are banned for lithovores; everything liquid is legal (ruling),
		/// as is fungus (ruling: mushrooms are honorary minerals) and every exotic that is neither.</summary>
		static bool IngredientBanned(GameObject obj)
		{
			if (obj.LiquidVolume != null)
				return false;
			return obj.HasTag("Plant") || obj.HasTag("Meat");
		}

		static bool RecipeHasBanned(CookingRecipe recipe)
		{
			if (recipe == null)
				return false;
			if (recipe.HasPlants()) // fungus deliberately NOT checked (ruling 2026-07-22)
				return true;
			foreach (ICookingRecipeComponent component in recipe.Components)
			{
				if (component is PreparedCookingRecipieComponentBlueprint blueprintComponent
					&& !string.IsNullOrEmpty(blueprintComponent.ingredientBlueprint)
					&& GameObjectFactory.Factory.Blueprints.TryGetValue(blueprintComponent.ingredientBlueprint, out GameObjectBlueprint bp)
					&& bp.HasTag("Meat"))
				{
					return true;
				}
			}
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Campfire), nameof(Campfire.IsValidCookingIngredient))]
		[HarmonyPatch(new Type[] { typeof(GameObject) })]
		static void Lithovore_FilterIngredients(GameObject obj, ref bool __result)
		{
			if (__result && PlayerIsLithovore() && IngredientBanned(obj))
				__result = false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Campfire), nameof(Campfire.CookPresetMeal))]
		static bool Lithovore_CookPresetMeal(Campfire __instance, int __0, ref bool __result)
		{
			if (!PlayerIsLithovore())
				return true;
			CookingRecipe recipe = __instance.presetMeals[__0];
			if (!RecipeHasBanned(recipe))
				return true;
			// full Carnivorous mirror (Campfire.CookPresetMeal decompiled): hunger NOT cleared, 50% Ill
			if (!Campfire.IsHungry(The.Player))
			{
				Popup.Show("You aren't hungry. Instead, you relax by the warmth of the fire.");
				__result = false;
				return false;
			}
			The.Player.FireEvent("ClearFoodEffects");
			The.Player.CleanEffects();
			Popup.Show("It sits in your stomach like what it is: not stone.");
			if (Stat.Random(1, 100) <= 50)
			{
				Popup.Show("Ugh, you feel sick.");
				The.Player.ApplyEffect(new Ill(100));
			}
			The.Player.GetPart<Stomach>().CookCount++;
			__instance.AfterCooked();
			__result = true;
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(CookingRecipe), nameof(CookingRecipe.ApplyEffectsTo))]
		static bool Lithovore_RecipeEffects(CookingRecipe __instance, GameObject __0)
		{
			if (__0 == null || !__0.HasPart<Cleo_TerraFirma_Lithovore>() || !RecipeHasBanned(__instance))
				return true;
			if (Stat.Random(1, 100) <= 50)
			{
				if (__0.IsPlayer())
					Popup.Show("Ugh, you feel sick.");
				__0.ApplyEffect(new Ill(100));
			}
			return false; // no buff from a banned meal
		}
	}
}
