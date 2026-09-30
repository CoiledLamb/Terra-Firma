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
		private const string SCAV_AMMO_BP = "Cleo_TerraFirma_Rock_Pebbles";   // was Rock_Generic (deleted 2026-08-25); scavenger tests need a FallenRockPart carrier
		private const string BABOON_BP = "Baboon";
		private const string SPAWN_MARK = "TFHarnessSpawn";
		private const string STATUETTE_BP = "Cleo_TerraFirma_Rock_BaboonFigurine";
		private const string FIGURINE_ITEM_BP = "Cleo_TerraFirma_BaboonFigurineItem";
		private const string APE_BP = "Cleo_TerraFirma_Rock_ApeStatue";
		private const string RUBBLE_BP = "Cleo_TerraFirma_Rock_Rubble";
		private const string PEBBLES_BP = "Cleo_TerraFirma_Rock_Pebbles";
		private const string DULLTEST_BP = "Cleo_TerraFirma_Rock_Pebbles";   // was GENERIC_BP = Rock_Generic (deleted 2026-08-25); the dull test needs a FallenRockPart carrier
		private const string ROBOT_BP = "Scrapbot";
		private const string MOTH_BP = "Glowmoth";

		[WishCommand(Command = "tfverify")]
		public static void Help()
		{
			MessageQueue.AddPlayerMessage("TF harness (BETA TRIM 2026-08-02; catalog dev doors PARKED on branch quarantine-hold 2026-08-22) -- instant: tfquick (pockets+rep), tffeedback (2026-08-07 batch: Husangeion name/bleed, statue popup tag, rough-vs-tier look-text, gaze-wall lockout belt), tfredoubt (8 scripted casts: ring census by band, push/stun, carve depth, water displacement -- chews nearby natural stone). Turn-driven: tfform (5t), tfshape (Stoneshaper, 3t; incl. shape.mourning swap-quiet stamp), tffis (NPC caster, 20t), tfscav (30t, you are the target). tfbatch = the surviving set, ~60 turns standing still, ends with ONE 'BATCH COMPLETE' verdict line for the quick boot-and-test loop. Stage-for-eyes: tfgazewall (plain + animated holo wall, you cast the gaze). Diagnostic: tfstatueai (12t, statue-AI inertness repro: control + animated-wall + miner muses, snapshot + tick telemetry). Peek: tfspore (pure read).", "W");
			if (!Helpers.VERIFY_WATCH)
				MessageQueue.AddPlayerMessage("VERIFY_WATCH is OFF -- the harness is disabled.", "R");
		}

		/// <summary>Fungal-lottery state peek (user request 2026-07-25, for the save-quit-reload
		/// experiment): a PURE READ of the stored permutation -- prints to chat and the watcher,
		/// never rolls, never writes, so it cannot contaminate the experiment. EMPTY is itself an
		/// answer: no fungal rock has been minted this boot (creation is what rolls the lottery).</summary>
		/// <summary>NPC-caster spawner (2026-08-06, riding the advertisement pass: RandomMutations
		/// rolls are random, so eyeballing NPC casts needs a deterministic door). "tfnpc gorgon 4"
		/// spawns a hostile snapjaw beside the player carrying the named mutation at the given
		/// level (default 4), pre-aggroed so its AI starts advertising immediately. Keys: gorgon,
		/// eb, shaper, calcify, fissure (geomancy deliberately absent -- nothing for an NPC to
		/// use). Dev door only; strips with the harness.</summary>
		[WishCommand(Command = "tfnpc")]
		public static void SpawnNpcCaster(string args)
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			string[] parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0)
			{
				MessageQueue.AddPlayerMessage("tfnpc <gorgon|eb|shaper|calcify|fissure> [level] -- hostile snapjaw with the mutation, default level 4.", "W");
				return;
			}
			int level = parts.Length > 1 && int.TryParse(parts[1], out int lv) ? Math.Max(1, lv) : 4;
			XRL.World.Parts.Mutation.BaseMutation mutation;
			switch (parts[0].ToLower())
			{
				case "gorgon": mutation = new XRL.World.Parts.Mutation.Cleo_TerraFirma_GorgonsGaze(); break;
				case "eb": mutation = new XRL.World.Parts.Mutation.Cleo_TerraFirma_EarthenBarrage(); break;
				case "shaper": mutation = new XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper(); break;
				case "calcify": mutation = new XRL.World.Parts.Mutation.Cleo_TerraFirma_Calcify(); break;
				case "fissure": mutation = new XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure(); break;
				default:
					MessageQueue.AddPlayerMessage($"tfnpc: unknown mutation key '{parts[0]}' (gorgon|eb|shaper|calcify|fissure).", "R");
					return;
			}
			Cell here = The.Player?.CurrentCell;
			Cell spot = here?.GetLocalAdjacentCells(3)?.Find(c => c.IsEmpty());
			if (spot == null)
			{
				MessageQueue.AddPlayerMessage("tfnpc: no open cell within 3 tiles.", "R");
				return;
			}
			GameObject npc = GameObjectFactory.Factory.CreateObject("Snapjaw");
			npc.RequirePart<XRL.World.Parts.Mutations>().AddMutation(mutation, level);
			spot.AddObject(npc);
			npc.Brain.Attacked(The.Player); // pre-aggro: the ad events start firing on its first turn
			MessageQueue.AddPlayerMessage($"tfnpc: hostile Snapjaw with {parts[0]} Lv{level} at ({spot.X},{spot.Y}).", "W");
			Helpers.VerifyLog("NPC", $"tfnpc spawned Snapjaw + {mutation.GetType().Name} Lv{level} @({spot.X},{spot.Y})");
		}

		/// <summary>The full sweep, BETA TRIM shape (redoubt pivot, RULED 2026-08-02): three
		/// synchronous scenarios, then the turn-driven four chained through the driver's queue.
		/// Held-content scenarios (streak/strange/spill/puddle/mine/tar/devour/eat/hopper/carom)
		/// PARKED on branch quarantine-hold 2026-08-22 with the catalog; restore from that branch.
		/// Pre-trim chain at tag fissure-v2-grenade.</summary>
		[WishCommand(Command = "tfbatch")]
		public static void Batch()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			BatchPass = 0;
			BatchFail = 0;
			BatchFails.Clear();
			BatchActive = true;
			QuickSync();
			FeedbackSync();
			RedoubtSync();
			SetupShape(The.Player, "fis,scav"); // form held in quarantine/ 2026-09-30
		}

		// ---------------------------------------------------------------- round 3, synchronous

		/// <summary>The quick shelf, beta-trim shape: baboon pocket counts (inverted at the
		/// pivot) + figurine rep-on-take. Two asserts, one wish, no turns. (heal/cage asserts
		/// and the quickape chain are BETA-HELD with their rocks.)</summary>
		[WishCommand(Command = "tfquick")]
		public static void Quick()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			QuickSync();
		}

		internal static void QuickSync()
		{
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;

			// pockets, INVERTED at the redoubt pivot (RULED 2026-08-02: mod-rock pockets CUT,
			// plain Small Stones kept -- "let them keep their stones"): the merge still reaches
			// Hulking by inheritance but NOT Quartz; now it must deliver ZERO mod rocks and
			// 2-3 base-game Small Stones.
			GameObject hulk = GameObject.Create("Hulking Baboon");
			GameObject quartz = GameObject.Create("Quartz Baboon");
			int hulkRocks = CountModRocks(hulk);
			int quartzRocks = CountModRocks(quartz);
			int hulkPlain = 0;
			if (hulk?.Inventory != null)
				foreach (GameObject item in hulk.Inventory.GetObjects())
					if (item.Blueprint == "Small Stone")
						hulkPlain += item.Count;
			Report("quick.pockets", hulkRocks == 0 && quartzRocks == 0 && hulkPlain >= 2,
				$"Hulking Baboon spawns with {hulkRocks} mod rock(s) (want 0 post-pivot) and {hulkPlain} Small Stone(s) (want >=2), Quartz Baboon with {quartzRocks} mod rock(s) (want 0 -- outside the merge)");
			hulk?.Obliterate();
			quartz?.Obliterate();

			// quick.rep (baboon figurine rep-on-take) held in quarantine/ 2026-09-30 with the figurine

			// BETA-HELD quick.heal + quick.cage (healstone, aegischist) PARKED on branch
			// quarantine-hold 2026-08-22 with the catalog; restore from that branch.
		}

		/// <summary>The 2026-08-07 Discord-feedback batch, synchronous: Husangeion rename (ASCII-only
		/// after the H-root-sk-theta-vos glyph bug: the classic font draws by raw codepoint, so any
		/// accented char renders as CP437 salad) + salt-trace bleed with the producer left pure ·
		/// statue blueprint popup-eligible again (blanket NoFollowerDeathPopup deleted per the
		/// popup-on-expiry ruling) · rough carves read "crudely depicts" while other tiers keep the
		/// base "intricately" (tier-1-only ruling). The swap-quiet property stamp is asserted in
		/// tfshape (shape.mourning), where a real swap-carve runs.</summary>
		[WishCommand(Command = "tffeedback")]
		public static void Feedback()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			FeedbackSync();
		}

		internal static void FeedbackSync()
		{
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;

			// Husangeion: rename landed, name stays renderable, bleed carries the salt trace
			GameObject boar = GameObject.Create("Cleo_TerraFirma_BoarVessel");
			if (boar == null)
			{
				Report("feedback.boar", false, "SETUP FAIL - boar vessel blueprint would not spawn");
			}
			else
			{
				string name = boar.Render?.DisplayName ?? "";
				bool ascii = true;
				foreach (char ch in name)
					if (ch < ' ' || ch > '~') { ascii = false; break; }
				Report("feedback.boarname", name == "Husangeion" && ascii,
					$"display name '{name}' (want 'Husangeion', pure ASCII)");
				string bleed = boar.GetPropertyOrTag("BleedLiquid", "");
				LiquidProducer producer = boar.GetPart<LiquidProducer>();
				bool saltTrace = bleed == "water-900,salt-100";
				bool purePour = producer != null && producer.Liquid == "water";
				Report("feedback.boarbleed", saltTrace && purePour,
					$"BleedLiquid='{bleed}' (want water-900,salt-100, the ruled salt trace), producer liquid='{(producer == null ? "MISSING" : producer.Liquid)}' (want water -- the drinkable cargo stays pure)");
				// mood-ring glaze (2026-08-07; empty rule settled third-round on NEGATIVE SPACE,
				// "the channels fluid runs through"): part mounted, and a fresh boar's detail
				// matches its rolled cargo -- B over water, k over an empty tank (StartVolume
				// 1d33-1 CAN roll zero). Cider/wine tints are eyes-on -- pour and watch.
				bool tintMounted = boar.HasPart<Cleo_TerraFirma_VesselTint>();
				string detailNow = boar.Render?.DetailColor ?? "?";
				bool hasCargo = (boar.LiquidVolume?.Volume ?? 0) > 0;
				string wantDetail = hasCargo ? "B" : "k";
				Report("feedback.boartint", tintMounted && detailNow == wantDetail,
					$"VesselTint part {(tintMounted ? "mounted" : "MISSING")}, fresh-boar detail '{detailNow}' over {(hasCargo ? "water cargo (want B)" : "an empty tank (want k, the empty channels)")}");
				// it/its ruling, same day: Gender tag rides the blueprint
				string gender = boar.GetPropertyOrTag("Gender", "");
				Report("feedback.boargender", gender == "neuterperson",
					$"Gender='{gender}' (want neuterperson -- the base creature it/its idiom)");
				boar.Obliterate();
			}

			// statue blueprint: the mourning popup is eligible again (blanket tag deleted)
			GameObject inert = GameObject.Create("Cleo_TerraFirma_StoneshaperStatue");
			bool loud = inert != null && !inert.HasTagOrProperty("NoFollowerDeathPopup");
			Report("feedback.mourntag", loud, loud
				? "statue blueprint carries NO blanket NoFollowerDeathPopup (combat + expiry popups restored)"
				: "statue blueprint STILL blanket-tagged -- the popup ruling is not in effect");
			if (inert != null) inert.Obliterate();

			// tier look-text: rough diverges to "crudely", every other tier keeps the base sentence
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 1)
			{
				Report("feedback.look", false, "SETUP FAIL - no open adjacent cell for the test wall");
				return;
			}
			GameObject wall = GameObject.Create("Sandstone");
			open[0].AddObject(wall);
			GameObject muse = GameObject.Create(BABOON_BP);
			muse.SetIntProperty(SPAWN_MARK, 1);
			GameObject rough = XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper.MakeStatueOfCreatureFromWall(muse, wall, "rough");
			string roughDesc = rough?.GetPart<Description>()?._Short ?? "";
			bool crude = roughDesc.Contains("crudely depicts") && !roughDesc.Contains("intricately");
			Report("feedback.roughlook", crude, crude
				? "rough carve reads 'crudely depicts' with no 'intricately' residue"
				: $"rough carve look-text WRONG: '{Snip(roughDesc)}'");
			GameObject fine = XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper.MakeStatueOfCreatureFromWall(muse, wall, "fine");
			string fineDesc = fine?.GetPart<Description>()?._Short ?? "";
			bool untouched = fineDesc.Contains("intricately depicts") && !fineDesc.Contains("crudely");
			Report("feedback.tierlook", untouched, untouched
				? "fine carve keeps the base 'intricately depicts' (tier-1-only divergence holds)"
				: $"fine carve look-text WRONG: '{Snip(fineDesc)}'");
			if (rough != null && GameObject.Validate(rough)) rough.Obliterate();
			if (fine != null && GameObject.Validate(fine)) fine.Obliterate();
			if (GameObject.Validate(muse)) muse.Obliterate();
			if (GameObject.Validate(wall)) wall.Obliterate();

			// gaze-wall lockout regression (major bug 2026-08-07): a brainless gaze target must
			// RELEASE on the first tick, never NRE the action loop into the endless pass cycle.
			// BeginGaze bypasses the command gate ON PURPOSE -- this drives the TickGaze belt,
			// the same path that self-heals a save made inside the lockout.
			if (open.Count < 2)
			{
				Report("feedback.gazewall", false, "SETUP FAIL - need two open adjacent cells for the gaze rig");
				return;
			}
			GameObject gorgon = GameObject.Create("Snapjaw");
			gorgon.SetIntProperty(SPAWN_MARK, 1);
			open[1].AddObject(gorgon);
			var gaze = new XRL.World.Parts.Mutation.Cleo_TerraFirma_GorgonsGaze();
			gorgon.RequirePart<Mutations>().AddMutation(gaze, 4);
			GameObject holoWall = GameObject.Create("HolographicShaleWall");
			holoWall.SetIntProperty(SPAWN_MARK, 1);
			open[0].AddObject(holoWall);
			bool released;
			string gazeDetail;
			try
			{
				gaze.BeginGaze(holoWall);
				gaze.TickGaze();
				released = gaze.Target == null;
				gazeDetail = released
					? "brainless HolographicShaleWall released on tick one (no NRE, no lockout)"
					: $"gaze STILL HOLDS {gaze.Target?.Blueprint ?? "?"} after the tick -- the release belt did not fire";
			}
			catch (Exception ex)
			{
				released = false;
				gazeDetail = $"tick THREW {ex.GetType().Name} -- the endless-pass lockout is live ({ex.Message})";
			}
			Report("feedback.gazewall", released, gazeDetail);
			if (GameObject.Validate(holoWall)) holoWall.Obliterate();
			if (GameObject.Validate(gorgon)) gorgon.Obliterate();

			// entombment regression (user report 2026-08-15: "armor should be lootable"). The old
			// code cleared the equip SLOT and handed the item to nobody, so worn gear was destroyed
			// by petrification while the backpack survived. Worn item must end up inside the statue.
			GameObject victim = GameObject.Create("Snapjaw");
			victim.SetIntProperty(SPAWN_MARK, 1);
			GameObject worn = GameObject.Create("Leather Armor");
			GameObject carried = GameObject.Create("Leather Boots");
			GameObject tomb = GameObject.Create("Cleo_TerraFirma_QuarriedBlock");
			tomb.SetIntProperty(SPAWN_MARK, 1);
			victim.ReceiveObject(worn);
			victim.ReceiveObject(carried);
			bool equipped = victim.ForceEquipObject(worn, "Body", Silent: true);
			StatueHelpers.EntombPossessions(victim, tomb);
			bool wornInTomb = tomb.Inventory != null && tomb.Inventory.GetObjectsDirect().Contains(worn);
			bool carriedInTomb = tomb.Inventory != null && tomb.Inventory.GetObjectsDirect().Contains(carried);
			string entombDetail =
				!equipped ? "SETUP FAIL - could not equip the test armor on a Snapjaw body" :
				(wornInTomb && carriedInTomb) ? "worn armor AND carried boots both entombed (the orphaning bug stays fixed)" :
				!wornInTomb ? "WORN ARMOR LOST - it never reached the statue, the slot-clear orphan is back" :
				"carried item lost - the inventory sweep broke";
			Report("feedback.entomb", equipped && wornInTomb && carriedInTomb, entombDetail);
			if (GameObject.Validate(tomb)) tomb.Obliterate();
			if (GameObject.Validate(victim)) victim.Obliterate();
		}

		private static string Snip(string s) => s.IsNullOrEmpty() ? "(empty)" : (s.Length > 90 ? s.Substring(0, 90) + "..." : s);

		/// <summary>Manual staging for the gaze-wall fix (user test design 2026-08-07): one PLAIN
		/// HolographicShaleWall (gaze it -> expect "You cannot transfix..." with NO cooldown burn)
		/// and one ANIMATED one (gaze it -> expect a normal petrify duel; animation grants Brain +
		/// donor stats and strips HologramInvulnerability). Eyes-on wish -- you cast the gaze.</summary>
		[WishCommand(Command = "tfgazewall")]
		public static void GazeWallStage()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 2)
			{
				MessageQueue.AddPlayerMessage("tfgazewall: need two open adjacent cells.", "R");
				return;
			}
			GameObject plain = GameObject.Create("HolographicShaleWall");
			open[0].AddObject(plain);
			GameObject animated = GameObject.Create("HolographicShaleWall");
			open[1].AddObject(animated);
			AnimateObject.Animate(animated);
			Helpers.VerifyLog("GAZE", $"tfgazewall staged: plain wall @({open[0].X},{open[0].Y}), animated wall @({open[1].X},{open[1].Y})");
			MessageQueue.AddPlayerMessage($"Staged: PLAIN holographic shale at ({open[0].X},{open[0].Y}) -- gaze must refuse, no cooldown; ANIMATED at ({open[1].X},{open[1].Y}) -- gaze must duel normally.", "W");
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

			// popup ruling 2026-08-07: the swap branch alone stamps quiet-suppression on the
			// OUTGOING statue (graveyard reference still readable post-obliterate); the incoming
			// statue must stay popup-eligible so its combat death / natural expiry mourns loud
			bool swapQuiet = statueA != null && statueA.GetStringProperty("NoFollowerDeathPopup") == "1";
			bool freshLoud = statueB != null && !statueB.HasTagOrProperty("NoFollowerDeathPopup");
			Report("shape.mourning", swapQuiet && freshLoud,
				$"swap-carve suppression stamp on the outgoing statue: {(swapQuiet ? "present (swap stays a log line)" : "MISSING (swap would popup)")}; incoming statue popup-eligible: {(freshLoud ? "yes (combat/expiry mourn loud)" : "NO (something re-tagged it)")}");

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

		/// <summary>Statue-AI diagnostic (USER REPORT 2026-08-10: statues carved from an
		/// animated wall and from a miner stood FULLY INERT -- no move, no attack, no follow).
		/// Static analysis cleared every suspected channel: the statue is a fresh object (no
		/// muse AI parts), its Brain is fresh (Mobile default), and zero body mobility is only
		/// a -60 movespeed penalty (Body.cs:2427) -- so the dead dial is unknown. This battery
		/// runs the REAL pipeline (the statics CarveFrom composes: MakeStatue + Animate +
		/// ApplyStatueBuffs; no mutation or character requirement) on a control baboon plus
		/// both reported muses, snapshots every AI-relevant dial at birth, then drives all
		/// three with Directed kill goals for 12 turns with telemetry every 3rd tick. Run by
		/// hand (not in tfbatch): it exists to answer ONE boot's question.</summary>
		[WishCommand(Command = "tfstatueai")]
		public static void StatueAI()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			SetupStatueAI(The.Player, "");
		}

		internal static void SetupStatueAI(GameObject ply, string queue)
		{
			Cell origin = ply?.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (zone == null)
				return;
			// three lanes: statue at offset*2, its quarry at offset*5, one compass direction each
			int[][] dirs = { new[] { 0, -1 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { -1, 0 }, new[] { 1, -1 }, new[] { -1, 1 } };
			var statueCells = new List<Cell>();
			var dummyCells = new List<Cell>();
			foreach (int[] d in dirs)
			{
				if (statueCells.Count >= 3)
					break;
				// the WHOLE lane must be open, statue through quarry (first run 2026-08-10:
				// the control lane froze statue AND quarry both -- unvalidated middle cells
				// left an environment artifact indistinguishable from the bug under test)
				bool laneOpen = true;
				for (int step = 1; step <= 5; step++)
				{
					Cell c = zone.GetCell(origin.X + d[0] * step, origin.Y + d[1] * step);
					if (c == null || c.IsSolid() || (step >= 2 && c.HasObjectWithPart("Combat")))
					{
						laneOpen = false;
						break;
					}
				}
				if (!laneOpen)
					continue;
				statueCells.Add(zone.GetCell(origin.X + d[0] * 2, origin.Y + d[1] * 2));
				dummyCells.Add(zone.GetCell(origin.X + d[0] * 5, origin.Y + d[1] * 5));
			}
			if (statueCells.Count < 3) { Report("statueai", false, "SETUP FAIL - need three open statue/quarry lanes within 5"); RunNext(queue, ply); return; }

			GameObject refWall = GameObject.Create("Sandstone");
			Cleo_TerraFirma_StoneshaperWallProperties wallProps = refWall.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			if (wallProps == null) { Report("statueai", false, "SETUP FAIL - spawned Sandstone lacks StoneshaperWallProperties"); refWall.Obliterate(); RunNext(queue, ply); return; }

			// muses: the control, the two reported breakers. The wall muse is ANIMATED first --
			// the exact play path (BodyType tag "Wall" gives it the Wall anatomy on animation).
			GameObject museControl = GameObject.Create(BABOON_BP);
			GameObject museWall = GameObject.Create("Sandstone");
			XRL.World.Parts.AnimateObject.Animate(museWall);
			GameObject museMiner = GameObject.Create("Miner Mk I");
			GameObject[] muses = { museControl, museWall, museMiner };
			string[] keys = { "control", "wall", "miner" };

			var statues = new GameObject[3];
			var dummies = new GameObject[3];
			var startCells = new Cell[3];
			var dummyHp = new int[3];
			for (int i = 0; i < 3; i++)
			{
				Helpers.VerifyLog("AUTO", $"statueai.{keys[i]} muse: {muses[i].Blueprint}, anatomy={muses[i].Body?.Anatomy ?? "NONE"}, IsCreature={muses[i].IsCreature}");
				GameObject statue = XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper.MakeStatueOfCreatureFromWall(muses[i], refWall, keys[i], wallProps);
				if (statue == null)
				{
					Report($"statueai.{keys[i]}", false, $"statue creation returned NULL for muse {muses[i].Blueprint} (IsCreature={muses[i].IsCreature})");
					continue;
				}
				XRL.World.Parts.AnimateObject.Animate(statue);
				XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper.ApplyStatueBuffs(ply, muses[i], statue, 5, wallProps);
				statue.SetIntProperty(SPAWN_MARK, 1);
				statueCells[i].AddObject(statue);
				GameObject dummy = GameObject.Create(BABOON_BP);
				dummy.GetStat("Hitpoints").BaseValue = 600; // must survive 12 turns of testimony
				dummy.SetIntProperty(SPAWN_MARK, 1);
				dummyCells[i].AddObject(dummy);
				statues[i] = statue;
				dummies[i] = dummy;
				startCells[i] = statueCells[i];
				dummyHp[i] = dummy.GetStat("Hitpoints").Value;
				// the birth snapshot: every dial inertness could ride. A dead statue names its
				// killer here or in the tick telemetry -- that is this battery's whole product.
				var armed = new List<string>();
				if (statue.Body != null)
				{
					foreach (BodyPart p in statue.Body.LoopParts())
					{
						if (p.DefaultBehavior != null)
							armed.Add($"{p.Type}:{p.DefaultBehavior.Blueprint}");
					}
				}
				bool anyDismembered;
				int movePenalty = statue.Body?.CalculateMobilitySpeedPenalty(out anyDismembered) ?? -1;
				Helpers.VerifyLog("AUTO", $"statueai.{keys[i]} snapshot: anatomy={statue.Body?.Anatomy ?? "NONE"}, mobility={statue.Body?.GetTotalMobility() ?? -1}, movePenalty={movePenalty}, Speed={statue.GetStatValue("Speed")}, MoveSpeed={statue.GetStatValue("MoveSpeed")}, Energy={statue.GetStatValue("Energy")}, weight={statue.Physics?.Weight ?? -1}, leader={statue.Brain?.PartyLeader?.Blueprint ?? "NONE"}, wanders={statue.Brain?.Wanders ?? false}, hostile={statue.Brain?.Hostile ?? false}, armed=[{string.Join("|", armed.ToArray())}]");
			}
			foreach (GameObject muse in muses)
				muse.Obliterate();
			refWall.Obliterate();

			// 20 turns, two phases: 12 of kill-drive, then 8 of NATURAL follow-back (no forced
			// goals -- the reported play symptom is companion behavior, not forced combat)
			Cleo_TerraFirma_TestDriver driver = new Cleo_TerraFirma_TestDriver { Scenario = "statueai", TurnsRemaining = 20, TurnsTotal = 20, Queue = queue };
			driver.InitStatueAI(statues, dummies, keys, startCells, dummyHp);
			ply.AddPart(driver);
			MessageQueue.AddPlayerMessage("Harness: statue-AI diagnostic running -- 12 turns kill-drive, then 8 turns natural follow.", "W");
		}

		// ---------------------------------------------------------------- round 5

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
		/// itself the fis-npc answer: no AI advertisement. SPAWN DISTANCE 4-6 -> 2-3 (first
		/// pulse boot 2026-08-09): the pulse pass tightened the worth-casting gate to coverage
		/// (adjacency always, rings 2-3 coin-flipped), so a caster at 4-6 correctly never
		/// advertises — the scenario now spawns inside coverage to test that the gate FIRES.</summary>
		internal static void SetupFis(GameObject ply, string queue)
		{
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> ring = origin.GetLocalAdjacentCells(3);
			ring.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat") || c.DistanceTo(origin) < 2);
			if (ring.Count == 0) { Report("fis", false, "SETUP FAIL - no open cell at distance 2-3"); RunNext(queue, ply); return; }
			GameObject caster = GameObject.Create("Hulking Baboon");
			caster.SetIntProperty(SPAWN_MARK, 1);
			caster.GetStat("Hitpoints").BaseValue = 150;
			Mutations mutations = caster.RequirePart<Mutations>();
			mutations.AddMutation(new XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure(), 5);
			ring[0].AddObject(caster);
			// DIRECTED kill (root cause of the 0-cast boots, decomp Kill.cs:506): a plain
			// Brain.Target assignment makes a NON-directed Kill goal, which pops itself
			// every turn against a target the actor isn't hostile to -- and baboon REP on
			// the test character (figurine scenarios) made the player exactly that. The
			// Directed flag skips the hostility check; the harness orders the kill.
			caster.Brain?.WantToKill(ply, "harness", Directed: true);
			Cleo_TerraFirma_TestDriver driver = new Cleo_TerraFirma_TestDriver { Scenario = "fis", TurnsRemaining = 20, TurnsTotal = 20, Queue = queue };
			driver.InitFis(caster, CountRingPieces(ply.CurrentZone));
			ply.AddPart(driver);
			MessageQueue.AddPlayerMessage("Harness: NPC redoubt caster running -- 20 turns; expect the earth to heave and a ring to rise.", "W");
		}

		internal static int CountLooseFissureRocks(Zone zone)
		{
			int n = 0;
			if (zone != null)
				foreach (GameObject obj in zone.GetObjects())
					if (obj.Blueprint == PEBBLES_BP && obj.CurrentCell != null && obj.InInventory == null && obj.Equipped == null)
						n += obj.Count;
			return n;
		}

		/// <summary>Redoubt census: every mod rock-family object standing in the zone -- fabric
		/// (rubble/crumbled/wall/crack), salvage keepers, debris, and the figure.</summary>
		internal static int CountRingPieces(Zone zone)
		{
			int n = 0;
			if (zone != null)
				foreach (GameObject obj in zone.GetObjects())
					if (obj.CurrentCell != null && obj.InInventory == null && obj.Equipped == null
						&& (obj.Blueprint.StartsWith("Cleo_TerraFirma_Rock_") || obj.Blueprint == "Cleo_TerraFirma_RedoubtFigure"))
						n += obj.Count;
			return n;
		}

		// ---------------------------------------------------------------- redoubt battery (2026-08-04)

		private const string FIS_COMMAND = "Cleo_TerraFirma_CommandFissure";
		private const string FABRIC_CRUMBLED_BP = "Cleo_TerraFirma_Rock_CrumbledWall";
		private const string FABRIC_WALL_BP = "Cleo_TerraFirma_Rock_Wall";
		private const string NATURAL_WALL_BP = "Sandstone";
		private const string METAL_WALL_BP = "MetalWall";
		private const string WOOD_WALL_BP = "WoodWall";
		private const string PUDDLE_BP = "FreshWaterPuddle";

		[WishCommand(Command = "tfredoubt")]
		public static void Redoubt()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			RedoubtSync();
		}

		/// <summary>The redoubt battery (owed since the pivot: ring census by band, wave
		/// push/stun, carve depth, water displacement): eight player-path casts through the
		/// real CommandEvent pipeline at scripted levels, choreography muted so every deposit
		/// lands synchronously and assertable. The casts CHEW LIVE TERRAIN (natural stone
		/// answers the wave, radius up to 5) -- run somewhere you don't mind rearranging.
		/// Spawns and deposits are swept; a native Fissure's level and cooldown are restored.</summary>
		internal static void RedoubtSync()
		{
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (zone == null)
				return;
			MessageQueue.AddPlayerMessage("Harness: redoubt battery running -- 8 scripted casts; the wave will chew natural stone around you and shove anything standing nearby.", "W");
			Mutations mutations = ply.RequirePart<Mutations>();
			XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure fis = ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure>();
			bool granted = false;
			int savedLevel = 0, savedCooldown = 0;
			ActivatedAbilityEntry entry = null;
			if (fis == null)
			{
				fis = new XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure();
				mutations.AddMutation(fis, 1);
				granted = true;
			}
			else
			{
				savedLevel = fis.BaseLevel;
				entry = ply.ActivatedAbilities?.GetAbility(fis.ActivatedAbilityID);
				savedCooldown = entry?.Cooldown ?? 0;
			}
			int savedCap = fis.CapOverride;
			bool suppressed = Popup.Suppress;
			Popup.Suppress = true;
			Cleo_TerraFirma_StoneRiseFX.HarnessMute = true;
			try
			{
				RedoubtRingCensus(ply, zone, origin, fis);
				RedoubtPush(ply, zone, origin, fis);
				RedoubtCarve(ply, zone, origin, fis);
				RedoubtWater(ply, zone, origin, fis);
			}
			finally
			{
				Cleo_TerraFirma_StoneRiseFX.HarnessMute = false;
				Popup.Suppress = suppressed;
				if (granted)
					mutations.RemoveMutation(fis);
				else
				{
					fis.CapOverride = savedCap;
					fis.Level = savedLevel;
					if (entry != null)
						entry.Cooldown = savedCooldown;
				}
				SweepSpawns(zone);
			}
		}

		private static void RedoubtCast(GameObject ply, XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure fis, int level)
		{
			// CapOverride lifts the character-level mutation cap (the mosquito-Wings-10 door);
			// without it every scripted level clamps back to the test character's cap -- the
			// 2026-08-04 boot ran all eight casts at Lv1 because of exactly this
			fis.CapOverride = level;
			fis.Level = level;
			CommandEvent.Send(ply, FIS_COMMAND);
		}

		// the cast's own Euclidean contract (Mutation_Fissure InDisk/OnRing), restated here as
		// the assertion's independent copy -- if the cast's geometry drifts, the census fails
		private static bool RedoubtDisk(int dx, int dy, int r) => dx * dx + dy * dy <= r * r + r;

		private static bool RedoubtRim(int dx, int dy, int r)
		{
			int d2 = dx * dx + dy * dy;
			return d2 > r * r - r && d2 <= r * r + r;
		}

		private static int RedoubtCheb(Cell a, Cell b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

		private static bool RedoubtIsFabric(string bp) => bp == RUBBLE_BP || bp == FABRIC_CRUMBLED_BP || bp == FABRIC_WALL_BP;

		private static bool RedoubtIsScatter(string bp) => bp == "SmallBoulder" || bp == PEBBLES_BP || bp == "MediumBoulder";   // SmallBoulder = fissure's post-deletion debris

		private static HashSet<GameObject> SnapshotZone(Zone zone)
		{
			HashSet<GameObject> set = new HashSet<GameObject>();
			foreach (GameObject o in zone.GetObjects())
				set.Add(o);
			return set;
		}

		private static List<GameObject> NewSince(Zone zone, HashSet<GameObject> before)
		{
			List<GameObject> fresh = new List<GameObject>();
			foreach (GameObject o in zone.GetObjects())
				if (!before.Contains(o))
					fresh.Add(o);
			return fresh;
		}

		private static void RedoubtCleanup(List<GameObject> fresh)
		{
			foreach (GameObject o in fresh)
			{
				if (!GameObject.Validate(o))
					continue;
				string bp = o.Blueprint;
				if (bp != null && (bp.StartsWith("Cleo_TerraFirma_Rock_") || bp == "Cleo_TerraFirma_RedoubtFigure" || bp == "MediumBoulder"))
					o.Obliterate();
			}
		}

		private static bool RedoubtHasOpenLiquid(Cell c)
		{
			foreach (GameObject o in c.Objects)
				if (o.IsOpenLiquidVolume())
					return true;
			return false;
		}

		private static int RedoubtCountLiquids(Zone zone)
		{
			int n = 0;
			foreach (GameObject o in zone.GetObjects())
				if (o.CurrentCell != null && o.IsOpenLiquidVolume())
					n++;
			return n;
		}

		/// <summary>Ring census, one cast per band (L1/4/7/10): fabric confined to the Euclidean
		/// rim, gap count exact on a clear disk, wake scatter interior-only, and NO beta-held
		/// content (crack/salvage/figure) leaking out of the marked blocks. Wall share is
		/// reported, not asserted -- rolled variance on small rims is too noisy to pin.</summary>
		private static void RedoubtRingCensus(GameObject ply, Zone zone, Cell origin, XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure fis)
		{
			int[] levels = { 1, 4, 7, 10 };
			List<string> fails = new List<string>();
			List<string> notes = new List<string>();
			foreach (int lv in levels)
			{
				int radius = XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure.GetRingRadius(lv);
				int gapWant = XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure.GetGapCount(lv);
				HashSet<Cell> openRim = new HashSet<Cell>();
				bool diskClear = true;
				for (int dx = -radius; dx <= radius; dx++)
				{
					for (int dy = -radius; dy <= radius; dy++)
					{
						if (dx == 0 && dy == 0)
							continue;
						if (!RedoubtDisk(dx, dy, radius))
							continue;
						Cell c = zone.GetCell(origin.X + dx, origin.Y + dy);
						if (c == null || c.IsSolid() || c.HasObjectWithPart("Combat"))
						{
							diskClear = false;
							continue;
						}
						if (RedoubtRim(dx, dy, radius))
							openRim.Add(c);
					}
				}
				HashSet<GameObject> before = SnapshotZone(zone);
				RedoubtCast(ply, fis, lv);
				List<GameObject> fresh = NewSince(zone, before);
				int onRim = 0, offRim = 0, walls = 0, scatter = 0, scatterOnRim = 0, leaks = 0;
				foreach (GameObject o in fresh)
				{
					Cell c = o.CurrentCell;
					string bp = o.Blueprint;
					if (c == null || bp == null)
						continue;
					if (RedoubtIsFabric(bp))
					{
						if (openRim.Contains(c)) { onRim++; if (bp == FABRIC_WALL_BP) walls++; }
						else offRim++;
					}
					else if (RedoubtIsScatter(bp))
					{
						scatter++;
						if (openRim.Contains(c))
							scatterOnRim++;
					}
					else if (bp.StartsWith("Cleo_TerraFirma_"))
					{
						leaks++;
						fails.Add($"L{lv}: BETA-HELD leak -- {bp} spawned by the cast");
					}
				}
				int gapsObserved = openRim.Count - onRim;
				string line = $"L{lv} r{radius}: {onRim} fabric on {openRim.Count} open rim cells ({walls} walls), {gapsObserved} gaps (table {gapWant}), {scatter} wake scatter";
				if (diskClear)
				{
					if (offRim > 0) fails.Add($"L{lv}: {offRim} fabric OFF the rim");
					if (scatterOnRim > 0) fails.Add($"L{lv}: {scatterOnRim} wake scatter ON the rim");
					if (gapsObserved != gapWant) fails.Add($"L{lv}: {gapsObserved} gaps, table says {gapWant} (disk was clear)");
				}
				else
					line += " [terrain in the disk: census reported, not asserted]";
				notes.Add(line);
				RedoubtCleanup(fresh);
			}
			Report("redoubt.ring", fails.Count == 0,
				string.Join(" | ", notes.ToArray()) + (fails.Count > 0 ? "; FAILS: " + string.Join("; ", fails.ToArray()) : "; fabric held to the Euclidean rim at every band, no beta-held content leaked"));
		}

		/// <summary>The churn's crowd control at L5 (v5 staircase: push force 7000 clamped to
		/// one ring per pulse, dice 3d4/2d4/1d4 rolled per penetration at PV 4+Str, ONE stun
		/// check per cast at 2r, stepped DC 18 at L5, moved-only): baboons ringed at distance
		/// 1 ride the full churn (3 hits, shoved ring by ring to the rim and evicted clear),
		/// so all must end further out, all damaged (three pen rolls vs baboon AV going 0-pen
		/// every time is the only flake path), and at least one stunned; a glowmoth overhead
		/// must be untouched (ground churn spares flyers); a loose Small Stone inside the disk
		/// must not move (loot rule -- the wave never touches items).</summary>
		private static void RedoubtPush(GameObject ply, Zone zone, Cell origin, XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure fis)
		{
			const int LV = 5;
			string[] dirs = { "N", "S", "E", "W" };
			List<GameObject> victims = new List<GameObject>();
			Dictionary<GameObject, Cell> starts = new Dictionary<GameObject, Cell>();
			Dictionary<GameObject, int> hp = new Dictionary<GameObject, int>();
			foreach (string dir in dirs)
			{
				Cell c1 = origin.GetCellFromDirection(dir);
				Cell c2 = c1?.GetCellFromDirection(dir);
				// the push lane: distance-2 must be open too, so "moved" is physically possible
				if (c1 == null || c2 == null || c1.IsSolid() || c2.IsSolid() || c1.HasObjectWithPart("Combat") || c2.HasObjectWithPart("Combat"))
					continue;
				GameObject b = GameObject.Create(BABOON_BP);
				b.SetIntProperty(SPAWN_MARK, 1);
				c1.AddObject(b);
				// 60 HP was the single-hit wave's era: the pulse churn KILLED two probes (first
				// pulse boot 2026-08-09), under-counting every assert. The 400-HP fix then
				// SILENTLY FAILED for a boot era (caught at the 2026-08-15 batch, all four
				// probes DOWN to 7-17 dmg): the engine finalizes Hitpoints from Toughness at
				// zone entry, so a BaseValue set BEFORE AddObject is clobbered by the recalc.
				// Set AFTER placement, and log if it ever fails to stick again.
				b.GetStat("Hitpoints").BaseValue = 400;
				b.GetStat("Hitpoints").Penalty = 0;
				victims.Add(b);
				starts[b] = c1;
				hp[b] = b.GetStat("Hitpoints").Value;
				if (hp[b] < 300)
					Helpers.VerifyLog("AUTO", $"redoubt.push SETUP WARN: probe HP set did not stick (Value {hp[b]}) -- census will under-count");
			}
			if (victims.Count < 2)
			{
				Report("redoubt.push", false, "SETUP FAIL - need at least two open two-deep orthogonal lanes");
				SweepSpawns(zone);
				return;
			}
			// the flyer probe and the loot probe sit strictly INSIDE the rim (d2 <= r*r-r), where
			// only non-wall wake scatter can land -- a wall deposit would sweep loot legitimately
			GameObject moth = null;
			Cell mothCell = null;
			int mothHP = 0;
			GameObject loot = null;
			Cell lootCell = null;
			int rad = XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure.GetRingRadius(LV);
			foreach (Cell c in origin.GetLocalAdjacentCells(2))
			{
				int dx = c.X - origin.X, dy = c.Y - origin.Y;
				if (c.IsSolid() || c.HasObjectWithPart("Combat") || RedoubtCheb(c, origin) != 2)
					continue;
				if (dx * dx + dy * dy > rad * rad - rad)
					continue;
				if (moth == null)
				{
					moth = GameObject.Create(MOTH_BP);
					moth.SetIntProperty(SPAWN_MARK, 1);
					c.AddObject(moth);
					// spawned creatures start GROUNDED (the AI toggles flight on its own turns,
					// and no turns pass here) -- the 2026-08-04 boot's moth ate the wave for
					// exactly this reason. Force takeoff; if it still won't fly, the probe
					// reports itself inert instead of failing the scenario.
					XRL.World.Capabilities.Flight.StartFlying(moth, moth);
					mothCell = c;
					mothHP = moth.GetStat("Hitpoints").Value;
					continue;
				}
				loot = GameObject.Create("Small Stone");
				c.AddObject(loot);
				lootCell = c;
				break;
			}
			bool mothFlying = moth != null && moth.IsFlying;
			HashSet<GameObject> before = SnapshotZone(zone);
			RedoubtCast(ply, fis, LV);
			int movedN = 0, hurtN = 0, stunN = 0, counted = 0;
			foreach (GameObject b in victims)
			{
				if (!GameObject.Validate(b) || b.CurrentCell == null)
					continue;
				counted++;
				if (RedoubtCheb(b.CurrentCell, origin) > RedoubtCheb(starts[b], origin))
					movedN++;
				if (b.GetStat("Hitpoints").Value < hp[b])
					hurtN++;
				if (b.HasEffect(typeof(XRL.World.Effects.Stun)))
					stunN++;
			}
			bool mothSpared = !mothFlying || (GameObject.Validate(moth) && moth.CurrentCell == mothCell && moth.GetStat("Hitpoints").Value == mothHP);
			bool lootHeld = loot == null || (GameObject.Validate(loot) && loot.CurrentCell == lootCell);
			bool pass = counted == victims.Count && movedN == counted && hurtN == counted && stunN >= 1 && mothSpared && lootHeld;
			Report("redoubt.push", pass,
				$"L{LV} wave vs {victims.Count} baboons at distance 1: {movedN} pushed outward (want all), {hurtN} damaged (want all), {stunN} stunned (moved-only rule, stepped DC {XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure.GetStunSaveDC(LV)}; >=1 wanted, all-save flake is rare)" +
				(moth == null ? "; no room for the flyer probe"
					: !mothFlying ? "; flyer probe INERT (moth would not take off -- exemption untested this run)"
					: $"; flyer {(mothSpared ? "spared untouched" : "TOUCHED -- ground-wave exemption broken")}") +
				(loot != null ? $"; loose Small Stone {(lootHeld ? "unmoved (loot rule holds)" : "MOVED OR DESTROYED -- loot rule broken")}" : "; no room for the loot probe"));
			if (GameObject.Validate(loot))
				loot.Obliterate();
			RedoubtCleanup(NewSince(zone, before));
			SweepSpawns(zone);
		}

		private static GameObject RedoubtRigWall(string bp, Cell c, int av = -1, int hp = -1)
		{
			GameObject w = GameObject.Create(bp);
			if (av >= 0)
				w.GetStat("AV").BaseValue = av;
			if (hp >= 0)
				w.GetStat("Hitpoints").BaseValue = hp;
			c.AddObject(w);
			return w;
		}

		/// <summary>Carve under the PULSE PASS (per-pulse bites RULED 2026-08-08: every wall in
		/// a pulse's coverage takes one carve-as-attack bite; the depth ladder and per-ray
		/// budget retired, repetition digs). Glass-jaw sandstones (AV 0, 1 HP: any penetration
		/// fells them, and vs AV 0 the wave always penetrates) make the CHAIN asserts
		/// deterministic: a two-file falls one wall per pulse at ANY level (pulse 1 fells ring
		/// 1, pulse 2 surges the breach and fells ring 2), and at L4 a three-file falls
		/// entirely by pulse 3 -- whose ring IS the rim since the radius-3 re-dial (RULED
		/// 2026-08-10), so the three-file's last wall doubles as the rim-bite probe. The
		/// ring-4 glass-jaw INVERTED same re-dial: it now sits BEYOND everything, and at 1 HP
		/// any bite would fell it -- it must STAND, proving the front never bites past
		/// coverage (the old front-bite window is empty at radius 3). The
		/// armored survivor (AV 999, bitten up to 3 times, zero pens each) must STAND
		/// untouched -- the leftover half of the ruling. CLAMP REMOVED (L20 pass, RULED
		/// 2026-08-09 "potentially fine it destroys all wall types"): glass-jaw WOOD falls
		/// (Soft), AV-0 METAL now FALLS too (armor, not class, is the boundary), native-AV
		/// metal still holds untouched (PV 18 vs AV 30, the zero-pen path).</summary>
		private static void RedoubtCarve(GameObject ply, Zone zone, Cell origin, XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure fis)
		{
			Cell e1 = origin.GetCellFromDirection("E");
			Cell e2 = e1?.GetCellFromDirection("E");
			Cell e3 = e2?.GetCellFromDirection("E");
			Cell e4 = e3?.GetCellFromDirection("E");
			Cell w1 = origin.GetCellFromDirection("W");
			Cell n1 = origin.GetCellFromDirection("N");
			Cell s1 = origin.GetCellFromDirection("S");
			if (e1 == null || e2 == null || e3 == null || e4 == null || w1 == null || n1 == null || s1 == null
				|| e1.IsSolid() || e2.IsSolid() || e3.IsSolid() || e4.IsSolid() || w1.IsSolid() || n1.IsSolid() || s1.IsSolid())
			{
				Report("redoubt.carve", false, "SETUP FAIL - need open E1/E2/E3/E4/W1/N1/S1 cells");
				return;
			}
			List<string> fails = new List<string>();

			// cast A -- L1: the pulse chain replaces the depth budget, so BOTH glass-jaws of
			// the two-file fall (one per pulse) even at band 1
			GameObject sand1 = RedoubtRigWall(NATURAL_WALL_BP, e1, 0, 1);
			Cleo_TerraFirma_StoneshaperWallProperties props = sand1.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			if (props == null || props.MaterialType != "stone" || !props.Natural)
			{
				Report("redoubt.carve", false, $"SETUP FAIL - spawned {NATURAL_WALL_BP} is not classified natural stone (Merges Natural flag missing or unmapped)");
				sand1.Obliterate();
				return;
			}
			GameObject sand2 = RedoubtRigWall(NATURAL_WALL_BP, e2, 0, 1);
			GameObject survivor = RedoubtRigWall(NATURAL_WALL_BP, s1, 999);
			int survivorHP = survivor.GetStat("Hitpoints").Value;
			GameObject metal = RedoubtRigWall(METAL_WALL_BP, w1, 0); // AV 0: pens guaranteed -- clamp REMOVED (L20 pass), so it must FALL now
			GameObject wood = RedoubtRigWall(WOOD_WALL_BP, n1, 0, 1); // glass-jaw Soft: must fall to the wave
			HashSet<GameObject> before = SnapshotZone(zone);
			RedoubtCast(ply, fis, 1);
			if (GameObject.Validate(sand1) && sand1.CurrentCell != null) fails.Add("L1: first glass-jaw sandstone NOT felled");
			if (GameObject.Validate(sand2) && sand2.CurrentCell != null) fails.Add("L1: second glass-jaw sandstone NOT felled (pulse 2 must surge the breach and bite ring 2)");
			if (!GameObject.Validate(survivor) || survivor.CurrentCell != s1) fails.Add("L1: AV-999 survivor FELL (armor must hold)");
			else if (survivor.GetStat("Hitpoints").Value != survivorHP) fails.Add("L1: AV-999 survivor took damage on zero penetrations");
			if (GameObject.Validate(metal) && metal.CurrentCell != null) fails.Add("L1: AV-0 METAL wall STANDING (clamp removed 2026-08-09 -- all wall types fall by AV+HP, and AV 0 guarantees felling pens)");
			if (GameObject.Validate(wood) && wood.CurrentCell != null) fails.Add("L1: glass-jaw WOOD wall NOT felled (Soft class takes the full attack)");
			RedoubtCleanup(NewSince(zone, before));
			if (GameObject.Validate(sand2)) sand2.Obliterate();
			if (GameObject.Validate(wood)) wood.Obliterate();
			if (GameObject.Validate(metal)) metal.Obliterate(); // cast B wants the native-AV holds probe, not the clamp probe

			// cast B -- L4: the three-file falls entirely (one wall per pulse; ring 3 IS the
			// rim now), the ring-4 glass-jaw sits beyond the radius-3 rim and must STAND
			// untouched (no front overreach), the survivor stands for the at-all-levels claim,
			// and a NATIVE-AV metal wall (AV 30 vs PV 18: zero pens ever) probes the
			// attacked-but-untouched path of the Hard class
			GameObject b1 = RedoubtRigWall(NATURAL_WALL_BP, e1, 0, 1);
			GameObject b2 = RedoubtRigWall(NATURAL_WALL_BP, e2, 0, 1);
			GameObject b3 = RedoubtRigWall(NATURAL_WALL_BP, e3, 0, 1);
			GameObject b4 = RedoubtRigWall(NATURAL_WALL_BP, e4, 0, 1); // ring 4: BEYOND the radius-3 rim -- the no-overreach probe (1 HP: any bite fells it)
			metal = RedoubtRigWall(METAL_WALL_BP, w1);
			int metalHP = metal.GetStat("Hitpoints").Value;
			if (!GameObject.Validate(survivor) || survivor.CurrentCell != s1)
			{
				survivor = RedoubtRigWall(NATURAL_WALL_BP, s1, 999);
				survivorHP = survivor.GetStat("Hitpoints").Value;
			}
			before = SnapshotZone(zone);
			RedoubtCast(ply, fis, 4);
			if (GameObject.Validate(b1) && b1.CurrentCell != null) fails.Add("L4: first glass-jaw sandstone NOT felled");
			if (GameObject.Validate(b2) && b2.CurrentCell != null) fails.Add("L4: second glass-jaw sandstone NOT felled (pulse chain broken at ring 2)");
			if (GameObject.Validate(b3) && b3.CurrentCell != null) fails.Add("L4: third glass-jaw sandstone NOT felled (pulse chain broken at ring 3)");
			if (!GameObject.Validate(b4) || b4.CurrentCell == null) fails.Add("L4: ring-4 glass-jaw sandstone TOUCHED (it sits beyond the radius-3 rim -- the front must not bite past coverage)");
			if (!GameObject.Validate(survivor) || survivor.CurrentCell != s1 || survivor.GetStat("Hitpoints").Value != survivorHP) fails.Add("L4: AV-999 survivor fell or took damage");
			// "untouched" retired with the clamp (first pulse boot 2026-08-09): exploding pen
			// dice give PV 18 a ~12% chip per bite vs AV 30, and three bites a cast make chips
			// ROUTINE -- the wall must STAND (felling needs ~3 pens in one cast, <1%), but
			// damage is legal now. Chip-vs-clean is reported, not asserted.
			if (!GameObject.Validate(metal) || metal.CurrentCell != w1) fails.Add("L4: native-AV METAL wall FELL (AV 30 vs PV 18 must stand; felling odds are sub-1%)");
			RedoubtCleanup(NewSince(zone, before));
			if (GameObject.Validate(metal)) metal.Obliterate();
			if (GameObject.Validate(survivor)) survivor.Obliterate();
			if (GameObject.Validate(b4)) b4.Obliterate(); // expected to STAND now (radius-3 re-dial) -- clear it
			Report("redoubt.carve", fails.Count == 0,
				fails.Count == 0
					? "per-pulse bites hold: the L1 two-file fell one wall per pulse, the L4 three-file fell entirely (ring 3 = the rim since the radius-3 re-dial), the ring-4 wall stood untouched beyond the rim (no front overreach); the AV-999 survivor stood untouched at both bands (armor is the boundary); clamp-removed rules verified -- glass-jaw wood AND AV-0 metal both FELL, native-AV metal untouched on zero pens"
					: string.Join("; ", fails.ToArray()));
		}

		/// <summary>Walls over water at L7 (band 3, wall share 55±12): every open rim cell is
		/// flooded first, so raised walls MUST displace their pools (or downgrade to rubble
		/// where undammable). Asserts: the zone-wide open-liquid census is unchanged (water is
		/// currency, never destroyed), and no wall shares a tile with standing liquid.</summary>
		private static void RedoubtWater(GameObject ply, Zone zone, Cell origin, XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure fis)
		{
			const int LV = 7;
			int radius = XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure.GetRingRadius(LV);
			List<GameObject> puddles = new List<GameObject>();
			HashSet<Cell> wet = new HashSet<Cell>();
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dy = -radius; dy <= radius; dy++)
				{
					if (!RedoubtRim(dx, dy, radius))
						continue;
					Cell c = zone.GetCell(origin.X + dx, origin.Y + dy);
					if (c == null || c.IsSolid() || c.HasObjectWithPart("Combat") || RedoubtHasOpenLiquid(c))
						continue;
					GameObject pool = GameObject.Create(PUDDLE_BP);
					c.AddObject(pool);
					puddles.Add(pool);
					wet.Add(c);
				}
			}
			if (wet.Count < 6)
			{
				Report("redoubt.water", false, $"SETUP FAIL - only {wet.Count} open rim cells to flood (want >=6)");
				foreach (GameObject p in puddles)
					if (GameObject.Validate(p))
						p.Obliterate();
				return;
			}
			int liquidsBefore = RedoubtCountLiquids(zone);
			HashSet<GameObject> before = SnapshotZone(zone);
			RedoubtCast(ply, fis, LV);
			int liquidsAfter = RedoubtCountLiquids(zone);
			List<GameObject> fresh = NewSince(zone, before);
			int wallsTotal = 0, wallsOnWet = 0, cohabit = 0;
			foreach (GameObject o in fresh)
			{
				if (o.Blueprint != FABRIC_WALL_BP || o.CurrentCell == null)
					continue;
				wallsTotal++;
				if (wet.Contains(o.CurrentCell))
					wallsOnWet++;
				if (RedoubtHasOpenLiquid(o.CurrentCell))
					cohabit++;
			}
			bool conserved = liquidsAfter == liquidsBefore;
			bool pass = conserved && cohabit == 0 && wallsOnWet >= 1;
			Report("redoubt.water", pass,
				$"L{LV} cast over a flooded rim ({wet.Count} puddles laid): open-liquid census {liquidsBefore}->{liquidsAfter} ({(conserved ? "water conserved" : "WATER DESTROYED OR DUPLICATED")}), {wallsTotal} walls raised, {wallsOnWet} on formerly wet cells (want >=1), {cohabit} sharing a tile with standing liquid (want 0: displaced or downgraded)");
			RedoubtCleanup(fresh);
			foreach (GameObject p in puddles)
				if (GameObject.Validate(p))
					p.Obliterate();
		}

		// ---------------------------------------------------------------- round 3, turn-driven setups

		internal static void SetupQuickApe(GameObject ply, string queue)
		{
			// held 2026-09-30: the creature and its TimedForm moved to quarantine/ (original in
			// quarantine/TerraFirma/Scripts/_TestHarness-held.cs)
			Report("quick.ape", false, "HELD - moved to quarantine/ 2026-09-30");
			RunNext(queue, ply);
		}

		[WishCommand(Command = "tfdull")]
		public static void Dull()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			// RETIRED 2026-07-25 (fragility ruling: the dull clock is CUT -- "a stone keeps until
			// it goes off"). The scenario's PASS stands in the ledger as history; the code path it
			// proved is dormant (DullAfter 0 mod-wide). SetupDull kept below in case the clock ever
			// returns as a longer fuse.
			MessageQueue.AddPlayerMessage("tfdull RETIRED: the dull clock was cut 2026-07-25 (a stone keeps until it goes off). Ledger entry stands as history.", "W");
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
			// held 2026-09-30: the creature and its TimedForm moved to quarantine/ (original in
			// quarantine/TerraFirma/Scripts/_TestHarness-held.cs)
			Report("form", false, "HELD - moved to quarantine/ 2026-09-30");
			RunNext(queue, ply);
		}

		internal static void RunNext(string queue, GameObject ply)
		{
			if (queue.IsNullOrEmpty() || ply == null)
			{
				FinishBatch(); // end of a tfbatch chain (or a lost-state abort dropped the queue); no-op outside a batch
				return;
			}
			int comma = queue.IndexOf(',');
			string next = comma < 0 ? queue : queue.Substring(0, comma);
			string rest = comma < 0 ? "" : queue.Substring(comma + 1);
			switch (next)
			{
				case "form": SetupForm(ply, rest); break;
				case "scav": SetupScav(ply, rest); break;
				case "quickape": SetupQuickApe(ply, rest); break;
				case "shape": SetupShape(ply, rest); break;
				case "fis": SetupFis(ply, rest); break;
			}
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
				// DIRECTED kill (root cause found 2026-08-09, decomp Kill.cs:506): plain
				// Brain.Target makes a NON-directed Kill goal that self-pops vs targets the
				// baboon isn't feeling-hostile to (figurine rep!) -- "arena-dependent
				// engagement" was this. Directed skips the hostility check.
				baboon.Brain?.WantToKill(ply, "harness", Directed: true);
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

		// quick-loop tally (2026-08-07 sprucing): tfbatch counts its Reports and closes with ONE
		// grep-able verdict line, so a boot-and-test pass is launch -> tfbatch -> read the verdict
		internal static int BatchPass;
		internal static int BatchFail;
		internal static readonly List<string> BatchFails = new List<string>();
		internal static bool BatchActive;

		internal static void Report(string scenario, bool pass, string detail)
		{
			if (BatchActive)
			{
				if (pass) BatchPass++;
				else { BatchFail++; BatchFails.Add(scenario); }
			}
			Helpers.VerifyLog("AUTO", $"SCENARIO {scenario}: {(pass ? "PASS" : "FAIL")} - {detail}");
			MessageQueue.AddPlayerMessage($"Harness {scenario}: {(pass ? "&GPASS" : "&RFAIL")}&y - {detail}");
		}

		internal static void FinishBatch()
		{
			if (!BatchActive)
				return;
			BatchActive = false;
			string verdict = BatchFail == 0 ? "ALL GREEN" : "FAILS: " + string.Join(", ", BatchFails);
			Helpers.VerifyLog("AUTO", $"BATCH COMPLETE: {BatchPass} pass / {BatchFail} fail -- {verdict}");
			MessageQueue.AddPlayerMessage($"Harness batch complete: {BatchPass} pass / {BatchFail} fail -- {verdict}", BatchFail == 0 ? "G" : "R");
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
		[NonSerialized] private GameObject FormCreature;
		[NonSerialized] private int FigurineBaseline;
		[NonSerialized] private GameObject ApeCreature;
		[NonSerialized] private int RubbleBaseline;
		[NonSerialized] private GameObject DullRock;
		[NonSerialized] private int PebblesBaseline;
		[NonSerialized] private ActivatedAbilityEntry ShapeEntry;
		[NonSerialized] private int ShapeCdBeforeKill;
		[NonSerialized] private GameObject FisCaster;
		[NonSerialized] private int FisRocksBaseline;
		[NonSerialized] private GameObject[] AiStatues;
		[NonSerialized] private GameObject[] AiDummies;
		[NonSerialized] private string[] AiKeys;
		[NonSerialized] private Cell[] AiStartCells;
		[NonSerialized] private int[] AiDummyHpBase;
		[NonSerialized] private bool[] AiEverMoved;
		[NonSerialized] private bool[] AiKillMoved;
		[NonSerialized] private int[] AiKillDmg;
		[NonSerialized] private int[] AiFollowBaseDist;

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

		public void InitStatueAI(GameObject[] statues, GameObject[] dummies, string[] keys, Cell[] startCells, int[] dummyHpBase)
		{
			AiStatues = statues;
			AiDummies = dummies;
			AiKeys = keys;
			AiStartCells = startCells;
			AiDummyHpBase = dummyHpBase;
			AiEverMoved = new bool[statues.Length];
			AiKillMoved = new bool[statues.Length];
			AiKillDmg = new int[statues.Length];
			AiFollowBaseDist = new int[statues.Length];
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
					Cleo.TerraFirma.Scripts.TestHarnessWishes.FinishBatch(); // the abort drops the queue; close the tally so the verdict line still lands
					return base.HandleEvent(E);
				}
				if (Scenario == "scav" || Scenario == "fis" || Scenario == "statueai")
					ReassertHostileLocks(ParentObject);
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

		/// <summary>Live-fire find, first pulse boot 2026-08-09 (scav: 0 throws in 116
		/// baboon-turns, moveGoal flapping): a one-time Brain.Target assignment DECAYS as the
		/// AI re-evaluates, and a passive target dummy never re-earns aggro -- the armed
		/// baboons drifted into wander. The lock is re-asserted every tick now; these
		/// scenarios measure throw cadence and cast advertisement, not aggro persistence.</summary>
		private void ReassertHostileLocks(GameObject owner)
		{
			// telemetry every 5th tick (second pulse boot 2026-08-09, still 0 casts / 0
			// throws): dist + Kill-goal survival + top goal name per actor -- Brain.Target
			// READS from the Kill goal stack (decomp Brain.cs:512), so target=NULL here means
			// the Kill goal died since the last re-lock, and the top-goal name says what ate it
			bool telemetry = TurnsRemaining % 5 == 0;
			if (Scenario == "statueai")
			{
				// phase 1 (12 ticks): Directed-kill drive (the fis/scav idiom, Kill.cs:506
				// lesson). phase 2 (8 ticks): NATURAL follow-back -- goals cleared, quarries
				// removed, nothing asserted; a companion 4-5 cells from its leader should
				// close in on its own. The play symptom lives in phase 2's territory.
				bool killPhase = TurnsRemaining > 8;
				for (int i = 0; i < AiStatues.Length; i++)
				{
					GameObject statue = AiStatues[i];
					if (!GameObject.Validate(statue) || statue.CurrentCell == null)
						continue;
					if (statue.CurrentCell != AiStartCells[i])
						AiEverMoved[i] = true;
					GameObject quarry = AiDummies[i];
					if (TurnsRemaining == 8)
					{
						// phase boundary: bank the kill verdict, strip the combat scaffolding
						AiKillMoved[i] = AiEverMoved[i];
						AiKillDmg[i] = GameObject.Validate(quarry) ? AiDummyHpBase[i] - quarry.GetStat("Hitpoints").Value : AiDummyHpBase[i];
						AiFollowBaseDist[i] = statue.DistanceTo(owner);
						statue.Brain?.Goals.Clear();
						if (GameObject.Validate(quarry))
							quarry.Obliterate();
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("AUTO", $"statueai.{AiKeys[i]} phase 2 begins: goals cleared, quarry removed, distToLeader={AiFollowBaseDist[i]}");
						continue;
					}
					if (killPhase)
					{
						if (statue.Brain != null && GameObject.Validate(quarry) && statue.Brain.Target == null)
							statue.Brain.WantToKill(quarry, "harness", Directed: true);
						if (TurnsRemaining % 3 == 0)
						{
							string goal = statue.Brain != null && statue.Brain.Goals.Count > 0 ? statue.Brain.Goals.Peek().GetType().Name : "none";
							Cleo.TerraFirma.Scripts.Helpers.VerifyLog("AUTO", $"statueai.{AiKeys[i]} kill tick {TurnsTotal - TurnsRemaining}: cell=({statue.CurrentCell.X},{statue.CurrentCell.Y}), energy={statue.GetStatValue("Energy")}, target={(statue.Brain?.Target?.Blueprint ?? "NULL")}, topGoal={goal}, distToQuarry={(GameObject.Validate(quarry) ? statue.DistanceTo(quarry) : -1)}");
						}
					}
					else if (TurnsRemaining % 2 == 0)
					{
						string goal = statue.Brain != null && statue.Brain.Goals.Count > 0 ? statue.Brain.Goals.Peek().GetType().Name : "none";
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("AUTO", $"statueai.{AiKeys[i]} follow tick {TurnsTotal - TurnsRemaining}: distToLeader={statue.DistanceTo(owner)}, topGoal={goal}");
					}
				}
				return;
			}
			if (Scenario == "fis")
			{
				if (FisCaster?.Brain != null)
				{
					if (FisCaster.Brain.Target == null)
						FisCaster.Brain.WantToKill(owner, "harness", Directed: true); // Directed: survives the hostility check (Kill.cs:506)
					if (telemetry)
					{
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("FIS", $"npc telemetry: {FisCaster.Blueprint} dist {FisCaster.DistanceTo(owner)}, target={(FisCaster.Brain.Target?.Blueprint ?? "NULL")}, topGoal={(FisCaster.Brain.Goals.Count > 0 ? FisCaster.Brain.Goals.Peek().GetType().Name : "none")}");
						// end-to-end probe (third pulse boot 2026-08-09: zero gate lines
						// despite a live Kill goal inside coverage): call the event chain
						// DIRECTLY. If the mutation's gate line fires here with a command in
						// the list, the chain works and the AI's tactic loop simply never
						// consults abilities for this actor; if it stays silent, the event
						// never reaches the mutation part at all.
						var cmds = AIGetOffensiveAbilityListEvent.GetFor(FisCaster, owner);
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("FIS", $"npc probe: direct GetFor -> {(cmds?.Count ?? -1)} command(s)");
					}
				}
				return;
			}
			Zone z = owner?.CurrentZone;
			if (z == null)
				return;
			foreach (GameObject o in z.GetObjects())
			{
				if (o.HasPart<Cleo_TerraFirma_TestThrowCounter>() && o.Brain != null)
				{
					if (o.Brain.Target == null)
						o.Brain.WantToKill(owner, "harness", Directed: true); // Directed: survives the hostility check (Kill.cs:506)
					if (telemetry)
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SCAV", $"telemetry: {o.Blueprint} dist {o.DistanceTo(owner)}, target={(o.Brain.Target?.Blueprint ?? "NULL")}, topGoal={(o.Brain.Goals.Count > 0 ? o.Brain.Goals.Peek().GetType().Name : "none")}");
				}
			}
		}

		private void Finish(GameObject owner)
		{
			if (Scenario == "scav")
				FinishScav(owner);
			else if (Scenario == "form")
				FinishForm(owner);
			else if (Scenario == "quickape")
				FinishQuickApe(owner);
			else if (Scenario == "dull")
				FinishDull(owner);
			else if (Scenario == "shape")
				FinishShape();
			else if (Scenario == "fis")
				FinishFis(owner);
			else if (Scenario == "statueai")
				FinishStatueAI(owner);
			Cleo.TerraFirma.Scripts.TestHarnessWishes.SweepSpawns(owner.CurrentZone);
			Cleo.TerraFirma.Scripts.TestHarnessWishes.RunNext(Queue, owner);
		}

		private void FinishStatueAI(GameObject owner)
		{
			for (int i = 0; i < AiStatues.Length; i++)
			{
				GameObject statue = AiStatues[i];
				if (!GameObject.Validate(statue))
				{
					Cleo.TerraFirma.Scripts.TestHarnessWishes.Report($"statueai.{AiKeys[i]}", false, "statue DIED or vanished mid-run (quarry fought back too hard?)");
					continue;
				}
				bool killOk = AiKillMoved[i] && AiKillDmg[i] > 0;
				int endDist = statue.DistanceTo(owner);
				bool followOk = endDist <= 2 || endDist < AiFollowBaseDist[i];
				Cleo.TerraFirma.Scripts.TestHarnessWishes.Report($"statueai.{AiKeys[i]}", killOk && followOk,
					$"kill phase (12t, Directed goal 3 away): {(AiKillMoved[i] ? "MOVED" : "never left its tile")}, quarry damage {AiKillDmg[i]}; " +
					$"follow phase (8t, no goals): distToLeader {AiFollowBaseDist[i]}->{endDist} {(followOk ? "(follows)" : "(DOES NOT FOLLOW)")}. " +
					(killOk && followOk ? "statue acts and follows -- this muse is fine" : "read this statue's snapshot + tick lines for the dead dial"));
			}
		}

		private void FinishFis(GameObject owner)
		{
			// REDOUBT shape (pivot 2026-08-02): a cast now deposits ring fabric (rubble/crumbled/
			// wall/crack/salvage), not a shower of loose stones -- count ring pieces by prefix.
			bool casterAlive = GameObject.Validate(FisCaster);
			int piecesNow = Cleo.TerraFirma.Scripts.TestHarnessWishes.CountRingPieces(owner.CurrentZone);
			int delta = piecesNow - FisRocksBaseline;
			bool pass = casterAlive && delta >= 5;
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("fis", pass,
				$"NPC redoubt caster over {TurnsTotal} turns: ring pieces {FisRocksBaseline}->{piecesNow} (+{delta}, want >=5 = a wave happened and deposited a rim). A zero here answers fis-npc: no AI advertisement on the mutation.");
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
