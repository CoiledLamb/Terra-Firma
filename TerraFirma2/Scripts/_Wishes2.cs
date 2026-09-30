using System.Collections.Generic;
using XRL;
using XRL.Messages;
using XRL.Rules;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace Cleo.TerraFirma.Scripts
{
	/// <summary>Terra Firma 2 staging wishes, split from the base mod's WishHandler 2026-09-27 when TF2 became its own assembly.</summary>
	[HasWishCommand]
	public class TerraFirma2Wishes
	{
		/// <summary>Creature staging wish (user 2026-08-15, opening the creatures branch: a
		/// branch "for the creatures, including stonecarvers, so i can generate them in game";
		/// staging-wish-first chosen at the same sitting, the real spawn wiring deliberately
		/// after). Lines the whole roster up east of the player -- brains pacified and frozen,
		/// godmode on -- so they can be walked past and looked at instead of fought. The
		/// tfgazewall idiom: one wish, the subjects side by side.
		///
		/// Every spawn logs its Render tile string, so art the deploy never carried shows in
		/// the log as well as on screen (rockhound and snapjaw miner both sat on undeployed
		/// sprites from 2026-08-11 until the 0.1.2 deploy).
		///
		/// The last two entries are SPRITE CHECKS, NOT CREATURES: no stonecarver NPC blueprint
		/// exists yet -- the roster build is this branch's real job and its color map still has
		/// unruled slots -- so the wish paints stonecarver.png and stonerat.png onto plain
		/// WatervineFarmer bodies at runtime. Nothing about the roster is committed by that; it
		/// is the art standing in the world at true scale, and it ships no design.
		///
		/// Wish again to release: brains restored exactly as they were, godmode off.
		/// BRANCH-ONLY -- this does not ride 0.1.2.</summary>
		private static readonly (string Blueprint, string Note)[] CreatureRoster = new[]
		{
			("Cleo_TerraFirma_Cobble Baboon", "EB 4: orbiting pebbles, thrown stones, visible whorl. Butchers into cobblefur."),
			("Cleo_TerraFirma_Rockhound", "RockSweeper gut -- swallow and spill."),
			("Cleo_TerraFirma_Snapjaw Miner", "the pickaxe IS the AI: Digging installed, PathAsBurrower answered."),
			("Cleo_TerraFirma_Burrowing Urchin", "wall-walker, Burrowing 5, aculei in the belly."),
			("Cleo_TerraFirma_BoarVessel", "Husangeion -- passive, 64-dram tank, glaze tracks the liquid."),
		};

		private static readonly (string Tile, string Label)[] CarverSpriteChecks = new[]
		{
			("stonecarver.png", "STONECARVER SPRITE -- human base, no blueprint yet"),
			("stonerat.png", "STONERAT SPRITE -- mole base, no blueprint yet"),
		};

		private static readonly List<(GameObject Object, bool Mobile, bool Wanders, bool Hostile)> StagedRoster =
			new List<(GameObject, bool, bool, bool)>();

		[WishCommand(Command = "tfcreatures")]
		public static void CreatureStage()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (origin == null || zone == null)
				return;

			if (StagedRoster.Count > 0)
			{
				int released = 0;
				foreach (var staged in StagedRoster)
				{
					if (staged.Object == null || !staged.Object.IsValid() || staged.Object.Brain == null)
						continue;
					staged.Object.Brain.Mobile = staged.Mobile;
					staged.Object.Brain.Wanders = staged.Wanders;
					staged.Object.Brain.Hostile = staged.Hostile;
					released++;
				}
				StagedRoster.Clear();
				The.Core.IDKFA = false;
				Helpers.VerifyLog("STAGE", $"tfcreatures: released {released}, godmode OFF");
				MessageQueue.AddPlayerMessage($"tfcreatures: {released} released -- brains restored as they were, godmode OFF. The menagerie is yours.", Capitalize: false);
				return;
			}

			int slot = 0;
			Cell NextCell()
			{
				while (slot < 25)
				{
					int dx = 2 + slot;
					slot++;
					foreach (int dy in new int[] { 0, -1, 1, -2, 2 })
					{
						int x = origin.X + dx;
						int y = origin.Y + dy;
						if (x < 0 || y < 0 || x >= zone.Width || y >= zone.Height)
							continue;
						Cell candidate = zone.GetCell(x, y);
						if (candidate == null || candidate.IsSolid() || candidate.HasObjectWithPart("Combat"))
							continue;
						return candidate;
					}
				}
				return null;
			}

			void Stage(GameObject subject, Cell where, string label)
			{
				where.AddObject(subject);
				subject.MakeActive();
				if (subject.Brain != null)
				{
					StagedRoster.Add((subject, subject.Brain.Mobile, subject.Brain.Wanders, subject.Brain.Hostile));
					subject.Brain.Mobile = false;   // stand still and be looked at
					subject.Brain.Wanders = false;
					subject.Brain.Hostile = false;
				}
				string tile = subject.Render?.Tile;
				Helpers.VerifyLog("STAGE", $"{label} at {where.X},{where.Y}: blueprint={subject.Blueprint} tile=\"{(tile.IsNullOrEmpty() ? "(none -- renders as its glyph)" : tile)}\" name=\"{subject.DisplayNameOnlyDirect}\"");
			}

			The.Core.IDKFA = true;
			int placed = 0;
			List<string> legend = new List<string>();
			List<string> trouble = new List<string>();

			foreach (var entry in CreatureRoster)
			{
				GameObject subject = null;
				try
				{
					subject = GameObject.Create(entry.Blueprint);
				}
				catch (System.Exception ex)
				{
					trouble.Add($"{entry.Blueprint} would not create ({ex.GetType().Name})");
					Helpers.VerifyLog("STAGE", $"{entry.Blueprint}: CREATE FAILED ({ex.GetType().Name}) -- blueprint missing or malformed");
					continue;
				}
				if (subject == null)
				{
					trouble.Add($"{entry.Blueprint} came back null -- blueprint missing?");
					Helpers.VerifyLog("STAGE", $"{entry.Blueprint}: CREATE returned null");
					continue;
				}
				Cell where = NextCell();
				if (where == null)
				{
					subject.Obliterate();
					trouble.Add($"{entry.Blueprint}: no open cell left in the row");
					continue;
				}
				Stage(subject, where, entry.Blueprint);
				placed++;
				legend.Add($"{placed}. {subject.DisplayNameOnlyDirect} -- {entry.Note}");
			}

			foreach (var check in CarverSpriteChecks)
			{
				GameObject subject = null;
				try
				{
					subject = GameObject.Create("WatervineFarmer");
				}
				catch (System.Exception ex)
				{
					trouble.Add($"sprite check {check.Tile}: no chassis ({ex.GetType().Name})");
					continue;
				}
				if (subject == null)
				{
					trouble.Add($"sprite check {check.Tile}: WatervineFarmer chassis came back null");
					continue;
				}
				Cell where = NextCell();
				if (where == null)
				{
					subject.Obliterate();
					trouble.Add($"sprite check {check.Tile}: no open cell left in the row");
					continue;
				}
				subject.Render.Tile = check.Tile;
				subject.Render.DisplayName = check.Label;
				Stage(subject, where, check.Label);
				placed++;
				legend.Add($"{placed}. {check.Label} -- art only, on a plain human body.");
			}

			MessageQueue.AddPlayerMessage($"tfcreatures: {placed} staged in a row east of you, frozen and pacified, godmode ON. Wish again to release them.", Capitalize: false);
			foreach (string line in legend)
				MessageQueue.AddPlayerMessage(line, Capitalize: false);
			foreach (string line in trouble)
				MessageQueue.AddPlayerMessage("tfcreatures TROUBLE: " + line, Capitalize: false);
		}
		/// <summary>Stonecarver pack stager. Shape is the user's, 2026-08-15: "i imagined the pack
		/// would spawn a mix of each type from a poptable, with one leader." It now does exactly
		/// that, because the roles finally exist to mix.
		///
		/// Rank and file roll from `Cleo_TerraFirma_Stonecarver Pack` (SnapjawParty1 idiom), so the
		/// composition varies per wish and the rockhound turns up SOMETIMES, per its own ruling.
		/// The leader is spawned separately: a PLAIN carver chassis run through the epithet builder.
		/// That is not a shortcut, it is the ruled structure showing its shape. Rank and file take
		/// their mutation from their PROFESSION; the overseer takes its mutation from its NAME. It
		/// needs no profession blueprint for the same reason it needs no bespoke art.
		///
		/// Printed to chat: the leader's full roll, then the actual composition of the pack that
		/// spawned, so the table's weights can be judged from real draws rather than from the XML.
		///
		/// Pacified and frozen, sharing the tfcreatures staging list, so either wish's second call
		/// releases everything. BRANCH-ONLY.</summary>
		private const string CarverPackTable = "Cleo_TerraFirma_Stonecarver Pack";

		[WishCommand(Command = "tfcarverpack")]
		public static void CarverPack()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			Zone zone = origin?.ParentZone;
			if (origin == null || zone == null)
				return;

			if (StagedRoster.Count > 0)
			{
				CreatureStage(); // one release path for both wishes
				return;
			}

			List<Cell> ring = origin.GetLocalAdjacentCells(6);
			ring.RemoveAll(c => c.IsSolid() || c.HasObjectWithPart("Combat") || origin.DistanceTo(c) < 3);

			Cell TakeCell()
			{
				if (ring.Count == 0)
					return null;
				int idx = Stat.Random(0, ring.Count - 1);
				Cell c = ring[idx];
				ring.RemoveAt(idx);
				return c;
			}

			void Freeze(GameObject who)
			{
				who.MakeActive();
				if (who.Brain == null)
					return;
				StagedRoster.Add((who, who.Brain.Mobile, who.Brain.Wanders, who.Brain.Hostile));
				who.Brain.Mobile = false;
				who.Brain.Wanders = false;
				who.Brain.Hostile = false;
			}

			// the leader first, so a cramped spot still gets the thing worth looking at
			Cell leaderCell = TakeCell();
			if (leaderCell == null)
			{
				MessageQueue.AddPlayerMessage("tfcarverpack: no open cell at range 3-6, move somewhere roomier.", Capitalize: false);
				return;
			}
			GameObject overseer = GameObject.Create("Cleo_TerraFirma_Stonecarver");
			if (overseer == null)
			{
				MessageQueue.AddPlayerMessage("tfcarverpack: stonecarver chassis came back null.", Capitalize: false);
				return;
			}
			StonecarverEpithet.Roll roll = StonecarverEpithet.Build(overseer);
			leaderCell.AddObject(overseer);
			Freeze(overseer);
			Helpers.VerifyLog("CARVER", $"overseer: name={roll.PersonalName} ({roll.NamePieces}) byname={roll.Byname} material={roll.Material} -> {roll.PerkText} craft={roll.Craft} -> {roll.KitText}");

			var census = new Dictionary<string, int>();
			int spawned = 0;
			List<string> trouble = new List<string>();
			foreach (var entry in PopulationManager.Generate(CarverPackTable))
			{
				for (int n = 0; n < entry.Number; n++)
				{
					Cell where = TakeCell();
					if (where == null)
					{
						trouble.Add("ran out of open cells, pack truncated");
						break;
					}
					GameObject member = null;
					try
					{
						member = GameObject.Create(entry.Blueprint);
					}
					catch (System.Exception ex)
					{
						trouble.Add($"{entry.Blueprint} would not create ({ex.GetType().Name})");
						continue;
					}
					if (member == null)
					{
						trouble.Add($"{entry.Blueprint} came back null");
						continue;
					}
					where.AddObject(member);
					Freeze(member);
					string label = member.DisplayNameOnlyDirect;
					census[label] = (census.ContainsKey(label) ? census[label] : 0) + 1;
					spawned++;
				}
			}

			The.Core.IDKFA = true;
			MessageQueue.AddPlayerMessage($"tfcarverpack: 1 overseer + {spawned} rank and file, frozen and pacified, godmode ON. Wish again (or tfcreatures) to release.", Capitalize: false);
			MessageQueue.AddPlayerMessage($"overseer: {{{{W|{roll.PersonalName} {roll.Byname}}}}} (legendary, pink)", Capitalize: false);
			MessageQueue.AddPlayerMessage($"  craft \"{roll.Craft}\" grants {roll.KitText}; material \"{roll.Material}\" grants {roll.PerkText}", Capitalize: false);
			MessageQueue.AddPlayerMessage($"  name assembled from {roll.NamePieces}", Capitalize: false);
			List<string> lines = new List<string>();
			foreach (var pair in census)
				lines.Add($"{pair.Value}x {pair.Key}");
			MessageQueue.AddPlayerMessage(spawned > 0
				? $"this draw: {string.Join(", ", lines)}. Weights are DRAFT, judge them from a few draws."
				: "this draw came up empty, which the table allows. Wish again.", Capitalize: false);
			foreach (string line in trouble)
				MessageQueue.AddPlayerMessage("tfcarverpack TROUBLE: " + line, Capitalize: false);
			Helpers.VerifyLog("CARVER", $"tfcarverpack: overseer + {spawned} rank and file [{string.Join("/", lines)}]");
		}
		/// <summary>Reputation jingle audition (user 2026-08-15: "can i listen to the files and
		/// judge myself?"). The clips are packed inside Unity's resources.assets with no loose ogg
		/// anywhere, so extracting them would need AssetRipper. Playing them does not: they are
		/// ordinary engine sound paths and the mod already plays those.
		///
		/// tfjingle cycles the candidates for the Quarriers' Guild, one per wish, naming each
		/// before it plays. tfjingleneg plays the NEGATIVE half of whichever is currently selected,
		/// because a faction picks a PAIR and the sour one is half of what you are choosing.
		///
		/// Sharing a jingle is base-game practice, not a compromise: sultanCult is used by five
		/// factions and village by four. BRANCH-ONLY.</summary>
		private static readonly (string Key, string Why)[] JingleCandidates = new[]
		{
			("mopango", "the underground dwellers; your own tracker already runs a mopango-symmetry thread, and you just put the mole on the emblem"),
			("cragmensch", "the stone-creature faction, the closest thing in the game to a stone people"),
			("barathrumites", "underground artisans, guild-adjacent, but they read strongly as tinkers"),
			("farmersGuild", "what the guild is set to now: workaday, institutional"),
			("merchantsGuild", "the other guild; the civic register at its most formal"),
			("hermits", "outsider register, if quarriers should sound apart from the settled world"),
			("issachariTribe", "a desert people rather than a trade body"),
		};

		private static int JingleIndex = -1;

		/// <summary>Carving picker test (carving spec 2026-09-29): opens the shared subject
		/// picker and reports the choice. Nothing is carved or consumed.</summary>
		[WishCommand(Command = "tfcarvepick")]
		public static void CarvePickTest()
		{
			string pick = Cleo_TerraFirma_CarvingPicker.Pick("");
			if (pick == null)
			{
				MessageQueue.AddPlayerMessage("[tfcarvepick] cancelled");
				return;
			}
			GameObjectBlueprint bp = GameObjectFactory.Factory.GetBlueprintIfExists(pick);
			MessageQueue.AddPlayerMessage("[tfcarvepick] " + pick + " (" + bp?.CachedDisplayNameStripped + ")");
		}

		[WishCommand(Command = "tfjingle")]
		public static void JingleAudition()
		{
			JingleIndex = (JingleIndex + 1) % JingleCandidates.Length;
			var pick = JingleCandidates[JingleIndex];
			string path = $"Sounds/Reputation/sfx_reputation_{pick.Key}_positive";
			SoundManager.PreloadClipSet(path);
			The.Player?.PlayWorldSound(path, 1f);
			MessageQueue.AddPlayerMessage($"tfjingle [{JingleIndex + 1}/{JingleCandidates.Length}] {{{{W|{pick.Key}}}}} positive: {pick.Why}", Capitalize: false);
			MessageQueue.AddPlayerMessage("wish tfjingle again for the next candidate, or tfjingleneg to hear this one's sour half.", Capitalize: false);
			Helpers.VerifyLog("JINGLE", $"audition {pick.Key} positive");
		}

		[WishCommand(Command = "tfjingleneg")]
		public static void JingleAuditionNegative()
		{
			if (JingleIndex < 0)
			{
				MessageQueue.AddPlayerMessage("tfjingleneg: nothing selected yet, wish tfjingle first.", Capitalize: false);
				return;
			}
			var pick = JingleCandidates[JingleIndex];
			string path = $"Sounds/Reputation/sfx_reputation_{pick.Key}_negative";
			SoundManager.PreloadClipSet(path);
			The.Player?.PlayWorldSound(path, 1f);
			MessageQueue.AddPlayerMessage($"tfjingleneg: {{{{W|{pick.Key}}}}} negative.", Capitalize: false);
			Helpers.VerifyLog("JINGLE", $"audition {pick.Key} negative");
		}

		/// <summary>Gravel shotgun and both supported stone sizes for a manual playtest.</summary>
		[WishCommand(Command = "tfgravel")]
		public static void GravelShotgunKit()
		{
			if (The.Player == null) return;
			GameObject gun = GameObject.Create("Cleo_TerraFirma_Gravel Shotgun");
			gun.MakeUnderstood();
			The.Player.ReceiveObject(gun);
			The.Player.ReceiveObject(GameObject.Create("Small Stone"));
			The.Player.ReceiveObject(GameObject.Create("Large Stone"));
			MessageQueue.AddPlayerMessage("tfgravel: grinder shotgun, small stone and large stone received.", Capitalize: false);
		}

		/// <summary>Glowstone pair dev door (2026-08-22): a glowsphere to break, a lantern to
		/// load. Everything else about the loop is reachable from these two.</summary>
		[WishCommand(Command = "tflantern")]
		public static void GlowstoneKit()
		{
			GameObject Ply = The.Player;
			if (Ply == null)
				return;
			Ply.ReceiveObject(GameObject.Create("Glowsphere"));
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_GlowstoneLantern"));
			MessageQueue.AddPlayerMessage("tflantern: glowsphere + lantern received. break the sphere, load a shard.", Capitalize: false);
			Helpers.VerifyLog("LANTERN", "tflantern kit granted");
		}

		/// <summary>Grinder cell dev door (2026-08-22): the cell plus a handful of stones to feed it.</summary>
		[WishCommand(Command = "tfgrinder")]
		public static void GrinderKit()
		{
			GameObject Ply = The.Player;
			if (Ply == null)
				return;
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_GrinderCell"));
			for (int i = 0; i < 3; i++)
				Ply.ReceiveObject(GameObject.Create("Small Stone"));
			MessageQueue.AddPlayerMessage("tfgrinder: piezoelectric cell + 3 small stones received. feed it.", Capitalize: false);
			Helpers.VerifyLog("GRINDER", "tfgrinder kit granted");
		}

		/// <summary>Mole cricket dev door (2026-08-22).</summary>
		[WishCommand(Command = "tfcricket")]
		public static void CricketKit()
		{
			GameObject Ply = The.Player;
			if (Ply == null)
				return;
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_MoleCricket"));
			MessageQueue.AddPlayerMessage("tfcricket: mole cricket received. equip it floating nearby; unequip kills it.", Capitalize: false);
			Helpers.VerifyLog("CRICKET", "tfcricket granted");
		}

		/// <summary>Seismophone dev door (2026-08-23): the ear plus a piezoelectric cell to feed it.</summary>
		[WishCommand(Command = "tfseismophone")]
		public static void SeismophoneKit()
		{
			GameObject Ply = The.Player;
			if (Ply == null)
				return;
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_Seismophone"));
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_GrinderCell"));
			MessageQueue.AddPlayerMessage("tfseismophone: seismophone + piezoelectric cell received. socket the cell, switch it on, stand by a wall.", Capitalize: false);
			Helpers.VerifyLog("SEISMO", "tfseismophone kit granted");
		}

		/// <summary>Animation table dev door (2026-08-24): the table placed beside you plus
		/// figurines in two materials to feed it.</summary>
		[WishCommand(Command = "tffigtable")]
		public static void AnimationTableKit()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			if (origin == null)
				return;
			Cell spot = origin.GetFirstEmptyAdjacentCell(1, 2);
			if (spot == null)
			{
				MessageQueue.AddPlayerMessage("tffigtable: no open ground for the table.", Capitalize: false);
				return;
			}
			spot.AddObject(GameObject.Create("Cleo_TerraFirma_AnimationTable"));
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_Random Stone Figurine"));
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_Random Stone Figurine"));
			Ply.ReceiveObject(GameObject.Create("Random Copper Figurine"));
			MessageQueue.AddPlayerMessage("tffigtable: animation table placed + 3 figurines received. interact with the table.", Capitalize: false);
			Helpers.VerifyLog("FIGTABLE", "tffigtable kit granted");
		}

		/// <summary>Box of chalk dev door (2026-08-22): the box plus a chalkboard to write on.</summary>
		[WishCommand(Command = "tfchalk")]
		public static void ChalkKit()
		{
			GameObject Ply = The.Player;
			Cell origin = Ply?.CurrentCell;
			if (origin == null)
				return;
			Ply.ReceiveObject(GameObject.Create("Cleo_TerraFirma_BoxOfChalk"));
			Cell boardCell = origin.GetCellFromDirection("N") ?? origin;
			boardCell.AddObject(GameObject.Create("Chalkboard"));
			MessageQueue.AddPlayerMessage("tfchalk: box of chalk received, chalkboard placed north. draw and write.", Capitalize: false);
			Helpers.VerifyLog("CHALK", "tfchalk kit granted");
		}
	}

	/// <summary>Dev wishes moved out of real-code files 2026-09-30 so the player build can leave
	/// this whole file out (tools/publish-beta.ps1 exclude list). Bodies stay where their state
	/// lives; these are thin doors.</summary>
	[HasWishCommand]
	public static class TerraFirma2DevWishes
	{
		[WishCommand(Command = "tfwarrenstats")]
		public static void WarrenStats() => WarrenSurvey.RunSurvey(true);

		[WishCommand(Command = "tfwarrendress")]
		public static void WarrenDress() => WarrenCarver.CompareWorldFurnishing();

		[WishCommand(Command = "tfshufflers")]
		public static void Shufflers() => The.Player.ReceiveObject("Cleo_TerraFirma_SeismicShufflers");
	}

	/// <summary>Dev wish: run the v2 carve on the CURRENT zone (random seed each
	/// call, so re-wishing rerolls; the world-build path uses the ZoneID seed
	/// instead). Rides the VERIFY_WATCH strip like the rest of the tf* harness.</summary>
	[HasWishCommand]
	public static class WarrenMakerTest
	{
		[WishCommand(Command = "tfwarrenv2")]
		public static void CarveHere()
		{
			Zone Z = The.Player?.CurrentZone;
			if (Z == null) return;
			int seed = XRL.Rules.Stat.Random(1, int.MaxValue - 1);
			var r = WarrenCarver.Build(Z, seed);
			MessageQueue.AddPlayerMessage(r.Built
				? $"tfwarrenv2: {r.Stage} warren, {r.Occupancy}, {r.Floor} floor, {r.Pockets} pockets (seed {r.Seed}, attempt {r.Attempt}). Roles in the log."
				: $"tfwarrenv2: {{{{R|no site}}}} - {r.Reason} (seed {r.Seed}).");
		}

		/// <summary>Testing wish (user ask 2026-08-23): reveal every warren secret on
		/// the world map at once, so verifying does not mean walking the desert.
		/// FORCE-BUILDS each claimed zone first (round 9 refinement, closing the
		/// first-visit hole for the test loop): duds erase their own notes during
		/// the build sweep, so every note this wish reveals is a REAL warren by
		/// construction. Keys on the "stonecarver" secret attribute.</summary>
		[WishCommand(Command = "tfwarrens")]
		public static void RevealAll()
		{
			// snapshot first: the build sweep deletes dud notes out from under us
			var claims = new System.Collections.Generic.List<Qud.API.JournalMapNote>();
			foreach (var note in Qud.API.JournalAPI.MapNotes)
				if (note.Attributes != null && note.Attributes.Contains("stonecarver")) claims.Add(note);
			int built = 0;
			foreach (var note in claims)
			{
				if (!The.ZoneManager.IsZoneBuilt(note.ZoneID))
				{
					The.ZoneManager.GetZone(note.ZoneID);   // builds; a refusal erases its note
					built++;
				}
			}
			int revealed = 0, real = 0;
			foreach (var note in claims)
			{
				if (Qud.API.JournalAPI.GetMapNote(note.ID) == null) continue;   // erased in the sweep
				real++;
				if (note.Revealed) continue;
				Qud.API.JournalAPI.RevealMapNote(note, silent: true);
				revealed++;
			}
			Helpers.VerifyLog("WARREN", $"tfwarrens: {claims.Count} claims, {built} zones built in the sweep, {real} real warrens, {revealed} newly revealed");
			MessageQueue.AddPlayerMessage(claims.Count > 0
				? $"tfwarrens: {real} real warrens on the map ({claims.Count} were claimed; the difference erased itself). Every note is a warren."
				: "tfwarrens: {{R|no warren secrets in this world}} - was it embarked before the placement extension?");
		}
	}
}
