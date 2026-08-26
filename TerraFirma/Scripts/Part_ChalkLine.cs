using System;
using XRL.UI;

namespace XRL.World.Parts
{
	/// <summary>
	/// A drawn chalk line on the floor (box-of-chalk sitting 2026-08-22; connection rework same
	/// day after the user's live catch "the chalk lines don't really seem to want to connect?").
	/// The eight chalkline*.png sprites are a BOX-DRAWING KIT, named for which part of a chalked
	/// plot they are: N/S = top/bottom edges (horizontal), E/W = right/left edges (vertical),
	/// corners named by position - chalklineNE is a rectangle's northeast corner, arms south+west.
	/// Auto-tiling reads the four cardinal neighbors and picks the piece whose arms reach them,
	/// and every placement or smear RE-TILES the neighbors so they reach back. Tees follow the
	/// same position convention (chalklineTN sits on the north edge, stem reaching south into
	/// the plot) and chalklineX is the four-way (user sprites 2026-08-25).
	/// Smear: open to anyone adjacent; owned camp lines broadcast for help via native ownership
	/// (BroadcastForHelp), player lines are unowned and wipe clean.
	/// Trample (user shape 2026-08-23, amending the 08-22 "doesn't wear off" shape with traffic
	/// wear): a creature stepping on the cell has a small chance to smear the line - the light
	/// mote-scuff beat, never the deliberate smear's Hold, because any passing NPC could not be
	/// allowed to hitch the game thread mid-move. Owner-faction walkers skip the roll entirely
	/// (their own camp traffic minds its marks - and cannot anger itself); everyone else who
	/// tramples an owned line gets the same help broadcast a deliberate smear does.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_ChalkLine : IPart
	{
		public const string SMEAR_COMMAND = "Cleo_TerraFirma_SmearChalkLine";

		/// <summary>Percent chance per step that a walker smears the line. CLAUDE DRAFT dial.</summary>
		public const int TRAMPLE_SMEAR_CHANCE = 5;

		/// <summary>The stroke direction this piece was drawn with; the re-tile fallback when
		/// no neighboring chalk remains.</summary>
		public string StrokeDir;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID
				|| ID == GetNavigationWeightEvent.ID
				|| ID == ObjectEnteredCellEvent.ID;
		}

