using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public class ThoughtWorker_Quirk_NightOwl : ThoughtWorker
    {
        public override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.CanHaveWants() && p.GetWantsData().quirks.Any(q => q.def == DefsOf.WQ_Quirk_NightOwl))
            {
                var hour = GenLocalDate.HourInteger(p);
                if (hour >= 20 || hour < 4)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
