using System;
using System.Collections.Generic;
using System.Linq;
using Cleo.TerraFirma.Scripts;
using Qud.API;
using XRL.Rules;
using XRL.World;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_QuestBurden : IPart
	{
		public static readonly string[] KINDS = { "heavy", "lost", "sluggish" };

		public static readonly Dictionary<string, string> ITEM_KINDS = new Dictionary<string, string>
		{
			{ "Cleo_TerraFirma_Random Stone Figurine", "shunned" },
			{ "Cleo_TerraFirma_BoxOfChalk", "frail" },
			{ "Cleo_TerraFirma_QuestWinePitcher", "addled" },
			{ "Cleo_TerraFirma_QuestHookah", "hazy" },
			{ "Cleo_TerraFirma_QuestQuarriedStone", "heavy" },
			{ "Cleo_TerraFirma_PorterStone", "heavy" },
			{ "Orrery", "lost" }
		};

		public static IEnumerable<string> AllKinds => KINDS.Concat(ITEM_KINDS.Values.SelectMany(v => v.Split(','))).Distinct();

		public static bool Has(string Kinds, string Kind) => Kinds != null && Array.IndexOf(Kinds.Split(','), Kind) >= 0;

		public bool Has(string Kind) => Has(this.Kind, Kind);

		public const int HEAVY_WEIGHT = 60;

		public static int ShunnedRep => RandomFigurine.COPPER_REP_VALUE * 2;

		public string Kind;

		public long CarriedSince = -1;

		public int SavePenalty => CarriedSince < 0 ? 1
			: Math.Min(Cleo_TerraFirma_QuestBurdenedEffect.MAX_SAVE_PENALTY, 1 + (int)((The.Game.TimeTicks - CarriedSince) / Calendar.TurnsPerDay));

		public static Cleo_TerraFirma_QuestBurden Roll(GameObject Item, string Kind = null)
		{
			if (Item == null)
				return null;
			bool own = ITEM_KINDS.TryGetValue(Item.Blueprint, out string ownKind);
			Kind ??= own ? ownKind : KINDS.GetRandomElement();
			if (Has(Kind, "shunned") && !Shun(Item))
				Kind = "heavy";
			if (Has(Kind, "heavy") && !(own && Has(ownKind, "heavy")) && Item.Physics != null)
				Item.Physics.Weight += HEAVY_WEIGHT;
			if (Item.Blueprint == "Orrery")
				Item.RemovePart<Examiner>();
			var part = new Cleo_TerraFirma_QuestBurden { Kind = Kind };
			Item.AddPart(part);
			return part;
		}

		private static bool Shun(GameObject Item)
		{
			AddsRep own = Item.GetPart<AddsRep>();
			if (own != null && !string.IsNullOrEmpty(own.Faction))
			{
				own.Value = -Math.Abs(own.Value) * 2;
				if (own.Faction.Contains(':'))
				{
					own.Faction = string.Join(",", own.Faction.Split(',').Select(f =>
					{
						int colon = f.IndexOf(':');
						if (colon < 0 || !int.TryParse(f.Substring(colon + 1), out int v))
							return f;
						return f.Substring(0, colon) + ":" + (-Math.Abs(v) * 2);
					}));
				}
				own.WorksOnCarrier = true;
				own.WorksOnEquipper = false;
				return true;
			}
			GameObject sample = EncountersAPI.GetASampleCreature(b => !b.HasTag("NoFigurine"));
			if (sample == null)
				return false;
			var list = new List<string>();
			if (sample.Brain != null)
				foreach (KeyValuePair<string, int> pair in sample.Brain.Allegiance)
					if (pair.Key != XRL.World.WorldBuilders.Cleo_TerraFirma_WarrenQuests.FACTION)
						list.Add(pair.Key);
			string factions = string.Join(",", list);
			sample.Obliterate();
			if (string.IsNullOrEmpty(factions))
				return false;
			AddsRep rep = AddsRep.AddModifier(Item, factions, -ShunnedRep);
			if (rep == null)
				return false;
			rep.WorksOnCarrier = true;
			rep.WorksOnEquipper = false;
			return true;
		}

		public bool Carried => Kind != null && Kind.Split(',').Any(k => k != "heavy" && k != "shunned");

		public override bool WantTurnTick() => Carried;

		public override void TurnTick(long TimeTick, int Amount) => Burden(ParentObject.Holder);

		public override bool WantEvent(int ID, int cascade)
			=> base.WantEvent(ID, cascade) || ID == TakenEvent.ID || ID == EquippedEvent.ID || ID == GetShortDescriptionEvent.ID;

		public override bool HandleEvent(TakenEvent E)
		{
			Burden(E.Actor);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EquippedEvent E)
		{
			Burden(E.Actor);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			var lines = new List<string>();
			if (Has("frail"))
				lines.Add($"-1 to physical saves for each day you carry it (up to -{Cleo_TerraFirma_QuestBurdenedEffect.MAX_SAVE_PENALTY}).");
			if (Has("addled"))
				lines.Add($"-1 to mental saves for each day you carry it (up to -{Cleo_TerraFirma_QuestBurdenedEffect.MAX_SAVE_PENALTY}).");
			if (Has("lost"))
				lines.Add($"Chance of becoming lost increased by {-Cleo_TerraFirma_QuestBurdenedEffect.LOST_BONUS}%.");
			if (Has("heavy") && !(ITEM_KINDS.TryGetValue(ParentObject.Blueprint, out string own) && Has(own, "heavy")))
				lines.Add($"+{HEAVY_WEIGHT} lbs. weight.");
			if (Has("sluggish"))
				lines.Add($"-{Cleo_TerraFirma_QuestBurdenedEffect.SLUGGISH_MOVE}% Move Speed");
			foreach (string line in lines)
				E.Postfix.AppendRules(line);
			return base.HandleEvent(E);
		}

		public void Burden(GameObject Holder)
		{
			if (!Carried || Holder == null || !Holder.IsCreature || ParentObject.Holder != Holder)
				return;
			string id = ParentObject.ID;
			if (CarriedSince < 0)
				CarriedSince = The.Game.TimeTicks;
			if (Holder.Effects != null && Holder.Effects.Any(e => e is Cleo_TerraFirma_QuestBurdenedEffect b && b.ItemID == id))
				return;
			Holder.ApplyEffect(new Cleo_TerraFirma_QuestBurdenedEffect { ItemID = id, Kind = Kind });
			Helpers.VerifyLog("QUEST", $"burden {Kind}: {Holder.DisplayNameOnlyDirectAndStripped} carries {ParentObject.DisplayNameOnlyDirectAndStripped}");
		}
	}
}

