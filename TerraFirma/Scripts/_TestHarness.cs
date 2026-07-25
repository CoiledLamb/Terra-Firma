using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL;
using XRL.Messages;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// TEMPORARY automated verification harness (2026-07-24, user-blessed experiment: "i'm pro
	/// building this but mostly because i'm curious if it will actually work out"). Wish-driven
	/// scenario runner: builds a scenario in the live zone, drives PLAYER-path actions through
	/// the real engine pipeline (PerformThrow fires BeforeThrown internally -- decomp-verified,
	/// GameObject.cs:15333), pumps world turns by eating player actions (the Paralyzed idiom:
	/// BeginTakeActionEvent + PreventAction zeroes energy engine-side, ActionManager.cs:743),
	/// and asserts on ENGINE STATE directly -- not log inference. Results land as
	/// [TF-VERIFY] [AUTO] lines for the watcher plus in-game messages.
	///
	/// Proof-of-concept scenarios (the current batch's riskiest items):
	///   tfstreak  -- streakschorl re-equip re-fix: last-held stone thrown, spares in bag
	///   tfhopper  -- millstone hopper: powered mill grinds, dead mill doesn't (5 turns)
	///   tfscav    -- baboon throw cadence + starvation detector (30 turns; you stand still!)
	///
	/// Rides the same kill switch as the rest of the instrumentation (Helpers.VERIFY_WATCH) and
	/// joins the strip-before-release obligation.
	/// </summary>
	[HasWishCommand]
	public class TestHarnessWishes
	{
		private const string STREAK_BP = "Cleo_TerraFirma_Rock_Hypersonic"; // display name: streakschorl
		private const string HOPPER_STOCK_BP = "Cleo_TerraFirma_Rock_Salt";
		private const string SCAV_AMMO_BP = "Cleo_TerraFirma_Rock_Generic";
		private const string BABOON_BP = "Baboon";
		private const string SPAWN_MARK = "TFHarnessSpawn";
		private const string STRANGE_BP = "Cleo_TerraFirma_Rock_Strange";
		private const string STRANGE_POOL = "Cleo_TerraFirma_StrangeRocksPool";
		private const string STATUETTE_BP = "Cleo_TerraFirma_Rock_BaboonFigurine";
		private const string FIGURINE_ITEM_BP = "Cleo_TerraFirma_BaboonFigurineItem";
		private const string APE_BP = "Cleo_TerraFirma_Rock_ApeStatue";
		private const string RUBBLE_BP = "Cleo_TerraFirma_Rock_Rubble";
		private const string PEBBLES_BP = "Cleo_TerraFirma_Rock_Pebbles";
		private const string HEALSTONE_BP = "Cleo_TerraFirma_Rock_Healstone";
		private const string AEGISCHIST_BP = "Cleo_TerraFirma_Rock_Shieldstone"; // display: aegischist
		private const string DRAINSTONE_BP = "Cleo_TerraFirma_Rock_Drainstone";
		private const string CAROM_BP = "Cleo_TerraFirma_Rock_Rubber";           // display: caromcobble
		private const string CRACK_BP = "Cleo_TerraFirma_Rock_Crack";
		private const string GEODE_BP = "Cleo_TerraFirma_Rock_Soupstone";        // display: brine geode
		private const string GENERIC_BP = "Cleo_TerraFirma_Rock_Generic";
		private const string ROBOT_BP = "Scrapbot";

		[WishCommand(Command = "tfverify")]
		public static void Help()
		{
			MessageQueue.AddPlayerMessage("TF harness -- instant: tfstreak, tfstrange, tfcarom, tfcrack, tfspill, tfpuddle, tfdevour + tfeat (lithovore only). Turn-driven: tfquick (5t), tfhopper (5t), tfform (5t), tfdull (5t), tfshape (Stoneshaper, 3t), tffis (NPC caster, 20t), tfdrain (25t), tfscav (30t, you are the target). tfbatch = everything, ~100 turns standing still.", "W");
			if (!Helpers.VERIFY_WATCH)
				MessageQueue.AddPlayerMessage("VERIFY_WATCH is OFF -- the harness is disabled.", "R");
		}

		/// <summary>The full sweep (round 3 shape): six synchronous scenarios now, then the
		/// turn-driven five chained through the driver's queue (~75 turns standing still).</summary>
		[WishCommand(Command = "tfbatch")]
		public static void Batch()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			Streak();
			Strange();
			QuickSync();
			CaromSync();
			CrackSync();
			SpillSync();
			PuddleSync();
			DevourSync();
			EatSync();
			SetupHopper(The.Player, "form,quickape,dull,drain,shape,fis,scav");
		}

		// ---------------------------------------------------------------- round 3, synchronous

		/// <summary>The quick shelf: baboon pocket counts, figurine rep-on-take, healstone
		/// step-heal + ConsumedOnStep, aegischist cage. Four asserts, one wish, no turns.
		/// (The ape-statue -> rubble half rides the turn-driven 'quickape' scenario.)</summary>
		[WishCommand(Command = "tfquick")]
		public static void Quick()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			QuickSync();
			SetupQuickApe(The.Player, "");
		}

		internal static void QuickSync()
		{
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;

			// pockets: the merge reaches Hulking by inheritance but NOT Quartz (inherits BaseBaboon)
			GameObject hulk = GameObject.Create("Hulking Baboon");
			GameObject quartz = GameObject.Create("Quartz Baboon");
			int hulkRocks = CountModRocks(hulk);
			int quartzRocks = CountModRocks(quartz);
			Report("quick.pockets", hulkRocks >= 1 && quartzRocks == 0,
				$"Hulking Baboon spawns with {hulkRocks} mod rock(s) (want >=1), Quartz Baboon with {quartzRocks} (want 0 -- outside the merge)");
			hulk?.Obliterate();
			quartz?.Obliterate();

			// figurine rep: INTENT is +200 while carried (AddsRep). Suspicion from recon: the part
			// defaults to equipper-only (WorksOnCarrier unset in our XML) -- delta 0 confirms it.
			int repBefore = The.Game.PlayerReputation.Get("Baboons");
			GameObject fig = GameObject.Create(FIGURINE_ITEM_BP);
			origin.AddObject(fig);
			ply.TakeObject(fig, NoStack: false, Silent: true, EnergyCost: 0);
			int repAfter = The.Game.PlayerReputation.Get("Baboons");
			fig.Obliterate();
			int repFinal = The.Game.PlayerReputation.Get("Baboons");
			int delta = repAfter - repBefore;
			Report("quick.rep", delta == 200,
				$"figurine take: Baboons rep {repBefore}->{repAfter} (delta {delta}, INTENT +200 carried), after removal {repFinal}" +
				(delta == 0 ? " -- AddsRep never applied: the WorksOnCarrier suspicion is CONFIRMED (part is equipper-only without it)" : ""));

			// healstone via the step path (no throw-damage confound): wounded patient forced over
			// the stone until the 25% proc lands; heal must raise HP and ConsumedOnStep must eat the stone
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count >= 2)
			{
				GameObject patient = GameObject.Create(BABOON_BP);
				patient.SetIntProperty(SPAWN_MARK, 1);
				patient.GetStat("Hitpoints").BaseValue = 60;
				patient.GetStat("Hitpoints").Penalty += 25;
				open[0].AddObject(patient);
				GameObject healstone = GameObject.Create(HEALSTONE_BP);
				open[1].AddObject(healstone);
				int hpBefore = patient.GetStat("Hitpoints").Value;
				int entries = ForceEntries(patient, open[0], open[1], 30, () => !GameObject.Validate(healstone));
				int hpAfter = GameObject.Validate(patient) ? patient.GetStat("Hitpoints").Value : -1;
				bool consumed = !GameObject.Validate(healstone);
				Report("quick.heal", consumed && hpAfter > hpBefore,
					$"step-proc after {entries} crossing(s): HP {hpBefore}->{hpAfter} (want risen), stone {(consumed ? "consumed (ConsumedOnStep)" : "STILL THERE")}");
				if (GameObject.Validate(healstone)) healstone.Obliterate();
				if (GameObject.Validate(patient)) patient.Obliterate();
			}
			else
				Report("quick.heal", false, "SETUP FAIL - need two open adjacent cells");

			// aegischist cage: thrown hit rings the victim in forcefield. Round-3 run rang 3/8 at a
			// cramped lane-end -- ambiguous between mod behavior and harness terrain; now the target
			// stands in the open (>=7 open sides) and blocked sides are reported per-cell.
			Cell spot = FindLaneOpenEnd(origin, 3, 7);
			if (spot != null)
			{
				GameObject victim = GameObject.Create(BABOON_BP);
				victim.SetIntProperty(SPAWN_MARK, 1);
				victim.GetStat("Hitpoints").BaseValue = 100;
				spot.AddObject(victim);
				// up to 4 throws: even at perfect accuracy the engine keeps an agility-based
				// connect roll (harness find, third distinct miss mode 2026-07-24 -- the lend
				// kills scatter, not the connect); a whiff drops its ring at the landing cell
				// and expires on its own in 12 rounds
				int ring = 0;
				int blocked = 0;
				int attempts = 0;
				List<GameObject> segments = new List<GameObject>();
				for (int attempt = 1; attempt <= 4 && GameObject.Validate(victim); attempt++)
				{
					attempts = attempt;
					ThrowAt(ply, AEGISCHIST_BP, victim);
					ring = 0;
					blocked = 0;
					segments.Clear();
					Cell vc = victim.CurrentCell;
					if (vc != null)
						foreach (Cell adj in vc.GetLocalAdjacentCells())
						{
							bool found = false;
							foreach (GameObject obj in adj.Objects)
								if (obj.HasPart("Forcefield")) { ring++; segments.Add(obj); found = true; }
							if (!found && (adj.IsSolid() || adj.HasObjectWithPart("Combat")))
								blocked++;
						}
					if (ring >= 7)
						break;
				}
				Report("quick.cage", GameObject.Validate(victim) && ring >= 7,
					$"{ring} forcefield segment(s) ring the victim after {attempts} throw(s), {blocked} side(s) blocked (want >=7 of 8; whiff rings expire on their own)");
				foreach (GameObject seg in segments) seg.Obliterate();
				if (GameObject.Validate(victim)) victim.Obliterate();
			}
			else
				Report("quick.cage", false, "SETUP FAIL - no clear lane ending in an open-surrounded cell");
		}

		/// <summary>Caromcobble trip statistics: 40 forced crossings of the peel in one wish
		/// (ObjectEnteredCellEvent fires synchronously per entry). StepChance 25 -> ~10 expected
		/// trips; accept 3-22 (binomial tails under 0.05%). HoldChance 100 -> the peel NEVER
		/// breaks. Whenever the StepChance re-ruling lands, re-run against the new band.</summary>
		[WishCommand(Command = "tfcarom")]
		public static void CaromSync()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 2) { Report("carom", false, "SETUP FAIL - need two open adjacent cells"); return; }
			GameObject rock = GameObject.Create(CAROM_BP);
			rock.SetIntProperty(SPAWN_MARK, 1);
			open[0].AddObject(rock);
			GameObject victim = GameObject.Create(BABOON_BP);
			victim.SetIntProperty(SPAWN_MARK, 1);
			victim.GetStat("Hitpoints").BaseValue = 100;
			open[1].AddObject(victim);
			int procs = 0;
			for (int i = 0; i < 40 && GameObject.Validate(victim); i++)
			{
				victim.DirectMoveTo(open[0], 0, Forced: true, IgnoreCombat: true);
				if (victim.HasEffect(typeof(XRL.World.Effects.Prone)))
				{
					procs++;
					victim.RemoveEffect<XRL.World.Effects.Prone>();
				}
				victim.DirectMoveTo(open[1], 0, Forced: true, IgnoreCombat: true);
			}
			bool alive = GameObject.Validate(rock) && rock.CurrentCell == open[0];
			Report("carom", alive && procs >= 3 && procs <= 22,
				$"40 forced crossings: {procs} trip(s) (StepChance 25 -> expect ~10, accept 3-22), peel {(alive ? "intact through every proc (HoldChance 100)" : "BROKE OR MOVED")}");
			if (GameObject.Validate(rock)) rock.Obliterate();
			if (GameObject.Validate(victim)) victim.Obliterate();
		}

		/// <summary>Crack pull-down: force a victim over the cracked floor until the 15% proc
		/// drops it to the zone below (60 crossings: miss odds ~0.006%).</summary>
		[WishCommand(Command = "tfcrack")]
		public static void CrackSync()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 2) { Report("crack", false, "SETUP FAIL - need two open adjacent cells"); return; }
			GameObject crack = GameObject.Create(CRACK_BP);
			crack.SetIntProperty(SPAWN_MARK, 1);
			open[0].AddObject(crack);
			GameObject victim = GameObject.Create(BABOON_BP);
			victim.SetIntProperty(SPAWN_MARK, 1);
			victim.GetStat("Hitpoints").BaseValue = 100; // survives the landing
			open[1].AddObject(victim);
			Zone startZone = victim.CurrentZone;
			int n = 0;
			bool fell = false;
			for (; n < 60 && GameObject.Validate(victim); n++)
			{
				victim.DirectMoveTo(open[0], 0, Forced: true, IgnoreCombat: true);
				if (victim.CurrentZone != startZone) { fell = true; break; }
				victim.DirectMoveTo(open[1], 0, Forced: true, IgnoreCombat: true);
			}
			Report("crack", fell, fell
				? $"victim fell through to {victim.CurrentZone?.ZoneID ?? "the zone below"} after {n + 1} crossing(s) (StepChance 15)"
				: $"victim NEVER fell in 60 crossings (miss odds ~0.006% -- the FallThrough payload looks dead)");
			if (GameObject.Validate(victim)) victim.Obliterate();
			if (GameObject.Validate(crack)) crack.Obliterate();
		}

		/// <summary>Liquid-rock destruction spill (RULED 2026-07-24): a brine geode killed by
		/// incidental damage releases its pool -- assert one puddle, 4-32 drams (the 4d8 audit).</summary>
		[WishCommand(Command = "tfspill")]
		public static void SpillSync()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 1) { Report("spill", false, "SETUP FAIL - no open adjacent cell"); return; }
			GameObject geode = GameObject.Create(GEODE_BP);
			open[0].AddObject(geode);
			geode.Destroy(null, Silent: true); // destruction, not obliteration: the spill path
			GameObject puddle = null;
			int drams = 0;
			foreach (GameObject obj in open[0].Objects)
				if (obj.LiquidVolume != null) { puddle = obj; drams = obj.LiquidVolume.Volume; break; }
			Report("spill", puddle != null && drams >= 4 && drams <= 32,
				$"destruction spilled {(puddle == null ? "NO puddle" : drams + " dram(s)")} (want 4-32 per the 4d8 pool-volume audit)");
			if (puddle != null && GameObject.Validate(puddle)) puddle.Obliterate();
		}

		// ---------------------------------------------------------------- round 4, synchronous

		/// <summary>rock-onepuddle: a liquid rock that BREAKS on the use-path must leave exactly
		/// ONE puddle (the hit-payload's), the death-spill suppressed against doubles. Retries
		/// until a throw both connects and breaks; pre-cleans between attempts.</summary>
		[WishCommand(Command = "tfpuddle")]
		public static void PuddleSync()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			Cell lane = FindLane(origin, 3);
			if (lane == null) { Report("puddle", false, "SETUP FAIL - no open lane"); return; }
			GameObject victim = GameObject.Create(BABOON_BP);
			victim.SetIntProperty(SPAWN_MARK, 1);
			victim.GetStat("Hitpoints").BaseValue = 150;
			lane.AddObject(victim);
			bool scored = false;
			int puddles = -1;
			int attempts = 0;
			for (int i = 1; i <= 6 && GameObject.Validate(victim); i++)
			{
				attempts = i;
				SweepPuddlesAround(victim); // pre-clean: prior attempts' splashes
				GameObject stone = ThrowAt(ply, GEODE_BP, victim);
				if (stone == null)
					break;
				if (!GameObject.Validate(stone))
				{
					puddles = CountPuddlesAround(victim);
					if (puddles == 0)
						continue; // the breaking throw MISSED (splash landed elsewhere) -- doesn't test the suppression; retry
					scored = true;
					break;
				}
				stone.Obliterate(); // survived (25%): clear it and retry
			}
			Report("puddle", scored && puddles == 1, scored
				? $"use-path break scored on throw {attempts}: {puddles} puddle object(s) at the impact (want exactly 1 -- death-spill suppressed against doubles)"
				: $"no scoring break in {attempts} throw(s) (break odds 75%/throw; misses excluded) -- INCONCLUSIVE, re-run");
			SweepPuddlesAround(victim);
			if (GameObject.Validate(victim)) victim.Obliterate();
		}

		private static int CountPuddlesAround(GameObject who)
		{
			int n = 0;
			Cell c = who?.CurrentCell;
			if (c == null)
				return 0;
			foreach (GameObject obj in c.Objects)
				if (obj.LiquidVolume != null) n++;
			foreach (Cell adj in c.GetLocalAdjacentCells())
				foreach (GameObject obj in adj.Objects)
					if (obj.LiquidVolume != null) n++;
			return n;
		}

		private static void SweepPuddlesAround(GameObject who)
		{
			Cell c = who?.CurrentCell;
			if (c == null)
				return;
			List<GameObject> doomed = new List<GameObject>();
			foreach (GameObject obj in c.Objects)
				if (obj.LiquidVolume != null) doomed.Add(obj);
			foreach (Cell adj in c.GetLocalAdjacentCells())
				foreach (GameObject obj in adj.Objects)
					if (obj.LiquidVolume != null) doomed.Add(obj);
			foreach (GameObject obj in doomed)
				obj.Obliterate();
		}

		/// <summary>lith-devour: the universal statue-devourability audit's descent detection,
		/// eaten live — a famished lithovore devours an EaterStatue (base Statue family, no
		/// Statue part, exactly the blueprint-descent case). Requires a lithovore character;
		/// hunger is forced to famished for the bite and restored after.</summary>
		[WishCommand(Command = "tfdevour")]
		public static void DevourSync()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			if (ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_Lithovore>() == null)
			{
				Report("devour", false, "SETUP SKIP - needs a lithovore character (the defect part is absent)");
				return;
			}
			Stomach stomach = ply.GetPart<Stomach>();
			if (stomach == null) { Report("devour", false, "SETUP FAIL - no Stomach part"); return; }
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 1) { Report("devour", false, "SETUP FAIL - no open adjacent cell"); return; }
			GameObject statue = GameObject.Create("EaterStatue");
			statue.SetIntProperty(SPAWN_MARK, 1);
			open[0].AddObject(statue);
			int hungerBefore = stomach.HungerLevel;
			stomach.HungerLevel = 2; // famished: the devour gate
			bool checkResult;
			bool suppressed = Popup.Suppress;
			Popup.Suppress = true;
			try { checkResult = InventoryActionEvent.Check(ply, ply, statue, "Cleo_TerraFirma_DevourStatue"); }
			finally { Popup.Suppress = suppressed; }
			bool gone = !GameObject.Validate(statue);
			stomach.HungerLevel = hungerBefore; // the devour cleared it; the test should not feed you
			Report("devour", gone,
				$"famished devour of an EaterStatue (base Statue family, blueprint-descent detection): action check={checkResult}, statue {(gone ? "devoured whole" : "STILL STANDS")}; hunger restored to pre-test");
			if (!gone && GameObject.Validate(statue))
				statue.Obliterate();
		}

		[WishCommand(Command = "tfshape")]
		public static void Shape()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			SetupShape(The.Player, "");
		}

		/// <summary>Stoneshaper core, driven through the extracted CarveFrom seam (the UI pickers
		/// stay human): carve #1 asserts statue + CurStatue tracking; a Young Ivory attempt
		/// asserts the clean no-body fail (no cooldown, wall intact); carve #2 asserts the
		/// swap-down commit; then the statue is destroyed and a 3-turn watch asserts the
		/// cooldown only TICKS -- no reset, no zero-out (the shape-cd retirement watch).</summary>
		internal static void SetupShape(GameObject ply, string queue)
		{
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper shaper = ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper>();
			if (shaper == null) { Report("shape", false, "SETUP SKIP - needs a Stoneshaper (rockwineverytime character)"); RunNext(queue, ply); return; }
			List<Cell> open = new List<Cell>(origin.GetLocalAdjacentCells(2));
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 5) { Report("shape", false, "SETUP FAIL - need five open cells within 2"); RunNext(queue, ply); return; }

			// wall 1 + muse A -> carve #1
			GameObject wall1 = GameObject.Create("Sandstone");
			open[0].AddObject(wall1);
			if (wall1.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>() == null)
			{
				Report("shape", false, "SETUP FAIL - spawned Sandstone lacks StoneshaperWallProperties (merge gap?)");
				wall1.Obliterate();
				RunNext(queue, ply);
				return;
			}
			GameObject museA = GameObject.Create(BABOON_BP);
			museA.SetIntProperty(SPAWN_MARK, 1);
			open[1].AddObject(museA);
			GameObject statueA;
			bool suppressed = Popup.Suppress;
			Popup.Suppress = true;
			try { statueA = shaper.CarveFrom(ply, museA, open[0]); }
			finally { Popup.Suppress = suppressed; }
			bool carvedA = statueA != null && GameObject.Validate(statueA) && shaper.CurStatue == statueA;
			if (statueA != null) statueA.SetIntProperty(SPAWN_MARK, 1);
			Report("shape.carve", carvedA, $"carve #1 (Baboon muse, spawned Sandstone wall): statue {(carvedA ? "stands and is tracked as CurStatue" : "FAILED")}");
			ActivatedAbilityEntry entry = ply.ActivatedAbilities?.GetAbility(shaper.ActivatedAbilityID);
			int cdAfterCarve = entry?.Cooldown ?? -1;

			// Young Ivory -> clean no-body fail: null statue, wall untouched, cooldown not re-taken
			GameObject wall2 = GameObject.Create("Sandstone");
			open[2].AddObject(wall2);
			GameObject ivory = GameObject.Create("Young Ivory");
			ivory.SetIntProperty(SPAWN_MARK, 1);
			open[3].AddObject(ivory);
			GameObject statueNull;
			suppressed = Popup.Suppress;
			Popup.Suppress = true;
			try { statueNull = shaper.CarveFrom(ply, ivory, open[2]); }
			finally { Popup.Suppress = suppressed; }
			bool wallIntact = GameObject.Validate(wall2) && wall2.CurrentCell == open[2];
			int cdAfterIvory = entry?.Cooldown ?? -1;
			Report("shape.ivory", statueNull == null && wallIntact && shaper.CurStatue == statueA && cdAfterIvory <= cdAfterCarve,
				$"Young Ivory attempt: statue={(statueNull == null ? "refused (no body plan)" : "CARVED?!")}, wall {(wallIntact ? "intact" : "SPENT")}, cooldown {cdAfterCarve}->{cdAfterIvory} (must not re-take)");

			// muse B (robot anatomy) -> swap-down commit
			GameObject museB = GameObject.Create(ROBOT_BP);
			museB.SetIntProperty(SPAWN_MARK, 1);
			open[4].AddObject(museB);
			GameObject statueB;
			suppressed = Popup.Suppress;
			Popup.Suppress = true;
			try { statueB = shaper.CarveFrom(ply, museB, open[2]); }
			finally { Popup.Suppress = suppressed; }
			bool swapped = statueB != null && GameObject.Validate(statueB) && shaper.CurStatue == statueB && statueB != statueA;
			if (statueB != null) statueB.SetIntProperty(SPAWN_MARK, 1);
			Report("shape.swap", swapped, $"carve #2 (Scrapbot muse): {(swapped ? "swap-down committed -- new statue tracked, prior returned to stone (gear entombed)" : "SWAP FAILED")}");

			if (!swapped || entry == null)
			{
				RunNext(queue, ply);
				return;
			}
			// the shape-cd watch: destroy the statue, then 3 turns -- the cooldown may only TICK
			int cdBeforeKill = entry.Cooldown;
			statueB.Destroy(null, Silent: true);
			Cleo_TerraFirma_TestDriver driver = new Cleo_TerraFirma_TestDriver { Scenario = "shape", TurnsRemaining = 3, TurnsTotal = 3, Queue = queue };
			driver.InitShape(entry, cdBeforeKill);
			ply.AddPart(driver);
			MessageQueue.AddPlayerMessage("Harness: shape cooldown-watch running -- 3 turns after the statue's destruction.", "W");
		}

		// ---------------------------------------------------------------- round 5

		/// <summary>The eaten-channel sweep (lithovore only): one representative rock per cheap
		/// channel, eaten via the mutation's own command, with per-channel state asserts where
		/// the state is readable and consume-only where it isn't. Deliberately SKIPPED: the
		/// EatenDetonate pair (a live grenade in your gut), the fungal twins (a real infection
		/// on the player), mullstone (needs a staged cooling ability), and the deep four
		/// (bite-back + full meal, live-verified in the kitchen round). Everything the test
		/// does to the player is restored: hunger, HP, position, temperature normalizes.</summary>
		[WishCommand(Command = "tfeat")]
		public static void EatSync()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			if (ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_Lithovore>() == null)
			{
				Report("eat", false, "SETUP SKIP - needs a lithovore character");
				return;
			}
			Stomach stomach = ply.GetPart<Stomach>();
			if (stomach == null) { Report("eat", false, "SETUP FAIL - no Stomach part"); return; }
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			GameObject neighbor = null;
			if (open.Count > 0)
			{
				neighbor = GameObject.Create(BABOON_BP); // the thunderclap's ear
				neighbor.SetIntProperty(SPAWN_MARK, 1);
				neighbor.GetStat("Hitpoints").BaseValue = 100;
				open[0].AddObject(neighbor);
			}

			int hungerBefore = stomach.HungerLevel;
			int cookBefore = stomach.CookingCounter;
			int penaltyBefore = ply.GetStat("Hitpoints").Penalty;
			Cell home = origin;

			string[,] rows = {
				{ GEODE_BP, "water", "brine geode" },
				{ "Cleo_TerraFirma_Rock_Salt", "consume", "chunk of salt (tasty)" },
				{ "Cleo_TerraFirma_Rock_Pointy", "bleed", "pointy rock" },
				{ "Cleo_TerraFirma_Rock_ShatteredStalactite", "bleed", "shattered stalactite" },
				{ "Cleo_TerraFirma_Rock_CompressedAsh", "harm", "compressed ash (choke)" },
				{ "Cleo_TerraFirma_Rock_Melting", "harm", "sludgabbro-acid (melting)" },
				{ CAROM_BP, "moved", "caromcobble (hop)" },
				{ "Cleo_TerraFirma_Rock_Shunting", "moved", "farflint (teleport)" },
				{ STREAK_BP, "daze", "streakschorl (thunderclap)" },
				{ "Cleo_TerraFirma_Rock_Sludgestone", "spawnliquid", "sludgestone" },
				{ "Cleo_TerraFirma_Rock_Spiderweb", "webs", "gossamarl (3 webs)" },
				{ "Cleo_TerraFirma_Rock_Whorlstone", "belly", "vertigneiss (gas belly)" },
				{ "Cleo_TerraFirma_Rock_Scorching", "hot", "pyroclase" },
				{ "Cleo_TerraFirma_Rock_Icy", "cold", "rimeholm" },
				{ "Cleo_TerraFirma_Rock_Voltaic", "consume", "galvena (self-arc/crackle)" },
				{ "Cleo_TerraFirma_Rock_Spiny", "consume", "urchinite (quill burst)" },
				{ STRANGE_BP, "consume", "strange rock (mimic eat)" },
			};

			int passCount = 0;
			List<string> fails = new List<string>();
			for (int i = 0; i < rows.GetLength(0); i++)
			{
				string bp = rows[i, 0];
				string kind = rows[i, 1];
				string label = rows[i, 2];
				GameObject rock = GameObject.Create(bp);
				if (rock == null) { fails.Add(label + " (no blueprint)"); continue; }
				ply.ReceiveObject(rock);
				int hpBefore = ply.GetStat("Hitpoints").Value;
				int waterBefore = stomach.Water;
				int tempBefore = ply.Physics?.Temperature ?? 25;
				Cell cellBefore = ply.CurrentCell;
				int liquidsBefore = CountPuddlesAround(ply);
				int websBefore = CountAround(ply, "Web");
				bool suppressed = Popup.Suppress;
				Popup.Suppress = true;
				try { InventoryActionEvent.Check(ply, ply, rock, "Cleo_TerraFirma_EatStone"); }
				finally { Popup.Suppress = suppressed; }
				bool consumed = !GameObject.Validate(rock) || rock.InInventory != ply;
				bool pass;
				string detail;
				switch (kind)
				{
					case "water":
						pass = consumed && stomach.Water > waterBefore;
						detail = $"water {waterBefore}->{stomach.Water}";
						break;
					case "bleed":
						pass = consumed && ply.HasEffect(typeof(XRL.World.Effects.Bleeding));
						detail = "internal bleeding " + (pass ? "applied" : "MISSING");
						ply.RemoveEffect<XRL.World.Effects.Bleeding>();
						break;
					case "harm":
						pass = consumed && ply.GetStat("Hitpoints").Value < hpBefore;
						detail = $"HP {hpBefore}->{ply.GetStat("Hitpoints").Value}";
						break;
					case "moved":
						pass = consumed && ply.CurrentCell != cellBefore;
						detail = pass ? "displaced" : "did not move";
						if (ply.CurrentCell != home)
							ply.DirectMoveTo(home, 0, Forced: true, IgnoreCombat: true);
						break;
					case "daze":
						pass = consumed && neighbor != null && GameObject.Validate(neighbor) && neighbor.HasEffect(typeof(XRL.World.Effects.Dazed)) && !ply.HasEffect(typeof(XRL.World.Effects.Dazed));
						detail = "neighbor " + (pass ? "dazed, eater spared" : "NOT dazed (or eater dazed)");
						if (neighbor != null && GameObject.Validate(neighbor))
							neighbor.RemoveEffect<XRL.World.Effects.Dazed>();
						break;
					case "spawnliquid":
						pass = consumed && CountPuddlesAround(ply) > liquidsBefore;
						detail = "puddle " + (pass ? "spawned" : "MISSING");
						SweepPuddlesAround(ply);
						break;
					case "webs":
						int websNow = CountAround(ply, "Web");
						pass = consumed && websNow - websBefore >= 2;
						detail = $"webs +{websNow - websBefore} (want >=2 of 3; own-cell placement varies)";
						SweepAround(ply, "Web");
						break;
					case "belly":
						pass = consumed && ply.HasEffect(typeof(XRL.World.Effects.Cleo_TerraFirma_GasBelly));
						detail = "gas belly " + (pass ? "settled in" : "MISSING");
						ply.RemoveEffect<XRL.World.Effects.Cleo_TerraFirma_GasBelly>();
						break;
					case "hot":
						pass = consumed && (ply.Physics?.Temperature ?? 25) > tempBefore;
						detail = $"temperature {tempBefore}->{ply.Physics?.Temperature}";
						break;
					case "cold":
						pass = consumed && (ply.Physics?.Temperature ?? 25) < tempBefore;
						detail = $"temperature {tempBefore}->{ply.Physics?.Temperature}";
						break;
					default: // consume
						pass = consumed;
						detail = consumed ? "consumed (effect is log/eyeball-grade)" : "NOT consumed";
						break;
				}
				Helpers.VerifyLog("AUTO", $"eat.{label}: {(pass ? "PASS" : "FAIL")} - {detail}");
				if (pass) passCount++;
				else fails.Add(label);
				if (GameObject.Validate(rock) && rock.InInventory == ply)
					rock.Obliterate(); // a refused eat should not litter the pack
			}

			// the test must not feed, hurt, or relocate you on net
			stomach.HungerLevel = hungerBefore;
			stomach.CookingCounter = cookBefore;
			ply.GetStat("Hitpoints").Penalty = penaltyBefore;
			if (ply.CurrentCell != home)
				ply.DirectMoveTo(home, 0, Forced: true, IgnoreCombat: true);
			if (neighbor != null && GameObject.Validate(neighbor))
				neighbor.Obliterate();
			Report("eat", fails.Count == 0,
				$"{passCount}/{rows.GetLength(0)} eaten channels asserted{(fails.Count > 0 ? "; FAILS: " + string.Join(", ", fails) : "")}; skipped by design: detonate pair, fungal twins, mullstone, deep four. Player state restored.");
		}

		private static int CountAround(GameObject who, string blueprint)
		{
			int n = 0;
			Cell c = who?.CurrentCell;
			if (c == null)
				return 0;
			foreach (GameObject obj in c.Objects)
				if (obj.Blueprint == blueprint) n++;
			foreach (Cell adj in c.GetLocalAdjacentCells())
				foreach (GameObject obj in adj.Objects)
					if (obj.Blueprint == blueprint) n++;
			return n;
		}

		private static void SweepAround(GameObject who, string blueprint)
		{
			Cell c = who?.CurrentCell;
			if (c == null)
				return;
			List<GameObject> doomed = new List<GameObject>();
			foreach (GameObject obj in c.Objects)
				if (obj.Blueprint == blueprint) doomed.Add(obj);
			foreach (Cell adj in c.GetLocalAdjacentCells())
				foreach (GameObject obj in adj.Objects)
					if (obj.Blueprint == blueprint) doomed.Add(obj);
			foreach (GameObject obj in doomed)
				obj.Obliterate();
		}

		[WishCommand(Command = "tffis")]
		public static void Fis()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			SetupFis(The.Player, "");
		}

		/// <summary>fis-npc: the first NPC-caster scenario — a Hulking Baboon granted Fissure L5,
		/// hostile-locked, 20 turns to cast. PASS = the zone gained loose fissure stones (a cast
		/// happened); living-fall allegiance is REPORTED (seam-dependent). A 0-cast result is
		/// itself the fis-npc answer: no AI advertisement.</summary>
		internal static void SetupFis(GameObject ply, string queue)
		{
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> ring = origin.GetLocalAdjacentCells(6);
			ring.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat") || c.DistanceTo(origin) < 4);
			if (ring.Count == 0) { Report("fis", false, "SETUP FAIL - no open cell at distance 4-6"); RunNext(queue, ply); return; }
			GameObject caster = GameObject.Create("Hulking Baboon");
			caster.SetIntProperty(SPAWN_MARK, 1);
			caster.GetStat("Hitpoints").BaseValue = 150;
			Mutations mutations = caster.RequirePart<Mutations>();
			mutations.AddMutation(new XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure(), 5);
			ring[0].AddObject(caster);
			if (caster.Brain != null)
				caster.Brain.Target = ply;
			Cleo_TerraFirma_TestDriver driver = new Cleo_TerraFirma_TestDriver { Scenario = "fis", TurnsRemaining = 20, TurnsTotal = 20, Queue = queue };
			driver.InitFis(caster, CountLooseFissureRocks(ply.CurrentZone));
			ply.AddPart(driver);
			MessageQueue.AddPlayerMessage("Harness: NPC Fissure caster running -- 20 turns; expect falling rocks.", "W");
		}

		internal static int CountLooseFissureRocks(Zone zone)
		{
			int n = 0;
			if (zone != null)
				foreach (GameObject obj in zone.GetObjects())
					if (obj.GetPart<Cleo_TerraFirma_FallenRockPart>() != null && obj.CurrentCell != null && obj.InInventory == null && obj.Equipped == null)
						n += obj.Count;
			return n;
		}

		// ---------------------------------------------------------------- round 3, turn-driven setups

		internal static void SetupQuickApe(GameObject ply, string queue)
		{
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 1) { Report("quick.ape", false, "SETUP FAIL - no open adjacent cell"); RunNext(queue, ply); return; }
			GameObject ape = GameObject.Create(APE_BP);
			Cleo_TerraFirma_TimedForm timed = ape?.GetPart<Cleo_TerraFirma_TimedForm>();
			if (timed == null) { Report("quick.ape", false, "SETUP FAIL - ape statue spawn or TimedForm missing"); RunNext(queue, ply); return; }
			ape.SetIntProperty(SPAWN_MARK, 1);
			timed.TurnsLeft = 2;
			open[0].AddObject(ape);
			Cleo_TerraFirma_TestDriver driver = new Cleo_TerraFirma_TestDriver { Scenario = "quickape", TurnsRemaining = 5, TurnsTotal = 5, Queue = queue };
			driver.InitQuickApe(ape, CountInZone(ply.CurrentZone, RUBBLE_BP));
			ply.AddPart(driver);
			MessageQueue.AddPlayerMessage("Harness: ape-statue expiry running -- 5 turns.", "W");
		}

		[WishCommand(Command = "tfdull")]
		public static void Dull()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			SetupDull(The.Player, "");
		}

		/// <summary>The anti-hoarding clock, never before verified end-to-end (nobody carries a
		/// rock 1000 turns on purpose): wind DullAfter to 2, carry it, assert crumble-to-pebbles.</summary>
		internal static void SetupDull(GameObject ply, string queue)
		{
			if (ply?.CurrentCell == null)
				return;
			GameObject rock = GameObject.Create(GENERIC_BP);
			Cleo_TerraFirma_FallenRockPart frp = rock?.GetPart<Cleo_TerraFirma_FallenRockPart>();
			if (frp == null) { Report("dull", false, "SETUP FAIL - generic rock or FallenRockPart missing"); RunNext(queue, ply); return; }
			frp.DullAfter = 2;
			frp.DullAfterMax = 2; // the expiry roll (first carried turn) collapses to exactly 2
			ply.ReceiveObject(rock);
			Cleo_TerraFirma_TestDriver driver = new Cleo_TerraFirma_TestDriver { Scenario = "dull", TurnsRemaining = 5, TurnsTotal = 5, Queue = queue };
			driver.InitDull(rock, CountInBag(ply, PEBBLES_BP));
			ply.AddPart(driver);
			MessageQueue.AddPlayerMessage("Harness: dull-clock scenario running -- 5 turns carrying a doomed rock.", "W");
		}

		[WishCommand(Command = "tfdrain")]
		public static void Drain()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			SetupDrain(The.Player, "");
		}

		/// <summary>The drainstone lifecycle: player-path latch on flesh (LifeDrain applied),
		/// starved attach on a robot (no leech), the one-shot corpse-can't-latch path, then 25
		/// turns for the two endings -- gorged burst vs dry crumble.</summary>
		internal static void SetupDrain(GameObject ply, string queue)
		{
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			Cell laneA = FindLane(origin, 3);
			if (laneA == null) { Report("drain", false, "SETUP FAIL - no open lane"); RunNext(queue, ply); return; }

			// flesh: latch + LifeDrain, buffed to survive the full 15 rounds for the gorged burst
			GameObject fleshy = GameObject.Create(BABOON_BP);
			fleshy.SetIntProperty(SPAWN_MARK, 1);
			fleshy.GetStat("Hitpoints").BaseValue = 100;
			laneA.AddObject(fleshy);
			GameObject stoneA = ThrowAt(ply, DRAINSTONE_BP, fleshy);
			bool latchedA = GameObject.Validate(stoneA) && stoneA.Equipped == fleshy;
			bool draining = GameObject.Validate(fleshy) && fleshy.HasEffect(typeof(XRL.World.Effects.LifeDrain));
			Report("drain.latch", latchedA && draining,
				$"thrown latch on flesh: stone {(latchedA ? "riding the victim" : "NOT attached")}, LifeDrain {(draining ? "applied" : "MISSING")}");

			// robot: attach-but-no-leech (IsOrganic gate)
			GameObject robot = GameObject.Create(ROBOT_BP);
			robot.SetIntProperty(SPAWN_MARK, 1);
			robot.GetStat("Hitpoints").BaseValue = 100;
			Cell laneB = FindLane(origin, 2) ?? laneA.GetLocalAdjacentCells().Find(c => !c.IsSolid() && !c.HasObjectWithPart("Combat"));
			if (laneB == null) { Report("drain.robot", false, "SETUP FAIL - nowhere to stand the robot"); }
			else
			{
				laneB.AddObject(robot);
				GameObject stoneR = ThrowAt(ply, DRAINSTONE_BP, robot);
				bool latchedR = GameObject.Validate(stoneR) && stoneR.Equipped == robot;
				bool dryR = GameObject.Validate(robot) && !robot.HasEffect(typeof(XRL.World.Effects.LifeDrain));
				Report("drain.robot", latchedR && dryR,
					$"thrown latch on a {ROBOT_BP}: stone {(latchedR ? "attached" : "NOT attached")}, LifeDrain {(dryR ? "correctly withheld (inorganic)" : "APPLIED TO A ROBOT")}");
				if (latchedA)
				{
					Cleo_TerraFirma_TestDriver driver = new Cleo_TerraFirma_TestDriver { Scenario = "drain", TurnsRemaining = 25, TurnsTotal = 25, Queue = queue };
					driver.InitDrain(fleshy, stoneA, robot, stoneR,
						GameObject.Validate(fleshy) ? fleshy.GetStat("Hitpoints").Value : 0,
						GameObject.Validate(robot) ? robot.GetStat("Hitpoints").Value : 0);
					ply.AddPart(driver);
					MessageQueue.AddPlayerMessage("Harness: drain endings running -- 25 turns while two stones ride out their latches.", "W");
					// one-shot check rides the same setup, before the driver turns start
				}
			}

			// one-shot: a 1-HP victim dies to the throw itself; a corpse can't be latched
			Cell laneC = FindLane(origin, 3);
			if (laneC != null)
			{
				GameObject frail = GameObject.Create(BABOON_BP);
				frail.SetIntProperty(SPAWN_MARK, 1);
				frail.GetStat("Hitpoints").BaseValue = 1;
				laneC.AddObject(frail);
				GameObject stoneB = ThrowAt(ply, DRAINSTONE_BP, frail);
				bool dead = !GameObject.Validate(frail) || frail.GetStat("Hitpoints").Value <= 0;
				bool loose = GameObject.Validate(stoneB) && stoneB.Equipped == null && stoneB.InInventory == null;
				Report("drain.oneshot", dead && loose,
					$"1-HP victim: {(dead ? "killed by the throw" : "SURVIVED (assert void)")}, stone {(loose ? "loose as loot, unlatched" : "NOT loose")}");
				if (GameObject.Validate(stoneB)) stoneB.Obliterate();
			}

			if (!latchedA)
				RunNext(queue, ply);
		}

		/// <summary>Slot-normalized player-path throw: force-equip a fresh stone and PerformThrow
		/// it at the victim. Returns the stone (often spent/broken afterwards). Logs a diagnostic
		/// line either way -- the round-3 drain anomalies (guard fail on a live robot; a batch
		/// throw with no dispatch at all) were unreadable without perform-result + victim HP delta
		/// + where the stone ended up.</summary>
		internal static GameObject ThrowAt(GameObject ply, string blueprint, GameObject victim)
		{
			BodyPart slot = ply.GetFirstBodyPart("Thrown Weapon");
			if (slot == null || victim?.CurrentCell == null)
				return null;
			if (slot.Equipped != null)
				slot.ForceUnequip(Silent: true);
			GameObject stone = GameObject.Create(blueprint);
			if (stone == null)
				return null;
			ply.ForceEquipObject(stone, slot, Silent: true);
			if (slot.Equipped != stone)
				return null;
			int hpBefore = victim.GetStat("Hitpoints")?.Value ?? -1;
			// harness bug #5 (2026-07-24 night run): asserts assumed guaranteed hits, but only
			// streakschorl is NeverMiss -- the catalog throws honestly (AimVariance + the engine's
			// fall-one-short), and the verify marathons never saw a miss only because of the
			// playtest god-arm. Deterministic hits via the mod's own lend idiom: stamp the
			// close-range accuracy property for this throw (zeroes AimVariance AND skips the
			// fall-short branch, GameObject.cs:15389), return it after.
			bool lent = false;
			if (!ply.HasIntProperty("CloseThrowRangeAccuracyBonus") && !ply.HasIntProperty("CloseThrowRangeAccuracySkillBonus"))
			{
				ply.ModIntProperty("CloseThrowRangeAccuracyBonus", 100);
				lent = true;
			}
			bool performed;
			bool suppressed = Popup.Suppress;
			Popup.Suppress = true;
			try { performed = ply.PerformThrow(stone, victim.CurrentCell, victim); }
			finally
			{
				Popup.Suppress = suppressed;
				if (lent)
					ply.ModIntProperty("CloseThrowRangeAccuracyBonus", -100, RemoveIfZero: true);
			}
			int hpAfter = GameObject.Validate(victim) ? (victim.GetStat("Hitpoints")?.Value ?? -1) : -1;
			string where = !GameObject.Validate(stone) ? "destroyed"
				: stone.Equipped != null ? "equipped on " + stone.Equipped.Blueprint
				: stone.CurrentCell != null ? "loose at " + stone.CurrentCell.X + "," + stone.CurrentCell.Y
				: stone.InInventory != null ? "in " + stone.InInventory.Blueprint + "'s inventory"
				: "in limbo";
			Helpers.VerifyLog("AUTO", $"throwat: {blueprint} at {victim.Blueprint} -> perform={performed}, victim HP {hpBefore}->{hpAfter}, stone {where}");
			return stone;
		}

		/// <summary>A straight clear lane whose END cell also has at least minOpenNeighbors open
		/// sides. Harness bug #6 (2026-07-24, caught by the throwat diagnostic): the cage throw
		/// connected with a TinkerTurret INTERPOSED on the flight path -- the target's
		/// surroundings were open but the line to it wasn't. Throw targets need both.</summary>
		internal static Cell FindLaneOpenEnd(Cell origin, int length, int minOpenNeighbors)
		{
			string[] dirs = { "E", "W", "N", "S", "NE", "NW", "SE", "SW" };
			foreach (string dir in dirs)
			{
				Cell walk = origin;
				bool clear = true;
				for (int step = 0; step < length; step++)
				{
					walk = walk.GetCellFromDirection(dir);
					if (walk == null || walk.IsSolid() || walk.HasObjectWithPart("Combat"))
					{
						clear = false;
						break;
					}
				}
				if (!clear)
					continue;
				int openSides = 0;
				foreach (Cell adj in walk.GetLocalAdjacentCells())
					if (!adj.IsSolid() && !adj.HasObjectWithPart("Combat"))
						openSides++;
				if (openSides >= minOpenNeighbors)
					return walk;
			}
			return null;
		}

		/// <summary>Force a creature back and forth across a cell (DirectMoveTo, forced) --
		/// ObjectEnteredCellEvent fires synchronously per entry, so step-proc statistics need no
		/// turn pump. Stops early when stop() returns true. Returns entries performed.</summary>
		internal static int ForceEntries(GameObject victim, Cell offCell, Cell onCell, int max, Func<bool> stop)
		{
			int n = 0;
			for (; n < max && GameObject.Validate(victim); n++)
			{
				victim.DirectMoveTo(onCell, 0, Forced: true, IgnoreCombat: true);
				if (stop())
					return n + 1;
				victim.DirectMoveTo(offCell, 0, Forced: true, IgnoreCombat: true);
			}
			return n;
		}

		internal static int CountModRocks(GameObject who)
		{
			int n = 0;
			List<GameObject> pack = who?.Inventory?.GetObjectsDirect();
			if (pack != null)
				foreach (GameObject item in pack)
					if (item.Blueprint != null && item.Blueprint.StartsWith("Cleo_TerraFirma_Rock_"))
						n += item.Count;
			return n;
		}

		/// <summary>The strange-rock audit invariant ("everything it does should actually happen,
		/// no null" -- RULED 2026-07-24) as a standing assertion: 50 pool rolls, every rolled
		/// blueprint must be a creature-free grenade-carrier or payload-mimic, never strange
		/// itself. This is the invariant most likely to rot silently when future rocks join the
		/// catalog. Prototypes are created in memory and obliterated -- no zone side effects; the
		/// live fire-through/"visibly does something" half stays an eyeball item.</summary>
		[WishCommand(Command = "tfstrange")]
		public static void Strange()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			const int ROLLS = 50;
			int nulls = 0;
			List<string> bad = new List<string>();
			HashSet<string> seen = new HashSet<string>();
			for (int i = 0; i < ROLLS; i++)
			{
				string rolled = PopulationManager.RollOneFrom(STRANGE_POOL)?.Blueprint;
				if (rolled == null) { nulls++; continue; }
				seen.Add(rolled);
				if (rolled == STRANGE_BP) { bad.Add(rolled + " (itself)"); continue; }
				GameObject proto = GameObject.Create(rolled);
				if (proto == null) { bad.Add(rolled + " (no such blueprint)"); continue; }
				bool creature = proto.Brain != null;
				bool grenade = proto.GetFirstPartDescendedFrom<IGrenade>() != null;
				Cleo_TerraFirma_FallenRockPart frp = proto.GetPart<Cleo_TerraFirma_FallenRockPart>();
				bool mimic = frp != null && !frp.Payload.IsNullOrEmpty() && frp.Payload != "Strange";
				if (creature || (!grenade && !mimic))
					bad.Add(rolled + (creature ? " (creature)" : " (payloadless fizzle)"));
				proto.Obliterate();
			}
			bool pass = bad.Count == 0 && nulls == 0;
			Report("strange", pass,
				$"{ROLLS} pool rolls, {seen.Count} distinct blueprints, {nulls} null roll(s), {bad.Count} bad: {(bad.Count > 0 ? string.Join("; ", bad) : "none")} (every roll must detonate or mimic, never fizzle)");
		}

		// ---------------------------------------------------------------- streak (synchronous)

		/// <summary>The re-fix's exact failure shape (user find 2026-07-24): fly the LAST HELD
		/// streakschorl with spares in the BAG -> the next one must land in the hand. Setup order
		/// matters -- equip one BEFORE granting spares, or all three merge into one equipped stack
		/// and the flight's stack-split mimics a bag re-equip (the session's own false positive).</summary>
		[WishCommand(Command = "tfstreak")]
		public static void Streak()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			BodyPart slot = ply.GetFirstBodyPart("Thrown Weapon");
			if (slot == null) { Report("streak", false, "SETUP FAIL - player has no Thrown Weapon body part"); return; }

			// an open 3-cell lane for the flight
			Cell target = FindLane(origin, 3);
			if (target == null) { Report("streak", false, "SETUP FAIL - no open 3-cell lane from the player"); return; }

			// normalize the slot: a previous run's re-equipped stone (or the player's own thrown
			// weapon) otherwise absorbs the new stone by stacking and the equip silently no-ops --
			// the first live run's retry failure (2026-07-24), found in five minutes by the harness
			// doing exactly what it was built for
			if (slot.Equipped != null)
				slot.ForceUnequip(Silent: true);

			GameObject first = GameObject.Create(STREAK_BP);
			if (first == null) { Report("streak", false, "SETUP FAIL - could not create " + STREAK_BP); return; }
			// ForceEquipObject, NOT inventory-then-equip: passing through the bag stacks the new
			// stone onto any leftover streakschorls and the ref we hold goes stale
			ply.ForceEquipObject(first, slot, Silent: true);
			if (slot.Equipped != first) { Report("streak", false, "SETUP FAIL - could not equip the streakschorl into the thrown slot (even force-equip refused)"); return; }
			for (int i = 0; i < 2; i++)
				ply.ReceiveObject(GameObject.Create(STREAK_BP)); // spares AFTER the equip: separate bag stack
			int bagBefore = CountInBag(ply, STREAK_BP);

			GameObject baboon = GameObject.Create(BABOON_BP);
			baboon.SetIntProperty(SPAWN_MARK, 1);
			target.AddObject(baboon);

			bool suppressed = Popup.Suppress;
			Popup.Suppress = true;
			try
			{
				ply.PerformThrow(first, baboon.CurrentCell ?? target, baboon);
			}
			finally
			{
				Popup.Suppress = suppressed;
			}

			GameObject after = slot.Equipped;
			int bagAfter = CountInBag(ply, STREAK_BP);
			// two legal outcomes (live find 2026-07-25): on an Earthen Barrage character with
			// Take Stones on, the replacement flow's Check() is VETOED by EB's auto-sculpt --
			// the handler owns the slot and equips a sculpted stone instead. That is the
			// designed interaction (the re-fix's own Check-first contract), not a failure.
			bool bagPath = after != null && after.Blueprint == STREAK_BP && after != first;
			bool sculptPath = after != null && after.Blueprint == "Cleo_TerraFirma_EphemeralStone" && bagAfter == bagBefore;
			bool pass = bagPath || sculptPath;
			Report("streak", pass,
				$"slot after flight = {(after == null ? "EMPTY" : after.Blueprint + " x" + after.Count)}, bag {bagBefore}->{bagAfter}" +
				(bagPath ? " (re-equipped from bag, player path)"
				: sculptPath ? " (EB auto-sculpt vetoed the bag re-equip and armed a sculpted stone -- the Check-first contract's OTHER branch, working as designed)"
				: " (expected a bag spare OR an EB sculpted stone)"));
			if (GameObject.Validate(baboon))
				baboon.Obliterate(); // survivors don't get to hold a grudge
		}

		// ---------------------------------------------------------------- hopper (5 turns)

		/// <summary>Two wish-spawned mills: one force-powered (ChargeUse=0), one force-dead
		/// (unpayable ChargeUse), two salt rocks each. Five turns later the powered mill must
		/// hold powders and no stones; the dead mill must hold exactly its stones.</summary>
		[WishCommand(Command = "tfhopper")]
		public static void Hopper()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			SetupHopper(The.Player, "");
		}

		internal static void SetupHopper(GameObject ply, string queue)
		{
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 2) { Report("hopper", false, "SETUP FAIL - need two open adjacent cells for the mills"); RunNext(queue, ply); return; }

			GameObject millLive = SpawnMill(open[0], powered: true);
			GameObject millDead = SpawnMill(open[1], powered: false);
			if (millLive == null || millDead == null) { Report("hopper", false, "SETUP FAIL - Millstone spawn or Mill part missing"); RunNext(queue, ply); return; }

			Cleo_TerraFirma_TestDriver driver = new Cleo_TerraFirma_TestDriver { Scenario = "hopper", TurnsRemaining = 5, TurnsTotal = 5, Queue = queue };
			driver.InitHopper(millLive, millDead, stonesEach: 2);
			ply.AddPart(driver);
			MessageQueue.AddPlayerMessage("Harness: hopper scenario running -- standing still for 5 turns while the mills work.", "W");
		}

		/// <summary>Statuette expiry mechanics (batch item 5's assertable half): a spawned
		/// statuette with its TimedForm clock wound to 2 must, within 5 turns, be GONE and
		/// replaced by a rep-granting figurine item with its pocket stones spilled beside it.
		/// The popup ABSENCE stays the user's eyeball -- the property stamp is pre-obliteration
		/// and unobservable after the fact.</summary>
		[WishCommand(Command = "tfform")]
		public static void Form()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			SetupForm(The.Player, "");
		}

		internal static void SetupForm(GameObject ply, string queue)
		{
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 1) { Report("form", false, "SETUP FAIL - no open adjacent cell for the statuette"); RunNext(queue, ply); return; }
			GameObject statuette = GameObject.Create(STATUETTE_BP);
			Cleo_TerraFirma_TimedForm timed = statuette?.GetPart<Cleo_TerraFirma_TimedForm>();
			if (timed == null) { Report("form", false, "SETUP FAIL - statuette spawn or TimedForm part missing"); RunNext(queue, ply); return; }
			statuette.SetIntProperty(SPAWN_MARK, 1);
			timed.TurnsLeft = 2; // wind the clock: set BEFORE the first tick so the 500-1000 roll never runs
			open[0].AddObject(statuette);
			int baseline = CountInZone(ply.CurrentZone, FIGURINE_ITEM_BP);
			Cleo_TerraFirma_TestDriver driver = new Cleo_TerraFirma_TestDriver { Scenario = "form", TurnsRemaining = 5, TurnsTotal = 5, Queue = queue };
			driver.InitForm(statuette, baseline);
			ply.AddPart(driver);
			MessageQueue.AddPlayerMessage("Harness: form scenario running -- 5 turns while the statuette's clock runs out.", "W");
		}

		internal static void RunNext(string queue, GameObject ply)
		{
			if (queue.IsNullOrEmpty() || ply == null)
				return;
			int comma = queue.IndexOf(',');
			string next = comma < 0 ? queue : queue.Substring(0, comma);
			string rest = comma < 0 ? "" : queue.Substring(comma + 1);
			switch (next)
			{
				case "hopper": SetupHopper(ply, rest); break;
				case "form": SetupForm(ply, rest); break;
				case "scav": SetupScav(ply, rest); break;
				case "quickape": SetupQuickApe(ply, rest); break;
				case "dull": SetupDull(ply, rest); break;
				case "drain": SetupDrain(ply, rest); break;
				case "shape": SetupShape(ply, rest); break;
				case "fis": SetupFis(ply, rest); break;
			}
		}

		private static GameObject SpawnMill(Cell where, bool powered)
		{
			GameObject mill = GameObject.Create("Millstone");
			if (mill == null)
				return null;
			Mill part = mill.GetPart<Mill>();
			if (part == null)
				return null;
			part.ChargeUse = powered ? 0 : 999999999; // 0 = trivially powered; unpayable = guaranteed dead
			mill.SetIntProperty(SPAWN_MARK, 1);
			where.AddObject(mill);
			for (int i = 0; i < 2; i++)
				mill.ReceiveObject(GameObject.Create(HOPPER_STOCK_BP));
			return mill;
		}

		// ---------------------------------------------------------------- scav (30 turns)

		/// <summary>Four armed baboons, thirty turns, the player as the standing target. Counter
		/// parts on each baboon sample its thrown slot every baboon-turn: (ref, count) change =
		/// a throw; slot EMPTY with stones in pocket = a starved turn (the cadence regression's
		/// in-code signature -- with the free re-ready working it should be ZERO).</summary>
		[WishCommand(Command = "tfscav")]
		public static void Scav()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			SetupScav(The.Player, "");
		}

		internal static void SetupScav(GameObject ply, string queue)
		{
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;

			List<Cell> ring = origin.GetLocalAdjacentCells(6);
			ring.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat") || c.DistanceTo(origin) < 4);
			int spawned = 0;
			for (int i = 0; i < ring.Count && spawned < 4; i += Math.Max(1, ring.Count / 4))
			{
				GameObject baboon = GameObject.Create(BABOON_BP);
				if (baboon == null)
					break;
				baboon.SetIntProperty(SPAWN_MARK, 1);
				for (int s = 0; s < 3; s++)
				{
					GameObject ammo = GameObject.Create(SCAV_AMMO_BP);
					ammo.SetIntProperty(SPAWN_MARK, 1);
					baboon.ReceiveObject(ammo);
				}
				baboon.AddPart(new Cleo_TerraFirma_TestThrowCounter());
				ring[i].AddObject(baboon);
				// hostile-lock onto the player: a live run produced 0 throws in 118 turns because
				// the baboons never acquired ANY target -- engagement is arena-dependent unless forced
				if (baboon.Brain != null)
					baboon.Brain.Target = ply; // direct target assignment; faction hostility does the rest
				spawned++;
			}
			if (spawned == 0) { Report("scav", false, "SETUP FAIL - no open cells at distance 4-6 for baboons"); RunNext(queue, ply); return; }

			Cleo_TerraFirma_TestDriver driver = new Cleo_TerraFirma_TestDriver { Scenario = "scav", TurnsRemaining = 30, TurnsTotal = 30, Queue = queue };
			driver.InitScav();
			ply.AddPart(driver);
			MessageQueue.AddPlayerMessage($"Harness: scav scenario running -- {spawned} armed baboon(s), 30 turns, you are the target dummy. Hold still.", "W");
		}

		// ---------------------------------------------------------------- shared

		internal static Cell FindLane(Cell origin, int length)
		{
			string[] dirs = { "E", "W", "N", "S", "NE", "NW", "SE", "SW" };
			foreach (string dir in dirs)
			{
				Cell walk = origin;
				bool clear = true;
				for (int step = 0; step < length; step++)
				{
					walk = walk.GetCellFromDirection(dir);
					if (walk == null || walk.IsSolid() || (step < length - 1 && walk.HasObjectWithPart("Combat")))
					{
						clear = false;
						break;
					}
				}
				if (clear && !walk.HasObjectWithPart("Combat"))
					return walk;
			}
			return null;
		}

		internal static int CountInZone(Zone zone, string blueprint)
		{
			int n = 0;
			if (zone != null)
				foreach (GameObject obj in zone.GetObjects())
					if (obj.Blueprint == blueprint)
						n += obj.Count;
			return n;
		}

		internal static int CountInBag(GameObject who, string blueprint)
		{
			int n = 0;
			List<GameObject> bag = who.Inventory?.GetObjectsDirect();
			if (bag != null)
				foreach (GameObject item in bag)
					if (item.Blueprint == blueprint)
						n += item.Count;
			return n;
		}

		internal static void Report(string scenario, bool pass, string detail)
		{
			Helpers.VerifyLog("AUTO", $"SCENARIO {scenario}: {(pass ? "PASS" : "FAIL")} - {detail}");
			MessageQueue.AddPlayerMessage($"Harness {scenario}: {(pass ? "&GPASS" : "&RFAIL")}&y - {detail}");
		}

		internal static void SweepSpawns(Zone zone)
		{
			if (zone == null)
				return;
			List<GameObject> marked = new List<GameObject>();
			foreach (GameObject obj in zone.GetObjects())
				if (obj.GetIntProperty(SPAWN_MARK) == 1 && !obj.IsPlayer())
					marked.Add(obj);
			foreach (GameObject obj in marked)
				obj.Obliterate();
		}
	}
}

