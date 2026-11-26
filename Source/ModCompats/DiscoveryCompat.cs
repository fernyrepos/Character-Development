using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [StaticConstructorOnStartup]
    public static class DiscoveryCompat
    {
        public static readonly bool IsActive = ModsConfig.IsActive("ferny.discoveries");

        private static FieldInfo discoveredThingsField;
        private static FieldInfo discoveredFactionsField;
        private static FieldInfo discoveredXenotypesField;

        static DiscoveryCompat()
        {
            if (IsActive)
            {
                var type = AccessTools.TypeByName("Discoveries.DiscoveryTracker");
                discoveredThingsField = AccessTools.Field(type, "discoveredThingDefNames");
                discoveredFactionsField = AccessTools.Field(type, "discoveredFactionDefNames");
                discoveredXenotypesField = AccessTools.Field(type, "discoveredXenotypeDefNames");
            }
        }

        public static bool IsDiscovered(ThingDef def)
        {
            if (!IsActive)
            {
                return true;
            }

            var set = (HashSet<string>)discoveredThingsField.GetValue(null);
            return set.Contains(def.defName);
        }

        public static bool IsDiscovered(FactionDef def)
        {
            if (!IsActive)
            {
                return true;
            }

            var set = (HashSet<string>)discoveredFactionsField.GetValue(null);
            return set.Contains(def.defName);
        }

        public static bool IsDiscovered(XenotypeDef def)
        {
            if (!IsActive)
            {
                return true;
            }

            var set = (HashSet<string>)discoveredXenotypesField.GetValue(null);
            return set.Contains(def.defName);
        }
    }
}
