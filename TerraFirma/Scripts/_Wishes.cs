using System.Collections.Generic;
using XRL;
using XRL.Messages;
using XRL.Rules;
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

		/// <summary>One of every LIVE fissure rock: takeables into the pack, creatures and standing
		/// stone into open cells nearby. BETA-HELD trim (redoubt pivot, RULED 2026-08-02): the
		/// prefix enumeration would mint ~50 held blueprints, so the wish filters to the keeper
		/// roster + structural pieces for the beta. Restore = empty the BetaLive set.</summary>
		private static readonly HashSet<string> BetaLive = new HashSet<string>
		{
			// keepers (trim RULED 2026-08-02)
			"Cleo_TerraFirma_Rock_Ajistone",
			"Cleo_TerraFirma_Rock_BaboonFigurine",
			"Cleo_TerraFirma_Rock_Rubber",
			"Cleo_TerraFirma_Rock_Vantabsidian",
			"Cleo_TerraFirma_Rock_Shunting",
			"Cleo_TerraFirma_Rock_Omnigeyser1",
			"Cleo_TerraFirma_Rock_Omnigeyser2",
			"Cleo_TerraFirma_Rock_Omnigeyser3",
			// structural fabric (incl. the ambiguity-1 promotions)
			"Cleo_TerraFirma_Rock_Pebbles",
			"Cleo_TerraFirma_Rock_Generic",
			"Cleo_TerraFirma_Rock_Rubble",
			"Cleo_TerraFirma_Rock_CrumbledWall",
			"Cleo_TerraFirma_Rock_Wall",
			"Cleo_TerraFirma_Rock_Crack",
			"Cleo_TerraFirma_Rock_Stalagmite",
			"Cleo_TerraFirma_Rock_ShatteredStalactite",
		};

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
				if (!BetaLive.Contains(bp.Name))
					continue; // BETA-HELD rock; see the trim ruling
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

		/// <summary>Pen-distribution measurement for the Fissure pulse feel session (build item,
		/// spec design/fissure-pulse-rework-2026-08-08.md): Stat.RollDamagePenetrations sampled
		/// a few thousand times per cell across PV 4..10 x AV 0/4/8/12, expected penetrations
		/// and expected ring-1 damage under the v3 pulse ladder written to Player.log as
		/// [TF-VERIFY][FIS] lines. The feel session dials against MEASUREMENTS, not estimates.
		/// Rides the TF-VERIFY strip at 1.0 like the rest of the harness.</summary>
		[WishCommand(Command = "tfpen")]
		public static void PenDistribution()
		{
			const int TRIALS = 4000;
			int[] avs = { 0, 4, 8, 12 };
			Helpers.VerifyLog("FIS", $"tfpen: expected penetrations over {TRIALS} rolls per cell (PV rows 4..10, AV cols 0/4/8/12)");
			double[,] expectedPens = new double[11, 13];
			for (int pv = 4; pv <= 10; pv++)
			{
				List<string> cells = new List<string>();
				foreach (int av in avs)
				{
					long total = 0;
					for (int i = 0; i < TRIALS; i++)
						total += Stat.RollDamagePenetrations(av, pv, pv);
					expectedPens[pv, av] = (double)total / TRIALS;
					cells.Add($"AV{av}={expectedPens[pv, av]:F2}");
				}
				Helpers.VerifyLog("FIS", $"tfpen PV {pv}: {string.Join(", ", cells.ToArray())}");
			}
			// expected ring-1 TOTAL damage (all three pulses connecting) = expected pens x the
			// level's summed average dice -- the number the staircase is actually dialing.
			// Sample levels track the v5 staircase's dice-change points, not the old bands.
			int[] bandLevels = { 1, 4, 6, 8, 10 };
			foreach (int lv in bandLevels)
			{
				double diceSum = 0;
				List<string> ladder = new List<string>();
				for (int p = 1; p <= Cleo_TerraFirma_Fissure.PULSE_COUNT; p++)
				{
					string dice = Cleo_TerraFirma_Fissure.GetPulseDice(lv, p);
					long tot = 0;
					for (int i = 0; i < TRIALS; i++)
						tot += dice.RollCached();
					diceSum += (double)tot / TRIALS;
					ladder.Add(dice);
				}
				foreach (int av in avs)
				{
					List<string> cells = new List<string>();
					for (int pv = 4; pv <= 10; pv++)
						cells.Add($"PV{pv}={expectedPens[pv, av] * diceSum:F1}");
					Helpers.VerifyLog("FIS", $"tfpen L{lv} ({string.Join("/", ladder.ToArray())}, avg sum {diceSum:F1}) ring-1 total vs AV {av}: {string.Join(", ", cells.ToArray())}");
				}
			}
			MessageQueue.AddPlayerMessage("tfpen: pen and damage distributions written to Player.log.", Capitalize: false);
		}
	}
}
