using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(RelationsUtility), nameof(RelationsUtility.TryDevelopBondRelation))]
    public static class RelationsUtility_TryDevelopBondRelation_Patch
    {
        public static void Postfix(Pawn humanlike, Pawn animal, bool __result)
        {
            if (__result && humanlike.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(humanlike, new WantWorkerContext(triggerType: WantTriggerType.BondedWithAnimal, contextDef: animal.def, contextPawn: animal));
            }
        }
    }
}
