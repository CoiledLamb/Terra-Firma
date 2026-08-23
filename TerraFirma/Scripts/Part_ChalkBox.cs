using System;
using XRL.Rules;
using XRL.UI;

namespace XRL.World.Parts
{
	/// <summary>
	/// The box of chalk (design sitting 2026-08-22; ergonomics rework same day, USER RULING:
	/// actions live on the target when a target object exists, and floor-drawing - which has no
	/// target object - becomes an ACTIVATED ABILITY granted while the box is carried ("what if it
	/// gave you an ability? or perhaps ... the action should be on the thing you're doing the
	/// action on ... this goes for anything else we do also"). Draw = ability, direction-picked,
	/// stroke lands in the adjacent cell and auto-tiles (Part_ChalkLine). Chalkboard writing
	/// moved to the BOARD (Part_ChalkboardWritable, merged onto base Chalkboard). This part keeps
	/// the uses pool: 8-12 rolled at creation (user number), one per stroke or board-write,
	/// spent box crumbles.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_ChalkBox : IPart
	{
		public const string DRAW_COMMAND = "Cleo_TerraFirma_CommandDrawChalk";
		public const string LINE_BLUEPRINT = "Cleo_TerraFirma_ChalkLine";

		/// <summary>Strokes left in the box; rolled 8-12 at creation (user number).</summary>
		public int Uses = -1;

		/// <summary>The draw ability this box granted its holder, if it was the granting box.</summary>
		public Guid AbilityID = Guid.Empty;

		public override bool SameAs(IPart p)
		{
			// never engine-stack (user bug 2026-08-23: equal-Uses boxes stacked and the merge
			// kept ONE pool - 10+10 read as x2 but spent as 10). Stacker rides base Item, the
			// chain's root, so every stack path (cell spawn, pickup, trade) must be refused
			// here; boxes COMBINE via MergeIntoCarried instead
			return false;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ObjectCreatedEvent.ID
				|| ID == TakenEvent.ID
				|| ID == AddedToInventoryEvent.ID
				|| ID == EquippedEvent.ID
				|| ID == DroppedEvent.ID
				|| ID == BeforeDestroyObjectEvent.ID
				|| ID == PooledEvent<GetDisplayNameEvent>.ID;
		}

		public override bool HandleEvent(ObjectCreatedEvent E)
		{
			if (Uses < 0)
			{
				Uses = Stat.Random(8, 12);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			if (Uses >= 0 && ParentObject.IsReal && ParentObject.IsTakeable())
			{
				E.AddTag("{{y|(" + Uses + ")}}");
				// self-heal for the button count: the ability entry's DisplayName SERIALIZES,
				// so a fresh load shows the previous session's stale total until something
				// syncs; any render of a carried box (inventory open, message line) fixes it
				GameObject who = ParentObject.InInventory ?? ParentObject.Equipped;
				if (who != null)
				{
					SyncAbilityName(who);
				}
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(TakenEvent E)
		{
			EnsureGrant(E.Actor);
			return base.HandleEvent(E);
		}

		// ReceiveObject arrivals (wish grants, trades, containers) fire AddedToInventoryEvent,
		// never TakenEvent - the EB pickup lesson (2026-08-12) applying here: without this, a
		// granted box neither registers nor resyncs the button count
		public override bool HandleEvent(AddedToInventoryEvent E)
		{
			EnsureGrant(ParentObject.InInventory);
			return base.HandleEvent(E);
		}

		// equipping moves the box out of the Inventory part without firing Taken/Dropped;
		// re-sync so a box in hand keeps counting (live-test round 3: the 20 -> 10 button)
		public override bool HandleEvent(EquippedEvent E)
		{
			EnsureGrant(E.Actor);
			return base.HandleEvent(E);
		}

		/// <summary>Every chalk box the creature carries, pack AND hands - GetInventoryDirect
		/// alone made an equipped box invisible to the count, the fullest-box finder, and the
		/// heir list (live-test round 3).</summary>
		private static System.Collections.Generic.List<GameObject> CarriedBoxes(GameObject who, GameObject skip = null)
		{
			var found = new System.Collections.Generic.List<GameObject>();
			if (who == null)
			{
				return found;
			}
			foreach (GameObject item in who.GetInventoryDirectAndEquipment())
			{
				if (item != skip && item.HasPart<Cleo_TerraFirma_ChalkBox>())
				{
					found.Add(item);
				}
			}
			return found;
		}

		private void EnsureGrant(GameObject who)
		{
			if (who != null && who.IsCreature)
			{
				HealLegacyStack();
				if (MergeIntoCarried(who))
				{
					return; // this box poured itself into a carried one and is gone
				}
			}
			if (who != null && who.IsCreature && who.ActivatedAbilities != null)
			{
				ActivatedAbilityEntry existing = who.ActivatedAbilities.GetAbilityByCommand(DRAW_COMMAND);
				if (existing == null)
				{
					// icon = the box's own look; description per the base AddAbility idiom
					AbilityID = who.ActivatedAbilities.AddAbility("Draw with Chalk", DRAW_COMMAND, "Items",
						Description: "Draw a chalk line on an adjacent square.", // DRAFT (ability name + description)
						Silent: true,
						UITileDefault: new ConsoleLib.Console.Renderable(ParentObject.Render));
					// CommandEvent's cascade (17) NEVER reaches inventory (bugfix 2026-08-22, user:
					// "the ability doesn't seem to work") - the legacy string event is the delivery
					// path: CommandEvent.Send fires it on the actor when a part is registered, the
					// BleedingOnHit idiom
					who.RegisterPartEvent(this, DRAW_COMMAND);
				}
				else if (AbilityID == Guid.Empty && !AnyOtherBoxClaimsGrant(who))
				{
					// the ability exists but no living box owns it - the granting box was
					// destroyed without a DroppedEvent (crumbled at zero, burned, etc.), which
					// left a dead button behind; adopt the orphaned grant
					AbilityID = existing.ID;
					who.RegisterPartEvent(this, DRAW_COMMAND);
				}
				SyncAbilityName(who);
			}
		}

		/// <summary>Boxes COMBINE instead of stacking (user shape 2026-08-23: "ideally all chalk
		/// boxes will stack on to one, their charges combining"): a box arriving in a creature's
		/// possession pours its sticks into a box already carried and disappears. The survivor is
		/// the grant-holding box when one exists, so the registered command handler never moves.
		/// Returns true if this box merged away.</summary>
		private bool MergeIntoCarried(GameObject who)
		{
			if (Uses <= 0)
			{
				return false; // a spent husk has nothing to pour; the crumble path owns it
			}
			var others = CarriedBoxes(who, skip: ParentObject);
			if (others.Count == 0)
			{
				return false;
			}
			Cleo_TerraFirma_ChalkBox survivor = null;
			foreach (GameObject other in others)
			{
				Cleo_TerraFirma_ChalkBox part = other.GetPart<Cleo_TerraFirma_ChalkBox>();
				if (part.AbilityID != Guid.Empty)
				{
					survivor = part;
					break;
				}
			}
			if (survivor == null)
			{
				survivor = others[0].GetPart<Cleo_TerraFirma_ChalkBox>();
			}
			survivor.HealLegacyStack(); // never pour into an unhealed engine-stack
			int poured = Uses;
			survivor.Uses += poured;
			Uses = 0;
			if (who.IsPlayer())
			{
				IComponent<GameObject>.AddPlayerMessage("You tip the new chalk into your box."); // DRAFT
			}
			// the crumble idiom for removing a box mid-event, belt included (spent boxes once
			// lingered at zero; a merge husk carries the same risk)
			GameObject husk = ParentObject;
			bool destroyed = husk.Destroy();
			bool gone = !GameObject.Validate(husk) || husk.IsInGraveyard();
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK",
				$"box merge: +{poured} -> carried box now {survivor.Uses}, husk Destroy()={destroyed}, gone={gone}");
			if (!gone && husk.InInventory != null)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", "merge BELT: husk survived Destroy in an inventory; obliterating");
				husk.Obliterate();
			}
			SyncAbilityName(who);
			return true;
		}

		/// <summary>An engine-stack of boxes from before the no-stack rule keeps ONE pool for the
		/// whole stack. The player has been reading "(N) xK" as N sticks per box, so the heal
		/// grants exactly that: one box, Uses multiplied out by the stack count.</summary>
		private void HealLegacyStack()
		{
			Stacker stacker = ParentObject.Stacker;
			if (stacker != null && stacker.StackCount > 1)
			{
				int k = stacker.StackCount;
				Uses *= k;
				stacker.StackCount = 1;
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"legacy stack healed: x{k} -> one box, {Uses} uses");
			}
		}

		/// <summary>The button shows the carried total (user ask 2026-08-22: "show how much chalk
		/// you have on the draw with chalk button") - the EB Take Stones display idiom.</summary>
		public static void SyncAbilityName(GameObject who)
		{
			ActivatedAbilityEntry ent = who?.ActivatedAbilities?.GetAbilityByCommand(DRAW_COMMAND);
			if (ent == null)
			{
				return;
			}
			int total = 0;
			foreach (GameObject item in CarriedBoxes(who))
			{
				int uses = item.GetPart<Cleo_TerraFirma_ChalkBox>().Uses;
				if (uses > 0)
				{
					total += uses;
				}
			}
			ent.DisplayName = "Draw with Chalk (" + total + ")"; // DRAFT format
		}

		public override bool HandleEvent(DroppedEvent E)
		{
			ReleaseGrant(E.Actor);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeDestroyObjectEvent E)
		{
			// Destroy fires no DroppedEvent, so a box that crumbles (or burns) while carried
			// must release its grant here or the ability outlives every box that could serve it
			ReleaseGrant(ParentObject.InInventory ?? ParentObject.Equipped);
			return base.HandleEvent(E);
		}

		/// <summary>Hand the grant to another carried box if one remains, else revoke it.</summary>
		private void ReleaseGrant(GameObject who)
		{
			if (who == null || AbilityID == Guid.Empty)
			{
				return;
			}
			var others = CarriedBoxes(who, skip: ParentObject);
			who.UnregisterPartEvent(this, DRAW_COMMAND);
			if (others.Count > 0)
			{
				Cleo_TerraFirma_ChalkBox heir = others[0].GetPart<Cleo_TerraFirma_ChalkBox>();
				heir.AbilityID = AbilityID;
				who.RegisterPartEvent(heir, DRAW_COMMAND);
			}
			else
			{
				who.ActivatedAbilities?.RemoveAbility(AbilityID);
			}
			AbilityID = Guid.Empty;
			SyncAbilityName(who);
		}

		private bool AnyOtherBoxClaimsGrant(GameObject who)
		{
			foreach (GameObject item in CarriedBoxes(who, skip: ParentObject))
			{
				if (item.GetPart<Cleo_TerraFirma_ChalkBox>().AbilityID != Guid.Empty)
				{
					return true;
				}
			}
			return false;
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == DRAW_COMMAND)
			{
				GameObject actor = E.GetGameObjectParameter("Actor");
				if (actor != null)
				{
					if ((ParentObject.InInventory == actor || ParentObject.Equipped == actor) && Uses > 0)
					{
						AttemptDraw(actor);
					}
					else
					{
						// belt (live-test 2026-08-22): if the registered box is spent or gone,
						// delegate to a live carried box rather than eating the command. No
						// re-registration here - we are inside this event's dispatch; ownership
						// self-heals at the next take/drop/crumble
						Cleo_TerraFirma_ChalkBox live = FindCarriedBox(actor);
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK",
							$"draw re-route: registered box uses={Uses}, inInv={(ParentObject.InInventory == actor)}, live={(live != null)}");
						if (live != null && live != this)
						{
							live.AttemptDraw(actor);
						}
						else if (actor.IsPlayer())
						{
							Popup.ShowFail("You have no chalk."); // DRAFT
						}
					}
				}
			}
			return base.FireEvent(E);
		}

		private void AttemptDraw(GameObject actor)
		{
			if (Uses <= 0)
			{
				if (actor.IsPlayer())
				{
					Popup.ShowFail("The box is empty."); // DRAFT
				}
				return;
			}
			Cell target = PickDirection("Draw where?", actor); // DRAFT prompt (IComponent helper returns the cell)
			if (target == null || target == actor.CurrentCell)
			{
				return;
			}
			string dir = actor.CurrentCell.GetDirectionFromCell(target);
			if (target.HasWall() || target.HasObjectWithPart(nameof(Cleo_TerraFirma_ChalkLine)))
			{
				if (actor.IsPlayer())
				{
					Popup.ShowFail("There is no clear floor there to chalk."); // DRAFT
				}
				return;
			}
			GameObject line = GameObject.Create(LINE_BLUEPRINT);
			Cleo_TerraFirma_ChalkLine linePart = line.GetPart<Cleo_TerraFirma_ChalkLine>();
			if (linePart != null)
			{
				linePart.StrokeDir = dir;
			}
			// draw reveal (user shape 2026-08-23): the mask must exist BEFORE the line does --
			// it wears the cell's floor paint, so the juice queue's spin-up frames render
			// floor-over-floor and the stroke is born already hidden. Hold is the carve-swap
			// lesson; 0 means unseen/classic/unpainted and the draw lands as before.
			float settle = Cleo.TerraFirma.Scripts.Cleo_TerraFirma_ChalkRevealFX.Play(target);
			if (settle > 0f)
			{
				Cleo.TerraFirma.Scripts.Cleo_TerraFirma_ChalkRevealFX.Hold(settle);
			}
			target.AddObject(line);
			// no draw sfx by ruling 2026-08-22 (the tattoo-gun placeholder was removed; the
			// sound pass picks a real one if any)
			// the new piece reads its neighbors, and the neighbors reach back
			linePart?.Retile();
			Cleo_TerraFirma_ChalkLine.RetileNeighbors(target);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"line drawn {dir}, reveal {(settle > 0f ? "played" : "skipped")}, {Uses - 1} uses left");
			actor.UseEnergy(1000, "Item DrawChalk");
			ConsumeUse(actor);
		}

		/// <summary>Spend one stroke; the board's write action calls this too. Crumbles at zero.</summary>
		public void ConsumeUse(GameObject actor)
		{
			HealLegacyStack(); // a legacy engine-stack spends as one pool; make it whole first
			Uses--;
			GameObject box = ParentObject;
			GameObject holder = box.InInventory ?? actor;
			if (Uses <= 0)
			{
				if (actor != null && actor.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage("The last stick of chalk crumbles to dust."); // DRAFT
				}
				// instrumented (live-test 2026-08-22: spent boxes reported LINGERING at zero with
				// no exception in the log) - record what Destroy actually does, and force the
				// removal if it quietly leaves the corpse in an inventory
				bool destroyed = box.Destroy();
				bool gone = !GameObject.Validate(box) || box.IsInGraveyard();
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK",
					$"crumble: Destroy()={destroyed}, gone={gone}, context={(box?.InInventory != null ? "inventory" : box?.CurrentCell != null ? "cell" : "none")}");
				if (!gone && box.InInventory != null)
				{
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", "crumble BELT: spent box survived Destroy in an inventory; obliterating");
					box.Obliterate();
				}
			}
			SyncAbilityName(holder);
		}

		/// <summary>The fullest chalk box a creature carries (pack or hands), for target-side actions.</summary>
		public static Cleo_TerraFirma_ChalkBox FindCarriedBox(GameObject actor)
		{
			Cleo_TerraFirma_ChalkBox best = null;
			foreach (GameObject item in CarriedBoxes(actor))
			{
				Cleo_TerraFirma_ChalkBox box = item.GetPart<Cleo_TerraFirma_ChalkBox>();
				if (box.Uses > 0 && (best == null || box.Uses > best.Uses))
				{
					best = box;
				}
			}
			return best;
		}
	}
}
