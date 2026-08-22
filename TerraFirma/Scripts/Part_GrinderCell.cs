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
	/// stone value = weight x 25 charge (re-ruled from x20 same sitting), trickle 10/turn.
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
		public const int CHARGE_PER_POUND = 25; // re-ruled 2026-08-22 ("weight*25")
		public const int TRICKLE_PER_TURN = 10;

		/// <summary>Charge remaining in the stone currently being ground (weight x 25 at load).</summary>
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
		/// weight, LiquidVolume's own GetExtrinsicWeightEvent idiom. One pound leaves per 25
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
				if (stones.Count == 0)
				{
					if (E.Actor.IsPlayer())
					{
						Popup.ShowFail("You have no stone to feed it."); // DRAFT
					}
					return false;
				}
				GameObject pick;
				if (E.Actor.IsPlayer())
				{
					// stones differ wildly in worth (weight) and in regret (gems grind too), so
					// the player picks; NPCs take the heaviest
					List<string> options = new List<string>();
					foreach (GameObject stone in stones)
					{
						int w = stone.GetPart<Physics>()?.Weight ?? 0;
						options.Add(stone.DisplayName + " {{K|(" + w + "#)}}");
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
						Cleo.TerraFirma.Scripts.Helpers.VerifyLog("GRINDER", $"stone ground out, cell at {cell.Charge}/{cell.MaxCharge}");
					}
				}
				// cell full: the spiral idles and the stone keeps; no charge is wasted
			}
			return base.HandleEvent(E);
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
