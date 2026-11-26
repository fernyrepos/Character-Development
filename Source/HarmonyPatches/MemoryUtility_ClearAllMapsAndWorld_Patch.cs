using HarmonyLib;
using Verse.Profile;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
    public static class MemoryUtility_ClearAllMapsAndWorld_Patch
    {
        public static void Prefix()
        {
            State.Reset();
        }
    }
}
