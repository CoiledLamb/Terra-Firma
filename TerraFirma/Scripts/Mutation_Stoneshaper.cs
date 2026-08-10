using Cleo.TerraFirma.Scripts;
using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.Anatomy;
using XRL.World.Effects;
using XRL.World.Parts.Skill;

namespace XRL.World.Parts.Mutation
{
	[Serializable]
	public class Cleo_TerraFirma_Stoneshaper : BaseMutation
	{
		#region Fields
		private const string COMMAND_ID = "Cleo_TerraFirma_CommandStoneshaper";
		private const string DEFAULT_BEHAVIOR_BP = "Cleo_TerraFirma_StoneshaperStatueNaturalWeapon"; // the statues' fists; see ApplyStatueBuffs

		private const int BASE_DURATION = 100;
		private const int TURNS_PER_LEVEL = 40;
		private const int INFINITE_DURATION_PLACEHOLDER = -69420;

		private const int BASE_COOLDOWN = 120;

		/// <summary>
		/// Reference to the current statue animated by this mutation.
		/// Don't reference this directly -- use <see cref="CurStatue"/> instead.
		/// </summary>
		[SerializeField]
		private GameObject _curStatue = null;

		/// <summary>
		/// Getter/private setter for <see cref="_curStatue"/>.
		/// May become null at any time; make sure to sanity check.
		/// </summary>
		public GameObject CurStatue
		{
			private set
			{
				_curStatue = value;
			}
			get
			{
				if (_curStatue != null && (_curStatue.IsInGraveyard() || _curStatue.IsNowhere()))
					_curStatue = null;
				return _curStatue;
			}
		}

		public enum WallType
		{
			Stone = 1,
			StoneAndMetal = 2,
			All = 3
		}

		/// <summary>
		/// How many turns are remaining before the current statue expires.
		/// </summary>
		public int StatueLifetimeRemaining;
		#endregion

		#region Save/load
		public override void Write(GameObject Basis, SerializationWriter Writer)
		{
			Writer.WriteGameObject(_curStatue);
			base.Write(Basis, Writer);
		}

		public override void Read(GameObject Basis, SerializationReader Reader)
		{
			_curStatue = Reader.ReadGameObject();
			base.Read(Basis, Reader);
		}
		#endregion

		#region Basic data
		public override string GetDescription()
		{
			return "You animate earth to fight for you.";
		}

		public override string GetLevelText(int Level)
		{
			WallType wt = GetValidWallsAtLevel(Level);
			int duration = GetDuration(Level);
			// 2026-07-26 voice sitting rulings, built 2026-07-27: opener de-imperatived into Stone
			// Gaze's own "You can gaze..." construction (creature-then-wall order verified against
			// the code's picker prompts); the "stone renditions" natural-weapons line CUT (cosmetic
			// copying goes undocumented); the inheritance mega-line split with attribute names
			// unwrapped; attack line split per Horns ("fine"); the flowery closer CUT ENTIRELY
			// (expiry-container behavior discoverable in play); Cooldown lands last, bare. The
			// wall-type wrap stays - it scales with level, which is what rules-wrap is for.
			return "You can gaze at a creature, then at a wall, to carve an animate statue of that creature.\n" +
				"The statue inherits the creature's body plan and physique (Strength, Agility, Toughness).\n" +
				"It does not inherit mutations, skills, or equipment.\n" +
				"Nearby enemies must make a Willpower save (difficulty {{rules|" + GetDistractionSaveDifficulty(Level) + "}}) or direct their attacks at the statue.\n" +
				"Can target {{rules|" + (wt == WallType.Stone ? "stone" : (wt == WallType.StoneAndMetal ? "stone and metal" : "almost any")) + " walls}}\n" +
				"Statue hit points: {{rules|" + GetHP(Level) + "}}\n" +
				"Statue AV: {{rules|" + GetAV(Level) + "}}\n" +
				"Statue damage increment: {{rules|" + GetNaturalDamage(Level) + "}}\n" +
				// to-hit line REMOVED with the retired flat bonus (2026-08-09): accuracy and
				// penetration now ride the inherited physique, which the text above already
				// documents ("inherits the creature's body plan and physique")

				(duration == INFINITE_DURATION_PLACEHOLDER
					? "The statue lasts {{rules|until destroyed}}. Carving another releases it.\n"
					: "The statue lasts for {{rules|" + duration + "}} rounds.\n") +
				"Cooldown: " + BASE_COOLDOWN + " rounds";
		}

