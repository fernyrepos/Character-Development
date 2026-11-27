using UIRethemed.Core;
using UnityEngine;
using WantsAndQuirks;

namespace WantsAndQuirksUIRethemed
{
    // Character Development's theme colors, chosen in UI Rethemed - Core's settings.
    internal static class Looks
    {
        private const string Mod = "ferny.characterdevelopment";

        public static Color Reward => Theme.ModColor(Mod, "reward");
        public static Color Progress => Theme.ModColor(Mod, "progress");
        public static Color MentalBreak => Theme.ModColor(Mod, "mentalBreak");
        public static Color Points => Theme.ModColor(Mod, "points");
        public static Color NewGlow => Theme.ModColor(Mod, "newGlow");

        public static Color Rarity(RewardRarity rarity)
        {
            switch (rarity)
            {
                case RewardRarity.Uncommon: return Theme.ModColor(Mod, "rarityUncommon");
                case RewardRarity.Rare: return Theme.ModColor(Mod, "rarityRare");
                case RewardRarity.Legendary: return Theme.ModColor(Mod, "rarityLegendary");
                default: return Theme.ModColor(Mod, "rarityCommon");
            }
        }
    }
}
