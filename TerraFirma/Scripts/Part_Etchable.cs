using System;
using System.Collections.Generic;
using ConsoleLib.Console;
using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World.Anatomy;
using Cleo.TerraFirma.Scripts;

namespace XRL.World.Parts
{
	/// <summary>
	/// The etching verb (feature promoted from the gearset pitch 2026-08-05; user rulings same
	/// day: the verb lives on the TARGET rather than the tool, targets are statues and non-metal
	/// walls (held rocks are out), the inscription is typed OR markov-generated behind
	/// semi-flavored menu options, and re-etching is allowed with the flavor of striking a thin
	/// sheet off the old face). Merged onto the base Wall / Statue / Random Statue blueprints in
	/// Merges.xml, so Stoneshaper statues and the redoubt figure inherit it for free.
	///
	/// The verb only shows when the actor carries both halves of an etching kit (feedback
	/// ruling 2026-08-06: "possible as long as you're holding both a hammer and knife") —
	/// a striker (Cudgel-skill weapon, or the Cleo_TerraFirma_EtchTool "Mallet" tag) and an
	/// edge (ShortBlades-skill weapon, or the "Chisel" tag). The mod's own mallet and chisel
	/// inherit BaseCudgel/BaseDagger, so they qualify through the same skill gate; the tag
	/// remains an override lane for future tools that aren't weapons at all. The pair's
	/// tier (the LESSER of the two) drives the letter-garble roll below.
	/// Wall gate rides the audited Cleo_TerraFirma_StoneshaperWallProperties merges: a wall
	/// with no properties part refuses (un-audited family), and MaterialType "metal" refuses
	/// per the ruling. Statues etch freely, including animated ones.
	///
	/// Inscription storage is one plain string field, the base game's own Graffitied idiom,
	/// so serialization is safe by construction. Markov text rides the LibraryCorpus exactly
	/// the way Graffitied does, minus the weathering obfuscation: this carving is fresh.
	/// </summary>
	[Serializable]
	public class Cleo_TerraFirma_Etchable : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_Etch";
		public const string TOOL_TAG = "Cleo_TerraFirma_EtchTool";
		public const string CORPUS = "LibraryCorpus.json";
		// AskString whitelist: prose punctuation, no markup metacharacters
		private const string TYPED_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .,!?'-:;()";
		private const int TYPED_MAX = 60;
		// Garble dials (feedback 2026-08-06, REROUTED 2026-08-07 through the graffiti law --
		// user, with the graffitied-canvas screenshot: "i want the garbled text to work like
		// graffiti"). Slip chance = (CEILING - pairTier) * PCT feeds Grammar.Obfuscate's
		// noiseValue, pair tier = the LESSER of the striker's and edge's Tier tags: tier 0
		// slips 24% of characters, tier 3 slips 12%, tier 6+ carves clean. BOTH numbers are
		// CLAUDE DIALS awaiting ratification — and note the mod's own blunted chisel (tier 1)
		// + splintered mallet (tier 0) land at tier 0, so the starter kit garbles hard; the
		// worn-tool names read as intent, but raising their Tier tags in ObjectBlueprints.xml
		// is the one-line veto if the starter kit should carve clean.
		private const int GARBLE_TIER_CEILING = 6;
		private const int GARBLE_PCT_PER_TIER = 4;
		// Improvised penalty (user 2026-08-15, on the proper-tools build: they wanted purpose made
		// tools to beat "a carbide dagger and hammer being used in place of it"). Before this, the
		// tag bought NOTHING: any ShortBlades edge and any Cudgel striker counted at face tier, so
		// a carbide dagger etched exactly as cleanly as a carbide chisel and the whole tool ladder
		// was cosmetic against improvisation. Written as a PENALTY on improvised tools rather than
		// a bonus on proper ones so that nothing already tuned moves: the blunted pair stays at its
		// ruled 24%, steel stays 16%, carbide stays 8%, and only borrowed weapons get worse.
		// Two rungs is the gap, so an improvised tool always carves like a purpose made one two
		// tiers below it. Clamped at 0 so a tier 0 club cannot push garble past the 24% ceiling.
		// CLAUDE DIAL awaiting ratification, same as the two above it.
		private const int IMPROVISED_TIER_PENALTY = 2;

