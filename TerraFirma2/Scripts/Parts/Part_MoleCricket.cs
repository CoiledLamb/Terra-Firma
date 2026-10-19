using System;
using System.Collections.Generic;
using XRL.Rules;

namespace XRL.World.Parts
{
	/// <summary>
	/// The symbiotic mole cricket (description USER TEXT FINAL 2026-08-22; replaces the Fresh-air
	/// trio's attendant chisel, user reflavor ruling same day). Floating Nearby live animal on the
	/// Symbiotic Firefly chassis, DestroyOnUnequip INCLUDED by ruling ("yes it inherits destroy on
	/// unequip" - one companion, one goodbye, the firefly law). Behavior per the user's shape:
	/// one roll per turn while worn - "sometimes attacks for a small dice roll (thinking something
	/// like 1d2)" an adjacent hostile (peck, damage attributed to the wearer, the Quills idiom, so
	/// XP and aggro stay sane), else "sometimes digs a wall": chip damage to an adjacent NATURAL
	/// stone wall (Cleo_TerraFirma_StoneshaperWallProperties Natural=true, the carve gate's own
	/// marker - the cricket scrapes earth, not blast doors). The *maybe* etch-a-phrase rider
	/// stays OPEN (user's maybe twice running; needs the AttemptEtch commit-path refactor from
	/// the idle-etch thread). Proc chances and dig damage are CLAUDE DIALS, unruled.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_MoleCricket : IPart
	{
		/// <summary>Chance in 100 per turn to peck an adjacent hostile. DIAL.</summary>
		public int PeckChance = 35;

		/// <summary>Peck damage: the user's own number ("thinking something like 1d2").</summary>
		public string PeckDamage = "1d2";

		/// <summary>Chance in 100 per turn to scrape an adjacent natural wall when no peck. DIAL.
		/// IDLE-GATED (RULED 2026-08-22, "idle gating works i think"): digs only while the wearer
		/// is standing still with no target - "scrapes IDLY at the earth", the description's own
		/// word. Walking leaves no smear of shallow wall damage; digging concentrates where the
		/// wearer lingers, so a campsite divot is chosen, not accidental.</summary>
		public int DigChance = 25;

		/// <summary>Wearer's position last turn, for the stationary half of the idle gate.</summary>
		public string LastWearerPos;

		/// <summary>The wall the cricket has committed to (user live-catch 2026-08-22: first-found
		/// cell iteration made it "always prefer upper-left" - the pick is now RANDOM among
		/// adjacent candidates, then loyal to that wall until it dies or is out of reach; the
		/// commit is what concentrates a campsite divot instead of smearing eight faces).</summary>
		public GameObject DigTarget;

		/// <summary>Chip damage per scrape against the wall. DIAL.</summary>
		public string DigDamage = "2d4";

		/// <summary>Played at the wall on each scrape: the generic dig foley the mod already uses.
		/// XML-settable dials; [NonSerialized] so the shipped part keeps its saved layout.</summary>
		[NonSerialized] public string DigSound = "Sounds/Foley/fly_generic_dig";
		[NonSerialized] public float DigSoundVolume = 0.5f;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<EndTurnEvent>.ID;
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			GameObject wearer = ParentObject.Equipped;
			if (GameObject.Validate(ref wearer) && wearer.CurrentCell != null)
			{
				// one roll per turn: hostile first, else earth
				GameObject prey = FindAdjacentHostile(wearer);
				if (prey != null && PeckChance.in100())
				{
					int dmg = Math.Max(1, PeckDamage.RollCached());
					prey.TakeDamage(dmg, "from " + ParentObject.t() + "!", "Physical",
						null, null, Owner: wearer, Attacker: wearer, Source: ParentObject); // death reason draft, approved 2026-09-30
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CRICKET", $"pecked {prey.Blueprint} for {dmg}");
				}
				else if (prey == null && IsWearerIdle(wearer) && DigChance.in100())
				{
					if (!GameObject.Validate(ref DigTarget) || !IsAdjacentWall(wearer, DigTarget))
					{
						DigTarget = PickAdjacentNaturalWall(wearer);
					}
					if (DigTarget != null)
					{
						int dmg = Math.Max(1, DigDamage.RollCached());
						if (!DigSound.IsNullOrEmpty())
							DigTarget.CurrentCell?.PlayWorldSound(DigSound, DigSoundVolume);
						DigTarget.TakeDamage(dmg, "from scraping claws.", "Physical",
							null, null, Owner: wearer, Attacker: null, Source: ParentObject, Accidental: true);
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CRICKET", $"scraped {DigTarget.Blueprint} for {dmg}");
					}
				}
			}
			return base.HandleEvent(E);
		}

		/// <summary>Idle = no combat target AND same cell as last turn (position tracked here).</summary>
		private bool IsWearerIdle(GameObject wearer)
		{
			Cell cell = wearer.CurrentCell;
			string pos = cell.ParentZone?.ZoneID + ":" + cell.X + "," + cell.Y;
			bool stationary = pos == LastWearerPos;
			LastWearerPos = pos;
			return stationary && wearer.Target == null;
		}

		private static GameObject FindAdjacentHostile(GameObject wearer)
		{
			foreach (Cell cell in wearer.CurrentCell.GetLocalAdjacentCells())
			{
				GameObject target = cell.GetCombatTarget(wearer);
				if (target != null && target.IsCreature && target.IsHostileTowards(wearer))
				{
					return target;
				}
			}
			return null;
		}

		private static bool IsNaturalWall(GameObject obj)
		{
			Cleo_TerraFirma_StoneshaperWallProperties props = obj.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			return props != null && Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(props);
		}

		private static bool IsAdjacentWall(GameObject wearer, GameObject wall)
		{
			Cell wallCell = wall.CurrentCell;
			return wallCell != null && wearer.CurrentCell != null
				&& wallCell.ParentZone == wearer.CurrentCell.ParentZone
				&& wearer.CurrentCell.GetLocalAdjacentCells().Contains(wallCell)
				&& IsNaturalWall(wall);
		}

		/// <summary>Random pick among ALL adjacent natural walls (first-found was upper-left biased).</summary>
		private static GameObject PickAdjacentNaturalWall(GameObject wearer)
		{
			List<GameObject> candidates = new List<GameObject>();
			foreach (Cell cell in wearer.CurrentCell.GetLocalAdjacentCells())
			{
				foreach (GameObject obj in cell.Objects)
				{
					if (IsNaturalWall(obj))
					{
						candidates.Add(obj);
					}
				}
			}
			if (candidates.Count == 0)
			{
				return null;
			}
			return candidates[Stat.Random(0, candidates.Count - 1)];
		}
	}
}
