using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class Pawn_SetFaction_Patch
    {
        public static void Prefix(Pawn __instance, Faction newFaction)
        {
            if (__instance.Faction == Faction.OfPlayer && newFaction != Faction.OfPlayer)
            {
                if (__instance.CanHaveWants())
                {
                    WantsAndQuirksUtility.CheckWants(__instance, new WantWorkerContext(triggerType: WantTriggerType.LeftFaction));
                }
            }
        }

        public static void Postfix(Pawn __instance, Faction newFaction, Pawn recruiter)
        {
            if (newFaction == Faction.OfPlayer && __instance.RaceProps.Animal && recruiter != null && recruiter.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(recruiter, new WantWorkerContext(triggerType: WantTriggerType.AnimalTamed, contextDef: __instance.def));
            }
        }
    }
}
