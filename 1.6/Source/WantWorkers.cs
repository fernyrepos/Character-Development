using System.Linq;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public class WantWorker_Thought : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            return pawn.needs?.mood?.thoughts?.memories?.GetFirstMemoryOfDef(def.completedByThought) != null;
        }
    }

    public class WantWorker_FreedFromSlavery : WantWorker_Thought
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return pawn.IsSlaveOfColony && base.CanGenerate(pawn);
        }

        public override bool IsSatisfied(Pawn pawn)
        {
            return !pawn.IsSlaveOfColony || base.IsSatisfied(pawn);
        }
    }

    public class WantWorker_RoomStat : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return pawn.ownership.OwnedRoom != null && base.CanGenerate(pawn);
        }

        public override bool IsSatisfied(Pawn pawn)
        {
            var room = pawn.ownership.OwnedRoom;
            return room != null && room.GetStat(def.roomStat) >= def.roomStatThreshold;
        }
    }

    public class WantWorker_PrettierRoom : WantWorker_RoomStat
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return pawn.ownership.OwnedBed != null && !IsSatisfied(pawn);
        }
    }

    public class WantWorker_SeeAurora : WantWorker
    {
        private bool IsAuroraActive(Pawn pawn) => pawn.Spawned && pawn.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.Aurora);

        public override bool IsSatisfied(Pawn pawn)
        {
            return IsAuroraActive(pawn) && pawn.Awake() && !pawn.Position.Roofed(pawn.Map);
        }

        public override bool CanGenerate(Pawn pawn)
        {
            return !IsAuroraActive(pawn);
        }
    }

    public class WantWorker_Bionic : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn) => pawn.health?.hediffSet?.CountAddedAndImplantedParts() >= def.countThreshold;
    }

    public class WantWorker_GetMarried : WantWorker_Thought
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return pawn.GetFirstSpouse() == null && pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance) != null;
        }
    }

    public class WantWorker_EquipWeapon : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => !pawn.WorkTagIsDisabled(WorkTags.Violent);

        public override bool IsCompleted(Pawn pawn, WantWorkerContext context) => context.triggerType == WantTriggerType.WeaponEquipped;
    }

    public class WantWorker_TakeDrug : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context) => context.triggerType == WantTriggerType.DrugIngested;
    }

    public class WantWorker_NewOutfit : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context) => context.triggerType == WantTriggerType.ApparelAdded;
    }

    public class WantWorker_Resurrection : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context) => context.triggerType == WantTriggerType.Resurrected;
    }

    public abstract class WantWorker_BondBase : WantWorker
    {
        public static bool IsBondableAnimal(ThingDef def)
        {
            return def.race != null && !def.IsCorpse && def.race.Animal && def.race.trainability != null && def.race.trainability.intelligenceOrder >= TrainabilityDefOf.Intermediate.intelligenceOrder;
        }

        protected bool IsBondedTo(Pawn pawn, Pawn animal)
        {
            return !animal.Dead && pawn.relations.DirectRelationExists(PawnRelationDefOf.Bond, animal);
        }

        protected bool IsBondedTo(Pawn pawn, ThingDef animalDef)
        {
            var relations = pawn.relations.DirectRelations;
            for (int i = 0; i < relations.Count; i++)
            {
                var rel = relations[i];
                if (rel.def == PawnRelationDefOf.Bond && !rel.otherPawn.Dead && rel.otherPawn.def == animalDef)
                {
                    return true;
                }
            }
            return false;
        }

        protected bool HasAnyAnimalBond(Pawn pawn)
        {
            var relations = pawn.relations.DirectRelations;
            for (int i = 0; i < relations.Count; i++)
            {
                var rel = relations[i];
                if (rel.def == PawnRelationDefOf.Bond && !rel.otherPawn.Dead && rel.otherPawn.RaceProps.Animal)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class WantWorker_BondWithAnimal : WantWorker_BondBase
    {
        public override bool IsSatisfied(Pawn pawn) => HasAnyAnimalBond(pawn);

        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            if (context.triggerType == WantTriggerType.BondedWithAnimal)
            {
                if (context.contextPawn is Pawn animal)
                {
                    return IsBondedTo(pawn, animal);
                }
                return IsSatisfied(pawn);
            }
            return IsSatisfied(pawn);
        }
    }

    public class WantWorker_BecomePsycaster : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            return pawn.HasPsylink;
        }
    }

    public class WantWorker_BecomeParent : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            return pawn.relations.ChildrenCount > 0;
        }
    }

    public class WantWorker_Propose : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover, x => !x.Dead) != null && pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance) == null && pawn.GetFirstSpouse() == null && base.CanGenerate(pawn);
        }

        public override bool IsSatisfied(Pawn pawn)
        {
            return pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance) != null || pawn.GetFirstSpouse() != null;
        }

        public override bool IsValid(Pawn pawn)
        {
            return pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover, x => !x.Dead) != null || pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance, x => !x.Dead) != null || pawn.GetFirstSpouse() != null;
        }
    }

    public class WantWorker_ColonyWealth : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            return pawn.MapHeld != null && pawn.MapHeld.wealthWatcher.WealthTotal >= def.wealthThreshold;
        }
    }

    public class WantWorker_BecomeNoble : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn) => pawn.royalty.HasAnyTitleIn(Faction.OfEmpire);
    }

    public class WantWorker_BecomeIdeologicalFigure : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn) => pawn.Ideo?.GetRole(pawn) != null;
    }

    public class WantWorker_BecomeLeader : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn) => pawn.Ideo?.GetRole(pawn)?.def == PreceptDefOf.IdeoRole_Leader;
    }

    public class WantWorker_HasHediff : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            var hd = pawn.health.hediffSet.GetFirstHediffOfDef(def.targetHediff);
            return hd != null && hd.Severity >= def.targetHediffSeverity;
        }
    }

    public class WantWorker_CureHediff : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn) => !pawn.health.hediffSet.HasHediff(def.targetHediff);
    }

    public class WantWorker_ImproveComposure : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return base.CanGenerate(pawn) && TherapyCompat.HasComposure(pawn, out _);
        }
        
        public override bool IsSatisfied(Pawn pawn) => TherapyCompat.HasHighComposure(pawn);
    }

    public class WantWorker_ResolveTraumaticTrait : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn) => !TherapyCompat.HasTraumaticTrait(pawn);
    }

    public class WantWorker_ThoughtAny : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            if (def.targetThoughts.NullOrEmpty() || pawn.needs?.mood?.thoughts?.memories == null)
                return false;
            foreach (var t in def.targetThoughts)
            {
                if (pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(t) != null)
                    return true;
            }
            return false;
        }
    }

    public class WantWorker_HasTrait : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            if (!base.CanGenerate(pawn))
                return false;
            for (int i = 0; i < def.targetTraits.Count; i++)
            {
                var trait = def.targetTraits[i];
                if (ProgressionEducationCompat.Active && ProgressionEducationCompat.IsProficiencyTrait(trait) && !ProgressionEducationCompat.IsProficiencyTraitEnabled(trait))
                    return false;
            }
            return true;
        }

        public override bool IsSatisfied(Pawn pawn)
        {
            if (def.targetTraits.NullOrEmpty() || pawn.story?.traits == null)
                return false;
            foreach (var t in def.targetTraits)
            {
                if (pawn.story.traits.HasTrait(t))
                    return true;
            }
            return false;
        }
    }

    public class WantWorker_Bleeding : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn) => pawn.health?.hediffSet?.BleedRateTotal >= def.targetHediffSeverity;
    }

    public class WantWorker_EquipQuality : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            if (pawn.equipment?.Primary != null && pawn.equipment.Primary.TryGetQuality(out var q1) && q1 >= def.targetQuality)
                return true;
            if (pawn.apparel?.WornApparel != null)
            {
                var worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    if (worn[i].TryGetQuality(out var q2) && q2 >= def.targetQuality)
                        return true;
                }
            }
            return false;
        }
    }

    public class WantWorker_OpinionCount : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            var count = 0;
            var pawns = pawn.MapHeld?.mapPawns?.FreeColonistsSpawned;
            if (pawns == null)
                return false;
            pawns.AddRange(pawn.relations.RelatedPawns);
            pawns = pawns.Where(x => x.RaceProps.Humanlike).Distinct().ToList();
            for (int i = 0; i < pawns.Count; i++)
            {
                var other = pawns[i];
                if (other != pawn && other.relations != null)
                {
                    if (def.opinionThreshold > 0 && other.relations.OpinionOf(pawn) >= def.opinionThreshold)
                        count++;
                    else if (def.opinionThreshold < 0 && other.relations.OpinionOf(pawn) <= def.opinionThreshold)
                        count++;
                }
            }
            return count >= def.countThreshold;
        }
    }

    public class WantWorker_BeautifulLover : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            var partners = LovePartnerRelationUtility.ExistingLovePartners(pawn, allowDead: false);
            for (int i = 0; i < partners.Count; i++)
            {
                if (partners[i].otherPawn.GetStatValue(StatDefOf.PawnBeauty) >= 1f)
                    return true;
            }
            return false;
        }
    }

    public class WantWorker_Inspired : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn) => pawn.InspirationDef != null;
    }

    public class WantWorker_BecomeGrandparent : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => pawn.relations != null && pawn.relations.ChildrenCount > 0 && !IsSatisfied(pawn);
        public override bool IsSatisfied(Pawn pawn)
        {
            if (pawn.relations == null)
                return false;
            foreach (var child in pawn.relations.Children)
            {
                if (child.relations != null && child.relations.ChildrenCount > 0)
                    return true;
            }
            return false;
        }
    }

    public class WantWorker_Record : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn) => pawn.records?.GetValue(def.targetRecord) >= def.countThreshold;
    }

    public class WantWorker_DiscoverAnimal : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTarget(pawn) != null;
        public override Def GetRandomTarget(Pawn pawn)
        {
            var undiscovered = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.race != null && d.IsCorpse is false && d.race.Animal && !DiscoveryCompat.IsDiscovered(d));
            return undiscovered.TryRandomElement(out var result) ? result : null;
        }
        public override bool IsTargetDiscovered(Def target) => target is ThingDef animal && DiscoveryCompat.IsDiscovered(animal);
    }

    public class WantWorker_DiscoverFaction : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTarget(pawn) != null;
        public override Def GetRandomTarget(Pawn pawn)
        {
            var meetable = Find.WorldObjects.Settlements.Where(s => s.Faction != null).Select(s => s.Faction.def).ToHashSet();
            var undiscovered = DefDatabase<FactionDef>.AllDefsListForReading.Where(d => !d.isPlayer && d.hidden is false && meetable.Contains(d) && !DiscoveryCompat.IsDiscovered(d));
            return undiscovered.TryRandomElement(out var result) ? result : null;
        }
        public override bool IsTargetDiscovered(Def target) => target is FactionDef faction && DiscoveryCompat.IsDiscovered(faction);
    }

    public class WantWorker_DiscoverXenotype : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTarget(pawn) != null;
        public override Def GetRandomTarget(Pawn pawn)
        {
            var undiscovered = DefDatabase<XenotypeDef>.AllDefsListForReading.Where(d => !DiscoveryCompat.IsDiscovered(d));
            return undiscovered.TryRandomElement(out var result) ? result : null;
        }
        public override bool IsTargetDiscovered(Def target) => target is XenotypeDef xeno && DiscoveryCompat.IsDiscovered(xeno);
    }

    public class WantWorker_DiscoverBuilding : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTarget(pawn) != null;
        public override Def GetRandomTarget(Pawn pawn)
        {
            var undiscovered = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.building != null && d.IsFrame is false && d.IsBlueprint is false && !DiscoveryCompat.IsDiscovered(d));
            return undiscovered.TryRandomElement(out var result) ? result : null;
        }
        public override bool IsTargetDiscovered(Def target) => target is ThingDef building && DiscoveryCompat.IsDiscovered(building);
    }

    public class WantWorker_EatFood : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTarget(pawn) != null && base.CanGenerate(pawn);
        public override Def GetRandomTarget(Pawn pawn)
        {
            var foods = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.IsIngestible && d.ingestible.preferability >= FoodPreferability.MealSimple);
            return foods.TryRandomElement(out var result) ? result : null;
        }
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.FoodEaten;
        }
    }

    public class WantWorker_SkillBase : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return pawn.skills.skills.Any(s => s.TotallyDisabled is false && s.Level < 20) && GetRandomTarget(pawn) != null && base.CanGenerate(pawn);
        }

        public override Def GetRandomTarget(Pawn pawn)
        {
            var skills = pawn.skills.skills.Where(s => s.TotallyDisabled is false && s.Level < 20).Select(s => s.def);
            return skills.TryRandomElement(out var result) ? result : null;
        }
    }

    public class WantWorker_SkillLevel : WantWorker_SkillBase
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            if (def.targetSkill != null)
            {
                return pawn.skills.GetSkill(def.targetSkill).Level >= def.skillLevelThreshold;
            }
            return false;
        }
        public override bool IsSatisfiedWithTarget(Pawn pawn, Def targetDef)
        {
            if (targetDef is SkillDef skill)
            {
                return pawn.skills.GetSkill(skill).Level >= def.skillLevelThreshold;
            }
            return false;
        }
        public override Def GetRandomTarget(Pawn pawn)
        {
            var skills = pawn.skills.skills.Where(s => !s.TotallyDisabled && s.Level < def.skillLevelThreshold).Select(s => s.def);
            return skills.TryRandomElement(out var result) ? result : null;
        }
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            if (context.triggerType == WantTriggerType.SkillIncreased)
            {
                return context.contextAmount >= def.skillLevelThreshold;
            }
            return IsSatisfied(pawn);
        }
    }

    public class WantWorker_GetLovinWith : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTargetPawn(pawn) != null && base.CanGenerate(pawn);
        public override Pawn GetRandomTargetPawn(Pawn pawn)
        {
            var partners = LovePartnerRelationUtility.ExistingLovePartners(pawn, allowDead: false);
            return partners.TryRandomElement(out var result) ? result.otherPawn : null;
        }
        public override bool IsSatisfiedWithPawnTarget(Pawn pawn, Pawn targetPawn)
        {
            var thought = pawn.needs?.mood?.thoughts?.memories?.GetFirstMemoryOfDef(ThoughtDefOf.GotSomeLovin);
            return thought != null && thought.otherPawn == targetPawn;
        }
        public override bool IsValidWithPawnTarget(Pawn pawn, Pawn targetPawn)
        {
            return targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed;
        }
    }

    public class WantWorker_BeFriendsWith : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTargetPawn(pawn) != null && base.CanGenerate(pawn);
        public override Pawn GetRandomTargetPawn(Pawn pawn)
        {
            if (pawn.MapHeld == null)
                return null;
            var potential = pawn.MapHeld.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Humanlike && p != pawn && !pawn.relations.RelatedPawns.Contains(p) && pawn.relations.OpinionOf(p) < 50);
            return potential.TryRandomElement(out var result) ? result : null;
        }
        public override bool IsSatisfiedWithPawnTarget(Pawn pawn, Pawn targetPawn)
        {
            return pawn.relations.OpinionOf(targetPawn) >= 50;
        }
    }

    public class WantWorker_MarryLover : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTargetPawn(pawn) != null && base.CanGenerate(pawn);
        public override Pawn GetRandomTargetPawn(Pawn pawn)
        {
            var fiance = pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance, x => !x.Dead);
            if (fiance != null)
                return fiance;
            var lover = pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover, x => !x.Dead);
            return lover;
        }
        public override bool IsSatisfiedWithPawnTarget(Pawn pawn, Pawn targetPawn)
        {
            return pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, targetPawn);
        }
        public override bool IsValidWithPawnTarget(Pawn pawn, Pawn targetPawn)
        {
            return targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed;
        }
    }

    public class WantWorker_MakeQuality : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.RecipeCompleted && (QualityCategory)context.contextAmount >= def.targetQuality;
        }
    }

    public class WantWorker_Trade : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return !pawn.WorkTagIsDisabled(WorkTags.Social) && base.CanGenerate(pawn);
        }
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.Traded;
        }
    }

    public class WantWorker_HostDinnerParty : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context) => context.triggerType == WantTriggerType.HostedParty;
    }

    public class WantWorker_SeeNewPlace : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context) => context.triggerType == WantTriggerType.SawNewPlace;
    }

    public class WantWorker_NewSettlement : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context) => context.triggerType == WantTriggerType.NewSettlement;
    }

    public class WantWorker_TakeSpecificDrug : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTarget(pawn) != null && base.CanGenerate(pawn);
        public override Def GetRandomTarget(Pawn pawn)
        {
            var drugs = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.IsDrug);
            return drugs.TryRandomElement(out var result) ? result : null;
        }

        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.DrugIngested;
        }
    }

    public class WantWorker_FallInLoveWithXenotype : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTarget(pawn) != null && base.CanGenerate(pawn);
        public override Def GetRandomTarget(Pawn pawn)
        {
            var xenos = DefDatabase<XenotypeDef>.AllDefsListForReading;
            return xenos.TryRandomElement(out var result) ? result : null;
        }

        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.FellInLove;
        }
    }

    public class WantWorker_LeaveFaction : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context) => context.triggerType == WantTriggerType.LeftFaction;
    }

    public class WantWorker_Die : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context) => context.triggerType == WantTriggerType.Died;
    }

    public class WantWorker_NewEntertainmentBuilding : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTarget(pawn) != null;
        public override Def GetRandomTarget(Pawn pawn)
        {
            var existingJoyKinds = pawn.MapHeld?.listerBuildings.allBuildingsColonist
                .Where(b => b.def.building?.joyKind != null)
                .Select(b => b.def.building.joyKind).ToHashSet();

            var allJoyKinds = DefDatabase<JoyKindDef>.AllDefsListForReading;
            var missing = allJoyKinds.Where(j => existingJoyKinds == null || !existingJoyKinds.Contains(j));
            return missing.Where(x => DefDatabase<ThingDef>.AllDefs.Any(b => b.building?.joyKind == x)).TryRandomElement(out var result) ? result : null;
        }
        public override bool IsSatisfiedWithTarget(Pawn pawn, Def targetDef)
        {
            if (targetDef is JoyKindDef joyKind && pawn.MapHeld != null)
            {
                return pawn.MapHeld.listerBuildings.allBuildingsColonist.Any(b => b.def.building?.joyKind == joyKind);
            }
            return false;
        }
    }

    public class WantWorker_SeeSpace : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn) => pawn.MapHeld?.Biome == BiomeDefOf.Space;
    }

    public class WantWorker_BecomeTempered : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            return TherapyCompat.IsTempered(pawn);
        }
    }

    public class WantWorker_Ritual : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            if (context.triggerType == WantTriggerType.RitualCompleted && context.contextDef == def.targetRitual)
            {
                if (def.targetRitualRole.NullOrEmpty() || context.contextString == def.targetRitualRole)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class WantWorker_ImproveSkill : WantWorker_SkillBase
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.SkillIncreased;
        }
    }

    public class WantWorker_ResolveTraumaticPassion : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            return !TherapyCompat.HasTraumaticPassion(pawn);
        }
    }

    public class WantWorker_BecomeXenotype : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTarget(pawn) != null && base.CanGenerate(pawn);
        public override Def GetRandomTarget(Pawn pawn)
        {
            var discovered = DefDatabase<XenotypeDef>.AllDefsListForReading.Where(d => DiscoveryCompat.IsDiscovered(d) && pawn.genes?.Xenotype != d);
            return discovered.TryRandomElement(out var result) ? result : null;
        }

        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.XenotypeChanged;
        }

        public override bool IsSatisfiedWithTarget(Pawn pawn, Def targetDef)
        {
            return pawn.genes?.Xenotype == targetDef as XenotypeDef;
        }
    }

    public class WantWorker_BondWithDiscoveredAnimal : WantWorker_BondBase
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTarget(pawn) != null && base.CanGenerate(pawn);
        public override Def GetRandomTarget(Pawn pawn)
        {
            var discoveredAnimals = DefDatabase<ThingDef>.AllDefsListForReading.Where(d =>
                IsBondableAnimal(d) &&
                DiscoveryCompat.IsDiscovered(d) &&
                !IsSatisfiedWithTarget(pawn, d));
            return discoveredAnimals.TryRandomElement(out var result) ? result : null;
        }

        public override bool IsSatisfiedWithTarget(Pawn pawn, Def targetDef)
        {
            if (targetDef is ThingDef animalDef)
            {
                return IsBondedTo(pawn, animalDef);
            }
            return false;
        }

        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            if (context.triggerType == WantTriggerType.BondedWithAnimal)
            {
                if (context.contextPawn is Pawn animal)
                {
                    return IsBondedTo(pawn, animal);
                }
                if (context.contextDef is ThingDef animalDef)
                {
                    return IsSatisfiedWithTarget(pawn, animalDef);
                }
                return true;
            }
            return false;
        }
    }

    public class WantWorker_DeadRival : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => GetRandomTargetPawn(pawn) != null && base.CanGenerate(pawn);

        public override Pawn GetRandomTargetPawn(Pawn pawn)
        {
            if (pawn.MapHeld == null)
                return null;
            var potential = pawn.MapHeld.mapPawns.AllPawnsSpawned.Where(p => p != pawn && p.RaceProps.Humanlike && !p.Dead && pawn.relations.OpinionOf(p) <= -20 && !pawn.relations.RelatedPawns.Contains(p));
            return potential.TryRandomElement(out var result) ? result : null;
        }

        public override bool IsSatisfiedWithPawnTarget(Pawn pawn, Pawn targetPawn)
        {
            return targetPawn == null || targetPawn.Dead || targetPawn.Destroyed;
        }
        public override bool IsValidWithPawnTarget(Pawn pawn, Pawn targetPawn)
        {
            return targetPawn == null || targetPawn.Dead || targetPawn.Destroyed || pawn.relations.OpinionOf(targetPawn) <= -20;
        }
    }

    public class WantWorker_LoseWeight : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return RimBodyCompat.GetBodyFat(pawn) > 20f && base.CanGenerate(pawn);
        }

        public override bool IsSatisfied(Pawn pawn)
        {
            return RimBodyCompat.GetBodyFat(pawn) <= 15f;
        }
    }

    public class WantWorker_GainMuscle : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return RimBodyCompat.GetMuscleMass(pawn) < 20f && base.CanGenerate(pawn);
        }

        public override bool IsSatisfied(Pawn pawn)
        {
            return RimBodyCompat.GetMuscleMass(pawn) >= 25f;
        }
    }

    public class WantWorker_DriveVehicle : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.BoardedVehicle;
        }
    }

    public class WantWorker_AdvanceEra : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context) => context.triggerType == WantTriggerType.AdvancedEra;
    }

    public class WantWorker_EatDessert : WantWorker
    {
        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.FoodEaten && context.contextDef is ThingDef thingDef && thingDef.ingestible?.joyKind == DefsOf.VCE_Confectionery;
        }
    }

    public class WantWorker_JoinDeserters : WantWorker
    {
        public override bool CanGenerate(Pawn pawn) => !DesertersCompat.IsDesertersActive() && base.CanGenerate(pawn);
        public override bool IsSatisfied(Pawn pawn) => DesertersCompat.IsDesertersActive();
    }

    public class WantWorker_UseDeclassifier : WantWorker_ThoughtAny
    {
        public override bool CanGenerate(Pawn pawn) => DesertersCompat.IsDesertersActive() && base.CanGenerate(pawn);
    }

    public class WantWorker_TameAnimal : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return !pawn.WorkTagIsDisabled(WorkTags.Animals) && GetRandomTarget(pawn) != null && base.CanGenerate(pawn);
        }

        public override Def GetRandomTarget(Pawn pawn)
        {
            var tameableAnimals = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.race != null && d.race.Animal && d.IsCorpse is false && d.GetStatValueAbstract(StatDefOf.Wildness) < 1f && (!DiscoveryCompat.IsActive || DiscoveryCompat.IsDiscovered(d)));
            return tameableAnimals.TryRandomElement(out var result) ? result : null;
        }

        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.AnimalTamed;
        }
    }

    public class WantWorker_BuildBuilding : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            return pawn.skills.GetSkill(SkillDefOf.Construction).Level >= 5 && GetRandomTarget(pawn) != null && base.CanGenerate(pawn);
        }

        public override Def GetRandomTarget(Pawn pawn)
        {
            var buildings = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.category == ThingCategory.Building && d.BuildableByPlayer && !d.IsFrame && !d.IsBlueprint && !VEFCompat.IsHiddenDesignator(d) && d.constructionSkillPrerequisite >= 5 && pawn.skills.GetSkill(SkillDefOf.Construction).Level >= d.constructionSkillPrerequisite && (d.researchPrerequisites.NullOrEmpty() || d.researchPrerequisites.All(r => r.IsFinished)));
            return buildings.TryRandomElement(out var result) ? result : null;
        }

        public override bool IsCompleted(Pawn pawn, WantWorkerContext context)
        {
            return context.triggerType == WantTriggerType.BuildingConstructed;
        }
    }
    
    public class WantWorker_DoubleBed : WantWorker
    {
        public override bool CanGenerate(Pawn pawn)
        {
            var bed = pawn.ownership.OwnedBed;
            return bed != null
                && bed.SleepingSlotsCount < 2
                && pawn.GetFirstSpouse() == null
                && pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover, x => !x.Dead) == null
                && pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance, x => !x.Dead) == null
                && base.CanGenerate(pawn);
        }

        public override bool IsSatisfied(Pawn pawn)
        {
            var bed = pawn.ownership.OwnedBed;
            return bed != null && bed.SleepingSlotsCount >= 2;
        }
    }

    public class WantWorker_Imprisoned : WantWorker
    {
        public override bool IsSatisfied(Pawn pawn)
        {
            return pawn.IsPrisoner;
        }
    }
}
