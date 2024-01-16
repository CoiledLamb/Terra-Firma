// Earthen Barrage Scripting
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConsoleLib.Console;
using XRL;
using XRL.Liquids;
using XRL.Messages;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Capabilities;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;


namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class Cleo_EarthenBarrage : BaseMutation
    {

	    public const int COUNT = 1000;

	    public const int WILLPOWER_BASELINE = 16;

	    public const int WILLPOWER_FACTOR = 5;

	    public const int WILLPOWER_CEILING_FACTOR = 5;

	    public const int WILLPOWER_FLOOR_DIVISOR = 5;

        public string Blueprint = "Ephemeral Stone";
        public string CommandID;
        public Guid EarthenBarrageActivatedAbilityID = Guid.Empty;

        public EarthenBarrage()
	    {
		    DisplayName = "Earthen Barrage";
		    base.Type = "Mental";
	    }

        public bool ActivateEarthenBarrage(
            GameObject Actor,
            GameObject Target = null,
            IEvent FromEvent = null
        )
        {
            if (!GameObject.Validate(ref Actor))
            {
                return false;
            }
            if (!Actor.IsActivatedAbilityUsable(ActivatedAbilityID))
            {
                return false;
            }
            BodyPart part = GetTargetBodyPart(Actor);
            GameObject obj = GenerateObject();
            if (part == null)
            {
                string msg = "You have no place available to hold " + (obj?.an() ?? "the result") + ".";
                obj?.Obliterate();
                return Actor.Fail(msg);
            }
            if (!CheckRealityDistortion(Actor, FromEvent))
            {
                obj?.Obliterate();
                return false;
            }
            Actor.ReceiveObject(obj);
            if (!GameObject.Validate(obj) || obj.InInventory != Actor)
            {
                obj?.Obliterate();
                return Actor.Fail("Something went wrong.");
            }
            Event equipEv = Event.New("CommandEquipObject");
            equipEv.SetParameter("Object", obj);
            equipEv.SetParameter("BodyPart", part);
            equipEv.SetSilent(true);
            if (!Actor.FireEvent(equipEv))
            {
                obj?.Obliterate();
                return Actor.Fail("Something went wrong.");
            }
            XDidYToZ(obj, "shimmer", "into existence in", Actor, (part.Type == "Thrown Weapon" ? "grasp" : part.GetOrdinalName()), IndefiniteSubject: true, PossessiveObject: true);
            FromEvent?.RequestInterfaceExit();
            return true;
        }

         private bool CheckRealityDistortion(GameObject Actor, IEvent FromEvent = null)
        {
            Event ev = Event.New("InitiateRealityDistortionLocal");
            ev.SetParameter("Object", Actor);
            ev.SetParameter("Device", this);
            return Actor.FireEvent(ev, FromEvent);
        }

        private GameObject GenerateObject()
        {
            GameObject obj = GameObject.Create(Blueprint);
            obj.SetIntProperty("NeverStack", 1);
            obj.RemovePart("ExistenceSupport");
            var xs = obj.RequirePart<ExistenceSupport>();
            xs.SupportedBy = ParentObject;
            xs.ValidateEveryTurn = true;
            xs.SilentRemoval = true;
            return obj;
        }

        public static BodyPart GetTargetBodyPart(GameObject Subject)
        {
            return Subject?.GetUnequippedPreferredBodyPartOrAlternate(
                PreferredType: "Thrown Weapon",
                AlternateType: "Hand"
            );
        }

        public string GetDamage(int Level)
	{
		if (Level <= 1)
		{
			return "1d3";
		}
		if (Level <= 2)
		{
			return "1d4";
		}
		if (Level <= 3)
		{
			return "1d5";
		}
		if (Level <= 4)
		{
			return "1d4+1";
		}
		if (Level <= 5)
		{
			return "1d5+1";
		}
		if (Level <= 6)
		{
			return "1d4+2";
		}
		if (Level <= 7)
		{
			return "1d5+2";
		}
		if (Level <= 8)
		{
			return "1d4+3";
		}
		if (Level <= 9)
		{
			return "1d5+3";
		}
		if (Level > 9)
		{
			return "1d5+" + (Level - 6);
		}
		return "1d4+4";
	}

	public int GetEartenBarragePenetrationBonus()
	{
		return 4 + (base.Level - 1) / 2;
	}

    }
}