namespace XRL.World.Parts
{
	/// <summary>Turn pump + scenario finisher, attached to the PLAYER for the scenario's span.
	/// Eats player actions via the Paralyzed idiom (PreventAction; the engine zeroes energy
	/// itself) so the world runs at full AI while the player provably does nothing. Live
	/// scenario state is [NonSerialized] on purpose: a save/load mid-scenario aborts as
	/// INCONCLUSIVE instead of asserting against reloaded ghosts.</summary>
	[Serializable]
	public class Cleo_TerraFirma_TestDriver : IPart
	{
		public string Scenario = "";
		public int TurnsRemaining;
		public int TurnsTotal;

		/// <summary>Comma-separated scenarios to run after this one (tfbatch chaining). A lost-state
		/// abort drops the queue too -- a chain shouldn't outlive the save/load that broke it.</summary>
		public string Queue = "";

		[NonSerialized] private bool LiveSetup;
		[NonSerialized] private GameObject MillLive;
		[NonSerialized] private GameObject MillDead;
		[NonSerialized] private int StonesEach;
		[NonSerialized] private bool LoggedReadiness;
		[NonSerialized] private GameObject FormCreature;
		[NonSerialized] private int FigurineBaseline;
		[NonSerialized] private GameObject ApeCreature;
		[NonSerialized] private int RubbleBaseline;
		[NonSerialized] private GameObject DullRock;
		[NonSerialized] private int PebblesBaseline;
		[NonSerialized] private GameObject DrainFleshy;
		[NonSerialized] private GameObject DrainStoneA;
		[NonSerialized] private GameObject DrainRobot;
		[NonSerialized] private GameObject DrainStoneR;
		[NonSerialized] private int FleshyHpAtLatch;
		[NonSerialized] private int RobotHpAtLatch;
		[NonSerialized] private ActivatedAbilityEntry ShapeEntry;
		[NonSerialized] private int ShapeCdBeforeKill;
		[NonSerialized] private GameObject FisCaster;
		[NonSerialized] private int FisRocksBaseline;

