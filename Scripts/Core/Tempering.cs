using System;
using System.Collections.Generic;
using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Capabilities;
using XRL.World.Parts;

namespace Cleo.TerraFirma.Scripts
{
	public static class Cleo_TerraFirma_Tempering
	{
		public const string OWED_PREFIX = "Cleo_TerraFirma_TemperOwed@";

		public const string ROLL_PROPERTY = "Cleo_TerraFirma_TemperRoll";

		public const string NAMING_TYPE = "Cleo_TerraFirma_Tempering";

		public const int OFFER_COUNT = 3;

		public const string WRIT_BLUEPRINT = "Cleo_TerraFirma_GuildWrit";

		public static string OwedKey(string WarrenZoneID) => OWED_PREFIX + WarrenZoneID;

		public static void Grant(GameObject Player, string WarrenZoneID, int Count = 1)
		{
			if (Player == null || string.IsNullOrEmpty(WarrenZoneID) || Count <= 0)
				return;
			Player.ModIntProperty(OwedKey(WarrenZoneID), Count);
			Helpers.VerifyLog("TEMPER", $"granted {Count} at {WarrenZoneID}, now {Owed(Player, WarrenZoneID)}");
		}

		public static int Owed(GameObject Player, string WarrenZoneID)
		{
			if (Player == null || string.IsNullOrEmpty(WarrenZoneID))
				return 0;
			return Player.GetIntProperty(OwedKey(WarrenZoneID));
		}

		public static GameObject FindWrit(GameObject Player) => Player?.Inventory?.FindObjectByBlueprint(WRIT_BLUEPRINT);

		public static int Available(GameObject Player, string WarrenZoneID)
		{
			return Owed(Player, WarrenZoneID) + (FindWrit(Player) != null ? 1 : 0);
		}

		public static string SpendOne(GameObject Player, string WarrenZoneID)
		{
			if (Owed(Player, WarrenZoneID) > 0)
			{
				Player.ModIntProperty(OwedKey(WarrenZoneID), -1, RemoveIfZero: true);
				return "owed";
			}
			GameObject writ = FindWrit(Player);
			if (writ == null)
				return null;
			writ.SplitFromStack().Destroy(Silent: true);
			Helpers.VerifyLog("TEMPER", $"guild writ spent at {WarrenZoneID ?? "null"}");
			return "writ";
		}

		public static bool CanTemper(GameObject Item)
		{
			if (Item == null || !Item.IsValid() || Item.IsTemporary || !Item.Physics.IsReal)
				return false;
			if (Item.HasPart<Cleo_TerraFirma_ModTempered>() || Item.HasPart<NaturalEquipment>())
				return false;
			if (Item.HasProperty("RelicName") || Item.HasTagOrProperty("QuestItem"))
				return false;
			return Subtype(Item) != null;
		}

		public static string Subtype(GameObject Item)
		{
			if (Item.GetInventoryCategory() == "Cybernetic Implants")
				return null;
			if (Item.HasPart<Armor>())
				return "armor";
			if (Item.HasPart<Shield>())
				return "shield";
			if (Item.TryGetPart(out MissileWeapon mw))
			{
				string skill = mw.Skill;
				if (skill == "Rifle" || skill == "Bow" || skill == "Pistol")
					return "ranged";
			}
			if (Item.TryGetPart(out MeleeWeapon melee) && !melee.IsImprovisedWeapon())
			{
				string skill = melee.Skill;
				if (skill == "Axe" || skill == "ShortBlades" || skill == "LongBlades" || skill == "Cudgel")
					return "weapon";
			}
			return null;
		}

