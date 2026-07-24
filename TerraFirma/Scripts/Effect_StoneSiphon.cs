using System;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;

namespace XRL.World.Effects
{
	/// <summary>
	/// The siphonyx eaten-raw effect (RULED 2026-07-24, supersedes the round-10 LifeDrain with its
	/// random-drainer fallback: "ideally the drain would just go nowhere, and it'd deal consistent
	/// damage for a while, maybe ending in a burst of healing when the effect expires"). The stone
	/// drinks a flat amount each round into itself -- nobody is fed -- and when the effect runs its
	/// full course, everything it took bursts back into the eater at once. Dying or losing the
	/// effect early returns nothing: the stone keeps what it drank.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneSiphon : Effect
	{
		/// <summary>Damage die per round (RE-NUMBERED 2026-07-24: "1d3 for 10 turns sounds better", was flat 2 for 7).</summary>
		public string DrainDie = "1d3";

		/// <summary>Everything the stone has drunk so far; returned in full at natural expiry.</summary>
		public int TotalDrained;

		public Cleo_TerraFirma_StoneSiphon()
		{
			DisplayName = "{{r|siphoned}}"; // DRAFT name
		}

		public Cleo_TerraFirma_StoneSiphon(int Duration, string DrainDie) : this()
		{
			base.Duration = Duration;
			this.DrainDie = DrainDie;
		}

		public override string GetDetails()
		{
			return "The swallowed stone drinks " + DrainDie + " HP each round. What it takes returns all at once when it is sated."; // DRAFT
		}

		public override bool UseStandardDurationCountdown() => true;

		public override bool WantTurnTick() => true;

		public override void TurnTick(long TimeTick, int Amount)
		{
			GameObject holder = base.Object;
			if (holder == null || Duration <= 0)
				return;
			int drink = 0;
			for (int i = 0; i < Math.Max(1, Amount); i++)
				drink += Math.Max(1, DrainDie.RollCached());
			TotalDrained += drink;
			Helpers.VerifyLog("LITH", $"siphon: {holder.Blueprint} loses {drink} (pooled {TotalDrained}, {Duration} rounds left)");
			holder.TakeDamage(drink, "from the stone siphoning within!", "Physical",
				null, null, Attacker: null, Owner: null, Source: null);
		}

		public override void Remove(GameObject Object)
		{
			// natural expiry only (Duration exhausted): the burst of healing. An early removal --
			// death, cure, dispel -- returns nothing; the stone keeps what it drank.
			if (Duration <= 0 && TotalDrained > 0 && GameObject.Validate(Object))
			{
				Helpers.VerifyLog("LITH", $"siphon burst: {TotalDrained} returned to {Object.Blueprint}");
				Object.Heal(TotalDrained, Message: true, FloatText: true);
				if (Object.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage("The stone bursts, and everything it drank floods back through you!"); // DRAFT
			}
			base.Remove(Object);
		}
	}
}
