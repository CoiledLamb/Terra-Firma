using System;
using XRL.Rules;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects
{
	// The mod's first bespoke cooking domains (RULED 2026-07-22, pass-2 worklist round):
	//
	//   Cleo_TerraFirma_Web (gossamarl + its ground form) -- "great pull" ruling:
	//     unit  = Spinnerets grant (the slimeSpitting -> SlimeGlands pattern exactly; Spinnerets
	//             verified a real BaseMutation)
	//     trig  = CookingDomainTongue_OnStuck, borrowed as-is ("whenever you get stuck,")
	//     act   = webs erupt around the eater (below)
	//     Side effect of the move: gossamarl vacates stabilityMinor, which tanglelith inherits --
	//     the oldest open row on the domain table closes at zero extra code.
	//
	//   Cleo_TerraFirma_Spacetime (myriad gyrestone + ground form) -- "worth it" ruling:
	//     units = borrowed whole from the teleport domain (UnitBlink / UnitTeleportOther); the
	//             domain's own identity lives in the action
	//     trig  = CookingDomainTeleport_OnTeleport, borrowed ("whenever you teleport,")
	//     act   = a space-time vortex opens nearby (below)
	//
	// Class shapes mirror decompiled base examples (QuillBurst action, SlimeGlands unit).

	//   Cleo_TerraFirma_ArmorMinor (compressed ash + Ground_CompressedAsh, RULED 2026-07-25) --
	//     aegischist's T1 shadow: base has no weak-armor domain, so the mod builds it.
	//     unit  = +1 AV passive (below); units-only mapping, no trigger/action.

	[Serializable]
	public class Cleo_TerraFirma_CookingDomainArmorMinor_UnitAV : ProceduralCookingEffectUnit
	{
		public override string GetDescription()
		{
			return "+1 AV";
		}

		public override string GetTemplatedDescription()
		{
			return "+1 AV";
		}

		public override void Init(GameObject target)
		{
		}

		public override void Apply(GameObject Object, Effect parent)
		{
			Object.GetStat("AV").Bonus += 1;
		}

		public override void Remove(GameObject Object, Effect parent)
		{
			Object.GetStat("AV").Bonus -= 1;
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_CookingDomainWeb_UnitSpinnerets : ProceduralCookingEffectUnitMutation<Spinnerets>
	{
		public Cleo_TerraFirma_CookingDomainWeb_UnitSpinnerets()
		{
			AddedTier = "1";
			BonusTier = "0";
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_CookingDomainWeb_WebsErupt_ProceduralCookingTriggeredAction : ProceduralCookingTriggeredAction
	{
		public override string GetDescription()
		{
			return "webs erupt around @them.";
		}

		public override string GetTemplatedDescription()
		{
			return "webs erupt around @them.";
		}

		public override string GetNotification()
		{
			return "@they burst with silk!";
		}

		public override void Apply(GameObject go)
		{
			Cell cell = go.Physics?.CurrentCell;
			if (cell == null)
				return;
			foreach (Cell adjacent in cell.GetLocalAdjacentCells())
			{
				if (!adjacent.IsSolid())
					adjacent.AddObject(GameObject.Create("Web"));
			}
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_CookingDomainSpacetime_VortexOpen_ProceduralCookingTriggeredAction : ProceduralCookingTriggeredAction
	{
		public override string GetDescription()
		{
			return "a space-time vortex opens nearby.";
		}

		public override string GetTemplatedDescription()
		{
			return "a space-time vortex opens nearby.";
		}

		public override string GetNotification()
		{
			return "space tears open nearby!";
		}

		public override void Apply(GameObject go)
		{
			Cell cell = go.Physics?.CurrentCell;
			if (cell == null)
				return;
			System.Collections.Generic.List<Cell> open = cell.GetLocalAdjacentCells();
			open.RemoveAll((Cell x) => x.IsSolid());
			if (open.Count > 0)
				open[Stat.Random(0, open.Count - 1)].AddObject(GameObject.Create("Space-Time Vortex"));
		}
	}
}