namespace XRL.World.Effects
{
	using XRL.World.Parts;

	[Serializable]
	public class Cleo_TerraFirma_QuestBurdenedEffect : Effect
	{
		public const int LOST_BONUS = -50;
		public const int MAX_SAVE_PENALTY = 4;
		public const int SLUGGISH_MOVE = 25;
		public const int HAZY_ODDS = 400;

		public string ItemID;
		public string Kind;

		public Cleo_TerraFirma_QuestBurdenedEffect()
		{
			Duration = DURATION_INDEFINITE;
		}

		public override int GetEffectType() => TYPE_MENTAL | TYPE_NEGATIVE;

		private GameObject Item()
		{
			GameObject item = null;
			Object?.ForeachInventoryAndEquipment(o =>
			{
				if (item == null && o.IDMatch(ItemID))
					item = o;
			});
			return item;
		}

		private bool Has(string Kind) => Cleo_TerraFirma_QuestBurden.Has(this.Kind, Kind);

		private int Penalty => Item()?.GetPart<Cleo_TerraFirma_QuestBurden>()?.SavePenalty ?? 1;

		public override string GetDescription()
		{
			if (Has("frail")) return "{{K|burdened}} (physical saves -" + Penalty + ")";
			if (Has("addled")) return "{{K|addled}} (mental saves -" + Penalty + ")";
			if (Has("hazy")) return "{{K|craving}}";
			if (Has("lost")) return "{{K|waylaid}}";
			if (Has("sluggish")) return "{{K|sluggish}}";
			return "{{K|burdened}}";
		}

		public override string GetDetails()
		{
			var lines = new List<string>();
			if (Has("lost")) lines.Add("More likely to get lost while traveling.");
			if (Has("frail")) lines.Add($"-{Penalty} to physical saves, growing by 1 each day you carry it (up to -{MAX_SAVE_PENALTY}).");
			if (Has("addled")) lines.Add($"-{Penalty} to mental saves, growing by 1 each day you carry it (up to -{MAX_SAVE_PENALTY}).");
			if (Has("hazy")) lines.Add("Now and then you take a puff, which leaves you slow and drinks the fresh water you carry.");
			if (Has("sluggish")) lines.Add($"-{SLUGGISH_MOVE}% Move Speed");
			return string.Join("\n", lines);
		}

