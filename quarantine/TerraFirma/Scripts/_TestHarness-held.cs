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
