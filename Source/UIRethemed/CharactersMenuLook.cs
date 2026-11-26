using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;
using Verse.Sound;
using WantsAndQuirks;

namespace WantsAndQuirksUIRethemed
{
    // The Characters menu in the UI Rethemed look: title bar with the colony's numbers, search and sort; the pawn list; the selected pawn's wants and quirks; the rewards.
    [StaticConstructorOnStartup]
    internal sealed class CharactersMenuLook : ICharactersMenuLook
    {
        private const string Part = "Character Development Characters menu";
        private const float TitleHeight = 40f;
        private const float Pad = 6f;
        private const float ControlHeight = 28f;
        private const float ListWidth = 560f;
        private const float SelectedWidth = 440f;
        private const float MinRewardsWidth = 520f;
        private const float HeadingHeight = 26f;
        private const float RowHeight = 36f;
        private const float RowGap = 3f;
        private const float QuirkIconSize = 24f;
        private const float NameWidth = 220f;
        private const float WantsWidth = 70f;
        private const float PointsWidth = 80f;
        private const float ReadoutHeight = 30f;
        private const float InstructionHeight = 44f;
        private const int RefreshFrames = 30;
        private const string SearchControl = "WQ_UIR_Search";

        private static readonly Texture2D UserTex = CoreWidgets.Icon("user");
        private static readonly Texture2D OpenTex = CoreWidgets.Icon("arrow-square-out");
        private static readonly Texture2D HelpTex = CoreWidgets.Icon("question");

        private enum SortKey { Name, Wants, Points, Quirks }

        // One listed pawn and its text, rebuilt with the list
        private sealed class Row
        {
            public Pawn pawn;
            public string name;
            public string nameTip;
            public string wants;
            public string wantsTip;
            public string points;
            public string pointsTip;
            public int wantsCount;
            public int pointsCount;
            public int quirkCount;
        }

        private readonly Action draw;
        private readonly Action close;
        private readonly Func<SortChoices> sortChoices;
        private readonly Func<bool> isOpen;
        private readonly List<Pawn> pawns = new List<Pawn>();
        private readonly List<Row> pool = new List<Row>();
        private readonly List<Row> rows = new List<Row>();
        private readonly List<SummaryStat> stats = new List<SummaryStat>();
        private readonly Comparison<Row> compare;
        private MainTabWindow_Characters menu;
        private Rect inner;
        private float openedAt;
        private int lastDrawFrame = -10;
        private int builtFrame = -1000;
        private bool dirty = true;
        private string search = "";
        private string builtSearch;
        private SortKey sortBy = SortKey.Name;
        private bool descending;
        private SortChoices choices;
        private string sortLabel = "";
        private string sortTip = "";
        private string searchFound = "";
        private Vector2 listScroll;
        private Vector2 wantsScroll;
        private Vector2 quirksScroll;
        private int reorderGroup;
        private Pawn shownPawn;

        public CharactersMenuLook()
        {
            draw = DrawContents;
            close = () => Failsafe.Later(() => Find.MainTabsRoot.EscapeCurrentTab());
            sortChoices = SortChoices;
            isOpen = () => menu != null && Find.WindowStack.IsOpen(menu);
            compare = Compare;
        }

        public bool Failed => Failsafe.Failed(Part);

        public Vector2 Size(MainTabWindow_Characters menu)
        {
            var width = Mathf.Min(UI.screenWidth, ListWidth + SelectedWidth + 600f + Pad * 4f);
            var height = Mathf.Clamp(UI.screenHeight * 0.75f, 600f, 860f);
            return new Vector2(width, Mathf.Min(height, UI.screenHeight - 35f));
        }

        public void Draw(MainTabWindow_Characters menu, Rect inRect)
        {
            this.menu = menu;
            inner = inRect;
            if (Time.frameCount - lastDrawFrame > 2)
            {
                Opened();
            }
            lastDrawFrame = Time.frameCount;
            HoverCard.BeginRequests();
            try
            {
                Failsafe.Run(Part, draw);
            }
            finally
            {
                Theme.CloseScrolls();
                Theme.Reset();
                HoverCard.EndRequests();
                HoverCard.DrawRequested();
                Theme.Reset();
            }
        }

