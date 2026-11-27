using System.Reflection;
using HarmonyLib;
using UIRethemed.Core;
using Verse;
using WantsAndQuirks;

namespace WantsAndQuirksUIRethemed
{
    // Gives Character Development's screens the UI Rethemed look; this assembly loads only with UI Rethemed - Core.
    [StaticConstructorOnStartup]
    public static class UIRethemedStartup
    {
        static UIRethemedStartup()
        {
            CharacterDevelopmentLook.WantsTab = new WantsTabLook();
            CharacterDevelopmentLook.CharactersMenu = new CharactersMenuLook();
            CharacterDevelopmentLook.Settings = new SettingsLook();
            var mod = LoadedModManager.GetMod<WantsAndQuirksMod>();
            SettingsWindow.UseTheme(mod);
            SettingsWindow.Size(mod, 1100f, 820f);
            new Harmony("ferny.characterdevelopment.uirethemed").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
