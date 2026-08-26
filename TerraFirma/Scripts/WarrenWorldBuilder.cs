using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using Genkit;
using XRL.UI;

namespace XRL.World.WorldBuilders
{
	/// <summary>
	/// SCOUT-AND-QUOTA warren placement (user concept 2026-08-18 "instead of only
	/// trying to place 25, we could place more, and then trim down to 25 warrens
	/// that fit our tables"; greenlit 08-24; ported 2026-08-25). Replaces the
	/// overclaim-27 blind placement + self-erasing rumors: a claim is now written
	/// only AFTER its warren physically exists, so no rumor can lie by construction.
	///
	/// Flow, receipts in the decomp (2026-08-18 + 2026-08-25):
	///  1. OnAfterBuild - fires after BuildMutableEncounters, so farms and forts
	///     have already claimed their zones (JoppaWorldBuilder.cs:3655).
	///  2. Enumerate the mutable DesertCanyon pool - never sample it
	///     (GetMutableLocationWithTerrain draws WITH replacement, MutabilityMap.cs:35);
	///     the list is private, so reflection with the engine sampler as fallback.
	///  3. Shuffle by a derived world-seed key (The.Game.GetWorldSeed, pure fn,
	///     XRLGame.cs:1505) - the ambient stream is never consumed.
	///  4. Scout: MATERIALIZE each candidate zone (the tfwarrens-sweep precedent)
	///     and dry-run the carve forced-flourishing. Degrade honesty demotes to
	///     what the band holds, so the dry run's stage IS the zone's capacity, and
	///     the scorer IS the builder - they cannot disagree. The design doc's
	///     derived-canyon twist died when the shipped carver went host-read (the
	///     REAL zone replaces genCanyon); materializing is the honest replacement.
	///  5. Fill the stage quota biggest-first, carve winners on the spot (a
	///     flourishing slot replays its probe exactly), wire the claim, and stamp
	///     WarrenMaker with the assigned stage so a zone REBUILD reproduces it.
	/// </summary>
	[JoppaWorldBuilderExtension]
	public class Cleo_TerraFirma_WarrenPlacement : IJoppaWorldBuilderExtension
	{
		public const int WARRENS_PER_WORLD = 25;   // ruled 2026-08-18; pig-farm frequency
		public const int SCOUT_N = 50;             // CLAUDE dial (more = better tails, slower worldgen)
		public const int QUOTA_FL = 5;             // CLAUDE dial; fl-reclaimed/world ~ this x 0.14
		public const int QUOTA_CAMP = 9;           // CLAUDE dial; working fills the remainder
		public const bool QUOTA_ROLLED = true;     // CLAUDE lean: +/-1 per world so worlds differ

		private static readonly string[] STAGE_NAMES = { "working", "camp", "flourishing" };

		public override void OnAfterBuild(JoppaWorldBuilder Builder)
		{
			try { Place(Builder); }
			catch (Exception x)
			{
				Helpers.VerifyLog("WARREN", $"EXCEPTION in scout placement: {x}");
				MetricsManager.LogException("Cleo_TerraFirma_WarrenPlacement", x);
			}
		}

		private class Scouted
		{
			public string ZoneID;
			public Location2D Loc;
			public int Capacity = -1;   // -1 = no site; else STAGES index
			public int Floor;
			public bool Used;
		}

