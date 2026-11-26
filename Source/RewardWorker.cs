using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public class RewardWorker
    {
        public RewardDef def;

        public virtual IEnumerable<ThingDef> GetValidItems(Map map)
        {
            if (!def.requiresItem)
                yield return null;
        }

        public virtual IEnumerable<Pawn> GetValidPawns(Map map)
        {
            if (!def.requiresPawn)
                yield break;
        }

        public virtual bool CanGenerate()
        {
            if (WantsAndQuirksMod.settings.disabledRewardDefNames.Contains(def.defName))
                return false;
            return def.CanGenerate();
        }

        public virtual bool CanBestowOn(Pawn pawn, ThingDef item = null, Pawn targetPawn = null)
        {
            if (def.isQuirk && pawn.GetWantsData().HasQuirk(def, item, targetPawn))
                return false;
            return def.PassesRecipientFilter(pawn);
        }

        public virtual void OnAcquired(Pawn pawn, Quirk quirk)
        {
        }

        public virtual void OnRemoved(Pawn pawn, Quirk quirk)
        {
        }

        public virtual void Notify_Ingested(Pawn pawn, Quirk quirk, Thing ingestible)
        {
        }

        protected void TryGainThought(Pawn pawn, Quirk quirk)
        {
            if (quirk.def.thought != null)
            {
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(quirk.def.thought);
            }
        }
    }
}
