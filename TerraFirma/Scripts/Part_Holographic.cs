using System;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	/// <summary>
	/// THE HOLOGRAPHIC ROCK (user concept 2026-07-24, designed + ruled 2026-07-25): a rock that
	/// isn't there. The user's mechanics, verbatim: "If you fail an Int check, you can pick it up
	/// and throw it at an enemy. If they fail an Int check, they'll be hit by it and take damage."
	/// A weapon that only works between the credulous — psychosomatic ballistics.
	///
	/// The rulings this part implements:
	///  · Int save DC 12 (RULED: "i feel like it'd benefit from a lower int save? something like 12")
	///  · BELIEF IS STICKY, globally per character: one roll EVER, first encounter, either end —
	///    fail once and you believe in holographic rocks forever; pass once and you can't unsee
	///    the trick. (Also what stops autoexplore retry loops and pickup save-scumming.)
	///  · MentalShield/mindless auto-pass everywhere: machines don't believe — can't be fooled
	///    into carrying it, can't be hit by it (and the mill can't grind it; see the grind parts).
	///  · Take is gated by revert: the engine's take succeeds, then a disbeliever's hand closes
	///    on nothing and the rock returns to the floor — robust across every pickup path.
	///  · A BELIEVED hit does ordinary generic-rock damage (the mundanity is the punchline) and
	///    CONSUMES the stone (discharge law: the lie collapses on contact). A disbelieved throw
	///    passes through — no damage — and the rock lands and persists (nothing happened; it was
	///    never there). It can kill; the death is the epitaph's problem.
	///
	/// BUILD WATCH (flagged for verify): the disbelieved-hit cancel returns false from
	/// WeaponThrowHit and is expected to suppress the damage step — if live play shows damage
	/// landing on disbelievers anyway, this is the seam to fix (a tfholo scenario is the
	/// natural next harness add).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_HolographicPart : IPart
	{
		public const string BELIEVER_PROP = "Cleo_TerraFirma_HoloBeliever";
		public const string DISBELIEVER_PROP = "Cleo_TerraFirma_HoloDisbeliever";
		public const int INT_DC = 12; // RULED 2026-07-25

		[NonSerialized]
		private bool BelievedHit; // set on a believed throw connect; consumed at AfterThrown

		public override bool SameAs(IPart p) => p is Cleo_TerraFirma_HolographicPart && base.SameAs(p);

		/// <summary>The one roll, sticky forever. True = this creature believes the rock exists.</summary>
		public static bool Believes(GameObject who)
		{
			if (who == null)
				return false;
			if (who.HasPart("MentalShield") || who.Brain == null)
				return false; // machines and the mindless: nothing to fool
			if (who.HasIntProperty(BELIEVER_PROP))
				return true;
			if (who.HasIntProperty(DISBELIEVER_PROP))
				return false;
			bool sawThrough = who.MakeSave("Intelligence", INT_DC, null, null, "Cleo_TerraFirma_Holographic Illusion");
			who.SetIntProperty(sawThrough ? DISBELIEVER_PROP : BELIEVER_PROP, 1);
			Helpers.VerifyLog("HOLO", $"{who.Blueprint} first encounter, Int save DC {INT_DC}: {(sawThrough ? "SEES THROUGH (forever)" : "BELIEVES (forever)")}");
			return !sawThrough;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == TakenEvent.ID
				|| ID == AfterThrownEvent.ID;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("ProjectileHit");
			Registrar.Register("WeaponThrowHit");
			base.Register(Object, Registrar);
		}

		public override bool HandleEvent(TakenEvent E)
		{
			GameObject taker = E.Actor;
			if (taker != null && !Believes(taker))
			{
				Cell floor = taker.CurrentCell;
				GameObject stone = ParentObject;
				if (floor != null && stone != null)
				{
					stone.RemoveFromContext();
					floor.AddObject(stone);
					Helpers.VerifyLog("HOLO", $"{taker.Blueprint} reached for it -- hand closed on nothing (take reverted)");
					if (taker.IsPlayer())
						IComponent<GameObject>.AddPlayerMessage("Your hand closes on nothing."); // DRAFT
				}
				return false;
			}
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "ProjectileHit" || E.ID == "WeaponThrowHit")
			{
				GameObject target = E.GetGameObjectParameter("Defender");
				if (GameObject.Validate(ref target))
				{
					if (Believes(target))
					{
						BelievedHit = true; // the lie lands; AfterThrown collapses it
						Helpers.VerifyLog("HOLO", $"believed hit: {target.Blueprint} takes the rock that isn't there");
					}
					else
					{
						Helpers.VerifyLog("HOLO", $"disbelieved: the rock passes through {target.Blueprint} (damage cancelled)");
						if (IComponent<GameObject>.Visible(target))
							IComponent<GameObject>.AddPlayerMessage(ParentObject.The + ParentObject.DisplayNameOnly + " passes through " + target.the + target.DisplayNameOnly + "."); // DRAFT
						return false; // expected to suppress the hit; see BUILD WATCH above
					}
				}
			}
			return base.FireEvent(E);
		}

		public override bool HandleEvent(AfterThrownEvent E)
		{
			if (BelievedHit && GameObject.Validate(ParentObject))
			{
				// discharge law: the believed hit spent the stone -- the lie collapses on contact
				BelievedHit = false;
				Helpers.VerifyLog("HOLO", $"{ParentObject.Blueprint} collapses (believed hit consumed it)");
				ParentObject.Destroy(null, Silent: true);
			}
			return base.HandleEvent(E);
		}
	}
}
