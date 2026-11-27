using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WantsAndQuirks
{
    public class WantDef : WQ_BaseDef
    {
        public Type workerClass = typeof(WantWorker);
        public string iconPath;
        public ThingDef iconThingPath;
        public bool preferIconPath;
        public int reward = 1000;
        public float commonality = 1f;
        public ThoughtDef completedByThought;
        public RoomStatDef roomStat;
        public float roomStatThreshold;
        public float wealthThreshold;
        public string fulfilledText;
        public HediffDef targetHediff;
        public float targetHediffSeverity;
        public int countThreshold = 1;
        public List<ThoughtDef> targetThoughts;
        public List<TraitDef> targetTraits;
        public QualityCategory targetQuality;
        public int opinionThreshold;
        public RecordDef targetRecord;
        public SkillDef targetSkill;
        public int skillLevelThreshold;
        public JoyKindDef joyKind;
        public PreceptDef targetRitual;
        public string targetRitualRole;
        public bool isMentalBreakWant;
        public int minimumColonists;

        [Unsaved(false)]
        private WantWorker workerInt;
        [Unsaved(false)]
        private Texture2D iconInt;

        public WantWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (WantWorker)Activator.CreateInstance(workerClass);
                    workerInt.def = this;
                }
                return workerInt;
            }
        }

        public Texture2D Icon => iconThingPath != null ? iconThingPath.uiIcon : (iconInt ??= ContentFinder<Texture2D>.Get(iconPath));
    }
}
