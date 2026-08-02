using System;
using Cleo.TerraFirma.Scripts;
using XRL.Messages;
using XRL.Rules;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.Parts;

namespace XRL.World.Parts.Mutation
{
	/// <summary>
	/// Lithovore (physical defect, 2 MP): you eat rocks. Only rocks.
	///
	/// The two doors into the stomach, and what this defect does at each:
	///  - Item eating (Food part path): blocked wholesale via BeforeConsumeEvent -- verified the
	///    event dispatches to the Actor first and a false return cancels before anything lands.
	///    Rocks carry no Food part; this part adds its own "Eat" action to anything tagged
	///    Cleo_TerraFirma_IsAnyStone (BaseStone descendants AND gemstones, both tagged in
	///    Merges.xml) and replays Food's own event sequence ("OnEat" on the item, "Eating" on the
	///    eater, "AddFood" satiation, "Eaten"/"AfterEat") so the base game's stock *OnEat parts
	///    (FearOnEat, ConfuseOnEat, MutationInfectionOnEat...) fire natively on rock blueprints.
	///  - Cooked meals (Campfire path): hardcoded engine-side; handled by the Harmony patches in
	///    _HarmonyPatches.cs (plants and meats banned, stones/liquids/exotics legal -- ruling
	///    2026-07-22).
	///
	/// Eaten-effect channels read from blueprint tags (all magnitudes DRAFT, Claude numbers):
	///   Cleo_TerraFirma_DangerousToEat        warn prompt (mirrors the dangerous-drink confirm,
	///                                         same Options.ConfirmDangerousLiquid gate)
	///   Cleo_TerraFirma_EatenHarm[Type]       typed internal damage dice
	///   Cleo_TerraFirma_EatenWater            AddWater amount (10000 = one drink)
	///   Cleo_TerraFirma_EatenHeal             heal dice on the eater (healstone, RULED 2026-07-27)
	///   Cleo_TerraFirma_EatenTasty            forced tasty roll (salt ruling; RollTasty itself is
	///                                         The.Player-hardcoded so we apply the effect directly)
	///   Cleo_TerraFirma_EatenTemperature      temperature delta on the eater (TemperatureOnEat is
	///                                         hardcoded +100 engine-side, useless for cold rocks)
	///   Cleo_TerraFirma_EatenDetonate         the rock's grenade part detonates at the eater's cell
	///                                         (deep four "elemental pulse" ruling + volatile rock)
	///   Cleo_TerraFirma_EatenMagnetPulse      radius: 75% the FETCH (loose metal items in radius
	///                                         yank to the eater; items only), 25% the old full
	///                                         pulse centered on the EATER (gear rip + robot drag)
	///   Cleo_TerraFirma_EatenSpawn[Count]     blueprint spawned at the eater's cell (gyrestone)
	///   Cleo_TerraFirma_EatenStrange          rolls the fissure pool for a surrogate rock that has
	///                                         a meaningful eaten entry, and eats AS it (ruling:
	///                                         only rolls that actually do something)
	///   Cleo_TerraFirma_EatenBleed            internal Bleeding (dice; save 20) -- the sharp stones
	///   Cleo_TerraFirma_EatenDrain            LifeDrain rounds, drainer null (the random-drainer
	///                                         fallback feeds someone, somewhere)
	///   Cleo_TerraFirma_EatenSpew             GasBelly: gas blueprint (or "Random") vented
	///                                         periodically for 50-250 turns
	///   Cleo_TerraFirma_EatenQuillBurst       damage die sprayed at all neighbors, eater spared
	///   Cleo_TerraFirma_EatenLovesick         Lovesick rounds; beloved = nearest visible creature
	///   Cleo_TerraFirma_EatenForceBubble      shard blueprint ringing the eater (static)
	///   Cleo_TerraFirma_EatenRandomGas        aji-conch gas roll at the eater's cell (density)
	///   Cleo_TerraFirma_EatenFugueClone       one allied fugue copy (duration)
	///   Cleo_TerraFirma_EatenArc[Voltage]     damage die: one arc leaps at the nearest visible
	///                                         creature within EATEN_ARC_RANGE, indiscriminate;
	///                                         eater unharmed; nobody in range = harmless crackle
	///   Cleo_TerraFirma_EatenThunderclap      daze die: all adjacent creatures Dazed, eater spared
	///   Cleo_TerraFirma_EatenHop              radius: the eater bounces -- an involuntary harmless
	///                                         hop to a random open cell within it (caromcobble)
	///   Cleo_TerraFirma_EatenDread            dread radiates: per-neighbor Terrified mental attack
	///                                         (dreadroot's exact save shape); WITCHWOOD_BITE% it
	///                                         turns inward and the eater flees instead
	///   Cleo_TerraFirma_EatenConfuse          WITCHWOOD_BITE% the whorl takes hold (dreadroot-shaped
	///                                         Confusion attack; the base part's buildup cut in port)
	///   Cleo_TerraFirma_EatenNap              duration dice: WITCHWOOD_BITE% the nap lands (Asleep)
	///   Cleo_TerraFirma_EatenDarkSight        duration: dark-adapted (temporary DarkVision mod,
	///                                         skulk-tonic shape); WITCHWOOD_BITE% Blind (short)
	///   Cleo_TerraFirma_EatenSureFooted       duration: anchored (saves vs forced movement and
	///                                         knockdown); WITCHWOOD_BITE% briefly Stuck
	///   Cleo_TerraFirma_EatenSlickProof       duration: cannot slip; WITCHWOOD_BITE% the oil passes
	///                                         through (slick underfoot)
	///   Cleo_TerraFirma_EatenEffect           marker: "this rock's eat does something" -- put it on
	///                                         any rock carrying an *OnEat part or eat channel tag;
	///                                         the strange-roll filter keys off it
	///
	/// Devour (rulings 2026-07-22): when famished (HungerLevel >= 2, the same gate base uses for
	/// Gross food), large masses of stone can be devoured whole -- statues (base furniture, the
	/// mod's inert container statues whose entombed gear spills at your feet, and living
	/// Stoneshaper statues, killed by the act with their possessions dropping the way death always
	/// drops them), plus rubble and boulders. Boulders inherit BaseStone and so carry IsAnyStone --
	/// they are deliberately routed OUT of the casual snack path and into this famished-only valve.
	/// </summary>
	public class Cleo_TerraFirma_Lithovore : BaseMutation
	{
		private const string EAT_COMMAND = "Cleo_TerraFirma_EatStone";
		private const string DEVOUR_COMMAND = "Cleo_TerraFirma_DevourStatue";
		private const string STONE_TAG = "Cleo_TerraFirma_IsAnyStone";
		private const string STRANGE_ROLL_TABLE = "Cleo_TerraFirma_StrangeRocksPool"; // curated pool (strange audit 2026-07-24) -- every entry has real payloads, so eaten-marker rerolls waste far fewer tries
		private const int STRANGE_ROLL_TRIES = 30;
		private const int EATEN_ARC_RANGE = 5; // RATIFIED 2026-07-24 (galvena arc reach)
		private const int WITCHWOOD_BITE = 25; // the house witchwood bite rate (RULED 2026-07-31, eaten-table comb: positive most eats, this % the bite instead)
		private const int BLIND_BITE_DURATION = 10; // vantabsidian's bite (DRAFT; user-flagged potentially revisitable)
		private const int STUCK_BITE_DURATION = 5; // tanglelith's bite (DRAFT)

