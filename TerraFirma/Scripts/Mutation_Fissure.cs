using System;
using System.Collections.Generic;
using System.Threading;
using Cleo.TerraFirma.Scripts;
using ConsoleLib.Console;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
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
		// ANIMATION PASS v2 (user spec 2026-08-03): sprite-slide emergence on the CombatJuice
		// layer -- see Fx_StoneRise.cs, where all tempo/geometry dials now live. The v1
		// buffer-stamp approach (color-block pulse) was rejected at feel review: "transitioning
		// color blocks don't communicate what is actually happening."
		// Calcify's verified "large stone impact" -- the wave's slam
		private const string SLAM_SOUND = "Sounds/Throw/sfx_throwing_stone_large_impact";
		private const string DEBRIS_BLUEPRINT = "Cleo_TerraFirma_Rock_Generic";
		private const string RUBBLE_BLUEPRINT = "Cleo_TerraFirma_Rock_Rubble";
		private const string CRUMBLED_BLUEPRINT = "Cleo_TerraFirma_Rock_CrumbledWall";
		private const string WALL_BLUEPRINT = "Cleo_TerraFirma_Rock_Wall";
		private const string CRACK_BLUEPRINT = "Cleo_TerraFirma_Rock_Crack";
		private const string FIGURE_BLUEPRINT = "Cleo_TerraFirma_RedoubtFigure";
		private const string SALVAGE_TABLE = "Cleo_TerraFirma_RedoubtSalvage";
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

		/// <summary>Penetration bonus of the wave's bite against wall AV (carve-as-attack,
		/// RULED 2026-08-04; Burrowing Claws' wall-pen precedent of 3x level): soft sedimentary
		/// stone (AV 6-10) answers from L1, marble (AV 25) asks for band 3, granite (AV 40)
		/// shrugs at any level a mortal reaches. Draft dial, feel-session re-dial reserved.</summary>
		public static int GetCarvePV(int Lv) => 6 + 3 * Lv;

		/// <summary>% of a wall's MAX hit points torn away per penetration: three pens fell any
		/// wall, one or two leave it standing damaged -- "some walls are leftover".</summary>
		public const int CARVE_PCT_PER_PEN = 34;

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
			// bites at most carveDepth natural-stone cells and stops dead at the first
			// constructed wall OR the first natural wall that shrugs its attack (carve-as-attack,
			// RULED 2026-08-04: the bite is a penetration roll vs the wall's AV, resolved the
			// first time any ray reaches the wall; every later ray reuses the outcome). Cells
			// beyond a stop are unreached (no push, no ring there).
			var reached = new HashSet<Cell>();
			var carved = new HashSet<Cell>();      // walls that FELL -- these feed the ring's mass credits
			var wallFate = new Dictionary<Cell, bool>(); // every attacked wall cell -> did it fall?
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
					for (int i = 1; i < ray.Count; i++) // skip the origin point
					{
						Cell step = zone.GetCell(ray[i].X, ray[i].Y);
						if (step == null) { blocked = true; break; }
						// a wall another ray already resolved: a fallen one still spends THIS
						// ray's bite (unspent budgets must not chain through breaches -- the
						// full-row corridor bug), a standing one blocks outright
						if (wallFate.TryGetValue(step, out bool fell))
						{
							if (!fell || wallsBitten >= carveDepth)
							{
								blocked = true;
								break;
							}
							wallsBitten++;
							continue;
						}
						if (step.IsSolid())
						{
							if (wallsBitten >= carveDepth)
							{
								blocked = true; // the bite is spent
								break;
							}
							GameObject wall = GetSolidObject(step);
							if (wall == null || !IsCarvableStone(wall))
							{
								blocked = true; // the stone doesn't answer
								break;
							}
							bool died = WaveBitesWall(wall, step);
							wallFate[step] = died;
							if (!died)
							{
								blocked = true; // the stone held -- a leftover wall, and the wave breaks on it
								break;
							}
							wallsBitten++;
							carved.Add(step);
						}
					}
					if (blocked)
						continue;
					// every cell along a clean ray is reached (the wave passes through it)
					for (int i = 1; i < ray.Count; i++)
					{
						Cell step = zone.GetCell(ray[i].X, ray[i].Y);
						if (step != null)
							reached.Add(step);
					}
				}
			}
			int carvedMass = carved.Count; // the fallen reappear as ring fabric

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

			// ---- the ring: fabric on every reached, open rim cell, minus the gaps.
			// Deposits are PLANNED here and applied after the animation plays -- the rim must
			// stay visually empty until the eruption beat resolves into stone (arrival, not
			// pre-spoiled architecture).
			var rim = new List<Cell>();
			var rimAll = new List<Cell>(); // full circle incl. gaps-to-be, for the eruption stamp
			foreach (Cell c in reached)
			{
				if (OnRing(c.X - origin.X, c.Y - origin.Y, radius))
				{
					rimAll.Add(c);
					if (!c.IsSolid())
						rim.Add(c);
				}
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
			int wallShare = GetWallShare(Level);
			string dominantPaint = DominantWallPaint(zone, out char paintFg, out char paintDetail, out char paintBg);
			int deposited = 0, debris = 0;
			var deposits = new List<(Cell cell, string bp, float time)>();
			foreach (Cell c in rim)
			{
				if (StillHoldsCombatant(c))
				{
					// no burying the thunderdome's audience: loose stone instead of fabric
					deposits.Add((c, DEBRIS_BLUEPRINT, 0f));
					debris++;
					continue;
				}
				string fabric = RollFabric(wallShare);
				while (upgradeCredits > 0 && fabric != WALL_BLUEPRINT)
				{
					fabric = fabric == RUBBLE_BLUEPRINT ? CRUMBLED_BLUEPRINT : WALL_BLUEPRINT;
					upgradeCredits--;
				}
				deposits.Add((c, fabric, 0f));
				deposited++;
			}
			/* BETA-HELD (feel round 2, user 2026-08-03: "all salvage + baboon and geyser, for
			   now" -- reconsider once the mutation is functioning). The geyser-in-gap and the
			   keeper-salvage rolls, cut together; the figure survives (lore-bearing, not loot).
			   Feel round 1's crack-gate hold rides inside. Restore alongside GetGeyserChance/
			   GetSalvageRolls/SALVAGE_TABLE.
			bool geyserPlaced = false;
			foreach (Cell gap in gaps)
			{
				if (!geyserPlaced && GetGeyserChance(Level).in100() && !StillHoldsCombatant(gap))
				{
					deposits.Add((gap, GetGeyserBlueprint(Level), 0f));
					geyserPlaced = true;
					continue;
				}
				if (CRACK_GATE_CHANCE.in100())
					deposits.Add((gap, CRACK_BLUEPRINT, 0f));
			}
			for (int s = 0; s < GetSalvageRolls(Level); s++)
			{
				if (rim.Count > 0 && 50.in100())
				{
					Cell c = rim[Stat.Random(0, rim.Count - 1)];
					string bp = PopulationManager.RollOneFrom(SALVAGE_TABLE)?.Blueprint;
					if (bp != null)
						deposits.Add((c, bp, 0f));
				}
			}
			*/
			/* BETA-HELD (feel round 2 correction, user 2026-08-03: "i think you're confused
			   about the figurine - that's what i meant by the baboon") -- the stone figure
			   rides the same cut as salvage and geyser after all. Blueprint stays defined;
			   restore alongside GetFigureChance.
			// the figure: at most one, rare, an event -- it REPLACES its cell's fabric rather
			// than stacking a second solid on it
			if (rim.Count > 0 && GetFigureChance(Level).in100())
			{
				Cell c = rim[Stat.Random(0, rim.Count - 1)];
				if (!StillHoldsCombatant(c))
				{
					for (int i = 0; i < deposits.Count; i++)
					{
						if (deposits[i].cell == c)
						{
							deposits[i] = (c, FIGURE_BLUEPRINT, deposits[i].time);
							Helpers.VerifyLog("FIS", $"RING FIGURE raised @({c.X},{c.Y})");
							break;
						}
					}
				}
			}
			*/
			// interior wake scatter (FEEL ROUND 2, user: "spawn some stones or boulders in
			// addition to the ring. breaking up the inside will also make it feel more
			// uniform") -- loose stone in the wave's wake, timed to each shell's passing
			int scattered = 0;
			foreach (Cell c in reached)
			{
				int dx = c.X - origin.X;
				int dy = c.Y - origin.Y;
				int ringIdx = RingIndexOf(dx, dy, radius);
				if (ringIdx == 0 || c.IsSolid() || StillHoldsCombatant(c))
					continue;
				int roll = Stat.Random(1, 100);
				string bp = roll <= 10 ? DEBRIS_BLUEPRINT : (roll <= 13 ? "Cleo_TerraFirma_Rock_Pebbles" : (roll <= 15 ? "MediumBoulder" : null));
				if (bp != null)
				{
					deposits.Add((c, bp, ringIdx * Cleo_TerraFirma_StoneRiseFX.SHELL_SEC + Cleo_TerraFirma_StoneRiseFX.TRAVEL_RISE));
					scattered++;
				}
			}

			// ---- the animation + settle: choreography enqueued on the juice layer, then a
			// frame-pump loop (the rays' own draw-and-sleep idiom) keeps the world rendering
			// live while each piece materializes at its own moment -- no more lump-sum settle
			// with frozen smoke (feel round 2, note 1)
			Cleo_TerraFirma_StoneRiseFX.Schedule schedule = null;
			if (!Cleo_TerraFirma_StoneRiseFX.HarnessMute && zone.IsActive() && origin.IsVisible())
				schedule = Cleo_TerraFirma_StoneRiseFX.Play(origin, radius, reached, gaps, dominantPaint, paintFg, paintDetail, paintBg, OnRing);
			void Place((Cell cell, string bp, float time) plan)
			{
				string bp = plan.bp;
				// walls over water (feel round 3: "the walls look weird when cast over water"):
				// the heave DISPLACES the pool into an adjacent dry cell -- water is currency,
				// never destroyed. If every neighbor is wet or solid, the deep can't be dammed:
				// the wall downgrades to rubble breaking the surface.
				if (bp == WALL_BLUEPRINT)
				{
					GameObject liquid = null;
					foreach (GameObject o in plan.cell.Objects)
					{
						if (o.IsOpenLiquidVolume())
						{
							liquid = o;
							break;
						}
					}
					if (liquid != null)
					{
						Cell dry = null;
						foreach (Cell adj in plan.cell.GetLocalAdjacentCells())
						{
							if (adj.IsSolid())
								continue;
							bool wet = false;
							foreach (GameObject o in adj.Objects)
							{
								if (o.IsOpenLiquidVolume())
								{
									wet = true;
									break;
								}
							}
							if (!wet)
							{
								dry = adj;
								break;
							}
						}
						if (dry != null)
						{
							liquid.RemoveFromContext();
							dry.AddObject(liquid);
							Helpers.VerifyLog("FIS", $"wall over liquid @({plan.cell.X},{plan.cell.Y}): pool displaced to ({dry.X},{dry.Y})");
						}
						else
						{
							bp = RUBBLE_BLUEPRINT;
							Helpers.VerifyLog("FIS", $"wall over liquid @({plan.cell.X},{plan.cell.Y}): no dry neighbor, downgraded to rubble");
						}
					}
				}
				if (bp == WALL_BLUEPRINT || bp == FIGURE_BLUEPRINT)
					SweepLooseItemsAside(plan.cell); // a wall owns its whole tile: loot spills out the side, never vanishes
				GameObject piece = GameObject.Create(bp);
				if (bp == WALL_BLUEPRINT)
				{
					if (!dominantPaint.IsNullOrEmpty())
						piece.SetStringProperty("PaintedWall", dominantPaint); // the ring dresses as the local geology
					// the dressing is COLORS as much as tile (the paint stamp changes shape only):
					// dominant material's own palette, or the gray fallback where nothing answers
					if (piece.Render != null)
					{
						piece.Render.TileColor = "&" + paintFg;
						piece.Render.DetailColor = paintDetail.ToString();
						piece.Render.ColorString = "&" + paintFg + "^" + paintBg;
					}
				}
				StampOwner(piece);
				plan.cell.AddObject(piece);
				// fresh painted-family walls have no tile until the painter runs (ASCII flash
				// through the settle loop -- user flag 2026-08-04, Calcify showed it too):
				// paint on landing; no-ops for the unpainted fabric
				plan.cell.CheckPaintWallsAround(piece);
				// smoke thinned twice (feel round 3, then 2026-08-04 "bit less smoke") --
				// fabric puffs light, wake scatter only sometimes
				bool scatter = bp != WALL_BLUEPRINT && bp != CRUMBLED_BLUEPRINT && bp != RUBBLE_BLUEPRINT;
				if (!scatter)
					SmokePuff(plan.cell, bp == WALL_BLUEPRINT ? 2 : 1);
				else if (25.in100())
					SmokePuff(plan.cell);
			}
			if (schedule != null)
			{
				CombatJuice.cameraShake(0.3f); // the ground complains while the wave runs
				for (int i = 0; i < deposits.Count; i++)
				{
					if (deposits[i].time <= 0f && schedule.SettleTimes.TryGetValue(deposits[i].cell, out float settleAt))
						deposits[i] = (deposits[i].cell, deposits[i].bp, settleAt);
				}
				deposits.Sort((a, b) => a.time.CompareTo(b.time));
				var gapPuffs = new List<(Cell cell, float time)>();
				foreach (KeyValuePair<Cell, float> kv in schedule.GapTimes)
					gapPuffs.Add((kv.Key, kv.Value));
				gapPuffs.Sort((a, b) => a.time.CompareTo(b.time));
				ScreenBuffer buf = ScreenBuffer.GetScrapBuffer1(bLoadFromCurrent: true);
				TextConsole console = Popup._TextConsole;
				float t = 0f;
				int di = 0, gi = 0;
				float end = schedule.Total + 0.08f;
				while (t < end)
				{
					while (di < deposits.Count && deposits[di].time <= t)
						Place(deposits[di++]);
					while (gi < gapPuffs.Count && gapPuffs[gi].time <= t)
						SmokePuff(gapPuffs[gi++].cell); // the sections the earth couldn't hold
					XRLCore.Core.RenderBaseToBuffer(buf);
					console.DrawBuffer(buf);
					Thread.Sleep(40);
					t += 0.04f;
				}
				while (di < deposits.Count)
					Place(deposits[di++]);
				while (gi < gapPuffs.Count)
					SmokePuff(gapPuffs[gi++].cell);
			}
			else
			{
				// unseen cast: no choreography, everything lands at once
				foreach ((Cell cell, string bp, float time) plan in deposits)
					Place(plan);
				foreach (Cell gap in gaps)
					SmokePuff(gap);
			}
			Helpers.VerifyLog("FIS", $"RING complete: {deposited} fabric (wall share {wallShare}%), {gaps.Count} gap(s), {debris} debris-for-occupied, {scattered} wake scatter, carvedMass={carvedMass} (credits left {upgradeCredits}), paint={dominantPaint ?? "none/sandstone"}");
			return base.HandleEvent(E);
		}
		#endregion

		#region Helpers
		/// <summary>Per-cast wall share of the rim fabric, FEEL ROUND 2 (user 2026-08-03:
		/// "probably something like 10 going to 75 (with variance of 5-15 as you progress, so
		/// 75 could be 90 or 60)"): base 10/30/55/75 by band, rolled once per cast with
		/// variance 5/8/12/15.</summary>
		public static int GetWallShare(int Lv)
		{
			int baseShare, variance;
			switch (GetBand(Lv))
			{
				case 1: baseShare = 10; variance = 5; break;
				case 2: baseShare = 30; variance = 8; break;
				case 3: baseShare = 55; variance = 12; break;
				default: baseShare = 75; variance = 15; break;
			}
			return Math.Max(0, Math.Min(100, baseShare + Stat.Random(-variance, variance)));
		}

		/// <summary>Fabric by the cast's rolled wall share; the non-wall remainder splits 55/45 rubble/crumbled.</summary>
		private static string RollFabric(int wallShare)
		{
			int roll = Stat.Random(1, 100);
			if (roll <= wallShare)
				return WALL_BLUEPRINT;
			int rubbleThreshold = wallShare + (100 - wallShare) * 55 / 100;
			return roll <= rubbleThreshold ? RUBBLE_BLUEPRINT : CRUMBLED_BLUEPRINT;
		}

		/// <summary>The zone's dominant painted-wall material (FEEL ROUND 2: the ring dresses
		/// as the local geology), plus that material's tile colors for the emergence sprite
		/// (FEEL ROUND 3: hardcoded sandstone red made rising stone red everywhere). Null
		/// paint = no walls in the zone; deposits keep sandstone, sprite falls back to
		/// wall_rock in earthy ochre.</summary>
		private static string DominantWallPaint(Zone zone, out char fg, out char detail, out char bg)
		{
			// no-geology fallback = neutral GRAY plain rock (limestone's &y^K scheme), not the
			// Rock_Wall blueprint's inherited sandstone red -- feel flag 2026-08-04, a Joppa
			// cast against brinestalk: "my rocks did that yellow thing again"
			fg = 'y';
			detail = 'K';
			bg = 'K'; // dark-gray backing (limestone's fill): solid stone, not a hole; sprite renders opaque either way
			var counts = new Dictionary<string, int>();
			var samples = new Dictionary<string, GameObject>();
			foreach (GameObject obj in zone.GetObjects())
			{
				if (obj.Physics == null || !obj.Physics.Solid)
					continue;
				// only true geology dresses the ring (feel round 3 screenshot: a Joppa-outskirts
				// cast came out brinestalk-orange -- the scan was counting cane fencing and
				// masonry). Same classifier as the carve: the wave wears what it could carve.
				if (!IsCarvableStone(obj))
					continue;
				string paint = obj.GetTagOrStringProperty("PaintedWall");
				if (paint.IsNullOrEmpty())
					continue;
				if (paint.Contains(","))
					paint = paint.Split(',')[0];
				counts[paint] = counts.TryGetValue(paint, out int n) ? n + 1 : 1;
				if (!samples.ContainsKey(paint))
					samples[paint] = obj;
			}
			string best = null;
			int bestN = 0;
			foreach (KeyValuePair<string, int> kv in counts)
			{
				if (kv.Value > bestN)
				{
					best = kv.Key;
					bestN = kv.Value;
				}
			}
			if (best != null && samples.TryGetValue(best, out GameObject sample) && sample.Render != null)
			{
				string tc = sample.Render.TileColor;
				if (tc.IsNullOrEmpty())
					tc = sample.Render.ColorString;
				if (!tc.IsNullOrEmpty())
				{
					int amp = tc.IndexOf('&');
					if (amp >= 0 && amp + 1 < tc.Length)
						fg = tc[amp + 1];
				}
				if (!sample.Render.DetailColor.IsNullOrEmpty())
					detail = sample.Render.DetailColor[0];
				// the material's own background (the '^' half of ColorString), for the sprite's
				// opaque backing -- walls render as solid blocks, the rising stone should too
				string cs = sample.Render.ColorString;
				if (!cs.IsNullOrEmpty())
				{
					int caret = cs.IndexOf('^');
					if (caret >= 0 && caret + 1 < cs.Length)
						bg = cs[caret + 1];
				}
			}
			return best;
		}

		/// <summary>Which travel shell a disk cell belongs to (1..radius-1), or 0 for none.</summary>
		private static int RingIndexOf(int dx, int dy, int radius)
		{
			for (int r = 1; r < radius; r++)
			{
				if (OnRing(dx, dy, r))
					return r;
			}
			return 0;
		}

		/// <summary>The carve gate (corridor ruling 2026-08-02: NATURAL stone only -- constructed,
		/// metal, and ruins walls stop the wave at all levels). The Stoneshaper classification
		/// carries the flag: MaterialType "stone" AND Natural, marked in Merges.xml on the
		/// true-geology family. Part presence alone is NOT the gate -- wood fences, machine
		/// walls, and tomb masonry carry the part too, for statue purposes.</summary>
		private static bool IsCarvableStone(GameObject wall)
		{
			return Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(wall?.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>());
		}

		/// <summary>The wave's bite is an ATTACK, not an edit (RULED 2026-08-04, user: "rather
		/// than having it autonomously destroy walls, could we have it make an attack against
		/// the walls armor? so the end result is similar but some walls are leftover"):
		/// penetration roll vs the wall's AV, each penetration tearing away a share of the
		/// wall's MAX hit points -- percent-of-max is the Burrowing Claws wall grammar, so
		/// granite's five-digit health means granite endurance, not granite bookkeeping. Zero
		/// penetrations: the stone shrugs, no damage, and the wave breaks on it. Returns
		/// whether the wall fell.</summary>
		private bool WaveBitesWall(GameObject wall, Cell c)
		{
			int av = wall.Stat("AV");
			int pv = GetCarvePV(Level);
			int pens = Stat.RollDamagePenetrations(av, pv, pv);
			if (pens <= 0)
			{
				Helpers.VerifyLog("FIS", $"CARVE attack @({c.X},{c.Y}): {wall.Blueprint} AV {av} vs PV {pv} -- no penetration, the stone holds");
				SmokePuff(c, 1); // chip dust off the survivor
				return false;
			}
			int maxHP = wall.GetStat("Hitpoints").BaseValue;
			int dmg = Math.Max(1, maxHP * pens * CARVE_PCT_PER_PEN / 100);
			wall.TakeDamage(dmg, "from the heaving earth!", "Crushing", null, null,
				Attacker: ParentObject, Owner: ParentObject, Source: ParentObject);
			bool died = !GameObject.Validate(wall) || wall.CurrentCell == null || wall.IsInGraveyard();
			Helpers.VerifyLog("FIS", $"CARVE attack @({c.X},{c.Y}): {wall.Blueprint} AV {av} vs PV {pv} -- {pens} pen(s), {dmg} dmg, {(died ? "FELL" : "leftover standing")}");
			SmokePuff(c);
			return died;
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

		/// <summary>Gray shade-glyph particles drifting up -- the dust of the heave. Count
		/// trimmed 5 -> 2 (feel round 3: "a bit too much smoke") -> 1 default (2026-08-04,
		/// "bit less smoke on fissure").</summary>
		private static void SmokePuff(Cell C, int particles = 1)
		{
			C.ParticleBlip("&y*");
			for (int i = 0; i < particles; i++)
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
