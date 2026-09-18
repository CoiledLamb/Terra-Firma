using System;
using System.Collections.Generic;
using System.Globalization;
using XRL.Rules;
using XRL.World;
using XRL.World.ObjectBuilders;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace Cleo.TerraFirma.Scripts
{
	// Codex PROPOSAL: existing vocabulary and grants share XML definitions. Descriptions
	// are derived from those grants, rather than maintained in a second set of switches.
	// Engine precedents: Data.xml DataBucket; HeroMaker.ResolveTemplateTag;
	// GameObject.AddBaseStat changes BaseValue directly (BoostStat is proportional).
	public static class StonecarverEpithet
	{
		private const string NAME_STYLE = "Cleo_TerraFirma_Stonecarver";
		private const string MATERIAL_BANK = "Cleo_TerraFirma_NamingMaterials";
		private const string CRAFT_BANK = "Cleo_TerraFirma_NamingCrafts";

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

		private sealed class StatGrant
		{
			public string Stat;
			public int Minimum;
			public int Maximum;

			private static string Signed(int value) =>
				(value >= 0 ? "+" : "") + value.ToString(CultureInfo.InvariantCulture);

			public string Describe()
			{
				string amount = Signed(Minimum);
				if (Maximum != Minimum) amount += " to " + Signed(Maximum);
				return amount + " base " + Stat + (Maximum != Minimum ? " (rolled)" : "");
			}
		}

		private sealed class Definition
		{
			public readonly List<StatGrant> Stats = new List<StatGrant>();
			public BaseMutation Mutation;
			public int Level;

			public string Describe()
			{
				var pieces = new List<string>();
				foreach (var grant in Stats) pieces.Add(grant.Describe());
				if (Mutation != null) pieces.Add(Mutation.GetDisplayName() + " (level " + Level + ")");
				return string.Join(", ", pieces);
			}

			public void Apply(GameObject target)
			{
				foreach (var grant in Stats)
					target.AddBaseStat(grant.Stat, Stat.Random(grant.Minimum, grant.Maximum));
				if (Mutation != null)
				{
					var mutations = target.RequirePart<Mutations>();
					if (!mutations.HasMutation(Mutation.GetType().Name)) mutations.AddMutation(Mutation, Level);
				}
			}
		}

		private static Dictionary<string, Dictionary<string, string>> Bank(string id)
		{
			var bank = GameObjectFactory.Factory.GetBlueprint(id)?.xTags;
			if (bank == null || bank.Count == 0) throw new InvalidOperationException("Missing naming bank: " + id);
			return bank;
		}

		private static string Pick(string id)
		{
			var words = new List<string>(Bank(id).Keys);
			return words[Stat.Random(0, words.Count - 1)];
		}

		private static Definition Read(string bank, string word)
		{
			if (word == null || !Bank(bank).TryGetValue(word, out var fields))
				throw new InvalidOperationException("Unknown naming word: " + bank + "/" + word);
			var definition = new Definition();
			foreach (var field in fields)
			{
				switch (field.Key)
				{
					case "Mutation":
						switch (field.Value)
						{
							case "Cleo_TerraFirma_Fissure": definition.Mutation = new Cleo_TerraFirma_Fissure(); break;
							case "Cleo_TerraFirma_Stoneshaper": definition.Mutation = new Cleo_TerraFirma_Stoneshaper(); break;
							case "Cleo_TerraFirma_EarthenBarrage": definition.Mutation = new Cleo_TerraFirma_EarthenBarrage(); break;
							case "Cleo_TerraFirma_Geomancy": definition.Mutation = new Cleo_TerraFirma_Geomancy(); break;
							default: throw new InvalidOperationException("Unsupported naming mutation: " + field.Value);
						}
						break;
					case "Level": definition.Level = int.Parse(field.Value, CultureInfo.InvariantCulture); break;
					case "Strength": case "Toughness": case "Agility": case "Ego":
					case "Willpower": case "Intelligence": case "AV": case "DV":
					case "Hitpoints": case "MoveSpeed": case "HeatResistance":
					case "ColdResistance": case "ElectricResistance": case "AcidResistance":
						var range = field.Value.Split(new[] { ".." }, StringSplitOptions.None);
						if (range.Length > 2) throw new InvalidOperationException("Invalid naming range: " + field.Value);
						int min = int.Parse(range[0], CultureInfo.InvariantCulture);
						int max = int.Parse(range[range.Length - 1], CultureInfo.InvariantCulture);
						if (max < min) throw new InvalidOperationException("Reversed naming range: " + field.Value);
						definition.Stats.Add(new StatGrant { Stat = field.Key, Minimum = min, Maximum = max });
						break;
					default: throw new InvalidOperationException("Unknown naming field: " + field.Key);
				}
			}
			if ((definition.Mutation != null && definition.Level <= 0) || (definition.Mutation == null && definition.Level != 0))
				throw new InvalidOperationException("Naming mutation/level mismatch: " + word);
			if (definition.Stats.Count == 0 && definition.Mutation == null)
				throw new InvalidOperationException("Empty naming definition: " + word);
			return definition;
		}

		public static string RollPersonalName(out string pieces)
		{
			pieces = "Naming.xml:" + NAME_STYLE;
			string name = XRL.Names.NameMaker.MakeName(Special: NAME_STYLE, FailureOkay: true);
			if (name.IsNullOrEmpty())
			{
				MetricsManager.LogError("TerraFirma: namestyle '" + NAME_STYLE + "' rolled no name (check XML/Naming.xml); placeholder used");
				return "Nameless";
			}
			return name;
		}

		public static Roll RollByname()
		{
			Roll roll = default(Roll);
			roll.Material = Pick(MATERIAL_BANK);
			roll.Craft = Pick(CRAFT_BANK);
			roll.Byname = char.ToUpperInvariant(roll.Material[0]) + roll.Material.Substring(1) + roll.Craft;
			roll.PersonalName = RollPersonalName(out roll.NamePieces);
			roll.PerkText = DescribePerk(roll.Material);
			roll.KitText = DescribeKit(roll.Craft);
			return roll;
		}

		public static string DescribePerk(string material) => Read(MATERIAL_BANK, material).Describe();
		public static string DescribeKit(string craft) => Read(CRAFT_BANK, craft).Describe();

		public static void ApplyRoll(GameObject GO, Roll roll)
		{
			// Validate both definitions before changing the creature.
			var material = Read(MATERIAL_BANK, roll.Material);
			var craft = Read(CRAFT_BANK, roll.Craft);
			GO.GiveProperName(roll.PersonalName, Force: true);
			GO.RequirePart<Epithets>().AddEpithet(roll.Byname, -40);
			material.Apply(GO);
			craft.Apply(GO);
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
			// The PINK BODY, not just the pink name (user 2026-08-15, seeing the first pack: "i
			// expected the legendaries to be pink and dark grey"). It is the ruled overseer color,
			// and it is free: HeroMaker resolves HeroColorString/HeroTileColor to "&M" by default,
			// so a legendary carver costs zero bespoke art. We paint it here because we skip
			// MakeHero on purpose (it would roll its own honorific/epithet/title over our byname),
			// and the tile color rode along with the machinery we skipped.
			// DetailColor is left alone: the blueprint's dark grey IS the "and dark grey" half.
			// Worth knowing before dialling this: ~70% of stonecarver.png draws in the DETAIL
			// channel, so this reads as a dark grey body with pink trim. Putting "M" in
			// DetailColor instead would make the pink dominant.
			if (GO.Render != null)
			{
				GO.Render.ColorString = "&M";
				GO.Render.TileColor = "&M";
			}
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

}

namespace XRL.World.ObjectBuilders
{
	/// <summary>XML-facing wrapper: the goatfolk-hero idiom (GoatfolkHero1 precedent), carried
	/// by the Cleo_TerraFirma_Overseer blueprint as `&lt;builder Name="Cleo_TerraFirma_
	/// StonecarverLegendary" /&gt;`. MOVED into this namespace 2026-08-26: it was declared in
	/// the mod namespace, and ModManager.ResolveType concatenates "XRL.World.ObjectBuilders."
	/// plus the name with NO fallback search, so the old placement could never have resolved
	/// from XML; the original "a blueprint can carry this" comment was an untested claim,
	/// falsified at the overseer-as-blueprint recon. Stateless on purpose: builders are
	/// cached singletons.</summary>
	public class Cleo_TerraFirma_StonecarverLegendary : IObjectBuilder
	{
		public override void Apply(XRL.World.GameObject Object, string Context)
		{
			Cleo.TerraFirma.Scripts.StonecarverEpithet.Build(Object);
		}
	}
}
