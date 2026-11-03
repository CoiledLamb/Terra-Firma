using System;
using System.Collections.Generic;
using XRL.Language;
using XRL.Messages;
using XRL.Rules;
using XRL.World.AI.GoalHandlers;
using XRL.World.Anatomy;
using XRL.World.Capabilities;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_Inkling : IPart
	{
		public int ScareRadius = 4;

		public int LikedFeeling = 50;

		public int SpotSave = 10;

		public int CalmTurns = 5;

		public string MealEvery = "100-200";

		public int HungerRadius = 8;

		public int EatChance = 50;

		public int TurnsToMeal = -1;

		public bool Hidden;
		public bool Scared;
		public int QuietTurns;

		public string GraffitiText;

		public int HitchhikeCooldown = 10;

		[NonSerialized] public string AttachSound = "Sounds/Abilities/sfx_ability_spitSlime_spit";
		[NonSerialized] public float AttachSoundVolume = 0.5f;

		[NonSerialized] public string GraffitiSound = "Sounds/Interact/sfx_interact_bubbleLevel_flip";
		[NonSerialized] public float GraffitiSoundVolume = 0.5f;

		public string FledHostID;
		public int FledHostTurns;

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<BeginTakeActionEvent>.ID
				|| ID == TookDamageEvent.ID
				|| ID == AfterPetEvent.ID
				|| ID == GetDisplayNameEvent.ID
				|| ID == GetShortDescriptionEvent.ID
				|| ID == PooledEvent<GetHostilityRecognitionLimitsEvent>.ID;
		}

		public override bool HandleEvent(TookDamageEvent E)
		{
			Scared = true;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(AfterPetEvent E)
		{
			if (E.Actor != null && E.Actor != ParentObject)
			{
				BodyPart hand = null;
				if (E.Actor.Body != null && ParentObject.Body != null)
					Pettable.TryGetLimbs(E.Actor, ParentObject, out hand, out _);
				if (!RideOn(E.Actor, Preferred: hand, Petted: true))
				{
					string growl = GameText.VariableReplace("=subject.T= =verb:growl= softly.", ParentObject, E.Actor, StripColors: true);
					EmitMessage(growl, ' ', FromDialog: false, UsePopup: E.Actor.IsPlayer());
				}
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			Brain brain = ParentObject.Brain;
			if (brain == null || ParentObject.IsPlayer() || brain.PartyLeader != null || ParentObject.CurrentCell == null)
				return base.HandleEvent(E);

			if (FledHostTurns > 0 && --FledHostTurns == 0)
				FledHostID = null;

			GameObject threat = FindThreat();

			if (TurnsToMeal < 0)
				TurnsToMeal = MealEvery.RollCached();
			if (TurnsToMeal > 0)
				TurnsToMeal--;
			GameObject meal = TurnsToMeal == 0 ? FindTattooed() : null;
			GameObject ink = TurnsToMeal == 0 && meal == null ? FindInk(ParentObject, HungerRadius) : null;
			if (ink != null && !Hidden && ParentObject.DistanceTo(ink) <= 1)
			{
				DrinkInk(ink);
				TurnsToMeal = -1;
				ParentObject.UseEnergy(1000, "Pass");
				return false;
			}
			if (meal != null && ParentObject.DistanceTo(meal) <= 1 && RideOn(meal, Hungry: true))
			{
				TurnsToMeal = -1;
				return false;
			}

			if (Hidden)
			{
				if (!ParentObject.CurrentCell.HasWall())
				{
					Unhide();
					return base.HandleEvent(E);
				}
				if (meal != null || ink != null)
				{
					if (Emerge())
						return false;
				}
				else if (threat != null)
					QuietTurns = 0;
				else if (++QuietTurns >= CalmTurns && Emerge())
					return false;
				ParentObject.UseEnergy(1000, "Pass");
				return false;
			}

			if (meal != null || ink != null)
			{
				if (WalkTo(meal ?? ink))
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"hungry: heading for {(meal ?? ink).Blueprint}");
				return base.HandleEvent(E);
			}

			if (threat != null || Scared)
			{
				if (Hide())
					return false;
				if (Hitchhike())
					return false;
				if (threat != null && !brain.HasGoal("Flee"))
				{
					brain.Goals.Clear();
					brain.PushGoal(new Flee(threat, 3, Panicked: true, LocalOnly: true));
				}
			}
			return base.HandleEvent(E);
		}

		internal GameObject FindThreat(Cell Around = null, GameObject Victim = null)
		{
			Cell here = Around ?? ParentObject.CurrentCell;
			Zone zone = here?.ParentZone;
			if (zone == null)
				return null;
			GameObject best = null;
			int bestDist = int.MaxValue;
			for (int dx = -ScareRadius; dx <= ScareRadius; dx++)
			{
				for (int dy = -ScareRadius; dy <= ScareRadius; dy++)
				{
					Cell cell = zone.GetCell(here.X + dx, here.Y + dy);
					if (cell == null)
						continue;
					foreach (GameObject obj in cell.Objects)
					{
						if (obj == ParentObject || obj == Victim || obj.Brain == null || !obj.IsCreature || obj.HasPart<Cleo_TerraFirma_Inkling>())
							continue;
						if (Victim != null ? obj.IsHostileTowards(Victim)
							: obj.IsHostileTowards(ParentObject) || ParentObject.Brain.GetFeeling(obj) < LikedFeeling && obj.IsPlayerControlled())
						{
							int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
							if (d < bestDist)
							{
								best = obj;
								bestDist = d;
							}
						}
					}
				}
			}
			return best;
		}

		GameObject FindTattooed()
		{
			Cell here = ParentObject.CurrentCell;
			Zone zone = here?.ParentZone;
			if (zone == null)
				return null;
			GameObject best = null;
			int bestDist = int.MaxValue;
			for (int dx = -HungerRadius; dx <= HungerRadius; dx++)
			{
				for (int dy = -HungerRadius; dy <= HungerRadius; dy++)
				{
					Cell cell = zone.GetCell(here.X + dx, here.Y + dy);
					if (cell == null)
						continue;
					foreach (GameObject obj in cell.Objects)
					{
						if (obj == ParentObject || !obj.IsCreature)
							continue;
						if (!Cleo_TerraFirma_InklingTattoo.HasEdibleTattoo(obj))
							continue;
						int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
						if (d < bestDist)
						{
							best = obj;
							bestDist = d;
						}
					}
				}
			}
			return best;
		}

		internal static GameObject FindInk(GameObject Who, int Radius)
		{
			Cell here = Who.CurrentCell;
			Zone zone = here?.ParentZone;
			if (zone == null)
				return null;
			GameObject best = null;
			int bestDist = int.MaxValue;
			for (int dx = -Radius; dx <= Radius; dx++)
			{
				for (int dy = -Radius; dy <= Radius; dy++)
				{
					Cell cell = zone.GetCell(here.X + dx, here.Y + dy);
					if (cell == null || cell.HasWall())
						continue;
					foreach (GameObject obj in cell.Objects)
					{
						LiquidVolume lv = obj.LiquidVolume;
						if (lv == null || !lv.IsOpenVolume() || lv.Volume <= 0 || lv.GetPrimaryLiquidID() != "ink")
							continue;
						int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
						if (d < bestDist)
						{
							best = obj;
							bestDist = d;
						}
					}
				}
			}
			return best;
		}

		public const int INK_DRAMS = 5;

		internal void DrinkInk(GameObject Pool)
		{
			LiquidVolume lv = Pool?.LiquidVolume;
			if (lv == null)
				return;
			DidX("lap", "up the puddle of ink");
			lv.UseDrams(INK_DRAMS);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"drank ink from {Pool.Blueprint}");
		}

		internal bool WalkTo(GameObject Target)
		{
			Brain brain = ParentObject.Brain;
			if (brain == null || Target?.CurrentCell == null)
				return false;
#if !VERSION_1_0
			if (brain.HasGoal<MoveTo>() || brain.HasGoal<Step>())
#else
			if (brain.HasGoal("MoveTo") || brain.HasGoal("Step"))
#endif
				return false;
			Cell goal = null;
			if (Target.IsCreature)
			{
				int best = int.MaxValue;
				foreach (Cell c in Target.CurrentCell.GetLocalAdjacentCells())
				{
					if (!c.IsEmpty() || c.HasWall())
						continue;
					int d = ParentObject.CurrentCell.PathDistanceTo(c);
					if (d < best)
					{
						best = d;
						goal = c;
					}
				}
			}
			else
				goal = Target.CurrentCell;
			brain.Goals.Clear();
			brain.PushGoal(goal != null ? new MoveTo(goal) : new MoveTo(Target, shortBy: 1));
			return true;
		}

		bool Hitchhike()
		{
			List<GameObject> liked = new List<GameObject>();
			List<GameObject> others = new List<GameObject>();
			foreach (Cell cell in ParentObject.CurrentCell.GetLocalAdjacentCells())
			{
				foreach (GameObject obj in cell.Objects)
				{
					if (obj == ParentObject || !obj.IsCreature || obj.Body == null || obj.HasPart<Cleo_TerraFirma_Inkling>()
						|| (FledHostID != null && obj.ID == FledHostID))
						continue;
					if (ParentObject.Brain.GetFeeling(obj) >= LikedFeeling)
						liked.Add(obj);
					else
						others.Add(obj);
				}
			}
			liked.AddRange(others);
			foreach (GameObject host in liked)
			{
				if (RideOn(host))
				{
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"hitchhiked onto {host.Blueprint}");
					return true;
				}
			}
			return false;
		}

		internal bool Hide()
		{
			Cell here = ParentObject.CurrentCell;
			List<Cell> walls = new List<Cell>();
			foreach (Cell cell in here.GetLocalAdjacentCells())
			{
				if (cell.HasWall() && !cell.HasObjectWithPart("Combat"))
					walls.Add(cell);
			}
			if (walls.Count == 0)
				return false;
			Cell target = walls.GetRandomElement();
			ParentObject.Brain.WallWalker = true;
			if (!ParentObject.DirectMoveTo(target, 1000))
			{
				ParentObject.Brain.WallWalker = false;
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", "hide: move into wall refused");
				return false;
			}
			Hidden = true;
			Scared = false;
			ParentObject.Brain.LivesOnWalls = true;
			QuietTurns = 0;
			GraffitiText = RollGraffiti();
			ParentObject.Brain.Goals.Clear();
			DidX("seep", "into the wall");
			if (!GraffitiSound.IsNullOrEmpty())
				target.PlayWorldSound(GraffitiSound, GraffitiSoundVolume);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"hid in {target.GetFirstWall()?.Blueprint} at {target.X},{target.Y}");
			return true;
		}

		internal bool Emerge()
		{
			List<Cell> open = new List<Cell>();
			foreach (Cell cell in ParentObject.CurrentCell.GetLocalAdjacentCells())
			{
				if (!cell.HasWall() && cell.IsEmpty())
					open.Add(cell);
			}
			if (open.Count == 0)
				return false;
			Cell wall = ParentObject.CurrentCell;
			if (!ParentObject.DirectMoveTo(open.GetRandomElement(), 1000))
				return false;
			Unhide();
			DidX("peel", "away from the wall");
			if (!GraffitiSound.IsNullOrEmpty())
				wall.PlayWorldSound(GraffitiSound, GraffitiSoundVolume);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", "emerged from the wall");
			return true;
		}

		void Unhide()
		{
			Hidden = false;
			QuietTurns = 0;
			GraffitiText = null;
			if (ParentObject.Brain != null)
			{
				ParentObject.Brain.WallWalker = false;
				ParentObject.Brain.LivesOnWalls = false;
			}
		}

		static string RollGraffiti()
		{
			string corpus = "LibraryCorpus.json";
			MarkovBook.EnsureCorpusLoaded(corpus);
			string text = MarkovChain.GenerateShortSentence(MarkovBook.CorpusData[corpus]);
			return Grammar.Obfuscate(text.TrimEnd(' '), 10);
		}

		char InkColor
		{
			get
			{
				string cs = ParentObject.Render?.ColorString;
				int amp = cs?.IndexOf('&') ?? -1;
				return amp >= 0 && amp + 1 < cs.Length ? cs[amp + 1] : 'K';
			}
		}

		GameObject HiddenIn =>Hidden ? ParentObject.CurrentCell?.GetFirstWall() : null;

		public override bool Render(RenderEvent E)
		{
			GameObject wall = HiddenIn;
			if (wall?.Render != null)
			{
				E.Tile = wall.Render.Tile;
				E.RenderString = wall.Render.RenderString;
				E.ApplyColors("&" + InkColor, wall.Render.DetailColor, Mimic.ICON_COLOR_PRIORITY, Mimic.ICON_COLOR_PRIORITY);
			}
			return base.Render(E);
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			GameObject wall = HiddenIn;
			if (wall?.Render != null)
			{
				E.ReplacePrimaryBase(wall.Render.DisplayName);
				E.AddAdjective("{{graffitied|graffitied}}");
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			GameObject wall = HiddenIn;
			if (wall != null && !string.IsNullOrEmpty(GraffitiText))
			{
				E.Base.Clear().Append(wall.GetPart<Description>()?.Short ?? "");
				if (E.Infix.Length > 0)
					E.Infix.Append("\n");
				E.Infix.Append("\nGraffiti is scrawled across the surface. It reads: \n\n\"{{").Append(InkColor).Append("|")
					.Append(GraffitiText).Append("}}\"\n");
			}
			return base.HandleEvent(E);
		}

#if !VERSION_1_0
		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register(PerceptionEvent.ObjectID, EventOrder.LATE);
			base.Register(Object, Registrar);
		}

		public override bool HandleEvent(PerceptionEvent E)
		{
			if (Hidden && E.Object == ParentObject && !E.PlayerPerspective && E.Senses.Contains(Perception.Visual)
				&& (E.Actor.DistanceTo(ParentObject) > 1 || !E.Actor.MakeSave("Intelligence", SpotSave, Vs: "Inkling Spot", Source: ParentObject)))
				E.Senses.Remove(Perception.Visual);
			return base.HandleEvent(E);
		}
