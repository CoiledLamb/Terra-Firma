using System;
using System.Collections.Generic;
using XRL.UI;

namespace XRL.World.Parts
{
	// Codex PROPOSAL: burst feeding; passive supply and storage are native XML parts.
	// Base FusionReactor.ProduceCharge / IPowerTransmission supply the grid.
	// Capacitor.AddCharge stores the burst (base furniture's store, 2026-10-13; a mill saved
	// before then still carries an EnergyCell and is read the same way); the existing grinder
	// supplies the diet idiom.
	[Serializable]
	public class Cleo_TerraFirma_PiezoelectricMill : IPoweredPart
	{
		public int ChargePerPound = 50;
		public int FeedEnergy = 1000;
		/// <summary>Played once per stone fed (not per trickle turn): base rock crumble, four variants.
		/// XML-settable dials; [NonSerialized] so the shipped part keeps its saved layout.</summary>
		[NonSerialized] public string GrindSound = "Sounds/Misc/sfx_rock_crumble_short";
		[NonSerialized] public float GrindSoundVolume = 0.5f;
		private const string FeedCommand = "Cleo_TerraFirma_FeedPiezoelectricMill";

		public Cleo_TerraFirma_PiezoelectricMill()
		{
			ChargeUse = 0;
			WorksOnSelf = true;
			IsBootSensitive = false;
		}

		public override bool SameAs(IPart other)
		{
			var mill = other as Cleo_TerraFirma_PiezoelectricMill;
			return mill != null && mill.ChargePerPound == ChargePerPound
				&& mill.FeedEnergy == FeedEnergy && base.SameAs(other);
		}

		public override bool WantEvent(int ID, int cascade) => base.WantEvent(ID, cascade)
			|| ID == GetInventoryActionsEvent.ID || ID == InventoryActionEvent.ID
			|| ID == AutoexploreObjectEvent.ID;

		public override bool HandleEvent(AutoexploreObjectEvent E) => Cleo.TerraFirma.Scripts.FurnitureCellGuard.Block(E, ParentObject);

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			E.AddAction("Feed", "feed a stone", FeedCommand, null, 'f', FireOnActor: false);
			return base.HandleEvent(E);
		}

		private bool IsFood(GameObject stone) => stone != null && stone != ParentObject
			&& stone.HasTagOrProperty(Cleo_TerraFirma_GrinderCell.STONE_TAG)
			&& !stone.HasPart<Temporary>() && !stone.IsCreature
			&& string.IsNullOrEmpty(stone.Physics?.Owner);

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command != FeedCommand || E.Actor == null) return base.HandleEvent(E);
			var actorCell = E.Actor.CurrentCell;
			var millCell = ParentObject.CurrentCell;
			if (actorCell == null || millCell == null || actorCell.ParentZone != millCell.ParentZone
				|| Math.Abs(actorCell.X - millCell.X) > 1 || Math.Abs(actorCell.Y - millCell.Y) > 1) return false;
			if (!E.Actor.CanMoveExtremities(null, ShowMessage: true, Involuntary: false, AllowTelekinetic: true)) return false; // frozen: base hand-action gate
			var cap = ParentObject.GetPart<Capacitor>();
			var oldCell = cap == null ? ParentObject.GetPart<EnergyCell>() : null;
			int Charge() => cap?.Charge ?? oldCell.Charge;
			int MaxCharge() => cap?.MaxCharge ?? oldCell.MaxCharge;
			if ((cap == null && oldCell == null) || !IsReady(UseCharge: false, IgnoreCharge: true))
			{
				if (E.Actor.IsPlayer()) Popup.ShowFail(ParentObject.Does("are") + " not running.");
				return false;
			}
			if (Charge() >= MaxCharge() || ChargePerPound <= 0)
			{
				if (E.Actor.IsPlayer()) Popup.ShowFail(ParentObject.T() + " cannot store any more charge.");
				return false;
			}
			var stones = E.Actor.GetInventoryDirect(IsFood);
			int carried = stones.Count;
			var origin = E.Actor.CurrentCell;
			if (origin != null)
			{
				// Copy: the engine returns its cached adjacency list by reference.
				var reach = new List<Cell>(origin.GetLocalAdjacentCells());
				reach.Add(origin);
				foreach (var c in reach)
					foreach (var stone in c.Objects)
						if (IsFood(stone) && !stones.Contains(stone)) stones.Add(stone);
			}
			if (stones.Count == 0)
			{
				if (E.Actor.IsPlayer()) Popup.ShowFail("There is no stone in reach to feed it.");
				return false;
			}
			int choice = 0;
			if (E.Actor.IsPlayer())
			{
				var options = new List<string>();
				for (int i = 0; i < stones.Count; i++)
					options.Add(stones[i].DisplayName + " {{K|(" + (stones[i].Physics?.Weight ?? 0) + "#)}}"
						+ (i >= carried ? " {{K|(nearby)}}" : ""));
				choice = Popup.PickOption(Intro: "Feed which stone?", Options: options.ToArray(), AllowEscape: true);
				if (choice < 0) return false;
			}
			var pick = stones[choice];
			int burst = (int)Math.Min(int.MaxValue, (long)Math.Max(1, pick.Physics?.Weight ?? 0) * ChargePerPound);
			int accepted = Math.Min(burst, MaxCharge() - Charge());
			// A burst has no queue: make overflow explicit before consuming a whole stone.
			if (accepted < burst && E.Actor.IsPlayer()
				&& Popup.ShowYesNo("Only " + accepted + " of " + burst + " charge will fit. Feed the stone anyway?") != DialogResult.Yes) return false;
			pick = pick.SplitFromStack() ?? pick;
			string name = pick.t();
			if (!pick.Destroy()) return false;
			if (cap != null) cap.AddCharge(accepted); else oldCell.AddCharge(accepted);
			if (E.Actor.IsPlayer()) IComponent<GameObject>.AddPlayerMessage("You feed " + name + " into " + ParentObject.t() + ".");
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("MILL", $"burst={burst}, stored={accepted}, charge={Charge()}/{MaxCharge()}");
			if (!GrindSound.IsNullOrEmpty())
				ParentObject.PlayWorldSound(GrindSound, GrindSoundVolume);
			E.Actor.UseEnergy(FeedEnergy, "Item FeedPiezoelectricMill");
			E.RequestInterfaceExit();
			return base.HandleEvent(E);
		}
	}
}
