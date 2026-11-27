using HarmonyLib;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentAdded))]
    public static class Pawn_EquipmentTracker_Notify_EquipmentAdded_Patch
    {
        public static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            var pawn = __instance.pawn;
            if (pawn.CanHaveWants() && eq.def.IsWeapon)
            {
                WantsAndQuirksUtility.CheckWants(pawn, new WantWorkerContext(triggerType: WantTriggerType.WeaponEquipped, contextDef: eq.def));
            }
        }
    }
}
