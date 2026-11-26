using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public class Thought_Memory_LikesThing : Thought_Memory
    {
        public ThingDef thingDef;

        public override string LabelCap
        {
            get
            {
                if (thingDef != null)
                    return def.stages[0].label.Formatted(thingDef.label).CapitalizeFirst();
                return base.LabelCap;
            }
        }

        public override string Description
        {
            get
            {
                if (thingDef != null)
                    return def.stages[0].description.Formatted(thingDef.label);
                return base.Description;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref thingDef, "thingDef");
        }
    }
}