		public static int Score(GameObject Item, GameObject Owner)
		{
			int weapon = Brain.WeaponScore(Item, Owner);
			int missile = Brain.MissileWeaponScore(Item, Owner);
			double score = Item.GetTier() - 1 + Item.GetModificationCount();
			score += Math.Min(Brain.ArmorScore(Item, Owner).DiminishingReturns(0.5), 20);
			score += Math.Min(Brain.ShieldScore(Item, Owner).DiminishingReturns(1.0), 20);
			int kills = Item.GetIntProperty("KillsAsMeleeWeapon") - Item.GetIntProperty("AccidentalKillsAsMeleeWeapon") / 2
				+ Item.GetIntProperty("KillsAsThrownWeapon") - Item.GetIntProperty("AccidentalKillsAsThrownWeapon") / 2
				+ Item.GetIntProperty("KillsAsLauncher") - Item.GetIntProperty("AccidentalKillsAsLauncher") / 2
				+ Item.GetIntProperty("KillsAsProjectile") - Item.GetIntProperty("AccidentalKillsAsProjectile") / 2;
			if (kills > 0)
				score += Math.Min(kills.DiminishingReturns(1.0), 50);
			score += Math.Min(weapon.DiminishingReturns(1.0), 10);
			score += Math.Min(missile.DiminishingReturns(1.0), 10);
			int used = Item.GetIntProperty("InventoryActions") + Item.GetIntProperty("ActivatedAbilityCommandsProcessed") + Item.GetIntProperty("SifrahActions");
			if (used != 0)
				score += Math.Min(used.DiminishingReturns(1.0) / 4, 20);
			int bonus = Item.GetIntProperty("ItemNamingBonus");
			if (bonus != 0)
				score += Math.Min(bonus.DiminishingReturns(1.0), 10);
			if (Item.HasPart<MakersMark>())
				score += 1.0;
			else if (Item.TryGetPart(out Description d) && !string.IsNullOrEmpty(d.Mark))
				score += 1.0;
			if (score <= 0.0)
				score = 1.0;
			score = GetNamingChanceEvent.GetFor(Owner, Item, score);
			return Math.Max((int)Math.Round(score, MidpointRounding.AwayFromZero), 0);
		}

		public static List<GameObject> Candidates(GameObject Owner)
		{
			var result = new List<GameObject>();
			List<GameObject> equipped = Owner?.GetEquippedObjects();
			if (equipped == null)
				return result;
			var seen = new HashSet<GameObject>();
			var scored = new List<(GameObject Item, int Score, int Order)>();
			for (int i = 0; i < equipped.Count; i++)
			{
				GameObject item = equipped[i];
				if (!seen.Add(item) || !CanTemper(item))
					continue;
				scored.Add((item, Score(item, Owner), i));
			}
			scored.Sort((a, b) => a.Score != b.Score ? b.Score.CompareTo(a.Score) : a.Order.CompareTo(b.Order));
			for (int i = 0; i < scored.Count && i < OFFER_COUNT; i++)
				result.Add(scored[i].Item);
			return result;
		}

		public static bool GetRoll(GameObject Item, out string Kind, out int Amount)
		{
			Kind = null;
			Amount = 0;
			string stored = Item.GetStringProperty(ROLL_PROPERTY);
			if (!string.IsNullOrEmpty(stored))
			{
				int colon = stored.IndexOf(':');
				if (colon > 0 && int.TryParse(stored.Substring(colon + 1), out int amount))
				{
					Kind = stored.Substring(0, colon);
					Amount = amount;
					if (Fits(Item, Kind))
						return true;
				}
			}
			var pool = Pool(Item);
			if (pool.Count == 0)
				return false;
			var pick = pool.GetRandomElement();
			Kind = pick.Kind;
			Amount = pick.Amount;
			Item.SetStringProperty(ROLL_PROPERTY, Kind + ":" + Amount);
			return true;
		}

