using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.AI;
using XRL.World.Capabilities;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_PocketableWalker : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_PocketWalker";

		public override bool SameAs(IPart p) => true;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID;
		}

		public bool CanOffer(GameObject Actor)
		{
			return Actor != null && Actor.IsPlayer() && Actor != ParentObject && !ParentObject.IsPlayer()
				&& !ParentObject.HasPart<Cleo_TerraFirma_Simulacrum>()
				&& ParentObject.GetPart<Cleo_TerraFirma_AnimateFigurineDepiction>()?.Figurine != null;
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (CanOffer(E.Actor))
				E.AddAction("Pick up", "pick up", COMMAND, null, 'g', FireOnActor: false, Default: 0);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == COMMAND && CanOffer(E.Actor) && Pocket(E.Actor) != null)
				E.RequestInterfaceExit();
			return base.HandleEvent(E);
		}

		private bool Coax(GameObject Actor, bool? Roll)
		{
			bool won = false;
			if (Roll.HasValue)
				won = Roll.Value;
			else
			{
				int levels = Math.Max(ParentObject.Stat("Level") - Actor.Stat("Level"), 0);
				Mental.PerformAttack(E => { won = E.Penetrations > 0; return won; }, Actor, ParentObject, Actor, "Cleo_TerraFirma_PocketWalker", "1d8-6", 2, int.MinValue, int.MinValue, Actor.StatMod("Ego"), levels);
			}
			if (!won && Actor.IsPlayer())
				Popup.ShowFail(ParentObject.Does("are", int.MaxValue, null, null, null) + " unconvinced by your pleas.");
			return won;
		}

		public GameObject Pocket(GameObject Actor, bool? Roll = null)
		{
			GameObject walker = ParentObject;
			Cleo_TerraFirma_AnimateFigurineDepiction depiction = walker.GetPart<Cleo_TerraFirma_AnimateFigurineDepiction>();
			if (depiction?.Figurine == null || GameObjectFactory.Factory.GetBlueprintIfExists(depiction.Figurine) == null)
				return null;
			if (Actor.DistanceTo(walker) > 1)
			{
				if (Actor.IsPlayer())
					Popup.ShowFail("You can't reach " + walker.t() + ".");
				return null;
			}
			if (walker.IsHostileTowards(Actor))
			{
				if (Actor.IsPlayer())
					Popup.ShowFail(walker.T() + " won't hold still.");
				return null;
			}
			bool yours = walker.PartyLeader == Actor;
			if (!yours && !Coax(Actor, Roll))
			{
				walker.AddOpinion<OpinionAttack>(Actor, null);
				walker.Brain?.WantToKill(Actor, "because you tried to take me");
				Actor.UseEnergy(1000, "Item PocketWalker");
				return null;
			}

			GameObject figurine = depiction.MakeFigurine();
			if (figurine == null)
				return null;
			Statistic hp = walker.GetStat("Hitpoints"), av = walker.GetStat("AV");
			figurine.AddPart(new Cleo_TerraFirma_PocketedWalker
			{
				WalkerBlueprint = walker.Blueprint,
				Name = walker.Render.DisplayName,
				Description = walker.GetPart<Description>()?._Short,
				HP = hp?.BaseValue ?? 0,
				Damage = hp?.Penalty ?? 0,
				AV = av?.BaseValue ?? 0,
			});

			List<GameObject> gear = new List<GameObject>();
			foreach (GameObject o in walker.GetEquippedObjects())
				if (!o.IsNatural())
					gear.Add(o);
			if (walker.Inventory != null)
				foreach (GameObject o in walker.Inventory.GetObjects())
					if (!o.IsNatural() && !gear.Contains(o))
						gear.Add(o);
			foreach (GameObject o in gear)
			{
				o.ForceUnequipAndRemove(Silent: true);
				Actor.ReceiveObject(o);
			}

			string was = walker.t();
			if (walker.Brain != null)
				walker.Brain.PartyLeader = null;
			walker.Obliterate(Silent: true);
			Actor.ReceiveObject(figurine);
			if (Actor.IsPlayer())
				IComponent<GameObject>.AddPlayerMessage("You pick up " + was + ". " + figurine.Does("go", int.MaxValue, null, null, null) + " still.");
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("FIGPOCKET", $"pocketed {walker.Blueprint} as {figurine.Blueprint} ({depiction.Creature}), yours {yours}, gear {gear.Count}");
			Actor.UseEnergy(1000, "Item PocketWalker");
			return figurine;
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_PocketedWalker : IPart
	{
		public string WalkerBlueprint;
		public string Name;
		public string Description;
		public int HP;
		public int Damage;
		public int AV;

		public const string WAKING = "Cleo_TerraFirma_PocketedWalkerWaking";

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == DroppedEvent.ID
				|| ID == TakenEvent.ID;
		}

		public override bool HandleEvent(DroppedEvent E)
		{
			if (E.Actor != null && E.Actor.IsPlayer())
				ParentObject.SetIntProperty(WAKING, 1);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(TakenEvent E)
		{
			ParentObject.RemoveIntProperty(WAKING);
			return base.HandleEvent(E);
		}

		public override bool WantTurnTick() => true;

		public override void TurnTick(long TimeTick, int Amount)
		{
			if (ParentObject.GetIntProperty(WAKING) == 0)
				return;
			ParentObject.RemoveIntProperty(WAKING);
			Wake(The.Player);
		}

		public GameObject Wake(GameObject Actor)
		{
			Cell here = ParentObject.CurrentCell;
			if (Actor == null || here == null)
				return null;
			List<Cell> open = new List<Cell>();
			foreach (Cell c in here.GetLocalAdjacentCells())
				if (c.IsEmptyFor(Actor))
					open.Add(c);
			return open.Count == 0 ? null : Release(Actor, open.GetRandomElement());
		}

		public GameObject Release(GameObject Actor, Cell Cell)
		{
			if (Cell == null || !Cell.IsEmptyFor(Actor) || GameObjectFactory.Factory.GetBlueprintIfExists(WalkerBlueprint) == null)
				return null;
			GameObject figurine = ParentObject.RemoveOne();
			GameObject walker = Cleo_TerraFirma_AnimationTablePart.BuildWalker(figurine, Actor, WalkerBlueprint, 1);
			Statistic hp = walker.GetStat("Hitpoints"), av = walker.GetStat("AV");
			if (hp != null)
			{
				hp.BaseValue = HP;
				hp.Penalty = Math.Min(Damage, HP - 1);
			}
			if (av != null)
				av.BaseValue = AV;
			if (!Name.IsNullOrEmpty())
				walker.Render.DisplayName = Name;
			Description desc = walker.GetPart<Description>();
			if (desc != null && Description != null)
				desc._Short = Description;
			Cell.AddObject(walker).MakeActive();
			if (Actor.IsPlayer())
				IComponent<GameObject>.AddPlayerMessage(figurine.T() + " stirs.");
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("FIGPOCKET", $"released {figurine.Blueprint} as {walker.DisplayNameOnlyStripped}, HP {HP - Damage}/{HP}");
			figurine.Obliterate(Silent: true);
			return walker;
		}
	}
}
