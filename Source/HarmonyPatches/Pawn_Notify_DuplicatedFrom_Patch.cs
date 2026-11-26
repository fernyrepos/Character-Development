using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Notify_DuplicatedFrom))]
    public static class Pawn_Notify_DuplicatedFrom_Patch
    {
        public static void Postfix(Pawn __instance, Pawn source)
        {
            if (!WantsAndQuirksUtility.TryGetWantsData(source, out var sourceData))
                return;

            var data = __instance.GetWantsData();
            foreach (var sourceQuirk in sourceData.quirks)
            {
                var quirk = new Quirk(sourceQuirk.def, sourceQuirk.item, sourceQuirk.pawnTarget);
                data.quirks.Add(quirk);
                var link = sourceData.grantedGenes.FirstOrDefault(l => l.quirk == sourceQuirk);
                if (link != null)
                {
                    var gene = __instance.genes.GenesListForReading.First(g => g.def == link.gene.def);
                    data.grantedGenes.Add(new GrantedGeneLink(gene, quirk));
                }
            }
        }
    }
}
