using System;
using System.Collections.Generic;
using System.Linq;
using Cleo.TerraFirma.Scripts;
using XRL.UI;

namespace XRL.World.Parts
{
	/// <summary>
	/// Lathe carving (design/carving-spec-2026-09-29.md), merged onto the base Lathe. The verb
	/// lives on the lathe (etching's rule, as the wall's Carve does) and shows only while the
	/// player carries something it can turn: any item tagged Cleo_TerraFirma_LatheFigurine,
	/// whose value names the base figurine blueprint it becomes (Merges.xml: copper, silver and
	/// gold nuggets, rough and polished gems, onto base's own "Random X Figurine" set).
	/// Order (user 2026-09-29): power check, then material, then subject; nothing is spent
	/// until a subject is chosen. The figurine is base RandomFigurine with Creature preset, the
	/// same shape as base's own "Rhinox figure". Refusal and success lines draft, approved 2026-09-30.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_LatheCarving : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_LatheCarve";
		public const string INPUT_TAG = "Cleo_TerraFirma_LatheFigurine";

		/// <summary>Played once at the lathe when a figurine comes off it: the base engraver clip.
		/// XML-settable dials; [NonSerialized] so the shipped part keeps its saved layout.</summary>
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

		// space on the lathe = Carve while an input is carried (user 2026-09-30); the
		// SmartUse pair with the chalkboard's gates, Chair's Sit being the base precedent
		private bool CanCarve(GameObject actor)
		{
			return actor != null && actor.IsPlayer() && actor != ParentObject && ParentObject.Understood()
				&& actor.PhaseMatches(ParentObject) && actor.FlightCanReach(ParentObject)
				&& GetInputs(actor).Count > 0;
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
			if (!Actor.CanMoveExtremities(null, ShowMessage: true, Involuntary: false, AllowTelekinetic: true)) // frozen: base Engraver/Tinker gate
				return false;
			ChargeSink sink = ParentObject.GetPart<ChargeSink>();
			if (sink != null && !sink.IsReady())
				return Actor.Fail(ParentObject.Does("click", int.MaxValue, null, null, "merely") + "."); // base IActivePart unpowered line

			List<GameObject> inputs = GetInputs(Actor);
			if (inputs.Count == 0)
				return Actor.Fail("You have nothing to carve.");
			GameObject material = Popup.PickGameObject("Choose something to carve.", inputs, AllowEscape: true);
			if (material == null)
				return false;
			string figurineBlueprint = material.GetTag(INPUT_TAG);
			if (GameObjectFactory.Factory.GetBlueprintIfExists(figurineBlueprint) == null)
				return Actor.Fail("You cannot carve " + material.t() + ".");

			string subject = Cleo_TerraFirma_CarvingPicker.Pick("");
			if (subject == null)
				return false;

			// unmodified: the figurine wears only the material's mods, no roll of its own (user 2026-10-19)
			GameObject figurine = GameObject.CreateUnmodified(figurineBlueprint, BeforeObjectCreated: o =>
			{
				RandomFigurine rf = o.GetPart<RandomFigurine>();
				if (rf != null)
					rf.Creature = subject;
			});
			if (figurine == null)
				return Actor.Fail("You cannot carve " + material.t() + ".");
			// the material's mods carry over (playtest 2026-10-19: a gigantic nugget turned a plain
			// figurine). By name and tier through ItemModding, not ModificationApplicable: a
			// figurine has no mod key, so that gate refuses everything.
			foreach (IPart part in material.PartsList.ToArray())
			{
				if (part is IModification mod && !figurine.HasPart(mod.Name))
					XRL.World.Tinkering.ItemModding.ApplyModification(figurine, mod.Name, mod.Tier);
			}

			material.RemoveOne().Obliterate();
			Actor.ReceiveObject(figurine);
			if (!CarveSound.IsNullOrEmpty())
				ParentObject.PlayWorldSound(CarveSound, CarveSoundVolume);
			IComponent<GameObject>.XDidYToZ(Actor, "lathe", figurine, EndMark: ".", IndefiniteObject: true, Source: Actor);
			Actor.UseEnergy(1000, "Item Lathe Carve");
			return true;
		}
	}
}
