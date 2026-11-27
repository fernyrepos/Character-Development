using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Need_Food), nameof(Need_Food.FoodFallPerTickAssumingCategory))]
    public static class Need_Food_FoodFallPerTickAssumingCategory_Patch
    {
        public static void Postfix(Need_Food __instance, ref float __result)
        {
            var pawn = __instance.pawn;
            if (pawn.genes != null && WantsAndQuirksUtility.TryGetWantsData(pawn, out var data))
            {
                if (data.grantedGenes.Count > 0)
                {
                    var ogMet = 0;
                    var trueMet = 0;
                    foreach (var item in pawn.genes.GenesListForReading)
                    {
                        if (!item.Overridden)
                        {
                            ogMet += item.def.biostatMet;
                            if (!item.IsGrantedGene())
                            {
                                trueMet += item.def.biostatMet;
                            }
                        }
                    }

                    var ogMultiplier = GeneTuning.MetabolismToFoodConsumptionFactorCurve.Evaluate(ogMet);
                    var trueMultiplier = GeneTuning.MetabolismToFoodConsumptionFactorCurve.Evaluate(trueMet);
                    __result = (__result / ogMultiplier) * trueMultiplier;
                }
            }
        }
    }
}