		public override bool CanLevel() => false;

		// DRAFT description -- user text pass owed, house voice attempted
		public override string GetDescription()
		{
			// user-ruled final text 2026-07-27, complete as-is
			return "You eat stone exclusively.\n\n"
				+ "Some stones have additional effects when eaten.\n"
				+ "You can't cook with plant or meat ingredients.";
		}

		public override string GetLevelText(int Level) => "";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == BeforeConsumeEvent.ID
				|| ID == OwnerGetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID;
		}

		// ---- Door one: normal food is refused ----
		public override bool HandleEvent(BeforeConsumeEvent E)
		{
			if (E.Eat && E.Actor == ParentObject && E.Object != null && !IsEdibleStone(E.Object))
			{
				E.Actor.Fail("Your stomach turns at anything but stone.");
				return false;
			}
			return base.HandleEvent(E);
		}

		// ---- Actions: Eat on stones, Devour on statues (twiddle menu reaches world objects) ----
		public override bool HandleEvent(OwnerGetInventoryActionsEvent E)
		{
			if (E.Actor == ParentObject && E.Object != null && E.Object != ParentObject)
			{
				// devour-class first: boulders are BaseStone descendants (IsAnyStone) but are a
				// famished-only mouthful (ruling 2026-07-22), not a snack
				if (IsDevourableMass(E.Object))
				{
					if (IsFamished())
						E.AddAction("Devour", "devour", DEVOUR_COMMAND, null, 'v', FireOnActor: true);
				}
				else if (IsEdibleStone(E.Object) && E.Object.Takeable)
				{
					E.AddAction("Eat", "eat", EAT_COMMAND, null, 'a', FireOnActor: true);
				}
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == EAT_COMMAND && E.Actor == ParentObject)
			{
				if (EatStone(E.Actor, E.Item))
					E.RequestInterfaceExit();
				return false;
			}
			if (E.Command == DEVOUR_COMMAND && E.Actor == ParentObject)
			{
				if (DevourMass(E.Actor, E.Item))
					E.RequestInterfaceExit();
				return false;
			}
			return base.HandleEvent(E);
		}

		// ---- Predicates ----
		private static bool IsEdibleStone(GameObject obj) => obj != null && obj.HasTag(STONE_TAG);

		private static bool IsDevourableMass(GameObject obj)
		{
			if (obj == null)
				return false;
			// Base furniture statues, the mod's inert container statues (RandomStatue chassis), and
			// living Stoneshaper statues (still carry their statue parts through animation).
			if (obj.HasPart("Statue") || obj.HasPart("RandomStatue")
				|| obj.HasPart("Cleo_TerraFirma_StoneshaperStatueStatsPart"))
			{
				return true;
			}
			GameObjectBlueprint bp = obj.GetBlueprint();
			if (bp == null)
				return false;
			// Blueprint descent catches what the part checks miss (audit 2026-07-23): the base
			// "Statue" blueprint family carries NO Statue part at all -- the Eater statues,
			// Oboroqoru, Keresh, and the Six Day Stilt shrine statuary are pure Render + Physics,
			// which is why shrine statues slipped devourability in play. Descent covers the whole
			// family and any future children (the gold Eater statues ride along: universal means
			// universal, and a lithovore does not ask what the statue is plated with).
			if (bp.DescendsFrom("Statue") || bp.DescendsFrom("Random Statue"))
				return true;
			// Rubble and boulders (ruling 2026-07-22): famished-only mouthfuls. Rubble covers the
			// mod's own rubble + crumbled walls by inheritance; BaseBoulder covers the whole base family.
			return bp.DescendsFrom("Rubble") || bp.DescendsFrom("BaseBoulder");
		}

