using System;
using System.Collections.Generic;
using XRL.Language;
using XRL.Messages;
using XRL.Rules;
using XRL.World.AI.GoalHandlers;
using XRL.World.Anatomy;

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
	/// Graffitied's line followed by the inkling's own description (user 2026-10-17). It comes
	/// back out after CalmTurns with nothing scary near.
	///
	/// Petted (base Pettable, so only when it likes you): it rides on you as a tattoo. The whole
	/// creature leaves the map and is held by Cleo_TerraFirma_InklingTattoo on the host, so its
	/// inventory, skills and the rest come back intact when it hops off.
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

		/// <summary>Quiet turns in the wall before it comes out. DIAL.</summary>
		public int CalmTurns = 5;

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
			if (Hidden)
			{
				if (!ParentObject.CurrentCell.HasWall())
				{
					// the wall went away under it (dug, disintegrated): it's just standing there now
					Unhide();
					return base.HandleEvent(E);
				}
				if (threat != null)
					QuietTurns = 0;
				else if (++QuietTurns >= CalmTurns && Emerge())
					return false;
				ParentObject.UseEnergy(1000, "Pass");
				return false;
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
				ParentObject.Brain.WallWalker = false;
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
				string own = E.Base.ToString();
				E.Base.Clear().Append(wall.GetPart<Description>()?.Short ?? "");
				if (E.Infix.Length > 0)
					E.Infix.Append("\n");
				// base Graffitied's line, then the inkling's own description (user 2026-10-17)
				E.Infix.Append("\nGraffiti is scrawled across the surface. It reads: \n\n\"{{K|")
					.Append(GraffitiText).Append("}}\"\n\n").Append(own);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetHostilityRecognitionLimitsEvent E)
		{
			if (Hidden)
				E.MaxDistance(1);
			return base.HandleEvent(E);
		}

		/// <summary>Leave the map and ride on Host as a tattoo. False if Host has no room for one.</summary>
		public bool RideOn(GameObject Host)
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