        private void Opened()
        {
            openedAt = ThemeMotion.Now;
            search = "";
            dirty = true;
            listScroll = Vector2.zero;
        }

        private float Age => ThemeMotion.On ? ThemeMotion.Now - openedAt : float.MaxValue;

        private void DrawContents()
        {
            if (dirty || builtSearch != search || Time.frameCount - builtFrame >= RefreshFrames)
            {
                Rebuild();
            }
            SearchField.StopOnClickAway();
            Rounded.WindowBackground(inner, Corners.TopRight);
            var rect = inner.ContractedBy(Pad);
            DrawTitleBar(new Rect(rect.x, rect.y, rect.width, TitleHeight));

            var body = new Rect(rect.x, rect.y + TitleHeight + Pad, rect.width, rect.height - TitleHeight - Pad);
            var listRect = new Rect(body.x, body.y, ListWidth, body.height);
            var selectedRect = new Rect(listRect.xMax + Pad, body.y, SelectedWidth, body.height);
            var rewardsRect = new Rect(selectedRect.xMax + Pad, body.y, Mathf.Max(MinRewardsWidth, body.xMax - selectedRect.xMax - Pad), body.height);
            DrawList(listRect);
            DrawSelected(selectedRect);
            DrawRewards(rewardsRect);
        }

        // List, summary and sort labels: rebuilt when opened, when the search or sort changes, and twice a second while open
        private void Rebuild()
        {
            dirty = false;
            builtFrame = Time.frameCount;
            builtSearch = search;
            MainTabWindow_Characters.CollectPawns(pawns);
            if (menu.SelectedPawn != null && !pawns.Contains(menu.SelectedPawn))
            {
                menu.SelectedPawn = null;
            }

            var perPawn = WantsAndQuirksMod.settings.pawnSpecificRewardPoints;
            int wantsTotal = 0, pointsTotal = 0, quirksTotal = 0;
            rows.Clear();
            for (int i = 0; i < pawns.Count; i++)
            {
                var pawn = pawns[i];
                var data = pawn.GetWantsData();
                wantsTotal += data.activeWants.Count;
                pointsTotal += perPawn ? data.rewardPoints : 0;
                quirksTotal += data.quirks.Count;
                var title = pawn.story?.TitleShortCap;
                var name = title.NullOrEmpty() ? pawn.LabelShortCap.ToString() : pawn.LabelShortCap + ", " + title;
                if (!search.NullOrEmpty() && name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (pool.Count <= rows.Count)
                {
                    pool.Add(new Row());
                }
                var row = pool[rows.Count];
                row.pawn = pawn;
                row.name = name;
                row.nameTip = title.NullOrEmpty() ? null : pawn.story.TitleCap;
                row.wantsCount = data.activeWants.Count;
                row.wants = row.wantsCount.ToString();
                row.wantsTip = row.wantsCount > 0 ? string.Join("\n", data.activeWants.Select(w => w.LabelCap)) : null;
                row.pointsCount = perPawn ? data.rewardPoints : 0;
                row.points = row.pointsCount.ToString();
                row.pointsTip = "WQ_CharacterPoints".Translate() + ": " + data.characterPoints + " / " + WantsAndQuirksUtility.GetPawnCharacterPointsNeeded(data);
                row.quirkCount = data.quirks.Count;
                rows.Add(row);
            }
            rows.Sort(compare);
            if (!perPawn)
            {
                pointsTotal = State.rewardPoints;
            }

            stats.Clear();
            stats.Add(new SummaryStat(WantsTabLook.MenuTex, pawns.Count, Theme.HighlightGray, "WQ_UIR_StatCharacters".Translate(), "WQ_UIR_StatCharactersTip".Translate()));
            stats.Add(new SummaryStat(WantsTabLook.WantsTex, wantsTotal, Looks.Progress, "WQ_Wants".Translate(), "WQ_UIR_StatWantsTip".Translate()));
            stats.Add(new SummaryStat(WantsTabLook.GiftTex, pointsTotal, Looks.Reward, "WQ_UIR_StatPoints".Translate(), "WQ_UIR_StatPointsTip".Translate()));
            stats.Add(new SummaryStat(WantsTabLook.QuirksTex, quirksTotal, Theme.HighlightBlue, "WQ_Quirks".Translate(), "WQ_UIR_StatQuirksTip".Translate()));

            sortLabel = SortLabel(sortBy);
            var direction = descending ? "UIRethemedCore_Descending".Translate() : "UIRethemedCore_Ascending".Translate();
            sortTip = "WQ_UIR_SortTip".Translate(sortLabel, direction);
            searchFound = "WQ_UIR_SearchFound".Translate(rows.Count);
        }

        private int Compare(Row a, Row b)
        {
            int result;
            switch (sortBy)
            {
                case SortKey.Wants: result = a.wantsCount.CompareTo(b.wantsCount); break;
                case SortKey.Points: result = a.pointsCount.CompareTo(b.pointsCount); break;
                case SortKey.Quirks: result = a.quirkCount.CompareTo(b.quirkCount); break;
                default: result = string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase); break;
            }
            if (descending)
            {
                result = -result;
            }
            return result != 0 ? result : a.pawn.thingIDNumber.CompareTo(b.pawn.thingIDNumber);
        }

