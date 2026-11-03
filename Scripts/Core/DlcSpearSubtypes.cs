using XRL;
using XRL.World.Skills;
#if !VERSION_1_0
using XRL.CharacterCreation;
#endif

namespace Cleo.TerraFirma.Scripts
{
	[HasModSensitiveStaticCache]
	public static class Cleo_TerraFirma_DlcSpearSubtypes
	{
		static readonly (string subtype, string[] noDlc)[] Swaps =
		{
			("Wayfarer", XRL.CharacterCreation.Cleo_TerraFirma_WayfarerKit.NoDlcSkills),
			("Averter", new[] { XRL.CharacterCreation.Cleo_TerraFirma_AverterKit.NO_DLC_SKILL }),
		};

		[PreGameCacheInit]
		public static void Stamp()
		{
			SkillEntry skill = Cleo_TerraFirma_DlcSpear.FindSkill();
			if (skill == null)
				return;
			foreach (var (name, noDlc) in Swaps)
			{
				SubtypeEntry entry = SubtypeFactory.GetSubtypeEntry(name);
				if (entry == null)
					continue;
#if VERSION_1_0
				foreach (string c in noDlc)
					entry.Skills.Remove(c);
				if (!entry.Skills.Contains(skill.Class))
					entry.Skills.Add(skill.Class);
#else
				foreach (string c in noDlc)
					entry.Skills.RemoveByName(c);
				if (!entry.Skills.TryFindByName(skill.Class, out _))
					entry.Skills.Add(new SkillModifier { Name = skill.Class });
#endif
			}
		}
	}
}
