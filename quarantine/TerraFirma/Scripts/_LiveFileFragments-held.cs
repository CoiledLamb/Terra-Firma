// Held 2026-09-30: fragments cut from live files when their content moved to quarantine/.
// Each block names the file and spot it came from. Not a compilable file.

// ==== Mutation_Fissure.cs, constants region
		// private const int CRACK_GATE_CHANCE = 50; // % per gap: the doors made of dice (BETA-HELD, feel round 1)

// ==== Mutation_Fissure.cs, blueprint constants
		private const string CRACK_BLUEPRINT = "Cleo_TerraFirma_Rock_Crack";
		private const string FIGURE_BLUEPRINT = "Cleo_TerraFirma_RedoubtFigure";
		private const string SALVAGE_TABLE = "Cleo_TerraFirma_RedoubtSalvage";

// ==== Mutation_Fissure.cs, band tables
		/// <summary>Keeper-salvage rolls per cast ("additional small spawns", ruled): 1 roll at bands 1-2, 2 at bands 3-4, each 50%.</summary>
		public static int GetSalvageRolls(int Lv) => GetBand(Lv) >= 3 ? 2 : 1;

// ==== Mutation_Fissure.cs, band tables
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

// ==== Mutation_Fissure.cs, ring deposits (after the rim fabric loop)
			/* BETA-HELD (feel round 2, user 2026-08-03: "all salvage + baboon and geyser, for
			   now" -- reconsider once the mutation is functioning). The keeper-salvage rolls;
			   the figure survives (lore-bearing, not loot). Feel round 1's crack-gate hold rides
			   inside. Restore alongside GetSalvageRolls/SALVAGE_TABLE. (The geyser-in-gap that
			   was held here died for good at the 2026-08-13 geyser cut -- its code and helpers
			   are scrubbed, not held; pre-cut shape at git tag fissure-v2-grenade.)
			foreach (Cell gap in gaps)
			{
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

// ==== Part_FallenRock.cs, payload switch
				case "FallThrough": // crack: the floor gives way beneath them. Flip the dormant StairsDown live,
				                    // let the native pull handle everything (zone below, fall damage, fliers
				                    // exempt), close it again. Dormant at rest = no native avoid-weight
				                    // broadcast, preserving the mine asymmetry.
					if (GameObject.Validate(ref victim) && ParentObject.GetPart<StairsDown>() is StairsDown stairs)
					{
						stairs.PullDown = true;
						try { stairs.CheckPullDown(victim); }
						finally { stairs.PullDown = false; }
					}
					break;

// ==== Part_FallenRock.cs, payload switch (cut 2026-09-30 in 7349e43)
				case "Latch": // drainstone: fastens onto the victim and drinks; amount = rounds (step = half via Reduced)
				{
					// victim identity captured pre-Validate (harness find 2026-07-24: the guard failed
					// on a throw at a live Scrapbot and the old line could not say WHO the payload
					// thought it was aimed at -- NULL = the victimless at-rest/landing path, a
					// blueprint = a real target whose ref went stale)
					string latchVictimDesc = victim == null ? "NULL" : victim.Blueprint;
					if (GameObject.Validate(ref victim) && ParentObject.GetPart<Cleo_TerraFirma_DrainLatch>() is Cleo_TerraFirma_DrainLatch latch)
					{
						Helpers.VerifyLog("DRAIN", $"latch dispatch: victim={latchVictimDesc}, reduced={Reduced}, stoneCell={(ParentObject.CurrentCell == null ? "null" : "set")} -> {(Reduced || ParentObject.CurrentCell != null ? "IMMEDIATE" : "DEFERRED")}");
						if (Reduced || ParentObject.CurrentCell != null)
							latch.Latch(victim, Math.Max(1, amount), source); // at rest (stepped, or a PLACED strange surrogate): latch now
						else
							latch.RequestLatch(victim, Math.Max(1, amount), source); // mid-flight (no cell yet): defer until the engine finishes the throw
					}
					else
						Helpers.VerifyLog("DRAIN", $"latch dispatch FAILED guard: victim={latchVictimDesc}, victimValid={GameObject.Validate(ref victim)}, partPresent={ParentObject.GetPart<Cleo_TerraFirma_DrainLatch>() != null}");
					break;
				}

// ==== Part_FallenRock.cs, SpawnObject's Make() (cut 2026-09-30 in 7349e43)
					// surge fields (deep galvena) get the thrower threaded in for kill attribution
					GameObject Make()
					{
						GameObject o = GameObject.Create(detail);
						if (o.GetPart<Cleo_TerraFirma_SurgePart>() is Cleo_TerraFirma_SurgePart sp)
							sp.Owner = source;
						return o;
					}
