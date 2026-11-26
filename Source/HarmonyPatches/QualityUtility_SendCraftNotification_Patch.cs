using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(QualityUtility), nameof(QualityUtility.SendCraftNotification))]
    public static class QualityUtility_SendCraftNotification_Patch
    {
        public static void Postfix(Thing thing, Pawn worker)
        {
            if (worker.CanHaveWants())
            {
                if (thing.TryGetQuality(out var qc))
                {
                    WantsAndQuirksUtility.CheckWants(worker, new WantWorkerContext(triggerType: WantTriggerType.RecipeCompleted, contextDef: thing.def, contextAmount: (int)qc));
                }
            }
        }
    }
}
