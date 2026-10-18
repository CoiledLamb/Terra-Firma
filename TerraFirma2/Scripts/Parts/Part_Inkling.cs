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
	/// <summary>
	/// The inkling (TODO: build the inkling; designed with the user 2026-10-17, recon in
	/// design/inkling-recon-2026-10-17.md). A skittish cave prey animal with two hiding places.
	///
	/// Scared (hurt, or something it doesn't like comes within ScareRadius): it slips into an
	/// adjacent wall and passes for graffiti. Entry is the engine's own wall rule, Brain.WallWalker
	/// (Physics.HandleEvent(ObjectEnteringCellEvent)), so nothing phases; the disguise is base
	/// Mimic's render idiom aimed at the wall in its own cell, and the look text is base
	/// Graffitied's line, reading like any graffitied wall (user 2026-10-17). It comes
	/// back out after CalmTurns with nothing scary near.
	///
	/// Petted (base Pettable, so only when it likes you): it rides on you as a tattoo. The whole
	/// creature leaves the map and is held by Cleo_TerraFirma_InklingTattoo on the host, so its
	/// inventory, skills and the rest come back intact when it hops off.
	///
	/// Hungry (every MealEvery turns, hiding or not, and hunger beats fear): it walks to the
	/// nearest tattooed creature within HungerRadius and jumps on as a tattoo, no petting needed,
	/// then eats from riding (user 2026-10-17). A wild inkling never eats directly.
	///
	/// Radii and turn counts are Codex PROPOSAL dials. Message lines are DRAFTS for the user.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_Inkling : IPart
	{
		/// <summary>Anything it doesn't like this close scares it. DIAL.</summary>
		public int ScareRadius = 4;

		/// <summary>Feeling at or above which a creature doesn't scare it (Pettable's "liked" line).</summary>
		public int LikedFeeling = 50;

		/// <summary>Intelligence save an adjacent NPC makes to spot it hiding (user 2026-10-18). DIAL.</summary>
		public int SpotSave = 10;

		/// <summary>Quiet turns in the wall before it comes out. DIAL.</summary>
		public int CalmTurns = 5;

		/// <summary>Turns between a wild inkling's meals, rolled fresh after each. DIAL.</summary>
		public string MealEvery = "100-200";

		/// <summary>How far a hungry inkling will go for a tattooed creature. DIAL.</summary>
		public int HungerRadius = 8;

		/// <summary>Chance in 100 a meal eats the tattoo outright; otherwise it smears it. DIAL.</summary>
		public int EatChance = 50;

		public int TurnsToMeal = -1;

		public bool Hidden;
		public bool Scared;
		public int QuietTurns;

		/// <summary>What the graffiti reads while it hides (rolled per hiding, base Graffitied's recipe).</summary>
		public string GraffitiText;

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
				RideOn(E.Actor);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			Brain brain = ParentObject.Brain;
			if (brain == null || ParentObject.IsPlayer() || brain.PartyLeader != null || ParentObject.CurrentCell == null)
				return base.HandleEvent(E);

			GameObject threat = FindThreat();

			// hunger ticks whether or not it is hiding, and beats fear (user 2026-10-17: it should
			// care about your tattoos before it rides you; in-game, a hiding inkling never got hungry)
			if (TurnsToMeal < 0)
				TurnsToMeal = MealEvery.RollCached();
			if (TurnsToMeal > 0)
				TurnsToMeal--;
			GameObject meal = TurnsToMeal == 0 ? FindTattooed() : null;
			// adjacent: it jumps on, straight out of the wall if need be, and eats from riding
			// (user 2026-10-17: "it should have to jump on to you to eat the tattoo")
			if (meal != null && ParentObject.DistanceTo(meal) <= 1 && RideOn(meal, Hungry: true))
			{
				TurnsToMeal = -1;
				return false;
			}

			if (Hidden)
			{
				if (!ParentObject.CurrentCell.HasWall())
				{
					// the wall went away under it (dug, disintegrated): it's just standing there now
					Unhide();
					return base.HandleEvent(E);
				}
				if (meal != null)
				{
					// hunger draws it out of the wall
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

			if (meal != null)
			{
				// only when not already walking: MoveTo spends its first turn pathfinding and pushes
				// Step goals (MoveTo.TakeAction), so re-pushing it every turn never takes a step
				if (!brain.HasGoal<MoveTo>() && !brain.HasGoal<Step>())
				{
					// head for an open cell beside the meal; FindPath won't end on an occupied cell
					Cell goal = null;
					int best = int.MaxValue;
					foreach (Cell c in meal.CurrentCell.GetLocalAdjacentCells())
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
					brain.Goals.Clear();
					brain.PushGoal(goal != null ? new MoveTo(goal) : new MoveTo(meal, shortBy: 1));
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"hungry: heading for {meal.Blueprint}");
				}
				return base.HandleEvent(E);
			}

			if (threat != null || Scared)
			{
				if (Hide())
					return false;
				if (threat != null && !brain.HasGoal("Flee"))
				{
					brain.Goals.Clear();
					brain.PushGoal(new Flee(threat, 3, Panicked: true, LocalOnly: true));
				}
			}
			return base.HandleEvent(E);
		}

		GameObject FindThreat()
		{
			Cell here = ParentObject.CurrentCell;
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
						if (obj == ParentObject || obj.Brain == null || !obj.IsCreature || obj.HasPart<Cleo_TerraFirma_Inkling>())
							continue;
						if (obj.IsHostileTowards(ParentObject) || ParentObject.Brain.GetFeeling(obj) < LikedFeeling && obj.IsPlayerControlled())
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
						Tattoos tattoos = obj.GetPart<Tattoos>();
						if (tattoos == null || tattoos.Descriptions.Count == 0)
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

		bool Hide()
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
			// the base wall-dweller flag: look (base LookSorter) then ranks it with the wall and picks
			// the creature, and the zone wall sanity check leaves it be (caught in game 2026-10-17)
			ParentObject.Brain.LivesOnWalls = true;
			QuietTurns = 0;
			GraffitiText = RollGraffiti();
			ParentObject.Brain.Goals.Clear();
			DidX("seep", "into the wall"); // DRAFT line
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"hid in {target.GetFirstWall()?.Blueprint} at {target.X},{target.Y}");
			return true;
		}

		bool Emerge()
		{
			List<Cell> open = new List<Cell>();
			foreach (Cell cell in ParentObject.CurrentCell.GetLocalAdjacentCells())
			{
				if (!cell.HasWall() && cell.IsEmpty())
					open.Add(cell);
			}
			if (open.Count == 0)
				return false;
			if (!ParentObject.DirectMoveTo(open.GetRandomElement(), 1000))
				return false;
			Unhide();
			DidX("peel", "away from the wall"); // DRAFT line
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
			// base Graffitied.Graffiti's recipe, minus the wall recolor
			string corpus = "LibraryCorpus.json";
			MarkovBook.EnsureCorpusLoaded(corpus);
			string text = MarkovChain.GenerateShortSentence(MarkovBook.CorpusData[corpus]);
			return Grammar.Obfuscate(text.TrimEnd(' '), 10);
		}

		GameObject HiddenIn => Hidden ? ParentObject.CurrentCell?.GetFirstWall() : null;

		public override bool Render(RenderEvent E)
		{
			GameObject wall = HiddenIn;
			if (wall?.Render != null)
			{
				// Mimic's idiom, aimed at the wall it sits in; the ink shows as the wall's foreground
				E.Tile = wall.Render.Tile;
				E.RenderString = wall.Render.RenderString;
				E.ApplyColors("&K", wall.Render.DetailColor, Mimic.ICON_COLOR_PRIORITY, Mimic.ICON_COLOR_PRIORITY);
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
				// base Graffitied's line alone: it reads like any graffitied wall (user 2026-10-17)
				E.Infix.Append("\nGraffiti is scrawled across the surface. It reads: \n\n\"{{K|")
					.Append(GraffitiText).Append("}}\"\n");
			}
			return base.HandleEvent(E);
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			// object side, LATE: runs after PerceptionEvent's base line-of-sight check, which counts
			// solid cells, so a hunter looking at the wall would otherwise see it
			Registrar.Register(PerceptionEvent.ObjectID, EventOrder.LATE);
			base.Register(Object, Registrar);
		}

		public override bool HandleEvent(PerceptionEvent E)
		{
			// hidden, NPCs only see it from adjacent, and then only on an Intelligence save (user
			// 2026-10-18: "a 10 int save"), rolled per look, the way Hidden's search rolls per move;
			// base smell, hearing and clairvoyance run earlier on the looker's side and still find it
			if (Hidden && E.Object == ParentObject && !E.PlayerPerspective && E.Senses.Contains(Perception.Visual)
				&& (E.Actor.DistanceTo(ParentObject) > 1 || !E.Actor.MakeSave("Intelligence", SpotSave, Vs: "Inkling Spot", Source: ParentObject)))
				E.Senses.Remove(Perception.Visual);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetHostilityRecognitionLimitsEvent E)
		{
			if (Hidden)
				E.MaxDistance(1);
			return base.HandleEvent(E);
		}

		/// <summary>Leave the map and ride on Host as a tattoo. False if Host has no room for one.</summary>
		public bool RideOn(GameObject Host, bool Hungry = false)
		{
			if (Host?.Body == null || Host.HasPart<Cleo_TerraFirma_InklingTattoo>() || ParentObject.CurrentCell == null)
				return false;
			List<BodyPart> spots = new List<BodyPart>();
			foreach (BodyPart part in Host.Body.GetParts())
			{
				if (Tattoos.CanApplyTattoo(Host, part, true, true) == Tattoos.ApplyResult.Tattooable)
					spots.Add(part);
			}
			if (spots.Count == 0)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"ride: no tattooable skin on {Host.Blueprint}");
				return false;
			}
			BodyPart spot = spots.GetRandomElement();
			string text = Grammar.A(ParentObject.Render.DisplayName);
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
				TurnsToMeal = Hungry ? 1 : -1, // hungry: eats at the end of this turn
			});
			if (Host.IsPlayer())
				MessageQueue.AddPlayerMessage($"{ParentObject.Does("seep")} into your skin and settles on your {spot.Name}."); // DRAFT line
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("INKLING", $"riding {Host.Blueprint} on {spot.Name} as '{text}'");
			return true;
		}

		/// <summary>Back on the map next to Host (called by the rider part).</summary>
		public void HopOff(Cell Landing, bool Frightened)
		{
			Landing.AddObject(ParentObject);
			Scared = Frightened;
		}
	}
}