		public void InitHopper(GameObject millLive, GameObject millDead, int stonesEach)
		{
			MillLive = millLive;
			MillDead = millDead;
			StonesEach = stonesEach;
			LiveSetup = true;
		}

		public void InitScav()
		{
			LiveSetup = true;
		}

		public void InitForm(GameObject creature, int figurineBaseline)
		{
			FormCreature = creature;
			FigurineBaseline = figurineBaseline;
			LiveSetup = true;
		}

		public void InitQuickApe(GameObject ape, int rubbleBaseline)
		{
			ApeCreature = ape;
			RubbleBaseline = rubbleBaseline;
			LiveSetup = true;
		}

		public void InitDull(GameObject rock, int pebblesBaseline)
		{
			DullRock = rock;
			PebblesBaseline = pebblesBaseline;
			LiveSetup = true;
		}

		public void InitDrain(GameObject fleshy, GameObject stoneA, GameObject robot, GameObject stoneR, int fleshyHp, int robotHp)
		{
			DrainFleshy = fleshy;
			DrainStoneA = stoneA;
			DrainRobot = robot;
			DrainStoneR = stoneR;
			FleshyHpAtLatch = fleshyHp;
			RobotHpAtLatch = robotHp;
			LiveSetup = true;
		}

		public void InitShape(ActivatedAbilityEntry entry, int cdBeforeKill)
		{
			ShapeEntry = entry;
			ShapeCdBeforeKill = cdBeforeKill;
			LiveSetup = true;
		}

