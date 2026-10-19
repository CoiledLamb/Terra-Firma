using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.UI;
using XRL.World.Capabilities;
using XRL.World.Effects;

namespace XRL.World.Parts.Mutation
{
	[Serializable]
	public class Cleo_TerraFirma_GorgonsGaze : BaseMutation
	{
		#region Fields
		private const string COMMAND_ID = "Cleo_TerraFirma_CommandGorgonsGaze";
		private const int NUM_OF_SAVES = 10;
		private const int SUCCESSES_TO_BREAK = 3;
		private const int STONE_MARK_MIN_FAILS = 4;
		private const int QUICKNESS_LOSS_PER_SAVE = 10;

		private const int BASE_COOLDOWN = 80; // matches Sunder Mind, this mutation's mental-side mirror

		/// <summary>Game-state flag the dev wish tfgazebounce sets: every tick passes the bounce pre-roll.</summary>
		public const string FORCE_BOUNCE_STATE = "Cleo_TerraFirma_ForceGazeBounce";

		/// <summary>
		/// The <see cref="GameObject"/> currently being targeted by this mutation.
		/// </summary>
		public GameObject Target;

		/// <summary>
		/// How many saves that the current <see cref="Target"/> has failed since the effect began. Cumulative, not consecutive.
		/// </summary>
		public int CurFailedSaves;

		/// <summary>
		/// How many saves the current <see cref="Target"/> has made. Each one strips a stage; the third breaks the gaze.
		/// </summary>
		public int CurSuccessfulSaves;

		/// <summary>
		/// Cached reference to the <see cref="Cleo_TerraFirma_Petrifying"/> instance associated with this mutation.
		/// NonSerialized -- the engine's field writer binary-formatter-warns on custom effect types; re-acquired from the target after load.
		/// </summary>
		[NonSerialized]
		public Cleo_TerraFirma_Petrifying AssocEffect;

		/// <summary>
		/// The rising-stone-band overlay's shared state (VFX, ruled 2026-08-06). NonSerialized by design:
		/// juice entries don't survive a save, and the next tick after a load re-creates the band at the
		/// then-current height (every tick rolls a save, so a state change is at most one round away).
		/// </summary>
		[NonSerialized]
		private Cleo_TerraFirma_PetrifyBandState BandFx;
		#endregion

		#region Basic data
		public override string GetDescription()
		{
			return "Your unblinking gaze stops creatures in their tracks.";
		}

		public override string GetLevelText(int Level)
		{
			// 2026-07-26 voice sitting rulings, built 2026-07-27: save line de-flowered (user: "too
			// flowery" - stiffen/shake off -> advances/reverses); the staged-escape ladder simplified
			// to its worst case (user: "saying too much" - per-stage recovery goes undocumented);
			// "Each failure:" label recast as a sentence (labels are for values, not effects); the
			// stone-mark mega-line split in two (effect + stats); the Toughness semicolon becomes a
			// period (base's one-semicolon corpus); "sight" and every flat constant unwrap - only
			// the computed save difficulty keeps its highlight.
			return "You transfix an opponent with your unblinking gaze and slowly turn them to stone.\n" +
				"Taking any action other than passing the turn will break your gaze.\n" +
				"Each round your target makes a Toughness save. Each failure advances petrification by one stage. Each success reverses one stage.\n" +
				"Save difficulty: {{rules|" + GetSaveDifficulty(Level) + "}} + your Willpower modifier, +1 per failed save\n" +
				"Each failed save holds your target in place and applies -" + QUICKNESS_LOSS_PER_SAVE + " Quickness.\n" +
				"Three successful saves break the gaze.\n" +
				"Targets that fail " + NUM_OF_SAVES + " saves are permanently turned to stone.\n" +
				"Targets that escape after " + STONE_MARK_MIN_FAILS + " or more failures are left partially petrified for " + Cleo_TerraFirma_PartiallyPetrified.DURATION + " rounds.\n" +
				"Partial petrification: -" + Cleo_TerraFirma_PartiallyPetrified.MOVE_SPEED_PENALTY + " move speed, -" + Cleo_TerraFirma_PartiallyPetrified.QUICKNESS_PENALTY + " Quickness, +" + Cleo_TerraFirma_PartiallyPetrified.AV_BONUS + " AV\n" +
				"Range: sight\n" +
				"Cooldown: " + BASE_COOLDOWN + " rounds";
		}

