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
		private const string SCARY_BP = "Cleo_TerraFirma_Rock_Scary"; // display: grimslate (tfmine's test mine; was dazestone until its 2026-07-25 cut)
		private const string TANGLE_BP = "Cleo_TerraFirma_Rock_Bola";      // display: tanglelith (name pending mini-slate)
		private const string TAR_PATCH_BP = "Cleo_TerraFirma_TarPatch";
		private const string MOTH_BP = "Glowmoth";

		[WishCommand(Command = "tfverify")]
		public static void Help()
		{
			MessageQueue.AddPlayerMessage("TF harness (BETA TRIM 2026-08-02) -- instant: tfquick (pockets+rep), tffeedback (2026-08-07 batch: Husangeion name/bleed, statue popup tag, rough-vs-tier look-text, gaze-wall lockout belt), tfcarom, tfcrack, tfredoubt (8 scripted casts: ring census by band, push/stun, carve depth, water displacement -- chews nearby natural stone). Turn-driven: tfform (5t), tfshape (Stoneshaper, 3t; incl. shape.mourning swap-quiet stamp), tffis (NPC caster, 20t), tfscav (30t, you are the target). tfbatch = the surviving set, ~60 turns standing still, ends with ONE 'BATCH COMPLETE' verdict line for the quick boot-and-test loop. Stage-for-eyes: tfgazewall (plain + animated holo wall, you cast the gaze). Peek: tfspore (pure read). BETA-HELD dev doors (test held content, run by hand only): tfstreak, tfstrange, tfspill, tfpuddle, tfmine, tftar, tfhopper, tfdrain, tfdevour + tfeat (need kitchenrock's Lithovore grant), quickape/dull via queue.", "W");
			if (!Helpers.VERIFY_WATCH)
				MessageQueue.AddPlayerMessage("VERIFY_WATCH is OFF -- the harness is disabled.", "R");
		}

		/// <summary>Fungal-lottery state peek (user request 2026-07-25, for the save-quit-reload
		/// experiment): a PURE READ of the stored permutation -- prints to chat and the watcher,
		/// never rolls, never writes, so it cannot contaminate the experiment. EMPTY is itself an
		/// answer: no fungal rock has been minted this boot (creation is what rolls the lottery).</summary>
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
		/// Held-content scenarios (streak/strange/spill/puddle/mine/tar/devour/eat/hopper/
		/// quickape/drain) left compiled as hand-run dev doors; restore them to this chain with
		/// the catalog. Pre-trim chain at tag fissure-v2-grenade.</summary>
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
			CaromSync();
			CrackSync();
			RedoubtSync();
			SetupForm(The.Player, "shape,fis,scav");
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

			/* BETA-HELD (redoubt pivot, RULED 2026-08-02): quick.heal + quick.cage test held rocks
			   (healstone, aegischist). Restore with the catalog.
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
			BETA-HELD end */
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

		/// <summary>Caromcobble trip statistics: 40 forced crossings of the peel in one wish
		/// (ObjectEnteredCellEvent fires synchronously per entry). StepChance 50 (re-ruled
		/// 2026-07-25, was 25) -> ~20 expected trips; accept 9-31 (binomial tails under 0.05%).
		/// HoldChance 100 -> the peel NEVER breaks.</summary>
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
			Report("carom", alive && procs >= 9 && procs <= 31,
				$"40 forced crossings: {procs} trip(s) (StepChance 50 -> expect ~20, accept 9-31), peel {(alive ? "intact through every proc (HoldChance 100)" : "BROKE OR MOVED")}");
			if (GameObject.Validate(rock)) rock.Obliterate();
			if (GameObject.Validate(victim)) victim.Obliterate();
		}

		/// <summary>Tanglelith tar redesign (RULED 2026-07-25), three asserts in one wish:
		/// step discharge (patch underfoot + stepper stuck + stone spent) · tar persistence
		/// (the patch SURVIVES its catches -- the web contrast; stick rolls are save-based, so
		/// sticks are counted over 6 crossings, not asserted per-crossing) · flyer-downing
		/// (a thrown hit forces a flying Glowmoth out of the air; conditional on the moth
		/// actually flying at throw time -- the detail line says which path ran).</summary>
		[WishCommand(Command = "tftar")]
		public static void TarSync()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			Zone zone = origin.ParentZone;
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 2) { Report("tar", false, "SETUP FAIL - need two open adjacent cells"); return; }
			int patchesBefore = CountInZone(zone, TAR_PATCH_BP);
			// 1: step discharge -- patch underfoot, stepper stuck, stone spent
			GameObject rock = GameObject.Create(TANGLE_BP);
			rock.SetIntProperty(SPAWN_MARK, 1);
			open[0].AddObject(rock);
			GameObject walker = GameObject.Create(BABOON_BP);
			walker.SetIntProperty(SPAWN_MARK, 1);
			walker.GetStat("Hitpoints").BaseValue = 100;
			open[1].AddObject(walker);
			walker.DirectMoveTo(open[0], 0, Forced: true, IgnoreCombat: true);
			bool stuckOnStep = walker.HasEffect(typeof(XRL.World.Effects.Stuck));
			bool spent = !GameObject.Validate(rock);
			int patchesMade = CountInZone(zone, TAR_PATCH_BP) - patchesBefore;
			Report("tar.step", stuckOnStep && spent && patchesMade >= 1,
				$"step discharge: stepper {(stuckOnStep ? "STUCK" : "free (want stuck)")}, stone {(spent ? "spent (discharge law)" : "SURVIVED (should be gone)")}, patches +{patchesMade} (want >=1)");
			// 2: persistence -- the patch keeps catching and survives every catch
			int sticks = 0;
			for (int i = 0; i < 6 && GameObject.Validate(walker); i++)
			{
				walker.RemoveEffect<XRL.World.Effects.Stuck>();
				walker.DirectMoveTo(open[1], 0, Forced: true, IgnoreCombat: true);
				walker.DirectMoveTo(open[0], 0, Forced: true, IgnoreCombat: true);
				if (walker.HasEffect(typeof(XRL.World.Effects.Stuck)))
					sticks++;
			}
			walker.RemoveEffect<XRL.World.Effects.Stuck>();
			bool patchAlive = CountInZone(zone, TAR_PATCH_BP) - patchesBefore >= patchesMade;
			Report("tar.holds", patchAlive && sticks >= 1,
				$"persistence: {sticks}/6 re-crossings stuck (save-based; want >=1), patch {(patchAlive ? "SURVIVES its catches (tar keeps catching -- the web contrast)" : "GONE (tar must not spend itself)")}");
			if (GameObject.Validate(walker)) walker.Obliterate();
			// 3: the anti-air shot -- a thrown hit forces a flyer down into the tar
			GameObject moth = GameObject.Create(MOTH_BP);
			moth.SetIntProperty(SPAWN_MARK, 1);
			open[1].AddObject(moth);
			bool wasFlying = moth.IsFlying;
			bool mothStuck = false, downed = false;
			for (int t = 0; t < 3 && GameObject.Validate(moth) && !mothStuck; t++)
			{
				ThrowAt(ply, TANGLE_BP, moth); // retries: the agility connect roll survives perfect accuracy
				mothStuck = moth.HasEffect(typeof(XRL.World.Effects.Stuck));
			}
			downed = wasFlying && GameObject.Validate(moth) && !moth.IsFlying;
			Report("tar.flyer", mothStuck && (!wasFlying || downed), wasFlying
				? $"flying Glowmoth: {(downed ? "FORCED DOWN out of the air" : "STILL AIRBORNE (Flight.Fall failed)")}, {(mothStuck ? "stuck in the tar" : "not stuck")}"
				: $"Glowmoth was NOT flying at throw time -- grounding path unexercised (limit for the ledger); {(mothStuck ? "stuck applied" : "and not stuck either")}");
			if (GameObject.Validate(moth)) moth.Obliterate();
			// sweep: tar patches have no inventory and only this mod mints them
			foreach (Cell c in zone.GetCells())
				foreach (GameObject o in new List<GameObject>(c.Objects))
					if (o.Blueprint == TAR_PATCH_BP)
						o.Obliterate();
		}

		/// <summary>Discharge law + party gate in one wish (fragility system RULED 2026-07-25):
		/// a player-owned grimslate must NOT trigger on an allied baboon crossing it (party gate),
		/// then must fire on a non-party baboon's FIRST crossing (StepChance 100) and be consumed
		/// by its own discharge (HoldChance 0). (Test mine was dazestone until its cut.)</summary>
		[WishCommand(Command = "tfmine")]
		public static void MineSync()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = origin.GetLocalAdjacentCells();
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 2) { Report("mine", false, "SETUP FAIL - need two open adjacent cells"); return; }
			GameObject rock = GameObject.Create(SCARY_BP);
			rock.SetIntProperty(SPAWN_MARK, 1);
			Cleo_TerraFirma_FallenRockPart part = rock.GetPart<Cleo_TerraFirma_FallenRockPart>();
			if (part == null) { Report("mine", false, "SETUP FAIL - grimslate has no FallenRockPart"); rock.Obliterate(); return; }
			part.Owner = ply;
			open[0].AddObject(rock);
			// half 1: an ALLIED crossing must pass over safely (party gate)
			GameObject friendly = GameObject.Create(BABOON_BP);
			friendly.SetIntProperty(SPAWN_MARK, 1);
			friendly.GetStat("Hitpoints").BaseValue = 100;
			open[1].AddObject(friendly);
			friendly.Brain?.SetAlliedLeader<XRL.World.AI.AllySummon>(ply, 0, Silent: true);
			friendly.DirectMoveTo(open[0], 0, Forced: true, IgnoreCombat: true);
			bool allyTripped = friendly.HasEffect(typeof(XRL.World.Effects.Terrified));
			bool intactAfterAlly = GameObject.Validate(rock) && rock.CurrentCell == open[0];
			friendly.DirectMoveTo(open[1], 0, Forced: true, IgnoreCombat: true);
			Report("mine.party", intactAfterAlly && !allyTripped,
				$"allied crossing: stone {(intactAfterAlly ? "intact" : "GONE (gate failed)")}, ally {(allyTripped ? "TERRIFIED (gate failed)" : "unharmed")} -- owner-party gate");
			if (GameObject.Validate(friendly)) friendly.Obliterate();
			// half 2: a non-party crossing must fire FIRST STEP and consume the stone
			bool fired = false, consumed = false;
			if (GameObject.Validate(rock))
			{
				GameObject stranger = GameObject.Create(BABOON_BP);
				stranger.SetIntProperty(SPAWN_MARK, 1);
				stranger.GetStat("Hitpoints").BaseValue = 100;
				open[1].AddObject(stranger);
				stranger.DirectMoveTo(open[0], 0, Forced: true, IgnoreCombat: true);
				fired = stranger.HasEffect(typeof(XRL.World.Effects.Terrified));
				consumed = !GameObject.Validate(rock);
				if (GameObject.Validate(stranger)) stranger.Obliterate();
			}
			Report("mine.discharge", fired && consumed,
				$"stranger crossing: payload {(fired ? "fired first step (StepChance 100)" : "did NOT fire")}, stone {(consumed ? "consumed by its own discharge (HoldChance 0)" : "SURVIVED (discharge law violated)")}");
			if (GameObject.Validate(rock)) rock.Obliterate();
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

		/// <summary>lith-devour, EXTENDED 2026-07-27 (pre-beta hardening): the full devour
		/// surface, six phases — (gate) a sated devour is refused; (descent) the EaterStatue
		/// blueprint-descent bite, plus the hunger-actually-clears assert the original row
		/// skipped; (spill) an inert container statue spills its entombed item at its feet;
		/// (living) a living devourable dies properly and drops its possessions (the stats
		/// part stamped at zero shifts to satisfy the predicate and route the Die branch;
		/// XP zeroed); (rubble) rubble devoured whole; (boulder) refused as a sated snack,
		/// then devoured straight from the pack while famished. Hunger and cooking counter
		/// restored; every spawn swept.</summary>
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
			Cell stage = open[0];
			int hungerBefore = stomach.HungerLevel;
			int cookBefore = stomach.CookingCounter;
			int passCount = 0;
			List<string> fails = new List<string>();
			bool suppressed = Popup.Suppress;
			Popup.Suppress = true;
			try
			{
				// ---- gate + descent: one statue serves both phases ----
				GameObject statue = GameObject.Create("EaterStatue");
				statue.SetIntProperty(SPAWN_MARK, 1);
				stage.AddObject(statue);
				stomach.HungerLevel = 0; // sated: the gate must refuse
				InventoryActionEvent.Check(ply, ply, statue, "Cleo_TerraFirma_DevourStatue");
				bool gated = GameObject.Validate(statue);
				Helpers.VerifyLog("AUTO", $"devour.gate: {(gated ? "PASS" : "FAIL")} - {(gated ? "sated devour refused, statue untouched" : "statue eaten WITHOUT the famished gate")}");
				if (gated) passCount++; else fails.Add("gate");

				stomach.HungerLevel = 2; // famished
				InventoryActionEvent.Check(ply, ply, statue, "Cleo_TerraFirma_DevourStatue");
				bool gone = !GameObject.Validate(statue);
				bool cleared = stomach.HungerLevel < 2;
				Helpers.VerifyLog("AUTO", $"devour.descent: {(gone && cleared ? "PASS" : "FAIL")} - EaterStatue {(gone ? "devoured whole (blueprint-descent detection)" : "STILL STANDS")}, hunger {(cleared ? "cleared" : "NOT cleared")}");
				if (gone && cleared) passCount++; else fails.Add("descent");
				if (!gone && GameObject.Validate(statue)) statue.Obliterate();

				// ---- spill: inert container statue with an entombed item ----
				GameObject container = GameObject.Create("Cleo_TerraFirma_StoneshaperStatue");
				container.SetIntProperty(SPAWN_MARK, 1);
				container.RequirePart<Inventory>();
				GameObject relic = GameObject.Create("Dagger");
				container.ReceiveObject(relic);
				stage.AddObject(container);
				stomach.HungerLevel = 2;
				InventoryActionEvent.Check(ply, ply, container, "Cleo_TerraFirma_DevourStatue");
				bool contGone = !GameObject.Validate(container);
				bool spilled = GameObject.Validate(relic) && relic.CurrentCell == stage;
				Helpers.VerifyLog("AUTO", $"devour.spill: {(contGone && spilled ? "PASS" : "FAIL")} - container statue {(contGone ? "devoured" : "STILL STANDS")}, entombed dagger {(spilled ? "spilled at its feet" : "MISSING")}");
				if (contGone && spilled) passCount++; else fails.Add("spill");
				if (GameObject.Validate(relic)) relic.Obliterate();
				if (!contGone && GameObject.Validate(container)) container.Obliterate();

				// ---- living: the Die(actor) branch — dies properly, possessions drop ----
				GameObject living = GameObject.Create(BABOON_BP);
				living.SetIntProperty(SPAWN_MARK, 1);
				living.GetStat("XPValue").BaseValue = 0; // the test must not level you
				living.AddPart(new XRL.World.Parts.Cleo_TerraFirma_StoneshaperStatueStatsPart()); // zero shifts; satisfies the devour predicate, routes the living branch
				GameObject keepsake = GameObject.Create("Dagger");
				living.ReceiveObject(keepsake);
				stage.AddObject(living);
				stomach.HungerLevel = 2;
				InventoryActionEvent.Check(ply, ply, living, "Cleo_TerraFirma_DevourStatue");
				bool died = !GameObject.Validate(living) || living.Stat("Hitpoints") <= 0 || living.CurrentCell == null;
				bool dropped = GameObject.Validate(keepsake) && keepsake.CurrentCell != null;
				Helpers.VerifyLog("AUTO", $"devour.living: {(died && dropped ? "PASS" : "FAIL")} - living devourable {(died ? "died properly" : "SURVIVED the devour")}, its dagger {(dropped ? "dropped as loot" : "MISSING")}");
				if (died && dropped) passCount++; else fails.Add("living");
				if (GameObject.Validate(keepsake)) keepsake.Obliterate();
				if (GameObject.Validate(living)) living.Obliterate();
				foreach (GameObject leftover in new List<GameObject>(stage.Objects))
					if (leftover.Blueprint != null && leftover.Blueprint.Contains("Corpse")) leftover.Obliterate();

				// ---- rubble: the eating-a-path ruling ----
				GameObject rubble = GameObject.Create("Rubble");
				rubble.SetIntProperty(SPAWN_MARK, 1);
				stage.AddObject(rubble);
				stomach.HungerLevel = 2;
				InventoryActionEvent.Check(ply, ply, rubble, "Cleo_TerraFirma_DevourStatue");
				bool rubbleGone = !GameObject.Validate(rubble);
				Helpers.VerifyLog("AUTO", $"devour.rubble: {(rubbleGone ? "PASS" : "FAIL")} - rubble {(rubbleGone ? "devoured (path-eating works)" : "STILL THERE")}");
				if (rubbleGone) passCount++; else fails.Add("rubble");
				if (!rubbleGone && GameObject.Validate(rubble)) rubble.Obliterate();

				// ---- boulder: snack door refuses it sated; pack devour takes it famished ----
				GameObject boulder = GameObject.Create("MediumBoulder");
				ply.ReceiveObject(boulder);
				// probes (2026-08-01, first-run FAIL was unattributable: no eat log, boulder gone --
				// these name which step loses it: the receive, the eat-attempt, or the devour)
				Helpers.VerifyLog("AUTO", $"devour.boulder probe after receive: valid={GameObject.Validate(boulder)}, inInv={boulder != null && boulder.InInventory == ply}, onGround={boulder?.CurrentCell != null}");
				stomach.HungerLevel = 0;
				InventoryActionEvent.Check(ply, ply, boulder, "Cleo_TerraFirma_EatStone");
				Helpers.VerifyLog("AUTO", $"devour.boulder probe after eat-attempt: valid={GameObject.Validate(boulder)}, inInv={boulder != null && boulder.InInventory == ply}, onGround={boulder?.CurrentCell != null}");
				bool refused = GameObject.Validate(boulder) && boulder.InInventory == ply;
				stomach.HungerLevel = 2;
				InventoryActionEvent.Check(ply, ply, boulder, "Cleo_TerraFirma_DevourStatue");
				bool boulderGone = !GameObject.Validate(boulder) || boulder.InInventory != ply;
				bool boulderCleared = stomach.HungerLevel < 2;
				bool boulderPass = refused && boulderGone && boulderCleared;
				Helpers.VerifyLog("AUTO", $"devour.boulder: {(boulderPass ? "PASS" : "FAIL")} - sated snack {(refused ? "refused" : "ACCEPTED (boulders must not be snackable)")}, famished pack-devour {(boulderGone ? "consumed from inventory" : "DID NOT consume")}, hunger {(boulderCleared ? "cleared" : "NOT cleared")}");
				if (boulderPass) passCount++; else fails.Add("boulder");
				if (GameObject.Validate(boulder) && boulder.InInventory == ply) boulder.Obliterate();
			}
			finally
			{
				Popup.Suppress = suppressed;
				stomach.HungerLevel = hungerBefore; // devours cleared it; the test should not feed you
				stomach.CookingCounter = cookBefore;
			}
			Report("devour", fails.Count == 0,
				$"{passCount}/6 devour phases asserted{(fails.Count > 0 ? "; FAILS: " + string.Join(", ", fails) : "")} (gate, descent, spill, living, rubble, boulder); hunger + cooking counter restored, spawns swept");
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
				neighbor = GameObject.Create(BABOON_BP); // the galvena arc's target (was also the thunderclap's ear until its 2026-07-25 cut)
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
				{ "Cleo_TerraFirma_Rock_Healstone", "heal", "healstone (eaten heal, ruled 2026-07-27)" },
				{ "Cleo_TerraFirma_Rock_Salt", "consume", "chunk of salt (tasty)" },
				{ "Cleo_TerraFirma_Rock_Pointy", "bleed", "pointy rock" },
				{ "Cleo_TerraFirma_Rock_ShatteredStalactite", "bleed", "shattered stalactite" },
				{ "Cleo_TerraFirma_Rock_CompressedAsh", "belly", "compressed ash (ash belly; choke cut 2026-07-25)" },
				{ "Cleo_TerraFirma_Rock_Melting", "harm", "sludgabbro-acid (melting)" },
				{ CAROM_BP, "moved", "caromcobble (hop)" },
				{ "Cleo_TerraFirma_Rock_Shunting", "moved", "farflint (teleport)" },
				{ STREAK_BP, "consume", "streakschorl (plain snack; thunderclap cut 2026-07-25)" },
				{ "Cleo_TerraFirma_Rock_Rusty", "oxidize", "oxidian (rusting limbs)" },
				{ "Cleo_TerraFirma_Rock_Sludgestone", "spawnliquid", "sludgestone" },
				{ "Cleo_TerraFirma_Rock_Spiderweb", "webs", "gossamarl (3 webs)" },
				{ "Cleo_TerraFirma_Rock_Whorlstone", "belly", "vertigneiss (gas belly)" },
				{ "Cleo_TerraFirma_Rock_Scorching", "hot", "pyroclase" },
				{ "Cleo_TerraFirma_Rock_Icy", "cold", "rimeholm" },
				{ "Cleo_TerraFirma_Rock_Voltaic", "consume", "galvena (self-arc/crackle)" },
				{ "Cleo_TerraFirma_Rock_Spiny", "consume", "urchinite (quill burst)" },
				{ STRANGE_BP, "consume", "strange rock (mimic eat)" },
				// eaten-table comb rows (2026-07-31): witchwood channels assert EITHER branch --
				// exactly one of the two outcomes must land where both are raw applications;
				// save-gated channels (grimslate dread, vertigneiss confuse) stay log-grade
				{ "Cleo_TerraFirma_Rock_Scary", "consume", "grimslate (dread invert; save-gated, log-grade)" },
				{ "Cleo_TerraFirma_Rock_Vantabsidian", "darksight", "vantabsidian (dark-adapted or the blind bite)" },
				{ "Cleo_TerraFirma_Rock_Torpor", "sleepbelly", "lullabaster (sleep belly; nap bite-gated)" },
				{ "Cleo_TerraFirma_Rock_Bola", "footing", "tanglelith (sure-footed or the stuck bite)" },
				{ "Cleo_TerraFirma_Rock_Oilstone", "greased", "oilstone (sure-greased or the slick bite)" },
				{ "Cleo_TerraFirma_Rock_Hexstone", "anchor", "lattice euclase (tether + belly, detonate cut)" },
				{ "Cleo_TerraFirma_Rock_Holographic", "hologram", "holographic rock (zero-satiety joke)" },
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
				int cookRowBefore = stomach.CookingCounter;
				Cell cellBefore = ply.CurrentCell;
				if (kind == "spawnliquid" || kind == "greased")
					SweepPuddlesAround(ply); // pools MERGE: a leftover puddle absorbs the spawn and the object count never moves (live find: sludgestone's slime vanished into melting's acid)
				int healStagedPenalty = 0;
				if (kind == "heal") { ply.GetStat("Hitpoints").Penalty += 5; healStagedPenalty = ply.GetStat("Hitpoints").Penalty; } // a heal needs a wound to show itself; the stage is restored with the rest
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
					case "heal":
						pass = consumed && ply.GetStat("Hitpoints").Penalty < healStagedPenalty;
						detail = $"HP penalty {healStagedPenalty}->{ply.GetStat("Hitpoints").Penalty} (staged wound {(pass ? "healed" : "NOT healed")})";
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
					case "oxidize":
						pass = consumed && ply.HasEffect(typeof(XRL.World.Effects.Cleo_TerraFirma_OxidizedLimbs));
						detail = "oxidized effect " + (pass ? "applied (RustOnHit rides the natural weapons)" : "MISSING");
						ply.RemoveEffect<XRL.World.Effects.Cleo_TerraFirma_OxidizedLimbs>(); // Remove() strips the stamped parts
						break;
					case "hot":
						pass = consumed && (ply.Physics?.Temperature ?? 25) > tempBefore;
						detail = $"temperature {tempBefore}->{ply.Physics?.Temperature}";
						break;
					case "cold":
						pass = consumed && (ply.Physics?.Temperature ?? 25) < tempBefore;
						detail = $"temperature {tempBefore}->{ply.Physics?.Temperature}";
						break;
					case "darksight":
						bool adapted = ply.HasEffect(typeof(XRL.World.Effects.Cleo_TerraFirma_DarkAdapted));
						bool blinded = ply.HasEffect(typeof(XRL.World.Effects.Blind));
						pass = consumed && (adapted || blinded);
						detail = adapted ? "dark-adapted (the 75%)" : blinded ? "blind (the bite)" : "NEITHER effect landed";
						ply.RemoveEffect<XRL.World.Effects.Cleo_TerraFirma_DarkAdapted>();
						ply.RemoveEffect<XRL.World.Effects.Blind>();
						break;
					case "sleepbelly":
						pass = consumed && ply.HasEffect(typeof(XRL.World.Effects.Cleo_TerraFirma_GasBelly));
						detail = "sleep belly " + (pass ? "settled in" : "MISSING")
							+ (ply.HasEffect(typeof(XRL.World.Effects.Asleep)) ? "; the nap landed (the bite)" : "");
						ply.RemoveEffect<XRL.World.Effects.Cleo_TerraFirma_GasBelly>();
						ply.RemoveEffect<XRL.World.Effects.Asleep>();
						break;
					case "footing":
						bool footed = ply.HasEffect(typeof(XRL.World.Effects.Cleo_TerraFirma_SureFooted));
						bool stuckBite = ply.HasEffect(typeof(XRL.World.Effects.Stuck));
						pass = consumed && (footed || stuckBite);
						detail = footed ? "sure-footed (the 75%)" : stuckBite ? "stuck (the bite)" : "NEITHER landed";
						ply.RemoveEffect<XRL.World.Effects.Cleo_TerraFirma_SureFooted>();
						ply.RemoveEffect<XRL.World.Effects.Stuck>();
						break;
					case "greased":
						bool greasedOn = ply.HasEffect(typeof(XRL.World.Effects.Cleo_TerraFirma_SureGreased));
						bool slicked = CountPuddlesAround(ply) > liquidsBefore;
						pass = consumed && (greasedOn || slicked);
						detail = greasedOn ? "sure-greased (the 75%)" : slicked ? "slick underfoot (the bite)" : "NEITHER landed";
						ply.RemoveEffect<XRL.World.Effects.Cleo_TerraFirma_SureGreased>();
						SweepPuddlesAround(ply);
						break;
					case "anchor":
						bool tethered = ply.HasEffect(typeof(XRL.World.Effects.RealityStabilized));
						bool anchorBelly = ply.HasEffect(typeof(XRL.World.Effects.Cleo_TerraFirma_GasBelly));
						pass = consumed && tethered && anchorBelly && ply.GetStat("Hitpoints").Value == hpBefore;
						detail = $"tether {(tethered ? "on" : "MISSING")}, belly {(anchorBelly ? "on" : "MISSING")}, HP {hpBefore}->{ply.GetStat("Hitpoints").Value} (detonate stays cut)";
						ply.RemoveEffect<XRL.World.Effects.RealityStabilized>();
						ply.RemoveEffect<XRL.World.Effects.Cleo_TerraFirma_GasBelly>();
						break;
					case "hologram":
						pass = consumed && stomach.CookingCounter == cookRowBefore;
						detail = "satiety " + (stomach.CookingCounter == cookRowBefore ? "unchanged (the joke completes)" : $"CHANGED {cookRowBefore}->{stomach.CookingCounter}");
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

			// mullstone refresh (the last cheap skip, staged): exactly one ability left cooling,
			// eat -> it must refresh to 0; every cooldown snapshot-restored after (net-neutral)
			int totalRows = rows.GetLength(0);
			ActivatedAbilities abilities = ply.ActivatedAbilities;
			if (abilities?.AbilityByGuid != null && abilities.AbilityByGuid.Count > 0)
			{
				totalRows++;
				Dictionary<Guid, int> snapshot = new Dictionary<Guid, int>();
				foreach (KeyValuePair<Guid, ActivatedAbilityEntry> kv in abilities.AbilityByGuid)
					snapshot[kv.Key] = kv.Value.Cooldown;
				ActivatedAbilityEntry staged = null;
				foreach (ActivatedAbilityEntry entry in abilities.AbilityByGuid.Values)
				{
					if (staged == null) { staged = entry; entry.Cooldown = 500; }
					else if (entry.Cooldown > 0) entry.Cooldown = 0;
				}
				GameObject mull = GameObject.Create("Cleo_TerraFirma_Rock_Mullstone");
				ply.ReceiveObject(mull);
				bool sup = Popup.Suppress;
				Popup.Suppress = true;
				try { InventoryActionEvent.Check(ply, ply, mull, "Cleo_TerraFirma_EatStone"); }
				finally { Popup.Suppress = sup; }
				bool refreshed = staged != null && staged.Cooldown <= 0;
				Helpers.VerifyLog("AUTO", $"eat.mullstone (staged refresh): {(refreshed ? "PASS" : "FAIL")} - the one cooling ability {(refreshed ? "refreshed to ready" : "still cooling: " + (staged?.Cooldown ?? -1))}");
				if (refreshed) passCount++;
				else fails.Add("mullstone refresh");
				foreach (KeyValuePair<Guid, int> kv in snapshot)
					if (abilities.AbilityByGuid.TryGetValue(kv.Key, out ActivatedAbilityEntry restoreEntry))
						restoreEntry.Cooldown = kv.Value;
				if (GameObject.Validate(mull) && mull.InInventory == ply)
					mull.Obliterate();
			}
			else
				Helpers.VerifyLog("AUTO", "eat.mullstone (staged refresh): SKIP - no activated abilities on this character");

			// the test must not feed, hurt, or relocate you on net
			stomach.HungerLevel = hungerBefore;
			stomach.CookingCounter = cookBefore;
			ply.GetStat("Hitpoints").Penalty = penaltyBefore;
			if (ply.CurrentCell != home)
				ply.DirectMoveTo(home, 0, Forced: true, IgnoreCombat: true);
			if (neighbor != null && GameObject.Validate(neighbor))
				neighbor.Obliterate();
			Report("eat", fails.Count == 0,
				$"{passCount}/{totalRows} eaten channels asserted{(fails.Count > 0 ? "; FAILS: " + string.Join(", ", fails) : "")}; skipped by design: detonate pair, fungal twins, deep four, lodestone (the 25% bite rips real gear). Player state restored (cooldowns included).");
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
			if (caster.Brain != null)
				caster.Brain.Target = ply;
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
					if (obj.GetPart<Cleo_TerraFirma_FallenRockPart>() != null && obj.CurrentCell != null && obj.InInventory == null && obj.Equipped == null)
						n += obj.Count;
			return n;
		}

		/// <summary>Redoubt census: every mod rock-family object standing in the zone -- fabric
		/// (rubble/crumbled/wall/crack), salvage keepers, geysers, debris, and the figure.</summary>
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

		private static bool RedoubtIsScatter(string bp) => bp == GENERIC_BP || bp == PEBBLES_BP || bp == "MediumBoulder";

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
		/// content (crack/geyser/salvage/figure) leaking out of the marked blocks. Wall share is
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
				// 60 HP was the single-hit wave's era: the pulse churn at a Str-invested L5
				// (PV 7, 3-5 pens/pulse) dealt 60-77 across the ride and KILLED two probes
				// (first pulse boot 2026-08-09), which under-counted every assert. The probes
				// must survive all three pulses to testify.
				b.GetStat("Hitpoints").BaseValue = 400;
				c1.AddObject(b);
				victims.Add(b);
				starts[b] = c1;
				hp[b] = b.GetStat("Hitpoints").Value;
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
		/// entirely by pulse 3. A ring-4 glass-jaw probes the dissipating-front bite (CLAUDE
		/// PICK at port time, flagged for feel: rim walls take ONE bite on the way out). The
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

			// cast B -- L4: the three-file falls entirely (one wall per pulse), the ring-4
			// glass-jaw falls to the dissipating front's single bite, the survivor stands for
			// the at-all-levels claim, and a NATIVE-AV metal wall (AV 30 vs PV 18: zero pens
			// ever) probes the attacked-but-untouched path of the Hard class
			GameObject b1 = RedoubtRigWall(NATURAL_WALL_BP, e1, 0, 1);
			GameObject b2 = RedoubtRigWall(NATURAL_WALL_BP, e2, 0, 1);
			GameObject b3 = RedoubtRigWall(NATURAL_WALL_BP, e3, 0, 1);
			GameObject b4 = RedoubtRigWall(NATURAL_WALL_BP, e4, 0, 1); // ring 4: the front-bite probe
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
			if (GameObject.Validate(b4) && b4.CurrentCell != null) fails.Add("L4: ring-4 glass-jaw sandstone NOT felled (dissipating front must bite rim walls once)");
			if (!GameObject.Validate(survivor) || survivor.CurrentCell != s1 || survivor.GetStat("Hitpoints").Value != survivorHP) fails.Add("L4: AV-999 survivor fell or took damage");
			// "untouched" retired with the clamp (first pulse boot 2026-08-09): exploding pen
			// dice give PV 18 a ~12% chip per bite vs AV 30, and three bites a cast make chips
			// ROUTINE -- the wall must STAND (felling needs ~3 pens in one cast, <1%), but
			// damage is legal now. Chip-vs-clean is reported, not asserted.
			if (!GameObject.Validate(metal) || metal.CurrentCell != w1) fails.Add("L4: native-AV METAL wall FELL (AV 30 vs PV 18 must stand; felling odds are sub-1%)");
			RedoubtCleanup(NewSince(zone, before));
			if (GameObject.Validate(metal)) metal.Obliterate();
			if (GameObject.Validate(survivor)) survivor.Obliterate();
			Report("redoubt.carve", fails.Count == 0,
				fails.Count == 0
					? "per-pulse bites hold: the L1 two-file fell one wall per pulse, the L4 three-file fell entirely, the ring-4 wall fell to the front's single bite; the AV-999 survivor stood untouched at both bands (armor is the boundary); clamp-removed rules verified -- glass-jaw wood AND AV-0 metal both FELL, native-AV metal untouched on zero pens"
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
			// RETIRED 2026-07-25 (fragility ruling: the dull clock is CUT -- "a stone keeps until
			// it goes off"). The scenario's PASS stands in the ledger as history; the code path it
			// proved is dormant (DullAfter 0 mod-wide). SetupDull kept below in case the clock ever
			// returns as a longer fuse.
			MessageQueue.AddPlayerMessage("tfdull RETIRED: the dull clock was cut 2026-07-25 (a stone keeps until it goes off). Ledger entry stands as history.", "W");
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

			// step-path latch (the last drain footnote): an at-rest drainstone underfoot latches
			// IMMEDIATELY on its 25% step proc -- the smarter Latch channel's other half
			List<Cell> stepOpen = origin.GetLocalAdjacentCells();
			stepOpen.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (stepOpen.Count >= 2)
			{
				GameObject stepVictim = GameObject.Create(BABOON_BP);
				stepVictim.SetIntProperty(SPAWN_MARK, 1);
				stepVictim.GetStat("Hitpoints").BaseValue = 100;
				stepOpen[0].AddObject(stepVictim);
				GameObject stepStone = GameObject.Create(DRAINSTONE_BP);
				stepStone.SetIntProperty(SPAWN_MARK, 1);
				stepOpen[1].AddObject(stepStone);
				int crossings = ForceEntries(stepVictim, stepOpen[0], stepOpen[1], 30,
					() => GameObject.Validate(stepStone) && stepStone.Equipped == stepVictim);
				bool latchedStep = GameObject.Validate(stepStone) && stepStone.Equipped == stepVictim;
				bool drainingStep = GameObject.Validate(stepVictim) && stepVictim.HasEffect(typeof(XRL.World.Effects.LifeDrain));
				Report("drain.steplatch", latchedStep && drainingStep,
					$"at-rest step latch after {crossings} crossing(s) (StepChance 25, IMMEDIATE channel): stone {(latchedStep ? "riding the stepper" : "NOT attached")}, LifeDrain {(drainingStep ? "applied (half duration, step rules)" : "MISSING")}");
				if (GameObject.Validate(stepVictim)) stepVictim.Obliterate();
				if (GameObject.Validate(stepStone)) stepStone.Obliterate();
			}
			else
				Report("drain.steplatch", false, "SETUP SKIP - no room for the step lane");

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
			{
				FinishBatch(); // end of a tfbatch chain (or a lost-state abort dropped the queue); no-op outside a batch
				return;
			}
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
					Cleo.TerraFirma.Scripts.TestHarnessWishes.FinishBatch(); // the abort drops the queue; close the tally so the verdict line still lands
					return base.HandleEvent(E);
				}
				if (Scenario == "hopper" && !LoggedReadiness)
				{
					LoggedReadiness = true;
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("AUTO", $"hopper tick 1: live mill turning={MillTurning(MillLive)}, dead mill turning={MillTurning(MillDead)}");
				}
				if (Scenario == "scav" || Scenario == "fis")
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
			if (Scenario == "fis")
			{
				if (FisCaster?.Brain != null && FisCaster.Brain.Target == null)
					FisCaster.Brain.Target = owner;
				return;
			}
			Zone z = owner?.CurrentZone;
			if (z == null)
				return;
			foreach (GameObject o in z.GetObjects())
			{
				if (o.HasPart<Cleo_TerraFirma_TestThrowCounter>() && o.Brain != null && o.Brain.Target == null)
					o.Brain.Target = owner;
			}
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
			// band floor = StonesEach * 3: the 2-3/rock hand rate PLUS the mill's +1 bonus (ruled 2026-07-25)
			bool pass = liveStones == 0 && livePowders >= StonesEach * 3 && deadStones == StonesEach && deadPowders == 0;
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("hopper", pass,
				$"after {TurnsTotal} turns: powered mill stones={liveStones} powders={livePowders} (want 0 / >={StonesEach * 3}, mill +1 rate), dead mill stones={deadStones} powders={deadPowders} (want {StonesEach} / 0)");
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
