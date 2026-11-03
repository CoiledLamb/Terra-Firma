using System;
using System.Linq;
using XRL.Rules;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_MeltedStatue : IPart
	{
		public const string NAME = "melted statue";
		public const string TILE = "melted_statue.png";
		public const string DESCRIPTION = "Liquid vitriol has made a termite's nest out of once fine artistry. The shapes and curvature of stone are now unrecognizable.";

		public const string PUDDLE = "Cleo_TerraFirma_ShallowAcidPuddle";

		public string Splash;
		public int SplashRadius = 3;

		public override bool SameAs(IPart p) => true;

		public static bool IsMelted(GameObject Object) => Object?.HasPart<Cleo_TerraFirma_MeltedStatue>() == true;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == AfterObjectCreatedEvent.ID
				|| ID == ZoneBuiltEvent.ID
				|| ID == PooledEvent<GetPointsOfInterestEvent>.ID;
		}

		public override bool HandleEvent(GetPointsOfInterestEvent E)
		{
			E.List.RemoveAll(p => IsMelted(p.Object));
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ZoneBuiltEvent E)
		{
			if (!Splash.IsNullOrEmpty())
			{
				int n = Splash.RollCached();
				Splash = null;
				SplashPuddles(n);
			}
			return base.HandleEvent(E);
		}

		public int SplashPuddles(int Count)
		{
			Cell here = ParentObject.CurrentCell;
			Zone z = here?.ParentZone;
			if (z == null)
				return 0;
			var open = z.GetCells(c => c != here && !c.IsSolid() && c.DistanceTo(here) <= SplashRadius
				&& !c.Objects.Any(o => o.HasPart<LiquidVolume>())).ShuffleInPlace();
			int placed = 0;
			foreach (Cell c in open)
			{
				if (placed >= Count)
					break;
				c.AddObject(PUDDLE);
				placed++;
			}
			return placed;
		}

		public override bool HandleEvent(AfterObjectCreatedEvent E)
		{
			Undress();
			return base.HandleEvent(E);
		}

		public void Undress()
		{
			Render render = ParentObject.Render;
			if (render != null)
			{
				render.DisplayName = NAME;
				render.Tile = TILE;
				render.RenderString = "|";
			}
			Description description = ParentObject.GetPart<Description>();
			if (description != null)
				description._Short = DESCRIPTION;
			ParentObject.RemovePart<Lovely>();
			ParentObject.SetImportant(false, force: true);
		}
	}
}