		public override bool Mutate(GameObject GO, int Level = 1)
		{
			ActivatedAbilityID = AddMyActivatedAbility("Gorgon's Gaze", COMMAND_ID, "Physical Mutations");
			return base.Mutate(GO, Level);
		}

		public override bool Unmutate(GameObject GO)
		{
			RemoveMyActivatedAbility(ref ActivatedAbilityID);
			if (AssocEffect != null)
				AssocEffect.Receding = true;
			GO?.GetEffect<Cleo_TerraFirma_PetrifyingReflected>()?.UpdateData(null, true); // C09: a held backfire lets go with the mutation
			Cleo_TerraFirma_GorgonPetrifyFX.Release(BandFx, ShrinkOut: true);
			BandFx = null;
			return base.Unmutate(GO);
		}

		public override void CollectStats(Templates.StatCollector stats, int Level)
		{
			stats.Set("SavesToPetrify", NUM_OF_SAVES);
			stats.Set("BreakSuccesses", SUCCESSES_TO_BREAK);
			stats.Set("MarkFails", STONE_MARK_MIN_FAILS);
			stats.Set("Range", "sight");
			stats.Set("SaveDifficulty", GetSaveDifficulty(Level));
			stats.Set("QuicknesLoss", QUICKNESS_LOSS_PER_SAVE);
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), BASE_COOLDOWN);
		}
		#endregion

		#region Events
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) &&
				ID != SingletonEvent<BeginTakeActionEvent>.ID &&
				ID != SingletonEvent<BeforeTakeActionEvent>.ID &&
				ID != PooledEvent<CommandEvent>.ID &&
				ID != AIGetOffensiveAbilityListEvent.ID &&
				ID != SingletonEvent<EndTurnEvent>.ID)
				return ID == SingletonEvent<UseEnergyEvent>.ID;
			return true;
		}

		// ---- NPC advertisement (2026-08-06, user: legendary creatures can roll pool mutations).
		// The channel is the hard part, solved by the recorded Sunder Mind recipe: the tick stays
		// on BeginTakeActionEvent (fires once per turn), and BeforeTakeActionEvent -- which fires
		// per ACTION -- consumes a channeling NPC's actions with PassTurn(), whose energy
		// signature (Passive + "Pass") the UseEnergy break filter already ignores. Per-tick
		// LOS-break stays UNRULED and matches the player's own contract (zone-exit only). ----
		public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
		{
			if (Target == null && E.Target != null && E.Distance <= GetRange(Level) &&
				IsMyActivatedAbilityAIUsable(ActivatedAbilityID) && !E.Target.HasEffect<Cleo_TerraFirma_Petrifying>())
				E.Add(COMMAND_ID);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			// mid-channel, a non-player caster spends every action holding the gaze
			// (MemberOfPsychicBattle's exact idiom: PassTurn, swallow the action)
			if (Target != null && !ParentObject.IsPlayer())
			{
				ParentObject.PassTurn();
				return false;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command != COMMAND_ID)
				return base.HandleEvent(E);
			GameObject victim;
			if (E.Actor != null && !E.Actor.IsPlayer())
			{
				// AI-fired commands carry the target (recorded plumbing); the picker is player UX
				victim = E.Target;
				if (!GameObject.Validate(ref victim) || victim == ParentObject || victim.Brain == null || victim.HasEffect<Cleo_TerraFirma_Petrifying>())
					return false;
			}
			else
			{
				Cell cell = PickDestinationCell(GetRange(Level), RequireCombat: true, Label: "Choose a target to gaze at.");
				if (cell == null)
					return false;
				victim = cell.GetCombatTarget(ParentObject, true);
				if (victim == null)
					return false;
				if (victim == ParentObject)
				{
					if (victim.IsPlayer())
					{
						Popup.Show("You gaze at yourself.");
						Popup.Show("Everything seems to be in order.");
					}
					return false;
				}
				if (victim.HasEffect<Cleo_TerraFirma_Petrifying>())
				{
					if (E.Actor.IsPlayer())
						Popup.ShowFail($"{victim.T()}{victim.Is} already being petrified! Wait your turn!");
					return false;
				}
				// THE HOLOGRAPHIC-SHALE LOCKOUT (user major-bug report 2026-08-07): the picker's
				// GetCombatTarget call rides the engine default AllowInanimate=true, so a plain
				// wall -- ANY wall, holographic or not; furniture too -- was a legal victim. A
				// brainless target then NREs TickGaze's Target.Brain.Attacked on EVERY
				// BeginTakeActionEvent: the engine aborts the player's action each segment, and
				// the gaze can never break because breaking requires a SUCCESSFUL energy spend --
				// the endless pass cycle with the game live. (An ANIMATED wall is a real creature
				// with a Brain and full stats and duels fine; animation even strips
				// HologramInvulnerability.) Refuse brainless victims before the energy/cooldown
				// spend; every true creature carries a Brain, mindless ones included.
				if (victim.Brain == null)
				{
					if (E.Actor.IsPlayer())
						Popup.ShowFail($"You cannot transfix {victim.t()}."); // draft, approved 2026-09-30
					return false;
				}
			}
			UseEnergy(1000, $"Physical Mutation {Name}");
			CooldownMyActivatedAbility(ActivatedAbilityID, BASE_COOLDOWN);
			SoundManager.PreloadClipSet("Sounds/Abilities/sfx_ability_sunderMind_abort");
			SoundManager.PreloadClipSet("Sounds/Abilities/sfx_ability_sunderMind_dig");
			ParentObject.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_attack");
			victim.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_attack");
			BeginGaze(victim);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			TickGaze();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UseEnergyEvent E)
		{
			if (Target != null && (!E.Passive || (E.Type != null && !E.Type.Contains("Pass"))))
			{
				Helpers.VerifyLog("GAZE", $"caster acted (energy type: {E.Type ?? "?"}) -> gaze breaks at {CurFailedSaves}F/{CurSuccessfulSaves}S");
				CancelGaze($"You blink, releasing {Target.t()} from your gaze!");
			}
			return base.HandleEvent(E);
		}

		#endregion

		#region Helpers
		public static int GetSaveDifficulty(int Lv)
		{
			// REBALANCED 2026-07-23 (was 8 + 2×Lv; riders vetoed, formula-only change): the old
			// curve cliffed -- 26% statue vs contemporaries at Lv1 jumping to 91%+ by Lv4, with
			// boss-tier trivialized by Lv7. Flattening to 11 + Lv opens stronger (Lv1 secures
			// kills: ~70% trash / ~56% equals) and rises smoothly (equals 56/83/97/99; the real
			// difficulty curve lives in the boss column: 16/40/70/83). Sim: scratchpad gaze_curves.py.
			return 11 + Lv;
		}

		public static int GetRange(int Lv) => 80; // "sight", matching Sunder Mind — if you can see it, you can stone it

		public void BeginGaze(GameObject Victim)
		{
			Cleo_TerraFirma_GorgonPetrifyFX.Release(BandFx, ShrinkOut: false); // a stale band never carries to a new victim
			BandFx = null;
			Target = Victim;
			CurFailedSaves = 0;
			CurSuccessfulSaves = 0;
			Helpers.VerifyLog("GAZE", $"BEGIN: {ParentObject.Blueprint} Lv{Level} gazes at {Victim.Blueprint}; base DC {GetSaveDifficulty(Level)} + caster WIL mod, +1/fail");
			XDidY(ParentObject, "level", $"an unblinking gaze at {Victim.t()}", "!", ColorAsGoodFor: ParentObject, ColorAsBadFor: Victim);
			if (Victim.IsPlayer())
			{
				// no space before GetVerb: it returns " begins" with its own leading space (the
				// double-space was live-caught 2026-08-06, first sighting of the player-victim popup)
				Popup.Show($"{ParentObject.T()} {The.Player.DescribeDirectionToward(ParentObject)}{ParentObject.GetVerb("begin")} turning you to stone with an unblinking gaze!");
				AutoAct.Interrupt(ParentObject, true, true);
			}
			AssocEffect = new Cleo_TerraFirma_Petrifying(ParentObject);
			Target.ApplyEffect(AssocEffect, ParentObject);
			if (Victim.HasEffect<Asleep>())
				Victim.FireEvent("WakeUp"); // the gaze lands now; see TickGaze for why this is needed
			ParentObject.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_attack");
			Target.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_attack");
		}

		public void TickGaze()
		{
			if (Target == null || !Target.IsValid() || Target.IsInGraveyard())
			{
				CancelGaze(Silent: true);
				return;
			}
			if (ParentObject.CurrentZone != Target.CurrentZone)
			{
				CancelGaze($"{Target.T()}{Target.GetVerb("exit")} the reach of your unblinking gaze, breaking its hold.");
				return;
			}
			// stunned/confused/asleep/paralyzed caster breaks the channel -- the 2026-07-24
			// DEFERRED verify, un-deferred by NPC gorgons existing (2026-08-06 advertisement
			// pass): the NPC pass-turn loop would otherwise hold a gaze straight through a stun.
			// Sunder Mind's own channel breaks on these; now ours does too, for every caster.
			if (ParentObject.HasEffect<Stun>() || ParentObject.HasEffect<StunGasStun>() || ParentObject.HasEffect<Confused>() || ParentObject.HasEffect<Asleep>() || ParentObject.HasEffect<Paralyzed>())
			{
				Helpers.VerifyLog("GAZE", $"caster incapacitated (stun/confusion/sleep/paralysis) -> gaze breaks at {CurFailedSaves}F/{CurSuccessfulSaves}S");
				CancelGaze($"You cannot hold your unblinking gaze!"); // draft, approved 2026-09-30
				return;
			}
			// belt for the command gate: a brainless Target (gazed on a pre-fix build, or a
			// creature that lost its brain mid-channel) releases instead of NRE-ing the action
			// loop -- this line is also what UNSTICKS a save made inside the lockout
			if (Target.Brain == null)
			{
				Helpers.VerifyLog("GAZE", $"brainless target {Target.Blueprint} -> gaze releases (lockout self-heal)");
				CancelGaze(Silent: true);
				return;
			}
			AssocEffect ??= Target.GetEffect<Cleo_TerraFirma_Petrifying>(); // re-link after save/load
			// party members hold no grudge, matching base's damage route (Brain.HandleEvent(TookDamageEvent)
			// skips InSamePartyAs); the gaze deals no damage, so it would otherwise take Shield Slam's
			// unfiltered Brain.Attacked route and turn a gazed companion against its leader
			if (!Target.InSamePartyAs(ParentObject))
				Target.Brain.Attacked(ParentObject);
			// player report 2026-10-12: a gazed sleeper slept on. Base Asleep wakes only on
			// "TakeDamage" or "WakeUp" (Asleep.FireEvent, decompiled 2026-10-12) and the gaze deals
			// no damage, so each tick wakes the target the way a hit would (Bed.cs fires the same event)
			if (Target.HasEffect<Asleep>())
				Target.FireEvent("WakeUp");
			// C09: a refractor on the line can bounce this tick back at the gazer, in place of
			// the target's save
			if (TryBounce())
				return;
			// caster's Willpower (the discipline to hold the gaze unblinking) raises the save DC, same mechanism as the lithofex's Ego;
			// each accrued failure raises it further -- the stiffer they get, the harder it is to look away
			bool targetResists = Target.MakeSave("Toughness", GetSaveDifficulty(Level) + CurFailedSaves, ParentObject, "Willpower", Vs: Name, IgnoreNatural20: !Target.IsPlayer(), Source: ParentObject);
			Helpers.VerifyLog("GAZE", $"tick: {Target.Blueprint} save vs DC {GetSaveDifficulty(Level) + CurFailedSaves}+WIL -> {(targetResists ? "RESIST" : "FAIL")} (tally now {CurFailedSaves + (targetResists ? 0 : 1)}F/{CurSuccessfulSaves + (targetResists ? 1 : 0)}S)");
			if (targetResists)
			{
				CurSuccessfulSaves++;
				if (CurSuccessfulSaves >= SUCCESSES_TO_BREAK)
				{
					Helpers.VerifyLog("GAZE", $"3rd success: gaze BROKEN by {Target.Blueprint}{(CurFailedSaves >= STONE_MARK_MIN_FAILS ? " (stone-mark eligible at " + CurFailedSaves + " fails)" : "")}");
					CancelGaze($"{Target.T()}{Target.GetVerb("shake")} off your unblinking gaze entirely, breaking its hold!");
					return;
				}
				Target.PlayWorldSound("Sounds/Damage/sfx_damage_stone", 1f, Combat: true);
				if (CurSuccessfulSaves == 1)
				{
					Helpers.VerifyLog("GAZE", "1st success: pin released");
					AssocEffect?.ReleasePin();
					XDidY(Target, "wrench", "free of the stone's grip", "!", ColorAsGoodFor: Target, ColorAsBadFor: ParentObject);
				}
				else
				{
					Helpers.VerifyLog("GAZE", "2nd success: slow cleared");
					AssocEffect?.ClearSlow();
					XDidY(Target, "shake", "the stiffness from " + Target.its + " limbs", "!", ColorAsGoodFor: Target, ColorAsBadFor: ParentObject);
				}
				UpdateBandFx("success"); // the stone visibly slides back -- the breaking-out half of the ruled read
				return;
			}
			AssocEffect?.ApplyFailure();
			CurFailedSaves++;
			if (CurFailedSaves >= NUM_OF_SAVES)
			{
				Helpers.VerifyLog("GAZE", $"10th fail: {Target.Blueprint} PETRIFIED (statue + entombed possessions)");
				if (!Petrify(ParentObject, Target))
					AssocEffect = null; // Petrify removed it; keep CancelGaze from touching a detached effect
				// the statue IS the outcome -- zero the tally so CancelGaze's stone-mark branch
				// (escapees only) can't fire on the freshly petrified victim (log-caught
				// 2026-08-06: a spurious 500-round mark + "remains partially petrified" stinger
				// landed on the dying object under every statue swap)
				CurFailedSaves = 0;
				CancelGaze(Silent: true);
			}
			else
			{
				ParentObject.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig");
				Target.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig");
				// feel round 1 (user 2026-08-06): the asterisk splatter/blip replaced by small
				// rocks popping off the body -- shedding a little more of itself per fail
				Cleo_TerraFirma_GorgonPetrifyFX.RockPops(Target, 2 + CurFailedSaves / 4);
				UpdateBandFx("fail"); // the stone visibly climbs
				if (ParentObject.IsPlayer())
					DidX("continue", "to gaze", "…", ColorAsGoodFor: ParentObject, ColorAsBadFor: Target, Source: ParentObject);
			}
		}

		/// <summary>
		/// C09 gaze bounce (closed with the user 2026-10-13/15, built 2026-10-19). A 50% pre-roll
		/// halves every refractor's chance for gaze; on a pass the engine's own <c>RefractLight</c>
		/// event walks the line the way base <c>IDelayedLineMutation.Refract</c> does (gazer's cell
		/// skipped, target's cell included), so refractive gear, Light Manipulation, the refractor
		/// implant, glitter gas and a resonant whorl all answer it. The first refractor that
		/// answers bounces the tick: the target takes nothing, and the gazer saves against its own
		/// gaze on its own tally (<see cref="Cleo_TerraFirma_PetrifyingReflected"/>).
		/// </summary>
		/// <returns>True when the tick bounced and the target's save is skipped.</returns>
		private bool TryBounce()
		{
			bool forced = The.Game?.GetBooleanGameState(FORCE_BOUNCE_STATE) == true; // dev wish tfgazebounce
			if (!forced && XRL.Rules.Stat.Random(0, 1) != 0)
				return false;
			Cell from = ParentObject.CurrentCell;
			Cell to = Target.CurrentCell;
			if (from == null || to == null || from.ParentZone != to.ParentZone)
				return false;
			List<Cell> path = new List<Cell>();
			foreach (var p in Zone.Line(from.X, from.Y, to.X, to.Y))
			{
				Cell c = from.ParentZone.GetCell(p.X, p.Y);
				if (c != null && c != from && !path.Contains(c))
					path.Add(c);
			}
			if (!path.Contains(to))
				path.Add(to);
			// handlers cast Angle straight to float, so it must be set; Direction is pre-seeded
			// back at the gazer (the whorl draws its scatter track along it)
			float angle = (float)(Math.Atan2(to.X - from.X, to.Y - from.Y) * 180.0 / Math.PI);
			Event e = null;
			foreach (Cell cell in path)
			{
				if (!cell.HasObjectWithRegisteredEvent("RefractLight"))
					continue;
				if (e == null)
				{
					e = Event.New("RefractLight");
					e.SetParameter("Attacker", ParentObject);
					e.SetParameter("Angle", angle);
					e.SetParameter("Sound", "sfx_light_refract");
				}
				e.SetParameter("Cell", cell);
				e.SetParameter("Direction", (int)angle + 180);
				e.SetParameter("Verb", null);
				e.SetParameter("By", (object)null);
				if (cell.FireEvent(e))
					continue;
				GameObject by = e.GetGameObjectParameter("By");
				if (!GameObject.Validate(ref by))
					continue;
				// base's RefractLight part (the targe and lune) plays sfx_light_refract itself; the other
				// refractors (ModRefractive, Light Manipulation, gravel whorls) don't, so they keep this one
				// (duplicate dropped 2026-10-19, user)
				string sound = e.GetStringParameter("Sound");
				if (!sound.IsNullOrEmpty() && !by.HasPart<RefractLight>())
					by.PlayWorldSound(sound, 0.5f, Combat: true);
				XDidY(by, e.GetStringParameter("Verb") ?? "refract", "the unblinking gaze", "!", ColorAsGoodFor: Target, ColorAsBadFor: ParentObject);
				Helpers.VerifyLog("GAZE", $"BOUNCE: {by.Blueprint} on {Target.Blueprint} {(e.GetStringParameter("Verb") ?? "refract")}s the gaze back at {ParentObject.Blueprint}{(forced ? " (forced pre-roll)" : "")}");
				GazerSave();
				return true;
			}
			return false;
		}

		/// <summary>
		/// The gazer's save against its own bounced gaze: same Toughness save and DC formula as a
		/// target's, +1 per gazer fail. A pass does nothing; a fail pins and slows the gazer one
		/// stage, and ten fails turn it to stone.
		/// </summary>
		private void GazerSave()
		{
			// CLAUDE PROPOSAL (rule 3, 2026-10-19): a gazer already petrifying under someone
			// else's gaze still has its tick bounced, but takes no save of its own; one ladder
			// per creature, so two gazes never stack stages on one body
			if (ParentObject.HasEffect<Cleo_TerraFirma_Petrifying>())
			{
				Helpers.VerifyLog("GAZE", "bounce: gazer already petrifying under another gaze, no gazer save");
				return;
			}
			Cleo_TerraFirma_PetrifyingReflected self = ParentObject.GetEffect<Cleo_TerraFirma_PetrifyingReflected>();
			if (self != null && self.Receding)
			{
				ParentObject.RemoveEffect(self); // a fading earlier backfire; this gaze starts its own tally
				self = null;
			}
			int fails = self?.GazerFails ?? 0;
			bool resists = ParentObject.MakeSave("Toughness", GetSaveDifficulty(Level) + fails, ParentObject, "Willpower", Vs: Name, IgnoreNatural20: !ParentObject.IsPlayer(), Source: ParentObject);
			Helpers.VerifyLog("GAZE", $"gazer save vs DC {GetSaveDifficulty(Level) + fails}+WIL -> {(resists ? "RESIST" : "FAIL")} (gazer tally {fails + (resists ? 0 : 1)}F)");
			if (resists)
				return;
			if (self == null)
			{
				self = new Cleo_TerraFirma_PetrifyingReflected(ParentObject);
				if (!ParentObject.ApplyEffect(self))
				{
					Helpers.VerifyLog("GAZE", "gazer fail: petrifying refused (cannot be held in place), no stage");
					return;
				}
			}
			self.ApplyFailure();
			self.GazerFails++;
			Helpers.VerifyLog("GAZE", $"GAZER FAIL {self.GazerFails}/{NUM_OF_SAVES}: {ParentObject.Blueprint}");
			if (self.GazerFails < NUM_OF_SAVES)
			{
				Cleo_TerraFirma_GorgonPetrifyFX.RockPops(ParentObject, 2 + self.GazerFails / 4);
				return;
			}
			Helpers.VerifyLog("GAZE", $"GAZER STONED: {ParentObject.Blueprint} petrified by its own reflected gaze");
			if (!Petrify(ParentObject, ParentObject))
				ParentObject.RemoveEffect(self); // death prevented; the stone goes with it
			CancelGaze(Silent: true);
		}

		/// <summary>
		/// Drive the rising-stone-band overlay off the channel's NET state (fails minus successes) --
		/// the tug-of-war read the user ruled 2026-08-06 ("both the animation and the breaking out").
		/// The STATUE still counts cumulative fails; the divergence is deliberate and recorded in TODO.
		/// </summary>
		private void UpdateBandFx(string why)
		{
			int net = Math.Max(0, CurFailedSaves - CurSuccessfulSaves);
			float frac = Cleo_TerraFirma_GorgonPetrifyFX.FracForNet(net);
			BandFx = Cleo_TerraFirma_GorgonPetrifyFX.Show(BandFx, Target, frac);
			Helpers.VerifyLog("GAZE", $"band ({why}): net {net} -> frac {frac:0.00}{(BandFx == null ? " [no sprite: muted/unseen/text-mode]" : "")}");
		}

		public void CancelGaze(string Reason = "", bool Silent = false)
		{
			// every end path sheds the band the same way: it slides off where they stood
			// (over a fresh statue the gray-on-gray shrink is invisible -- the statue IS the
			// completed look; over an escapee it reads as the stone letting go)
			Cleo_TerraFirma_GorgonPetrifyFX.Release(BandFx, ShrinkOut: true);
			BandFx = null;
			if (AssocEffect == null && GameObject.Validate(Target))
				AssocEffect = Target.GetEffect<Cleo_TerraFirma_Petrifying>(); // re-link after save/load
			if (!Silent)
				ParentObject.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_abort");
			if (CurFailedSaves >= STONE_MARK_MIN_FAILS && GameObject.Validate(Target) && !Target.IsInGraveyard())
			{
				// they got deep enough into the stone that some of it stays: slower, stiffer, harder
				Helpers.VerifyLog("GAZE", $"cancel at {CurFailedSaves} fails: STONE-MARK applied to {Target.Blueprint} (partially petrified, 500 rds)");
				if (AssocEffect != null)
					Target.RemoveEffect(AssocEffect);
				Target.ApplyEffect(new Cleo_TerraFirma_PartiallyPetrified(), ParentObject);
				Target.PlayWorldSound("Sounds/Damage/sfx_damage_stone", 1f, Combat: true);
				XDidY(Target, "remain", "partially petrified", "!", ColorAsBadFor: Target, Source: ParentObject);
			}
			else
				AssocEffect?.UpdateData(null, true);
			// C09: a gazer its own bounced gaze got 4+ fails into keeps a partial petrification,
			// the same rule as a target; under that it just recedes
			Cleo_TerraFirma_PetrifyingReflected self = ParentObject?.GetEffect<Cleo_TerraFirma_PetrifyingReflected>();
			if (self != null && !self.Receding && GameObject.Validate(ParentObject) && !ParentObject.IsInGraveyard())
			{
				if (self.GazerFails >= STONE_MARK_MIN_FAILS)
				{
					Helpers.VerifyLog("GAZE", $"GAZER PARTIAL: gaze ended at {self.GazerFails} gazer fails, {ParentObject.Blueprint} partially petrified (500 rds)");
					ParentObject.RemoveEffect(self);
					ParentObject.ApplyEffect(new Cleo_TerraFirma_PartiallyPetrified(), ParentObject);
					ParentObject.PlayWorldSound("Sounds/Damage/sfx_damage_stone", 1f, Combat: true);
					XDidY(ParentObject, "remain", "partially petrified", "!", ColorAsBadFor: ParentObject, Source: ParentObject);
				}
				else
					self.UpdateData(null, true);
			}
			CurFailedSaves = 0;
			CurSuccessfulSaves = 0;
			if (ParentObject.IsPlayer() && !Silent && Reason != string.Empty)
				AddPlayerMessage(Reason, ConsequentialColorChar(null, ParentObject));
			if (Target != null && !Silent)
				XDidY(Target, "stop", "turning to stone", "!", ColorAsGoodFor: Target, ColorAsBadFor: ParentObject);
			Target = null;
			AssocEffect = null;
		}

		/// <returns>False when the death was prevented (e.g. NPC Precognition) and the victim lives.</returns>
		public static bool Petrify(GameObject Medusa, GameObject Victim)
		{
			// feel round 1: the completion shower -- a hard burst of rock debris under the statue swap
			Cleo_TerraFirma_GorgonPetrifyFX.RockPops(Victim, 10, Burst: true);
			Victim.PlayWorldSound("burn_blast", 1f, Combat: true); // at the statue, not the player (audit 2026-09-29)
			if (Victim.IsPlayer())
				Achievement.TURNED_STONE.Unlock();
			Victim.SetIntProperty("SuppressCorpseDrops", 1);
			Cell curCell = Victim.CurrentCell;
			GameObject statue = StoneGaze.CreateStatueOf(Victim); // no reason to reinvent the weel
			Cleo.TerraFirma.Scripts.StatueHelpers.EntombPossessions(Victim, statue); // possessions petrify with them -- the statue is lootable
			// pronoun, not poss(): poss repeated an NPC gazer's name ("the X turns the Y to stone
			// with the X's unblinking gaze"); C09 adds the reflexive form for a bounced gaze
			bool self = Medusa == Victim;
			if (self)
				XDidY(Medusa, "turn", $"{Medusa.itself} to stone with {Medusa.its} unblinking gaze", "!", ColorAsBadFor: Medusa, Source: Medusa);
			else
				XDidYToZ(Medusa, "turn", Victim, $"to stone with {Medusa.its} unblinking gaze", "!", ColorAsGoodFor: Medusa, ColorAsBadFor: Victim, Source: Medusa);
			bool died = self
				? Victim.Die(Medusa, null, "You turned yourself to stone with your unblinking gaze.",
					$"{Victim.It} @@turned {Victim.itself} to stone with {Victim.its} unblinking gaze.")
				: Victim.Die(Medusa, null, $"You were turned to stone by the unblinking gaze of {Medusa.an()}.",
					$"{Victim.It + Victim.GetVerb("were", true, true)} @@turned to stone by the unblinking gaze of {Medusa.an()}.");
			if (died)
			{
				curCell.AddObject(statue);
				return true;
			}
			else
			{
				// player report 2026-10-11: an NPC saved by Precognition (OnBeforeDie restores
				// only hitpoints and temperature) walked off still carrying ten fails of
				// petrifying. The death was undone, so the stone goes with it.
				Victim.RemoveEffect<Cleo_TerraFirma_Petrifying>();
				Cleo.TerraFirma.Scripts.StatueHelpers.ReturnPossessions(statue, Victim); // death was prevented -- give it all back
				// the flag is persistent and gates ALL drop paths on every future death; unwind it
				// the way the engine's Consumer part does (Consumer.cs:128)
				Victim.ModIntProperty("SuppressCorpseDrops", -1, RemoveIfZero: true);
				statue.Obliterate();
				return false;
			}
		}
		#endregion
	}
}
