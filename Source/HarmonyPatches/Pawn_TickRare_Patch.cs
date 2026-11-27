using HarmonyLib;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TickRare))]
    public static class Pawn_TickRare_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.CanHaveWants())
            {
                WantsAndQuirksUtility.TickWants(__instance);
            }
        }
    }
}
