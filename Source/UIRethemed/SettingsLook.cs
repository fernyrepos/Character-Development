using System;
using System.Collections.Generic;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;
using WantsAndQuirks;

namespace WantsAndQuirksUIRethemed
{
    // The settings page in the UI Rethemed look: page tabs; General as folding cards with reset-to-default sliders; want lists with search, ticks and
    // commonality sliders; rewards grouped by rarity. Same settings, ranges and defaults as the classic page.
    [StaticConstructorOnStartup]
    internal sealed class SettingsLook : ISettingsLook
    {
        private const string Part = "Character Development settings";
        private const float TabsHeight = 34f;
        private const float Gap = 12f;
        private const float ControlHeight = 28f;
        private const float WantIconSize = 40f;
        private const float RewardIconSize = 32f;
        private const string WantsKey = "Wants";
        private const string RewardsKey = "Rewards";
        private const string MenuKey = "Menu";
        private const string SearchControl = "WQ_UIR_SettingsSearch";

        private static readonly Texture2D GeneralTex = CoreWidgets.Icon("sliders");
        private static readonly Texture2D TraumaticTex = CoreWidgets.WarningTex;

        private readonly HashSet<string> open = new HashSet<string> { WantsKey, RewardsKey, MenuKey };
        private readonly SettingsCards cards;
        private readonly Action draw;
        private readonly Func<float, float, float> leftCards;
        private readonly Func<float, float, float> rightCards;
        private readonly Action<Listing_Standard> wantsCard;
        private readonly Action<Listing_Standard> rewardsCard;
        private readonly Action<Listing_Standard> menuCard;
        private readonly Func<float, string> wholeNumber = v => Mathf.RoundToInt(v).ToString();
        private readonly Func<float, string> percent = v => Mathf.RoundToInt(v) + "%";
        private readonly Func<float, string> days = v => "WQ_UIR_Days".Translate(Mathf.RoundToInt(v)).Resolve();
        private readonly List<string> pageLabels = new List<string>();
        private readonly List<Texture> pageIcons = new List<Texture>();
        private readonly Dictionary<RewardRarity, string> rarityLabels = new Dictionary<RewardRarity, string>();

        private readonly Dictionary<WantDef, Action<float>> commonalitySetters = new Dictionary<WantDef, Action<float>>();
        private readonly Listing_Standard listing = new Listing_Standard { maxOneColumn = true };
        private readonly Listing_Standard rowListing = new Listing_Standard { maxOneColumn = true };
        private readonly Dictionary<string, string> text = new Dictionary<string, string>();

        private WantsAndQuirksMod mod;
        private Rect inner;
        private int page;
        private string search = "";
        private Vector2 leftScroll;
        private Vector2 rightScroll;
        private float leftHeight;
        private float rightHeight;
        private Vector2 listScroll;
        private float listHeight = 1000f;
        private int listPage = -1;

        public SettingsLook()
        {
            cards = new SettingsCards("CharacterDevelopment", () => open);
            draw = DrawContents;
            leftCards = LeftCards;
            rightCards = RightCards;
            wantsCard = WantsCard;
            rewardsCard = RewardsCard;
            menuCard = MenuCard;
        }

        public bool Failed => Failsafe.Failed(Part);

        private static WantsAndQuirksSettings Settings => WantsAndQuirksMod.settings;

        // Translated once per key for the session
        private string T(string key)
        {
            if (!text.TryGetValue(key, out var value))
            {
                value = key.Translate();
                text[key] = value;
            }
            return value;
        }

        public void Draw(WantsAndQuirksMod mod, Rect inRect)
        {
            this.mod = mod;
            inner = inRect;
            if (pageLabels.Count == 0)
            {
                pageLabels.Add(T("WQ_SettingsTabGeneral"));
                pageLabels.Add(T("WQ_SettingsTabWants"));
                pageLabels.Add(T("WQ_SettingsTabTraumaticWants"));
                pageLabels.Add(T("WQ_UIR_RewardsPage"));
                pageIcons.Add(GeneralTex);
                pageIcons.Add(WantsTabLook.WantsTex);
                pageIcons.Add(TraumaticTex);
                pageIcons.Add(WantsTabLook.GiftTex);
                foreach (RewardRarity rarity in Enum.GetValues(typeof(RewardRarity)))
                {
                    rarityLabels[rarity] = ("WQ_UIR_Rarity" + rarity).Translate();
                }
            }
            try
            {
                Failsafe.Run(Part, draw);
            }
            finally
            {
                Theme.CloseScrolls();
                Theme.Reset();
            }
            cards.Finish();
        }

