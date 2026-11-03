using System;
using Cleo.TerraFirma.Scripts;
using XRL.World.AI;

namespace XRL.World.Parts.Mutation
{
	[Serializable]
	public class Cleo_TerraFirma_CovetousPsyche : BaseMutation
	{
		public const int VALUE_PER_GLIMMER = 20;

		public int LastBonus;

		public override bool CanLevel() => false;

		public override string GetDescription()
		{
			return "You have broken a covenant of the stone, and dipped the fruits of the earth in the psychic sea.\n\n" +
				"+1 psychic glimmer per " + VALUE_PER_GLIMMER + " value of trade goods, gems and figurines you carry.\n" +
				"Psychic hunters gain an additional random mental mutation.\n" +
				"-100 reputation with the Quarriers' Guild.";
		}

		public override string GetLevelText(int Level) => "";

		public static bool IsCoveted(GameObject Object)
		{
			if (Object.HasPart<RandomFigurine>())
				return true;
			GameObjectBlueprint bp = Object.GetBlueprint(false);
			return bp != null && bp.InheritsFrom("BaseCurrency");
		}

		public static int CarriedValue(GameObject Who)
		{
			double total = 0;
			Who?.Inventory?.ForeachObject(o =>
			{
				if (IsCoveted(o))
					total += (o.GetPart<Commerce>()?.Value ?? 0) * Math.Max(1, o.Count);
			});
			return (int)total;
		}

		public static int GlimmerFor(GameObject Who) => CarriedValue(Who) / VALUE_PER_GLIMMER;

		public static int HunterRankFor(GameObject Bearer) => (Bearer?.Level ?? 1) / 2 + 1;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == PooledEvent<GetPsychicGlimmerEvent>.ID
				|| ID == PooledEvent<AfterAddOpinionEvent>.ID
				|| ID == SingletonEvent<EndTurnEvent>.ID;
		}

		public override bool HandleEvent(GetPsychicGlimmerEvent E)
		{
			if (E.Subject == ParentObject)
				E.Level += GlimmerFor(ParentObject);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (ParentObject.IsPlayer())
			{
				int bonus = GlimmerFor(ParentObject);
				if (bonus != LastBonus)
				{
					LastBonus = bonus;
					GlimmerChangeEvent.Send(ParentObject);
				}
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(AfterAddOpinionEvent E)
		{
			if (E.Subject == ParentObject && E.Opinion is OpinionPsychicHunt && !E.Renew && E.Actor != null && E.Actor != ParentObject)
			{
				Cleo_TerraFirma_CovetousHunter part = E.Actor.RequirePart<Cleo_TerraFirma_CovetousHunter>();
				part.Rank = HunterRankFor(ParentObject);
				Helpers.VerifyLog("COVETOUS", $"{E.Actor.Blueprint} hunts {ParentObject.Blueprint}: extra mental mutation at rank {part.Rank} pending");
			}
			return base.HandleEvent(E);
		}
	}
}
