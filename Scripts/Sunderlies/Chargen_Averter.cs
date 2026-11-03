using System;
using System.Collections.Generic;
using System.Linq;
using Cleo.TerraFirma.Scripts;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Skills;

namespace XRL.CharacterCreation
{
	public class Cleo_TerraFirma_AverterKit : ICustomChargenClass
	{
		public const string NO_DLC_SKILL = "LongBlades";
		public const string GLAIVE = "Cleo_TerraFirma_SilverGlaive";

		public static string SpearWeaponSkill(out SkillEntry skill)
		{
			skill = Cleo_TerraFirma_DlcSpear.FindSkill();
			if (skill == null)
				return null;
			string fromSpear = Cleo_TerraFirma_DlcSpear.FindSpear()?.GetPartParameter<string>("MeleeWeapon", "Skill");
			return string.IsNullOrEmpty(fromSpear) ? skill.Class : fromSpear;
		}

		public static bool Retarget(GameObject glaive, string weaponSkill)
		{
			if (glaive == null || glaive.Blueprint != GLAIVE || string.IsNullOrEmpty(weaponSkill))
				return false;
			MeleeWeapon melee = glaive.GetPart<MeleeWeapon>();
			if (melee != null)
				melee.Skill = weaponSkill;
			ThrownWeapon thrown = glaive.GetPart<ThrownWeapon>();
			if (thrown != null)
				thrown.Attributes = weaponSkill;
			return true;
		}

		public void BuildCharacterBody(GameObject body)
		{
			if (body == null)
				return;
			string weaponSkill = SpearWeaponSkill(out SkillEntry skill);
			if (weaponSkill == null)
			{
				Helpers.VerifyLog("AVERTER", "no-DLC kit (no spear skill)");
				return;
			}
			if (body.HasSkill(NO_DLC_SKILL))
				body.RemoveSkill(NO_DLC_SKILL);
			if (!body.HasSkill(skill.Class))
				body.AddSkill(skill.Class);
			int glaives = 0;
			var carried = (body.Inventory?.Objects ?? new List<GameObject>()).Concat(body.GetEquippedObjects() ?? new List<GameObject>()).ToList();
			foreach (GameObject o in carried)
				if (Retarget(o, weaponSkill))
					glaives++;
			Helpers.VerifyLog("AVERTER", $"DLC kit: -{NO_DLC_SKILL} +{skill.Class}, {glaives} glaive(s) -> {weaponSkill}");
		}

		public IEnumerable<string> GetChargenInfo()
		{
			Cleo.TerraFirma.Scripts.Cleo_TerraFirma_DlcSpearSubtypes.Stamp();
			yield break;
		}
	}
}