        private void DrawContents()
        {
            SearchField.StopOnClickAway();
            SettingsCards.ThemeButton(inner, mod.SettingsCategory());

            var tabs = new Rect(inner.x, inner.y, inner.width, TabsHeight);
            var picked = CoreWidgets.PageTabs(tabs, pageLabels, pageIcons, page);
            if (picked >= 0 && picked != page)
            {
                page = picked;
                search = "";
            }
            var body = new Rect(inner.x, tabs.yMax + Gap, inner.width, inner.height - TabsHeight - Gap);
            switch (page)
            {
                case 1:
                    DrawWantList(body, Settings.NormalWantDefs);
                    break;
                case 2:
                    DrawWantList(body, Settings.TraumaticWantDefs);
                    break;
                case 3:
                    DrawRewards(body);
                    break;
                default:
                    DrawGeneral(body);
                    break;
            }
        }

        // General: the Wants card on the left; the Rewards and Characters menu cards on the right
        private void DrawGeneral(Rect rect)
        {
            var half = Mathf.Floor((rect.width - Gap) / 2f);
            leftHeight = cards.Column(new Rect(rect.x, rect.y, half, rect.height), ref leftScroll, leftHeight, leftCards);
            rightHeight = cards.Column(new Rect(rect.xMax - half, rect.y, half, rect.height), ref rightScroll, rightHeight, rightCards);
        }

        private float LeftCards(float y, float width) => cards.Card(y, width, WantsKey, WantsTabLook.WantsTex, T("WQ_Wants"), null, wantsCard);

        private float RightCards(float y, float width)
        {
            y = cards.Card(y, width, RewardsKey, WantsTabLook.GiftTex, T("WQ_Rewards"), null, rewardsCard);
            return cards.Card(y, width, MenuKey, WantsTabLook.MenuTex, DefsOf.WQ_CharactersMenu.LabelCap, null, menuCard);
        }

        private void WantsCard(Listing_Standard listing)
        {
            listing.ColumnWidth -= 2f;
            var s = Settings;
            SettingsCards.Tick(listing, T("WQ_EnableWantsSystem"), ref s.enableWantsSystem, null);
            SettingsCards.Tick(listing, T("WQ_EnableMentalBreakWants"), ref s.enableMentalBreakWants, null);
            SettingsCards.Tick(listing, T("WQ_DisableTechLevelRestrictions"), ref s.disableTechLevelRestrictions, null);
            listing.Gap(4f);
            cards.Slider(listing, T("WQ_UIR_StartingWants"), null, s.startingWantsCount, 0f, 10f, 1f, wholeNumber, 0f, v => Settings.startingWantsCount = Mathf.RoundToInt(v));
            cards.Slider(listing, T("WQ_UIR_MaxActiveWants"), null, s.maxActiveWants, 1f, 10f, 1f, wholeNumber, 4f, v => Settings.maxActiveWants = Mathf.RoundToInt(v));
            cards.Slider(listing, T("WQ_UIR_RerollsPerWant"), null, s.rerollsPerWant, 0f, 10f, 1f, wholeNumber, 2f, v => Settings.rerollsPerWant = Mathf.RoundToInt(v));
            cards.Slider(listing, T("WQ_UIR_NewWantMin"), T("WQ_UIR_NewWantTip"), s.wantGenerationFrequencyDays.min, 1f, 60f, 1f, days, 1f,
                v => Settings.wantGenerationFrequencyDays = new IntRange(Mathf.RoundToInt(v), Mathf.Max(Mathf.RoundToInt(v), Settings.wantGenerationFrequencyDays.max)));
            cards.Slider(listing, T("WQ_UIR_NewWantMax"), T("WQ_UIR_NewWantTip"), s.wantGenerationFrequencyDays.max, 1f, 60f, 1f, days, 8f,
                v => Settings.wantGenerationFrequencyDays = new IntRange(Mathf.Min(Mathf.RoundToInt(v), Settings.wantGenerationFrequencyDays.min), Mathf.RoundToInt(v)));
        }