		private bool IsFamished()
		{
			Stomach stomach = ParentObject?.GetPart<Stomach>();
			return stomach != null && stomach.HungerLevel >= 2;
		}

		// ---- Eating a stone: Food's own sequence, replayed ----
		private bool EatStone(GameObject actor, GameObject item)
		{
			if (item == null || !IsEdibleStone(item) || IsDevourableMass(item))
				return false;
			if (!actor.CanMoveExtremities("Eat", ShowMessage: true))
				return false;
			Stomach stomach = actor.GetPart<Stomach>();
			if (stomach == null)
			{
				actor.Fail("You are unable to consume food.");
				return false;
			}
			// dangerous-drink mirror: same prompt shape, same option gate (LiquidVolume precedent)
			if (item.HasTag("Cleo_TerraFirma_DangerousToEat") && actor.IsPlayer()
				&& Options.ConfirmDangerousLiquid
				&& Popup.WarnYesNoCancel("Are you sure you want to eat " + item.t(Single: true) + "?") != 0)
			{
				return false;
			}
			if (!BeforeConsumeEvent.Check(actor, actor, item, Eat: true))
				return false;

			// take exactly one from the stack
			GameObject single = item.Count > 1 ? item.RemoveOne() : item;

			// the joke completes (comb D4, RULED 2026-07-31): the hologram does not survive
			// scrutiny this intimate -- zero satiety, no channels, no OnEat. Player line is the
			// user's own, verbatim (RULED same day). NPCs get the silent no-op.
			if (single.HasPart("Cleo_TerraFirma_HolographicPart"))
			{
				Helpers.VerifyLog("LITH", $"eat: {actor.Blueprint} bit down on the holographic rock (zero satiety, joke completes)");
				if (actor.IsPlayer())
					Popup.Show("You bite down on the holographic rock. It flickers to nothingness in your mouth.");
				actor.PlayWorldSound("Human_Eating");
				actor.UseEnergy(1000, "Item Eat");
				if (GameObject.Validate(single))
					single.Obliterate(); // it was never there
				return true;
			}

			// strange rock: eat AS a surrogate rolled from the pool -- only rolls that do something
			GameObject source = single;
			GameObject surrogate = null;
			if (single.HasTag("Cleo_TerraFirma_EatenStrange"))
			{
				surrogate = RollEdibleSurrogate();
				if (surrogate != null)
				{
					source = surrogate;
					if (actor.IsPlayer())
						Popup.Show("Something in it tastes of " + surrogate.DisplayNameOnlyDirect + ".");
				}
			}

			// Food.cs's event sequence, so stock *OnEat parts on rock blueprints fire natively
			Event ev = Event.New("OnEat");
			ev.SetParameter("Actor", actor);
			ev.SetParameter("Eater", actor);
			ev.SetParameter("Subject", actor);
			ev.SetParameter("Food", source);
			ev.SetParameter("Object", source);
			source.FireEvent(ev);
			ev.ID = "Eating";
			actor.FireEvent(ev);

			FireEatChannels(actor, source);

			// satiation scales with size (ruling 2026-07-22): pebble-weight stones (weight <= 1 --
			// pebbles, Small Stones, gemstones) are half a snack; everything else a full Snack.
			// (Carnivorous is chargen-excluded, so the AddFood path always lands.)
			int weight = single.Physics != null ? single.Physics.Weight : 5;
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("LITH", $"eat: {actor.Blueprint} ate {single.Blueprint} (weight {weight} -> {(weight <= 1 ? "HALF snack" : "full Snack")}){(surrogate != null ? $"; strange surrogate={surrogate.Blueprint}" : "")}");
			if (weight <= 1)
				stomach.CookingCounter -= Food.SMALL_HUNGER_AMOUNT / 2;
			else
				actor.FireEvent(Event.New("AddFood", "Satiation", "Snack"));

			if (GameObject.Validate(source))
			{
				ev.ID = "Eaten";
				source.FireEvent(ev);
				ev.ID = "AfterEat";
				source.FireEvent(ev);
			}

			if (actor.IsPlayer())
				Popup.Show("You eat " + single.t(Single: true) + ".\nYou are now {{|" + stomach.FoodStatus() + "}} and {{|" + stomach.WaterStatus() + "}}.");
			actor.PlayWorldSound("Human_Eating");
			actor.UseEnergy(1000, "Item Eat");

			if (GameObject.Validate(surrogate))
				surrogate.Destroy(null, Silent: true);
			if (GameObject.Validate(single))
				single.Destroy(null, Silent: true);
			return true;
		}

