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
		// internal (was private) so the harness's tfspore pure-read wish can peek without
		// duplicating the key or the decode table (2026-07-25)
		internal const string STATE_KEY = "Cleo_TerraFirma_FungalLottery";

		internal static readonly string[] GASES =
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
			// cross-reload diagnosis (bug caught 2026-07-24: four different permutations across one
			// save's boots despite the SetStringGameState persistence below -- somewhere the stored
			// state comes back empty after a load). The log now distinguishes ROLLED-FRESH from
			// LOADED so the next session names the broken link before we redesign the storage.
			bool rolledFresh = perm.IsNullOrEmpty() || perm.Length < GASES.Length;
			if (rolledFresh)
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
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SPORE", $"lottery: save permutation={perm} ({(rolledFresh ? "ROLLED FRESH - stored state was empty" : "loaded from game state")}), {ParentObject.Blueprint} (slot {Slot}) carries {gas}");
			GasGrenade grenade = ParentObject.GetPart<GasGrenade>();
			if (grenade != null)
				grenade.GasObject = gas;
			Cleo_TerraFirma_FallenRockPart rock = ParentObject.GetPart<Cleo_TerraFirma_FallenRockPart>();
			if (rock != null && rock.Payload == "Gas")
				rock.PayloadDetail = gas;
		}
	}
}
