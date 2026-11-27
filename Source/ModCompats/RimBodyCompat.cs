using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
namespace WantsAndQuirks
{
    [StaticConstructorOnStartup]
    public static class RimBodyCompat
    {
        public static readonly bool Active;
        private static readonly Type compType;
        private static readonly FieldInfo bodyFatField;
        private static readonly FieldInfo muscleMassField;
        static RimBodyCompat()
        {
            var active = ModsConfig.IsActive("maux36.rimbody");
            if (active)
            {
                compType = AccessTools.TypeByName("Maux36.Rimbody.CompPhysique");
                if (compType != null)
                {
                    bodyFatField = AccessTools.Field(compType, "BodyFat");
                    muscleMassField = AccessTools.Field(compType, "MuscleMass");
                }
                if (compType == null || bodyFatField == null || muscleMassField == null)
                {
                    Log.Error("[WantsAndQuirks] RimBody compatibility failed to initialize. Disabling RimBody wants.");
                    active = false;
                }
            }
            Active = active;
        }
        public static float GetBodyFat(Pawn pawn)
        {
            if (!Active)
                return 0f;
            var comp = pawn.AllComps.FirstOrDefault(c => c.GetType() == compType);
            return comp != null ? (float)bodyFatField.GetValue(comp) : 0f;
        }
        public static float GetMuscleMass(Pawn pawn)
        {
            if (!Active)
                return 0f;
            var comp = pawn.AllComps.FirstOrDefault(c => c.GetType() == compType);
            return comp != null ? (float)muscleMassField.GetValue(comp) : 0f;
        }
    }
}
