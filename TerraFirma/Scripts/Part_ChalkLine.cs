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
			render.Tile = "chalkline" + PieceFor(n, s, e, w, cell, StrokeDir) + ".png";
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

		// N/S and E/W are 1px-offset variants of the same edge; cell parity picks one stably
		private static string Horizontal(Cell cell)
		{
			return cell.Y % 2 == 0 ? "N" : "S";
		}

		private static string Vertical(Cell cell)
		{
			return cell.X % 2 == 0 ? "W" : "E";
		}
	}
}
