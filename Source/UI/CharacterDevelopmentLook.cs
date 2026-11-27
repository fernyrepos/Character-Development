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

    // Draws the Characters menu in another look, frame included.
    public interface ICharactersMenuLook
    {
        // True once the look has failed; the menu then draws in the classic look.
        bool Failed { get; }
        Vector2 Size(MainTabWindow_Characters menu);
        void Draw(MainTabWindow_Characters menu, Rect inRect);
    }

    // Draws the mod settings page in another look.
    public interface ISettingsLook
    {
        // True once the look has failed; the page then draws in the classic look.
        bool Failed { get; }
        void Draw(WantsAndQuirksMod mod, Rect inRect);
    }

    // The looks other assemblies give the mod's screens; with none set, or after one fails, the classic look draws.
    public static class CharacterDevelopmentLook
    {
        public static IWantsTabLook WantsTab;
        public static ICharactersMenuLook CharactersMenu;
        public static ISettingsLook Settings;
        private static bool settingsFailed;
        private static bool wantsTabFailed;
        private static bool charactersMenuFailed;

        public static IWantsTabLook ActiveWantsTab => wantsTabFailed || WantsTab == null || WantsTab.Failed ? null : WantsTab;

        public static ICharactersMenuLook ActiveCharactersMenu => charactersMenuFailed || CharactersMenu == null || CharactersMenu.Failed ? null : CharactersMenu;

        public static ISettingsLook ActiveSettings => settingsFailed || Settings == null || Settings.Failed ? null : Settings;

        public static void SettingsFailed(Exception ex)
        {
            settingsFailed = true;
            Log.Error("[Character Development] The themed settings page failed and stays classic for this session: " + ex);
        }

        public static void CharactersMenuFailed(Exception ex)
        {
            charactersMenuFailed = true;
            Log.Error("[Character Development] The themed Characters menu failed and stays classic for this session: " + ex);
        }

        public static void WantsTabFailed(Exception ex)
        {
            wantsTabFailed = true;
            Log.Error("[Character Development] The themed wants tab failed and stays classic for this session: " + ex);
        }
    }
}
