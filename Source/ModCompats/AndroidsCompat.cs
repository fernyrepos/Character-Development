using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace WantsAndQuirks
{
    [StaticConstructorOnStartup]
    public static class AndroidsCompat
    {
        public static readonly bool Active = ModsConfig.IsActive("vanillaracesexpanded.android");
        private static readonly Type androidGeneDefType;
        private static readonly FieldInfo removeWhenAwakenedField;

        static AndroidsCompat()
        {
            if (Active)
            {
                androidGeneDefType = AccessTools.TypeByName("VREAndroids.AndroidGeneDef");
                removeWhenAwakenedField = AccessTools.Field(androidGeneDefType, "removeWhenAwakened");
            }
        }

        public static bool IsUnawakenedAndroid(Pawn pawn)
        {
            if (!Active)
                return false;

            var genes = pawn.genes.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                var geneDef = genes[i].def;
                if (geneDef.GetType() == androidGeneDefType && (bool)removeWhenAwakenedField.GetValue(geneDef))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
