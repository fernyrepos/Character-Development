using System.Collections.Generic;
using Verse;

namespace WantsAndQuirks
{
    public static class State
    {
        public static int characterPoints;
        public static int currentCharacterPointsNeeded;
        public static int rewardPoints;
        public static List<RewardNode> rewardNodes = new List<RewardNode>();
        public static int quirkLoadIDCounter;

        public static void ExposeData()
        {
            Scribe_Values.Look(ref characterPoints, "WQ_characterPoints", 0);
            Scribe_Values.Look(ref currentCharacterPointsNeeded, "WQ_currentCharacterPointsNeeded", 0);
            Scribe_Values.Look(ref rewardPoints, "WQ_rewardPoints", 0);
            Scribe_Collections.Look(ref rewardNodes, "WQ_rewardNodes", LookMode.Deep);
            Scribe_Values.Look(ref quirkLoadIDCounter, "WQ_quirkLoadIDCounter", 0);
            rewardNodes ??= new List<RewardNode>();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                rewardNodes.RemoveAll(n => n.def == null || (n.def.requiresItem && n.item == null) || (n.def.requiresPawn && n.pawnTarget == null));
                for (int i = 0; i < rewardNodes.Count; i++)
                {
                    rewardNodes[i].drawPos = rewardNodes[i].pos;
                }
            }
        }

        public static void Reset()
        {
            characterPoints = 0;
            currentCharacterPointsNeeded = 0;
            rewardPoints = 0;
            rewardNodes = new List<RewardNode>();
            quirkLoadIDCounter = 0;
        }
    }
}
