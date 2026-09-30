using Cleo.TerraFirma.Scripts;
using System;
using XRL.World.Anatomy;

namespace XRL.World.Parts
{
	/// <summary>
	/// Simple part that handles HP and AV offsets for statues created by Stoneshaper, and re-arms
	/// the statue's natural weapons after the engine regenerates default equipment.
	/// The numbers passed to this are calculated in <see cref="Mutation.Cleo_TerraFirma_Stoneshaper.ApplyStatueBuffs(GameObject, GameObject, GameObject, int, Cleo_TerraFirma_StoneshaperWallProperties)"/>.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneshaperStatueStatsPart : IPart
	{
		public int CurHp;
		public int CurAv;

		// Re-arm data (assumption-audit fix 2026-08-29): Body.RegenerateDefaultEquipment
		// obliterates and recreates any DefaultBehavior whose Blueprint matches its part's
		// DefaultBehaviorBlueprint (fires on dismemberment/regrowth/implant via UpdateBodyParts),
		// wiping the in-place customization ApplyStatueBuffs made. The recreate happens BEFORE
		// RegenerateDefaultEquipmentEvent.Send (Body.cs:2307-2337), so re-applying from that event
		// sticks; Horns.RegrowHorns is the base precedent. MuseWeaponMap holds one
		// "Type\tName\tTile" line per muse-armed slot type.
		//
		// SAVE LAYOUT (fixed 2026-09-30): the engine reads a part's public fields by position
		// with no end check (IComponent.Read), and 0.1.2.5 saved exactly CurHp and CurAv. These
		// two stay out of the save ([NonSerialized]) and persist as string properties on the
		// statue instead, so an old statue loads clean. Never add a saved public field here.
		[NonSerialized] public string NaturalWeaponDamage;
		[NonSerialized] public string MuseWeaponMap = "";
		private const string DAMAGE_PROP = "Cleo_TerraFirma_StatueFistDamage";
		private const string MAP_PROP = "Cleo_TerraFirma_StatueMuseWeapons";

		public Cleo_TerraFirma_StoneshaperStatueStatsPart() { }

		public Cleo_TerraFirma_StoneshaperStatueStatsPart(int CurHp, int CurAv)
		{
			Helpers.DebugLog($"initializing stoneshaper statue: {CurHp}, {CurAv}", "STU");
			this.CurHp = CurHp;
			this.CurAv = CurAv;
		}

		public override bool WantEvent(int ID, int Cascade)
		{
			return base.WantEvent(ID, Cascade)
				|| ID == PooledEvent<GetDisplayNameEvent>.ID
				|| ID == SingletonEvent<GetDebugInternalsEvent>.ID
				|| ID == PooledEvent<RegenerateDefaultEquipmentEvent>.ID;
		}

		public override bool HandleEvent(GetDebugInternalsEvent E)
		{
			E.AddEntry(this, nameof(CurHp), CurHp);
			E.AddEntry(this, nameof(CurAv), CurAv);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(RegenerateDefaultEquipmentEvent E)
		{
			RearmNaturalWeapons();
			return base.HandleEvent(E);
		}

		/// <summary>
		/// Re-applies the statue's natural-weapon customization (mutation-formula damage, slot,
		/// muse name/tile) to every body part carrying the statue fist blueprint. Idempotent;
		/// safe on regrown limbs, which arrive with a fresh unmodified fist.
		/// </summary>
		public void RearmNaturalWeapons()
		{
			if (ParentObject?.Body == null)
				return;
			string damage = NaturalWeaponDamage.IsNullOrEmpty() ? ParentObject.GetStringProperty(DAMAGE_PROP) : NaturalWeaponDamage;
			string map = MuseWeaponMap.IsNullOrEmpty() ? ParentObject.GetStringProperty(MAP_PROP) : MuseWeaponMap;
			if (damage.IsNullOrEmpty())
				return;
			foreach (BodyPart part in ParentObject.Body.LoopParts())
			{
				if (part.DefaultBehaviorBlueprint != Mutation.Cleo_TerraFirma_Stoneshaper.DEFAULT_BEHAVIOR_BP || part.DefaultBehavior == null)
					continue;
				MeleeWeapon wp = part.DefaultBehavior.RequirePart<MeleeWeapon>();
				wp.BaseDamage = damage;
				wp.Slot = part.Type;
				if (!map.IsNullOrEmpty() && part.Type != null)
				{
					foreach (string line in map.Split('\n'))
					{
						string[] cols = line.Split('\t');
						if (cols.Length == 3 && cols[0] == part.Type)
						{
							part.DefaultBehavior.Render.DisplayName = cols[1];
							part.DefaultBehavior.Render.Tile = cols[2];
							break;
						}
					}
				}
				Helpers.VerifyLog("SHAPE", $"re-arm: statue {part.Type} weapon re-decorated after default-equipment regen");
			}
		}

		public override void Attach()
		{
			if (!NaturalWeaponDamage.IsNullOrEmpty())
				ParentObject.SetStringProperty(DAMAGE_PROP, NaturalWeaponDamage);
			if (!MuseWeaponMap.IsNullOrEmpty())
				ParentObject.SetStringProperty(MAP_PROP, MuseWeaponMap);
			StatShifter.SetStatShift("Hitpoints", CurHp, true);
			StatShifter.SetStatShift("AV", CurAv, true);
		}

		public override void Remove()
		{
			StatShifter.RemoveStatShifts();
		}
	}
}
