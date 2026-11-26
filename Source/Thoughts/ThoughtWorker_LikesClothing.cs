using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public class ThoughtWorker_LikesClothing : ThoughtWorker
    {
        public override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.CanHaveWants())
                return false;

            var quirks = p.GetWantsData().quirks;
            for (int i = 0; i < quirks.Count; i++)
            {
                var quirk = quirks[i];
                if (quirk.def == DefsOf.WQ_Quirk_LikesClothing)
                {
                    var worn = p.apparel.WornApparel;
                    for (int j = 0; j < worn.Count; j++)
                    {
                        if (worn[j].def == quirk.item)
                            return ThoughtState.ActiveAtStage(0, quirk.item.label);
                    }
                }
            }

            return false;
        }
    }
}
