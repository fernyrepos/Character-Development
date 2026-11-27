using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public class ThoughtWorker_Quirk_LikesPerson : ThoughtWorker
    {
        public override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
        {
            if (p.CanHaveWants())
            {
                var quirks = p.GetWantsData().quirks;
                for (int i = 0; i < quirks.Count; i++)
                {
                    if (quirks[i].def == DefsOf.WQ_Quirk_LikesPerson && quirks[i].pawnTarget == other)
                        return true;
                }
            }
            return false;
        }
    }

    public class ThoughtWorker_Quirk_DislikesPerson : ThoughtWorker
    {
        public override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
        {
            if (p.CanHaveWants())
            {
                var quirks = p.GetWantsData().quirks;
                for (int i = 0; i < quirks.Count; i++)
                {
                    if (quirks[i].def == DefsOf.WQ_Quirk_DislikesPerson && quirks[i].pawnTarget == other)
                        return true;
                }
            }
            return false;
        }
    }

    public class ThoughtWorker_Quirk_LovesPerson : ThoughtWorker
    {
        public override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
        {
            if (p.CanHaveWants())
            {
                var quirks = p.GetWantsData().quirks;
                for (int i = 0; i < quirks.Count; i++)
                {
                    if (quirks[i].def == DefsOf.WQ_Quirk_LovesPerson && quirks[i].pawnTarget == other)
                        return true;
                }
            }
            return false;
        }
    }
}
