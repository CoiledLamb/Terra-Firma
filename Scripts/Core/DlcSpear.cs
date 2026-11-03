using System;
using XRL.World;
using XRL.World.Skills;

namespace Cleo.TerraFirma.Scripts
{
	public static class Cleo_TerraFirma_DlcSpear
	{
		public static readonly string[] SkillNames = { "Polearm", "Polearms", "Spear", "Spears" };

		public const string SPEAR_NAME = "bronze spear";

		public static SkillEntry FindSkill()
		{
			SkillFactory factory = SkillFactory.Factory;
			if (factory?.SkillList == null)
				return null;
			foreach (string name in SkillNames)
			{
				foreach (SkillEntry entry in factory.SkillList.Values)
				{
					if (entry == null || string.IsNullOrEmpty(entry.Class))
						continue;
					if (string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
						return entry;
#if !VERSION_1_0
					string shown = ConsoleLib.Console.ColorUtility.StripFormatting(entry.GetDisplayName() ?? "");
					if (string.Equals(shown, name, StringComparison.OrdinalIgnoreCase))
						return entry;
#endif
				}
			}
			return null;
		}

		public static GameObjectBlueprint FindSpear()
		{
			foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
			{
				if (bp == null || bp.IsBaseBlueprint() || !bp.HasPart("MeleeWeapon"))
					continue;
				if (bp.CachedDisplayNameStrippedLC == SPEAR_NAME)
					return bp;
			}
			return null;
		}

		public static bool Present(out SkillEntry Skill, out GameObjectBlueprint Spear)
		{
			Skill = FindSkill();
			Spear = FindSpear();
			return Skill != null && Spear != null;
		}
	}
}
