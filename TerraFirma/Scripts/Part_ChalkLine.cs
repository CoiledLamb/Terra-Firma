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
	/// and every placement or smear RE-TILES the neighbors so they reach back. No T-junction or
	/// cross sprites exist yet (3-4 arm cells fall back to a straight); flagged to the user, who
	/// offered to draw more.
	/// Smear: open to anyone adjacent; owned camp lines broadcast for help via native ownership
	/// (BroadcastForHelp), player lines are unowned and wipe clean.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_ChalkLine : IPart
	{
		public const string SMEAR_COMMAND = "Cleo_TerraFirma_SmearChalkLine";

		/// <summary>The stroke direction this piece was drawn with; the re-tile fallback when
		/// no neighboring chalk remains.</summary>
		public string StrokeDir;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID;
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
			// straights win when an opposing pair exists (also the 3-4 arm fallback: no
			// T-junction or cross sprites in the kit yet)
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
