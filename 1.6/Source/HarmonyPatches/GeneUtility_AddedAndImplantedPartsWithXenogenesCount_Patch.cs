using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    // Vanilla AddedAndImplantedPartsWithXenogenesCount adds a single +1 when the pawn has
    // any xenogenes at all, regardless of how many. Quirk-granted genes are added as
    // xenogenes (AddGene(def, xenogene: true)), so a pawn whose only xenogenes come from
    // quirks would otherwise count as "modified" for transhumanists/body purists.
    //
    // Only undo that single +1, and only when every xenogene the pawn has was granted by
    // a quirk. Subtracting grantedGenes.Count instead removes real prosthetics/implants
    // from the count whenever a pawn has more than one granted gene, or when it also has
    // genuine xenogenes.
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.AddedAndImplantedPartsWithXenogenesCount))]
    public static class GeneUtility_AddedAndImplantedPartsWithXenogenesCount_Patch
    {
        public static void Postfix(Pawn pawn, ref int __result)
        {
            if (!ModsConfig.BiotechActive || pawn?.genes == null)
                return;
            if (!WantsAndQuirksUtility.TryGetWantsData(pawn, out var data) || data.grantedGenes.Count == 0)
                return;

            var xenogenes = pawn.genes.Xenogenes;
            if (xenogenes.Count == 0)
                return;
            for (int i = 0; i < xenogenes.Count; i++)
            {
                if (!xenogenes[i].IsGrantedGene())
                    return;
            }

            __result -= 1;
        }
    }
}
