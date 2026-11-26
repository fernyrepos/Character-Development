using System;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;
using WantsAndQuirks;

namespace WantsAndQuirksUIRethemed
{
    // The wants tab in the UI Rethemed look: Inspector Tabs' frame, a character points readout, wants as cards, quirks as rows, and the rewards beside the tab.
    [StaticConstructorOnStartup]
    internal sealed class WantsTabLook : IWantsTabLook
    {
        private const string Part = "Character Development wants tab";
        private const Corners FrameCorners = Corners.TopLeft | Corners.TopRight | Corners.BottomRight;
        private const float TitleHeight = 30f;
        private const float Pad = 8f;
        private const float ReadoutHeight = 30f;
        private const float MainWidth = 660f;
        private const float RewardsWidth = 460f;
        private const float WantsShare = 0.62f;
        private const float PanelChrome = 30f + 4f + 8f;

        private static readonly Texture2D TitleTex = CoreWidgets.Icon("sparkle");
        public static readonly Texture2D MenuTex = CoreWidgets.Icon("users-three");
        public static readonly Texture2D GiftTex = CoreWidgets.Icon("gift");
        public static readonly Texture2D WantsTex = CoreWidgets.Icon("heart");
        public static readonly Texture2D QuirksTex = CoreWidgets.Icon("smiley");

        private readonly Action draw;
        private ITab_Pawn_WantsAndQuirks tab;
        private Pawn pawn;
        private Rect frame;
        private Vector2 wantsScroll;
        private Vector2 quirksScroll;
        private int reorderGroup;
        private Pawn shownFor;
        private float shownAt;

        public WantsTabLook()
        {
            draw = DrawContents;
        }

        public bool Failed => Failsafe.Failed(Part);

        public Vector2 Size(ITab_Pawn_WantsAndQuirks tab, Pawn pawn)
        {
            var rows = Mathf.Clamp(WantsAndQuirksMod.settings.maxActiveWants, 3, 6);
            var height = TitleHeight + Pad + ReadoutHeight + Pad + PanelChrome + rows * (WantCards.CardHeight + WantCards.CardGap) + Pad;
            return new Vector2(tab.RewardsOpen ? MainWidth + RewardsWidth : MainWidth, Mathf.Ceil(height));
        }

        public void Draw(ITab_Pawn_WantsAndQuirks tab, Pawn pawn, Rect rect)
        {
            this.tab = tab;
            this.pawn = pawn;
            frame = rect;
            if (pawn != shownFor)
            {
                shownFor = pawn;
                shownAt = ThemeMotion.Now;
            }
            HoverCard.BeginRequests();
            try
            {
                Failsafe.Run(Part, draw);
            }
            finally
            {
                Theme.CloseScrolls();
                Theme.Reset();
                Rounded.WindowBorder(rect, FrameCorners);
                HoverCard.EndRequests();
                HoverCard.DrawRequested();
                Theme.Reset();
            }
        }

        private float Age => ThemeMotion.On ? ThemeMotion.Now - shownAt : float.MaxValue;

        private void DrawContents()
        {
            var data = pawn.GetWantsData();
            Rounded.WindowBackground(frame, FrameCorners);
            DrawTitleBar(new Rect(0f, 0f, frame.width, TitleHeight));

            var body = new Rect(Pad, TitleHeight + Pad, MainWidth - Pad * 2f, frame.height - TitleHeight - Pad * 2f);
            PointsReadout.Draw(new Rect(body.x, body.y, body.width, ReadoutHeight), pawn);

            var panels = new Rect(body.x, body.y + ReadoutHeight + Pad, body.width, body.height - ReadoutHeight - Pad);
            var wantsWidth = Mathf.Floor(panels.width * WantsShare);
            DrawWants(new Rect(panels.x, panels.y, wantsWidth - Pad / 2f, panels.height), data);
            DrawQuirks(new Rect(panels.x + wantsWidth + Pad / 2f, panels.y, panels.width - wantsWidth - Pad / 2f, panels.height), data);

            if (tab.RewardsOpen)
            {
                DrawRewards(new Rect(MainWidth, TitleHeight + Pad, frame.width - MainWidth - Pad, frame.height - TitleHeight - Pad * 2f));
            }
        }

