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

		[WishCommand(Command = "tfverify")]
		public static void Help()
		{
			MessageQueue.AddPlayerMessage("TF harness: 'tfstreak' (re-equip, instant) - 'tfstrange' (pool purity, instant) - 'tfhopper' (mill grind, 5 turns) - 'tfform' (statuette expiry, 5 turns) - 'tfscav' (baboon cadence, 30 turns, you are the target) - 'tfbatch' (all five, ~40 turns).", "W");
			if (!Helpers.VERIFY_WATCH)
				MessageQueue.AddPlayerMessage("VERIFY_WATCH is OFF -- the harness is disabled.", "R");
		}

		/// <summary>Round 2 (2026-07-24): everything in sequence. The synchronous pair runs now;
		/// the turn-driven three chain through the driver's queue: hopper -> form -> scav.</summary>
		[WishCommand(Command = "tfbatch")]
		public static void Batch()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			Streak();
			Strange();
			SetupHopper(The.Player, "form,scav");
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
			bool pass = after != null && after.Blueprint == STREAK_BP && after != first;
			Report("streak", pass,
				$"slot after flight = {(after == null ? "EMPTY" : after.Blueprint + " x" + after.Count)}, bag {bagBefore}->{bagAfter}" +
				(pass ? " (re-equipped from bag, player path)" : " (expected a spare readied from the bag)"));
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
			Cleo.TerraFirma.Scripts.TestHarnessWishes.SweepSpawns(owner.CurrentZone);
			Cleo.TerraFirma.Scripts.TestHarnessWishes.RunNext(Queue, owner);
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
