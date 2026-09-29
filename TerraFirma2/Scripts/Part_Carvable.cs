using System;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	/// <summary>
	/// Carving a wall into a statue (design/carving-spec-2026-09-29.md). The verb lives on the
	/// target, as etching's does (Cleo_TerraFirma_Etchable, merged on the base Wall root): the
	/// action shows only while the player wears gaslight gauntlets and the wall is natural
	/// stone, so walls stay quiet otherwise. The gauntlets pay the charge; the subject comes
	/// from the shared carving picker; the statue is the Stoneshaper's
	/// MakeStatueOfCreatureFromWall (the StatueMaker prototype's flow), inert.
	/// Refusal and success lines DRAFT.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_Carvable : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_CarveWall";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID;
		}

		private bool IsCarvableStone()
		{
			return Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(ParentObject.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>());
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (E.Actor != null && E.Actor.IsPlayer() && IsCarvableStone()
				&& Cleo_TerraFirma_GaslightGauntlets.FindWorn(E.Actor) != null)
				E.AddAction("Carve", "carve", COMMAND, null, 'v', FireOnActor: false);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == COMMAND && E.Actor != null && E.Actor.IsPlayer() && AttemptCarve(E.Actor))
				E.RequestInterfaceExit();
			return base.HandleEvent(E);
		}

		/// <summary>Returns true when a statue was carved and the turn spent.</summary>
		private bool AttemptCarve(GameObject Actor)
		{
			if (!IsCarvableStone())
				return false;
			if (Actor.DistanceTo(ParentObject) > 1)
				return Actor.Fail("You cannot reach " + ParentObject.t() + ".");
			GameObject worn = Cleo_TerraFirma_GaslightGauntlets.FindWorn(Actor);
			var gauntlets = worn?.GetPart<Cleo_TerraFirma_GaslightGauntlets>();
			if (gauntlets == null)
				return Actor.Fail("You have nothing to carve with.");
			if (!worn.TestCharge(gauntlets.CarveCharge, LiveOnly: false, 0L))
				return Actor.Fail(worn.Does("click", int.MaxValue, null, null, "merely") + "."); // base IActivePart unpowered line

			string subject = Cleo_TerraFirma_CarvingPicker.Pick("");
			if (subject == null)
				return false;

			Cell cell = ParentObject.CurrentCell;
			var props = ParentObject.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			GameObject muse = GameObjectFactory.Factory.CreateSampleObject(subject);
			GameObject statue = muse == null ? null : Mutation.Cleo_TerraFirma_Stoneshaper.MakeStatueOfCreatureFromWall(muse, ParentObject, string.Empty, props);
			if (statue == null || cell == null)
			{
				muse?.Obliterate();
				statue?.Obliterate();
				return Actor.Fail("The stone will not take that shape.");
			}
			if (!worn.UseCharge(gauntlets.CarveCharge, LiveOnly: false, 0L))
			{
				muse.Obliterate();
				statue.Obliterate();
				return Actor.Fail(worn.Does("click", int.MaxValue, null, null, "merely") + ".");
			}
			IComponent<GameObject>.XDidYToZ(Actor, "carve", "a statue of", muse, EndMark: "!", ColorAsGoodFor: Actor, IndefiniteObject: true, Source: Actor);
			PlayWorldSound("Sounds/Throw/sfx_throwing_stone_small_impact", 1f, SourceCell: cell);
			muse.Obliterate();
			Actor.UseEnergy(1000, "Item Gaslight Gauntlets Carve");
			ParentObject.Obliterate();
			cell.AddObject(statue);
			return true;
		}
	}
}
