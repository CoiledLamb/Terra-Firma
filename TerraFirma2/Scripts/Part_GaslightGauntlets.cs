using System;
using Cleo.TerraFirma.Scripts;
using XRL.World.Anatomy;

namespace XRL.World.Parts
{
	/// <summary>
	/// Gaslight gauntlets (design/carving-spec-2026-09-29.md). Three jobs:
	///
	/// Name: base Gaslight's charged/uncharged swap ("{{gaslight|gaslight}}" only while the cell
	/// can pay), done here because the base Gaslight part is weapon-only.
	///
	/// Fists: while worn, each hand under the gauntlets fights with a gaslight fist. Copies base
	/// CyberneticsFistReplacement (hands whose SupportsDependent matches the worn part's
	/// DependsOn get a new DefaultBehavior tagged TemporaryDefaultBehavior), but driven by
	/// equip instead of implant. Restoring is base's own path: Body.RegenerateDefaultEquipment
	/// obliterates TemporaryDefaultBehavior fists, rebuilds the originals, then sends
	/// RegenerateDefaultEquipmentEvent (cascades to equipment), where a still-worn pair
	/// re-applies. Equip and unequip just ask the body to regenerate.
	///
	/// Carving: an inventory action on the worn pair turns an adjacent natural stone wall into
	/// an inert statue of a creature chosen in the shared carving picker, through the
	/// Stoneshaper's MakeStatueOfCreatureFromWall (the StatueMaker prototype's flow).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GaslightGauntlets : IPart
	{
		public const string COMMAND_CARVE = "Cleo_TerraFirma_CarveWithGauntlets";

		public string FistObject = "Cleo_TerraFirma_GaslightFist";
		public string ChargedName = "{{gaslight|gaslight}} gauntlets";
		public string UnchargedName = "gaslight gauntlets";
		public int PunchCharge = 100;
		public int CarveCharge = 1000;

		[NonSerialized]
		private bool Unequipping;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != EquippedEvent.ID && ID != UnequippedEvent.ID
				&& ID != PooledEvent<RegenerateDefaultEquipmentEvent>.ID && ID != PooledEvent<GetDisplayNameEvent>.ID
				&& ID != GetInventoryActionsEvent.ID)
				return ID == InventoryActionEvent.ID;
			return true;
		}

		public bool CanPay(int Charge)
		{
			return ParentObject.TestCharge(Charge, LiveOnly: false, 0L);
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			if (E.DB.PrimaryBase == UnchargedName && CanPay(PunchCharge))
				E.ReplacePrimaryBase(ChargedName);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EquippedEvent E)
		{
			E.Actor?.Body?.UpdateBodyParts();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnequippedEvent E)
		{
			Unequipping = true;
			try
			{
				E.Actor?.Body?.UpdateBodyParts();
			}
			finally
			{
				Unequipping = false;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(RegenerateDefaultEquipmentEvent E)
		{
			if (!Unequipping && E.Body != null && ParentObject.Equipped == E.Body.ParentObject && ParentObject.IsEquippedProperly())
				ApplyFists(E.Body);
			return base.HandleEvent(E);
		}

		private void ApplyFists(Body Body)
		{
			string dependency = ParentObject.EquippedOn()?.DependsOn;
			if (string.IsNullOrEmpty(dependency))
				return;
			foreach (BodyPart hand in Body.GetPart("Hand"))
			{
				if (hand.Extrinsic || hand.SupportsDependent != dependency)
					continue;
				if (hand.DefaultBehavior?.Blueprint == FistObject)
					continue;
				hand.DefaultBehavior?.Obliterate();
				hand.DefaultBehavior = GameObject.Create(FistObject);
				hand.DefaultBehavior.SetStringProperty("TemporaryDefaultBehavior", "Cleo_TerraFirma_GaslightGauntlets");
			}
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (ParentObject.Equipped != null && ParentObject.Equipped == E.Actor)
				E.AddAction("Carve", "carve", COMMAND_CARVE, null, 'v', Default: 10);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == COMMAND_CARVE && E.Actor != null && E.Actor.IsPlayer())
			{
				if (Carve(E.Actor))
					E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
		}

		/// <summary>Returns true when a statue was carved and the turn spent.</summary>
		private bool Carve(GameObject Actor)
		{
			if (Actor.OnWorldMap())
				return Actor.Fail("You cannot carve anything here.");
			if (!CanPay(CarveCharge))
				return Actor.Fail(ParentObject.Does("click", int.MaxValue, null, null, "merely") + "."); // base IActivePart unpowered line

			Cell cell = Actor.Physics.PickDestinationCell(1, RequireCombat: false, Label: "Choose an adjacent wall to carve.");
			if (cell == null)
				return false;
			GameObject wall = cell.GetFirstWall();
			if (wall == null || !cell.IsAdjacentTo(Actor.CurrentCell))
				return Actor.Fail("You must choose an adjacent wall.");
			var props = wall.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			if (!Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(props))
				return Actor.Fail("You can only carve natural stone.");

			string subject = Cleo_TerraFirma_CarvingPicker.Pick("");
			if (subject == null)
				return false;

			GameObject muse = GameObjectFactory.Factory.CreateSampleObject(subject);
			GameObject statue = muse == null ? null : Mutation.Cleo_TerraFirma_Stoneshaper.MakeStatueOfCreatureFromWall(muse, wall, string.Empty, props);
			if (statue == null)
			{
				muse?.Obliterate();
				return Actor.Fail("The stone will not take that shape.");
			}
			if (!ParentObject.UseCharge(CarveCharge, LiveOnly: false, 0L))
			{
				muse.Obliterate();
				statue.Obliterate();
				return Actor.Fail(ParentObject.Does("click", int.MaxValue, null, null, "merely") + ".");
			}
			wall.Obliterate();
			cell.AddObject(statue);
			IComponent<GameObject>.XDidYToZ(Actor, "carve", "a statue of", muse, EndMark: "!", ColorAsGoodFor: Actor, IndefiniteObject: true, Source: Actor);
			PlayWorldSound("Sounds/Throw/sfx_throwing_stone_small_impact", 1f, SourceCell: cell);
			muse.Obliterate();
			Actor.UseEnergy(1000, "Item Gaslight Gauntlets Carve");
			return true;
		}
	}

	/// <summary>
	/// The fist the gaslight gauntlets lend (1d4, set in XML). Each swing asks the worn
	/// gauntlets for the gaslight charge; if they pay, the swing gets gaslight's penetration
	/// (base Gaslight kris/chisel: +5 PV for 100 charge), added the way a weapon's PenBonus is
	/// (Combat adds PenBonus to both PenetrationBonus and MaxPenetrationBonus).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GaslightFist : IPart
	{
		public int PenetrationBonus = 5;

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == GetWeaponMeleePenetrationEvent.ID;
			return true;
		}

		public override bool HandleEvent(GetWeaponMeleePenetrationEvent E)
		{
			if (E.Weapon == ParentObject && E.Attacker != null)
			{
				foreach (GameObject item in E.Attacker.GetEquippedObjectsReadonly())
				{
					var gauntlets = item.GetPart<Cleo_TerraFirma_GaslightGauntlets>();
					if (gauntlets != null && item.IsEquippedProperly())
					{
						if (item.UseCharge(gauntlets.PunchCharge, LiveOnly: false, 0L))
						{
							E.PenetrationBonus += PenetrationBonus;
							E.MaxPenetrationBonus += PenetrationBonus;
						}
						break;
					}
				}
			}
			return base.HandleEvent(E);
		}
	}
}
