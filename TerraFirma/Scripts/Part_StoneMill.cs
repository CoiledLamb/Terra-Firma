using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using ConsoleLib.Console;
using XRL.Language;
using XRL.Rules;
using XRL.UI;

namespace XRL.World.Parts
{
	/// <summary>
	/// Stone milling (RULED 2026-07-22): grinding rocks into weightless cooking-ingredient powders
	/// requires carrying the base game's Pestle -- the tool is the gate, no skill required.
	///
	/// One part, two homes (both attached via Merges.xml):
	///  - on the CAMPFIRE blueprint: GetCookingActionsEvent dispatches to the campfire object
	///    (SendToActorAndObject, decompiled), so the cooking menu grows a "Grind stones" entry when
	///    the player carries a pestle and stones. The event does NOT cascade into inventory, which
	///    is why this can't live on the pestle alone.
	///  - on the PESTLE blueprint: its own GetInventoryActionsEvent offers "grind stones" anywhere,
	///    no campfire needed (ruling: manual grinding stays possible).
	/// Both routes run the same batch menu, mirroring Campfire.PreserveExotic's decompiled shape:
	/// pick a stack, ask a count, loop until done -- so specific rocks can be kept, not ground.
	///
	/// Output: Cleo_TerraFirma_StoneMeal, stamped at grind time with the source rock's name and its
	/// PreparedCookingIngredient type (comma option lists ride along whole -- ground ajistone keeps
	/// its random-breath identity). Domainless rocks become plain "rock flour" (the PCI part is
	/// removed). Stamped powders of different types never stack together: PreparedCookingIngredient
	/// .SameAs compares type and description postfix (decompiled).
	///
	/// Numbers (signed 2026-07-22): 2-3 servings per rock, 5-6 per boulder, 1000 energy per grind.
	/// Milling is SAFE and silent (re-ruled 2026-07-22 verification session, supersedes the warn
	/// ruling): no hazard warnings, and no gyrestone vortex release -- live-fire showed Destroy
	/// never detonates grenade rocks, so the warnings were a bluff; the user cut the whole hazard
	/// layer rather than make it real. (The Gravel Whorl's tear-apart of dangerous stones is a
	/// separate system and unaffected.)
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneMill : IPart
	{
		private const string GRIND_COMMAND = "Cleo_TerraFirma_GrindStones";
		private const string GRIND_ONE_COMMAND = "Cleo_TerraFirma_GrindThisStone";
		private const string STONE_TAG = "Cleo_TerraFirma_IsAnyStone";
		private const string POWDER_BLUEPRINT = "Cleo_TerraFirma_StoneMeal";

		/// <summary>GRIND-FIRST (ruled 2026-07-22): milling is the ONLY door to the rock pantry --
		/// rocks carry no ingredient parts; the domains live on Ground_* forms. Third home added
		/// same day: the base Millstone (Joppa's mill, which already grinds glands into pastes)
		/// grinds stones with no pestle needed -- set false on its merge line.</summary>
		public bool RequiresPestle = true;

		public override bool SameAs(IPart p) => true;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetCookingActionsEvent.ID
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID;
			// || ID == GetShortDescriptionEvent.ID; -- pantry rules text, COMMENTED OUT (user 2026-07-22)
		}

