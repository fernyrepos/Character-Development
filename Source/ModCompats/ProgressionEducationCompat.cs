using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [StaticConstructorOnStartup]
    public static class ProgressionEducationCompat
    {
        public static readonly bool Active;

        private static readonly Type proficiencyDefType;
        private static readonly FieldInfo settingsField;
        private static readonly FieldInfo enableProficiencySystemField;
        private static readonly MethodInfo isProficiencyTraitMethod;
        private static readonly MethodInfo isTrackEnabledMethod;
        private static readonly FieldInfo tiersField;
        private static readonly FieldInfo tierTraitDefField;

        static ProgressionEducationCompat()
        {
            Active = ModsConfig.IsActive("ferny.progressioneducation");
            if (Active)
            {
                var modType = AccessTools.TypeByName("ProgressionEducation.EducationMod");
                var settingsType = AccessTools.TypeByName("ProgressionEducation.EducationSettings");
                var utilityType = AccessTools.TypeByName("ProgressionEducation.ProficiencyUtility");
                proficiencyDefType = AccessTools.TypeByName("ProgressionEducation.ProficiencyDef");
                var tierDefType = AccessTools.TypeByName("ProgressionEducation.ProficiencyTierDef");

                settingsField = AccessTools.Field(modType, "settings");
                enableProficiencySystemField = AccessTools.Field(settingsType, "enableProficiencySystem");
                isProficiencyTraitMethod = AccessTools.Method(utilityType, "IsProficiencyTrait", new[] { typeof(TraitDef) });
                isTrackEnabledMethod = AccessTools.Method(utilityType, "IsTrackEnabled");
                tiersField = AccessTools.Field(proficiencyDefType, "tiers");
                tierTraitDefField = AccessTools.Field(tierDefType, "traitDef");
            }
        }

        public static bool IsProficiencyTrait(TraitDef trait)
        {
            return (bool)isProficiencyTraitMethod.Invoke(null, new object[] { trait });
        }

        public static bool IsProficiencyTraitEnabled(TraitDef trait)
        {
            var settings = settingsField.GetValue(null);
            if (!(bool)enableProficiencySystemField.GetValue(settings))
                return false;

            foreach (var track in GenDefDatabase.GetAllDefsInDatabaseForDef(proficiencyDefType))
            {
                var tiers = (IList)tiersField.GetValue(track);
                for (int i = 0; i < tiers.Count; i++)
                {
                    if ((TraitDef)tierTraitDefField.GetValue(tiers[i]) == trait)
                    {
                        return (bool)isTrackEnabledMethod.Invoke(null, new object[] { track });
                    }
                }
            }

            return true;
        }
    }
}
