using System;
using System.Collections.Generic;
using XRL.Language;
using XRL.Messages;
using XRL.Rules;
using XRL.World.Anatomy;

namespace XRL.World.Parts
{
	/// <summary>
	/// An inkling riding on its host as a tattoo (see Cleo_TerraFirma_Inkling). The creature itself
	/// is held here, off the map: the plain GameObject field rides IComponent.Write's token
	/// serialization, the same WriteGameObject call base LocalCache uses to keep whole objects
	/// out of the zone, so it saves and loads with everything it had.
	///
	/// Its tattoo is one entry in the host's base Tattoos part; only that entry is ever removed,
	/// never the whole body part's list. It hops off when the host is hurt or dies, or when its
	/// tattoo is gone, or on its own after RideFor turns or when there is nothing left to eat. While riding it gets hungry and eats or garbles the host's other tattoos
	/// (the Mark of Death included, user 2026-10-17: "no need for a guard there").
	///
	/// Hunger timing is a Codex PROPOSAL dial. Message lines approved by the user 2026-10-18.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_InklingTattoo : IPart
	{
		public GameObject Inkling;
		public int BodyPartID;
		public string TattooText;

		/// <summary>Turns between meals, rolled fresh after each. DIAL.</summary>
		public string MealEvery = "300-600";

		/// <summary>Chance in 100 a meal eats the tattoo outright; otherwise it garbles it. DIAL.</summary>
		public int EatChance = 50;

		public int TurnsToMeal = -1;

		/// <summary>Turns it rides before wandering off on its own (user 2026-10-17: it never left). DIAL.</summary>
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
			if (E.ID == "BeforeDeathRemoval")
				HopOff(Frightened: true, Silent: false);
			return base.FireEvent(E);
		}

		public override bool HandleEvent(TookDamageEvent E)
		{
			if (E.Damage == null || E.Damage.Amount > 0)
				HopOff(Frightened: true, Silent: false);
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
				// someone took the tattoo off (another inkling's meal, a tattoo gun): it falls off too
				HopOff(Frightened: true, Silent: false);
				return base.HandleEvent(E);
			}
			if (TurnsToLeave < 0)
				TurnsToLeave = RideFor.RollCached();
			if (--TurnsToLeave <= 0)
			{
				if (!HopOff(Frightened: false, Silent: false))
					TurnsToLeave = 1; // boxed in: try again next turn
				return base.HandleEvent(E);
			}
			if (TurnsToMeal < 0)
				TurnsToMeal = MealEvery.RollCached();
			if (--TurnsToMeal <= 0)
			{
				TurnsToMeal = -1;
				// nothing left to eat on this host: it goes looking elsewhere
				if (!Feed())
					HopOff(Frightened: false, Silent: false);
			}
			return base.HandleEvent(E);
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
				tattoos.RemoveTattoo(BodyPartID); // also clears colors and rechecks the Mark of Death
			else
				ParentObject.CheckMarkOfDeath();
		}

		/// <summary>Eat or garble one of the host's other tattoos. True if it found one.</summary>
		public bool Feed()
		{
			return GameObject.Validate(ref Inkling) && EatOne(Inkling, ParentObject, BodyPartID, TattooText, EatChance);
		}

		/// <summary>Eater eats or garbles one of Host's tattoos, skipping one entry (a rider's own
		/// tattoo; pass null to skip none). Shared by riding and wild inklings. True if it ate.</summary>
		public static bool EatOne(GameObject Eater, GameObject Host, int SkipPart, string SkipText, int EatChance)
		{
			Tattoos tattoos = Host.GetPart<Tattoos>();
			if (tattoos == null)
				return false;
			List<(int Part, int Index)> meals = new List<(int, int)>();
			foreach (KeyValuePair<int, List<string>> entry in tattoos.Descriptions)
			{
				bool skippedSelf = SkipText == null;
				for (int i = 0; i < entry.Value.Count; i++)
				{
					if (!skippedSelf && entry.Key == SkipPart && entry.Value[i] == SkipText)
					{
						skippedSelf = true;
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
			// base Tattoos keeps one ink color per creature, not per tattoo: the inkling takes its
			// main color (user 2026-10-18: sticks, main only); the host keeps its tint until its
			// last tattoo goes, which base RemoveTattoo already handles
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

		/// <summary>Put the inkling back on the map beside the host and drop its tattoo.</summary>
		public bool HopOff(bool Frightened, bool Silent)
		{
			Cell here = ParentObject.CurrentCell;
			if (here == null || !GameObject.Validate(ref Inkling))
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
			ParentObject.RemovePart(this);
			inkling.GetPart<Cleo_TerraFirma_Inkling>()?.HopOff(open.GetRandomElement(), Frightened);
			if (!Silent && ParentObject.IsPlayer())
				MessageQueue.AddPlayerMessage($"{inkling.Does("peel")} off your skin.");
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"hopped off {ParentObject.Blueprint} (frightened={Frightened})");
			return true;
		}
	}
}