        private void RewardsCard(Listing_Standard listing)
        {
            listing.ColumnWidth -= 2f;
            var s = Settings;
            SettingsCards.Tick(listing, T("WQ_PawnSpecificRewardPoints"), ref s.pawnSpecificRewardPoints, null);
            SettingsCards.Tick(listing, T("WQ_RerollBubblesOnSelection"), ref s.rerollBubblesOnSelection, null);
            listing.Gap(4f);
            cards.Slider(listing, T("WQ_UIR_PointsNeeded"), null, s.pointsNeededForReward, 100f, 3000f, 10f, wholeNumber, 1000f, v => Settings.pointsNeededForReward = Mathf.RoundToInt(v));
            cards.Slider(listing, T("WQ_UIR_PointsIncrease"), null, s.pointsNeededIncreasePerCompletion, 0f, 1000f, 10f, wholeNumber, 0f, v => Settings.pointsNeededIncreasePerCompletion = Mathf.RoundToInt(v));
            cards.Slider(listing, T("WQ_UIR_BubblesPerRoll"), null, s.bubblesPerRoll, 1f, 50f, 1f, wholeNumber, 10f, v => Settings.bubblesPerRoll = Mathf.RoundToInt(v));
        }

        private void MenuCard(Listing_Standard listing)
        {
            listing.ColumnWidth -= 2f;
            SettingsCards.Tick(listing, T("WQ_EnableCharactersMenu"), ref Settings.enableCharactersMenu, null);
        }

        // One row per want: icon, tick, commonality slider with reset; turned-off wants are grayed out and their slider is inert
        private void DrawWantList(Rect rect, List<WantDef> defs)
        {
            if (listPage != page)
            {
                listPage = page;
                listScroll = Vector2.zero;
            }
            var top = new Rect(rect.x, rect.y, rect.width, ControlHeight);
            var resetLabel = T("WQ_UIR_ResetWants");
            var resetWidth = CoreWidgets.ButtonWidth(resetLabel, true);
            var resetRect = new Rect(top.xMax - resetWidth, top.y, resetWidth, ControlHeight);
            var anyChanged = AnyChanged(defs);
            if (CoreWidgets.Button(resetRect, resetLabel, CoreWidgets.ResetTex, null, anyChanged, anyChanged ? T("WQ_UIR_ResetWantsTip") : T("WQ_UIR_ResetWantsNothing")))
            {
                var list = defs;
                cards.Confirm(T("WQ_UIR_ResetWantsConfirm"), () => ResetWants(list));
            }
            SearchField.Draw(new Rect(top.x, top.y, Mathf.Min(320f, resetRect.x - 8f - top.x), ControlHeight), ref search, SearchControl, T("WQ_UIR_SearchWants"));

            var outRect = new Rect(rect.x, top.yMax + 8f, rect.width, rect.height - ControlHeight - 8f);
            var view = Theme.BeginScroll(outRect, ref listScroll, listHeight);
            listing.Begin(new Rect(0f, 0f, view.width, 100000f));
            var shown = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                var label = def.LabelCap.Resolve();
                if (!search.NullOrEmpty() && label.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                shown++;
                DrawWantRow(listing, def, label);
            }
            listHeight = Mathf.Max(listing.CurHeight, 1f);
            listing.End();
            Theme.EndScroll();
            if (shown == 0)
            {
                CoreWidgets.Empty(outRect, T("WQ_UIR_NoWantsFound"));
            }
        }

