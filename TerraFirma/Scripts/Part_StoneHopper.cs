using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;

namespace XRL.World.Parts
{
	/// <summary>
	/// The millstone's stone hopper (HOPPER RULING, user 2026-07-24: "we could just have the
	/// millstone grind stones from it's inventory, which is it's normal process" / "menu probably
	/// goes"). Drop stones INTO the millstone and, while the mill is powered and turning, it
	/// grinds one stone per turn into servings of the matching ground powder, which accumulate
	/// in the millstone's inventory -- the base Joppa mill's own vinewafer workflow, extended to
	/// rocks. Replaces the menu grind on the mill home entirely; the native power gate does the
	/// work the old hidden-option check faked (a dead mill just doesn't grind its hopper).
	/// Recipe shared with the pestle/campfire grind (Part_StoneMill.MakePowder + the same
	/// 2-3 servings per rock, 5-6 per boulder). Throughput 1 stone/turn: DRAFT dial.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_StoneHopper : IPart
	{
		private const string STONE_TAG = "Cleo_TerraFirma_IsAnyStone";

		public override bool SameAs(IPart p) => p is Cleo_TerraFirma_StoneHopper && base.SameAs(p);

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("EndTurn"); // the native Mill's own cadence
			base.Register(Object, Registrar);
		}

		/// <summary>The mill turns only under power -- the sibling Mill part's own readiness check.</summary>
		private bool MillTurning()
		{
			if (ParentObject?.GetPart<Mill>() is Mill mill)
				return mill.IsReady(UseCharge: false, IgnoreCharge: false, IgnoreLiquid: false, IgnoreBootSequence: false, IgnoreBreakage: false, IgnoreRust: false, IgnoreEMP: false, IgnoreRealityStabilization: false, IgnoreSubject: false, IgnoreLocallyDefinedFailure: false, 1, null, UseChargeIfUnpowered: false, 0L);
			return false; // no Mill part = nothing turns the stone
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "EndTurn" && MillTurning())
				GrindOne();
			return base.FireEvent(E);
		}

		private void GrindOne()
		{
			List<GameObject> hopper = ParentObject.Inventory?.GetObjectsDirect();
			if (hopper == null)
				return;
			GameObject rock = null;
			foreach (GameObject item in hopper)
			{
				if (item.HasTag(STONE_TAG))
				{
					rock = item;
					break;
				}
			}
			if (rock == null)
				return;
			string sourceBlueprint = rock.Blueprint ?? "";
			string sourceName = rock.Render?.DisplayName ?? "stone";
			string sourceType = rock.GetPart<PreparedCookingIngredient>()?.type;
			bool boulder = rock.GetBlueprint()?.DescendsFrom("BaseBoulder") ?? false;
			GameObject single = rock.Count > 1 ? rock.RemoveOne() : rock;
			int servings = boulder ? Stat.Random(5, 6) : Stat.Random(2, 3);
			GameObject powder = null;
			for (int s = 0; s < servings; s++)
			{
				powder = Cleo_TerraFirma_StoneMill.MakePowder(sourceBlueprint, sourceName, sourceType);
				ParentObject.ReceiveObject(powder); // products stay in the hopper for collection
			}
			if (GameObject.Validate(single))
				single.Destroy(null, Silent: true);
			ParentObject.PlayWorldSound("sfx_damage_stone");
			Helpers.VerifyLog("GRIND", $"hopper: {ParentObject.Blueprint} milled 1x {sourceBlueprint} -> {servings} serving(s) of {powder?.Blueprint ?? "nothing"}{(boulder ? " (boulder rate)" : "")}");
			if (Visible())
				IComponent<GameObject>.AddPlayerMessage(ParentObject.The + ParentObject.DisplayNameOnly + ParentObject.GetVerb("mill") + " " + sourceName + " into " + (powder?.Render?.DisplayName ?? "rock flour") + "."); // native mill message shape
		}
	}
}