		/// <summary>
		/// Pantry rules text (user request 2026-07-22; BUILT then COMMENTED OUT same day, user:
		/// "comment it back out for now" -- re-enable by restoring this method and the WantEvent
		/// line above): grind-first hid the rocks' cooking life from the item screen, so the stone
		/// itself would say what milling yields. Composed from the Ground_ twin's BLUEPRINT, never
		/// an instance -- the fungal twins' blueprint default is plain "fungus", so the lottery
		/// stays unspoiled by construction. Strange rock special-cased (its 36-way union list would
		/// print a wall; the gamble rock earns a shrug). Grind-special and grenade stones get a
		/// hazard clause. Scope: takeable Cleo_TerraFirma_Rock_* only -- foreign stones and
		/// gemstones stay silent (their flour grind is the wasteful comedy, undocumented by
		/// design). Base note: PreservableItem shows no such text; this deliberately exceeded base
		/// for teachability. All text DRAFT.
		/// </summary>
		/*
		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			GameObject o = ParentObject;
			if (o != null && o.Brain == null && o.Physics != null && o.Physics.Takeable
				&& o.HasTag(STONE_TAG) && !o.Blueprint.IsNullOrEmpty()
				&& o.Blueprint.StartsWith("Cleo_TerraFirma_Rock_"))
			{
				string twinName = o.Blueprint.Replace("_Rock_", "_Ground_");
				if (GameObjectFactory.Factory.Blueprints.TryGetValue(twinName, out GameObjectBlueprint twin))
				{
					string groundName = twin.GetPartParameter("Render", "DisplayName", "powder");
					if (o.Blueprint == "Cleo_TerraFirma_Rock_Strange")
						E.Postfix.AppendRules("Milled, it yields " + groundName + ": adds who-knows-what to cooked meals."); // DRAFT
					else
					{
						string adds = ComposeDomainOrList(twin.GetPartParameter<string>("PreparedCookingIngredient", "type", null));
						if (!string.IsNullOrEmpty(adds))
							E.Postfix.AppendRules("Milled, it yields " + groundName + ": adds " + adds + " effects to cooked meals.");
						else
							E.Postfix.AppendRules("Milled, it yields " + groundName + ".");
					}
				}
				else
				{
					E.Postfix.AppendRules("Milled, it yields rock flour.");
				}
				if (!string.IsNullOrEmpty(o.GetTag("Cleo_TerraFirma_GrindSpawn")) || o.GetFirstPartDescendedFrom<IGrenade>() != null)
					E.Postfix.AppendRules("Milling it is not gentle."); // DRAFT
			}
			return base.HandleEvent(E);
		}
		*/

		/// <summary>The PCI's own description recipe (decompiled), shared by the stamped-powder path
		/// and the rocks' pantry rules text: comma-list type -> per-keyword mapping Description tags,
		/// deduped, or-listed. Null/empty type (or no resolvable keywords) returns null.</summary>
		private static string ComposeDomainOrList(string type)
		{
			if (string.IsNullOrEmpty(type))
				return null;
			List<string> descriptions = new List<string>();
			foreach (string option in type.Split(','))
			{
				if (GameObjectFactory.Factory.Blueprints.TryGetValue("ProceduralCookingIngredient_" + option, out GameObjectBlueprint bp))
				{
					string tag = bp.GetTag("Description");
					if (!string.IsNullOrEmpty(tag) && !descriptions.Contains(tag))
						descriptions.Add(tag);
				}
			}
			return descriptions.Count > 0 ? Grammar.MakeOrList(descriptions) : null;
		}

		// campfire home: the cooking menu entry
		public override bool HandleEvent(GetCookingActionsEvent E)
		{
			if (E.Actor != null && GetPestle(E.Actor) != null && HasGrindableStone(E.Actor))
			{
				E.AddAction("GrindStones", "Grind stones with your pestle.", GRIND_COMMAND, null, 'g', FireOnActor: false, Default: 0, Priority: 45);
			}
			return base.HandleEvent(E);
		}

