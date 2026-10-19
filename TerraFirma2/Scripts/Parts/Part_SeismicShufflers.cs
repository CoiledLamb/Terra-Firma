using System;
using System.Collections.Generic;
using XRL.Wish;

namespace XRL.World.Parts
{
    // Acquisition and balance are configured in XML.
    // EnteredCell registration follows Yurtmat/CobblefurCloak. Lower-zone access uses
    // Cell.GetCellFromDirectionGlobal; Temporary owns projection expiry and zone thaw.
    [Serializable]
    public class Cleo_TerraFirma_SeismicShufflers : IPoweredPart
    {
        public int PreviewDuration = 2;
        public int PassiveRadius = 1;
        public int PeekChargeUse = 100;
        public float RiseSeconds = 0.25f;
        public int RiseStaggerMs = 250;
        public float RisePixels = 8f;
        public string PeekTile;
        public string PeekColor = "&y";
        public char PeekDetailColor = 'C';
        /// <summary>Played once per Seismic Peek at the shufflers, as the walls start to rise. Set on the
        /// blueprint; [NonSerialized] so the shipped part keeps its saved layout.</summary>
        [NonSerialized] public string PeekSound = "Sounds/Abilities/sfx_ability_leyShift_repetition";
        [NonSerialized] public float PeekSoundVolume = 0.5f;
        /// <summary>Played instead of PeekSound when the scan finds no walls below (user 2026-10-19:
        /// "should click"). Base empty-weapon click, written bare as base code writes it.</summary>
        [NonSerialized] public string EmptyPeekSound = "sfx_missile_reloadFail_noAmmo";
        [NonSerialized] public float EmptyPeekSoundVolume = 0.5f;
        public Guid AbilityID;
        public string CommandID;
        public List<GameObject> Projections = new List<GameObject>();

        public Cleo_TerraFirma_SeismicShufflers()
        {
            ChargeUse = 1;
            WorksOnEquipper = true;
        }

        public override bool SameAs(IPart p) => false;

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == EquippedEvent.ID || ID == UnequippedEvent.ID;
        }

