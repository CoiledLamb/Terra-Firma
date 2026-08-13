using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects
{
	/// <summary>
	/// Applied and maintained solely by <see cref="Cleo_TerraFirma_EarthenBarrage"/> while it is toggled on with stones in reserve.
	/// Deflects physical projectiles at a rate of (<see cref="DeflectChance"/> in 100); energy projectiles pass through untouched.
	/// A successful deflect rolls one of three outcomes, 50/30/20 (RE-RULED 2026-07-22, supersedes
	/// even thirds same day -- rarity graded to drama): the whorl devours the projectile (plain
	/// stones feed the reserve +1; DANGEROUS stones -- grenade or grind-special, the mill's hazard
	/// boundary -- are instead torn apart in the mass and their effects trigger on the defender),
	/// swats it aside (the original behavior), or sends it careening along a fully random line with
	/// its inertia preserved.
	/// Resonant whorls additionally SCATTER LASE-TYPE BEAMS via the engine's native RefractLight
	/// machinery (RULED 2026-07-21, built 2026-07-22): beams only -- laser missile shots carry a
	/// Projectile param and stay in the outcome scale above, no double-dip.
	/// The reserve itself is the shield: deflecting costs nothing, but every stone thrown weakens it.
	/// (AV/DV stat shifts CUT 2026-08-06, beta rebalance pass, user: "cut stat shift from whorl" --
	/// deflection is the whole defensive identity now; Remove keeps RemoveStatShifts for old saves.)
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GravelWhorl : Effect
	{
		private const string DEBUG_CONTEXT = "GVW";
		private const int RICOCHET_RANGE = 5; // RATIFIED 2026-07-23 (carom line length; half EB throw range, a hair over the caromcobble's 4-cell legs)

		/// <summary>
		/// The chance for an incoming deflectable projectile to be deflected.
		/// Kept in sync by the mutation as charges change; see <see cref="Cleo_TerraFirma_EarthenBarrage.SyncWhorl"/>.
		/// </summary>
		public int DeflectChance;

		/// <summary>
		/// Attunement, synced from the mutation. Normal whorls deflect physical projectiles and grant AV;
		/// resonant whorls deflect Light-tagged projectiles (laser fire) and grant DV instead.
		/// </summary>
		public bool Resonant;


		/// <summary>Dev doors (tfwhorlgym/tfwhorllaser/tfwhorloutcome wishes, VFX sitting
		/// 2026-08-12; split from the original single force-devour flag once the swat and
		/// ricochet animations needed verifying too): ForceDeflect procs every type-eligible
		/// projectile, chance ignored; ForcedOutcome pins the 50/30/20 roll (0 natural,
		/// 1 devour, 2 swat, 3 ricochet). Static and unserialized -- process-wide, survive
		/// save/load in-session, off by default; ride the TF-VERIFY strip family. Type
		/// gating (physical vs resonant) deliberately still applies.</summary>
		public static bool DebugForceDeflect;
		public static int DebugForcedOutcome = 1; // the wishes arm deflect; devour stays the default pin

		#region Basic info
		public Cleo_TerraFirma_GravelWhorl()
		{
			// lifetime is purely managed by the mutation's SyncWhorl -- no countdown, no expiry race.
			// (a short refreshed duration caused the effect to flicker off/on every turn, spamming the
			// engine's generic status-effect-stop sound)
			Duration = 9999;
			DisplayName = "{{y|gravel whorl}}";
		}

		public Cleo_TerraFirma_GravelWhorl(int deflectChance) : this()
		{
			DeflectChance = deflectChance;
		}

		public override string GetDetails()
		{
			// RATIFIED 2026-07-23; TRIMMED by user ruling 2026-08-06 (AI-tells audit): the
			// "% chance to deflect" sentence and "Stones devoured feed the reserve." cut. This
			// also retires the 2026-07-23 dash flag (the dashes lived in the cut sentence).
			// The deflect mechanic stays documented in the Earthen Barrage / Attune Stones text.
			if (Resonant)
				return "Sheathed in keening gravel. Physical projectiles pass through. Lasts while stones remain in reserve.";
			return "Sheathed in whorling gravel. Lasers and other energy attacks pass through. Lasts while stones remain in reserve.";
		}

		public override bool UseStandardDurationCountdown() => false;
		#endregion

		#region Logic
		/// <summary>
		/// Updates the deflect chance and attunement to match the current reserve.
		/// (The AV/DV reserve-size shifts -- +1/+2 AV, resonant +2/+4 DV per the 2026-07-23
		/// compensation ruling -- were CUT at the 2026-08-06 rebalance: any lingering shift
		/// from a pre-cut save is cleared here.)
		/// </summary>
		public void Sync(int NewDeflectChance, int Charges, bool NewResonant)
		{
			if (NewDeflectChance != DeflectChance || NewResonant != Resonant)
				Helpers.VerifyLog("GVW", $"sync: chance={NewDeflectChance}% charges={Charges} resonant={NewResonant}");
			DeflectChance = NewDeflectChance;
			Resonant = NewResonant;
			StatShifter.RemoveStatShifts();
		}

		public override bool Apply(GameObject Object)
		{
			// registered regardless of attunement (Sync flips Resonant at runtime); the handler
			// gates on Resonant. String-event registration = the BlinkingTicSickness effect idiom.
			Object.RegisterEffectEvent(this, "RefractLight");
			return true;
		}

		public override void Remove(GameObject Object)
		{
			Object.UnregisterEffectEvent(this, "RefractLight");
			StatShifter.RemoveStatShifts();
			base.Remove(Object);
		}

		/// <summary>
		/// Resonant beam-scatter (spec'd 2026-07-21, built 2026-07-22): the engine's native
		/// RefractLight machinery, which lives in the BEAM path -- so resonant whorls deflect
		/// Lase-type beams, closing the "direct beams pass through" gap. BEAMS ONLY: laser missile
		/// shots fire this same event with a Projectile param set (decompiled MissileWeapon.cs) and
		/// are excluded here -- they already roll the missile-hit outcome scale; no double-dip.
		/// Direction is deliberately NOT set: both beam paths pre-seed Direction with
		/// Stat.Random(0,359), so an unset Direction = a fully random scatter, engine-native --
		/// gravel scatters, espers mirror. Sound stays the engine's sfx_light_refract default.
		/// </summary>
		public override bool FireEvent(Event E)
		{
			if (E.ID == "RefractLight" && Resonant
				&& E.GetGameObjectParameter("Projectile") == null
				&& (DebugForceDeflect || DeflectChance.in100()))
			{
				Helpers.VerifyLog("GVW", $"BEAM SCATTER: resonant whorl refracted a Lase-type beam (chance={DeflectChance}%)");
				E.SetParameter("By", Object);
				E.SetParameter("Verb", "scatter"); // RATIFIED 2026-07-23 (engine default "refract" is mirror-flavored; gravel scatters)
				// second-leg light track (VFX sitting 2026-08-12): the pre-seeded Direction we
				// leave untouched is exactly what the beam path consumes after we return false
				Cleo_TerraFirma_GravelWhorlFX.PlayRefractTrack(Object.CurrentCell, E.GetIntParameter("Direction"));
				return false;
			}
			return base.FireEvent(E);
		}

		/// <summary>
		/// Whether the whorl can interact with this projectile at all.
		/// Objects without a Projectile part (thrown items) always count as physical;
		/// otherwise anything tagged as light/energy slips through the gaps.
		/// </summary>
		public static bool IsPhysicalProjectile(GameObject Projectile)
		{
			string attrs = Projectile?.GetPart<XRL.World.Parts.Projectile>()?.Attributes;
			if (attrs.IsNullOrEmpty())
				return true;
			return !(attrs.Contains("Light") || attrs.Contains("Laser") || attrs.Contains("Plasma") || attrs.Contains("Electric"));
		}
		#endregion

		#region Events
		public override bool WantEvent(int ID, int Cascade)
		{
			if (!base.WantEvent(ID, Cascade))
				return ID == PooledEvent<DefenderMissileHitEvent>.ID;
			return true;
		}

		/// <summary>
		/// Light-tagged projectiles (laser fire) are the resonant whorl's prey. Direct beams never enter
		/// this event at all and pass through in either mode.
		/// </summary>
		public static bool IsLightProjectile(GameObject Projectile)
		{
			string attrs = Projectile?.GetPart<XRL.World.Parts.Projectile>()?.Attributes;
			return !attrs.IsNullOrEmpty() && attrs.Contains("Light");
		}

		public override bool HandleEvent(DefenderMissileHitEvent E)
		{
			bool deflectable = Resonant ? IsLightProjectile(E.Projectile) : IsPhysicalProjectile(E.Projectile);
			bool procs = deflectable && (DebugForceDeflect || DeflectChance.in100());
			Helpers.VerifyLog("GVW", $"incoming {E.Projectile?.Blueprint ?? "projectile"} (resonant={Resonant}): {(deflectable ? (procs ? "deflect PROC" : $"deflectable, no proc ({DeflectChance}%)") : "wrong type, passes through")}");
			if (procs)
			{
				// three-stage deflection (RE-RULED 2026-07-22: 50/30/20, supersedes even thirds --
				// devour common, swat mundane, ricochet the rare story): the whorl devours the
				// projectile, swats it aside, or sends it careening with its inertia preserved
				int outcome = DebugForceDeflect && DebugForcedOutcome > 0
					? (DebugForcedOutcome == 1 ? 1 : DebugForcedOutcome == 2 ? 51 : 81)
					: Stat.Random(1, 100);
				Helpers.VerifyLog("GVW", $"outcome roll {outcome} -> {(outcome <= 50 ? "DEVOUR" : outcome <= 80 ? "SWAT" : "RICOCHET")}");
				if (outcome <= 50)
					Devour(E.Projectile, E.Attacker);
				else if (outcome <= 80)
					Swat(E.Projectile);
				else
					Ricochet(E.Projectile);
				PlayWorldSound("Sounds/Throw/sfx_throwing_stone_small_impact", 1f, Combat: true);
				E.Done = true;
				return false;
			}
			return base.HandleEvent(E);
		}

		/// <summary>
		/// Outcome 1: the projectile is devoured. A plain stone feeds the reserve -- +1 charge,
		/// capped at the mutation's max (RULED: the whorl grows); anything else is simply unmade
		/// (obliteration RULED KEPT 2026-07-23: thrown items are almost always disposable, and
		/// throwing expensive things at a whorl deserves what it finds out).
		/// EXCEPT dangerous projectiles: stones with effects on throw-destruction (RULED
		/// 2026-07-22: the mill's own hazard boundary, IGrenade part or GrindSpawn tag) --
		/// EXTENDED to ALL grenades 2026-07-23 (a devoured grenade has done its job) -- don't
		/// join the mass, they're TORN APART in it: grenades detonate at the defender's cell
		/// (blame stays with the thrower), grind-special stones release their contents around
		/// the defender.
		/// </summary>
		private void Devour(GameObject projectile, GameObject attacker)
		{
			string whorlName = Object.Poss(DisplayName.Strip());
			if (projectile == null)
			{
				// resonant verb set (RULED 2026-08-12: snuffs/diffuses accepted from draft,
				// refract the user's own word): resonance unmakes light, mass gets ground
				XDidY(whorlName, Resonant ? "snuffs" : "devours", "the projectile", EndMark: "!", Source: Object);
				Cleo_TerraFirma_GravelWhorlFX.PlayDevourSwirl(Object, null, attacker); // renderless: whorl-gray flecks
				return;
			}
			XRL.World.Parts.IGrenade grenade = projectile.GetFirstPartDescendedFrom<XRL.World.Parts.IGrenade>();
			string grindSpawn = projectile.GetTag("Cleo_TerraFirma_GrindSpawn");
			if (grenade != null || !string.IsNullOrEmpty(grindSpawn))
			{
				Helpers.VerifyLog("GVW", $"devour: DANGEROUS projectile {projectile.Blueprint} torn apart (grenade={grenade != null}, grindSpawn={grindSpawn ?? "none"})");
				XDidYToZ(whorlName, "tears", projectile, "apart", EndMark: "!", Source: Object);
				if (!string.IsNullOrEmpty(grindSpawn))
				{
					int spawnCount = 1;
					int.TryParse(projectile.GetTag("Cleo_TerraFirma_GrindSpawnCount", "1"), out spawnCount);
					SpawnAround(grindSpawn, Math.Max(1, spawnCount));
				}
				grenade?.Detonate(Object.CurrentCell, attacker ?? Object, Object, Indirect: true);
				if (GameObject.Validate(projectile))
					projectile.Obliterate();
				return;
			}
			if (projectile.HasTag("Cleo_TerraFirma_IsAnyStone"))
			{
				XDidYToZ(whorlName, "devours", projectile, EndMark: "!", Source: Object);
				Cleo_TerraFirma_GravelWhorlFX.PlayDevourSwirl(Object, projectile, attacker); // before Obliterate: palette theft needs the live Render
				Cleo_TerraFirma_EarthenBarrage eb = Object.GetPart<Cleo_TerraFirma_EarthenBarrage>();
				if (eb != null && eb.Charges < Cleo_TerraFirma_EarthenBarrage.MaxCharges(eb.Level))
				{
					eb.Charges++;
					eb.SyncWhorl();
					Helpers.VerifyLog("GVW", $"devour: plain stone {projectile.Blueprint} fed the reserve -> charges now {eb.Charges}");
					if (Object.IsPlayer())
						AddPlayerMessage("The whorl thickens."); // RATIFIED 2026-07-23 ("churns" floated and withdrawn: thickens carries the growth)
				}
				else
					Helpers.VerifyLog("GVW", $"devour: plain stone {projectile.Blueprint}, NO charge gained (reserve full or no EB part)");
				projectile.Obliterate();
				return;
			}
			Helpers.VerifyLog("GVW", $"devour: non-stone {projectile.Blueprint} OBLITERATED (RULED KEPT 2026-07-23)");
			XDidYToZ(whorlName, Resonant ? "snuffs" : "devours", projectile, EndMark: "!", Source: Object);
			Cleo_TerraFirma_GravelWhorlFX.PlayDevourSwirl(Object, projectile, attacker);
			projectile.Obliterate();
		}

		/// <summary>First object lands at the defender's cell, the rest scatter into open neighbors
		/// (the Lithovore EatenSpawn scatter shape; the mill's SpawnAround is private to its class).</summary>
		private void SpawnAround(string blueprint, int count)
		{
			Cell cell = Object.CurrentCell;
			if (cell == null)
				return;
			cell.AddObject(GameObject.Create(blueprint));
			List<Cell> open = cell.GetLocalAdjacentCells();
			open.RemoveAll((Cell x) => x.IsSolid());
			for (int i = 1; i < count && open.Count > 0; i++)
			{
				int idx = Stat.Random(0, open.Count - 1);
				open[idx].AddObject(GameObject.Create(blueprint));
				open.RemoveAt(idx);
			}
		}

		/// <summary>Outcome 2: the original behavior -- munitions fizzle, real items are batted into
		/// a nearby open cell instead of destroying potential loot.</summary>
		private void Swat(GameObject projectile)
		{
			string whorlName = Object.Poss(DisplayName.Strip());
			if (projectile == null)
				XDidY(whorlName, Resonant ? "diffuses" : "deflects", "the projectile", EndMark: "!", Source: Object);
			else if (projectile.HasPart<XRL.World.Parts.Projectile>())
			{
				// ephemeral munition (bullet, laser bolt) -- nothing real to preserve
				Helpers.VerifyLog("GVW", $"swat: munition {projectile.Blueprint} fizzled");
				XDidYToZ(whorlName, Resonant ? "diffuses" : "deflects", projectile, EndMark: "!", Source: Object);
				Cleo_TerraFirma_GravelWhorlFX.PlaySwatBeat(Object.CurrentCell, null); // grit pinch: ground to nothing
				projectile.Obliterate();
			}
			else
			{
				XDidYToZ(whorlName, "swats", projectile, "aside", EndMark: "!", Source: Object);
				List<Cell> open = Object.CurrentCell?.GetLocalAdjacentCells()?.FindAll(x => !x.IsSolid());
				Cell landing = (open != null && open.Count > 0) ? open[Stat.Random(0, open.Count - 1)] : Object.CurrentCell;
				Helpers.VerifyLog("GVW", $"swat: real item {projectile.Blueprint} batted to ({landing?.X},{landing?.Y}), loot preserved");
				landing?.AddObject(projectile);
				MarkDeflected(projectile, landing);
				Cleo_TerraFirma_GravelWhorlFX.PlaySwatBeat(Object.CurrentCell, landing); // dust + blip: the loot pointer
			}
		}

		/// <summary>
		/// Outcome 3: inertia preserved -- the projectile careens off the whorl along a FULLY RANDOM
		/// line (RULED after checking the Light Manipulation precedent: base reflect is angle-based,
		/// not target-picked -- it sets a direction and lets geometry decide). The first creature on
		/// the line takes a flat d20 vs DV (the whorl has no marksmanship) and the projectile's own
		/// damage; the carom ends at the first body, hit or flinched. Walls stop it. Real items land
		/// where the carom ends; munitions spend themselves. Kill attribution: the whorl's owner.
		/// Payload channels deliberately do NOT fire (the whorl redirects mass, not triggers).
		/// VFX (RULED 2026-07-23, "a good one to implement now"): staggered gravel blips along the
		/// carom line + a dust puff where it ends; richer animations parked for a later pass.
		/// All numbers DRAFT.
		/// </summary>
		private void Ricochet(GameObject projectile)
		{
			string whorlName = Object.Poss(DisplayName.Strip());
			Cell origin = Object.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (origin == null || zone == null)
			{
				Swat(projectile);
				return;
			}
			bool ephemeral = projectile == null || projectile.HasPart<XRL.World.Parts.Projectile>();
			string damageDie = ephemeral
				? projectile?.GetPart<XRL.World.Parts.Projectile>()?.BaseDamage
				: (projectile.GetPart<XRL.World.Parts.ThrownWeapon>()?.Damage ?? "1d2");
			if (projectile == null)
			{
				if (Resonant)
					XDidY(whorlName, "refracts", "the projectile", EndMark: "!", Source: Object);
				else
					XDidY(whorlName, "sends", "the projectile careening", EndMark: "!", Source: Object);
			}
			else if (Resonant)
				XDidYToZ(whorlName, "refracts", projectile, EndMark: "!", Source: Object);
			else
				XDidYToZ(whorlName, "sends", projectile, "careening", EndMark: "!", Source: Object);
			Cleo_TerraFirma_GravelWhorlFX.PlayRicochetElbow(origin); // the bend gets a joint (go-ahead 2026-08-12)
			double rad = Stat.Random(0, 359) * Math.PI / 180.0;
			int ex = Math.Max(0, Math.Min(zone.Width - 1, origin.X + (int)Math.Round(Math.Cos(rad) * RICOCHET_RANGE)));
			int ey = Math.Max(0, Math.Min(zone.Height - 1, origin.Y + (int)Math.Round(Math.Sin(rad) * RICOCHET_RANGE)));
			Cell landing = origin;
			int traceStep = 0; // staggers the carom trace so the blips read as travel, not a flash
			foreach (Point p in Zone.Line(origin.X, origin.Y, ex, ey))
			{
				Cell step = zone.GetCell(p.X, p.Y);
				if (step == null || step == origin)
					continue;
				if (step.IsSolid())
					break;
				landing = step;
				if (step.InActiveZone)
					The.ParticleManager.Add("&y±", step.X, step.Y, 0f, 0f, 12, 0f, 0f, traceStep * 40L);
				traceStep++;
				GameObject victim = null;
				foreach (GameObject o in step.Objects)
				{
					if (o != Object && o.HasPart("Combat") && o.PhaseMatches(Object))
					{
						victim = o;
						break;
					}
				}
				if (victim != null)
				{
					if (Stat.Random(1, 20) > Stats.GetCombatDV(victim))
					{
						int dmg = damageDie.IsNullOrEmpty() ? 1 : Math.Max(1, damageDie.RollCached());
						Helpers.VerifyLog("GVW", $"ricochet: struck {victim.Blueprint} for {dmg} (die {damageDie ?? "1"})");
						victim.TakeDamage(dmg, "from a careening projectile!", "Physical",
							null, null, Owner: Object, Attacker: null, Source: projectile);
					}
					else
					{
						Helpers.VerifyLog("GVW", $"ricochet: {victim.Blueprint} flinched out of the way");
						IComponent<GameObject>.XDidY(victim, "flinch", "out of the way", "!", null, null, victim);
					}
					break; // the carom ends at the first body, hit or flinched
				}
			}
			Helpers.VerifyLog("GVW", $"ricochet: {projectile?.Blueprint ?? "munition"} carom ended at ({landing.X},{landing.Y}); {(projectile == null || ephemeral ? "munition spent" : "item lands there")}");
			if (landing != origin)
				Cleo_TerraFirma_GravelWhorlFX.PlayCaromLanding(landing); // grit in tiles, dust in text (less-smoke round 2026-08-12)
			if (projectile != null)
			{
				if (ephemeral)
					projectile.Obliterate();
				else
				{
					landing.AddObject(projectile);
					MarkDeflected(projectile, landing);
				}
			}
		}

		/// <summary>
		/// RULED 2026-07-23: a deflected throw is the whorl's to place and not the thrower's use
		/// to break. The engine's PerformThrow finishes the throw after this outcome has acted --
		/// re-placing the weapon at its own landing cell and rolling throw-fragility -- so real
		/// deflected thrown weapons get marked: the mark restores the outcome's placement at
		/// AfterThrownEvent, and FallenRockPart skips its fragility roll while marked.
		/// </summary>
		private void MarkDeflected(GameObject projectile, Cell landing)
		{
			if (projectile == null || landing == null)
				return;
			XRL.World.Parts.Cleo_TerraFirma_WhorlDeflectedPart mark =
				projectile.RequirePart<XRL.World.Parts.Cleo_TerraFirma_WhorlDeflectedPart>();
			mark.Intended = landing;
		}
		#endregion
	}
}