		public static List<(string Kind, int Amount)> Pool(GameObject Item)
		{
			var pool = new List<(string, int)>();
			switch (Subtype(Item))
			{
				case "weapon":
					pool.Add(("DieSize", 2));
					pool.Add(("Damage", 1));
					if (Fits(Item, "StrCap"))
						pool.Add(("StrCap", 3));
					pool.Add(("ToHit", 2));
					pool.Add(("Pen", 1));
					break;
				case "ranged":
					pool.Add(("MissilePen", 1));
					pool.Add(("MissileDie", 2));
					pool.Add(("MissileDamage", 1));
					pool.Add(("Accuracy", Item.GetPart<MissileWeapon>().WeaponAccuracy > 0 ? Stat.Random(5, 10) : Stat.Random(2, 6)));
					break;
				case "armor":
					pool.Add(("AV", 1));
					pool.Add(("DV", 1));
					pool.Add(("MA", 2));
					pool.Add(("Acid", 10));
					pool.Add(("Cold", 10));
					pool.Add(("Heat", 10));
					pool.Add(("Elec", 10));
					foreach (string attribute in Attributes)
						pool.Add((attribute, 1));
					pool.Add(("ArmorToHit", 2));
					if (Fits(Item, "SpeedPenalty"))
						pool.Add(("SpeedPenalty", Math.Min(Item.GetPart<Armor>().SpeedPenalty, Stat.Random(5, 10))));
					break;
				case "shield":
					pool.Add(("ShieldAV", 1));
					if (Fits(Item, "ShieldDV"))
						pool.Add(("ShieldDV", 1));
					if (Fits(Item, "ShieldSpeedPenalty"))
						pool.Add(("ShieldSpeedPenalty", Math.Min(Item.GetPart<Shield>().SpeedPenalty, Stat.Random(5, 10))));
					break;
			}
			return pool;
		}

		private static readonly string[] Attributes = { "Strength", "Agility", "Toughness", "Intelligence", "Willpower", "Ego" };

		public static bool Fits(GameObject Item, string Kind)
		{
			string subtype = Subtype(Item);
			switch (Kind)
			{
				case "DieSize": case "Damage": case "ToHit": case "Pen":
					return subtype == "weapon";
				case "StrCap":
					return subtype == "weapon" && Item.GetPart<MeleeWeapon>().MaxStrengthBonus != 999;
				case "MissilePen": case "MissileDie": case "MissileDamage": case "Accuracy":
					return subtype == "ranged";
				case "AV": case "DV": case "MA": case "Acid": case "Cold": case "Heat": case "Elec": case "ArmorToHit":
				case "Strength": case "Agility": case "Toughness": case "Intelligence": case "Willpower": case "Ego":
					return subtype == "armor";
				case "SpeedPenalty":
					return subtype == "armor" && Item.GetPart<Armor>().SpeedPenalty > 0;
				case "ShieldAV":
					return subtype == "shield";
				case "ShieldDV":
					return subtype == "shield" && Item.GetPart<Shield>().DV < 0;
				case "ShieldSpeedPenalty":
					return subtype == "shield" && Item.GetPart<Shield>().SpeedPenalty > 0;
			}
			return false;
		}

		public static string Describe(string Kind, int Amount)
		{
			switch (Kind)
			{
				case "DieSize": case "MissileDie": return $"+{Amount} damage die size";
				case "Damage": case "MissileDamage": return $"+{Amount} damage";
				case "StrCap": return $"+{Amount} strength bonus cap";
				case "ToHit": case "ArmorToHit": return $"+{Amount} to hit";
				case "Pen": case "MissilePen": return $"+{Amount} penetration";
				case "Accuracy": return $"+{Amount} accuracy";
				case "AV": case "ShieldAV": return $"+{Amount} AV";
				case "DV": case "ShieldDV": return $"+{Amount} DV";
				case "MA": return $"+{Amount} MA";
				case "Acid": return $"+{Amount} acid resistance";
				case "Cold": return $"+{Amount} cold resistance";
				case "Heat": return $"+{Amount} heat resistance";
				case "Elec": return $"+{Amount} electrical resistance";
				case "SpeedPenalty": case "ShieldSpeedPenalty": return $"{Amount} less speed penalty";
				default: return $"+{Amount} {Kind}";
			}
		}

