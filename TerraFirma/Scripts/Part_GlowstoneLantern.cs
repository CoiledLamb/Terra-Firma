using System;
using XRL.UI;

namespace XRL.World.Parts
{
	/// <summary>
	/// The glowstone lantern's socket (user rulings 2026-08-22: socket idiom over consume-on-light;
	/// lantern strictly better than a bare shard; shards loadable). Holds one glowstone shard as a
	/// real object, EnergyCellSocket pattern: load/unload, a half-burned shard comes back out
	/// half-burned. While a fueled shard is socketed the lantern is lit at a STEADY radius 8 (no
	/// mote step-down) and burns the shard's own MoteProperties.Fuel at HALF rate (one tick every
	/// other turn); both "better" axes are Claude dials, unruled numbers. The shard rides base
	/// MoteProperties per the user's ruling ("let's just use moteproperties on this one"), so the
	/// lantern owns the burn while the shard sits in its field (stored objects receive no EndTurn)
	/// and re-implements only the fuel-bracket name tag (getLitDescription is private in base).
	/// Lit/unlit tile swap: lantern_lit.png / lantern_unlit.png (user sprites, 2026-08-22).
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GlowstoneLantern : IPart
	{
		public const string LOAD_COMMAND = "Cleo_TerraFirma_LoadGlowstoneLantern";
		public const string UNLOAD_COMMAND = "Cleo_TerraFirma_UnloadGlowstoneLantern";
		/// <summary>What the socket accepts and spawns. XML-settable since the 2026-08-29 ladder pass.</summary>
		public string ShardBlueprint = "Cleo_TerraFirma_GlowstoneShard";
		public const int STEADY_RADIUS = 8;

		// USER TEXT, FINAL (delivered 2026-08-22 evening, swapped verbatim): the lit line
		// supersedes the original locked description ("a wrought metal cage encloses a
		// flickering shard."), the unlit line retires the placeholder debt.
		public const string LIT_DESCRIPTION = "A wrought metal cage and panes of crystal enclose a phosphorescent shard, amplifying its flickering glow.";
		public const string UNLIT_DESCRIPTION = "A wrought metal cage and panes of crystal enclose an inert wire socket.";

		public GameObject Shard;

		/// <summary>Half-rate burn phase: fuel ticks only when this flips to 0.</summary>
		public int BurnPhase;

		/// <summary>
		/// Blueprint dials for kit/loot lanterns (spine light group, 2026-08-22): StartLoaded
		/// spawns the lantern with a fragment already socketed, its fuel rolled from the
		/// StartFuel dice at creation (the piezoelectric cell's StartCharge idiom). A part-burned
		/// fragment on a working lantern reads as a light in use, not shelf stock.
		/// </summary>
		public bool StartLoaded;
		public string StartFuel = "2000-4500";

		public override bool SameAs(IPart p)
		{
			// never stack two lanterns if either holds a shard
			if ((p as Cleo_TerraFirma_GlowstoneLantern).Shard != null || Shard != null)
			{
				return false;
			}
			return base.SameAs(p);
		}

		public override IPart DeepCopy(GameObject Parent, Func<GameObject, GameObject> MapInv)
		{
			// EnergyCellSocket.DeepCopy shape: the held shard is copied with the lantern
			Cleo_TerraFirma_GlowstoneLantern copy = (Cleo_TerraFirma_GlowstoneLantern)base.DeepCopy(Parent, MapInv);
			if (GameObject.Validate(ref Shard))
			{
				copy.Shard = MapInv?.Invoke(Shard) ?? Shard.DeepCopy(CopyEffects: false, CopyID: false, MapInv);
			}
			else
			{
				copy.Shard = null;
			}
			return copy;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == SingletonEvent<EndTurnEvent>.ID
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID
				|| ID == PooledEvent<GetDisplayNameEvent>.ID
				|| ID == ObjectCreatedEvent.ID;
		}

		public override bool HandleEvent(ObjectCreatedEvent E)
		{
			if (StartLoaded && Shard == null)
			{
				GameObject shard = GameObject.Create(ShardBlueprint);
				MoteProperties mote = shard?.GetPart<MoteProperties>();
				if (mote != null)
				{
					int fuel = XRL.Rules.Stat.Roll(StartFuel);
					mote.Fuel = fuel < 1 ? 1 : fuel;
				}
				Shard = shard;
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("LANTERN", $"spawn-loaded shard fuel={GetFuel()}");
			}
			SyncState();
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if (Shard == null)
			{
				E.AddAction("Load", "load fragment", LOAD_COMMAND, null, 'o', FireOnActor: false);
			}
			else
			{
				E.AddAction("Unload", "remove fragment", UNLOAD_COMMAND, null, 'o', FireOnActor: false);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == LOAD_COMMAND && E.Actor != null)
			{
				GameObject pick = FindBestShard(E.Actor);
				if (pick == null)
				{
					if (E.Actor.IsPlayer())
					{
						Popup.ShowFail("You have no glowsphere fragment to load."); // DRAFT
					}
					return false;
				}
				pick = pick.SplitFromStack() ?? pick;
				pick.RemoveFromContext();
				Shard = pick;
				SyncState();
				if (E.Actor.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage("You seat " + pick.t() + " inside " + ParentObject.t() + "."); // DRAFT
				}
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("LANTERN", $"loaded shard fuel={GetFuel()}");
				E.Actor.UseEnergy(1000, "Item LoadGlowstoneLantern");
				E.RequestInterfaceExit();
			}
			else if (E.Command == UNLOAD_COMMAND && E.Actor != null && Shard != null)
			{
				GameObject outShard = Shard;
				Shard = null;
				E.Actor.ReceiveObject(outShard);
				SyncState();
				if (E.Actor.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage("You take " + outShard.t() + " out of " + ParentObject.t() + "."); // DRAFT
				}
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("LANTERN", "shard removed");
				E.Actor.UseEnergy(1000, "Item UnloadGlowstoneLantern");
				E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (GameObject.Validate(ref Shard))
			{
				MoteProperties mote = Shard.GetPart<MoteProperties>();
				if (mote != null)
				{
					BurnPhase = 1 - BurnPhase;
					if (BurnPhase == 0 && mote.Fuel > 0)
					{
						mote.Fuel--;
						if (mote.Fuel <= 0)
						{
							if (ParentObject.Equipped != null && ParentObject.Equipped.IsPlayer())
							{
								IComponent<GameObject>.AddPlayerMessage("The fragment inside " + The.Player.Poss(ParentObject) + " goes dark."); // DRAFT
							}
							Cleo.TerraFirma.Scripts.Helpers.VerifyLog("LANTERN", "socketed shard burned out");
							Shard.Obliterate();
							Shard = null;
						}
					}
				}
				SyncState();
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			// mirror MoteProperties' status tag so the lantern reads its shard's life at a glance
			int fuel = GetFuel();
			if (fuel > 0 && ParentObject.IsReal)
			{
				E.AddTag("(" + FuelDescription(fuel) + ")");
			}
			return base.HandleEvent(E);
		}

		private GameObject FindBestShard(GameObject actor)
		{
			// deterministic pick: the fullest shard carried (no picker UI for v1; flagged in TODO)
			GameObject best = null;
			int bestFuel = -1;
			foreach (GameObject item in actor.GetInventoryAndEquipmentReadonly())
			{
				if (item.Blueprint == ShardBlueprint)
				{
					int fuel = item.GetPart<MoteProperties>()?.Fuel ?? 0;
					if (fuel > bestFuel)
					{
						bestFuel = fuel;
						best = item;
					}
				}
			}
			return best;
		}

		private int GetFuel()
		{
			if (!GameObject.Validate(ref Shard))
			{
				return 0;
			}
			return Shard.GetPart<MoteProperties>()?.Fuel ?? 0;
		}

		[NonSerialized]
		private int FrameOffset;

		/// <summary>MoteProperties.Render's EXACT flicker (decomp-verified: 60-frame cycle,
		/// Y under 15, W to 30, C to 45, W after, random 1-5 frame jitter, priority 50),
		/// applied to the DETAIL channel so the glow flickers through the dark cage - the
		/// flicker IS the socketed shard showing through the glass.</summary>
		public override bool Render(RenderEvent E)
		{
			if (GetFuel() > 0)
			{
				int num = (XRL.Core.XRLCore.CurrentFrame + FrameOffset) % 60;
				if (!XRL.UI.Options.DisableTextAnimationEffects)
				{
					FrameOffset += XRL.Rules.Stat.Random(1, 5);
				}
				string color = num < 15 ? "Y" : num < 30 ? "W" : num >= 45 ? "W" : "C";
				E.ApplyDetailColor(color, 50);
			}
			return base.Render(E);
		}

		/// <summary>MoteProperties.getLitDescription's exact brackets (it is private in base).</summary>
		private static string FuelDescription(int fuel)
		{
			if (fuel > 3500)
			{
				return "{{Y|blazing}}";
			}
			if (fuel > 2000)
			{
				return "{{W|bright}}";
			}
			if (fuel > 500)
			{
				return "dim";
			}
			return "{{K|faint}}";
		}

		/// <summary>Lit flag, steady radius, tile, and description all follow the socket state.</summary>
		private void SyncState()
		{
			bool lit = GetFuel() > 0;
			LightSource light = ParentObject.GetPart<LightSource>();
			if (light != null)
			{
				light.Lit = lit;
				light.Radius = STEADY_RADIUS;
			}
			Render render = ParentObject.GetPart<Render>();
			if (render != null)
			{
				render.Tile = lit ? "lantern_lit.png" : "lantern_unlit.png";
			}
			// the y/Y AnimatedMaterialGeneric approach is GONE (user live-catch 2026-08-22:
			// "lantern flickers between white and grey, not the colors the shard flickers -
			// and at a totally different rate"); the shard's visible flicker was never its
			// anim part but MoteProperties.Render's fast Y/W/C cycle - now mirrored in this
			// part's own Render override. Strip any anim part older saves still carry.
			AnimatedMaterialGeneric anim = ParentObject.GetPart<AnimatedMaterialGeneric>();
			if (anim != null)
			{
				ParentObject.RemovePart(anim);
			}
			Description desc = ParentObject.GetPart<Description>();
			if (desc != null)
			{
				desc.Short = lit ? LIT_DESCRIPTION : UNLIT_DESCRIPTION;
			}
		}
	}
}
