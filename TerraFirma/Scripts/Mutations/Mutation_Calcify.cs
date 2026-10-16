using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleLib.Console;
using UnityEngine;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.Capabilities;

namespace XRL.World.Parts.Mutation
{
	public class Cleo_TerraFirma_Calcify : BaseMutation
	{
		#region Fields
		private const string COMMAND_ID = "Cleo_TerraFirma_CommandCalcify";

		private const int BASE_WALLS_CREATED = 4; // 4 at Lv1, exactly 9 at Lv10 -- Force Wall's count at full investment, in permanent stone

		// RULED 2026-08-06 (rebalance pass, user: "we can try out cool down"): with true-copy walls,
		// the every-level lattice interleaves -- even levels buy +1 wall, odd levels buy the
		// cooldown step. RE-DIALED at the L20 pass (RULED 2026-08-09, user's own shape: "what if
		// the cooldown was -5 each *other* level"): the step halved 10 -> 5, so the 100 -> 50
		// budget spreads across ten odd-level steps and the floor lands EXACTLY at L19 -- every
		// level through 20 buys something, no dead half past the old L9 plateau. Accepted with
		// open eyes: the mid game slows (L9 waits 75 where it waited 50); the fortress comes
		// slower unless you invest past 10. Declining-cooldown precedent: Burgeoning 115-10L,
		// Teleport Other 115-10L, Belcher 210-10L (all floor-clamped).
		private const int COOLDOWN_MAX = 100;
		private const int COOLDOWN_STEP = 5;
		private const int COOLDOWN_FLOOR = 50;
		#endregion

		#region Basic data
		public override string GetDescription()
		{
			// A' PICKED at the 2026-07-26 voice sitting ("A' sounds fine honestly"): Force Wall's
			// concrete register; "out of thin air" appears nowhere in base text. Built 2026-07-27.
			return "You raise walls of solid stone.";
		}

		public override string GetLevelText(int Level)
		{
			// REBALANCED 2026-08-06 (user: "calcify should fully copy the wall it's adjacent to if
			// possible, including description and stats. it should no longer gain HP scaling per
			// level"): the HP line and the 0 AV / -10 DV line retired with the stat overrides --
			// walls now carry their blueprint's own numbers. Replacement lines approved
			// 2026-09-30. Cooldown lands last per the ~100%-of-base convention (2026-07-26 sitting).
			// cooldown scales now (odd levels), so it earns the rules-wrap the flat 90 never had
			return "Creates {{rules|" + GetWallsCreated(Level) + "}} contiguous walls in empty tiles starting adjacent to you.\n" +
				"Cooldown: {{rules|" + GetCooldown(Level) + "}} rounds";
		}

		public override bool Mutate(GameObject GO, int Level = 1)
		{
			ActivatedAbilityID = AddMyActivatedAbility("Calcify", COMMAND_ID, "Physical Mutations");
			return base.Mutate(GO, Level);
		}

		public override bool Unmutate(GameObject GO)
		{
			RemoveMyActivatedAbility(ref ActivatedAbilityID);
			return base.Unmutate(GO);
		}