		public static bool Apply(GameObject Item, string Kind, int Amount, GameObject Owner)
		{
			if (!CanTemper(Item) || !Fits(Item, Kind))
				return false;
			GameObject wearer = Item.Equipped;
			BodyPart slot = wearer?.FindEquippedObject(Item);
			if (wearer != null)
			{
				Event unequip = Event.New("CommandUnequipObject");
				unequip.SetParameter("BodyPart", slot);
				unequip.SetFlag("NoStack", State: true);
				if (!wearer.FireEvent(unequip) || Item.Equipped != null)
				{
					Helpers.VerifyLog("TEMPER", $"{Item.DebugName} would not come off, nothing changed");
					return false;
				}
			}
			try
			{
				ApplyStat(Item, Kind, Amount);
				Item.AddPart(new Cleo_TerraFirma_ModTempered(Kind, Amount));
				Item.AddPart(new Cleo_TerraFirma_TemperedLabel());
				Item.RemoveStringProperty(ROLL_PROPERTY);
				Item.SetImportant(flag: true, force: true);
				Helpers.VerifyLog("TEMPER", $"{Item.DebugName}: {Kind} {Amount}");
			}
			finally
			{
				if (wearer != null && slot != null && slot.Equipped == null && GameObject.Validate(ref Item))
					wearer.FireEvent(Event.New("CommandEquipObject", "Object", Item, "BodyPart", slot));
				if (Item.Equipped == null && Item.InInventory == null && Item.CurrentCell == null)
					(wearer ?? Owner)?.ReceiveObject(Item);
			}
			return true;
		}

		private static void ApplyStat(GameObject Item, string Kind, int Amount)
		{
			switch (Kind)
			{
				case "DieSize": Item.GetPart<MeleeWeapon>().AdjustDamageDieSize(Amount); break;
				case "Damage": Item.GetPart<MeleeWeapon>().AdjustDamage(Amount); break;
				case "StrCap": Item.GetPart<MeleeWeapon>().AdjustBonusCap(Amount); break;
				case "ToHit": Item.GetPart<MeleeWeapon>().HitBonus += Amount; break;
				case "Pen": Item.GetPart<MeleeWeapon>().PenBonus += Amount; break;
				case "MissilePen": Item.RequirePart<MissilePerformance>().PenetrationModifier += Amount; break;
				case "MissileDie": Item.RequirePart<MissilePerformance>().DamageDieModifier += Amount; break;
				case "MissileDamage": Item.RequirePart<MissilePerformance>().DamageModifier += Amount; break;
				case "Accuracy":
				{
					MissileWeapon mw = Item.GetPart<MissileWeapon>();
					if (mw.WeaponAccuracy > 0)
						mw.WeaponAccuracy = Math.Max(mw.WeaponAccuracy - Amount, 0);
					else
						mw.AimVarianceBonus += Amount;
					break;
				}
				case "AV": Item.GetPart<Armor>().AV += Amount; break;
				case "DV": Item.GetPart<Armor>().DV += Amount; break;
				case "MA": Item.GetPart<Armor>().MA += Amount; break;
				case "Acid": Item.GetPart<Armor>().Acid += Amount; break;
				case "Cold": Item.GetPart<Armor>().Cold += Amount; break;
				case "Heat": Item.GetPart<Armor>().Heat += Amount; break;
				case "Elec": Item.GetPart<Armor>().Elec += Amount; break;
				case "Strength": Item.GetPart<Armor>().Strength += Amount; break;
				case "Agility": Item.GetPart<Armor>().Agility += Amount; break;
				case "Toughness": Item.GetPart<Armor>().Toughness += Amount; break;
				case "Intelligence": Item.GetPart<Armor>().Intelligence += Amount; break;
				case "Willpower": Item.GetPart<Armor>().Willpower += Amount; break;
				case "Ego": Item.GetPart<Armor>().Ego += Amount; break;
				case "ArmorToHit": Item.GetPart<Armor>().ToHit += Amount; break;
				case "SpeedPenalty": { Armor a = Item.GetPart<Armor>(); a.SpeedPenalty = Math.Max(a.SpeedPenalty - Amount, 0); break; }
				case "ShieldAV": Item.GetPart<Shield>().AV += Amount; break;
				case "ShieldDV": Item.GetPart<Shield>().DV += Amount; break;
				case "ShieldSpeedPenalty": { Shield s = Item.GetPart<Shield>(); s.SpeedPenalty = Math.Max(s.SpeedPenalty - Amount, 0); break; }
			}
		}