		private static void Place(JoppaWorldBuilder Builder)
		{
			// loading note: this is the one added step that does real work. DRAFT
			// text, rides the release voice pass. StepProgress is the lairs
			// builder's own in-phase idiom - no NextStep, the bar budget stands.
			WorldCreationProgress.StepProgress("Scouting canyons...");
			var pool = EnumeratePool(Builder);
			var rng = new Random(The.Game.GetWorldSeed("TFWARRENSCOUT"));
			for (int i = pool.Count - 1; i > 0; i--)
			{
				int j = rng.Next(i + 1);
				(pool[i], pool[j]) = (pool[j], pool[i]);
			}
			int scoutN = Math.Min(SCOUT_N, pool.Count);
			var scored = new List<Scouted>();
			// timing split (user note 2026-08-25 "load times are a bit severe...
			// A/B test what the times are like"): zone builds vs carve probes,
			// separately, so the dial decision is informed - if zone builds
			// dominate, fewer scouts is the lever; if probes do, a cheaper scorer.
			var swZone = new System.Diagnostics.Stopwatch();
			var swProbe = new System.Diagnostics.Stopwatch();
			for (int i = 0; i < scoutN; i++)
			{
				var L = pool[i];
				string zoneID = Zone.XYToID("JoppaWorld", L.X, L.Y, 10);
				// no per-scout counter: StepProgress keys on its TEXT, so a changing
				// string mints a new display entry per call (user flag 2026-08-25,
				// fifty "Scouting canyons" rows) - one line per phase, the lairs way
				The.ZoneManager.AdjustZoneGenerationTierTo(zoneID);
				swZone.Start();
				Zone Z = The.ZoneManager.GetZone(zoneID);
				swZone.Stop();
				if (Z == null) continue;
				swProbe.Start();
				var probe = WarrenCarver.Build(Z, WarrenCarver.SeedFor(zoneID), 2, apply: false);
				swProbe.Stop();
				scored.Add(new Scouted
				{
					ZoneID = zoneID,
					Loc = L,
					Capacity = probe.Built ? probe.StageIdx : -1,
					Floor = probe.Built ? probe.Floor : 0
				});
			}
			// the quota (dials); rolled in a +/-1 band so world stage-mixes differ
			int fl = Math.Max(0, QUOTA_FL + (QUOTA_ROLLED ? rng.Next(-1, 2) : 0));
			int camp = Math.Max(0, QUOTA_CAMP + (QUOTA_ROLLED ? rng.Next(-1, 2) : 0));
			int working = Math.Max(0, WARRENS_PER_WORLD - fl - camp);
			scored.Sort((a, b) => a.Capacity != b.Capacity
				? b.Capacity.CompareTo(a.Capacity)
				: b.Floor.CompareTo(a.Floor));
			int capFl = 0, capCamp = 0, capAny = 0;
			foreach (var s in scored)
			{
				if (s.Capacity >= 2) capFl++;
				if (s.Capacity >= 1) capCamp++;
				if (s.Capacity >= 0) capAny++;
			}
			// assign biggest-first; carve immediately; a refusal burns no slot,
			// the next capable spare steps up (refusals should be ~impossible for
			// flourishing - an exact probe replay - and rare below it)
			WorldCreationProgress.StepProgress("Raising quarry warrens...");
			var swClaim = new System.Diagnostics.Stopwatch();
			swClaim.Start();
			int sited = 0, refusals = 0;
			var builtByStage = new int[3];
			void Fill(int slots, int stage)
			{
				foreach (var s in scored)
				{
					if (slots <= 0) return;
					if (s.Used || s.Capacity < stage) continue;
					s.Used = true;
					var r = Claim(Builder, s, stage);
					if (r == null) { refusals++; continue; }
					builtByStage[r.StageIdx]++;
					sited++;
					slots--;
				}
			}
			Fill(fl, 2);
			Fill(camp, 1);
			Fill(working, 0);
			swClaim.Stop();
			// the capacity assert: quota vs capability vs what actually stood up
			Helpers.VerifyLog("WARREN",
				$"scout: pool {pool.Count}, scouted {scored.Count}, capable fl {capFl} / camp+ {capCamp} / any {capAny}; " +
				$"quota {fl}/{camp}/{working} (fl/camp/working); sited {sited} " +
				$"(built {builtByStage[2]} fl / {builtByStage[1]} camp / {builtByStage[0]} working), refusals {refusals}" +
				(sited < WARRENS_PER_WORLD ? $" - UNDER TARGET {WARRENS_PER_WORLD}" : ""));
			// the A/B datum: without-scout time ~ the claim phase alone (winners
			// would build lazily on visit anyway); the scout premium is zone+probe
			Helpers.VerifyLog("WARREN",
				$"scout timing: zone builds {swZone.ElapsedMilliseconds}ms ({scored.Count} zones, " +
				$"avg {(scored.Count > 0 ? swZone.ElapsedMilliseconds / scored.Count : 0)}ms), " +
				$"probes {swProbe.ElapsedMilliseconds}ms, claims {swClaim.ElapsedMilliseconds}ms; " +
				$"scout premium {swZone.ElapsedMilliseconds + swProbe.ElapsedMilliseconds}ms at SCOUT_N {SCOUT_N}");
		}

