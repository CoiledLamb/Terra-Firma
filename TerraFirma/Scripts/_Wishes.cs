using System.Collections.Generic;
using XRL;
using XRL.Messages;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace Cleo.TerraFirma.Scripts
{
	[HasWishCommand]
	public class WishHandler
	{
		[WishCommand(Command = "rockwineverytime")]
		public static void RockWinEveryTime()
		{
			if (Popup.AskString("Set this character up for Terra Firma debugging? Type ROCK to confirm. This is irreversible; if you're not sure what this means, hit Cancel!", WantsSpecificPrompt: "ROCK")?.ToUpper() != "ROCK")
				return;
			GameObject Ply = The.Player;
			Mutations Muts = Ply.GetPart<Mutations>();
			Muts.AddMutation(new Cleo_TerraFirma_Calcify());
			Muts.AddMutation(new Cleo_TerraFirma_Geomancy());
			Muts.AddMutation(new Cleo_TerraFirma_EarthenBarrage());
			Muts.AddMutation(new Cleo_TerraFirma_Stoneshaper());
			Muts.AddMutation(new Cleo_TerraFirma_GorgonsGaze());
			Muts.AddMutation(new Cleo_TerraFirma_Fissure());
			Muts.AddMutation(new XRL.World.Parts.Mutation.NightVision());
			Ply.GetFirstBodyPart("Head").Implant(GameObject.Create("OpticalMultiscanner"));
			Ply.GetStat("Hitpoints").BaseValue = 250;
			Ply.GetStat("MP").BaseValue = 100;
			Ply.GetStat("SP").BaseValue = 69420;
			Ply.GetStat("Level").BaseValue = 30;
			Ply.Inventory.AddObject("Cleo_TerraFirma_StatueMaker");
			MessageQueue.AddPlayerMessage("rock win every time", Capitalize: false);
		}

		/// <summary>The test kitchen (user request 2026-07-22): a millstone at your feet, a pestle in
		/// your pack, three of every takeable rock to grind (threes so stack-quantity grinding gets
		/// exercised), Small Stones and a boulder for the flour and big-grind paths, and an optional
		/// Lithovore grant so the kitchen tests with or without the diet.</summary>
		[WishCommand(Command = "kitchenrock")]
		public static void KitchenRock()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count > 0)
				open[0].AddObject(GameObject.Create("Millstone"));
			Ply.Inventory.AddObject(GameObject.Create("Pestle"));
			int stocked = 0;
			foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
			{
				if (!bp.Name.StartsWith("Cleo_TerraFirma_Rock_"))
					continue;
				for (int i = 0; i < 3; i++)
				{
					GameObject rock = GameObjectFactory.Factory.CreateObject(bp.Name);
					if (rock == null)
						break;
					if (rock.Brain == null && rock.Physics != null && rock.Physics.Takeable)
					{
						Ply.Inventory.AddObject(rock);
						stocked++;
					}
					else
					{
						rock.Obliterate(); // creatures and terrain are not kitchen stock
						break;
					}
				}
			}
			for (int i = 0; i < 5; i++)
				Ply.Inventory.AddObject(GameObject.Create("Small Stone")); // rock-flour stock
			Ply.Inventory.AddObject(GameObject.Create("MediumBoulder"));   // big-grind + famished-devour test
			// the full kitchen skillset (user request 2026-07-22): choose ingredients (base +
			// MealPreparation), three at once (Spicer), and Carbide Chef so Inspired dish-naming
			// -- the HistorySpice consumer -- is testable too
			Skills skills = Ply.GetPart<Skills>();
			if (skills != null)
			{
				skills.AddSkill("CookingAndGathering");
				skills.AddSkill("CookingAndGathering_MealPreparation");
				skills.AddSkill("CookingAndGathering_Spicer");
				skills.AddSkill("CookingAndGathering_CarbideChef");
			}
			if (!Ply.HasPart<Cleo_TerraFirma_Lithovore>()
				&& Popup.ShowYesNo("Also gain the Lithovore defect? (Test the kitchen with the diet, or without.)") == 0)
			{
				Ply.GetPart<Mutations>()?.AddMutation(new Cleo_TerraFirma_Lithovore());
			}
			MessageQueue.AddPlayerMessage("the kitchen is stocked: " + stocked + " rocks, a pestle, a millstone, and appetite optional.", Capitalize: false);
		}

		/// <summary>Diagnostic (2026-07-22): why isn't the pestle offering "grind stones"?
		/// Reports whether the merge landed and each gate of the action condition.</summary>
		[WishCommand(Command = "grindcheck")]
		public static void GrindCheck()
		{
			GameObject Ply = The.Player;
			List<GameObject> pestles = Ply.GetInventoryDirect((GameObject go) => go.Blueprint == "Pestle");
			int stones = Ply.GetInventoryDirect((GameObject go) => go.HasTag("Cleo_TerraFirma_IsAnyStone")).Count;
			string msg = "pestles in pack: " + pestles.Count + "\nstone stacks in pack: " + stones;
			if (pestles.Count > 0)
			{
				GameObject pestle = pestles[0];
				msg += "\npestle has StoneMill part: " + (pestle.GetPart<Cleo_TerraFirma_StoneMill>() != null);
				msg += "\npestle wants GetInventoryActionsEvent: " + pestle.WantEvent(GetInventoryActionsEvent.ID, 0);
				msg += "\npestle blueprint: " + pestle.Blueprint;
			}
			Popup.Show(msg);
		}

		/// <summary>One of every fissure rock: takeables into the pack, creatures and standing
		/// stone into open cells nearby. Enumerated by blueprint prefix, so future rocks join free.</summary>
		[WishCommand(Command = "fissurerocks")]
		public static void FissureRocks()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = origin.GetLocalAdjacentCells(3);
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			int pocketed = 0, placed = 0, skipped = 0;
			foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
			{
				if (!bp.Name.StartsWith("Cleo_TerraFirma_Rock_"))
					continue;
				GameObject rock = GameObjectFactory.Factory.CreateObject(bp.Name);
				if (rock == null)
					continue;
				if (rock.Brain == null && rock.Physics != null && rock.Physics.Takeable)
				{
					Ply.Inventory.AddObject(rock);
					pocketed++;
				}
				else if (open.Count > 0)
				{
					open[0].AddObject(rock);
					open.RemoveAt(0);
					placed++;
				}
				else
				{
					rock.Obliterate();
					skipped++; // no room; wish again somewhere more open
				}
			}
			MessageQueue.AddPlayerMessage("the ceiling provides: " + pocketed + " rocks pocketed, " + placed + " placed nearby" + (skipped > 0 ? ", " + skipped + " skipped for lack of room" : "") + ".", Capitalize: false);
		}
	}
}