		// pestle home (the manual anywhere-action), millstone home (no pestle needed), and -- the
		// ergonomic front door -- STONE home: every IsAnyStone item offers "grind" directly when a
		// pestle is carried (merged onto BaseStone; interacting with the rock is the natural gesture)
		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (ParentObject == null || E.Actor == null)
				return base.HandleEvent(E);
			if ((ParentObject.Blueprint == "Pestle" || (!RequiresPestle && HostMillTurning())) && HasGrindableStone(E.Actor))
			{
				E.AddAction("GrindStones", "grind stones", GRIND_COMMAND, null, 'g', FireOnActor: false);
			}
			else if (ParentObject.HasTag(STONE_TAG) && GetPestle(E.Actor) != null)
			{
				E.AddAction("GrindThis", "grind with your pestle", GRIND_ONE_COMMAND, null, 'g', FireOnActor: false);
			}
			return base.HandleEvent(E);
		}

		/// <summary>Millstone power gate (RULED lean 2026-07-24, vetoable): the base Mill part is
		/// mechanically POWERED, and the ruling leans into that -- a dead millstone doesn't turn.
		/// The pestle remains the unpowered fallback anywhere. Non-mill homes are unaffected.</summary>
		private bool HostMillTurning()
		{
			if (ParentObject?.GetPart<Mill>() is Mill mill)
				return mill.IsReady(UseCharge: false, IgnoreCharge: false, IgnoreLiquid: false, IgnoreBootSequence: false, IgnoreBreakage: false, IgnoreRust: false, IgnoreEMP: false, IgnoreRealityStabilization: false, IgnoreSubject: false, IgnoreLocallyDefinedFailure: false, 1, null, UseChargeIfUnpowered: false, 0L);
			return true;
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == GRIND_COMMAND && E.Actor != null)
			{
				if (!RequiresPestle && !HostMillTurning())
				{
					Helpers.VerifyLog("GRIND", $"millstone refused {E.Actor.Blueprint}: not powered/turning (the power-gate lean working)");
					E.Actor.Fail("The millstone is still."); // DRAFT
					return false;
				}
				// the pestle IS the tool; the millstone replaces it; the campfire only hosts the menu
				bool needPestle = RequiresPestle && ParentObject?.Blueprint != "Pestle";
				RunGrindMenu(E.Actor, needPestle);
				return false;
			}
			if (E.Command == GRIND_ONE_COMMAND && E.Actor != null)
			{
				GrindDirect(E.Actor, E.Item);
				return false;
			}
			return base.HandleEvent(E);
		}

		/// <summary>The rock-side action: grind exactly this stack, pestle required.</summary>
		private static void GrindDirect(GameObject actor, GameObject rock)
		{
			if (rock == null || !rock.HasTag(STONE_TAG))
				return;
			if (GetPestle(actor) == null)
			{
				actor.Fail("You need a pestle to grind stones.");
				return;
			}
			int count = 1;
			if (rock.Count > 1)
			{
				int? asked = Popup.AskNumber(rock.DisplayNameOnlyDirect + ": how many do you want to grind? (max = " + rock.Count + ")", "Sounds/UI/ui_notification", "", rock.Count, 0, rock.Count);
				if (!asked.HasValue || asked.Value <= 0)
					return;
				count = asked.Value;
			}
			GrindStack(actor, rock, count);
			actor.UseEnergy(1000, "Grind Stones");
		}

		private static GameObject GetPestle(GameObject actor)
		{
			List<GameObject> found = actor.GetInventoryDirect((GameObject go) => go.Blueprint == "Pestle");
			return found.Count > 0 ? found[0] : null;
		}

		private static bool HasGrindableStone(GameObject actor)
		{
			return actor.GetInventoryDirect((GameObject go) => go.HasTag(STONE_TAG)).Count > 0;
		}

		/// <summary>PreserveExotic's decompiled loop shape: enumerate, pick, count, grind, repeat.</summary>
		private static void RunGrindMenu(GameObject actor, bool needPestle = true)
		{
			if (needPestle && GetPestle(actor) == null)
			{
				actor.Fail("You need a pestle to grind stones.");
				return;
			}
			while (true)
			{
				List<GameObject> stacks = actor.GetInventoryDirect((GameObject go) => go.HasTag(STONE_TAG));
				if (stacks.Count == 0)
				{
					if (actor.IsPlayer())
						Popup.Show("You have no stones left to grind.");
					return;
				}
				string[] names = new string[stacks.Count];
				IRenderable[] icons = new IRenderable[stacks.Count];
				for (int i = 0; i < stacks.Count; i++)
				{
					names[i] = stacks[i].DisplayName;
					icons[i] = stacks[i].RenderForUI();
				}
				int pick = Popup.PickOption("Choose a stone to grind.", null, "", "Sounds/UI/ui_notification", names, null, icons, null, null, null, null, 0, 60, 0, -1, AllowEscape: true);
				if (pick < 0)
					return;
				GameObject rock = stacks[pick];
				int count = 1;
				if (rock.Count > 1)
				{
					int? asked = Popup.AskNumber(rock.DisplayNameOnlyDirect + ": how many do you want to grind? (max = " + rock.Count + ")", "Sounds/UI/ui_notification", "", rock.Count, 0, rock.Count);
					if (!asked.HasValue || asked.Value <= 0)
						continue;
					count = asked.Value;
				}
				GrindStack(actor, rock, count);
				actor.UseEnergy(1000, "Grind Stones");
			}
		}

		private static void GrindStack(GameObject actor, GameObject rock, int count)
		{
			// read the source's identity before anything is destroyed
			string sourceBlueprint = rock.Blueprint ?? "";
			string sourceName = rock.Render?.DisplayName ?? "stone";
			string sourceType = rock.GetPart<PreparedCookingIngredient>()?.type;
			// gyrestone vortex release CUT (re-ruled 2026-07-22 verification session): milling is
			// safe -- the powder is the whole yield. GrindSpawn tags remain on blueprints for the
			// Gravel Whorl's tear-apart system.
			bool boulder = rock.GetBlueprint()?.DescendsFrom("BaseBoulder") ?? false;
			int made = 0;
			GameObject lastPowder = null;
			for (int i = 0; i < count; i++)
			{
				GameObject single = rock.Count > 1 ? rock.RemoveOne() : rock;
				int servings = boulder ? Stat.Random(5, 6) : Stat.Random(2, 3);
				for (int s = 0; s < servings; s++)
				{
					lastPowder = MakePowder(sourceBlueprint, sourceName, sourceType);
					actor.ReceiveObject(lastPowder);
					made++;
				}
				if (GameObject.Validate(single))
					single.Destroy(null, Silent: true);
				if (i < count - 1 && !GameObject.Validate(rock))
					break; // stack died early somehow -- bail gracefully (live-fire showed Destroy never detonates, but the guard is free)
			}
			Helpers.VerifyLog("GRIND", $"{actor.Blueprint} ground {count}x {sourceBlueprint} -> {made} serving(s) of {lastPowder?.Blueprint ?? "nothing"}{(boulder ? " (boulder rate 5-6)" : "")}");
			actor.PlayWorldSound("sfx_damage_stone");
			if (actor.IsPlayer() && made > 0)
			{
				string powderName = lastPowder?.Render?.DisplayName ?? "rock flour";
				string message = "You grind " + sourceName + " into " + Grammar.Cardinal(made) + " serving" + (made == 1 ? "" : "s") + " of " + powderName + ".";
				Popup.Show(message);
			}
		}

		private static GameObject MakePowder(string sourceBlueprint, string sourceName, string sourceType)
		{
			// per-rock powder blueprints (RULED 2026-07-22, the "split" pole of the naming wart):
			// Cleo_TerraFirma_Rock_X grinds into Cleo_TerraFirma_Ground_X -- a real blueprint with a
			// static name and domain, so Inspired dish-naming and HistorySpice key on something true.
			// Fungal grounds carry the lottery part themselves and restamp to their infection's
			// domains on creation. Anything without a Ground_ twin (flour sources, foreign stones)
			// falls through to the stamped generic below.
			string powderBlueprint = sourceBlueprint.Replace("_Rock_", "_Ground_");
			if (powderBlueprint != sourceBlueprint
				&& GameObjectFactory.Factory.Blueprints.ContainsKey(powderBlueprint))
			{
				return GameObject.Create(powderBlueprint);
			}
			GameObject powder = GameObject.Create(POWDER_BLUEPRINT);
			PreparedCookingIngredient pci = powder.GetPart<PreparedCookingIngredient>();
			if (string.IsNullOrEmpty(sourceType))
			{
				// domainless source: rock flour -- the stonecarvers' baking staple. Keeps the
				// blueprint's tastyMinor (RULED 2026-07-22, the crusty-loaf family): flour forces
				// the tasty roll, satiation stays zero, and it closes the dull-rock loop.
				if (powder.Render != null)
					powder.Render.DisplayName = "rock flour";
			}
			else
			{
				if (powder.Render != null)
					powder.Render.DisplayName = "ground " + sourceName;
				if (pci != null)
				{
					// restamp type AND the auto-written description postfix (ObjectCreatedEvent wrote
					// it from the placeholder type; ComposeDomainOrList mirrors its own composition)
					pci.type = sourceType;
					pci.charges = 1;
					string orList = ComposeDomainOrList(sourceType);
					if (!string.IsNullOrEmpty(orList))
						pci.descriptionPostfix = "Adds " + orList + " effects to cooked meals.";
				}
			}
			return powder;
		}
	}
}