		public void InitFis(GameObject caster, int rocksBaseline)
		{
			FisCaster = caster;
			FisRocksBaseline = rocksBaseline;
			LiveSetup = true;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == SingletonEvent<BeginTakeActionEvent>.ID;
			return true;
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			if (TurnsRemaining > 0)
			{
				if (!LiveSetup)
				{
					Cleo.TerraFirma.Scripts.TestHarnessWishes.Report(Scenario, false, "INCONCLUSIVE - driver state lost (save/load mid-scenario?); aborting");
					TurnsRemaining = 0;
					ParentObject.RemovePart(this);
					return base.HandleEvent(E);
				}
				if (Scenario == "hopper" && !LoggedReadiness)
				{
					LoggedReadiness = true;
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("AUTO", $"hopper tick 1: live mill turning={MillTurning(MillLive)}, dead mill turning={MillTurning(MillDead)}");
				}
				TurnsRemaining--;
				E.PreventAction = true;
				if (TurnsRemaining <= 0)
				{
					// remove FIRST: Finish may chain the queue's next scenario, whose setup
					// attaches a fresh driver -- two drivers must never coexist on the player.
					// But RemovePart NULLS ParentObject (first live run of round 2: three NREs,
					// one per finisher that read it, chain dead at hopper) -- capture the owner
					// before removal and pass it through.
					GameObject owner = ParentObject;
					owner.RemovePart(this);
					Finish(owner);
				}
			}
			return base.HandleEvent(E);
		}

