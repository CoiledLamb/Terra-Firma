using System;

namespace XRL.World.Parts
{
	/// <summary>
	/// Data-only part responsible for holding stat and mutation offsets applied to Stoneshaper statues that are created from this object.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneshaperWallProperties : IPart
	{
		/// <summary>
		/// Either "stone", "metal", or "other".
		/// </summary>
		public string MaterialType;
		/// <summary>
		/// True geology -- stone the world grew, not a wall anyone built. The redoubt wave
		/// (Fissure) carves only walls with this flag and dresses its ring as them; everything
		/// constructed refuses the wave (corridor ruling 2026-08-02: "the World Engine's bones
		/// don't answer"). Set in Merges.xml on the natural-stone family; defaults false.
		/// </summary>
		public bool Natural;

		/// <summary>The house classifier for the earth mutations' shared stone call (harmonized
		/// RULED 2026-08-04): natural stone answers, everything constructed refuses. Fissure's
		/// carve gate and ring dressing and Calcify's wall-borrowing all route through here.</summary>
		public static bool IsNaturalStone(Cleo_TerraFirma_StoneshaperWallProperties props)
		{
			return props != null && props.MaterialType == "stone" && props.Natural;
		}
		/// <summary>
		/// This is applied to the name of a statue created from this wall, which will cause it to render as something like "shale statue" and etc.
		/// </summary>
		public string MaterialDisplayName;
		/// <summary>
		/// The name of the stat to offset on a derived statue. If non-null, <see cref="StatShift"/> should also be defined.
		/// </summary>
		public string StatName;
		/// <summary>
		/// The amount to offset <see cref="StatName"/> by. Does nothing without a stat name.
		/// </summary>
		public int StatShift;
		/// <summary>
		/// The name of the mutation to apply to a derived statue. If non-null, <see cref="MutationLevel"/> should also be defined.
		/// </summary>
		public string MutationName;
		/// <summary>
		/// The level of the applied mutation defined in <see cref="MutationName"/>. For unleveled mutations, just set this to 1.
		/// </summary>
		public int MutationLevel;
	}
}