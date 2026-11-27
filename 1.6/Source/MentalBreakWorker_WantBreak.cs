using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace WantsAndQuirks
{
    public class MentalBreakWorker_WantBreak : MentalBreakWorker
    {
        public override bool BreakCanOccur(Pawn pawn)
        {
            return pawn.CanHaveWants() && base.BreakCanOccur(pawn);
        }

        public override bool TryStart(Pawn pawn, string reason, bool causedByMood)
        {
            if (!base.TryStart(pawn, reason, causedByMood))
            {
                return false;
            }

            var data = pawn.GetWantsData();
            var mentalWants = DefDatabase<WantDef>.AllDefsListForReading
            .Where(w => w.isMentalBreakWant && !data.activeWants.Any(aw => aw.def == w) && w.Worker.CanHaveWant(pawn) && w.Worker.CanGenerate(pawn))
            .ToList();

            if (!mentalWants.TryRandomElementByWeight(w => w.commonality * WantsAndQuirksMod.settings.GetCommonalityMultiplier(w), out var chosen))
                return false;
            ActiveWant replaced = null;

            if (data.activeWants.Count >= WantsAndQuirksMod.settings.maxActiveWants || (data.activeWants.Count > 0 && Rand.Bool))
            {
                replaced = data.activeWants.RandomElement();
                data.activeWants.Remove(replaced);
            }

            var targetPawn = chosen.Worker.GetRandomTargetPawn(pawn);
            ActiveWant newWant;
            if (targetPawn != null)
            {
                newWant = new ActiveWantWithPawnTarget { def = chosen, targetPawn = targetPawn, assignedTick = Find.TickManager.TicksGame };
            }
            else
            {
                var targetDef = chosen.Worker.GetRandomTarget(pawn);
                if (targetDef != null)
                {
                    newWant = new ActiveWantWithTarget { def = chosen, targetDef = targetDef, assignedTick = Find.TickManager.TicksGame };
                }
                else
                {
                    newWant = chosen.Worker.CreateActiveWant(pawn);
                    newWant.def = chosen;
                    newWant.assignedTick = Find.TickManager.TicksGame;
                }
            }

            data.activeWants.Add(newWant);

            var msg = replaced != null
            ? "WQ_MentalBreakWantReplaced".Translate(pawn.Named("PAWN"), newWant.LabelCap, replaced.LabelCap)
            : "WQ_MentalBreakWantAdded".Translate(pawn.Named("PAWN"), newWant.LabelCap);

            Messages.Message(msg, pawn, MessageTypeDefOf.NegativeEvent);

            return true;
        }
    }
}
