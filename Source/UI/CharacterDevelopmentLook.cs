using System;
using UnityEngine;
using Verse;

namespace WantsAndQuirks
{
    // Draws the wants tab in another look. Set by the UI Rethemed add-on, which loads only with UI Rethemed - Core.
    public interface IWantsTabLook
    {
        // True once the look has failed; the tab then draws in the classic look.
        bool Failed { get; }
        Vector2 Size(ITab_Pawn_WantsAndQuirks tab, Pawn pawn);
        void Draw(ITab_Pawn_WantsAndQuirks tab, Pawn pawn, Rect rect);
    }

    // The looks other assemblies give the mod's screens; with none set, or after one fails, the classic look draws.
    public static class CharacterDevelopmentLook
    {
        public static IWantsTabLook WantsTab;
        private static bool wantsTabFailed;

        public static IWantsTabLook ActiveWantsTab => wantsTabFailed || WantsTab == null || WantsTab.Failed ? null : WantsTab;

        public static void WantsTabFailed(Exception ex)
        {
            wantsTabFailed = true;
            Log.Error("[Character Development] The themed wants tab failed and stays classic for this session: " + ex);
        }
    }
}
