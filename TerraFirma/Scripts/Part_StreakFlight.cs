using System;
using System.Collections.Generic;
using System.Linq;
using Cleo.TerraFirma.Scripts;
using ConsoleLib.Console;
using XRL.Rules;
using XRL.UI;
using XRL.World.AI.Pathfinding;
using XRL.World.Capabilities;

namespace XRL.World.Parts
{
	/// <summary>
	/// Streakschorl's seek-and-bounce flight (RULED 2026-07-22): thrown at a creature, the stone
	/// flies the geomagnetic disc's flight -- it pathfinds to the aimed target as a flyer (walls
	/// block it, creatures and gases do not), strikes, then chains to up to <see cref="Bounces"/>
	/// more hostiles within range -- and SPENDS ITSELF at the end of the flight (single use, no
	/// return leg). Hybrid to-hit (RULED): the aimed target is auto-hit (the never-miss identity);
	/// bounce targets roll the disc's d20 + Agility vs DV and can flinch out of the way. Damage
	/// rides the rock's own ThrownWeapon part per hit (1d6 vibro/Vorpal), exactly as the disc reads
	/// its own. No hostile reachable (or thrown out of flight range, or no flyable line) = the
	/// handler returns true and the normal never-miss throw proceeds: the fallback rock lands
	/// intact and step-fragile as before.
	///
	/// Transcribed from decompiled GeomagneticDisc (BeforeThrown hijack + private DoThrow), with
	/// the disc's return leg, power plant (IPoweredPart/charge/power-load), two-discs-collision
	/// check, examine-failure behavior, and flex-phase provider support all deliberately dropped
	/// (rocks take no weapon mods). Known transcription semantics kept from the disc, flagged for
	/// verify: a ProjectileMovingEvent handler that DISPLACES the stone mid-flight ends the flight
	/// with the stone restored to the thrower's context (the disc's own early-return behavior --
	/// reads as "it found its way back", accept or revisit); non-displacing interceptions strike
	/// (HitOverride) or just end the flight, then the stone is spent as normal. Bounces RULED 2 flat;
	/// Range 6 RATIFIED 2026-07-24.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StreakFlightPart : IPart
	{
		/// <summary>Extra victims after the aimed target (RULED: 2 flat).</summary>
		public string Bounces = "2";

		/// <summary>Per-leg range bonus on top of base throw range (the disc's Range, dialed down from its 12).</summary>
		public string Range = "6";

		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("BeforeThrown");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "BeforeThrown")
			{
				GameObject thrower = E.GetGameObjectParameter("Thrower");
				if (thrower == null)
					return false;
				if (ParentObject.CurrentCell != null)
					return false;
				GameObject target = E.GetGameObjectParameter("ApparentTarget");
				Cell targetCell = E.GetParameter("TargetCell") as Cell;
				if (targetCell == null)
					return true;
				int legRange = thrower.GetBaseThrowRange(ParentObject, null, targetCell, thrower.DistanceTo(targetCell)) + ParentObject.GetIntProperty("ThrowRangeBonus") + Range.RollCached();
				FindPath aimedPath = null;
				if (target == null || target == thrower)
				{
					// the disc's auto-seek: thrown at nothing in particular, it finds a hostile itself
					List<GameObject> minds = thrower.CurrentCell.FastFloodSearch("Brain", legRange);
					if (minds.Count > 0)
					{
						minds.ShuffleInPlace();
						foreach (GameObject mind in minds)
						{
							if (mind != thrower && (thrower.IsHostileTowards(mind) || mind.IsHostileTowards(thrower)))
							{
								FindPath seekPath = new FindPath(thrower.CurrentCell, mind.CurrentCell, PathGlobal: false, PathUnlimited: true, ParentObject, 99, ExploredOnly: false, Juggernaut: false, IgnoreCreatures: true, IgnoreGases: true);
								if (seekPath.Usable && seekPath.Directions.Count <= legRange)
								{
									target = mind;
									aimedPath = seekPath;
									break;
								}
							}
						}
					}
					if (target == null || target == thrower)
					{
						Helpers.VerifyLog("STRK", "no hostile to seek -> FALLBACK normal never-miss throw");
						return true; // nothing to seek -- the normal never-miss throw proceeds
					}
				}
				if (thrower.DistanceTo(target) > legRange)
				{
					Helpers.VerifyLog("STRK", $"target {target.Blueprint} beyond flight range {legRange} -> FALLBACK normal throw");
					return true; // beyond flight range -- normal throw
				}
				if (aimedPath == null)
				{
					aimedPath = new FindPath(thrower.CurrentCell, target.CurrentCell, PathGlobal: false, PathUnlimited: true, ParentObject, 99, ExploredOnly: false, Juggernaut: false, IgnoreCreatures: true, IgnoreGases: true);
					if (!aimedPath.Usable || aimedPath.Directions.Count > legRange)
					{
						Helpers.VerifyLog("STRK", $"no flyable line to {target.Blueprint} -> FALLBACK normal throw");
						return true; // no flyable line -- normal throw
					}
				}
				if (thrower.IsPlayer())
					thrower.Target = target;
				// the flight is committed: give back the never-miss accuracy lend NOW. The hijack
				// bypasses WeaponThrowHit/AfterThrown (the normal return points), and the stone is
				// spent at flight's end -- without this the lend leaks permanently (live-fire find
				// 2026-07-23: only ONE lend line across three flights = the property stuck).
				ParentObject.GetPart<Cleo_TerraFirma_FallenRockPart>()?.ReturnLentAccuracy();
				// capture the slot BEFORE the split/flight: the hijack bypasses the engine's throw
				// completion, so we must fire its replacement flow ourselves after the stone is spent
				XRL.World.Anatomy.BodyPart thrownSlot = ParentObject.EquippedOn();
				ParentObject.SplitFromStack(); // fly (and spend) exactly one from a stack
				thrower.UseEnergy(1000, "Physical Skill Throwing Item");
				List<GameObject> victims = Event.NewGameObjectList();
				List<FindPath> legs = new List<FindPath>();
				victims.Add(target);
				legs.Add(aimedPath);
				int bounces = Bounces.RollCached();
				for (int i = 0; i < bounces && i < victims.Count; i++)
				{
					List<GameObject> nearby = victims[i].CurrentCell.FastFloodSearch("Brain", legRange);
					if (nearby.Count <= 0)
						continue;
					nearby.ShuffleInPlace();
					foreach (GameObject next in nearby)
					{
						if (next == thrower || victims.Contains(next) || (!thrower.IsHostileTowards(next) && !next.IsHostileTowards(thrower)))
							continue;
						FindPath leg = new FindPath(victims[i].CurrentCell, next.CurrentCell, PathGlobal: false, PathUnlimited: true, ParentObject, 99, ExploredOnly: false, Juggernaut: false, IgnoreCreatures: true, IgnoreGases: true);
						if (leg.Usable && leg.Directions.Count <= legRange)
						{
							victims.Add(next);
							legs.Add(leg);
							break;
						}
					}
				}
				Helpers.VerifyLog("STRK", $"FLIGHT: {thrower.Blueprint} throws at {victims[0].Blueprint}; {victims.Count - 1} bounce target(s) [{string.Join(", ", victims.Skip(1).Select(v => v.Blueprint))}], legRange={legRange}");
				DoFlight(thrower, legs, victims, E.GetIntParameter("Phase"), E);
				// the flight spent the stone outside the engine's throw path, so no one fired the
				// replacement flow -- the next streakschorl in the bag never re-equipped and EB's
				// auto-sculpt never re-armed (user live-fire find 2026-07-24). Fire it ourselves,
				// but only when the slot really was vacated (fallback throws keep the normal path).
				if (thrownSlot != null && (!GameObject.Validate(ParentObject) || ParentObject.EquippedOn() != thrownSlot))
				{
					Helpers.VerifyLog("STRK", $"flight spent the stone -> firing ReplaceThrownWeapon for {thrower.Blueprint}'s {thrownSlot.GetOrdinalName()}");
					ReplaceThrownWeaponEvent.Check(thrower, ParentObject, thrownSlot);
				}
				return false;
			}
			return base.FireEvent(E);
		}

		private void DoFlight(GameObject Actor, List<FindPath> Paths, List<GameObject> Targets, int Phase, IEvent ParentEvent)
		{
			if (Paths == null || Paths.Count <= 0)
				return;
			ParentObject.GetContext(out var ObjectContext, out var CellContext, out var BodyPartContext, out var Relation, out var RelationManager);
			try
			{
				// the disc's own whoosh suits a stone breaking the sound barrier; retag if it grates
				PlayWorldSound(ParentObject.GetTagOrStringProperty("ThrownSound", "Sounds/Throw/sfx_throwing_geoMagneticDisc_throw"));
				Cell cell = CellContext ?? ParentObject.GetCurrentCell();
				bool active = Actor.CurrentZone.IsActive();
				bool vfx = active && Options.UseParticleVFX;
				string text = ParentObject.t();
				string damageMessage = "from " + text + ".";
				if (Phase == 0)
					Phase = XRL.World.Capabilities.Phase.getWeaponPhase(Actor, GetActivationPhaseEvent.GetFor(ParentObject));
				int toHitBonus = Actor.StatMod("Agility") + Actor.GetIntProperty("ThrowToHitBonus") + Actor.GetIntProperty("ThrowToHitSkillBonus");
				if (RelationManager != null)
					ParentObject.RemoveFromContext();
				// (deliberately NO return leg appended here: the flight ends at the last victim)
				List<GameObject> wanters = Actor.CurrentZone.GetObjectsThatWantEvent(PooledEvent<ProjectileMovingEvent>.ID, ProjectileMovingEvent.CascadeLevel);
				ProjectileMovingEvent moving = null;
				if (wanters.Count > 0)
				{
					moving = PooledEvent<ProjectileMovingEvent>.FromPool();
					moving.Attacker = Actor;
					moving.Projectile = ParentObject;
					moving.Throw = true;
				}
				TextConsole textConsole = Look._TextConsole;
				ScreenBuffer scrapBuffer = TextConsole.ScrapBuffer;
				ScreenBuffer scrapBuffer2 = TextConsole.ScrapBuffer2;
				if (active && !vfx)
				{
					scrapBuffer.Draw();
					The.Core.RenderMapToBuffer(scrapBuffer);
					scrapBuffer2.Copy(scrapBuffer);
				}
				RenderEvent r = ((active && !vfx) ? ParentObject.RenderForUI(null, AsIfKnown: true) : null);
				int i = 0;
				for (int count = Paths.Count; i < count; i++)
				{
					FindPath leg = Paths[i];
					GameObject victim = ((Targets != null && Targets.Count > i) ? Targets[i] : null);
					bool aimed = i == 0; // hybrid to-hit ruling: the aimed strike cannot be flinched away
					if (moving != null)
					{
						moving.ApparentTarget = victim;
						moving.TargetCell = victim?.CurrentCell ?? Actor.CurrentCell;
						moving.Path = leg.Steps.Select((Cell c) => c.Point).ToList();
						moving.ScreenBuffer = scrapBuffer;
					}
					bool showUninvolved = false;
					int j = 0;
					for (int last = leg.Steps.Count - 1; j <= last; j++)
					{
						leg.Steps[j].WakeCreaturesInArea();
						if (RelationManager != null)
						{
							leg.Steps[j].AddObject(ParentObject, Forced: false, System: false, IgnoreGravity: true, NoStack: true, Silent: false, Repaint: true, FlushTransient: true, null, "Thrown", null, Actor, null, null, null, ParentEvent);
							if (!GameObject.Validate(ParentObject) || ParentObject.CurrentCell != leg.Steps[j])
								return;
						}
						if (moving != null)
						{
							bool intercepted = false;
							moving.Cell = leg.Steps[j];
							moving.PathIndex = j;
							foreach (GameObject wanter in wanters)
							{
								bool passed = wanter.HandleEvent(moving);
								if (!GameObject.Validate(ParentObject) || ParentObject.CurrentCell != leg.Steps[j])
									return;
								if (moving.ActivateShowUninvolved)
								{
									showUninvolved = true;
									moving.ActivateShowUninvolved = false;
								}
								if (moving.RecheckPhase)
									Phase = ParentObject.GetPhase();
								if (moving.HitOverride != null)
								{
									victim = moving.HitOverride;
									moving.HitOverride = null;
									aimed = false; // an interception is not the aimed strike
									intercepted = true;
									break;
								}
								if (!passed)
								{
									victim = null;
									intercepted = true;
									break;
								}
							}
							if (intercepted)
							{
								i = count; // the flight ends here (no return leg to reroute onto)
								break;
							}
						}
						if (active && !vfx)
						{
							scrapBuffer.Copy(scrapBuffer2);
							scrapBuffer.Goto(leg.Steps[j].X, leg.Steps[j].Y);
							scrapBuffer.Write(r);
							scrapBuffer.Draw();
							textConsole.WaitFrame();
						}
						cell = leg.Steps[j];
					}
					if (vfx && cell != null && leg.Steps[0] != cell)
					{
						The.Core.RenderBase();
						CombatJuiceEntryMissileWeaponVFX entry = CombatJuice.Throw(Actor, ParentObject, leg.Steps[0].Location, cell.Location);
						if (i >= count - 1)
							CombatJuice.BlockUntilFinished((CombatJuiceEntry)entry, (IList<GameObject>)null, 1500, Interruptible: true);
					}
					if (victim == null)
						continue;
					int roll = Stat.Random(1, 20);
					int total = toHitBonus + roll;
					int dv = Stats.GetCombatDV(victim);
					Helpers.VerifyLog("STRK", $"leg {i}: {victim.Blueprint} {(aimed ? "AIMED auto-hit" : $"bounce roll {roll}+{toHitBonus}={total} vs DV {dv} -> {(total > dv || roll == 20 ? "hit" : "flinch")}")}");
					if (aimed || total > dv || roll == 20)
					{
						if (victim.PhaseMatches(Phase))
						{
							victim.PlayWorldSound(ParentObject.GetTagOrStringProperty("ImpactSound", "Sounds/Throw/sfx_throwing_geoMagneticDisc_impact"));
							bool isCreature = victim.IsCreature;
							string blueprint = victim.Blueprint;
							WeaponUsageTracking.TrackThrownWeaponHit(Actor, ParentObject, isCreature, blueprint, Accidental: false);
							if (IComponent<GameObject>.Visible(victim))
								victim.ParticleBlip("&C\u0003", 10, 0L);
							int av = Stats.GetCombatAV(victim);
							string Damage = ParentObject.GetPart<ThrownWeapon>()?.Damage ?? "1d6";
							int Penetration = av;
							int PenetrationBonus = 0;
							int PenetrationModifier = av;
							bool Vorpal = true;
							GetThrownWeaponPerformanceEvent.GetFor(ParentObject, ref Damage, ref Penetration, ref PenetrationBonus, ref PenetrationModifier, ref Vorpal, Prospective: false, Actor, victim);
							int dmg = Damage.GetCachedDieRoll().Resolve();
							Event hitEvent = Event.New("WeaponPseudoThrowHit");
							hitEvent.SetParameter("Damage", dmg);
							hitEvent.SetParameter("Owner", Actor);
							hitEvent.SetParameter("Attacker", Actor);
							hitEvent.SetParameter("Defender", victim);
							hitEvent.SetParameter("Weapon", ParentObject);
							hitEvent.SetParameter("Projectile", ParentObject);
							hitEvent.SetParameter("ApparentTarget", victim);
							ParentObject.FireEvent(hitEvent);
							dmg = hitEvent.GetIntParameter("Damage");
							victim.TakeDamage(ref dmg, "Thrown PseudoThrown Vorpal Cudgel", null, null, Owner: Actor, Attacker: null, Source: ParentObject, Perspective: null, DescribeAsFrom: null, Message: damageMessage, Accidental: false, Environmental: false, Indirect: false, ShowUninvolved: showUninvolved, IgnoreVisibility: false, ShowForInanimate: true, SilentIfNoDamage: false, NoSetTarget: false, UsePopups: false, Phase: Phase);
							WeaponUsageTracking.TrackThrownWeaponDamage(Actor, ParentObject, isCreature, blueprint, Accidental: false, dmg);
						}
						else if (IComponent<GameObject>.Visible(victim))
						{
							victim.ParticleBlip("&b\t", 10, 0L);
							DidXToY("pass", "through", victim, null, "!", null, null, victim);
						}
					}
					else
					{
						victim.ParticleBlip("&K\t", 10, 0L);
						IComponent<GameObject>.XDidY(victim, "flinch", "out of the way of " + text, "!", null, null, victim);
					}
				}
				// spent: the single-use ruling -- the stone ends every completed flight as dust
				if (GameObject.Validate(ParentObject))
				{
					Helpers.VerifyLog("STRK", "flight complete: stone SPENT (single-use)");
					Actor.PlayWorldSound("sfx_destroy_stone");
					IComponent<GameObject>.EmitMessage(Actor, ParentObject.T() + ParentObject.GetVerb("spend") + " " + ParentObject.itself + " in a last crack of speed."); // DRAFT
					ParentObject.Destroy(null, Silent: true);
				}
			}
			catch (Exception x)
			{
				MetricsManager.LogException("Cleo_TerraFirma_StreakFlight DoFlight", x);
			}
			finally
			{
				if (RelationManager != null && GameObject.Validate(ParentObject))
				{
					ParentObject.RemoveFromContext();
					RelationManager.RestoreContextRelation(ParentObject, ObjectContext, CellContext, BodyPartContext, Relation);
				}
			}
		}
	}
}
