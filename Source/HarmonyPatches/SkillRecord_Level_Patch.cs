using HarmonyLib;
using RimWorld;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
    public static class SkillRecord_Level_Patch
    {
        public static void Prefix(SkillRecord __instance, out int __state)
        {
            __state = __instance.levelInt;
        }

        public static void Postfix(SkillRecord __instance, int __state)
        {
            if (__state != __instance.levelInt && __instance.Pawn.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(__instance.Pawn, new WantWorkerContext(triggerType: WantTriggerType.SkillIncreased, contextDef: __instance.def, contextAmount: __instance.levelInt));
            }
        }
    }
}
