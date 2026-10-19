using System.Collections.Generic;
using ConsoleLib.Console;
using Kobold;
using UnityEngine;
using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// Gorgon's Gaze petrification buildup (design conversation 2026-08-06, user ruling:
	/// the RISING STONE BAND -- "the stone is climbing them"). A stone-colorized copy of
	/// the target's OWN tile, clipped to its bottom fraction by sub-rect atlas clones
	/// (the carve reveal's mechanism), overlaid on the target in place: legs first, the
	/// stone line climbing with each failed save and sliding back down with each success.
	/// The band tracks NET state (fails minus successes) -- the tug-of-war read, per the
	/// user's "both the animation and the breaking out" -- while the statue still counts
	/// CUMULATIVE fails; that divergence is deliberate and recorded in TODO.
	///
	/// Turn/realtime bridge: ONE long-lived juice entry per channel, driven through a
	/// shared state object the game thread writes and the Unity side reads. No juice
	/// entry ever touches a live XRL.World.GameObject -- the game thread snapshots the
	/// tile once and thereafter only pokes plain fields (volatile, word-sized). Entry
	/// death: superseded flag, shrink-out completion, a global generation bump (save
	/// load), or the lifetime cap -- every path parks the sprite behind the map first
	/// (the "error shale" lesson) and self-finishes by zeroing its own duration, which
	/// the manager's per-frame loop collects the same frame. The band must also DECLINE
	/// the turn sweep: CombatJuice.startTurn force-finishes every default entry two
	/// turns after enqueue (clearUpToTurn), and a channel lives for many turns --
	/// canFinishUpToTurn() => false is the engine's own escape hatch for exactly this.
	/// Zero contact with Fx_StoneRise.cs beyond reusing its public CloneSubRect.
	/// </summary>
	public class Cleo_TerraFirma_PetrifyBandState
	{
		/// <summary>Game thread orders the band gone NOW (no shrink). The entry parks and dies.</summary>
		public volatile bool Superseded;

		/// <summary>Game thread orders a shrink-out: the band slides to zero, then the entry dies.</summary>
		public volatile bool ShrinkOut;

		/// <summary>Set by the entry's finish() -- the game thread may then create a replacement.</summary>
		public volatile bool Dead;

		/// <summary>Target's cell as X + Y * 1000 (single word, so a mid-write read can't tear the pair).</summary>
		public volatile int PackedCell;

		/// <summary>Band height the entry lerps toward, 0..1 of the tile's height.</summary>
		public volatile float TargetFrac;

		/// <summary>GlobalGen at creation; a later bump (save load) orphans this state's entry.</summary>
		public int Gen;
	}

	/// <summary>An OWNED copy of a creature's look for the band sprite. RenderForUI returns a
	/// REUSED per-object buffer on stable (GameObject._contextRender, Reset each call) and a
	/// Visual struct on lang-experimental; either way its tile and flips are copied here at
	/// snapshot time and nothing of it is kept. Colors are the stone
	/// dressing, not the creature's: the band is the petrified part.</summary>
	public class Cleo_TerraFirma_CreatureSnapshotRenderable : IRenderable
	{
		private string TilePath;
		private char Fg;
		private char Detail;
		private char Bg;
		private bool HFlip;
		private bool VFlip;

		public Cleo_TerraFirma_CreatureSnapshotRenderable(string tilePath, char fg, char detail, char bg, bool hFlip, bool vFlip)
		{
			TilePath = tilePath;
			Fg = fg;
			Detail = detail;
			Bg = bg;
			HFlip = hFlip;
			VFlip = vFlip;
		}

		public string getTile() => TilePath;
		public string getRenderString() => "@";
		public string getColorString() => "&" + Fg + "^" + Bg;
		public string getTileColor() => "&" + Fg + "^" + Bg;
		public char getDetailColor() => Detail;
		public ColorChars getColorChars() => new ColorChars { foreground = Fg, background = Bg, detail = Detail };
		public bool getHFlip() => HFlip;
		public bool getVFlip() => VFlip;
	}

	/// <summary>
	/// The band itself: a pooled sprite of the target's tile in stone colors, textureInfo
	/// swapped to bottom-band sub-rect clones as the displayed fraction moves (exact pixel
	/// heights -- the Kobold atlas stores full untrimmed rects, AtlasDefinition sets
	/// trimElements false, so bottom-anchor math is exact). Transparent backing ('k' = alpha
	/// 0): only the silhouette draws -- the opposite call from the wall sprites, as ruled.
	/// Chassis rules inherited: normals-stomp color repair every frame, park-behind-map on
	/// every exit, every piece of pool state (scale, textureInfo) captured and restored,
	/// sub-rect clones destroyed at finish. Rotated/diced atlas entries fall back to the
	/// carve block's vertical-squash look. VFlip targets get the squash fallback's anchor
	/// wrong-way-up; no base-game creature ships VFlipped -- accepted watch item.
	/// </summary>
	public class Cleo_TerraFirma_JuicePetrifyBand : CombatJuiceEntry
	{
		private IRenderable Tile;
		private Cleo_TerraFirma_PetrifyBandState State;
		private int Gen;
		private ex3DSprite2 Target; // first-acquired sprite; the whole show in fallback mode, Band[0] otherwise
		private float Shown;
		private Vector3 OrigScale;
		private bool ScaleFallback;
		private exTextureInfo BaseInfo;
		private bool SubRectable;
		private bool HFlipped;
		private Color Foreground;
		private Color Background;
		private Color Detail;
		private Color DetailNudge;
		// the serrated band (feel round 5, user: "i was just thinking a serrated/dithered
		// edge"): SERRATION_COLS independent column strips, each biting 0..DEPTH px below
		// the true height on a deterministic pattern that shifts as the band climbs --
		// real teeth, honest (never above the true fill)
		private ex3DSprite2[] Band;
		private Dictionary<int, exTextureInfo>[] BandCache; // per column, keyed by that column's height
		private int[] AppliedBandPx;
		// the waterline (feel rounds 2+3): stacked 1px rows riding each column's own top
		// edge (the line follows the serration), art's two-channel structure kept, lower
		// row a step darker, both breathing toward stone gray on offset phases
		private ex3DSprite2[] Line; // index = row * COLS + col
		private Color[] LineFg;
		private Color[] LineDetail;
		private Dictionary<int, exTextureInfo>[] LineCache;
		private int[] AppliedLinePx;
		private Color StoneGray;

		public Cleo_TerraFirma_JuicePetrifyBand(IRenderable tile, Cleo_TerraFirma_PetrifyBandState state, Color mixer, Color creatureDetail)
		{
			Tile = tile;
			State = state;
			Gen = state.Gen;
			duration = Cleo_TerraFirma_GorgonPetrifyFX.LIFE_CAP_SEC;
			// row palettes, top to bottom: the frontier row wears mixer over the creature's
			// detail color (the art's pixels split the line into two live shades); the lower
			// row steps everything toward stone -- a gradient into the gray beneath
			StoneGray = ConsoleLib.Console.ColorUtility.ColorMap.GetValue(Cleo_TerraFirma_GorgonPetrifyFX.STONE_DETAIL);
			LineFg = new Color[Cleo_TerraFirma_GorgonPetrifyFX.LINE_ROWS];
			LineDetail = new Color[Cleo_TerraFirma_GorgonPetrifyFX.LINE_ROWS];
			for (int r = 0; r < Cleo_TerraFirma_GorgonPetrifyFX.LINE_ROWS; r++)
			{
				float darken = r * Cleo_TerraFirma_GorgonPetrifyFX.LINE_ROW_DARKEN;
				LineFg[r] = Color.Lerp(mixer, StoneGray, darken);
				LineDetail[r] = Color.Lerp(creatureDetail, StoneGray, darken);
			}
		}

		/// <summary>Decline the per-turn sweep (see class doc) -- the band outlives many turns
		/// and ends only by its state flags. The sweep marks us async and moves on.</summary>
		public override bool canFinishUpToTurn() => false;

		public override void start()
		{
			Vector3 parked = CellCenter() + new Vector3(0f, 0f, Cleo_TerraFirma_GorgonPetrifyFX.Z_PARKED);
			Target = SpriteManager.GetPooledSprite(Tile, Transparent: true);
			Foreground = Target.color;
			Background = Target.backcolor;
			Detail = Target.detailcolor;
			DetailNudge = new Color(Detail.r, Detail.g, Detail.b, 0.996f);
			Transform transform = Target.gameObject.transform;
			transform.SetParent(GameManager.Instance.TileRoot.transform);
			OrigScale = transform.localScale; // includes any flip signs GetPooledSprite applied
			ScaleFallback = false;
			Shown = 0f;
			BaseInfo = Target.textureInfo;
			SubRectable = BaseInfo != null && !BaseInfo.rotated && !BaseInfo.isDiced;
			HFlipped = Tile.getHFlip();
			transform.position = parked;
			// serrated columns + waterline ride only the sub-rect path (the squash fallback
			// has no honest pixel edge to serrate); all sprites share the SAME cached atlas
			// info -- never mutated, only cloned, per the CloneSubRect contract
			Band = null;
			BandCache = null;
			AppliedBandPx = null;
			Line = null;
			LineCache = null;
			AppliedLinePx = null;
			if (SubRectable)
			{
				int cols = Cleo_TerraFirma_GorgonPetrifyFX.SERRATION_COLS;
				int rows = Cleo_TerraFirma_GorgonPetrifyFX.LINE_ROWS;
				Band = new ex3DSprite2[cols];
				BandCache = new Dictionary<int, exTextureInfo>[cols];
				AppliedBandPx = new int[cols];
				Band[0] = Target; // the first-acquired sprite becomes column 0
				for (int c = 0; c < cols; c++)
				{
					if (c > 0)
					{
						Band[c] = SpriteManager.GetPooledSprite(Tile, Transparent: true);
						Transform bt = Band[c].gameObject.transform;
						bt.SetParent(GameManager.Instance.TileRoot.transform);
						bt.position = parked;
					}
					BandCache[c] = new Dictionary<int, exTextureInfo>();
				}
				Line = new ex3DSprite2[rows * cols];
				LineCache = new Dictionary<int, exTextureInfo>[rows * cols];
				AppliedLinePx = new int[rows * cols];
				for (int i = 0; i < Line.Length; i++)
				{
					Line[i] = SpriteManager.GetPooledSprite(Tile, Transparent: true);
					LineCache[i] = new Dictionary<int, exTextureInfo>();
					Transform lt = Line[i].gameObject.transform;
					lt.SetParent(GameManager.Instance.TileRoot.transform);
					lt.position = parked;
				}
			}
		}

		public override void update()
		{
			if (Target == null)
				return;
			// normals-stomp repair, verbatim idiom from the rise entry (first-use FillBuffers
			// overwrites the detail channel with geometric normals -> black detail; textureInfo
			// swaps re-queue the stomping flush, so the repair earns its keep here every frame)
			if (Band != null)
			{
				for (int c = 0; c < Band.Length; c++)
				{
					Band[c].color = Foreground;
					Band[c].backcolor = Background;
					Band[c].detailcolor = DetailNudge;
					Band[c].detailcolor = Detail;
				}
			}
			else
			{
				Target.color = Foreground;
				Target.backcolor = Background;
				Target.detailcolor = DetailNudge;
				Target.detailcolor = Detail;
			}
			if (Line != null)
			{
				// per-row recolor with the breath: each row's palette leans toward stone gray
				// and back on a slow offset sine -- the line reads as WORKING at the frontier.
				// Setting colors every frame doubles as the normals-stomp repair.
				int cols = Cleo_TerraFirma_GorgonPetrifyFX.SERRATION_COLS;
				for (int i = 0; i < Line.Length; i++)
				{
					int r = i / cols;
					float breath = Cleo_TerraFirma_GorgonPetrifyFX.LINE_SHIMMER_AMP *
						(0.5f + 0.5f * Mathf.Sin(t * (2f * Mathf.PI / Cleo_TerraFirma_GorgonPetrifyFX.LINE_SHIMMER_SEC) + r * Cleo_TerraFirma_GorgonPetrifyFX.LINE_SHIMMER_PHASE));
					Color fg = Color.Lerp(LineFg[r], StoneGray, breath);
					Color dt = Color.Lerp(LineDetail[r], StoneGray, breath);
					Line[i].color = fg;
					Line[i].backcolor = Background;
					Line[i].detailcolor = new Color(dt.r, dt.g, dt.b, 0.996f);
					Line[i].detailcolor = dt;
				}
			}
			if (State.Superseded || Gen != Cleo_TerraFirma_GorgonPetrifyFX.GlobalGen)
			{
				Die();
				return;
			}
			float goal = State.ShrinkOut ? 0f : State.TargetFrac;
			float rate = State.ShrinkOut ? Cleo_TerraFirma_GorgonPetrifyFX.SHRINK_OUT_PER_SEC : Cleo_TerraFirma_GorgonPetrifyFX.GROW_PER_SEC;
			Shown = Mathf.MoveTowards(Shown, goal, rate * Time.deltaTime);
			if (State.ShrinkOut && Shown <= 0.001f)
			{
				Die();
				return;
			}
			Vector3 center = CellCenter();
			int fullH = BaseInfo != null ? BaseInfo.height : 24;
			int px = Mathf.Clamp(Mathf.RoundToInt(Shown * fullH), 0, fullH);
			if (px <= 0)
			{
				// nothing to show yet (net dithered back to zero) -- stay parked, stay alive
				ParkAll(center);
				return;
			}
			if (SubRectable)
			{
				int cols = Band.Length;
				float colWFrac = 1f / cols;
				float colW = BaseInfo.width / (float)cols;
				float halfW = BaseInfo.width * 0.5f;
				float halfH = fullH * 0.5f;
				for (int c = 0; c < cols; c++)
				{
					// each column bites 0..DEPTH px below the true height; the pattern is a
					// pure function of (column, height) -- stable between frames, crawling as
					// the band climbs. Never above the honest fill, never cut to nothing.
					int colPx = Mathf.Max(1, px - SerrationDepth(c, px));
					// GetPooledSprite renders HFlip by mirroring each sprite about its own
					// center, so reassembling the full image under flip means mirroring the
					// column POSITIONS too
					float baseX = -halfW + (c + 0.5f) * colW;
					float worldX = HFlipped ? -baseX : baseX;
					if (colPx != AppliedBandPx[c])
					{
						if (!BandCache[c].TryGetValue(colPx, out exTextureInfo sub))
						{
							// fractions of the rect reproduce exact pixels (16/COLS and px/fullH
							// both round back to their integer sources)
							sub = Cleo_TerraFirma_StoneRiseFX.CloneSubRect(BaseInfo, c * colWFrac, 0f, colWFrac, colPx / (float)fullH);
							BandCache[c][colPx] = sub;
						}
						Band[c].textureInfo = sub;
						AppliedBandPx[c] = colPx;
					}
					Band[c].transform.position = center + new Vector3(worldX, -halfH + colPx * 0.5f, Cleo_TerraFirma_GorgonPetrifyFX.Z_FRONT);
					if (Line != null)
					{
						// the waterline rides each column's own serrated top edge; a row that
						// would fall below the tile's bottom parks (a 1px column has one row)
						for (int r = 0; r < Cleo_TerraFirma_GorgonPetrifyFX.LINE_ROWS; r++)
						{
							int i = r * cols + c;
							int rowTop = colPx - r;
							int rowBottom = rowTop - 1;
							if (rowBottom < 0)
							{
								Line[i].transform.position = center + new Vector3(0f, 0f, Cleo_TerraFirma_GorgonPetrifyFX.Z_PARKED);
								continue;
							}
							if (rowTop != AppliedLinePx[i])
							{
								if (!LineCache[i].TryGetValue(rowTop, out exTextureInfo slice))
								{
									slice = Cleo_TerraFirma_StoneRiseFX.CloneSubRect(BaseInfo, c * colWFrac, rowBottom / (float)fullH, colWFrac, 1f / fullH);
									LineCache[i][rowTop] = slice;
								}
								Line[i].textureInfo = slice;
								AppliedLinePx[i] = rowTop;
							}
							Line[i].transform.position = center + new Vector3(worldX, -halfH + rowBottom + 0.5f, Cleo_TerraFirma_GorgonPetrifyFX.Z_LINE);
						}
					}
				}
			}
			else
			{
				// rotated/diced atlas entry: the carve block's vertical-squash fallback
				Target.gameObject.transform.localScale = new Vector3(OrigScale.x, OrigScale.y * Shown, OrigScale.z);
				ScaleFallback = true;
				Target.transform.position = center + new Vector3(0f, -Cleo_TerraFirma_GorgonPetrifyFX.HALF_TILE * (1f - Shown), Cleo_TerraFirma_GorgonPetrifyFX.Z_FRONT);
			}
		}

		/// <summary>0..SERRATION_DEPTH_PX bite for a column at a band height -- deterministic
		/// (no state, no RNG: stable between frames, crawling as the band moves). A real
		/// integer hash, not a linear formula: linear multipliers make a repeating staircase
		/// (a slanted edge), where teeth want to look broken.</summary>
		private static int SerrationDepth(int col, int px)
		{
			unchecked
			{
				uint h = (uint)(col * 374761393 + px * 668265263);
				h ^= h >> 13;
				h *= 1274126177u;
				h ^= h >> 16;
				return (int)(h % (uint)(Cleo_TerraFirma_GorgonPetrifyFX.SERRATION_DEPTH_PX + 1));
			}
		}

		private Vector3 CellCenter()
		{
			int packed = State.PackedCell;
			return gameManager.getTileCenter(packed % 1000, packed / 1000);
		}

		private void ParkAll(Vector3 center)
		{
			Vector3 parked = center + new Vector3(0f, 0f, Cleo_TerraFirma_GorgonPetrifyFX.Z_PARKED);
			if (Band != null)
			{
				for (int c = 0; c < Band.Length; c++)
					Band[c].transform.position = parked;
			}
			else
				Target.transform.position = parked;
			if (Line != null)
			{
				for (int i = 0; i < Line.Length; i++)
					Line[i].transform.position = parked;
			}
		}

		/// <summary>Park behind the map NOW, then self-finish: zeroing duration makes the
		/// manager's per-frame loop (t > duration) collect us this same frame -- no waiting
		/// on turn cadence, no lagging opaque sprite (the "error shale" lesson).</summary>
		private void Die()
		{
			ParkAll(CellCenter());
			duration = 0f;
		}

		public override void finish()
		{
			if (Band != null)
			{
				// pool-contamination guards: textureInfo is pool state, restored per sprite
				for (int c = 0; c < Band.Length; c++)
				{
					if (Band[c] == null)
						continue;
					if (BaseInfo != null && AppliedBandPx[c] > 0)
						Band[c].textureInfo = BaseInfo;
					SpriteManager.Return(Band[c]);
					Band[c] = null;
				}
				Band = null;
				Target = null; // Band[0] was the same sprite
			}
			else if (Target != null)
			{
				// fallback mode: scale is the pool state that moved
				if (ScaleFallback)
					Target.gameObject.transform.localScale = OrigScale;
				SpriteManager.Return(Target);
				Target = null;
			}
			if (Line != null)
			{
				for (int i = 0; i < Line.Length; i++)
				{
					if (Line[i] == null)
						continue;
					if (BaseInfo != null && AppliedLinePx[i] > 0)
						Line[i].textureInfo = BaseInfo;
					SpriteManager.Return(Line[i]);
					Line[i] = null;
				}
				Line = null;
			}
			if (BandCache != null)
			{
				// the clones are ScriptableObjects we own; destroy or they leak per channel
				for (int c = 0; c < BandCache.Length; c++)
				{
					if (BandCache[c] == null)
						continue;
					foreach (exTextureInfo sub in BandCache[c].Values)
					{
						if (sub != null)
							Object.Destroy(sub);
					}
				}
				BandCache = null;
			}
			if (LineCache != null)
			{
				for (int i = 0; i < LineCache.Length; i++)
				{
					if (LineCache[i] == null)
						continue;
					foreach (exTextureInfo slice in LineCache[i].Values)
					{
						if (slice != null)
							Object.Destroy(slice);
					}
				}
				LineCache = null;
			}
			State.Dead = true;
			base.finish();
		}
	}

	/// <summary>
	/// One small rock popping off a petrifying target (feel round 1, user 2026-08-06:
	/// "instead of * coming off the enemy as you petrify them, maybe we could have small
	/// rock textures pop out periodically?"). A pooled sprite of a base-game stone item
	/// tile at small scale on the carve debris's ballistic arc -- a short hop, gravity,
	/// gone. Fired per failed save and as a burst under the statue swap. Declines the
	/// turn sweep like the band (a fast round could otherwise force-finish it mid-arc);
	/// its own short duration collects it per-frame.
	/// </summary>
	public class Cleo_TerraFirma_JuicePetrifyChunk : CombatJuiceEntry
	{
		private IRenderable Tile;
		private ex3DSprite2 Target;
		private Vector3 Origin;
		private float Vx;
		private float Vy0;
		private float Air;
		private float Scale;
		private Vector3 OrigScale;
		private Color Foreground;
		private Color Background;
		private Color Detail;
		private Color DetailNudge;
		private const float GRAVITY = 1000f; // px/s^2, the carve debris's fall

		public Cleo_TerraFirma_JuicePetrifyChunk(IRenderable tile, Vector3 origin, float vx, float vy0, float air, float scale)
		{
			Tile = tile;
			Origin = origin;
			Vx = vx;
			Vy0 = vy0;
			Air = Mathf.Max(0.01f, air);
			Scale = scale;
			duration = this.Air + 0.02f;
		}

		public override bool canFinishUpToTurn() => false;

		public override void start()
		{
			Target = SpriteManager.GetPooledSprite(Tile, Transparent: true);
			Foreground = Target.color;
			Background = Target.backcolor;
			Detail = Target.detailcolor;
			DetailNudge = new Color(Detail.r, Detail.g, Detail.b, 0.996f);
			Transform transform = Target.gameObject.transform;
			transform.SetParent(GameManager.Instance.TileRoot.transform);
			OrigScale = transform.localScale;
			transform.localScale = OrigScale * Scale; // pool state -- restored at finish
			transform.position = Origin + new Vector3(0f, 0f, Cleo_TerraFirma_GorgonPetrifyFX.Z_CHUNK);
		}

		public override void update()
		{
			if (Target == null)
				return;
			// normals-stomp repair, the house idiom
			Target.color = Foreground;
			Target.backcolor = Background;
			Target.detailcolor = DetailNudge;
			Target.detailcolor = Detail;
			if (t >= Air)
			{
				// spent: park BEHIND the map immediately rather than waiting on finish()
				Target.transform.position = Origin + new Vector3(0f, 0f, Cleo_TerraFirma_GorgonPetrifyFX.Z_PARKED);
				return;
			}
			float y = Vy0 * t - 0.5f * GRAVITY * t * t;
			Target.transform.position = Origin + new Vector3(Vx * t, y, Cleo_TerraFirma_GorgonPetrifyFX.Z_CHUNK);
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

	/// <summary>Game-thread API. The mutation holds the returned state object in a
	/// [NonSerialized] field and passes it back on every call; Show updates the live band
	/// in place or creates a fresh entry when none is alive (first fail, after a load,
	/// after visibility took it down). All Unity-side work happens in the entry -- this
	/// side only composes strings and pokes fields (the freeze lesson: the game thread
	/// never touches the atlas).</summary>
	[HasCallAfterGameLoaded]
	public static class Cleo_TerraFirma_GorgonPetrifyFX
	{
		/// <summary>Harness kill switch (GAZE scenarios): no sprites, mechanics untouched.
		/// Set (and always restored) only by the test harness; rides the TF-VERIFY strip.</summary>
		public static bool HarnessMute;

		/// <summary>Bumped when a save loads: every pre-load band entry sees the mismatch and
		/// parks itself. Belt-and-suspenders under the manager's own finishAll paths.</summary>
		public static volatile int GlobalGen;

		// stone dressing (Claude dials under the ruled shape): two-tone gray -- fg 'y' with
		// detail 'K' keeps the creature's interior shapes readable in the stone; the base
		// game's Random Statue wears flat &y/y, so the completed statue reads one shade
		// flatter than the band that built it. Feel-round dial.
		public const char STONE_FG = 'y';
		public const char STONE_DETAIL = 'K';
		// FEEL ROUND 4 (user: "i wish the texture wasn't so solid on the band"): POROUS BAND.
		// The band's detail channel renders fully transparent (the engine's own 'k'-under-
		// Transparent alpha-0 path -- the exact mechanism the backing already uses), so the
		// stone covers only the art's fg-channel pixels and the creature's detail pixels stay
		// ALIVE through the stone: a lattice following the sprite's own structure, flesh
		// showing through. Creatures whose art is mostly detail-channel will read mostly
		// un-stoned -- if one surfaces, the flip side (stone the detail, open the fg) is the
		// same one-char change; watch item, not built.
		public const bool BAND_POROUS = true;
		// band ladder (Claude dials): net 1 = 12% (feet, ~3px of a 24px tile -- visible but
		// low), net 9 = 92% (chin-deep; 10 fails is the statue, never shown as a band)
		private const float BAND_MIN_FRAC = 0.12f;
		private const float BAND_MAX_FRAC = 0.92f;
		// motion (Claude dials): grow crosses one net-step (~0.1) in ~0.22s -- a lurch, not a
		// pop; shrink-out clears even a chin-deep band in ~0.6s
		public const float GROW_PER_SEC = 0.45f;
		public const float SHRINK_OUT_PER_SEC = 1.6f;
		public const float LIFE_CAP_SEC = 3600f; // hard stop for a band nothing ever released (belt-and-suspenders)
		public const float Z_FRONT = -10f;
		public const float Z_CHUNK = -11f; // debris flies in FRONT of the band
		public const float Z_PARKED = 100f;
		public const float HALF_TILE = 12f;
		// rock-pop dials (feel round 1): counts per beat, arc energy, sprite scale
		private const string CHUNK_TILE_SMALL = "Items/sw_smallstone.bmp"; // base-game stone items -- guaranteed atlas residents
		private const string CHUNK_TILE_LARGE = "Items/sw_stone_large.bmp";
		private const int CHUNK_LARGE_PCT = 30;        // % of pops that use the chunkier tile
		private const float CHUNK_SCALE_MIN = 0.20f;
		private const float CHUNK_SCALE_MAX = 0.35f;
		// waterline dials (feel round 2, user 2026-08-06: gray-on-gray legibility -- feral dogs
		// wear the band's own '&y'). The band's top edge carries the game's MIXER SHADE: "a
		// third color in addition to black and white" that "takes the detail and main color
		// and mixes *them* together" (user correction same sitting: the mixer contributes NO
		// pigment of its own -- an earlier earth-partner reading, hex #7b6529, is superseded).
		// A gray-bodied dog's line therefore inherits its detail channel's warmth; a creature
		// whose two channels are BOTH the same gray keeps an invisible line by canon -- the
		// mixer is honest, not decorative.
		// FEEL ROUND 3 (user: "apply some dithering to the waterline so it's not just flat"):
		// the line is now two stacked 1px rows, each keeping the tile art's OWN channel
		// structure (fg = mixer family, detail = a second shade -- the art's pixel
		// distribution dithers the line spatially for free), the bottom row a step darker
		// (gradient edge), and both rows breathing slowly toward stone gray on offset phases
		// (temporal shimmer -- slow enough to read as the line WORKING, not blinking).
		// FEEL ROUND 5 (user: "i was just thinking a serrated/dithered edge"): the band is
		// column strips, each biting 0..DEPTH px below the true height -- a jagged frontier
		// instead of a flat bar. Teeth are a pure function of (column, height): stable
		// between frames, crawling as the band climbs.
		public const int SERRATION_COLS = 4;     // strips across the 16px tile (4px teeth)
		public const int SERRATION_DEPTH_PX = 2; // deepest bite below the true height
		public const int LINE_ROWS = 2;      // 1px each; the band's top edge
		public const float LINE_MIX = 0.5f;  // blend position between main (0) and detail (1)
		public const float LINE_ROW_DARKEN = 0.4f;   // bottom row's extra step toward stone dark
		public const float LINE_SHIMMER_SEC = 1.4f;  // one breath cycle
		public const float LINE_SHIMMER_AMP = 0.35f; // how far the breath reaches toward stone gray
		public const float LINE_SHIMMER_PHASE = 0.9f; // radians between the two rows
		public const float Z_LINE = -10.5f; // just in front of the band, behind the debris

		[CallAfterGameLoaded]
		public static void AfterGameLoaded()
		{
			GlobalGen++;
		}

		/// <summary>The band ladder: net fails-minus-successes to band fraction.</summary>
		public static float FracForNet(int net)
		{
			if (net <= 0)
				return 0f;
			return BAND_MIN_FRAC + (BAND_MAX_FRAC - BAND_MIN_FRAC) * Mathf.Clamp01((net - 1) / 8f);
		}

		/// <summary>Update the live band (cell + height) or create one. Returns the state the
		/// caller should hold; null = no band (muted, unseen, text mode, no tile) -- callers
		/// just try again next tick.</summary>
		public static Cleo_TerraFirma_PetrifyBandState Show(Cleo_TerraFirma_PetrifyBandState state, XRL.World.GameObject target, float frac)
		{
			Cell cell = target?.CurrentCell;
			if (HarnessMute || cell == null || GameManager.Instance == null || !Options.UseTiles || !cell.IsVisible())
			{
				// out of sight (or no way to draw): take the band down instantly; a later
				// visible tick re-creates it at the then-current height
				Release(state, ShrinkOut: false);
				return null;
			}
			if (state != null && !state.Dead && !state.Superseded && !state.ShrinkOut)
			{
				state.PackedCell = cell.X + cell.Y * 1000;
				state.TargetFrac = frac;
				return state;
			}
			Release(state, ShrinkOut: false); // a dying holdover never comes back -- replace it
			// the game's mod compiler defines VERSION_1_0 on stable and a higher VERSION_1_x on
			// lang-experimental (1_1 on 2.0.212, 1_2 on 2.0.213), where RenderForUI returns Visual.
#if !VERSION_1_0
			Visual v = target.RenderForUI();
			if (!v.Valid || v.Text || v.Tile.IsNullOrEmpty()) // Text: a glyph-only Visual, the old empty-Tile case
				return null;
#else
			RenderEvent re = target.RenderForUI(); // REUSED buffer -- copy fields now, never keep the ref
			if (re == null || re.Tile.IsNullOrEmpty())
				return null;
#endif
			Cleo_TerraFirma_PetrifyBandState fresh = new Cleo_TerraFirma_PetrifyBandState
			{
				PackedCell = cell.X + cell.Y * 1000,
				TargetFrac = frac,
				Gen = GlobalGen,
			};
#if !VERSION_1_0
			WaterlineColors(target, v, out Color mixer, out Color creatureDetail);
			CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuicePetrifyBand(
				new Cleo_TerraFirma_CreatureSnapshotRenderable(v.Tile, STONE_FG, BAND_POROUS ? 'k' : STONE_DETAIL, 'k', v.HFlip, v.VFlip),
				fresh, mixer, creatureDetail), async: true);
#else
			WaterlineColors(re, out Color mixer, out Color creatureDetail);
			CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuicePetrifyBand(
				new Cleo_TerraFirma_CreatureSnapshotRenderable(re.Tile, STONE_FG, BAND_POROUS ? 'k' : STONE_DETAIL, 'k', re.HFlip, re.VFlip),
				fresh, mixer, creatureDetail), async: true);
#endif
			return fresh;
		}

		/// <summary>The waterline's palette for this target: the mixer shade (its main and
		/// detail render channels mixed together -- the game's own third-color idiom, no
		/// pigment of ours) plus the raw detail color for the line's dither channel. Computed
		/// once at snapshot time from the same RenderForUI result the tile came from. On lang the
		/// Visual reads Detail 'w' when nothing set a detail color; with an empty Render.DetailColor
		/// that 'w' is the fallback, and no detail means the main color, as on stable.</summary>
#if !VERSION_1_0
		private static void WaterlineColors(XRL.World.GameObject target, Visual v, out Color mixer, out Color creatureDetail)
		{
			char fg = v.Foreground == '\0' ? 'y' : v.Foreground;
			char dt = v.Detail == 'w' && (target.Render?.DetailColor.IsNullOrEmpty() ?? true) ? fg : v.Detail;
#else
		private static void WaterlineColors(RenderEvent re, out Color mixer, out Color creatureDetail)
		{
			char fg = 'y';
			string cs = re.ColorString;
			if (!cs.IsNullOrEmpty())
			{
				int amp = cs.IndexOf('&');
				if (amp >= 0 && amp + 1 < cs.Length)
					fg = cs[amp + 1];
			}
			char dt = re.DetailColor.IsNullOrEmpty() ? fg : re.DetailColor[0];
#endif
			Color main = ConsoleLib.Console.ColorUtility.ColorMap.GetValue(fg);
			creatureDetail = ConsoleLib.Console.ColorUtility.ColorMap.GetValue(dt);
			mixer = Color.Lerp(main, creatureDetail, LINE_MIX);
		}

		/// <summary>Small rocks popping off a petrifying target -- the per-fail beat (and, with
		/// Burst, the shower under the statue swap). Fire-and-forget; skipped when muted,
		/// unseen, or text-mode, same gate as the band. Game thread composes strings and
		/// numbers only.</summary>
		public static void RockPops(XRL.World.GameObject target, int count, bool Burst = false)
		{
			Cell cell = target?.CurrentCell;
			if (HarnessMute || cell == null || GameManager.Instance == null || !Options.UseTiles || !cell.IsVisible())
				return;
			Vector3 center = GameManager.Instance.getTileCenter(cell.X, cell.Y);
			for (int i = 0; i < count; i++)
			{
				string tile = Stat.Random(1, 100) <= CHUNK_LARGE_PCT ? CHUNK_TILE_LARGE : CHUNK_TILE_SMALL;
				Cleo_TerraFirma_CreatureSnapshotRenderable look = new Cleo_TerraFirma_CreatureSnapshotRenderable(
					tile, STONE_FG, STONE_DETAIL, 'k', Stat.Random(0, 1) == 1, Stat.Random(0, 1) == 1);
				// spawn on the body, pop up, gravity takes over; bursts fly harder and wider
				Vector3 origin = center + new Vector3(Stat.Random(-5, 5), Stat.Random(-8, 2), 0f);
				float vx = Stat.Random(12, Burst ? 42 : 28) * (Stat.Random(0, 1) == 1 ? 1f : -1f);
				float vy0 = Stat.Random(Burst ? 90 : 60, Burst ? 150 : 110);
				float air = 0.01f * Stat.Random(Burst ? 40 : 32, Burst ? 55 : 45);
				float scale = CHUNK_SCALE_MIN + 0.01f * Stat.Random(0, (int)((CHUNK_SCALE_MAX - CHUNK_SCALE_MIN) * 100f));
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuicePetrifyChunk(look, origin, vx, vy0, air, scale), async: true);
			}
		}

		/// <summary>Take a band down: ShrinkOut slides it to zero first (escape, mark, statue),
		/// otherwise it vanishes this frame (visibility loss, defensive resets). Safe on null
		/// and on already-dead state; callers null their ref after.</summary>
		public static void Release(Cleo_TerraFirma_PetrifyBandState state, bool ShrinkOut)
		{
			if (state == null || state.Dead)
				return;
			if (ShrinkOut)
				state.ShrinkOut = true;
			else
				state.Superseded = true;
		}
	}
}
