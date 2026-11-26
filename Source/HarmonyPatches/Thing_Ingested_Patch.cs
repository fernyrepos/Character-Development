using HarmonyLib;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Thing_Ingested_Patch
    {
        public static void Postfix(Thing __instance, Pawn ingester)
        {
            if (ingester.CanHaveWants())
            {
                if (__instance.def.IsDrug)
                {
                    WantsAndQuirksUtility.CheckWants(ingester, new WantWorkerContext(triggerType: WantTriggerType.DrugIngested, contextDef: __instance.def));
                }
                if (__instance.def.IsIngestible)
                {
                    WantsAndQuirksUtility.CheckWants(ingester, new WantWorkerContext(triggerType: WantTriggerType.FoodEaten, contextDef: __instance.def));
                }
                var data = ingester.GetWantsData();
                foreach (var quirk in data.quirks)
                {
                    quirk.def.Worker.Notify_Ingested(ingester, quirk, __instance);
                }
            }
        }
    }
}
