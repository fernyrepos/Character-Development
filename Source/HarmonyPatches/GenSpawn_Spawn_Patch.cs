using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), new[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) })]
    public static class GenSpawn_Spawn_Patch
    {
        public static void Postfix(Thing __result, Map map, bool respawningAfterLoad)
        {
            if (respawningAfterLoad) return;
            if (__result is not Pawn pawn || !pawn.CanHaveWants()) return;
            if (map.IsPlayerHome) return;

            WantsAndQuirksUtility.CheckWants(pawn, new WantWorkerContext(triggerType: WantTriggerType.SawNewPlace));
        }
    }
}
