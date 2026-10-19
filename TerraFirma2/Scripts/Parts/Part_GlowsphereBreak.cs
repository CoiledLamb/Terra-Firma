using System;
using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World.Effects;

namespace XRL.World.Parts
{
	/// <summary>
	/// Merged onto base Glowsphere (Merges.xml). Two break paths (user rulings 2026-08-22):
	/// (1) a deliberate "break" inventory action yielding 2-3 glowsphere fragments ("2-3 fragments
	/// per sphere"; break-as-verb over a passive condition was the delegated Claude call);
	/// (2) thrown glowspheres shatter on landing ("glowspheres could break on thrown? shoot out a
	/// couple shards, do damage on hit, apply phosphorescent") - impact damage rides the merged
	/// ThrownWeapon part, phosphorescent (base Effects.Luminous) lands on a creature hit, and the
	/// crash scatters 1-3 already-lit fragments where the sphere lands (thrown yield RULED
	/// same sitting). Breaking happens in AfterThrownEvent, once the sphere has actually
	/// landed, so the throw machinery never sees its projectile die mid-flight.
	/// Floating Glowsphere deliberately NOT merged: it inherits LightSource, not Glowsphere,
	/// and whether it breaks too is an open fork.
	/// Player-facing text says "glowsphere fragment" everywhere (user 2026-08-22: "make sure they
	/// go by 'glowsphere fragments' and not glowstone, which i invented, oops"); internal
	/// blueprint/class names keep Glowstone to avoid churn, noted in TODO.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_GlowsphereBreak : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_BreakGlowsphere";

		/// <summary>What the sphere breaks into. XML-settable since the 2026-08-29 ladder pass.</summary>
		public string ShardBlueprint = "Cleo_TerraFirma_GlowstoneShard";
		// Blueprint tuning; defaults preserve behavior for existing saved objects.
		public string BreakYield = "2-3";
		public string ThrowYield = "1-3";
		public string GlowDuration = "30-60";

		/// <summary>Played once when the sphere breaks, by hand or thrown: base's gemstone shatter (the
		/// sphere is "opal-like" in its base description). XML-settable dials; [NonSerialized] so the
		/// shipped part keeps its saved layout.</summary>
		[NonSerialized] public string BreakSound = "Sounds/Damage/sfx_destroy_gemstone";
		[NonSerialized] public float BreakSoundVolume = 0.5f;

		public override bool SameAs(IPart p)
		{
			return p is Cleo_TerraFirma_GlowsphereBreak o
				&& o.ShardBlueprint == ShardBlueprint && o.BreakYield == BreakYield
				&& o.ThrowYield == ThrowYield && o.GlowDuration == GlowDuration && base.SameAs(p);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID
				|| ID == AfterThrownEvent.ID;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("ThrownProjectileHit");
			base.Register(Object, Registrar);
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			E.AddAction("Break", "break", COMMAND, null, 'b', FireOnActor: false);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == COMMAND && E.Actor != null)
			{
				if (!E.Actor.CanMoveExtremities(null, ShowMessage: true, Involuntary: false, AllowTelekinetic: true)) // frozen: base hand-action gate
					return false;
				// destructive and irreversible; a permanent light becomes depleting fragments
				if (E.Actor.IsPlayer()
					&& Popup.ShowYesNo("Really break your glowsphere?") != DialogResult.Yes) // USER TEXT 2026-08-22 (capitalization normalized)
				{
					return false;
				}
				GameObject sphere = ParentObject.SplitFromStack() ?? ParentObject;
				int count = BreakYield.RollCached();
				for (int i = 0; i < count; i++)
				{
					E.Actor.ReceiveObject(GameObject.Create(ShardBlueprint));
				}
				if (E.Actor.IsPlayer())
				{
					Popup.Show("The glowsphere breaks into " + Grammar.Cardinal(count) + " glowing fragments."); // draft, approved 2026-09-30
				}
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHARD", $"glowsphere broken into {count} fragments by {E.Actor.Blueprint}");
				if (!BreakSound.IsNullOrEmpty())
					E.Actor.PlayWorldSound(BreakSound, BreakSoundVolume);
				E.Actor.UseEnergy(1000, "Item BreakGlowsphere");
				sphere.Destroy();
				E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "ThrownProjectileHit")
			{
				// impact damage is the merged ThrownWeapon part's job; this is the glow rub-off
				GameObject target = E.GetGameObjectParameter("Defender");
				if (GameObject.Validate(target) && target.IsCreature && !target.HasEffect<Luminous>())
				{
					target.ApplyEffect(new Luminous(GlowDuration.RollCached()));
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHARD", $"thrown glowsphere marked {target.Blueprint} phosphorescent");
				}
			}
			return base.FireEvent(E);
		}

		public override bool HandleEvent(AfterThrownEvent E)
		{
			// the sphere has landed; shatter it where it lies
			Cell cell = ParentObject.CurrentCell;
			if (cell != null)
			{
				int count = ThrowYield.RollCached();
				for (int i = 0; i < count; i++)
				{
					GameObject fragment = GameObject.Create(ShardBlueprint);
					cell.AddObject(fragment);
					fragment.GetPart<MoteProperties>()?.Light(); // the crash site glows
				}
				if (IComponent<GameObject>.Visible(ParentObject))
				{
					IComponent<GameObject>.AddPlayerMessage("The glowsphere shatters, scattering glowing fragments."); // draft, approved 2026-09-30
				}
				if (!BreakSound.IsNullOrEmpty())
					cell.PlayWorldSound(BreakSound, BreakSoundVolume);
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHARD", $"thrown glowsphere shattered into {count} fragments");
				ParentObject.Destroy();
			}
			return base.HandleEvent(E);
		}
	}
}
