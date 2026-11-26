using HarmonyLib;
using RimWorld;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(TradeDeal), nameof(TradeDeal.TryExecute))]
    public static class TradeDeal_TryExecute_Patch
    {
        public static void Postfix(bool __result, ref bool actuallyTraded)
        {
            if (__result && actuallyTraded && TradeSession.playerNegotiator != null && TradeSession.playerNegotiator.CanHaveWants())
            {
                WantsAndQuirksUtility.CheckWants(TradeSession.playerNegotiator, new WantWorkerContext(triggerType: WantTriggerType.Traded));
            }
        }
    }
}
