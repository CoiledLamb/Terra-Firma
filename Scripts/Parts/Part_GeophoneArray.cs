using System;
using System.Collections.Generic;
using System.Linq;
using Cleo.TerraFirma.Scripts;
using UnityEngine;
using XRL.World.Parts.Mutation;
using CellData = XRL.World.Parts.Mutation.Cleo_TerraFirma_Geomancy.Cleo_TerraFirma_GeomancyCellData;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_GeophoneArray : IPart
	{
		[NonSerialized]
		public List<CellData> Cells = new List<CellData>();

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ImplantedEvent.ID
				|| ID == UnimplantedEvent.ID
				|| ID == BeforeRenderEvent.ID
				|| ID == SingletonEvent<EndTurnEvent>.ID;
		}

		public override bool HandleEvent(ImplantedEvent E)
		{
			GameObject who = E.Implantee;
			who.SetIntProperty(Cleo_TerraFirma_Geomancy.GEOPHONE_PROPERTY, 1);
			who.RegisterEvent(this, BeforeRenderEvent.ID, Serialize: true);
			who.RegisterEvent(this, SingletonEvent<EndTurnEvent>.ID, Serialize: true);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnimplantedEvent E)
		{
			GameObject who = E.Implantee;
			who.RemoveIntProperty(Cleo_TerraFirma_Geomancy.GEOPHONE_PROPERTY);
			who.UnregisterEvent(this, BeforeRenderEvent.ID);
			who.UnregisterEvent(this, SingletonEvent<EndTurnEvent>.ID);
			Cells.Clear();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeRenderEvent E)
		{
			long now = Cleo_TerraFirma_GeomancyPulseFX.NowMS;
			foreach (CellData cd in Cells)
			{
				if (cd.RevealAtMS > now)
					continue;
				if (!cd.MapMarked)
				{
					cd.Cell.SetExplored();
					cd.MapMarked = true;
				}
				cd.Cell.ParentZone.AddLight(cd.Cell.X, cd.Cell.Y, 0, LightLevel.Omniscient);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			foreach (CellData cd in Cells.ToArray())
			{
				if (--cd.Duration <= 0)
					Cells.Remove(cd);
			}
			return base.HandleEvent(E);
		}

		public static Cleo_TerraFirma_GeophoneArray FindImplanted(GameObject Actor)
		{
			if (Actor?.Body == null)
				return null;
			foreach (GameObject implant in Actor.Body.GetInstalledCyberneticsReadonly() ?? new System.Collections.Generic.List<GameObject>())
			{
				var part = implant.GetPart<Cleo_TerraFirma_GeophoneArray>();
				if (part != null)
					return part;
			}
			return null;
		}

		public static bool GeomancyHandles(GameObject Actor) =>
			Actor.HasPart<Cleo_TerraFirma_Geomancy>() && Actor.IsRealityDistortionUsable();

		public int OnStoneThrown(GameObject Actor, GameObject Weapon)
		{
			if (Actor == null || !Actor.IsPlayer() || Actor != ParentObject.Implantee)
				return 0;
			if (!Cleo_TerraFirma_Geomancy.IsGeophoneStone(Weapon) || GeomancyHandles(Actor))
				return 0;
			Cell landingCell = Weapon.CurrentCell;
			if (landingCell == null || landingCell.OnWorldMap())
				return 0;
			int radius = Cleo_TerraFirma_Geomancy.GeophoneRadius(Weapon);
			int duration = Cleo_TerraFirma_Geomancy.GeophoneDuration(Weapon);
			List<Cell> picked = landingCell.GetLocalAdjacentCellsCircular(radius, true).Where(c => !c.OnWorldMap()).ToList();
			IComponent<GameObject>.XDidY(Actor, "listen", "for vibrations", ColorAsGoodFor: Actor);
			Helpers.VerifyLog("GEOPHONE", $"reveal: {Weapon.Blueprint} ({Weapon.WeightEach} lb) landed, radius {radius}, {duration} turns, {picked.Count} cells");
			bool animate = !Cleo_TerraFirma_StoneRiseFX.HarnessMute && landingCell.ParentZone != null && landingCell.ParentZone.IsActive();
			long now = animate ? Cleo_TerraFirma_GeomancyPulseFX.NowMS : 0L;
			List<(Cell cell, float dist)> pulseCells = animate ? new List<(Cell cell, float dist)>(picked.Count) : null;
			float maxDist = 0f;
			foreach (Cell c in picked)
			{
				var cd = new CellData(c, duration, false);
				if (animate)
				{
					int dx = c.X - landingCell.X, dy = c.Y - landingCell.Y;
					float dist = Mathf.Sqrt(dx * dx + dy * dy);
					cd.RevealAtMS = now + (long)(dist * Cleo_TerraFirma_GeomancyPulseFX.RING_MS);
					pulseCells.Add((c, dist));
					if (dist > maxDist)
						maxDist = dist;
				}
				else
				{
					c.SetExplored();
					cd.MapMarked = true;
				}
				Cells.Add(cd);
			}
			if (animate)
				Cleo_TerraFirma_GeomancyPulseFX.Play(landingCell, maxDist, pulseCells);
			return picked.Count;
		}

		public override void Write(GameObject Basis, SerializationWriter Writer)
		{
			Writer.Write(Cells.Count);
			foreach (CellData cd in Cells)
			{
				Writer.Write(cd.Cell.ParentZone.ZoneID);
				Writer.Write(cd.Cell.X);
				Writer.Write(cd.Cell.Y);
				Writer.Write(cd.Duration);
			}
			base.Write(Basis, Writer);
		}

		public override void Read(GameObject Basis, SerializationReader Reader)
		{
			int count = Reader.ReadInt32();
			Cells = new List<CellData>();
			for (int i = 0; i < count; i++)
			{
				string zoneId = Reader.ReadString();
				int x = Reader.ReadInt32();
				int y = Reader.ReadInt32();
				int duration = Reader.ReadInt32();
				Cell cell = The.ZoneManager.GetZone(zoneId)?.GetCell(x, y);
				if (cell != null)
					Cells.Add(new CellData(cell, duration, false));
			}
			base.Read(Basis, Reader);
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_GeophoneStonePart : IPart
	{
		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == AfterThrownEvent.ID;
		}

		public override bool HandleEvent(AfterThrownEvent E)
		{
			Cleo_TerraFirma_GeophoneArray.FindImplanted(E.Actor)?.OnStoneThrown(E.Actor, E.Item);
			return base.HandleEvent(E);
		}
	}
}
