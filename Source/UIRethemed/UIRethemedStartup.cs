using System.Reflection;
using HarmonyLib;
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
            new Harmony("ferny.characterdevelopment.uirethemed").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