#endif

		public override bool HandleEvent(GetHostilityRecognitionLimitsEvent E)
		{
			if (Hidden)
				E.MaxDistance(1);
			return base.HandleEvent(E);
		}

		public bool RideOn(GameObject Host, bool Hungry = false, BodyPart Preferred = null, bool Petted = false)
		{
			if (Host?.Body == null || ParentObject.CurrentCell == null)
				return false;
			HashSet<int> taken = new HashSet<int>();
			foreach (Cleo_TerraFirma_InklingTattoo rider in Cleo_TerraFirma_InklingTattoo.Riders(Host))
				taken.Add(rider.BodyPartID);
			List<BodyPart> spots = new List<BodyPart>();
			foreach (BodyPart part in Host.Body.GetParts())
			{
				if (!taken.Contains(part.ID) && Tattoos.CanApplyTattoo(Host, part, true, true) == Tattoos.ApplyResult.Tattooable)
					spots.Add(part);
			}
			if (spots.Count == 0)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"ride: no tattooable skin on {Host.Blueprint}");
				return false;
			}
			BodyPart spot = null;
			if (Preferred != null)
			{
				if (spots.Contains(Preferred))
					spot = Preferred;
				else if (Preferred.ParentPart != null && spots.Contains(Preferred.ParentPart))
					spot = Preferred.ParentPart;
			}
			if (spot == null)
				spot = spots.GetRandomElement();
			string text = ParentObject.HasProperName ? ParentObject.Render.DisplayName : Grammar.A(ParentObject.Render.DisplayName);
			if (!Tattoos.IsSuccess(Tattoos.ApplyTattoo(Host, spot, true, false, text)))
				return false;

			Unhide();
			Scared = false;
			ParentObject.Brain?.Goals.Clear();
			ParentObject.CurrentCell.RemoveObject(ParentObject);
			Host.AddPart(new Cleo_TerraFirma_InklingTattoo
			{
				Inkling = ParentObject,
				BodyPartID = spot.ID,
				TattooText = text,
				TurnsToMeal = Hungry ? 1 : -1,
			});
			if (!AttachSound.IsNullOrEmpty())
				Host.PlayWorldSound(AttachSound, AttachSoundVolume);
			if (Host.IsPlayer())
				EmitMessage(Host, $"{ParentObject.Does("seep")} into your skin and settles on your {spot.GetOrdinalName()}.", ' ', FromDialog: false, UsePopup: Petted);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"riding {Host.Blueprint} on {spot.Name} as '{text}'");
			return true;
		}

		public void HopOff(Cell Landing, bool Frightened, GameObject From = null)
		{
			Landing.AddObject(ParentObject);
			if (!AttachSound.IsNullOrEmpty())
				(From?.CurrentCell ?? Landing).PlayWorldSound(AttachSound, AttachSoundVolume);
			Scared = Frightened;
			if (Frightened && From != null)
			{
				FledHostID = From.ID;
				FledHostTurns = HitchhikeCooldown;
			}
		}
	}
}
