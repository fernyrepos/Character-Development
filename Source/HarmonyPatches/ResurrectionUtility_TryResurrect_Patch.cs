using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(ResurrectionUtility), nameof(ResurrectionUtility.TryResurrect))]
    public static class ResurrectionUtility_TryResurrect_Patch
    {
        public static void Postfix(Pawn pawn, bool __result)
        {
            if (__result && pawn.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(pawn, new WantWorkerContext(triggerType: WantTriggerType.Resurrected));
            }
        }
    }
}
