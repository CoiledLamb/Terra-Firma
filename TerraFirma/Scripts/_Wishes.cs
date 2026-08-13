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

		/// <summary>Whorl VFX sampler (dev door, sitting 2026-08-12): fires all four outcome
		/// beats at once, spatially separated so each reads. Devour swirl orbits the PLAYER
		/// (palette-thieved from a freshly spawned torch -- red/white flecks prove the theft
		/// at a glance); ricochet elbow 3 east; swat loot-pointer 3 west; fizzle grit 3 south;
		/// refract second-leg track from 3 north heading up-right. Pure FX, no mechanics.</summary>
		[WishCommand(Command = "tfwhorlfx")]
		public static void WhorlFxSampler()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (origin == null || zone == null)
				return;
			Cell At(int dx, int dy) => zone.GetCell(
				System.Math.Max(0, System.Math.Min(zone.Width - 1, origin.X + dx)),
				System.Math.Max(0, System.Math.Min(zone.Height - 1, origin.Y + dy)));
			GameObject morsel = GameObject.Create("Torch");
			Cleo_TerraFirma_GravelWhorlFX.PlayDevourSwirl(Ply, morsel);
			morsel.Obliterate();
			Cleo_TerraFirma_GravelWhorlFX.PlayRicochetElbow(At(3, 0));
			Cell swatFrom = At(-3, 0);
			Cell swatLanding = At(-4, -1) != swatFrom ? At(-4, -1) : At(-2, 1);
			Cleo_TerraFirma_GravelWhorlFX.PlaySwatBeat(swatFrom, swatLanding);
			Cleo_TerraFirma_GravelWhorlFX.PlaySwatBeat(At(0, 3), null);
			Cleo_TerraFirma_GravelWhorlFX.PlayRefractTrack(At(0, -3), 135); // engine convention: x+=Sin, y+=Cos -> up-right
			Cleo_TerraFirma_GravelWhorlFX.PlayDevourLight(At(3, -3), 'R', "beamColor0::#FF0000;;beamColor1::#FF8000"); // the light-devour beat in the laser rifle's beam colors
			MessageQueue.AddPlayerMessage("tfwhorlfx: devour swirl on you (torch palette), elbow 3E, swat pointer 3W, fizzle 3S, refract track 3N, laser ember-catch 3NE.", Capitalize: false);
		}

		/// <summary>Live-fire devour gym (user request, VFX sitting 2026-08-12: "temporarily
		/// turn devour chance to 100%, set idkfa to on, and spawn a pack of baboons so i can
		/// see it as many times as i want"). TOGGLE: first call arms force-devour + godmode
		/// and spawns the pack (6 baboons at throwing range, 5 Small Stones each = 30 devours
		/// on tap); second call disarms force-devour and godmode, baboons are yours to keep.
		/// Needs the whorl actually up (EB toggled on with stones) -- warns if it is not.</summary>
		[WishCommand(Command = "tfwhorlgym")]
		public static void WhorlGym()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (origin == null || zone == null)
				return;
			bool arming = !XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForceDeflect;
			XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForceDeflect = arming;
			The.Core.IDKFA = arming;
			Helpers.VerifyLog("GVW", $"tfwhorlgym: force-deflect {(arming ? "ON" : "OFF")}, godmode {(arming ? "ON" : "OFF")}");
			if (!arming)
			{
				MessageQueue.AddPlayerMessage("tfwhorlgym: force-deflect and godmode OFF. The baboons are yours to keep.", Capitalize: false);
				return;
			}
			List<Cell> ring = origin.GetLocalAdjacentCells(6);
			ring.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat") || origin.DistanceTo(c) < 4);
			int spawned = 0;
			while (spawned < 6 && ring.Count > 0)
			{
				int idx = Stat.Random(0, ring.Count - 1);
				GameObject baboon = GameObject.Create("Baboon");
				ring[idx].AddObject(baboon);
				baboon.MakeActive();
				baboon.Brain.Target = Ply; // no warm-up: they open throwing
				ring.RemoveAt(idx);
				spawned++;
			}
			string whorlNote = Ply.HasEffect<XRL.World.Effects.Cleo_TerraFirma_GravelWhorl>()
				? "" : " NOTE: your whorl is not up -- toggle Earthen Barrage with stones in reserve or nothing will deflect.";
			MessageQueue.AddPlayerMessage($"tfwhorlgym: force-deflect ON, godmode ON, {spawned} baboons spawned at throwing range. Wish again to disarm; tfwhorloutcome picks the outcome.{whorlNote}", Capitalize: false);
		}

		/// <summary>Laser gym (user request 2026-08-12: "spawns a laser turret tinker, calls
		/// idkfa, and makes sure earthen barrage is levelled to 10 so attune works"; round 2
		/// same day, after a run of resonant=False pass-throughs in the log: character level
		/// raised to 19 so the mutation ranks stick -- the user's own fix -- and the wish now
		/// flips the whorl AND Attune Stones toggles itself through the mutation's command
		/// path, gated on current state so a re-wish never flips them back OFF): EB ensured +
		/// leveled to 10, reserve topped, godmode + force-devour armed (wish tfwhorlgym once
		/// to disarm both), Laser Turret Tinker spawned at range and aimed at you.</summary>
		[WishCommand(Command = "tfwhorllaser")]
		public static void WhorlLaserGym()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (origin == null || zone == null)
				return;
			Cleo_TerraFirma_EarthenBarrage eb = EnsureWhorlReady(Ply);
			if (!eb.Resonant)
				CommandEvent.Send(Ply, "Cleo_TerraFirma_CommandAttuneStones"); // resonant is the mode that eats lasers
			XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForceDeflect = true;
			The.Core.IDKFA = true;
			List<Cell> ring = origin.GetLocalAdjacentCells(9);
			ring.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat") || origin.DistanceTo(c) < 6);
			GameObject tinker = null;
			if (ring.Count > 0)
			{
				tinker = GameObject.Create("Laser Turret Tinker");
				ring[Stat.Random(0, ring.Count - 1)].AddObject(tinker);
				tinker.MakeActive();
				tinker.Brain.Target = Ply;
			}
			Helpers.VerifyLog("GVW", $"tfwhorllaser: char L{Ply.GetStatValue("Level")}, EB L{eb.Level} charges {eb.Charges}, whorl {(Ply.HasEffect<XRL.World.Effects.Cleo_TerraFirma_GravelWhorl>() ? "UP" : "DOWN?!")}, resonant={eb.Resonant}, force-deflect ON, godmode ON, tinker {(tinker != null ? "spawned" : "NO OPEN CELL")}");
			MessageQueue.AddPlayerMessage($"tfwhorllaser: char level {Ply.GetStatValue("Level")}, EB {eb.Level}, reserve full, whorl {(Ply.HasEffect<XRL.World.Effects.Cleo_TerraFirma_GravelWhorl>() ? "up" : "DOWN -- something refused, check the log")} and {(eb.Resonant ? "attuned" : "NOT attuned?!")}, godmode + force-deflect ON{(tinker != null ? ", Laser Turret Tinker at range" : " -- no open cell for the tinker, move somewhere roomier and rewish")}. Wish tfwhorlgym to disarm; tfwhorloutcome to pick the outcome.", Capitalize: false);
		}

		/// <summary>Shared gym plumbing (extracted at the swat/rico-lock round): character
		/// level to 19 (mutation ranks cap on character level; 19 carries rank 10), EB
		/// ensured at level 10, reserve topped, whorl toggle flipped on through the
		/// mutation's own command if it is down. Attune is deliberately NOT touched here --
		/// callers and tfwhorlattune own the mode.</summary>
		private static Cleo_TerraFirma_EarthenBarrage EnsureWhorlReady(GameObject Ply)
		{
			if (Ply.GetStatValue("Level") < 19)
				Ply.GetStat("Level").BaseValue = 19;
			Mutations muts = Ply.RequirePart<Mutations>();
			Cleo_TerraFirma_EarthenBarrage eb = Ply.GetPart<Cleo_TerraFirma_EarthenBarrage>();
			if (eb == null)
			{
				muts.AddMutation(new Cleo_TerraFirma_EarthenBarrage());
				eb = Ply.GetPart<Cleo_TerraFirma_EarthenBarrage>();
			}
			if (eb.Level < 10)
				muts.LevelMutation(eb, 10);
			eb.Charges = Cleo_TerraFirma_EarthenBarrage.MaxCharges(eb.Level);
			eb.SyncWhorl();
			if (!Ply.HasEffect<XRL.World.Effects.Cleo_TerraFirma_GravelWhorl>())
				CommandEvent.Send(Ply, "Cleo_TerraFirma_CommandEarthenBarrage"); // also auto-equips a stone
			return eb;
		}

		/// <summary>Outcome locks (user: "a wish that lock the chance at 100 for swat, and
		/// another to lock it at 100 for ricochet - make sure it's lvl 10 and i can swap
		/// between attune and base"): full gym setup + force-deflect + the outcome pinned.
		/// Mode swapping = tfwhorlattune. Attackers come from tfwhorlgym (throwers) or
		/// tfwhorllaser (beams).</summary>
		[WishCommand(Command = "tfwhorlswat")]
		public static void WhorlSwatLock() => WhorlOutcomeLock(2, "SWAT");

		[WishCommand(Command = "tfwhorlrico")]
		public static void WhorlRicoLock() => WhorlOutcomeLock(3, "RICOCHET");

		private static void WhorlOutcomeLock(int mode, string name)
		{
			GameObject Ply = The.Player;
			if (Ply?.CurrentCell == null)
				return;
			Cleo_TerraFirma_EarthenBarrage eb = EnsureWhorlReady(Ply);
			XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForceDeflect = true;
			XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForcedOutcome = mode;
			The.Core.IDKFA = true;
			Helpers.VerifyLog("GVW", $"outcome lock: {name}, EB L{eb.Level}, resonant={eb.Resonant}");
			MessageQueue.AddPlayerMessage($"every proc now rolls {name}. EB {eb.Level}, whorl up, currently {(eb.Resonant ? "ATTUNED (resonant): lasers" : "BASE (gravel): matter")} -- tfwhorlattune swaps. Godmode + force-deflect ON; tfwhorlgym disarms.", Capitalize: false);
		}

		/// <summary>The attune/base swapper: one wish flips the whorl's mode through the
		/// mutation's own Attune Stones command (retune + resync side effects included),
		/// setting up EB first so it always works.</summary>
		[WishCommand(Command = "tfwhorlattune")]
		public static void WhorlAttuneSwap()
		{
			GameObject Ply = The.Player;
			if (Ply?.CurrentCell == null)
				return;
			Cleo_TerraFirma_EarthenBarrage eb = EnsureWhorlReady(Ply);
			CommandEvent.Send(Ply, "Cleo_TerraFirma_CommandAttuneStones");
			MessageQueue.AddPlayerMessage($"whorl now {(eb.Resonant ? "ATTUNED (resonant): deflects lasers, physical passes through" : "BASE (gravel): deflects matter, light passes through")}.", Capitalize: false);
		}

		/// <summary>Outcome picker for the gyms (user, VFX sitting 2026-08-12: "we still need
		/// to verify the 50/30 animations"): cycles the forced-outcome pin devour -> swat ->
		/// ricochet -> natural roll -> devour. Only bites while a gym has force-deflect
		/// armed; wish repeatedly to land on the outcome under test.</summary>
		[WishCommand(Command = "tfwhorloutcome")]
		public static void WhorlOutcomeCycle()
		{
			int mode = (XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForcedOutcome + 1) % 4;
			XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForcedOutcome = mode;
			string name = mode == 1 ? "DEVOUR" : mode == 2 ? "SWAT" : mode == 3 ? "RICOCHET" : "natural 50/30/20 roll";
			bool armed = XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForceDeflect;
			Helpers.VerifyLog("GVW", $"tfwhorloutcome: forced outcome -> {name} (force-deflect {(armed ? "armed" : "NOT armed")})");
			MessageQueue.AddPlayerMessage($"tfwhorloutcome: every proc now rolls {name}.{(armed ? "" : " NOTE: force-deflect is not armed -- run tfwhorlgym or tfwhorllaser first.")}", Capitalize: false);
		}
	}
}
