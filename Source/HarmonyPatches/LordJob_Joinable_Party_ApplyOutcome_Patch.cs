using HarmonyLib;
using RimWorld;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(LordJob_Joinable_Party), "ApplyOutcome")]
    public static class LordJob_Joinable_Party_ApplyOutcome_Patch
    {
        public static void Postfix(LordJob_Joinable_Party __instance)
        {
            if (__instance.organizer != null && __instance.organizer.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(__instance.organizer, new WantWorkerContext(triggerType: WantTriggerType.HostedParty));
            }
        }
    }
}
