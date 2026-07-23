using System;
using Cleo.TerraFirma.Scripts;
using XRL.Messages;
using XRL.World;
using XRL.World.Parts;

namespace XRL.World.Effects
{
	/// <summary>
	/// The burp-family eaten status (user ruling 2026-07-22, name deliberately NOT "burp" anything;
	/// "roiling innards" is a DRAFT for the text pass): eating a gas-bearing stone leaves the gas
	/// settling in you for 50-250 turns, and every so often some of it finds the way out -- a small
	/// cloud of the stone's own gas at your feet. Conchalk passes "Random" and rolls the aji-conch
	/// table fresh per emission, as is its whole personality. Gas does not discriminate; neither do
	/// your insides.
	/// Numbers (all DRAFT): spew every 8 turns, density 30 (between the step-proc 40 and nothing).
	/// Re-eating refreshes/extends rather than stacking; the newest stone's gas takes over.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GasBelly : Effect
	{
		public string GasBlueprint = "ConfusionGas";

		public int SpewEvery = 8;

		public int SpewDensity = 30;

		/// <summary>The gas you are CURRENTLY producing (ruling 2026-07-22): while roiling, that gas
		/// cannot affect you -- your insides already surrendered to it. For conchalk ("Random") this
		/// tracks the most recent venting, so the protection rotates with each emission.</summary>
		public string CurrentGas;

		private int Counter;

		public Cleo_TerraFirma_GasBelly()
		{
			DisplayName = "{{w|roiling innards}}"; // DRAFT name
		}

		public Cleo_TerraFirma_GasBelly(int Duration, string GasBlueprint) : this()
		{
			base.Duration = Duration;
			this.GasBlueprint = GasBlueprint;
			if (GasBlueprint != "Random")
				CurrentGas = GasBlueprint; // fixed-gas stones protect from the first swallow
		}

		public override string GetDetails()
		{
			return "Something mineral is still settling. Periodically vents gas.\nThe gas currently venting can't affect you."; // DRAFT
		}

		public override bool UseStandardDurationCountdown() => true;

		public override bool Apply(GameObject Object)
		{
			if (Object.GetEffect<Cleo_TerraFirma_GasBelly>() is Cleo_TerraFirma_GasBelly prior)
			{
				// refresh, don't stack: the newest stone's gas takes over, the longer clock stands
				prior.Duration = Math.Max(prior.Duration, Duration);
				prior.GasBlueprint = GasBlueprint;
				if (GasBlueprint != "Random")
					prior.CurrentGas = GasBlueprint;
				Helpers.VerifyLog("BELLY", $"refresh (no stack): gas now {GasBlueprint}, duration {prior.Duration}");
				return false;
			}
			Helpers.VerifyLog("BELLY", $"applied to {Object.Blueprint}: gas={GasBlueprint}, duration={Duration}, spew every {SpewEvery} at density {SpewDensity}");
			return true;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<EndTurnEvent>.ID
				|| ID == PooledEvent<CheckGasCanAffectEvent>.ID;
		}

		// the resistance half (ruling 2026-07-22): the gas you're currently venting can't touch you.
		// Every base gas behavior gates through CheckGasCanAffectEvent (decompiled IsAffectable),
		// so blueprint-matching here covers your own vents AND ambient clouds of the same gas.
		public override bool HandleEvent(CheckGasCanAffectEvent E)
		{
			if (E.Object == base.Object && E.GasObject != null
				&& !string.IsNullOrEmpty(CurrentGas) && E.GasObject.Blueprint == CurrentGas)
			{
				Helpers.VerifyLog("BELLY", $"gas immunity: {CurrentGas} cannot affect the roiling bearer");
				return false;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (++Counter >= SpewEvery)
			{
				Counter = 0;
				Spew();
			}
			return base.HandleEvent(E);
		}

		private void Spew()
		{
			GameObject bearer = base.Object;
			if (bearer?.CurrentCell == null)
				return;
			string bp = GasBlueprint;
			if (bp == "Random")
				bp = PopulationManager.GenerateOne("DynamicObjectsTable:AjiConch")?.Blueprint;
			if (string.IsNullOrEmpty(bp))
				return;
			CurrentGas = bp; // the protection follows the product
			Helpers.VerifyLog("BELLY", $"vent: {bp} at density {SpewDensity} (protection now covers {bp}; {Duration} turns left)");
			GameObject gasObj = GameObject.Create(bp);
			Gas gas = gasObj.GetPart<Gas>();
			if (gas != null)
			{
				gas.Creator = bearer;
				gas.Density = SpewDensity;
			}
			bearer.CurrentCell.AddObject(gasObj);
			if (bearer.IsPlayer())
				MessageQueue.AddPlayerMessage("Gas roils out of you.");
		}
	}
}
