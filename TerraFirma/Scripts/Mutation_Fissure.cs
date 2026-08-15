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
	/// Fissure, rebuilt 2026-08-02 (THE REDOUBT PIVOT) and re-waved 2026-08-09 (THE PULSE
	/// PASS, spec design/fissure-pulse-rework-2026-08-08.md, every shape item RULED): the
	/// single-hit wave became THREE PULSES in one action -- pulse t hits everything within
	/// ring t (origin tile included, caster exempt) at attenuating dice, then pushes the hit
	/// one ring outward (clamp ruled). Pulse hits are ATTACKS at PV = Strength mod (the
	/// BC-split fork, ruled; corrected 2026-08-10 -- the engine's base 4 is display-only,
	/// see GetCreaturePV); walls take one carve-as-attack bite per covering pulse
	/// (repetition digs -- the depth ladder retired). The rim rises at radius 3 (re-dialed
	/// from 4 at the first live look, RULED 2026-08-10) as a NON-EXPIRING ring; rim
	/// eviction bounces or slams whoever stands where it erupts.
	/// Items are never destroyed and never moved (loot rule, ruled). Non-wave numbers per
	/// design/redoubt-numbers-draft.md -- every constant is a feel dial. Prior casts
	/// preserved at tags fissure-v2-grenade and fissure-v3-redoubt.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_Fissure : BaseMutation
	{
		#region Fields
		private const string COMMAND_ID = "Cleo_TerraFirma_CommandFissure";
		// REBALANCED 2026-08-06 (user: "instead of fissure range expanding with level, it could
		// get a bit more damage and stun increment"; radius 4 + ladders proposed, user: "sounds
		// fine"): the ring is a FIXED radius at every level -- the scaling budget moved into
		// the damage and stun ladders below. Progression feel rides gap count + wall share.
		// RE-DIALED 4 -> 3 (USER RULED 2026-08-10, first live look at the pulse build: "it
		// looks like the new version is 1 radius too big? ... i think the ring radius should
		// be 3"; supersedes the 08-06 radius-4 freeze): the rim now sits ON pulse 3's outer
		// ring -- rim walls take their single bite from the third pulse itself, and the
		// dissipating front's bite window goes empty (see the front pass), so nothing
		// double-bites and the one-bite rim grammar holds.
		private const int RING_RADIUS = 3;
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
			// USER WORDING, RULED 2026-08-09 (desc pass v3: "what if instead of 'you send a
			// wave' it was 'you send pulses'"): the description carries the pulse flavor --
			// the base game's own split, flavor in the description, mechanics in the level
			// text. Supersedes the 08-06 voice-audit option B ("wave") it was built from;
			// ring identity and mid-register kept.
			return "You send pulses through the earth, raising a ring of broken stone.";
		}

		public override string GetLevelText(int Level)
		{
			// SIGNED OFF 2026-08-09 (desc pass v3, corpus doc
			// design/mutation-level-text-corpus-2026-08-09.md): user picked option C, the bare
			// stat block ("i lean towards C", then "bare C seems fine" over the Stunning
			// Force-style behavior sentence), with the pulse flavor moved into the
			// description. Precedent for the bare block: Phasing/Burgeoning/Temporal Fugue.
			// Ruled silences stay silent: flyer exemption, loot, gap ladder, engulf catch,
			// moved-only stun nuance, pinning, eviction, walls (repetition digs, undocumented).
			return "Ring radius: " + GetRingRadius(Level) + " squares\n" +
				"Damage: {{rules|" + GetPulseDice(Level, 1) + "/" + GetPulseDice(Level, 2) + "/" + GetPulseDice(Level, 3) + "}} in three spreading pulses\n" +
				"Creatures caught in its path are pushed outward and must make a Toughness save or be stunned for {{rules|" + GetStunRounds(Level) + "}} " + (GetStunRounds(Level) == 1 ? "round" : "rounds") + ".\n" +
				"Cooldown: " + GetCooldown(Level) + " rounds";
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
			stats.Set("WaveDamage", GetPulseDice(Level, 1) + "/" + GetPulseDice(Level, 2) + "/" + GetPulseDice(Level, 3));
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), GetCooldown(Level));
		}
		#endregion

		#region Scaling (all values = design/redoubt-numbers-draft.md dials)
		/// <summary>Level band, every 3 levels, UNCAPPED (L20 pass, RULED 2026-08-09: the cap
		/// at 4 froze the fabric dials at L10 by inertia -- rapid advancement reaches L20, and
		/// the ring keeps maturing with it; bands 5/6/7 land at L13/16/19).</summary>
		public static int GetBand(int Lv) => 1 + (Lv - 1) / 3;

		public static int GetRingRadius(int Lv) => RING_RADIUS; // fixed (rebalance 2026-08-06); signature kept for the call sites

		/// <summary>Gaps in the rim, FEEL ROUND 1 (user, 2026-08-02): more at low levels -- 6/5/4/3
		/// by band. The ruled proportional-completeness now runs on both axes: circumference grows
		/// AND gap count falls as you level. L20 pass (RULED 2026-08-09 "probably fine to fully
		/// enclose"): the ladder continues past the old cap -- 2/1/0 at bands 5/6/7 -- so a
		/// rapid-advanced L19+ cast raises a naturally SEALED ring, no recast-refresh required.</summary>
		public static int GetGapCount(int Lv) => Math.Max(0, 7 - GetBand(Lv));

		/// <summary>FEEL ROUND 1 (user): "the ring feels a bit too square" -- the disk and rim are
		/// Euclidean circles now, not Chebyshev squares. Integer tests: disk = d2 &lt;= r²+r
		/// ((r+0.5)² rounded), ring = r²-r &lt; d2 &lt;= r²+r.</summary>
		private static bool InDisk(int dx, int dy, int r) => dx * dx + dy * dy <= r * r + r;

		private static bool OnRing(int dx, int dy, int r)
		{
			int d2 = dx * dx + dy * dy;
			return d2 > r * r - r && d2 <= r * r + r;
		}

		// GetCarveDepth RETIRED (pulse pass, RULED 2026-08-08 "probably replacing, it's more
		// natural - could hit like pickaxes and dig through them with repeated damage"): carve
		// depth stopped being a separate band ladder -- every wall in a pulse's coverage takes
		// one bite, so a ring-1 wall takes up to 3 and the natural bound is the radius itself.

		/// <summary>Penetration bonus of the wave's bite against wall AV (carve-as-attack,
		/// RULED 2026-08-04; Burrowing Claws' wall-pen precedent of 3x level): soft sedimentary
		/// stone (AV 6-10) answers from L1, marble (AV 25) asks for band 3, granite (AV 40)
		/// shrugs at any level a mortal reaches. Draft dial, feel-session re-dial reserved.</summary>
		public static int GetCarvePV(int Lv) => 6 + 3 * Lv;

		/// <summary>% of a wall's MAX hit points torn away per penetration: three pens fell any
		/// wall, one or two leave it standing damaged -- "some walls are leftover".</summary>
		public const int CARVE_PCT_PER_PEN = 34;

		/// <summary>How many pulses one cast throws; pulse t covers everything within ring t.</summary>
		public const int PULSE_COUNT = 3;

		/// <summary>Per-pulse damage dice -- THE STAIRCASE (ladder v5, RULED 2026-08-09 numbers
		/// sitting, superseding the v3 band ladder; user shape: "scaling should be more
		/// gradual as you level it up," one cell per level in front/middle/tail order, die-size
		/// step first ("maybe 3d4 instead of 4d3"), then counts, with stun levels interleaved
		/// ("it could also go to 4d4 after the first stun level, and repeat that pattern");
		/// waypoints ruled: base 3d3/2d3/1d3, tail step at L4, stun at L5, 4d4 at L6).
		/// Cadence: pulse p upgrades at levels 4(s-1)+p+1 -- front L2/6/10/14/18, middle
		/// L3/7/11/15/19, tail L4/8/12/16/20 -- steps ALTERNATE die-size and die-count (L20
		/// pass amendment, see the method body). L10 = 4d5/3d4/2d4 (paper 24.5), L20 = 5d6/
		/// 4d6/3d6 (42); rapid-advanced and Ego-boosted ranks keep climbing the same
		/// staircase. These are PER-PENETRATION rolls (pen sim tables in the spec's v5
		/// section). Cells stay feel dials; pinned-tail variant rejected same sitting ("i
		/// don't like pinned tail").</summary>
		public static string GetPulseDice(int Lv, int pulse)
		{
			// ALTERNATING steps (L20 pass, RULED 2026-08-09; user: "why don't the dice...
			// repeat the pattern of increasing the dice value as well as number of dice?"):
			// each pulse cycles die-size, then die-count -- 3d3 > 3d4 > 4d4 > 4d5 > 5d5 > 5d6
			// -- so "hits HARDER, not just more" recurs all the way up (d5 is engine-legal,
			// six base-game precedents). Same step levels as before: 4(s-1)+p+1.
			int steps = Lv >= pulse + 1 ? (Lv - pulse - 1) / 4 + 1 : 0;
			int count = (4 - pulse) + steps / 2;
			int die = 3 + (steps + 1) / 2;
			return count + "d" + die;
		}

		/// <summary>Creature-hit PV (RULED 2026-08-08 as "4 + Strength mod, the melee-weapon
		/// grammar itself"; CORRECTED 2026-08-10, user-confirmed after the first live churn
		/// one-shot Joppa: the engine's base 4 is DISPLAY-ONLY). Decomp: displayed PV =
		/// mechanical + RuleSettings.VISUAL_PENETRATION_BONUS (4), appended in name-lines and
		/// stat text only -- MeleeWeapon.GetNormalPenetration rolls PenBonus (0 on ordinary
		/// weapons) + capped Str mod, no 4 anywhere mechanical. The old 4+Str was a unit
		/// error: display units fed to the roll, a weapon reading ~11, hence 3-5 guaranteed
		/// pens vs chaff. Mechanical grammar now: Strength mod ALONE; any future user-facing
		/// PV text must add the visual 4 back. The level-scaled bonus stays REJECTED
		/// (2026-08-08 "slippery slope"); the MaxStrengthBonus-style cap stays reserved.</summary>
		public int GetCreaturePV() => ParentObject.StatMod("Strength");

		/// <summary>Stun rounds ride the staircase's stun levels (RULED 2026-08-09: "the stun
		/// should hit at level 5" then the cadence repeats -- L5/9/13...): 1 at L1-4, 2 at
		/// L5-8, 3 at L9-12, 4 at L13+. FLAGGED at the sitting, accepted by adoption: round 4
		/// moves out of base reach (L13 = Ego-boost territory); the staircase is uncapped so
		/// boosted ranks keep climbing. Supersedes the band ladder (was 1/2/3/4 at 1/4/7/10).</summary>
		public static int GetStunRounds(int Lv) => 1 + (Lv - 1) / 4;

		/// <summary>Stun save DC, +2 on every stun level (RULED 2026-08-09: "make sure the
		/// stun isn't as strong as stunning force (via saves)," steps as "the shape of the
		/// upgrade pattern," not a rider; realigned to the stun levels at "why is the save DC
		/// not in level 9?"; the L20 pass removed my cap at the user's prompt, "why doesn't
		/// the DC scale?"): 16/18/20/22/24 at L1/5/9/13/17, uncapped -- each stun level buys
		/// rounds AND save difficulty together, no per-level creep. Anchor (decomp-verified
		/// StunningForce.cs:265): SF is Toughness 15+Level, flat 3 rounds -- DC 25 by L10 and
		/// 35 by L20 vs our 20/24; weaker via saves holds all the way up. The same DC gates
		/// the wall-rider slam save, unchanged.</summary>
		public static int GetStunSaveDC(int Lv) => 16 + 2 * ((Lv - 1) / 4);

		/// <summary>Engine push force: weight-vs-force does the thunderdome sort natively (heavy
		/// creatures barely move; whatever stands its ground is locked in with you).</summary>
		public static int GetPushForce(int Lv) => (2 + Lv) * 1000;

		// 100 FLAT (precedent dig 2026-08-02): exact Force Wall/Bubble parity -- the ruled
		// analogues -- and 2x Stunning Force for "Stunning Force plus a permanent fortress".
		// The product never expires, so level must not also buy frequency (no double-dip).
		public static int GetCooldown(int Lv) => 100;

		/// <summary>Keeper-salvage rolls per cast ("additional small spawns", ruled): 1 roll at bands 1-2, 2 at bands 3-4, each 50%.</summary>
		public static int GetSalvageRolls(int Lv) => GetBand(Lv) >= 3 ? 2 : 1;

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
			if (!base.WantEvent(ID, cascade) && ID != PooledEvent<CommandEvent>.ID && ID != PooledEvent<CheckExistenceSupportEvent>.ID)
				return ID == AIGetOffensiveAbilityListEvent.ID;
			return true;
		}

		public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
		{
			// pulse coverage is rings 1-3 (since the radius-3 re-dial that includes the rim
			// ring itself), and adjacency is the damage mode (3 hits): the worth-casting gate
			// rides the coverage radius, with fringe casts coin-flipped so the AI leans point-blank
			// (CLAUDE PICK at port time, spec's AI note; the flip is a feel dial)
			bool usable = IsMyActivatedAbilityAIUsable(ActivatedAbilityID);
			bool added = false;
			if (usable)
			{
				if (E.Distance <= 1)
					added = true;
				else if (E.Distance <= PULSE_COUNT && Stat.Random(1, 100) <= 50)
					added = true;
				if (added)
					E.Add(COMMAND_ID);
			}
			// gate telemetry (first pulse boot 2026-08-09: fis-npc 0 casts in 20 turns even
			// from inside coverage -- this line answers whether advertisement fires and the
			// failure is downstream, or never fires at all). NPC-only; rides the TF-VERIFY strip.
			if (!ParentObject.IsPlayer())
				Helpers.VerifyLog("FIS", $"AI gate: {ParentObject.Blueprint} dist {E.Distance}, usable={usable} -> {(added ? "ADVERTISED" : "held")}");
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
			int creaturePV = GetCreaturePV();
			UseEnergy(1000, "Physical Mutation Fissure");
			CooldownMyActivatedAbility(ActivatedAbilityID, GetCooldown(Level));
			ParentObject.PlayWorldSound(SLAM_SOUND, 1f, Combat: true);
			XDidY(ParentObject, "drive", "a wave through the earth", "!", ColorAsGoodFor: ParentObject);
			Helpers.VerifyLog("FIS", $"PULSE cast by {ParentObject.Blueprint} Lv{Level}: radius={radius}, dice={GetPulseDice(Level, 1)}/{GetPulseDice(Level, 2)}/{GetPulseDice(Level, 3)}, creaturePV={creaturePV} (Str mod; engine's +4 is display-only), wallPV={GetCarvePV(Level)}, stun={GetStunRounds(Level)}r DC{GetStunSaveDC(Level)}");

			// ---- the three pulses (spec design/fissure-pulse-rework-2026-08-08.md, shape all
			// RULED 2026-08-08/09): pulse t sweeps everything within ring t -- rays from the
			// origin, standing walls block and shadow exactly as before -- biting every standing
			// wall it reaches ONCE (carve-as-attack grammar unchanged; the depth ladder and the
			// per-ray bite budget retired, repetition digs), hitting every creature in coverage
			// at the pulse's dice (attacks at PV 4+Str, the BC-split fork), then pushing the hit
			// one ring outward (clamp ruled; weight-vs-force still decides IF they move).
			var fallen = new HashSet<Cell>();     // wall cells opened this cast (fell or yielded): free passage for every later ray
			var carved = new HashSet<Cell>();     // stone that FELL -- the ring's mass credits
			var ownFelledCells = new HashSet<Cell>(); // the maker's own fabric, recycled this cast
			int ownFelled = 0;
			var lastMovedPulse = new Dictionary<GameObject, int>(); // stun: ONE check per cast, on the last pulse that moved the creature (ruled)
			var hitCounts = new Dictionary<GameObject, int>();

			// one wavefront: rays to every cell within ring `reach`; standing walls whose ring
			// index sits in [biteFloor, biteCeil] take one bite each (per sweep) -- a wall that
			// holds blocks and shadows, a wall that falls opens passage for the rest of the
			// sweep (the front surges through the fresh breach; corridor chaining is geometry-
			// bounded by `reach` now, the retired budget's job). Returns the cells the front
			// passed through; `struckNow` collects the standing walls it broke against.
			HashSet<Cell> Sweep(int reach, int biteFloor, int biteCeil, HashSet<Cell> struckNow)
			{
				var reached = new HashSet<Cell>();
				var held = new HashSet<Cell>(); // walls that shrugged THIS sweep: block without re-rolling
				for (int dx = -reach; dx <= reach; dx++)
				{
					for (int dy = -reach; dy <= reach; dy++)
					{
						if (dx == 0 && dy == 0)
							continue;
						if (!InDisk(dx, dy, reach)) // Euclidean circle, not Chebyshev square (feel round 1)
							continue;
						Cell target = zone.GetCell(origin.X + dx, origin.Y + dy);
						if (target == null || reached.Contains(target))
							continue;
						List<Point> ray = Zone.Line(origin.X, origin.Y, target.X, target.Y);
						bool blocked = false;
						for (int i = 1; i < ray.Count; i++) // skip the origin point
						{
							Cell step = zone.GetCell(ray[i].X, ray[i].Y);
							if (step == null) { blocked = true; break; }
							if (fallen.Contains(step) || ownFelledCells.Contains(step))
								continue; // the breach is open ground for every ray after it
							if (held.Contains(step)) { blocked = true; break; }
							if (!step.IsSolid())
								continue;
							GameObject wall = GetSolidObject(step);
							// own fabric yields (RULED 2026-08-06, the recast-refresh fix): the
							// earth that rose for you kneels for you -- no attack roll, the mass
							// recycled straight into this cast's upgrade credits
							string maker = wall?.GetStringProperty(RING_MAKER_PROP);
							if (!maker.IsNullOrEmpty() && ParentObject.idmatch(maker))
							{
								if (IsCarvableStone(wall))
									carved.Add(step);
								ownFelledCells.Add(step);
								ownFelled++;
								Helpers.VerifyLog("FIS", $"own fabric yields @({step.X},{step.Y}): {wall.Blueprint} recycled");
								SmokePuff(step, 1);
								wall.Obliterate(); // no death drops: the mass moved into the credits
								continue;
							}
							int wallRing = RingOf(step.X - origin.X, step.Y - origin.Y);
							if (wall == null || wallRing < biteFloor || wallRing > biteCeil)
							{
								// solid non-wall, or outside this front's bite window (only the
								// dissipating front carries a window): the wave breaks on it
								held.Add(step);
								struckNow?.Add(step);
								blocked = true;
								break;
							}
							// every wall takes the attack (RULED 2026-08-05: "machinery still gets
							// hurt by the earth, it just doesn't get destroyed"); class handling
							// lives in WaveBitesWall, unchanged
							if (WaveBitesWall(wall, step))
							{
								fallen.Add(step);
								if (IsCarvableStone(wall))
									carved.Add(step); // ring mass stays stone-only
								continue;
							}
							held.Add(step);
							struckNow?.Add(step);
							blocked = true; // the wall held -- leftover standing, and the wave breaks on it
							break;
						}
						if (blocked)
							continue;
						// every cell along a clean ray is reached (the front passes through it)
						for (int i = 1; i < ray.Count; i++)
						{
							Cell step = zone.GetCell(ray[i].X, ray[i].Y);
							if (step != null)
								reached.Add(step);
						}
					}
				}
				return reached;
			}

			for (int pulse = 1; pulse <= PULSE_COUNT; pulse++)
			{
				string dice = GetPulseDice(Level, pulse);
				var struckNow = new HashSet<Cell>();
				HashSet<Cell> coverage = Sweep(pulse, 1, pulse, struckNow);
				// origin tile in coverage all 3 pulses, caster exempt (RULED 2026-08-08
				// "agreed": the engulf catch). An unpushable epicenter occupant takes the
				// DEPLETING sequence (RULED 2026-08-09): the dice belong to the pulse, never
				// the position -- nothing special-cased here, the loop just runs.
				coverage.Add(origin);
				var victims = new List<GameObject>();
				foreach (Cell c in coverage)
				{
					foreach (GameObject o in c.GetObjectsWithPart("Combat"))
					{
						if (o != ParentObject && o.PhaseMatches(ParentObject) && !victims.Contains(o))
							victims.Add(o);
					}
				}
				// outermost first: the churned vacate outward ahead of the shove behind them
				victims.Sort((a, b) => ChebyshevDistance(origin, b.CurrentCell).CompareTo(ChebyshevDistance(origin, a.CurrentCell)));
				Helpers.VerifyLog("FIS", $"pulse {pulse}: coverage {coverage.Count} cell(s), {victims.Count} creature(s), dice {dice}, {struckNow.Count} standing wall(s) struck");
				foreach (GameObject victim in victims)
				{
					if (!GameObject.Validate(victim) || victim.CurrentCell == null)
						continue;
					if (victim.IsFlying)
					{
						// a ground churn: the airborne feel only the dust (dial -- flag if it grates)
						Helpers.VerifyLog("FIS", $"pulse {pulse} spares {victim.Blueprint} (flying)");
						continue;
					}
					// the pulse hit is an ATTACK (mitigation fork RULED 2026-08-08 "i lean
					// option b"): PV 4+Str vs the target's AV, the pulse dice rolled once per
					// penetration. Zero-pen pulses exist -- vs a high-AV brute the cast is
					// control + fortress, which the redoubt draft already embraced.
					int av = victim.Stat("AV");
					int pens = Stat.RollDamagePenetrations(av, creaturePV, creaturePV);
					int dmg = 0;
					for (int p = 0; p < pens; p++)
						dmg += dice.RollCached();
					if (dmg > 0)
					{
						victim.TakeDamage(dmg, "from the heaving earth!", "Crushing",
							"You were battered by a wave of earth.",
							victim.It + victim.GetVerb("were", true, true) + " @@battered by a wave of earth.",
							Attacker: ParentObject, Owner: ParentObject, Source: ParentObject);
					}
					hitCounts[victim] = hitCounts.TryGetValue(victim, out int hc) ? hc + 1 : 1;
					if (!GameObject.Validate(victim) || victim.CurrentCell == null)
					{
						Helpers.VerifyLog("FIS", $"pulse {pulse} hits {victim.Blueprint}: AV {av} vs PV {creaturePV}, {pens} pen(s), {dmg} dmg -- DOWN");
						continue;
					}
					Cell before = victim.CurrentCell;
					string dir = before == origin ? DIRS8[Stat.Random(0, 7)] : origin.GetDirectionFromCell(before);
					// one ring per pulse (clamp RULED 2026-08-08 "i like the clamp more");
					// weight-vs-force does the thunderdome sort on WHETHER it moves
					victim.Push(dir, GetPushForce(Level), 1);
					bool moved = victim.CurrentCell != before;
					string note = moved ? "MOVED" : "HELD GROUND";
					if (moved)
						lastMovedPulse[victim] = pulse;
					else
					{
						// pinning (RULED 2026-08-08 "slamming enemies into walls sounds good"):
						// held in place by standing stone at their back = slammed against it at
						// the current pulse's die, again each remaining pulse they sit in coverage
						Cell behind = before.GetCellFromDirection(dir);
						if (behind != null && behind.IsSolid() && GetSolidObject(behind) != null)
						{
							int slam = dice.RollCached();
							victim.TakeDamage(slam, "from the heaving earth!", "Crushing",
								"You were battered by a wave of earth.",
								victim.It + victim.GetVerb("were", true, true) + " @@battered by a wave of earth.",
								Attacker: ParentObject, Owner: ParentObject, Source: ParentObject);
							note = $"PINNED (slam {slam})";
						}
					}
					Helpers.VerifyLog("FIS", $"pulse {pulse} hits {victim.Blueprint}: AV {av} vs PV {creaturePV}, {pens} pen(s), {dmg} dmg, push {dir} {note}");
				}
				// wall riders (RULED 2026-08-08 "that's fine to me"): the save-or-slam runs PER
				// PULSE for climbers/burrowers on walls this pulse broke against, at this
				// pulse's die -- riders of walls that FELL took the normal pulse instead
				var riders = new List<GameObject>();
				foreach (Cell c in struckNow)
				{
					foreach (GameObject o in c.GetObjectsWithPart("Combat"))
					{
						if (o != ParentObject && o.PhaseMatches(ParentObject) && !riders.Contains(o))
							riders.Add(o);
					}
				}
				foreach (GameObject rider in riders)
				{
					if (!GameObject.Validate(rider) || rider.CurrentCell == null)
						continue;
					if (rider.IsFlying)
					{
						Helpers.VerifyLog("FIS", $"pulse {pulse} spares {rider.Blueprint} (flying, at a wall)");
						continue;
					}
					if (rider.MakeSave("Toughness", GetStunSaveDC(Level), null, null, "Fissure Slam", IgnoreNaturals: false, IgnoreNatural1: false, IgnoreNatural20: false, IgnoreGodmode: false, ParentObject))
					{
						Helpers.VerifyLog("FIS", $"pulse {pulse}: {rider.Blueprint} clings to its wall (made the slam save)");
						continue;
					}
					int slam = dice.RollCached();
					rider.TakeDamage(slam, "from the heaving earth!", "Crushing",
						"You were battered by a wave of earth.",
						rider.It + rider.GetVerb("were", true, true) + " @@battered by a wave of earth.",
						Attacker: ParentObject, Owner: ParentObject, Source: ParentObject);
					Helpers.VerifyLog("FIS", $"pulse {pulse} slams {rider.Blueprint} against its wall: {slam} dmg (failed the slam save)");
				}
			}

			// stun: ONE check per cast (RULED 2026-08-08 "sounds good"), riding the last pulse
			// that moved the creature; kept your feet = kept your wits, the never-moved never
			// roll. Rounds 1/2/3/4 by band, Toughness DC 16+L, all unchanged.
			foreach (KeyValuePair<GameObject, int> kv in lastMovedPulse)
			{
				GameObject victim = kv.Key;
				if (!GameObject.Validate(victim) || victim.CurrentCell == null || !victim.HasPart("Combat"))
					continue;
				if (!victim.MakeSave("Toughness", GetStunSaveDC(Level), null, null, "Fissure Stun", IgnoreNaturals: false, IgnoreNatural1: false, IgnoreNatural20: false, IgnoreGodmode: false, ParentObject))
				{
					victim.ApplyEffect(new Stun(GetStunRounds(Level), GetStunSaveDC(Level)));
					Helpers.VerifyLog("FIS", $"churn stuns {victim.Blueprint} for {GetStunRounds(Level)} round(s) (last moved on pulse {kv.Value})");
				}
			}
			foreach (KeyValuePair<GameObject, int> kv in hitCounts)
				Helpers.VerifyLog("FIS", $"churn total: {kv.Key.Blueprint} took {kv.Value} pulse hit(s)");

			// ---- the dissipating front (spec's OPEN item, CLAUDE PICK at port time -- log
			// says so, flagged for feel): the wave still travels to the rim radius to place the
			// ring (blocked rays shadow it, unchanged). Bite window = only rings the pulses did
			// NOT cover: at radius 3 (re-dial, RULED 2026-08-10) the rim ring IS pulse 3's outer
			// ring, so the window is empty and every rim wall keeps its single pulse-3 bite --
			// a wall that held still blocks and shadows, the old leftover grammar unchanged.
			// (At any radius > PULSE_COUNT the window is [PULSE_COUNT+1, radius], the pre-re-dial
			// behavior exactly.)
			HashSet<Cell> reached = Sweep(radius, PULSE_COUNT + 1, radius, null);
			int carvedMass = carved.Count; // the fallen reappear as ring fabric
			Helpers.VerifyLog("FIS", $"front pass: {reached.Count} cell(s) reached to the rim, carvedMass={carvedMass}, ownFelled={ownFelled}");

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
			// seal on refresh (RULED 2026-08-06, with own-fabric-yields: user picked the sealed
			// variant knowing it allows the closed donut): a recast that recycled at least a
			// gap-quota's worth of its own old fabric has earned a ring with no gaps rolled.
			// Threshold = GetGapCount is the dial; a fresh-ground cast (ownFelled 0) is unchanged.
			bool sealRing = ownFelled >= GetGapCount(Level);
			var gaps = new List<Cell>();
			if (sealRing)
				Helpers.VerifyLog("FIS", $"ring SEALS: {ownFelled} own pieces recycled (quota {GetGapCount(Level)}), no gaps rolled");
			else
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
			string dominantPaint = DominantWallPaint(zone, out string paintAtlas, out string paintExt, out char paintFg, out char paintDetail, out char paintBg);
			int deposited = 0, debris = 0;
			var deposits = new List<(Cell cell, string bp, float time)>();
			foreach (Cell c in rim)
			{
				// rim eviction (RULED 2026-08-08, feel-pass confirm reserved): the fabric
				// ALWAYS rises -- the ring completes -- and whoever stands where it erupts is
				// bounced off the emergence; which way is geometry. Only the both-blocked
				// corridor micro-fallback keeps the old debris downgrade.
				if (StillHoldsCombatant(c) && !TryEvictRimOccupants(c, origin, GetPulseDice(Level, PULSE_COUNT)))
				{
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
					// timed to the beat whose front first covers this ring (pulse r = beat r)
					deposits.Add((c, bp, (ringIdx - 1) * Cleo_TerraFirma_StoneRiseFX.BEAT_SEC + Cleo_TerraFirma_StoneRiseFX.BOB_RISE));
					scattered++;
				}
			}

			// ---- the animation + settle: choreography enqueued on the juice layer, then a
			// frame-pump loop (the rays' own draw-and-sleep idiom) keeps the world rendering
			// live while each piece materializes at its own moment -- no more lump-sum settle
			// with frozen smoke (feel round 2, note 1)
			Cleo_TerraFirma_StoneRiseFX.Schedule schedule = null;
			if (!Cleo_TerraFirma_StoneRiseFX.HarnessMute && zone.IsActive() && origin.IsVisible())
				schedule = Cleo_TerraFirma_StoneRiseFX.Play(origin, radius, reached, gaps, dominantPaint, paintAtlas, paintExt, paintFg, paintDetail, paintBg, OnRing);
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
				plan.cell.AddObject(piece); // AddObject paints natively (CheckPaintWallsAround inside; diagnosed 2026-08-04)
				if (bp == WALL_BLUEPRINT)
				{
					string paintedTile = piece.Render?.Tile;
					Helpers.VerifyLog("FIS", $"wall landed @({plan.cell.X},{plan.cell.Y}): post-paint Render.Tile={(paintedTile.IsNullOrEmpty() ? "NULL (paint did not take)" : paintedTile)}");
				}
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
				int di = 0, gi = 0, si = 0;
				float end = schedule.Total + 0.08f;
				while (t < end)
				{
					// three camera shakes, descending (concept B, RULED 2026-08-09) -- one per
					// ripple beat, fired at the beat's moment instead of the old lump-sum shake
					while (si < schedule.ShakeTimes.Count && schedule.ShakeTimes[si].time <= t)
						CombatJuice.cameraShake(schedule.ShakeTimes[si++].mag);
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
			// one repaint over the finished redoubt: deposits land one at a time through the
			// settle loop, each painted knowing only its predecessors -- landing-order variants
			// linger otherwise (the light-vs-dark mystery, 2026-08-04)
			ZoneManager.PaintWalls(zone, origin.X - radius - 1, origin.Y - radius - 1, origin.X + radius + 1, origin.Y + radius + 1);
			Helpers.VerifyLog("FIS", $"RING complete: {deposited} fabric (wall share {wallShare}%), {gaps.Count} gap(s), {debris} debris-for-occupied, {scattered} wake scatter, carvedMass={carvedMass} (credits left {upgradeCredits}), paint={dominantPaint ?? "none/sandstone"}");
			return base.HandleEvent(E);
		}
		#endregion

		#region Helpers
		/// <summary>Per-cast wall share of the rim fabric, FEEL ROUND 2 (user 2026-08-03:
		/// "probably something like 10 going to 75 (with variance of 5-15 as you progress, so
		/// 75 could be 90 or 60)"): base 10/30/55/75 by band, rolled once per cast with
		/// variance 5/8/12/15. L20 pass (band uncap, RULED 2026-08-09): the ladder continues
		/// 85/92/98 at bands 5/6/7 with tightening variance -- a rapid-advanced rim converges
		/// on solid rampart. Extension rows are CLAUDE dials under the standing delegation.</summary>
		public static int GetWallShare(int Lv)
		{
			int baseShare, variance;
			switch (GetBand(Lv))
			{
				case 1: baseShare = 10; variance = 5; break;
				case 2: baseShare = 30; variance = 8; break;
				case 3: baseShare = 55; variance = 12; break;
				case 4: baseShare = 75; variance = 15; break;
				case 5: baseShare = 85; variance = 10; break;
				case 6: baseShare = 92; variance = 6; break;
				default: baseShare = 98; variance = 2; break;
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
		private static string DominantWallPaint(Zone zone, out string atlas, out string ext, out char fg, out char detail, out char bg)
		{
			atlas = null; // the FX composes with the painter's defaults when these stay null
			ext = null;
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
				atlas = sample.GetTagOrStringProperty("PaintedWallAtlas");
				ext = sample.GetTagOrStringProperty("PaintedWallExtension");
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

		/// <summary>True geology (corridor ruling 2026-08-02; role narrowed 2026-08-05): since the
		/// attack-any-wall ruling this no longer gates who gets ATTACKED -- it gates what counts
		/// as STONE: ring-mass credit and ring dressing draw only from walls with MaterialType
		/// "stone" AND Natural (Merges.xml, the true-geology family). Part presence alone is NOT
		/// the flag -- wood fences, machine walls, and tomb masonry carry the part too, for
		/// statue purposes.</summary>
		private static bool IsCarvableStone(GameObject wall)
		{
			return Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(wall?.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>());
		}

		/// <summary>The wave's material read (RULED 2026-08-05: attack any wall; the Hard
		/// class's never-destroyed clamp REMOVED at the L20 pass, RULED 2026-08-09 --
		/// "potentially fine it destroys all wall types"). The classes survive as bookkeeping,
		/// not protection: natural stone feeds the ring's mass credits and dressing; soft
		/// organics (MaterialType "other": plant, fungus, wood -- DRAFT boundary) and hard
		/// walls (metal, machine, constructed stone, unmarked fulcrete/brick/ruins) fall by
		/// their own AV and hit points but never feed the ring.</summary>
		private enum WaveWallClass { Stone, Soft, Hard }

		private static WaveWallClass ClassifyWaveWall(GameObject wall)
		{
			var props = wall?.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			if (Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(props))
				return WaveWallClass.Stone;
			if (props?.MaterialType == "other")
				return WaveWallClass.Soft;
			return WaveWallClass.Hard;
		}

		/// <summary>The wave's bite is an ATTACK, not an edit (RULED 2026-08-04, user: "rather
		/// than having it autonomously destroy walls, could we have it make an attack against
		/// the walls armor? so the end result is similar but some walls are leftover"):
		/// penetration roll vs the wall's AV, each penetration tearing away a share of the
		/// wall's MAX hit points -- percent-of-max is the Burrowing Claws wall grammar, so
		/// granite's five-digit health means granite endurance, not granite bookkeeping. Zero
		/// penetrations: the stone shrugs, no damage, and the wave breaks on it. HARD-CLASS
		/// CLAMP REMOVED at the L20 pass (RULED 2026-08-09: "i think it's fine if we remove
		/// the 1HP clamp," clarified "potentially fine it destroys all wall types" --
		/// supersedes the 2026-08-05 never-destroyed half of attack-any-wall): every wall
		/// class now stands or falls by its own AV and hit points. The practical boundary
		/// survives as armor -- machine walls (AV ~30+) shrug the wave until roughly L8 and
		/// only crumble reliably deep into rapid-advancement territory. Class still gates
		/// mass credit and dressing (stone only). FLAGGED FOR FEEL: watch machinery casts.
		/// All feedback is dust, never messages (user 2026-08-05: popup spam). Returns
		/// whether the wall fell.</summary>
		private bool WaveBitesWall(GameObject wall, Cell c)
		{
			WaveWallClass cls = ClassifyWaveWall(wall);
			int av = wall.Stat("AV");
			int pv = GetCarvePV(Level);
			int pens = Stat.RollDamagePenetrations(av, pv, pv);
			if (pens <= 0)
			{
				Helpers.VerifyLog("FIS", $"CARVE attack @({c.X},{c.Y}): {wall.Blueprint} [{cls}] AV {av} vs PV {pv} -- no penetration, the wall holds");
				SmokePuff(c, 1); // chip dust off the survivor
				return false;
			}
			int maxHP = wall.GetStat("Hitpoints").BaseValue;
			int dmg = Math.Max(1, maxHP * pens * CARVE_PCT_PER_PEN / 100);
			wall.TakeDamage(dmg, "from the heaving earth!", "Crushing", null, null,
				Attacker: ParentObject, Owner: ParentObject, Source: ParentObject);
			bool died = !GameObject.Validate(wall) || wall.CurrentCell == null || wall.IsInGraveyard();
			Helpers.VerifyLog("FIS", $"CARVE attack @({c.X},{c.Y}): {wall.Blueprint} [{cls}] AV {av} vs PV {pv} -- {pens} pen(s), {dmg} dmg, {(died ? "FELL" : "leftover standing")}");
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

		private const string FUGUE_DEPOSIT_PROP = "Cleo_TerraFirma_FugueRingPiece";
		/// <summary>Every ring deposit remembers its maker's GameObject.ID (recast-refresh ruling
		/// 2026-08-06): a later wave from the same caster fells own fabric freely and recycles the
		/// mass. Fabric placed before this stamp existed has no marker and behaves as world stone.</summary>
		private const string RING_MAKER_PROP = "Cleo_TerraFirma_RingMaker";

		/// <summary>The shaker owns the field (party gate, RULED 2026-07-25): ring-born pieces that
		/// carry the rock part stamp their caster so the redoubt never mines its own party.
		/// Also the fugue tether's attach point (RULED 2026-08-06: "calcify/fissure stone can
		/// disappear when the fugue clone dies"): a temporal copy's deposits ride ExistenceSupport
		/// pointed at the clone, answered by this mutation's CheckExistenceSupportEvent handler --
		/// the engine's inverted contract, where an UNANSWERED check means instant obliteration
		/// (the Calcify tether bug of the same day is the precedent). The marker property scopes
		/// the answer to pieces we tethered.</summary>
		private void StampOwner(GameObject rock)
		{
			rock.SetStringProperty(RING_MAKER_PROP, ParentObject.ID); // recast-refresh: the deposit knows its maker
			if (rock.GetPart<Cleo_TerraFirma_FallenRockPart>() is Cleo_TerraFirma_FallenRockPart part)
				part.Owner = ParentObject;
			if (ParentObject.HasStringProperty("FugueCopy"))
			{
				rock.SetIntProperty(FUGUE_DEPOSIT_PROP, 1);
				XRL.World.Parts.ExistenceSupport support = rock.RequirePart<XRL.World.Parts.ExistenceSupport>();
				support.SupportedBy = ParentObject;
				support.ValidateEveryTurn = true;
			}
		}

		/// <summary>The answering half of the fugue tether: returning false = "supported"
		/// (ForceBubble's idiom). Only pieces this clone's own casts marked ever ask.</summary>
		public override bool HandleEvent(CheckExistenceSupportEvent E)
		{
			if (E.Object != null && E.Object.GetIntProperty(FUGUE_DEPOSIT_PROP) > 0)
				return false;
			return base.HandleEvent(E);
		}

		private static int ChebyshevDistance(Cell a, Cell b)
		{
			if (a == null || b == null)
				return int.MaxValue;
			return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
		}

		/// <summary>Euclidean ring index of an offset (the smallest r whose disk holds it) --
		/// the pulse fronts' distance grammar, same integer tests as InDisk/OnRing.</summary>
		private static int RingOf(int dx, int dy)
		{
			int d2 = dx * dx + dy * dy;
			int r = 0;
			while (d2 > r * r + r)
				r++;
			return r;
		}

		/// <summary>Push direction for a victim sharing the caster's tile (the engulf catch):
		/// the churn has no outward there, so the shove picks a compass point at random.</summary>
		private static readonly string[] DIRS8 = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

		/// <summary>Rim eviction (RULED 2026-08-08, feel-pass confirm reserved): the fabric
		/// ALWAYS rises, and which way the occupant bounces is geometry. Outward open = the
		/// emergence FLINGS them clear of the ring (no damage; a token fling die stays a dial
		/// at 0). Outward blocked = slammed against the blocker (the last pulse's die, the pin
		/// grammar) and deposited INSIDE the ring with the caster -- the almost-escaped thrown
		/// back into the thunderdome. Returns false where the cell could not be cleared (both
		/// faces blocked -- the corridor micro-fallback -- or an occupant out of phase): that
		/// spot keeps the debris downgrade, crushed-but-not-buried.</summary>
		private bool TryEvictRimOccupants(Cell c, Cell origin, string slamDice)
		{
			var occupants = new List<GameObject>();
			foreach (GameObject o in c.Objects)
			{
				if (o.HasPart("Combat") && o.IsValid() && !o.IsInGraveyard())
					occupants.Add(o);
			}
			string dir = origin.GetDirectionFromCell(c);   // outward: origin -> rim, continued
			Cell outCell = c.GetCellFromDirection(dir);
			Cell inCell = c.GetCellFromDirection(c.GetDirectionFromCell(origin)); // inward: back toward the caster
			foreach (GameObject o in occupants)
			{
				if (!GameObject.Validate(o) || o.CurrentCell != c)
					continue;
				if (!o.PhaseMatches(ParentObject))
					return false; // out of phase with the wave: nothing to bounce it with
				if (outCell != null && !outCell.IsSolid())
				{
					o.DirectMoveTo(outCell);
					Helpers.VerifyLog("FIS", $"rim eviction: {o.Blueprint} bounced clear to ({outCell.X},{outCell.Y})");
				}
				else if (inCell != null && !inCell.IsSolid())
				{
					int slam = slamDice.RollCached();
					o.TakeDamage(slam, "from the heaving earth!", "Crushing",
						"You were battered by a wave of earth.",
						o.It + o.GetVerb("were", true, true) + " @@battered by a wave of earth.",
						Attacker: ParentObject, Owner: ParentObject, Source: ParentObject);
					if (GameObject.Validate(o) && o.CurrentCell == c)
						o.DirectMoveTo(inCell);
					Helpers.VerifyLog("FIS", $"rim eviction: {o.Blueprint} slammed for {slam} and deposited inside at ({inCell.X},{inCell.Y})");
				}
				else
				{
					Helpers.VerifyLog("FIS", $"rim eviction: {o.Blueprint} walled on both faces -- the debris downgrade stands");
					return false;
				}
			}
			return !StillHoldsCombatant(c);
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
		/// "bit less smoke on fissure"). Public since 2026-08-05: Calcify and the Stoneshaper
		/// carve borrow it AFTER their reveals finish (RULED 2026-08-05: smoke during either
		/// animation hid what was being revealed; 1-4 puffs at the end instead).</summary>
		public static void SmokePuff(Cell C, int particles = 1)
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
