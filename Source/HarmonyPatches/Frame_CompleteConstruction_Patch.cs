using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
    public static class Frame_CompleteConstruction_Patch
    {
        public static void Postfix(Frame __instance, Pawn worker)
        {
            if (worker != null && worker.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(worker, new WantWorkerContext(triggerType: WantTriggerType.BuildingConstructed, contextDef: __instance.BuildDef));
            }
        }
    }
}
