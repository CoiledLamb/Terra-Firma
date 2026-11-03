using System;
using Cleo.TerraFirma.Scripts;
using Qud.API;
using XRL.Language;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_Grotesque : IPart
	{
		public bool Dormant = true;
		public int WakeRadius = 3;

		public string Material = "stone";
		public string StatueColor = "&y";
		public string StatueDetail = "y";

		public string DisguiseBlueprint;
		public bool DisguiseAddedLovely;

		public string AwakeColor = "&y";
		public string AwakeDetail = "r";

		public string AwakeTile;
		public string AwakeRenderString;
		public string AwakeName;
		public string AwakeDescription;

		public string WokeBy;

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == AfterObjectCreatedEvent.ID
				|| ID == SingletonEvent<BeginTakeActionEvent>.ID
				|| ID == BeforeTookDamageEvent.ID
				|| ID == TookDamageEvent.ID
				|| ID == PooledEvent<GetHostilityRecognitionLimitsEvent>.ID;
		}

		public override bool HandleEvent(AfterObjectCreatedEvent E)
		{
			if (Dormant)
				DressAsStatue();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			if (!Dormant || ParentObject.IsPlayer())
				return base.HandleEvent(E);
			GameObject waker = FindWaker();
			if (waker != null)
				Wake("saw " + waker.Blueprint + " at " + ParentObject.DistanceTo(waker));
			E.PreventAction = true;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeTookDamageEvent E)
		{
			Helpers.VerifyLog("GROTESQUE", $"{ParentObject.ID} BeforeTookDamage {E.Damage?.Amount}, dormant {Dormant}");
			if (Dormant)
				Wake("before took damage from " + (E.Actor?.Blueprint ?? "nothing"));
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(TookDamageEvent E)
		{
			Helpers.VerifyLog("GROTESQUE", $"{ParentObject.ID} TookDamage {E.Damage?.Amount}, dormant {Dormant}");
			if (Dormant)
				Wake("took damage from " + (E.Actor?.Blueprint ?? "nothing"));
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetHostilityRecognitionLimitsEvent E)
		{
			if (Dormant)
				E.MaxDistance(0);
			return base.HandleEvent(E);
		}

		private GameObject FindWaker()
		{
			Cell origin = ParentObject.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (zone == null || ParentObject.Brain == null)
				return null;
			GameObject best = null;
			int bestDistance = int.MaxValue;
			foreach (GameObject o in zone.FastSquareVisibility(origin.X, origin.Y, WakeRadius, "Combat", ParentObject, VisibleToPlayerOnly: false, IncludeWalls: true, skip: null))
			{
				if (o == ParentObject || o.IsWall() || o.Brain == null)
					continue;
				if (!ParentObject.IsHostileTowards(o))
					continue;
				int distance = ParentObject.DistanceTo(o);
				if (distance <= WakeRadius && distance < bestDistance)
				{
					best = o;
					bestDistance = distance;
				}
			}
			return best;
		}

		public void DressAsStatue(GameObject Creature = null)
		{
			Render render = ParentObject.Render;
			Description description = ParentObject.GetPart<Description>();
			if (render == null || description == null)
				return;
			GameObject creature = Creature ?? EncountersAPI.GetACreature();
			if (creature == null)
				return;
			if (AwakeTile.IsNullOrEmpty())
			{
				AwakeTile = render.Tile;
				AwakeRenderString = render.RenderString;
				AwakeName = render.DisplayName;
				AwakeDescription = description._Short;
			}
			DisguiseBlueprint = creature.Blueprint;
			string depicted = creature.an(int.MaxValue, null, null, AsIfKnown: false, Single: false, NoConfusion: false, NoColor: false, Stripped: true, WithoutTitles: false, Short: true, BaseOnly: true);
			render.DisplayName = Grammar.InitLower(Material) + " statue of " + depicted;
			render.RenderString = creature.Render.RenderString;
			render.Tile = creature.Render.Tile;
			SetColors(StatueColor, StatueDetail);
			Description creatureDescription = creature.GetPart<Description>();
			description._Short = "This statue worked from " + Material + " intricately depicts " + depicted + ":\n\n" + GameText.VariableReplace(creatureDescription?._Short ?? "", creature);
			if (DisguiseAddedLovely && !creature.HasPart<Lovely>())
			{
				ParentObject.RemovePart<Lovely>();
				DisguiseAddedLovely = false;
			}
			else if (creature.HasPart<Lovely>() && !ParentObject.HasPart<Lovely>())
			{
				ParentObject.RequirePart<Lovely>();
				DisguiseAddedLovely = true;
			}
			PrefabImposter plinth = ParentObject.GetPart<PrefabImposter>();
			if (plinth != null)
				plinth.Disabled = false;
		}

		public void Wake(string Why)
		{
			if (!Dormant)
				return;
			Dormant = false;
			WokeBy = Why;
			Render render = ParentObject.Render;
			string tile = render?.Tile;
			char statueFg = ColorChar(StatueColor, 'y');
			char statueDetail = ColorChar(StatueDetail, 'y');
			if (render != null)
			{
				if (!AwakeTile.IsNullOrEmpty())
					render.Tile = AwakeTile;
				if (!AwakeRenderString.IsNullOrEmpty())
					render.RenderString = AwakeRenderString;
				if (!AwakeName.IsNullOrEmpty())
					render.DisplayName = AwakeName;
				SetColors(AwakeColor, AwakeDetail);
			}
			if (DisguiseAddedLovely)
			{
				ParentObject.RemovePart<Lovely>();
				DisguiseAddedLovely = false;
			}
			Description description = ParentObject.GetPart<Description>();
			if (description != null && !AwakeDescription.IsNullOrEmpty())
				description._Short = AwakeDescription;
			PrefabImposter plinth = ParentObject.GetPart<PrefabImposter>();
			if (plinth != null)
				plinth.Disabled = true;
			ParentObject.Brain?.Goals.Clear();
			Cell cell = ParentObject.CurrentCell;
			if (cell != null && cell.IsVisible())
			{
				ParentObject.PlayWorldSound("Sounds/Damage/sfx_destroy_stone", 1f, Combat: true);
				if (!tile.IsNullOrEmpty())
					Cleo_TerraFirma_StoneRiseFX.PlayEtchReveal(cell, tile, statueFg, statueDetail);
			}
			Helpers.VerifyLog("GROTESQUE", $"{ParentObject.ID} woke: {Why}");
		}

		private void SetColors(string Color, string Detail)
		{
			Render render = ParentObject.Render;
			render.ColorString = Color;
			render.TileColor = Color;
			render.DetailColor = Detail;
		}

		private static char ColorChar(string Color, char Fallback)
		{
			if (Color.IsNullOrEmpty())
				return Fallback;
			int amp = Color.IndexOf('&');
			if (amp >= 0 && amp + 1 < Color.Length)
				return Color[amp + 1];
			return Color[0];
		}
	}
}
