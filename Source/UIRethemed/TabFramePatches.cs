using HarmonyLib;
using Verse;
using WantsAndQuirks;

namespace WantsAndQuirksUIRethemed
{
    // While the themed wants tab is the one drawn, vanilla's square tab background and close button are left out, as UI Rethemed - Inspector Tabs does for its tabs.
    [HarmonyPatch(typeof(InspectTabBase), nameof(InspectTabBase.DoTabGUI))]
    internal static class Patch_InspectTabBase_DoTabGUI
    {
        internal const int TabWindowId = 235086;

        // Set for each tab drawn, and read while its window draws later in the frame.
        internal static bool themedWantsTab;

        private static void Prefix(InspectTabBase __instance)
        {
            themedWantsTab = __instance is ITab_Pawn_WantsAndQuirks && CharacterDevelopmentLook.ActiveWantsTab != null;
        }
    }

    [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.ImmediateWindow))]
    internal static class Patch_WindowStack_ImmediateWindow
    {
        private static void Prefix(int ID, ref bool doBackground)
        {
            if (ID == Patch_InspectTabBase_DoTabGUI.TabWindowId && Patch_InspectTabBase_DoTabGUI.themedWantsTab)
            {
                doBackground = false;
            }
        }
    }

    [HarmonyPatch(typeof(Widgets), nameof(Widgets.CloseButtonFor))]
    internal static class Patch_Widgets_CloseButtonFor
    {
        private static bool Prefix(ref bool __result)
        {
            if (Patch_InspectTabBase_DoTabGUI.themedWantsTab && Find.WindowStack.currentlyDrawnWindow is ImmediateWindow window && window.ID == Patch_InspectTabBase_DoTabGUI.TabWindowId)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