		/// <summary>The mod-side eaten-effect channels (stock *OnEat parts already fired via "OnEat").</summary>
		private void FireEatChannels(GameObject actor, GameObject source)
		{
			string tag;
			// verify instrumentation: one line per channel that actually fires, so the log-watch
			// can confirm every bespoke eat without eyeballing each message
			void LogChannel(string channel, string detail = null) =>
				Helpers.VerifyLog("LITH", $"eaten channel {channel} ({source.Blueprint}){(string.IsNullOrEmpty(detail) ? "" : ": " + detail)}");

			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenWater")))
			{
				if (int.TryParse(tag, out int amount))
				{
					LogChannel("Water", $"+{amount}");
					actor.FireEvent(Event.New("AddWater", "Amount", amount));
				}
			}

			// healstone (RULED 2026-07-27, closing mechanical-review flag L1): the healing rock
			// heals eaten too, at its own payload die
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenHeal")))
			{
				LogChannel("Heal", tag);
				actor.Heal(Math.Max(1, tag.RollCached()), Message: true, FloatText: true);
			}

			if (source.HasTag("Cleo_TerraFirma_EatenTasty"))
			{
				LogChannel("Tasty");
				ApplyRandomTasty(actor);
			}

			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenTemperature")))
			{
				if (int.TryParse(tag, out int delta) && actor.Physics != null)
				{
					LogChannel("Temperature", $"delta {delta}");
					actor.Physics.Temperature += delta;
				}
			}

			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenHarm")))
			{
				string type = source.GetTag("Cleo_TerraFirma_EatenHarmType", "Physical");
				LogChannel("Harm", $"{tag} {type}");
				actor.TakeDamage(Math.Max(1, tag.RollCached()),
					"from " + source.an() + " settling poorly!", type,
					null, null, Attacker: null, Owner: null, Source: source);
			}

			// nuanced pains (ruling 2026-07-22): sharp things bleed you from within, the leech
			// drinks on the way down, gas stones settle into the long unrest
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenBleed")))
			{
				// the engine's own internal-bleeding flag, made for exactly this
				LogChannel("Bleed", $"internal {tag}");
				actor.ApplyEffect(new Bleeding(tag, 20, null, true, Internal: true));
			}

			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenDrain")))
			{
				if (int.TryParse(tag, out int drainRounds))
				{
					// RE-RULED 2026-07-24 (supersedes the LifeDrain/random-drainer version): the
					// drain goes NOWHERE -- 1d3/round pooled inside the stone, then everything
					// bursts back into the eater when the effect runs its full course. Early
					// removal returns nothing.
					LogChannel("Drain", $"siphon {drainRounds} rounds");
					actor.ApplyEffect(new XRL.World.Effects.Cleo_TerraFirma_StoneSiphon(Math.Max(1, drainRounds), "1d3"));
				}
			}

			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenSpew")))
			{
				// the burp family settles in for a while (50-250 turns ruled); "Random" = conchalk
				LogChannel("Spew", $"gas belly {tag}");
				actor.ApplyEffect(new Cleo_TerraFirma_GasBelly(Stat.Random(50, 250), tag));
			}

			// lodestone RE-RULED 2026-07-31 (comb, "fetch sounds good"): most eats FETCH loose
			// metal to the eater; the bite is the old full pulse (gear rip + robot drag intact)
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenMagnetPulse")))
			{
				if (int.TryParse(tag, out int radius) && actor.CurrentCell != null)
				{
					if (Stat.Random(1, 100) <= WITCHWOOD_BITE)
					{
						LogChannel("MagnetPulse", $"the bite -- full pulse grips the eater (radius {radius})");
						MagneticPulse.EmitMagneticPulse(actor, Math.Max(1, radius));
					}
					else
					{
						// the fetch: loose metal items only (engine metal test), walked along the
						// line toward the eater, stopping adjacent (MagneticPulse's own walk shape)
						int fetched = 0;
						foreach (Cell c2 in actor.CurrentCell.GetAdjacentCells(Math.Max(1, radius)))
						{
							foreach (GameObject o in new System.Collections.Generic.List<GameObject>(c2.Objects))
							{
								if (o.Brain != null || o.Equipped != null || o.InInventory != null)
									continue;
								if (!o.IsTakeable() || !CanBeMagneticallyManipulatedEvent.Check(o))
									continue;
								System.Collections.Generic.List<Tuple<Cell, char>> line = o.GetLineTo(actor);
								Cell dest = null;
								for (int k = 1; k < line.Count - 1 && line[k].Item1.IsEmptyOfSolidFor(o, o.IsCombatObject(NoBrainOnly: true)); k++)
									dest = line[k].Item1;
								if (dest != null && dest != o.CurrentCell)
								{
									o.DirectMoveTo(dest);
									fetched++;
								}
							}
						}
						LogChannel("MagnetFetch", $"{fetched} loose item(s) dragged in (radius {radius})");
						if (actor.IsPlayer())
							MessageQueue.AddPlayerMessage(fetched > 0
								? "Loose metal skitters toward you." // DRAFT
								: "The pull finds nothing loose to claim."); // DRAFT
					}
				}
			}

			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenSpawn")))
			{
				// first lands underfoot, the rest scatter into open neighbors (SpawnObject's spirit)
				string countDie = source.GetTag("Cleo_TerraFirma_EatenSpawnCount", "1");
				int count = Math.Max(1, countDie.RollCached());
				Cell cell = actor.CurrentCell;
				if (cell != null)
				{
					LogChannel("Spawn", $"{count}x {tag}");
					cell.AddObject(GameObject.Create(tag));
					System.Collections.Generic.List<Cell> open = cell.GetLocalAdjacentCells();
					open.RemoveAll((Cell x) => x.IsSolid());
					for (int i = 1; i < count && open.Count > 0; i++)
					{
						int idx = Stat.Random(0, open.Count - 1);
						open[idx].AddObject(GameObject.Create(tag));
						open.RemoveAt(idx);
					}
				}
			}

			// eaten quill burst (urchinite): you are the urchin -- neighbors are sprayed; the eater is
			// USUALLY spared, but one quill in ten finds the way down wrong (10% self-prick, RULED
			// 2026-07-24: "give it a small chance to prick the eater when they gobble it raw")
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenQuillBurst")) && actor.CurrentCell != null)
			{
				LogChannel("QuillBurst", tag);
				System.Collections.Generic.List<Cell> sprayed = actor.CurrentCell.GetLocalAdjacentCells();
				sprayed.Add(actor.CurrentCell);
				foreach (Cell c2 in sprayed)
					// snapshot: TakeDamage can kill mid-loop, mutating the live object list under the
					// enumerator (the geyser-urchinite MODERROR class, fixed the same way here)
					foreach (GameObject o in new System.Collections.Generic.List<GameObject>(c2.Objects))
						if (o != actor && o.HasPart("Combat") && o.PhaseMatches(actor))
							o.TakeDamage(tag.RollCached(), "from a burst of stone quills!", "Stabbing", null, null, Attacker: actor, Owner: actor, Source: source);
				if (Stat.Random(1, 100) <= 10)
				{
					Helpers.VerifyLog("LITH", "urchinite self-prick (the 10%)");
					actor.TakeDamage(tag.RollCached(), "from a quill pricking on the way down!", "Stabbing", null, null, Attacker: null, Owner: null, Source: source);
				}
			}

			// eaten lovesick (lovestone): you fall for the next thing you see -- the nearest visible
			// creature becomes the beloved (DRAFT reading, flagged); nothing in sight = your own reflection
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenLovesick")) && actor.CurrentCell != null)
			{
				if (int.TryParse(tag, out int rounds))
				{
					GameObject beloved = null;
					int best = int.MaxValue;
					foreach (GameObject o in actor.CurrentCell.ParentZone.GetObjectsWithPart("Combat"))
					{
						if (o == actor || !actor.HasLOSTo(o))
							continue;
						int d = actor.DistanceTo(o);
						if (d < best)
						{
							best = d;
							beloved = o;
						}
					}
					LogChannel("Lovesick", beloved != null ? $"beloved={beloved.Blueprint} at d={best}" : "nothing in sight - SELF-love");
					actor.ApplyEffect(new Lovesick(Math.Max(1, rounds), beloved ?? actor));
				}
			}

			// eaten force bubble (shieldstone): force closes around the eater (static shards; the
			// shard blueprint carries its own Temporary duration)
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenForceBubble")) && actor.CurrentCell != null)
			{
				int shards = 0;
				foreach (Cell bc in actor.CurrentCell.GetLocalAdjacentCells())
				{
					if (!bc.IsSolid() && bc.GetCombatTarget(actor, IgnoreFlight: true, IgnoreAttackable: false, IgnorePhase: false, 0, null, null, null, null, null, AllowInanimate: false) == null)
					{
						bc.AddObject(GameObject.Create(tag));
						shards++;
					}
				}
				LogChannel("ForceBubble", $"{shards} shard(s) placed");
			}

			// eaten random gas (ajistone): the aji conch's roll, centered on your own unfortunate cell
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenRandomGas")) && actor.CurrentCell != null)
			{
				if (int.TryParse(tag, out int density))
				{
					string gasBp = PopulationManager.GenerateOne("DynamicObjectsTable:AjiConch")?.Blueprint;
					if (!string.IsNullOrEmpty(gasBp))
					{
						LogChannel("RandomGas", $"{gasBp} density {density}");
						GameObjectBlueprint gbp = GameObjectFactory.Factory.GetBlueprint(gasBp);
						GameObject gasObj = GameObject.Create(gasBp);
						Gas gas = gasObj.GetPart<Gas>();
						if (gas != null)
						{
							gas.Creator = actor;
							gas.Density = Math.Max(1, density);
							if (gbp.GetTag("GasGenerationAddSeeping").EqualsNoCase("true") || gbp.GetPartParameter("Gas", "Seeping", Default: false))
								gas.Seeping = true;
						}
						actor.CurrentCell.AddObject(gasObj);
					}
				}
			}

			// eaten fugue clone (blurring rock): one temporal copy of the eater, serving the eater
			// (the thrown semantics collapse: you are both victim and thrower). RULED 2026-07-24:
			// a low chance (10%) of a SECOND copy -- sometimes the blur doubles.
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenFugueClone")) && actor.CurrentCell != null)
			{
				if (int.TryParse(tag, out int cloneDuration))
				{
					System.Collections.Generic.List<Cell> spots = actor.CurrentCell.GetLocalAdjacentCells();
					spots.RemoveAll((Cell x) => x.IsSolid() || x.HasObjectWithPart("Combat"));
					int copies = Stat.Random(1, 100) <= 10 ? 2 : 1;
					LogChannel("FugueClone", $"{copies} planned, {spots.Count} open cell(s), duration {cloneDuration}");
					for (int i = 0; i < copies && spots.Count > 0; i++)
					{
						int idx = Stat.Random(0, spots.Count - 1);
						Cell home = spots[idx];
						spots.RemoveAt(idx);
						GameObject clone = TemporalFugue.CreateFugueCopyOf(actor, actor, home, source,
							IsRealityDistortionBased: true, cloneDuration, HostileCopyChance: 100,
							Context: "Cleo_TerraFirma_LithovoreEat");
						if (clone == null)
							break;
						if (clone.Brain != null)
							clone.Brain.SetAlliedLeader<XRL.World.AI.AllySummon>(actor, 0, Silent: true);
						if (i == 1)
							Helpers.VerifyLog("LITH", "blurring eat: the 10% SECOND clone appeared");
					}
				}
			}

			// eaten arc (galvena): the charge exits through you and leaps at the nearest visible
			// creature in range -- indiscriminate, like the veins it came from (companions included,
			// gossamarl-web precedent: physics doesn't discriminate). Discharge call shape = the
			// mod's Discharge channel (Part_FallenRock / Part_SurgeField). Eater takes nothing.
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenArc")) && actor.CurrentCell != null)
			{
				string voltage = source.GetTag("Cleo_TerraFirma_EatenArcVoltage", "1d4");
				GameObject mark = null;
				int nearest = int.MaxValue;
				foreach (GameObject o in actor.CurrentCell.ParentZone.GetObjectsWithPart("Combat"))
				{
					if (o == actor || !o.PhaseMatches(actor) || !actor.HasLOSTo(o))
						continue;
					int d = actor.DistanceTo(o);
					if (d <= EATEN_ARC_RANGE && d < nearest)
					{
						nearest = d;
						mark = o;
					}
				}
				LogChannel("Arc", mark != null ? $"strikes {mark.Blueprint} at d={nearest} (voltage {voltage})" : "no target within range - harmless crackle");
				if (mark != null)
					mark.Discharge(null, Math.Max(1, voltage.RollCached()), 0, tag, null, actor, source, mark);
				else if (actor.IsPlayer())
					MessageQueue.AddPlayerMessage("The charge crackles over your skin and finds nowhere to go."); // DRAFT
			}

			// eaten hop (caromcobble): the bounce goes down fighting -- an involuntary harmless hop
			// to a random open cell in radius (ruling 2026-07-22; radius DRAFT)
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenHop")) && actor.CurrentCell != null)
			{
				if (int.TryParse(tag, out int hopRadius))
				{
					System.Collections.Generic.List<Cell> spots = actor.CurrentCell.GetAdjacentCells(Math.Max(1, hopRadius));
					spots.RemoveAll((Cell x) => x.IsSolid() || x.HasObjectWithPart("Combat"));
					if (spots.Count > 0)
					{
						Cell dest = spots[Stat.Random(0, spots.Count - 1)];
						LogChannel("Hop", $"to ({dest.X},{dest.Y}), radius {hopRadius}");
						actor.DirectMoveTo(dest, 0, Forced: true, IgnoreCombat: true);
						if (actor.IsPlayer())
							MessageQueue.AddPlayerMessage("You bounce."); // DRAFT
					}
					else
						LogChannel("Hop", "no open cell - hop fizzled");
				}
			}

			// eaten thunderclap (streakschorl): the stone's speed leaves you all at once -- a crack
			// of displaced air; neighbors are dazed, the eater is spared (quill-burst loop shape)
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenThunderclap")) && actor.CurrentCell != null)
			{
				int dazedCount = 0;
				System.Collections.Generic.List<Cell> clapped = actor.CurrentCell.GetLocalAdjacentCells();
				clapped.Add(actor.CurrentCell);
				foreach (Cell c2 in clapped)
					foreach (GameObject o in c2.Objects)
						if (o != actor && o.HasPart("Combat") && o.PhaseMatches(actor))
						{
							o.ApplyEffect(new Dazed(Math.Max(1, tag.RollCached())));
							dazedCount++;
						}
				LogChannel("Thunderclap", $"{dazedCount} neighbor(s) dazed ({tag}), eater spared");
				if (actor.IsPlayer())
					MessageQueue.AddPlayerMessage("The stone's speed leaves you all at once."); // DRAFT
			}

			// eaten oxidize (oxidian, RULED 2026-07-25): "what it touches, it teaches to decay" --
			// eating it is being taught; RustOnHit rides the eater's natural weapons for the duration
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenOxidize")))
			{
				actor.ApplyEffect(new Cleo_TerraFirma_OxidizedLimbs(Math.Max(1, tag.RollCached())));
				LogChannel("Oxidize", $"RustOnHit lent to natural weapons for {tag} turns");
				if (actor.IsPlayer())
					MessageQueue.AddPlayerMessage("Your teeth taste of old iron."); // DRAFT
			}

			// eaten dread (grimslate, comb C1 RULED 2026-07-31): dread radiates -- per-neighbor
			// mental attack in dreadroot's exact save shape (Terrified 1d8 vs 8, 2d4 rounds, flee
			// from the eater); the bite turns it inward, dreadroot's own self-attack verbatim
			if (source.HasTag("Cleo_TerraFirma_EatenDread") && actor.CurrentCell != null)
			{
				if (Stat.Random(1, 100) <= WITCHWOOD_BITE)
				{
					LogChannel("Dread", "the bite -- dread turns inward");
					if (PerformMentalAttack((MentalAttackEvent MAE) => Terrified.Attack(MAE, null, actor.CurrentCell), actor, actor, null, "Terrify OnEat", "1d8", 8, "2d4".RollCached()) && actor.IsPlayer())
						actor.ForfeitTurn();
				}
				else
				{
					int routed = 0;
					foreach (Cell c2 in actor.CurrentCell.GetLocalAdjacentCells())
						// snapshot: routed creatures move mid-loop (the geyser-urchinite enumerator class)
						foreach (GameObject o in new System.Collections.Generic.List<GameObject>(c2.Objects))
							if (o != actor && o.HasPart("Combat") && o.PhaseMatches(actor)
								&& PerformMentalAttack((MentalAttackEvent MAE) => Terrified.Attack(MAE, null, actor.CurrentCell), actor, o, null, "Terrify OnEat", "1d8", 8, "2d4".RollCached()))
								routed++;
					LogChannel("Dread", $"radiates outward: {routed} neighbor(s) routed");
					if (actor.IsPlayer())
						MessageQueue.AddPlayerMessage("Dread pours off of you."); // DRAFT
				}
			}

			// eaten confuse (vertigneiss, comb C3): the port of ConfuseOnEat's mental attack,
			// bite-gated; the base part's buildup mechanic deliberately cut (recorded)
			if (source.HasTag("Cleo_TerraFirma_EatenConfuse"))
			{
				if (Stat.Random(1, 100) <= WITCHWOOD_BITE)
				{
					LogChannel("Confuse", "the bite -- the whorl takes hold");
					PerformMentalAttack((MentalAttackEvent MAE) => Confusion.Confuse(MAE, Attack: false, 1, 3, Silent: true), actor, actor, null, "Confuse OnEat", "1d8", 8, "5d3".RollCached());
				}
				else
					LogChannel("Confuse", "swallowed clean (the 75%)");
			}

			// eaten nap (lullabaster, comb C4): the KO chance-gated; the belly is the personality
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenNap")))
			{
				if (Stat.Random(1, 100) <= WITCHWOOD_BITE)
				{
					LogChannel("Nap", $"the bite -- the nap lands ({tag})");
					actor.ApplyEffect(new Asleep { Duration = Math.Max(1, tag.RollCached()) });
				}
				else
					LogChannel("Nap", "swallowed clean (the 75%)");
			}

			// eaten dark sight (vantabsidian, comb C2): your eyes drink the dark -- or it drinks back
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenDarkSight")))
			{
				if (int.TryParse(tag, out int sightDuration))
				{
					if (Stat.Random(1, 100) <= WITCHWOOD_BITE)
					{
						LogChannel("DarkSight", $"the bite -- the dark drinks back (Blind {BLIND_BITE_DURATION})");
						actor.ApplyEffect(new Blind { Duration = BLIND_BITE_DURATION });
						if (actor.IsPlayer())
							MessageQueue.AddPlayerMessage("The dark drinks back."); // DRAFT
					}
					else
					{
						LogChannel("DarkSight", $"dark-adapted {sightDuration}");
						actor.ApplyEffect(new Cleo_TerraFirma_DarkAdapted(sightDuration));
					}
				}
			}

			// eaten sure-footed (tanglelith, comb D2): the tar grips inward -- or too well
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenSureFooted")))
			{
				if (int.TryParse(tag, out int footDuration))
				{
					if (Stat.Random(1, 100) <= WITCHWOOD_BITE)
					{
						LogChannel("SureFooted", $"the bite -- the tar grips too well (Stuck {STUCK_BITE_DURATION})");
						actor.ApplyEffect(new Stuck(STUCK_BITE_DURATION));
						if (actor.IsPlayer())
							MessageQueue.AddPlayerMessage("The tar grips too well."); // DRAFT
					}
					else
					{
						LogChannel("SureFooted", $"anchored {footDuration}");
						actor.ApplyEffect(new Cleo_TerraFirma_SureFooted(footDuration));
					}
				}
			}

			// eaten slick-proof (oilstone, comb D1, the user's own witchwood shape): oil
			// recognizes its own -- or passes straight through
			if (!string.IsNullOrEmpty(tag = source.GetTag("Cleo_TerraFirma_EatenSlickProof")))
			{
				if (int.TryParse(tag, out int greaseDuration))
				{
					if (Stat.Random(1, 100) <= WITCHWOOD_BITE)
					{
						LogChannel("SlickProof", "the bite -- the oil passes through (slick underfoot)");
						actor.CurrentCell?.AddObject(GameObject.Create("Cleo_TerraFirma_OilPuddle"));
						if (actor.IsPlayer())
							MessageQueue.AddPlayerMessage("The oil passes straight through."); // DRAFT
					}
					else
					{
						LogChannel("SlickProof", $"sure-greased {greaseDuration}");
						actor.ApplyEffect(new Cleo_TerraFirma_SureGreased(greaseDuration));
					}
				}
			}

			// last: detonation destroys the source (deep four's internal elemental pulse; volatile rock)
			if (source.HasTag("Cleo_TerraFirma_EatenDetonate") && actor.CurrentCell != null)
			{
				IGrenade grenade = source.GetFirstPartDescendedFrom<IGrenade>();
				LogChannel("Detonate", grenade != null ? grenade.GetType().Name + " bursts in the eater" : "NO grenade part found - nothing to detonate");
				grenade?.Detonate(actor.CurrentCell, actor, actor, Indirect: true);
			}
		}

