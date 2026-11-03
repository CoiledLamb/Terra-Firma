using System;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.ZoneBuilders;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_IconoclastStatue : IPart
	{
		public bool Dormant = true;

		public const string FALLBACK_MUSE = "Humanoid";
		public const string ANIMATE_FIGURINE = "Cleo_TerraFirma_AnimateFigurine";
		public const string EVIL_TWIN_FACTIONS = "Entropic-100,Mean-100,Playerhater-99";

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == TookDamageEvent.ID
				|| ID == BeforeDeathRemovalEvent.ID;
		}

		public override bool HandleEvent(TookDamageEvent E)
		{
			if (Dormant && The.Player != null)
				Wake(The.Player, "took damage from " + (E.Actor?.Blueprint ?? "nothing"));
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeDeathRemovalEvent E)
		{
			FailQuestTarget(ParentObject);
			return base.HandleEvent(E);
		}

		public static int LevelFor(GameObject Bearer) => Math.Max(1, Bearer?.Level ?? 1);

		public const int HP_PER_LEVEL = 20;
		public const int PHYSIQUE_FLOOR = 18;
		public static int PhysiqueFloorFor(int Level) => Math.Min(PHYSIQUE_FLOOR, 12 + Level / 2);
		public const int MAX_MUTATION_RANK = 20;
		public static int HPFor(int Level) => HP_PER_LEVEL * Level;
		public static int AVFor(int Level) => Cleo_TerraFirma_Stoneshaper.GetAV(Level);
		public static int HPFor(int Level, bool Melted) => Melted ? HPFor(Level) / 2 : HPFor(Level);
		public static int AVFor(int Level, bool Melted) => Melted ? AVFor(Level) / 2 : AVFor(Level);
		public static string DamageFor(int Level) => Level / 3 > 0 ? $"1d6+{Level / 3}" : "1d6";
		public static int MutationRankFor(int Level) => Math.Min(Level / 3 + 1, MAX_MUTATION_RANK);

		public void Wake(GameObject Bearer, string Why)
		{
			if (!Dormant)
				return;
			Dormant = false;
			if (ParentObject.InInventory != null || ParentObject.Equipped != null || ParentObject.CurrentCell == null)
			{
				ParentObject.RemovePart(this);
				return;
			}
			if (Cleo_TerraFirma_Iconoclast.IsFigurine(ParentObject))
				WakeFigurine(Bearer, Why);
			else
				WakeStatue(Bearer, Why);
		}

		private void WakeStatue(GameObject Bearer, string Why)
		{
			GameObject statue = ParentObject;
			Cell cell = statue.CurrentCell;
			string tile = statue.Render?.Tile;
			char fg = ColorChar(statue.Render?.ColorString, 'y');
			char detail = ColorChar(statue.Render?.DetailColor, 'y');
			string depicted = statue.GetPart<RandomStatue>()?.BaseBlueprint;
			GameObject muse = GameObject.CreateSample(depicted.IsNullOrEmpty() ? FALLBACK_MUSE : depicted)
				?? GameObject.CreateSample(FALLBACK_MUSE);
			int level = LevelFor(Bearer);
			bool melted = Cleo_TerraFirma_MeltedStatue.IsMelted(statue);
			try
			{
				AnimateObject.Animate(statue);
				if (muse != null)
					Cleo_TerraFirma_Stoneshaper.ApplyStatueBuffs(Bearer, muse, statue, level, null);
			}
			finally
			{
				muse?.Obliterate(Silent: true);
			}
			statue.RemovePart<Distraction>();
			ConversationScript talk = statue.GetPart<ConversationScript>();
			if (talk != null)
				talk.ConversationID = "NewlySentientBeings";
			Strengthen(statue, level, melted);
			string mutation = AddStoneMutations(statue, MutationRankFor(level), StoneMutationsFor(Bearer));
			MakeHostile(statue, Bearer);
			if (cell != null && cell.IsVisible())
			{
				statue.PlayWorldSound("Sounds/Damage/sfx_destroy_stone", 1f, Combat: true);
				if (!tile.IsNullOrEmpty())
					Cleo_TerraFirma_StoneRiseFX.PlayEtchReveal(cell, tile, fg, detail);
			}
			Helpers.VerifyLog("ICONOCLAST", $"{statue.ID} ({statue.DisplayNameOnlyStripped}) woke: {Why}; muse {depicted ?? FALLBACK_MUSE}, level {level}{(melted ? ", melted" : "")}, {statue.GetStatValue("Hitpoints")} HP, AV {statue.GetStatValue("AV")}, {mutation}");
		}

		public static void Strengthen(GameObject Statue, int Level, bool Melted = false)
		{
			foreach (string stat in new[] { "Strength", "Agility", "Toughness" })
			{
				Statistic s = Statue.GetStat(stat);
				int floor = PhysiqueFloorFor(Level);
				if (s != null && s.BaseValue < floor)
					s.BaseValue = floor;
			}
			var old = Statue.GetPart<Cleo_TerraFirma_StoneshaperStatueStatsPart>();
			string museMap = old?.MuseWeaponMap ?? "";
			if (old != null)
				Statue.RemovePart(old);
			var stats = new Cleo_TerraFirma_StoneshaperStatueStatsPart(HPFor(Level, Melted) - Statue.GetStatValue("Hitpoints"), AVFor(Level, Melted) - Statue.GetStatValue("AV"))
			{
				NaturalWeaponDamage = DamageFor(Level),
				MuseWeaponMap = museMap,
			};
			Statue.AddPart(stats);
			stats.RearmNaturalWeapons();
		}

		private void WakeFigurine(GameObject Bearer, string Why)
		{
			GameObject figurine = ParentObject;
			Cell cell = figurine.CurrentCell;
			GameObject animate = GameObject.Create(ANIMATE_FIGURINE);
			if (animate == null)
				return;
			animate.Render.DisplayName = "animate " + figurine.DisplayNameOnlyStripped;
			animate.Render.Tile = figurine.Render.Tile;
			animate.Render.RenderString = figurine.Render.RenderString;
			animate.Render.ColorString = figurine.Render.ColorString;
			animate.Render.DetailColor = figurine.Render.DetailColor;
			Description from = figurine.GetPart<Description>();
			Description to = animate.GetPart<Description>();
			if (from != null && to != null)
				to._Short = from._Short;
			animate.SetIntProperty(Cleo_TerraFirma_Iconoclast.ROLLED, 1);
			animate.AddPart(new Cleo_TerraFirma_IconoclastStatue { Dormant = false });
			MakeHostile(animate, Bearer);
			char fg = ColorChar(figurine.Render.ColorString, 'y');
			char detail = ColorChar(figurine.Render.DetailColor, 'y');
			string tile = figurine.Render.Tile;
			string name = figurine.DisplayNameOnlyStripped;
			figurine.Obliterate(Silent: true);
			cell.AddObject(animate);
			if (cell.IsVisible())
			{
				animate.PlayWorldSound("Sounds/Damage/sfx_destroy_stone", 1f, Combat: true);
				if (!tile.IsNullOrEmpty())
					Cleo_TerraFirma_StoneRiseFX.PlayEtchReveal(cell, tile, fg, detail);
			}
			Helpers.VerifyLog("ICONOCLAST", $"figurine {name} woke as {animate.ID}: {Why}");
		}

		public static readonly Func<BaseMutation>[] STONE_POOL =
		{
			() => new Cleo_TerraFirma_Fissure(),
			() => new Cleo_TerraFirma_EarthenBarrage(),
			() => new Cleo_TerraFirma_Calcify(),
			() => new Cleo_TerraFirma_GorgonsGaze(),
		};

		public static int StoneMutationsFor(GameObject Bearer) => Math.Min(STONE_POOL.Length, 1 + (Bearer?.Level ?? 1) / 10);

		public static string AddStoneMutations(GameObject Statue, int Level, int Count)
		{
			var picks = new System.Collections.Generic.List<int> { 0, 1, 2, 3 };
			var names = new System.Collections.Generic.List<string>();
			Mutations mutations = Statue.RequirePart<Mutations>();
			for (int i = 0; i < Count && picks.Count > 0; i++)
			{
				int at = Stat.Random(0, picks.Count - 1);
				BaseMutation mutation = STONE_POOL[picks[at]]();
				picks.RemoveAt(at);
				Compat.AddMutation(mutations, mutation, Level);
				names.Add(mutation.GetType().Name + " " + Level);
			}
			return string.Join(", ", names);
		}

		public static void MakeHostile(GameObject Object, GameObject Bearer)
		{
			Brain brain = Object.Brain;
			if (brain == null)
				return;
			brain.PartyLeader = null;
			brain.Hibernating = false;
			brain.Staying = false;
			brain.Passive = false;
			brain.Factions = EVIL_TWIN_FACTIONS;
			brain.Allegiance.Hostile = true;
			brain.Allegiance.Calm = false;
			Grudge(Bearer, brain, 0);
		}

		private static void Grudge(GameObject Actor, Brain Brain, int Depth)
		{
			if (Actor == null || Depth >= 100)
				return;
			Brain.AddOpinion<OpinionInscrutable>(Actor);
			Grudge(Actor.PartyLeader, Brain, Depth + 1);
			if (Actor.TryGetEffect<Effects.Dominated>(out var dominated))
				Grudge(dominated.Dominator, Brain, Depth + 1);
		}

		public static void FailQuestTarget(GameObject Target)
		{
			if (!Target.HasPart<InteractQuestTarget>())
				return;
			var system = The.Game?.GetSystem<InteractWithAnObjectDynamicQuestManager.System>();
			if (system?.quests == null)
				return;
			for (int i = system.quests.Count - 1; i >= 0; i--)
			{
				var entry = system.quests[i];
				if (!Target.IDMatch(entry.itemID))
					continue;
				system.quests.RemoveAt(i);
				The.Game.FailQuestStep(entry.questID, entry.questStepID);
				Helpers.VerifyLog("ICONOCLAST", $"quest target {Target.ID} died: failed {entry.questID} / {entry.questStepID}");
			}
		}

		private static char ColorChar(string Color, char Fallback)
		{
			if (Color.IsNullOrEmpty())
				return Fallback;
			char c = Color[Color.Length - 1];
			return char.IsLetter(c) ? c : Fallback;
		}
	}
}
