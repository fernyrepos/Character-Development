using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class NodeResearch_AdvanceEra_Patch
    {
        public static bool Prepare() => ModsConfig.IsActive("ferny.noderesearch");
        public static void Postfix(ResearchProjectDef proj)
        {
            if (proj.modExtensions != null && proj.modExtensions.Any(e => e.GetType().Name == "EmergenceExtension"))
            {
                foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
                {
                    if (pawn.CanHaveWants())
                    {
                        WantsAndQuirksUtility.CheckWants(pawn, new WantWorkerContext(triggerType: WantTriggerType.AdvancedEra));
                    }
                }
            }
        }
    }
}
