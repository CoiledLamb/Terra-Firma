using System.Collections.Generic;
using XRL.Rules;
using XRL.World;
using XRL.World.ObjectBuilders;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// The stonecarver legendary builder: rolls the byname FIRST, then grants what the byname
	/// promises. Straight off the base game's own goatfolk pattern (decomp `GoatfolkHero1.cs`:
	/// MakeEpithet, then a switch on the name text that hands out mutations, stats and gear --
	/// Mamon Souldrinker drains life BECAUSE the generator named him Souldrinker).
	///
	/// RULED 2026-08-15 (name-drives-kit + the two-axis amendment): the byname is one compound
	/// word whose MATERIAL half grants a perk and whose CRAFT half grants the kit, so the name
	/// is the whole character sheet. The epithet bank IS the mutation pool -- a mutation is
	/// rollable if and only if a craft word vouches for it, and a word may not enter the bank
	/// until that mutation's NPC AI is committed.
	///
	/// WORD COUNT PER COLUMN = SPAWN WEIGHT. Fissure 3 : Stoneshaper 3 : Earthen Barrage 3 :
	/// Geomancy 2 : office 2. Re-weighting the roster is a vocabulary edit, nothing else.
	///
	/// STILL OPEN, deliberately not decided here:
	///   - the BASELINE KIT under the grant (what a carver carries before the byname tops it
	///     up). This builder adds only what the byname promises; the base blueprint is bare.
	///   - Gorgon's Gaze has no craft word yet, so no carver can roll it.
	///   - Calcify's word (limewright floated) waits on its NPC AI cost check, as does Geomancy's.
	///   - Every mutation LEVEL below is a Claude placeholder for the numbers sitting.
	/// </summary>
	public static class StonecarverEpithet
	{
		// ---- personal-name banks (ACCEPTED as-drafted 2026-08-14; five infix adds user-blessed
		// 2026-08-15 after the roller demo). Junction architecture: C-final prefixes, VCV infixes,
		// C-initial postfixes, so every join is collision-proof and no guard is needed.
		private static readonly string[] Prefixes = { "ar", "vel", "lar", "than", "ram", "sem", "cul", "tles", "pev", "ras", "mun", "spur" };
		private static readonly string[] Infixes = { "utha", "athi", "eni", "ena", "ari", "ithi", "aru", "eli", "anthi", "elthu", "arna", "uni", "avi" };
		private static readonly string[] Postfixes = { "tha", "thia", "na", "nia", "ce", "za", "til", "sa", "cna" };

		// ---- byname banks (materials FINAL 13 + crafts 13, RULED 2026-08-15)
		private static readonly string[] Materials = { "stone", "flint", "scree", "cairn", "grit", "chalk", "spall", "marl", "cobble", "salt", "karst", "quartz", "lode" };
		private static readonly string[] Crafts = { "smith", "knapper", "delver", "wright", "setter", "graver", "ward", "carter", "slinger", "augur", "reader", "reeve", "scribe" };

		public struct Roll
		{
			public string PersonalName;
			public string Material;
			public string Craft;
			public string Byname;
			public string PerkText;
			public string KitText;
			public string NamePieces;
		}

		private static string Pick(string[] bank) => bank[Stat.Random(0, bank.Length - 1)];

		private static string Capitalize(string s) => s.IsNullOrEmpty() ? s : char.ToUpper(s[0]) + s.Substring(1);

		/// <summary>Rolls a personal name from the etruscan-derived banks: prefix + 0-1 infix +
		/// postfix. The infix coin-flip is the length dial the tracker flagged (shipping
		/// InfixAmount 1 vs 0-2 is a taste call still owed).</summary>
		public static string RollPersonalName(out string pieces)
		{
			string prefix = Pick(Prefixes);
			string infix = Stat.Random(0, 1) == 1 ? Pick(Infixes) : "";
			string postfix = Pick(Postfixes);
			pieces = infix.IsNullOrEmpty() ? $"{prefix}+{postfix}" : $"{prefix}+{infix}+{postfix}";
			return Capitalize(prefix + infix + postfix);
		}

		/// <summary>Rolls the two-axis byname. Material and craft are drawn independently -- the
		/// perk axis and the kit axis do not constrain each other.</summary>
		public static Roll RollByname()
		{
			Roll roll = default(Roll);
			roll.Material = Pick(Materials);
			roll.Craft = Pick(Crafts);
			roll.Byname = Capitalize(roll.Material + roll.Craft);
			roll.PersonalName = RollPersonalName(out roll.NamePieces);
			roll.PerkText = DescribePerk(roll.Material);
			roll.KitText = DescribeKit(roll.Craft);
			return roll;
		}

		/// <summary>Material half -> the perk (the clan-effect axis). Magnitude discipline from
		/// the goatfolk precedent: single stats ~+2, combos +1/+1.</summary>
		public static string DescribePerk(string material)
		{
			switch (material)
			{
				case "stone": return "+1 Strength, +1 Toughness";
				case "flint": return "+2 Agility";
				case "scree": return "+1-2 AV (rolled)";
				case "cairn": return "+2 Ego";
				case "grit": return "+2 Willpower";
				case "chalk": return "+2 Intelligence";
				case "spall": return "+2 DV";
				case "marl": return "+HP";
				case "cobble": return "faster MoveSpeed";
				case "salt": return "heat resistance 50";
				case "karst": return "cold resistance 50";
				case "quartz": return "electrical resistance 50";
				case "lode": return "wealth (richer gear population)";
				default: return "(no perk mapped)";
			}
		}

		/// <summary>Craft half -> the kit. Office words (reeve/scribe) grant no mutation on
		/// purpose: kit words vs office words is the grammar of the bank.</summary>
		public static string DescribeKit(string craft)
		{
			switch (craft)
			{
				case "smith":
				case "knapper":
				case "delver": return "Fissure";
				case "wright":
				case "setter":
				case "graver": return "Stoneshaper";
				case "ward":
				case "carter":
				case "slinger": return "Earthen Barrage";
				case "augur":
				case "reader": return "Geomancy";
				case "reeve": return "office: +Ego, +Willpower";
				case "scribe": return "office: +Intelligence, +Willpower";
				default: return "(no kit mapped)";
			}
		}

		/// <summary>Applies a roll to a real creature: names it, hangs the byname as its epithet,
		/// pays the material perk, grants the craft kit. Naming precedes grants, same as the
		/// goatfolk builder -- there is no ordering race.</summary>
		public static void ApplyRoll(GameObject GO, Roll roll)
		{
			GO.GiveProperName(roll.PersonalName, Force: true);
			GO.RequirePart<Epithets>().AddEpithet(roll.Byname, -40);

			switch (roll.Material)
			{
				case "stone": GO.BoostStat("Strength", 1); GO.BoostStat("Toughness", 1); break;
				case "flint": GO.BoostStat("Agility", 2); break;
				case "scree": GO.BoostStat("AV", Stat.Random(1, 2)); break;
				case "cairn": GO.BoostStat("Ego", 2); break;
				case "grit": GO.BoostStat("Willpower", 2); break;
				case "chalk": GO.BoostStat("Intelligence", 2); break;
				case "spall": GO.BoostStat("DV", 2); break;
				case "marl": GO.BoostStat("Hitpoints", 10); break;
				// MoveSpeed is inverted (lower is faster); -0.75 is Clan Yr's own idiom
				case "cobble": GO.BoostStat("MoveSpeed", -0.75); break;
				case "salt": GO.BoostStat("HeatResistance", 50); break;
				case "karst": GO.BoostStat("ColdResistance", 50); break;
				case "quartz": GO.BoostStat("ElectricResistance", 50); break;
				case "lode": GO.SetIntProperty("Cleo_TerraFirma_LodeWealth", 1); break; // gear population rides the wiring pass
			}

			Mutations mutations = GO.RequirePart<Mutations>();
			switch (roll.Craft)
			{
				case "smith":
				case "knapper":
				case "delver":
					if (!mutations.HasMutation("Cleo_TerraFirma_Fissure"))
						mutations.AddMutation(new Cleo_TerraFirma_Fissure(), 4);
					break;
				case "wright":
				case "setter":
				case "graver":
					if (!mutations.HasMutation("Cleo_TerraFirma_Stoneshaper"))
						mutations.AddMutation(new Cleo_TerraFirma_Stoneshaper(), 4);
					break;
				case "ward":
				case "carter":
				case "slinger":
					if (!mutations.HasMutation("Cleo_TerraFirma_EarthenBarrage"))
						mutations.AddMutation(new Cleo_TerraFirma_EarthenBarrage(), 4);
					break;
				case "augur":
				case "reader":
					if (!mutations.HasMutation("Cleo_TerraFirma_Geomancy"))
						mutations.AddMutation(new Cleo_TerraFirma_Geomancy(), 4);
					break;
				case "reeve":
					GO.BoostStat("Ego", 2);
					GO.BoostStat("Willpower", 2);
					break;
				case "scribe":
					GO.BoostStat("Intelligence", 2);
					GO.BoostStat("Willpower", 2);
					break;
			}
		}

		/// <summary>Stamps the legendary layer the goatfolk way, NOT through `HeroMaker.MakeHero`.
		/// Deliberate: decomp shows MakeHero rolls its own honorific + epithet + title + extra
		/// title out of the engine's Hero naming tables, which would sit alongside our byname and
		/// bury the thing the bank exists to produce. `GoatfolkHero1` doesn't call it either --
		/// it stamps the pink name color and doubles HP itself, and the blueprint carries the
		/// rest. We do the same, plus the Hero property so the engine still reads it as legendary
		/// (map icon, rep hooks).</summary>
		public static void MakeLegendary(GameObject GO)
		{
			GO.SetIntProperty("Hero", 1);
			GO.SetStringProperty("Role", "Hero");
			GO.RequirePart<DisplayNameColor>().SetColorByPriority("M", 30);
			GO.MultiplyStat("Hitpoints", 2);
		}

		/// <summary>Roll and apply in one call, returning the roll so a caller can print it.</summary>
		public static Roll Build(GameObject GO, bool Legendary = true)
		{
			if (Legendary)
				MakeLegendary(GO);
			Roll roll = RollByname();
			ApplyRoll(GO, roll);
			return roll;
		}
	}

	/// <summary>XML-facing wrapper, so a blueprint can carry this the moment the roster lands
	/// (`&lt;builder Class="Cleo_TerraFirma_StonecarverLegendary" /&gt;`). Nothing references it
	/// yet -- the roster blueprints are still owed rulings.</summary>
	public class Cleo_TerraFirma_StonecarverLegendary : IObjectBuilder
	{
		public override void Apply(GameObject Object, string Context)
		{
			StonecarverEpithet.Build(Object);
		}
	}
}
