using System;
using Cleo.TerraFirma.Scripts;
using XRL.Messages;
using XRL.World.Effects;

namespace XRL.World.Parts.Mutation
{
	[Serializable]
	public class Cleo_TerraFirma_CapriciousDevotion : BaseMutation
	{
		public const int CAP = 5;

		public const int STEP_DAYS = 2;

		public long LastPrayed = -1;
		public long LastDesecrated = -1;

		public int ShownMental;
		public int ShownPhysical;

		public override bool CanLevel() => false;

		public override string GetDescription()
		{
			return "You have broken a covenant of the stone, and must now make amends for ancient trespasses.\n\n" +
				"Every " + STEP_DAYS + " days you go without praying, -1 to Mental Saves.\n" +
				"Every " + STEP_DAYS + " days you go without desecrating a statue, -1 to Physical Saves.\n" +
				"Maximum penalty: {{rules|-" + CAP + "}}\n" +
				"-100 reputation with the Quarriers' Guild.";
		}

		public override string GetLevelText(int Level) => "";

		public override bool Mutate(GameObject GO, int Level = 1)
		{
			long now = Calendar.TotalTimeTicks;
			if (LastPrayed < 0)
				LastPrayed = now;
			if (LastDesecrated < 0)
				LastDesecrated = now;
			return base.Mutate(GO, Level);
		}

		public override bool Unmutate(GameObject GO)
		{
			GO.RemoveEffect<Cleo_TerraFirma_UnatonedEffect>();
			return base.Unmutate(GO);
		}

		public static int Penalty(long Since)
		{
			if (Since < 0)
				return 0;
			long steps = (Calendar.TotalTimeTicks - Since) / ((long)STEP_DAYS * Calendar.TurnsPerDay);
			return (int)Math.Max(0, Math.Min(CAP, steps));
		}

		public static int DaysUntilNext(long Since)
		{
			if (Since < 0 || Penalty(Since) >= CAP)
				return -1;
			long step = (long)STEP_DAYS * Calendar.TurnsPerDay;
			long left = step - (Calendar.TotalTimeTicks - Since) % step;
			return (int)((left + Calendar.TurnsPerDay - 1) / Calendar.TurnsPerDay);
		}

		public int MentalPenalty => Penalty(LastPrayed);
		public int PhysicalPenalty => Penalty(LastDesecrated);

		public static bool IsMentalStat(string Stat) => Stat == "Willpower" || Stat == "Ego" || Stat == "Intelligence";
		public static bool IsPhysicalStat(string Stat) => Stat == "Strength" || Stat == "Agility" || Stat == "Toughness";

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("Prayed");
			Registrar.Register("Desecrated");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if ((E.ID == "Prayed" || E.ID == "Desecrated") && E.GetGameObjectParameter("Actor") == ParentObject)
			{
				if (E.ID == "Prayed")
					LastPrayed = Calendar.TotalTimeTicks;
				else
					LastDesecrated = Calendar.TotalTimeTicks;
				Helpers.VerifyLog("DEVOTION", $"{E.ID}: mental -{MentalPenalty}, physical -{PhysicalPenalty}");
				Sync();
			}
			return base.FireEvent(E);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == ModifyDefendingSaveEvent.ID || ID == SingletonEvent<EndTurnEvent>.ID;
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			Sync();
			return base.HandleEvent(E);
		}

		public void Sync()
		{
			int mental = MentalPenalty;
			int physical = PhysicalPenalty;
			if (ParentObject.IsPlayer())
			{
				if (mental > ShownMental)
					MessageQueue.AddPlayerMessage($"You've gone {mental * STEP_DAYS} days without praying. You suffer -{mental} to mental saves.", 'K');
				else if (mental == 0 && ShownMental > 0)
					MessageQueue.AddPlayerMessage("Your prayer is heard. Your mental saves are restored.", 'g');
				if (physical > ShownPhysical)
					MessageQueue.AddPlayerMessage($"You've gone {physical * STEP_DAYS} days without desecrating a statue. You suffer -{physical} to physical saves.", 'K');
				else if (physical == 0 && ShownPhysical > 0)
					MessageQueue.AddPlayerMessage("The desecration is done. Your physical saves are restored.", 'g');
			}
			ShownMental = mental;
			ShownPhysical = physical;
			bool penalized = mental > 0 || physical > 0;
			bool shown = ParentObject.HasEffect<Cleo_TerraFirma_UnatonedEffect>();
			if (penalized && !shown)
				ParentObject.ApplyEffect(new Cleo_TerraFirma_UnatonedEffect());
			else if (!penalized && shown)
				ParentObject.RemoveEffect<Cleo_TerraFirma_UnatonedEffect>();
		}

		public override bool HandleEvent(ModifyDefendingSaveEvent E)
		{
			int penalty = IsMentalStat(E.Stat) ? MentalPenalty : IsPhysicalStat(E.Stat) ? PhysicalPenalty : 0;
			if (penalty > 0)
				E.Roll -= penalty;
			return base.HandleEvent(E);
		}
	}
}
