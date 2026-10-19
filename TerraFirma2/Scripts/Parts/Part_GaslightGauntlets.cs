using System;
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
	/// Carving lives on the wall, not here (etching's rule: the verb belongs to the target);
	/// see Cleo_TerraFirma_Carvable. This part only supplies the charge and FindWorn.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GaslightGauntlets : IPart
	{
		public string FistObject = "Cleo_TerraFirma_GaslightFist";
		public string ChargedName = "{{gaslight|gaslight}} gauntlets";
		public string UnchargedName = "gaslight gauntlets";
		public int PunchCharge = 100;
		public int CarveCharge = 1000;

		/// <summary>Played at the wearer when a punch spends PunchCharge: base's gaslight blade swing.
		/// (Carving already sounds through Stoneshaper.PlayCarveReveal.) XML-settable dials;
		/// [NonSerialized] so the shipped part keeps its saved layout.</summary>
		[NonSerialized] public string PunchSound = "Sounds/Melee/longBlades/sfx_melee_longBlade_gaslight_swing";
		[NonSerialized] public float PunchSoundVolume = 0.5f;

		[NonSerialized]
		private bool Unequipping;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != EquippedEvent.ID && ID != UnequippedEvent.ID
				&& ID != PooledEvent<RegenerateDefaultEquipmentEvent>.ID)
				return ID == PooledEvent<GetDisplayNameEvent>.ID;
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

		/// <summary>The gauntlets an actor is properly wearing, or null.</summary>
		public static GameObject FindWorn(GameObject Actor)
		{
			if (Actor == null)
				return null;
			foreach (GameObject item in Actor.GetEquippedObjectsReadonly())
			{
				if (item.HasPart<Cleo_TerraFirma_GaslightGauntlets>() && item.IsEquippedProperly())
					return item;
			}
			return null;
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
				GameObject worn = Cleo_TerraFirma_GaslightGauntlets.FindWorn(E.Attacker);
				var gauntlets = worn?.GetPart<Cleo_TerraFirma_GaslightGauntlets>();
				if (gauntlets != null && worn.UseCharge(gauntlets.PunchCharge, LiveOnly: false, 0L))
				{
					if (!gauntlets.PunchSound.IsNullOrEmpty())
						E.Attacker.PlayWorldSound(gauntlets.PunchSound, gauntlets.PunchSoundVolume, Combat: true);
					E.PenetrationBonus += PenetrationBonus;
					E.MaxPenetrationBonus += PenetrationBonus;
				}
			}
			return base.HandleEvent(E);
		}
	}
}
