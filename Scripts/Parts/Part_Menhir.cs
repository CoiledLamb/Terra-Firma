using System;
using System.Collections.Generic;
using System.Text;
using Cleo.TerraFirma.Scripts;
using XRL.Language;
using XRL.Rules;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	[Serializable]
	public class Cleo_TerraFirma_Menhir : IPart
	{
		public const string COMMAND = "Cleo_TerraFirma_CommandMenhirCarve";

		public int MentalCount = 1;
		public int MentalLevel = 1;
		public int SliceMin = 60;
		public int SliceMax = 120;
		public int Garble = 20;
		public bool Attune;

		public int CarveHP = 3;
		public int CarveAIThreshold = 15;
		public string CarveCooldown = "1d2+1";
		public int CarveEnergy = 1000;

		public bool Rolled;
		public Guid CarveAbilityID = Guid.Empty;

		public string RolledMentals;
		public string RolledSlice;

		public override bool SameAs(IPart p) => false;

		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade)
				|| ID == AfterObjectCreatedEvent.ID
				|| ID == AIGetOffensiveAbilityListEvent.ID;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register(COMMAND);
			base.Register(Object, Registrar);
		}

		public override bool HandleEvent(AfterObjectCreatedEvent E)
		{
			if (!Rolled)
			{
				Rolled = true;
				if (CarveAbilityID == Guid.Empty)
					CarveAbilityID = AddMyActivatedAbility("Carve Stone", COMMAND, "Physical Mutations", null, "ö");
				RollMentals();
				Inscribe();
				AttuneStones();
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
		{
			if (WantsToCarve())
				E.Add(COMMAND);
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == COMMAND && !Carve())
				return false;
			return base.FireEvent(E);
		}

		public Cleo_TerraFirma_EarthenBarrage Barrage => ParentObject.GetPart<Cleo_TerraFirma_EarthenBarrage>();

		public bool WantsToCarve()
		{
			Cleo_TerraFirma_EarthenBarrage barrage = Barrage;
			if (barrage == null || barrage.Charges > 0)
				return false;
			if (!IsMyActivatedAbilityAIUsable(CarveAbilityID) || !IsMyActivatedAbilityUsable(CarveAbilityID))
				return false;
			if (CarveAIThreshold > 0 && ParentObject.Stat("Hitpoints") < CarveAIThreshold)
				return false;
			BodyPart slot = ParentObject.GetFirstBodyPart("Thrown Weapon");
			return slot != null && slot.Equipped == null;
		}

		public bool Carve()
		{
			Cleo_TerraFirma_EarthenBarrage barrage = Barrage;
			if (barrage == null || ParentObject.GetFirstThrownWeapon() != null)
				return false;
			barrage.Activate();
			GameObject stone = ParentObject.GetFirstThrownWeapon();
			if (stone == null)
			{
				Helpers.VerifyLog("MENHIR", $"{ParentObject.ID} carve made no stone");
				return false;
			}
			if (Visible())
				AddPlayerMessage(ParentObject.Does("carve") + " " + stone.an() + " from the substance of " + ParentObject.its + " body.");
			if (CarveHP > 0)
				ParentObject.TakeDamage(CarveHP, Owner: ParentObject, Message: "from using " + ParentObject.its + " body as raw materials.", Attributes: "Fabrication");
			if (CarveEnergy > 0)
				ParentObject.UseEnergy(CarveEnergy, "Physical Ability Fabricate");
			int cooldown = CarveCooldown.RollCached();
			if (cooldown > 0)
				CooldownMyActivatedAbility(CarveAbilityID, cooldown);
			Helpers.VerifyLog("MENHIR", $"{ParentObject.ID} carved {stone.Blueprint} for {CarveHP} HP, now {ParentObject.Stat("Hitpoints")}, cooldown {cooldown}");
			return true;
		}

		public void RollMentals()
		{
			Mutations mutations = ParentObject.RequirePart<Mutations>();
			List<string> got = new List<string>();
			for (int i = 0; i < MentalCount; i++)
			{
				List<MutationEntry> pool = new List<MutationEntry>(mutations.GetMutatePool());
				pool.ShuffleInPlace();
				MutationEntry pick = null;
				foreach (MutationEntry entry in pool)
				{
					if (entry.Category != null && entry.Category.Name == "Mental" && !ParentObject.HasPart(entry.Class) && entry.Cost > 1)
					{
						pick = entry;
						break;
					}
				}
				if (pick == null)
					break;
				Compat.AddMutation(mutations, pick, MentalLevel);
				got.Add(pick.Class);
			}
			RolledMentals = string.Join(", ", got);
			Helpers.VerifyLog("MENHIR", $"{ParentObject.ID} rolled {got.Count}/{MentalCount} mentals at {MentalLevel}: {RolledMentals}");
		}

		public void Inscribe()
		{
			Cleo_TerraFirma_Etchable etchable = ParentObject.GetPart<Cleo_TerraFirma_Etchable>();
			if (etchable == null || !etchable.Inscription.IsNullOrEmpty())
				return;
			RolledSlice = Slice(Stat.Random(SliceMin, SliceMax));
			etchable.Inscription = GarbleLines(RolledSlice, Garble);
		}

		public static string Slice(int Length)
		{
			string text = INSCRIPTION;
			if (Length >= text.Length)
				return text;
			int start = Stat.Random(0, text.Length - Length);
			int end = start + Length;
			while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
				start--;
			while (end < text.Length && !char.IsWhiteSpace(text[end]))
				end++;
			return text.Substring(start, end - start).Trim();
		}

		public static string GarbleLines(string Text, int Noise)
		{
			if (Noise <= 0 || Text.IsNullOrEmpty())
				return Text;
			int lost = Stat.Random(1, 100) <= 5 ? 100 : 0;
			string[] lines = Text.Split('\n');
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0)
					sb.Append('\n');
				sb.Append(Grammar.Obfuscate(lines[i], Noise, 80, lost));
			}
			return sb.ToString();
		}

		public void AttuneStones()
		{
			Cleo_TerraFirma_EarthenBarrage barrage = Barrage;
			if (!Attune || barrage == null || barrage.AttuneAbilityID == Guid.Empty || barrage.Resonant)
				return;
			ToggleMyActivatedAbility(barrage.AttuneAbilityID, Silent: true, SetState: true);
		}

		public const string INSCRIPTION =
