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
		private const string COMMAND_ID = "Cleo_TerraFirma_CommandGorgonsGaze";
		private const int NUM_OF_SAVES = 10;
		private const int SUCCESSES_TO_BREAK = 3;
		private const int STONE_MARK_MIN_FAILS = 4;
		private const int QUICKNESS_LOSS_PER_SAVE = 10;

		private const int BASE_COOLDOWN = 80;

		public const string FORCE_BOUNCE_STATE = "Cleo_TerraFirma_ForceGazeBounce";

		public const string SHARED_COUNT_TAG = "Cleo_TerraFirma_SharedStoneCount";

		public const string STONE_COUNT_PROPERTY = "Cleo_TerraFirma_StoneCount";

		public GameObject Target;

		public int CurFailedSaves;

		public int CurSuccessfulSaves;

		[NonSerialized]
		public Cleo_TerraFirma_Petrifying AssocEffect;

		[NonSerialized]
		private Cleo_TerraFirma_PetrifyBandState BandFx;

		public override string GetDescription()
		{
			return "Your unblinking gaze stops creatures in their tracks.";
		}

		public override string GetLevelText(int Level)
		{
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
			GO?.GetEffect<Cleo_TerraFirma_PetrifyingReflected>()?.UpdateData(null, true);
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

		public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
		{
			if (Target == null && E.Target != null && E.Distance <= GetRange(Level) &&
				IsMyActivatedAbilityAIUsable(ActivatedAbilityID) && !AlreadyPetrifying(E.Target))
				E.Add(COMMAND_ID);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
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
				victim = E.Target;
				if (!GameObject.Validate(ref victim) || victim == ParentObject || victim.Brain == null || AlreadyPetrifying(victim))
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
				if (AlreadyPetrifying(victim))
				{
					if (E.Actor.IsPlayer())
						Popup.ShowFail($"{victim.T()}{victim.Is} already being petrified! Wait your turn!");
					return false;
				}
				if (victim.Brain == null)
				{
					if (E.Actor.IsPlayer())
						Popup.ShowFail($"You cannot transfix {victim.t()}.");
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

		public static int GetSaveDifficulty(int Lv)
		{
			return 11 + Lv;
		}

		public static int GetRange(int Lv) => 80;

		public static bool UsesSharedCount(GameObject Caster) => Caster != null && Caster.HasPropertyOrTag(SHARED_COUNT_TAG);

		private bool AlreadyPetrifying(GameObject Victim)
		{
			Cleo_TerraFirma_Petrifying effect = Victim?.GetEffect<Cleo_TerraFirma_Petrifying>();
			return effect != null && (!UsesSharedCount(ParentObject) || effect.Active);
		}

		public static int GetStoneCount(GameObject Victim)
		{
			if (Victim == null)
				return 0;
			if (Victim.GetEffect<Cleo_TerraFirma_Petrifying>() == null)
			{
				Victim.RemoveIntProperty(STONE_COUNT_PROPERTY);
				return 0;
			}
			return Victim.GetIntProperty(STONE_COUNT_PROPERTY);
		}

		public static void SetStoneCount(GameObject Victim, int Count)
		{
			Victim?.SetIntProperty(STONE_COUNT_PROPERTY, Count, RemoveIfZero: true);
		}

		public static bool TouchPetrify(GameObject Caster, GameObject Victim, int FallbackLevel)
		{
			if (!GameObject.Validate(ref Victim) || Victim == Caster || Victim.Brain == null || Victim.IsInGraveyard())
				return false;
			int level = Caster?.GetPart<Cleo_TerraFirma_GorgonsGaze>()?.Level ?? 0;
			if (level <= 0)
				level = FallbackLevel;
			int count = GetStoneCount(Victim);
			bool resists = Victim.MakeSave("Toughness", GetSaveDifficulty(level) + count, Caster, "Willpower", Vs: "Gorgon's Gaze", IgnoreNatural20: !Victim.IsPlayer(), Source: Caster);
			Helpers.VerifyLog("GAZE", $"touch: {Victim.Blueprint} save vs DC {GetSaveDifficulty(level) + count}+WIL -> {(resists ? "RESIST" : "FAIL")} (count {count + (resists ? 0 : 1)}/{NUM_OF_SAVES})");
			if (resists)
				return false;
			Cleo_TerraFirma_Petrifying effect = Victim.GetEffect<Cleo_TerraFirma_Petrifying>();
			if (effect == null)
			{
				effect = new Cleo_TerraFirma_Petrifying(Caster);
				if (!Victim.ApplyEffect(effect, Caster))
				{
					Helpers.VerifyLog("GAZE", "touch: petrifying refused (cannot be held in place), no stage");
					return false;
				}
				effect.UpdateData(null, true);
			}
			effect.ApplyFailure();
			if (!effect.Active)
				effect.UpdateData(null, true);
			count++;
			SetStoneCount(Victim, count);
			if (count >= NUM_OF_SAVES)
			{
				Helpers.VerifyLog("GAZE", $"touch: {Victim.Blueprint} PETRIFIED at {count} stages");
				Petrify(Caster, Victim, "stony touch");
				return true;
			}
			Victim.PlayWorldSound("Sounds/Damage/sfx_damage_stone", 1f, Combat: true);
			Cleo_TerraFirma_GorgonPetrifyFX.RockPops(Victim, 2 + count / 4);
			return true;
		}

		public void BeginGaze(GameObject Victim)
		{
			Cleo_TerraFirma_GorgonPetrifyFX.Release(BandFx, ShrinkOut: false);
			BandFx = null;
			Target = Victim;
			CurFailedSaves = 0;
			CurSuccessfulSaves = 0;
			Helpers.VerifyLog("GAZE", $"BEGIN: {ParentObject.Blueprint} Lv{Level} gazes at {Victim.Blueprint}; base DC {GetSaveDifficulty(Level)} + caster WIL mod, +1/fail");
			XDidY(ParentObject, "level", $"an unblinking gaze at {Victim.t()}", "!", ColorAsGoodFor: ParentObject, ColorAsBadFor: Victim);
			if (Victim.IsPlayer())
			{
				Popup.Show($"{ParentObject.T()} {The.Player.DescribeDirectionToward(ParentObject)}{ParentObject.GetVerb("begin")} turning you to stone with an unblinking gaze!");
				AutoAct.Interrupt(ParentObject, true, true);
			}
			Cleo_TerraFirma_Petrifying existing = UsesSharedCount(ParentObject) ? Victim.GetEffect<Cleo_TerraFirma_Petrifying>() : null;
			if (existing != null)
			{
				CurFailedSaves = GetStoneCount(Victim);
				existing.Initiator = ParentObject;
				existing.UpdateData(null, false);
				existing.Pinned = true;
				AssocEffect = existing;
				Helpers.VerifyLog("GAZE", $"shared count: gaze resumes {Victim.Blueprint} at {CurFailedSaves} stages");
			}
			else
			{
				AssocEffect = new Cleo_TerraFirma_Petrifying(ParentObject);
				if (!Target.ApplyEffect(AssocEffect, ParentObject))
				{
					Helpers.VerifyLog("GAZE", $"BEGIN refused: {Victim.Blueprint} cannot be held in place, gaze never starts");
					AssocEffect = null;
					Target = null;
					return;
				}
			}
			if (Victim.HasEffect<Asleep>())
				Victim.FireEvent("WakeUp");
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
			if (ParentObject.HasEffect<Stun>() || ParentObject.HasEffect<StunGasStun>() || ParentObject.HasEffect<Confused>() || ParentObject.HasEffect<Asleep>() || ParentObject.HasEffect<Paralyzed>())
			{
				Helpers.VerifyLog("GAZE", $"caster incapacitated (stun/confusion/sleep/paralysis) -> gaze breaks at {CurFailedSaves}F/{CurSuccessfulSaves}S");
				CancelGaze($"You cannot hold your unblinking gaze!");
				return;
			}
			if (Target.Brain == null)
			{
				Helpers.VerifyLog("GAZE", $"brainless target {Target.Blueprint} -> gaze releases (lockout self-heal)");
				CancelGaze(Silent: true);
				return;
			}
			AssocEffect ??= Target.GetEffect<Cleo_TerraFirma_Petrifying>();
			if (!Target.InSamePartyAs(ParentObject))
				Target.Brain.Attacked(ParentObject);
			if (Target.HasEffect<Asleep>())
				Target.FireEvent("WakeUp");
			if (TryBounce())
				return;
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
				UpdateBandFx("success");
				return;
			}
			AssocEffect?.ApplyFailure();
			CurFailedSaves++;
			if (UsesSharedCount(ParentObject))
				SetStoneCount(Target, CurFailedSaves);
			if (CurFailedSaves >= NUM_OF_SAVES)
			{
				Helpers.VerifyLog("GAZE", $"10th fail: {Target.Blueprint} PETRIFIED (statue + entombed possessions)");
				if (!Petrify(ParentObject, Target))
					AssocEffect = null;
				CurFailedSaves = 0;
				CancelGaze(Silent: true);
			}
			else
			{
				ParentObject.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig");
				Target.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig");
				Cleo_TerraFirma_GorgonPetrifyFX.RockPops(Target, 2 + CurFailedSaves / 4);
				UpdateBandFx("fail");
				if (ParentObject.IsPlayer())
					DidX("continue", "to gaze", "…", ColorAsGoodFor: ParentObject, ColorAsBadFor: Target, Source: ParentObject);
			}
		}

		private bool TryBounce()
		{
			bool forced = The.Game?.GetBooleanGameState(FORCE_BOUNCE_STATE) == true;
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

		private void GazerSave()
		{
			if (ParentObject.HasEffect<Cleo_TerraFirma_Petrifying>())
			{
				Helpers.VerifyLog("GAZE", "bounce: gazer already petrifying under another gaze, no gazer save");
				return;
			}
			Cleo_TerraFirma_PetrifyingReflected self = ParentObject.GetEffect<Cleo_TerraFirma_PetrifyingReflected>();
			if (self != null && self.Receding)
			{
				ParentObject.RemoveEffect(self);
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
				ParentObject.RemoveEffect(self);
			CancelGaze(Silent: true);
		}

		public static bool AcidStrip(GameObject Gazer, GameObject Victim)
		{
			Cleo_TerraFirma_GorgonsGaze gaze = Gazer?.GetPart<Cleo_TerraFirma_GorgonsGaze>();
			if (gaze == null || Victim == null || gaze.Target != Victim || gaze.CurFailedSaves <= 0)
				return false;
			gaze.CurFailedSaves--;
			if (UsesSharedCount(Gazer))
				SetStoneCount(Victim, gaze.CurFailedSaves);
			gaze.UpdateBandFx("acid");
			return true;
		}

		private void UpdateBandFx(string why)
		{
			int net = Math.Max(0, CurFailedSaves - CurSuccessfulSaves);
			float frac = Cleo_TerraFirma_GorgonPetrifyFX.FracForNet(net);
			BandFx = Cleo_TerraFirma_GorgonPetrifyFX.Show(BandFx, Target, frac);
			Helpers.VerifyLog("GAZE", $"band ({why}): net {net} -> frac {frac:0.00}{(BandFx == null ? " [no sprite: muted/unseen/text-mode]" : "")}");
		}

		public void CancelGaze(string Reason = "", bool Silent = false)
		{
			Cleo_TerraFirma_GorgonPetrifyFX.Release(BandFx, ShrinkOut: true);
			BandFx = null;
			if (AssocEffect == null && GameObject.Validate(Target))
				AssocEffect = Target.GetEffect<Cleo_TerraFirma_Petrifying>();
			if (!Silent)
				ParentObject.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_abort");
			if (CurFailedSaves >= STONE_MARK_MIN_FAILS && GameObject.Validate(Target) && !Target.IsInGraveyard() && !Cleo_TerraFirma_Petrifying.IsAcidBodied(Target))
			{
				Helpers.VerifyLog("GAZE", $"cancel at {CurFailedSaves} fails: STONE-MARK applied to {Target.Blueprint} (partially petrified, 500 rds)");
				bool shared = UsesSharedCount(ParentObject);
				if (shared)
					AssocEffect?.UpdateData(null, true);
				else if (AssocEffect != null)
					Target.RemoveEffect(AssocEffect);
				if (!shared || !Target.HasEffect<Cleo_TerraFirma_PartiallyPetrified>())
				{
					Target.ApplyEffect(new Cleo_TerraFirma_PartiallyPetrified(), ParentObject);
					Target.PlayWorldSound("Sounds/Damage/sfx_damage_stone", 1f, Combat: true);
					XDidY(Target, "remain", "partially petrified", "!", ColorAsBadFor: Target, Source: ParentObject);
				}
			}
			else
				AssocEffect?.UpdateData(null, true);
			Cleo_TerraFirma_PetrifyingReflected self = ParentObject?.GetEffect<Cleo_TerraFirma_PetrifyingReflected>();
			if (self != null && !self.Receding && GameObject.Validate(ParentObject) && !ParentObject.IsInGraveyard())
			{
				if (self.GazerFails >= STONE_MARK_MIN_FAILS && !Cleo_TerraFirma_Petrifying.IsAcidBodied(ParentObject))
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

		public static GameObject CreateStatueOf(GameObject Source)
		{
			GameObject statue = GameObject.Create("Cleo_TerraFirma_GazeStatue");
			statue.RequirePart<RandomStatue>().SetCreature(Source.DeepCopy());
			if (XRL.Rules.Stat.Random(1, 100) <= 50)
			{
				statue.SetStringProperty("QuestVerb", "pray at");
				statue.SetStringProperty("QuestEvent", "Prayed");
			}
			else
			{
				statue.SetStringProperty("QuestVerb", "desecrate");
				statue.SetStringProperty("QuestEvent", "Desecrated");
			}
			statue.RequirePart<Shrine>();
			return statue;
		}

		public static bool Petrify(GameObject Medusa, GameObject Victim, string With = "unblinking gaze")
		{
			Cleo_TerraFirma_GorgonPetrifyFX.RockPops(Victim, 10, Burst: true);
			Victim.PlayWorldSound("burn_blast", 1f, Combat: true);
			if (Victim.IsPlayer())
				Achievement.TURNED_STONE.Unlock();
			Victim.SetIntProperty("SuppressCorpseDrops", 1);
			Cell curCell = Victim.CurrentCell;
			GameObject statue = CreateStatueOf(Victim);
			Cleo.TerraFirma.Scripts.StatueHelpers.EntombPossessions(Victim, statue);
			bool self = Medusa == Victim;
			if (self)
				XDidY(Medusa, "turn", $"{Medusa.itself} to stone with {Medusa.its} {With}", "!", ColorAsBadFor: Medusa, Source: Medusa);
			else
				XDidYToZ(Medusa, "turn", Victim, $"to stone with {Medusa.its} {With}", "!", ColorAsGoodFor: Medusa, ColorAsBadFor: Victim, Source: Medusa);
			bool died = self
				? Victim.Die(Medusa, null, $"You turned yourself to stone with your {With}.",
					$"{Victim.It} @@turned {Victim.itself} to stone with {Victim.its} {With}.")
				: Victim.Die(Medusa, null, $"You were turned to stone by the {With} of {Medusa.an()}.",
					$"{Victim.It + Victim.GetVerb("were", true, true)} @@turned to stone by the {With} of {Medusa.an()}.");
			if (died)
			{
				if (curCell.HasWall() && statue.Render != null && statue.Render.RenderLayer <= 7)
					statue.Render.RenderLayer = 8;
				curCell.AddObject(statue);
				return true;
			}
			else
			{
				Victim.RemoveEffect<Cleo_TerraFirma_Petrifying>();
				Cleo.TerraFirma.Scripts.StatueHelpers.ReturnPossessions(statue, Victim);
				Victim.ModIntProperty("SuppressCorpseDrops", -1, RemoveIfZero: true);
				statue.Obliterate();
				return false;
			}
		}
	}
}
