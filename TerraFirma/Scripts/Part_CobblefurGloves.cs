using System;
using XRL.Rules;
using XRL.World.Anatomy;

namespace XRL.World.Parts
{
	/// <summary>
	/// Cobblefur gloves: the 2026-08-02 gearset "catch gloves" concept, landed via baboon
	/// butchery (user 2026-08-13). Reactive rock-catch: an incoming THROWN stone (the mod's
	/// IsAnyStone tag on the projectile) has a flat, uncontested chance to be snatched out of
	/// the air before it strikes. Interception rides the same defender-side event the Gravel
	/// Whorl deflection uses (DefenderMissileHitEvent, E.Done consume idiom); the item-part-
	/// listens-on-wearer plumbing is the base Yurtmat's trick.
	/// Caught rock destination (Claude lean built, gearset fork, veto cheap): auto-readied
	/// into an empty Thrown Weapon slot (the scavenger's proven re-ready plumbing), else
	/// inventory. Catching a rock and instantly offering it back is the whole comedy.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_CobblefurGloves : IPart
	{
		/// <summary>Flat catch chance per incoming thrown rock. DRAFT 25 (numbers sitting owed).
		/// Uncontested by design (user: "0/1 statline, since it's completely uncontested").</summary>
		public int Chance = 25;

		public override bool SameAs(IPart p)
		{
			return p is Cleo_TerraFirma_CobblefurGloves o && o.Chance == Chance && base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != EquippedEvent.ID)
				return ID == UnequippedEvent.ID;
			return true;
		}

		public override bool HandleEvent(EquippedEvent E)
		{
			if (E.Item.IsEquippedProperly())
				E.Actor.RegisterEvent(this, DefenderMissileHitEvent.ID, 0, Serialize: true);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnequippedEvent E)
		{
			E.Actor.UnregisterEvent(this, DefenderMissileHitEvent.ID);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(DefenderMissileHitEvent E)
		{
			GameObject wearer = ParentObject?.Equipped;
			GameObject rock = E.Projectile;
			if (wearer != null && rock != null && rock.HasTag("Cleo_TerraFirma_IsAnyStone")
				&& Stat.Random(1, 100) <= Chance)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GLOV", $"{wearer.Blueprint} catch PROC on {rock.Blueprint} ({Chance}%)");
				IComponent<GameObject>.XDidYToZ(wearer, "snatch", rock, "out of the air", null, null, null, wearer);
				if (wearer.TakeObject(rock, NoStack: false, Silent: true, EnergyCost: 0))
				{
					// re-ready idiom (StoneScavenger's ruled free action): empty thrown slot gets
					// the caught rock offered straight back
					BodyPart slot = wearer.GetFirstBodyPart("Thrown Weapon");
					if (slot != null && slot.Equipped == null)
						wearer.FireEvent(Event.New("CommandEquipObject", "Object", rock, "BodyPart", slot));
				}
				E.Done = true;
				return false;
			}
			return base.HandleEvent(E);
		}
	}
}
