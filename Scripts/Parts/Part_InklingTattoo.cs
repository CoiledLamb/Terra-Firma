using System;
using System.Collections.Generic;
using XRL.Language;
using XRL.Messages;
using XRL.Rules;
using XRL.World.Anatomy;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_InklingTattoo : IPart
	{
		public GameObject Inkling;
		public int BodyPartID;
		public string TattooText;

		public string MealEvery = "300-600";

		public int EatChance = 50;

		public int TurnsToMeal = -1;

		public string RideFor = "200-500";

		public int TurnsToLeave = -1;

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<EndTurnEvent>.ID
				|| ID == TookDamageEvent.ID;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("BeforeDeathRemoval");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "BeforeDeathRemoval" && ParentObject != null)
				HopOffAll(ParentObject);
			return base.FireEvent(E);
		}

		public override bool HandleEvent(TookDamageEvent E)
		{
			if ((E.Damage == null || E.Damage.Amount > 0) && ParentObject != null)
				HopOffAll(ParentObject, KeepPets: true);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (!GameObject.Validate(ref Inkling))
			{
				RemoveOwnTattoo();
				ParentObject.RemovePart(this);
				return base.HandleEvent(E);
			}
			if (!HasOwnTattoo())
			{
				HopOff(Frightened: true, Silent: false);
				return base.HandleEvent(E);
			}
			bool pet = IsPet;
			if (TurnsToLeave < 0)
				TurnsToLeave = RideFor.RollCached();
			if (!pet && --TurnsToLeave <= 0)
			{
				if (!HopOff(Frightened: false, Silent: false))
					TurnsToLeave = 1;
				return base.HandleEvent(E);
			}
			if (TurnsToMeal < 0)
				TurnsToMeal = MealEvery.RollCached();
			if (--TurnsToMeal <= 0)
			{
				TurnsToMeal = -1;
				if (pet && !HasEdibleTattoo(ParentObject))
				{
					if (Inkling.GetPart<Cleo_TerraFirma_Inkling>()?.FindThreat(ParentObject.CurrentCell, ParentObject) != null
						|| !HopOff(Frightened: false, Silent: false))
						TurnsToMeal = PET_RETRY;
				}
				else if (!Feed())
					HopOff(Frightened: false, Silent: false);
			}
			return base.HandleEvent(E);
		}

		public const int PET_RETRY = 10;

		public bool IsPet => GameObject.Validate(ref Inkling) && Inkling.HasPart<Cleo_TerraFirma_InklingPet>()
			&& ParentObject != null && Inkling.PartyLeader == ParentObject;

		public static List<Cleo_TerraFirma_InklingTattoo> Riders(GameObject Host)
		{
			List<Cleo_TerraFirma_InklingTattoo> riders = new List<Cleo_TerraFirma_InklingTattoo>();
			if (Host != null)
			{
				foreach (IPart part in Host.PartsList)
				{
					if (part is Cleo_TerraFirma_InklingTattoo rider)
						riders.Add(rider);
				}
			}
			return riders;
		}

		static void HopOffAll(GameObject Host, bool KeepPets = false)
		{
			foreach (Cleo_TerraFirma_InklingTattoo rider in Riders(Host))
			{
				if (KeepPets && rider.IsPet)
					continue;
				rider.HopOff(Frightened: true, Silent: false);
			}
		}

		public static bool HasEdibleTattoo(GameObject Host)
		{
			Tattoos tattoos = Host?.GetPart<Tattoos>();
			if (tattoos == null)
				return false;
			int count = 0;
			foreach (List<string> list in tattoos.Descriptions.Values)
				count += list.Count;
			return count > Riders(Host).Count;
		}

		List<string> OwnList()
		{
			Tattoos tattoos = ParentObject.GetPart<Tattoos>();
			if (tattoos != null && tattoos.Descriptions.TryGetValue(BodyPartID, out List<string> list))
				return list;
			return null;
		}

		bool HasOwnTattoo() => OwnList()?.Contains(TattooText) ?? false;

		void RemoveOwnTattoo()
		{
			Tattoos tattoos = ParentObject.GetPart<Tattoos>();
			List<string> list = OwnList();
			if (list == null || !list.Remove(TattooText))
				return;
			if (list.Count == 0)
				tattoos.RemoveTattoo(BodyPartID);
			else
				ParentObject.CheckMarkOfDeath();
		}

		public bool Feed()
		{
			return GameObject.Validate(ref Inkling) && EatOne(Inkling, ParentObject, EatChance);
		}

		public static bool EatOne(GameObject Eater, GameObject Host, int EatChance)
		{
			Tattoos tattoos = Host.GetPart<Tattoos>();
			if (tattoos == null)
				return false;
			List<(int Part, string Text)> skip = new List<(int, string)>();
			foreach (Cleo_TerraFirma_InklingTattoo rider in Riders(Host))
				skip.Add((rider.BodyPartID, rider.TattooText));
			List<(int Part, int Index)> meals = new List<(int, int)>();
			foreach (KeyValuePair<int, List<string>> entry in tattoos.Descriptions)
			{
				for (int i = 0; i < entry.Value.Count; i++)
				{
					int s = skip.IndexOf((entry.Key, entry.Value[i]));
					if (s >= 0)
					{
						skip.RemoveAt(s);
						continue;
					}
					meals.Add((entry.Key, i));
				}
			}
			if (meals.Count == 0)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"hungry, but no other tattoos on {Host.Blueprint}");
				return false;
			}
			TakeInk(Eater, tattoos);
			(int part, int index) = meals.GetRandomElement();
			List<string> list = tattoos.Descriptions[part];
			string old = list[index];
			string partName = Host.Body?.GetPartByID(part)?.GetOrdinalName() ?? "skin";
			if (Stat.Random(1, 100) <= EatChance)
			{
				list.RemoveAt(index);
				if (list.Count == 0)
					tattoos.RemoveTattoo(part);
				else
					Host.CheckMarkOfDeath();
				if (Host.IsPlayer())
					MessageQueue.AddPlayerMessage($"{Eater.Does("eat")} the tattoo of {old} on your {partName}.");
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"ate tattoo '{old}' on {Host.Blueprint}'s {partName}");
			}
			else
			{
				list[index] = Grammar.Obfuscate(old, 10);
				Host.CheckMarkOfDeath();
				if (Host.IsPlayer())
					MessageQueue.AddPlayerMessage($"{Eater.Does("nibble")} at the tattoo of {old} on your {partName}, smearing it.");
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"garbled tattoo '{old}' -> '{list[index]}' on {Host.Blueprint}'s {partName}");
			}
			return true;
		}

		static void TakeInk(GameObject Eater, Tattoos HostTattoos)
		{
			string ink = HostTattoos.ColorString;
			int amp = ink?.IndexOf('&') ?? -1;
			if (amp < 0 || amp + 1 >= ink.Length || Eater.Render == null)
				return;
			string main = "&" + ink[amp + 1];
			Eater.Render.ColorString = main;
			if (!Eater.Render.TileColor.IsNullOrEmpty())
				Eater.Render.TileColor = main;
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"took ink {main} from {HostTattoos.ParentObject?.Blueprint}");
		}

		public bool HopOff(bool Frightened, bool Silent)
		{
			if (ParentObject == null || Inkling == null)
				return false;
			Cell here = ParentObject.CurrentCell;
			if (here == null || !GameObject.Validate(ref Inkling))
				return false;
			if (here.ParentZone == null || here.ParentZone.IsWorldMap())
				return false;
			List<Cell> open = new List<Cell>();
			foreach (Cell cell in here.GetLocalAdjacentCells())
			{
				if (cell.IsEmpty() && !cell.HasWall())
					open.Add(cell);
			}
			if (open.Count == 0)
				return false;
			RemoveOwnTattoo();
			GameObject inkling = Inkling;
			Inkling = null;
			GameObject host = ParentObject;
			host.RemovePart(this);
			inkling.GetPart<Cleo_TerraFirma_Inkling>()?.HopOff(open.GetRandomElement(), Frightened, host);
			if (!Silent && host.IsPlayer())
				MessageQueue.AddPlayerMessage($"{inkling.Does("peel")} off your skin.");
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"hopped off {host.Blueprint} (frightened={Frightened})");
			return true;
		}
	}
}
