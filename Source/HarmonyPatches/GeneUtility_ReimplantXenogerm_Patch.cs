using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.ReimplantXenogerm))]
    public static class GeneUtility_ReimplantXenogerm_Patch
    {
        public static void Postfix(Pawn caster, Pawn recipient)
        {
            if (!WantsAndQuirksUtility.TryGetWantsData(caster, out var data))
                return;
            for (int i = recipient.genes.xenogenes.Count - 1; i >= 0; i--)
            {
                var gene = recipient.genes.xenogenes[i];
                if (data.grantedGenes.Any(l => l.gene.def == gene.def))
                    recipient.genes.RemoveGene(gene);
            }
        }
    }
}