        private static string SortLabel(SortKey key)
        {
            switch (key)
            {
                case SortKey.Wants: return "WQ_Wants".Translate();
                case SortKey.Points: return "WQ_UIR_StatPoints".Translate();
                case SortKey.Quirks: return "WQ_Quirks".Translate();
                default: return "WQ_UIR_SortName".Translate();
            }
        }

        private void SetSort(SortKey key)
        {
            if (sortBy == key)
            {
                descending = !descending;
            }
            else
            {
                sortBy = key;
                descending = key != SortKey.Name;
            }
            dirty = true;
        }

        // Title bar: menu name, the colony's numbers, search, sort, help, then Core's settings and close
        private void DrawTitleBar(Rect bar)
        {
            var room = CoreWidgets.TitleBar(bar, WantsTabLook.MenuTex, menu.def.LabelCap, Age, close, LoadedModManager.GetMod<WantsAndQuirksMod>());
            var y = Mathf.Round(bar.center.y - ControlHeight / 2f);
            var right = room.xMax;
            CoreWidgets.TitleTool(bar, ref right, HelpTex, "WQ_UIR_Help".Translate(), "WQ_QuirksInfoText".Translate());

            var sortWidth = SortMenu.ButtonWidth(sortLabel);
            var sort = new Rect(right - sortWidth, y, sortWidth, ControlHeight);
            SortMenu.Button(sort, new Rect(menu.windowRect.position + sort.position, sort.size), sortLabel, descending, sortTip, sortChoices, isOpen);

            var searchWidth = Mathf.Clamp(sort.x - 4f - room.x - 32f, 140f, 240f);
            var searchRect = new Rect(sort.x - 4f - searchWidth, y, searchWidth, ControlHeight);
            SearchField.Draw(searchRect, ref search, SearchControl, "WQ_UIR_SearchHint".Translate(), searchFound);

            CoreWidgets.SummaryStrip(new Rect(room.x + 16f, y, searchRect.x - 8f - room.x - 16f, ControlHeight), stats);
        }

        private SortChoices SortChoices()
        {
            if (choices == null)
            {
                choices = new SortChoices
                {
                    title = "WQ_UIR_SortMenu".Translate(),
                    pickSort = key =>
                    {
                        if (key is SortKey k && k != sortBy)
                        {
                            sortBy = k;
                            descending = k != SortKey.Name;
                            dirty = true;
                        }
                    },
                    setDescending = value =>
                    {
                        descending = value;
                        dirty = true;
                    }
                };
                choices.sorts.Add(new SortOption(SortKey.Name, SortLabel(SortKey.Name), UserTex, Theme.HighlightGray));
                choices.sorts.Add(new SortOption(SortKey.Wants, SortLabel(SortKey.Wants), WantsTabLook.WantsTex, Looks.Progress));
                choices.sorts.Add(new SortOption(SortKey.Points, SortLabel(SortKey.Points), WantsTabLook.GiftTex, Looks.Reward));
                choices.sorts.Add(new SortOption(SortKey.Quirks, SortLabel(SortKey.Quirks), WantsTabLook.QuirksTex, Theme.HighlightBlue));
            }
            choices.sortBy = sortBy;
            choices.descending = descending;
            return choices;
        }

