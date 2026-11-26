using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(JoyUtility), nameof(JoyUtility.JoyTickCheckEnd))]
    public static class JoyUtility_JoyTickCheckEnd_Patch
    {
        public static void Postfix(bool __result, Pawn pawn, Building joySource)
        {
            if (__result && joySource != null && pawn.CanHaveWants())
            {
                var data = pawn.GetWantsData();
                for (int i = 0; i < data.quirks.Count; i++)
                {
                    if (data.quirks[i].def == DefsOf.WQ_Quirk_LikesRecreationBuilding && data.quirks[i].item == joySource.def)
                    {
                        if (ThoughtMaker.MakeThought(DefsOf.WQ_Thought_LikesRecreationBuilding) is Thought_Memory_LikesThing thought)
                        {
                            thought.thingDef = joySource.def;
                            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
                        }
                    }
                }
            }
        }
    }
}
