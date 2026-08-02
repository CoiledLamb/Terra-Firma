using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.Core;
using XRL.Rules;
using XRL.World.Effects; // Stun (the wave's crowd control)
using XRL.World.Parts;

namespace XRL.World.Parts.Mutation
{
	/// <summary>
	/// Fissure, rebuilt 2026-08-02 (THE REDOUBT PIVOT, ruled same day): a wave of earth radiates
	/// out from the caster -- damaging, pushing, and stunning what it moves -- and deposits a
	/// rough NON-EXPIRING ring of broken stone where it dissipates. The wave CARVES natural
	/// stone (level-scaled depth; constructed walls stop it: the World Engine's bones don't
	/// answer), and carved mass thickens the ring. Items are never destroyed and never moved
	/// (loot rule, ruled). Numbers per design/redoubt-numbers-draft.md -- every constant here
	/// is a feel-session dial. Fissure v2 (grenade catalog) preserved at tag fissure-v2-grenade.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_Fissure : BaseMutation
	{
		#region Fields
		private const string COMMAND_ID = "Cleo_TerraFirma_CommandFissure";
		private const int MAX_RING_RADIUS = 5;
		// FEEL ROUND 1 (user, 2026-08-02): "walls feel too solid at lower levels - need more gaps"
		// -- fixed 3 became a falling ladder; see GetGapCount. Crack-gates commented out same round.
		// private const int CRACK_GATE_CHANCE = 50; // % per gap: the doors made of dice (BETA-HELD, feel round 1)
		// Calcify's verified "large stone impact" -- the wave's slam
		private const string SLAM_SOUND = "Sounds/Throw/sfx_throwing_stone_large_impact";
		private const string DEBRIS_BLUEPRINT = "Cleo_TerraFirma_Rock_Generic";
		private const string RUBBLE_BLUEPRINT = "Cleo_TerraFirma_Rock_Rubble";
		private const string CRUMBLED_BLUEPRINT = "Cleo_TerraFirma_Rock_CrumbledWall";
		private const string WALL_BLUEPRINT = "Cleo_TerraFirma_Rock_Wall";
		private const string CRACK_BLUEPRINT = "Cleo_TerraFirma_Rock_Crack";
		private const string FIGURE_BLUEPRINT = "Cleo_TerraFirma_RedoubtFigure";
		private const string SALVAGE_TABLE = "Cleo_TerraFirma_RedoubtSalvage";
		// the Stoneshaper merges already classified every wall: this part = natural stone (carvable),
		// its absence = constructed/metal (the wave breaks against it) -- connection doctrine, ruled
		private const string CARVABLE_PART = "Cleo_TerraFirma_StoneshaperWallProperties";
		private static readonly string[] SMOKE_COLORS = { "&K", "&y", "&w" };
		#endregion

		#region Basic data
		public override string GetDescription()
		{
			// DRAFT (redoubt pivot 2026-08-02): ships for the beta under the standing
			// beta-draft-text ruling; the vigilant signoff rides the release voice sitting.
			return "You drive a wave through the earth that hurls your surroundings outward and raises a ring of broken stone.";
		}

		public override string GetLevelText(int Level)
		{
			// DRAFT (redoubt pivot 2026-08-02), same beta-draft status as GetDescription.
			return "Ring radius: {{rules|" + GetRingRadius(Level) + "}} squares\n" +
				"Damage: {{rules|" + GetWaveDamage(Level) + "}}\n" +
				"Creatures are pushed outward; those moved must save or be stunned for {{rules|" + GetStunRounds(Level) + "}} " + (GetStunRounds(Level) == 1 ? "round" : "rounds") + ".\n" +
				"The wave carves through {{rules|" + GetCarveDepth(Level) + "}} " + (GetCarveDepth(Level) == 1 ? "square" : "squares") + " of natural stone.\n" +
				"The ring of stone persists.\n" +
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
			stats.Set("WaveDamage", GetWaveDamage(Level));
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), GetCooldown(Level));
		}
		#endregion

		#region Scaling (all values = design/redoubt-numbers-draft.md dials)
		/// <summary>Level band 1-4: L1-3 / L4-6 / L7-9 / L10+.</summary>
		public static int GetBand(int Lv) => Math.Min(4, 1 + (Lv - 1) / 3);

		public static int GetRingRadius(int Lv) => Math.Min(MAX_RING_RADIUS, 1 + GetBand(Lv));

		/// <summary>Gaps in the rim, FEEL ROUND 1 (user, 2026-08-02): more at low levels -- 6/5/4/3
		/// by band. The ruled proportional-completeness now runs on both axes: circumference grows
		/// AND gap count falls as you level.</summary>
		public static int GetGapCount(int Lv) => 7 - GetBand(Lv);

		/// <summary>FEEL ROUND 1 (user): "the ring feels a bit too square" -- the disk and rim are
		/// Euclidean circles now, not Chebyshev squares. Integer tests: disk = d2 &lt;= r²+r
		/// ((r+0.5)² rounded), ring = r²-r &lt; d2 &lt;= r²+r.</summary>
		private static bool InDisk(int dx, int dy, int r) => dx * dx + dy * dy <= r * r + r;

		private static bool OnRing(int dx, int dy, int r)
		{
			int d2 = dx * dx + dy * dy;
			return d2 > r * r - r && d2 <= r * r + r;
		}

		/// <summary>Cells of natural stone the wave chews through along any path; full radius at band 4.</summary>
		public static int GetCarveDepth(int Lv) => GetBand(Lv) >= 4 ? GetRingRadius(Lv) : GetBand(Lv);

		public static string GetWaveDamage(int Lv)
		{
			switch (GetBand(Lv))
			{
				case 1: return "1d3+1";
				case 2: return "1d3+2";
				case 3: return "1d3+3";
				default: return "1d3+5";
			}
		}

		/// <summary>1/2/2/3 by band (draft table said 1-2 at band 2; deterministic reads cleaner in the level text -- dial).</summary>
		public static int GetStunRounds(int Lv)
		{
			switch (GetBand(Lv))
			{
				case 1: return 1;
				case 2:
				case 3: return 2;
				default: return 3;
			}
		}

		public static int GetStunSaveDC(int Lv) => 16 + Lv;

		/// <summary>Engine push force: weight-vs-force does the thunderdome sort natively (heavy
		/// creatures barely move; whatever stands its ground is locked in with you).</summary>
		public static int GetPushForce(int Lv) => (2 + Lv) * 1000;

		// 100 FLAT (precedent dig 2026-08-02): exact Force Wall/Bubble parity -- the ruled
		// analogues -- and 2x Stunning Force for "Stunning Force plus a permanent fortress".
		// The product never expires, so level must not also buy frequency (no double-dip).
		public static int GetCooldown(int Lv) => 100;

		/// <summary>Keeper-salvage rolls per cast ("additional small spawns", ruled): 1 roll at bands 1-2, 2 at bands 3-4, each 50%.</summary>
		public static int GetSalvageRolls(int Lv) => GetBand(Lv) >= 3 ? 2 : 1;

		/// <summary>% chance a gap holds a geyser (the door that shoots back): 0/10/15/20 by band.</summary>
		public static int GetGeyserChance(int Lv)
		{
			switch (GetBand(Lv))
			{
				case 1: return 0;
				case 2: return 10;
				case 3: return 15;
				default: return 20;
			}
		}

		/// <summary>Omnigeyser tier by band (2->1, 3->2, 4->3).</summary>
		public static string GetGeyserBlueprint(int Lv) => "Cleo_TerraFirma_Rock_Omnigeyser" + Math.Max(1, GetBand(Lv) - 1);

		/// <summary>% chance the rim raises a stone figure (USER RULED into v1: "figurine can come
		/// in first pass"): 0/2/4/6 by band, at most one per cast. A figure is an event, not a fixture.</summary>
		public static int GetFigureChance(int Lv)
		{
			switch (GetBand(Lv))
			{
				case 1: return 0;
				case 2: return 2;
				case 3: return 4;
				default: return 6;
			}
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
			// a self-centered wave: worth casting when the enemy is inside or at the rim
			if (E.Distance <= GetRingRadius(Level) && IsMyActivatedAbilityAIUsable(ActivatedAbilityID))
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
			Zone zone = origin?.ParentZone;
			if (zone == null)
				return false;
			int radius = GetRingRadius(Level);
			int carveDepth = GetCarveDepth(Level);
			UseEnergy(1000, "Physical Mutation Fissure");
			CooldownMyActivatedAbility(ActivatedAbilityID, GetCooldown(Level));
			ParentObject.PlayWorldSound(SLAM_SOUND, 1f, Combat: true);
			XDidY(ParentObject, "drive", "a wave through the earth", "!", ColorAsGoodFor: ParentObject);
			Helpers.VerifyLog("FIS", $"REDOUBT cast by {ParentObject.Blueprint} Lv{Level}: radius={radius}, carveDepth={carveDepth}, damage={GetWaveDamage(Level)}, stun={GetStunRounds(Level)}r DC{GetStunSaveDC(Level)}");

			// ---- reach + carve: ray from origin to every disk cell; along any ray the wave
			// penetrates at most carveDepth natural-stone cells and stops dead at the first
			// constructed wall. Cells beyond the stop are unreached (no push, no ring there).
			var reached = new HashSet<Cell>();
			var carved = new HashSet<Cell>();
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dy = -radius; dy <= radius; dy++)
				{
					if (dx == 0 && dy == 0)
						continue;
					if (!InDisk(dx, dy, radius)) // Euclidean circle, not Chebyshev square (feel round 1)
						continue;
					Cell target = zone.GetCell(origin.X + dx, origin.Y + dy);
					if (target == null || reached.Contains(target))
						continue;
					List<Point> ray = Zone.Line(origin.X, origin.Y, target.X, target.Y);
					int wallsBitten = 0;
					bool blocked = false;
					var rayCarves = new List<Cell>();
					for (int i = 1; i < ray.Count; i++) // skip the origin point
					{
						Cell step = zone.GetCell(ray[i].X, ray[i].Y);
						if (step == null) { blocked = true; break; }
						if (step.IsSolid() && !carved.Contains(step))
						{
							GameObject wall = GetSolidObject(step);
							if (wall == null || !wall.HasPart(CARVABLE_PART) || wallsBitten >= carveDepth)
							{
								blocked = true; // the stone doesn't answer, or the bite is spent
								break;
							}
							wallsBitten++;
							rayCarves.Add(step);
						}
					}
					if (blocked)
						continue;
					foreach (Cell c in rayCarves)
						carved.Add(c);
					// every cell along a clean ray is reached (the wave passes through it)
					for (int i = 1; i < ray.Count; i++)
					{
						Cell step = zone.GetCell(ray[i].X, ray[i].Y);
						if (step != null)
							reached.Add(step);
					}
				}
			}
			// physically carve: clean removal, no drops -- the mass reappears as ring fabric
			int carvedMass = 0;
			foreach (Cell c in carved)
			{
				GameObject wall = GetSolidObject(c);
				if (wall != null)
				{
					Helpers.VerifyLog("FIS", $"CARVE @({c.X},{c.Y}): {wall.Blueprint}");
					wall.Obliterate();
					carvedMass++;
					SmokePuff(c);
				}
			}

			// ---- the wave: damage + push + stun-if-moved, working outward so inner creatures
			// vacate before outer ones are considered. Items are untouched (loot rule, ruled).
			var waveVictims = new List<GameObject>();
			foreach (Cell c in reached)
			{
				foreach (GameObject o in c.GetObjectsWithPart("Combat"))
				{
					if (o != ParentObject && o.PhaseMatches(ParentObject) && !waveVictims.Contains(o))
						waveVictims.Add(o);
				}
			}
			waveVictims.Sort((a, b) => ChebyshevDistance(origin, a.CurrentCell).CompareTo(ChebyshevDistance(origin, b.CurrentCell)));
			foreach (GameObject victim in waveVictims)
			{
				if (!GameObject.Validate(victim) || victim.CurrentCell == null)
					continue;
				if (victim.IsFlying)
				{
					// a ground wave: the airborne feel only the dust (dial -- flag if it grates)
					Helpers.VerifyLog("FIS", $"wave spares {victim.Blueprint} (flying)");
					continue;
				}
				int dmg = GetWaveDamage(Level).RollCached();
				victim.TakeDamage(dmg, "from the heaving earth!", "Crushing",
					"You were battered by a wave of earth.",
					victim.It + victim.GetVerb("were", true, true) + " @@battered by a wave of earth.",
					Attacker: ParentObject, Owner: ParentObject, Source: ParentObject);
				if (!GameObject.Validate(victim) || victim.CurrentCell == null)
					continue;
				Cell before = victim.CurrentCell;
				string dir = origin.GetDirectionFromCell(before);
				victim.Push(dir, GetPushForce(Level), radius + 1);
				bool moved = victim.CurrentCell != before;
				Helpers.VerifyLog("FIS", $"wave hits {victim.Blueprint}: {dmg} dmg, push {dir} {(moved ? "MOVED" : "HELD GROUND")}");
				// kept your feet = kept your wits: only the moved risk the stun (ruled riff, drafted)
				if (moved && victim.IsValid() && victim.HasPart("Combat")
					&& !victim.MakeSave("Toughness", GetStunSaveDC(Level), null, null, "Fissure Stun", IgnoreNaturals: false, IgnoreNatural1: false, IgnoreNatural20: false, IgnoreGodmode: false, ParentObject))
				{
					victim.ApplyEffect(new Stun(GetStunRounds(Level), GetStunSaveDC(Level)));
					Helpers.VerifyLog("FIS", $"wave stuns {victim.Blueprint} for {GetStunRounds(Level)} round(s)");
				}
			}

			// ---- the ring: fabric on every reached, open rim cell, minus the gaps
			var rim = new List<Cell>();
			foreach (Cell c in reached)
			{
				if (OnRing(c.X - origin.X, c.Y - origin.Y, radius) && !c.IsSolid())
					rim.Add(c);
			}
			var gaps = new List<Cell>();
			for (int g = 0; g < GetGapCount(Level) && rim.Count > 0; g++)
			{
				int pick = Stat.Random(0, rim.Count - 1);
				gaps.Add(rim[pick]);
				rim.RemoveAt(pick);
			}
			// carved mass thickens the ring: each carved wall banks one upgrade credit
			// (rubble -> crumbled wall -> fallen wall) spent as the rim deposits
			int upgradeCredits = carvedMass;
			int deposited = 0, debris = 0;
			foreach (Cell c in rim)
			{
				if (StillHoldsCombatant(c))
				{
					// no burying the thunderdome's audience: loose stone instead of fabric
					c.AddObject(GameObject.Create(DEBRIS_BLUEPRINT));
					debris++;
					continue;
				}
				string fabric = RollFabric(Level);
				while (upgradeCredits > 0 && fabric != WALL_BLUEPRINT)
				{
					fabric = fabric == RUBBLE_BLUEPRINT ? CRUMBLED_BLUEPRINT : WALL_BLUEPRINT;
					upgradeCredits--;
				}
				if (fabric == WALL_BLUEPRINT)
					SweepLooseItemsAside(c); // a wall owns its whole tile: loot spills out the side, never vanishes
				GameObject piece = GameObject.Create(fabric);
				StampOwner(piece);
				c.AddObject(piece);
				SmokePuff(c);
				deposited++;
			}
			// gaps: at depth, sometimes the door that shoots back
			bool geyserPlaced = false;
			foreach (Cell gap in gaps)
			{
				if (!geyserPlaced && GetGeyserChance(Level).in100() && !StillHoldsCombatant(gap))
				{
					GameObject geyser = GameObject.Create(GetGeyserBlueprint(Level));
					StampOwner(geyser);
					gap.AddObject(geyser);
					geyserPlaced = true;
					Helpers.VerifyLog("FIS", $"RING geyser in gap @({gap.X},{gap.Y}): {GetGeyserBlueprint(Level)}");
					continue;
				}
				/* BETA-HELD (feel round 1, user 2026-08-02: "we can comment out cracks for now")
				   -- the crack-gate under gaps. Restore with CRACK_GATE_CHANCE.
				if (CRACK_GATE_CHANCE.in100())
				{
					GameObject crack = GameObject.Create(CRACK_BLUEPRINT);
					StampOwner(crack);
					gap.AddObject(crack);
					Helpers.VerifyLog("FIS", $"RING crack-gate in gap @({gap.X},{gap.Y})");
				}
				*/
			}
			// keeper salvage among the rim ("additional small spawns", ruled)
			int salvaged = 0;
			for (int s = 0; s < GetSalvageRolls(Level); s++)
			{
				if (rim.Count > 0 && 50.in100())
				{
					Cell c = rim[Stat.Random(0, rim.Count - 1)];
					string bp = PopulationManager.RollOneFrom(SALVAGE_TABLE)?.Blueprint;
					if (bp != null)
					{
						GameObject rock = GameObject.Create(bp);
						StampOwner(rock);
						c.AddObject(rock);
						salvaged++;
						Helpers.VerifyLog("FIS", $"RING salvage @({c.X},{c.Y}): {bp}");
					}
				}
			}
			// the figure: at most one, rare, an event (USER RULED into v1)
			if (rim.Count > 0 && GetFigureChance(Level).in100())
			{
				Cell c = rim[Stat.Random(0, rim.Count - 1)];
				if (!StillHoldsCombatant(c))
				{
					SweepLooseItemsAside(c);
					GameObject figure = GameObject.Create(FIGURE_BLUEPRINT);
					c.AddObject(figure);
					Helpers.VerifyLog("FIS", $"RING FIGURE raised @({c.X},{c.Y})");
				}
			}
			Helpers.VerifyLog("FIS", $"RING complete: {deposited} fabric, {gaps.Count} gap(s), {debris} debris-for-occupied, {salvaged} salvage, carvedMass={carvedMass} (credits left {upgradeCredits}), figure={(GetFigureChance(Level) > 0 ? "rolled" : "n/a")}");
			return base.HandleEvent(E);
		}
		#endregion

		#region Helpers
		/// <summary>Base fabric mix by band: rubble/crumbled/wall at 70/25/5 -> 55/30/15 -> 40/35/25 -> 30/35/35 (dials).</summary>
		private static string RollFabric(int Lv)
		{
			int roll = Stat.Random(1, 100);
			switch (GetBand(Lv))
			{
				case 1: return roll <= 70 ? RUBBLE_BLUEPRINT : (roll <= 95 ? CRUMBLED_BLUEPRINT : WALL_BLUEPRINT);
				case 2: return roll <= 55 ? RUBBLE_BLUEPRINT : (roll <= 85 ? CRUMBLED_BLUEPRINT : WALL_BLUEPRINT);
				case 3: return roll <= 40 ? RUBBLE_BLUEPRINT : (roll <= 75 ? CRUMBLED_BLUEPRINT : WALL_BLUEPRINT);
				default: return roll <= 30 ? RUBBLE_BLUEPRINT : (roll <= 65 ? CRUMBLED_BLUEPRINT : WALL_BLUEPRINT);
			}
		}

		/// <summary>The first solid, non-creature occupant of a cell -- the wall, for carving purposes.</summary>
		private static GameObject GetSolidObject(Cell C)
		{
			foreach (GameObject o in C.Objects)
			{
				if (o.Physics != null && o.Physics.Solid && !o.HasPart("Combat"))
					return o;
			}
			return null;
		}

		/// <summary>The shaker owns the field (party gate, RULED 2026-07-25): ring-born pieces that
		/// carry the rock part stamp their caster so the redoubt never mines its own party.</summary>
		private void StampOwner(GameObject rock)
		{
			if (rock.GetPart<Cleo_TerraFirma_FallenRockPart>() is Cleo_TerraFirma_FallenRockPart part)
				part.Owner = ParentObject;
		}

		private static int ChebyshevDistance(Cell a, Cell b)
		{
			if (a == null || b == null)
				return int.MaxValue;
			return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
		}

		/// <summary>Loose items (corpses, dropped gear) spill from under a solid deposit into one open
		/// adjacent cell -- the Gravel Whorl's swat-aside recipe; the loot rule forbids burying.</summary>
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
				Helpers.VerifyLog("FIS", $"loot sweep: {loose.Count} loose item(s) left in place beneath the fabric (no open adjacent cell; loot rule forbids burying, so the piece shares the tile)");
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

		/// <summary>Gray shade-glyph particles drifting up -- the dust of the heave.</summary>
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