        public override bool HandleEvent(EquippedEvent E)
        {
            if (E.Item == ParentObject && E.Item.IsEquippedProperly() && E.Actor.IsPlayer())
            {
                E.Actor.RegisterEvent(this, EnteredCellEvent.ID, 0, Serialize: true);
                E.Actor.RegisterEvent(this, CommandEvent.ID, 0, Serialize: true);
                if (AbilityID == Guid.Empty)
                {
                    CommandID = "Cleo_TerraFirma_SeismicPeek_" + Guid.NewGuid().ToString("N");
                    AbilityID = E.Actor.AddActivatedAbility("Seismic Peek", CommandID, "Items",
                        "You briefly project holograms of nearby walls one stratum below you.\n\n" +
                        $"Duration: {PreviewDuration} rounds\nLow charge draw on activate.", "~");
                }
                // ActivatedAbilityEntry.GetUITile honors an explicit renderable before XML defaults.
                var ability = E.Actor.GetPart<ActivatedAbilities>()?.GetAbility(AbilityID);
                if (ability != null && !string.IsNullOrEmpty(PeekTile))
                    ability.UITileDefault = new ConsoleLib.Console.Renderable(PeekTile, "~", PeekColor, null, PeekDetailColor);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(UnequippedEvent E)
        {
            if (E.Item == ParentObject)
            {
                E.Actor.UnregisterEvent(this, EnteredCellEvent.ID);
                E.Actor.UnregisterEvent(this, CommandEvent.ID);
                E.Actor.RemoveActivatedAbility(ref AbilityID);
                ClearProjections();
            }
            return base.HandleEvent(E);
        }

        private GameObject Wearer => ParentObject.IsEquippedProperly() ? ParentObject.Equipped : null;

        private static Cell Below(Cell cell)
        {
            if (cell == null || cell.OnWorldMap() || cell.ParentZone.IsWorldMap())
                return null;
            return cell.GetCellFromDirectionGlobal("D", bLocalOnly: false, bLiveZonesOnly: false);
        }

        // The passive never builds or thaws a zone. Generating the stratum below inside the
        // step's EnteredCell broke a live object's event registry on stable (2026-10-19: floor
        // cushion Registry.Add, then TurnTick NREs that froze world map travel). A live stratum
        // is mapped at once; any other one keeps its marks in string game state (one '1' per
        // cell) and they are revealed the first time the wearer stands in that zone.
        public const string PENDING_STATE = "Cleo_TerraFirma_ShufflerMarks:";

        private static string BelowZoneID(Zone zone)
        {
            if (zone == null || zone.IsWorldMap())
                return null;
            try
            {
                return ZoneID.Assemble(zone.GetZoneWorld(), zone.GetZonewX(), zone.GetZonewY(),
                    zone.GetZoneX(), zone.GetZoneY(), zone.GetZoneZ() + 1);
            }
            catch
            {
                return null;
            }
        }

        private static void ApplyPendingMarks(Zone zone)
        {
            if (zone == null || The.Game == null)
                return;
            string key = PENDING_STATE + zone.ZoneID;
            if (!The.Game.HasStringGameState(key))
                return;
            string marks = The.Game.GetStringGameState(key);
            The.Game.RemoveStringGameState(key);
            int revealed = 0;
            for (int i = 0; i < marks.Length && i < zone.Width * zone.Height; i++)
            {
                if (marks[i] != '1')
                    continue;
                zone.GetCell(i % zone.Width, i / zone.Width)?.SetExplored();
                revealed++;
            }
            Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHUFFLERS", $"revealed {revealed} pending cells in {zone.ZoneID}");
        }

        public override bool HandleEvent(EnteredCellEvent E)
        {
            GameObject wearer = Wearer;
            // arrival by stairs or other forced moves still reveals what was queued here
            if (wearer != null && wearer.IsPlayer() && E.Object == wearer)
                ApplyPendingMarks(wearer.CurrentCell?.ParentZone);
            if (wearer != null && wearer.IsPlayer() && E.Object == wearer && !E.Forced && !E.System)
            {
                Cell here = wearer.CurrentCell;
                Zone zone = here?.ParentZone;
                string belowID = BelowZoneID(zone);
                // StrideMason likewise charges only when its movement effect can run.
                if (belowID != null && IsReady(UseCharge: true))
                {
                    The.ZoneManager.CachedZones.TryGetValue(belowID, out Zone live);
                    int w = live?.Width ?? zone.Width, h = live?.Height ?? zone.Height;
                    string key = PENDING_STATE + belowID;
                    char[] marks = live != null ? null
                        : The.Game.GetStringGameState(key).PadRight(w * h, '0').ToCharArray();
                    int mapped = 0;
                    for (int dy = -PassiveRadius; dy <= PassiveRadius; dy++)
                    for (int dx = -PassiveRadius; dx <= PassiveRadius; dx++)
                    {
                        int x = here.X + dx, y = here.Y + dy;
                        if (x < 0 || y < 0 || x >= w || y >= h)
                            continue;
                        if (live != null)
                            live.GetCell(x, y)?.SetExplored();
                        else
                            marks[y * w + x] = '1';
                        mapped++;
                    }
                    if (marks != null)
                        The.Game.SetStringGameState(key, new string(marks));
                    Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHUFFLERS",
                        $"{(live != null ? "mapped" : "queued")} {mapped} cells around {belowID}@{here.X},{here.Y}");
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command != CommandID || string.IsNullOrEmpty(CommandID))
                return base.HandleEvent(E);
            GameObject wearer = Wearer;
            if (wearer == null || !wearer.IsPlayer())
                return false;
            // Temporary expiry invalidates/removes projections; stale references must not block a new peek.
            // Only live projections on the wearer's own level block (2026-10-19, peek stuck on
            // "can't make another" after leaving a zone): a left zone stops ticking its Temporary,
            // and a frozen zone pools its objects, so the old reference can come back as another object.
            Projections.RemoveAll(p => !IsOurProjection(p));
            foreach (GameObject projection in Projections)
                if (projection.CurrentCell?.ParentZone == wearer.CurrentZone)
                    return wearer.Fail("You can't make another projection yet.");
            Cell origin = wearer.CurrentCell;
            Cell below = Below(origin);
            if (below == null)
                return wearer.Fail("There is no lower stratum to scan here.");
            if (!IsReady(UseCharge: true, ChargeUse: PeekChargeUse))
                return wearer.Fail(ParentObject.Does("click", int.MaxValue, null, null, "merely") + "."); // base IActivePart unpowered line

            ClearProjections();
            int walls = 0;
            for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
            {
                if (Math.Abs(dx) == 2 && Math.Abs(dy) == 2)
                    continue;
                int x = origin.X + dx, y = origin.Y + dy;
                // Bound the prototype to this zone; never wrap its footprint across an edge.
                if (x < 0 || y < 0 || x >= origin.ParentZone.Width || y >= origin.ParentZone.Height
                    || x >= below.ParentZone.Width || y >= below.ParentZone.Height)
                    continue;
                GameObject wall = below.ParentZone.GetCell(x, y).GetFirstWall();
                if (wall?.Render == null)
                    continue;
                GameObject projection = GameObject.Create(PROJECTION_BLUEPRINT);
                projection.Render.Tile = wall.Render.Tile;
                projection.Render.RenderString = wall.Render.RenderString;
                projection.Render.HFlip = wall.Render.HFlip;
                projection.Render.VFlip = wall.Render.VFlip;
                projection.GetPart<Temporary>().Duration = Math.Max(1, PreviewDuration);
                projection.GetPart<Temporary>().LastTurn = The.Game.Turns;
                Cell target = origin.ParentZone.GetCell(x, y);
                Cleo.TerraFirma.Scripts.SeismicRise arrival = null;
                // Skip overlays where real walls occupy the destination or the short rise path.
                string skip = RiseSeconds <= 0 ? "animation disabled"
                    : !target.ParentZone.IsActive() ? "inactive zone"
                    : target.GetFirstWall() != null ? "real wall in destination"
                    : y + 1 < target.ParentZone.Height && target.ParentZone.GetCell(x, y + 1).GetFirstWall() != null
                        ? "real wall immediately south" : null;
                if (skip == null)
                    arrival = new Cleo.TerraFirma.Scripts.SeismicRise(projection, target,
                        XRL.Rules.Stat.RandomCosmetic(0, Math.Max(0, RiseStaggerMs)) / 1000f,
                        RiseSeconds, RisePixels);
                else
                    Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHUFFLERS-RISE", $"{x},{y}: skipped ({skip})");
                // Hide before insertion so there is no frame showing the final wall before its rise.
                target.AddObject(projection);
                if (arrival != null)
                    CombatJuiceManager.enqueueEntry(arrival, async: true);
                Projections.Add(projection);
                walls++;
            }
            Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHUFFLERS",
                $"peek {below.ParentZone.ZoneID}: {walls} projected walls, {PreviewDuration} turns");
            // after the scan, so an empty one clicks instead of rising (user 2026-10-19); the rises
            // are queued async, so the sound still lands as they start
            string sound = walls > 0 ? PeekSound : EmptyPeekSound;
            if (!sound.IsNullOrEmpty())
                ParentObject.PlayWorldSound(sound, walls > 0 ? PeekSoundVolume : EmptyPeekSoundVolume);
            if (walls == 0)
                wearer.ShowFailure("You find no walls beneath you.");
            wearer.UseEnergy(1000, "Item Seismic Peek");
            return false;
        }

        public const string PROJECTION_BLUEPRINT = "Cleo_TerraFirma_SeismicProjection";

        private static bool IsOurProjection(GameObject projection)
        {
            return GameObject.Validate(projection) && projection.Blueprint == PROJECTION_BLUEPRINT
                && projection.CurrentCell != null;
        }

        private void ClearProjections()
        {
            foreach (GameObject projection in Projections)
                if (IsOurProjection(projection))
                    projection.Obliterate(Silent: true);
            Projections.Clear();
        }
    }
}
