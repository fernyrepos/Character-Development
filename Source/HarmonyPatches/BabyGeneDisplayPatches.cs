using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public static class BabyGeneDisplayUtility
    {
        private static HashSet<GeneDef> quirkGenes;

        public static bool IsQuirkGene(GeneDef def)
        {
            quirkGenes ??= DefDatabase<RewardDef>.AllDefsListForReading.Where(r => r.gene != null).Select(r => r.gene).ToHashSet();
            return quirkGenes.Contains(def);
        }

        public static IEnumerable<GeneDef> WithoutQuirkGenes(IEnumerable<GeneDef> genes)
        {
            return genes?.Where(g => !IsQuirkGene(g));
        }
    }

    [StaticConstructorOnStartup]
    public static class XenotypeSummaryCompat
    {
        static XenotypeSummaryCompat()
        {
            if (!ModsConfig.IsActive("Zhuma.XenotypeSummary"))
                return;

            var method = AccessTools.Method("XenotypeSummary.PatchUtility:GetGenes");
            if (method == null)
                return;

            WantsAndQuirksMod.harmony.Patch(method, postfix: new HarmonyMethod(typeof(XenotypeSummaryCompat), nameof(Postfix)));
        }

        public static void Postfix(Thing target, GeneSet genesOverride, ref IEnumerable<GeneDef> __result)
        {
            if (target == null && genesOverride != null)
                __result = BabyGeneDisplayUtility.WithoutQuirkGenes(__result);
        }
    }
}
