using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation))]
    public static class Pawn_RelationsTracker_AddDirectRelation_Patch
    {
        public static void Postfix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            if (def == PawnRelationDefOf.Lover || def == PawnRelationDefOf.Fiance || def == PawnRelationDefOf.Spouse)
            {
                if (__instance.pawn.CanHaveWants())
                {
                    WantsAndQuirksUtility.CheckWants(__instance.pawn, new WantWorkerContext(triggerType: WantTriggerType.FellInLove, contextDef: otherPawn.genes?.Xenotype, contextPawn: otherPawn));
                }
            }
        }
    }
}
