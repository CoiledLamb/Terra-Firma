using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;
using XRL.World.Anatomy; // BodyPart
using XRL.World.Effects;

namespace XRL.World.Parts
{
	/// <summary>
	/// The drainstone's appetite (redesign RULED 2026-07-22: "cursed equipment"). On a hit it fastens
	/// onto a random body part of the victim — displacing worn gear, rerolling slots held by natural
	/// weapons (user ruling) — becomes Cursed (unremovable), and applies the engine's native LifeDrain,
	/// which heals the drainer with every point it takes. After RoundsLeft it crumbles away, spent
	/// (fixed-duration ruling: disposable like the rest of the rock family). If it comes off early
	/// (victim died, limb gone), the curse lifts and the stone survives as loot.
	/// Thrown latches are DEFERRED to AfterThrownEvent: force-equipping mid-flight would fight the
	/// engine's own placement of the thrown object. Step latches fire immediately.
	/// The blueprint carries Armor WornOn="*" so any slot accepts it (verified wildcard).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_DrainLatch : IPart
	{
		/// <summary>Damage drained per round while latched; every point heals the drainer (native LifeDrain).</summary>
		public string DrainDie = "1d3";

		/// <summary>Rounds of latch remaining; 0 = not latched.</summary>
		public int RoundsLeft;

		/// <summary>Who the drained life flows to. Null is allowed (unowned floor stone): the native
		/// effect picks someone semi-random — ruled acceptable chaos ("random if not spawned from the mutation").</summary>
		public GameObject Drainer;

		/// <summary>Latched onto something with no life to drink (RULED 2026-07-23: inorganics —
		/// robots, animated furniture — get attach-but-no-leech). Detection = the engine's own
		/// LifeDrain.IsValidTarget (IsOrganic gate), chosen over NoBleed: robots bleed oil, so a
		/// bleed test would misread them as drainable. Decides the ending: gorged burst vs dry crumble.</summary>
		public bool Starved;

		[NonSerialized]
		private GameObject PendingVictim;

		[NonSerialized]
		private int PendingDuration;

		[NonSerialized]
		private GameObject PendingDrainer;

		public override bool SameAs(IPart p)
		{
			// config-only: latched stones are equipped and never stack anyway
			return p is Cleo_TerraFirma_DrainLatch o && o.DrainDie == DrainDie && base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == AfterThrownEvent.ID;
		}

		/// <summary>Queue a latch for after the engine finishes resolving the throw.</summary>
		public void RequestLatch(GameObject victim, int duration, GameObject drainer)
		{
			Helpers.VerifyLog("DRAIN", $"latch QUEUED for {victim?.Blueprint ?? "?"} ({duration} rounds), awaiting AfterThrown");
			PendingVictim = victim;
			PendingDuration = duration;
			PendingDrainer = drainer;
		}

		public override bool HandleEvent(AfterThrownEvent E)
		{
			Helpers.VerifyLog("DRAIN", $"AfterThrown reached DrainLatch: pending={(PendingVictim == null ? "NONE" : PendingVictim.Blueprint)}");
			GameObject victim = PendingVictim;
			bool hadPending = victim != null; // Validate(ref) nulls invalid refs; remember we had one
			PendingVictim = null;
			if (GameObject.Validate(ref victim))
				Latch(victim, PendingDuration, PendingDrainer);
			else if (hadPending)
				// the one formerly-silent link (user-diagnosed 2026-07-24: throw damage one-shot the
				// victim before the deferred latch could validate them; correct behavior, now spoken)
				Helpers.VerifyLog("DRAIN", "pending victim died before the latch could take hold; stone rests as loot");
			return base.HandleEvent(E);
		}

		/// <summary>Fasten this stone onto <paramref name="victim"/> for <paramref name="duration"/> rounds.</summary>
		public void Latch(GameObject victim, int duration, GameObject drainer)
		{
			if (RoundsLeft > 0 || !GameObject.Validate(ref victim) || victim.Body == null)
			{
				Helpers.VerifyLog("DRAIN", $"latch DECLINED at entry: roundsLeft={RoundsLeft}, victimValid={GameObject.Validate(ref victim)}, hasBody={victim?.Body != null}");
				return;
			}
			GameObject.Validate(ref drainer);
			ParentObject.SplitFromStack();
			// candidate slots: reroll natural-weapon slots (claws and fists stay; masking a fist-only
			// slot counts too), then let the engine's own slot validation arbitrate — try a few.
			List<BodyPart> parts = new List<BodyPart>();
			foreach (BodyPart bp in victim.Body.GetParts())
			{
				if (bp.Equipped != null && bp.Equipped.IsNatural())
					continue;
				if (bp.Equipped == null && bp.DefaultBehavior != null)
					continue;
				parts.Add(bp);
			}
			Cell home = ParentObject.CurrentCell; // restore point if every slot refuses
			bool latched = false;
			for (int tries = 0; tries < 6 && parts.Count > 0 && !latched; tries++)
			{
				int idx = Stat.Random(0, parts.Count - 1);
				BodyPart slot = parts[idx];
				parts.RemoveAt(idx);
				ParentObject.RemoveFromContext();
				if (victim.ForceEquipObject(ParentObject, slot, Silent: false, 0))
				{
					latched = true;
					if (IComponent<GameObject>.Visible(victim))
						IComponent<GameObject>.AddPlayerMessage(ParentObject.Does("fasten") + " onto " + victim.t() + "'s " + slot.GetOrdinalName() + "!");
				}
			}
			Starved = !XRL.World.Parts.Mutation.LifeDrain.IsValidTarget(victim);
			if (latched)
			{
				if (!ParentObject.HasPart<Cursed>())
					ParentObject.AddPart(new Cursed { RevealInDescription = true });
				RoundsLeft = duration;
				Drainer = drainer;
				Helpers.VerifyLog("DRAIN", $"LATCHED onto {victim.Blueprint} for {duration} rounds; drainer={drainer?.Blueprint ?? "none (native random fallback)"}, Cursed applied{(Starved ? "; STARVED (inorganic, no leech)" : "")}");
			}
			else
			{
				Helpers.VerifyLog("DRAIN", $"no slot accepted the latch on {victim.Blueprint}; stone restored{(Starved ? "" : ", drain still applies")}");
				if (home != null && ParentObject.CurrentCell == null && ParentObject.Equipped == null && ParentObject.InInventory == null)
					home.AddObject(ParentObject); // no slot took it: back where it was
			}
			// the drain runs either way -- latched or merely struck, the stone has tasted -- UNLESS
			// there is nothing in the victim to drink (attach-but-no-leech ruling): the stone still
			// rides out its rounds and withers dry at the end
			if (!Starved)
				victim.ApplyEffect(new LifeDrain(duration, 10, DrainDie, drainer, false), drainer ?? ParentObject);
		}

		private void Unlatch(bool crumble)
		{
			bool surrogate = ParentObject.GetIntProperty("Cleo_TerraFirma_MimicSurrogate") > 0;
			Helpers.VerifyLog("DRAIN", crumble ? $"unlatch: duration spent -> {(Starved ? "dry crumble (starved)" : "gorged burst")}" : $"unlatch: came off early (death/dismemberment) -> curse lifts, {(surrogate ? "mimic surrogate OBLITERATED (no minted siphonyx -- ruling 2026-08-01)" : "stone survives as loot")}");
			RoundsLeft = 0;
			Drainer = null;
			if (ParentObject.HasPart<Cursed>())
				ParentObject.RemovePart<Cursed>();
			if (crumble)
			{
				// distinct endings (RULED 2026-07-23, phrases from the user's own ruling): a fed stone
				// goes out gorged; one latched to lifeless stone withers dry. Both DRAFT for the text pass.
				if (IComponent<GameObject>.Visible(ParentObject))
					IComponent<GameObject>.AddPlayerMessage(Starved
						? ParentObject.Does("wither") + " and crumble away, dry and unfed."
						: ParentObject.Does("burst") + " like a blood balloon, finally sated.");
				ParentObject.Destroy(null, Silent: true);
			}
			else if (surrogate)
			{
				// the strange-roll rider (RULED 2026-08-01, "b works"): the ride was real, the rock
				// never was -- an early unlatch must not mint a permanent siphonyx
				ParentObject.Obliterate();
			}
		}

		public override bool WantTurnTick()
		{
			return RoundsLeft > 0;
		}

		public override void TurnTick(long TimeTick, int Amount)
		{
			if (RoundsLeft <= 0)
				return;
			if (ParentObject.Equipped == null)
			{
				// came off early (victim died or lost the limb): the curse lifts, the stone survives
				Unlatch(crumble: false);
				return;
			}
			RoundsLeft -= Amount;
			if (RoundsLeft <= 0)
				Unlatch(crumble: true);
		}
	}
}
