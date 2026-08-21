using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WantsAndQuirks
{
    public enum WantTriggerType
    {
        None,
        ApparelAdded,
        WeaponEquipped,
        DrugIngested,
        Resurrected,
        BondedWithAnimal,
        FoodEaten,
        Traded,
        RecipeCompleted,
        SkillIncreased,
        XenotypeChanged,
        RitualCompleted,
        HostedParty,
        SawNewPlace,
        NewSettlement,
        FellInLove,
        LeftFaction,
        Died,
        AdvancedEra,
        BoardedVehicle,
        AnimalTamed,
        BuildingConstructed
    }

    public struct WantWorkerContext
    {
        public WantTriggerType triggerType;
        public Def contextDef;
        public Pawn contextPawn;
        public int contextAmount;
        public string contextString;

        public WantWorkerContext(WantTriggerType triggerType, Def contextDef = null, Pawn contextPawn = null, int contextAmount = 0, string contextString = null)
        {
            this.triggerType = triggerType;
            this.contextDef = contextDef;
            this.contextPawn = contextPawn;
            this.contextAmount = contextAmount;
            this.contextString = contextString;
        }
    }

    public static class WantsAndQuirksUtility
    {
        private static readonly List<Thought> tmpThoughts = new List<Thought>();
        private static readonly ConditionalWeakTable<Pawn, PawnWantsData> pawnData = new ConditionalWeakTable<Pawn, PawnWantsData>();
        public static readonly ConditionalWeakTable<Message, Pawn> wantMessages = new ConditionalWeakTable<Message, Pawn>();
        public static PawnWantsData GetWantsData(this Pawn pawn)
        {
            if (!pawnData.TryGetValue(pawn, out var data))
            {
                data = new PawnWantsData();
                pawnData.Add(pawn, data);
            }
            return data;
        }

        public static bool TryGetWantsData(this Pawn pawn, out PawnWantsData data)
        {
            return pawnData.TryGetValue(pawn, out data);
        }

        public static bool CanHaveWants(this Pawn pawn)
        {
            if (!WantsAndQuirksMod.settings.enableWantsSystem || pawn.DestroyedOrNull() || !pawn.RaceProps.Humanlike || (!pawn.IsColonist && !pawn.IsSlaveOfColony))
                return false;

            if (pawn.DevelopmentalStage == DevelopmentalStage.Baby || pawn.DevelopmentalStage == DevelopmentalStage.Newborn)
                return false;

            if (AndroidsCompat.IsUnawakenedAndroid(pawn))
                return false;

            return true;
        }

        public static void CheckWants(Pawn pawn, WantWorkerContext context)
        {
            var data = pawn.GetWantsData();
            for (int i = data.activeWants.Count - 1; i >= 0; i--)
            {
                var want = data.activeWants[i];
                if (want.IsCompleted(pawn, context))
                {
                    CompleteWant(pawn, data, want);
                }
            }
        }

        public static bool HasTraumaticDesire(this Pawn pawn)
        {
            if (!pawn.TryGetWantsData(out var data))
                return false;
            for (int i = 0; i < data.activeWants.Count; i++)
            {
                if (data.activeWants[i].def.isMentalBreakWant)
                    return true;
            }
            return false;
        }

        public static List<string> GetTraumaticDesireDefNames(this Pawn pawn)
        {
            var result = new List<string>();
            if (!pawn.TryGetWantsData(out var data))
                return result;
            for (int i = 0; i < data.activeWants.Count; i++)
            {
                if (data.activeWants[i].def.isMentalBreakWant)
                    result.Add(data.activeWants[i].def.defName);
            }
            return result;
        }

        public static void ResolveTraumaticDesire(this Pawn pawn, string defName)
        {
            if (!pawn.TryGetWantsData(out var data))
                return;
            for (int i = data.activeWants.Count - 1; i >= 0; i--)
            {
                if (data.activeWants[i].def.isMentalBreakWant && data.activeWants[i].def.defName == defName)
                {
                    data.activeWants.RemoveAt(i);
                    return;
                }
            }
        }

        public static void AddCharacterPoints(Pawn pawn, int amount)
        {
            State.characterPoints = Mathf.Max(State.characterPoints + amount, 0);
            while (State.characterPoints >= WantsAndQuirksMod.settings.pointsNeededForReward)
            {
                State.characterPoints -= WantsAndQuirksMod.settings.pointsNeededForReward;
                State.rewardPoints++;
                if (pawn != null)
                {
                    Messages.Message("WQ_RewardPointEarned".Translate(pawn.Named("PAWN")), null, MessageTypeDefOf.PositiveEvent, false);
                }
                else
                {
                    Messages.Message("WQ_RewardPointEarnedDebug".Translate(), null, MessageTypeDefOf.PositiveEvent, false);
                }
            }
        }

        public static void CompleteWant(Pawn pawn, PawnWantsData data, ActiveWant want)
        {
            AddCharacterPoints(pawn, want.def.reward);
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                var text = !string.IsNullOrEmpty(want.def.fulfilledText) ? want.def.fulfilledText.Formatted(pawn.Named("PAWN"), want.LabelCap) : "WQ_WantCompleted".Translate(pawn.Named("PAWN"), want.LabelCap);
                var msg = new Message(text, MessageTypeDefOf.SilentInput, pawn);
                wantMessages.Add(msg, pawn);
                Messages.Message(msg);
                DefsOf.WQ_WantCompleted.PlayOneShotOnCamera();
            }
            data.activeWants.Remove(want);
        }

        public static void GenerateGlobalRewardBubbles()
        {
            var list = new List<RewardNode>();
            var total = WantsAndQuirksMod.settings.bubblesPerRoll;
            var legendaryCount = 0;
            var rareCount = 0;
            var uncommonCount = 0;
            var commonCount = 0;

            for (int i = 0; i < total; i++)
            {
                var roll = Rand.Value;
                if (roll < 0.05f)
                {
                    legendaryCount++;
                }
                else if (roll < 0.20f)
                {
                    rareCount++;
                }
                else if (roll < 0.50f)
                {
                    uncommonCount++;
                }
                else
                {
                    commonCount++;
                }
            }

            FillRewardSlots(list, RewardRarity.Legendary, ref legendaryCount);
            FillRewardSlots(list, RewardRarity.Rare, ref rareCount);
            FillRewardSlots(list, RewardRarity.Uncommon, ref uncommonCount);
            FillRewardSlots(list, RewardRarity.Common, ref commonCount);

            while (list.Count < total)
            {
                var node = GenerateNodeForRarity(list, RewardRarity.Common) ??
                            GenerateNodeForRarity(list, RewardRarity.Uncommon) ??
                            GenerateNodeForRarity(list, RewardRarity.Rare) ??
                            GenerateNodeForRarity(list, RewardRarity.Legendary);
                if (node == null)
                    break;
                list.Add(node);
            }

            State.rewardNodes = list;
        }

        private static void FillRewardSlots(List<RewardNode> list, RewardRarity rarity, ref int count)
        {
            var limit = count;
            for (int i = 0; i < limit; i++)
            {
                var node = GenerateNodeForRarity(list, rarity);
                if (node != null)
                {
                    list.Add(node);
                }
                else
                {
                    count--;
                }
            }
        }

        public static RewardNode GenerateSingleRewardBubble(List<RewardNode> existingNodes)
        {
            var roll = Rand.Value;
            var rarity = RewardRarity.Common;
            if (roll < 0.05f)
            {
                rarity = RewardRarity.Legendary;
            }
            else if (roll < 0.20f)
            {
                rarity = RewardRarity.Rare;
            }
            else if (roll < 0.50f)
            {
                rarity = RewardRarity.Uncommon;
            }

            return GenerateNodeForRarity(existingNodes, rarity) ??
            GenerateNodeForRarity(existingNodes, RewardRarity.Common) ??
            GenerateNodeForRarity(existingNodes, RewardRarity.Uncommon) ??
            GenerateNodeForRarity(existingNodes, RewardRarity.Rare) ??
            GenerateNodeForRarity(existingNodes, RewardRarity.Legendary);
        }

        private static RewardNode GenerateNodeForRarity(List<RewardNode> existingNodes, RewardRarity rarity)
        {
            var map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map == null)
                return null;

            var validDefs = DefDatabase<RewardDef>.AllDefsListForReading.Where(rDef =>
            {
                if (rDef.rarity != rarity)
                    return false;
                if (!rDef.Worker.CanGenerate())
                    return false;
                if (rDef.requiresItem)
                {
                    var items = rDef.Worker.GetValidItems(map);
                    return items.Any(item => !existingNodes.Any(n => n.def == rDef && n.item == item));
                }
                return !existingNodes.Any(n => n.def == rDef);
            }).ToList();

            if (validDefs.TryRandomElement(out var chosenDef))
            {
                ThingDef chosenItem = null;
                Pawn chosenPawn = null;
                if (chosenDef.requiresItem)
                {
                    var validItems = chosenDef.Worker.GetValidItems(map).Where(item => !existingNodes.Any(n => n.def == chosenDef && n.item == item));
                    chosenItem = validItems.RandomElement();
                }
                else if (chosenDef.requiresPawn)
                {
                    var validPawns = chosenDef.Worker.GetValidPawns(map).Where(p => !existingNodes.Any(n => n.def == chosenDef && n.pawnTarget == p));
                    if (!validPawns.TryRandomElement(out chosenPawn))
                        return null;
                }

                var node = new RewardNode
                {
                    def = chosenDef,
                    item = chosenItem,
                    pawnTarget = chosenPawn,
                    pos = new Vector2(Rand.Range(-100f, 100f), Rand.Range(-100f, 100f))
                };
                node.drawPos = node.pos;
                return node;
            }
            return null;
        }

        public static void RerollWant(Pawn pawn, PawnWantsData data, ActiveWant want)
        {
            var newWant = GenerateRandomWant(pawn, data, false);
            if (newWant == null)
                return;
            newWant.assignedTick = want.assignedTick;
            newWant.rerollCount = want.rerollCount + 1;
            data.activeWants[data.activeWants.IndexOf(want)] = newWant;
        }

        public static ActiveWant CreateWant(Pawn pawn, WantDef def, Def targetDef = null, Pawn targetPawn = null, bool sendNotification = true)
        {
            ActiveWant want;
            if (targetPawn != null)
                want = new ActiveWantWithPawnTarget
                {
                    def = def,
                    targetPawn = targetPawn,
                    assignedTick = Find.TickManager.TicksGame
                };
            else if (targetDef != null)
                want = new ActiveWantWithTarget
                {
                    def = def,
                    targetDef = targetDef,
                    assignedTick = Find.TickManager.TicksGame
                };
            else
            {
                var autoTargetPawn = def.Worker.GetRandomTargetPawn(pawn);
                if (autoTargetPawn != null)
                {
                    want = new ActiveWantWithPawnTarget
                    {
                        def = def,
                        targetPawn = autoTargetPawn,
                        assignedTick = Find.TickManager.TicksGame
                    };
                }
                else
                {
                    var autoTargetDef = def.Worker.GetRandomTarget(pawn);
                    if (autoTargetDef != null)
                    {
                        want = new ActiveWantWithTarget
                        {
                            def = def,
                            targetDef = autoTargetDef,
                            assignedTick = Find.TickManager.TicksGame
                        };
                    }
                    else
                    {
                        want = new ActiveWant
                        {
                            def = def,
                            assignedTick = Find.TickManager.TicksGame
                        };
                    }
                }
            }
            if (sendNotification && PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                var text = "WQ_NewWantGenerated".Translate(pawn.Named("PAWN"));
                var msg = new Message(text, MessageTypeDefOf.SilentInput, pawn);
                wantMessages.Add(msg, pawn);
                Messages.Message(msg);
                DefsOf.WQ_NewWantBell.PlayOneShotOnCamera();
            }
            return want;
        }

        public static ActiveWant AddWant(Pawn pawn, PawnWantsData data, WantDef def, Def targetDef = null, bool sendNotification = true)
        {
            var want = CreateWant(pawn, def, targetDef, sendNotification: sendNotification);
            data.activeWants.Add(want);
            return want;
        }

        public static ActiveWant AddWantWithPawnTarget(Pawn pawn, PawnWantsData data, WantDef def, Pawn targetPawn, bool sendNotification = true)
        {
            var want = CreateWant(pawn, def, null, targetPawn, sendNotification);
            data.activeWants.Add(want);
            return want;
        }

        public static ActiveWant GenerateRandomWant(Pawn pawn, PawnWantsData data, bool sendNotification = true)
        {
            var availableDefs = DefDatabase<WantDef>.AllDefsListForReading.Where(x => !x.isMentalBreakWant && !data.activeWants.Any(w => w.def == x) && x.Worker.CanHaveWant(pawn) && x.Worker.CanGenerate(pawn)).ToList();
            if (!WantsAndQuirksMod.settings.enableMentalBreakWants)
            {
                availableDefs.RemoveAll(d => d.isMentalBreakWant);
            }
            if (availableDefs.TryRandomElementByWeight(x => x.commonality, out var chosenDef))
            {
                var targetPawn = chosenDef.Worker.GetRandomTargetPawn(pawn);
                if (targetPawn != null)
                {
                    return CreateWant(pawn, chosenDef, null, targetPawn, sendNotification);
                }
                var targetDef = chosenDef.Worker.GetRandomTarget(pawn);
                if (targetDef != null)
                {
                    return CreateWant(pawn, chosenDef, targetDef, sendNotification: sendNotification);
                }
                return CreateWant(pawn, chosenDef, sendNotification: sendNotification);
            }
            return null;
        }

        public static void AddQuirk(Pawn pawn, RewardDef def, ThingDef item, Pawn targetPawn = null)
        {
            var data = pawn.GetWantsData();
            if (data.HasQuirk(def, item, targetPawn))
            {
                return;
            }
            var quirk = new Quirk(def, item, targetPawn);
            if (def.isQuirk)
            {
                data.quirks.Add(quirk);
            }
            def.Worker.OnAcquired(pawn, quirk);
            if (!def.claimMessage.NullOrEmpty())
            {
                Messages.Message(def.claimMessage.Formatted(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        public static void TickWants(Pawn pawn)
        {
            var data = pawn.GetWantsData();

            if (data.nextWantTick == -1)
            {
                InitializePawnWants(pawn, data);
            }

            bool wasAtCapacity = data.activeWants.Count >= WantsAndQuirksMod.settings.maxActiveWants;

            CheckWants(pawn, new WantWorkerContext(WantTriggerType.None));

            for (int i = data.activeWants.Count - 1; i >= 0; i--)
            {
                if (!data.activeWants[i].IsValid(pawn))
                {
                    data.activeWants.RemoveAt(i);
                }
            }

            bool isAtCapacity = data.activeWants.Count >= WantsAndQuirksMod.settings.maxActiveWants;
            if (wasAtCapacity && !isAtCapacity)
            {
                data.nextWantTick = Find.TickManager.TicksGame + GetNextWantInterval();
                return;
            }

            if (isAtCapacity)
            {
                data.nextWantTick = Find.TickManager.TicksGame + GetNextWantInterval();
                return;
            }

            if (Find.TickManager.TicksGame >= data.nextWantTick)
            {
                var generated = GenerateRandomWant(pawn, data);
                if (generated != null)
                {
                    data.activeWants.Add(generated);
                }
                data.nextWantTick = Find.TickManager.TicksGame + GetNextWantInterval();
            }
        }

        public static void InitializePawnWants(Pawn pawn, PawnWantsData data)
        {
            data.nextWantTick = Find.TickManager.TicksGame + GetNextWantInterval();
            for (int i = 0; i < WantsAndQuirksMod.settings.startingWantsCount; i++)
            {
                var generated = GenerateRandomWant(pawn, data, false);
                if (generated != null)
                {
                    data.activeWants.Add(generated);
                }
            }
        }

        private static int GetNextWantInterval()
        {
            var range = WantsAndQuirksMod.settings.wantGenerationFrequencyDays;
            return (int)(Rand.Range(range.min, (float)range.max) * GenDate.TicksPerDay);
        }

        public static bool IsGrantedGene(this Gene gene)
        {
            if (TryGetWantsData(gene.pawn, out var data))
            {
                for (int i = 0; i < data.grantedGenes.Count; i++)
                {
                    if (data.grantedGenes[i].gene == gene)
                        return true;
                }
            }
            return false;
        }

        public static bool HasThought(Pawn pawn, ThoughtDef thought)
        {
            if (pawn.needs?.mood?.thoughts == null)
                return false;

            if (thought.IsMemory)
            {
                return pawn.needs.mood.thoughts.memories?.GetFirstMemoryOfDef(thought) != null;
            }

            if (thought.IsSocial)
            {
                return false; // unsupported
            }

            if (thought.IsSituational)
            {
                if (pawn.needs.mood.thoughts.situational == null)
                    return false;

                tmpThoughts.Clear();
                pawn.needs.mood.thoughts.situational.AppendMoodThoughts(tmpThoughts);
                return tmpThoughts.Any(t => t.def == thought);
            }

            return false;
        }
    }
}
