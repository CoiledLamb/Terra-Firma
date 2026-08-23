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

		/// <summary>Whorl VFX sampler (dev door, sitting 2026-08-12; rico beat swapped at the
		/// 2026-08-14 redesign ruling): fires all outcome beats at once, spatially separated
		/// so each reads. Devour swirl orbits the PLAYER (palette-thieved from a freshly
		/// spawned torch -- red/white flecks prove the theft at a glance); rico bounce+flight
		/// 3 east flying to 6 east (no leg-1 delay -- the bounce fires immediately here); swat
		/// loot-pointer 3 west; fizzle grit 3 south; refract second-leg track from 3 north
		/// heading up-right. Pure FX, no mechanics.</summary>
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
			GameObject stone = GameObject.Create("Small Stone");
			Cleo_TerraFirma_GravelWhorlFX.PlayRicochetFlight(stone, At(3, 0), At(6, 0), 0f);
			stone.Obliterate();
			morsel.Obliterate();
			Cell swatFrom = At(-3, 0);
			Cell swatLanding = At(-4, -1) != swatFrom ? At(-4, -1) : At(-2, 1);
			Cleo_TerraFirma_GravelWhorlFX.PlaySwatBeat(swatFrom, swatLanding);
			Cleo_TerraFirma_GravelWhorlFX.PlaySwatBeat(At(0, 3), null);
			Cleo_TerraFirma_GravelWhorlFX.PlayRefractTrack(At(0, -3), 135); // engine convention: x+=Sin, y+=Cos -> up-right
			Cleo_TerraFirma_GravelWhorlFX.PlayDevourLight(At(3, -3), 'R', "beamColor0::#FF0000;;beamColor1::#FF8000"); // the light-devour beat in the laser rifle's beam colors
			MessageQueue.AddPlayerMessage("tfwhorlfx: devour swirl on you (torch palette), rico bounce 3E flying to 6E, swat pointer 3W, fizzle 3S, refract track 3N, laser ember-catch 3NE.", Capitalize: false);
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

		/// <summary>The devour lock, added 2026-08-16 (user asked for "the one like tfwhorlrico
		/// but only for devouring small stones" and found there wasn't one). Swat and ricochet
		/// got shims because the 2026-08-12 request named those two by name; devour was already
		/// mode 1 and never got its own door. Worth having anyway: tfwhorlgym pins devour by
		/// default but assumes your whorl is already up, while this runs EnsureWhorlReady first
		/// -- EB granted, leveled to 10, reserve topped, whorl toggled on -- so it works from a
		/// bare character. Attackers still come from tfwhorlgym or tfwhorllaser.</summary>
		[WishCommand(Command = "tfwhorldevour")]
		public static void WhorlDevourLock() => WhorlOutcomeLock(1, "DEVOUR");

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

		/// <summary>Geomancy wall-HP regression probe (user flag 2026-08-12: "the harmony patch we
		/// used for geomancy stopped letting us see wall HP when a rock is thrown"). Static analysis
		/// cleared every layer (patch code unchanged since the 1.1 era, target signature alive,
		/// Strings.WoundLevel calls the patched (GameObject, GameObject) overload, walls carry no
		/// NoDamageReadout, Gemstone merge intact) -- the remaining suspect is JIT INLINING of the
		/// one-line HasScanningFor delegator, which a static read cannot see. This wish attaches the
		/// scan part to the nearest wall and asks the same question three ways:
		///   compiled call  -- can itself be inlined (same blind spot as the engine's callers)
		///   reflection call -- NEVER inlined; answers "did the patch apply at all"
		///   Strings.WoundLevel -- the actual display path; numbers present = the UI would show HP
		/// Matrix: reflect FALSE = patch never applied (check buildLog / PatchAll gating).
		/// reflect TRUE + WoundLevel bare = the display path's call is inlined past the detour.
		/// reflect TRUE + WoundLevel numeric = engine chain fine; the bug is upstream (the part
		/// never attaches in real play -- next probe is a live gemstone throw). Run by hand, not
		/// in tfbatch; the part is left on the wall (Duration 50) so the look UI can be eyeballed.</summary>
		[WishCommand(Command = "tfgeoscan")]
		public static void GeoScanProbe()
		{
			GameObject Ply = The.Player;
			if (Ply?.CurrentCell == null)
				return;
			GameObject wall = null;
			foreach (Cell c in Ply.CurrentCell.GetLocalAdjacentCellsCircular(3, true))
			{
				wall = c.GetObjects(x => x.IsWall())?.Find(x => true);
				if (wall != null)
					break;
			}
			if (wall == null)
			{
				MessageQueue.AddPlayerMessage("tfgeoscan: no wall within 3 cells -- stand near one.", Capitalize: false);
				return;
			}
			if (wall.TryGetPart<XRL.World.Parts.Cleo_TerraFirma_PartGeomancyScan>(out var scan))
				scan.Duration = 50;
			else
			{
				wall.AddPart<XRL.World.Parts.Cleo_TerraFirma_PartGeomancyScan>();
				wall.GetPart<XRL.World.Parts.Cleo_TerraFirma_PartGeomancyScan>().Duration = 50;
			}
			bool compiled = XRL.World.Capabilities.Scanning.HasScanningFor(Ply, wall);
			bool reflected = false;
			string reflectNote = "ok";
			try
			{
				var mi = typeof(XRL.World.Capabilities.Scanning).GetMethod("HasScanningFor",
					new System.Type[] { typeof(GameObject), typeof(GameObject) });
				reflected = (bool)mi.Invoke(null, new object[] { Ply, wall });
			}
			catch (System.Exception ex)
			{
				reflectNote = ex.GetType().Name;
			}
			string wound = Strings.WoundLevel(wall) ?? "";
			bool woundNumeric = wound.Contains("/");
			string verdict =
				!reflected ? "PATCH NOT APPLIED (reflection path saw no detour -- check PatchAll/buildLog)" :
				!woundNumeric ? "INLINING CONFIRMED (patch alive via reflection, but WoundLevel's internal call bypasses it)" :
				"ENGINE CHAIN FINE (WoundLevel shows numbers -- the bug is upstream: does the part attach on real gemstone throws?)";
			Helpers.VerifyLog("GEOSCAN", $"wall={wall.Blueprint} compiled={compiled} reflected={reflected}({reflectNote}) wound=\"{wound}\" -> {verdict}");
			MessageQueue.AddPlayerMessage($"tfgeoscan [{wall.Blueprint}]: compiled={compiled}, reflected={reflected}, WoundLevel {(woundNumeric ? "NUMERIC" : "bare")}. {verdict} Scan part left on the wall (50 turns) -- look at it now: exact HP visible?", Capitalize: false);
		}
		/// <summary>Creature staging wish (user 2026-08-15, opening the creatures branch: a
		/// branch "for the creatures, including stonecarvers, so i can generate them in game";
		/// staging-wish-first chosen at the same sitting, the real spawn wiring deliberately
		/// after). Lines the whole roster up east of the player -- brains pacified and frozen,
		/// godmode on -- so they can be walked past and looked at instead of fought. The
		/// tfgazewall idiom: one wish, the subjects side by side.
		///
		/// Every spawn logs its Render tile string, so art the deploy never carried shows in
		/// the log as well as on screen (rockhound and snapjaw miner both sat on undeployed
		/// sprites from 2026-08-11 until the 0.1.2 deploy).
		///
		/// The last two entries are SPRITE CHECKS, NOT CREATURES: no stonecarver NPC blueprint
		/// exists yet -- the roster build is this branch's real job and its color map still has
		/// unruled slots -- so the wish paints stonecarver.png and stonerat.png onto plain
		/// WatervineFarmer bodies at runtime. Nothing about the roster is committed by that; it
		/// is the art standing in the world at true scale, and it ships no design.
		///
		/// Wish again to release: brains restored exactly as they were, godmode off.
		/// BRANCH-ONLY -- this does not ride 0.1.2.</summary>
		private static readonly (string Blueprint, string Note)[] CreatureRoster = new[]
		{
			("Cleo_TerraFirma_Cobble Baboon", "EB 4: orbiting pebbles, thrown stones, visible whorl. Butchers into cobblefur."),
			("Cleo_TerraFirma_Rockhound", "RockSweeper gut -- swallow and spill."),
			("Cleo_TerraFirma_Snapjaw Miner", "the pickaxe IS the AI: Digging installed, PathAsBurrower answered."),
			("Cleo_TerraFirma_Burrowing Urchin", "wall-walker, Burrowing 5, aculei in the belly."),
			("Cleo_TerraFirma_BoarVessel", "Husangeion -- passive, 64-dram tank, glaze tracks the liquid."),
		};

		private static readonly (string Tile, string Label)[] CarverSpriteChecks = new[]
		{
			("stonecarver.png", "STONECARVER SPRITE -- human base, no blueprint yet"),
			("stonerat.png", "STONERAT SPRITE -- mole base, no blueprint yet"),
		};

		private static readonly List<(GameObject Object, bool Mobile, bool Wanders, bool Hostile)> StagedRoster =
			new List<(GameObject, bool, bool, bool)>();

		[WishCommand(Command = "tfcreatures")]
		public static void CreatureStage()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (origin == null || zone == null)
				return;

			if (StagedRoster.Count > 0)
			{
				int released = 0;
				foreach (var staged in StagedRoster)
				{
					if (staged.Object == null || !staged.Object.IsValid() || staged.Object.Brain == null)
						continue;
					staged.Object.Brain.Mobile = staged.Mobile;
					staged.Object.Brain.Wanders = staged.Wanders;
					staged.Object.Brain.Hostile = staged.Hostile;
					released++;
				}
				StagedRoster.Clear();
				The.Core.IDKFA = false;
				Helpers.VerifyLog("STAGE", $"tfcreatures: released {released}, godmode OFF");
				MessageQueue.AddPlayerMessage($"tfcreatures: {released} released -- brains restored as they were, godmode OFF. The menagerie is yours.", Capitalize: false);
				return;
			}

			int slot = 0;
			Cell NextCell()
			{
				while (slot < 25)
				{
					int dx = 2 + slot;
					slot++;
					foreach (int dy in new int[] { 0, -1, 1, -2, 2 })
					{
						int x = origin.X + dx;
						int y = origin.Y + dy;
						if (x < 0 || y < 0 || x >= zone.Width || y >= zone.Height)
							continue;
						Cell candidate = zone.GetCell(x, y);
						if (candidate == null || candidate.IsSolid() || candidate.HasObjectWithPart("Combat"))
							continue;
						return candidate;
					}
				}
				return null;
			}

			void Stage(GameObject subject, Cell where, string label)
			{
				where.AddObject(subject);
				subject.MakeActive();
				if (subject.Brain != null)
				{
					StagedRoster.Add((subject, subject.Brain.Mobile, subject.Brain.Wanders, subject.Brain.Hostile));
					subject.Brain.Mobile = false;   // stand still and be looked at
					subject.Brain.Wanders = false;
					subject.Brain.Hostile = false;
				}
				string tile = subject.Render?.Tile;
				Helpers.VerifyLog("STAGE", $"{label} at {where.X},{where.Y}: blueprint={subject.Blueprint} tile=\"{(tile.IsNullOrEmpty() ? "(none -- renders as its glyph)" : tile)}\" name=\"{subject.DisplayNameOnlyDirect}\"");
			}

			The.Core.IDKFA = true;
			int placed = 0;
			List<string> legend = new List<string>();
			List<string> trouble = new List<string>();

			foreach (var entry in CreatureRoster)
			{
				GameObject subject = null;
				try
				{
					subject = GameObject.Create(entry.Blueprint);
				}
				catch (System.Exception ex)
				{
					trouble.Add($"{entry.Blueprint} would not create ({ex.GetType().Name})");
					Helpers.VerifyLog("STAGE", $"{entry.Blueprint}: CREATE FAILED ({ex.GetType().Name}) -- blueprint missing or malformed");
					continue;
				}
				if (subject == null)
				{
					trouble.Add($"{entry.Blueprint} came back null -- blueprint missing?");
					Helpers.VerifyLog("STAGE", $"{entry.Blueprint}: CREATE returned null");
					continue;
				}
				Cell where = NextCell();
				if (where == null)
				{
					subject.Obliterate();
					trouble.Add($"{entry.Blueprint}: no open cell left in the row");
					continue;
				}
				Stage(subject, where, entry.Blueprint);
				placed++;
				legend.Add($"{placed}. {subject.DisplayNameOnlyDirect} -- {entry.Note}");
			}

			foreach (var check in CarverSpriteChecks)
			{
				GameObject subject = null;
				try
				{
					subject = GameObject.Create("WatervineFarmer");
				}
				catch (System.Exception ex)
				{
					trouble.Add($"sprite check {check.Tile}: no chassis ({ex.GetType().Name})");
					continue;
				}
				if (subject == null)
				{
					trouble.Add($"sprite check {check.Tile}: WatervineFarmer chassis came back null");
					continue;
				}
				Cell where = NextCell();
				if (where == null)
				{
					subject.Obliterate();
					trouble.Add($"sprite check {check.Tile}: no open cell left in the row");
					continue;
				}
				subject.Render.Tile = check.Tile;
				subject.Render.DisplayName = check.Label;
				Stage(subject, where, check.Label);
				placed++;
				legend.Add($"{placed}. {check.Label} -- art only, on a plain human body.");
			}

			MessageQueue.AddPlayerMessage($"tfcreatures: {placed} staged in a row east of you, frozen and pacified, godmode ON. Wish again to release them.", Capitalize: false);
			foreach (string line in legend)
				MessageQueue.AddPlayerMessage(line, Capitalize: false);
			foreach (string line in trouble)
				MessageQueue.AddPlayerMessage("tfcreatures TROUBLE: " + line, Capitalize: false);
		}
		/// <summary>Stonecarver pack stager. Shape is the user's, 2026-08-15: "i imagined the pack
		/// would spawn a mix of each type from a poptable, with one leader." It now does exactly
		/// that, because the roles finally exist to mix.
		///
		/// Rank and file roll from `Cleo_TerraFirma_Stonecarver Pack` (SnapjawParty1 idiom), so the
		/// composition varies per wish and the rockhound turns up SOMETIMES, per its own ruling.
		/// The leader is spawned separately: a PLAIN carver chassis run through the epithet builder.
		/// That is not a shortcut, it is the ruled structure showing its shape. Rank and file take
		/// their mutation from their PROFESSION; the overseer takes its mutation from its NAME. It
		/// needs no profession blueprint for the same reason it needs no bespoke art.
		///
		/// Printed to chat: the leader's full roll, then the actual composition of the pack that
		/// spawned, so the table's weights can be judged from real draws rather than from the XML.
		///
		/// Pacified and frozen, sharing the tfcreatures staging list, so either wish's second call
		/// releases everything. BRANCH-ONLY.</summary>
		private const string CarverPackTable = "Cleo_TerraFirma_Stonecarver Pack";

		[WishCommand(Command = "tfcarverpack")]
		public static void CarverPack()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (origin == null || zone == null)
				return;

			if (StagedRoster.Count > 0)
			{
				CreatureStage(); // one release path for both wishes
				return;
			}

			List<Cell> ring = origin.GetLocalAdjacentCells(6);
			ring.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat") || origin.DistanceTo(c) < 3);

			Cell TakeCell()
			{
				if (ring.Count == 0)
					return null;
				int idx = Stat.Random(0, ring.Count - 1);
				Cell c = ring[idx];
				ring.RemoveAt(idx);
				return c;
			}

			void Freeze(GameObject who)
			{
				who.MakeActive();
				if (who.Brain == null)
					return;
				StagedRoster.Add((who, who.Brain.Mobile, who.Brain.Wanders, who.Brain.Hostile));
				who.Brain.Mobile = false;
				who.Brain.Wanders = false;
				who.Brain.Hostile = false;
			}

			// the leader first, so a cramped spot still gets the thing worth looking at
			Cell leaderCell = TakeCell();
			if (leaderCell == null)
			{
				MessageQueue.AddPlayerMessage("tfcarverpack: no open cell at range 3-6, move somewhere roomier.", Capitalize: false);
				return;
			}
			GameObject overseer = GameObject.Create("Cleo_TerraFirma_Stonecarver");
			if (overseer == null)
			{
				MessageQueue.AddPlayerMessage("tfcarverpack: stonecarver chassis came back null.", Capitalize: false);
				return;
			}
			StonecarverEpithet.Roll roll = StonecarverEpithet.Build(overseer);
			leaderCell.AddObject(overseer);
			Freeze(overseer);
			Helpers.VerifyLog("CARVER", $"overseer: name={roll.PersonalName} ({roll.NamePieces}) byname={roll.Byname} material={roll.Material} -> {roll.PerkText} craft={roll.Craft} -> {roll.KitText}");

			var census = new Dictionary<string, int>();
			int spawned = 0;
			List<string> trouble = new List<string>();
			foreach (var entry in PopulationManager.Generate(CarverPackTable))
			{
				for (int n = 0; n < entry.Number; n++)
				{
					Cell where = TakeCell();
					if (where == null)
					{
						trouble.Add("ran out of open cells, pack truncated");
						break;
					}
					GameObject member = null;
					try
					{
						member = GameObject.Create(entry.Blueprint);
					}
					catch (System.Exception ex)
					{
						trouble.Add($"{entry.Blueprint} would not create ({ex.GetType().Name})");
						continue;
					}
					if (member == null)
					{
						trouble.Add($"{entry.Blueprint} came back null");
						continue;
					}
					where.AddObject(member);
					Freeze(member);
					string label = member.DisplayNameOnlyDirect;
					census[label] = (census.ContainsKey(label) ? census[label] : 0) + 1;
					spawned++;
				}
			}

			The.Core.IDKFA = true;
			MessageQueue.AddPlayerMessage($"tfcarverpack: 1 overseer + {spawned} rank and file, frozen and pacified, godmode ON. Wish again (or tfcreatures) to release.", Capitalize: false);
			MessageQueue.AddPlayerMessage($"overseer: {{{{W|{roll.PersonalName} {roll.Byname}}}}} (legendary, pink)", Capitalize: false);
			MessageQueue.AddPlayerMessage($"  craft \"{roll.Craft}\" grants {roll.KitText}; material \"{roll.Material}\" grants {roll.PerkText}", Capitalize: false);
			MessageQueue.AddPlayerMessage($"  name assembled from {roll.NamePieces}", Capitalize: false);
			List<string> lines = new List<string>();
			foreach (var pair in census)
				lines.Add($"{pair.Value}x {pair.Key}");
			MessageQueue.AddPlayerMessage(spawned > 0
				? $"this draw: {string.Join(", ", lines)}. Weights are DRAFT, judge them from a few draws."
				: "this draw came up empty, which the table allows. Wish again.", Capitalize: false);
			foreach (string line in trouble)
				MessageQueue.AddPlayerMessage("tfcarverpack TROUBLE: " + line, Capitalize: false);
			Helpers.VerifyLog("CARVER", $"tfcarverpack: overseer + {spawned} rank and file [{string.Join("/", lines)}]");
		}
		/// <summary>Reputation jingle audition (user 2026-08-15: "can i listen to the files and
		/// judge myself?"). The clips are packed inside Unity's resources.assets with no loose ogg
		/// anywhere, so extracting them would need AssetRipper. Playing them does not: they are
		/// ordinary engine sound paths and the mod already plays those.
		///
		/// tfjingle cycles the candidates for the Quarriers' Guild, one per wish, naming each
		/// before it plays. tfjingleneg plays the NEGATIVE half of whichever is currently selected,
		/// because a faction picks a PAIR and the sour one is half of what you are choosing.
		///
		/// Sharing a jingle is base-game practice, not a compromise: sultanCult is used by five
		/// factions and village by four. BRANCH-ONLY.</summary>
		private static readonly (string Key, string Why)[] JingleCandidates = new[]
		{
			("mopango", "the underground dwellers; your own tracker already runs a mopango-symmetry thread, and you just put the mole on the emblem"),
			("cragmensch", "the stone-creature faction, the closest thing in the game to a stone people"),
			("barathrumites", "underground artisans, guild-adjacent, but they read strongly as tinkers"),
			("farmersGuild", "what the guild is set to now: workaday, institutional"),
			("merchantsGuild", "the other guild; the civic register at its most formal"),
			("hermits", "outsider register, if quarriers should sound apart from the settled world"),
			("issachariTribe", "a desert people rather than a trade body"),
		};

		private static int JingleIndex = -1;

		[WishCommand(Command = "tfjingle")]
		public static void JingleAudition()
		{
			JingleIndex = (JingleIndex + 1) % JingleCandidates.Length;
			var pick = JingleCandidates[JingleIndex];
			string path = $"Sounds/Reputation/sfx_reputation_{pick.Key}_positive";
			SoundManager.PreloadClipSet(path);
			The.Player?.PlayWorldSound(path, 1f);
			MessageQueue.AddPlayerMessage($"tfjingle [{JingleIndex + 1}/{JingleCandidates.Length}] {{{{W|{pick.Key}}}}} positive: {pick.Why}", Capitalize: false);
			MessageQueue.AddPlayerMessage("wish tfjingle again for the next candidate, or tfjingleneg to hear this one's sour half.", Capitalize: false);
			Helpers.VerifyLog("JINGLE", $"audition {pick.Key} positive");
		}

		[WishCommand(Command = "tfjingleneg")]
		public static void JingleAuditionNegative()
		{
			if (JingleIndex < 0)
			{
				MessageQueue.AddPlayerMessage("tfjingleneg: nothing selected yet, wish tfjingle first.", Capitalize: false);
				return;
			}
			var pick = JingleCandidates[JingleIndex];
			string path = $"Sounds/Reputation/sfx_reputation_{pick.Key}_negative";
			SoundManager.PreloadClipSet(path);
			The.Player?.PlayWorldSound(path, 1f);
			MessageQueue.AddPlayerMessage($"tfjingleneg: {{{{W|{pick.Key}}}}} negative.", Capitalize: false);
			Helpers.VerifyLog("JINGLE", $"audition {pick.Key} negative");
		}

		/// <summary>Glowstone pair dev door (2026-08-22): a glowsphere to break, a lantern to
		/// load. Everything else about the loop is reachable from these two.</summary>
		[WishCommand(Command = "tflantern")]
		public static void GlowstoneKit()
		{
			GameObject Ply = The.Player;
			if (Ply == null)
				return;
			Ply.ReceiveObject(GameObject.Create("Glowsphere"));
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_GlowstoneLantern"));
			MessageQueue.AddPlayerMessage("tflantern: glowsphere + lantern received. break the sphere, load a shard.", Capitalize: false);
			Helpers.VerifyLog("LANTERN", "tflantern kit granted");
		}

		/// <summary>Grinder cell dev door (2026-08-22): the cell plus a handful of stones to feed it.</summary>
		[WishCommand(Command = "tfgrinder")]
		public static void GrinderKit()
		{
			GameObject Ply = The.Player;
			if (Ply == null)
				return;
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_GrinderCell"));
			for (int i = 0; i < 3; i++)
				Ply.ReceiveObject(GameObject.Create("Small Stone"));
			MessageQueue.AddPlayerMessage("tfgrinder: piezoelectric cell + 3 small stones received. feed it.", Capitalize: false);
			Helpers.VerifyLog("GRINDER", "tfgrinder kit granted");
		}

		/// <summary>Mole cricket dev door (2026-08-22).</summary>
		[WishCommand(Command = "tfcricket")]
		public static void CricketKit()
		{
			GameObject Ply = The.Player;
			if (Ply == null)
				return;
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_MoleCricket"));
			MessageQueue.AddPlayerMessage("tfcricket: mole cricket received. equip it floating nearby; unequip kills it.", Capitalize: false);
			Helpers.VerifyLog("CRICKET", "tfcricket granted");
		}

		/// <summary>Box of chalk dev door (2026-08-22): the box plus a chalkboard to write on.</summary>
		[WishCommand(Command = "tfchalk")]
		public static void ChalkKit()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			if (origin == null)
				return;
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_BoxOfChalk"));
			Cell boardCell = origin.GetCellFromDirection("N") ?? origin;
			boardCell.AddObject(GameObject.Create("Chalkboard"));
			MessageQueue.AddPlayerMessage("tfchalk: box of chalk received, chalkboard placed north. draw and write.", Capitalize: false);
			Helpers.VerifyLog("CHALK", "tfchalk kit granted");
		}
	}
}
