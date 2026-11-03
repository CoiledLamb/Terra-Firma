using System;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_InklingPet : IPart
	{
		public string MealEvery = "300-600";

		public int TurnsToMeal = -1;

		public bool Boarded;

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == SingletonEvent<BeginTakeActionEvent>.ID;
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			GameObject leader = ParentObject.PartyLeader;
			Cleo_TerraFirma_Inkling inkling = ParentObject.GetPart<Cleo_TerraFirma_Inkling>();
			if (leader == null || inkling == null || ParentObject.IsPlayer() || ParentObject.CurrentCell == null)
				return base.HandleEvent(E);

			if (inkling.Hidden && inkling.Emerge())
				return false;

			bool beside = leader.CurrentZone == ParentObject.CurrentZone && ParentObject.DistanceTo(leader) <= 1;
			if (!Boarded)
			{
				Boarded = true;
				if (beside && inkling.RideOn(leader))
					return false;
			}

			bool threat = inkling.FindThreat(Victim: ParentObject) != null;
			if (threat || Cleo_TerraFirma_InklingTattoo.HasEdibleTattoo(leader))
			{
				if (beside && inkling.RideOn(leader))
					return false;
				if (leader.CurrentZone == ParentObject.CurrentZone)
					inkling.WalkTo(leader);
				return base.HandleEvent(E);
			}

			if (TurnsToMeal < 0)
				TurnsToMeal = MealEvery.RollCached();
			if (TurnsToMeal > 0)
				TurnsToMeal--;
			if (TurnsToMeal == 0)
			{
				GameObject ink = Cleo_TerraFirma_Inkling.FindInk(ParentObject, inkling.HungerRadius);
				if (ink != null)
				{
					if (ParentObject.DistanceTo(ink) <= 1)
					{
						inkling.DrinkInk(ink);
						TurnsToMeal = -1;
						ParentObject.UseEnergy(1000, "Pass");
						return false;
					}
					inkling.WalkTo(ink);
				}
			}
			return base.HandleEvent(E);
		}
	}
}
