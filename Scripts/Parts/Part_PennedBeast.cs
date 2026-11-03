using System;
using System.Collections.Generic;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_PennedBeast : IPart
	{
		public string PenZoneID;
		public int PenX1 = -1;
		public int PenY1 = -1;
		public int PenX2 = -1;
		public int PenY2 = -1;
		public int GateX = -1;
		public int GateY = -1;

		[NonSerialized]
		private bool LastInPen;

		public const string PEN_CELLS_PROPERTY = "Cleo_TerraFirma_PenCells";
		[NonSerialized]
		private HashSet<int> PenCellCache;
		[NonSerialized]
		private string PenCellSource;

		public override bool SameAs(IPart p) => false;

		public bool HasPen => !string.IsNullOrEmpty(PenZoneID) && PenX1 >= 0;

		public void SetPen(Zone zone, int x1, int y1, int x2, int y2, int gateX = -1, int gateY = -1, IEnumerable<string> cells = null)
		{
			if (cells != null) ParentObject?.SetStringProperty(PEN_CELLS_PROPERTY, string.Join(",", cells));
			else ParentObject?.RemoveStringProperty(PEN_CELLS_PROPERTY);
			GateX = gateX;
			GateY = gateY;
			PenZoneID = zone?.ZoneID;
			PenX1 = Math.Min(x1, x2);
			PenY1 = Math.Min(y1, y2);
			PenX2 = Math.Max(x1, x2);
			PenY2 = Math.Max(y1, y2);
		}

		public void ClearPen()
		{
			PenZoneID = null;
			PenX1 = PenY1 = PenX2 = PenY2 = -1;
			GateX = GateY = -1;
			ParentObject?.RemoveStringProperty(PEN_CELLS_PROPERTY);
		}

		public bool InPen()
		{
			Cell cell = ParentObject?.CurrentCell;
			return HasPen && cell != null && cell.ParentZone != null && cell.ParentZone.ZoneID == PenZoneID
				&& cell.X >= PenX1 && cell.X <= PenX2 && cell.Y >= PenY1 && cell.Y <= PenY2
				&& InPenCells(cell.X, cell.Y);
		}

		private bool InPenCells(int x, int y)
		{
			string src = ParentObject.GetStringProperty(PEN_CELLS_PROPERTY);
			if (string.IsNullOrEmpty(src)) return true;
			if (PenCellCache == null || PenCellSource != src)
			{
				PenCellCache = new HashSet<int>();
				foreach (string xy in src.Split(','))
				{
					int dot = xy.IndexOf('.');
					if (dot > 0 && int.TryParse(xy.Substring(0, dot), out int cx) && int.TryParse(xy.Substring(dot + 1), out int cy))
						PenCellCache.Add(cy * 1000 + cx);
				}
				PenCellSource = src;
			}
			return PenCellCache.Contains(y * 1000 + x);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<BeginTakeActionEvent>.ID;
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			Sync();
			return base.HandleEvent(E);
		}

		public void Sync()
		{
			bool inPen = InPen();
			Mutation.Burrowing burrowing = ParentObject.GetPart<Mutation.Burrowing>();
			if (burrowing != null)
			{
				burrowing.bBurrowWhenBored = !inPen;
				ActivatedAbilityEntry ability = ParentObject.GetActivatedAbility(burrowing.ActivatedAbilityID);
				if (ability != null)
					ability.AIDisable = inPen;
			}
			if (inPen)
			{
				ParentObject.GetEffect<Burrowed>()?.Emerge();
				ShutGate();
			}
			if (inPen != LastInPen)
			{
				LastInPen = inPen;
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("PEN", $"{ParentObject.Blueprint} {(inPen ? "entered" : "left")} its pen");
			}
		}

		public void ShutGate()
		{
			if (GateX < 0) return;
			Cell gate = ParentObject.CurrentCell?.ParentZone?.GetCell(GateX, GateY);
			if (gate == null || gate.HasObjectWithPart("Combat")) return;
			foreach (GameObject o in gate.GetObjectsWithPart("Door"))
			{
				Door door = o.GetPart<Door>();
				if (door != null && door.Open)
				{
					door.PerformClose();
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("PEN", $"{ParentObject.Blueprint} shut the pen gate");
				}
			}
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_PenLatch : IActivePart
	{
		public Cleo_TerraFirma_PenLatch()
		{
			WorksOnSelf = true;
			IsBreakageSensitive = false;
			IsRustSensitive = false;
		}

		public override bool SameAs(IPart p) => p is Cleo_TerraFirma_PenLatch && base.SameAs(p);

		public override bool GetActivePartLocallyDefinedFailure()
		{
			GameObject who = ParentObject?.Equipped;
			return who != null && who.GetPart<Cleo_TerraFirma_PennedBeast>()?.InPen() == true;
		}

		public override string GetActivePartLocallyDefinedFailureDescription() => "Penned";
	}
}
