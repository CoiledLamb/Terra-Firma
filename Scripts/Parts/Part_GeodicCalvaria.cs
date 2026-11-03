using System;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_GeodicCalvaria : IPart
	{
		public int MentalArmor = 2;
		public int Glimmer = 20;
		public string Mutation = "Sense Psychic";
		public int MutationLevel = 1;

		public int AppliedMA;
		public int AppliedGlimmer;
		public Guid MutationTracker = Guid.Empty;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ImplantedEvent.ID
				|| ID == UnimplantedEvent.ID
				|| ID == SingletonEvent<BeginTakeActionEvent>.ID
				|| ID == PooledEvent<GetPsychicGlimmerEvent>.ID
				|| ID == GetShortDescriptionEvent.ID;
		}

		public int GetMentalArmor(GameObject who) => GetAvailableComputePowerEvent.AdjustUp(who, MentalArmor);

		public int GetGlimmer(GameObject who) => GetAvailableComputePowerEvent.AdjustUp(who, Glimmer);

		public override bool HandleEvent(ImplantedEvent E)
		{
			GameObject who = E.Implantee;
			MutationEntry entry = MutationFactory.GetMutationEntryByName(Mutation);
			if (entry != null && MutationTracker == Guid.Empty)
				MutationTracker = who.RequirePart<Mutations>().AddMutationMod(entry, MutationLevel, Mutations.MutationModifierTracker.SourceType.Equipment, ParentObject.DisplayNameOnlyStripped);
			who.RegisterEvent(this, SingletonEvent<BeginTakeActionEvent>.ID, Serialize: true);
			Refresh(who);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnimplantedEvent E)
		{
			GameObject who = E.Implantee;
			who.UnregisterEvent(this, SingletonEvent<BeginTakeActionEvent>.ID);
			SetMentalArmor(who, 0);
			if (MutationTracker != Guid.Empty)
			{
				who.GetPart<Mutations>()?.RemoveMutationMod(MutationTracker);
				MutationTracker = Guid.Empty;
			}
			AppliedGlimmer = 0;
			who.SyncMutationLevelAndGlimmer();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			GameObject who = ParentObject.Implantee;
			if (who != null)
				Refresh(who);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetPsychicGlimmerEvent E)
		{
			if (E.Subject != null && E.Subject == ParentObject.Implantee)
				E.Level += GetGlimmer(E.Subject);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			E.Postfix.AppendRules("Compute power on the local lattice increases this implant's effectiveness.");
			return base.HandleEvent(E);
		}

		private void SetMentalArmor(GameObject who, int amount)
		{
			Statistic ma = who.GetStat("MA");
			if (ma == null || amount == AppliedMA)
				return;
			ma.BaseValue += amount - AppliedMA;
			AppliedMA = amount;
		}

		public void Refresh(GameObject who)
		{
			SetMentalArmor(who, GetMentalArmor(who));
			int glimmer = GetGlimmer(who);
			if (glimmer != AppliedGlimmer)
			{
				AppliedGlimmer = glimmer;
				who.SyncMutationLevelAndGlimmer();
			}
		}
	}
}
