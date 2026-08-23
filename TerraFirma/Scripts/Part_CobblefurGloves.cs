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
			// ALL THROWN ITEMS, not just stones (user rework 2026-08-22). Launcher == null is
			// the thrown-vs-shot gate; a lit grenade caught is your problem now.
			if (wearer != null && rock != null && E.Launcher == null
				&& !E.Done && GameObject.Validate(rock) && !rock.IsInGraveyard()
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
				// HOT POTATO (user design 2026-08-22: "catching a grenade could allow you a
				// turn before it explodes, so you can throw it back"): without this, a caught
				// grenade detonates IN YOUR FACE the same instant - IGrenade.Detonate resolves
				// a null cell through the HOLDER and drops the grenade at your feet before the
				// boom (decomp-verified). The marker cancels that via BeforeDetonateEvent,
				// keeps it in hand for a short fuse, then it goes off wherever it is.
				if (rock.GetPartDescendedFrom<IGrenade>() != null)
				{
					Cleo_TerraFirma_CaughtGrenade fuse = rock.RequirePart<Cleo_TerraFirma_CaughtGrenade>();
					fuse.Catcher = wearer;
					fuse.FuseTurns = 1; // ~one usable turn; DRAFT dial
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GLOV", $"caught LIVE GRENADE {rock.Blueprint}, fuse ticking");
				}
				E.Done = true;
				return false;
			}
			return base.HandleEvent(E);
		}
	}

	/// <summary>
	/// The caught-grenade fuse (user design 2026-08-22): a snatched grenade gets a short grace
	/// window instead of detonating in the catcher's face. While the fuse runs and the grenade
	/// is still in the catcher's possession, BeforeDetonateEvent is CANCELED and the grenade is
	/// pulled back into hand (Detonate drops it to the cell before asking). Throw it away and
	/// the next detonation attempt is allowed through - the marker strips itself. Sit on it and
	/// the fuse runs out: it detonates wherever it is, holder resolved by the engine.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_CaughtGrenade : IPart
	{
		public int FuseTurns = 1;
		public GameObject Catcher;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<EndTurnEvent>.ID
				|| ID == PooledEvent<BeforeDetonateEvent>.ID;
		}

		public override bool HandleEvent(BeforeDetonateEvent E)
		{
			if (FuseTurns > 0 && GameObject.Validate(ref Catcher))
			{
				Cell here = ParentObject.CurrentCell;
				bool inCatcherReach = ParentObject.Equipped == Catcher || ParentObject.InInventory == Catcher
					|| (here != null && here == Catcher.CurrentCell);
				if (inCatcherReach)
				{
					if (here != null)
					{
						// Detonate already dumped it on the floor; back into hand it goes
						ParentObject.RemoveFromContext();
						Catcher.ReceiveObject(ParentObject);
					}
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GLOV", $"grenade detonation held off, fuse {FuseTurns}");
					return false;
				}
			}
			// thrown away (or fuse spent): let it happen, job done
			ParentObject.RemovePart(this);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			FuseTurns--;
			if (FuseTurns < 0)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GLOV", "caught grenade fuse expired");
				IGrenade grenade = ParentObject.GetPartDescendedFrom<IGrenade>();
				GameObject catcher = Catcher;
				ParentObject.RemovePart(this);
				grenade?.Detonate(null, catcher);
			}
			return base.HandleEvent(E);
		}
	}
}