		/// <summary>Campfire.RandomTastyEffect is private and The.Player-hardcoded; this is its shape,
		/// applied to the eater (NPC-safe). Same seven small effects, same message convention.</summary>
		private static void ApplyRandomTasty(GameObject actor)
		{
			string msg = "It's tastier than usual.";
			Effect effect;
			switch (Stat.Random(0, 6))
			{
				case 0: effect = new BasicCookingEffect_Hitpoints(msg); break;
				case 1: effect = new BasicCookingEffect_MA(msg); break;
				case 2: effect = new BasicCookingEffect_MS(msg); break;
				case 3: effect = new BasicCookingEffect_Quickness(msg); break;
				case 4: effect = new BasicCookingEffect_RandomStat(msg); break;
				case 5: effect = new BasicCookingEffect_Regeneration(msg); break;
				default: effect = new BasicCookingEffect_ToHit(msg); break;
			}
			actor.ApplyEffect(effect);
		}

		/// <summary>Rolls the fissure pool for a rock whose eaten entry actually does something
		/// (ruling 2026-07-22: no fizzle rolls). Never returns another strange rock.</summary>
		private static GameObject RollEdibleSurrogate()
		{
			for (int i = 0; i < STRANGE_ROLL_TRIES; i++)
			{
				string bpName = PopulationManager.RollOneFrom(STRANGE_ROLL_TABLE)?.Blueprint;
				if (string.IsNullOrEmpty(bpName))
					continue;
				if (!GameObjectFactory.Factory.Blueprints.TryGetValue(bpName, out GameObjectBlueprint bp))
					continue;
				if (bp.HasTag("Cleo_TerraFirma_EatenStrange"))
					continue;
				if (bp.HasTag("Cleo_TerraFirma_EatenEffect") || bp.HasTag("Cleo_TerraFirma_DangerousToEat"))
					return GameObject.Create(bpName);
			}
			return null;
		}

