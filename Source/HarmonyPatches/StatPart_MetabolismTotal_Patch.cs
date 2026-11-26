using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(StatPart_MetabolismTotal), nameof(StatPart_MetabolismTotal.CurveXGetter))]
    public static class StatPart_MetabolismTotal_Patch
    {
        public static void Postfix(StatRequest req, ref float __result)
        {
            if (req.Thing is Pawn pawn)
            {
                var genes = pawn.genes.GenesListForReading;
                for (int i = 0; i < genes.Count; i++)
                {
                    if (genes[i].IsGrantedGene())
                        __result -= genes[i].def.biostatMet;
                }
            }
        }
    }
}
