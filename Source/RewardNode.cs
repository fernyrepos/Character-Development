using UnityEngine;
using Verse;

namespace WantsAndQuirks
{
    public class RewardNode : IExposable
    {
        public RewardDef def;
        public ThingDef item;
        public Pawn pawnTarget;
        public Vector2 pos;
        public Vector2 velocity;
        [Unsaved(false)]
        public Vector2 drawPos;
        [Unsaved(false)]
        public Vector2 dampVelocity;
        public bool hasBeenViewed;
        [Unsaved(false)]
        public float glowStartRealTime = -1f;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Defs.Look(ref item, "item");
            Scribe_References.Look(ref pawnTarget, "pawnTarget");
            Scribe_Values.Look(ref pos, "pos");
            Scribe_Values.Look(ref hasBeenViewed, "hasBeenViewed", true);
        }

        public string LabelCap => def.requiresItem ? string.Format(def.LabelCap, item.label).CapitalizeFirst() : (def.requiresPawn && pawnTarget != null ? string.Format(def.LabelCap, pawnTarget.LabelShort).CapitalizeFirst() : def.LabelCap);
        public string Description => def.requiresItem ? string.Format(def.description, item.label) : (def.requiresPawn && pawnTarget != null ? string.Format(def.description, pawnTarget.LabelShort) : def.description);
    }
}
