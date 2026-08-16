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

		// Daze rider CUT 2026-08-06 (beta rebalance pass, user: "cutting the daze rider from
		// earthen barrage"): constants, formula, part attachment, and text all removed.
		// Part_DazeOnThrowPens.cs survives as an orphan -- earmarked for a wild rock-thrower
		// variant (see the cragmensch note in TODO-archive).

		private const int TURNS_PER_CHARGE = 15;

		private const int ROCK_LIFETIME_MIN = 50;
		private const int ROCK_LIFETIME_MAX = 100;

		/// <summary>World-map travel advances time in batched turn ticks far larger than the rock
		/// lifetime, so the per-end-of-turn sustain cannot keep pace and the held stone crumbles
		/// mid-journey (user beta flag 2026-08-06). While held, the stone rides this oversized
		/// lease instead; the promised 50-100 round lifetime is handed back the moment it is thrown.</summary>
		private const int HELD_SUSTAIN_LEASE = 9999;

		/// <summary>
		/// How many charges are left to draw from.
		/// </summary>
		public int Charges;

		/// <summary>
		/// How many full turns have passed since a charge has last been regained.
		/// </summary>
		public int TurnsSinceLastCharge;

		/// <summary>
		/// One-shot QoL latch (user 2026-08-06): the first end-of-turn with the toggle on arms a
		/// starting character automatically, exactly once. Serialized so a load never re-fires it.
		/// </summary>
		public bool EverArmed;

		/// <summary>
		/// True while the last end-of-turn saw us on the world map -- arms the arrival backstop
		/// (user beta flag 2026-08-06). Serialized so saving mid-journey keeps it.
		/// </summary>
		public bool WasOnWorldMap;

		/// <summary>Bump when the serialized shape changes again; Read branches on it.</summary>
		private const int FORMAT_VERSION = 2;

		public override void Write(GameObject Basis, SerializationWriter Writer)
		{
			Writer.Write(Cleo.TerraFirma.Scripts.Helpers.SERIAL_SENTINEL);
			Writer.Write(FORMAT_VERSION);
			base.Write(Basis, Writer);
		}

		public override void Read(GameObject Basis, SerializationReader Reader)
		{
			// legacy blocks fail this having cost only 4 bytes; ReadError recovers or resets.
			// The check exists because WasOnWorldMap (2026-08-07 deploy wave) changed the
			// positional field layout, and EB's all-primitive tail means a misaligned read
			// would NOT throw -- it would silently overrun into the next part's stream.
			if (Reader.ReadInt32() != Cleo.TerraFirma.Scripts.Helpers.SERIAL_SENTINEL)
				throw new FormatException("Cleo_TerraFirma_EarthenBarrage: pre-sentinel block");
			Reader.ReadInt32(); // FORMAT_VERSION: branch here when the shape next changes
			base.Read(Basis, Reader);
		}

		/// <summary>
		/// Save-compat recovery (2026-08-07 shim): first re-parse the block as the pre-sentinel
		/// LOCAL layout (current reflection fields, no header) and accept only on EXACT
		/// end-of-block alignment -- that loads every post-WasOnWorldMap local save perfectly.
		/// The friends-beta layout lacks WasOnWorldMap, so its re-parse lands one byte short of
		/// the block end and falls through to a clean reset (state re-primes: charges regrow,
		/// the toggle re-arms; mutation LEVEL resets too -- the one casualty, flagged in the
		/// beta update note). Either way the reader seeks cleanly past and the engine logs nothing.
		/// </summary>
		public override bool ReadError(Exception Exception, SerializationReader Reader, long Start, int Length)
		{
			try
			{
				Reader.Stream.Position = Start;
				Reader.ReadTokenizedType();
				StatShifter.Load(Reader, ParentObject); // discard: EB carries no stat shifts
				base.Read(ParentObject, Reader);
				if (Reader.Stream.Position == Start + Length)
				{
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SERIAL", $"EB pre-sentinel local block recovered in place (charges={Charges}, level intact)");
					return true;
				}
			}
			catch
			{
			}
			Charges = 0;
			TurnsSinceLastCharge = 0;
			EverArmed = false;
			WasOnWorldMap = false;
			Reader.Stream.Position = Start + Length;
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SERIAL", $"EB legacy block reset ({Length} byte(s) skipped; beta-layout save -- level re-set expected)");
			return true;
		}

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
			// 2026-07-26 voice sitting (upper ladder accepted as drafted, "it's fine"), built
			// 2026-07-27: the design-rationale parenthetical CUT; compound lines split one fact per
			// line; retune QoL line CUT (goes undocumented, base idiom); reserve clause CUT from the
			// slot line; vibro phrasing recast on the Vibro Dagger/Blade RulesDescription precedent
			// (states the rule, drops the word "vibro", names no trade-off - the deeper precedent);
			// flat constants bare per the Electrical Generation pattern (user RULED "do whatever is
			// most base game accurate there"). (The old DV-in-place-of-AV caveat retired with the
			// whorl stat shifts, cut 2026-08-06 -- the parked whorl-DV fix is moot.)
			return "You sculpt a stone every {{rules|" + GetTurnsPerCharge() + "}} rounds, up to a reserve of {{rules|" + MaxCharges(Level) + "}}.\n" +
				"Sculpted stones occupy your thrown weapon slot.\n" +
				"Stones not held as a thrown weapon crumble to dust after " + ROCK_LIFETIME_MIN + "-" + ROCK_LIFETIME_MAX + " rounds.\n" +
				"Damage increment: {{rules|" + GetThrowDamage(Level) + "}}\n" +
				// DRAFT (pen-cap rework 2026-08-09, awaiting text signoff): the bonus line
				// became a cap line; phrasing is Claude's until the user rules
				"Stone penetration: your Strength modifier, up to {{rules|" + GetPenetrationCap(Level) + "}}\n" +
				// AV/DV lines CUT with the whorl stat shifts (rebalance 2026-08-06)
				"Stones in reserve whorl about you and deflect physical projectiles.\n" +
				"Lasers and other energy attacks pass through the whorl.\n" +
				"Deflect chance: 3% per stone in reserve, +1% per mutation level" +
				(Level >= RESONANT_UNLOCK_LEVEL
					? "\nYou may attune your stones to {{C|resonance}}.\nResonant stones' penetration value is equal to the defender's armor value.\nWhile attuned, your whorl deflects light instead of matter."
					: "\nAt level " + RESONANT_UNLOCK_LEVEL + ", you learn to attune your stones to {{C|resonance}}.");
		}

		public override void CollectStats(Templates.StatCollector stats)
		{
			stats.Set("Damage", GetThrowDamage(Level));
			stats.Set("PenetrationCap", GetPenetrationCap(Level));
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
			// NPC auto-arm (2026-08-06 advertisement pass, user: legendaries can roll pool
			// mutations): the Take Stones toggle is player UX -- a generated NPC keeps it on
			// and keeps a stone sculpted. No AI ability list needed: base AI throws thrown-slot
			// weapons natively (the wild-baboon scavenger precedent), so staying armed IS the
			// advertisement. Quiet arm -- the retrieve flourish is for deliberate casts.
			if (!ParentObject.IsPlayer())
			{
				if (!IsMyActivatedAbilityToggledOn(ActivatedAbilityID))
					ToggleMyActivatedAbility(ActivatedAbilityID);
				if (IsMyActivatedAbilityToggledOn(ActivatedAbilityID) && ParentObject.GetFirstThrownWeapon() == null && CanGetStone(true))
					Activate();
			}
			else if (!EverArmed && IsMyActivatedAbilityToggledOn(ActivatedAbilityID))
			{
				// one-shot QoL (user 2026-08-06): a character who begins with Take Stones toggled
				// on begins ARMED -- the first end-of-turn sculpts their opening stone instead of
				// waiting for a manual retoggle. Fires once ever; the deliberate re-arm scope
				// (throws + the ran-dry regen edge) stays exactly as ruled 2026-07-23.
				EverArmed = true;
				if (ParentObject.GetFirstThrownWeapon() == null && CanGetStone(true))
				{
					Activate();
					XDidY(ParentObject, "retrieve", $"a {GetStoneName(Level)}", Source: ParentObject);
				}
			}
			// world-map arrival backstop (user beta flag 2026-08-06, "take stone auto equip not
			// working after coming down from world map"): if travel still cost us the held stone
			// (a journey outlasting even the oversized lease, or a loss path we haven't met),
			// re-sculpt on the first ground turn. Not a widening of the ruled re-arm scope --
			// travel loss is the wear of time, not a player choice. NPCs skip it: their branch
			// above already re-arms every turn.
			if (ParentObject.OnWorldMap())
			{
				WasOnWorldMap = true;
			}
			else if (WasOnWorldMap)
			{
				WasOnWorldMap = false;
				if (ParentObject.IsPlayer() && IsMyActivatedAbilityToggledOn(ActivatedAbilityID)
					&& ParentObject.GetFirstThrownWeapon() == null && CanGetStone(true))
				{
					Activate();
					XDidY(ParentObject, "retrieve", $"a {GetStoneName(Level)}", Source: ParentObject);
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("EB", "world-map arrival re-arm: slot came back empty, sculpted a fresh stone");
				}
			}
			GameObject heldPebble = ParentObject.FindEquippedItem(x => IsOurStone(x) && x.IsEquippedAsThrownWeapon());
			if (heldPebble != null)
			{
				Temporary t = heldPebble.GetPart<Temporary>();
				t.Duration = HELD_SUSTAIN_LEASE; // oversized lease vs world-map turn batching; the 50-100 round promise returns on throw
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
			SyncAbilityName(); // the "+1" armed suffix (2026-08-12) can go stale without a charge change -- e.g. a held stone lost to anything but a throw
			SyncWhorl();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ReplaceThrownWeaponEvent E)
		{
			if (Retuning)
				return base.HandleEvent(E);
			// the thrown stone leaves the sustain: hand back the promised 50-100 round lifetime
			// (its lease was oversized while held -- see HELD_SUSTAIN_LEASE)
			if (E.PreviouslyEquipped != null && IsOurStone(E.PreviouslyEquipped)
				&& E.PreviouslyEquipped.GetPart<Temporary>() is Temporary flung)
				flung.Duration = Stat.Random(ROCK_LIFETIME_MIN, ROCK_LIFETIME_MAX);
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
			SyncAbilityName(); // a dry throw drops the "+1" suffix with no Activate() to sync it (2026-08-12)
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
			// FLAT PEN BONUS RETIRED (L20 pass, RULED 2026-08-09 "yeag, that sounds great"
			// over the simmed menu): the mutation now raises the stone's Penetration CAP
			// (1+L/3) and the thrower's Strength mod fills it -- ThrownWeapon.cs:122, PV =
			// min(StrMod, Penetration) + bonus, the same build-investment grammar as
			// Fissure's pulse PV. Vanilla thrown caps run 0-12, ours 1..7 by L20. Ego-lean
			// casters throw PV-1 pebble artillery; the L20 spike costs Str 30. Attuned
			// stones get NO trained pen at all (RULED same sitting: "attuning loses your PV
			// bonus entirely") -- pure vibro, the fifth-of-the-bonus bookkeeping retired.
			if (stone.TryGetPart(out MeleeWeapon mw))
			{
				mw.BaseDamage = scaledDamage;
				mw.PenBonus = 0;
				if (!Resonant)
					mw.MaxStrengthBonus = GetPenetrationCap(Level); // melee mirrors the throw: Str fills a level-scaled cap
			}
			if (stone.TryGetPart(out ThrownWeapon tw))
			{
				tw.Damage = scaledDamage;
				tw.PenetrationBonus = 0;
				if (!Resonant)
					tw.Penetration = GetPenetrationCap(Level);
			}
			return stone;
		}

		/// <summary>
		/// Updates the name of this mutation's activated ability to reflect how many charges it has remaining.
		/// Format RULED 2026-08-12 (user, from the subscriber's 0-but-can-still-throw report: "0/3+1
		/// makes sense as like 'i have no stones in reserve and 1 equipped'"): reserve/max plus "+1"
		/// while a sculpted stone rides the thrown slot, so 0 without the suffix always means dry.
		/// </summary>
		private void SyncAbilityName()
		{
			ActivatedAbilityEntry ent = MyActivatedAbility(ActivatedAbilityID);
			if (ent == null)
				return;
			bool armed = ParentObject?.FindEquippedItem(x => IsOurStone(x) && x.IsEquippedAsThrownWeapon()) != null;
			ent.DisplayName = $"Take Stones ({Charges}/{MaxCharges(Level)}{(armed ? "+1" : "")})";
		}

		/// <summary>
		/// Pickup re-arm (subscriber report 2026-08-12, fix shape ruled same day): grabbing one of
		/// our own sculpted stones off the ground while the toggle is on and the thrown slot is
		/// empty puts it straight back in hand -- the stoop IS the intent to throw. Called by
		/// <see cref="Cleo_TerraFirma_StoneRearmOnTakePart"/>, which gates on came-from-a-cell
		/// (BeginBeingTaken snapshot) -- NOT on TakenEvent alone, because the unequip-to-inventory
		/// flow also ends in the take machinery (eCommandFreeTakeObject IS CommandTakeObject; the
		/// ungated version fought the engine's slot-clearing during swaps and the stone died --
		/// user live-catch, same day). Deliberate unequips and swap displacements therefore never
		/// route here; the 2026-07-23 end-of-turn reabsorb governs them. Deliberately does NOT
		/// touch plain rocks (IsAnyStone): auto-wielding ordinary loot would hijack scavenging;
		/// a dry thrower still self-heals on the regen tick.
		/// </summary>
		public void OnStoneTaken(GameObject Stone)
		{
			if (!GameObject.Validate(ParentObject) || !GameObject.Validate(Stone))
				return;
			if (!IsOurStone(Stone) || !IsMyActivatedAbilityToggledOn(ActivatedAbilityID))
				return;
			if (ParentObject.GetFirstThrownWeapon() != null || ParentObject.Body?.GetUnequippedPart("Thrown Weapon") == null)
				return;
			ParentObject.AutoEquip(Stone, true, Silent: true);
			if (Stone.GetPart<Temporary>() is Temporary lease)
				lease.Duration = HELD_SUSTAIN_LEASE; // back on the held lease immediately; EndTurn would catch it anyway
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("EB", $"pickup re-arm: {Stone.Blueprint} taken off the ground went straight back into the empty thrown slot");
			SyncAbilityName();
			SyncWhorl();
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

		/// <summary>Thrown-stone damage dice.
		/// LADDER F, RULED 2026-08-15 ("we can go with F") -- the early-weighted cut.
		/// History: the 2026-08-06 pen cut spent penetration and left the dice alone, but
		/// stones still one-shot electrofuges around L4. The sim (design/eb_damage_sim2.py)
		/// found the reason: damage rolls once PER PENETRATION, so dice count multiplies and
		/// the problem is the CEILING, not the average. 2d6+1 tops out at 26 on a two-pen
		/// throw against an 18-22 HP target. Trading dice for flat bonus at a similar average
		/// pulls that ceiling down -- which is why this is a table and not a smaller formula.
		/// L1-10 is hand-authored; L11+ falls through to the pre-existing curve, which the
		/// table meets exactly at L10 (4d6+1), so there is no seam and the lategame is
		/// untouched by design (the whole point of picking F over G).
		/// Every level still ticks: dice or flat goes up at all ten steps.</summary>
		private static readonly int[] LADDER_DICE = { 1, 1, 1, 1, 2, 2, 3, 3, 4, 4 };
		private static readonly int[] LADDER_FLAT = { 0, 1, 2, 3, 0, 1, 0, 1, 0, 1 };

		public static string GetThrowDamage(int Lv)
		{
			int dice, bonus;
			if (Lv <= 10)
			{
				int i = Mathf.Clamp(Lv, 1, 10) - 1;
				dice = LADDER_DICE[i];
				bonus = LADDER_FLAT[i];
			}
			else
			{
				dice = Mathf.FloorToInt(Lv / 3) + 1;
				bonus = Lv % 3;
			}
			return $"{dice}d6{(bonus != 0 ? $"+{bonus}" : "")}";
		}

		/// <summary>The stone's Penetration CAP -- how much of the thrower's Strength mod the
		/// stone can carry into PV (ThrownWeapon.cs:122: PV = min(StrMod, Penetration)).
		/// History: flat bonus Lv/2 -&gt; Lv/3 (2026-08-06 pen cut, "I lean penetration cut") -&gt;
		/// RETIRED as a bonus at the L20 pass (RULED 2026-08-09): the same 1+Lv/3 ladder now
		/// scales the cap instead, so penetration is bought with Strength, not granted by
		/// level. 2/4/7 at L3/9/18, inside the vanilla thrown range (0-12, cluster at 9).</summary>
		public static int GetPenetrationCap(int Lv)
		{
			return 1 + Lv / 3;
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
