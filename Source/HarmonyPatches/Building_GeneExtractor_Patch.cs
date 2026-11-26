using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Building_GeneExtractor), nameof(Building_GeneExtractor.CanAcceptPawn))]
    public static class Building_GeneExtractor_CanAcceptPawn_Patch
    {
        public static void Postfix(Pawn pawn, ref AcceptanceReport __result)
        {
            if (!__result.Accepted)
                return;

            var genes = pawn.genes.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                var gene = genes[i];
                if (!gene.IsGrantedGene() && gene.def.passOnDirectly && gene.def.biostatArc == 0)
                    return;
            }
            __result = "PawnHasNoGenes".Translate(pawn.Named("PAWN"));
        }
    }

    [HarmonyPatch]
    public static class Building_GeneExtractor_SelectionWeight_Patch
    {
        public static MethodBase TargetMethod()
        {
            var nestedClasses = typeof(Building_GeneExtractor).GetNestedTypes(AccessTools.all);
            foreach (var nested in nestedClasses)
            {
                var methods = nested.GetMethods(AccessTools.all);
                foreach (var method in methods)
                {
                    if (method.Name.Contains("SelectionWeight"))
                    {
                        return method;
                    }
                }
            }
            return null;
        }

        public static void Postfix(ref float __result, Gene g)
        {
            if (g.IsGrantedGene())
            {
                __result = 0f;
            }
        }
    }
}
