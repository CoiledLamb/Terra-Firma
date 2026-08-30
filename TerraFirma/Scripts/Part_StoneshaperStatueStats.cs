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
		public string NaturalWeaponDamage;
		public string MuseWeaponMap = "";

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
			if (NaturalWeaponDamage.IsNullOrEmpty() || ParentObject?.Body == null)
				return;
			foreach (BodyPart part in ParentObject.Body.LoopParts())
			{
				if (part.DefaultBehaviorBlueprint != Mutation.Cleo_TerraFirma_Stoneshaper.DEFAULT_BEHAVIOR_BP || part.DefaultBehavior == null)
					continue;
				MeleeWeapon wp = part.DefaultBehavior.RequirePart<MeleeWeapon>();
				wp.BaseDamage = NaturalWeaponDamage;
				wp.Slot = part.Type;
				if (!MuseWeaponMap.IsNullOrEmpty() && part.Type != null)
				{
					foreach (string line in MuseWeaponMap.Split('\n'))
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
			StatShifter.SetStatShift("Hitpoints", CurHp, true);
			StatShifter.SetStatShift("AV", CurAv, true);
		}

		public override void Remove()
		{
			StatShifter.RemoveStatShifts();
		}
	}
}
