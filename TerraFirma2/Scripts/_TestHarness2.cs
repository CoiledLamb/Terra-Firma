using System;
using System.Collections.Generic;
using XRL;
using XRL.Messages;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Effects;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// Terra Firma 2's half of the verification harness (2026-09-30). TF1's harness cannot see
	/// TF2 types, so TF2 checks live here with their own Report. Same idioms: build the scenario
	/// in the live zone, drive real engine calls, assert on engine state, log [TF-VERIFY] [AUTO]
	/// lines. Everything is synchronous. Dev-only: publish-beta.ps1 drops this file from player
	/// builds, and every wish rides Helpers.VERIFY_WATCH.
	///
	///   tfgear   -- mudskin girdle, flint-spike pauldrons, gaslight gauntlets (worn by a Snapjaw;
	///               the carve checks borrow your hands for a moment and give them back)
	///   tfmoss   -- cave moss cooking (grant, boost, 50% pebble trigger, throw signal) and the
	///               adjacent auto-harvest (lends you Harvestry for the run if you lack it)
	///   tfcairn  -- topple, owner kept, the owner's people pass, restack blocked, restack
	///   tfbatch2 -- all three, ending with one verdict line
	/// </summary>
	[HasWishCommand]
	public class TestHarness2Wishes
	{
		private const string SPAWN_MARK = "TFHarnessSpawn";   // the TF1 harness sweeps the same mark

		private static int Pass, Fail;
		private static readonly List<string> Fails = new List<string>();

		private static void Report(string scenario, bool pass, string detail)
		{
			if (pass) Pass++;
			else { Fail++; Fails.Add(scenario); }
			Helpers.VerifyLog("AUTO", $"SCENARIO {scenario}: {(pass ? "PASS" : "FAIL")} - {detail}");
			MessageQueue.AddPlayerMessage($"Harness {scenario}: {(pass ? "&GPASS" : "&RFAIL")}&y - {detail}");
		}

		private static bool Begin()
		{
			if (!Helpers.VERIFY_WATCH)
			{
				MessageQueue.AddPlayerMessage("VERIFY_WATCH is OFF -- the harness is disabled.", "R");
				return false;
			}
			return The.Player?.CurrentCell != null;
		}

		private static void Run(Action body)
		{
			bool suppressed = Popup.Suppress;
			Popup.Suppress = true;
			try { body(); }
			catch (Exception ex)
			{
				Report("harness2", false, $"THREW {ex.GetType().Name}: {ex.Message}");
				Helpers.VerifyLog("AUTO", ex.ToString());
			}
			finally
			{
				Popup.Suppress = suppressed;
				Sweep(The.Player?.CurrentZone);
			}
		}

		private static void Sweep(Zone zone)
		{
			if (zone == null)
				return;
			List<GameObject> marked = new List<GameObject>();
			foreach (GameObject o in zone.GetObjects())
				if (o.GetIntProperty(SPAWN_MARK) == 1 && !o.IsPlayer())
					marked.Add(o);
			foreach (GameObject o in marked)
				o.Obliterate();
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

		private static List<Cell> OpenAround(Cell origin, int radius)
		{
			List<Cell> open = new List<Cell>(origin.GetLocalAdjacentCells(radius));
			open.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat"));
			return open;
		}

		private static GameObject SturdySnapjaw(Cell c)
		{
			GameObject s = Spawn("Snapjaw", c);
			if (s != null)
			{
				s.GetStat("Hitpoints").BaseValue = 400;   // after placement: zone entry recalculates HP
				s.GetStat("Hitpoints").Penalty = 0;
			}
			return s;
		}

		[WishCommand(Command = "tfbatch2")]
		public static void Batch()
		{
			if (!Begin()) return;
			Pass = 0; Fail = 0; Fails.Clear();
			Run(GearChecks);
			Run(MossChecks);
			Run(CairnChecks);
			string verdict = Fail == 0 ? "ALL GREEN" : "FAILS: " + string.Join(", ", Fails);
			Helpers.VerifyLog("AUTO", $"BATCH2 COMPLETE: {Pass} pass / {Fail} fail -- {verdict}");
			MessageQueue.AddPlayerMessage($"Harness batch2 complete: {Pass} pass / {Fail} fail -- {verdict}", Fail == 0 ? "G" : "R");
		}

		[WishCommand(Command = "tfgear")]
		public static void Gear() { if (Begin()) Run(GearChecks); }

		[WishCommand(Command = "tfmoss")]
		public static void Moss() { if (Begin()) Run(MossChecks); }

		[WishCommand(Command = "tfcairn")]
		public static void Cairn() { if (Begin()) Run(CairnChecks); }

		// ---------------------------------------------------------------- gear

		private static void GearChecks()
		{
			Cell origin = The.Player.CurrentCell;
			List<Cell> open = OpenAround(origin, 2);
			if (open.Count < 2) { Report("gear", false, "SETUP FAIL - need two open cells within 2"); return; }
			Girdle(open[0]);
			Pauldrons(open[0], open);
			Gauntlets(open[0]);
			GauntletCarve(origin);
		}

		private static void Girdle(Cell c)
		{
			GameObject wearer = SturdySnapjaw(c);
			GameObject girdle = GameObject.Create("Cleo_TerraFirma_MudskinGirdle");
			wearer.ReceiveObject(girdle);
			if (!wearer.ForceEquipObject(girdle, "Body", Silent: true)) { Report("gear.girdle", false, "SETUP FAIL - could not wear the girdle"); wearer.Obliterate(); return; }
			int av0 = wearer.GetStatValue("AV");
			int Stacks() => wearer.TryGetEffect<Cleo_TerraFirma_Scabbed>(out var s) ? s.Stacks : 0;
			void Bleed() => wearer.TakeDamage(1, "from bleeding.", "Bleeding Unavoidable");
			Bleed();
			int s1 = Stacks(), av1 = wearer.GetStatValue("AV");
			wearer.TakeDamage(1, "from a harness slap.", "Physical");
			int sHit = Stacks();
			Bleed(); Bleed();
			int s3 = Stacks(), av3 = wearer.GetStatValue("AV");
			// flake: run the clock out twice (Countdown restarts at FlakeInterval after each step)
			wearer.TryGetEffect<Cleo_TerraFirma_Scabbed>(out var scab);
			if (scab != null) scab.Countdown = 1;
			EndTurnEvent.Send(wearer);
			int sFlake1 = Stacks();
			if (wearer.TryGetEffect<Cleo_TerraFirma_Scabbed>(out scab)) scab.Countdown = 1;
			EndTurnEvent.Send(wearer);
			bool gone = !wearer.HasEffect<Cleo_TerraFirma_Scabbed>();
			int avAfter = wearer.GetStatValue("AV");
			// unequip clears a fresh hardening
			Bleed();
			bool hardAgain = Stacks() == 1;
			girdle.ForceUnequip(Silent: true);
			bool clearedOnUnequip = !wearer.HasEffect<Cleo_TerraFirma_Scabbed>();
			bool pass = s1 == 1 && av1 == av0 + 1 && sHit == 1 && s3 == 2 && av3 == av0 + 2
				&& sFlake1 == 1 && gone && avAfter == av0 && hardAgain && clearedOnUnequip;
			Report("gear.girdle", pass,
				$"bleed 1: stacks {s1}, AV {av0}->{av1} (want 1, +1); plain hit leaves {sHit}; bleed 3: stacks {s3}, AV +{av3 - av0} (want cap 2); flake steps to {sFlake1} then {(gone ? "gone" : "STUCK")}, AV back to {avAfter} (want {av0}); unequip {(clearedOnUnequip ? "clears it" : "LEAVES IT")}");
			wearer.Obliterate();
		}

		private static void Pauldrons(Cell c, List<Cell> open)
		{
			GameObject wearer = SturdySnapjaw(c);
			GameObject pauldrons = GameObject.Create("Cleo_TerraFirma_FlintSpikePauldrons");
			wearer.ReceiveObject(pauldrons);
			if (!wearer.ForceEquipObject(pauldrons, "Body", Silent: true)) { Report("gear.pauldrons", false, "SETUP FAIL - could not wear the pauldrons"); wearer.Obliterate(); return; }
			Cell next = null;
			foreach (Cell a in c.GetLocalAdjacentCells())
				if (!a.IsSolid() && !a.HasObjectWithPart("Combat"))
				{
					next = a;
					break;
				}
			if (next == null) { Report("gear.pauldrons", false, "SETUP FAIL - no open cell beside the wearer"); wearer.Obliterate(); return; }
			GameObject attacker = SturdySnapjaw(next);
			bool Bleeding() => attacker.HasEffect<XRL.World.Effects.Bleeding>();
			// no weapon: breath and blast shapes must not bleed the source
			wearer.TakeDamage(3, "from a harness blast.", "Fire", Attacker: attacker);
			bool blastClean = !Bleeding();
			wearer.TakeDamage(3, "from a harness tick.", "Physical", Attacker: attacker, Indirect: true);
			bool indirectClean = !Bleeding();
			// a real adjacent melee hit, forced to land and penetrate
			BodyPart hand = null;
			GameObject weapon = attacker.Body?.GetMainWeapon(out int _, out hand);
			bool swung = false;
			if (weapon != null)
			{
				Combat.MeleeAttackWithWeapon(attacker, wearer, weapon, hand, "Autohit,Autopen");
				swung = true;
			}
			bool meleeBleeds = Bleeding();
			string rules = pauldrons.GetPart<Description>()?.GetShortDescription(true) ?? "";
			bool rulesLine = rules.Contains("difficulty " + pauldrons.GetPart<Cleo_TerraFirma_FlintSpikePauldrons>().GetSaveTarget());
			Report("gear.pauldrons", blastClean && indirectClean && swung && meleeBleeds && rulesLine,
				$"weaponless damage {(blastClean ? "no bleed" : "BLED")}; indirect {(indirectClean ? "no bleed" : "BLED")}; adjacent melee {(!swung ? "NO WEAPON TO SWING" : meleeBleeds ? "bleeds the attacker" : "NO BLEED")}; rules line {(rulesLine ? "shows the save difficulty" : "MISSING the difficulty")}");
			attacker.Obliterate();
			wearer.Obliterate();
		}

		private static GameObject ChargedGauntlets(int charge)
		{
			GameObject gauntlets = GameObject.Create("Cleo_TerraFirma_GaslightGauntlets");
			GameObject cell = GameObject.Create("Chem Cell");
			EnergyCell ec = cell?.GetPart<EnergyCell>();
			if (ec != null)
				ec.Charge = Math.Min(charge, ec.MaxCharge);
			gauntlets.GetPart<EnergyCellSocket>()?.SetCell(cell);
			return gauntlets;
		}

		private static void Gauntlets(Cell c)
		{
			GameObject wearer = SturdySnapjaw(c);
			GameObject gauntlets = ChargedGauntlets(10000);
			wearer.ReceiveObject(gauntlets);
			if (!wearer.ForceEquipObject(gauntlets, "Hands", Silent: true)) { Report("gear.gauntlets", false, "SETUP FAIL - could not wear the gauntlets"); wearer.Obliterate(); return; }
			var part = gauntlets.GetPart<Cleo_TerraFirma_GaslightGauntlets>();
			GameObject fist = null;
			int fists = 0;
			foreach (BodyPart hand in wearer.Body.GetPart("Hand"))
				if (hand.DefaultBehavior?.Blueprint == part.FistObject)
				{
					fists++;
					fist = hand.DefaultBehavior;
				}
			// read as identified: the gauntlets carry Examiner Complexity 4, so a fresh spawn shows
			// its unknown-gloves name to the player and the gaslight swap never sees its base name
			string Named() => gauntlets.GetDisplayName(AsIfKnown: true);
			bool chargedName = Named().Contains("{{gaslight|gaslight}}");
			int Pen(out int spent)
			{
				int before = gauntlets.QueryCharge(LiveOnly: false, 0L);
				int pens = 0, stat = 0, maxStat = 0, pb = 0, maxPb = 0;
				GetWeaponMeleePenetrationEvent.Process(ref pens, ref stat, ref maxStat, ref pb, ref maxPb, 0, false, "", "", wearer, null, fist);
				spent = before - gauntlets.QueryCharge(LiveOnly: false, 0L);
				return pb;
			}
			int spentFull = 0;
			int pvFull = fist == null ? -1 : Pen(out spentFull);
			EnergyCell ec = gauntlets.GetPart<EnergyCellSocket>()?.Cell?.GetPart<EnergyCell>();
			if (ec != null) ec.Charge = 0;
			bool plainName = !Named().Contains("{{gaslight|gaslight}}");
			int pvEmpty = fist == null ? -1 : Pen(out int _);
			gauntlets.ForceUnequip(Silent: true);
			int fistsLeft = 0;
			foreach (BodyPart hand in wearer.Body.GetPart("Hand"))
				if (hand.DefaultBehavior?.Blueprint == part.FistObject)
					fistsLeft++;
			bool pass = fists >= 1 && chargedName && pvFull == 5 && spentFull == part.PunchCharge && plainName && pvEmpty == 0 && fistsLeft == 0;
			Report("gear.gauntlets", pass,
				$"worn: {fists} gaslight fist(s) (want >=1); name {(chargedName ? "gaslit" : "PLAIN")} while charged, {(plainName ? "plain" : "STILL GASLIT")} when empty; punch PV +{pvFull} for {spentFull} charge (want +5 for {part.PunchCharge}), empty cell +{pvEmpty} (want 0); off: {fistsLeft} gaslight fist(s) left (want 0)");
			wearer.Obliterate();
		}

		/// <summary>The carve verb is player-only, so this borrows the player's hands: take off
		/// whatever is on them, wear test gauntlets, check the action on natural stone versus a
		/// built wall, check the low-charge refusal spends nothing, then give the hands back.</summary>
		private static void GauntletCarve(Cell origin)
		{
			GameObject ply = The.Player;
			BodyPart hands = ply.Body?.GetFirstPart("Hands");
			if (hands == null) { Report("gear.carve", false, "SETUP FAIL - you have no Hands slot"); return; }
			List<Cell> open = OpenAround(origin, 1);
			if (open.Count < 2) { Report("gear.carve", false, "SETUP FAIL - need two open cells beside you"); return; }
			GameObject stone = Spawn("Sandstone", open[0]);
			GameObject built = Spawn("MetalWall", open[1]);
			GameObject previous = hands.Equipped;
			if (previous != null)
				hands.ForceUnequip(Silent: true);
			GameObject gauntlets = ChargedGauntlets(500);   // under the 1000 carve cost
			gauntlets.SetIntProperty(SPAWN_MARK, 1);
			ply.ReceiveObject(gauntlets);
			try
			{
				ply.ForceEquipObject(gauntlets, hands, Silent: true);
				bool Offers(GameObject wall)
				{
					var actions = new Dictionary<string, InventoryAction>();
					GetInventoryActionsEvent.Send(ply, wall, actions);
					return actions.ContainsKey("Carve");
				}
				bool onStone = Offers(stone);
				bool onBuilt = Offers(built);
				int before = gauntlets.QueryCharge(LiveOnly: false, 0L);
				InventoryActionEvent.Check(stone, ply, stone, Cleo_TerraFirma_Carvable.COMMAND);
				bool spentNothing = gauntlets.QueryCharge(LiveOnly: false, 0L) == before && GameObject.Validate(stone) && stone.CurrentCell == open[0];
				gauntlets.ForceUnequip(Silent: true);
				bool offBare = !Offers(stone);
				Report("gear.carve", onStone && !onBuilt && spentNothing && offBare,
					$"Carve offered on natural stone: {(onStone ? "yes" : "NO")}; on a metal wall: {(onBuilt ? "YES (should not)" : "no")}; 500-charge attempt: {(spentNothing ? "refused, nothing spent, wall intact" : "SPENT OR CARVED")}; with bare hands: {(offBare ? "not offered" : "STILL OFFERED")}");
			}
			finally
			{
				if (gauntlets.Equipped != null)
					gauntlets.ForceUnequip(Silent: true);
				gauntlets.Obliterate();
				if (previous != null && GameObject.Validate(previous))
					ply.ForceEquipObject(previous, hands, Silent: true);
			}
		}

		// ---------------------------------------------------------------- moss

		private static void MossChecks()
		{
			Cell origin = The.Player.CurrentCell;
			List<Cell> open = OpenAround(origin, 2);
			if (open.Count < 2) { Report("moss", false, "SETUP FAIL - need two open cells within 2"); return; }
			MossUnit();
			MossTrigger();
			MossSignal(open);
			MossHarvest(origin);
		}

		private static void MossUnit()
		{
			// grant: an eater without Earthen Barrage gains it, and loses it when the meal wears off
			GameObject eater = Spawn("Snapjaw");
			ProceduralCookingEffect meal = ProceduralCookingEffect.CreateJustUnits(new List<string> { "Cleo_TerraFirma_caveMoss" });
			meal.Init(eater);   // base cooking inits before applying (ProceduralCookingEffect.cs:114 throws otherwise)
			bool applied = eater.ApplyEffect(meal);
			bool granted = eater.HasPart<Cleo_TerraFirma_EarthenBarrage>();
			eater.RemoveEffect(meal);
			bool revoked = !eater.HasPart<Cleo_TerraFirma_EarthenBarrage>();
			// boost: an eater with it at level 3 gets more, and drops back after
			GameObject barrager = Spawn("Snapjaw");
			var eb = new Cleo_TerraFirma_EarthenBarrage();
			barrager.RequirePart<Mutations>().AddMutation(eb, 3);
			int baseLevel = eb.Level;
			ProceduralCookingEffect meal2 = ProceduralCookingEffect.CreateJustUnits(new List<string> { "Cleo_TerraFirma_caveMoss" });
			meal2.Init(barrager);
			barrager.ApplyEffect(meal2);
			int boosted = barrager.GetPart<Cleo_TerraFirma_EarthenBarrage>()?.Level ?? -1;
			barrager.RemoveEffect(meal2);
			int after = barrager.GetPart<Cleo_TerraFirma_EarthenBarrage>()?.Level ?? -1;
			Report("moss.unit", applied && granted && revoked && boosted > baseLevel && after == baseLevel,
				$"no Barrage: meal {(granted ? "grants it" : "GRANTS NOTHING")}, wearing off {(revoked ? "takes it back" : "LEAVES IT")}; Barrage L{baseLevel}: meal raises it to L{boosted}, back to L{after} after (want {baseLevel})");
			eater.Obliterate();
			barrager.Obliterate();
		}

		private static int CountCarried(GameObject who, string blueprint)
		{
			int n = 0;
			foreach (GameObject o in who.GetInventoryAndEquipment())
				if (o.Blueprint == blueprint)
					n += o.Count;
			return n;
		}

		private static void MossTrigger()
		{
			GameObject eater = Spawn("Snapjaw");
			BodyPart slot = eater.GetFirstBodyPart("Thrown Weapon");
			if (slot?.Equipped != null)
				slot.ForceUnequip(Silent: true);
			ProceduralCookingEffectWithTrigger meal = ProceduralCookingEffect.CreateTriggeredAction("Cleo_TerraFirma_caveMoss", "Cleo_TerraFirma_caveMoss");
			meal.Init(eater);
			eater.ApplyEffect(meal);
			const int TRIES = 40;
			for (int i = 0; i < TRIES; i++)
				eater.FireEvent(Event.New(Cleo_TerraFirma_CookingDomainCaveMoss_OnThrowStone.THREW_STONE));
			int pebbles = CountCarried(eater, "Cleo_TerraFirma_MossPebble");
			bool slotted = slot == null || slot.Equipped?.Blueprint == "Cleo_TerraFirma_MossPebble";
			eater.RemoveEffect(meal);
			// 40 fair coins land 8-32 heads all but about 1 time in 100,000
			Report("moss.trigger", pebbles >= 8 && pebbles <= 32 && slotted,
				$"{TRIES} stone throws gave {pebbles} sculpted pebble(s) (want about half, 8-32); first one {(slotted ? "went to the thrown slot" : "DID NOT reach the empty thrown slot")}");
			eater.Obliterate();
		}

		/// <summary>Counts the signal a thrown stone forwards to its thrower.</summary>
		[Serializable]
		public class Ear : IPart
		{
			public int Heard;

			public override void Register(GameObject Object, IEventRegistrar Registrar)
			{
				Registrar.Register(Cleo_TerraFirma_CookingDomainCaveMoss_OnThrowStone.THREW_STONE);
				base.Register(Object, Registrar);
			}

			public override bool FireEvent(Event E)
			{
				if (E.ID == Cleo_TerraFirma_CookingDomainCaveMoss_OnThrowStone.THREW_STONE)
					Heard++;
				return base.FireEvent(E);
			}
		}

		private static void MossSignal(List<Cell> open)
		{
			GameObject thrower = SturdySnapjaw(open[0]);
			GameObject target = SturdySnapjaw(open[1]);
			Ear ear = new Ear();
			thrower.AddPart(ear);
			BodyPart slot = thrower.GetFirstBodyPart("Thrown Weapon");
			GameObject stone = GameObject.Create("Small Stone");
			bool performed = false;
			if (slot != null)
			{
				if (slot.Equipped != null)
					slot.ForceUnequip(Silent: true);
				thrower.ForceEquipObject(stone, slot, Silent: true);
				thrower.ModIntProperty("CloseThrowRangeAccuracyBonus", 100);
				try { performed = thrower.PerformThrow(stone, target.CurrentCell, target); }
				finally { thrower.ModIntProperty("CloseThrowRangeAccuracyBonus", -100, RemoveIfZero: true); }
			}
			Report("moss.signal", performed && ear.Heard == 1,
				!performed ? "SETUP FAIL - the Snapjaw would not throw" : $"one thrown Small Stone told its thrower {ear.Heard} time(s) (want 1)");
			if (GameObject.Validate(stone)) stone.Obliterate();
		}

		private static void MossHarvest(Cell origin)
		{
			GameObject ply = The.Player;
			Cell wallCell = null;
			foreach (Cell c in OpenAround(origin, 1))
			{
				wallCell = c;
				break;
			}
			Cell far = null;
			foreach (Cell c in OpenAround(origin, 2))
				if (c.DistanceTo(origin) == 2)
				{
					far = c;
					break;
				}
			if (wallCell == null || far == null) { Report("moss.harvest", false, "SETUP FAIL - need an open cell beside you and one two away"); return; }
			GameObject MossWall(Cell c)
			{
				GameObject w = Spawn("Shale", c);
				w.SetStringProperty("PaintedWall", Cleo_TerraFirma_ShaleMoss.VINE_PAINT);
				var moss = w.GetPart<Cleo_TerraFirma_ShaleMoss>();
				if (moss != null) moss.StartRipeChance = "1:1";
				ZoneBuiltEvent ev = ZoneBuiltEvent.FromPool();
				ev.Zone = c.ParentZone;
				w.HandleEvent(ev);
				return w;
			}
			GameObject near = MossWall(wallCell);
			GameObject away = MossWall(far);
			Harvestable h = near.GetPart<Harvestable>();
			if (h == null || !h.Ripe || near.GetPart<Harvestable>() == null) { Report("moss.harvest", false, "SETUP FAIL - the test shale did not grow ripe moss"); return; }

			bool lent = false;
			CookingAndGathering_Harvestry skill = ply.GetPart<CookingAndGathering_Harvestry>();
			if (skill == null)
			{
				ply.AddSkill("CookingAndGathering_Harvestry");
				skill = ply.GetPart<CookingAndGathering_Harvestry>();
				lent = true;
			}
			ActivatedAbilityEntry toggle = skill == null ? null : ply.ActivatedAbilities?.GetAbility(skill.ActivatedAbilityID);
			bool savedToggle = toggle?.ToggleState ?? false;
			try
			{
				if (toggle == null) { Report("moss.harvest", false, "SETUP FAIL - no Harvestry toggle"); return; }
				int start = CountCarried(ply, "Cleo_TerraFirma_MossClump");
				toggle.ToggleState = false;
				EndTurnEvent.Send(near);
				int offGot = CountCarried(ply, "Cleo_TerraFirma_MossClump") - start;
				toggle.ToggleState = true;
				EndTurnEvent.Send(away);
				int farGot = CountCarried(ply, "Cleo_TerraFirma_MossClump") - start;
				EndTurnEvent.Send(near);
				int onGot = CountCarried(ply, "Cleo_TerraFirma_MossClump") - start;
				for (int i = 0; i < 3; i++)
					EndTurnEvent.Send(near);
				int total = CountCarried(ply, "Cleo_TerraFirma_MossClump") - start;
				bool reverted = !near.GetPart<Harvestable>().Ripe && !near.DisplayName.Contains("mossy");
				bool hostilesNear = ply.ArePerceptibleHostilesNearby(logSpot: false, popSpot: false, null, null, null, Options.AutoexploreIgnoreEasyEnemies, Options.AutoexploreIgnoreDistantEnemies);
				Report("moss.harvest", offGot == 0 && farGot == 0 && onGot == 1 && total == 1 && reverted,
					$"toggle off: {offGot} clump(s) (want 0); two tiles away: {farGot} (want 0); adjacent with toggle on: {onGot} (want 1); after 3 more turns: {total} (want 1, no duplicate); wall {(reverted ? "back to plain shale" : "STILL MOSSY")}" +
					(hostilesNear && onGot == 0 ? " -- hostiles in view block automatic harvest, rerun somewhere quiet" : ""));
				foreach (GameObject o in ply.GetInventoryAndEquipment())
					if (o.Blueprint == "Cleo_TerraFirma_MossClump")
					{
						o.Obliterate();
						break;
					}
			}
			finally
			{
				if (toggle != null)
					toggle.ToggleState = savedToggle;
				if (lent)
					ply.RemoveSkill("CookingAndGathering_Harvestry");
			}
		}

		// ---------------------------------------------------------------- cairn

		private static void CairnChecks()
		{
			GameObject ply = The.Player;
			Cell origin = ply.CurrentCell;
			Cell spot = null, side = null;
			foreach (Cell c in OpenAround(origin, 1))
			{
				foreach (Cell s in OpenAround(c, 1))
					if (s != origin && s != c)
					{
						spot = c;
						side = s;
						break;
					}
				if (spot != null)
					break;
			}
			if (spot == null) { Report("cairn", false, "SETUP FAIL - need an open cell beside you with an open neighbor"); return; }
			GameObject local = Spawn("Cleo_TerraFirma_Hewer", side);
			string owner = local?.GetPrimaryFaction();
			if (string.IsNullOrEmpty(owner)) { Report("cairn", false, "SETUP FAIL - the hewer has no primary faction to own a cairn"); return; }

			GameObject cairn = Spawn("Cleo_TerraFirma_Cairn", spot);
			cairn.Physics.Owner = owner;
			// the owner's people pass
			local.DirectMoveTo(spot, 0, Forced: true, IgnoreCombat: true);
			bool localPassed = GameObject.Validate(cairn) && cairn.CurrentCell == spot;
			local.DirectMoveTo(side, 0, Forced: true, IgnoreCombat: true);
			local.Obliterate();

			// a stranger topples it; the scatter keeps the owner
			GameObject stranger = SturdySnapjaw(side);
			stranger.DirectMoveTo(spot, 0, Forced: true, IgnoreCombat: true);
			GameObject scatter = null;
			foreach (GameObject o in spot.Objects)
				if (o.Blueprint == "Cleo_TerraFirma_CairnToppled")
					scatter = o;
			bool toppled = !GameObject.Validate(cairn) && scatter != null;
			bool ownerKept = scatter?.Physics?.Owner == owner;
			if (scatter == null) { Report("cairn", false, $"the owner's people {(localPassed ? "passed" : "TOPPLED IT")}; a stranger's step left NO scatter"); return; }
			scatter.SetIntProperty(SPAWN_MARK, 1);

			// restack blocked while the stranger stands there, then allowed
			InventoryActionEvent.Check(scatter, ply, scatter, Cleo_TerraFirma_CairnRestack.COMMAND);
			bool blocked = GameObject.Validate(scatter) && scatter.CurrentCell == spot;
			stranger.DirectMoveTo(side, 0, Forced: true, IgnoreCombat: true);
			int energyBefore = ply.Energy?.Value ?? 0;
			InventoryActionEvent.Check(scatter, ply, scatter, Cleo_TerraFirma_CairnRestack.COMMAND);
			GameObject standing = null;
			foreach (GameObject o in spot.Objects)
				if (o.Blueprint == "Cleo_TerraFirma_Cairn")
					standing = o;
			standing?.SetIntProperty(SPAWN_MARK, 1);
			int spent = energyBefore - (ply.Energy?.Value ?? 0);
			bool restacked = standing != null && standing.Physics?.Owner == owner && !GameObject.Validate(scatter);
			Report("cairn.restack", localPassed && toppled && ownerKept && blocked && restacked && spent >= 900 && spent <= 1100,
				$"owner's people {(localPassed ? "pass over it" : "TOPPLED IT")}; stranger {(toppled ? "topples it" : "DID NOT TOPPLE IT")}, scatter owner {(ownerKept ? "kept" : "LOST")}; restack with someone standing there {(blocked ? "refused" : "ALLOWED")}; restack when clear {(restacked ? "stands it back up, owner kept" : "FAILED")}, energy {spent} (want one turn, 900-1100 with the engine's jitter)");
		}
	}
}
