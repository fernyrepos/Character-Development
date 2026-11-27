using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public class ThoughtWorker_Quirk_EarlyBird : ThoughtWorker
    {
        public override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.CanHaveWants() && p.GetWantsData().quirks.Any(q => q.def == DefsOf.WQ_Quirk_EarlyBird))
            {
                var hour = GenLocalDate.HourInteger(p);
                if (hour >= 6 && hour < 12)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
