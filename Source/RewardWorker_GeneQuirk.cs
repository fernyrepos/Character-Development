using Verse;

namespace WantsAndQuirks
{
    public class RewardWorker_GeneQuirk : RewardWorker
    {
        public override void OnAcquired(Pawn pawn, Quirk quirk)
        {
            var gene = pawn.genes.AddGene(def.gene, true);
            pawn.GetWantsData().grantedGenes.Add(new GrantedGeneLink(gene, quirk));
        }

        public override void OnRemoved(Pawn pawn, Quirk quirk)
        {
            var data = pawn.GetWantsData();
            for (int i = data.grantedGenes.Count - 1; i >= 0; i--)
            {
                var link = data.grantedGenes[i];
                if (link.quirk == quirk && link.gene.IsGrantedGene())
                {
                    pawn.genes.RemoveGene(link.gene);
                    data.grantedGenes.RemoveAt(i);
                }
            }
        }
    }
}