        private void DrawWantRow(Listing_Standard listing, WantDef def, string label)
        {
            var s = Settings;
            var row = listing.GetRect(26f + 2f + 30f + 12f);
            listing.Gap(4f);
            var enabled = !s.disabledWantDefNames.Contains(def.defName);
            CoreWidgets.Card(row, false, null, false);

            var iconRect = new Rect(row.x + 10f, Mathf.Round(row.center.y - WantIconSize / 2f), WantIconSize, WantIconSize);
            var icon = WantsAndQuirksSettings.GetDisplayIcon(def);
            if (icon != null)
            {
                GUI.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                Widgets.DrawTextureFitted(iconRect, icon, 1f);
                GUI.color = Color.white;
            }

            var body = new Rect(iconRect.xMax + 12f, row.y + 6f, row.xMax - 10f - iconRect.xMax - 12f, row.height - 12f);
            rowListing.Begin(body);
            if (SettingsCards.Tick(rowListing, label, ref enabled, def.description))
            {
                if (enabled)
                    s.disabledWantDefNames.Remove(def.defName);
                else
                    s.disabledWantDefNames.Add(def.defName);
            }
            var pct = s.GetCommonalityModifierPercent(def);
            if (enabled)
            {
                if (!commonalitySetters.TryGetValue(def, out var set))
                {
                    var want = def;
                    set = v => Settings.SetCommonalityModifierPercent(want, v);
                    commonalitySetters[def] = set;
                }
                cards.Slider(rowListing, T("WQ_UIR_Commonality"), T("WQ_UIR_CommonalityTip"), pct, 0f, 300f, 1f, percent, 100f, set);
            }
            else
            {
                GUI.enabled = false;
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                SettingsCards.SliderLine(rowListing, T("WQ_UIR_Commonality"), null, pct, 0f, 300f, 1f, percent, out _);
                GUI.enabled = true;
                GUI.color = Color.white;
            }
            rowListing.End();
        }

        private static bool AnyChanged(List<WantDef> defs)
        {
            var s = Settings;
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (s.disabledWantDefNames.Contains(def.defName) || s.wantCommonalityModifiers.ContainsKey(def.defName))
                    return true;
            }
            return false;
        }

        private static void ResetWants(List<WantDef> defs)
        {
            var s = Settings;
            for (int i = 0; i < defs.Count; i++)
            {
                s.disabledWantDefNames.Remove(defs[i].defName);
                s.wantCommonalityModifiers.Remove(defs[i].defName);
            }
        }

        // Rewards grouped by rarity under headings in the theme's rarity colors; one tick each with the description as its tip
        private void DrawRewards(Rect rect)
        {
            if (listPage != page)
            {
                listPage = page;
                listScroll = Vector2.zero;
            }
            var s = Settings;
            var defs = s.RewardDefs;
            var view = Theme.BeginScroll(rect, ref listScroll, listHeight);
            listing.Begin(new Rect(0f, 0f, view.width, 100000f));
            RewardRarity? group = null;
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (group != def.rarity)
                {
                    group = def.rarity;
                    if (i > 0)
                    {
                        listing.Gap(10f);
                    }
                    var heading = listing.GetRect(28f);
                    var color = Looks.Rarity(def.rarity);
                    Rounded.Fill(new Rect(heading.x, heading.y + 4f, 4f, heading.height - 8f), color);
                    CoreWidgets.Line(new Rect(heading.x + 12f, heading.y, heading.width - 12f, heading.height), rarityLabels[def.rarity], color);
                    listing.Gap(2f);
                }
                var row = listing.GetRect(RewardIconSize + 8f);
                listing.Gap(4f);
                var enabled = !s.disabledRewardDefNames.Contains(def.defName);
                CoreWidgets.Card(row, false, null, false);
                var iconRect = new Rect(row.x + 8f, row.y + 4f, RewardIconSize, RewardIconSize);
                var icon = WantsAndQuirksSettings.GetDisplayIcon(def);
                if (icon != null)
                {
                    GUI.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                    Widgets.DrawTextureFitted(iconRect, icon, 1f);
                    GUI.color = Color.white;
                }
                var tickArea = new Rect(iconRect.xMax + 10f, Mathf.Round(row.center.y - 13f), row.xMax - 10f - iconRect.xMax - 10f, 28f);
                rowListing.Begin(tickArea);
                if (SettingsCards.Tick(rowListing, def.LabelCap, ref enabled, def.description))
                {
                    if (enabled)
                        s.disabledRewardDefNames.Remove(def.defName);
                    else
                        s.disabledRewardDefNames.Add(def.defName);
                }
                rowListing.End();
            }
            listHeight = Mathf.Max(listing.CurHeight, 1f);
            listing.End();
            Theme.EndScroll();
        }
    }
}
