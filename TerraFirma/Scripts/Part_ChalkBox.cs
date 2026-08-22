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
			if ((p as Cleo_TerraFirma_ChalkBox).Uses != Uses)
			{
				return false;
			}
			return base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ObjectCreatedEvent.ID
				|| ID == TakenEvent.ID
				|| ID == DroppedEvent.ID
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
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(TakenEvent E)
		{
			GameObject who = E.Actor;
			if (who != null && who.IsCreature && who.ActivatedAbilities != null
				&& who.ActivatedAbilities.GetAbilityByCommand(DRAW_COMMAND) == null)
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
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(DroppedEvent E)
		{
			GameObject who = E.Actor;
			if (who != null && AbilityID != Guid.Empty)
			{
				// hand the grant to another carried box if one remains, else revoke
				var others = who.GetInventoryDirect((GameObject go) => go != ParentObject && go.HasPart<Cleo_TerraFirma_ChalkBox>());
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
			}
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == DRAW_COMMAND)
			{
				GameObject actor = E.GetGameObjectParameter("Actor");
				if (actor != null && ParentObject.InInventory == actor)
				{
					AttemptDraw(actor);
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
			target.AddObject(line);
			actor.PlayWorldSound("Sounds/Interact/sfx_interact_tattoo"); // PLACEHOLDER sound (tattoo gun; closest base cousin) - sound pass flagged
			// the new piece reads its neighbors, and the neighbors reach back
			linePart?.Retile();
			Cleo_TerraFirma_ChalkLine.RetileNeighbors(target);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("CHALK", $"line drawn {dir}, {Uses - 1} uses left");
			actor.UseEnergy(1000, "Item DrawChalk");
			ConsumeUse(actor);
		}

		/// <summary>Spend one stroke; the board's write action calls this too. Crumbles at zero.</summary>
		public void ConsumeUse(GameObject actor)
		{
			Uses--;
			if (Uses <= 0)
			{
				if (actor != null && actor.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage("The last stick of chalk crumbles to dust."); // DRAFT
				}
				ParentObject.Destroy();
			}
		}

		/// <summary>The fullest chalk box a creature carries, for target-side actions.</summary>
		public static Cleo_TerraFirma_ChalkBox FindCarriedBox(GameObject actor)
		{
			Cleo_TerraFirma_ChalkBox best = null;
			foreach (GameObject item in actor.GetInventoryDirect((GameObject go) => go.HasPart<Cleo_TerraFirma_ChalkBox>()))
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
