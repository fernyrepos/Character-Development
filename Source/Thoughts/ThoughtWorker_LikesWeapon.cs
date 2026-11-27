using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public class ThoughtWorker_LikesWeapon : ThoughtWorker
    {
        public override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.CanHaveWants())
                return false;

            var quirks = p.GetWantsData().quirks;
            for (int i = 0; i < quirks.Count; i++)
            {
                var quirk = quirks[i];
                if (quirk.def == DefsOf.WQ_Quirk_LikesWeapon && p.equipment.Primary?.def == quirk.item)
                {
                    return ThoughtState.ActiveAtStage(0, quirk.item.label);
                }
            }

            return false;
        }
    }
}
