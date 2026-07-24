using System;
using System.Collections.Generic;
using UnityEngine;
using XRL.Rules;
using XRL.World.Effects;

namespace XRL.World.Parts.Mutation
{
	public class Cleo_TerraFirma_EarthenBarrage : BaseMutation
	{
		#region Fields
		private const string COMMAND_ID = "Cleo_TerraFirma_CommandEarthenBarrage";
		private const string COMMAND_ATTUNE_ID = "Cleo_TerraFirma_CommandAttuneStones";
		private const string BLUEPRINT = "Cleo_TerraFirma_EphemeralStone";
		private const string BLUEPRINT_RESONANT = "Cleo_TerraFirma_EphemeralStoneResonant";
		private const int RESONANT_UNLOCK_LEVEL = 10;

		private const float BASE_DAZE_CHANCE = 10f;
		private const float DAZE_CHANCE_PER_LEVEL = 3f;

		private const int TURNS_PER_CHARGE = 15;

		private const int ROCK_LIFETIME_MIN = 50;
		private const int ROCK_LIFETIME_MAX = 100;

		/// <summary>
		/// How many charges are left to draw from.
		/// </summary>
		public int Charges;

		/// <summary>
		/// How many full turns have passed since a charge has last been regained.
		/// </summary>
		public int TurnsSinceLastCharge;

		/// <summary>
		/// Cached reference to the <see cref="Cleo_TerraFirma_GravelWhorl"/> instance associated with this mutation.
		/// Maintained by <see cref="SyncWhorl"/>; active while the ability is toggled on with stones in reserve.
		/// NonSerialized -- the engine's field writer binary-formatter-warns on custom effect types; SyncWhorl re-acquires after load.
		/// </summary>
		[NonSerialized]
		public Cleo_TerraFirma_GravelWhorl AssocWhorl;

		/// <summary>
		/// The Attune Stones toggle, granted at level <see cref="RESONANT_UNLOCK_LEVEL"/>. See <see cref="SyncAttuneAbility"/>.
		/// </summary>
		public Guid AttuneAbilityID = Guid.Empty;

		/// <summary>
		/// Whether stones are currently attuned to resonance (the Attune Stones toggle is on).
		/// </summary>
		public bool Resonant => AttuneAbilityID != Guid.Empty && IsMyActivatedAbilityToggledOn(AttuneAbilityID);

		/// <summary>
		/// Guards <see cref="RetuneHeldStone"/> against the thrown-weapon replacement handler double-sculpting.
		/// </summary>
		[NonSerialized]
		private bool Retuning;
		#endregion

		#region Basic data

		public override string GetDescription()
		{
			return "You form hefty rocks suitable for throwing.";
		}

		public override string GetLevelText(int Level)
		{
			return "You sculpt stones at a rate of 1 every {{rules|" + GetTurnsPerCharge() + "}} rounds, up to a reserve of {{rules|" + MaxCharges(Level) + "}}.\n" +
				"While active, a sculpted stone occupies your thrown weapon slot and is replaced from your reserve when thrown.\n" +
				"Stones crumble to dust after {{rules|" + ROCK_LIFETIME_MIN + "}}-{{rules|" + ROCK_LIFETIME_MAX + "}} rounds of not being held as a thrown weapon.\n" +
				"Damage increment: {{rules|" + GetThrowDamage(Level) + "}}\n" +
				"Penetration bonus: {{rules|+" + GetThrowPenetrations(Level) + "}} (attuned stones forgo it; resonance pierces by matching, not force)\n" +
				"Daze chance on penetrating hit: {{rules|" + GetThrowDazeChanceIn100(Level) + "%}}\n" +
				"While you have stones in reserve, they whorl about you and deflect physical projectiles. Lasers and other energy attacks pass through.\n" +
				"Deflect chance: {{rules|3%}} per stone in reserve, +{{rules|1%}} per mutation level\n" +
				"+{{rules|1}} AV while 3 or more stones are in reserve; +{{rules|2}} AV while 9 or more are" +
				(Level >= RESONANT_UNLOCK_LEVEL
					? "\nYou may attune your stones to {{C|resonance}}: they penetrate as vibro weapons, and your whorl deflects light instead of matter, granting {{rules|+2}}/{{rules|+4}} DV in place of AV.\nAttuning retunes the stone in your hand at no cost."
					: "\nAt level {{rules|" + RESONANT_UNLOCK_LEVEL + "}}, you learn to attune your stones to {{C|resonance}}.");
		}