		private static bool MillTurning(GameObject mill)
		{
			if (mill?.GetPart<Mill>() is Mill part)
				return part.IsReady(UseCharge: false, IgnoreCharge: false, IgnoreLiquid: false, IgnoreBootSequence: false, IgnoreBreakage: false, IgnoreRust: false, IgnoreEMP: false, IgnoreRealityStabilization: false, IgnoreSubject: false, IgnoreLocallyDefinedFailure: false, 1, null, UseChargeIfUnpowered: false, 0L);
			return false;
		}

		private void Finish(GameObject owner)
		{
			if (Scenario == "hopper")
				FinishHopper();
			else if (Scenario == "scav")
				FinishScav(owner);
			else if (Scenario == "form")
				FinishForm(owner);
			else if (Scenario == "quickape")
				FinishQuickApe(owner);
			else if (Scenario == "dull")
				FinishDull(owner);
			else if (Scenario == "drain")
				FinishDrain();
			else if (Scenario == "shape")
				FinishShape();
			else if (Scenario == "fis")
				FinishFis(owner);
			Cleo.TerraFirma.Scripts.TestHarnessWishes.SweepSpawns(owner.CurrentZone);
			Cleo.TerraFirma.Scripts.TestHarnessWishes.RunNext(Queue, owner);
		}

