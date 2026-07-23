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
	/// Also grants a small AV bonus that scales with the size of the reserve (see <see cref="Sync"/>).
	/// The reserve itself is the shield: deflecting costs nothing, but every stone thrown weakens it.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GravelWhorl : Effect
	{
		private const string DEBUG_CONTEXT = "GVW";
		private const int RICOCHET_RANGE = 5; // DRAFT (carom line length)

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
			// DRAFT text (outcome clause added with the three-stage ruling; beam-scatter clause
			// with the RefractLight upgrade, both 2026-07-22)
			if (Resonant)
				return $"Sheathed in keening gravel. {DeflectChance}% chance to deflect light-based projectiles -- devoured, swatted aside, or sent careening -- and to scatter laser beams. Physical projectiles pass through. Effect remains while stones remain in reserve.";
			return $"Sheathed in whorling gravel. {DeflectChance}% chance to deflect physical projectiles: devoured, swatted aside, or sent careening. Stones devoured feed the reserve. Lasers and other energy attacks pass through. Effect remains while stones remain in reserve.";
		}

		public override bool UseStandardDurationCountdown() => false;
		#endregion

		#region Logic
		/// <summary>
		/// Updates the deflect chance, attunement, and stat shift to match the current reserve.
		/// Normal: +1 AV at 3+ stones, +2 at 9+. Resonant: +2 DV at 3+, +3 at 9+.
		/// </summary>
		public void Sync(int NewDeflectChance, int Charges, bool NewResonant)
		{
			if (NewDeflectChance != DeflectChance || NewResonant != Resonant)
				Helpers.VerifyLog("GVW", $"sync: chance={NewDeflectChance}% charges={Charges} resonant={NewResonant}");
			DeflectChance = NewDeflectChance;
			Resonant = NewResonant;
			if (Resonant)
			{
				StatShifter.SetStatShift("AV", 0);
				StatShifter.SetStatShift("DV", Charges >= 9 ? 3 : (Charges >= 3 ? 2 : 0));
			}
			else
			{
				StatShifter.SetStatShift("DV", 0);
				StatShifter.SetStatShift("AV", Charges >= 9 ? 2 : (Charges >= 3 ? 1 : 0));
			}
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
				&& DeflectChance.in100())
			{
				Helpers.VerifyLog("GVW", $"BEAM SCATTER: resonant whorl refracted a Lase-type beam (chance={DeflectChance}%)");
				E.SetParameter("By", Object);
				E.SetParameter("Verb", "scatter"); // DRAFT
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
			bool procs = deflectable && DeflectChance.in100();
			Helpers.VerifyLog("GVW", $"incoming {E.Projectile?.Blueprint ?? "projectile"} (resonant={Resonant}): {(deflectable ? (procs ? "deflect PROC" : $"deflectable, no proc ({DeflectChance}%)") : "wrong type, passes through")}");
			if (procs)
			{
				// three-stage deflection (RE-RULED 2026-07-22: 50/30/20, supersedes even thirds --
				// devour common, swat mundane, ricochet the rare story): the whorl devours the
				// projectile, swats it aside, or sends it careening with its inertia preserved
				int outcome = Stat.Random(1, 100);
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
		/// capped at the mutation's max (RULED: the whorl grows); anything else is simply unmade.
		/// EXCEPT dangerous stones (RULED 2026-07-22): stones with effects on throw-destruction --
		/// the mill's own hazard boundary, IGrenade part or GrindSpawn tag -- don't join the mass,
		/// they're TORN APART in it: grenades detonate at the defender's cell (blame stays with the
		/// thrower), grind-special stones release their contents around the defender.
		/// </summary>
		private void Devour(GameObject projectile, GameObject attacker)
		{
			string whorlName = Object.Poss(DisplayName.Strip());
			if (projectile == null)
			{
				XDidY(whorlName, "devour", "the projectile", EndMark: "!", Actor: Object);
				return;
			}
			if (projectile.HasTag("Cleo_TerraFirma_IsAnyStone"))
			{
				XRL.World.Parts.IGrenade grenade = projectile.GetFirstPartDescendedFrom<XRL.World.Parts.IGrenade>();
				string grindSpawn = projectile.GetTag("Cleo_TerraFirma_GrindSpawn");
				if (grenade != null || !string.IsNullOrEmpty(grindSpawn))
				{
					Helpers.VerifyLog("GVW", $"devour: DANGEROUS stone {projectile.Blueprint} torn apart (grenade={grenade != null}, grindSpawn={grindSpawn ?? "none"})");
					XDidYToZ(whorlName, "tear", projectile, "apart", EndMark: "!", Actor: Object);
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
				XDidYToZ(whorlName, "devour", projectile, EndMark: "!", Actor: Object);
				Cleo_TerraFirma_EarthenBarrage eb = Object.GetPart<Cleo_TerraFirma_EarthenBarrage>();
				if (eb != null && eb.Charges < Cleo_TerraFirma_EarthenBarrage.MaxCharges(eb.Level))
				{
					eb.Charges++;
					eb.SyncWhorl();
					Helpers.VerifyLog("GVW", $"devour: plain stone {projectile.Blueprint} fed the reserve -> charges now {eb.Charges}");
					if (Object.IsPlayer())
						AddPlayerMessage("The whorl thickens."); // DRAFT
				}
				else
					Helpers.VerifyLog("GVW", $"devour: plain stone {projectile.Blueprint}, NO charge gained (reserve full or no EB part)");
				projectile.Obliterate();
				return;
			}
			Helpers.VerifyLog("GVW", $"devour: non-stone {projectile.Blueprint} OBLITERATED (flagged-vetoable delta: old spec dropped it at feet)");
			XDidYToZ(whorlName, "devour", projectile, EndMark: "!", Actor: Object);
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
				XDidY(whorlName, "deflect", "the projectile", EndMark: "!", Actor: Object);
			else if (projectile.HasPart<XRL.World.Parts.Projectile>())
			{
				// ephemeral munition (bullet, laser bolt) -- nothing real to preserve
				Helpers.VerifyLog("GVW", $"swat: munition {projectile.Blueprint} fizzled");
				XDidYToZ(whorlName, "deflect", projectile, EndMark: "!", Actor: Object);
				projectile.Obliterate();
			}
			else
			{
				XDidYToZ(whorlName, "swat", projectile, "aside", EndMark: "!", Actor: Object);
				List<Cell> open = Object.CurrentCell?.GetLocalAdjacentCells()?.FindAll(x => !x.IsSolid());
				Cell landing = (open != null && open.Count > 0) ? open[Stat.Random(0, open.Count - 1)] : Object.CurrentCell;
				Helpers.VerifyLog("GVW", $"swat: real item {projectile.Blueprint} batted to ({landing?.X},{landing?.Y}), loot preserved");
				landing?.AddObject(projectile);
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
				XDidY(whorlName, "send", "the projectile careening", EndMark: "!", Actor: Object);
			else
				XDidYToZ(whorlName, "send", projectile, "careening", EndMark: "!", Actor: Object);
			double rad = Stat.Random(0, 359) * Math.PI / 180.0;
			int ex = Math.Max(0, Math.Min(zone.Width - 1, origin.X + (int)Math.Round(Math.Cos(rad) * RICOCHET_RANGE)));
			int ey = Math.Max(0, Math.Min(zone.Height - 1, origin.Y + (int)Math.Round(Math.Sin(rad) * RICOCHET_RANGE)));
			Cell landing = origin;
			foreach (Point p in Zone.Line(origin.X, origin.Y, ex, ey))
			{
				Cell step = zone.GetCell(p.X, p.Y);
				if (step == null || step == origin)
					continue;
				if (step.IsSolid())
					break;
				landing = step;
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
			if (projectile != null)
			{
				if (ephemeral)
					projectile.Obliterate();
				else
					landing.AddObject(projectile);
			}
		}
		#endregion
	}
}
