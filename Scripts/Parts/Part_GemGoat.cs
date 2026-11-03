using System;
using XRL.Language;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_GemGoat : IPart
	{
		public int Chance = 1;

		public string Gems = "Agate,Topaz,Jasper,Amethyst,Sapphire,Emerald,Peridot";

		public string TablePrefix = "Cleo_TerraFirma_GemGoat_";

		public string CorpsePrefix = "Cleo_TerraFirma_GemGoatCorpse_";

		public int CorpseChance = 80;

		public string HornBlueprints = "Goat_Horns";

		public int HornChancePerThousand = 1;

		public string GemDescription;

		public string ForceGem;

		public bool Rolled;

		public string Gem;

		public string GemBlueprint => Gem == null ? null : "Rough " + Gem;

		public string Table => Gem == null ? null : TablePrefix + Gem;

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == ObjectCreatedEvent.ID || (ID == BeforeMeleeAttackEvent.ID && Gem != null);
		}

		public override bool HandleEvent(ObjectCreatedEvent E)
		{
			if (!Rolled && E.Context != "Sample")
				Roll();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeMeleeAttackEvent E)
		{
			if (E.Actor == ParentObject && E.Weapon != null && IsHorn(E.Weapon))
				EnsureTransmute(E.Weapon);
			return base.HandleEvent(E);
		}

		public static bool RollsGem(int chance) => chance.in100();

		public bool Roll()
		{
			if (Rolled)
				return false;
			Rolled = true;
			string[] gems = Gems.Split(',');
			string gem = !ForceGem.IsNullOrEmpty() ? ForceGem : RollsGem(Chance) ? gems.GetRandomElement() : null;
			if (gem == null)
				return false;
			Become(gem.Trim());
			return true;
		}

		private void Become(string gem)
		{
			Gem = gem;
			GameObjectBlueprint model = GameObjectFactory.Factory.GetBlueprintIfExists(GemBlueprint);
			if (model == null)
			{
				MetricsManager.LogError($"Cleo_TerraFirma_GemGoat: no blueprint {GemBlueprint}");
				Gem = null;
				return;
			}
			Render render = ParentObject.Render;
			render.DisplayName = GemWord(model) + " " + render.DisplayName;
			string color = model.GetPartParameter<string>("Render", "ColorString");
			if (!color.IsNullOrEmpty())
				render.ColorString = color;
			string tile = model.GetPartParameter<string>("Render", "TileColor");
			render.TileColor = tile.IsNullOrEmpty() ? color : tile;
			string detail = model.GetPartParameter<string>("Render", "DetailColor");
			if (!detail.IsNullOrEmpty())
				render.DetailColor = detail;
			if (!GemDescription.IsNullOrEmpty() && ParentObject.TryGetPart(out Description description))
				description._Short = GemDescription;
			if (ParentObject.TryGetPart(out Corpse corpse))
			{
				corpse.CorpseChance = CorpseChance;
				corpse.CorpseBlueprint = CorpsePrefix + Gem;
			}
			ApplyHorns();
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GEMGOAT", $"{ParentObject.Blueprint} became a {Gem} goat ({render.DisplayName})");
		}

		public static string GemWord(GameObjectBlueprint model)
		{
			string name = model.GetPartParameter<string>("Render", "DisplayName") ?? "";
			if (name.StartsWith("rough "))
				name = name.Substring(6);
			if (name.EndsWith(" gemstone"))
				name = name.Substring(0, name.Length - 9);
			return name;
		}

		public bool IsHorn(GameObject weapon) => weapon.HasPart<MeleeWeapon>() && HornBlueprints.CachedCommaExpansion().Contains(weapon.Blueprint);

		public int ApplyHorns()
		{
			int n = 0;
			foreach (GameObject o in ParentObject.GetEquippedObjects())
				if (IsHorn(o) && EnsureTransmute(o))
					n++;
			if (ParentObject.Inventory != null)
				foreach (GameObject o in ParentObject.Inventory.GetObjects())
					if (IsHorn(o) && EnsureTransmute(o))
						n++;
			return n;
		}

		public bool EnsureTransmute(GameObject weapon)
		{
			if (Gem == null)
				return false;
			ModTransmuteOnHit mod = weapon.GetPart<ModTransmuteOnHit>();
			if (mod != null && mod.Table == Table)
				return true;
			if (mod != null)
				weapon.RemovePart(mod);
			weapon.AddPart(new ModTransmuteOnHit(HornChancePerThousand, Table) { DescriptionTerm = Grammar.A(Gem.ToLowerInvariant()) });
			return true;
		}
	}
}