		public override void CollectStats(Templates.StatCollector stats, int Level)
		{
			stats.Set("WallNum", GetWallsCreated(Level));
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), GetCooldown(Level));
		}
		#endregion

		#region Events
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != AIGetDefensiveAbilityListEvent.ID && ID != AIGetRetreatAbilityListEvent.ID && ID != PooledEvent<CheckExistenceSupportEvent>.ID)
				return ID == PooledEvent<CommandEvent>.ID;
			return true;
		}

		/// <summary>The answering half of the fugue tether (BUG FOUND at the 2026-08-06 residue
		/// eyeball: clone walls vanished INSTANTLY -- the cast attached ExistenceSupport pointed
		/// at the clone, but nothing on the clone ANSWERED CheckExistenceSupportEvent, and an
		/// unanswered check reads as unsupported on the first validation tick). The engine's
		/// contract is inverted stop-propagation: returning FALSE means "supported" -- ForceBubble
		/// is the decomp precedent. Scope is safe by construction: the check only fires at the
		/// creature the wall's ExistenceSupport points to, and only fugue-clone casts point one at
		/// their caster -- so answering for any calcify wall here can never over-claim. The clone
		/// carries this mutation (fugue copies mutations), answers while it exists, and its
		/// obliteration silences the answer: the wall dissipates exactly when the clone does.</summary>
		public override bool HandleEvent(CheckExistenceSupportEvent E)
		{
			if (E.Object?.HasPart<Cleo_TerraFirma_CalcifyWallPart>() == true)
				return false;
			return base.HandleEvent(E);
		}

		// ---- AI advertisement (NPC gap closed 2026-07-23; the quarriers ruling had already
		// committed this work). DEFENSIVE and RETREAT channels only, never offensive: unlike
		// Force Wall, whose owner can shoot out through their own fields, calcified stone is
		// opaque to everyone -- an offensive advertisement would teach NPCs to wall off their
		// own attacks. ----
		public override bool HandleEvent(AIGetDefensiveAbilityListEvent E)
		{
			GameObject threat;
			if (IsMyActivatedAbilityAIUsable(ActivatedAbilityID) && (threat = FindWallThreat(E.Target)) != null)
				E.Add(COMMAND_ID, 1, null, Inv: false, Self: false, threat);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(AIGetRetreatAbilityListEvent E)
		{
			GameObject threat;
			if (IsMyActivatedAbilityAIUsable(ActivatedAbilityID) && (threat = FindWallThreat(E.Target)) != null)
				E.Add(COMMAND_ID, 1, null, Inv: false, Self: false, threat);
			return base.HandleEvent(E);
		}

		/// <summary>The wall-off target: the event's target when valid, else the nearest visible
		/// hostile (Force Wall's retreat-scan recipe -- the retreat event's Target is often null).</summary>
		private GameObject FindWallThreat(GameObject given)
		{
			if (GameObject.Validate(ref given))
				return given;
			GameObject nearest = null;
			int nearestDistance = int.MaxValue;
			ParentObject.CurrentZone?.ForeachObject(delegate(GameObject obj)
			{
				if (obj.IsCreature && obj != ParentObject && ParentObject.IsHostileTowards(obj)
					&& ParentObject.HasLOSTo(obj, IncludeSolid: true, BlackoutStops: false, UseTargetability: true))
				{
					int d = ParentObject.DistanceTo(obj);
					if (d < nearestDistance)
					{
						nearest = obj;
						nearestDistance = d;
					}
				}
			});
			return nearest;
		}

		/// <summary>NPC cell-picking (the other half of the gap): a contiguous line across the
		/// approach -- the cell adjacent to the caster toward the threat, extended along both
		/// orthogonals. Force Wall's perpendicular-line brain, anchored caster-side to honor
		/// Calcify's own walls-start-adjacent-to-you rule. Occupied cells stay in the list;
		/// the placer below skips them (partial lines accepted, same as partial player casts).</summary>
		private List<Cell> PickNpcWallLine(GameObject threat)
		{
			Cell here = ParentObject.CurrentCell;
			Cell there = threat?.CurrentCell;
			if (here == null || there == null || here.ParentZone != there.ParentZone)
				return null;
			string dir = here.GetDirectionFromCell(there);
			// audit fix 2026-08-29: GetCellFromDirection walks GLOBALLY into any live adjacent zone
			// (GetCellFromDirectionGlobal, bLocalOnly false), so an edge cast could place walls next
			// door and then feed the neighbor zone's local X/Y into this zone's repaint rectangle.
			// Clamp the whole line to the caster's zone; a line shortened by the map edge is just a
			// partial line, which the placer already accepts.
			Cell anchor = here.GetCellFromDirection(dir);
			if (anchor == null || anchor.ParentZone != here.ParentZone)
				return null;
			int walls = GetWallsCreated(Level);
			List<Cell> line = new List<Cell>(walls) { anchor };
			string[] flanks = XRL.Rules.Directions.GetOrthogonalDirections(dir);
			Cell left = anchor, right = anchor;
			while (line.Count < walls && (left != null || right != null))
			{
				left = left?.GetCellFromDirection(flanks[0]);
				if (left != null && left.ParentZone != here.ParentZone)
					left = null;
				if (left != null && line.Count < walls)
					line.Add(left);
				right = right?.GetCellFromDirection(flanks[1]);
				if (right != null && right.ParentZone != here.ParentZone)
					right = null;
				if (right != null && line.Count < walls)
					line.Add(right);
			}
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CALC", $"NPC cast: {ParentObject.Blueprint} walls off {threat.Blueprint} ({dir}ward), {line.Count} candidate cells");
			return line;
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command != COMMAND_ID)
				return base.HandleEvent(E);
			List<Cell> targeted = null;
			if (ParentObject.IsPlayer())
				targeted = PickFieldAdjacent(GetWallsCreated(Level), ParentObject);
			else
				targeted = PickNpcWallLine(E.Target ?? ParentObject.Target);
			if (targeted.IsNullOrEmpty())
				return false;
			// the stone call, harmonized with Fissure's carve gate (RULED 2026-08-04: "we should
			// harmonize the stone call as well"): only TRUE GEOLOGY answers. The zone's default
			// wall must itself be natural stone, neighbor borrowing demands the Natural flag,
			// and where nothing natural stands, the shared gray fallback rises.
			string bp = ParentObject.CurrentZone.GetDefaultWall();
			string rawDefault = bp;
			if (bp != null)
			{
				GameObjectBlueprint defaultWall = GameObjectFactory.Factory.GetBlueprintIfExists(bp);
				if (defaultWall == null
					|| defaultWall.GetPartParameter<string>("Cleo_TerraFirma_StoneshaperWallProperties", "MaterialType") != "stone"
					|| !defaultWall.GetPartParameter("Cleo_TerraFirma_StoneshaperWallProperties", "Natural", false))
					bp = null;
			}
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CALC", $"stone call: zone default wall={rawDefault ?? "none"}, vet {(bp == null ? "REFUSED (not natural stone) -- fallback/borrow decides" : "passed")}");
			// plan first, place after the rise (VFX pass 2026-08-04, user-confirmed: the
			// Fissure emergence machinery, one connected slab): the borrow stays sticky and
			// order-dependent exactly as before, but no wall exists until the stone has risen
			List<Cell> planCells = new List<Cell>();
			List<string> planBps = new List<string>();
			foreach (Cell cell in targeted)
			{
				if (!cell.IsEmpty())
					continue;
				foreach (Cell c in cell.GetCardinalAdjacentCellsWhere(x => x.HasWall()))
				{
					var wallProps = c.GetFirstWall().GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
					if (Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(wallProps))
					{
						bp = wallProps.ParentObject.Blueprint;
						break;
					}
				}
				planCells.Add(cell);
				planBps.Add(bp ?? "Cleo_TerraFirma_WallCalcify");
			}
			// nothing empty to fill: refuse before the turn and cooldown are spent (audit 2026-09-29).
			// Line approved 2026-09-30, shaped on PhotosyntheticSkin's "You need sunlight to bask in." and the
			// card's "Only empty spaces will form walls."; Fail pops for the player, silent for NPCs.
			if (planCells.Count == 0)
				return ParentObject.Fail("You need empty spaces to calcify.");
			UseEnergy(1000, $"Physical Mutation {Name}");
			CooldownMyActivatedAbility(ActivatedAbilityID, GetCooldown(Level));
			PlayWorldSound("Sounds/Throw/sfx_throwing_stone_large_impact", 1f, Combat: true); // the slam covers the rise
			// USER 2026-08-05 ("possible screen shake add for calcify"), RULED + BUILT 2026-08-14: one
			// small shake beside the slam -- gentler than the redoubt's descending beats (0.3+); visibility
			// gate keeps an off-screen NPC calcifier from rattling the player's screen.
			if (ParentObject.IsVisible())
				CombatJuice.cameraShake(0.15f);
			void PlaceWall(int i)
			{
				GameObject wall = GameObject.Create(planBps[i]);
				// REBALANCED 2026-08-06 (user): the wall is a TRUE COPY of its source blueprint --
				// native HP, AV, DV, and description all stand. The part stays as the "calcified"
				// name prefix and the fugue-tether marker, but shifts nothing.
				wall.AddPart(new Cleo_TerraFirma_CalcifyWallPart());
				Phase.carryOver(ParentObject, wall);
				// fugue-clone casts (RULED 2026-08-05, recon 2026-07-24: "walls as temporal as
				// their maker"): a temporal fugue copy's walls ride the engine's own
				// ExistenceSupport and dissipate the moment the clone does. Normal casts skip.
				if (ParentObject.HasStringProperty("FugueCopy"))
				{
					XRL.World.Parts.ExistenceSupport support = wall.RequirePart<XRL.World.Parts.ExistenceSupport>();
					support.SupportedBy = ParentObject;
					support.ValidateEveryTurn = true;
				}
				planCells[i].AddObject(wall); // AddObject paints natively (CheckPaintWallsAround inside; diagnosed 2026-08-04)
				// zone-build dressing (user 2026-08-05, calcified shale beside water wore plain red
				// while its natural neighbors wore vineshale green): ShaleVines runs only on
				// ZoneBuiltEvent, which our walls postdate. Run the same check at placement so a
				// water-adjacent calcified wall dresses exactly as the zone's own stone does; the
				// post-cast region repaint picks up its swapped paint value.
				if (wall.TryGetPart(out XRL.World.Parts.ShaleVines vines))
					vines.GrowVines();
				string paintedTile = wall.Render?.Tile;
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CALC", $"wall landed @({planCells[i].X},{planCells[i].Y}): bp={planBps[i]}, post-paint Render.Tile={(paintedTile.IsNullOrEmpty() ? "NULL (paint did not take)" : paintedTile)}");
			}
			Cleo.TerraFirma.Scripts.Cleo_TerraFirma_StoneRiseFX.Schedule schedule = null;
			Zone zone = ParentObject.CurrentZone;
			if (!Cleo.TerraFirma.Scripts.Cleo_TerraFirma_StoneRiseFX.HarnessMute && zone != null && zone.IsActive() && ParentObject.CurrentCell?.IsVisible() == true)
				schedule = Cleo.TerraFirma.Scripts.Cleo_TerraFirma_StoneRiseFX.PlayWallRise(planCells, planBps);
			if (schedule != null)
			{
				// Fissure's settle idiom: the world renders live while the stones come up in
				// pick order, and each real wall lands exactly at its own sprite's handoff
				ScreenBuffer buf = ScreenBuffer.GetScrapBuffer1(bLoadFromCurrent: true);
				TextConsole console = Popup._TextConsole;
				float t = 0f;
				float end = schedule.Total + 0.08f;
				int placed = 0;
				while (t < end)
				{
					while (placed < planCells.Count
						&& (!schedule.SettleTimes.TryGetValue(planCells[placed], out float settleAt) || settleAt <= t))
						PlaceWall(placed++);
					XRLCore.Core.RenderBaseToBuffer(buf);
					console.DrawBuffer(buf);
					Thread.Sleep(40);
					t += 0.04f;
				}
				while (placed < planCells.Count)
					PlaceWall(placed++);
			}
			else
			{
				for (int i = 0; i < planCells.Count; i++)
					PlaceWall(i);
			}
			// smoke only AFTER the line has fully risen (RULED 2026-08-05: per-landing smoke hid
			// the risen walls; "have the smoke spawn at the end", "a random amount of puffs ...
			// between 1-4") -- each puff lands on a random cell of the finished line
			if (planCells.Count > 0)
			{
				int puffs = Stat.Random(1, 4);
				for (int i = 0; i < puffs; i++)
					Cleo_TerraFirma_Fissure.SmokePuff(planCells[Stat.Random(0, planCells.Count - 1)]);
			}
			// one repaint over the finished line: walls land one at a time (the ripple), so each
			// painted knowing only its predecessors -- landing-order variants linger otherwise
			// (the light-vs-dark column mystery, 2026-08-04)
			if (planCells.Count > 0)
			{
				int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
				foreach (Cell c in planCells)
				{
					minX = Math.Min(minX, c.X); maxX = Math.Max(maxX, c.X);
					minY = Math.Min(minY, c.Y); maxY = Math.Max(maxY, c.Y);
				}
				ZoneManager.PaintWalls(zone, minX - 1, minY - 1, maxX + 1, maxY + 1);
			}
			DidX("calcify", "loose matter", ColorAsGoodFor: ParentObject, Source: ParentObject);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CALC", $"cast Lv{Level}: {planCells.Count} wall(s) raised of {targeted.Count} targeted (formula {GetWallsCreated(Level)}), true-copy stats (rebalance 2026-08-06), blueprint={bp ?? "FALLBACK Cleo_TerraFirma_WallCalcify"}, animated={(schedule != null ? schedule.Total.ToString("0.00") + "s pick-order ripple" : "no")}{(ParentObject.HasStringProperty("FugueCopy") ? ", fugue-tethered (walls die with the clone)" : "")}");
			return base.HandleEvent(E);
		}
		#endregion

		#region Helpers
		public static int GetWallsCreated(int Lv) => (int)(BASE_WALLS_CREATED + Mathf.Floor(Lv / 2f));

		// GetWallHp RETIRED 2026-08-06 (user rebalance: "no longer gain HP scaling per level") --
		// walls carry their source blueprint's own hit points; level buys count only.

		/// <summary>95 at L1 falling 5 per odd level to the 50 floor at L19 (re-dial RULED
		/// 2026-08-09; was -10/odd flooring at L9, which left the odd half of the lattice dead
		/// through boost territory). 50 = Stunning Force's flat, half Force Wall's 100.</summary>
		public static int GetCooldown(int Lv) => Math.Max(COOLDOWN_MAX - COOLDOWN_STEP * ((Lv + 1) / 2), COOLDOWN_FLOOR);
		#endregion
	}
}
