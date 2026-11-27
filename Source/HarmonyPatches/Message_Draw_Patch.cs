using HarmonyLib;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(Message), nameof(Message.Draw))]
    public static class Message_Draw_Patch
    {
        public static Message drawingMessage;

        public static void Prefix(Message __instance)
        {
            if (WantsAndQuirksUtility.wantMessages.TryGetValue(__instance, out _))
            {
                drawingMessage = __instance;
            }
        }
    }
}
