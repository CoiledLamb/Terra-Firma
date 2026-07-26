using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.Core;
using XRL.Rules;
using XRL.World.AI; // AllySummon (the Burgeoning summon recipe)
using XRL.World.Parts;

namespace XRL.World.Parts.Mutation
{
	/// <summary>
	/// Fissure, rebuilt 2026-07-22 (Spec v2): the ceiling comes down in a self-centered ring.
	/// A few rocks fall (crushing what they strike), sometimes spreading outward; what falls
	/// lies where it fell, throwable ammunition and step-on hazard both. No targeting, so
	/// NPC casters work through the same path. See TODO.md + design/fissure-rock-list.md.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_Fissure : BaseMutation
	{
		#region Fields
		private const string COMMAND_ID = "Cleo_TerraFirma_CommandFissure";
		// base spill; +5 per depth step, frozen at SPILL_MAX (depth-quantity ruling 2026-07-23)
		private const int SPILL_CHANCE_BASE = 20;
		private const int SPILL_MAX = 50;
		// Burgeoning's prime-plant batching read geologically (seam ruling 2026-07-23): one stratum
		// dominates each collapse -- this share of falls shed the per-cast seam stone
		private const int SEAM_CHANCE = 50;
		private const int MAX_RING_RADIUS = 4;
		// Calcify's verified "large stone impact" -- the collapse slam
		private const string SLAM_SOUND = "Sounds/Throw/sfx_throwing_stone_large_impact";
		private const string CRUSH_DAMAGE_TAG = "Cleo_TerraFirma_CrushDamage";
		private const string CRUSH_INSTAKILL_TAG = "Cleo_TerraFirma_CrushInstakill";
		private const string DEBRIS_BLUEPRINT = "Cleo_TerraFirma_Rock_Generic";

		/// <summary>Optional per-blueprint override for what a solid fall shatters into (e.g. a stalagmite shatters into its stalactite, not generic rock).</summary>
		private const string DEBRIS_TAG = "Cleo_TerraFirma_DebrisBlueprint";

		private const string STALAGMITE_BLUEPRINT = "Cleo_TerraFirma_Rock_Stalagmite";

		private const string STALACTITE_BLUEPRINT = "Cleo_TerraFirma_Rock_ShatteredStalactite";

		private static readonly string[] SMOKE_COLORS = { "&K", "&y", "&w" };
		#endregion

		#region Basic data
		public override string GetDescription()
		{
			return "You shake the stones of the deep ceiling loose and bring them down around you.";
		}

		public override string GetLevelText(int Level)
		{
			return "The earth above you gives way, and its stones come down around you.\n" +
				"{{rules|" + GetFalls(Level) + "}} rocks fall within {{rules|" + GetRingRadius(Level) + "}} squares of you, crushing whatever they strike for their heft plus {{rules|" + Level + "}}.\n" +
				"The ceiling nearest you falls thickest and hardest.\n" +
				"Each collapse favors one seam of stone; about half the falls shed it.\n" +
				"The collapse sometimes spreads outward. Deeper, it spreads farther.\n" +
				"Deeper strata shed stronger stone.\n" +
				"What falls lies where it fell: take it up and hurl it, or leave it underfoot. Your enemies pay fallen rocks no mind. Pocketed too long, a fissure rock crumbles to plain stone.\n" +
				"Cooldown: {{rules|" + GetCooldown(Level) + "}} rounds";
		}

		public override bool Mutate(GameObject GO, int Level = 1)
		{
			ActivatedAbilityID = AddMyActivatedAbility("Fissure", COMMAND_ID, "Physical Mutations");
			return base.Mutate(GO, Level);
		}

		public override bool Unmutate(GameObject GO)
		{
			RemoveMyActivatedAbility(ref ActivatedAbilityID);
			return base.Unmutate(GO);
		}

		public override void CollectStats(Templates.StatCollector stats, int Level)
		{
			stats.Set("RingRadius", GetRingRadius(Level));
			stats.Set("Falls", GetFalls(Level));
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), GetCooldown(Level));
		}
		#endregion

		#region Scaling
		public static int GetRingRadius(int Lv) => Math.Min(MAX_RING_RADIUS, 1 + (Lv - 1) / 3);

		/// <summary>6/10/16/24 by radius — the disk rework ladder (user sign-off 2026-07-22): with
		/// center-weighted placement this keeps adjacent-cell odds RISING with rank (~75/58/60/62%).</summary>
		public static int GetFalls(int Lv)
		{
			switch (GetRingRadius(Lv))
			{
				case 1: return 6;
				case 2: return 10;
				case 3: return 16;
				default: return 24;
			}
		}

		/// <summary>Burgeoning's tier roll, realigned (RULED 2026-07-23, supersedes bands + primes):
		/// par = ceil(eff/2), spread rand(-1..+2) per fall — half of all rolls land ABOVE par —
		/// clamped to the nine tier tables. Effective level tops at 18 (Lv10 + depth cap 8), so
		/// tier 9 is exactly the deepest strata's par. Primes RETIRED same ruling: they existed to
		/// patch the old tables' downward skew, and the spread skews up instead.</summary>
		public static int RollTier(int EffLevel)
		{
			int tier = (int)Math.Ceiling(EffLevel / 2f) + Stat.Random(-1, 2);
			return Math.Max(1, Math.Min(9, tier));
		}

		public static string GetTierTable(int Tier) => "Cleo_TerraFirma_FissureTier" + Tier;

		/// <summary>One roll of the tier machinery resolved to a blueprint — seam, falls, and spill all draw from here.</summary>
		public static string RollRockBlueprint(int EffLevel)
		{
			return PopulationManager.RollOneFrom(GetTierTable(RollTier(EffLevel)))?.Blueprint;
		}

		// RE-RATIFIED 2026-07-24 (was 105-5L floor 40 = ~4x Burgeoning's max-level 15; user: "2x as
		// slow at max instead"): same opening rhythm, lands at 30 = exactly 2x at Lv10
		public static int GetCooldown(int Lv) => Math.Max(105 - 8 * Lv, 30);

		/// <summary>Deeper casts spread farther (depth-quantity ruling 2026-07-23): filler-only growth,
		/// structurally outward, frozen at SPILL_MAX so the deepest strata don't drown the disk.</summary>
		public static int GetSpillChance(GameObject who) => Math.Min(SPILL_CHANCE_BASE + 5 * GetDepthBonus(who), SPILL_MAX);

		/// <summary>
		/// The deeper the strata, the stronger the stone the ceiling sheds (user ruling 2026-07-22).
		/// Surface is stratum 10 (engine convention: Z > 10 = "the deep underground"); every 5 strata
		/// below adds +1 effective level for tier selection only, capped at +8 — reached at Z50 and
		/// flat below (RULED 2026-07-23; the strata run far deeper than 50, but the ceiling's
		/// generosity plateaus). The mutation's own numbers (falls, radius, cooldown) never change
		/// with depth; spill chance does (see GetSpillChance).
		/// </summary>
		public static int GetDepthBonus(GameObject who)
		{
			Zone zone = who?.CurrentZone;
			if (zone == null || zone.IsWorldMap())
				return 0;
			return Math.Min(Math.Max((zone.Z - 10) / 5, 0), 8);
		}
		#endregion

		#region Events
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != PooledEvent<CommandEvent>.ID)
				return ID == AIGetOffensiveAbilityListEvent.ID;
			return true;
		}

		public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
		{
			// a self-centered collapse: worth casting when the enemy is at or near the ring
			if (E.Distance <= GetRingRadius(Level) + 1 && IsMyActivatedAbilityAIUsable(ActivatedAbilityID))
				E.Add(COMMAND_ID);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command != COMMAND_ID)
				return base.HandleEvent(E);
			if (ParentObject.OnWorldMap())
			{
				E.Actor?.Fail("You cannot do that on the world map.");
				return false;
			}
			Cell origin = ParentObject.CurrentCell;
			if (origin == null)
				return false;
			int radius = GetRingRadius(Level);
			List<Cell> area = GetDiskCells(origin, radius);
			if (area.Count == 0)
			{
				E.Actor?.Fail("There is nowhere for the stone to fall.");
				return false;
			}
			UseEnergy(1000, "Physical Mutation Fissure");
			CooldownMyActivatedAbility(ActivatedAbilityID, GetCooldown(Level));
			ParentObject.PlayWorldSound(SLAM_SOUND, 1f, Combat: true);
			// surface convention (user wording 2026-07-22): under open sky there is no ceiling to bring
			// down -- the earth itself convulses (same Z <= 10 rule as the stalactite->stalagmite swap)
			if ((origin.ParentZone?.Z ?? 10) <= 10)
				IComponent<GameObject>.EmitMessage(ParentObject, "The earth heaves and convulses!", ColorAsGoodFor: ParentObject); // green like the ceiling line (user flag 2026-07-23)
			else
				XDidY(ParentObject, "bring", "the ceiling down", "!", ColorAsGoodFor: ParentObject);
			int effectiveLevel = Level + GetDepthBonus(ParentObject);
			int falls = Math.Min(GetFalls(Level), area.Count);
			int spillChance = GetSpillChance(ParentObject);
			// the seam (RULED 2026-07-23): one stratum dominates each collapse — rolled once, and
			// every fall and spill echo coin-flips between the seam stone and a fresh tier roll.
			// Same-stone falls also stack in the pack: the batching is quality-of-life, not power.
			string seam = RollRockBlueprint(effectiveLevel);
			// seam eligibility (RULED 2026-07-25, with the holographic rock): NoSeam-tagged
			// blueprints never dominate a collapse -- re-roll, and if the table won't cooperate,
			// this cast simply has no seam
			for (int tries = 0; tries < 8 && HasNoSeamTag(seam); tries++)
				seam = RollRockBlueprint(effectiveLevel);
			if (HasNoSeamTag(seam))
				seam = null;
			Helpers.VerifyLog("FIS", $"cast by {ParentObject.Blueprint} Lv{Level} at Z={ParentObject.CurrentZone?.Z}: eff={effectiveLevel} (depth+{GetDepthBonus(ParentObject)}), radius={radius}, falls={falls}, spill={spillChance}%, seam={seam ?? "none"}");
			// living-fall cap (RULED 2026-07-25, user: "capping somewhere like, 3-5 and then
			// letting the rest of the seam roll something of that tier is fair" -- answers the
			// nine-baboon jackpot cast): each collapse sheds at most a handful of living things;
			// once the cap is hit, further living rolls re-roll the tier table for inert stone.
			// The cap itself is rolled 3-5 per cast (Claude reading of the range, DRAFT).
			int livingCap = Stat.Random(3, 5);
			int livingFalls = 0;
			string PickCapped()
			{
				string bp = PickFallBlueprint(seam, effectiveLevel);
				if (!IsLivingBlueprint(bp))
					return bp;
				if (livingFalls < livingCap)
				{
					livingFalls++;
					return bp;
				}
				string was = bp;
				for (int tries = 0; tries < 8 && IsLivingBlueprint(bp); tries++)
					bp = RollRockBlueprint(effectiveLevel);
				if (IsLivingBlueprint(bp))
					bp = DEBRIS_BLUEPRINT; // the tier table would not stop rolling flesh; stone it is
				Helpers.VerifyLog("FIS", $"living cap {livingCap} reached: {was} re-rolled to {bp}");
				return bp;
			}
			for (int i = 0; i < falls; i++)
			{
				Cell landing = TakeWeightedCell(area, origin, radius);
				DropRock(landing, PickCapped(), origin, radius, "fall");
				if (spillChance.in100())
				{
					Cell echo = PickSpillCell(origin, landing);
					if (echo != null)
						DropRock(echo, PickCapped(), origin, radius, "spill"); // one echo, never chained
				}
			}
			return base.HandleEvent(E);
		}
		#endregion

		#region The collapse
		/// <summary>All open cells within Chebyshev distance <paramref name="R"/> of <paramref name="origin"/>,
		/// the origin itself excluded — the collapse never lands on the caster (disk rework 2026-07-22;
		/// the old exactly-R ring left a growing safe hole around the caster at rank 2+).</summary>
		private static List<Cell> GetDiskCells(Cell origin, int R)
		{
			List<Cell> disk = new List<Cell>();
			Zone zone = origin.ParentZone;
			if (zone == null)
				return disk;
			for (int dx = -R; dx <= R; dx++)
			{
				for (int dy = -R; dy <= R; dy++)
				{
					if (dx == 0 && dy == 0)
						continue;
					Cell c = zone.GetCell(origin.X + dx, origin.Y + dy);
					if (c != null && !c.IsSolid())
						disk.Add(c);
				}
			}
			return disk;
		}

		/// <summary>Removes and returns a random cell weighted by closeness-to-epicenter squared — the
		/// ceiling nearest the caster falls thickest (user ruling 2026-07-22). No enemy-seeking: the
		/// earth doesn't aim, it just falls hardest where it was shaken hardest.</summary>
		private static Cell TakeWeightedCell(List<Cell> cells, Cell origin, int R)
		{
			int total = 0;
			for (int i = 0; i < cells.Count; i++)
			{
				int w = R + 1 - ChebyshevDistance(origin, cells[i]);
				total += w * w;
			}
			int roll = Stat.Random(1, Math.Max(1, total));
			for (int i = 0; i < cells.Count; i++)
			{
				int w = R + 1 - ChebyshevDistance(origin, cells[i]);
				roll -= w * w;
				if (roll <= 0)
				{
					Cell picked = cells[i];
					cells.RemoveAt(i);
					return picked;
				}
			}
			Cell last = cells[cells.Count - 1];
			cells.RemoveAt(cells.Count - 1);
			return last;
		}

		/// <summary>An adjacent cell of <paramref name="landing"/> strictly farther from <paramref name="origin"/> — the collapse only ever spreads outward.</summary>
		private static Cell PickSpillCell(Cell origin, Cell landing)
		{
			int landingDist = ChebyshevDistance(origin, landing);
			List<Cell> candidates = new List<Cell>();
			foreach (Cell c in landing.GetAdjacentCells())
			{
				if (c != null && !c.IsSolid() && ChebyshevDistance(origin, c) > landingDist)
					candidates.Add(c);
			}
			if (candidates.Count == 0)
				return null;
			return candidates[Stat.Random(0, candidates.Count - 1)];
		}

		private static int ChebyshevDistance(Cell a, Cell b)
		{
			return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
		}

		/// <summary>The seam stone half the time, a fresh tier roll otherwise. Spill echoes draw here
		/// identically — Burgeoning treats its spread cells the same as its burst cells.</summary>
		private static string PickFallBlueprint(string seam, int effLevel)
		{
			if (seam != null && SEAM_CHANCE.in100())
				return seam;
			return RollRockBlueprint(effLevel);
		}

		/// <summary>NoSeam-tagged blueprints (the holographic rock) never dominate a collapse.</summary>
		private static bool HasNoSeamTag(string bp)
		{
			if (bp == null)
				return false;
			GameObjectBlueprint def = GameObjectFactory.Factory.GetBlueprintIfExists(bp);
			return def != null && def.HasTag("Cleo_TerraFirma_NoSeam");
		}

		/// <summary>A blueprint that would land ALIVE (DropRock's brain-branch) — the living-fall cap counts these.</summary>
		private static bool IsLivingBlueprint(string bp)
		{
			if (bp == null)
				return false;
			GameObjectBlueprint def = GameObjectFactory.Factory.GetBlueprintIfExists(bp);
			return def != null && def.HasPart("Brain");
		}

		/// <summary>One rock (<paramref name="Blueprint"/>, pre-rolled by the seam/tier machinery) falls into <paramref name="C"/>: smoke, crush, then the rock itself (or debris, if something solid failed to kill what it landed on).</summary>
		private void DropRock(Cell C, string Blueprint, Cell origin, int Radius, string FallKind = "?")
		{
			string blueprint = Blueprint;
			if (blueprint == null)
				return;
			// spawn geography (user ruling 2026-07-22): under open sky there is no ceiling to shed stalactites --
			// those rolls raise stalagmites from the floor instead; caves keep the stalactite-heavy mix
			bool surfaceSwap = blueprint == STALACTITE_BLUEPRINT && (C.ParentZone?.Z ?? 10) <= 10;
			if (surfaceSwap)
				blueprint = STALAGMITE_BLUEPRINT;
			Helpers.VerifyLog("FIS", $"{FallKind} fall @({C.X},{C.Y}) d={ChebyshevDistance(origin, C)}: {blueprint}{(surfaceSwap ? " (surface: stalactite->stalagmite)" : "")}");
			GameObject rock = GameObject.Create(blueprint);
			// the shaker owns the field (party gate, RULED 2026-07-25): fissure-born stones stamp
			// their caster so the collapse never mines its own summoner's party
			if (rock.GetPart<Cleo_TerraFirma_FallenRockPart>() is Cleo_TerraFirma_FallenRockPart casterStamp)
				casterStamp.Owner = ParentObject;
			SmokePuff(C);
			C.PlayWorldSound(SLAM_SOUND);
			// crush whatever is beneath
			string crush = rock.GetPropertyOrTag(CRUSH_DAMAGE_TAG);
			int instakill = 0;
			int.TryParse(rock.GetPropertyOrTag(CRUSH_INSTAKILL_TAG), out instakill);
			if (!crush.IsNullOrEmpty())
			{
				List<GameObject> victims = new List<GameObject>();
				foreach (GameObject o in C.Objects)
				{
					if (o.HasPart("Combat") && o.PhaseMatches(rock))
						victims.Add(o); // fliers are NOT spared -- it falls from above
				}
				foreach (GameObject victim in victims)
				{
					if (instakill > 0 && instakill.in100())
					{
						Helpers.VerifyLog("FIS", $"CRUSH INSTAKILL: {blueprint} on {victim.Blueprint} ({instakill}% proc)");
						XDidYToZ(ParentObject, "crush", victim, "beneath falling stone", "!", ColorAsGoodFor: ParentObject, ColorAsBadFor: victim);
						victim.Die(ParentObject, null, "You were crushed beneath falling stone.",
							victim.It + victim.GetVerb("were", true, true) + " @@crushed beneath falling stone.");
					}
					else
					{
						// +Level: the fall itself scales with mastery; +1d6 per step inside the rim —
						// the ceiling directly overhead falls hardest (proximity ruling 2026-07-22)
						int bonus = Level;
						int proximitySteps = Math.Max(0, Radius - ChebyshevDistance(origin, C));
						for (int p = 0; p < proximitySteps; p++)
							bonus += Stat.Random(1, 6);
						Helpers.VerifyLog("FIS", $"crush: {blueprint} on {victim.Blueprint} = {crush} + level {Level} + proximity {proximitySteps}d6 (bonus total {bonus})");
						victim.TakeDamage(crush.RollCached() + bonus, "from falling stone!", "Crushing",
							"You were crushed beneath falling stone.",
							victim.It + victim.GetVerb("were", true, true) + " @@crushed beneath falling stone.",
							Attacker: ParentObject, Owner: ParentObject, Source: ParentObject);
					}
				}
			}
			// a living thing that falls (the ape statue) lands ALIVE: allied to whoever brought the ceiling
			// down, worth no XP -- Burgeoning's summon recipe verbatim; sidesteps into an open cell rather
			// than sharing one with a combatant, and if nowhere will hold it, the earth keeps it
			if (rock.Brain != null)
			{
				Cell home = C;
				if (StillHoldsCombatant(home))
				{
					List<Cell> openAdj = home.GetLocalAdjacentCells()?.FindAll(x => !x.IsSolid() && !StillHoldsCombatant(x));
					home = (openAdj != null && openAdj.Count > 0) ? openAdj[Stat.Random(0, openAdj.Count - 1)] : null;
				}
				if (home == null)
				{
					Helpers.VerifyLog("FIS", $"living fall {blueprint}: nowhere to stand, obliterated");
					rock.Obliterate();
					return;
				}
				rock.Brain.SetAlliedLeader<AllySummon>(ParentObject, 0, Silent: true);
				rock.MakeActive();
				if (rock.HasStat("XPValue"))
					rock.GetStat("XPValue").BaseValue = 0;
				Helpers.VerifyLog("FIS", $"living fall: {blueprint} lands ALIVE @({home.X},{home.Y}), allied to {ParentObject.Blueprint}, XP zeroed{(home != C ? " (sidestepped)" : "")}");
				home.AddObject(rock);
				return;
			}
			// a solid mass that failed to kill what it fell on shatters to debris instead of standing
			bool solid = rock.Physics != null && rock.Physics.Solid;
			if (solid && StillHoldsCombatant(C))
			{
				string debris = rock.GetPropertyOrTag(DEBRIS_TAG) ?? DEBRIS_BLUEPRINT;
				rock.Obliterate();
				int shards = Stat.Random(1, 2);
				Helpers.VerifyLog("FIS", $"solid {blueprint} failed to kill -> shattered into {shards}x {debris}");
				for (int i = 0; i < shards; i++)
					C.AddObject(GameObject.Create(debris));
				return;
			}
			if (solid)
				SweepLooseItemsAside(C); // a wall owns its whole tile (user ruling): loot spills out the side, never vanishes beneath
			Cleo_TerraFirma_FallenRockPart fallenPart = rock.GetPart<Cleo_TerraFirma_FallenRockPart>();
			if (fallenPart != null)
				fallenPart.Owner = ParentObject;
			C.AddObject(rock);
		}

		/// <summary>Loose items (corpses, dropped gear) spill from under a solid fall into one open adjacent
		/// cell — the Gravel Whorl's swat-aside recipe. If nothing adjacent is open, the earth keeps what it takes.</summary>
		private static void SweepLooseItemsAside(Cell C)
		{
			List<GameObject> loose = new List<GameObject>();
			foreach (GameObject o in C.Objects)
			{
				if (o.Physics != null && o.Physics.Takeable && !o.HasPart("Combat"))
					loose.Add(o);
			}
			if (loose.Count == 0)
				return;
			List<Cell> open = C.GetLocalAdjacentCells()?.FindAll(x => !x.IsSolid());
			if (open == null || open.Count == 0)
			{
				Helpers.VerifyLog("FIS", $"loot sweep: {loose.Count} loose item(s) BURIED (no open adjacent cell) -- the earth keeps what it takes");
				return;
			}
			Cell spill = open[Stat.Random(0, open.Count - 1)];
			Helpers.VerifyLog("FIS", $"loot sweep: {loose.Count} loose item(s) swept aside to ({spill.X},{spill.Y})");
			foreach (GameObject o in loose)
			{
				o.RemoveFromContext();
				spill.AddObject(o);
			}
		}

		private static bool StillHoldsCombatant(Cell C)
		{
			foreach (GameObject o in C.Objects)
			{
				if (o.HasPart("Combat") && o.IsValid() && !o.IsInGraveyard())
					return true;
			}
			return false;
		}

		/// <summary>Gray shade-glyph particles drifting up out of the landing cell -- the dust of the fall.</summary>
		private static void SmokePuff(Cell C)
		{
			C.ParticleBlip("&y*");
			for (int i = 0; i < 5; i++)
			{
				string glyph = SMOKE_COLORS[Stat.Random(0, SMOKE_COLORS.Length - 1)] + (char)(176 + Stat.Random(0, 2));
				XRLCore.ParticleManager.AddSinusoidal(glyph, C.X, C.Y,
					1.5f * Stat.Random(1, 4), 0.1f * Stat.Random(1, 60), 0.1f + 0.025f * Stat.Random(0, 4),
					1f, 0f, 0f, -0.15f - 0.05f * Stat.Random(1, 4), 999, 0L);
			}
		}
		#endregion
	}
}
