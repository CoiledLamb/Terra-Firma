// Held 2026-09-30 from TerraFirma/Scripts/_TestHarness.cs (class TestHarnessWishes).
// Paste back into that class when Part_TimedForm.cs / Part_FungalLottery.cs return.

		[WishCommand(Command = "tfspore")]
		public static void SporePeek()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			XRLGame game = The.Game;
			if (game == null)
			{
				MessageQueue.AddPlayerMessage("tfspore: no game loaded.", "R");
				return;
			}
			string perm = game.GetStringGameState(Cleo_TerraFirma_FungalLottery.STATE_KEY);
			if (perm.IsNullOrEmpty())
			{
				MessageQueue.AddPlayerMessage("SPORE state: EMPTY -- no permutation stored (no fungal rock minted yet this boot; this read did NOT roll one).", "W");
				Helpers.VerifyLog("SPORE", "peek (tfspore): stored state EMPTY (pure read, no roll)");
				return;
			}
			var decode = new System.Text.StringBuilder();
			for (int i = 0; i < perm.Length && i < Cleo_TerraFirma_FungalLottery.GASES.Length; i++)
			{
				int g = perm[i] - '0';
				if (g < 0 || g >= Cleo_TerraFirma_FungalLottery.GASES.Length)
					continue;
				if (decode.Length > 0)
					decode.Append(", ");
				decode.Append($"slot {i} -> {Cleo_TerraFirma_FungalLottery.GASES[g]}");
			}
			MessageQueue.AddPlayerMessage($"SPORE state: permutation={perm} ({decode})", "W");
			Helpers.VerifyLog("SPORE", $"peek (tfspore): stored permutation={perm} (pure read)");
		}


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

// tfcrack and its constant, held 2026-09-30 with the crack
		private const string CRACK_BP = "Cleo_TerraFirma_Rock_Crack";

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


// quick.rep, cut from QuickSync 2026-09-30 with the baboon figurine item
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


// SetupDull (and its "dull" case in RunNext), held 2026-09-30 with FallenRockPart
		/// <summary>The anti-hoarding clock, never before verified end-to-end (nobody carries a
		/// rock 1000 turns on purpose): wind DullAfter to 2, carry it, assert crumble-to-pebbles.</summary>
		internal static void SetupDull(GameObject ply, string queue)
		{
			if (ply?.CurrentCell == null)
				return;
			GameObject rock = GameObject.Create(DULLTEST_BP);
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

