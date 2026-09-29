using System;
using XRL.World.Parts.Mutation;

// Cave moss cooking domain (closed 2026-09-29). Mapping lives in XML
// (ProceduralCookingIngredient_Cleo_TerraFirma_caveMoss); the engine resolves every
// class named there as "XRL.World.Effects." + name (ProceduralCookingEffect.cs), so the
// three effect classes must stay in this namespace. Shapes copy base domains exactly:
// the unit is CookingDomainBurrowing_UnitBurrowingClaws, the trigger is
// CookingDomainTeleport_OnTeleport, the action is CookingDomainTeleport_Teleport_...
namespace XRL.World.Effects
{
	/// <summary>Generic: grants or boosts Earthen Barrage. Tiers are the base low-tier
	/// defaults (ProceduralCookingEffectUnitMutation, CookingDomainPlant_UnitBurgeoningLowTier),
	/// since the ingredient is common.</summary>
	[Serializable]
	public class Cleo_TerraFirma_CookingDomainCaveMoss_UnitEarthenBarrage : ProceduralCookingEffectUnitMutation<Cleo_TerraFirma_EarthenBarrage>
	{
		public Cleo_TerraFirma_CookingDomainCaveMoss_UnitEarthenBarrage()
		{
			AddedTier = "1-2";
			BonusTier = "2-3";
		}
	}

	/// <summary>Condition: whenever the eater throws a stone, 50% (user 2026-09-29). Base fires AfterThrown only
	/// on the thrown item, so Cleo_TerraFirma_StoneThrowSignal (merged onto BaseStone)
	/// forwards it to the thrower as THREW_STONE.</summary>
	[Serializable]
	public class Cleo_TerraFirma_CookingDomainCaveMoss_OnThrowStone : ProceduralCookingEffectWithTrigger
	{
		public const string THREW_STONE = "Cleo_TerraFirma_ThrewStone";

		public override string GetTriggerDescription()
		{
			return "whenever @thisCreature throw@s a stone, there's a 50% chance";
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register(THREW_STONE);
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			// frequent-event triggers carry a chance in base (CookingDomainTongue_OnStuck's 50%);
			// Earthen Barrage keeps stones coming, so an unrolled trigger fires every throw
			if (E.ID == THREW_STONE && 50.in100())
				Trigger();
			return base.FireEvent(E);
		}
	}

	/// <summary>Effect: a sculpted pebble for the thrown slot, or the pack when the slot is
	/// full. Equip order copies Earthen Barrage (ReceiveObject, then AutoEquip only into an
	/// empty slot). The pebble blueprint pins its own damage, see ObjectBlueprints.xml.</summary>
	[Serializable]
	public class Cleo_TerraFirma_CookingDomainCaveMoss_Pebble_ProceduralCookingTriggeredAction : ProceduralCookingTriggeredAction
	{
		public string PebbleBlueprint = "Cleo_TerraFirma_MossPebble";

		public override string GetDescription()
		{
			return "@they gain a sculpted pebble.";
		}

		public override void Apply(GameObject Subject)
		{
			if (Subject == null)
				return;
			GameObject pebble = GameObject.Create(PebbleBlueprint);
			Subject.ReceiveObject(pebble);
			if (Subject.GetFirstThrownWeapon() == null)
				Subject.AutoEquip(pebble, true, Silent: true);
		}
	}
}

namespace XRL.World.Parts
{
	/// <summary>Forwards a stone's AfterThrown to its thrower, so cooking triggers on the
	/// thrower can hear it. Merged onto base BaseStone; deliberately separate from the
	/// quarantined v1 rock part, which also listens to throws.</summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneThrowSignal : IPart
	{
		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == AfterThrownEvent.ID;
		}

		public override bool HandleEvent(AfterThrownEvent E)
		{
			E.Actor?.FireEvent(Event.New(XRL.World.Effects.Cleo_TerraFirma_CookingDomainCaveMoss_OnThrowStone.THREW_STONE, "Object", ParentObject));
			return base.HandleEvent(E);
		}
	}
}