        // The pawns with wants or points: sortable headings, then one row each with portrait, name and title, wants, reward points and quirk icons
        private void DrawList(Rect rect)
        {
            var listInner = CoreWidgets.Panel(rect, "WQ_UIR_CharacterList".Translate(), WantsTabLook.MenuTex);
            var perPawn = WantsAndQuirksMod.settings.pawnSpecificRewardPoints;
            var heading = new Rect(listInner.x, listInner.y, listInner.width, HeadingHeight);
            var x = heading.x + 4f + RowHeight;
            if (CoreWidgets.SortHeading(new Rect(x, heading.y, NameWidth, heading.height), SortLabel(SortKey.Name), null, sortBy == SortKey.Name, descending, null, null, true))
            {
                SetSort(SortKey.Name);
            }
            x += NameWidth;
            if (CoreWidgets.SortHeading(new Rect(x, heading.y, WantsWidth, heading.height), SortLabel(SortKey.Wants), WantsTabLook.WantsTex, sortBy == SortKey.Wants, descending, null, null))
            {
                SetSort(SortKey.Wants);
            }
            x += WantsWidth;
            if (perPawn)
            {
                if (CoreWidgets.SortHeading(new Rect(x, heading.y, PointsWidth, heading.height), SortLabel(SortKey.Points), WantsTabLook.GiftTex, sortBy == SortKey.Points, descending, null, null))
                {
                    SetSort(SortKey.Points);
                }
                x += PointsWidth;
            }
            if (CoreWidgets.SortHeading(new Rect(x, heading.y, heading.xMax - x, heading.height), SortLabel(SortKey.Quirks), WantsTabLook.QuirksTex, sortBy == SortKey.Quirks, descending, null, null, true))
            {
                SetSort(SortKey.Quirks);
            }

            var outRect = new Rect(listInner.x, heading.yMax + 2f, listInner.width, listInner.yMax - heading.yMax - 2f);
            if (rows.Count == 0)
            {
                CoreWidgets.Empty(outRect, search.NullOrEmpty() ? "WQ_UIR_NoCharacters".Translate() : "WQ_UIR_NoneFound".Translate());
                return;
            }
            var view = Theme.BeginScroll(outRect, ref listScroll, rows.Count * (RowHeight + RowGap) - RowGap);
            var y = 0f;
            for (int i = 0; i < rows.Count; i++)
            {
                var rowRect = new Rect(view.x, y, view.width, RowHeight);
                if (rowRect.yMax >= listScroll.y && rowRect.y <= listScroll.y + outRect.height)
                {
                    DrawRow(rowRect, rows[i], i % 2 == 1, perPawn);
                }
                y += RowHeight + RowGap;
            }
            Theme.EndScroll();
        }

