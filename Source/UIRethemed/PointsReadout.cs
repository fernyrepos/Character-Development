using UIRethemed.Core;
using UnityEngine;
using Verse;
using WantsAndQuirks;

namespace WantsAndQuirksUIRethemed
{
    // Character points toward the next reward point: the pawn's, or the colony's when points aren't per pawn (pawn may then be null).
    internal static class PointsReadout
    {
        public static void Draw(Rect rect, Pawn pawn)
        {
            var perPawn = WantsAndQuirksMod.settings.pawnSpecificRewardPoints && pawn != null;
            var data = perPawn ? pawn.GetWantsData() : null;
            var current = perPawn ? data.characterPoints : State.characterPoints;
            var needed = perPawn ? WantsAndQuirksUtility.GetPawnCharacterPointsNeeded(data) : WantsAndQuirksUtility.GetGlobalCharacterPointsNeeded();
            var label = (perPawn ? "WQ_CharacterPoints" : "WQ_UIR_ColonyCharacterPoints").Translate() + "  " + CoreWidgets.Amount(current) + " / " + CoreWidgets.Amount(needed);
            CoreWidgets.Readout(rect, WantCards.StarTex, Looks.Points, label, needed > 0 ? Mathf.Clamp01((float)current / needed) : 0f, false);
            var toSpend = pawn != null ? WantsAndQuirksUtility.RewardPointsToSpend(pawn) : State.rewardPoints;
            CoreWidgets.Tip(rect, "WQ_CharacterPoints".Translate(), "WQ_UIR_PointsTip".Translate(CoreWidgets.Amount(needed), toSpend));
        }
    }
}
