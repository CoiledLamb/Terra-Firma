using System;
using System.Collections.Generic;
using XRL.Language;
using XRL.Rules;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_LapidaryClaws : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_CommandLapidaryClaws";
		public const string DERIVATION_CONTEXT = "Facet";

		public string commandId = "";
		public Guid ActivatedAbilityID = Guid.Empty;

		public int Chance = 5;
		public string ConversionTag = "RockTumblerOutput";
		public float GiganticFactor = 3f;
		public string Verb = "facet";
		public string Preposition = "into";

		[NonSerialized] public string CarveSound = "Sounds/Interact/sfx_interact_engraver";
		[NonSerialized] public float CarveSoundVolume = 0.5f;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool WantTurnTick()
		{
			return true;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ImplantedEvent.ID
				|| ID == UnimplantedEvent.ID
				|| ID == PooledEvent<CommandEvent>.ID;
		}

		public override bool HandleEvent(ImplantedEvent E)
		{
			ActivatedAbilityID = E.Implantee.AddDynamicCommand(out commandId, COMMAND, "Carve Figurine", "Cybernetics", null, "*");
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnimplantedEvent E)
		{
			E.Implantee.RemoveActivatedAbility(ref ActivatedAbilityID);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command == commandId && !commandId.IsNullOrEmpty() && E.Actor != null && E.Actor == ParentObject.Implantee && E.Actor.IsPlayer())
				CarveFigurine(E.Actor);
			return base.HandleEvent(E);
		}

		public static GameObject FindImplanted(GameObject Actor)
		{
			if (Actor?.Body == null)
				return null;
			foreach (GameObject implant in Actor.Body.GetInstalledCyberneticsReadonly() ?? new System.Collections.Generic.List<GameObject>())
			{
				if (implant.HasPart<Cleo_TerraFirma_LapidaryClaws>())
					return implant;
			}
			return null;
		}

		public static List<GameObject> GetInputs(GameObject Actor) =>
			Actor.GetInventory(o => o.HasTag(Cleo_TerraFirma_LatheCarving.INPUT_TAG) || Cleo_TerraFirma_SimulacrumLoom.IsLoomStone(o));

		public bool CarveFigurine(GameObject Actor)
		{
			if (!Actor.CanMoveExtremities(null, ShowMessage: true, Involuntary: false, AllowTelekinetic: false))
				return false;
			GameObject figurine = Cleo_TerraFirma_LatheCarving.CarveFigurine(Actor, GetInputs(Actor));
			if (figurine == null)
				return false;
			if (!CarveSound.IsNullOrEmpty())
				Actor.PlayWorldSound(CarveSound, CarveSoundVolume);
			IComponent<GameObject>.XDidYToZ(Actor, "carve", figurine, EndMark: ".", IndefiniteObject: true, Source: Actor);
			Actor.UseEnergy(1000, "Cybernetics Lapidary Claws Carve");
			return true;
		}

		public override void TurnTick(long TimeTick, int Amount)
		{
			GameObject who = ParentObject.Implantee;
			if (who == null)
				return;
			for (int i = 0; i < Amount; i++)
			{
				foreach (GameObject item in who.GetInventory(o => !o.GetPropertyOrTag(ConversionTag).IsNullOrEmpty()))
					Facet(item, who);
			}
		}

		public bool Facet(GameObject Object, GameObject Holder, bool Force = false)
		{
			string text = Object?.GetPropertyOrTag(ConversionTag);
			if (text.IsNullOrEmpty())
				return false;
			if (!Force && !Chance.in100())
				return false;
			int num = 1;
			if (text.Contains(":"))
			{
				string[] array = text.Split(new string[1] { ":" }, 2, StringSplitOptions.None);
				text = array[0];
				num = array[1].RollCached();
				if (num <= 0)
					return false;
			}
			if (GiganticFactor != 1f && Object.HasPart<ModGigantic>())
				num = (int)(num * GiganticFactor);
			GameObject result = GameObject.CreateUnmodified(text);
			if (result == null)
				return false;
			List<GameObject> all = null;
			List<GameObject> extra = null;
			if (num > 1)
			{
				if (result.CanGenerateStacked())
					result.Count = num;
				else
				{
					all = new List<GameObject> { result };
					extra = new List<GameObject>();
					for (int i = 1; i < num; i++)
					{
						GameObject more = GameObject.CreateUnmodified(text);
						if (more != null)
						{
							all.Add(more);
							extra.Add(more);
						}
					}
				}
			}
			if (Object.GetIntProperty("StoredByPlayer") > 0)
				Object.SetIntProperty("FromStoredByPlayer", 1);
			Object.SplitFromStack();
			WasDerivedFromEvent.Send(Object, ParentObject, result, extra, all, DERIVATION_CONTEXT);
			DerivationCreatedEvent.Send(result, ParentObject, Object, DERIVATION_CONTEXT);
			if (!Verb.IsNullOrEmpty())
			{
				if (all != null)
					DidXToY(Verb, Object, Preposition, Grammar.MakeAndList(all), IndefiniteObject: true, SubjectPossessedBy: Holder, UseVisibilityOf: Holder);
				else
					DidXToYWithZ(Verb, Object, Preposition, result, IndefiniteDirectObject: true, IndefiniteIndirectObject: true, SubjectPossessedBy: Holder, UseVisibilityOf: Holder);
			}
			Object.ReplaceWith(result);
			if (extra != null)
			{
				foreach (GameObject more in extra)
					Holder.ReceiveObject(more);
			}
			result.CheckStack();
			return true;
		}
	}
}
