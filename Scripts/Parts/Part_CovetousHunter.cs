using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_CovetousHunter : IPart
	{
		public const string MUTATION_PROPERTY = "Cleo_TerraFirma_CovetousMutation";

		public int Rank = 1;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == EnteredCellEvent.ID || ID == BeginTakeActionEvent.ID;
		}

		public override bool HandleEvent(EnteredCellEvent E)
		{
			Apply();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			Apply();
			return base.HandleEvent(E);
		}

		public string Apply()
		{
			GameObject hunter = ParentObject;
			if (hunter == null)
				return null;
			Mutations muts = hunter.RequirePart<Mutations>();
			List<MutationEntry> pool = new List<MutationEntry>(muts.GetMutatePool());
			pool.ShuffleInPlace();
			MutationEntry pick = null;
			foreach (MutationEntry e in pool)
			{
				if (e.Category != null && e.Category.Name == "Mental" && e.Cost > 1 && !hunter.HasPart(e.Class))
				{
					pick = e;
					break;
				}
			}
			if (pick != null)
			{
				Compat.AddMutation(muts, pick, Rank);
				hunter.SetStringProperty(MUTATION_PROPERTY, pick.Class);
			}
			Helpers.VerifyLog("COVETOUS", $"{hunter.Blueprint} ({hunter.DisplayNameOnlyStripped}): extra mutation {pick?.Class ?? "none left"} at rank {Rank}");
			hunter.RemovePart(this);
			return pick?.Class;
		}
	}
}
