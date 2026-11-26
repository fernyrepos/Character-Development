using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(GeneUIUtility), "RecacheGenes")]
    public static class GeneUIUtility_RecacheGenes_Patch
    {
        public static void Postfix(Thing target, GeneSet genesOverride)
        {
            if (target == null && genesOverride != null)
            {
                for (int i = GeneUIUtility.geneDefs.Count - 1; i >= 0; i--)
                {
                    var geneDef = GeneUIUtility.geneDefs[i];
                    if (!BabyGeneDisplayUtility.IsQuirkGene(geneDef))
                        continue;
                    GeneUIUtility.geneDefs.RemoveAt(i);
                    GeneUIUtility.gcx -= geneDef.biostatCpx;
                    GeneUIUtility.met -= geneDef.biostatMet;
                    GeneUIUtility.arc -= geneDef.biostatArc;
                }
                return;
            }

            if (target is Pawn pawn && WantsAndQuirksUtility.TryGetWantsData(pawn, out var data))
            {
                foreach (var link in data.grantedGenes)
                {
                    var removed = false;
                    if (GeneUIUtility.xenogenes.Contains(link.gene))
                    {
                        GeneUIUtility.xenogenes.Remove(link.gene);
                        removed = true;
                    }
                    if (GeneUIUtility.endogenes.Contains(link.gene))
                    {
                        GeneUIUtility.endogenes.Remove(link.gene);
                        removed = true;
                    }

                    if (removed && !link.gene.Overridden)
                    {
                        GeneUIUtility.gcx -= link.gene.def.biostatCpx;
                        GeneUIUtility.met -= link.gene.def.biostatMet;
                        GeneUIUtility.arc -= link.gene.def.biostatArc;
                    }
                }
            }
        }
    }
}
