using HarmonyLib;
using RimWorld;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.SetXenotype))]
    public static class Pawn_GeneTracker_SetXenotype_Patch
    {
        public static void Postfix(Pawn_GeneTracker __instance, XenotypeDef xenotype)
        {
            if (__instance.pawn.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(__instance.pawn, new WantWorkerContext(triggerType: WantTriggerType.XenotypeChanged, contextDef: xenotype));
            }
        }
    }
}
