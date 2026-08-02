using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;
using XRL.World.AI; // AllySummon (fugue clones lent to the thrower)
using XRL.World.Effects;

namespace XRL.World.Parts
{
	/// <summary>
	/// Shared behavior for rocks shaken loose by Fissure.
	/// Provides the step-on payload channel (rocks are dangerous where they lie), the thrown-hit
	/// channel for payloads with no native part (liquid splashes), and the pathfinding asymmetry:
	/// hostiles get no avoidance weight and walk right on (the Tinkering_Mine precedent -- that's
	/// what makes floor hazards weapons), while player-controlled actors and the owner's
	/// non-enemies route around. Payloads whose thrown channel IS native (GasGrenade, HEGrenade,
	/// ThermalGrenade, EMPGrenade, DischargeOnHit, BleedingOnHit) carry those parts on the
	/// blueprint; this part only adds what the engine doesn't.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_FallenRockPart : IPart
	{
		/// <summary>Chance in 100 that a creature entering this rock's cell sets off the payload. 0 = inert underfoot.</summary>
		public int StepChance;

		/// <summary>Payload type: Gas, Explosion, Discharge, Temperature, EMP, Bleed, Liquid, Effect, Water, Heal, RandomGas, SpawnObject, Teleport, Strange, RustGear, BreakGear, QuillBurst, AbilityLock, TempAlly, Latch, FallThrough, MagnetPulse, FugueClones, ForceWall, ForceBubble, or Ricochet (caromcobble: bounces after the throw resolves, hit or miss; amount = legs, detail = damage die). Empty = no payload (a rock that only breaks or blocks).</summary>
		public string Payload = "";

		/// <summary>Payload detail: gas/puddle/spawn blueprint, damage die (Discharge/Bleed/Explosion), heal die (Heal), duration die (EMP), effect name (Effect: Blind/Luminous/Mutating/Stuck/ShatteredArmor/Terrified/Prone/Phased/Lovesick/Dazed), or population table (RandomGas; empty = the aji conch's table).</summary>
		public string PayloadDetail = "";

		/// <summary>Payload magnitude: gas density, explosion force, discharge voltage, temperature delta, EMP radius, effect duration (Effect), water units (Water). Step-on procs use half.</summary>
		public int PayloadAmount;

		/// <summary>Destroy the rock when its step-on payload fires (explosions destroy it regardless).</summary>
		public bool ConsumedOnStep;

		/// <summary>Chance in 100 that the rock survives a use — a completed throw, or a triggered step payload.
		/// Rolled fresh every use, so a lucky stone can keep holding on indefinitely (user ruling 2026-07-22).
		/// Grenade-carrying rocks consume themselves on throw regardless; ConsumedOnStep overrides on step.</summary>
		public int HoldChance; // discharge law (RULED 2026-07-25): 0 -- a fired payload consumes the stone; zoners (caromcobble/crack) override to 100

		/// <summary>Optional throw-hit RIDER (vitriolite etch, RULED 2026-07-25 "easy to sign off
		/// on"): an effect applied on top of the main payload when a throw connects, at
		/// ThrowRiderChance in 100. Vocabulary starts at ShatteredArmor (the etch) and grows on
		/// demand. Throw-only by design -- the step path never rides.</summary>
		public string ThrowRider;
		public int ThrowRiderChance;
		public int ThrowRiderAmount;

		/// <summary>Turns of being carried (inventory or equipped) before the rock goes dull — crumbles
		/// into pebbles, payload and potency lost (anti-hoarding ruling 2026-07-22; product re-ruled
		/// pebbles + clock re-ruled 1000-2500 same day, verification session). 0 = never.
		/// Floor rocks never age: what lies in the world stays part of the world.</summary>
		public int DullAfter; // dull clock CUT (RULED 2026-07-25, "a stone keeps until it goes off"); 0 = never, the code path stays for a future longer fuse

		/// <summary>Upper bound of the dull clock: each rock rolls its actual expiry once, between
		/// DullAfter and this, on its first carried turn — hoards stagger instead of expiring as one
		/// message wall. 0 = fixed at DullAfter.</summary>
		public int DullAfterMax;

		/// <summary>The rolled expiry for THIS rock (0 = not yet rolled). Deliberately not in SameAs
		/// (same stacking fuzz as HeldTurns: merges share the receiving stack's roll).</summary>
		public int DullAt;

		/// <summary>Turns spent carried so far. Deliberately not in SameAs: stacks merge across ages and
		/// share the receiving stack's clock (accepted fuzz).</summary>
		public int HeldTurns;

		/// <summary>Also fire the payload, at full strength, when this rock is thrown and hits (for payloads with no native on-hit part).</summary>
		public bool PayloadOnThrowHit;

		/// <summary>Optional step-only payload override, for rocks whose step behavior differs in KIND from their thrown one
		/// (chunk of salt: Water on the creature hit, salt puddle underfoot). Used with its own numbers as given, never halved.</summary>
		public string StepPayload = "";

		/// <summary>Detail for <see cref="StepPayload"/>.</summary>
		public string StepPayloadDetail = "";

		/// <summary>Magnitude for <see cref="StepPayload"/>.</summary>
		public int StepPayloadAmount;

		/// <summary>Hypersonic rock: zero AimVariance on every throw of this rock (GetThrowProfileEvent dispatches to the
		/// thrown object, so the rock speaks for itself). Residual honesty note: the engine's fall-one-short distance
		/// variance (1d3-2) still applies to throwers without close-range accuracy (Deft Throwing / the cybernetic);
		/// with either, this rock is a true never-miss. Scatter -- the dominant miss source -- is gone for everyone.</summary>
		public bool NeverMiss;

		/// <summary>Damage die the thrower suffers on every completed throw of this rock — the deep stones
		/// bite back (#55 shared rule; on-throw sting RULED 2026-07-22). Empty = harmless to use.</summary>
		public string SelfHarm = "";

		/// <summary>Damage attribute for <see cref="SelfHarm"/> and the holding burn (Electric/Heat/Cold/Acid).</summary>
		public string SelfHarmType = "Physical";

		/// <summary>While a self-harming rock is EQUIPPED (hand or thrown slot — pack storage is safe wrapping),
		/// the holder takes 1 point of typed damage every this-many turns ("small burn while holding", user add
		/// 2026-07-22). 0 = no holding burn. Applies to anyone holding it: baboons and geysers included.</summary>
		public int HoldHarmEvery = 5;

		/// <summary>Blueprint scattered near the impact/step cell as lingering terrain (lava patches,
		/// convalessence patches, acid pools — the deep elementals' signature residue).</summary>
		public string Byproduct = "";

		/// <summary>Count die of byproducts on a completed throw (at the victim's cell; the landing cell on a miss). Empty = none.</summary>
		public string ByproductOnThrow = "";

		/// <summary>Count die of byproducts when the step payload triggers. Empty = none.</summary>
		public string ByproductOnStep = "";

		/// <summary>Turns spent equipped so far, toward the next holding burn. Deliberately not in SameAs (same stacking fuzz as HeldTurns).</summary>
		public int EquipTurns;

		/// <summary>Pathfinding weight advertised to actors entitled to avoid this rock (7 = mine-grade "route around").</summary>
		public int AvoidWeight = 7;

		/// <summary>The creature whose fissure dropped this rock; their non-enemies get pathing courtesy.</summary>
		public GameObject Owner;

		[NonSerialized]
		private bool FiredOnHit;

		/// <summary>Set on the use-path destruction sites (fragility breaks, ConsumedOnStep) so the
		/// death-spill doesn't double the puddle a payload already made this use.</summary>
		[NonSerialized]
		private bool SuppressDeathSpill;

		[NonSerialized]
		private bool ThrowFXDone; // sting + byproducts fired for the throw in flight (hit path); AfterThrown covers misses and resets

		[NonSerialized]
		private int PendingPulseRadius; // lodestone hit mid-flight: the epicenter is wherever it lands, so the pulse waits for AfterThrown
		private bool PendingRicochet; // caromcobble: the bounce starts from wherever the rock comes to rest, so it too waits for AfterThrown
		private const int RICOCHET_LEG_RANGE = 4; // DRAFT (cells per bounce leg)

		[NonSerialized]
		private GameObject AccuracyLentTo;

		public override bool SameAs(IPart p)
		{
			// config-compare so identical rocks stack; Owner deliberately excluded (courtesy-only)
			return p is Cleo_TerraFirma_FallenRockPart o
				&& o.StepChance == StepChance
				&& o.Payload == Payload
				&& o.PayloadDetail == PayloadDetail
				&& o.PayloadAmount == PayloadAmount
				&& o.ConsumedOnStep == ConsumedOnStep
				&& o.HoldChance == HoldChance
				&& o.ThrowRider == ThrowRider
				&& o.ThrowRiderChance == ThrowRiderChance
				&& o.ThrowRiderAmount == ThrowRiderAmount
				&& o.DullAfter == DullAfter
				&& o.DullAfterMax == DullAfterMax
				&& o.PayloadOnThrowHit == PayloadOnThrowHit
				&& o.StepPayload == StepPayload
				&& o.StepPayloadDetail == StepPayloadDetail
				&& o.StepPayloadAmount == StepPayloadAmount
				&& o.NeverMiss == NeverMiss
				&& o.SelfHarm == SelfHarm
				&& o.SelfHarmType == SelfHarmType
				&& o.HoldHarmEvery == HoldHarmEvery
				&& o.Byproduct == Byproduct
				&& o.ByproductOnThrow == ByproductOnThrow
				&& o.ByproductOnStep == ByproductOnStep
				&& base.SameAs(p);
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			// WeaponThrowHit fires BEFORE damage is applied (ModDisplacer's channel), so payloads land on
			// the victim even when the throw kills them (live-fire finding 2026-07-22: the post-damage
			// ThrownProjectileHit event saw only an invalid corpse). ProjectileHit kept for launcher compat.
			Registrar.Register("ProjectileHit");
			Registrar.Register("WeaponThrowHit");
			base.Register(Object, Registrar);
		}

		/// <summary>Rocks are not munitions (ruling 2026-07-22): the grenade parts' "detonate" action
		/// reads as "crush" on stones. This event fires AFTER every GetInventoryActions handler, so
		/// part order can't put the grenade's label back.</summary>
		public override bool HandleEvent(GetInventoryActionsAlwaysEvent E)
		{
			if (E.Actions != null && E.Actions.TryGetValue("Detonate", out InventoryAction detonate))
				detonate.Display = "crush";
			return base.HandleEvent(E);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) &&
				ID != ObjectEnteredCellEvent.ID &&
				ID != GetNavigationWeightEvent.ID &&
				ID != GetAdjacentNavigationWeightEvent.ID &&
				ID != AfterThrownEvent.ID &&
				ID != GetInventoryActionsAlwaysEvent.ID &&
				ID != BeforeDestroyObjectEvent.ID &&
				ID != PooledEvent<GetThrowProfileEvent>.ID &&
				ID != TakenEvent.ID)
				return ID == PooledEvent<InterruptAutowalkEvent>.ID;
			return true;
		}

		/// <summary>Spill on destruction (RULED 2026-07-24): a liquid rock that DIES releases what it
		/// held -- burned in your pack, caught in a blast, shot on the ground, the brine gets out.
		/// Deliberately NOT on obliteration (a whorl-devoured stone was drunk by the mass; decay
		/// crumbles dry -- the potency left with the dull) and NOT on use-path breaks (the payload
		/// already puddled this use; see SuppressDeathSpill).</summary>
		public override bool HandleEvent(BeforeDestroyObjectEvent E)
		{
			if (!E.Obliterate && !SuppressDeathSpill)
			{
				string puddle = Payload == "Liquid" ? PayloadDetail : (StepPayload == "Liquid" ? StepPayloadDetail : null);
				if (!puddle.IsNullOrEmpty())
				{
					Cell cell = ParentObject.GetCurrentCell();
					if (cell != null)
					{
						Helpers.VerifyLog("ROCK", $"death spill: {ParentObject.Blueprint} destroyed -> {puddle} released");
						if (IComponent<GameObject>.Visible(ParentObject))
							IComponent<GameObject>.AddPlayerMessage(ParentObject.Does("break") + " open, spilling what it held.");
						cell.AddObject(GameObject.Create(puddle));
					}
				}
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetThrowProfileEvent E)
		{
			if (NeverMiss && E.Object == ParentObject)
			{
				E.AimVariance = 0;
				// real throws carry a TargetCell; range-display queries pass null and get no side effects
				if (E.TargetCell != null && E.Actor != null)
				{
					E.Range = Math.Max(E.Range, E.Distance); // never short for want of arm
					if (!E.Actor.HasIntProperty("CloseThrowRangeAccuracyBonus") && !E.Actor.HasIntProperty("CloseThrowRangeAccuracySkillBonus"))
					{
						// lend the thrower perfect close-range accuracy for this throw, so the engine's
						// fall-one-short distance variance can't apply; returned on hit or landing
						ReturnLentAccuracy();
						E.Actor.ModIntProperty("CloseThrowRangeAccuracyBonus", 100);
						AccuracyLentTo = E.Actor;
						Helpers.VerifyLog("HYP", $"accuracy lend: +100 CloseThrowRangeAccuracyBonus to {E.Actor.Blueprint}");
					}
				}
			}
			return base.HandleEvent(E);
		}

		public override bool WantTurnTick()
		{
			return DullAfter > 0 || (HoldHarmEvery > 0 && !SelfHarm.IsNullOrEmpty());
		}

		public override void TurnTick(long TimeTick, int Amount)
		{
			// the holding burn: a deep stone in the HAND (not the pack) wears on its holder
			if (HoldHarmEvery > 0 && !SelfHarm.IsNullOrEmpty() && ParentObject.Equipped is GameObject wielder)
			{
				EquipTurns += Amount;
				if (EquipTurns >= HoldHarmEvery)
				{
					EquipTurns -= HoldHarmEvery;
					Helpers.VerifyLog("ROCK", $"holdburn: {wielder.Blueprint} takes 1 {SelfHarmType} holding {ParentObject.Blueprint}");
					wielder.TakeDamage(1, "from holding " + ParentObject.an() + "!", SelfHarmType, null, null, Attacker: null, Owner: null, Source: ParentObject);
				}
			}
			if (DullAfter <= 0)
				return;
			GameObject holder = ParentObject.InInventory ?? ParentObject.Equipped;
			if (holder == null)
				return; // only carried rocks age
			if (DullAt <= 0)
				DullAt = DullAfterMax > DullAfter ? Stat.Random(DullAfter, DullAfterMax) : DullAfter;
			HeldTurns += Amount;
			if (HeldTurns < DullAt)
				return;
			// gone dull: the potency crumbles out; pebbles remain (product re-ruled 2026-07-22)
			int count = ParentObject.Stacker?.StackCount ?? 1;
			Helpers.VerifyLog("ROCK", $"decay: {ParentObject.Blueprint} x{count} crumbled to pebbles after {HeldTurns} carried turns (rolled expiry {DullAt} of {DullAfter}-{DullAfterMax})");
			GameObject dull = GameObject.Create("Cleo_TerraFirma_Rock_Pebbles");
			if (count > 1 && dull.Stacker != null)
				dull.Stacker.StackCount = count;
			if (holder.IsPlayer())
				IComponent<GameObject>.AddPlayerMessage(ParentObject.Does("crumble") + " to pebbles.");
			holder.ReceiveObject(dull);
			// Obliterate, NOT Destroy: Stacker cancels a plain Destroy on a stack and just decrements
			// StackCount (decompiled BeforeDestroyObjectEvent) -- since the dull replacement above was
			// minted at FULL stack count, destroying one-at-a-time duplicated rocks, N(N+1)/2 per stack
			// (live-fire find 2026-07-22: x3/x2/x1 crumble staircase in the log)
			ParentObject.Obliterate(null, Silent: true);
		}

		/// <summary>Public: the streakschorl flight hijack must call this too — a hijacked flight never
		/// fires WeaponThrowHit/AfterThrown, and the spent stone takes the un-returnable lend to its
		/// grave (live-fire leak 2026-07-23: the thrower kept +100 permanently).</summary>
		public void ReturnLentAccuracy()
		{
			if (GameObject.Validate(ref AccuracyLentTo))
			{
				AccuracyLentTo.ModIntProperty("CloseThrowRangeAccuracyBonus", -100, RemoveIfZero: true);
				Helpers.VerifyLog("HYP", $"accuracy lend RETURNED by {AccuracyLentTo.Blueprint} (now has property: {AccuracyLentTo.HasIntProperty("CloseThrowRangeAccuracyBonus")})");
			}
			AccuracyLentTo = null;
		}

		public override bool HandleEvent(AfterThrownEvent E)
		{
			ReturnLentAccuracy();
			// ownership follows possession (party gate, 2026-07-25): the thrower owns what they threw
			if (E.Actor != null)
				Owner = E.Actor;
			// whorl-deflected throw (RULED 2026-07-23): restore the outcome's placement BEFORE any
			// landing-cell logic (miss payloads, deferred pulses fire from the right cell), and the
			// stone skips its own fragility below -- a deflection is the whorl's use, not the thrower's
			bool deflected = false;
			if (ParentObject.GetPart<Cleo_TerraFirma_WhorlDeflectedPart>() is Cleo_TerraFirma_WhorlDeflectedPart mark)
			{
				deflected = true;
				mark.Restore();
			}
			bool fired = FiredOnHit;
			FiredOnHit = false;
			Helpers.VerifyLog("ROCK", $"throw resolved: {ParentObject.Blueprint} at rest @({ParentObject.CurrentCell?.X},{ParentObject.CurrentCell?.Y}); hit-payload fired={fired}{(deflected ? "; WHORL-DEFLECTED (fragility skipped)" : "")}");
			// misses still sting and still shed their residue -- at the landing cell
			if (!ThrowFXDone)
			{
				StingThrower(E.Actor);
				SpawnByproducts(ParentObject.CurrentCell, ByproductOnThrow);
			}
			ThrowFXDone = false;
			// a miss still goes off where it lands (user ruling 2026-07-22)
			if (!fired && PayloadOnThrowHit)
			{
				Helpers.VerifyLog("ROCK", $"miss: {ParentObject.Blueprint} payload fires at the landing cell");
				FirePayload(null, ParentObject.CurrentCell, Reduced: false);
			}
			// the lodestone's deferred pulse: now that the stone is at rest, it is the epicenter
			if (PendingPulseRadius > 0)
			{
				if (GameObject.Validate(ParentObject) && ParentObject.CurrentCell != null)
					XRL.World.Parts.Mutation.MagneticPulse.EmitMagneticPulse(ParentObject, PendingPulseRadius);
				PendingPulseRadius = 0;
			}
			// the caromcobble's bounce: from wherever the throw ended -- hit or miss, the rock never
			// lands where you aimed (ruling 2026-07-22)
			if (PendingRicochet)
			{
				PendingRicochet = false;
				if (GameObject.Validate(ParentObject) && ParentObject.CurrentCell != null)
					DoRicochet(E.Actor);
			}
			// discharge law (RULED 2026-07-25): the throw spends the stone only if its payload went
			// off (hit, or the miss rail above) -- payloadless ammo and step-only stones land intact
			// and recoverable. Grenade-carriers consume themselves in detonation; whorl-deflected
			// throws don't roll at all (the ruling above).
			if (!deflected && GameObject.Validate(ParentObject) && ParentObject.GetFirstPartDescendedFrom<IGrenade>() == null
				&& PayloadOnThrowHit && !Payload.IsNullOrEmpty())
			{
				bool holds = HoldChance.in100();
				Helpers.VerifyLog("ROCK", $"fragility (throw): {ParentObject.Blueprint} {(holds ? $"HOLDS (chance {HoldChance})" : $"BREAKS (chance {HoldChance})")} (discharge law)");
				if (!holds)
				{
					SuppressDeathSpill = true; // the throw's payload already released this use
					ParentObject.Destroy(null, Silent: true);
				}
			}
			return base.HandleEvent(E);
		}

		/// <summary>
		/// The Ricochet payload (caromcobble, ruling 2026-07-22): after the throw resolves -- hit or
		/// miss -- the rock bounces PayloadAmount legs, each a fully random angle line-trace (the
		/// gravel whorl's carom shape) of up to RICOCHET_LEG_RANGE cells. The first creature on a
		/// leg takes a flat d20 vs DV (bounces have no marksmanship) and PayloadDetail damage
		/// (attribution: the thrower); each leg ends at the first body, hit or flinched, or at a
		/// wall, and the rock travels with it, landing intact where the last leg ends (pair with
		/// HoldChance 100 -- rubber does not break, it bounces). All numbers DRAFT.
		/// </summary>
		private void DoRicochet(GameObject thrower)
		{
			string die = PayloadDetail.IsNullOrEmpty() ? "1d2" : PayloadDetail;
			int legs = Math.Max(1, PayloadAmount);
			for (int leg = 0; leg < legs; leg++)
			{
				Cell origin = ParentObject.CurrentCell;
				Zone zone = origin?.ParentZone;
				if (zone == null)
					return;
				double rad = Stat.Random(0, 359) * Math.PI / 180.0;
				int ex = Math.Max(0, Math.Min(zone.Width - 1, origin.X + (int)Math.Round(Math.Cos(rad) * RICOCHET_LEG_RANGE)));
				int ey = Math.Max(0, Math.Min(zone.Height - 1, origin.Y + (int)Math.Round(Math.Sin(rad) * RICOCHET_LEG_RANGE)));
				Cell landing = origin;
				foreach (Point p in Zone.Line(origin.X, origin.Y, ex, ey))
				{
					Cell step = zone.GetCell(p.X, p.Y);
					if (step == null || step == origin)
						continue;
					if (step.IsSolid())
						break;
					landing = step;
					GameObject victim = null;
					foreach (GameObject o in step.Objects)
					{
						if (o != ParentObject && o.HasPart("Combat") && o.PhaseMatches(ParentObject))
						{
							victim = o;
							break;
						}
					}
					if (victim != null)
					{
						if (Stat.Random(1, 20) > Stats.GetCombatDV(victim))
						{
							Helpers.VerifyLog("ROCK", $"carom leg {leg + 1}: struck {victim.Blueprint}");
							victim.TakeDamage(Math.Max(1, die.RollCached()), "from " + ParentObject.an() + " caroming into " + victim.them + "!", "Physical", null, null, Owner: thrower, Attacker: null, Source: ParentObject);
						}
						else
						{
							Helpers.VerifyLog("ROCK", $"carom leg {leg + 1}: {victim.Blueprint} flinched");
							IComponent<GameObject>.XDidY(victim, "flinch", "out of the way of " + ParentObject.t(), "!", null, null, victim);
						}
						break; // the leg ends at the first body, hit or flinched
					}
				}
				if (landing != origin)
				{
					ParentObject.DirectMoveTo(landing, 0, Forced: true, IgnoreCombat: true);
					if (!GameObject.Validate(ParentObject) || ParentObject.CurrentCell != landing)
					{
						Helpers.VerifyLog("ROCK", $"carom leg {leg + 1}: move interrupted (chasm/destroyed) -- bounce ends");
						return; // something interrupted the move (fell down a chasm, destroyed) -- the bounce ends
					}
					Helpers.VerifyLog("ROCK", $"carom leg {leg + 1}: bounced to ({landing.X},{landing.Y})");
					PlayWorldSound("Sounds/Throw/sfx_throwing_stone_small_impact");
				}
				else
					Helpers.VerifyLog("ROCK", $"carom leg {leg + 1}: no travel (wall-boxed or zero line)");
			}
		}

		#region Pathfinding -- rocks don't discriminate, but the earth knows its own
		private bool ShouldAvoid(GameObject actor)
		{
			if (actor == null || StepChance <= 0)
				return false;
			// party gate (RULED 2026-07-25): the owner's own field is home ground -- nothing to
			// avoid, no autowalk interrupt; other people's stones stay dangerous
			if (IsSafeStepper(actor))
				return false;
			if (actor.IsPlayerControlled())
				return true;
			// mirror of the mine's owner courtesy: the owner's non-enemies see the danger, enemies don't
			return GameObject.Validate(ref Owner) && !actor.IsHostileTowards(Owner);
		}

		/// <summary>The party gate (RULED 2026-07-25, rung b of the ladder: "b makes sense"): true
		/// when the stepper is the stone's owner or shares the owner's ultimate leader. Ownership
		/// follows possession (stamped on take and on throw) plus the Fissure caster's stamp at
		/// DropRock; unowned stones bite everyone (recorded residue of retiring StoneStep).</summary>
		private bool IsSafeStepper(GameObject stepper)
		{
			GameObject owner = Owner;
			if (!GameObject.Validate(ref owner) || stepper == null)
				return false;
			if (stepper == owner)
				return true;
			GameObject stepperHead = stepper.Brain?.GetFinalLeader() ?? stepper;
			GameObject ownerHead = owner.Brain?.GetFinalLeader() ?? owner;
			return stepperHead == ownerHead || stepperHead == owner || ownerHead == stepper;
		}

		/// <summary>Ownership follows possession (party gate, 2026-07-25): whoever carries the stone
		/// owns it, and their party walks it safely once it is back on the ground.</summary>
		public override bool HandleEvent(TakenEvent E)
		{
			if (E.Actor != null)
				Owner = E.Actor;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetNavigationWeightEvent E)
		{
			if (StepChance > 0)
			{
				// our answer depends on WHO asks; the cell cache is keyed by capability flags only,
				// so a cached answer would be served to the wrong actor -- never allow caching
				E.Uncacheable = true;
				if (E.Smart && E.PhaseMatches(ParentObject) && ShouldAvoid(E.Actor))
					E.MinWeight(AvoidWeight);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetAdjacentNavigationWeightEvent E)
		{
			if (StepChance > 0)
			{
				E.Uncacheable = true;
				if (E.Smart && E.PhaseMatches(ParentObject) && ShouldAvoid(E.Actor))
					E.MinWeight(2);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InterruptAutowalkEvent E)
		{
			if (StepChance > 0 && ShouldAvoid(E.Actor))
			{
				E.IndicateObject = ParentObject;
				return false;
			}
			return base.HandleEvent(E);
		}
		#endregion

		#region Triggers
		public override bool HandleEvent(ObjectEnteredCellEvent E)
		{
			GameObject stepper = E.Object;
			// (StoneStep tag RETIRED 2026-07-25: seam-born creatures land allied, so the party gate
			// below covers them -- "no carveout just for baboons, no conversation about apes")
			if (StepChance > 0 && GameObject.Validate(ref stepper) && stepper != ParentObject &&
				stepper.IsCombatObject() && stepper.PhaseAndFlightMatches(ParentObject))
			{
				// party gate (RULED 2026-07-25): never triggers on its owner or the owner's party --
				// the caster stands safe in their own fissure field
				if (IsSafeStepper(stepper))
				{
					Helpers.VerifyLog("ROCK", $"party gate: {stepper.Blueprint} passes over {ParentObject.Blueprint} (owner's own)");
					return base.HandleEvent(E);
				}
				if (StepChance.in100())
				{
					Helpers.VerifyLog("ROCK", $"STEP trigger: {stepper.Blueprint} set off {ParentObject.Blueprint} (StepChance {StepChance})");
					Cell here = ParentObject.CurrentCell; // captured: the payload may consume the rock
					FirePayload(stepper, here, Reduced: true);
					SpawnByproducts(here, ByproductOnStep);
					// discharge law (RULED 2026-07-25): only a fired payload consumes the stone --
					// nothing discharged, nothing to lose (ammo is just rock). HoldChance survives
					// solely as the zoner override (caromcobble/crack 100).
					string effPayload = !StepPayload.IsNullOrEmpty() ? StepPayload : Payload;
					if (!ConsumedOnStep && !effPayload.IsNullOrEmpty() && GameObject.Validate(ParentObject))
					{
						bool holds = HoldChance.in100();
						Helpers.VerifyLog("ROCK", $"fragility (step): {ParentObject.Blueprint} {(holds ? "HOLDS" : "BREAKS")} (discharge law, HoldChance {HoldChance})");
						if (!holds)
						{
							SuppressDeathSpill = true; // the step payload already released this use
							ParentObject.Destroy(null, Silent: true);
						}
					}
				}
			}
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "ProjectileHit" || E.ID == "WeaponThrowHit")
			{
				ReturnLentAccuracy();
				GameObject target = E.GetGameObjectParameter("Defender");
				bool valid = GameObject.Validate(ref target);
				Helpers.VerifyLog("ROCK", $"throw hit ({E.ID}): {ParentObject.Blueprint} -> {(valid ? target.Blueprint : "no valid defender")}");
				// sting + residue must fire HERE for grenade-carrying rocks: their detonation destroys the
				// rock before AfterThrownEvent can be delivered (part is ordered above the grenade in XML)
				if (!ThrowFXDone)
				{
					ThrowFXDone = true;
					StingThrower(E.GetGameObjectParameter("Attacker"));
					SpawnByproducts(valid ? target.CurrentCell : ParentObject.CurrentCell, ByproductOnThrow);
				}
				if (PayloadOnThrowHit && valid)
				{
					FiredOnHit = true;
					FirePayload(target, target.CurrentCell, Reduced: false, E.GetGameObjectParameter("Attacker"));
				}
				// throw-hit rider (vitriolite etch): rides on top of the payload, never replaces it
				if (valid && !ThrowRider.IsNullOrEmpty() && ThrowRiderChance.in100())
				{
					if (ThrowRider == "ShatteredArmor" && GameObject.Validate(ref target))
					{
						target.ApplyEffect(new ShatteredArmor(1, Math.Max(1, ThrowRiderAmount), Owner), Owner);
						Helpers.VerifyLog("ROCK", $"etch rider: {ParentObject.Blueprint} cracked {target.Blueprint}'s armor (chance {ThrowRiderChance})");
					}
				}
			}
			return base.FireEvent(E);
		}
		#endregion

		/// <summary>The #55 shared rule: throwing a deep stone costs the thrower a little (typed by SelfHarmType).
		/// Called at most once per throw (guarded by ThrowFXDone at the call sites).</summary>
		private void StingThrower(GameObject thrower)
		{
			if (SelfHarm.IsNullOrEmpty() || !GameObject.Validate(ref thrower))
				return;
			int sting = Math.Max(1, SelfHarm.RollCached());
			Helpers.VerifyLog("ROCK", $"sting: {thrower.Blueprint} takes {sting} {SelfHarmType} throwing {ParentObject.Blueprint}");
			thrower.TakeDamage(sting, "from " + ParentObject.an() + " biting back!", SelfHarmType, null, null, Attacker: null, Owner: null, Source: ParentObject);
		}

		/// <summary>Scatters this rock's lingering terrain (lava patches, acid pools) into <paramref name="C"/>
		/// and open neighbors; <paramref name="countDie"/> rolls how many.</summary>
		private void SpawnByproducts(Cell C, string countDie)
		{
			if (Byproduct.IsNullOrEmpty() || countDie.IsNullOrEmpty() || C == null)
				return;
			int count = Math.Max(1, countDie.RollCached());
			Helpers.VerifyLog("ROCK", $"byproduct: {count}x {Byproduct} around ({C.X},{C.Y})");
			C.AddObject(GameObject.Create(Byproduct));
			List<Cell> open = C.GetLocalAdjacentCells();
			open.RemoveAll((Cell c2) => c2.IsSolid());
			for (int i = 1; i < count && open.Count > 0; i++)
			{
				int idx = Stat.Random(0, open.Count - 1);
				open[idx].AddObject(GameObject.Create(Byproduct));
				open.RemoveAt(idx);
			}
		}

		/// <summary>Sets off this rock's payload at <paramref name="C"/>, at half strength when <paramref name="Reduced"/>.
		/// <paramref name="attacker"/> is whoever set it off (the thrower on hits); falls back to <see cref="Owner"/>.</summary>
		public void FirePayload(GameObject victim, Cell C, bool Reduced, GameObject attacker = null)
		{
			C ??= ParentObject.CurrentCell;
			if (C == null)
				return;
			GameObject.Validate(ref Owner);
			GameObject source = attacker ?? Owner;
			GameObject.Validate(ref source);
			string payload = Payload;
			string detail = PayloadDetail;
			int amount = Reduced ? PayloadAmount / 2 : PayloadAmount; // may be negative (cold deltas)
			if (Reduced && !StepPayload.IsNullOrEmpty())
			{
				// step-only override: a payload different in kind from the thrown one, with its own explicit (unhalved) numbers
				payload = StepPayload;
				detail = StepPayloadDetail;
				amount = StepPayloadAmount;
			}
			bool consumed = ConsumedOnStep;
			if (!payload.IsNullOrEmpty())
				Helpers.VerifyLog("ROCK", $"payload: {ParentObject.Blueprint} fires {payload}/{(detail.IsNullOrEmpty() ? "-" : detail)} amt={amount} reduced={Reduced} victim={victim?.Blueprint ?? "none"} @({C.X},{C.Y})");
			switch (payload)
			{
				case "Liquid": // the puddle blueprint carries its own liquid volume
					if (!detail.IsNullOrEmpty())
						C.AddObject(GameObject.Create(detail));
					break;
				case "Gas": // GasGrenade recipe, single cell
				{
					if (detail.IsNullOrEmpty())
						break;
					GameObject gasObj = GameObject.Create(detail);
					Gas gas = gasObj.GetPart<Gas>();
					if (gas != null)
					{
						gas.Creator = Owner;
						gas.Density = Math.Max(1, amount);
					}
					C.AddObject(gasObj);
					break;
				}
				case "RandomGas": // warped ajistone: the aji conch's exact roll -- a random gas blueprint from a dynamic table
				{
					string table = detail.IsNullOrEmpty() ? "DynamicObjectsTable:AjiConch" : detail;
					string gasBp = PopulationManager.GenerateOne(table)?.Blueprint;
					if (gasBp.IsNullOrEmpty())
						break;
					GameObjectBlueprint bp = GameObjectFactory.Factory.GetBlueprint(gasBp);
					GameObject gasObj = GameObject.Create(gasBp);
					Gas gas = gasObj.GetPart<Gas>();
					if (gas != null)
					{
						gas.Creator = Owner;
						gas.Density = Math.Max(1, amount);
						if (bp.GetTag("GasGenerationAddSeeping").EqualsNoCase("true") || bp.GetPartParameter("Gas", "Seeping", Default: false))
							gas.Seeping = true; // conch recipe: these gases expect to seep
					}
					C.AddObject(gasObj);
					break;
				}
				case "Explosion":
					ParentObject.Explode(Math.Max(1, amount), Owner, detail.IsNullOrEmpty() ? null : detail);
					consumed = false; // Explode destroys the rock itself
					break;
				case "Ricochet": // caromcobble: marks pending; executes at AfterThrown when the rock is at rest (the lodestone deferred-pulse pattern)
					PendingRicochet = true;
					break;
				case "Discharge": // DischargeOnHit call shape
					if (GameObject.Validate(ref victim))
						victim.Discharge(null, Math.Max(1, amount), 0, detail.IsNullOrEmpty() ? "1d2" : detail, null, Owner ?? ParentObject, ParentObject, victim);
					break;
				case "Temperature": // ThermalGrenade recipe (exact call shape); sign of the amount picks hot/cold
					C.TemperatureChange(amount, Owner ?? ParentObject, Radiant: false, MinAmbient: false, MaxAmbient: false, IgnoreResistance: false, ParentObject.GetPhase());
					break;
				case "EMP": // EMPGrenade recipe; amount = radius
					XRL.World.Parts.Mutation.ElectromagneticPulse.EMP(C, Math.Max(1, amount), detail.IsNullOrEmpty() ? 4 : detail.RollCached(), IncludeBaseCell: true, ParentObject.GetPhase());
					break;
				case "Bleed":
					if (GameObject.Validate(ref victim))
						victim.ApplyEffect(new Bleeding(detail.IsNullOrEmpty() ? "1" : detail, 16, Owner, Stack: true), Owner);
					break;
				case "Effect": // named effect applied to whoever set it off; amount = duration in rounds
				{
					if (!GameObject.Validate(ref victim) || detail.IsNullOrEmpty())
						break;
					Effect fx = null;
					if (detail == "Blind")
						fx = new Blind(Math.Max(1, amount));
					else if (detail == "Luminous")
						fx = new Luminous(Math.Max(1, amount));
					else if (detail == "Mutating")
						fx = new Mutating(Math.Max(1, amount)); // engine-gated: only mutants and True Kin can be set mutating
					else if (detail == "Stuck")
						fx = new Stuck(Math.Max(1, amount), 25, "Entangle Stuck Restraint", ParentObject); // StickyOnHit's save numbers
					else if (detail == "ShatteredArmor")
						fx = new ShatteredArmor(1, Math.Max(1, amount), Owner); // the engine's "cracked": -1 AV for the duration
					else if (detail == "Terrified")
						fx = new Terrified(Math.Max(1, amount), C); // flees the place the rock struck
					else if (detail == "Prone")
						fx = new Prone();
					else if (detail == "Dazed")
						fx = new Dazed(Math.Max(1, amount)); // dazestone: Effect channel on purpose, so strange's mimic sees it
					else if (detail == "Phased")
					{
						// phasalt vs walls (RULED 2026-07-25, gate + honest log): Phased on a
						// non-combat solid manifests nothing in-engine (user tested, 8 throws) --
						// withhold it rather than log a phase that never happens. Real
						// walk-through-walls is parked in the design budget as a slim potential.
						if (!victim.IsCombatObject())
							Helpers.VerifyLog("ROCK", $"payload Phased: {victim.Blueprint} is not a combatant (wall?) -> withheld (no observable effect on walls)");
						else
							fx = new Phased(Math.Max(1, amount));
					}
					else if (detail == "Lovesick")
						fx = new Lovesick(Math.Max(1, amount), source ?? ParentObject);
					if (fx != null)
						victim.ApplyEffect(fx, Owner);
					break;
				}
				case "Tar": // tanglelith tar redesign (RULED 2026-07-25): amount = patch count -- one at
				            // the target cell, the rest scattered to open neighbors (web-scatter shape).
				            // The victim is stuck in the fresh tar, and a FLYING victim is forced down
				            // first (Flight.Fall -- the petrify effect's own grounding idiom): the throw
				            // is the anti-air shot; the patches themselves catch only walkers.
				{
					string patchBp = detail.IsNullOrEmpty() ? "Cleo_TerraFirma_TarPatch" : detail;
					int patches = Math.Max(1, amount);
					C.AddObject(GameObject.Create(patchBp));
					List<Cell> openAdj = C.GetLocalAdjacentCells();
					openAdj.RemoveAll((Cell c2) => c2.IsSolid());
					for (int i = 1; i < patches && openAdj.Count > 0; i++)
					{
						int idx = Stat.Random(0, openAdj.Count - 1);
						openAdj[idx].AddObject(GameObject.Create(patchBp));
						openAdj.RemoveAt(idx);
					}
					if (GameObject.Validate(ref victim))
					{
						if (victim.IsFlying)
						{
							XRL.World.Capabilities.Flight.Fall(victim);
							Helpers.VerifyLog("ROCK", $"tar: {victim.Blueprint} forced down out of the air");
						}
						victim.ApplyEffect(new Stuck(12, 25, "Entangle Stuck Restraint", ParentObject), Owner); // StickyOnHit's save numbers; duration DRAFT
					}
					break;
				}
				case "SpawnObject": // spiderweb stone: scatter copies of a blueprint into the cell and open neighbors; amount = count
				{
					if (detail.IsNullOrEmpty())
						break;
					// surge fields (deep galvena) get the thrower threaded in for kill attribution
					GameObject Make()
					{
						GameObject o = GameObject.Create(detail);
						if (o.GetPart<Cleo_TerraFirma_SurgePart>() is Cleo_TerraFirma_SurgePart sp)
							sp.Owner = source;
						return o;
					}
					int count = Math.Max(1, amount);
					C.AddObject(Make());
					List<Cell> open = C.GetLocalAdjacentCells();
					open.RemoveAll((Cell c2) => c2.IsSolid());
					for (int i = 1; i < count && open.Count > 0; i++)
					{
						int idx = Stat.Random(0, open.Count - 1);
						open[idx].AddObject(Make());
						open.RemoveAt(idx);
					}
					break;
				}
				case "Strange": // strange rock: it was some other rock's trouble all along; detail = rock
				                // table (default: the CURATED pool -- audit RULED 2026-07-24, "everything
				                // it does should actually happen, no null": payload/grenade rocks only,
				                // creatures/terrain/generics cut). The surrogate is PLACED at the target
				                // cell before firing -- this repairs the old dud mimics (lodestone's pulse
				                // epicenter, crack's pull-down, drainstone's latch all need a real cell) --
				                // then cleaned up, unless it latched onto someone.
				{
					string table = detail.IsNullOrEmpty() ? "Cleo_TerraFirma_StrangeRocksPool" : detail;
					string rolled = null;
					for (int tries = 0; tries < 6 && rolled == null; tries++)
					{
						rolled = PopulationManager.RollOneFrom(table)?.Blueprint;
						if (rolled == ParentObject.Blueprint)
							rolled = null; // never itself: no infinite mirrors
					}
					if (rolled == null)
						break;
					GameObject other = GameObject.Create(rolled);
					IGrenade grenade = other.GetFirstPartDescendedFrom<IGrenade>();
					Cleo_TerraFirma_FallenRockPart otherPart = other.GetPart<Cleo_TerraFirma_FallenRockPart>();
					bool mimics = grenade == null && otherPart != null && !otherPart.Payload.IsNullOrEmpty() && otherPart.Payload != "Strange";
					Helpers.VerifyLog("ROCK", $"STRANGE rolled {rolled} -> {(grenade != null ? "detonates" : mimics ? "mimics payload " + otherPart.Payload : "PAYLOADLESS (curated pool should not contain this) -> fizzle")}");
					if (grenade != null)
					{
						grenade.Detonate(C, Owner, victim, Indirect: true); // detonation consumes it
					}
					else if (mimics)
					{
						C.AddObject(other); // place-then-fire: the payload runs from a real cell
						otherPart.Owner = Owner;
						// RULED 2026-08-01 ("b works", the siphonyx-mint flag): a surrogate that
						// force-equips (drainstone latch) rides the victim as designed, but must
						// never outlive the ride -- DrainLatch's early-unlatch checks this stamp
						other.SetIntProperty("Cleo_TerraFirma_MimicSurrogate", 1);
						otherPart.FirePayload(victim, C, Reduced);
						// the surrogate's work is done -- unless it force-equipped itself (drainstone
						// latch), in which case it rides the victim to its natural end
						if (GameObject.Validate(other) && other.Equipped == null)
							other.Obliterate();
					}
					else
					{
						other.Obliterate(); // defensive: nothing in the curated pool should land here
					}
					break;
				}
				case "Teleport": // shunting rock: ModDisplacer's call shape, unpowered; amount = max distance, min = half (it means to SEND you)
					if (GameObject.Validate(ref victim))
					{
						int max = Math.Max(1, amount);
						victim.RandomTeleport(IComponent<GameObject>.Visible(victim), null, ParentObject, Owner ?? ParentObject, null, Math.Max(1, max / 2), max, !victim.IsPlayer(), null, Forced: false, IgnoreCombat: true, Voluntary: false, false);
					}
					break;
				case "Water": // chunk of salt: quench thirst via Stomach's native AddWater event; amount = water units (10000 = one drink)
					if (GameObject.Validate(ref victim))
						victim.FireEvent(Event.New("AddWater", "Amount", Math.Max(1, amount)));
					break;
				case "Heal": // healstone: mends the creature struck (throw it at yourself) or the stepper; detail = heal die, step procs half
				{
					if (!GameObject.Validate(ref victim))
						break;
					string healDie = detail.IsNullOrEmpty() ? "2d4" : detail;
					int heal = Math.Max(1, healDie.RollCached());
					if (Reduced)
						heal = Math.Max(1, heal / 2);
					Helpers.VerifyLog("ROCK", $"heal: {ParentObject.Blueprint} mends {victim.Blueprint} for {heal} (die {healDie}, reduced={Reduced})");
					victim.Heal(heal, Message: true, FloatText: true);
					break;
				}
				case "RustGear": // oxidian: rust seeks a random metal thing the victim is wearing
				{
					if (!GameObject.Validate(ref victim))
						break;
					List<GameObject> gear = victim.Body?.GetEquippedObjects();
					if (gear == null || gear.Count == 0)
						break;
					for (int t = 0; t < 4; t++)
					{
						GameObject item = gear[Stat.Random(0, gear.Count - 1)];
						if (item.HasPart("Metal") && item.ApplyEffect(new Rusted(), source))
							break;
					}
					break;
				}
				case "BreakGear": // deep vitriolite: acid finds the straps and seams. Cracks the victim's armor
				                  // (ShatteredArmor; amount = duration, halved on step) and breaks one random
				                  // piece of their equipped gear (the engine's Broken effect, Rusted's sibling).
				{
					if (!GameObject.Validate(ref victim))
						break;
					victim.ApplyEffect(new ShatteredArmor(1, Math.Max(1, amount <= 0 ? 100 : amount), Owner), Owner);
					List<GameObject> gear = victim.Body?.GetEquippedObjects();
					if (gear == null || gear.Count == 0)
						break;
					for (int t = 0; t < 4; t++)
					{
						GameObject item = gear[Stat.Random(0, gear.Count - 1)];
						if (item.ApplyEffect(new Broken(), source))
							break;
					}
					break;
				}
				case "QuillBurst": // urchinite: stone needles spray the cell and its neighbors; detail = damage die
				{
					string die = detail.IsNullOrEmpty() ? "1d4" : detail;
					List<Cell> sprayed = C.GetLocalAdjacentCells();
					sprayed.Add(C);
					foreach (Cell c2 in sprayed)
					{
						// snapshot: TakeDamage can kill (or destroy) mid-loop, mutating the cell's live
						// object list under the enumerator (MODERROR caught live 2026-07-23: a geyser's
						// urchinite miss threw InvalidOperationException here)
						foreach (GameObject o in new List<GameObject>(c2.Objects))
						{
							if (o != ParentObject && o.HasPart("Combat") && o.PhaseMatches(ParentObject))
								o.TakeDamage(die.RollCached(), "from a burst of stone quills!", "Stabbing", null, null, Attacker: source, Owner: source, Source: ParentObject);
						}
					}
					break;
				}
				case "AbilityLock": // mullstone: cooldowns slam shut; amount = rounds. Full hit = every ability; step = one at random
				{
					if (!GameObject.Validate(ref victim))
						break;
					ActivatedAbilities aa = victim.GetPart<ActivatedAbilities>();
					if (aa?.AbilityByGuid == null || aa.AbilityByGuid.Count == 0)
						break;
					int segments = Math.Max(1, amount) * 10;
					List<ActivatedAbilityEntry> entries = new List<ActivatedAbilityEntry>(aa.AbilityByGuid.Values);
					if (Reduced)
					{
						ActivatedAbilityEntry one = entries[Stat.Random(0, entries.Count - 1)];
						entries.Clear();
						entries.Add(one);
					}
					foreach (ActivatedAbilityEntry entry in entries)
						entry.Cooldown = Math.Max(entry.Cooldown, segments);
					break;
				}
				case "FallThrough": // crack: the floor gives way beneath them. Flip the dormant StairsDown live,
				                    // let the native pull handle everything (zone below, fall damage, fliers
				                    // exempt), close it again. Dormant at rest = no native avoid-weight
				                    // broadcast, preserving the mine asymmetry.
					if (GameObject.Validate(ref victim) && ParentObject.GetPart<StairsDown>() is StairsDown stairs)
					{
						stairs.PullDown = true;
						try { stairs.CheckPullDown(victim); }
						finally { stairs.PullDown = false; }
					}
					break;
				case "MagnetPulse": // lodestone: the engine's own pulse (MagneticPulse mutation, decompiled) --
				                    // drags loose metal and robots to the stone, rips non-chest worn metal off
				                    // bodies in radius. amount = radius, step = half via Reduced.
					if (ParentObject.CurrentCell != null)
						XRL.World.Parts.Mutation.MagneticPulse.EmitMagneticPulse(ParentObject, Math.Max(1, amount));
					else
						PendingPulseRadius = Math.Max(1, amount); // mid-flight: emit once the throw places the stone
					break;
				case "FugueClones": // blurring rock: temporal copies of the victim that serve the THROWER
				                    // (ruling 2026-07-22: outnumber them with themselves). Hostile-copy path =
				                    // leaderless copies already targeting the original; the thrower then claims
				                    // them. Unowned stones stop there: copies of you that want you dead.
				                    // amount = copies, detail = duration; native guards handle copies-of-copies,
				                    // normality (reality-distortion based), and XP zeroing.
				{
					if (!GameObject.Validate(ref victim))
						break;
					int copies = Math.Max(1, amount);
					int cloneDuration = detail.IsNullOrEmpty() ? 20 : detail.RollCached();
					Cell around = victim.CurrentCell ?? C;
					List<Cell> spots = around.GetLocalAdjacentCells();
					spots.RemoveAll((Cell x) => x.IsSolid() || x.HasObjectWithPart("Combat"));
					for (int i = 0; i < copies && spots.Count > 0; i++)
					{
						int idx = Stat.Random(0, spots.Count - 1);
						Cell home = spots[idx];
						spots.RemoveAt(idx);
						GameObject clone = XRL.World.Parts.Mutation.TemporalFugue.CreateFugueCopyOf(
							source ?? ParentObject, victim, home, ParentObject,
							IsRealityDistortionBased: true, cloneDuration, HostileCopyChance: 100,
							Context: "Cleo_TerraFirma_BlurringRock");
						if (clone == null)
							break; // fugue guard said no (copy-of-copy, normality, unreplicable)
						if (GameObject.Validate(ref source) && clone.Brain != null)
							clone.Brain.SetAlliedLeader<AllySummon>(source, 0, Silent: true);
					}
					break;
				}
				case "Latch": // drainstone: fastens onto the victim and drinks; amount = rounds (step = half via Reduced)
				{
					// victim identity captured pre-Validate (harness find 2026-07-24: the guard failed
					// on a throw at a live Scrapbot and the old line could not say WHO the payload
					// thought it was aimed at -- NULL = the victimless at-rest/landing path, a
					// blueprint = a real target whose ref went stale)
					string latchVictimDesc = victim == null ? "NULL" : victim.Blueprint;
					if (GameObject.Validate(ref victim) && ParentObject.GetPart<Cleo_TerraFirma_DrainLatch>() is Cleo_TerraFirma_DrainLatch latch)
					{
						Helpers.VerifyLog("DRAIN", $"latch dispatch: victim={latchVictimDesc}, reduced={Reduced}, stoneCell={(ParentObject.CurrentCell == null ? "null" : "set")} -> {(Reduced || ParentObject.CurrentCell != null ? "IMMEDIATE" : "DEFERRED")}");
						if (Reduced || ParentObject.CurrentCell != null)
							latch.Latch(victim, Math.Max(1, amount), source); // at rest (stepped, or a PLACED strange surrogate): latch now
						else
							latch.RequestLatch(victim, Math.Max(1, amount), source); // mid-flight (no cell yet): defer until the engine finishes the throw
					}
					else
						Helpers.VerifyLog("DRAIN", $"latch dispatch FAILED guard: victim={latchVictimDesc}, victimValid={GameObject.Validate(ref victim)}, partPresent={ParentObject.GetPart<Cleo_TerraFirma_DrainLatch>() != null}");
					break;
				}
				case "TempAlly": // lovestone thrown: a hostile act of sudden devotion; amount = rounds
					if (GameObject.Validate(ref victim) && GameObject.Validate(ref source) && victim.Brain != null)
						victim.ApplyEffect(new Cleo_TerraFirma_Enamored(Math.Max(1, amount), source), source);
					break;
				case "ForceWall": // aegischist thrown (cage RULED 2026-07-23): a HIT closes an impassable
				                  // ring around the victim -- the step recipe's bubble, and a true prison:
				                  // the shards' base Forcefield is Unwalkable, whose CanPass rejects
				                  // EVERYONE regardless of owner (decomp-verified; the old worry about
				                  // owner-pass leaks is moot). A MISS keeps the original terrain-denial
				                  // wall line at the landing cell. Cage blueprint = StepPayloadDetail (the
				                  // bubble shard) when set, else detail. Partial cages against walls and
				                  // bystanders accepted: the wall is part of the cage.
				{
					if (detail.IsNullOrEmpty())
						break;
					if (GameObject.Validate(ref victim) && victim.CurrentCell != null)
					{
						string cageBp = StepPayloadDetail.IsNullOrEmpty() ? detail : StepPayloadDetail;
						Helpers.VerifyLog("ROCK", $"aegischist hit: force cage closes around {victim.Blueprint}");
						PlaceForceRing(victim.CurrentCell, cageBp);
						break;
					}
					int dir = Stat.Random(0, 3);
					int ddx = dir == 0 ? 1 : dir == 1 ? 0 : 1, ddy = dir == 0 ? 0 : dir == 1 ? 1 : (dir == 2 ? 1 : -1);
					Zone z = C.ParentZone;
					for (int step = -1; step <= 1; step++)
					{
						Cell wc = z.GetCell(C.X + ddx * step, C.Y + ddy * step);
						if (wc != null && !wc.IsSolid() && wc.GetCombatTarget(ParentObject, IgnoreFlight: true, IgnoreAttackable: false, IgnorePhase: false, 0, null, null, null, null, null, AllowInanimate: false) == null)
							wc.AddObject(GameObject.Create(detail));
					}
					break;
				}
				case "ForceBubble": // shieldstone stepped on: force closes around whoever set it off; detail = blueprint
				{
					if (!detail.IsNullOrEmpty())
						PlaceForceRing(victim?.CurrentCell ?? C, detail);
					break;
				}
			}
			if (consumed)
			{
				SuppressDeathSpill = true; // consumption follows its own payload; no double spill
				ParentObject.Destroy(null, Silent: true);
			}
		}

		/// <summary>A ring of force shards in the 8 cells around <paramref name="around"/>, skipping
		/// solid and combatant-occupied cells (partial cages accepted: the wall is part of the cage).
		/// Shared by the aegischist's step bubble and its thrown-hit cage.</summary>
		private void PlaceForceRing(Cell around, string blueprint)
		{
			foreach (Cell bc in around.GetLocalAdjacentCells())
			{
				if (!bc.IsSolid() && bc.GetCombatTarget(ParentObject, IgnoreFlight: true, IgnoreAttackable: false, IgnorePhase: false, 0, null, null, null, null, null, AllowInanimate: false) == null)
					bc.AddObject(GameObject.Create(blueprint));
			}
		}
	}
}
