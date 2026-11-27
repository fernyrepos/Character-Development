using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.Cleanup))]
    public static class JobDriver_Cleanup_Fish_Patch
    {
        public static void Postfix(JobDriver __instance, JobCondition condition)
        {
            if (condition != JobCondition.Succeeded)
                return;

            var type = __instance.GetType();
            if (type != typeof(JobDriver_Fish) && type.FullName != "VCE_Fishing.JobDriver_Fish")
                return;

            var pawn = __instance.pawn;
            if (pawn != null && pawn.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(pawn, new WantWorkerContext(triggerType: WantTriggerType.Fished));
            }
        }
    }
}
