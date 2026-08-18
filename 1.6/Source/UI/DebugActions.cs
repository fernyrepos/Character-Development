using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    public static class DebugActions
    {
        [DebugAction("Wants and Quirks", "Add want...", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddWant(Pawn p)
        {
            if (!p.RaceProps.Humanlike)
                return;
            var data = p.GetWantsData();
            var options = new List<DebugMenuOption>();
            foreach (var def in DefDatabase<WantDef>.AllDefs.OrderBy(d => d.label))
            {
                options.Add(new DebugMenuOption(def.LabelCap, DebugMenuOptionMode.Action, () =>
                {
                    WantsAndQuirksUtility.AddWant(p, data, def);
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("Wants and Quirks", "Remove want...", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RemoveWant(Pawn p)
        {
            if (!p.RaceProps.Humanlike)
                return;
            var data = p.GetWantsData();
            if (data.activeWants.Count == 0)
                return;

            var options = new List<DebugMenuOption>();
            foreach (var want in data.activeWants.ToList())
            {
                options.Add(new DebugMenuOption(want.def.LabelCap, DebugMenuOptionMode.Action, () =>
                {
                    data.activeWants.Remove(want);
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("Wants and Quirks", "Complete want...", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CompleteWant(Pawn p)
        {
            if (!p.RaceProps.Humanlike)
                return;
            var data = p.GetWantsData();
            if (data.activeWants.Count == 0)
                return;

            var options = new List<DebugMenuOption>();
            foreach (var want in data.activeWants.ToList())
            {
                options.Add(new DebugMenuOption(want.def.LabelCap, DebugMenuOptionMode.Action, () =>
                {
                    WantsAndQuirksUtility.CompleteWant(p, data, want);
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("Wants and Quirks", "Add quirk...", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddQuirk(Pawn p)
        {
            if (!p.RaceProps.Humanlike)
                return;
            var options = new List<DebugMenuOption>();
            foreach (var def in DefDatabase<RewardDef>.AllDefs.OrderBy(d => d.label))
            {
                options.Add(new DebugMenuOption(def.LabelCap, DebugMenuOptionMode.Action, () =>
                {
                    WantsAndQuirksUtility.AddQuirk(p, def, null);
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("Wants and Quirks", "Remove quirk...", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RemoveQuirk(Pawn p)
        {
            if (!p.RaceProps.Humanlike)
                return;
            var data = p.GetWantsData();
            if (data.quirks.Count == 0)
                return;

            var options = new List<DebugMenuOption>();
            foreach (var quirk in data.quirks.ToList())
            {
                options.Add(new DebugMenuOption(quirk.def.LabelCap, DebugMenuOptionMode.Action, () =>
                {
                    data.quirks.Remove(quirk);
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("Wants and Quirks", "Set next want tick to now", false, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetNextWantTickNow(Pawn p)
        {
            if (!p.RaceProps.Humanlike)
                return;
            var data = p.GetWantsData();
            data.nextWantTick = Find.TickManager.TicksGame;
        }

        [DebugAction("Wants and Quirks", "Generate reward bubbles", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ForceGenerateBubbles()
        {
            WantsAndQuirksUtility.GenerateGlobalRewardBubbles();
        }

        [DebugAction("Wants and Quirks", "Add 100 character points", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddCharPoints()
        {
            WantsAndQuirksUtility.AddCharacterPoints(null, 100);
        }

        [DebugAction("Wants and Quirks", "Add 1 reward point", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddRewardPoint()
        {
            State.rewardPoints += 1;
        }

        [DebugAction("Wants and Quirks", "Reset all points", false, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ResetPoints()
        {
            State.characterPoints = 0;
            State.currentCharacterPointsNeeded = 0;
            State.rewardPoints = 0;

            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
            {
                if (!pawn.CanHaveWants())
                    continue;

                var data = pawn.GetWantsData();
                data.characterPoints = 0;
                data.currentCharacterPointsNeeded = 0;
                data.rewardPoints = 0;
            }
        }
    }
}
