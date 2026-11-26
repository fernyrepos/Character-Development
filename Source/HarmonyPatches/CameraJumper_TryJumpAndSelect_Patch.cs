using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(CameraJumper), nameof(CameraJumper.TryJumpAndSelect))]
    public static class CameraJumper_TryJumpAndSelect_Patch
    {
        public static void Postfix()
        {
            var msg = Message_Draw_Patch.drawingMessage;
            if (msg == null)
            {
                return;
            }
            if (!Messages.IsLive(msg))
            {
                Message_Draw_Patch.drawingMessage = null;
                return;
            }
            if (!WantsAndQuirksUtility.wantMessages.TryGetValue(msg, out var pawn))
            {
                Message_Draw_Patch.drawingMessage = null;
                return;
            }
            if (!pawn.CanHaveWants())
            {
                Message_Draw_Patch.drawingMessage = null;
                return;
            }
            Find.MainTabsRoot.SetCurrentTab(DefsOf.WQ_CharactersMenu);
            InspectPaneUtility.OpenTab(typeof(ITab_Pawn_WantsAndQuirks));
            Message_Draw_Patch.drawingMessage = null;
        }
    }
}
