using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleLib.Console;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_GraveyardOrbitUplink : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_CommandGraveyardOrbitUplink";
		public const string MARK = "Cleo_TerraFirma_OrbitalMark";
		public const string ICON = "graveyard_orbit_uplink_ability.png";

		public string commandId = "";
		public Guid ActivatedAbilityID = Guid.Empty;

		public int Cooldown = 200;
		public int Range = 12;
		public int Radius = 3;
		public int Delay = 3;
		public string Damage = "3d6";
		public int ConcussionLevel = 4;

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public int GetCooldown(GameObject Actor) => GetAvailableComputePowerEvent.AdjustDown(Actor, Cooldown);

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == ImplantedEvent.ID
				|| ID == UnimplantedEvent.ID
				|| ID == PooledEvent<CommandEvent>.ID
				|| ID == GetShortDescriptionEvent.ID;
		}

		public override bool HandleEvent(ImplantedEvent E)
		{
			ActivatedAbilityID = E.Implantee.AddDynamicCommand(out commandId, COMMAND, "Call Down Debris", "Cybernetics",
				Description: "You call down debris on an area within 12 squares. 3 rounds later, it deals 3d6 damage, throwing creatures back and stunning them. Can only be activated under open sky.", Icon: "*");
			ActivatedAbilityEntry ability = E.Implantee.GetActivatedAbility(ActivatedAbilityID);
			if (ability != null)
				ability.UITileDefault = new ConsoleLib.Console.Renderable(ICON, " ", "&c", "&c", 'r');
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(UnimplantedEvent E)
		{
			E.Implantee.RemoveActivatedAbility(ref ActivatedAbilityID);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			E.Postfix.AppendRules("Compute power on the local lattice decreases this implant's cooldown.");
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command == commandId && !commandId.IsNullOrEmpty() && E.Actor != null && E.Actor == ParentObject.Implantee && E.Actor.IsPlayer())
				Attempt(E.Actor);
			return base.HandleEvent(E);
		}

		public static bool OpenSky(Cell C)
		{
			Zone z = C?.ParentZone;
			return z != null && !z.IsWorldMap() && z.IsOutside();
		}

		private void Attempt(GameObject actor)
		{
			if (!OpenSky(actor.CurrentCell))
			{
				Popup.ShowFail("There's too much rock between you and the sky.");
				return;
			}
			Cell here = actor.CurrentCell;
			Cell center = PickTarget.ShowPicker(PickTarget.PickStyle.Circle, Radius, Range, here.X, here.Y, Locked: false, AllowVis.OnlyVisible, Label: "Call Down Debris");
			if (center == null)
				return;
			if (!OpenSky(center))
			{
				Popup.ShowFail("There's too much rock between there and the sky.");
				return;
			}
			if (Call(actor, center) == null)
				return;
			actor.CooldownActivatedAbility(ActivatedAbilityID, GetCooldown(actor));
			actor.UseEnergy(1000, "Cybernetics Graveyard Orbit Uplink");
			IComponent<GameObject>.AddPlayerMessage("You transmit a string of codes skyward. Something overhead comes loose.");
		}

		public GameObject Call(GameObject owner, Cell center)
		{
			if (center?.ParentZone == null)
				return null;
			Cell spot = center;
			if (spot.IsSolid())
				foreach (Cell c in ZoneCells(center, Radius))
					if (!c.IsSolid() && (spot == center || c.PathDistanceTo(center) < spot.PathDistanceTo(center)))
						spot = c;
			GameObject hub = GameObject.Create(MARK);
			if (hub == null)
				return null;
			if (hub.Render != null)
				hub.Render.Visible = false;
			spot.AddObject(hub);
			hub.SetIntProperty(Cleo_TerraFirma_OrbitalStrike.CENTER_X, center.X);
			hub.SetIntProperty(Cleo_TerraFirma_OrbitalStrike.CENTER_Y, center.Y);
			hub.AddPart(new Cleo_TerraFirma_OrbitalStrike
			{
				Owner = owner,
				Countdown = Delay,
				Radius = Radius,
				Damage = Damage,
				Level = ConcussionLevel,
				LastTurn = The.Game?.Turns ?? 0,
			});
			return hub;
		}

		public static List<Cell> ZoneCells(Cell center, int r)
		{
			var list = new List<Cell>();
			Zone z = center?.ParentZone;
			if (z == null)
				return list;
			for (int dy = -r; dy <= r; dy++)
				for (int dx = -r; dx <= r; dx++)
				{
					if (!(Math.Sqrt(dx * dx + dy * dy) <= r))
						continue;
					Cell c = z.GetCell(center.X + dx, center.Y + dy);
					if (c != null)
						list.Add(c);
				}
			return list;
		}
	}

	[Serializable]
	public class Cleo_TerraFirma_OrbitalStrike : IPart
	{
		public const string BOULDER = "SmallBoulder";
		public const string MEDIUM_BOULDER = "MediumBoulder";
		public const string LARGE_BOULDER = "LargeBoulder";
		public const string RUBBLE = "Cleo_TerraFirma_Rock_Rubble";
		public const string PEBBLES = "Cleo_TerraFirma_Rock_Pebbles";
		public static readonly string[] DEBRIS = { LARGE_BOULDER, MEDIUM_BOULDER, BOULDER, RUBBLE, PEBBLES };

		public const string CENTER_X = "Cleo_TerraFirma_StrikeCenterX";
		public const string CENTER_Y = "Cleo_TerraFirma_StrikeCenterY";
		public const int PaintDuration = 2500;

		public GameObject Owner;
		public int Countdown = 3;
		public int Radius = 3;
		public string Damage = "3d6";
		public int Level = 4;
		public int InnerRubble = 60;
		public int OuterRubble = 25;
		public long LastTurn;

		public override void Attach()
		{
			ParentObject.Flags |= GameObject.FLAG_ALWAYS_PAINT;
		}

		public Cell GetCenter()
		{
			Cell here = ParentObject?.CurrentCell;
			int x = ParentObject?.GetIntProperty(CENTER_X, -1) ?? -1;
			int y = ParentObject?.GetIntProperty(CENTER_Y, -1) ?? -1;
			if (here == null || x < 0 || y < 0)
				return here;
			return here.ParentZone.GetCell(x, y) ?? here;
		}

		public override void OnPaint(ScreenBuffer SB)
		{
			Cell center = GetCenter();
			Zone active = The.ZoneManager?.ActiveZone;
			if (center == null || center.ParentZone != active || SB == null)
				return;
			int rings = Radius + 1;
			int ring = Radius - (int)(IComponent<GameObject>.frameTimerMS % PaintDuration / (PaintDuration / rings));
			foreach (Cell c in Cleo_TerraFirma_GraveyardOrbitUplink.ZoneCells(center, Radius))
			{
				if (!c.IsVisible())
					continue;
				ConsoleChar ch = SB.Buffer[c.X, c.Y];
				int d = (int)Math.Round(Math.Sqrt((c.X - center.X) * (c.X - center.X) + (c.Y - center.Y) * (c.Y - center.Y)));
				if (d == ring)
				{
					ch.Background = The.Color.DarkRed;
					ch.TileBackground = The.Color.DarkRed;
					ch.Detail = The.Color.Red;
					ch.TileForeground = The.Color.Red;
				}
				else
				{
					ch.Background = The.Color.Red;
					ch.TileBackground = The.Color.Red;
					ch.Detail = The.Color.DarkRed;
					ch.TileForeground = The.Color.DarkRed;
				}
				ch.SetForeground('r');
			}
		}

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == ZoneActivatedEvent.ID || ID == ZoneThawedEvent.ID;
		}

		public override bool HandleEvent(ZoneActivatedEvent E)
		{
			CatchUp();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(ZoneThawedEvent E)
		{
			CatchUp();
			return base.HandleEvent(E);
		}

		private void CatchUp()
		{
			if (The.Game != null && The.Game.Turns - LastTurn > Countdown)
				ClearMarks();
		}

		public override bool WantTurnTick() => true;

		public override void TurnTick(long TimeTick, int Amount)
		{
			ParentObject.Flags |= GameObject.FLAG_ALWAYS_PAINT;
			LastTurn = TimeTick;
			Countdown -= Amount;
			if (Countdown <= 0)
				Strike();
		}

		public void ClearMarks()
		{
			Cell center = GetCenter();
			if (center == null)
				return;
			foreach (Cell c in Cleo_TerraFirma_GraveyardOrbitUplink.ZoneCells(center, Math.Max(Radius, 2)))
				foreach (GameObject o in c.GetObjects(Cleo_TerraFirma_GraveyardOrbitUplink.MARK))
					if (o != ParentObject)
						o.Obliterate(Silent: true);
			ParentObject.Obliterate(Silent: true);
		}

		public int Strike()
		{
			Cell center = GetCenter();
			if (center == null)
				return 0;
			List<Cell> cells = Cleo_TerraFirma_GraveyardOrbitUplink.ZoneCells(center, Radius);
			GameObject owner = Owner != null && Owner.IsValid() ? Owner : null;
			var pieces = new List<Piece>();
			foreach (Cell c in cells)
			{
				string bp = Rubble(c, center);
				GameObject o = bp == null ? null : GameObject.Create(bp);
				if (o != null)
					pieces.Add(new Piece { Cell = c, Object = o, Blueprint = bp });
			}
			AnimateFall(center, pieces);
			ClearMarks();
			int hit = 0;
			var targets = new List<GameObject>();
			foreach (Cell c in cells)
			{
				targets.Clear();
				foreach (GameObject o in c.GetObjectsWithPart("Combat"))
					targets.Add(o);
				foreach (GameObject o in targets)
				{
					if (!o.IsValid())
						continue;
					o.TakeDamage(Damage.RollCached(), "from %t falling debris!", "Bludgeoning",
						DeathReason: "You were crushed by debris from orbit.", ThirdPersonDeathReason: o.It + " was crushed by debris from orbit.",
						Owner: owner, Attacker: owner, Accidental: true);
					hit++;
				}
				if (c.IsVisible() && Stat.RandomCosmetic(1, 3) == 1)
					Cleo_TerraFirma_Fissure.SmokePuff(c);
			}
			if (owner != null && owner.CurrentCell != null && owner.CurrentZone == center.ParentZone)
				StunningForce.Concussion(center, owner, Level, Radius, Stun: true, Damage: false);
			foreach (Piece p in pieces)
			{
				GameObject o = p.Object;
				if (p.Cell.IsSolid())
				{
					o.Obliterate(Silent: true);
					continue;
				}
				if ((p.Blueprint == LARGE_BOULDER || p.Blueprint == RUBBLE) && p.Cell.HasObjectWithPart("Combat"))
				{
					o.Obliterate(Silent: true);
					o = GameObject.Create(MEDIUM_BOULDER);
					if (o == null)
						continue;
				}
				p.Cell.AddObject(o);
			}
			return hit;
		}

		public class Piece
		{
			public Cell Cell;
			public GameObject Object;
			public string Blueprint;
			public float Start;
			public bool Landed;
		}

		public static bool Animate = true;
		public const int FALL_ROWS = 4;
		public const float FRAME = 0.04f;

		public void AnimateFall(Cell center, List<Piece> pieces)
		{
			if (!Animate || pieces.Count == 0 || center.ParentZone != The.ZoneManager?.ActiveZone || Popup._TextConsole == null)
				return;
			var drops = new List<Piece>();
			float end = 0f;
			foreach (Piece p in pieces)
			{
				if (!p.Cell.IsVisible() || p.Object.Render == null)
					continue;
				int dx = Math.Abs(p.Cell.X - center.X), dy = Math.Abs(p.Cell.Y - center.Y);
				p.Start = dx == 0 && dy == 0 ? 0f
					: (dx <= 1 && dy <= 1 ? 0.10f + 0.02f * Stat.RandomCosmetic(0, 8) : 0.22f + 0.02f * Stat.RandomCosmetic(0, 12));
				end = Math.Max(end, p.Start + FALL_ROWS * FRAME);
				drops.Add(p);
			}
			if (drops.Count == 0)
				return;
			end += 0.12f;
			ScreenBuffer buf = ScreenBuffer.GetScrapBuffer1(bLoadFromCurrent: true);
			TextConsole console = Popup._TextConsole;
			for (float t = 0f; t < end; t += FRAME)
			{
				XRLCore.Core.RenderBaseToBuffer(buf);
				foreach (Piece p in drops)
				{
					if (t < p.Start)
						continue;
					int step = (int)((t - p.Start) / FRAME + 0.001f);
					int y = p.Cell.Y - Math.Max(0, FALL_ROWS - step);
					if (step >= FALL_ROWS && !p.Landed)
					{
						p.Landed = true;
						Cleo_TerraFirma_Fissure.SmokePuff(p.Cell);
						if (p.Cell == center)
							CombatJuice.cameraShake(0.3f);
					}
					if (y >= 0)
						DrawGlyph(buf, p.Cell.X, y, p.Object.Render);
				}
				console.DrawBuffer(buf);
				Thread.Sleep(40);
			}
		}

		private static void DrawGlyph(ScreenBuffer buf, int x, int y, Render r)
		{
			string glyph = r.RenderString.IsNullOrEmpty() ? "*" : r.RenderString.Substring(0, 1);
			string cs = Options.UseTiles && !r.TileColor.IsNullOrEmpty() ? r.TileColor : r.ColorString;
			char fg = 'y';
			int amp = cs == null ? -1 : cs.IndexOf('&');
			if (amp >= 0 && amp + 1 < cs.Length)
				fg = cs[amp + 1];
			char detail = r.DetailColor.IsNullOrEmpty() ? fg : r.DetailColor[0];
			buf.Goto(x, y);
			buf.Write("&" + fg + glyph);
			ConsoleChar ch = buf.Buffer[x, y];
			if (Options.UseTiles && !r.Tile.IsNullOrEmpty())
			{
				ch.Tile = r.Tile;
				ch.TileForeground = ColorUtility.ColorMap.GetValue(fg);
				ch.Detail = ColorUtility.ColorMap.GetValue(detail);
			}
			else
				ch.Tile = null;
		}

		public string Rubble(Cell c, Cell center)
		{
			if (c == null || c.IsSolid())
				return null;
			int dx = c.X - center.X, dy = c.Y - center.Y;
			string bp;
			if (dx == 0 && dy == 0)
				bp = LARGE_BOULDER;
			else if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1)
			{
				if (Stat.Random(1, 100) > InnerRubble)
					return null;
				int r = Stat.Random(1, 3);
				bp = r == 1 ? RUBBLE : (r == 2 ? MEDIUM_BOULDER : BOULDER);
			}
			else
			{
				if (Stat.Random(1, 100) > OuterRubble)
					return null;
				int r = Stat.Random(1, 100);
				bp = r <= 50 ? BOULDER : (r <= 80 ? MEDIUM_BOULDER : RUBBLE);
			}
			return bp;
		}
	}
}
