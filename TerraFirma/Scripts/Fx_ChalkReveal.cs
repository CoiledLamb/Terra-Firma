using ConsoleLib.Console;
using Kobold;
using System.Threading;
using UnityEngine;
using XRL;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// The chalk draw reveal (user shape 2026-08-23, their words: "applying an opaque
	/// background mask over the tile, having a sort of small puff of white particles, like
	/// what's used for fires, which reveals/dispels the mask in a dynamic motion").
	///
	/// Mechanism: four opaque quads wearing the cell's OWN floor paint tile them seamlessly
	/// from frame zero; the line object is placed SWAP_SEC later by the caller (Hold), so the
	/// juice queue's spin-up window shows floor-over-floor -- there is no frame in which the
	/// stroke can pop in (the carve-swap lesson from Fx_StoneRise, whose spin-up scar was
	/// "the walls aren't rendering during the animation"). The quads then depart staggered in
	/// random order while white motes wearing the stroke's own tile burst outward.
	///
	/// Chassis provenance: quads = JuiceCarveBlock (sub-rect atlas clones at natural size,
	/// opaque backing, normals-stomp color repair, park-behind-map exit, pool-state restore);
	/// motes = JuiceWhorlFleck (Transparent pooled sprite, settled fleck scale 0.22-0.34,
	/// canFinishUpToTurn declined). Stock Impacts/ImpactVFXFlameburst was ruled out on ground
	/// truth: not in the ParticleVFX config family, no color channel, cannot be white.
	/// Unseen/classic/unpainted cells return 0 and the caller places instantly -- the beat
	/// degrades to the pre-VFX behavior, never blocks the draw.
	/// </summary>
	public static class Cleo_TerraFirma_ChalkRevealFX
	{
		// dials, all CLAUDE DRAFT (mock: design/chalk-vfx-mock.html, frames approved 2026-08-23)
		public const float SWAP_SEC = 0.20f;        // caller holds this long, then places the line hidden under the mask
		private const float FRAG_STAGGER = 0.05f;   // departures, shuffled order
		private const float FRAG_LEAVE = 0.10f;     // one quad's drift-out window
		internal const float FRAG_DRIFT = 3f;       // px outward while leaving (the carve chunk's own lean)
		private const int MOTE_COUNT = 5;           // central burst; feel round 2 (user: "too many motes, dial it back a bit") from round 1's 8
		private const float MOTE_LIFE = 0.30f;
		private const float MOTE_R0 = 2f;           // born tight on the stroke
		private const float MOTE_R1 = 11f;          // flung toward the cell edge
		internal const float MOTE_RISE = 3f;        // px of upward drift across a mote's life
		private const float MOTE_SCALE_MIN = 0.22f; // the whorl's SETTLED fleck range, not renegotiated
		private const float MOTE_SCALE_MAX = 0.30f;
		private const float MOTE_STAGGER = 0.03f;
		private const int QUAD_PUFF_COUNT = 2;      // dust riding EACH fragment's departure; feel round 2, from 3
		private const float QUAD_PUFF_R0 = 1f;      // born on the quad itself
		private const float QUAD_PUFF_R1 = 7f;      // shorter throw; the riding dust hugs the cell
		// feel round 2 (user: "lighten the color more"): the motes WORE the 1px stroke tile, and
		// at fleck scale that is a sub-pixel sliver the filtering blends into the floor - white
		// rendered as gray. The whorl's own mote tile is a chunky blob that survives downscaling
		// ("guaranteed atlas resident", its journal note); solid chalk-white on both channels.
		private const string MOTE_TILE = "Items/sw_smallstone.bmp";
		internal const float FADE_OUT_FRAC = 0.25f;
		internal const float MASK_Z = -10f;         // carve block's own layer: in front of map tiles
		internal const float MOTE_Z = -12f;         // in front of the mask
		internal const float PARK_Z = 100f;         // behind the map

		/// <summary>Enqueues the reveal over the target cell. Returns how long the caller must
		/// Hold before adding the line object (0 = no beat, place immediately). The mask wears
		/// the cell's floor paint, so this must run BEFORE the line exists in the cell.</summary>
		public static float Play(Cell target)
		{
			if (target == null)
				return 0f;
			GameManager gm = GameManager.Instance;
			if (gm == null)
				return 0f;
			Zone zone = target.ParentZone;
			if (zone == null || !zone.IsActive() || !target.IsVisible())
				return 0f;
			// the mask promises "the cell keeps looking like bare floor"; without a painted
			// floor tile (classic render, unpainted cell) that promise cannot be kept
			if (!XRLCore.RenderFloorTextures || target.PaintTile.IsNullOrEmpty())
				return 0f;
			char fg = FirstColorChar(target.PaintTileColor.IsNullOrEmpty() ? target.PaintColorString : target.PaintTileColor, 'y');
			char detail = FirstColorChar(target.PaintDetailColor, 'k');
			IRenderable floor = new Cleo_TerraFirma_StoneTileRenderable(target.PaintTile, fg, detail, 'k');
			Vector3 center = gm.getTileCenter(target.X, target.Y);
			int[] order = { 0, 1, 2, 3 };
			for (int i = 3; i > 0; i--)
			{
				int j = Stat.RandomCosmetic(0, i);
				(order[i], order[j]) = (order[j], order[i]);
			}
			IRenderable mote = new Cleo_TerraFirma_StoneTileRenderable(MOTE_TILE, 'Y', 'Y', 'k');
			for (int q = 0; q < 4; q++)
			{
				float leaveAt = SWAP_SEC + order[q] * FRAG_STAGGER;
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceChalkMaskFrag(floor, center, q, leaveAt, FRAG_LEAVE, coverAllOnFallback: q == 0), async: true);
				// each departing quad exhales its own small puff (feel round 1: "more puffs of
				// white dust") -- born on the quad, thrown shorter than the central burst;
				// offsets mirror the frag class's quad geometry
				Vector3 quadOffset = new Vector3(q % 2 == 0 ? -4f : 4f, q / 2 == 0 ? 6f : -6f, 0f);
				for (int m = 0; m < QUAD_PUFF_COUNT; m++)
				{
					float ang = Stat.RandomCosmetic(0, 359) * (Mathf.PI / 180f);
					float scale = Mathf.Lerp(MOTE_SCALE_MIN, MOTE_SCALE_MAX, Stat.RandomCosmetic(0, 100) * 0.01f);
					CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceChalkMote(mote, center + quadOffset, ang, leaveAt + m * 0.02f, MOTE_LIFE, scale, QUAD_PUFF_R0, QUAD_PUFF_R1), async: true);
				}
			}
			for (int m = 0; m < MOTE_COUNT; m++)
			{
				float ang = (m + 0.5f) * (Mathf.PI * 2f / MOTE_COUNT) + Stat.RandomCosmetic(-30, 30) * 0.01f;
				float scale = Mathf.Lerp(MOTE_SCALE_MIN, MOTE_SCALE_MAX, Stat.RandomCosmetic(0, 100) * 0.01f);
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceChalkMote(mote, center, ang, SWAP_SEC + (m % 3) * MOTE_STAGGER, MOTE_LIFE, scale, MOTE_R0, MOTE_R1), async: true);
			}
			return SWAP_SEC;
		}

		/// <summary>The fissure pump idiom: hold the game thread while Unity composites, so the
		/// mask is reliably rendering before the caller mutates game state behind it.</summary>
		public static void Hold(float seconds)
		{
			ScreenBuffer buf = ScreenBuffer.GetScrapBuffer1(bLoadFromCurrent: true);
			TextConsole console = Popup._TextConsole;
			float t = 0f;
			while (t < seconds)
			{
				XRLCore.Core.RenderBaseToBuffer(buf);
				console.DrawBuffer(buf);
				Thread.Sleep(40);
				t += 0.04f;
			}
		}

		/// <summary>"&amp;w^k" / "&amp;w" / "w" -> 'w'; paint color strings are stored with
		/// their sigils in the cell fields.</summary>
		private static char FirstColorChar(string paint, char fallback)
		{
			if (paint.IsNullOrEmpty())
				return fallback;
			for (int i = 0; i < paint.Length; i++)
			{
				char c = paint[i];
				if (c != '&' && c != '^')
					return c;
			}
			return fallback;
		}
	}

	/// <summary>
	/// One quarter of the floor mask. All four assemble the cell's paint tile seamlessly at
	/// natural size (sub-rect clones; integer quarter offsets so no half-pixel seams), hold
	/// through the swap window, then drift outward a few px and park at their moment. On a
	/// rotated/diced atlas entry quads cannot sub-rect: quad 0 wears the WHOLE tile and
	/// shrinks to nothing over its window instead, quads 1-3 park unseen (single-cover
	/// fallback, the CarveBlock squash lesson).
	/// </summary>
	public class Cleo_TerraFirma_JuiceChalkMaskFrag : CombatJuiceEntry
	{
		private IRenderable Tile;
		private ex3DSprite2 Target;
		private Vector3 Center;
		private int Quad;              // 0..3: col = Quad % 2, row = Quad / 2 (row 0 = top)
		private float LeaveAt;
		private float LeaveSec;
		private bool CoverAllOnFallback;
		private bool Dead;
		private bool FallbackWhole;
		private Vector3 QuadOffset;
		private Vector3 OrigScale;
		private bool ScaleTouched;
		private exTextureInfo BaseInfo;
		private exTextureInfo SubInfo;
		private Color Foreground;
		private Color Background;
		private Color Detail;
		private Color DetailNudge;

		public Cleo_TerraFirma_JuiceChalkMaskFrag(IRenderable tile, Vector3 center, int quad, float leaveAt, float leaveSec, bool coverAllOnFallback)
		{
			Tile = tile;
			Center = center;
			Quad = quad;
			LeaveAt = leaveAt;
			LeaveSec = Mathf.Max(0.01f, leaveSec);
			CoverAllOnFallback = coverAllOnFallback;
			duration = leaveAt + this.LeaveSec + 0.02f;
		}

		public override bool canFinishUpToTurn() => false;

		public override void start()
		{
			Target = SpriteManager.GetPooledSprite(Tile, Transparent: false);
			Foreground = Target.color;
			Background = Target.backcolor;
			Detail = Target.detailcolor;
			DetailNudge = new Color(Detail.r, Detail.g, Detail.b, 0.996f);
			Transform transform = Target.gameObject.transform;
			transform.SetParent(GameManager.Instance.TileRoot.transform);
			OrigScale = transform.localScale;
			ScaleTouched = false;
			BaseInfo = Target.textureInfo;
			SubInfo = null;
			Dead = false;
			FallbackWhole = false;
			int col = Quad % 2;
			int row = Quad / 2;
			if (BaseInfo != null && !BaseInfo.rotated && !BaseInfo.isDiced)
			{
				// atlas y runs bottom-up (the StoneChunk flip lesson): top row samples fy 0.5
				SubInfo = Cleo_TerraFirma_StoneRiseFX.CloneSubRect(BaseInfo, col == 0 ? 0f : 0.5f, row == 0 ? 0.5f : 0f, 0.5f, 0.5f);
				Target.textureInfo = SubInfo;
				QuadOffset = new Vector3(col == 0 ? -4f : 4f, row == 0 ? 6f : -6f, 0f);
			}
			else if (CoverAllOnFallback)
			{
				FallbackWhole = true;
				QuadOffset = Vector3.zero;
			}
			else
			{
				Dead = true;
				transform.position = Center + new Vector3(0f, 0f, Cleo_TerraFirma_ChalkRevealFX.PARK_Z);
				return;
			}
			transform.position = Center + QuadOffset + new Vector3(0f, 0f, Cleo_TerraFirma_ChalkRevealFX.MASK_Z);
		}

		public override void update()
		{
			if (Target == null || Dead)
				return;
			// normals-stomp repair, verbatim house rule (first-use FillBuffers overwrites the
			// detail channel with geometric normals)
			Target.color = Foreground;
			Target.backcolor = Background;
			Target.detailcolor = DetailNudge;
			Target.detailcolor = Detail;
			if (t < LeaveAt)
			{
				Target.transform.position = Center + QuadOffset + new Vector3(0f, 0f, Cleo_TerraFirma_ChalkRevealFX.MASK_Z);
				return;
			}
			float p = (t - LeaveAt) / LeaveSec;
			if (p >= 1f)
			{
				Target.transform.position = Center + new Vector3(0f, 0f, Cleo_TerraFirma_ChalkRevealFX.PARK_Z);
				return;
			}
			float ease = 1f - (1f - p) * (1f - p);
			if (FallbackWhole)
			{
				Target.gameObject.transform.localScale = OrigScale * Mathf.Max(0.01f, 1f - ease);
				ScaleTouched = true;
				Target.transform.position = Center + new Vector3(0f, 0f, Cleo_TerraFirma_ChalkRevealFX.MASK_Z);
			}
			else
			{
				Vector3 dir = QuadOffset.normalized;
				Target.transform.position = Center + QuadOffset + dir * (Cleo_TerraFirma_ChalkRevealFX.FRAG_DRIFT * ease) + new Vector3(0f, 0f, Cleo_TerraFirma_ChalkRevealFX.MASK_Z);
			}
		}

		public override void finish()
		{
			if (Target != null)
			{
				if (ScaleTouched)
					Target.gameObject.transform.localScale = OrigScale;
				if (SubInfo != null && BaseInfo != null)
					Target.textureInfo = BaseInfo;
				SpriteManager.Return(Target);
				Target = null;
			}
			if (SubInfo != null)
			{
				// the clone is a ScriptableObject we own; destroy or it leaks per draw
				UnityEngine.Object.Destroy(SubInfo);
				SubInfo = null;
			}
			base.finish();
		}
	}

	/// <summary>
	/// One white chalk mote: a pooled sprite wearing the stroke's own tile at fleck scale,
	/// forced chalk-white, parked until its start, then flung radially ease-out with a slight
	/// rise, fading by scale over its last quarter. WhorlFleck lifecycle throughout.
	/// </summary>
	public class Cleo_TerraFirma_JuiceChalkMote : CombatJuiceEntry
	{
		private IRenderable Tile;
		private ex3DSprite2 Target;
		private Vector3 Center;
		private float Angle;
		private float StartAt;
		private float Life;
		private float Scale;
		private float R0;
		private float R1;
		private Vector3 OrigScale;
		private Color Background;

		public Cleo_TerraFirma_JuiceChalkMote(IRenderable tile, Vector3 center, float angle, float startAt, float life, float scale, float r0, float r1)
		{
			Tile = tile;
			Center = center;
			Angle = angle;
			StartAt = startAt;
			Life = Mathf.Max(0.01f, life);
			Scale = scale;
			R0 = r0;
			R1 = r1;
			duration = startAt + this.Life + 0.02f;
		}

		public override bool canFinishUpToTurn() => false;

		public override void start()
		{
			Target = SpriteManager.GetPooledSprite(Tile, Transparent: true);
			Background = Target.backcolor;
			Transform transform = Target.gameObject.transform;
			transform.SetParent(GameManager.Instance.TileRoot.transform);
			OrigScale = transform.localScale;
			transform.position = Center + new Vector3(0f, 0f, Cleo_TerraFirma_ChalkRevealFX.PARK_Z);
		}

		public override void update()
		{
			if (Target == null)
				return;
			float tm = t - StartAt;
			if (tm < 0f || tm >= Life)
			{
				Target.transform.position = Center + new Vector3(0f, 0f, Cleo_TerraFirma_ChalkRevealFX.PARK_Z);
				return;
			}
			// chalk-white every frame; the reassignment doubles as the normals-stomp repair
			Target.color = Color.white;
			Target.detailcolor = new Color(1f, 1f, 1f, 0.996f);
			Target.detailcolor = Color.white;
			Target.backcolor = Background;
			float p = tm / Life;
			float ease = 1f - (1f - p) * (1f - p);
			float r = Mathf.Lerp(R0, R1, ease);
			float fade = 1f;
			float fadeStart = 1f - Cleo_TerraFirma_ChalkRevealFX.FADE_OUT_FRAC;
			if (p > fadeStart)
				fade = 1f - (p - fadeStart) / Cleo_TerraFirma_ChalkRevealFX.FADE_OUT_FRAC;
			Target.gameObject.transform.localScale = OrigScale * (Scale * Mathf.Max(0.01f, fade));
			Target.transform.position = Center + new Vector3(
				Mathf.Cos(Angle) * r,
				Mathf.Sin(Angle) * r + Cleo_TerraFirma_ChalkRevealFX.MOTE_RISE * ease,
				Cleo_TerraFirma_ChalkRevealFX.MOTE_Z);
		}

		public override void finish()
		{
			if (Target != null)
			{
				Target.gameObject.transform.localScale = OrigScale; // pool state restore
				SpriteManager.Return(Target);
				Target = null;
			}
			base.finish();
		}
	}
}
