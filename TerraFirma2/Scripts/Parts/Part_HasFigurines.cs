using System;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;

namespace XRL.World.Parts
{
	/// <summary>
	/// The mason's escort (role-texture pitch 2026-08-23; count RULED same day: 2-4).
	/// HasGuards' decompiled grammar exactly: one placement on the first EnteredCellEvent,
	/// each figurine set walking in the nearest empty adjacent cell and bonded to the mason,
	/// so wherever a mason spawns (warren, dynamic encounter, wish) the swarm arrives with
	/// it. The one difference from HasGuards: the blueprint is pinned rather than rolled
	/// from a dynamic tier table.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_HasFigurinesPart : IBondedLeader
	{
		public string NumberOfFigurines = "2-4";
		public string FigurineBlueprint = "Cleo_TerraFirma_AnimateFigurine";
		public bool FigurinesPlaced;

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && (ID != EnteredCellEvent.ID || FigurinesPlaced))
				return false;
			return true;
		}

		public override bool HandleEvent(EnteredCellEvent E)
		{
			if (!FigurinesPlaced)
			{
				FigurinesPlaced = true;
				Cell cell = ParentObject.CurrentCell;
				int count = NumberOfFigurines.RollCached();
				int placed = 0;
				for (int i = 0; i < count; i++)
				{
					Cell spot = cell.GetFirstEmptyAdjacentCell(1, 2);
					if (spot == null)
						break;
					GameObject figurine = GameObject.Create(FigurineBlueprint);
					figurine.AddPart(new Cleo_TerraFirma_BondedFigurinePart(ParentObject));
					spot.AddObject(figurine).MakeActive();
					placed++;
				}
				Helpers.VerifyLog("FIGSWARM", $"{ParentObject.Blueprint} wakes {placed}/{count} figurine(s)");
			}
			return base.HandleEvent(E);
		}

		public override bool AllowStaticRegistration()
		{
			return true;
		}
	}

	/// <summary>The bond itself: PartyLeader + AllyBond via IBondedCompanion.Initialize.
	/// HiredGuard was not reusable; it hardcodes Faction "Merchants", a "hired guard" title
	/// and the MerchantGuard conversation. This one adds nothing over the base contract.</summary>
	[Serializable]
	public class Cleo_TerraFirma_BondedFigurinePart : IBondedCompanion
	{
		public Cleo_TerraFirma_BondedFigurinePart()
		{
		}

		public Cleo_TerraFirma_BondedFigurinePart(GameObject CompanionOf = null)
			: base(CompanionOf, "Cleo_TerraFirma_Quarriers", null, null, null, false)
		{
		}
	}
}
