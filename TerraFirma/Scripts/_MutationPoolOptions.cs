using XRL;
using XRL.UI;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>
	/// Per-mutation pool toggles (2026-08-05): each Terra Firma mutation can be removed from
	/// the seeded mutation pool via the options menu. Stamps the engine's own gating pair on
	/// our MutationEntry rows -- Hidden (chargen list; QudMutationsModuleWindow filters on it)
	/// and ExcludeFromPool (every mid-run draw: mutation-point buys, Unstable Genome,
	/// RandomlyMutate, all via Mutations.IncludedInMutatePool) -- the same two flags the base
	/// game's prerelease gating stamps at parse time. The entry itself stays registered in
	/// the factory dictionaries, so existing characters, saves, and blueprint-granted copies
	/// are untouched: the flags gate ACQUISITION only.
	///
	/// Stamped at two engine seams so an option flip lands without an app restart:
	///  - [PreGameCacheInit]: XRLCore startup + XRLCore.NewGame() -> LoadEverything(), which
	///    runs BEFORE CreateNewGame()/chargen -- every new game sees current options.
	///  - [CallAfterGameLoaded]: loading a save, so mid-run buys in a loaded game see an
	///    in-session flip too.
	/// Deliberately NOT [ModSensitiveCacheInit]: that fires inside the cache RESET, where
	/// forcing MutationFactory.CheckInit() could parse XML earlier than the engine intends;
	/// the two seams above already precede every pool consumer.
	/// </summary>
	[HasModSensitiveStaticCache] // required for the [PreGameCacheInit] scan to find this class
	[HasCallAfterGameLoaded]
	public static class MutationPoolOptions
	{
		// { option ID, Mutations.xml entry Name }
		static readonly string[,] Gates =
		{
			{ "Cleo_TerraFirma_PoolGeomancy", "Geomancy" },
			{ "Cleo_TerraFirma_PoolCalcify", "Calcify" },
			{ "Cleo_TerraFirma_PoolEarthenBarrage", "Earthen Barrage" },
			{ "Cleo_TerraFirma_PoolStoneshaper", "Stoneshaper" },
			{ "Cleo_TerraFirma_PoolGorgonsGaze", "Gorgon's Gaze" },
			{ "Cleo_TerraFirma_PoolFissure", "Fissure" },
		};

		[PreGameCacheInit]
		[CallAfterGameLoaded]
		public static void Stamp()
		{
			MutationFactory.CheckInit();
			for (int i = 0; i < Gates.GetLength(0); i++)
			{
				var entry = MutationFactory.GetMutationEntryByName(Gates[i, 1]);
				if (entry == null)
					continue;
				// only an explicit "No" gates; a missing/unset option leaves the mutation live
				bool off = Options.GetOption(Gates[i, 0]).EqualsNoCase("No");
				entry.Hidden = off;
				entry.ExcludeFromPool = off;
				if (off)
					Helpers.VerifyLog("POOL", $"{Gates[i, 1]} gated out of mutation pools ({Gates[i, 0]}=No)");
			}
		}
	}
}