		public string Inscription;

		/// <summary>
		/// Per-limb inscriptions for bodied statues (user ruling 2026-08-06: "etching a statue
		/// should bring up 'what limb do you want to etch' like tattoo guns"), keyed by BodyPart
		/// ID exactly the way the engine's Tattoos part keys its Descriptions. NonSerialized +
		/// explicit Write/Read below (the Tattoos precedent; the field writer is not trusted
		/// with dictionaries, per the AssocWhorl lesson). Bodiless surfaces (walls, plain
		/// statues) keep the single Inscription field untouched.
		/// </summary>
		[NonSerialized]
		public Dictionary<int, string> LimbInscriptions = new Dictionary<int, string>();

		/// <summary>Bump when the serialized shape changes again; Read branches on it.</summary>
		private const int FORMAT_VERSION = 3;

		public override void Write(GameObject Basis, SerializationWriter Writer)
		{
			Writer.Write(Helpers.SERIAL_SENTINEL);
			Writer.Write(FORMAT_VERSION);
			WriteLimbs(Writer);
			base.Write(Basis, Writer);
		}

		public override void Read(GameObject Basis, SerializationReader Reader)
		{
			// legacy blocks (v1 friends-beta: bare reflection string; v2 local: bare count
			// first) fail this check having cost only 4 bytes; ReadError recovers or resets
			if (Reader.ReadInt32() != Helpers.SERIAL_SENTINEL)
				throw new FormatException("Cleo_TerraFirma_Etchable: pre-sentinel block");
			Reader.ReadInt32(); // FORMAT_VERSION: branch here when the shape next changes
			ReadLimbs(Reader);
			base.Read(Basis, Reader);
		}

		private void WriteLimbs(SerializationWriter Writer)
		{
			Writer.Write(LimbInscriptions.Count);
			foreach (KeyValuePair<int, string> kv in LimbInscriptions)
			{
				Writer.Write(kv.Key);
				Writer.Write(kv.Value);
			}
		}

		private void ReadLimbs(SerializationReader Reader)
		{
			int n = Reader.ReadInt32();
			if (n < 0 || n > 1024)
				throw new FormatException("Cleo_TerraFirma_Etchable: implausible limb count " + n);
			LimbInscriptions = new Dictionary<int, string>(n);
			for (int i = 0; i < n; i++)
			{
				int key = Reader.ReadInt32();
				LimbInscriptions[key] = Reader.ReadString();
			}
		}

