using System;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	/// <summary>
	/// Transient mark attached by the gravel whorl when it deflects a REAL thrown weapon
	/// (swat/ricochet outcomes). The engine's PerformThrow finishes the throw AFTER the whorl
	/// has acted: it re-places the weapon at its own landing cell (overriding the outcome's
	/// placement) and lets FallenRockPart roll throw-fragility (both live-fire finds,
	/// 2026-07-22 verification session). RULED 2026-07-23: a deflected throw is the whorl's
	/// to place and not the thrower's use to break -- restore the outcome's placement and skip
	/// the fragility roll. This part restores placement at AfterThrownEvent and removes itself;
	/// FallenRockPart calls <see cref="Restore"/> first (so its landing-cell logic -- miss
	/// payloads, deferred pulses -- runs from the right cell) and skips fragility while marked.
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

		public override bool WantEvent(int ID, int cascade)
		{
			return ID == AfterThrownEvent.ID || base.WantEvent(ID, cascade);
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
