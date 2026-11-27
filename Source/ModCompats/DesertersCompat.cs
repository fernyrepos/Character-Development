using System;
using System.Reflection;
using HarmonyLib;
using Verse;
namespace WantsAndQuirks
{
    [StaticConstructorOnStartup]
    public static class DesertersCompat
    {
        public static readonly bool Active;
        private static readonly Type worldCompType;
        private static readonly FieldInfo instanceField;
        private static readonly FieldInfo activeField;
        static DesertersCompat()
        {
            var active = ModsConfig.IsActive("OskarPotocki.VFE.Deserters");
            if (active)
            {
                worldCompType = AccessTools.TypeByName("VFED.WorldComponent_Deserters");
                if (worldCompType != null)
                {
                    instanceField = AccessTools.Field(worldCompType, "Instance");
                    activeField = AccessTools.Field(worldCompType, "Active");
                }
                if (worldCompType == null || instanceField == null || activeField == null)
                {
                    Log.Error("[WantsAndQuirks] VFE Deserters compatibility failed to initialize. Disabling Deserters wants.");
                    active = false;
                }
            }
            Active = active;
        }
        public static bool IsDesertersActive()
        {
            if (!Active)
                return false;
            var instance = instanceField.GetValue(null);
            return (bool)activeField.GetValue(instance);
        }
    }
}
