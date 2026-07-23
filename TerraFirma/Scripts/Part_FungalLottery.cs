using System;
using System.Collections.Generic;
using System.Text;
using XRL.Rules;

namespace XRL.World.Parts
{
	/// <summary>
	/// The fungal rocks' lottery (user ruling 2026-07-22): which of the four fungal rocks carries
	/// which spore infection is shuffled once per save and held constant for that run — the rocks'
	/// names deliberately say nothing about it. On creation, reads (or rolls) the save's permutation
	/// and points the rock's GasGrenade and step payload at its assigned gas. Blueprint defaults
	/// remain as fallback when no game is loaded.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_FungalLottery : IPart
	{
		private const string STATE_KEY = "Cleo_TerraFirma_FungalLottery";

		private static readonly string[] GASES =
		{
			"FungalSporeGasPuff",
			"FungalSporeGasLuminous",
			"FungalSporeGasWax",
			"FungalSporeGasMumbles"
		};

		// Cuisine tells (user ruling 2026-07-22): the infection is discoverable through the POT --
		// each infection's rock carries domains hinting at what it is, indexed parallel to GASES:
		//   Puff (fickle gill): fungal effects only.
		//   Luminous (hoarshroom): cold or fungus -- the base freeze-dried hoarshrooms' EXACT type string.
		//   Wax (waxshroom): fungus only FOR NOW -- "acts like you ate wax" is OPEN: no wax-eating
		//     effect exists anywhere in base (verified); the wax half awaits a user definition.
		//   Mumbles (mumble mouth): fungus with a LOW chance of secrets (comma lists pick one random
		//     option, so duplicates are weights: 3x fungus + 1x secrets = 25%; DRAFT dial).
		private static readonly string[] DOMAINS =
		{
			"fungus",
			"cold,fungus",
			"fungus",
			"fungus,fungus,fungus,secrets"
		};

		/// <summary>This rock's ticket: index 0-3 into the save's shuffled gas order.</summary>
		public int Slot;

		public override bool SameAs(IPart p)
		{
			return p is Cleo_TerraFirma_FungalLottery o && o.Slot == Slot && base.SameAs(p);
		}

		public override void Initialize()
		{
			base.Initialize();
			try
			{
				AssignGas();
			}
			catch (Exception)
			{
				// no game state available (samples, chargen previews): blueprint defaults stand
			}
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == ObjectCreatedEvent.ID;
		}

		// Domain restamp lives in ObjectCreated, NOT Initialize: the PreparedCookingIngredient's own
		// ObjectCreated handler auto-writes the item description from the type ("Adds cold or
		// fungus-based effects...") which would print the lottery on the label. This part must sit
		// AFTER the PCI in the blueprint so its handler runs LAST: restamp the true domains, then
		// force the description back to neutral -- the tell stays experiential (cook and find out).
		public override bool HandleEvent(ObjectCreatedEvent E)
		{
			try
			{
				AssignDomains();
			}
			catch (Exception)
			{
				// blueprint default ("fungus") stands
			}
			return base.HandleEvent(E);
		}

		private void AssignDomains()
		{
			XRLGame game = XRL.The.Game;
			if (game == null || Slot < 0 || Slot >= DOMAINS.Length)
				return;
			string perm = game.GetStringGameState(STATE_KEY);
			if (perm.IsNullOrEmpty() || perm.Length < DOMAINS.Length)
				return; // AssignGas rolls the permutation; if it never ran, leave defaults alone
			PreparedCookingIngredient pci = ParentObject.GetPart<PreparedCookingIngredient>();
			if (pci == null)
				return;
			pci.type = DOMAINS[perm[Slot] - '0'];
			pci.descriptionPostfix = "Adds fungus-based effects to cooked meals."; // no label tells
		}

		private void AssignGas()
		{
			XRLGame game = XRL.The.Game;
			if (game == null || Slot < 0 || Slot >= GASES.Length)
				return;
			string perm = game.GetStringGameState(STATE_KEY);
			if (perm.IsNullOrEmpty() || perm.Length < GASES.Length)
			{
				List<int> pool = new List<int> { 0, 1, 2, 3 };
				StringBuilder sb = new StringBuilder();
				while (pool.Count > 0)
				{
					int i = Stat.Random(0, pool.Count - 1);
					sb.Append((char)('0' + pool[i]));
					pool.RemoveAt(i);
				}
				perm = sb.ToString();
				game.SetStringGameState(STATE_KEY, perm);
			}
			string gas = GASES[perm[Slot] - '0'];
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SPORE", $"lottery: save permutation={perm}, {ParentObject.Blueprint} (slot {Slot}) carries {gas}");
			GasGrenade grenade = ParentObject.GetPart<GasGrenade>();
			if (grenade != null)
				grenade.GasObject = gas;
			Cleo_TerraFirma_FallenRockPart rock = ParentObject.GetPart<Cleo_TerraFirma_FallenRockPart>();
			if (rock != null && rock.Payload == "Gas")
				rock.PayloadDetail = gas;
		}
	}
}
