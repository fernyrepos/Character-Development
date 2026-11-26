using System.Reflection;
using HarmonyLib;
using Verse;
namespace WantsAndQuirks
{
    [HarmonyPatch]
    public static class VehiclePawn_TryAddPawn_Patch
    {
        public static bool Prepare() => ModsConfig.IsActive("smashphil.vehicleframework");
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("Vehicles.VehiclePawn"), "TryAddPawn", new[] { typeof(Pawn), AccessTools.TypeByName("Vehicles.VehicleRoleHandler") });
        }
        public static void Postfix(Pawn pawn, bool __result)
        {
            if (__result && pawn.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(pawn, new WantWorkerContext(triggerType: WantTriggerType.BoardedVehicle));
            }
        }
    }
}
