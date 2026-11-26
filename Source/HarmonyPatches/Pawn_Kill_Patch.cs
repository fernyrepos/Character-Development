using HarmonyLib;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Pawn_Kill_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.Dead && __instance.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(__instance, new WantWorkerContext(triggerType: WantTriggerType.Died));
            }
        }
    }
}