		public override void CollectStats(Templates.StatCollector stats)
		{
			stats.Set("Damage", GetThrowDamage(Level));
			stats.Set("Penetrations", GetThrowPenetrations(Level));
			stats.Set("DazeChance", GetThrowDazeChanceIn100(Level).ToString());
			stats.Set("TurnsPerCharge", GetTurnsPerCharge());
			stats.Set("MaxCharges", MaxCharges(Level));
			stats.Set("Lifetime", $"{ROCK_LIFETIME_MIN}-{ROCK_LIFETIME_MAX}");
			stats.Set("DeflectFull", (3 * MaxCharges(Level)) + Level);
		}

		public override bool Mutate(GameObject GO, int Level = 1)
		{
			// user-requested option (2026-07-23): Take Stones can start toggled ON for new mutants
			// (existing characters keep whatever toggle state they had -- this only runs at gain time)
			bool defaultOn = XRL.UI.Options.GetOption("Cleo_TerraFirma_TakeStonesDefaultOn").EqualsNoCase("Yes");
			ActivatedAbilityID = AddMyActivatedAbility("Earthen Barrage", COMMAND_ID, "Physical Mutations", Toggleable: true, DefaultToggleState: defaultOn, IsWorldMapUsable: true);
			Charges = MaxCharges(Level);
			SyncAbilityName();
			SyncAttuneAbility();
			return base.Mutate(GO, Level);
		}

		public override bool Unmutate(GameObject GO)
		{
			RemoveMyActivatedAbility(ref ActivatedAbilityID);
			if (AttuneAbilityID != Guid.Empty)
				RemoveMyActivatedAbility(ref AttuneAbilityID);
			AssocWhorl ??= GO.GetEffect<Cleo_TerraFirma_GravelWhorl>();
			if (AssocWhorl != null)
			{
				GO.RemoveEffect(AssocWhorl);
				AssocWhorl = null;
			}
			return base.Unmutate(GO);
		}

		public override bool ChangeLevel(int NewLevel)
		{
			SyncAttuneAbility();
			return base.ChangeLevel(NewLevel);
		}

		/// <summary>
		/// Grants or revokes the Attune Stones toggle to match the current mutation level.
		/// If revoked while attuned, the held stone falls back to its normal form.
		/// </summary>
		private void SyncAttuneAbility()
		{
			if (!GameObject.Validate(ParentObject))
				return;
			if (Level >= RESONANT_UNLOCK_LEVEL)
			{
				if (AttuneAbilityID == Guid.Empty)
					AttuneAbilityID = AddMyActivatedAbility("Attune Stones", COMMAND_ATTUNE_ID, "Physical Mutations", Toggleable: true, IsWorldMapUsable: true);
			}
			else if (AttuneAbilityID != Guid.Empty)
			{
				RemoveMyActivatedAbility(ref AttuneAbilityID);
				RetuneHeldStone();
				SyncWhorl();
			}
		}
		#endregion

