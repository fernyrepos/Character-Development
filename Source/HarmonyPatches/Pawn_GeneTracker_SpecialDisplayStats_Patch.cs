using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.SpecialDisplayStats))]
    public static class Pawn_GeneTracker_SpecialDisplayStats_Patch
    {
        public static bool Prefix(Pawn_GeneTracker __instance, ref IEnumerable<StatDrawEntry> __result)
        {
            __result = GetSpecialDisplayStats(__instance);
            return false;
        }

        private static IEnumerable<StatDrawEntry> GetSpecialDisplayStats(Pawn_GeneTracker tracker)
        {
            if (!ModLister.BiotechInstalled)
            {
                yield break;
            }
            var list = tracker.GenesListForReading;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item.Active && !item.IsGrantedGene())
                {
                    var enumerable = item.SpecialDisplayStats();
                    if (enumerable != null)
                    {
                        foreach (var stat in enumerable)
                        {
                            yield return stat;
                        }
                    }
                }
            }
        }
    }
}
