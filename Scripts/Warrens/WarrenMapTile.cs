using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using Qud.API;
using XRL.World.Parts;

namespace XRL.World.WorldBuilders
{
	[Serializable]
	[HasCallAfterGameLoaded]
	public class Cleo_TerraFirma_WarrenMapTiles : IGameSystem
	{
		public const string ATTRIBUTE = "stonecarver";
		public const string CANYON_TILE = "Terrain/tf_quarry_canyon.png";
		public const string HILLS_TILE = "Terrain/tf_quarry_hills2.png";
		public const string DETAIL = "r";
		public const string OLD_TILE = "Cleo_TerraFirma_WarrenOldTile";
		public const string OLD_DETAIL = "Cleo_TerraFirma_WarrenOldDetail";

		public override void Register(XRLGame Game, IEventRegistrar Registrar)
		{
			Registrar.Register(SecretVisibilityChangedEvent.ID);
		}

		public override bool HandleEvent(SecretVisibilityChangedEvent E)
		{
			try
			{
				if (E.Entry is JournalMapNote note && IsWarren(note))
					Apply(note);
			}
			catch (Exception x)
			{
				MetricsManager.LogException("Cleo_TerraFirma_WarrenMapTiles", x);
			}
			return base.HandleEvent(E);
		}

		[CallAfterGameLoaded]
		public static void AfterGameLoaded()
		{
			try
			{
				if (The.Game == null)
					return;
				The.Game.RequireSystem<Cleo_TerraFirma_WarrenMapTiles>();
				Sweep();
			}
			catch (Exception x)
			{
				MetricsManager.LogException("Cleo_TerraFirma_WarrenMapTiles load", x);
			}
		}

		public static bool IsWarren(JournalMapNote note) => note?.Attributes != null && note.Attributes.Contains(ATTRIBUTE);

		public static int Sweep()
		{
			int quarried = 0;
			foreach (JournalMapNote note in new List<JournalMapNote>(JournalAPI.MapNotes))
				if (IsWarren(note) && Apply(note))
					quarried++;
			return quarried;
		}

		public static GameObject TerrainFor(string WorldID, int ParasangX, int ParasangY)
		{
			if (string.IsNullOrEmpty(WorldID))
				return null;
			Zone world = The.ZoneManager?.GetZone(WorldID);
			return world?.GetCell(ParasangX, ParasangY)?.GetFirstObjectWithPart("TerrainTravel");
		}

		public static bool Apply(JournalMapNote note)
		{
			GameObject terrain = TerrainFor(note.WorldID, note.ParasangX, note.ParasangY);
			if (terrain?.Render == null)
				return false;
			if (note.Revealed)
				return Quarry(terrain);
			foreach (JournalMapNote other in JournalAPI.MapNotes)
				if (other != note && other.Revealed && IsWarren(other) && other.WorldID == note.WorldID
					&& other.ParasangX == note.ParasangX && other.ParasangY == note.ParasangY)
					return Quarry(terrain);
			Restore(terrain);
			return false;
		}

		public static bool Quarry(GameObject terrain)
		{
			string tile = terrain.Render.Tile ?? "";
			if (tile == CANYON_TILE || tile == HILLS_TILE)
				return true;
			terrain.SetStringProperty(OLD_TILE, tile);
			terrain.SetStringProperty(OLD_DETAIL, terrain.Render.DetailColor ?? "");
			terrain.SetIntProperty("ForceMutableSave", 1);
			terrain.Render.Tile = tile.IndexOf("hills2", StringComparison.OrdinalIgnoreCase) >= 0 ? HILLS_TILE : CANYON_TILE;
			terrain.Render.DetailColor = DETAIL;
			return true;
		}

		public static void Restore(GameObject terrain)
		{
			string tile = terrain.GetStringProperty(OLD_TILE);
			if (string.IsNullOrEmpty(tile))
				return;
			terrain.Render.Tile = tile;
			terrain.Render.DetailColor = terrain.GetStringProperty(OLD_DETAIL) ?? "";
			terrain.RemoveStringProperty(OLD_TILE);
			terrain.RemoveStringProperty(OLD_DETAIL);
		}

		public static void Forget(JournalMapNote note)
		{
			if (note == null)
				return;
			bool was = note.Revealed;
			note.Revealed = false;
			try { Apply(note); }
			finally { note.Revealed = was; }
		}
	}
}