		#region Events
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) &&
				ID != SingletonEvent<EndTurnEvent>.ID &&
				ID != PooledEvent<CommandEvent>.ID &&
				ID != PooledEvent<CheckExistenceSupportEvent>.ID)
				return ID == PooledEvent<ReplaceThrownWeaponEvent>.ID;
			return true;
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command == COMMAND_ATTUNE_ID)
			{
				ToggleMyActivatedAbility(AttuneAbilityID);
				RetuneHeldStone();
				SyncWhorl();
				if (Resonant)
				{
					ParentObject.PlayWorldSound("Sounds/Damage/sfx_damage_gemstone", 1f);
					XDidY(ParentObject, "attune", "the stones to a keening pitch", Source: ParentObject);
				}
				else
				{
					ParentObject.PlayWorldSound("Sounds/Throw/sfx_throwing_stone_small_impact", 1f);
					XDidY(ParentObject, "still", "the stones' hum", Source: ParentObject);
				}
				return base.HandleEvent(E);
			}
			if (E.Command != COMMAND_ID)
				return base.HandleEvent(E);
			ToggleMyActivatedAbility(ActivatedAbilityID);
			if (CanGetStone(true) && IsMyActivatedAbilityToggledOn(ActivatedAbilityID) && E.Actor.GetFirstThrownWeapon() == null)
			{
				Activate();
				XDidY(ParentObject, "retrieve", $"a {GetStoneName(Level)}", Source: ParentObject);
			}
			SyncWhorl();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			GameObject heldPebble = ParentObject.FindEquippedItem(x => IsOurStone(x) && x.IsEquippedAsThrownWeapon());
			if (heldPebble != null)
			{
				Temporary t = heldPebble.GetPart<Temporary>();
				t.Duration = Stat.Random(ROCK_LIFETIME_MIN, ROCK_LIFETIME_MAX); // continually sustain as long as held
			}
			// a sculpted stone is a held intention, not a possession (user ruling 2026-07-23): one that
			// gets UNEQUIPPED into the pack returns to the reserve at end of turn -- charge refunded when
			// there's room, otherwise it simply crumbles. End-of-turn (not the unequip event) on purpose:
			// throws also unequip mid-flight, and hooking unequip would refund stones actually thrown.
			List<GameObject> pocketed = ParentObject.Inventory?.GetObjectsDirect()?.FindAll(IsOurStone);
			if (pocketed != null && pocketed.Count > 0)
			{
				foreach (GameObject stone in pocketed)
				{
					bool refunded = Charges < MaxCharges(Level);
					if (refunded)
						Charges++;
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("EB", $"pocketed sculpted stone reabsorbed ({(refunded ? "charge refunded, now " + Charges : "reserve full, crumbled")})");
					stone.Destroy(null, Silent: true);
				}
				SyncAbilityName();
				if (ParentObject.IsPlayer())
					XDidY(ParentObject, "return", "the stone to the earth", Source: ParentObject); // RATIFIED 2026-07-23 (one message for both refund and crumble: the stone leaves you either way)
			}
			if (Charges < MaxCharges(Level))
			{
				TurnsSinceLastCharge++;
				if (TurnsSinceLastCharge >= GetTurnsPerCharge())
				{
					Charges++;
					TurnsSinceLastCharge = 0;
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("EB", $"regen: +1 charge (now {Charges}/{MaxCharges(Level)}) after {GetTurnsPerCharge()} turns (WIL {ParentObject?.Stat("Willpower") ?? 0})");
					SyncAbilityName();
					// QoL -- equip stones automatically if we previously ran out of charges while throwing them
					if (Charges == 1 && CanGetStone(true) && ParentObject.GetFirstThrownWeapon() == null && IsMyActivatedAbilityToggledOn(ActivatedAbilityID))
					{
						Activate();
						XDidY(ParentObject, "retrieve", $"another {GetStoneName(Level)}", Source: ParentObject);
					}
				}
			}
			SyncWhorl();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ReplaceThrownWeaponEvent E)
		{
			if (Retuning)
				return base.HandleEvent(E);
			// re-arm after throwing ANY stone, not just our own pebbles (bug fix 2026-07-23: with the
			// toggle on, throwing a picked-up ground rock -- fissure fallout, Small Stones -- emptied
			// the slot and the old IsOurStone-only check shrugged; live-fire repro'd by the user).
			// Non-stone thrown weapons (javelins, grenades) still don't trigger a sculpt: the toggle
			// promises stones-on-tap, not a hijack of every thrown-weapon workflow.
			bool wasStone = E.PreviouslyEquipped != null
				&& (IsOurStone(E.PreviouslyEquipped) || E.PreviouslyEquipped.HasTag("Cleo_TerraFirma_IsAnyStone"));
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("EB", $"replace-thrown event: prev={E.PreviouslyEquipped?.Blueprint ?? "none"} (stone={wasStone}), toggle={IsMyActivatedAbilityToggledOn(ActivatedAbilityID)}, charges={Charges} -> {(wasStone && CanGetStone(true) && IsMyActivatedAbilityToggledOn(ActivatedAbilityID) ? "auto-sculpt replacement" : "no action")}");
			if (wasStone && CanGetStone(true) && IsMyActivatedAbilityToggledOn(ActivatedAbilityID))
			{
				Activate();
				XDidY(ParentObject, "retrieve", $"another {GetStoneName(Level)}", Source: ParentObject);
			}
			SyncWhorl();
			return base.HandleEvent(E);
		}
		#endregion

		#region Logic
		/// <summary>
		/// Whether or not a stone can currently be retrieved.
		/// Checks if <see cref="Charges"/> > 0 and if the parent object has a thrown slot open.
		/// </summary>
		/// <param name="Silent">If false, shows failure messages to the parent object.</param>
		public bool CanGetStone(bool Silent = false)
		{
			if (!GameObject.Validate(ParentObject) || !ParentObject.IsActivatedAbilityUsable(ActivatedAbilityID))
				return false;
			if (Charges <= 0)
			{
				if (!Silent)
					ParentObject.Fail("You have no stones available to throw.");
				return false;
			}
			var thrownSlot = ParentObject.Body?.GetUnequippedPart("Thrown Weapon");
			if (thrownSlot == null)
			{
				if (!Silent)
					ParentObject.Fail("You have no empty thrown weapon slots.");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Creates a stone using <see cref="MakeStone"/> and automatically equips it to the parent object.
		/// Charges are <b>not</b> sanity-checked in this function; the stone will be made even if none are remaining.
		/// </summary>
		public void Activate()
		{
			Charges = Mathf.Max(0, Charges - 1);
			GameObject stone = MakeStone();
			string tile = "", thrownsound = "", impactsound = "";
			GetStoneCosmeticData(Level, ref tile, ref thrownsound, ref impactsound);
			stone.DisplayName = (Resonant ? "{{C|resonant}} " : "") + "sculpted " + GetStoneName(Level);
			stone.Render.Tile = tile;
			stone.SetStringProperty("ThrownSound", thrownsound);
			stone.SetStringProperty("ImpactSound", impactsound);
			ParentObject.ReceiveObject(stone);
			ParentObject.AutoEquip(stone, true, Silent: true);
			SyncAbilityName();
		}

		/// <summary>
		/// Creates an object of type <see cref="BLUEPRINT"/> and configures its stats according to the mutation's level.
		/// </summary>
		public GameObject MakeStone()
		{
			GameObject stone = GameObject.Create(Resonant ? BLUEPRINT_RESONANT : BLUEPRINT);
			stone.SetIntProperty("NeverStack", 1);
			string scaledDamage = GetThrowDamage(Level);
			int scaledPens = GetThrowPenetrations(Level);
			// Attuned stones keep only a fifth of the trained penetration bonus (user ruling 2026-07-22:
			// a fraction of the unattuned value, +1 at Lv10): vibro already matches penetration to the
			// target's armor, and stacking the full bonus on top rolled ~6 penetrations against ANYTHING --
			// double a normal stone's damage vs the soft targets normal stones are supposed to own.
			if (Resonant)
				scaledPens /= 5;
			if (stone.TryGetPart(out MeleeWeapon mw))
			{
				mw.BaseDamage = scaledDamage;
				mw.PenBonus = scaledPens;
			}
			if (stone.TryGetPart(out ThrownWeapon tw))
			{
				tw.Damage = scaledDamage;
				tw.PenetrationBonus = scaledPens;
			}
			stone.AddPart(new Cleo_TerraFirma_DazeOnThrowPensPart(GetThrowDazeChanceIn100(Level)));
			return stone;
		}

		/// <summary>
		/// Updates the name of this mutation's activated ability to reflect how many charges it has remaining.
		/// </summary>
		private void SyncAbilityName()
		{
			ActivatedAbilityEntry ent = MyActivatedAbility(ActivatedAbilityID);
			if (ent != null)
				ent.DisplayName = $"Take Stones ({Charges}/{MaxCharges(Level)})";
		}
		#endregion

		#region Helpers
		/// <summary>
		/// Turns per regained charge, scaled by the owner's Willpower.
		/// Same formula as Light Manipulation's radius regrowth: -5% per point above 16, clamped to [base/5, base*5].
		/// </summary>
		public int GetTurnsPerCharge()
		{
			return GetTurnsPerCharge(ParentObject?.Stat("Willpower") ?? 16);
		}

		public static int GetTurnsPerCharge(int Willpower)
		{
			int num = TURNS_PER_CHARGE;
			int pct = (Willpower - 16) * 5;
			if (pct != 0)
				num = num * (100 - pct) / 100;
			return Mathf.Clamp(num, TURNS_PER_CHARGE / 5, TURNS_PER_CHARGE * 5);
		}

		/// <summary>
		/// Deflect chance for the gravel whorl: 3% per stone in reserve, +1% per mutation level.
		/// </summary>
		public int GetDeflectChance() => (3 * Charges) + Level;

		/// <summary>
		/// Whether an object is one of this mutation's sculpted stones, in either attunement.
		/// </summary>
		public static bool IsOurStone(GameObject Object) => Object?.Blueprint == BLUEPRINT || Object?.Blueprint == BLUEPRINT_RESONANT;

		/// <summary>
		/// Re-sculpts the stone currently held as a thrown weapon to match the current attunement,
		/// in place and at no charge cost -- a player who forgets to pre-toggle never wastes a stone.
		/// </summary>
		public void RetuneHeldStone()
		{
			GameObject held = ParentObject?.FindEquippedItem(x => IsOurStone(x) && x.IsEquippedAsThrownWeapon());
			if (held == null)
				return;
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("EB", $"retune in place: {held.Blueprint} -> {(Resonant ? "resonant" : "normal")} at net-zero charge cost");
			Retuning = true;
			try
			{
				held.Obliterate(Silent: true);
			}
			finally
			{
				Retuning = false;
			}
			Charges++; // Activate() spends one; net zero -- the retune is free
			Activate();
		}

		/// <summary>
		/// Applies, refreshes, or removes the <see cref="Cleo_TerraFirma_GravelWhorl"/> effect so it always
		/// reflects the current reserve. The reserve is the shield -- it whorls as long as stones remain,
		/// regardless of whether Take Stones is toggled on (the toggle only governs auto-equipping).
		/// </summary>
		public void SyncWhorl()
		{
			if (!GameObject.Validate(ParentObject))
				return;
			if (Charges > 0)
			{
				if (!ParentObject.HasEffect<Cleo_TerraFirma_GravelWhorl>())
				{
					AssocWhorl = new Cleo_TerraFirma_GravelWhorl(GetDeflectChance());
					ParentObject.ApplyEffect(AssocWhorl);
				}
				AssocWhorl ??= ParentObject.GetEffect<Cleo_TerraFirma_GravelWhorl>();
				if (AssocWhorl != null)
					AssocWhorl.Sync(GetDeflectChance(), Charges, Resonant); // lifetime is managed here, not by countdown
			}
			else
			{
				AssocWhorl ??= ParentObject.GetEffect<Cleo_TerraFirma_GravelWhorl>();
				if (AssocWhorl != null)
				{
					ParentObject.RemoveEffect(AssocWhorl);
					AssocWhorl = null;
				}
			}
		}

		public static string GetThrowDamage(int Lv)
		{
			var diceSize = Mathf.FloorToInt(Lv / 3);
			var bonus = Lv % 3;
			return $"{diceSize + 1}d6{(bonus != 0 ? $"+{bonus}" : "")}";
		}

		public static int GetThrowPenetrations(int Lv)
		{
			return (int)Mathf.Floor(Lv / 2);
		}

		public static float GetThrowDazeChanceIn100(int Lv)
		{
			return BASE_DAZE_CHANCE + (DAZE_CHANCE_PER_LEVEL * Lv);
		}

		public static int MaxCharges(int Lv)
		{
			return (int)(3 + Mathf.Floor(Lv - 1));
		}

		public static string GetStoneName(int Lv)
		{
			if (Lv >= 10)
				return "boulder";
			else if (Lv >= 5)
				return "stone";
			return "pebble";
		}

		public static void GetStoneCosmeticData(int Lv, ref string Tile, ref string ThrownSound, ref string ImpactSound)
		{
			if (Lv >= 10)
			{
				Tile = "Items/sw_mediumboulder.bmp";
				ThrownSound = "Sounds/Throw/sfx_throwing_stone_large_throw";
				ImpactSound = "Sounds/Throw/sfx_throwing_stone_large_impact";
			}
			else if (Lv >= 5)
			{
				Tile = "Items/sw_stone_large.bmp";
				ThrownSound = "Sounds/Throw/sfx_throwing_stone_medium_throw";
				ImpactSound = "Sounds/Throw/sfx_throwing_stone_medium_impact";
			}
			else
			{
				Tile = "Items/sw_smallstone.bmp";
				ThrownSound = "Sounds/Throw/sfx_throwing_stone_small_throw";
				ImpactSound = "Sounds/Throw/sfx_throwing_stone_small_impact";
			}
		}
		#endregion
	}
}