        // Title bar: "<Pawn>'s wants", then the Characters menu button beside Core's close and settings buttons.
        private void DrawTitleBar(Rect bar)
        {
            var buttonsLeft = bar.xMax - 10f - 24f - (UIRethemedCoreMod.Settings.settingsGear ? 28f : 0f);
            var menuRect = new Rect(buttonsLeft - 4f - 24f, Mathf.Round(bar.center.y - 12f), 24f, 24f);
            CoreWidgets.InspectTitleBar(bar, TitleTex, "WQ_PawnWants".Translate(pawn.LabelShortCap), Age, menuRect.x, Corners.Top);
            CoreWidgets.HeaderButtons(bar, () => Failsafe.Later(tab.Close), LoadedModManager.GetMod<WantsAndQuirksMod>());
            if (CoreWidgets.HeaderButton(menuRect, MenuTex, Theme.HighlightGray, DefsOf.WQ_CharactersMenu.LabelCap, DefsOf.WQ_CharactersMenu.description, 14f))
            {
                Failsafe.Later(ITab_Pawn_WantsAndQuirks.OpenCharactersMenu);
            }
        }

        private void DrawWants(Rect rect, PawnWantsData data)
        {
            var count = data.activeWants.Count + "/" + WantsAndQuirksMod.settings.maxActiveWants;
            var countWidth = CoreWidgets.Width(count) + 4f;
            var inner = CoreWidgets.Panel(rect, "WQ_Wants".Translate(), WantsTex, out var titleBar, countWidth + 8f);
            CoreWidgets.Line(new Rect(titleBar.xMax - 10f - countWidth, titleBar.y, countWidth, titleBar.height), count, Theme.TextDim, GameFont.Small, TextAnchor.MiddleRight);
            if (data.activeWants.Count == 0)
            {
                CoreWidgets.Empty(inner, "WQ_NoActiveWants".Translate());
                return;
            }
            WantCards.DrawWantList(inner, pawn, data, ref wantsScroll, ref reorderGroup);
        }

        // Quirks, with the rewards button in the panel's title bar: grayed out without points, lit when there are points, green while open.
        private void DrawQuirks(Rect rect, PawnWantsData data)
        {
            var points = WantsAndQuirksUtility.RewardPointsToSpend(pawn);
            var open = tab.RewardsOpen;
            var label = points.ToString();
            var toolWidth = CoreWidgets.HeaderToolWidth(label);
            var inner = CoreWidgets.Panel(rect, "WQ_Quirks".Translate(), QuirksTex, out var titleBar, toolWidth + 8f);
            var toolRect = new Rect(titleBar.xMax - 6f - toolWidth, Mathf.Round(titleBar.center.y - 12f), toolWidth, 24f);
            var tip = open ? "WQ_UIR_CloseRewards".Translate().Resolve() : points > 0 ? "WQ_RewardsButtonTip".Translate(points).Resolve() : "WQ_NoRewardPointsToSpend".Translate().Resolve();
            var live = open || points > 0;
            if (CoreWidgets.HeaderTool(toolRect, GiftTex, "WQ_Rewards".Translate(), tip, live, open ? Theme.HighlightGreen : Looks.Reward, live, label))
            {
                var owner = pawn;
                Failsafe.Later(() => tab.ToggleRewards(owner));
            }

            if (data.quirks.Count == 0)
            {
                CoreWidgets.Empty(inner, "WQ_UIR_NoQuirks".Translate());
                return;
            }
            WantCards.DrawQuirkList(inner, pawn, data, ref quirksScroll);
        }

        // The reward bubbles beside the tab, in the theme's rarity colors; a click gives the reward to this pawn.
        private void DrawRewards(Rect rect)
        {
            var pointsText = "WQ_RewardPoints".Translate(WantsAndQuirksUtility.RewardPointsToSpend(pawn)).Resolve();
            var pointsWidth = CoreWidgets.Width(pointsText) + 4f;
            var inner = CoreWidgets.Panel(rect, "WQ_Rewards".Translate(), GiftTex, out var titleBar, pointsWidth + 8f);
            CoreWidgets.Line(new Rect(titleBar.xMax - 10f - pointsWidth, titleBar.y, pointsWidth, titleBar.height), pointsText, Looks.Points, GameFont.Small, TextAnchor.MiddleRight);
            var field = tab.Bubbles;
            field.rarityColor = Looks.Rarity;
            var owner = pawn;
            field.Draw(inner, true, node => tab.ClaimForPawn(owner, node), node => WantsAndQuirksUtility.RewardBlockedReason(owner, node));
        }
    }
}
