using System;
using XRL.World.Parts;

namespace XRL.World.Effects
{
	/// <summary>
	/// Mudskin girdle hardening (08-22 shape: per bleed tick, capped, flakes off one step per
	/// interval, shown as a status for legibility). Name "scabbed" user-picked 2026-09-29.
	/// Applied and fed by <see cref="Cleo_TerraFirma_MudskinGirdle"/>; the AV rides this
	/// effect's StatShifter (PartiallyPetrified idiom), so removal is one call.
	/// Messages on the edges only, Carapace precedent (tighten/loosen through EmitMessage, which
	/// shows for visible wearers): one when the first stack sets, one when the last flakes off.
	/// Middle steps stay silent; each rides a bleed tick that already prints. Lines approved
	/// 2026-09-29; the NPC flake variant names the wearer (CLAUDE PROPOSAL).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_Scabbed : Effect
	{
		public int Stacks;
		public int Cap = 2;
		public int FlakeInterval = 20;
		public int Countdown;

		public Cleo_TerraFirma_Scabbed()
		{
			Duration = 1;
			DisplayName = "{{w|scabbed}}";
		}

		public override string GetDetails()
		{
			return "+" + Stacks + " AV.";
		}

		/// <summary>One bleed tick: one step harder, up to the cap, and the flake clock restarts.</summary>
		public void Harden()
		{
			if (Stacks == 0)
				IComponent<GameObject>.EmitMessage(Object, Object.Poss("girdle") + " hardens.");
			if (Stacks < Cap)
				Stacks++;
			Countdown = FlakeInterval;
			Sync();
		}

		private void Sync()
		{
			StatShifter.SetStatShift("AV", Stacks);
		}

		public override void Remove(GameObject Object)
		{
			StatShifter.RemoveStatShifts();
			base.Remove(Object);
		}

		public override bool WantEvent(int ID, int Cascade)
		{
			if (!base.WantEvent(ID, Cascade))
				return ID == SingletonEvent<EndTurnEvent>.ID;
			return true;
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (!WearingGirdle())
			{
				Object.RemoveEffect(this);
				return base.HandleEvent(E);
			}
			if (--Countdown <= 0)
			{
				Stacks--;
				Countdown = FlakeInterval;
				if (Stacks <= 0)
				{
					IComponent<GameObject>.EmitMessage(Object, Object.IsPlayer()
						? "The last of the mud flakes off."
						: "The last of the mud flakes off " + Object.poss("girdle") + ".");
					Object.RemoveEffect(this);
					return base.HandleEvent(E);
				}
			}
			Sync();
			return base.HandleEvent(E);
		}

		/// <summary>Belt and braces beside the girdle's own unequip removal: a girdle lost by
		/// any path the unequip event misses still takes its hardening with it.</summary>
		private bool WearingGirdle()
		{
			foreach (GameObject item in Object.GetEquippedObjectsReadonly())
			{
				if (item.HasPart<Cleo_TerraFirma_MudskinGirdle>() && item.IsEquippedProperly())
					return true;
			}
			return false;
		}
	}
}
