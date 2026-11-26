using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.AddedAndImplantedPartsWithXenogenesCount))]
    public static class GeneUtility_AddedAndImplantedPartsWithXenogenesCount_Patch
    {
        public static void Postfix(Pawn pawn, ref int __result)
        {
            if (!WantsAndQuirksUtility.TryGetWantsData(pawn, out var data))
                return;
            __result -= data.grantedGenes.Count;
        }
    }
}
