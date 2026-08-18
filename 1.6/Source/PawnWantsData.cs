using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WantsAndQuirks
{
    public class ActiveWant : IExposable
    {
        public WantDef def;
        public int assignedTick;
        public int rerollCount;

        public virtual string LabelCap => def.LabelCap;
        public virtual string Description => def.description;

        public virtual Texture Icon
        {
            get
            {
                if (def.discoveryRequirementThing != null && !def.preferIconPath)
                    return def.discoveryRequirementThing.uiIcon;
                return def.Icon;
            }
        }

        public virtual void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref assignedTick, "assignedTick");
            Scribe_Values.Look(ref rerollCount, "rerollCount");
        }

        public virtual bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return def.Worker.IsCompleted(pawn, context);
        }

        public virtual bool IsValid(Pawn pawn)
        {
            return def.Worker.IsValid(pawn);
        }
    }

    public class ActiveWantWithTarget : ActiveWant
    {
        public Def targetDef;
        private string targetDefName;
        private string targetDefTypeName;

        public override string LabelCap => def.label.Formatted(targetDef.label).CapitalizeFirst();
        public override string Description => def.description.Formatted(targetDef.label);

        public override Texture Icon
        {
            get
            {
                if (targetDef is ThingDef tDef)
                    return tDef.uiIcon;
                if (targetDef is XenotypeDef xDef)
                    return xDef.Icon;
                return base.Icon;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                targetDefName = targetDef.defName;
                targetDefTypeName = targetDef.GetType().Name;
            }
            Scribe_Values.Look(ref targetDefName, "targetDef");
            Scribe_Values.Look(ref targetDefTypeName, "targetDefType");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                var type = GenTypes.GetTypeInAnyAssembly(targetDefTypeName);
                targetDef = GenDefDatabase.GetDefSilentFail(type, targetDefName);
            }
        }

        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            if (def.Worker.IsTargetDiscovered(targetDef))
            {
                return true;
            }
            if (targetDef == context.contextDef)
            {
                return def.Worker.IsCompleted(pawn, context);
            }
            if (def.Worker.IsSatisfiedWithTarget(pawn, targetDef))
            {
                return true;
            }
            if (context.triggerType != WantTriggerType.None)
            {
                return false;
            }
            return base.IsCompleted(pawn, context);
        }

        public override bool IsValid(Pawn pawn)
        {
            if (targetDef != null && !def.Worker.IsValidWithTarget(pawn, targetDef))
                return false;
            return base.IsValid(pawn);
        }
    }

    public class ActiveWantWithPawnTarget : ActiveWant
    {
        public Pawn targetPawn;

        public override string LabelCap => def.label.Formatted(targetPawn.LabelShort).CapitalizeFirst();
        public override string Description => def.description.Formatted(targetPawn.LabelShort);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref targetPawn, "targetPawn");
        }

        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            if (targetPawn == context.contextPawn)
            {
                return def.Worker.IsCompleted(pawn, context);
            }
            if (def.Worker.IsSatisfiedWithPawnTarget(pawn, targetPawn))
            {
                return true;
            }
            if (context.triggerType != WantTriggerType.None)
            {
                return false;
            }
            return base.IsCompleted(pawn, context);
        }

        public override bool IsValid(Pawn pawn)
        {
            if (targetPawn != null && !def.Worker.IsValidWithPawnTarget(pawn, targetPawn))
                return false;
            return base.IsValid(pawn);
        }
    }

    public class GrantedGeneLink : IExposable
    {
        public Gene gene;
        public Quirk quirk;

        public GrantedGeneLink() { }

        public GrantedGeneLink(Gene gene, Quirk quirk)
        {
            this.gene = gene;
            this.quirk = quirk;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref gene, "gene");
            Scribe_References.Look(ref quirk, "quirk");
        }
    }

    public class PawnWantsData : IExposable
    {
        public List<ActiveWant> activeWants;
        public List<Quirk> quirks;
        public List<GrantedGeneLink> grantedGenes = new List<GrantedGeneLink>();
        public int nextWantTick;
        public int rewardPoints;

        public PawnWantsData()
        {
            activeWants = new List<ActiveWant>();
            quirks = new List<Quirk>();
            nextWantTick = -1;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref activeWants, "activeWants", LookMode.Deep);
            Scribe_Collections.Look(ref quirks, "quirks", LookMode.Deep);
            Scribe_Collections.Look(ref grantedGenes, "grantedGenes", LookMode.Deep);
            Scribe_Values.Look(ref nextWantTick, "nextWantTick", -1);
            Scribe_Values.Look(ref rewardPoints, "rewardPoints", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                activeWants ??= new List<ActiveWant>();
                quirks ??= new List<Quirk>();
                grantedGenes ??= new List<GrantedGeneLink>();
                activeWants.RemoveAll(w => w.def == null || (w is ActiveWantWithTarget t && t.targetDef == null) || (w is ActiveWantWithPawnTarget tp && tp.targetPawn == null));
                quirks.RemoveAll(q => q.def == null || (q.def.requiresItem && q.item == null) || (q.def.requiresPawn && q.pawnTarget == null));
                grantedGenes.RemoveAll(link => link.gene == null || link.quirk == null || !link.gene.IsGrantedGene());
            }
        }

        public bool HasQuirk(RewardDef def, ThingDef item, Pawn target)
        {
            for (int i = 0; i < quirks.Count; i++)
            {
                var q = quirks[i];
                if (q.def == def && q.item == item && q.pawnTarget == target)
                    return true;
            }
            return false;
        }
    }
}