		/// <summary>Soft pathing nudge (user docket 2026-08-25, "a lower weight on chalk
		/// lines also"): 2, well under the cairn's mine-grade 7 - a line on the ground is
		/// a boundary you'd rather not scuff, not an obstacle.</summary>
		public override bool HandleEvent(GetNavigationWeightEvent E)
		{
			if (E.Smart)
				E.MinWeight(2);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ObjectEnteredCellEvent E)
		{
			// walker gates on the Sticky chassis (solid, phase-matched) plus creature-and-grounded:
			// fliers touch nothing, and webs WANT fliers where chalk does not
			GameObject walker = E.Object;
			if (walker != null && walker.IsCreature && !walker.IsFlying
				&& walker.GetMatterPhase() <= 1 && walker.PhaseMatches(ParentObject))
			{
				string owner = ParentObject.Physics?.Owner;
				// the Bed ownership guard, inverted: owner-matched walkers (tag, name, or
				// faction) never roll - camp traffic minds its marks and cannot anger itself
				bool ownerMatched = !owner.IsNullOrEmpty()
					&& (walker.HasTagOrProperty(owner) || walker.DisplayNameOnly == owner || walker.BelongsToFaction(owner));
				if (!ownerMatched && XRL.Rules.Stat.Random(1, 100) <= TRAMPLE_SMEAR_CHANCE)
				{
					if (!owner.IsNullOrEmpty())
					{
						// trampling camp chalk is the same vandalism as scrubbing it
						ParentObject.Physics.BroadcastForHelp(walker);
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"owned chalk line ({owner}) trampled by {walker.Blueprint}, help broadcast");
					}
					else
					{
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"chalk line trampled by {walker.Blueprint}");
					}
					if (walker.IsPlayer())
					{
						IComponent<GameObject>.AddPlayerMessage("Your steps smear the chalk line."); // DRAFT
					}
					else if (ParentObject.IsVisible())
					{
						IComponent<GameObject>.AddPlayerMessage(walker.Does("smear") + " the chalk line underfoot."); // DRAFT
					}
					Cell cell = ParentObject.CurrentCell;
					Cleo.TerraFirma.Scripts.Cleo_TerraFirma_ChalkRevealFX.PlayScuff(cell);
					ParentObject.Destroy();
					RetileNeighbors(cell);
				}
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			E.AddAction("Smear", "smear away", SMEAR_COMMAND, null, 's', FireOnActor: false);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == SMEAR_COMMAND && E.Actor != null)
			{
				string owner = ParentObject.Physics?.Owner;
				if (!owner.IsNullOrEmpty())
				{
					// owned chalk: the smear is vandalism and the owner hears about it
					ParentObject.Physics.BroadcastForHelp(E.Actor);
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"owned chalk line ({owner}) smeared by {E.Actor.Blueprint}, help broadcast");
				}
				else
				{
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"chalk line smeared by {E.Actor.Blueprint}");
				}
				if (E.Actor.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage("You smear the chalk line away."); // DRAFT
				}
				Cell cell = ParentObject.CurrentCell;
				E.Actor.UseEnergy(1000, "Item SmearChalkLine");
				// smear reveal, inverted (RULED 2026-08-23): floor-copy quads fly IN over the
				// line; the removal happens behind the finished cover, whose exit is
				// floor-over-floor and therefore invisible. 0 = unseen/classic/unpainted,
				// remove immediately as before
				float cover = Cleo.TerraFirma.Scripts.Cleo_TerraFirma_ChalkRevealFX.PlaySmear(cell);
				if (cover > 0f)
				{
					Cleo.TerraFirma.Scripts.Cleo_TerraFirma_ChalkRevealFX.Hold(cover);
				}
				ParentObject.Destroy();
				RetileNeighbors(cell);
				E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
		}

		/// <summary>Re-tile every cardinal-adjacent chalk line around a cell (after a piece is
		/// placed or smeared, so the plot's arms keep reaching each other).</summary>
		public static void RetileNeighbors(Cell cell)
		{
			if (cell == null)
			{
				return;
			}
			foreach (string d in XRL.Rules.Directions.CardinalDirectionList)
			{
				Cell neighbor = cell.GetCellFromDirection(d);
				GameObject line = neighbor?.GetFirstObjectWithPart(nameof(Cleo_TerraFirma_ChalkLine));
				line?.GetPart<Cleo_TerraFirma_ChalkLine>()?.Retile();
			}
		}

		/// <summary>Pick this piece's sprite from its neighbors' positions (arms must reach every
		/// cardinal neighbor that holds chalk), falling back to the recorded stroke direction.</summary>
		public void Retile()
		{
			Cell cell = ParentObject.CurrentCell;
			Render render = ParentObject.GetPart<Render>();
			if (cell == null || render == null)
			{
				return;
			}
			bool n = HasChalk(cell, "N"), s = HasChalk(cell, "S"), e = HasChalk(cell, "E"), w = HasChalk(cell, "W");
			string tile = "chalkline" + PieceFor(n, s, e, w, cell, StrokeDir) + ".png";
			if (render.Tile != tile)
			{
				render.Tile = tile;
				// ripple: a role change here can change a run-mate's role (closing a rectangle
				// re-roles the whole edge); cascades only while tiles actually change
				RetileNeighbors(cell);
			}
		}

		private static bool HasChalk(Cell cell, string dir)
		{
			return cell.GetCellFromDirection(dir)?.GetFirstObjectWithPart(nameof(Cleo_TerraFirma_ChalkLine)) != null;
		}

		private static string PieceFor(bool n, bool s, bool e, bool w, Cell cell, string strokeDir)
		{
			// junctions first: four arms is the cross, three is a tee named for the plot
			// edge it sits on (stem always reaches inward, matching the corner convention)
			if (n && s && e && w)
			{
				return "X";
			}
			if (e && w && s)
			{
				return "TN";
			}
			if (e && w && n)
			{
				return "TS";
			}
			if (n && s && w)
			{
				return "TE";
			}
			if (n && s && e)
			{
				return "TW";
			}
			// straights win when an opposing pair exists
			if (n && s)
			{
				return Vertical(cell);
			}
			if (w && e)
			{
				return Horizontal(cell);
			}
			// plot corners, named by position: NE corner reaches south+west, and so around
			if (s && w)
			{
				return "NE";
			}
			if (s && e)
			{
				return "NW";
			}
			if (n && w)
			{
				return "SE";
			}
			if (n && e)
			{
				return "SW";
			}
			// one arm: lie along it
			if (n || s)
			{
				return Vertical(cell);
			}
			if (w || e)
			{
				return Horizontal(cell);
			}
			// alone: lie along the stroke that drew it; a diagonal stroke draws its namesake corner
			switch (strokeDir)
			{
				case "N":
				case "S":
					return Vertical(cell);
				case "NE":
				case "NW":
				case "SE":
				case "SW":
					return strokeDir;
				default:
					return Horizontal(cell);
			}
		}

		// ROLE, not parity (live-test diagnosis 2026-08-22: the parity dither was a Claude
		// misread of the kit - N is the TOP-edge stroke and S the BOTTOM-edge stroke, matching
		// the corners' own arm rows, so alternating them made every run zigzag). A straight
		// piece asks which way its run eventually TURNS: a horizontal run whose end turns
		// south is a top edge (N), turns north is a bottom edge (S); vertical runs likewise
		// (turns east = left edge = W, turns west = right edge = E). A bare run that never
		// turns reads N/W (dial).
		private const int RUN_WALK_MAX = 80;

		private static string Horizontal(Cell cell)
		{
			return RunEdgeRole(cell, "W", "E", "S", "N", "N", "S") ?? "N";
		}

		private static string Vertical(Cell cell)
		{
			return RunEdgeRole(cell, "N", "S", "E", "W", "W", "E") ?? "W";
		}

		/// <summary>Walk the run both ways; the first cell with a chalk neighbor on turnA/turnB
		/// names the edge (roleA/roleB). Null when the run never turns.</summary>
		private static string RunEdgeRole(Cell cell, string dir1, string dir2, string turnA, string turnB, string roleA, string roleB)
		{
			foreach (string d in new string[] { dir1, dir2 })
			{
				Cell c = cell;
				for (int i = 0; i < RUN_WALK_MAX && c != null; i++)
				{
					bool a = HasChalk(c, turnA);
					bool b = HasChalk(c, turnB);
					if (a && b)
					{
						break; // crossing; no verdict from this direction
					}
					if (a)
					{
						return roleA;
					}
					if (b)
					{
						return roleB;
					}
					c = c.GetCellFromDirection(d);
					if (c?.GetFirstObjectWithPart(nameof(Cleo_TerraFirma_ChalkLine)) == null)
					{
						break;
					}
				}
			}
			return null;
		}
	}
}
