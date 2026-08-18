using UnityEngine;
using Verse;

namespace WantsAndQuirks
{
    public class WantsAndQuirksSettings : ModSettings
    {
        public bool enableWantsSystem = true;
        public bool enableCharactersMenu = true;
        public bool enableMentalBreakWants = true;
        public bool disableTechLevelRestrictions = false;
        public int bubblesPerRoll = 10;
        public bool rerollBubblesOnSelection = false;
        public int rerollsPerWant = 2;
        public int pointsNeededForReward = 1000;
        private string pointsNeededForRewardBuffer;
        public int pointsNeededIncreasePerCompletion = 0;
        private string pointsNeededIncreasePerCompletionBuffer;
        public int startingWantsCount = 0;
        public int maxActiveWants = 4;
        public IntRange wantGenerationFrequencyDays = new IntRange(1, 8);
        public bool pawnSpecificRewardPoints = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableWantsSystem, "enableWantsSystem", true);
            Scribe_Values.Look(ref enableCharactersMenu, "enableCharactersMenu", true);
            Scribe_Values.Look(ref enableMentalBreakWants, "enableMentalBreakWants", true);
            Scribe_Values.Look(ref disableTechLevelRestrictions, "disableTechLevelRestrictions", false);
            Scribe_Values.Look(ref bubblesPerRoll, "bubblesPerRoll", 10);
            Scribe_Values.Look(ref rerollBubblesOnSelection, "rerollBubblesOnSelection", false);
            Scribe_Values.Look(ref rerollsPerWant, "rerollsPerWant", 2);
            Scribe_Values.Look(ref pointsNeededForReward, "pointsNeededForReward", 1000);
            Scribe_Values.Look(ref pointsNeededIncreasePerCompletion, "pointsNeededIncreasePerCompletion", 0);
            Scribe_Values.Look(ref startingWantsCount, "startingWantsCount", 0);
            Scribe_Values.Look(ref maxActiveWants, "maxActiveWants", 4);
            Scribe_Values.Look(ref wantGenerationFrequencyDays, "wantGenerationFrequencyDays", new IntRange(1, 8));
            Scribe_Values.Look(ref pawnSpecificRewardPoints, "pawnSpecificRewardPoints", false);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            var ls = new Listing_Standard();
            ls.Begin(inRect);
            ls.CheckboxLabeled("WQ_EnableWantsSystem".Translate(), ref enableWantsSystem);
            ls.CheckboxLabeled("WQ_EnableCharactersMenu".Translate(), ref enableCharactersMenu);
            ls.CheckboxLabeled("WQ_EnableMentalBreakWants".Translate(), ref enableMentalBreakWants);
            ls.CheckboxLabeled("WQ_DisableTechLevelRestrictions".Translate(), ref disableTechLevelRestrictions);
            ls.Label("WQ_BubblesPerRoll".Translate(bubblesPerRoll));
            bubblesPerRoll = (int)ls.Slider(bubblesPerRoll, 1, 50);
            ls.CheckboxLabeled("WQ_RerollBubblesOnSelection".Translate(), ref rerollBubblesOnSelection);
            ls.Label("WQ_RerollsPerWant".Translate(rerollsPerWant));
            rerollsPerWant = (int)ls.Slider(rerollsPerWant, 0, 10);
            ls.Label("WQ_PointsNeededForReward".Translate(pointsNeededForReward));
            ls.TextFieldNumeric(ref pointsNeededForReward, ref pointsNeededForRewardBuffer, 100, 3000);
            ls.Label("WQ_PointsNeededIncreasePerCompletion".Translate(pointsNeededIncreasePerCompletion));
            ls.TextFieldNumeric(ref pointsNeededIncreasePerCompletion, ref pointsNeededIncreasePerCompletionBuffer, 0, 1000);
            ls.Label("WQ_StartingWantsCount".Translate(startingWantsCount));
            startingWantsCount = (int)ls.Slider(startingWantsCount, 0, 10);
            ls.Label("WQ_MaxActiveWants".Translate(maxActiveWants));
            maxActiveWants = (int)ls.Slider(maxActiveWants, 1, 10);
            ls.Label("WQ_WantGenerationFrequency".Translate(wantGenerationFrequencyDays.min, wantGenerationFrequencyDays.max));
            ls.IntRange(ref wantGenerationFrequencyDays, 1, 60);
            ls.CheckboxLabeled("WQ_PawnSpecificRewardPoints".Translate(), ref pawnSpecificRewardPoints);
            ls.End();
        }
    }
}
