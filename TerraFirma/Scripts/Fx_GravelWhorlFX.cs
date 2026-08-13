using ConsoleLib.Console;
using Kobold;
using UnityEngine;
using XRL;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// The gravel whorl's outcome beats (VFX sitting 2026-08-12). Design principle from the
	/// sitting: each outcome encodes the projectile's FATE for a bystander -- devoured things
	/// visibly join the whorl, caroms get an elbow, swats point at the loot, scattered beams
	/// show their second leg.
	///
	/// DEVOUR (user concept, their words: the projectile "dissolves into a mass of pixels,
	/// swirling in a vaguely circular mass around the tile of whoever has the mutation"):
	/// pooled sprites at fleck scale -- the brazier-fire trick, indistinct pixel clumps whose
	/// motion+flicker carry the effect -- orbiting the defender 1-2 revolutions (RULED
	/// 2026-08-12), wearing the devoured projectile's own palette (and, for a third of the
	/// flecks, its own tile). Fire-and-forget juice entries on the petrify-chunk lifecycle:
	/// canFinishUpToTurn declined, park-behind-map when spent, pool scale restored at finish.
	/// The whorl has NO idle visual anywhere, so this beat is also the mutation's visual
	/// debut -- worth its weight.
	///
	/// The other beats are text particles in the carom trace's own register (&amp;y± family),
	/// no sprites. All dials CLAUDE, feel-round fodder.
	/// </summary>
	public static class Cleo_TerraFirma_GravelWhorlFX
	{
		// devour swirl dials
		private const int FLECK_COUNT = 16;          // sphere wants density (user sketch 2026-08-12)
		private const float SWIRL_SEC = 0.9f;
		private const float REVOLUTIONS = 1.6f;      // RULED 1-2 revolutions (2026-08-12)
		private const float FLECK_SCALE_MIN = 0.22f; // feel rounds 2-3 (user), up from 0.10-0.18 then a nudge;
		private const float FLECK_SCALE_MAX = 0.34f; // round 3 "*slightly* bigger, then that's good" -- SIZE SETTLED
		private const string FLECK_TILE = "Items/sw_smallstone.bmp"; // guaranteed atlas resident (petrify-chunk precedent)
		// elbow / track dials
		private const int ELBOW_PARTICLES = 8;
		private const int REFRACT_TRACK_CELLS = 5;
		// laser ember-catch dials (feel round 8, user concept: "utilize the pixels produced
		// by the end of the laser shot effect itself" -- flecks are born AT the beam's
		// terminus in the beam's own colors the instant it lands, so they read as the shot's
		// own dying pixels being seized; supersedes rounds 5-7's splitting ring, which
		// claimed an interception the engine's beam draw refuses to honor)
		private const int EMBER_COUNT = 16; // feel rounds 9 + 11 (user, twice: more particles), up from 8 then 12
		private const float EMBER_R0 = 2f;          // px; born tight on the beam's end flash
		private const float EMBER_R1 = 15f;         // px; flung out to the whorl's reach
		private const float EMBER_SPAN_RAD = 2.2f;  // ~126 degrees of coherent whorl-spin on the way out
		private const float EMBER_STAGGER_SEC = 0.03f;
		private const float EMBER_TRAVEL_SEC = 0.5f;
		private const float EMBER_SCALE_MIN = 0.16f; // spark-sized, thinner than the gravel mass
		private const float EMBER_SCALE_MAX = 0.26f;

		/// <summary>Devour: the projectile dissolves into a swirling pixel mass around the
		/// defender. Palette theft happens HERE, game-side, before the caller obliterates the
		/// projectile: live TileColor wins over ColorString (the carve-reveal lesson -- the
		/// blueprint lies, the Render tells the truth), detail rides along, and a third of
		/// the flecks wear the projectile's own tile at fleck scale.</summary>
		public static void PlayDevourSwirl(XRL.World.GameObject defender, XRL.World.GameObject projectile, XRL.World.GameObject attacker = null)
		{
			Cell cell = defender?.CurrentCell;
			if (cell == null || GameManager.Instance == null || !Options.UseTiles || !cell.IsVisible())
				return;
			char fg = 'y', dt = 'Y'; // whorl-gray defaults for renderless munitions
			string projTile = null;
			XRL.World.Parts.Render r = projectile?.Render;
			if (r != null)
			{
				string cs = !r.TileColor.IsNullOrEmpty() ? r.TileColor : r.ColorString;
				if (!cs.IsNullOrEmpty())
				{
					int amp = cs.IndexOf('&');
					if (amp >= 0 && amp + 1 < cs.Length)
						fg = cs[amp + 1];
				}
				dt = r.DetailColor.IsNullOrEmpty() ? fg : r.DetailColor[0];
				projTile = r.Tile;
			}
			// fiction split (user feel round 2026-08-12: the gravel swirl "makes less sense
			// for when a laser is devoured"): mass joins the whorl, light gets its dying
			// sparks seized and spun apart
			if (XRL.World.Effects.Cleo_TerraFirma_GravelWhorl.IsLightProjectile(projectile))
			{
				PlayDevourLight(cell, fg, projectile.GetPropertyOrTag("ProjectileVFXConfiguration"));
				return;
			}
			Helpers.VerifyLog("GVW", $"devour swirl: {FLECK_COUNT} flecks in &{fg}/{dt}, tile={(projTile.IsNullOrEmpty() ? "fleck-only" : projTile)}");
			Vector3 center = GameManager.Instance.getTileCenter(cell.X, cell.Y);
			float omega = REVOLUTIONS * 2f * Mathf.PI / SWIRL_SEC; // one coherent spin direction: it is a WHORL
			for (int i = 0; i < FLECK_COUNT; i++)
			{
				string tile = (!projTile.IsNullOrEmpty() && i % 3 == 0) ? projTile : FLECK_TILE;
				Cleo_TerraFirma_CreatureSnapshotRenderable look = new Cleo_TerraFirma_CreatureSnapshotRenderable(
					tile, fg, dt, 'k', Stat.Random(0, 1) == 1, Stat.Random(0, 1) == 1);
				// SPHERE distribution (user sketch 2026-08-12: "instead of a circle, the whorl
				// represents as a sphere"): each fleck rides its own latitude band -- asin maps
				// the even i-ladder to even SURFACE coverage, no pole crowding
				float lat = Mathf.Asin(2f * (i + 0.5f) / FLECK_COUNT - 1f);
				float phase0 = 0.0174533f * Stat.Random(0, 359);
				float scale = FLECK_SCALE_MIN + 0.01f * Stat.Random(0, (int)((FLECK_SCALE_MAX - FLECK_SCALE_MIN) * 100f));
				float omegaJitter = omega * (0.01f * Stat.Random(90, 110));
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceWhorlFleck(look, center, lat, phase0, omegaJitter, scale, SWIRL_SEC), async: true);
			}
		}

		/// <summary>A devoured LIGHT munition -- the EMBER-CATCH (user concept, feel round 8:
		/// "utilize the pixels produced by the end of the laser shot effect itself"): the
		/// engine always draws the beam prefab to the impact cell and ends it with its own
		/// terminal pixels there, so our flecks are born in that exact spot, in the beam
		/// prefab's own drawn colors (beamColor0/1 from ProjectileVFXConfiguration; the
		/// bolt's char palette is the fallback), the instant the hit resolves -- perceptual
		/// continuity, the shot's dying sparks seized by the whorl and spun apart. Each
		/// ember spirals OUTWARD from the terminus with a coherent spin, shrinking out as
		/// it flies. No interception claim anywhere: the beam lands, and what lands is
		/// unmade. No reserve feed (that was always IsAnyStone-gated).</summary>
		public static void PlayDevourLight(Cell cell, char fg, string projectileVfxConfig = null)
		{
			if (cell == null || GameManager.Instance == null || !Options.UseTiles || !cell.IsVisible())
				return;
			Helpers.VerifyLog("GVW", $"devour ember-catch: light munition's end sparks seized (&{fg})");
			Color? beamA = null, beamB = null;
			if (!projectileVfxConfig.IsNullOrEmpty())
			{
				foreach (string kv in projectileVfxConfig.Split(new string[] { ";;" }, System.StringSplitOptions.RemoveEmptyEntries))
				{
					if (kv.StartsWith("beamColor0::") && UnityEngine.ColorUtility.TryParseHtmlString(kv.Substring("beamColor0::".Length), out Color c0))
						beamA = c0;
					else if (kv.StartsWith("beamColor1::") && UnityEngine.ColorUtility.TryParseHtmlString(kv.Substring("beamColor1::".Length), out Color c1))
						beamB = c1;
				}
			}
			Vector3 center = GameManager.Instance.getTileCenter(cell.X, cell.Y);
			// spark-jack (feel round 10, user: "jack into the final sparks effect so there
			// isn't a moment where they are inexplicably doing a different thing"): seize the
			// beam prefab's own impactVfx at our cell for the ember beat's lifetime
			CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceSparkJack(
				center, EMBER_STAGGER_SEC * (EMBER_COUNT - 1) + EMBER_TRAVEL_SEC + 0.1f), async: true);
			for (int i = 0; i < EMBER_COUNT; i++)
			{
				Cleo_TerraFirma_CreatureSnapshotRenderable look = new Cleo_TerraFirma_CreatureSnapshotRenderable(
					FLECK_TILE, fg, 'W', 'k', Stat.Random(0, 1) == 1, Stat.Random(0, 1) == 1);
				float phase0 = 0.0174533f * Stat.Random(0, 359);
				float scale = EMBER_SCALE_MIN + 0.01f * Stat.Random(0, (int)((EMBER_SCALE_MAX - EMBER_SCALE_MIN) * 100f));
				CombatJuiceManager.enqueueEntry(new Cleo_TerraFirma_JuiceWhorlArc(
					look, center, phase0, 1, EMBER_SPAN_RAD, EMBER_R0, EMBER_R0, EMBER_R1, EMBER_R1,
					i * EMBER_STAGGER_SEC, EMBER_TRAVEL_SEC, scale, beamA, beamB), async: true);
			}
		}

		/// <summary>Ricochet elbow (user go-ahead 2026-08-12: "the elbow flash is a good
		/// instinct"): a sharp chip burst at the carom's turn point, StunningForce's radial
		/// ring idiom in the trace's own gravel register. The bend gets a joint.</summary>
		public static void PlayRicochetElbow(Cell origin)
		{
			if (origin == null || !origin.InActiveZone)
				return;
			for (int i = 0; i < ELBOW_PARTICLES; i++)
			{
				float a = i * 2f * Mathf.PI / ELBOW_PARTICLES;
				string glyph = i % 2 == 0 ? "&Y±" : "&W±";
				XRLCore.ParticleManager.Add(glyph, origin.X, origin.Y,
					Mathf.Sin(a) / 8f, Mathf.Cos(a) / 8f, 14, 0f, 0f, 0L);
			}
		}

		/// <summary>Swat: the fizzle gets a pinch of grit at the whorl; a batted real item
		/// gets grit AT ITS LANDING CELL -- the loot pointer, the practical half of "where
		/// did my thrown dagger go". Feel round 2026-08-12, corrected same day (user: "less
		/// smoke on rocks and no ascii characters in the tile mode" -- then "i meant just
		/// the dots, not the dithered tiles"): the beat is ± blips only. The ± block reads
		/// as dithered gravel and stays; DustPuff and its &amp;y. dot particles are gone,
		/// which also covers "less smoke". One vocabulary, both display modes.</summary>
		public static void PlaySwatBeat(Cell origin, Cell landing)
		{
			if (landing == null || landing == origin)
			{
				GritBlips(origin, 2); // munition fizzle: ground to nothing where it struck
				return;
			}
			GritBlips(landing, 3);
		}

		/// <summary>Where a carom ends: the same ± grit, replacing the engine dust puff.</summary>
		public static void PlayCaromLanding(Cell landing)
		{
			GritBlips(landing, 3);
		}

		/// <summary>A few &amp;y± blips drifting apart at a cell -- the dotless, smokeless
		/// landing beat, staggered so it settles rather than flashes.</summary>
		private static void GritBlips(Cell cell, int count)
		{
			if (cell == null || !cell.InActiveZone)
				return;
			for (int i = 0; i < count; i++)
				XRLCore.ParticleManager.Add("&y±",
					cell.X + 0.1f * Stat.Random(-3, 3), cell.Y + 0.1f * Stat.Random(-2, 2),
					0.015f * Stat.Random(-4, 4), 0.015f * Stat.Random(-3, 2), 12, 0f, 0f, i * 50L);
		}

		/// <summary>Resonant beam scatter: draw the SECOND LEG. Direction is the engine's own
		/// pre-seeded scatter angle read from the RefractLight event (decomp-verified consumer
		/// convention, IDelayedLineMutation.Refract: x += Sin(deg), y += Cos(deg)) -- so this
		/// track points exactly where the beam actually went. A flash at the bend, then a
		/// short staggered light-track; deliberately shorter than the real leg (a flourish,
		/// not a beam repaint). Color is light-register &amp;Y -- the event carries no beam
		/// palette; CLAUDE dial.</summary>
		public static void PlayRefractTrack(Cell origin, int directionDeg)
		{
			if (origin == null || !origin.InActiveZone)
				return;
			Zone zone = origin.ParentZone;
			float rad = directionDeg * Mathf.PI / 180f;
			float dx = Mathf.Sin(rad), dy = Mathf.Cos(rad);
			XRLCore.ParticleManager.Add("&W*", origin.X, origin.Y, 0f, 0f, 8, 0f, 0f, 0L);
			float x = origin.X, y = origin.Y;
			for (int i = 1; i <= REFRACT_TRACK_CELLS; i++)
			{
				x += dx; y += dy;
				Cell step = zone.GetCell((int)x, (int)y);
				if (step == null || step.IsSolid())
					break;
				if (step.InActiveZone)
					XRLCore.ParticleManager.Add(i % 2 == 0 ? "&Y*" : "&y*", step.X, step.Y, 0f, 0f, 12, 0f, 0f, i * 30L);
			}
		}
	}

	/// <summary>
	/// One fleck of the devour swirl -- the whorl as a SPHERE (user sketch 2026-08-12), not
	/// a ring: each fleck rides its own latitude band around a vertical axis, band radius
	/// shrinking toward the poles, depth faked by scale (front sweeps past big, back shrinks
	/// behind) and by z-order within the chunk layer. Per-frame fg/detail palette SWAP on a
	/// fast cycle -- AnimatedMaterialFire's flicker translated to sprite channels; the
	/// texture's own pixel structure churns the mass for free. Last stretch shrinks to
	/// nothing: the fleck sinks into the whorl. Lifecycle is the petrify chunk's verbatim:
	/// decline the turn sweep, park behind the map when spent, restore pool scale at finish.
	/// </summary>
	public class Cleo_TerraFirma_JuiceWhorlFleck : CombatJuiceEntry
	{
		private IRenderable Tile;
		private ex3DSprite2 Target;
		private Vector3 Center;
		private float Lat;
		private float Phase0;
		private float Omega;
		private float Scale;
		private float Life;
		private Vector3 OrigScale;
		private Color ColorA;
		private Color ColorB;
		private Color Background;
		private const float SPHERE_RX = 12f;     // px; equator radius
		private const float SPHERE_RY = 10f;     // px; pole-to-pole half-height
		private const float TILT_RAD = 0.21f;    // ~12 degrees; top axis leans LEFT (user 2026-08-12: "tilted slightly to the left")
		private const float DEPTH_SCALE_MIN = 0.6f; // back-of-sphere flecks shrink to this fraction
		private const float SPAWN_FRAC = 0.15f;  // the dissolve starts tight on the body
		private const float EASE_IN_SEC = 0.22f; // spawn -> full sphere
		private const float FADE_OUT_FRAC = 0.2f;
		private const float WOBBLE_PX = 1.5f;    // radius churn
		private const float FLICKER_HZ = 10f;

		public Cleo_TerraFirma_JuiceWhorlFleck(IRenderable tile, Vector3 center, float lat, float phase0, float omega, float scale, float life)
		{
			Tile = tile;
			Center = center;
			Lat = lat;
			Phase0 = phase0;
			Omega = omega;
			Scale = scale;
			Life = life;
			duration = life + 0.02f;
		}

		public override bool canFinishUpToTurn() => false;

		public override void start()
		{
			Target = SpriteManager.GetPooledSprite(Tile, Transparent: true);
			ColorA = Target.color;
			ColorB = Target.detailcolor;
			Background = Target.backcolor;
			Transform transform = Target.gameObject.transform;
			transform.SetParent(GameManager.Instance.TileRoot.transform);
			OrigScale = transform.localScale;
			transform.localScale = OrigScale * Scale;
			transform.position = Center + new Vector3(0f, 0f, Cleo_TerraFirma_GorgonPetrifyFX.Z_CHUNK);
		}

		public override void update()
		{
			if (Target == null)
				return;
			if (t >= Life)
			{
				Target.transform.position = Center + new Vector3(0f, 0f, Cleo_TerraFirma_GorgonPetrifyFX.Z_PARKED);
				return;
			}
			// palette flicker: swap fg/detail channels on the cycle (normals-stomp repair rides
			// along for free -- both channels are reassigned every frame)
			bool flip = ((int)((t + Phase0) * FLICKER_HZ)) % 2 == 0;
			Target.color = flip ? ColorA : ColorB;
			Target.detailcolor = flip ? ColorB : ColorA;
			Target.backcolor = Background;
			float ease = Mathf.Clamp01(t / EASE_IN_SEC);
			ease = 1f - (1f - ease) * (1f - ease); // ease-out: the burst decelerates onto the sphere
			float grow = Mathf.Lerp(SPAWN_FRAC, 1f, ease);
			float wobble = WOBBLE_PX * Mathf.Sin(t * 13f + Phase0 * 3f);
			// latitude-band orbit: x sweeps the band, depth is the sphere's front/back axis
			float bandR = grow * SPHERE_RX * Mathf.Cos(Lat) + wobble;
			float bandY = grow * SPHERE_RY * Mathf.Sin(Lat) + wobble * 0.4f;
			float theta = Phase0 + Omega * t;
			float depth = Mathf.Sin(theta); // -1 back .. +1 front
			float depthScale = Mathf.Lerp(DEPTH_SCALE_MIN, 1f, (depth + 1f) * 0.5f);
			float fade = 1f;
			float fadeStart = Life * (1f - FADE_OUT_FRAC);
			if (t > fadeStart)
				fade = 1f - (t - fadeStart) / (Life - fadeStart);
			Target.transform.localScale = OrigScale * (Scale * depthScale * Mathf.Max(0.01f, fade));
			// axis tilt: roll the whole sphere frame so the top pole leans left -- the sketch's
			// off-axis read. Plain 2D rotation of the band coordinates; depth is unaffected.
			float x = bandR * Mathf.Cos(theta);
			float tx = x * Mathf.Cos(TILT_RAD) - bandY * Mathf.Sin(TILT_RAD);
			float ty = x * Mathf.Sin(TILT_RAD) + bandY * Mathf.Cos(TILT_RAD);
			Target.transform.position = Center + new Vector3(tx, ty,
				Cleo_TerraFirma_GorgonPetrifyFX.Z_CHUNK - depth); // front flecks draw over back flecks
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

	/// <summary>
	/// The spark-jack (feel round 10): while the ember-catch plays, the beam prefab's own
	/// terminal sparks must not ALSO play, or the terminus shows two effects doing
	/// different things. Both engine beam drivers (laser_renderer, vls_renderer) expose
	/// their impact sparks as a public `impactVfx` child positioned at the impact cell --
	/// so this entry scans the live scene for the instance whose impactVfx sits on our
	/// tile (first SCAN_SEC only; FindObjectsOfType sees active-only, so parked pool
	/// instances are excluded), disables it, and RE-ENABLES it at finish -- the pooled
	/// prefab goes back exactly as found (the house capture/restore discipline). The
	/// cover duration outlives the ember beat so the re-enable lands after the VFX root
	/// has parked; a re-activation under an inactive root shows nothing. Known residual,
	/// accepted: a very fast follow-up shot reusing the pooled instance inside our cover
	/// window would fire spark-less once (cosmetic, self-heals at finish). No scan hit =
	/// clean no-op (text mode, VFX off, or a sparkless projectile).
	/// </summary>
	public class Cleo_TerraFirma_JuiceSparkJack : CombatJuiceEntry
	{
		private Vector3 Center;
		private UnityEngine.GameObject Jacked;
		private const float SCAN_SEC = 0.3f;
		private const float MATCH_SQR_PX = 16f; // impactVfx is placed by the same getTileCenter we use; generous epsilon

		public Cleo_TerraFirma_JuiceSparkJack(Vector3 center, float coverSec)
		{
			Center = center;
			duration = coverSec;
		}

		public override bool canFinishUpToTurn() => false;

		public override void update()
		{
			if (Jacked != null || t >= SCAN_SEC)
				return;
			foreach (laser_renderer r in UnityEngine.Object.FindObjectsOfType<laser_renderer>())
			{
				if (r.impactVfx != null && (r.impactVfx.transform.position - Center).sqrMagnitude < MATCH_SQR_PX)
				{
					Jacked = r.impactVfx;
					Jacked.SetActive(false);
					return;
				}
			}
			foreach (vls_renderer r in UnityEngine.Object.FindObjectsOfType<vls_renderer>())
			{
				if (r.impactVfx != null && (r.impactVfx.transform.position - Center).sqrMagnitude < MATCH_SQR_PX)
				{
					Jacked = r.impactVfx;
					Jacked.SetActive(false);
					return;
				}
			}
		}

		public override void finish()
		{
			if (Jacked != null)
			{
				Jacked.SetActive(true); // pool state restore -- playOnAwake re-fires only if the root is still live, which the cover duration outlasts
				Jacked = null;
			}
			base.finish();
		}
	}

	/// <summary>
	/// One fleck on an arc-or-spiral path around/from the tile center: radius lerps from
	/// (R0x,R0y) to (R1x,R1y) across the traversal -- equal pairs give the old fixed-radius
	/// arc, a small R0 gives the ember-catch's outward spiral -- while the angle sweeps
	/// SpanRad in Side's direction, flickering two colors (beam-color overrides win over
	/// the tile's own channels), shrinking out over the last stretch. StartAngle arrives in
	/// CELL space (y down) -- the y flip to pixel space happens here. Parks behind the map
	/// until its stagger delay elapses, so groups read as streams, not rings popping in.
	/// Same lifecycle as the devour fleck: decline the turn sweep, park when spent, restore
	/// pool scale at finish.
	/// </summary>
	public class Cleo_TerraFirma_JuiceWhorlArc : CombatJuiceEntry
	{
		private IRenderable Tile;
		private ex3DSprite2 Target;
		private Vector3 Center;
		private float StartAngle;
		private int Side;
		private float SpanRad;
		private float R0x;
		private float R0y;
		private float R1x;
		private float R1y;
		private float Delay;
		private float TravelSec;
		private float Scale;
		private Vector3 OrigScale;
		private Color ColorA;
		private Color ColorB;
		private Color Background;
		private Color? OverrideA;
		private Color? OverrideB;
		private const float SHRINK_TAIL_FRAC = 0.3f; // last stretch of the traversal fades the fleck out
		private const float FLICKER_HZ = 10f;

		public Cleo_TerraFirma_JuiceWhorlArc(IRenderable tile, Vector3 center, float startAngle, int side, float spanRad, float r0x, float r0y, float r1x, float r1y, float delay, float travelSec, float scale, Color? overrideA = null, Color? overrideB = null)
		{
			Tile = tile;
			Center = center;
			StartAngle = startAngle;
			Side = side;
			SpanRad = spanRad;
			R0x = r0x;
			R0y = r0y;
			R1x = r1x;
			R1y = r1y;
			Delay = delay;
			TravelSec = travelSec;
			Scale = scale;
			OverrideA = overrideA;
			OverrideB = overrideB;
			duration = delay + travelSec + 0.02f;
		}

		public override bool canFinishUpToTurn() => false;

		public override void start()
		{
			Target = SpriteManager.GetPooledSprite(Tile, Transparent: true);
			ColorA = OverrideA ?? Target.color;
			ColorB = OverrideB ?? Target.detailcolor;
			Background = Target.backcolor;
			Transform transform = Target.gameObject.transform;
			transform.SetParent(GameManager.Instance.TileRoot.transform);
			OrigScale = transform.localScale;
			transform.localScale = OrigScale * Scale;
			transform.position = Center + new Vector3(0f, 0f, Cleo_TerraFirma_GorgonPetrifyFX.Z_PARKED);
		}

		public override void update()
		{
			if (Target == null)
				return;
			if (t < Delay || t >= Delay + TravelSec)
			{
				Target.transform.position = Center + new Vector3(0f, 0f, Cleo_TerraFirma_GorgonPetrifyFX.Z_PARKED);
				return;
			}
			bool flip = ((int)((t + StartAngle) * FLICKER_HZ)) % 2 == 0;
			Target.color = flip ? ColorA : ColorB;
			Target.detailcolor = flip ? ColorB : ColorA;
			Target.backcolor = Background;
			float s = (t - Delay) / TravelSec;
			float a = StartAngle + Side * SpanRad * s;
			float rx = Mathf.Lerp(R0x, R1x, s);
			float ry = Mathf.Lerp(R0y, R1y, s);
			float fade = s > 1f - SHRINK_TAIL_FRAC ? (1f - s) / SHRINK_TAIL_FRAC : 1f;
			Target.transform.localScale = OrigScale * (Scale * Mathf.Max(0.01f, fade));
			// cell-space angle, pixel-space position: y flips
			Target.transform.position = Center + new Vector3(rx * Mathf.Cos(a), -ry * Mathf.Sin(a), Cleo_TerraFirma_GorgonPetrifyFX.Z_CHUNK);
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
