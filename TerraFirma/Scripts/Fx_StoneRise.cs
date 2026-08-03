using System;
using System.Collections.Generic;
using ConsoleLib.Console;
using Kobold;
using UnityEngine;
using XRL.Rules;
using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// The redoubt wave's emergence sprite (animation pass v2, user spec 2026-08-03: "a rock
	/// wall sprite sliding into the earth and briefly emerging" -- never fully hidden, no
	/// burrow idiom). A pooled sprite of the wall's own tile starts mostly buried below its
	/// cell, slides partway up, holds, and sinks back; rim instances rise near-full and hold
	/// until the real fabric takes over. Chassis = CombatJuiceEntryJump's pooled-sprite
	/// pattern (the thrown-object animation), tween shape ours. Pre-delay, the sprite parks
	/// BEHIND the map (z +100) so staggered cells stay unseen without ever "burrowing".
	/// </summary>
	public class Cleo_TerraFirma_JuiceStoneRise : CombatJuiceEntry
	{
		private IRenderable Tile;
		private ex3DSprite2 Target;
		private Vector3 Base;
		private float Delay;
		private float Rise;
		private float Hold;
		private float Sink;
		private float BuriedY;
		private float EmergeY;

		private bool StayUp; // rim pieces hold at full emergence until the real fabric takes over -- no dip before the handoff

		public Cleo_TerraFirma_JuiceStoneRise(IRenderable tile, Vector3 basePos, float delay, float rise, float hold, float sink, float buriedDepth, float emergeHeight, bool stayUp = false)
		{
			Tile = tile;
			Base = basePos;
			Delay = delay;
			Rise = Mathf.Max(0.01f, rise);
			Hold = hold;
			Sink = Mathf.Max(0.01f, sink);
			BuriedY = -buriedDepth;
			EmergeY = -buriedDepth + emergeHeight;
			StayUp = stayUp;
			duration = delay + this.Rise + hold + (stayUp ? 0f : this.Sink);
		}

		public override void start()
		{
			Target = SpriteManager.GetPooledSprite(Tile, Transparent: true);
			Transform transform = Target.gameObject.transform;
			transform.SetParent(GameManager.Instance.TileRoot.transform);
			transform.position = Base + new Vector3(0f, BuriedY, 100f); // parked behind the map until its cue
		}

		public override void update()
		{
			if (Target == null)
				return;
			float p = t - Delay;
			if (p < 0f)
				return;
			float y;
			if (p <= Rise)
				y = Mathf.Lerp(BuriedY, EmergeY, Easing.SineEaseOut(Mathf.Clamp01(p / Rise)));
			else if (StayUp || p <= Rise + Hold)
				y = EmergeY;
			else
				y = Mathf.Lerp(EmergeY, BuriedY, Easing.SineEaseInOut(Mathf.Clamp01((p - Rise - Hold) / Sink)));
			Target.transform.position = Base + new Vector3(0f, y, -10f);
		}

		public override void finish()
		{
			if (Target != null)
			{
				SpriteManager.Return(Target);
				Target = null;
			}
			base.finish();
		}
	}

	/// <summary>Enqueues the whole cast's emergence choreography and reports its total length
	/// (the game thread sleeps that long while the Unity side plays it out).</summary>
	public static class Cleo_TerraFirma_StoneRiseFX
	{
		// tempo dials (seconds)
		private const float SHELL_SEC = 0.10f;   // wave-front travel per shell
		private const float TRAVEL_RISE = 0.09f;
		private const float TRAVEL_HOLD = 0.03f;
		private const float TRAVEL_SINK = 0.10f;
		private const float RIM_STAGGER = 0.18f; // breakthrough spread ("softly", staggered)
		private const float RIM_RISE = 0.14f;
		private const float RIM_SINK = 0.06f;
		// tile geometry (world units; a tile is ~24 tall -- floatingText's +12 half-tile offset)
		private const float BURIED_DEPTH = 20f;
		private const float TRAVEL_EMERGE = 9f;  // a low shoulder of stone
		private const float RIM_EMERGE = 20f;    // near-full breakthrough

		/// <summary>Returns total choreography seconds; 0 = nothing enqueued.</summary>
		public static float Play(Cell origin, int radius, ICollection<Cell> reached, IRenderable wallTile, Func<int, int, int, bool> onRing)
		{
			if (origin == null || wallTile == null)
				return 0f;
			GameManager gm = GameManager.Instance;
			if (gm == null)
				return 0f;
			float rimStart = radius * SHELL_SEC;
			// linger trimmed (feel note 2026-08-03: "lingers a bit too long on the last bit"):
			// tail padding 0.20 -> 0.06, and rim pieces STAY UP into the handoff instead of
			// sinking first -- the real fabric appears as the held sprites vanish.
			float total = rimStart + RIM_STAGGER + RIM_RISE + 0.06f;
			int enqueued = 0;
			foreach (Cell c in reached)
			{
				if (!c.IsVisible())
					continue;
				int dx = c.X - origin.X;
				int dy = c.Y - origin.Y;
				bool rim = onRing(dx, dy, radius);
				Vector3 basePos = gm.getTileCenter(c.X, c.Y);
				if (rim)
				{
					float delay = rimStart + 0.01f * Stat.Random(0, (int)(RIM_STAGGER * 100f));
					float hold = total - delay - RIM_RISE; // every rim piece holds to the common end
					CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(wallTile, basePos, delay, RIM_RISE, hold, RIM_SINK, BURIED_DEPTH, RIM_EMERGE, stayUp: true), async: true);
					enqueued++;
					continue;
				}
				for (int r = 1; r < radius; r++)
				{
					if (onRing(dx, dy, r))
					{
						float delay = r * SHELL_SEC + 0.01f * Stat.Random(0, 4);
						CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceStoneRise(wallTile, basePos, delay, TRAVEL_RISE, TRAVEL_HOLD, TRAVEL_SINK, BURIED_DEPTH, TRAVEL_EMERGE), async: true);
						enqueued++;
						break;
					}
				}
			}
			return enqueued > 0 ? total : 0f;
		}
	}
}
