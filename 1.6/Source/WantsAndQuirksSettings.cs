using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace WantsAndQuirks
{
    public enum WantSettingsTab
    {
        General,
        Wants,
        TraumaticWants,
        Modifiers
    }

    public class WantsAndQuirksSettings : ModSettings
    {
        public bool enableWantsSystem = true;
        public bool enableCharactersMenu = true;
        public bool enableMentalBreakWants = true;
        public bool disableTechLevelRestrictions = false;
        public int bubblesPerRoll = 10;
        public bool rerollBubblesOnSelection = false;
        public int rerollsPerWant = 2;
        public int pointsNeededForReward = 1000;
        private string pointsNeededForRewardBuffer;
        public int pointsNeededIncreasePerCompletion = 0;
        private string pointsNeededIncreasePerCompletionBuffer;
        public int startingWantsCount = 0;
        public int maxActiveWants = 4;
        public IntRange wantGenerationFrequencyDays = new IntRange(1, 8);
        public bool pawnSpecificRewardPoints = true;
        public HashSet<string> disabledWantDefNames = new HashSet<string>();
        public Dictionary<string, float> wantCommonalityModifiers = new Dictionary<string, float>();
        public HashSet<string> disabledRewardDefNames = new HashSet<string>();

        private List<WantDef> normalWantDefsCache;
        private List<WantDef> traumaticWantDefsCache;
        private List<RewardDef> rewardDefsCache;
        private WantSettingsTab currentTab = WantSettingsTab.General;
        private Vector2 wantsScrollPosition;
        private Vector2 traumaticWantsScrollPosition;
        private Vector2 rewardsScrollPosition;
        private float wantsViewHeight = 1000f;
        private float traumaticWantsViewHeight = 1000f;
        private float rewardsViewHeight = 1000f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableWantsSystem, "enableWantsSystem", true);
            Scribe_Values.Look(ref enableCharactersMenu, "enableCharactersMenu", true);
            Scribe_Values.Look(ref enableMentalBreakWants, "enableMentalBreakWants", true);
            Scribe_Values.Look(ref disableTechLevelRestrictions, "disableTechLevelRestrictions", false);
            Scribe_Values.Look(ref bubblesPerRoll, "bubblesPerRoll", 10);
            Scribe_Values.Look(ref rerollBubblesOnSelection, "rerollBubblesOnSelection", false);
            Scribe_Values.Look(ref rerollsPerWant, "rerollsPerWant", 2);
            Scribe_Values.Look(ref pointsNeededForReward, "pointsNeededForReward", 1000);
            Scribe_Values.Look(ref pointsNeededIncreasePerCompletion, "pointsNeededIncreasePerCompletion", 0);
            Scribe_Values.Look(ref startingWantsCount, "startingWantsCount", 0);
            Scribe_Values.Look(ref maxActiveWants, "maxActiveWants", 4);
            Scribe_Values.Look(ref wantGenerationFrequencyDays, "wantGenerationFrequencyDays", new IntRange(1, 8));
            Scribe_Values.Look(ref pawnSpecificRewardPoints, "pawnSpecificRewardPoints", true);
            Scribe_Collections.Look(ref disabledWantDefNames, "disabledWantDefNames", LookMode.Value);
            disabledWantDefNames ??= new HashSet<string>();
            Scribe_Collections.Look(ref wantCommonalityModifiers, "wantCommonalityModifiers", LookMode.Value, LookMode.Value);
            wantCommonalityModifiers ??= new Dictionary<string, float>();
            Scribe_Collections.Look(ref disabledRewardDefNames, "disabledRewardDefNames", LookMode.Value);
            disabledRewardDefNames ??= new HashSet<string>();
        }

        public float GetCommonalityModifierPercent(WantDef def)
        {
            return wantCommonalityModifiers.TryGetValue(def.defName, out var pct) ? pct : 100f;
        }

        public float GetCommonalityMultiplier(WantDef def)
        {
            return GetCommonalityModifierPercent(def) / 100f;
        }

        private void EnsureWantDefCaches()
        {
            if (normalWantDefsCache != null)
                return;

            rewardDefsCache = DefDatabase<RewardDef>.AllDefsListForReading.OrderBy(d => d.rarity).ThenBy(d => d.label).ToList();

            var allWants = DefDatabase<WantDef>.AllDefsListForReading;
            normalWantDefsCache = allWants.Where(d => !d.isMentalBreakWant).OrderBy(d => d.label).ToList();
            traumaticWantDefsCache = allWants.Where(d => d.isMentalBreakWant).OrderBy(d => d.label).ToList();
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            EnsureWantDefCaches();

            var contentRect = inRect;
            contentRect.yMin += 32f;

            var tabs = new List<TabRecord>
            {
                new TabRecord("WQ_SettingsTabGeneral".Translate(), () => currentTab = WantSettingsTab.General, currentTab == WantSettingsTab.General),
                new TabRecord("WQ_SettingsTabWants".Translate(), () => currentTab = WantSettingsTab.Wants, currentTab == WantSettingsTab.Wants),
                new TabRecord("WQ_SettingsTabTraumaticWants".Translate(), () => currentTab = WantSettingsTab.TraumaticWants, currentTab == WantSettingsTab.TraumaticWants),
                new TabRecord("WQ_SettingsTabModifiers".Translate(), () => currentTab = WantSettingsTab.Modifiers, currentTab == WantSettingsTab.Modifiers)
            };
            TabDrawer.DrawTabs(contentRect, tabs);

            switch (currentTab)
            {
                case WantSettingsTab.Wants:
                    DrawWantDefListTab(contentRect, normalWantDefsCache, ref wantsScrollPosition, ref wantsViewHeight);
                    break;
                case WantSettingsTab.TraumaticWants:
                    DrawWantDefListTab(contentRect, traumaticWantDefsCache, ref traumaticWantsScrollPosition, ref traumaticWantsViewHeight);
                    break;
                case WantSettingsTab.Modifiers:
                    DrawRewardDefListTab(contentRect);
                    break;
                default:
                    DrawGeneralTab(contentRect);
                    break;
            }
        }

        private void DrawGeneralTab(Rect rect)
        {
            var ls = new Listing_Standard();
            ls.Begin(rect);
            ls.CheckboxLabeled("WQ_EnableWantsSystem".Translate(), ref enableWantsSystem);
            ls.CheckboxLabeled("WQ_EnableCharactersMenu".Translate(), ref enableCharactersMenu);
            ls.CheckboxLabeled("WQ_EnableMentalBreakWants".Translate(), ref enableMentalBreakWants);
            ls.CheckboxLabeled("WQ_PawnSpecificRewardPoints".Translate(), ref pawnSpecificRewardPoints);
            ls.CheckboxLabeled("WQ_DisableTechLevelRestrictions".Translate(), ref disableTechLevelRestrictions);
            ls.Label("WQ_BubblesPerRoll".Translate(bubblesPerRoll));
            bubblesPerRoll = (int)ls.Slider(bubblesPerRoll, 1, 50);
            ls.CheckboxLabeled("WQ_RerollBubblesOnSelection".Translate(), ref rerollBubblesOnSelection);
            ls.Label("WQ_RerollsPerWant".Translate(rerollsPerWant));
            rerollsPerWant = (int)ls.Slider(rerollsPerWant, 0, 10);
            ls.Label("WQ_PointsNeededForReward".Translate(pointsNeededForReward));
            ls.TextFieldNumeric(ref pointsNeededForReward, ref pointsNeededForRewardBuffer, 100, 3000);
            ls.Label("WQ_PointsNeededIncreasePerCompletion".Translate(pointsNeededIncreasePerCompletion));
            ls.TextFieldNumeric(ref pointsNeededIncreasePerCompletion, ref pointsNeededIncreasePerCompletionBuffer, 0, 1000);
            ls.Label("WQ_StartingWantsCount".Translate(startingWantsCount));
            startingWantsCount = (int)ls.Slider(startingWantsCount, 0, 10);
            ls.Label("WQ_MaxActiveWants".Translate(maxActiveWants));
            maxActiveWants = (int)ls.Slider(maxActiveWants, 1, 10);
            ls.Label("WQ_WantGenerationFrequency".Translate(wantGenerationFrequencyDays.min, wantGenerationFrequencyDays.max));
            ls.IntRange(ref wantGenerationFrequencyDays, 1, 60);
            ls.End();
        }

        private static Texture GetDisplayIcon(WantDef def)
        {
            if (def.discoveryRequirementThing != null && !def.preferIconPath)
                return def.discoveryRequirementThing.uiIcon;
            return def.Icon;
        }

        private void DrawWantDefListTab(Rect rect, List<WantDef> defs, ref Vector2 scrollPos, ref float viewHeight)
        {
            var viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);

            var ls = new Listing_Standard();
            ls.Begin(viewRect);
            ls.maxOneColumn = true;

            const float iconSize = 64f;
            const float checkboxHeight = 24f;
            const float labelHeight = 20f;
            const float sliderHeight = 24f;
            const float innerGap = 4f;
            const float blockHeight = checkboxHeight + innerGap + labelHeight + innerGap + sliderHeight;

            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                var enabled = !disabledWantDefNames.Contains(def.defName);
                var rowTint = enabled ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                var priorColor = GUI.color;

                var rowRect = ls.GetRect(blockHeight);
                var iconRect = new Rect(rowRect.x, rowRect.y, iconSize, rowRect.height);
                var contentX = rowRect.x + iconSize + 8f;
                var contentWidth = rowRect.width - iconSize - 8f;

                var icon = GetDisplayIcon(def);
                if (icon != null)
                {
                    var scale = Mathf.Min(1f, Mathf.Min(iconRect.width / icon.width, iconRect.height / icon.height));
                    var drawWidth = icon.width * scale;
                    var drawHeight = icon.height * scale;
                    var iconDrawRect = new Rect(
                        iconRect.x + (iconRect.width - drawWidth) / 2f,
                        iconRect.y + (iconRect.height - drawHeight) / 2f,
                        drawWidth, drawHeight);
                    GUI.color = rowTint;
                    GUI.DrawTexture(iconDrawRect, icon);
                    GUI.color = priorColor;
                }

                var checkRect = new Rect(contentX, rowRect.y, contentWidth, checkboxHeight);
                GUI.color = rowTint;
                Widgets.CheckboxLabeled(checkRect, def.LabelCap, ref enabled);
                GUI.color = priorColor;
                if (enabled)
                    disabledWantDefNames.Remove(def.defName);
                else
                    disabledWantDefNames.Add(def.defName);

                var modifierPct = GetCommonalityModifierPercent(def);
                var labelRect = new Rect(contentX, checkRect.yMax + innerGap, contentWidth, labelHeight);
                GUI.color = rowTint;
                Widgets.Label(labelRect, "WQ_CommonalityModifier".Translate(Mathf.RoundToInt(modifierPct)));
                GUI.color = priorColor;

                var sliderRect = new Rect(contentX, labelRect.yMax + innerGap, contentWidth, sliderHeight);
                GUI.color = rowTint;
                GUI.enabled = enabled;
                var newModifierPct = Widgets.HorizontalSlider(sliderRect, modifierPct, 0f, 300f);
                GUI.enabled = true;
                GUI.color = priorColor;
                if (enabled && !Mathf.Approximately(newModifierPct, modifierPct))
                {
                    if (Mathf.Approximately(newModifierPct, 100f))
                        wantCommonalityModifiers.Remove(def.defName);
                    else
                        wantCommonalityModifiers[def.defName] = newModifierPct;
                }

                ls.GapLine();
            }
            ls.End();
            viewHeight = ls.CurHeight;

            Widgets.EndScrollView();
        }

        private static Texture GetDisplayIcon(RewardDef def)
        {
            if (def.iconPath.NullOrEmpty() && (def.gene == null || def.gene.iconPath.NullOrEmpty()))
                return null;
            return def.Icon;
        }

        private void DrawRewardDefListTab(Rect rect)
        {
            var viewRect = new Rect(0f, 0f, rect.width - 16f, rewardsViewHeight);
            Widgets.BeginScrollView(rect, ref rewardsScrollPosition, viewRect);

            var ls = new Listing_Standard();
            ls.Begin(viewRect);
            ls.maxOneColumn = true;

            const float iconSize = 48f;
            const float checkboxHeight = 24f;
            const float labelHeight = 20f;
            const float innerGap = 4f;
            const float blockHeight = checkboxHeight + innerGap + labelHeight;

            for (int i = 0; i < rewardDefsCache.Count; i++)
            {
                var def = rewardDefsCache[i];
                var enabled = !disabledRewardDefNames.Contains(def.defName);
                var rowTint = enabled ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                var priorColor = GUI.color;

                var rowRect = ls.GetRect(blockHeight);
                var iconRect = new Rect(rowRect.x, rowRect.y, iconSize, rowRect.height);
                var contentX = rowRect.x + iconSize + 8f;
                var contentWidth = rowRect.width - iconSize - 8f;

                var icon = GetDisplayIcon(def);
                if (icon != null)
                {
                    var scale = Mathf.Min(1f, Mathf.Min(iconRect.width / icon.width, iconRect.height / icon.height));
                    var drawWidth = icon.width * scale;
                    var drawHeight = icon.height * scale;
                    var iconDrawRect = new Rect(
                        iconRect.x + (iconRect.width - drawWidth) / 2f,
                        iconRect.y + (iconRect.height - drawHeight) / 2f,
                        drawWidth, drawHeight);
                    GUI.color = rowTint;
                    GUI.DrawTexture(iconDrawRect, icon);
                    GUI.color = priorColor;
                }

                var checkRect = new Rect(contentX, rowRect.y, contentWidth, checkboxHeight);
                GUI.color = rowTint;
                Widgets.CheckboxLabeled(checkRect, def.LabelCap, ref enabled);
                if (enabled)
                    disabledRewardDefNames.Remove(def.defName);
                else
                    disabledRewardDefNames.Add(def.defName);

                var labelRect = new Rect(contentX, checkRect.yMax + innerGap, contentWidth, labelHeight);
                GUI.color = new Color(rowTint.r * 0.7f, rowTint.g * 0.7f, rowTint.b * 0.7f, rowTint.a);
                Widgets.Label(labelRect, def.rarity.ToString());
                GUI.color = priorColor;

                ls.GapLine();
            }
            ls.End();
            rewardsViewHeight = ls.CurHeight;

            Widgets.EndScrollView();
        }
    }
}
