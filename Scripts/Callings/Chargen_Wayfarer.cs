using System;
using System.Collections.Generic;
using System.Linq;
using Cleo.TerraFirma.Scripts;
using XRL.World;
using XRL.World.Skills;

namespace XRL.CharacterCreation
{
	public class Cleo_TerraFirma_WayfarerKit : ICustomChargenClass
	{
		public static readonly string[] NoDlcSkills = { "ShortBlades", "Survival_DesertCanyonSurvival" };

		public static readonly string[] NoDlcGear = { "Short Bow", "Wooden Arrow" };

		public void BuildCharacterBody(GameObject body)
		{
			if (body == null)
				return;
			if (!Cleo_TerraFirma_DlcSpear.Present(out SkillEntry skill, out GameObjectBlueprint spear))
			{
				Helpers.VerifyLog("WAYFARER", $"no-DLC kit (spear skill {skill?.Class ?? "none"}, spear {spear?.Name ?? "none"})");
				return;
			}
			foreach (string c in NoDlcSkills)
				if (body.HasSkill(c))
					body.RemoveSkill(c);
			if (!body.HasSkill(skill.Class))
				body.AddSkill(skill.Class);
			var carried = (body.Inventory?.Objects ?? new List<GameObject>()).Concat(body.GetEquippedObjects() ?? new List<GameObject>()).ToList();
			foreach (GameObject o in carried)
				if (Array.IndexOf(NoDlcGear, o.Blueprint) >= 0)
					o.Obliterate();
			GameObject item = GameObject.Create(spear.Name);
			if (item != null)
			{
				item.Seen();
				item.MakeUnderstood();
				body.ReceiveObject(item);
			}
			Helpers.VerifyLog("WAYFARER", $"DLC kit: -{string.Join(" -", NoDlcSkills)} +{skill.Class}, +{spear.Name}, -{string.Join(" -", NoDlcGear)}");
		}

		public IEnumerable<string> GetChargenInfo()
		{
			Cleo.TerraFirma.Scripts.Cleo_TerraFirma_DlcSpearSubtypes.Stamp();
			yield break;
		}
	}
}