		// ---- Devour: statues, rubble, boulders (famished-only, ruling 2026-07-22) ----
		private bool DevourMass(GameObject actor, GameObject statue)
		{
			if (statue == null || !IsDevourableMass(statue))
				return false;
			if (!IsFamished())
			{
				actor.Fail("You are not desperate enough to eat " + statue.t() + ".");
				return false;
			}
			// carried boulders may be devoured from the pack; world objects need adjacency
			bool held = statue.InInventory == actor;
			if (!held && (statue.CurrentCell == null || actor.CurrentCell == null
				|| actor.CurrentCell.DistanceTo(statue) > 1))
			{
				actor.Fail("You need to be next to " + statue.t() + " to eat " + statue.them + ".");
				return false;
			}
			if (!actor.CanMoveExtremities("Eat", ShowMessage: true))
				return false;

			Cell cell = statue.CurrentCell ?? actor.CurrentCell;
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("LITH", $"devour: {actor.Blueprint} devours {statue.Blueprint} ({(statue.Brain != null && statue.Stat("Hitpoints") > 0 ? "LIVING -> dies properly" : $"inert, spilling {statue.Inventory?.GetObjectsDirect()?.Count ?? 0} entombed item(s)")}); hunger cleared");
			if (statue.Brain != null && statue.Stat("Hitpoints") > 0)
			{
				// a living Stoneshaper statue: devouring kills it; death drops its possessions the
				// way death always does, and the Stoneshaper mutation's own handlers see a normal death
				if (actor.IsPlayer())
					// literal "it" on purpose (live-fire find 2026-07-23: the statue's pronoun provider
				// yielded possessive "its" in the objective slot) -- a statue is an object whatever
				// its muse was, so the hardcoded neuter is always correct
				Popup.Show("You set upon " + statue.t() + " and devour it, stone and all.");
				statue.Die(actor);
			}
			else
			{
				// inert statue: spill anything entombed inside (container statues), then consume
				Inventory inv = statue.Inventory;
				if (inv != null)
				{
					// copy first: AddObject removes each item from the inventory's live list
					foreach (GameObject entombed in new System.Collections.Generic.List<GameObject>(inv.GetObjectsDirect()))
						cell.AddObject(entombed);
				}
				if (actor.IsPlayer())
					Popup.Show("You devour " + statue.t() + ".");
				statue.Destroy(null, Silent: true);
			}

			actor.PlayWorldSound("sfx_destroy_stone");
			actor.GetPart<Stomach>()?.ClearHunger(); // a statue is a full meal and then some
			actor.UseEnergy(2000, "Devour Statue");  // DRAFT: slower than a snack, faster than a nap
			return true;
		}
	}
}
