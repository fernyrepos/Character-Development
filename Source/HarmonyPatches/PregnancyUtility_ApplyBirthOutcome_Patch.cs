using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [StaticConstructorOnStartup]
    public static class PregnancyUtility_ApplyBirthOutcome_Patch
    {
        static PregnancyUtility_ApplyBirthOutcome_Patch()
        {
            WantsAndQuirksMod.harmony.Patch(
                AccessTools.Method(typeof(PregnancyUtility), nameof(PregnancyUtility.ApplyBirthOutcome)),
                postfix: new HarmonyMethod(typeof(PregnancyUtility_ApplyBirthOutcome_Patch), nameof(Postfix)));
        }

        public static void Postfix(Thing __result, Pawn geneticMother, Pawn father)
        {
            var pawn = __result as Pawn ?? (__result as Corpse)?.InnerPawn;
            if (pawn == null)
                return;

            var parents = new List<Pawn> { geneticMother, father }.Where(x => x != null).ToList();
            if (parents.Count == 0)
            {
                return;
            }

            foreach (var parent in parents)
            {
                if (parent.CanHaveWants())
                {
                    WantsAndQuirksUtility.CheckWants(parent, new WantWorkerContext(WantTriggerType.GaveBirth, contextPawn: pawn));
                }

                foreach (var grandparent in GetParents(parent))
                {
                    if (grandparent.CanHaveWants())
                    {
                        WantsAndQuirksUtility.CheckWants(grandparent, new WantWorkerContext(WantTriggerType.GaveBirth, contextPawn: parent));
                    }
                }
            }

            if (pawn.genes == null)
            {
                return;
            }

            var data = pawn.GetWantsData();
            var grantedCount = 0;
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                var quirkDef = GetParentQuirkDef(parents, gene.def);
                if (quirkDef == null)
                {
                    continue;
                }
                if (data.HasQuirk(quirkDef, null, null))
                {
                    continue;
                }
                var quirk = new Quirk(quirkDef);
                data.quirks.Add(quirk);
                data.grantedGenes.Add(new GrantedGeneLink(gene, quirk));
                grantedCount++;
            }
        }

        private static IEnumerable<Pawn> GetParents(Pawn pawn)
        {
            if (pawn.relations == null)
                yield break;

            foreach (var rel in pawn.relations.DirectRelations)
            {
                if (rel.def == PawnRelationDefOf.Parent && rel.otherPawn != null)
                    yield return rel.otherPawn;
            }
        }

        private static RewardDef GetParentQuirkDef(List<Pawn> parents, GeneDef geneDef)
        {
            foreach (var parent in parents)
            {
                var parentData = parent.GetWantsData();
                foreach (var link in parentData.grantedGenes)
                {
                    if (link.gene?.def == geneDef && link.quirk != null)
                    {
                        return link.quirk.def;
                    }
                }
            }
            return null;
        }
    }
}
