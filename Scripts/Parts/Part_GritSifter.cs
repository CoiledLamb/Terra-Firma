using System;
using XRL.Rules;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_GritSifter : IPart
	{
		public const int SURFACE_Z = 10;
		public const int STRATA_PER_TIER = 5;

		public static readonly string[][] GEMS =
		{
			new[] { "Rough Agate" },
			new[] { "Rough Topaz" },
			new[] { "Rough Jasper" },
			new[] { "Rough Amethyst" },
			new[] { "Rough Sapphire" },
			new[] { "Rough Emerald", "Rough Peridot" },
		};

		public float Chance = 2f;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ImplantedEvent.ID
				|| ID == UnimplantedEvent.ID
				|| ID == KilledEvent.ID;
		}

		public override bool HandleEvent(ImplantedEvent E)
		{
			E.Implantee.RegisterEvent(this, KilledEvent.ID, Serialize: true);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnimplantedEvent E)
		{
			E.Implantee.UnregisterEvent(this, KilledEvent.ID);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(KilledEvent E)
		{
			OnKilled(E.Dying, E.Killer, E.Weapon, E.Projectile);
			return base.HandleEvent(E);
		}

		public GameObject OnKilled(GameObject Dying, GameObject Killer, GameObject Weapon, GameObject Projectile, bool Force = false)
		{
			if (Killer == null || Killer != ParentObject.Implantee || !IsDug(Dying, Weapon, Projectile))
				return null;
			if (!Force && Stat.Random(1, 10000) > (int)(CurrentChance(Killer) * 100f))
				return null;
			Cell cell = Dying.CurrentCell ?? Killer.CurrentCell;
			GameObject gem = GameObject.Create(GemFor(TierFor(cell?.ParentZone?.Z ?? SURFACE_Z)));
			if (gem == null)
				return null;
			Killer.ReceiveObject(gem);
			if (Killer.IsPlayer())
				IComponent<GameObject>.AddPlayerMessage("You sift " + gem.an() + " from the rubble.");
			return gem;
		}

		public float CurrentChance(GameObject Actor) => GetAvailableComputePowerEvent.AdjustUp(Actor, Chance);

		public static bool IsDug(GameObject Wall, GameObject Weapon, GameObject Projectile)
		{
			if (Wall == null || !Wall.IsWall() || Projectile != null || Weapon == null)
				return false;
			if (!Weapon.HasPart<Drill>() && !Weapon.HasPart<DiggingTool>())
				return false;
			return Cleo_TerraFirma_StoneshaperWallProperties.IsNaturalStone(Wall.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>());
		}

		public static int TierFor(int Z)
		{
			int depth = Math.Max(0, Z - SURFACE_Z);
			return Math.Min(GEMS.Length, 1 + Math.Max(0, depth - 1) / STRATA_PER_TIER);
		}

		public static string GemFor(int Tier)
		{
			string[] pool = GEMS[Math.Max(1, Math.Min(GEMS.Length, Tier)) - 1];
			return pool.GetRandomElement();
		}
	}
}
