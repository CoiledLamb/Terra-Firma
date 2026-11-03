using System;
using System.Collections.Generic;
using System.Linq;
using Cleo.TerraFirma.Scripts;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_SimulacrumLoom : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_CommandSimulacrumLoom";
		public const string STONE_FIGURINE = "Cleo_TerraFirma_Random Stone Figurine";

		public string commandId = "";
		public Guid ActivatedAbilityID = Guid.Empty;

		public int BaseDuration = 200;
		public int Cooldown = 100;
		public string WalkerBlueprint = "Cleo_TerraFirma_AnimateFigurine";
		public int StoneshaperLevel = 1;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public int GetDuration(GameObject Actor) => GetAvailableComputePowerEvent.AdjustUp(Actor, BaseDuration);

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ImplantedEvent.ID
				|| ID == UnimplantedEvent.ID
				|| ID == PooledEvent<CommandEvent>.ID
				|| ID == GetShortDescriptionEvent.ID;
		}

		public override bool HandleEvent(ImplantedEvent E)
		{
			ActivatedAbilityID = E.Implantee.AddDynamicCommand(out commandId, COMMAND, "Weave Simulacrum", "Cybernetics", null, "*");
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnimplantedEvent E)
		{
			E.Implantee.GetPart<Cleo_TerraFirma_SimulacrumLink>()?.Target?.FireEvent("CommandEndDomination");
			E.Implantee.RemoveActivatedAbility(ref ActivatedAbilityID);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			E.Postfix.AppendRules("Compute power on the local lattice increases this implant's effectiveness.");
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command == commandId && !commandId.IsNullOrEmpty() && E.Actor != null && E.Actor == ParentObject.Implantee && E.Actor.IsPlayer())
				Weave(E.Actor);
			return base.HandleEvent(E);
		}

		public static bool IsLoomStone(GameObject o)
		{
			if (o == null || !o.HasTag("Cleo_TerraFirma_IsAnyStone") || o.HasTag("Cleo_TerraFirma_IsGemstone"))
				return false;
			if (o.Blueprint == "Cleo_TerraFirma_Rock_Pebbles" || o.Blueprint == "Cleo_TerraFirma_EphemeralStone")
				return false;
			return !o.GetBlueprint().InheritsFrom("Cleo_TerraFirma_EphemeralStone");
		}

		public static bool IsLoomFigurine(GameObject o) => o != null && o.HasPart<RandomFigurine>() && !o.HasPart<Cleo_TerraFirma_PocketedWalker>();

		public static List<GameObject> GetInputs(GameObject Actor) =>
			Actor.GetInventory(o => IsLoomFigurine(o) || IsLoomStone(o));

		private bool Weave(GameObject Actor)
		{
			if (Actor.OnWorldMap())
				return Actor.Fail("You cannot do that on the world map.");
			if (Actor.HasPart<Cleo_TerraFirma_SimulacrumLink>() || Actor.HasEffect<Dominating>())
				return Actor.Fail("You can't do that.");
			List<GameObject> inputs = GetInputs(Actor);
			if (inputs.Count == 0)
				return Actor.Fail("You carry no figurine or stone to animate.");
			Cell spot = Actor.CurrentCell?.GetFirstEmptyAdjacentCell(1, 1);
			if (spot == null)
				return Actor.Fail("There is no open ground beside you.");
			GameObject pick = Popup.PickGameObject("Choose a figurine or stone to animate.", inputs, AllowEscape: true);
			if (pick == null)
				return false;

			GameObject figurine = IsLoomStone(pick) ? CarveFrom(pick) : TakeFigurine(pick);
			if (figurine == null)
				return false;
			return Inhabit(Actor, figurine, spot);
		}

		private static GameObject CarveFrom(GameObject stone)
		{
			string subject = Cleo_TerraFirma_CarvingPicker.Pick("");
			if (subject == null)
				return null;
			GameObject figurine = GameObject.CreateUnmodified(STONE_FIGURINE, BeforeObjectCreated: o =>
			{
				RandomFigurine rf = o.GetPart<RandomFigurine>();
				if (rf != null)
					rf.Creature = subject;
			});
			if (figurine == null)
				return null;
			foreach (IPart part in stone.PartsList.ToArray())
			{
				if (part is IModification mod && !figurine.HasPart(mod.Name))
					XRL.World.Tinkering.ItemModding.ApplyModification(figurine, mod.Name, mod.Tier);
			}
			stone.RemoveOne().Obliterate();
			return figurine;
		}

		private static GameObject TakeFigurine(GameObject pick)
		{
			GameObject figurine = pick.RemoveOne();
			figurine.RemoveFromContext();
			figurine.GetPart<AddsRep>()?.CheckApplyBonus();
			return figurine;
		}

		public GameObject BuildSimulacrum(GameObject figurine, GameObject Actor)
		{
			GameObject walker = Cleo_TerraFirma_AnimationTablePart.BuildWalker(figurine, Actor, WalkerBlueprint, StoneshaperLevel, Bond: false);
			walker.RemovePart<Corpse>();
			walker.RemovePart<MentalShield>();
			AddsRep rep = figurine.GetPart<AddsRep>();
			if (rep != null && !rep.Faction.IsNullOrEmpty())
			{
				AddsRep mine = new AddsRep(rep.Faction, rep.Value);
				mine.WorksOnSelf = true;
				mine.WorksOnEquipper = false;
				mine.WorksOnCarrier = false;
				walker.AddPart(mine);
			}
			walker.AddPart(new Cleo_TerraFirma_Simulacrum { Figurine = figurine });
			return walker;
		}

		public bool Inhabit(GameObject Actor, GameObject figurine, Cell spot)
		{
			GameObject walker = BuildSimulacrum(figurine, Actor);
			spot.AddObject(walker);
			walker.MakeActive();
			if (!walker.ApplyEffect(new Dominated(Actor, GetDuration(Actor))))
			{
				walker.GetPart<Cleo_TerraFirma_Simulacrum>()?.Stiffen(Silent: true);
				return Actor.Fail("Something prevents you from animating " + figurine.t() + ".");
			}
			Actor.AddPart(new Cleo_TerraFirma_SimulacrumLink { Target = walker, AbilityID = ActivatedAbilityID, Cooldown = Cooldown });
			IComponent<GameObject>.AddPlayerMessage("You imbue " + figurine.t() + " with life.");
			walker.SmallTeleportSwirl(null, "&M");
			Actor.UseEnergy(1000, "Cybernetics Simulacrum Loom");
			Actor.CooldownActivatedAbility(ActivatedAbilityID, Cooldown);
			The.Game.Player.Body = walker;
			IComponent<GameObject>.ThePlayer.Target = null;
			walker.GetPart<AddsRep>()?.CheckApplyBonus();
			Helpers.VerifyLog("LOOM", $"inhabit {figurine.Blueprint} ({figurine.GetPart<RandomFigurine>()?.Creature}) as {walker.DisplayNameOnlyStripped}, {walker.GetEffect<Dominated>()?.Duration} rounds");
			return true;
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_SimulacrumLink : IPart
	{
		public GameObject Target;
		public Guid AbilityID = Guid.Empty;
		public int Cooldown = 100;

		[NonSerialized] public static bool Quiet;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == TookDamageEvent.ID;
		}

		public override bool HandleEvent(TookDamageEvent E)
		{
			if (GameObject.Validate(ref Target))
				Target.FireEvent("InterruptDomination");
			return base.HandleEvent(E);
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("DominationBroken");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "DominationBroken")
				Break();
			return base.FireEvent(E);
		}

		public void Break()
		{
			GameObject walker = Target;
			Target = null;
			if (walker != null && walker.OnWorldMap())
				walker.PullDown();
			The.Game.Player.Body = ParentObject;
			IComponent<GameObject>.ThePlayer.Target = null;
			Dominating dominating = ParentObject.GetEffect<Dominating>();
			if (dominating != null)
				ParentObject.RemoveEffect(dominating);
			if (ParentObject.IsPlayer())
			{
				if (!Quiet)
					Popup.Show("{{r|Your domination is broken!}}");
				Domination.CheckMetempsychosis(ParentObject);
			}
			ParentObject.SmallTeleportSwirl(null, "&M");
			ParentObject.Brain?.Goals.Clear();
			ParentObject.CooldownActivatedAbility(AbilityID, Cooldown);
			if (walker != null)
				walker.GetPart<Cleo_TerraFirma_Simulacrum>()?.End();
			ParentObject.RemovePart(this);
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_Simulacrum : IPart
	{
		public GameObject Figurine;
		public bool Dying;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("BeforeDie");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "BeforeDie")
				Dying = true;
			return base.FireEvent(E);
		}

		public void End()
		{
			ParentObject.GetPart<AddsRep>()?.CheckApplyBonus();
			if (Dying || ParentObject.IsDying || ParentObject.hitpoints <= 0)
			{
				Helpers.VerifyLog("LOOM", $"{ParentObject.DisplayNameOnlyStripped} destroyed, figurine gone");
				Figurine?.Obliterate();
				Figurine = null;
				return;
			}
			Stiffen();
		}

		public void Stiffen(bool Silent = false)
		{
			Cell cell = ParentObject.CurrentCell;
			GameObject fig = Figurine;
			Figurine = null;
			if (fig != null && cell != null)
			{
				cell.AddObject(fig);
				if (!Silent && cell.IsVisible())
					IComponent<GameObject>.AddPlayerMessage(ParentObject.Does("stiffen") + " back into " + fig.an() + ".");
				Helpers.VerifyLog("LOOM", $"stiffened back into {fig.Blueprint} at {cell.X},{cell.Y}");
			}
			ParentObject.Obliterate();
		}
	}
}
