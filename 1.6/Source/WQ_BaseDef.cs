using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public class WQ_BaseDef : Def
    {
        public List<XenotypeDef> invalidXenotypes;
        public List<XenotypeDef> requiredXenotypes;
        public List<TraitRequirement> invalidTraits;
        public List<TraitRequirement> requiredTraits;
        public bool requiredTraitsAny;
        public List<GeneDef> invalidGenes;
        public List<GeneDef> requiredGenes;
        public bool requiredGenesAny;
        public List<HediffDef> invalidHediffs;
        public List<HediffDef> requiredHediffs;
        public bool requiredHediffsAny;
        public bool invalidNonViolent;
        public TechLevel minimumTechLevel = TechLevel.Undefined;
        public TechLevel maximumTechLevel = TechLevel.Undefined;
        public ThingDef discoveryRequirementThing;
        public FactionDef discoveryRequirementFaction;
        public XenotypeDef discoveryRequirementXenotype;
        public List<PawnRelationDef> requiredRelations;
        public Gender? requiredRelationGender = null;

        public virtual bool CanGenerate()
        {
            if (!WantsAndQuirksMod.settings.disableTechLevelRestrictions)
            {
                var tech = Faction.OfPlayer.def.techLevel;
                if (minimumTechLevel != TechLevel.Undefined && (int)tech < (int)minimumTechLevel)
                    return false;
                if (maximumTechLevel != TechLevel.Undefined && (int)tech > (int)maximumTechLevel)
                    return false;
            }
            if (DiscoveryCompat.IsActive)
            {
                if (discoveryRequirementThing != null && !DiscoveryCompat.IsDiscovered(discoveryRequirementThing))
                    return false;
                if (discoveryRequirementFaction != null && !DiscoveryCompat.IsDiscovered(discoveryRequirementFaction))
                    return false;
                if (discoveryRequirementXenotype != null && !DiscoveryCompat.IsDiscovered(discoveryRequirementXenotype))
                    return false;
            }
            return true;
        }

        public bool PassesRecipientFilter(Pawn pawn)
        {
            if (invalidNonViolent && pawn.WorkTagIsDisabled(WorkTags.Violent))
                return false;
            if (invalidTraits != null)
            {
                for (int i = 0; i < invalidTraits.Count; i++)
                    if (invalidTraits[i].HasTrait(pawn))
                        return false;
            }
            if (requiredTraits != null && requiredTraits.Count > 0)
            {
                if (requiredTraitsAny)
                {
                    var any = false;
                    for (int i = 0; i < requiredTraits.Count; i++)
                        if (requiredTraits[i].HasTrait(pawn))
                        { any = true; break; }
                    if (!any)
                        return false;
                }
                else
                {
                    for (int i = 0; i < requiredTraits.Count; i++)
                        if (!requiredTraits[i].HasTrait(pawn))
                            return false;
                }
            }
            if (invalidHediffs != null)
            {
                for (int i = 0; i < invalidHediffs.Count; i++)
                    if (pawn.health.hediffSet.HasHediff(invalidHediffs[i]))
                        return false;
            }
            if (requiredHediffs != null && requiredHediffs.Count > 0)
            {
                if (requiredHediffsAny)
                {
                    var any = false;
                    for (int i = 0; i < requiredHediffs.Count; i++)
                        if (pawn.health.hediffSet.HasHediff(requiredHediffs[i]))
                        { any = true; break; }
                    if (!any)
                        return false;
                }
                else
                {
                    for (int i = 0; i < requiredHediffs.Count; i++)
                        if (!pawn.health.hediffSet.HasHediff(requiredHediffs[i]))
                            return false;
                }
            }
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (invalidGenes != null)
                {
                    for (int i = 0; i < invalidGenes.Count; i++)
                        if (pawn.genes.HasActiveGene(invalidGenes[i]))
                            return false;
                }
                if (requiredGenes != null && requiredGenes.Count > 0)
                {
                    if (requiredGenesAny)
                    {
                        var any = false;
                        for (int i = 0; i < requiredGenes.Count; i++)
                            if (pawn.genes.HasActiveGene(requiredGenes[i]))
                            { any = true; break; }
                        if (!any)
                            return false;
                    }
                    else
                    {
                        for (int i = 0; i < requiredGenes.Count; i++)
                            if (!pawn.genes.HasActiveGene(requiredGenes[i]))
                                return false;
                    }
                }
                if (invalidXenotypes != null && invalidXenotypes.Contains(pawn.genes.Xenotype))
                    return false;
                if (requiredXenotypes != null && !requiredXenotypes.Contains(pawn.genes.Xenotype))
                    return false;
            }

            if (requiredRelations != null)
            {
                var found = pawn.relations.RelatedPawns.Any(other =>
                    !other.Dead &&
                    (requiredRelationGender is null || other.gender == requiredRelationGender) &&
                    pawn.GetRelations(other).Any(r => requiredRelations.Contains(r)));
                if (!found)
                    return false;
            }

            return true;
        }
    }
}
