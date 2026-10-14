using System;
using System.Collections.Generic;
using XRL;
using XRL.Messages;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.AI.GoalHandlers;
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
	///   tffrozen -- the 2026-10-14 frozen gates (TF1 and TF2 actions both; run by hand, not in
	///               tfbatch2): each gated action refuses frozen and spends nothing, the ungated
	///               mutations still answer frozen; ends with its own verdict line
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

		[WishCommand(Command = "tffrozen")]
		public static void Frozen()
		{
			if (!Begin()) return;
			Pass = 0; Fail = 0; Fails.Clear();
			Run(FrozenChecks);
			string verdict = Fail == 0 ? "ALL GREEN" : "FAILS: " + string.Join(", ", Fails);
			Helpers.VerifyLog("AUTO", $"FROZEN COMPLETE: {Pass} pass / {Fail} fail -- {verdict}");
			MessageQueue.AddPlayerMessage($"Harness frozen complete: {Pass} pass / {Fail} fail -- {verdict}", Fail == 0 ? "G" : "R");
		}

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
			eb.CapOverride = 20;   // CalcLevel caps AFTER food and gear bonuses; a snapjaw's cap is 1, which leaves the boost no room
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
			thrower.Obliterate();   // hostiles in view block automatic harvest, which runs next
			target.Obliterate();
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

		// ---------------------------------------------------------------- patch battery (0.2.5/0.2.6)

		[WishCommand(Command = "tfpatch2")]
		public static void Patch2()
		{
			if (!Begin()) return;
			Pass = 0; Fail = 0; Fails.Clear();
			Run(PatchOutriderHome);
			Run(PatchAutoexploreGuard);
			Run(PatchOldMillFeed);
			Run(PatchGranitefurPrices);
			Helpers.VerifyLog("AUTO", "SCENARIO patch.outrider.note: SKIP - the map note is stamped during a warren build (WarrenCarver.StampPatrols), which a harness cannot rerun; check a fresh warren in play");
			string verdict = Fail == 0 ? "ALL GREEN" : "FAILS: " + string.Join(", ", Fails);
			Helpers.VerifyLog("AUTO", $"PATCH2 COMPLETE: {Pass} pass / {Fail} fail -- {verdict}");
			MessageQueue.AddPlayerMessage($"Harness patch2 complete: {Pass} pass / {Fail} fail -- {verdict}", Fail == 0 ? "G" : "R");
		}

		/// <summary>0.2.5's outrider guard: with Brain.StartingCell null (the reporter's state),
		/// taking the post and walking the beat must not throw, and both leave a home point.</summary>
		private static void PatchOutriderHome()
		{
			Cell origin = The.Player.CurrentCell;
			List<Cell> open = OpenAround(origin, 3);
			if (open.Count < 3) { Report("patch.outrider.home", false, "SETUP FAIL - need three open cells within 3"); return; }
			GameObject outrider = Spawn("Cleo_TerraFirma_Outrider", open[0]);
			var patrol = outrider?.GetPart<Cleo_TerraFirma_OutriderPatrol>();
			if (patrol == null || outrider.Brain == null) { Report("patch.outrider.home", false, "SETUP FAIL - no outrider patrol part or brain"); return; }
			patrol.Beat = $"{open[1].X},{open[1].Y};{open[2].X},{open[2].Y}";
			patrol.Rest = $"{open[0].X},{open[0].Y}";
			long now = The.Game.TimeTicks;
			bool OnBeat(Cell c) => c == open[1] || c == open[2] || c == outrider.CurrentCell;
			string takeThrew = null, walkThrew = null;
			outrider.Brain.StartingCell = null;
			try { patrol.TakeDuty(now); }
			catch (Exception ex) { takeThrew = ex.GetType().Name; }
			Cell afterTake = outrider.Brain.StartingCell?.ResolveCell();
			outrider.Brain.StartingCell = null;
			patrol.OnDuty = true;
			patrol.PauseEnds = 0;
			patrol.LegStarted = now;
			try { patrol.HandleEvent(PooledEvent<AIBoredEvent>.FromPool()); }
			catch (Exception ex) { walkThrew = ex.GetType().Name; }
			Cell afterWalk = outrider.Brain.StartingCell?.ResolveCell();
			bool pass = takeThrew == null && walkThrew == null && afterTake != null && OnBeat(afterTake) && afterWalk != null && OnBeat(afterWalk);
			Report("patch.outrider.home", pass,
				$"null home, take the post: {(takeThrew != null ? "THREW " + takeThrew : afterTake == null ? "no home set" : OnBeat(afterTake) ? "home on a waypoint" : "home OFF the beat")}; null home, a bored turn on the beat: {(walkThrew != null ? "THREW " + walkThrew : afterWalk == null ? "no home set" : OnBeat(afterWalk) ? "home on the beat" : "home OFF the beat")}");
		}

		/// <summary>0.2.6's autoexplore guard, asked the way autoexplore asks
		/// (AutoexploreObjectEvent.GetAdjacentAction) with "autoget ammo" on. Control: an
		/// untakeable energy cell with no guard is the bug's shape and does come back "Autoget".
		/// Also an old-save mill (EnergyCell, no Capacitor) and an old-save table.</summary>
		private static void PatchAutoexploreGuard()
		{
			GameObject ply = The.Player;
			List<Cell> open = OpenAround(ply.CurrentCell, 3);
			if (open.Count < 5) { Report("patch.autoexplore", false, "SETUP FAIL - need five open cells within 3"); return; }
			GameObject OldSave(GameObject o)
			{
				o.RemovePart<Capacitor>();
				EnergyCell cell = new EnergyCell();
				cell.MaxCharge = 25000;
				cell.Charge = 0;
				o.AddPart(cell);
				return o;
			}
			GameObject table = Spawn("Cleo_TerraFirma_AnimationTable", open[0]);
			GameObject mill = Spawn("Cleo_TerraFirma_PiezoelectricMill", open[1]);
			GameObject oldTable = OldSave(Spawn("Cleo_TerraFirma_AnimationTable", open[2]));
			GameObject oldMill = OldSave(Spawn("Cleo_TerraFirma_PiezoelectricMill", open[3]));
			GameObject bare = Spawn("Chem Cell", open[4]);
			bare.Physics.Takeable = false;
			bool saved = Options.AutogetAmmo;
			Options.AutogetAmmo = true;
			try
			{
				string Ask(GameObject o) => AutoexploreObjectEvent.GetAdjacentAction(ply, o, "?", null) ?? "nothing";
				string t = Ask(table), m = Ask(mill), ot = Ask(oldTable), om = Ask(oldMill), b = Ask(bare);
				bool guarded = t != "Autoget" && m != "Autoget" && ot != "Autoget" && om != "Autoget";
				bool still = GameObject.Validate(table) && table.CurrentCell == open[0] && GameObject.Validate(mill) && mill.CurrentCell == open[1];
				Report("patch.autoexplore", guarded && b == "Autoget" && still,
					$"autoget ammo on; autoexplore's answer: table {t}, mill {m}, old-save table {ot}, old-save mill {om} (want no Autoget on any); unguarded untakeable energy cell (control): {b} (want Autoget)");
			}
			finally { Options.AutogetAmmo = saved; }
		}

		/// <summary>An old-save mill (EnergyCell, no Capacitor) still takes a fed stone: the feed
		/// code's fallback read, driven by a snapjaw so no picker opens.</summary>
		private static void PatchOldMillFeed()
		{
			if (!NpcRig("patch.oldmill", out GameObject npc, out Cell spot)) return;
			GameObject mill = Spawn("Cleo_TerraFirma_PiezoelectricMill", spot);
			mill.RemovePart<Capacitor>();
			EnergyCell cell = new EnergyCell();
			cell.MaxCharge = 25000;
			cell.Charge = 0;
			mill.AddPart(cell);
			GameObject stone = HeavyStone(npc);
			int e0 = EnergyOf(npc);
			InventoryActionEvent.Check(mill, npc, mill, MILL_FEED_COMMAND);
			int stored = mill.GetPart<EnergyCell>()?.Charge ?? -1;
			bool eaten = !GameObject.Validate(stone);
			Report("patch.oldmill", stored > 0 && eaten && EnergyOf(npc) < e0,
				$"old-save mill fed by a snapjaw: stores {stored} in its EnergyCell, stone {(eaten ? "eaten" : "STILL CARRIED")}, energy {(EnergyOf(npc) < e0 ? "spent" : "NOT SPENT")}");
		}

		private static void PatchGranitefurPrices()
		{
			(string bp, int want)[] set =
			{
				("Cleo_TerraFirma_Cobblefur Bracer", 40),
				("Cleo_TerraFirma_Cobblefur Gloves", 50),
				("Cleo_TerraFirma_Cobblefur Cloak", 45),
			};
			List<string> got = new List<string>();
			bool pass = true;
			foreach (var (bp, want) in set)
			{
				GameObjectBlueprint model = GameObjectFactory.Factory.GetBlueprintIfExists(bp);
				double value = model?.GetPartParameter<double>("Commerce", "Value", -1) ?? -1;   // Commerce.Value is a double; the parsed value unboxes only as one
				bool ok = value == want;
				pass &= ok;
				got.Add($"{bp.Replace("Cleo_TerraFirma_", "")} {(value < 0 ? "MISSING" : value.ToString())}{(ok ? "" : " (want " + want + ")")}");
			}
			Report("patch.granitefur", pass, "Commerce values: " + string.Join(", ", got));
		}

		// ---------------------------------------------------------------- frozen gates

		private const string FIS_COMMAND = "Cleo_TerraFirma_CommandFissure";
		private const string GAZE_COMMAND = "Cleo_TerraFirma_CommandGorgonsGaze";
		private const string SHAPER_COMMAND = "Cleo_TerraFirma_CommandStoneshaper";
		private const string CALCIFY_COMMAND = "Cleo_TerraFirma_CommandCalcify";
		private const string BARRAGE_COMMAND = "Cleo_TerraFirma_CommandEarthenBarrage";
		private const string STATUE_MAKER_COMMAND = "Cleo_TerraFirma_CommandActivateStatueMaker";
		private const string MILL_FEED_COMMAND = "Cleo_TerraFirma_FeedPiezoelectricMill";

		/// <summary>The frozen gates (2026-10-14, base parity). Frozen is Physics.IsFrozen
		/// (Temperature at or below BrittleTemperature), the one thing base CheckFrozen and
		/// CanMoveExtremities read (GameObject decompile 2026-10-14), so the rig sets the
		/// temperature straight and puts it back. NPC-capable actions run on a frozen snapjaw
		/// (refuses, spends nothing), then thawed on the same setup (acts), so a pass proves the
		/// frozen gate did the refusing. Player-only actions prompt right after their gate, so
		/// they only run frozen: a leaked gate shows as a picker opening mid-run.</summary>
		private static void FrozenChecks()
		{
			Zone zone = The.Player.CurrentZone;
			HashSet<GameObject> before = new HashSet<GameObject>(zone.GetObjects());
			void Tidy()
			{
				Sweep(zone);
				List<GameObject> fresh = new List<GameObject>();
				foreach (GameObject o in zone.GetObjects())
					if (!before.Contains(o) && !o.IsPlayer())
						fresh.Add(o);
				foreach (GameObject o in fresh)
					o.Obliterate();   // what the thawed controls made: a cairn, a walker
			}
			Action[] checks = { FrozenFissure, FrozenUngated, FrozenRestack, FrozenSmear, FrozenBreak, FrozenGrinderFeed, FrozenGrinderStop, FrozenMillFeed, FrozenAnimate, FrozenPlayerActions };
			foreach (Action check in checks)
			{
				try { check(); }
				catch (Exception ex)
				{
					Report("frozen." + check.Method.Name, false, $"THREW {ex.GetType().Name}: {ex.Message}");
					Helpers.VerifyLog("AUTO", ex.ToString());
				}
				finally { Tidy(); }
			}
		}

		/// <summary>Freezes by temperature alone and returns the old reading for the thaw.</summary>
		private static int Freeze(GameObject o)
		{
			int saved = o.Physics._Temperature;
			o.Physics._Temperature = o.Physics.BrittleTemperature - 50;
			return saved;
		}

		private static int EnergyOf(GameObject o) => o.Energy?.Value ?? 0;

		private static bool Has(Cell c, string blueprint)
		{
			foreach (GameObject o in c.Objects)
				if (o.Blueprint == blueprint)
					return true;
			return false;
		}

		/// <summary>A snapjaw within 2 of you, plus an open cell beside it that itself has open
		/// neighbors (the animation table wants somewhere to put its walker).</summary>
		private static bool NpcRig(string scenario, out GameObject npc, out Cell beside)
		{
			npc = null;
			beside = null;
			foreach (Cell c in OpenAround(The.Player.CurrentCell, 2))
				foreach (Cell b in OpenAround(c, 1))
					if (b != c && OpenAround(b, 1).Count >= 3)
					{
						npc = SturdySnapjaw(c);
						beside = b;
						if (npc != null)
							return true;
					}
			Report("frozen." + scenario, false, "SETUP FAIL - need an open cell within 2 with open ground around it");
			return false;
		}

		/// <summary>One NPC-capable gate. State fingerprints whatever the action would change.</summary>
		private static void GatedByNpc(string scenario, GameObject actor, Func<string> State, Action Act)
		{
			int e0 = EnergyOf(actor);
			string s0 = State();
			int saved = Freeze(actor);
			bool frozen = actor.IsFrozen();
			try { Act(); }
			finally { actor.Physics._Temperature = saved; }
			int e1 = EnergyOf(actor);
			string s1 = State();
			bool refused = e1 == e0 && s1 == s0;
			Act();
			int e2 = EnergyOf(actor);
			string s2 = State();
			bool acted = e2 < e1 && s2 != s1;
			Report("frozen." + scenario, frozen && refused && acted,
				$"{(frozen ? "frozen" : "SETUP FAIL - would not freeze")}: {(refused ? "refused, nothing spent (" + s1 + ")" : $"ACTED (energy {e0}->{e1}; {s0} -> {s1})")}; thawed control: {(acted ? $"acts (energy -{e1 - e2}; {s2})" : $"DID NOT ACT (energy {e1}->{e2}; {s2}), so the frozen refusal proves nothing")}");
		}

		private static void FrozenRestack()
		{
			if (!NpcRig("restack", out GameObject npc, out Cell spot)) return;
			GameObject scatter = Spawn("Cleo_TerraFirma_CairnToppled", spot);
			GatedByNpc("restack", npc,
				() => $"scatter {(GameObject.Validate(scatter) ? "down" : "gone")}, cairn {(Has(spot, "Cleo_TerraFirma_Cairn") ? "standing" : "absent")}",
				() => InventoryActionEvent.Check(scatter, npc, scatter, Cleo_TerraFirma_CairnRestack.COMMAND));
		}

		private static void FrozenSmear()
		{
			if (!NpcRig("smear", out GameObject npc, out Cell spot)) return;
			GameObject line = Spawn(Cleo_TerraFirma_ChalkBox.LINE_BLUEPRINT, spot);
			GatedByNpc("smear", npc,
				() => $"chalk line {(GameObject.Validate(line) ? "drawn" : "gone")}",
				() => InventoryActionEvent.Check(line, npc, line, Cleo_TerraFirma_ChalkLine.SMEAR_COMMAND));
		}

		private static void FrozenBreak()
		{
			if (!NpcRig("glowsphere", out GameObject npc, out Cell _)) return;
			GameObject sphere = GameObject.Create("Glowsphere");
			npc.ReceiveObject(sphere);
			GatedByNpc("glowsphere", npc,
				() => $"glowsphere {(GameObject.Validate(sphere) ? "whole" : "broken")}, {CountCarried(npc, "Cleo_TerraFirma_GlowstoneShard")} shard(s)",
				() => InventoryActionEvent.Check(sphere, npc, sphere, Cleo_TerraFirma_GlowsphereBreak.COMMAND));
		}

		/// <summary>A heavy stone in the snapjaw's pack: NPC feeders take the heaviest stone in
		/// reach, so this keeps the thawed control off any boulder lying nearby.</summary>
		private static GameObject HeavyStone(GameObject npc)
		{
			GameObject stone = GameObject.Create("Small Stone");
			stone.Physics.Weight = 99;
			npc.ReceiveObject(stone);
			return stone;
		}

		private static void FrozenGrinderFeed()
		{
			if (!NpcRig("grinder.feed", out GameObject npc, out Cell _)) return;
			GameObject cell = GameObject.Create("Cleo_TerraFirma_GrinderCell");
			npc.ReceiveObject(cell);
			var grinder = cell.GetPart<Cleo_TerraFirma_GrinderCell>();
			grinder.StoneChargeLeft = 0;
			GameObject stone = HeavyStone(npc);
			GatedByNpc("grinder.feed", npc,
				() => $"grind pool {grinder.StoneChargeLeft}, stone {(GameObject.Validate(stone) ? "carried" : "eaten")}",
				() => InventoryActionEvent.Check(cell, npc, cell, Cleo_TerraFirma_GrinderCell.FEED_COMMAND));
		}

		private static void FrozenGrinderStop()
		{
			if (!NpcRig("grinder.stop", out GameObject npc, out Cell _)) return;
			GameObject cell = GameObject.Create("Cleo_TerraFirma_GrinderCell");
			npc.ReceiveObject(cell);
			var grinder = cell.GetPart<Cleo_TerraFirma_GrinderCell>();
			grinder.StoneChargeLeft = 500;
			grinder.StoneName = "harness stone";
			GatedByNpc("grinder.stop", npc,
				() => $"grind pool {grinder.StoneChargeLeft}",
				() => InventoryActionEvent.Check(cell, npc, cell, Cleo_TerraFirma_GrinderCell.STOP_COMMAND));
		}

		private static void FrozenMillFeed()
		{
			if (!NpcRig("mill.feed", out GameObject npc, out Cell spot)) return;
			GameObject mill = Spawn("Cleo_TerraFirma_PiezoelectricMill", spot);
			Capacitor cap = mill.GetPart<Capacitor>();
			if (cap == null) { Report("frozen.mill.feed", false, "SETUP FAIL - the mill has no Capacitor"); return; }
			cap.Charge = 0;
			GameObject stone = HeavyStone(npc);
			GatedByNpc("mill.feed", npc,
				() => $"mill stores {cap.Charge}, stone {(GameObject.Validate(stone) ? "carried" : "eaten")}",
				() => InventoryActionEvent.Check(mill, npc, mill, MILL_FEED_COMMAND));
		}

		private static void FrozenAnimate()
		{
			if (!NpcRig("animate", out GameObject npc, out Cell spot)) return;
			GameObject table = Spawn("Cleo_TerraFirma_AnimationTable", spot);
			Capacitor cap = table.GetPart<Capacitor>();
			if (cap == null) { Report("frozen.animate", false, "SETUP FAIL - the table has no Capacitor"); return; }
			cap.Charge = cap.MaxCharge;
			GameObject figurine = GameObject.Create("Random Copper Figurine");
			npc.ReceiveObject(figurine);
			GatedByNpc("animate", npc,
				() => $"table charge {cap.Charge}, figurine {(GameObject.Validate(figurine) ? "carried" : "animated")}",
				() => InventoryActionEvent.Check(table, npc, table, Cleo_TerraFirma_AnimationTablePart.ANIMATE_COMMAND));
		}

		/// <summary>Fissure: both halves of base Stinger's gate. The cast's thawed control is
		/// skipped on purpose (a live pulse beside you would hit you and chew the walls); the
		/// thawed advertisement is reported for context only, since the ally hold can keep it
		/// quiet for its own reasons.</summary>
		private static void FrozenFissure()
		{
			Cell c = null;
			foreach (Cell x in OpenAround(The.Player.CurrentCell, 1))
			{
				c = x;
				break;
			}
			if (c == null) { Report("frozen.fissure", false, "SETUP FAIL - need an open cell beside you"); return; }
			GameObject npc = SturdySnapjaw(c);   // adjacent: distance 1 skips the fringe coin flip
			var fissure = new Cleo_TerraFirma_Fissure();
			npc.RequirePart<Mutations>().AddMutation(fissure, 1);
			ActivatedAbilityEntry entry = npc.ActivatedAbilities?.GetAbility(fissure.ActivatedAbilityID);
			bool ready = entry != null && entry.Cooldown <= 0 && npc.IsActivatedAbilityAIUsable(fissure.ActivatedAbilityID);
			bool Advertised()
			{
				foreach (AICommandList a in AIGetOffensiveAbilityListEvent.GetFor(npc, The.Player, 1))
					if (a.Command == FIS_COMMAND)
						return true;
				return false;
			}
			int e0 = EnergyOf(npc);
			int saved = Freeze(npc);
			bool frozen = npc.IsFrozen();
			bool adFrozen, cooled;
			try
			{
				adFrozen = Advertised();
				CommandEvent.Send(npc, FIS_COMMAND);
				cooled = entry != null && entry.Cooldown > 0;
			}
			finally { npc.Physics._Temperature = saved; }
			int e1 = EnergyOf(npc);
			bool adThawed = Advertised();
			Report("frozen.fissure", ready && frozen && !adFrozen && e1 == e0 && !cooled,
				$"{(ready ? "ability ready" : "SETUP FAIL - ability not ready")}, {(frozen ? "frozen" : "WOULD NOT FREEZE")}: advertisement {(adFrozen ? "OFFERED" : "withheld")}; cast {(e1 == e0 && !cooled ? "refused, no energy or cooldown" : $"SPENT (energy {e0}->{e1}, cooldown {entry?.Cooldown})")}; thawed it {(adThawed ? "advertises again" : "still holds (ally/importance hold, see the FIS log line)")}; thawed cast not run (the pulse would hit you)");
		}

		/// <summary>Base parity for the ungated mutations: a frozen caster still answers the way
		/// a thawed one does (Gaze and Stoneshaper offensive, Calcify defensive, Barrage's toggle).
		/// Geomancy has no command or advertisement to gate, so it is logged, not asserted.</summary>
		private static void FrozenUngated()
		{
			List<Cell> open = OpenAround(The.Player.CurrentCell, 1);
			if (open.Count < 2) { Report("frozen.ungated", false, "SETUP FAIL - need two open cells beside you"); return; }
			GameObject npc = SturdySnapjaw(open[0]);
			Spawn("Sandstone", open[1]);   // natural stone in reach for the shaper's wall finder
			Mutations m = npc.RequirePart<Mutations>();
			var gaze = new Cleo_TerraFirma_GorgonsGaze();
			var shaper = new Cleo_TerraFirma_Stoneshaper();
			var calcify = new Cleo_TerraFirma_Calcify();
			var barrage = new Cleo_TerraFirma_EarthenBarrage();
			m.AddMutation(gaze, 1);
			m.AddMutation(shaper, 1);
			m.AddMutation(calcify, 1);
			m.AddMutation(barrage, 1);
			GameObject target = The.Player;
			int dist = npc.DistanceTo(target);
			bool Lists(List<AICommandList> list, string command)
			{
				foreach (AICommandList a in list)
					if (a.Command == command)
						return true;
				return false;
			}
			bool[] Answers() => new[]
			{
				Lists(AIGetOffensiveAbilityListEvent.GetFor(npc, target, dist), GAZE_COMMAND),
				Lists(AIGetOffensiveAbilityListEvent.GetFor(npc, target, dist), SHAPER_COMMAND),
				Lists(AIGetDefensiveAbilityListEvent.GetFor(npc, target, dist), CALCIFY_COMMAND),
			};
			bool[] warm = Answers();
			ActivatedAbilityEntry toggle = npc.ActivatedAbilities?.GetAbility(barrage.ActivatedAbilityID);
			bool toggleBefore = toggle?.ToggleState ?? false;
			int saved = Freeze(npc);
			bool frozen = npc.IsFrozen();
			bool[] cold;
			bool toggleAfter;
			try
			{
				cold = Answers();
				CommandEvent.Send(npc, BARRAGE_COMMAND);
				toggleAfter = toggle?.ToggleState ?? toggleBefore;
			}
			finally { npc.Physics._Temperature = saved; }
			string[] names = { "gaze", "stoneshaper", "calcify" };
			for (int i = 0; i < names.Length; i++)
				Report("frozen.ungated." + names[i], frozen && warm[i] && cold[i],
					!warm[i] ? "SETUP FAIL - no advertisement even thawed, so parity is unproven here"
					: $"thawed advertises; frozen {(cold[i] ? "still advertises (base parity)" : "WITHHELD (a gate crept in)")}");
			Report("frozen.ungated.barrage", frozen && toggle != null && toggleAfter != toggleBefore,
				toggle == null ? "SETUP FAIL - no Earthen Barrage toggle" : $"frozen toggle {(toggleAfter != toggleBefore ? "flips (base parity)" : "DID NOT FLIP")}");
			Helpers.VerifyLog("AUTO", "SCENARIO frozen.ungated.geomancy: INFO - passive, no command or advertisement to gate; nothing asserted");
		}

		/// <summary>The player-only verbs, all frozen at once: Etch, statue-maker Activate, lathe
		/// Carve, gauntlet Carve, chalkboard Write, chalk Draw. Each prompts right after its gate,
		/// so there is no thawed control; a picker opening during the run means a gate leaked.
		/// The kit is lent and taken back; your hands are borrowed for the gauntlets.</summary>
		private static void FrozenPlayerActions()
		{
			GameObject ply = The.Player;
			if (ply.HasPart<Telekinesis>())
			{
				Helpers.VerifyLog("AUTO", "SCENARIO frozen.player: SKIP - you have Telekinesis, which base lets through every one of these gates (AllowTelekinetic)");
				MessageQueue.AddPlayerMessage("Harness frozen.player: SKIP - Telekinesis passes these gates in base too.", "W");
				return;
			}
			List<Cell> open = OpenAround(ply.CurrentCell, 1);
			if (open.Count < 3) { Report("frozen.player", false, "SETUP FAIL - need three open cells beside you"); return; }
			BodyPart hands = ply.Body?.GetFirstPart("Hands");
			if (hands == null) { Report("frozen.player", false, "SETUP FAIL - you have no Hands slot"); return; }
			GameObject wall = Spawn("Sandstone", open[0]);
			GameObject lathe = Spawn("Lathe", open[1]);
			GameObject board = Spawn("Chalkboard", open[2]);
			var etchable = wall.GetPart<Cleo_TerraFirma_Etchable>();
			List<GameObject> kit = new List<GameObject>();
			GameObject Lend(string blueprint)
			{
				GameObject o = GameObject.Create(blueprint);
				o.SetIntProperty(SPAWN_MARK, 1);   // also keeps it from stacking into your own
				kit.Add(o);
				ply.ReceiveObject(o);
				return o;
			}
			Lend("Cleo_TerraFirma_Blunted Chisel");
			Lend("Cleo_TerraFirma_Splintered Mallet");
			GameObject loop = Lend("Cleo_TerraFirma_StatueMaker");
			GameObject nugget = Lend("Copper Nugget");
			GameObject chalk = Lend("Cleo_TerraFirma_BoxOfChalk");
			var box = chalk.GetPart<Cleo_TerraFirma_ChalkBox>();
			GameObject previous = hands.Equipped;
			if (previous != null)
				hands.ForceUnequip(Silent: true);
			GameObject gauntlets = ChargedGauntlets(10000);
			gauntlets.SetIntProperty(SPAWN_MARK, 1);
			kit.Add(gauntlets);
			ply.ReceiveObject(gauntlets);
			ply.ForceEquipObject(gauntlets, hands, Silent: true);
			int ChalkLines()
			{
				int n = 0;
				foreach (Cell c in ply.CurrentCell.GetLocalAdjacentCells())
					if (Has(c, Cleo_TerraFirma_ChalkBox.LINE_BLUEPRINT))
						n++;
				return n;
			}
			void Gate(string scenario, Func<string> State, Action Act)
			{
				int e0 = EnergyOf(ply);
				string s0 = State();
				Act();
				int e1 = EnergyOf(ply);
				string s1 = State();
				bool refused = e1 == e0 && s1 == s0;
				Report("frozen." + scenario, refused,
					refused ? $"frozen: refused, nothing spent ({s1})" : $"frozen: ACTED (energy {e0}->{e1}; {s0} -> {s1})");
			}
			int saved = Freeze(ply);
			try
			{
				if (!ply.IsFrozen()) { Report("frozen.player", false, "SETUP FAIL - you would not freeze"); return; }
				Gate("etch", () => $"wall inscription \"{etchable?.Inscription}\"",
					() => InventoryActionEvent.Check(wall, ply, wall, Cleo_TerraFirma_Etchable.COMMAND));
				Gate("statuemaker", () => $"loop {(GameObject.Validate(loop) ? "kept" : "used up")}, wall {(GameObject.Validate(wall) ? "standing" : "sculpted")}",
					() => InventoryActionEvent.Check(loop, ply, loop, STATUE_MAKER_COMMAND));
				Gate("lathe", () => $"nugget {(GameObject.Validate(nugget) ? "x" + nugget.Count : "gone")}",
					() => InventoryActionEvent.Check(lathe, ply, lathe, Cleo_TerraFirma_LatheCarving.COMMAND));
				Gate("gauntletcarve", () => $"gauntlet charge {gauntlets.QueryCharge(LiveOnly: false, 0L)}, wall {(GameObject.Validate(wall) ? "standing" : "carved")}",
					() => InventoryActionEvent.Check(wall, ply, wall, Cleo_TerraFirma_Carvable.COMMAND));
				Gate("chalkboard", () => $"chalk {box.Uses} use(s), board {(board.HasPart<Cleo_TerraFirma_ChalkWriting>() ? "written" : "blank")}",
					() => InventoryActionEvent.Check(board, ply, board, Cleo_TerraFirma_ChalkboardWritable.WRITE_COMMAND));
				Gate("chalkdraw", () => $"chalk {box.Uses} use(s), {ChalkLines()} chalk line(s) around you",
					() => chalk.FireEvent(Event.New(Cleo_TerraFirma_ChalkBox.DRAW_COMMAND, "Actor", ply)));
			}
			finally
			{
				ply.Physics._Temperature = saved;
				if (gauntlets.Equipped != null)
					gauntlets.ForceUnequip(Silent: true);
				foreach (GameObject o in kit)
					if (GameObject.Validate(o))
						o.Obliterate();
				if (previous != null && GameObject.Validate(previous))
					ply.ForceEquipObject(previous, hands, Silent: true);
			}
		}
	}
}
