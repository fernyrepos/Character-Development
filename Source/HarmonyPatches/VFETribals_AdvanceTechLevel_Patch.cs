using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
namespace WantsAndQuirks
{
    [HarmonyPatch]
    public static class VFETribals_AdvanceTechLevel_Patch
    {
        public static bool Prepare() => ModsConfig.IsActive("OskarPotocki.VFE.Tribals");
        public static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("VFETribals.GameComponent_Tribals"), "AdvanceTechLevel");
        public static void Postfix()
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