		private void FinishFis(GameObject owner)
		{
			bool casterAlive = GameObject.Validate(FisCaster);
			int rocksNow = Cleo.TerraFirma.Scripts.TestHarnessWishes.CountLooseFissureRocks(owner.CurrentZone);
			int delta = rocksNow - FisRocksBaseline;
			int allies = 0;
			Zone zone = owner.CurrentZone;
			if (zone != null && casterAlive)
				foreach (GameObject obj in zone.GetObjects())
					if (obj != FisCaster && obj.Brain != null && obj.Brain.PartyLeader == FisCaster)
						allies++;
			bool pass = casterAlive && delta >= 3;
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("fis", pass,
				$"NPC Fissure caster over {TurnsTotal} turns: loose fissure stones {FisRocksBaseline}->{rocksNow} (+{delta}, want >=3 = a cast happened), {allies} living fall(s) allied to the caster (seam-dependent, reported only). A zero here answers fis-npc: no AI advertisement on the mutation.");
		}

		private void FinishShape()
		{
			int cdNow = ShapeEntry?.Cooldown ?? -1;
			// the retirement watch: after the statue's death the cooldown may only TICK DOWN --
			// a jump UP is a reset to BASE (the retired display logic resurfacing), a plunge
			// far below the tick rate is a zero-out. Band: 3 turns of ticking, generous margin.
			bool pass = ShapeEntry != null && cdNow <= ShapeCdBeforeKill && cdNow >= ShapeCdBeforeKill - 40;
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("shape.cd", pass,
				$"cooldown {ShapeCdBeforeKill}->{cdNow} across {TurnsTotal} turns after the statue's destruction (must only tick: no reset upward, no zero-out; shape-cd retirement watch)");
		}