        // A click selects or deselects the pawn; a double click jumps to them and opens their wants tab
        private void DrawRow(Rect rowRect, Row row, bool odd, bool perPawn)
        {
            var pawn = row.pawn;
            var selected = menu.SelectedPawn == pawn;
            CoreWidgets.ListRow(rowRect, selected, odd, Theme.HighlightGreen);

            var portrait = new Rect(rowRect.x + 4f, rowRect.y + 3f, RowHeight - 6f, RowHeight - 6f);
            CoreWidgets.Portrait(portrait, pawn);
            var x = rowRect.x + 4f + RowHeight;

            var nameRect = new Rect(x + 4f, rowRect.y, NameWidth - 8f, rowRect.height);
            CoreWidgets.Line(nameRect, row.name, Theme.TextBright);
            CoreWidgets.Tip(nameRect, pawn.LabelCap, row.nameTip);
            x += NameWidth;

            var wantsRect = new Rect(x, rowRect.y, WantsWidth, rowRect.height);
            CoreWidgets.Line(wantsRect, row.wants, row.wantsCount > 0 ? Theme.TextBright : Theme.TextFaint, GameFont.Small, TextAnchor.MiddleCenter);
            if (row.wantsTip != null)
            {
                CoreWidgets.Tip(wantsRect, "WQ_Wants".Translate(), row.wantsTip);
            }
            x += WantsWidth;

            if (perPawn)
            {
                var pointsRect = new Rect(x, rowRect.y, PointsWidth, rowRect.height);
                CoreWidgets.Line(pointsRect, row.points, row.pointsCount > 0 ? Looks.Reward : Theme.TextFaint, GameFont.Small, TextAnchor.MiddleCenter);
                CoreWidgets.Tip(pointsRect, "WQ_UIR_StatPoints".Translate(), row.pointsTip);
                x += PointsWidth;
            }

            var quirks = pawn.GetWantsData().quirks;
            var slots = Mathf.FloorToInt((rowRect.xMax - 4f - x) / (QuirkIconSize + 3f));
            var shown = quirks.Count > slots ? Mathf.Max(slots - 1, 0) : quirks.Count;
            var iconY = Mathf.Round(rowRect.center.y - QuirkIconSize / 2f);
            for (int q = 0; q < shown; q++)
            {
                var quirk = quirks[q];
                var iconRect = new Rect(x + 4f, iconY, QuirkIconSize, QuirkIconSize);
                var tex = WantsAndQuirksUtility.QuirkIcon(quirk, QuirkIconSize);
                if (tex != null)
                {
                    CoreWidgets.DrawIcon(iconRect, tex);
                }
                CoreWidgets.Tip(iconRect, quirk.LabelCap, quirk.Description);
                x += QuirkIconSize + 3f;
            }
            if (shown < quirks.Count)
            {
                var moreRect = new Rect(x + 4f, rowRect.y, QuirkIconSize + 10f, rowRect.height);
                CoreWidgets.Line(moreRect, "+" + (quirks.Count - shown), Theme.TextDim);
            }

            if (Mouse.IsOver(rowRect) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (Event.current.clickCount == 2)
                {
                    menu.SelectedPawn = pawn;
                    Failsafe.Later(() => MainTabWindow_Characters.OpenWantsTab(pawn));
                }
                else
                {
                    menu.SelectedPawn = selected ? null : pawn;
                }
                SoundDefOf.Click.PlayOneShotOnCamera();
                Event.current.Use();
            }
        }

