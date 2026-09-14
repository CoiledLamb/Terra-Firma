using System;
using ConsoleLib.Console;
using Kobold;
using UnityEngine;
using XRL.World;
using XRL.World.Parts;
using QudObject = XRL.World.GameObject;

namespace XRL.World.Parts
{
    [Serializable]
    public class Cleo_TerraFirma_SeismicArrival : IPart
    {
        // Presentation state must not persist into saves.
        [NonSerialized] public bool Rising;
        [NonSerialized] public long Deadline;

        public override bool Render(RenderEvent E)
        {
            if (Rising && System.Diagnostics.Stopwatch.GetTimestamp() < Deadline)
            {
                E.Tile = null;
                E.RenderString = "";
                return false;
            }
            Rising = false;
            return base.Render(E);
        }
    }
}

namespace Cleo.TerraFirma.Scripts
{
    // CombatJuiceEntryJump's pooled-sprite lifecycle, with a straight eased rise.
    // No opacity changes, impact, scale changes, or camera motion.
    public class SeismicRise : CombatJuiceEntry
    {
        private readonly QudObject Projection;
        private readonly Cell Cell;
        private readonly float Delay, Rise, Distance;
        private readonly Cleo_TerraFirma_SeismicArrival Arrival;
        private ex3DSprite2 Sprite;
        private Vector3 Rest;
        private Color Foreground, Background, Detail;

        public SeismicRise(QudObject projection, Cell cell, float delay, float rise, float distance)
        {
            Projection = projection;
            Cell = cell;
            Delay = Math.Max(0, delay);
            Rise = Math.Max(0.01f, rise);
            Distance = distance;
            duration = Delay + Rise;
            Arrival = projection.GetPart<Cleo_TerraFirma_SeismicArrival>();
            Arrival.Rising = true;
            Arrival.Deadline = System.Diagnostics.Stopwatch.GetTimestamp() + 2 * System.Diagnostics.Stopwatch.Frequency;
        }

        private bool Visible() => QudObject.Validate(Projection) && Projection.CurrentCell == Cell
            && Cell.ParentZone.IsActive() && Cell.IsVisible() && Cell.GetFirstWall() == null;

        public override void start()
        {
            if (!Visible()) { Arrival.Rising = false; return; }
            var tile = new Renderable(Projection.Render.Tile, Projection.Render.RenderString, "&C", null, 'c');
            Sprite = SpriteManager.GetPooledSprite(tile, Transparent: true);
            Foreground = Sprite.color;
            Background = Sprite.backcolor;
            Detail = Sprite.detailcolor;
            Sprite.transform.SetParent(GameManager.Instance.TileRoot.transform);
            Rest = GameManager.Instance.getTileCenter(Cell.X, Cell.Y);
            Sprite.transform.position = Rest + new Vector3(0, 0, 100);
        }

        public override void update()
        {
            if (Sprite == null) return;
            if (!Visible() || t >= duration || System.Diagnostics.Stopwatch.GetTimestamp() >= Arrival.Deadline)
            {
                Arrival.Rising = false;
                Sprite.transform.position = Rest + new Vector3(0, 0, 100);
                return;
            }
            if (t < Delay) return;
            Sprite.color = Foreground;
            Sprite.backcolor = Background;
            Sprite.detailcolor = Detail;
            float p = Mathf.Clamp01((t - Delay) / Rise);
            float eased = 1f - (1f - p) * (1f - p);
            Sprite.transform.position = Rest + new Vector3(0, -Distance * (1f - eased), -10);
        }

        public override void finish()
        {
            Arrival.Rising = false;
            if (Sprite != null) { SpriteManager.Return(Sprite); Sprite = null; }
            base.finish();
        }
    }
}
