using System;
using System.Collections.Generic;
using System.Text;
using Cleo.TerraFirma.Scripts;
using XRL;
using XRL.Messages;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Effects;
using XRL.World.Parts;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// TEMPORARY serialization round-trip harness (2026-08-05, user: "we need to make the
	/// harness detect those not happening as well I think?"). One wish, self-contained:
	///
	///   tfserial -- stages every beta-live serialization risk in the current zone, writes the
	///   expected state INTO the save (game-state strings serialize with it), then drives the
	///   engine's own QuickSave -> QuickLoad round trip (the exact synchronous pattern the
	///   CmdSave/CmdLoad keybinds use, decomp-verified XRLCore player-command loop). After the
	///   reload, the [CallAfterGameLoaded] hook below sees the pending marker in the LOADED
	///   game and asserts live state against the recorded expectations -- custom Write/Read
	///   paths included -- then restores the player and sweeps the probes.
	///
	/// Staged probes (beta-live content only):
	///   - Geomancy reveal cells (THE custom-serialization risk: hand-rolled Write/Read with
	///     zone/cell re-resolution) -- three cells, mixed durations, one DoScan
	///   - Stoneshaper CurStatue reference (WriteGameObject/ReadGameObject round trip) +
	///     StatueLifetimeRemaining countdown
	///   - Earthen Barrage reserve charge count
	///   - Gorgon's Gaze partial-petrify stone mark on a spawned probe (effect + duration)
	///   - One of each redoubt ring deposit: fabric wall, crumbled wall, rubble, and a
	///     calcified wall (with its live part)
	///
	/// WARNING: overwrites the character's quicksave AND (by the engine's own QuickLoad
	/// contract) copies it over the Primary save -- run on test characters. In-session reload
	/// only; the quit-to-desktop COLD load stays a manual checklist item (different code path).
	/// NOT part of tfbatch: the reload would sever the batch driver's [NonSerialized] state by
	/// design. Rides Helpers.VERIFY_WATCH and the strip-before-release obligation.
	/// </summary>
	[HasWishCommand]
	[HasCallAfterGameLoaded]
	public class SerializationHarnessWishes
	{
		private const string STATE_KEY = "Cleo_TerraFirma_SerialExpect";
		private const string SPAWN_MARK = "TFHarnessSpawn";   // same marker the main harness sweeps
		private const string PROBE_MARK = "TFSerialProbe";
		private const string GORGON_MARK = "TFSerialGorgon";
		private const string APE_MARK = "TFSerialApe";

		private static readonly string[] DEPOSIT_BPS =
		{
			"Cleo_TerraFirma_Rock_Wall",
			"Cleo_TerraFirma_Rock_CrumbledWall",
			"Cleo_TerraFirma_Rock_Rubble",
			"Cleo_TerraFirma_WallCalcify",
		};

		[WishCommand(Command = "tfserial")]
		public static void Serial()
		{
			if (!Helpers.VERIFY_WATCH)
			{
				MessageQueue.AddPlayerMessage("VERIFY_WATCH is OFF -- the harness is disabled.", "R");
				return;
			}
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (zone == null || The.Game == null)
				return;
			if (ply.HasPart<Cleo_TerraFirma_TestDriver>())
			{
				MessageQueue.AddPlayerMessage("A harness scenario is already running -- tfserial reloads the game and would sever it.", "R");
				return;
			}
			if (!The.Game.GetStringGameState(STATE_KEY).IsNullOrEmpty())
			{
				MessageQueue.AddPlayerMessage("A tfserial round trip is already pending -- load resolution will clear it.", "R");
				return;
			}

			List<Cell> open = origin.GetLocalAdjacentCells(2);
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			if (open.Count < 4)
			{
				TestHarnessWishes.Report("serial", false, "SETUP FAIL - need at least four open cells within 2");
				return;
			}

			MessageQueue.AddPlayerMessage("Harness: serialization round trip -- staging, quicksaving, and reloading. &WThis overwrites the quicksave and primary save.&y", "W");

			var expect = new List<string>();
			bool suppressed = Popup.Suppress;
			Popup.Suppress = true;
			try
			{
				int cellIdx = 0;

				// ---- ring/calcify deposits: one of each blueprint, marked, censused by mark
				var spawned = new List<string>();
				foreach (string bp in DEPOSIT_BPS)
				{
					if (cellIdx >= open.Count)
						break;
					GameObject o = GameObject.Create(bp);
					if (o == null)
						continue;
					o.SetIntProperty(SPAWN_MARK, 1);
					o.SetIntProperty(PROBE_MARK, 1);
					if (bp == "Cleo_TerraFirma_WallCalcify" && o.GetPart<Cleo_TerraFirma_CalcifyWallPart>() == null)
						o.AddPart(new Cleo_TerraFirma_CalcifyWallPart(0, o.GetStatValue("AV"))); // the live-cast part rides the probe too
					open[cellIdx++].AddObject(o);
					spawned.Add(bp);
					expect.Add("sp_" + bp + "=1");
				}

				// ---- Gorgon stone mark on a live probe
				if (cellIdx < open.Count)
				{
					GameObject baboon = GameObject.Create("Baboon");
					if (baboon != null)
					{
						baboon.SetIntProperty(SPAWN_MARK, 1);
						baboon.SetIntProperty(GORGON_MARK, 1);
						open[cellIdx++].AddObject(baboon);
						var mark = new Cleo_TerraFirma_PartiallyPetrified();
						baboon.ApplyEffect(mark);
						var applied = baboon.GetEffect<Cleo_TerraFirma_PartiallyPetrified>();
						if (applied != null)
							expect.Add("gorgDur=" + applied.Duration);
						else
							expect.Add("gorgDur=REFUSED"); // apply refused pre-save = its own finding
					}
				}

				Mutations mutations = ply.RequirePart<Mutations>();

				// ---- Geomancy: the hand-rolled Write/Read (zone/cell re-resolution)
				var geo = ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_Geomancy>();
				bool geoGranted = geo == null;
				if (geoGranted)
				{
					geo = new XRL.World.Parts.Mutation.Cleo_TerraFirma_Geomancy();
					mutations.AddMutation(geo, 1);
				}
				List<Cell> far = origin.GetLocalAdjacentCells(5);
				far.RemoveAll(c => c.DistanceTo(origin) < 3);
				int[] durations = { 25, 17, 9 };
				for (int i = 0; i < durations.Length && i < far.Count; i++)
					geo.Cells.Add(new XRL.World.Parts.Mutation.Cleo_TerraFirma_Geomancy.Cleo_TerraFirma_GeomancyCellData(far[i], durations[i], i == 2));
				var sig = new List<int>();
				foreach (var cd in geo.Cells)
					sig.Add(cd.Duration);
				sig.Sort();
				expect.Add("geoGrant=" + (geoGranted ? 1 : 0));
				expect.Add("geoCount=" + geo.Cells.Count);
				expect.Add("geoSig=" + string.Join(",", sig.ConvertAll(d => d.ToString()).ToArray()));

				// ---- Earthen Barrage: reserve charges
				var eb = ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_EarthenBarrage>();
				bool ebGranted = eb == null;
				if (ebGranted)
				{
					eb = new XRL.World.Parts.Mutation.Cleo_TerraFirma_EarthenBarrage();
					mutations.AddMutation(eb, 1);
				}
				expect.Add("ebGrant=" + (ebGranted ? 1 : 0));
				expect.Add("ebPrev=" + eb.Charges);
				eb.Charges = 2;
				expect.Add("ebCharges=2");

				// ---- Stoneshaper: CurStatue object reference + expiry countdown
				var sh = ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper>();
				bool shGranted = sh == null;
				if (shGranted)
				{
					sh = new XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper();
					mutations.AddMutation(sh, 1);
				}
				expect.Add("shGrant=" + (shGranted ? 1 : 0));
				expect.Add("shPrev=" + sh.StatueLifetimeRemaining);
				sh.StatueLifetimeRemaining = 12;
				expect.Add("shLife=12");
				bool wired = false;
				if (sh.CurStatue == null && cellIdx < open.Count)
				{
					GameObject ape = GameObject.Create("Cleo_TerraFirma_StoneshaperStatue"); // the ape statue until it left for quarantine/ 2026-09-30
					if (ape != null)
					{
						ape.SetIntProperty(SPAWN_MARK, 1);
						ape.SetIntProperty(APE_MARK, 1);
						open[cellIdx++].AddObject(ape);
						// CurStatue's setter is deliberately private (statue lifecycle belongs to the
						// carve path); the harness pokes the backing field directly -- our own class,
						// reflection is the least-invasive door and this file strips before release
						typeof(XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper)
							.GetField("_curStatue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
							?.SetValue(sh, ape);
						wired = sh.CurStatue == ape;
					}
				}
				expect.Add("shWired=" + (wired ? 1 : 0));

				string payload = string.Join(";", expect.ToArray());
				The.Game.SetStringGameState(STATE_KEY, payload);
				Helpers.VerifyLog("SERIAL", "staged for round trip: " + payload);
			}
			finally
			{
				Popup.Suppress = suppressed;
			}

			// the engine's own keybind pattern: synchronous QuickSave, then a blocking QuickLoad
			// that swaps the game out from under us -- touch NOTHING of the old game afterward
			The.Game.QuickSave();
			Helpers.VerifyLog("SERIAL", "quicksave written, initiating in-session quickload");
			System.Threading.Tasks.Task<bool> task = The.Game.QuickLoad();
			task.Wait();
			if (task.Result)
				Sidebar.UpdateState();
			else
				Helpers.VerifyLog("SERIAL", "SCENARIO serial: FAIL - QuickLoad returned false (save did not load)");
		}

		/// <summary>Fires on every game load; a no-op unless a tfserial round trip is pending
		/// in the loaded game's state. Asserts, restores the player, sweeps the probes.</summary>
		[CallAfterGameLoaded]
		public static void ResolveAfterLoad()
		{
			XRLGame game = The.Game;
			if (game == null)
				return;
			string payload = game.GetStringGameState(STATE_KEY);
			if (payload.IsNullOrEmpty())
				return;
			game.SetStringGameState(STATE_KEY, ""); // clear FIRST -- a throw below must not loop every load

			var exp = new Dictionary<string, string>();
			foreach (string pair in payload.Split(';'))
			{
				int eq = pair.IndexOf('=');
				if (eq > 0)
					exp[pair.Substring(0, eq)] = pair.Substring(eq + 1);
			}

			GameObject ply = The.Player;
			Zone zone = ply?.CurrentCell?.ParentZone;
			if (zone == null)
			{
				Helpers.VerifyLog("SERIAL", "SCENARIO serial: FAIL - loaded game has no player zone");
				return;
			}

			int passes = 0, fails = 0;
			void Check(string label, bool pass, string detail)
			{
				if (pass) passes++; else fails++;
				TestHarnessWishes.Report("serial." + label, pass, detail);
			}

			// ---- deposits: every marked probe survived with its blueprint intact
			var byBp = new Dictionary<string, int>();
			GameObject gorgonProbe = null, apeProbe = null, calcProbe = null;
			foreach (GameObject o in zone.GetObjects())
			{
				if (o.GetIntProperty(PROBE_MARK) == 1)
				{
					byBp[o.Blueprint] = (byBp.TryGetValue(o.Blueprint, out int n) ? n : 0) + 1;
					if (o.Blueprint == "Cleo_TerraFirma_WallCalcify")
						calcProbe = o;
				}
				if (o.GetIntProperty(GORGON_MARK) == 1)
					gorgonProbe = o;
				if (o.GetIntProperty(APE_MARK) == 1)
					apeProbe = o;
			}
			foreach (string bp in DEPOSIT_BPS)
			{
				if (!exp.ContainsKey("sp_" + bp))
					continue; // wasn't staged (cramped setup) -- not a failure
				bool here = byBp.TryGetValue(bp, out int n) && n == 1;
				Check("deposit", here, bp + (here ? " survived the round trip" : " MISSING after reload"));
			}
			if (calcProbe != null)
				Check("calcpart", calcProbe.GetPart<Cleo_TerraFirma_CalcifyWallPart>() != null,
					"calcified wall " + (calcProbe.GetPart<Cleo_TerraFirma_CalcifyWallPart>() != null ? "kept its live part" : "LOST CalcifyWallPart"));

			// ---- gorgon mark
			if (exp.TryGetValue("gorgDur", out string gorgDur))
			{
				if (gorgDur == "REFUSED")
					Check("gorgon", false, "stone mark refused to apply BEFORE the save (staging finding, not serialization)");
				else if (gorgonProbe == null)
					Check("gorgon", false, "petrify probe itself MISSING after reload");
				else
				{
					var eff = gorgonProbe.GetEffect<Cleo_TerraFirma_PartiallyPetrified>();
					Check("gorgon", eff != null && eff.Duration.ToString() == gorgDur,
						eff == null ? "stone mark LOST on reload" : $"stone mark held, duration {eff.Duration} (expected {gorgDur})");
				}
			}

			// ---- player mutations
			var geo = ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_Geomancy>();
			if (geo == null)
				Check("geo", false, "Geomancy part MISSING from loaded player");
			else
			{
				var sig = new List<int>();
				foreach (var cd in geo.Cells)
					sig.Add(cd.Duration);
				sig.Sort();
				string liveSig = string.Join(",", sig.ConvertAll(d => d.ToString()).ToArray());
				bool pass = geo.Cells.Count.ToString() == exp["geoCount"] && liveSig == exp["geoSig"];
				Check("geo", pass, $"reveal cells {geo.Cells.Count} (expected {exp["geoCount"]}), durations [{liveSig}] (expected [{exp["geoSig"]}]) -- the hand-rolled Write/Read");
			}

			var eb = ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_EarthenBarrage>();
			if (eb == null)
				Check("eb", false, "Earthen Barrage part MISSING from loaded player");
			else
				Check("eb", eb.Charges.ToString() == exp["ebCharges"], $"reserve charges {eb.Charges} (expected {exp["ebCharges"]})");

			var sh = ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper>();
			if (sh == null)
				Check("shaper", false, "Stoneshaper part MISSING from loaded player");
			else
			{
				Check("shaper", sh.StatueLifetimeRemaining.ToString() == exp["shLife"],
					$"statue lifetime {sh.StatueLifetimeRemaining} (expected {exp["shLife"]})");
				if (exp.TryGetValue("shWired", out string wired) && wired == "1")
					Check("shaper.ref", sh.CurStatue != null && sh.CurStatue == apeProbe,
						sh.CurStatue == null ? "CurStatue reference LOST (WriteGameObject round trip failed)" : "CurStatue reference re-resolved to the ape probe");
			}

			TestHarnessWishes.Report("serial", fails == 0,
				$"{passes}/{passes + fails} serialization probes held through QuickSave->QuickLoad{(fails > 0 ? "" : "; state restored, probes swept")}");

			// ---- restore the player exactly as staged, then sweep
			try
			{
				Mutations mutations = ply.GetPart<Mutations>();
				if (sh != null)
				{
					typeof(XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper)
						.GetField("_curStatue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
						?.SetValue(sh, null);
					if (exp.TryGetValue("shGrant", out string g) && g == "1")
						mutations?.RemoveMutation(sh);
					else if (exp.TryGetValue("shPrev", out string prev) && int.TryParse(prev, out int prevLife))
						sh.StatueLifetimeRemaining = prevLife;
				}
				if (eb != null)
				{
					if (exp.TryGetValue("ebGrant", out string g) && g == "1")
						mutations?.RemoveMutation(eb);
					else if (exp.TryGetValue("ebPrev", out string prev) && int.TryParse(prev, out int prevCharges))
						eb.Charges = prevCharges;
				}
				if (geo != null)
				{
					if (exp.TryGetValue("geoGrant", out string g) && g == "1")
						mutations?.RemoveMutation(geo);
					else
						// pre-existing Geomancy: shed only the three staged reveals (matched by
						// their untouched durations -- no turns pass inside the round trip)
						geo.Cells.RemoveAll(cd => cd.Duration == 25 || cd.Duration == 17 || cd.Duration == 9);
				}
				var marked = new List<GameObject>();
				foreach (GameObject o in zone.GetObjects())
					if (o.GetIntProperty(SPAWN_MARK) == 1 && !o.IsPlayer())
						marked.Add(o);
				foreach (GameObject o in marked)
					o.Obliterate();
			}
			catch (Exception x)
			{
				Helpers.VerifyLog("SERIAL", "cleanup exception (asserts above still stand): " + x.Message);
			}
		}
	}
}
