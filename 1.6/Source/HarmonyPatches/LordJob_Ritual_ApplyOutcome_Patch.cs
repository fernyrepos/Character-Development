using HarmonyLib;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [HarmonyPatch(typeof(LordJob_Ritual), nameof(LordJob_Ritual.ApplyOutcome))]
    public static class LordJob_Ritual_ApplyOutcome_Patch
    {
        public static void Postfix(LordJob_Ritual __instance, float progress, bool cancelled)
        {
            if (cancelled || __instance.Ritual == null)
                return;

            if (!RitualOutcomePositive(__instance))
                return;

            var ritualDef = __instance.Ritual.def;

            foreach (var pawn in __instance.assignments.Participants)
            {
                if (pawn.CanHaveWants())
                {
                    var role = __instance.assignments.RoleForPawn(pawn);
                    WantsAndQuirksUtility.CheckWants(pawn, new WantWorkerContext(triggerType: WantTriggerType.RitualCompleted, contextDef: ritualDef, contextString: role?.id));
                }
            }
        }

        private static bool RitualOutcomePositive(LordJob_Ritual ritual)
        {
            if (ritual.Ritual?.outcomeEffect is RitualOutcomeEffectWorker_FromQuality fromQuality)
            {
                if (fromQuality.def.outcomeChances.NullOrEmpty())
                    return true;

                var quality = fromQuality.GetQuality(ritual, ritual.Progress);
                var outcome = fromQuality.GetOutcome(quality, ritual);
                return outcome?.Positive ?? false;
            }
            return true;
        }
    }
}
