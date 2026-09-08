using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory), new Type[] { typeof(Thought_Memory), typeof(Pawn) })]
    public static class MemoryThoughtHandler_TryGainMemory_Patch
    {
        public static void Postfix(MemoryThoughtHandler __instance, Thought_Memory newThought)
        {
            // newThought can be null here: other mods patch TryGainMemory taking the parameter
            // by ref (Vanilla Traits Expanded, Vanilla Expanded Framework, ReGrowth Core and
            // Alien Races all do) and may null or replace it before this postfix runs.
            if (newThought?.def == null)
            {
                return;
            }

            var pawn = __instance.pawn;
            if (pawn.CanHaveWants())
            {
                var data = pawn.GetWantsData();
                for (int i = data.activeWants.Count - 1; i >= 0; i--)
                {
                    var want = data.activeWants[i];
                    if (want?.def == null)
                    {
                        continue;
                    }
                    if (want.def.completedByThought == newThought.def)
                    {
                        WantsAndQuirksUtility.CompleteWant(pawn, data, want);
                    }
                }
            }
        }
    }
}