		public override void CollectStats(Templates.StatCollector stats)
		{
			int duration = GetDuration(Level);
			if (duration == INFINITE_DURATION_PLACEHOLDER)
				stats.Set("Duration", "until destroyed");
			else
				stats.Set("Duration", duration);
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), BASE_COOLDOWN);
		}

		public override bool Mutate(GameObject GO, int Level = 1)
		{
			ActivatedAbilityID = AddMyActivatedAbility("Stoneshaper", COMMAND_ID, "Physical Mutations");
			return base.Mutate(GO, Level);
		}

		public override bool Unmutate(GameObject GO)
		{
			RemoveMyActivatedAbility(ref ActivatedAbilityID);
			return base.Unmutate(GO);
		}
		#endregion

		#region Events
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) &&
				ID != PooledEvent<CommandEvent>.ID &&
				ID != AIGetOffensiveAbilityListEvent.ID)
				return ID == SingletonEvent<EndTurnEvent>.ID;
			return true;
		}

		// ---- NPC advertisement (2026-08-06, user: legendary creatures can roll pool mutations
		// via RandomMutations -- "we could just work the advertisement out"). The AI branch rides
		// the harness's own picker-free seam: CarveFrom(actor, creature, wallCell). Muse = the
		// AI's combat target; wall = nearest carvable within NPC_WALL_SCAN_RADIUS, validity
		// mirroring the player picker's checks. ----
		private const int NPC_WALL_SCAN_RADIUS = 8; // Claude dial

		public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
		{
			if (IsMyActivatedAbilityAIUsable(ActivatedAbilityID) && E.Target != null && E.Target.Body != null && FindCarvableWallCellFor(E.Actor) != null)
				E.Add(COMMAND_ID);
			return base.HandleEvent(E);
		}

		/// <summary>The muse an NPC shaper actually carves (USER 2026-08-06: "pick the strongest
		/// monster nearby... determine strongest by level, since that's the easiest indicator"):
		/// the highest-Level creature in sight within the scan radius -- the statue inherits its
		/// muse's physique, so the beefiest model makes the best statue. The combat target is the
		/// fallback and keeps ties; the caster and its own current statue are excluded.</summary>
		private GameObject FindBestMuseFor(GameObject actor, GameObject fallback)
		{
			GameObject best = fallback;
			int bestLevel = fallback != null && fallback.HasStat("Level") ? fallback.GetStatValue("Level") : 0;
			Cell origin = actor?.CurrentCell;
			if (origin == null)
				return best;
			foreach (Cell c in origin.GetLocalAdjacentCells(NPC_WALL_SCAN_RADIUS))
			{
				foreach (GameObject obj in c.Objects)
				{
					if (!obj.IsCreature || obj.Body == null || obj == actor || obj == fallback || obj == CurStatue)
						continue;
					if (!obj.HasStat("Level") || obj.GetStatValue("Level") <= bestLevel)
						continue;
					if (!actor.HasLOSTo(obj))
						continue;
					best = obj;
					bestLevel = obj.GetStatValue("Level");
				}
			}
			return best;
		}

		/// <summary>Nearest wall cell this caster could legally carve from: the player picker's
		/// validity checks (solid wall, no hologram/walltrap/forcefield, no refusal tag, audited
		/// properties present, type within the level's reach), nearest-first.</summary>
		private Cell FindCarvableWallCellFor(GameObject actor)
		{
			Cell origin = actor?.CurrentCell;
			if (origin == null)
				return null;
			WallType validWalls = GetValidWallsAtLevel(Level);
			Cell best = null;
			int bestDist = int.MaxValue;
			foreach (Cell c in origin.GetLocalAdjacentCells(NPC_WALL_SCAN_RADIUS))
			{
				int d = origin.DistanceTo(c);
				if (d >= bestDist || !c.HasWall())
					continue;
				List<GameObject> walls = c.GetWalls();
				if (walls.Count == 0)
					continue;
				GameObject w = walls.First();
				if (w.HasPart<HologramMaterial>() || w.HasPart<Walltrap>() || w.HasPart<Forcefield>() || w.HasTag("Cleo_TerraFirma_NoStoneshape"))
					continue;
				Cleo_TerraFirma_StoneshaperWallProperties props = w.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
				if (props == null)
					continue;
				bool isStone = props.MaterialType == "stone";
				bool isMetal = props.MaterialType == "metal";
				if (validWalls == WallType.Stone && !isStone)
					continue;
				if (validWalls == WallType.StoneAndMetal && !isStone && !isMetal)
					continue;
				best = c;
				bestDist = d;
			}
			return best;
		}

		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command != COMMAND_ID)
				return base.HandleEvent(E);
			if (E.Actor != null && !E.Actor.IsPlayer())
			{
				// AI-fired commands carry the target (recorded plumbing: CommandEvent.Send
				// populates E.Target); the swap-confirm popup is player UX -- an NPC re-carve
				// swaps silently, cooldown-gated like everything else. The muse upgrades from
				// the combat target to the strongest creature in sight (user ruling, by Level).
				GameObject muse = E.Target;
				if (!GameObject.Validate(ref muse))
					return false;
				muse = FindBestMuseFor(E.Actor, muse);
				if (muse == null || muse.Body == null)
					return false;
				Cell npcWallCell = FindCarvableWallCellFor(E.Actor);
				if (npcWallCell == null)
					return false;
				return CarveFrom(E.Actor, muse, npcWallCell) != null && base.HandleEvent(E);
			}
			if (CurStatue != null)
			{
				// ANY living statue may be swapped (RULED 2026-07-23, extending the Lv10 path down:
				// "cooldown gates it most of the time anyways") -- but never by accident; permanent
				// ones especially can carry levels and gear
				if (E.Actor.IsPlayer() && Popup.ShowYesNo("Carving a new statue will return " + CurStatue.t() +
					" to lifeless stone. Anything worn or carried will be entombed within, recoverable by hand. Proceed?") == DialogResult.No)
					return false;
			}
		//MessageQueue.AddPlayerMessage("choosing target");
		targetSelection:
			Cell targetCreatureCell = PickDestinationCell(Snap: true, Label: "Choose a cell with a creature to copy.");
			if (targetCreatureCell == null)
				return false;
			GameObject creature = targetCreatureCell.GetCombatTarget(E.Actor, true);
			if (creature == null || !creature.IsCreature)
			{
				E.Actor.Fail("You must target a cell containing a creature.");
				goto targetSelection;
			}
			if (creature.Body == null)
			{
				// plants and other anatomy-less creatures (Young Ivory et al.) have nothing to render in stone;
				// without this check the Body.Rebuild in ApplyStatueBuffs throws and the cast dies silently
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHAPE", $"no-body fail: {creature.Blueprint} refused cleanly (no cooldown, no wall spent), re-prompting");
				E.Actor.Fail(creature.T() + creature.GetVerb("have") + " no body plan for you to carve.");
				goto targetSelection;
			}
			//MessageQueue.AddPlayerMessage($"target chosen: {creature.DisplayName}");

			//MessageQueue.AddPlayerMessage($"choosing wall: {creature.DisplayName}");
			Cell targetWallCell = targetCreatureCell;
			WallType validWalls = GetValidWallsAtLevel(Level);

		wallSelection:
			targetWallCell = PickDestinationCell(RequireCombat: false, Label: "Choose a wall to carve the statue from.");
			if (targetWallCell == null)
			{ // cancelled
			  //MessageQueue.AddPlayerMessage($"cancelled");
				return false;
			}
			else if (!targetWallCell.HasWall())
			{
				E.Actor.Fail("You must select a tile containing a solid wall.");
				//MessageQueue.AddPlayerMessage($"tile has no wall");
				goto wallSelection;
			}

			//MessageQueue.AddPlayerMessage($"getting walls...");
			List<GameObject> walls = targetWallCell.GetWalls();
			GameObject refWall = walls.First();
			if (refWall.HasPart<HologramMaterial>() || refWall.HasPart<Walltrap>() || refWall.HasPart<Forcefield>())
			{
				E.Actor.Fail("You must select a tile containing a solid wall.");
				goto wallSelection;
			}
			if (refWall.HasTag("Cleo_TerraFirma_NoStoneshape"))
			{
				E.Actor.Fail($"No matter how hard you try, {refWall.DisplayName} refuses to be sculpted.");
				goto wallSelection;
			}
			Cleo_TerraFirma_StoneshaperWallProperties wallProps = refWall.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			if (wallProps == null)
			{
				E.Actor.Fail($"No matter how hard you try, {refWall.DisplayName} refuses to be sculpted.");
				goto wallSelection;
			}
			bool isStoneWall = wallProps.MaterialType == "stone";
			bool isMetalWall = wallProps.MaterialType == "metal";

			if (validWalls != WallType.All)
			{
				if (validWalls == WallType.Stone && !isStoneWall)
				{
					E.Actor.Fail("You must target a wall made of stone.");
					goto wallSelection;
				}
				else if (validWalls == WallType.StoneAndMetal && !isStoneWall && !isMetalWall)
				{
					E.Actor.Fail("You must target a wall made of stone or metal.");
					goto wallSelection;
				}
			}

			return CarveFrom(E.Actor, creature, targetWallCell) != null && base.HandleEvent(E);
		}

		/// <summary>The carve core, extracted 2026-07-24 for the test harness: the UI shell above
		/// gathers muse + wall through interactive pickers (which a wish cannot drive); this does
		/// everything after — body/wall re-validation, the lovesick substitution, statue creation
		/// + buffs, the swap commit, wall consumption, placement, and the cooldown. Returns the
		/// statue, or null on any clean fail (no cooldown taken, no wall spent). Behavior is the
		/// handler's own, moved verbatim.</summary>
		internal GameObject CarveFrom(GameObject actor, GameObject creature, Cell targetWallCell)
		{
			if (creature == null || creature.Body == null)
			{
				// defensive twin of the handler's early check: the harness enters here directly
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHAPE", $"no-body fail: {creature?.Blueprint ?? "?"} refused cleanly (no cooldown, no wall spent)");
				if (creature != null)
					actor.Fail(creature.T() + creature.GetVerb("have") + " no body plan for you to carve.");
				return null;
			}
			List<GameObject> walls = targetWallCell.GetWalls();
			if (walls.Count == 0)
				return null;
			GameObject refWall = walls.First();
			Cleo_TerraFirma_StoneshaperWallProperties wallProps = refWall.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
			if (wallProps == null)
				return null;

			Lovesick lovesick = actor.GetEffect<Lovesick>();
			GameObject target = creature;
			if (lovesick != null)
			{
				creature = lovesick.Beauty; // >:)
				if (creature == null || creature.Body == null)
					creature = target; // the beloved can't be rendered in stone; carve what was actually asked for
			}
			else if (target.HasPart<Lovely>())
				actor.ApplyEffect(new Lovesick(Stat.Random(3000, 3600), target));

			string descriptor = "rough";
			if (Level >= 3)
				descriptor = "fine";
			if (Level >= 6)
				descriptor = "dignified";
			if (Level >= 9)
				descriptor = "exquisite";
			if (Level >= 12)
				descriptor = "masterwork";
			if (Level >= 15)
				descriptor = "immaculate";

			GameObject statue = MakeStatueOfCreatureFromWall(creature, refWall, descriptor, wallProps);
			if (statue == null)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHAPE", $"statue creation FAILED (null) for muse {creature?.Blueprint ?? "?"}");
				actor.Fail("The stone refuses to take shape.");
				return null;
			}
			AnimateObject.Animate(statue);

			ApplyStatueBuffs(actor, creature, statue, Level, wallProps);

			// commit point: only now does an existing statue (any lifetime, swap-down ruling 2026-07-23) get released
			if (CurStatue != null)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHAPE", "SWAP: prior statue de-animated (gear entombed) before the new carve");
				XDidY(CurStatue, "return", "wearily to earth", Source: CurStatue, ColorAsBadFor: ParentObject);
				// swap alone stays quiet (RULED 2026-08-07, with popup-on-expiry): the player just
				// confirmed this de-animation, so the mourning drops to a log line here while
				// natural expiry and combat deaths keep the popup (blueprint no longer blanket-tagged)
				CurStatue.SetStringProperty("NoFollowerDeathPopup", "1");
				DeanimateStatue(CurStatue);
			}
			CurStatue = statue;
			StatueLifetimeRemaining = GetDuration(Level);
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHAPE", $"carved: muse={creature.Blueprint}, wall={refWall?.Blueprint ?? "?"}, Lv{Level}, lifetime={(StatueLifetimeRemaining == INFINITE_DURATION_PLACEHOLDER ? "PERMANENT" : StatueLifetimeRemaining.ToString() + " turns")}");

			// carve reveal (VFX pass 2026-08-05, user concept from the wishlist; reshaped at feel
			// round 1: one whole stone, not a mosaic): a block sprite wearing the wall's own
			// painted tile covers the cell; the real wall->statue swap happens early and HIDDEN
			// behind it; three breaks each shrink the block by a third and pop a pair of debris
			// chunks off its top edge (hop, then fall), revealing the statue top-down.
			// Frame-pump settle idiom per Calcify; unseen, inactive-zone, and harness-muted
			// casts place instantly as before.
			void PlaceStatue()
			{
				walls.ForEach(x => x.Obliterate());
				targetWallCell.AddObject(statue, true, Silent: true);
			}
			Cleo_TerraFirma_StoneRiseFX.Schedule schedule = null;
			Zone zone = targetWallCell.ParentZone;
			if (!Cleo_TerraFirma_StoneRiseFX.HarnessMute && zone != null && zone.IsActive() && targetWallCell.IsVisible())
				schedule = Cleo_TerraFirma_StoneRiseFX.PlayCarveReveal(targetWallCell, refWall);
			if (schedule != null)
			{
				ScreenBuffer buf = ScreenBuffer.GetScrapBuffer1(bLoadFromCurrent: true);
				TextConsole console = Popup._TextConsole;
				float swapAt = schedule.SettleTimes[targetWallCell];
				bool swapped = false;
				int breaksPlayed = 0;
				float t = 0f;
				float end = schedule.Total + 0.08f;
				while (t < end)
				{
					if (!swapped && swapAt <= t)
					{
						PlaceStatue();
						swapped = true;
					}
					while (breaksPlayed < schedule.BreakTimes.Count && schedule.BreakTimes[breaksPlayed] <= t)
					{
						PlayWorldSound("Sounds/Damage/sfx_damage_stone", 1f, Combat: true, SourceCell: targetWallCell);
						breaksPlayed++;
					}
					XRLCore.Core.RenderBaseToBuffer(buf);
					console.DrawBuffer(buf);
					Thread.Sleep(40);
					t += 0.04f;
				}
				if (!swapped)
					PlaceStatue();
				// smoke only AFTER the reveal (RULED 2026-08-05: mid-carve smoke hid the statue;
				// "have the smoke spawn at the end", "a random amount of puffs ... between 1-4")
				Cleo_TerraFirma_Fissure.SmokePuff(targetWallCell, Stat.Random(1, 4));
			}
			else
				PlaceStatue();
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHAPE", $"carve reveal: {(schedule != null ? schedule.Total.ToString("0.00") + "s, block + 6 debris / 3 breaks" : "instant (unseen or harness-muted)")}");
			statue.Splatter($"&{statue.GetForegroundColor()}*");
			PlayWorldSound("Sounds/Throw/sfx_throwing_stone_small_impact", 1f, Combat: true, SourceCell: targetWallCell);

			if (lovesick != null && actor.IsPlayer())
				Popup.Show($"You attempt to picture {target.the} {target.DisplayName} in your mind, but all you can think about is {lovesick.Beauty.the} {lovesick.Beauty.DisplayName}…!");
			XDidYToZ(actor, $"create a sculpture of", creature, EndMark: "!", ColorAsGoodFor: actor, Source: actor);

			// every carve takes the same cooldown; recasting after it swaps the living statue out
			// (lifetime-as-cooldown display retired with the swap-down ruling, 2026-07-23)
			CooldownMyActivatedAbility(ActivatedAbilityID, BASE_COOLDOWN);
			return statue;
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			// lifetime-as-cooldown RETIRED with the swap-down ruling (2026-07-23): every carve takes
			// the normal BASE_COOLDOWN at cast time, and lifetime ticks silently here. Cost accepted
			// at the ruling: the ability card no longer counts down the statue's remaining turns.
			// The old scheme's three cooldown-normalization branches (permanent destroyed / natural
			// expiry / killed early) existed only to clear the pinned display -- all retired with it.
			if (StatueLifetimeRemaining == INFINITE_DURATION_PLACEHOLDER)
			{
				if (CurStatue == null)
					StatueLifetimeRemaining = 0; // permanent statue destroyed; nothing to normalize anymore
			}
			else if (CurStatue != null)
			{
				StatueLifetimeRemaining--;
				if (StatueLifetimeRemaining <= 0)
				{
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHAPE", "natural expiry: statue de-animates into inert lootable container");
					XDidY(CurStatue, "return", "wearily to earth", Source: CurStatue, ColorAsBadFor: ParentObject);
					DeanimateStatue(CurStatue);
					CurStatue = null;
				}
			}
			else if (StatueLifetimeRemaining > 0)
			{
				Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHAPE", $"statue died with {StatueLifetimeRemaining} turns of lifetime left");
				StatueLifetimeRemaining = 0;
			}
			return base.HandleEvent(E);
		}
		#endregion

		#region Helpers
		/// <summary>
		/// Sets up a provided statue with all of its unique properties as applied by this mutation.
		/// Includes anatomy changes, distraction, allegiance changes, wall-specific stat offsets/mutations, and pronouns.
		/// Does <b>not</b> change duration.
		/// </summary>
		/// <param name="Sculptor">The game object responsible for creating this statue.</param>
		/// <param name="Creature">The creature that this statue was created to reference.</param>
		/// <param name="Statue">The statue object to change the behavior of.</param>
		/// <param name="Lv">The effective level of the mutation.</param>
		/// <param name="WallProps">The properties of the wall that the statue was created from.</param>
		public static void ApplyStatueBuffs(GameObject Sculptor, GameObject Creature, GameObject Statue, int Lv, Cleo_TerraFirma_StoneshaperWallProperties WallProps)
		{
			bool infiniteDuration = Lv >= 10;
			if (Creature.Body != null) // anatomy-less muses keep the statue's default animated body
				Statue.Body.Rebuild(Creature.Body.Anatomy);

			Mutations m = Statue.GetPart<Mutations>();
			if (m != null)
			{
				if (m.MutationList != null)
				{
					foreach (BaseMutation mut in m.MutationList.ToArray())
						m.RemoveMutation(mut);
				}
				m.AddMutation(new NightVision());
			}

			Skills s = Statue.GetPart<Skills>();
			if (s?.SkillList != null)
			{
				foreach (BaseSkill skill in s.SkillList.ToArray())
					s.RemoveSkill(skill);
			}

			// map the muse's natural weapon looks by body part type -- statues fight with stone renditions
			// of the muse's own weapons (tile + name only; stats stay on the mutation's formula)
			Dictionary<string, GameObject> museWeapons = new();
			if (Creature.Body != null)
			{
				foreach (BodyPart part in Creature.Body.LoopParts())
				{
					GameObject museWeapon = part.DefaultBehavior ?? ((part.Equipped != null && part.Equipped.IsNatural()) ? part.Equipped : null);
					// generic filler weapons (DefaultFist et al.) are not distinctive muse weapons -- the
					// engine marks them UndesirableWeapon itself. Skip them so the statue keeps its gravelly
					// haymaker identity (live-fire find 2026-07-23: a goatfolk statue punched with plain 'fist')
					if (museWeapon != null && museWeapon.HasTag("UndesirableWeapon"))
						museWeapon = null;
					if (museWeapon != null && part.Type != null && !museWeapons.ContainsKey(part.Type))
						museWeapons[part.Type] = museWeapon;
				}
			}

			var naturalWeaponParts = Statue.Body.LoopParts();
			List<BodyPart> partsToReplace = new();
			foreach (var item in naturalWeaponParts)
			{
				if (item.Type == null || item.Abstract || item.Extrinsic)
					continue;
				if (item.DefaultBehavior != null)
				{
					item.DefaultBehaviorBlueprint = null;
					item.DefaultBehavior = null;
					partsToReplace.Add(item);
				}
				if (item.Equipped != null && item.Equipped.IsNatural())
				{
					if (!partsToReplace.Contains(item))
						partsToReplace.Add(item);
					item.Unequip();
				}
			}

			// ALSO arm every statue part whose TYPE matches a slot where the muse carried a natural
			// weapon: Body.Rebuild copies the anatomy but NOT DefaultBehaviors (those live on creature
			// blueprints, not anatomies), so a rebuilt statue usually has nothing to strip and the old
			// code fell through to the fist-on-Body fallback (live-fire find 2026-07-23: goat statue
			// got a fist on its Body while the horn sat unused in the muse map).
			foreach (var item in Statue.Body.LoopParts())
			{
				if (item.Type == null || item.Abstract || item.Extrinsic)
					continue;
				if (museWeapons.ContainsKey(item.Type) && !partsToReplace.Contains(item))
					partsToReplace.Add(item);
			}

			if (partsToReplace.IsNullOrEmpty())
			{
				//MessageQueue.AddPlayerMessage("looped parts is null. selecting body.");
				partsToReplace.Add(Statue.Body.GetPart("Body", true).First());
			}
			//MessageQueue.AddPlayerMessage($"adding natural weapon, part 2. replacing {partsToReplace.Count} parts...");
			foreach (var item in partsToReplace)
			{
				item.DefaultBehaviorBlueprint = DEFAULT_BEHAVIOR_BP;
				item.DefaultBehavior = GameObjectFactory.Factory.CreateUnmodifiedObject(DEFAULT_BEHAVIOR_BP);
				MeleeWeapon wp = item.DefaultBehavior.RequirePart<MeleeWeapon>();
				wp.BaseDamage = GetNaturalDamage(Lv);
				// flat HitBonus DROPPED at the statue identity audit (RULED 2026-08-09): it
				// stacked redundantly on the muse's inherited Agility, which already carries
				// accuracy -- the second muse-choice lever alongside Strength-driven pens
				wp.Slot = item.Type;
				if (item.Type != null && museWeapons.TryGetValue(item.Type, out GameObject museWeapon))
				{
					item.DefaultBehavior.Render.DisplayName = museWeapon.Render.DisplayName;
					item.DefaultBehavior.Render.Tile = museWeapon.Render.Tile;
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHAPE", $"muse weapon: statue {item.Type} armed as '{museWeapon.Render.DisplayName}' (mutation-formula stats)");
				}
				else
					Cleo.TerraFirma.Scripts.Helpers.VerifyLog("SHAPE", $"muse weapon: statue {item.Type} armed with fallback gravelly fist");
			}
			Statue.Body.RecalculateFirsts();

			string[] statsInherited = { "Strength", "Agility", "Toughness" };
			foreach (string stat in statsInherited)
				Statue.GetStat(stat).BaseValue = Creature.GetStatValue(stat);

			Statue.GetStat("Level").BaseValue = Sculptor.Level;
			if (!infiniteDuration)
				Statue.RemovePart<Leveler>();
			Cleo_TerraFirma_StoneshaperStatueStatsPart statsPart = new(GetHP(Lv) - Statue.GetStatValue("Hitpoints"), (GetAV(Lv)) - Statue.GetStatValue("AV"));
			Statue.AddPart(statsPart);
			if (WallProps != null)
			{
				// the hardcoded metal +2 AV was a DOUBLE-DIP (identity audit 2026-08-09):
				// Merges.xml already gives metal walls StatName="AV" StatShift="2", so metal
				// statues got +4. The XML rider is the single source now.
				if (WallProps.StatName != null)
					Statue.AddBaseStat(WallProps.StatName, WallProps.StatShift);
				if (WallProps.MutationName != null)
					Statue.RequirePart<Mutations>().AddMutation(WallProps.MutationName, WallProps.MutationLevel);
			}

			//MessageQueue.AddPlayerMessage($"caching stuff...");
			bool isVain = Creature == Sculptor || Creature.Blueprint == "Asphodel" || Sculptor.HasEffect<Lovesick>();
			//MessageQueue.AddPlayerMessage($"is vain: {isVain}, {The.Game.GetBooleanGameState("Cleo_TerraFirma_StateIsVain")}");
			if (isVain)
			{
				Statue.SetIntProperty("Cleo_TerraFirma_IsVain", 1);
				Statue.SetGender(Creature.GetGender());
				Statue.SetPronounSet(Creature.GetPronounSet());
			}
			else
			{
				Statue.SetGender("nonspecific");
				Statue.SetPronounSet("it/its");
			}
			Statue.Brain.Allegiance.Clear();
			Statue.Brain.BecomeCompanionOf(Sculptor, !infiniteDuration);
			//MessageQueue.AddPlayerMessage($"assigning convo script...");
			Statue.RequirePart<ConversationScript>().ConversationID = "Cleo_TerraFirma_ConversationStoneshaperStatue";
			// the statue fights AND draws aggro: nearby enemies must save vs Willpower or attack it instead of the sculptor.
			// a successful save grants long DistractionImmunity, so strong-willed enemies opt out once and stay out.
			Statue.AddPart(new Distraction
			{
				Source = Statue,
				Original = Sculptor,
				SaveVs = "Stoneshaper Distraction",
				SaveStat = "Willpower",
				SaveTarget = GetDistractionSaveDifficulty(Lv)
			});
		}

		/// <summary>
		/// Renders an animated statue back down to inert, lootable stone in place.
		/// The inert statue keeps the animated one's appearance and entombs everything it wore or carried.
		/// Used on natural expiry and on permanent-statue swaps; violent deaths go through the normal death path instead.
		/// </summary>
		public static void DeanimateStatue(GameObject Statue)
		{
			Statue.PlayWorldSound("Sounds/Damage/sfx_destroy_stone", 1f, Combat: true);
			Cell cell = Statue.CurrentCell;
			GameObject inert = GameObject.Create("Cleo_TerraFirma_StoneshaperStatue");
			inert.Render.DisplayName = Statue.Render.DisplayName;
			inert.Render.RenderString = Statue.Render.RenderString;
			inert.Render.Tile = Statue.Render.Tile;
			inert.Render.ColorString = Statue.Render.ColorString;
			inert.Render.DetailColor = Statue.Render.DetailColor;
			inert.Render.TileColor = Statue.Render.TileColor;
			Description statueDesc = Statue.GetPart<Description>();
			Description inertDesc = inert.GetPart<Description>();
			if (statueDesc != null && inertDesc != null)
				inertDesc._Short = statueDesc._Short;
			StatueHelpers.EntombPossessions(Statue, inert);
			Statue.SetStringProperty("CustomDeathVerb", "expired");
			Statue.Obliterate(Silent: true);
			cell?.AddObject(inert, true, Silent: true);
		}

		/// <summary>
		/// Creates a statue of the provided creature, using a wall as reference for several unique aesthetic properties.
		/// Stat shifts do not happen here; the result statue will be statistically normal in every way.
		/// </summary>
		/// <param name="Muse">The <see cref="GameObject"/> to be depicted. <b>Must be a creature.</b></param>
		/// <param name="Wall">The wall used as reference. The statue will inherit its physics properties and sprite colors.</param>
		/// <param name="NamePrefix">If non-null, the statue's display name will be altered to include this as a prefix.</param>
		/// <param name="WallProperties">If non-null, the statue will use the material listed for its name.</param>
		/// <returns>The created statue, or null if one or more of the arguments are invalid.</returns>
		public static GameObject MakeStatueOfCreatureFromWall(GameObject Muse, GameObject Wall, string NamePrefix = "", Cleo_TerraFirma_StoneshaperWallProperties WallProperties = null)
		{
			if (!Muse.IsCreature || !Wall.IsWall())
				return null;

			GameObject statue = GameObject.Create("Cleo_TerraFirma_StoneshaperStatue");
			RandomStatue randomStatuePart = statue.RequirePart<RandomStatue>();
			randomStatuePart.Material = WallProperties?.MaterialDisplayName ?? WallProperties?.MaterialType ?? "stone";
			randomStatuePart.SetCreature(Muse);
			statue.DisplayName = $"{(NamePrefix != string.Empty ? $"{NamePrefix} " : "")}{statue.DisplayName}";

			// a rough statue can't "intricately depict" anything (friend's catch via the feedback
			// thread; RULED 2026-08-07, "crudely" = user's word). Only tier 1 contradicts the base
			// RandomStatue sentence, so only tier 1 diverges from it; the descriptor switch is the
			// hook if a full per-tier ladder ever lands. Rides into the inert copy via DeanimateStatue.
			if (NamePrefix == "rough")
			{
				Description roughDesc = statue.GetPart<Description>();
				if (roughDesc != null)
					roughDesc._Short = roughDesc._Short.Replace(" intricately depicts ", " crudely depicts ");
			}

			statue.Physics.FlameTemperature = Wall.Physics.FlameTemperature;
			statue.Physics.VaporTemperature = Wall.Physics.VaporTemperature;
			statue.Physics.FreezeTemperature = Wall.Physics.FreezeTemperature;
			statue.Physics.BrittleTemperature = Wall.Physics.BrittleTemperature;
			statue.Physics.Organic = Wall.Physics.Organic;
			statue.Physics.Conductivity = Wall.Physics.Conductivity;

			statue.Render.ColorString = Wall.Render.ColorString;
			statue.Render.DetailColor = Wall.Render.DetailColor;
			statue.Render.TileColor = Wall.Render.TileColor;

			return statue;
		}

		public static int GetDuration(int Lv) => Lv >= 10 ? INFINITE_DURATION_PLACEHOLDER : BASE_DURATION + (Lv * TURNS_PER_LEVEL);

		public static int GetHP(int Lv) => Lv * 25;

		public static int GetAV(int Lv) => 3 + Mathf.FloorToInt(Lv / 2);

		public static WallType GetValidWallsAtLevel(int Lv)
		{
			if (Lv < 5)
				return WallType.Stone;
			else if (Lv < 10)
				return WallType.StoneAndMetal;
			return WallType.All;
		}

		public static int GetDistractionSaveDifficulty(int Lv)
		{
			return 10 + Lv;
		}

		public static string GetNaturalDamage(int Lv)
		{
			float dice = 1 + MathF.Floor(Lv / 3);
			return $"{dice}d3";
		}

		// GetToHitBonus RETIRED (identity audit, RULED 2026-08-09): the flat +Lv/3 stacked on
		// the muse's inherited Agility, which already carries the statue's accuracy. Alongside
		// the vibro cut (fist pens ride Strength, natural-weapon idiom), the muse's physique
		// IS the combat profile now -- pick your likeness with intent.
		#endregion
	}
}