		/// <summary>The pool, enumerated. Reflection on the private terrain list
		/// (MutabilityMap.cs:13); fallback is bounded engine draws with dedupe.</summary>
		private static List<Location2D> EnumeratePool(JoppaWorldBuilder Builder)
		{
			var pool = new List<Location2D>();
			try
			{
				var f = typeof(MutabilityMap).GetField("mutableLocationsByTerrainType",
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				if (f?.GetValue(Builder.mutableMap) is Dictionary<string, List<Location2D>> byTerrain
					&& byTerrain.TryGetValue("DesertCanyon", out var list))
					foreach (var L in list)
						if (Builder.mutableMap.GetMutable(L) > 0)
							pool.Add(L);
			}
			catch (Exception x)
			{
				Helpers.VerifyLog("WARREN", $"pool reflection failed ({x.Message}); falling back to engine draws");
			}
			if (pool.Count == 0)
			{
				var seen = new HashSet<Location2D>();
				for (int t = 0; t < SCOUT_N * 20 && pool.Count < SCOUT_N * 2; t++)
				{
					var L = Builder.mutableMap.GetMutableLocationWithTerrain("DesertCanyon");
					if (L == null) break;
					if (seen.Add(L)) pool.Add(L);
				}
			}
			return pool;
		}

		/// <summary>Carve the assigned stage into the already-built zone and wire
		/// the claim. Returns the carve result, or null on refusal (no claim is
		/// written - a rumor only exists where a warren stands).</summary>
		private static WarrenCarver.Result Claim(JoppaWorldBuilder Builder, Scouted s, int stage)
		{
			Zone Z = The.ZoneManager.GetZone(s.ZoneID);
			if (Z == null) return null;
			var r = WarrenCarver.Build(Z, WarrenCarver.SeedFor(s.ZoneID), stage);
			if (!r.Built)
			{
				Helpers.VerifyLog("WARREN", $"scout refusal: {s.ZoneID} scored capacity {s.Capacity} but refused stage {STAGE_NAMES[stage]} ({r.Reason})");
				return null;
			}
			// rebuild reproducer: if the engine ever regenerates this zone, the
			// stamped builder re-carves the SAME warren (same seed, same stage)
			The.ZoneManager.AddZoneBuilder(s.ZoneID, 6000, "Cleo_TerraFirma_WarrenMaker", "Stage", STAGE_NAMES[stage]);
			Builder.terrainComponents.TryGetValue(Location2D.Get(s.Loc.X / 3, s.Loc.Y / 3), out XRL.World.Parts.TerrainTravel pTravel);
			if (Options.ShowOverlandEncounters && pTravel != null)
			{
				pTravel.ParentObject.Render.RenderString = "Q";
				pTravel.ParentObject.Render.SetForegroundColor('y');
			}
			// secret name is player-facing text: DRAFT, rides the release voice pass
			string secretID = Builder.AddSecret(s.ZoneID, "a quarry warren",
				new string[3] { "stonecarver", "settlement", "humanoid" }, "Settlements");
			Builder.AddLocationFinder(s.ZoneID, secretID);
			// PASS-OVER ENCOUNTER (user ask 2026-08-23, the village/lair idiom).
			// LINE IS DRAFT, user voice owed. The first-visit-dud hole is CLOSED
			// by construction now: the warren already exists when this is wired.
			pTravel?.AddEncounter(new XRL.World.Parts.EncounterEntry(
				"You hear the ring of hammers on stone nearby. Would you like to investigate?",
				s.ZoneID, "", secretID, Optional: true));
			// non-hostile by goal statement -> friendlySettlements
			Builder.worldInfo.friendlySettlements.Add(new GeneratedLocationInfo
			{
				name = "a quarry warren",
				targetZone = s.ZoneID,
				zoneLocation = s.Loc,
				secretID = secretID
			});
			Builder.mutableMap.RemoveMutableLocation(s.Loc);
			Builder.mutableMap.SetMutable(s.Loc, 0);
			Helpers.VerifyLog("WARREN", $"claimed: {s.ZoneID} as {r.Stage} (assigned {STAGE_NAMES[stage]}, capacity {STAGE_NAMES[Math.Max(s.Capacity, 0)]}), {r.Floor} floor, {r.Occupancy}");
			return r;
		}
	}
}