		public override bool Apply(GameObject Object)
		{
			if (Has("sluggish"))
				StatShifter.SetStatShift(Object, "MoveSpeed", SLUGGISH_MOVE);
			return base.Apply(Object);
		}

		public override void Remove(GameObject Object)
		{
			StatShifter.RemoveStatShifts(Object);
			base.Remove(Object);
		}

		public override bool WantEvent(int ID, int cascade)
			=> base.WantEvent(ID, cascade) || ID == SingletonEvent<EndTurnEvent>.ID
				|| ID == GetLostChanceEvent.ID || ID == ModifyDefendingSaveEvent.ID;

		public override bool HandleEvent(EndTurnEvent E)
		{
			GameObject item = Item();
			if (item == null)
				Object.RemoveEffect(this);
			else if (Has("hazy") && Object.IsPlayer() && Stat.Random(1, HAZY_ODDS) == 1)
				Puff(item);
			return base.HandleEvent(E);
		}

		public void ForcePuff()
		{
			if (Has("hazy") && Item() is GameObject pipe)
				Puff(pipe);
		}

		private void Puff(GameObject Pipe)
		{
			Hookah hookah = Pipe.GetPart<Hookah>();
			LiquidVolume tank = Pipe.LiquidVolume;
			if (hookah == null || tank == null)
				return;
			LiquidVolume skin = Skin(Pipe);
			if (!hookah.CanPuff() && tank.Volume == 0 && skin != null && skin.UseDrams("water", 1))
			{
				tank.MixWith(new LiquidVolume("water", 1));
				skin = Skin(Pipe);
			}
			if (!hookah.CanPuff() || !hookah.SmokeHookah(Object, FromDialog: false))
				return;
			tank.UseDrams("water", 1);
			if (skin != null && skin.UseDrams("water", 1))
				tank.MixWith(new LiquidVolume("water", 1));
			Object.ApplyEffect(new Cleo_TerraFirma_HookahHazeEffect(Stat.Random(HazeMin, HazeMax)));
			Helpers.VerifyLog("QUEST", $"burden hazy: puffed {Pipe.DisplayNameOnlyDirectAndStripped}, tank {tank.Volume}, refilled from {skin?.ParentObject.DisplayNameOnlyDirectAndStripped ?? "nothing"}");
		}

		public const int HazeMin = 50, HazeMax = 100;

		private LiquidVolume Skin(GameObject Pipe)
		{
			LiquidVolume skin = null;
			Object.ForeachInventoryAndEquipment(o =>
			{
				if (skin == null && o != Pipe && o.LiquidVolume is LiquidVolume lv && lv.Amount("water") > 0)
					skin = lv;
			});
			return skin;
		}

		public override bool HandleEvent(GetLostChanceEvent E)
		{
			if (Has("lost"))
				E.PercentageBonus += LOST_BONUS;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ModifyDefendingSaveEvent E)
		{
			if ((Has("frail") && XRL.World.Parts.Mutation.Cleo_TerraFirma_CapriciousDevotion.IsPhysicalStat(E.Stat))
				|| (Has("addled") && XRL.World.Parts.Mutation.Cleo_TerraFirma_CapriciousDevotion.IsMentalStat(E.Stat)))
				E.Roll -= Penalty;
			return base.HandleEvent(E);
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_HookahHazeEffect : Effect
	{
		public Cleo_TerraFirma_HookahHazeEffect()
		{
		}

		public Cleo_TerraFirma_HookahHazeEffect(int Duration)
		{
			this.Duration = Duration;
		}

		public override int GetEffectType() => TYPE_MENTAL | TYPE_NEGATIVE | TYPE_REMOVABLE;

		public override bool UseStandardDurationCountdown() => true;

		public override string GetDescription() => "{{K|hazy}}";

		public override string GetDetails() => $"-{Cleo_TerraFirma_QuestBurdenedEffect.SLUGGISH_MOVE}% Move Speed";

		public override bool Apply(GameObject Object)
		{
			if (Object.TryGetEffect<Cleo_TerraFirma_HookahHazeEffect>(out var haze))
			{
				haze.Duration = Math.Max(haze.Duration, Duration);
				return false;
			}
			StatShifter.SetStatShift(Object, "MoveSpeed", Cleo_TerraFirma_QuestBurdenedEffect.SLUGGISH_MOVE);
			return base.Apply(Object);
		}

		public override void Remove(GameObject Object)
		{
			StatShifter.RemoveStatShifts(Object);
			base.Remove(Object);
		}
	}
}
