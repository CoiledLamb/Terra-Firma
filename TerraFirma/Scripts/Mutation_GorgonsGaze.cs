using System;
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
		#endregion

		#region Basic data
		public override string GetDescription()
		{
			return "Your unblinking gaze stops creatures in their tracks.";
		}

		public override string GetLevelText(int Level)
		{
			return "You transfix an opponent with your unblinking gaze and slowly turn them to stone.\n" +
				"Taking any action other than passing the turn will break your gaze.\n" +
				"Each round your target makes a Toughness save; failures stiffen them, and successes shake off one stage of petrification.\n" +
				"Save difficulty: {{rules|" + GetSaveDifficulty(Level) + "}} + your Willpower modifier, +{{rules|1}} per failed save\n" +
				"Each failure: held in place, {{rules|-" + QUICKNESS_LOSS_PER_SAVE + "}} Quickness\n" +
				"First success frees movement, second ends the slowing, third breaks the gaze entirely.\n" +
				"Targets that fail {{rules|" + NUM_OF_SAVES + "}} saves are permanently turned to stone.\n" +
				"Targets that escape after {{rules|" + STONE_MARK_MIN_FAILS + "}} or more failures are left partially petrified: {{rules|-" + Cleo_TerraFirma_PartiallyPetrified.MOVE_SPEED_PENALTY + "}} move speed, {{rules|-" + Cleo_TerraFirma_PartiallyPetrified.QUICKNESS_PENALTY + "}} Quickness, {{rules|+" + Cleo_TerraFirma_PartiallyPetrified.AV_BONUS + "}} AV for {{rules|" + Cleo_TerraFirma_PartiallyPetrified.DURATION + "}} rounds\n" +
				"Range: {{rules|sight}}\n" +
				"Cooldown: {{rules|" + BASE_COOLDOWN + "}} rounds";
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
				ID != PooledEvent<CommandEvent>.ID &&
				ID != SingletonEvent<EndTurnEvent>.ID)
				return ID == SingletonEvent<UseEnergyEvent>.ID;
			return true;
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command != COMMAND_ID)
				return base.HandleEvent(E);
			Cell cell = PickDestinationCell(GetRange(Level), RequireCombat: true, Label: "Choose a target to gaze at.");
			if (cell == null)
				return false;
			GameObject victim = cell.GetCombatTarget(ParentObject, true);
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
					Popup.ShowFail($"{victim.T()} is already being petrified! Wait your turn!");
				return false;
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
			return 8 + (Lv * 2);
		}

		public static int GetRange(int Lv) => 80; // "sight", matching Sunder Mind — if you can see it, you can stone it

		public void BeginGaze(GameObject Victim)
		{
			Target = Victim;
			CurFailedSaves = 0;
			CurSuccessfulSaves = 0;
			Helpers.VerifyLog("GAZE", $"BEGIN: {ParentObject.Blueprint} Lv{Level} gazes at {Victim.Blueprint}; base DC {GetSaveDifficulty(Level)} + caster WIL mod, +1/fail");
			XDidY(ParentObject, "level", $"an unblinking gaze at {Victim.t()}", "!", ColorAsGoodFor: ParentObject, ColorAsBadFor: Victim);
			if (Victim.IsPlayer())
			{
				Popup.Show($"{ParentObject.T()} {The.Player.DescribeDirectionToward(ParentObject)} {ParentObject.GetVerb("begin")} turning you to stone with an unblinking gaze!");
				AutoAct.Interrupt(ParentObject, true, true);
			}
			AssocEffect = new Cleo_TerraFirma_Petrifying(ParentObject);
			Target.ApplyEffect(AssocEffect, ParentObject);
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
				CancelGaze($"{Target.T()} exits the reach of your unblinking gaze, breaking its hold.");
				return;
			}
			AssocEffect ??= Target.GetEffect<Cleo_TerraFirma_Petrifying>(); // re-link after save/load
			Target.Brain.Attacked(ParentObject);
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
					CancelGaze($"{Target.T()} shakes off your unblinking gaze entirely, breaking its hold!");
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
				return;
			}
			AssocEffect?.ApplyFailure();
			CurFailedSaves++;
			if (CurFailedSaves >= NUM_OF_SAVES)
			{
				Helpers.VerifyLog("GAZE", $"10th fail: {Target.Blueprint} PETRIFIED (statue + entombed possessions)");
				Petrify(ParentObject, Target);
				CancelGaze(Silent: true);
			}
			else
			{
				ParentObject.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig");
				Target.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig");
				Target.Splatter("&y*");
				Target.ParticleBlip("&y*", CurFailedSaves);
				if (ParentObject.IsPlayer())
					DidX("continue", "to gaze", "…", ColorAsGoodFor: ParentObject, ColorAsBadFor: Target, Source: ParentObject);
			}
		}

		public void CancelGaze(string Reason = "", bool Silent = false)
		{
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
			CurFailedSaves = 0;
			CurSuccessfulSaves = 0;
			if (ParentObject.IsPlayer() && !Silent && Reason != string.Empty)
				AddPlayerMessage(Reason, ConsequentialColorChar(null, ParentObject));
			if (Target != null && !Silent)
				XDidY(Target, "stop", "turning to stone", "!", ColorAsGoodFor: Target, ColorAsBadFor: ParentObject);
			Target = null;
			AssocEffect = null;
		}

		public static void Petrify(GameObject Medusa, GameObject Victim)
		{
			Victim.Splatter("&y*");
			Victim.ParticleBlip("&y*", 20);
			The.Player.PlayWorldSound("burn_blast", 1f, Combat: true);
			if (Victim.IsPlayer())
				Achievement.TURNED_STONE.Unlock();
			Victim.SetIntProperty("SuppressCorpseDrops", 1);
			Cell curCell = Victim.CurrentCell;
			GameObject statue = StoneGaze.CreateStatueOf(Victim); // no reason to reinvent the weel
			Cleo.TerraFirma.Scripts.StatueHelpers.EntombPossessions(Victim, statue); // possessions petrify with them -- the statue is lootable
			XDidYToZ(Medusa, "turn", Victim, $"to stone with {Medusa.poss("unblinking gaze")}", "!", ColorAsGoodFor: Medusa, ColorAsBadFor: Victim, Source: Medusa);
			if (Victim.Die(Medusa, null, $"You were turned to stone by the unblinking gaze of {Medusa.an()}.",
				$"{Victim.It + Victim.GetVerb("were", true, true)} @@turned to stone by the unblinking gaze of {Medusa.an()}."))
			{
				curCell.AddObject(statue);
			}
			else
			{
				Cleo.TerraFirma.Scripts.StatueHelpers.ReturnPossessions(statue, Victim); // death was prevented -- give it all back
				statue.Obliterate();
			}
		}
		#endregion
	}
}