		private void FinishQuickApe(GameObject owner)
		{
			bool gone = !GameObject.Validate(ApeCreature);
			int rubbleNow = Cleo.TerraFirma.Scripts.TestHarnessWishes.CountInZone(owner.CurrentZone, "Cleo_TerraFirma_Rock_Rubble");
			int delta = rubbleNow - RubbleBaseline;
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("quick.ape", gone && delta >= 1,
				$"ape statue {(gone ? "crumbled and gone" : "STILL STANDING")} after {TurnsTotal} turns; rubble {RubbleBaseline}->{rubbleNow} (want +1, devourable per the ruling)");
		}

		private void FinishDull(GameObject owner)
		{
			bool goneFromBag = !GameObject.Validate(DullRock) || DullRock.InInventory != owner;
			int pebblesNow = Cleo.TerraFirma.Scripts.TestHarnessWishes.CountInBag(owner, "Cleo_TerraFirma_Rock_Pebbles");
			int delta = pebblesNow - PebblesBaseline;
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("dull", goneFromBag && delta >= 1,
				$"carried rock (clock wound to 2) {(goneFromBag ? "went dull and gone" : "STILL IN THE BAG")} after {TurnsTotal} turns; pebbles {PebblesBaseline}->{pebblesNow} (want >=+1, payload lost with the dull)");
		}

