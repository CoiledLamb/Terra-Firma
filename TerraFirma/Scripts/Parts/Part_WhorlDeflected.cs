using System;
using Cleo.TerraFirma.Scripts;
using UnityEngine;

namespace XRL.World.Parts
{
	/// <summary>
	/// Transient mark attached by the gravel whorl when it deflects a REAL thrown weapon
	/// (swat/ricochet outcomes). The engine's PerformThrow finishes the throw AFTER the whorl
	/// has acted: it re-places the weapon at its own landing cell (overriding the outcome's
	/// placement) and lets FallenRockPart roll throw-fragility (both live-fire finds,
	/// 2026-07-22 verification session). RULED 2026-07-23: a deflected throw is the whorl's
	/// to place and not the thrower's use to break -- restore the outcome's placement and skip
	/// the fragility roll. This part restores placement at AfterThrownEvent and removes itself.
	/// (FallenRockPart, which also called <see cref="Restore"/>, moved to quarantine/ 2026-09-30.)
	/// Added at runtime, so it always sits last in the part list -- self-removal mid-dispatch
	/// can't skip another part's handler.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_WhorlDeflectedPart : IPart
	{
		/// <summary>Where the whorl outcome put the weapon. Never persists: mark and resolution
		/// happen inside one throw action, so a save can never land between them.</summary>
		[NonSerialized]
		public Cell Intended;

		/// <summary>The whorl's cell -- where the deflection visually happens. When set, the
		/// engine's thrown-flight juice is re-pathed to END here (the stone flies to the whorl,
		/// not through it) and its landing dust is stripped (user 2026-08-14: "no smoke on whorl
		/// swat for rocks"; the poof was the ThrowVFX config's ParticleRate cluster lingering at
		/// the engine's endpoint, and our override wins the config dict because
		/// ConfigureMissileVisualEffectEvent fires after all defaults load).</summary>
		[NonSerialized]
		public Cell Bounce;

		/// <summary>Ricochet outcomes also get the bounce-then-thrown-leg presentation
		/// (RULED 2026-08-14) chained after the re-pathed leg 1.</summary>
		[NonSerialized]
		public bool Ricochet;

		public override bool WantEvent(int ID, int cascade)
		{
			return ID == AfterThrownEvent.ID
				|| ID == PooledEvent<ConfigureMissileVisualEffectEvent>.ID
				|| base.WantEvent(ID, cascade);
		}

		public override bool HandleEvent(ConfigureMissileVisualEffectEvent E)
		{
			if (E.Projectile == ParentObject && E.Path != null && Bounce != null)
			{
				E.Path.SetParameter("ParticleRate", "0"); // no dust cluster at any endpoint
				if (E.Path.first != null && GameManager.Instance != null)
				{
					// leg 1 ends at the whorl: compute its flight time the way the renderer
					// will (distance / Speed), then hand leg 2 the baton at that moment
					float speed = 350f;
					if (E.Path.TryGetValue("Speed", out string sp))
						float.TryParse(sp, out speed);
					if (speed <= 0f)
						speed = 350f;
					Vector3 a = GameManager.Instance.getTileCenter(E.Path.first.X, E.Path.first.Y);
					Vector3 b = GameManager.Instance.getTileCenter(Bounce.X, Bounce.Y);
					float leg1Sec = Vector3.Distance(a, b) / speed;
					E.Path.last = Bounce.Location;
					if (Ricochet && Intended != null && Intended != Bounce)
						Cleo_TerraFirma_GravelWhorlFX.PlayRicochetFlight(ParentObject, Bounce, Intended, leg1Sec);
				}
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(AfterThrownEvent E)
		{
			Restore();
			return base.HandleEvent(E);
		}

		/// <summary>Re-places the weapon at the whorl outcome's cell if the engine moved it, then removes the mark.</summary>
		public void Restore()
		{
			GameObject o = ParentObject;
			if (o == null)
				return;
			if (Intended != null && GameObject.Validate(o) && o.CurrentCell != null && o.CurrentCell != Intended)
			{
				Helpers.VerifyLog("GVW", $"deflect mark: engine re-placed {o.Blueprint} at ({o.CurrentCell.X},{o.CurrentCell.Y}), restoring whorl placement ({Intended.X},{Intended.Y})");
				o.DirectMoveTo(Intended, 0, Forced: true, IgnoreCombat: true);
			}
			Intended = null;
			o.RemovePart(this);
		}
	}
}