		/// <summary>
		/// Save-compat recovery (2026-08-07 shim): Etchable rides the base Wall merge, so EVERY
		/// wall of a pre-sentinel save carries a legacy block -- without this, each one logs an
		/// engine exception on zone thaw and misparse overruns can bleed into neighboring parts.
		/// First re-parse the block as the v2 local layout (count + pairs + reflection fields)
		/// and accept it only on EXACT end-of-block alignment -- that recovers every inscription
		/// from 2026-08-05..07 local saves while safely rejecting v1 beta blocks (their length
		/// can't align). Anything else resets to a blank etchable. Either way the reader seeks
		/// cleanly to the block end and the engine logs nothing.
		/// </summary>
		public override bool ReadError(Exception Exception, SerializationReader Reader, long Start, int Length)
		{
			try
			{
				Reader.Stream.Position = Start;
				Reader.ReadTokenizedType();
				StatShifter.Load(Reader, ParentObject); // discard: this part never shifts stats
				ReadLimbs(Reader);
				base.Read(ParentObject, Reader);
				if (Reader.Stream.Position == Start + Length)
				{
					Helpers.VerifyLog("SERIAL", $"Etchable v2 legacy block recovered in place ({LimbInscriptions.Count} limb inscription(s), inscription {(Inscription.IsNullOrEmpty() ? "empty" : "kept")})");
					return true;
				}
			}
			catch
			{
			}
			Inscription = null;
			LimbInscriptions = new Dictionary<int, string>();
			Reader.Stream.Position = Start + Length;
			Helpers.VerifyLog("SERIAL", $"Etchable legacy block reset ({Length} byte(s) skipped quietly)");
			return true;
		}

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == GetInventoryActionsEvent.ID
				|| ID == InventoryActionEvent.ID
				|| ID == GetShortDescriptionEvent.ID
				|| ID == PooledEvent<GetDisplayNameEvent>.ID;
		}

		/// <summary>Statues always answer; walls only if their audited properties part is
		/// present and not metal (USER ruling 2026-08-05: "walls (metal walls excluded)").</summary>
		public bool IsEtchableSurface()
		{
			if (ParentObject.IsWall())
			{
				Cleo_TerraFirma_StoneshaperWallProperties props = ParentObject.GetPart<Cleo_TerraFirma_StoneshaperWallProperties>();
				return props != null && props.MaterialType != "metal";
			}
			return true;
		}

		public static bool HasEtchTools(GameObject actor)
		{
			return GetEtchToolTier(actor) >= 0;
		}

		/// <summary>The best etching kit the actor carries, as a tier: -1 without a qualifying
		/// pair, else min(best striker, best edge). Feedback ruling 2026-08-06 ("as long as
		/// you're holding both a hammer and knife"): a striker is any Cudgel-skill weapon, an
		/// edge any ShortBlades-skill weapon; the Cleo_TerraFirma_EtchTool tag still counts
		/// its bearer regardless of skill (the override lane for future non-weapon tools).
		/// Long blades and axes deliberately do not qualify — hammer and knife, as ruled.
		/// The min() is the garble driver: a carve is only as clean as its worse half.</summary>
		public static int GetEtchToolTier(GameObject actor)
		{
			if (actor == null)
				return -1;
			int striker = -1, edge = -1;
			foreach (GameObject item in actor.GetInventoryAndEquipmentReadonly())
			{
				string kind = item.GetTag(TOOL_TAG);
				bool isStriker = kind == "Mallet";
				bool isEdge = kind == "Chisel";
				bool purposeMade = isStriker || isEdge;
				if (!purposeMade)
				{
					string skill = item.GetPart<MeleeWeapon>()?.Skill;
					if (skill == "Cudgel")
						isStriker = true;
					else if (skill == "ShortBlades")
						isEdge = true;
				}
				// a borrowed weapon carves like a real tool two rungs below it
				int tier = purposeMade
					? item.GetTier()
					: Math.Max(0, item.GetTier() - IMPROVISED_TIER_PENALTY);
				if (isStriker)
					striker = Math.Max(striker, tier);
				if (isEdge)
					edge = Math.Max(edge, tier);
			}
			return (striker < 0 || edge < 0) ? -1 : Math.Min(striker, edge);
		}

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			// quiet without the tools: the verb belongs to whoever carries chisel and mallet,
			// and every wall in the world showing an etch twiddle would be menu noise
			if (E.Actor != null && E.Actor.IsPlayer() && IsEtchableSurface() && HasEtchTools(E.Actor))
				E.AddAction("Etch", "etch", COMMAND, null, 'e', FireOnActor: false);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == COMMAND && E.Actor != null && E.Actor.IsPlayer())
				AttemptEtch(E.Actor);
			return base.HandleEvent(E);
		}

		private void AttemptEtch(GameObject actor)
		{
			if (!IsEtchableSurface())
				return;
			if (actor.DistanceTo(ParentObject) > 1)
			{
				IComponent<GameObject>.AddPlayerMessage("You cannot reach " + ParentObject.t() + "."); // DRAFT
				return;
			}
			int toolTier = GetEtchToolTier(actor);
			if (toolTier < 0)
			{
				IComponent<GameObject>.AddPlayerMessage("You want a hammer and a knife for that."); // DRAFT (safety branch; the verb is hidden without tools)
				return;
			}
			// bodied statues lead with the limb question, the tattoo gun's own idiom (user ruling
			// 2026-08-06): contact + non-abstract parts, cardinal names, escape backs out. A
			// bodiless target (walls, plain statues) skips straight to the old whole-surface flow.
			BodyPart limb = null;
			List<BodyPart> limbs = GetEtchableLimbs();
			if (limbs != null && limbs.Count > 0)
			{
				string[] limbNames = new string[limbs.Count];
				for (int i = 0; i < limbs.Count; i++)
					limbNames[i] = limbs[i].GetCardinalName();
				int pick = Popup.PickOption(
					Intro: "Choose a part to etch.", // DRAFT (TattooGun's prompt shape)
					Options: limbNames,
					AllowEscape: true);
				if (pick < 0)
					return;
				limb = limbs[pick];
			}
			bool reEtch = limb != null ? LimbInscriptions.ContainsKey(limb.ID) : !Inscription.IsNullOrEmpty();
			// semi-flavored two-way pick per the ruling; the two OPTION LINES are the USER'S OWN
			// VOICE (supplied + picked 2026-08-06, verbatim by standing rule -- do not polish);
			// the intro lines remain DRAFT for the release text pass
			int choice = Popup.PickOption(
				Intro: reEtch ? "Old words sit shallow in the stone." : "The face of the stone waits.", // DRAFT
				Options: new string[] { "Etch a message.", "Write from memory." },                      // USER VOICE, FINAL
				AllowEscape: true);
			string text = null;
			if (choice == 0)
			{
				// prompt aligned with the tattoo gun (feedback 2026-08-06) — its decomp-verified
				// shape is instruction + quoted example ("Describe your tattoo. For example: \"a
				// tiny snail\"."); the prose whitelist and 60-char cap stay ours (a carved message
				// wants punctuation the tattoo charset lacks)
				text = Popup.AskString("Etch your message. For example: \"live and drink\".", "", "Sounds/UI/ui_notification", TYPED_CHARS, null, TYPED_MAX); // DRAFT (example line is base-game idiom, not invented voice)
				if (text.IsNullOrEmpty())
					return;
			}
			else if (choice == 1)
			{
				MarkovBook.EnsureCorpusLoaded(CORPUS);
				if (!MarkovBook.CorpusData.TryGetValue(CORPUS, out MarkovChainData data))
				{
					IComponent<GameObject>.AddPlayerMessage("Nothing comes."); // DRAFT (corpus failed to load; logged by the engine)
					return;
				}
				// reroll template leftovers (the "=name=" class of token) the way MarkovBook's
				// own page loop does
				for (int i = 0; i < 5; i++)
				{
					text = MarkovChain.GenerateShortSentence(data).TrimEnd(' ');
					if (!text.Contains("="))
						break;
				}
			}
			else
			{
				return;
			}
			// the slip roll (feedback 2026-08-06): a low-tier kit garbles letters, typed and
			// markov alike — the chisel doesn't care where the words came from. The garbled
			// string IS the inscription: the stone keeps the botched carve, and the completion
			// popup echoes it honestly. Re-etching with better tools is the repair path.
			int garbled;
			text = GarbleByTier(text, toolTier, out garbled);
			if (garbled > 0)
				IComponent<GameObject>.AddPlayerMessage("Your rough tools slip, and some of the carving is lost."); // DRAFT (reworded for the block-glyph garble)
			if (reEtch)
				IComponent<GameObject>.AddPlayerMessage("You strike the old face away in a thin sheet of stone."); // DRAFT (re-etch ruling: "breaking off a sheet")
			bool firstEtch = Inscription.IsNullOrEmpty() && LimbInscriptions.Count == 0;
			if (limb != null)
				LimbInscriptions[limb.ID] = text;
			else
				Inscription = text;
			// the reveal (user 2026-08-07: "the same visual flair of carving away the top layer
			// to see the recolored item underneath" — and same day: NO random recolor, "it
			// should just flip the color of main to detail like it already does"): capture the
			// face's OLD dressing, flip beneath the cover, then run the stoneshaper carve
			// choreography — the cover block wears the pre-flip colors and is carved away band
			// by band, exposing the flipped face. Re-etches flip nothing; their cover matches
			// the face and the carve reads as re-working the same stone.
			string oldTile;
			char oldFg, oldDetail;
			CaptureFace(out oldTile, out oldFg, out oldDetail);
			if (firstEtch)
				ApplyEtchedLook();
			PlayEtchReveal(oldTile, oldFg, oldDetail);
			// push the flipped face into the frame buffer NOW (user live-catch 2026-08-07: "the
			// recolor doesn't seem to apply till *after* the animation is finished and the
			// message has displayed") -- the completion popup below BLOCKS the game thread, so
			// without a forced base render the world keeps showing the pre-flip frame straight
			// through the carve; the cover block is already enqueued, so the swap lands beneath
			// it. RenderBase is the engine's own pre-popup idiom (decomp: Destroy's death path).
			XRL.Core.XRLCore.Core.RenderBase();
			ParentObject.PlayWorldSound("Sounds/Damage/sfx_damage_stone");
			// completion popup echoing the words (user 2026-08-06: "'you etch' + whatever you
			// wrote should be popped up after you finish typing" -- TattooGun's own idiom:
			// Popup.Show("You tattoo " + text + " on ...") after the work lands)
			if (limb != null)
				Popup.Show("You etch \"" + text + "\" into " + Grammar.MakePossessive(ParentObject.t()) + " " + limb.GetOrdinalName() + "."); // DRAFT
			else
				Popup.Show("You etch \"" + text + "\" into " + ParentObject.t() + "."); // DRAFT
			actor.UseEnergy(1000, "Etching");
			Cleo.TerraFirma.Scripts.Helpers.VerifyLog("ETCH", $"{(reEtch ? "re-etched" : "etched")} {ParentObject.Blueprint}{(limb != null ? " [" + limb.GetOrdinalName() + "]" : "")} ({(choice == 0 ? "typed" : "markov")}, kit tier {toolTier}, {garbled} garbled): \"{text}\"");
		}

		/// <summary>The tier slip roll, graffiti-law edition (user ruling 2026-08-07, by
		/// screenshot: "i want the garbled text to work like graffiti"): Grammar.Obfuscate
		/// verbatim — the exact routine Graffitied runs. Spaces survive; a slipped character
		/// becomes one of the base game's block glyphs (80% of slips reuse ONE glyph rolled
		/// per carve, the rest scatter), and the base recipe's own 5% everything-lost roll
		/// rides in — the whole carve reduced to blocks; a one-arg veto if it ever grates.
		/// noiseValue = the standing tier formula; tier 6+ skips the call and carves clean
		/// (Obfuscate rolls its 5% even at noise 0, so the skip is a correctness gate).</summary>
		private static string GarbleByTier(string text, int tier, out int garbled)
		{
			garbled = 0;
			int chance = (GARBLE_TIER_CEILING - tier) * GARBLE_PCT_PER_TIER;
			if (chance <= 0 || text.IsNullOrEmpty())
				return text;
			string slipped = Grammar.Obfuscate(text, chance);
			if (slipped == text)
				return text;
			// casualty count for the slip message + log; the everything-lost roll drops
			// unslipped characters entirely, so a length change means a total loss
			if (slipped.Length != text.Length)
				garbled = text.Length;
			else
				for (int i = 0; i < text.Length; i++)
					if (slipped[i] != text[i])
						garbled++;
			return slipped;
		}

		/// <summary>The etched look (feedback 2026-08-06: "flips detail and main color on the
		/// object"): swap the Render foreground and detail channels, once, when the first
		/// inscription lands. No extra state — etched-ness IS the inscription fields, and the
		/// Render part serializes with the object, so the flip rides saves for free. The zone
		/// painter only rewrites Tile paths (decomp-checked), so repaints don't stomp it.
		/// Objects with no detail color or matching channels skip the flip silently.</summary>
		private void ApplyEtchedLook()
		{
			Render render = ParentObject.Render;
			if (render == null || render.DetailColor.IsNullOrEmpty())
				return;
			string source = !render.TileColor.IsNullOrEmpty() ? render.TileColor : render.ColorString;
			if (source.IsNullOrEmpty())
				return;
			int amp = source.IndexOf('&');
			if (amp < 0 || amp + 1 >= source.Length)
				return;
			char fg = source[amp + 1];
			char detail = render.DetailColor[0];
			if (fg == detail)
				return;
			render.DetailColor = fg.ToString();
			render.TileColor = SwapForeground(render.TileColor, detail);
			render.ColorString = SwapForeground(render.ColorString, detail);
		}

		/// <summary>Every &amp;-foreground in the spec becomes the given char; backgrounds
		/// (^) and everything else ride through untouched.</summary>
		private static string SwapForeground(string spec, char fg)
		{
			if (spec.IsNullOrEmpty())
				return spec;
			char[] chars = spec.ToCharArray();
			for (int i = 0; i < chars.Length - 1; i++)
			{
				if (chars[i] == '&')
					chars[i + 1] = fg;
			}
			return new string(chars);
		}

		/// <summary>Reads the face's live dressing off the Render part — tile path, foreground,
		/// detail — the same fields the old chip VFX read. Called BEFORE the etched-look flip
		/// so the carve cover can wear the pre-flip colors (the removed material).</summary>
		private void CaptureFace(out string tile, out char fg, out char detail)
		{
			Render render = ParentObject.Render;
			tile = render?.Tile;
			fg = 'y';
			detail = 'K';
			if (render == null)
				return;
			string tc = !render.TileColor.IsNullOrEmpty() ? render.TileColor : render.ColorString;
			if (!tc.IsNullOrEmpty())
			{
				int amp = tc.IndexOf('&');
				if (amp >= 0 && amp + 1 < tc.Length)
					fg = tc[amp + 1];
			}
			if (!render.DetailColor.IsNullOrEmpty())
				detail = render.DetailColor[0];
		}

		/// <summary>The etch VFX, upgraded to the FULL carve reveal (user 2026-08-07: "the same
		/// visual flair of carving away the top layer to see the recolored item underneath" —
		/// superseding the 08-06 chips-only cut): the stoneshaper cover-block choreography,
		/// wearing the captured pre-flip dressing, carved away in three top-down bands with
		/// paired debris chunks. Game-thread side composes renderables from the EXISTING
		/// Render.Tile path (no atlas probing — the freeze lesson); sub-rect clones happen
		/// Unity-side in the entries' start(). Unseen targets skip silently.</summary>
		private void PlayEtchReveal(string tile, char fg, char detail)
		{
			Cell cell = ParentObject.CurrentCell;
			if (cell == null || !cell.IsVisible() || tile.IsNullOrEmpty())
				return;
			Cleo_TerraFirma_StoneRiseFX.PlayEtchReveal(cell, tile, fg, detail);
		}

		/// <summary>Contact, non-abstract body parts, the tattoo gun's own eligibility cut
		/// (minus the flesh check: everything on a statue is stone). Null for bodiless targets.</summary>
		private List<BodyPart> GetEtchableLimbs()
		{
			Body body = ParentObject.Body;
			if (body == null)
				return null;
			List<BodyPart> limbs = null;
			foreach (BodyPart part in body.GetParts())
			{
				if (part.Contact && !part.Abstract)
					(limbs ??= new List<BodyPart>()).Add(part);
			}
			return limbs;
		}

		public override bool HandleEvent(GetDisplayNameEvent E)
		{
			if ((!Inscription.IsNullOrEmpty() || LimbInscriptions.Count > 0) && (!E.Understood() || !E.Object.HasProperName))
				E.AddAdjective("etched"); // DRAFT
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(GetShortDescriptionEvent E)
		{
			if (!Inscription.IsNullOrEmpty())
			{
				if (E.Infix.Length > 0)
					E.Infix.Append("\n");
				// DRAFT frame (Graffitied's structure, its own voice)
				E.Infix.Append("\nWords are chiseled into the surface. They read:\n\n\"").Append(Inscription).Append("\"\n");
			}
			if (LimbInscriptions.Count > 0 && ParentObject.Body is Body body)
			{
				foreach (KeyValuePair<int, string> kv in LimbInscriptions)
				{
					BodyPart part = body.GetPartByID(kv.Key);
					if (part == null)
						continue; // the limb is gone, and its words went with it (Tattoos' dangling-ID idiom, pruned lazily)
					if (E.Infix.Length > 0)
						E.Infix.Append("\n");
					// DRAFT frame, one line per etched limb
					E.Infix.Append("\nChiseled into ").Append(ParentObject.its).Append(" ").Append(part.GetOrdinalName())
						.Append(": \"").Append(kv.Value).Append("\"\n");
				}
			}
			return base.HandleEvent(E);
		}
	}
}
