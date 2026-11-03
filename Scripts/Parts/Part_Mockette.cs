using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using Qud.API;
using XRL.Rules;
using XRL.World.Anatomy;
using XRL.World.Capabilities;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_Mockette : IPart
	{
		public string FormTable = "BaroqueRuinsZoneGlobals-Creatures";
		public int AnyCreatureChance = 25;
		public string OwnTile = "mockette.png";
		public string OwnRenderString = "M";
		public string OwnDetailColor = "m";
		public string DisguiseDetailColor = "Y";
		public string EyeBlueprint = "Cleo_TerraFirma_MocketteEye";
		public string HandBlueprint = "Cleo_TerraFirma_MocketteHand";
		public int ConfusionLevel = 10;
		public int BuildupTimeout = 15;
		public int BuildupPerLook = 3;

		private const string LOOK_TURN_PROPERTY = "Cleo_TerraFirma_MocketteLookTurn";
		private const string LOOK_COUNT_PROPERTY = "Cleo_TerraFirma_MocketteLookCount";

		[NonSerialized]
		private static Dictionary<string, List<string>> RegionForms = new Dictionary<string, List<string>>();

		[NonSerialized]
		private static List<string> AnyForms;

		public override bool SameAs(IPart p) => false;

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("AfterLookedAt");
			base.Register(Object, Registrar);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != AfterObjectCreatedEvent.ID)
				return ID == SingletonEvent<EndTurnEvent>.ID;
			return true;
		}

		public override bool HandleEvent(AfterObjectCreatedEvent E)
		{
			EquipNaturalGear();
			ShowOwnShape();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			Shift();
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "AfterLookedAt")
			{
				GameObject looker = E.GetGameObjectParameter("Looker");
				if (looker == ParentObject)
					SelfLovesick();
				ConfuseLooker(looker);
			}
			return base.FireEvent(E);
		}

		private void EquipNaturalGear()
		{
			Body body = ParentObject.Body;
			if (body == null)
				return;
			foreach (BodyPart part in body.GetParts())
			{
				if (part.Equipped != null)
					continue;
				string blueprint = part.VariantType == "Lip" ? EyeBlueprint
					: part.VariantType == "Small Crack" ? HandBlueprint
					: null;
				if (blueprint == null)
					continue;
				GameObject item = GameObject.Create(blueprint);
				if (!ParentObject.ForceEquipObject(item, part, Silent: true, EnergyCost: 0))
				{
					Helpers.VerifyLog("MOCKETTE", $"could not equip {blueprint} on {part.VariantType ?? part.Type}");
					item.Obliterate();
				}
			}
			Cleo_TerraFirma_GorgonsGaze gaze = ParentObject.GetPart<Cleo_TerraFirma_GorgonsGaze>();
			Helpers.VerifyLog("MOCKETTE", $"natural gear equipped; Gorgon's Gaze level {(gaze == null ? "none" : gaze.Level.ToString())}");
		}

		private void Shift()
		{
			if (ParentObject.CurrentZone == null)
				return;
			GameObject victim = FindVictim();
			if (victim != null)
			{
				Render look = victim.Render;
				if (look != null && !look.Tile.IsNullOrEmpty())
				{
					SetShape(look.Tile, look.RenderString, DisguiseDetailColor, victim.HasPart<Lovely>());
					return;
				}
			}
			string blueprint = null;
			if (Stat.Random(1, 100) > AnyCreatureChance)
				blueprint = GetZoneForms().GetRandomElement();
			if (blueprint == null)
				blueprint = GetAnyForms().GetRandomElement();
			if (blueprint == null || !GameObjectFactory.Factory.Blueprints.TryGetValue(blueprint, out GameObjectBlueprint model))
			{
				ShowOwnShape();
				return;
			}
			SetShape(model.GetPartParameter<string>("Render", "Tile"), model.GetPartParameter<string>("Render", "RenderString"), DisguiseDetailColor, model.HasPart("Lovely"));
		}

		private GameObject FindVictim()
		{
			GameObject best = null;
			int bestCount = -1;
			foreach (GameObject obj in ParentObject.CurrentZone.GetObjects())
			{
				Cleo_TerraFirma_Petrifying effect = obj.GetEffect<Cleo_TerraFirma_Petrifying>();
				if (effect == null || effect.Initiator != ParentObject || obj.IsInGraveyard())
					continue;
				int count = Cleo_TerraFirma_GorgonsGaze.GetStoneCount(obj);
				if (count > bestCount || (count == bestCount && obj.IsPlayer()))
				{
					best = obj;
					bestCount = count;
				}
			}
			return best;
		}

		private void ShowOwnShape()
		{
			SetShape(OwnTile, OwnRenderString, OwnDetailColor, false);
		}

		private void SetShape(string Tile, string RenderString, string DetailColor, bool Lovely)
		{
			if (Tile.IsNullOrEmpty())
				return;
			Render render = ParentObject.Render;
			render.Tile = Tile;
			if (!RenderString.IsNullOrEmpty())
				render.RenderString = RenderString;
			render.ColorString = "&y";
			render.TileColor = "&y";
			render.DetailColor = DetailColor;
			if (Lovely)
				ParentObject.RequirePart<Lovely>();
			else
				ParentObject.RemovePart<Lovely>();
		}

		private List<string> GetZoneForms()
		{
			string region = ParentObject.CurrentZone?.GetTerrainRegion();
			string key = region.IsNullOrEmpty() ? FormTable : region;
			if (RegionForms.TryGetValue(key, out List<string> forms))
				return forms;
			forms = new List<string>();
			if (!region.IsNullOrEmpty())
			{
				AddTableForms(region + "ZoneGlobals-Creatures", forms);
				AddTableForms("DynamicObjectsTable:" + region + "_Creatures", forms);
			}
			if (forms.Count == 0)
				AddTableForms(FormTable, forms);
			Helpers.VerifyLog("MOCKETTE", $"form pool for region '{region}': {forms.Count} creatures ({string.Join(", ", forms)})");
			RegionForms[key] = forms;
			return forms;
		}

		private void AddTableForms(string Table, List<string> Forms)
		{
			if (Table.IsNullOrEmpty() || !PopulationManager.TryResolvePopulation(Table, out PopulationInfo _))
				return;
			foreach (string name in PopulationManager.GetEach(Table))
			{
				if (name != ParentObject.Blueprint && !Forms.Contains(name)
					&& GameObjectFactory.Factory.Blueprints.TryGetValue(name, out GameObjectBlueprint model) && IsForm(model))
					Forms.Add(name);
			}
		}

		private List<string> GetAnyForms()
		{
			if (AnyForms != null)
				return AnyForms;
			AnyForms = new List<string>();
			foreach (GameObjectBlueprint model in GameObjectFactory.Factory.BlueprintList)
			{
				if (model.Name != ParentObject.Blueprint && EncountersAPI.IsEligibleForDynamicEncounters(model)
					&& EncountersAPI.IsLegendaryEligible(model) && IsForm(model))
					AnyForms.Add(model.Name);
			}
			Helpers.VerifyLog("MOCKETTE", $"any-creature pool: {AnyForms.Count} creatures");
			return AnyForms;
		}

		private static bool IsForm(GameObjectBlueprint Model)
		{
			return !Model.IsBaseBlueprint() && Model.HasPart("Brain") && Model.HasPart("Body")
				&& !Model.GetPartParameter<string>("Render", "Tile").IsNullOrEmpty();
		}

		private void ConfuseLooker(GameObject Looker)
		{
			ConfuseLooker(ParentObject, Looker, ConfusionLevel, BuildupTimeout, BuildupPerLook);
		}

		public static bool ConfuseLooker(GameObject Source, GameObject Looker, int ConfusionLevel, int BuildupTimeout, int BuildupPerLook)
		{
			if (Looker == null || Looker.Brain == null || Looker.HasEffect<Confused>())
				return false;
			long turn = The.Game.Turns;
			int looks = turn - Looker.GetLongProperty(LOOK_TURN_PROPERTY) > BuildupTimeout ? 0 : Looker.GetIntProperty(LOOK_COUNT_PROPERTY);
			int level = Confusion.GetConfusionLevel(ConfusionLevel);
			int penalty = Confusion.GetMentalPenalty(ConfusionLevel);
			int attack = ConfusionLevel + looks * BuildupPerLook;
			bool confused = Mental.PerformAttack((MentalAttackEvent MAE) => Confusion.Confuse(MAE, Attack: false, level, penalty, Silent: true),
				Source, Looker, null, "Confuse Mockette Look", "1d8", 8, Confusion.GetDuration(ConfusionLevel).RollCached(), int.MinValue, attack);
			Helpers.VerifyLog("MOCKETTE", $"{Source.Blueprint} looked at by {Looker.Blueprint}: 1d8+{attack} (look {looks + 1} in a row) -> {(confused ? "CONFUSED" : "resisted")}");
			Looker.SetLongProperty(LOOK_TURN_PROPERTY, turn);
			Looker.SetIntProperty(LOOK_COUNT_PROPERTY, looks + 1);
			return confused;
		}

		private void SelfLovesick()
		{
			if (!ParentObject.HasPart<Lovely>() || ParentObject.HasEffect<Lovesick>() || ParentObject.Physics == null)
				return;
			bool organic = ParentObject.Physics.Organic;
			ParentObject.Physics.Organic = true;
			bool applied = ParentObject.ApplyEffect(new Lovesick(Stat.Random(3000, 3600), ParentObject));
			ParentObject.Physics.Organic = organic;
			Helpers.VerifyLog("MOCKETTE", $"looked at itself while lovely -> lovesick {(applied ? "applied" : "refused")}");
		}
	}
}