		private void FinishDrain()
		{
			bool fleshyAlive = GameObject.Validate(DrainFleshy);
			int fleshyHp = fleshyAlive ? DrainFleshy.GetStat("Hitpoints").Value : -1;
			bool drained = fleshyAlive && fleshyHp < FleshyHpAtLatch;
			bool drainOver = fleshyAlive && !DrainFleshy.HasEffect(typeof(XRL.World.Effects.LifeDrain));
			bool burstA = !GameObject.Validate(DrainStoneA);
			bool robotAlive = GameObject.Validate(DrainRobot);
			int robotHp = robotAlive ? DrainRobot.GetStat("Hitpoints").Value : -1;
			// robot HP is REPORTED, not asserted (two live lessons: robots regenerate the throw's
			// scratch, and live-arena bystanders can maul them during the wait -- 97->20 once).
			// The no-leech claim was already asserted at latch time (drain.robot); the ending's
			// claim is only that the stone dry-crumbles on schedule.
			bool crumbledR = !GameObject.Validate(DrainStoneR);
			bool pass = drained && drainOver && burstA && crumbledR;
			string stoneAState = burstA ? "gone (gorged burst)" : DrainStoneA?.Equipped != null ? "STILL ATTACHED" : "still exists, unattached";
			string stoneRState = crumbledR ? "gone (dry crumble)" : DrainStoneR?.Equipped != null ? "STILL ATTACHED" : "still exists, unattached (latch never took?)";
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("drain.endings", pass,
				$"after {TurnsTotal} turns -- flesh: HP {FleshyHpAtLatch}->{fleshyHp} ({(drained ? "drained" : "NOT drained")}), LifeDrain {(drainOver ? "expired" : "still ticking")}, stone {stoneAState}; robot: stone {stoneRState}, HP {RobotHpAtLatch}->{robotHp} (reported only -- bystanders/regen may move it)");
		}

		private void FinishForm(GameObject owner)
		{
			bool gone = !GameObject.Validate(FormCreature);
			Zone zone = owner.CurrentZone;
			int figurinesNow = Cleo.TerraFirma.Scripts.TestHarnessWishes.CountInZone(zone, "Cleo_TerraFirma_BaboonFigurineItem");
			int delta = figurinesNow - FigurineBaseline;
			// find the freshest figurine to report the spill beside it, then clean up the delta
			int spilled = 0;
			GameObject cleanupTarget = null;
			if (zone != null && delta > 0)
			{
				foreach (GameObject obj in zone.GetObjects())
				{
					if (obj.Blueprint == "Cleo_TerraFirma_BaboonFigurineItem")
						cleanupTarget = obj; // the last one found; with delta 1 (the normal case) it IS ours
				}
				if (cleanupTarget?.CurrentCell != null)
				{
					foreach (GameObject obj in cleanupTarget.CurrentCell.Objects)
						if (obj.HasTag("Cleo_TerraFirma_IsAnyStone") && obj.InInventory == null && obj.Equipped == null)
							spilled += obj.Count;
				}
			}
			bool pass = gone && delta > 0;
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("form", pass,
				$"statuette {(gone ? "expired and gone" : "STILL ALIVE")} after {TurnsTotal} turns; figurine items {FigurineBaseline}->{figurinesNow} (want +1); {spilled} loose pocket stone(s) beside the figurine");
			if (pass && cleanupTarget != null && GameObject.Validate(cleanupTarget))
				cleanupTarget.Obliterate(); // spilled pebbles stay as litter -- acceptable
		}

		private void FinishHopper()
		{
			CountMill(MillLive, out int liveStones, out int livePowders);
			CountMill(MillDead, out int deadStones, out int deadPowders);
			bool pass = liveStones == 0 && livePowders >= StonesEach * 2 && deadStones == StonesEach && deadPowders == 0;
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("hopper", pass,
				$"after {TurnsTotal} turns: powered mill stones={liveStones} powders={livePowders} (want 0 / >={StonesEach * 2}), dead mill stones={deadStones} powders={deadPowders} (want {StonesEach} / 0)");
		}

		private static void CountMill(GameObject mill, out int stones, out int powders)
		{
			stones = 0;
			powders = 0;
			List<GameObject> held = mill?.Inventory?.GetObjectsDirect();
			if (held == null)
				return;
			foreach (GameObject item in held)
			{
				if (item.HasTag("Cleo_TerraFirma_IsAnyStone"))
					stones += item.Count;
				else if (item.Blueprint != null && item.Blueprint.StartsWith("Cleo_TerraFirma_Ground"))
					powders += item.Count;
			}
		}

		private void FinishScav(GameObject owner)
		{
			int throws = 0;
			int starved = 0;
			int baboonTurns = 0;
			int survivors = 0;
			Zone zone = owner.CurrentZone;
			if (zone != null)
			{
				foreach (GameObject obj in zone.GetObjects())
				{
					if (obj.GetPart<Cleo_TerraFirma_TestThrowCounter>() is Cleo_TerraFirma_TestThrowCounter counter)
					{
						throws += counter.Throws;
						starved += counter.StarvedTurns;
						baboonTurns += counter.TurnsSeen;
						survivors++;
					}
				}
			}
			// the H1 detector: with the free pocket re-ready working, a baboon should never end
			// a sampled turn slot-empty while holding stones. Cadence is reported, not asserted --
			// the >= vanilla comparison still needs the user's control run for a baseline.
			bool pass = starved == 0 && throws > 0 && baboonTurns > 0;
			string cadence = baboonTurns > 0 ? ((double)throws / baboonTurns).ToString("0.00") : "n/a";
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("scav", pass,
				$"{survivors} baboon(s) surviving, {baboonTurns} baboon-turns sampled: {throws} throw(s) ({cadence}/turn), {starved} starved turn(s) [slot empty + stones pocketed] (want 0 starved, >0 throws)");
		}
	}

	/// <summary>Per-baboon throw sampler for the scav scenario: on each of the creature's own
	/// turns, compare the thrown slot's (object, count) against last turn -- any shrink or swap
	/// is a throw (a creature throws at most once per turn, so cap 1). Slot empty with stones
	/// still pocketed = a starved turn, the cadence regression's smoking gun.</summary>
	[Serializable]
	public class Cleo_TerraFirma_TestThrowCounter : IPart
	{
		public int Throws;
		public int StarvedTurns;
		public int TurnsSeen;

		[NonSerialized] private GameObject LastEquipped;
		[NonSerialized] private int LastCount;
		[NonSerialized] private bool Sampled;

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == SingletonEvent<BeginTakeActionEvent>.ID;
			return true;
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			TurnsSeen++;
			BodyPart slot = ParentObject.GetFirstBodyPart("Thrown Weapon");
			GameObject cur = slot?.Equipped;
			int curCount = cur?.Count ?? 0;
			if (Sampled && LastEquipped != null && (cur != LastEquipped || curCount < LastCount))
				Throws++; // at most one throw per creature-turn
			// starved = EMPTY on TWO consecutive samples with stones pocketed. A single EMPTY
			// blip is the throw itself: this sample runs before the scavenger's same-turn free
			// re-ready (first live run 2026-07-24: 8 "starved" turns, every one a phantom -- the
			// log showed each slot->EMPTY answered by "pocket re-ready ... (free action)" within
			// the same turn, zero REFUSED). Only emptiness that SURVIVES a whole turn is a lost turn.
			if (Sampled && cur == null && LastEquipped == null && PocketedStones() > 0)
				StarvedTurns++;
			LastEquipped = cur;
			LastCount = curCount;
			Sampled = true;
			return base.HandleEvent(E);
		}

		private int PocketedStones()
		{
			int n = 0;
			System.Collections.Generic.List<GameObject> pack = ParentObject.Inventory?.GetObjectsDirect();
			if (pack != null)
				foreach (GameObject item in pack)
					if (item.HasTag("Cleo_TerraFirma_IsAnyStone"))
						n += item.Count;
			return n;
		}
	}
}