        // The selected pawn's wants and quirks, with the same controls as their wants tab
        private void DrawSelected(Rect rect)
        {
            var pawn = menu.SelectedPawn;
            if (pawn == null)
            {
                var emptyInner = CoreWidgets.Panel(rect, "WQ_UIR_NoOneSelected".Translate(), UserTex);
                CoreWidgets.Empty(emptyInner, "WQ_UIR_SelectCharacter".Translate());
                return;
            }
            if (pawn != shownPawn)
            {
                shownPawn = pawn;
                wantsScroll = Vector2.zero;
                quirksScroll = Vector2.zero;
            }
            var data = pawn.GetWantsData();
            var rowsShown = Mathf.Clamp(WantsAndQuirksMod.settings.maxActiveWants, 3, 6);
            var wantsHeight = Mathf.Min(rect.height * 0.65f, 30f + 4f + 8f + rowsShown * (WantCards.CardHeight + WantCards.CardGap));
            var wantsRect = new Rect(rect.x, rect.y, rect.width, wantsHeight);

            var count = data.activeWants.Count + "/" + WantsAndQuirksMod.settings.maxActiveWants;
            var countWidth = CoreWidgets.Width(count) + 4f;
            var wantsInner = CoreWidgets.Panel(wantsRect, "WQ_PawnWants".Translate(pawn.LabelShortCap), WantsTabLook.WantsTex, out var titleBar, countWidth + 24f + 16f);
            var openRect = new Rect(titleBar.xMax - 6f - 24f, Mathf.Round(titleBar.center.y - 12f), 24f, 24f);
            if (CoreWidgets.HeaderTool(openRect, OpenTex, "WQ_UIR_OpenWantsTab".Translate(), null))
            {
                Failsafe.Later(() => MainTabWindow_Characters.OpenWantsTab(pawn));
            }
            CoreWidgets.Line(new Rect(openRect.x - 8f - countWidth, titleBar.y, countWidth, titleBar.height), count, Theme.TextDim, GameFont.Small, TextAnchor.MiddleRight);
            if (data.activeWants.Count == 0)
            {
                CoreWidgets.Empty(wantsInner, "WQ_NoActiveWants".Translate());
            }
            else
            {
                WantCards.DrawWantList(wantsInner, pawn, data, ref wantsScroll, ref reorderGroup);
            }

            var quirksRect = new Rect(rect.x, wantsRect.yMax + Pad, rect.width, rect.yMax - wantsRect.yMax - Pad);
            var quirksInner = CoreWidgets.Panel(quirksRect, "WQ_Quirks".Translate(), WantsTabLook.QuirksTex);
            if (data.quirks.Count == 0)
            {
                CoreWidgets.Empty(quirksInner, "WQ_UIR_NoQuirks".Translate());
            }
            else
            {
                WantCards.DrawQuirkList(quirksInner, pawn, data, ref quirksScroll);
            }
        }

        // Character points, then the reward bubbles in the theme's rarity colors. With a pawn selected a click gives them the reward; otherwise the Bestow reward dialog asks who gets it
        private void DrawRewards(Rect rect)
        {
            var pawn = menu.SelectedPawn;
            var perPawn = WantsAndQuirksMod.settings.pawnSpecificRewardPoints;
            if (pawn != null || !perPawn)
            {
                PointsReadout.Draw(new Rect(rect.x, rect.y, rect.width, ReadoutHeight), pawn);
                rect.yMin += ReadoutHeight + Pad;
            }

            var points = pawn != null ? WantsAndQuirksUtility.RewardPointsToSpend(pawn) : perPawn ? -1 : State.rewardPoints;
            var pointsText = points >= 0 ? "WQ_RewardPoints".Translate(points).Resolve() : null;
            var pointsWidth = pointsText != null ? CoreWidgets.Width(pointsText) + 4f : 0f;
            var title = pawn != null ? "WQ_UIR_RewardsFor".Translate(pawn.LabelShortCap).Resolve() : "WQ_Rewards".Translate().Resolve();
            var rewardsInner = CoreWidgets.Panel(rect, title, WantsTabLook.GiftTex, out var titleBar, pointsWidth + 8f);
            if (pointsText != null)
            {
                CoreWidgets.Line(new Rect(titleBar.xMax - 10f - pointsWidth, titleBar.y, pointsWidth, titleBar.height), pointsText, points > 0 ? Looks.Reward : Theme.TextDim, GameFont.Small, TextAnchor.MiddleRight);
            }

            var field = new Rect(rewardsInner.x, rewardsInner.y, rewardsInner.width, rewardsInner.height - InstructionHeight);
            var bubbles = menu.Bubbles;
            bubbles.rarityColor = Looks.Rarity;
            if (pawn != null)
            {
                bubbles.Draw(field, true, menu.ClaimReward, node => WantsAndQuirksUtility.RewardBlockedReason(pawn, node));
            }
            else
            {
                bubbles.Draw(field, true, menu.ClaimReward);
            }
            var instruction = pawn != null ? "WQ_UIR_ClickToGive".Translate(pawn.LabelShortCap).Resolve() : "WQ_ClaimInstruction".Translate().Resolve();
            Text.Font = GameFont.Small;
            Theme.Label(new Rect(rewardsInner.x + 8f, field.yMax, rewardsInner.width - 16f, InstructionHeight), instruction, TextAnchor.MiddleCenter, Theme.TextFaint, GameFont.Small);
        }
    }
}