		public static bool Offer(GameObject Player, GameObject Overseer, string WarrenZoneID)
		{
			if (Player == null || Available(Player, WarrenZoneID) <= 0)
				return false;
			List<GameObject> items = Candidates(Player);
			var rows = new List<string>();
			var kinds = new List<string>();
			var amounts = new List<int>();
			var offered = new List<GameObject>();
			foreach (GameObject item in items)
			{
				if (!GetRoll(item, out string kind, out int amount))
					continue;
				offered.Add(item);
				kinds.Add(kind);
				amounts.Add(amount);
				rows.Add(item.DisplayName + ": {{W|" + Describe(kind, amount) + "}}");
			}
			if (offered.Count == 0)
			{
				Popup.Show("You have nothing equipped that can be tempered.");
				return false;
			}
			rows.Add("Not yet.");
			var hotkeys = new List<char>();
			for (int i = 0; i < rows.Count; i++)
				hotkeys.Add((char)('a' + i));

			int choice = Popup.PickOption("", "What would you like tempered?", "", "Sounds/UI/ui_notification",
				rows, hotkeys, Context: Overseer, Spacing: 1, AllowEscape: true);
			if (choice < 0 || choice >= offered.Count)
				return false;

			GameObject picked = offered[choice];
			bool applied = false;
			try
			{
				applied = Apply(picked, kinds[choice], amounts[choice], Player);
			}
			finally
			{
				if (applied || picked.HasPart<Cleo_TerraFirma_ModTempered>())
					Helpers.VerifyLog("TEMPER", $"spent {SpendOne(Player, WarrenZoneID) ?? "nothing"} at {WarrenZoneID ?? "null"}, {Owed(Player, WarrenZoneID)} still owed there");
			}
			if (!applied)
			{
				Popup.Show(GameText.VariableReplace("You can't take off =subject.t= to have it tempered.", picked));
				return false;
			}
			Popup.Show(GameText.VariableReplace("=subject.T= =verb:take= on a new temper: ", picked) + Describe(kinds[choice], amounts[choice]) + ".");
			OfferName(picked, Player, Overseer);
			return true;
		}

		public static void OfferName(GameObject Item, GameObject Player, GameObject Overseer)
		{
			if (Item == null || Item.HasProperName || !ItemNaming.CanBeNamed(Item, Player))
				return;
			if (Popup.ShowYesNo("Would you like to name your tempered item?") != DialogResult.Yes)
				return;
			int lastLevel = Player.GetIntProperty("LastItemNamingDoneAtLevel");
			int atLevel = Player.GetIntProperty("ItemNamingDoneAtLevel");
			try
			{
				ItemNaming.NameItem(Item, Player, null, Overseer, null, NAMING_TYPE, CanBestow: false);
			}
			finally
			{
				Restore(Player, "LastItemNamingDoneAtLevel", lastLevel);
				Restore(Player, "ItemNamingDoneAtLevel", atLevel);
			}
		}

		private static void Restore(GameObject Player, string Name, int Value)
		{
			if (Value == 0)
				Player.RemoveIntProperty(Name);
			else
				Player.SetIntProperty(Name, Value);
		}
	}
}
