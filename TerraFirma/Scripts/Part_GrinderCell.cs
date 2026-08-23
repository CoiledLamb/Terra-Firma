using System;
using System.Collections.Generic;
using XRL.UI;

namespace XRL.World.Parts
{
	/// <summary>
	/// The grinder cell (design sitting 2026-08-22; description USER TEXT FINAL). User shape:
	/// "a high capacity cousin of the fidget cell... higher max charge, but harder to fill up.
	/// stones have weight, boulders especially, and grinding takes time." Rulings same sitting:
	/// grinds anywhere (no installed-in-device requirement) · trickle, not burst · no boulder
	/// cap ("no need for boulder cap" - a 300-lb slab is 600 turns of grinding, its own tax) ·
	/// mill law inapplicable (no armed stones shipped). Numbers user-approved 2026-08-22:
	/// stone value = weight x 50 charge (re-ruled 2026-08-23 with the cap drop to 25k; was
	/// x25, before that x20), trickle 10/turn.
	/// Feed idiom = the lantern socket pattern, one stone at a time, ground to nothing - no
	/// unload (a half-ground stone is not a thing; Claude call). Grinding PAUSES while the cell
	/// is full so no stone is wasted (Claude call). Diet gate = Cleo_TerraFirma_IsAnyStone
	/// (the geomancy/mill tag); gems grind at their weight, wastefully, comedy intact per the
	/// mill precedent.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GrinderCell : IPart
	{
		public const string FEED_COMMAND = "Cleo_TerraFirma_FeedGrinderCell";
		public const string STONE_TAG = "Cleo_TerraFirma_IsAnyStone";
		public const int CHARGE_PER_POUND = 50; // re-ruled 2026-08-23 ("raise the grind to *50 charge")
		public const int TRICKLE_PER_TURN = 10;

		/// <summary>Charge remaining in the stone currently being ground (weight x 50 at load).</summary>
		public int StoneChargeLeft;

		/// <summary>Display name of the stone being ground, for the status tag.</summary>
		public string StoneName;

		public override bool SameAs(IPart p)
		{
			Cleo_TerraFirma_GrinderCell o = p as Cleo_TerraFirma_GrinderCell;
			if (o.StoneChargeLeft != StoneChargeLeft || o.StoneName != StoneName)
			{
				return false;
			}
			return base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<EndTurnEvent>.ID
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID
				|| ID == PooledEvent<GetDisplayNameEvent>.ID
				|| ID == GetExtrinsicWeightEvent.ID;
		}

		/// <summary>Pounds of stone not yet ground away, derived from the fuel pool.</summary>
		public int RemainingStoneWeight()
		{
			return (StoneChargeLeft + CHARGE_PER_POUND - 1) / CHARGE_PER_POUND;
		}

		/// <summary>The waterskin law (user ruling 2026-08-22: "preserve the weight of what its
		/// grinding and steadily decrement that over time"): the cell carries its stone's unground
		/// weight, LiquidVolume's own GetExtrinsicWeightEvent idiom. One pound leaves per 50
		/// charge ground out - a fed boulder is heavy until the spiral has actually eaten it.</summary>
		public override bool HandleEvent(GetExtrinsicWeightEvent E)
		{
			E.Weight += RemainingStoneWeight();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (StoneChargeLeft <= 0)
			{
				E.AddAction("Feed", "feed a stone", FEED_COMMAND, null, 'f', FireOnActor: false);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == FEED_COMMAND && E.Actor != null)
			{
				// LOOSE INVENTORY ONLY (bugfix 2026-08-22, user repro: feeding the equipped
				// sculpted pebble destroyed it in the thrown slot, leaving a stale reference
				// that made EarthenBarrage's pickup re-arm bail forever at its
				// GetFirstThrownWeapon() != null guard). Equipped stones are not offered, and
				// Temporary (conjured) stones are excluded outright - they vanish on their own
				// and would otherwise be a free-energy loop. GetInventoryDirect = the mill's
				// own candidate idiom (Part_StoneMill).
				List<GameObject> stones = E.Actor.GetInventoryDirect((GameObject go) =>
					go.HasTagOrProperty(STONE_TAG) && go != ParentObject && !go.HasPart<Temporary>());
				int carried = stones.Count;
				// ADJACENCY FEED (user ruling 2026-08-23: "should pull stones from adjacency
				// rather than just your inventory, so selecting feed a stone knows if a boulder
				// is in the tile next to you"): stones on your own tile and the eight neighbors
				// join the picker and grind IN PLACE - a boulder never has to be lifted. Walls
				// refuse (a tagged wall is terrain, not a meal), creatures refuse, owned stones
				// are skipped rather than silently vandalized (dial), conjured stones excluded
				// as ever. Weight conservation stands as ruled: a fed boulder makes the cell
				// heavy until the spiral has actually eaten it.
				Cell origin = E.Actor.CurrentCell;
				if (origin != null)
				{
					List<Cell> reach = origin.GetLocalAdjacentCells();
					reach.Insert(0, origin);
					foreach (Cell c in reach)
					{
						foreach (GameObject go in c.Objects)
						{
							if (go.HasTagOrProperty(STONE_TAG) && go != ParentObject && !go.HasPart<Temporary>()
								&& !go.IsCreature && !go.IsWall()
								&& (go.GetPart<Physics>()?.Owner).IsNullOrEmpty())
							{
								stones.Add(go);
							}
						}
					}
				}
				if (stones.Count == 0)
				{
					if (E.Actor.IsPlayer())
					{
						Popup.ShowFail("There is no stone in reach to feed it."); // DRAFT (was inventory-only wording)
					}
					return false;
				}
				GameObject pick;
				if (E.Actor.IsPlayer())
				{
					// stones differ wildly in worth (weight) and in regret (gems grind too), so
					// the player picks; NPCs take the heaviest
					List<string> options = new List<string>();
					for (int i = 0; i < stones.Count; i++)
					{
						int w = stones[i].GetPart<Physics>()?.Weight ?? 0;
						options.Add(stones[i].DisplayName + " {{K|(" + w + "#)}}"
							+ (i >= carried ? " {{K|(nearby)}}" : "")); // DRAFT marker
					}
					int choice = Popup.PickOption(
						Intro: "Feed which stone?", // DRAFT
						Options: options.ToArray(),
						AllowEscape: true);
					if (choice < 0)
					{
						return false;
					}
					pick = stones[choice];
				}
				else
				{
					pick = stones[0];
					foreach (GameObject stone in stones)
					{
						if ((stone.GetPart<Physics>()?.Weight ?? 0) > (pick.GetPart<Physics>()?.Weight ?? 0))
						{
							pick = stone;
						}
					}
				}
				pick = pick.SplitFromStack() ?? pick;
				int weight = pick.GetPart<Physics>()?.Weight ?? 0;
				StoneChargeLeft = Math.Max(weight, 1) * CHARGE_PER_POUND;
				StoneName = pick.DisplayName;
				SyncGrindAnimation(true);
				ParentObject.FlushWeightCaches(); // the stone's weight moves into the cell, not into thin air
				if (E.Actor.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage("You feed " + pick.t() + " into " + ParentObject.t() + ", and the spiral begins to churn."); // DRAFT
				}
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GRINDER", $"fed {pick.Blueprint} weight={weight} pool={StoneChargeLeft}");
				pick.Destroy();
				E.Actor.UseEnergy(1000, "Item FeedGrinderCell");
				E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (StoneChargeLeft > 0)
			{
				EnergyCell cell = ParentObject.GetPart<EnergyCell>();
				if (cell != null && cell.Charge < cell.MaxCharge)
				{
					int tick = Math.Min(TRICKLE_PER_TURN, Math.Min(StoneChargeLeft, cell.MaxCharge - cell.Charge));
					int weightBefore = RemainingStoneWeight();
					cell.AddCharge(tick);
					StoneChargeLeft -= tick;
					if (RemainingStoneWeight() != weightBefore)
					{
						ParentObject.FlushWeightCaches(); // a pound ground away
					}
					if (StoneChargeLeft <= 0)
					{
						StoneName = null;
						SyncGrindAnimation(false);
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GRINDER", $"stone ground out, cell at {cell.Charge}/{cell.MaxCharge}");
					}
				}
				// cell full: the spiral idles and the stone keeps; no charge is wasted
			}
			return base.HandleEvent(E);
		}

		/// <summary>Churning spiral while grinding (user 2026-08-22: "set up animated sprite
		/// while grinding ... using the second sprite i just exported"): AnimatedMaterialGeneric
		/// tile frames alternating grinder_cell.png / grinder_cell2.png, added at feed and
		/// removed when the stone grinds out.</summary>
		private void SyncGrindAnimation(bool grinding)
		{
			AnimatedMaterialGeneric anim = ParentObject.GetPart<AnimatedMaterialGeneric>();
			if (grinding && anim == null)
			{
				anim = ParentObject.AddPart<AnimatedMaterialGeneric>();
				anim.AnimationLength = 60;
				anim.TileAnimationFrames = "0=grinder_cell.png,30=grinder_cell2.png";
			}
			else if (!grinding && anim != null)
			{
				ParentObject.RemovePart(anim);
				Render render = ParentObject.GetPart<Render>();
				if (render != null)
				{
					render.Tile = "grinder_cell.png";
				}
			}
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			if (StoneChargeLeft > 0 && ParentObject.IsReal)
			{
				E.AddTag("{{y|(grinding)}}");
			}
			return base.HandleEvent(E);
		}
	}
}
