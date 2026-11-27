using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(SettleUtility), nameof(SettleUtility.AddNewHome))]
    public static class SettleUtility_AddNewHome_Patch
    {
        public static void Postfix(Faction faction)
        {
            if (faction == Faction.OfPlayer)
            {
                foreach (var map in Find.Maps)
                {
                    foreach (var pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
                    {
                        if (pawn.CanHaveWants())
                        {
                            WantsAndQuirksUtility.CheckWants(pawn, new WantWorkerContext(triggerType: WantTriggerType.NewSettlement));
                        }
                    }
                }
                foreach (var caravan in Find.WorldObjects.Caravans)
                {
                    if (caravan.Faction == Faction.OfPlayer)
                    {
                        foreach (var pawn in caravan.PawnsListForReading)
                        {
                            if (pawn.CanHaveWants())
                            {
                                WantsAndQuirksUtility.CheckWants(pawn, new WantWorkerContext(triggerType: WantTriggerType.NewSettlement));
                            }
                        }
                    }
                }
            }
        }
    }
}
