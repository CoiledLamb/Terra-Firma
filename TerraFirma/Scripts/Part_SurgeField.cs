using System;
using System.Collections.Generic;
using XRL.Rules;

namespace XRL.World.Parts
{
	/// <summary>
	/// The deep galvena's afterglow: while this field object survives (its blueprint carries a native
	/// Temporary, the force-shard pattern), it arcs electricity into a random combatant in its own or
	/// an adjacent cell each turn. Spawned by the rock's SpawnObject payload, which threads the thrower
	/// in as Owner for kill attribution. Discharge call shape = the mod's Discharge channel
	/// (DischargeOnHit's recipe). TurnTick, not EndTurnEvent: inert zone objects reliably receive turn
	/// ticks (Temporary itself counts down on them — the webs/force shards prove the channel).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_SurgePart : IPart
	{
		/// <summary>Arc count die per turn.</summary>
		public string Voltage = "1d4";

		/// <summary>Damage die per arc.</summary>
		public string DamageRange = "1d4";

		/// <summary>Whose lightning this is (kill credit); set by the spawning rock part.</summary>
		public GameObject Owner;

		public override bool SameAs(IPart p)
		{
			return p is Cleo_TerraFirma_SurgePart o
				&& o.Voltage == Voltage
				&& o.DamageRange == DamageRange
				&& base.SameAs(p);
		}

		public override bool WantTurnTick()
		{
			return true;
		}

		public override void TurnTick(long TimeTick, int Amount)
		{
			Cell C = ParentObject.CurrentCell;
			if (C == null)
				return;
			GameObject.Validate(ref Owner);
			List<GameObject> targets = new List<GameObject>();
			List<Cell> reach = C.GetLocalAdjacentCells();
			reach.Add(C);
			foreach (Cell c2 in reach)
			{
				foreach (GameObject o in c2.Objects)
				{
					if (o != ParentObject && o.HasPart("Combat") && o.PhaseMatches(ParentObject))
						targets.Add(o);
				}
			}
			if (targets.Count > 0)
			{
				GameObject victim = targets[Stat.Random(0, targets.Count - 1)];
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SURGE", $"field arcs at {victim.Blueprint} ({targets.Count} in reach)");
				victim.Discharge(null, Math.Max(1, Voltage.RollCached()), 0, DamageRange, null, Owner ?? ParentObject, ParentObject, victim);
			}
		}
	}
}
