using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [StaticConstructorOnStartup]
    public static class TherapyCompat
    {
        private static readonly bool therapyActive;
        private static readonly bool traumaActive;
        private static readonly NeedDef stabilityNeedDef;
        private static readonly MethodInfo getTraumaIntegrityDataMethod;
        private static readonly MethodInfo getTraumaticTraitMethod;
        private static readonly FieldInfo temperedField;

        static TherapyCompat()
        {
            therapyActive = ModsConfig.IsActive("ferny.ProgressionTherapy");
            traumaActive = ModsConfig.IsActive("ferny.TraumaAndIntegrity");

            if (therapyActive)
            {
                stabilityNeedDef = DefDatabase<NeedDef>.GetNamed("PT_MentalStability");
            }
            if (traumaActive)
            {
                getTraumaIntegrityDataMethod = AccessTools.Method(AccessTools.TypeByName("TraumaAndIntegrity.Pawn_ExposeData_Patch"), "GetTraumaIntegrityData");
                getTraumaticTraitMethod = AccessTools.Method(AccessTools.TypeByName("TraumaAndIntegrity.TraumaIntegrityData"), "GetTraumaticTrait");
                temperedField = AccessTools.Field(AccessTools.TypeByName("TraumaAndIntegrity.TraumaIntegrityData"), "tempered");
            }
        }

        public static void ImproveComposure(Pawn pawn)
        {
            if (!therapyActive || HasComposure(pawn, out var need) is false)
                return;
            need.CurLevelPercentage += 0.05f;
        }

        public static bool HasHighComposure(Pawn pawn)
        {
            if (!therapyActive || HasComposure(pawn, out var need) is false)
                return false;
            return need.CurLevelPercentage >= 0.6f;
        }

        public static bool HasComposure(Pawn pawn, out Need need)
        {
            need = null;
            if (!therapyActive)
                return false;
            need = pawn.needs.TryGetNeed(stabilityNeedDef);
            return need != null;
        }

        public static bool HasTraumaticTrait(Pawn pawn)
        {
            if (!traumaActive)
                return false;
            var data = getTraumaIntegrityDataMethod.Invoke(null, new object[] { pawn });
            return data != null && getTraumaticTraitMethod.Invoke(data, new object[] { pawn }) != null;
        }

        public static bool IsTempered(Pawn pawn)
        {
            if (!traumaActive)
                return false;
            var data = getTraumaIntegrityDataMethod.Invoke(null, new object[] { pawn });
            return data != null && (bool)temperedField.GetValue(data);
        }

        public static bool HasTraumaticPassion(Pawn pawn)
        {
            if (!ModsConfig.IsActive("sarg.alphaskills"))
                return false;
            var passionDefType = AccessTools.TypeByName("VSE.Passions.PassionDef");
            var def = GenDefDatabase.GetDef(passionDefType, "AS_TraumaticPassion");
            if (def == null)
                return false;
            var index = (byte)def.index;
            foreach (var skill in pawn.skills.skills)
            {
                if ((uint)skill.passion == index)
                    return true;
            }
            return false;
        }

    }
}
