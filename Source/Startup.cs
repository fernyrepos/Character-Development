using System.Linq;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.race?.Humanlike == true))
            {
                var charTabType = typeof(ITab_Pawn_Character);
                var insertIndex = def.inspectorTabsResolved.Count;
                for (int i = 0; i < def.inspectorTabsResolved.Count; i++)
                {
                    if (def.inspectorTabsResolved[i].GetType() == charTabType)
                    {
                        insertIndex = i + 2;
                        break;
                    }
                }
                def.inspectorTabs.Insert(insertIndex, typeof(ITab_Pawn_WantsAndQuirks));
                def.inspectorTabsResolved.Insert(insertIndex, InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_WantsAndQuirks)));
            }
        }
    }
}
