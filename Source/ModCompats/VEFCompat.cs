using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [StaticConstructorOnStartup]
    public static class VEFCompat
    {
        public static readonly bool Active = ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core");
        private static readonly HashSet<BuildableDef> hiddenDesignators;
        private static readonly System.Type traitExtensionType;
        private static readonly System.Reflection.FieldInfo apparelExclusiveField;

        static VEFCompat()
        {
            if (Active)
            {
                traitExtensionType = AccessTools.TypeByName("VEF.Pawns.TraitExtension");
                apparelExclusiveField = AccessTools.Field(traitExtensionType, "apparelExclusiveTrait");
                var defType = AccessTools.TypeByName("VEF.Buildings.HiddenDesignatorsDef");
                var hiddenField = AccessTools.Field(defType, "hiddenDesignators");
                hiddenDesignators = new HashSet<BuildableDef>();
                foreach (Def def in GenDefDatabase.GetAllDefsInDatabaseForDef(defType))
                {
                    var list = (List<BuildableDef>)hiddenField.GetValue(def);
                    for (int i = 0; i < list.Count; i++)
                    {
                        hiddenDesignators.Add(list[i]);
                    }
                }
            }
        }

        public static bool IsApparelExclusiveTrait(TraitDef def)
        {
            if (def.defName == "VFEP_Shellcasket")
                return true;
            if (!Active || traitExtensionType == null || apparelExclusiveField == null || def.modExtensions == null)
                return false;
            for (int i = 0; i < def.modExtensions.Count; i++)
            {
                if (traitExtensionType.IsInstanceOfType(def.modExtensions[i]) && (bool)apparelExclusiveField.GetValue(def.modExtensions[i]))
                    return true;
            }
            return false;
        }

        public static bool IsHiddenDesignator(BuildableDef def)
        {
            if (IsInExcludedDesignationCategory(def))
                return true;
            if (!Active)
                return false;
            return hiddenDesignators.Contains(def);
        }

        private static bool IsInExcludedDesignationCategory(BuildableDef def)
        {
            return def.designationCategory?.defName == "VGE_Designer";
        }
    }
}
