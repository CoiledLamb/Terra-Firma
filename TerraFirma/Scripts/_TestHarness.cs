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
		private const string SCAV_AMMO_BP = "Cleo_TerraFirma_Rock_Pebbles";
		private const string BABOON_BP = "Baboon";
		private const string SPAWN_MARK = "TFHarnessSpawn";
		private const string RUBBLE_BP = "Cleo_TerraFirma_Rock_Rubble";
		private const string PEBBLES_BP = "Cleo_TerraFirma_Rock_Pebbles";
		private const string ROBOT_BP = "Scrapbot";
		private const string MOTH_BP = "Glowmoth";

		[WishCommand(Command = "tfverify")]
		public static void Help()
		{
			MessageQueue.AddPlayerMessage("TF harness (restructured 2026-09-30) -- instant: tfaudit (entomb, gaze release, barrage lease, calcify refusal, whorl swat of a pebble), tfredoubt (ring census by band, push/stun, water displacement, pebbles; chews nearby natural stone), tfshape (carve cost, swap, expiry, L10 permanence, limb re-arm, copy decay; swaps out your live statue). Turn-driven: tffis (NPC caster, 20t), tfscav (30t, you are the target). tfbatch = all of it, ~50 turns standing still, ends with ONE BATCH COMPLETE line. Diagnostic: tfstatueai (20t). Dev door: tfnpc. Round trip: tfserial (overwrites the quicksave).", "W");
			if (!Helpers.VERIFY_WATCH)
				MessageQueue.AddPlayerMessage("VERIFY_WATCH is OFF -- the harness is disabled.", "R");
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

		/// <summary>The full sweep (restructured 2026-09-30 around what is still owed): three
		/// synchronous batteries (audit, redoubt, shape), then the turn-driven two (fis, scav)
		/// chained through the driver's queue. Ends with ONE 'BATCH COMPLETE' verdict line.</summary>
		[WishCommand(Command = "tfbatch")]
		public static void Batch()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			if (The.Player?.HasPart<Cleo_TerraFirma_TestDriver>() == true) { MessageQueue.AddPlayerMessage("A harness scenario is already running.", "R"); return; }
			BatchPass = 0;
			BatchFail = 0;
			BatchFails.Clear();
			BatchActive = true;
			AuditSync();
			RedoubtSync();
			ShapeSync();
			SetupFis(The.Player, "scav");
		}

		// ---------------------------------------------------------------- audit battery (synchronous)

		/// <summary>The 2026-09-29 audit fixes plus the entomb regression and the 09-30 pebble
		/// swat: gaze.entomb, gaze.release, barrage.lease, calcify.refusal, whorl.swat.</summary>
		[WishCommand(Command = "tfaudit")]
		public static void Audit()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			AuditSync();
		}

		internal static void AuditSync()
		{
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			bool suppressed = Popup.Suppress;
			Popup.Suppress = true;
			Cleo_TerraFirma_StoneRiseFX.HarnessMute = true;
			try
			{
				AuditEntomb();
				AuditGazeRelease(origin);
				AuditBarrageLease();
				AuditCalcifyRefusal(origin);
				AuditWhorlSwat(ply, origin);
			}
			finally
			{
				Cleo_TerraFirma_StoneRiseFX.HarnessMute = false;
				Popup.Suppress = suppressed;
				SweepSpawns(origin.ParentZone);
			}
		}

		private static List<Cell> OpenAround(Cell origin, int radius)
		{
			List<Cell> open = new List<Cell>(origin.GetLocalAdjacentCells(radius));
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			return open;
		}

		private static GameObject Spawn(string blueprint, Cell c = null)
		{
			GameObject o = GameObject.Create(blueprint);
			if (o == null)
				return null;
			o.SetIntProperty(SPAWN_MARK, 1);
			c?.AddObject(o);
			return o;
		}

		/// <summary>Entombment regression (user report 2026-08-15: "armor should be lootable"):
		/// petrified gear, worn and carried, must end up inside the statue.</summary>
		private static void AuditEntomb()
		{
			GameObject victim = Spawn("Snapjaw");
			GameObject worn = GameObject.Create("Leather Armor");
			GameObject carried = GameObject.Create("Leather Boots");
			GameObject tomb = Spawn("Cleo_TerraFirma_QuarriedBlock");
			victim.ReceiveObject(worn);
			victim.ReceiveObject(carried);
			bool equipped = victim.ForceEquipObject(worn, "Body", Silent: true);
			StatueHelpers.EntombPossessions(victim, tomb);
			bool wornInTomb = tomb.Inventory != null && tomb.Inventory.GetObjectsDirect().Contains(worn);
			bool carriedInTomb = tomb.Inventory != null && tomb.Inventory.GetObjectsDirect().Contains(carried);
			string detail =
				!equipped ? "SETUP FAIL - could not equip the test armor on a Snapjaw body" :
				(wornInTomb && carriedInTomb) ? "worn armor AND carried boots both entombed" :
				!wornInTomb ? "WORN ARMOR LOST - it never reached the statue" :
				"carried item lost - the inventory sweep broke";
			Report("gaze.entomb", equipped && wornInTomb && carriedInTomb, detail);
			if (GameObject.Validate(tomb)) tomb.Obliterate();
			if (GameObject.Validate(victim)) victim.Obliterate();
		}

		/// <summary>Audit fix 2026-09-29 (Effect_Petrifying EndTurn): a victim whose gazer is
		/// still alive but no longer in its zone must release itself. Control first: with the
		/// gazer present, the effect holds.</summary>
		private static void AuditGazeRelease(Cell origin)
		{
			List<Cell> open = OpenAround(origin, 2);
			if (open.Count < 2) { Report("gaze.release", false, "SETUP FAIL - need two open cells within 2"); return; }
			GameObject gazer = Spawn("Snapjaw", open[0]);
			GameObject victim = Spawn("Snapjaw", open[1]);
			XRL.World.Effects.Cleo_TerraFirma_Petrifying effect = new XRL.World.Effects.Cleo_TerraFirma_Petrifying(gazer);
			bool applied = victim.ApplyEffect(effect);
			EndTurnEvent.Send(victim);
			bool heldWhilePresent = applied && !effect.Receding;
			gazer.CurrentCell.RemoveObject(gazer); // alive, but in no zone: the teleport-away shape
			EndTurnEvent.Send(victim);
			bool releasedWhenGone = effect.Receding;
			Report("gaze.release", heldWhilePresent && releasedWhenGone,
				!applied ? "SETUP FAIL - Petrifying would not apply to a Snapjaw"
				: $"gazer present: {(heldWhilePresent ? "effect holds" : "RELEASED EARLY")}; gazer out of the zone: {(releasedWhenGone ? "effect recedes on its own" : "STILL HELD (the stuck-victim trap is back)")}");
			if (GameObject.Validate(gazer)) gazer.Obliterate();
		}

		/// <summary>Audit fix 2026-09-29 (EarthenBarrage.Unmutate): stones still held when the
		/// mutation goes get the promised 50-100 round lease, not the 9999 held-sustain lease.</summary>
		private static void AuditBarrageLease()
		{
			GameObject holder = Spawn("Snapjaw");
			Mutations mutations = holder.RequirePart<Mutations>();
			XRL.World.Parts.Mutation.Cleo_TerraFirma_EarthenBarrage eb = new XRL.World.Parts.Mutation.Cleo_TerraFirma_EarthenBarrage();
			mutations.AddMutation(eb, 4);
			GameObject stone = GameObject.Create("Cleo_TerraFirma_EphemeralStone");
			stone.RequirePart<XRL.World.Parts.Temporary>().Duration = 9999;
			holder.ReceiveObject(stone);
			mutations.RemoveMutation(eb);
			XRL.World.Parts.Temporary lease = GameObject.Validate(stone) ? stone.GetPart<XRL.World.Parts.Temporary>() : null;
			int d = lease?.Duration ?? -1;
			Report("barrage.lease", d >= 50 && d <= 100,
				lease == null ? "stone or its Temporary lease VANISHED on removal" : $"held stone lease after the mutation left: {d} (want 50-100, not 9999)");
			if (GameObject.Validate(stone)) stone.Obliterate();
			holder.Obliterate();
		}

		/// <summary>Audit fix 2026-09-29 (Calcify command): an NPC whose whole wall line is
		/// occupied must refuse without spending its turn or cooldown. Positive control after:
		/// clear the line and the same cast lands walls and takes the cooldown.</summary>
		private static void AuditCalcifyRefusal(Cell origin)
		{
			Zone zone = origin.ParentZone;
			Cell casterCell = null, anchor = null, targetCell = null;
			foreach (Cell c in OpenAround(origin, 4))
			{
				Cell a = c.GetCellFromDirection("E");
				Cell b = a?.GetCellFromDirection("E");
				if (a == null || b == null || a.ParentZone != zone || b.ParentZone != zone)
					continue;
				if (a.IsSolid() || b.IsSolid() || a.HasObjectWithPart("Combat") || b.HasObjectWithPart("Combat") || b == origin || a == origin)
					continue;
				casterCell = c; anchor = a; targetCell = b;
				break;
			}
			if (casterCell == null) { Report("calcify.refusal", false, "SETUP FAIL - no open three-cell east lane within 4"); return; }
			GameObject caster = Spawn("Snapjaw", casterCell);
			GameObject threat = Spawn("Snapjaw", targetCell);
			XRL.World.Parts.Mutation.Cleo_TerraFirma_Calcify calc = new XRL.World.Parts.Mutation.Cleo_TerraFirma_Calcify();
			caster.RequirePart<Mutations>().AddMutation(calc, 1);
			ActivatedAbilityEntry entry = caster.ActivatedAbilities?.GetAbility(calc.ActivatedAbilityID);
			// block the whole candidate line: the anchor and its north/south flanks
			List<GameObject> blockers = new List<GameObject>();
			List<Cell> line = new List<Cell> { anchor };
			Cell up = anchor, down = anchor;
			for (int i = 0; i < 5; i++)
			{
				up = up?.GetCellFromDirection("N");
				down = down?.GetCellFromDirection("S");
				if (up != null && up.ParentZone == zone) line.Add(up);
				if (down != null && down.ParentZone == zone) line.Add(down);
			}
			foreach (Cell c in line)
				if (c.IsEmpty() && c != origin)
					blockers.Add(Spawn("Snapjaw", c));
			int energyBefore = caster.Energy?.Value ?? 0;
			int cdBefore = entry?.Cooldown ?? -1;
			CommandEvent.Send(caster, "Cleo_TerraFirma_CommandCalcify", Target: threat);
			int energyAfter = caster.Energy?.Value ?? 0;
			int cdAfter = entry?.Cooldown ?? -1;
			bool refusedClean = entry != null && cdAfter == cdBefore && energyAfter == energyBefore;
			foreach (GameObject b in blockers)
				if (GameObject.Validate(b)) b.Obliterate();
			HashSet<GameObject> before = SnapshotZone(zone);
			CommandEvent.Send(caster, "Cleo_TerraFirma_CommandCalcify", Target: threat);
			int walls = 0;
			foreach (GameObject o in NewSince(zone, before))
				if (o.HasPart<Cleo_TerraFirma_CalcifyWallPart>())
				{
					walls++;
					o.Obliterate();
				}
			bool controlCast = walls > 0 && (entry?.Cooldown ?? 0) > 0;
			Report("calcify.refusal", refusedClean && controlCast,
				entry == null ? "SETUP FAIL - Calcify ability entry missing on the NPC"
				: $"blocked line: cooldown {cdBefore}->{cdAfter}, energy {energyBefore}->{energyAfter} ({(refusedClean ? "refused, nothing spent" : "SPENT on a refusal")}); cleared line: {walls} wall(s), cooldown {entry.Cooldown} ({(controlCast ? "the cast works" : "CONTROL FAILED, so the refusal proves nothing")})");
		}

		/// <summary>09-30 regression after FallenRockPart left: a whorl-guarded target swats a
		/// thrown pebble into an adjacent cell, the pebble survives, and the deflection mark is
		/// gone once the throw resolves (WhorlDeflected restores its own landing now).</summary>
		private static void AuditWhorlSwat(GameObject ply, Cell origin)
		{
			Cell spot = null;
			foreach (Cell c in OpenAround(origin, 3))
				if (c.DistanceTo(origin) >= 2 && OpenAround(c, 1).Count >= 4)
				{
					spot = c;
					break;
				}
			if (spot == null) { Report("whorl.swat", false, "SETUP FAIL - no roomy open cell at distance 2-3"); return; }
			GameObject target = Spawn("Snapjaw", spot);
			target.ApplyEffect(new XRL.World.Effects.Cleo_TerraFirma_GravelWhorl(100));
			int hpBefore = target.GetStat("Hitpoints").Value;
			bool savedForce = XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForceDeflect;
			int savedOutcome = XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForcedOutcome;
			XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForceDeflect = true;
			XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForcedOutcome = 2;
			GameObject pebble;
			try { pebble = ThrowAt(ply, PEBBLES_BP, target); }
			finally
			{
				XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForceDeflect = savedForce;
				XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.DebugForcedOutcome = savedOutcome;
			}
			bool alive = pebble != null && GameObject.Validate(pebble) && pebble.CurrentCell != null;
			int dist = alive ? RedoubtCheb(pebble.CurrentCell, target.CurrentCell) : -1;
			bool unmarked = alive && !pebble.HasPart<Cleo_TerraFirma_WhorlDeflectedPart>();
			bool unhurt = target.GetStat("Hitpoints").Value == hpBefore;
			Report("whorl.swat", alive && dist == 1 && unmarked && unhurt,
				pebble == null ? "SETUP FAIL - the throw never happened (no thrown slot?)"
				: $"pebble {(alive ? $"swatted to distance {dist} (want 1)" : "LOST")}, deflection mark {(unmarked ? "cleared" : "LEFT BEHIND")}, target {(unhurt ? "unhurt" : "HIT despite the swat")}");
			if (GameObject.Validate(pebble)) pebble.Obliterate();
		}

		// ---------------------------------------------------------------- shape battery (synchronous)

		[WishCommand(Command = "tfshape")]
		public static void Shape()
		{
			if (!Helpers.VERIFY_WATCH) { Help(); return; }
			ShapeSync();
		}

		/// <summary>Stoneshaper through the extracted CarveFrom seam (the pickers stay human):
		/// carve + one-turn cost, swap-down + mourning stamps, natural expiry into an inert statue
		/// that keeps the gear, L10 permanence, limb re-arm on the statue's fists, and copy-of-a-
		/// copy decay. Grants the mutation for the run if missing; a live statue of yours is
		/// swapped out by the first carve.</summary>
		internal static void ShapeSync()
		{
			GameObject ply = The.Player;
			Cell origin = ply?.CurrentCell;
			if (origin == null)
				return;
			List<Cell> open = OpenAround(origin, 2);
			if (open.Count < 3) { Report("shape", false, "SETUP FAIL - need three open cells within 2 for walls"); return; }
			Mutations mutations = ply.RequirePart<Mutations>();
			XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper shaper = ply.GetPart<XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper>();
			bool granted = false;
			int savedLevel = 0, savedCooldown = 0;
			ActivatedAbilityEntry entry = null;
			if (shaper == null)
			{
				shaper = new XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper();
				mutations.AddMutation(shaper, 1);
				granted = true;
			}
			else
			{
				savedLevel = shaper.BaseLevel;
				entry = ply.ActivatedAbilities?.GetAbility(shaper.ActivatedAbilityID);
				savedCooldown = entry?.Cooldown ?? 0;
			}
			int savedCap = shaper.CapOverride;
			bool suppressed = Popup.Suppress;
			Popup.Suppress = true;
			Cleo_TerraFirma_StoneRiseFX.HarnessMute = true;
			try
			{
				shaper.CapOverride = 1;
				shaper.Level = 1;
				ShapeCarveSwapExpiry(ply, shaper, open);
				ShapePermanentRearm(ply, shaper, open[2]);
				ShapeCopyDecay(ply);
			}
			finally
			{
				Cleo_TerraFirma_StoneRiseFX.HarnessMute = false;
				Popup.Suppress = suppressed;
				shaper.StatueLifetimeRemaining = 0;
				if (granted)
					mutations.RemoveMutation(shaper);
				else
				{
					shaper.CapOverride = savedCap;
					shaper.Level = savedLevel;
					if (entry != null)
						entry.Cooldown = savedCooldown;
				}
				SweepSpawns(origin.ParentZone);
			}
		}

		private static GameObject ShapeCarve(GameObject ply, XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper shaper, string museBp, Cell wallCell)
		{
			wallCell.AddObject(GameObject.Create("Sandstone"));
			GameObject muse = Spawn(museBp);
			GameObject statue = shaper.CarveFrom(ply, muse, wallCell);
			statue?.SetIntProperty(SPAWN_MARK, 1);
			muse.Obliterate();
			return statue;
		}

		private static void ShapeCarveSwapExpiry(GameObject ply, XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper shaper, List<Cell> open)
		{
			int energyBefore = ply.Energy?.Value ?? 0;
			GameObject statueA = ShapeCarve(ply, shaper, BABOON_BP, open[0]);
			int spent = energyBefore - (ply.Energy?.Value ?? 0);
			bool carvedA = statueA != null && GameObject.Validate(statueA) && shaper.CurStatue == statueA;
			Report("shape.carve", carvedA && spent >= 900 && spent <= 1100,
				$"carve #1 (Baboon muse): statue {(carvedA ? "stands and is tracked" : "FAILED")}, energy spent {spent} (want one turn: 1000 with the engine's 0.9-1.1 jitter, GameObject.UseEnergy)");

			GameObject statueB = ShapeCarve(ply, shaper, ROBOT_BP, open[1]);
			bool swapped = statueB != null && GameObject.Validate(statueB) && shaper.CurStatue == statueB && statueB != statueA;
			Report("shape.swap", swapped, $"carve #2 (Scrapbot muse): {(swapped ? "swap-down committed, new statue tracked" : "SWAP FAILED")}");
			bool swapQuiet = statueA != null && statueA.GetStringProperty("NoFollowerDeathPopup") == "1";
			bool freshLoud = statueB != null && !statueB.HasTagOrProperty("NoFollowerDeathPopup");
			Report("shape.mourning", swapQuiet && freshLoud,
				$"outgoing statue quiet stamp {(swapQuiet ? "present" : "MISSING")}; incoming statue popup-eligible {(freshLoud ? "yes" : "NO")}");
			if (!swapped)
				return;

			// natural expiry: the statue de-animates into an inert statue that keeps its gear
			GameObject boots = GameObject.Create("Leather Boots");
			statueB.ReceiveObject(boots);
			Cell statueCell = statueB.CurrentCell;
			shaper.StatueLifetimeRemaining = 1;
			EndTurnEvent.Send(ply);
			GameObject inert = null;
			if (statueCell != null)
				foreach (GameObject o in statueCell.Objects)
					if (o.Blueprint == "Cleo_TerraFirma_StoneshaperStatue")
						inert = o;
			bool gearKept = inert?.Inventory != null && inert.Inventory.GetObjectsDirect().Contains(boots);
			Report("shape.expiry", shaper.CurStatue == null && inert != null && gearKept,
				$"lifetime ran out: tracking {(shaper.CurStatue == null ? "released" : "STILL HELD")}, inert statue {(inert != null ? "left in place" : "MISSING")}, its gear {(gearKept ? "kept inside" : "LOST")}");
			inert?.SetIntProperty(SPAWN_MARK, 1);
		}

		private static void ShapePermanentRearm(GameObject ply, XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper shaper, Cell wallCell)
		{
			shaper.CapOverride = 10;
			shaper.Level = 10;
			GameObject statue = ShapeCarve(ply, shaper, "Snapjaw", wallCell);
			if (statue == null) { Report("shape.permanent", false, "SETUP FAIL - the L10 carve failed"); return; }
			bool permanent = shaper.StatueLifetimeRemaining == XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper.GetDuration(10);
			for (int i = 0; i < 5; i++)
				EndTurnEvent.Send(ply);
			bool stands = GameObject.Validate(statue) && shaper.CurStatue == statue;
			Report("shape.permanent", permanent && stands,
				$"L10 carve: lifetime {(permanent ? "permanent" : "FINITE (" + shaper.StatueLifetimeRemaining + ")")}, after 5 turns the statue {(stands ? "still stands and is tracked" : "EXPIRED")}");
			if (!stands)
				return;

			// limb re-arm (audit fix 2026-08-29): regrowing a hand restores the stone fist
			string fist = XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper.DEFAULT_BEHAVIOR_BP;
			BodyPart hand = null;
			foreach (BodyPart p in statue.Body.LoopParts())
				if (p.DefaultBehaviorBlueprint == fist)
				{
					hand = p;
					break;
				}
			if (hand == null) { Report("shape.rearm", false, "SETUP FAIL - the Snapjaw statue has no stone fist to cut"); return; }
			string handType = hand.Type;
			GameObject severed = hand.Dismember();
			if (GameObject.Validate(severed)) severed.Obliterate();
			bool regrew = statue.Body.RegenerateLimb(WholeLimb: true);
			bool rearmed = false;
			foreach (BodyPart p in statue.Body.LoopParts())
				if (p.Type == handType && p.DefaultBehavior?.Blueprint == fist)
					rearmed = true;
			Report("shape.rearm", regrew && rearmed,
				$"cut a {handType}, regrew: {(regrew ? "limb back" : "REGROWTH FAILED")}, stone fist {(rearmed ? "re-armed" : "MISSING (bare hand)")}");
		}

		/// <summary>Copy-of-a-copy decay (RULED 2026-08-12): gen 1 and 2 copy clean, gen 3 loses
		/// 1-3 from one inherited stat. Measured as the drop in (statue - muse) Str+Agi+Tou
		/// between the clean gen-2 copy and the gen-3 copy, so any wall stat rider cancels out.</summary>
		private static void ShapeCopyDecay(GameObject ply)
		{
			GameObject wall = GameObject.Create("Sandstone");
			Cleo_TerraFirma_StoneshaperWallProperties props = wall.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			if (props == null) { Report("shape.copygen", false, "SETUP FAIL - Sandstone lacks StoneshaperWallProperties"); wall.Obliterate(); return; }
			string[] stats = { "Strength", "Agility", "Toughness" };
			int Sum(GameObject o, bool baseValues)
			{
				int n = 0;
				foreach (string s in stats)
					n += baseValues ? o.GetStat(s).BaseValue : o.GetStatValue(s);
				return n;
			}
			GameObject Copy(GameObject muse)
			{
				GameObject s = XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper.MakeStatueOfCreatureFromWall(muse, wall, "fine", props);
				if (s == null)
					return null;
				XRL.World.Parts.AnimateObject.Animate(s);
				XRL.World.Parts.Mutation.Cleo_TerraFirma_Stoneshaper.ApplyStatueBuffs(ply, muse, s, 5, props);
				return s;
			}
			GameObject origin = GameObject.Create("Snapjaw");
			GameObject g1 = Copy(origin);
			GameObject g2 = g1 == null ? null : Copy(g1);
			GameObject g3 = g2 == null ? null : Copy(g2);
			if (g3 == null)
				Report("shape.copygen", false, "SETUP FAIL - a statue copy returned null");
			else
			{
				int gen1 = g1.GetIntProperty("Cleo_TerraFirma_StatueCopyGen");
				int gen2 = g2.GetIntProperty("Cleo_TerraFirma_StatueCopyGen");
				int gen3 = g3.GetIntProperty("Cleo_TerraFirma_StatueCopyGen");
				int clean = Sum(g2, true) - Sum(g1, false);
				int decayed = Sum(g3, true) - Sum(g2, false);
				int loss = clean - decayed;
				Report("shape.copygen", gen1 == 1 && gen2 == 2 && gen3 == 3 && loss >= 1 && loss <= 3,
					$"generations {gen1}/{gen2}/{gen3} (want 1/2/3); gen 3 lost {loss} inherited Str+Agi+Tou relative to the clean gen 2 copy (want 1-3)");
			}
			foreach (GameObject o in new[] { g3, g2, g1, origin, wall })
				if (o != null && GameObject.Validate(o))
					o.Obliterate();
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

		/// <summary>fis-npc: the first NPC-caster scenario — a melee Snapjaw granted Fissure L5,
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
			// a MELEE caster (2026-09-30): the old Hulking Baboon kites since the scavenger merge
			// filled its pockets (topGoal Flee all run, never inside the wave), so it tested the
			// baboon AI, not the cast. HP after placement: zone entry recalculates it.
			GameObject caster = GameObject.Create("Snapjaw");
			caster.SetIntProperty(SPAWN_MARK, 1);
			Mutations mutations = caster.RequirePart<Mutations>();
			mutations.AddMutation(new XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure(), 5);
			ring[0].AddObject(caster);
			caster.GetStat("Hitpoints").BaseValue = 150;
			caster.GetStat("Hitpoints").Penalty = 0;
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

		/// <summary>Redoubt census: every mod rock-family object standing in the zone -- ring
		/// fabric (rubble/crumbled/wall) and pebbles.</summary>
		internal static int CountRingPieces(Zone zone)
		{
			int n = 0;
			if (zone != null)
				foreach (GameObject obj in zone.GetObjects())
					if (obj.CurrentCell != null && obj.InInventory == null && obj.Equipped == null
						&& (obj.Blueprint.StartsWith("Cleo_TerraFirma_Rock_")))
						n += obj.Count;
			return n;
		}

		// ---------------------------------------------------------------- redoubt battery (2026-08-04)

		private const string FIS_COMMAND = "Cleo_TerraFirma_CommandFissure";
		private const string FABRIC_CRUMBLED_BP = "Cleo_TerraFirma_Rock_CrumbledWall";
		private const string FABRIC_WALL_BP = "Cleo_TerraFirma_Rock_Wall";
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
				RedoubtWater(ply, zone, origin, fis);
				RedoubtPebbles(ply, zone, origin, fis);
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
				if (bp != null && (bp.StartsWith("Cleo_TerraFirma_Rock_") || bp == "MediumBoulder"))
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

		/// <summary>09-30 regression after FallenRockPart left for quarantine/: L10 casts until the
		/// wake scatter drops pebbles (3% per interior cell, so up to ten casts), then each pebble
		/// must carry no FallenRockPart, show its approved description, and throw cleanly at a
		/// baboon and land intact.</summary>
		private static void RedoubtPebbles(GameObject ply, Zone zone, Cell origin, XRL.World.Parts.Mutation.Cleo_TerraFirma_Fissure fis)
		{
			const string DESC = "Smooth spall from the world's slow grinding, come down at last.";
			List<GameObject> pebbles = new List<GameObject>();
			List<GameObject> spawnedByCasts = new List<GameObject>();
			int casts = 0;
			while (pebbles.Count == 0 && casts < 10)
			{
				HashSet<GameObject> before = SnapshotZone(zone);
				RedoubtCast(ply, fis, 10);
				casts++;
				foreach (GameObject o in NewSince(zone, before))
				{
					spawnedByCasts.Add(o);
					if (o.Blueprint == PEBBLES_BP && o.CurrentCell != null)
						pebbles.Add(o);
				}
			}
			if (pebbles.Count == 0)
			{
				Report("redoubt.pebbles", false, $"no pebbles in {casts} L10 casts (odds of that are about 1 in 150; rerun before believing it)");
				RedoubtCleanup(spawnedByCasts);
				return;
			}
			int legacy = 0, badDesc = 0;
			foreach (GameObject p in pebbles)
			{
				if (p.GetPart<Cleo_TerraFirma_FallenRockPart>() != null) legacy++;
				if (p.GetPart<Description>()?._Short != DESC) badDesc++;
			}
			string throwNote = "no open cell for the throw target";
			bool thrown = false;
			Cell spot = null;
			foreach (Cell c in origin.GetLocalAdjacentCells(2))
				if (!c.IsSolid() && !c.HasObjectWithPart("Combat") && c.DistanceTo(origin) == 2)
				{
					spot = c;
					break;
				}
			if (spot != null)
			{
				GameObject target = GameObject.Create(BABOON_BP);
				target.SetIntProperty(SPAWN_MARK, 1);
				spot.AddObject(target);
				GameObject stone = ThrowAt(ply, PEBBLES_BP, target);
				thrown = stone != null && GameObject.Validate(stone) && stone.CurrentCell != null;
				throwNote = stone == null ? "the throw never happened (no thrown slot?)" : thrown ? "a thrown pebble landed intact" : "the thrown pebble VANISHED";
				if (GameObject.Validate(stone)) stone.Obliterate();
			}
			Report("redoubt.pebbles", legacy == 0 && badDesc == 0 && thrown,
				$"{pebbles.Count} pebble(s) in {casts} cast(s): {legacy} still carrying FallenRockPart (want 0), {badDesc} with the wrong description (want 0); {throwNote}");
			RedoubtCleanup(spawnedByCasts);
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
				case "scav": SetupScav(ply, rest); break;
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
			// REDOUBT shape (pivot 2026-08-02): a cast deposits ring fabric (rubble/crumbled/wall)
			// and wake pebbles -- count ring pieces by prefix.
			bool casterAlive = GameObject.Validate(FisCaster);
			int piecesNow = Cleo.TerraFirma.Scripts.TestHarnessWishes.CountRingPieces(owner.CurrentZone);
			int delta = piecesNow - FisRocksBaseline;
			bool pass = casterAlive && delta >= 5;
			Cleo.TerraFirma.Scripts.TestHarnessWishes.Report("fis", pass,
				$"NPC redoubt caster over {TurnsTotal} turns: ring pieces {FisRocksBaseline}->{piecesNow} (+{delta}, want >=5 = a wave happened and deposited a rim). A zero here answers fis-npc: no AI advertisement on the mutation.");
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
