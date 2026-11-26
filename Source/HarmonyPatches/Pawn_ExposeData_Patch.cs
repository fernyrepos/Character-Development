using HarmonyLib;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
    public static class Pawn_ExposeData_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.CanHaveWants())
            {
                var data = __instance.GetWantsData();
                data.ExposeData();
                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    if (data.nextWantTick == -1)
                    {
                        WantsAndQuirksUtility.InitializePawnWants(__instance, data);
                    }
                }
            }
        }
    }
}