"THE OCEANIC ANIMUS,         THE GLIMMERING SEA,\n" +
"LO, THE STARRY EYED WHICH GLISTEN AND GLARE\n" +
"THROUGHOUT ITS EFFERVESCENT CURRENT SEEK\n" +
"ITS ESCAPE, YET THEY FORGET THAT THEY ARE ITS\n" +
"WATER. THEY HAVE FORGOTTEN THEIR OWN LIGHT\n" +
"IS THAT WHICH REFRACTS THROUGHOUT IT. I HAVE\n" +
"FOUND A DIFFERENT PATH, O ESPER. YOU FORGOT\n" +
"IN THE OCEAN THAT ONCE YOU WERE PART OF THE\n" +
"BED WHICH LIE ITS FOUNDATIONS; THE WORLD, THE\n" +
"STONE. YOU ARE THE PRODUCT OF EROSION, AS IS\n" +
"ALL OTHER THINGS. YOUR TRUE \"SELF\", DOTH DOES\n" +
"IT LIE? WITHIN, WITHOUT. THAT WHICH CARVES OUT\n" +
"THE GLEAMING RIVER HAS FORMED YOU, IT IS YOUR\n" +
"BEING DEFINED BY BEING BY BEING BY BEING EVER\n" +
"ONWARDS. YOU CAN CARVE YOURSELF, O ESPER. BE\n" +
"COME ONE ONCE AGAIN WITH THE STONE YOU ONCE\n" +
"WERE, STAND GAINST ITS FERVENT TIDE AND CARVE\n" +
"THINESELF INTO YOUR TRUE \"SELF\". BECOME YOUR\n" +
"SELF UNENDING, CARVE YOUR BEING INTO WHICH\n" +
"YOU SEEK TO BE. RETURN YOURSELF TO THE WORLD\n" +
"BY BECOMING ONE IN OF ITSELF. ONLY THEN SHALL\n" +
"YOU ESCAPE ITS CURRENTS BY STANDING UP ABOVE\n" +
"IT ALL, IN GLORIOUS, MONOLITHIC BEAUTY. - MNEHIR";
	}
}
