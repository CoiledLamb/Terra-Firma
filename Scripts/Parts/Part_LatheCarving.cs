using System;
using System.Collections.Generic;
using System.Linq;
using Cleo.TerraFirma.Scripts;
using XRL.UI;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_LatheCarving : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_LatheCarve";
		public const string INPUT_TAG = "Cleo_TerraFirma_LatheFigurine";

		[NonSerialized] public string CarveSound = "Sounds/Interact/sfx_interact_engraver";
		[NonSerialized] public float CarveSoundVolume = 0.5f;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID
				|| ID == CanSmartUseEvent.ID
				|| ID == CommandSmartUseEvent.ID;
		}

		private static List<GameObject> GetInputs(GameObject Actor)
		{
			return Actor.GetInventory(item => item.HasTag(INPUT_TAG));
		}

		private bool CanCarve(GameObject actor)
		{
			return actor != null && actor.IsPlayer() && actor != ParentObject && ParentObject.Understood()
				&& actor.PhaseMatches(ParentObject) && actor.FlightCanReach(ParentObject);
		}

		public override bool HandleEvent(CanSmartUseEvent E)
		{
			if (CanCarve(E.Actor))
				return false;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(CommandSmartUseEvent E)
		{
			if (CanCarve(E.Actor))
				AttemptCarve(E.Actor);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (E.Actor != null && E.Actor.IsPlayer() && GetInputs(E.Actor).Count > 0)
				E.AddAction("Carve", "carve", COMMAND, null, 'v', FireOnActor: false, Default: 5);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == COMMAND && E.Actor != null && E.Actor.IsPlayer() && AttemptCarve(E.Actor))
				E.RequestInterfaceExit();
			return base.HandleEvent(E);
		}

		private bool AttemptCarve(GameObject Actor)
		{
			if (Actor.DistanceTo(ParentObject) > 1)
				return Actor.Fail("You cannot reach " + ParentObject.t() + ".");
			if (!Actor.CanMoveExtremities(null, ShowMessage: true, Involuntary: false, AllowTelekinetic: true))
				return false;
			ChargeSink sink = ParentObject.GetPart<ChargeSink>();
			if (sink != null && !sink.IsReady())
				return Actor.Fail(ParentObject.Does("click", int.MaxValue, null, null, "merely") + ".");

			GameObject figurine = CarveFigurine(Actor, GetInputs(Actor));
			if (figurine == null)
				return false;
			if (!CarveSound.IsNullOrEmpty())
				ParentObject.PlayWorldSound(CarveSound, CarveSoundVolume);
			IComponent<GameObject>.XDidYToZ(Actor, "lathe", figurine, EndMark: ".", IndefiniteObject: true, Source: Actor);
			Actor.UseEnergy(1000, "Item Lathe Carve");
			return true;
		}

		public static string FigurineBlueprintFor(GameObject material)
		{
			string tag = material?.GetTag(INPUT_TAG);
			if (!tag.IsNullOrEmpty())
				return tag;
			return Cleo_TerraFirma_SimulacrumLoom.IsLoomStone(material) ? Cleo_TerraFirma_SimulacrumLoom.STONE_FIGURINE : null;
		}

		public static GameObject CarveFigurine(GameObject Actor, List<GameObject> inputs)
		{
			if (inputs == null || inputs.Count == 0)
			{
				Actor.Fail("You have nothing to carve.");
				return null;
			}
			GameObject material = Popup.PickGameObject("Choose something to carve.", inputs, AllowEscape: true);
			if (material == null)
				return null;
			string figurineBlueprint = FigurineBlueprintFor(material);
			if (figurineBlueprint == null || GameObjectFactory.Factory.GetBlueprintIfExists(figurineBlueprint) == null)
			{
				Actor.Fail("You cannot carve " + material.t() + ".");
				return null;
			}

			string subject = Cleo_TerraFirma_CarvingPicker.Pick("");
			if (subject == null)
				return null;

			GameObject figurine = GameObject.CreateUnmodified(figurineBlueprint, BeforeObjectCreated: o =>
			{
				RandomFigurine rf = o.GetPart<RandomFigurine>();
				if (rf != null)
					rf.Creature = subject;
			});
			if (figurine == null)
			{
				Actor.Fail("You cannot carve " + material.t() + ".");
				return null;
			}
			foreach (IPart part in material.PartsList.ToArray())
			{
				if (part is IModification mod && !figurine.HasPart(mod.Name))
					XRL.World.Tinkering.ItemModding.ApplyModification(figurine, mod.Name, mod.Tier);
			}

			material.RemoveOne().Obliterate();
			Actor.ReceiveObject(figurine);
			return figurine;
		}
	}
}
