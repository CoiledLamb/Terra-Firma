using Cleo.TerraFirma.Scripts;
using System;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	/// <summary>
	/// Used to track that an object is affected by Geomancy's scanning effect.
	/// For creatures, also applies <see cref="Cleo_TerraFirma_EarthlyVibrations"/> as a front-end benefit.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_PartGeomancyScan : IPart
	{
		private const string DEBUG_CONTEXT = "GMS";

		/// <summary>
		/// Decrements every turn. When it reaches 0, this part is removed from its parent object.
		/// This value is constantly refreshed by the Geomancy mutation itself as long as the part's owner stays within range.
		/// </summary>
		public int Duration;

		public Cleo_TerraFirma_PartGeomancyScan()
		{
			Duration = 2;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == SingletonEvent<EndTurnEvent>.ID;
			return true;
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			Duration--;
			Helpers.DebugLog($"earthly vibrations on {ParentObject.DisplayName}: {Duration}", DEBUG_CONTEXT);
			if (Duration <= 0)
			{
				ParentObject.RemovePart(this);
				Helpers.DebugLog($"-> removing…", DEBUG_CONTEXT);
			}
			else
			{
				if (ParentObject.IsCreature)
				{
					Helpers.DebugLog($"-> is creature…", DEBUG_CONTEXT);
					if (ParentObject.TryGetEffect<Cleo_TerraFirma_EarthlyVibrations>(out var vibin))
					{
						vibin.Duration = 2;
						Helpers.DebugLog($"--> maintaining effect", DEBUG_CONTEXT);
					}
					else
					{
						ParentObject.ApplyEffect(new Cleo_TerraFirma_EarthlyVibrations());
						Helpers.DebugLog($"--> adding inner vibrations effect", DEBUG_CONTEXT);
					}
				}
				else
					Helpers.DebugLog($"-> is not creature", DEBUG_CONTEXT);
			}
			return base.HandleEvent(E);
		}
	}
}
