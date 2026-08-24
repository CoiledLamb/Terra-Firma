using HarmonyLib;
using System;
using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Capabilities;
using XRL.World.Effects;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Skills.Cooking;

namespace Cleo.TerraFirma.Scripts.Patches
{
	// Harmony patches, kept to the minimum the engine forces on us. The old PerformThrow postfix was
	// replaced by the native AfterThrownEvent relay (see Cleo_TerraFirma_GeomancyStonePart).
	// Patch inventory as of 2026-07-22:
	//   1. Scanning.HasScanningFor postfix (gemstone scan) -- no native hook exists.
	//   2-4. The Lithovore meal-policy patches PARKED on branch quarantine-hold 2026-08-22
	//        with the Lithovore mutation (user ruling); restore from that branch.
	// (The seismophone beep fix was drafted here 2026-08-24 and moved to a native seam the
	//  same sitting on the user's "can we do it without harmony?": ApplyEffectEvent veto via
	//  cross-object RegisterEvent, see Part_Seismophone.cs.)
	[HarmonyPatch]
	class HarmonyPatches
	{
		/// <summary>
		/// Always reveals an object's stats if it has a <see cref="Cleo_TerraFirma_PartGeomancyScan"/> attached to it.
		/// </summary>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Scanning), nameof(Scanning.HasScanningFor))]
		[HarmonyPatch(new Type[] { typeof(GameObject), typeof(GameObject) })]
		static void Scanning_GetScanEpistemicStatus(GameObject who, GameObject obj, ref bool __result)
		{
			if (!__result && obj.HasPart<Cleo_TerraFirma_PartGeomancyScan>())
				__result = true;
		}

	}
}
