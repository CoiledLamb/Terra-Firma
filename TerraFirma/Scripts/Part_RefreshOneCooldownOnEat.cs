using System;
using System.Collections.Generic;
using Cleo.TerraFirma.Scripts;
using XRL.Rules;

namespace XRL.World.Parts
{
	/// <summary>
	/// The mullstone's eaten inversion, RULED 2026-07-23 (supersedes the base
	/// RefreshAllCooldownsOnEat at its 25%-per-ability default -- which read as a no-op in
	/// play -- and the floated 100% -- "all cooldowns clear is vastly overcorrecting"):
	/// eating the ability-locker unlocks exactly ONE random ability currently cooling down.
	/// Shape mirrors the base part (OnEat string event, same player message) so the Lithovore
	/// eat channel's replayed event sequence fires it natively.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_RefreshOneCooldownOnEat : IPart
	{
		public override bool SameAs(IPart p)
		{
			return true;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return ID == GetShortDescriptionEvent.ID || base.WantEvent(ID, cascade);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			E.Postfix.AppendRules("When eaten, one activated ability's cooldown is refreshed.");
			return base.HandleEvent(E);
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("OnEat");
			base.Register(Object, Registrar);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "OnEat")
			{
				GameObject eater = E.GetGameObjectParameter("Eater");
				ActivatedAbilities abilities = eater?.ActivatedAbilities;
				if (abilities?.AbilityByGuid != null)
				{
					List<ActivatedAbilityEntry> cooling = new List<ActivatedAbilityEntry>();
					foreach (ActivatedAbilityEntry entry in abilities.AbilityByGuid.Values)
					{
						if (entry.Cooldown > 0)
							cooling.Add(entry);
					}
					if (cooling.Count > 0)
					{
						ActivatedAbilityEntry chosen = cooling[Stat.Random(0, cooling.Count - 1)];
						chosen.Refresh(this);
						Helpers.VerifyLog("ROCK", $"mullstone eat: refreshed {chosen.DisplayName} for {eater.Blueprint} ({cooling.Count} were cooling down)");
						if (eater.IsPlayer())
							IComponent<GameObject>.AddPlayerMessage("You suddenly feel ready to use " + chosen.DisplayName + " again.");
					}
					else
						Helpers.VerifyLog("ROCK", $"mullstone eat: {eater.Blueprint} had no abilities cooling down -- nothing to refresh");
				}
			}
			return base.FireEvent(E);
		}
	}
}
