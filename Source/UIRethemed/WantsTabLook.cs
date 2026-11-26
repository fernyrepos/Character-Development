using System;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;
using Verse.Sound;
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
        private const float MainWidth = 640f;
        private const float RewardsWidth = 460f;
        private const float WantsShare = 0.6f;
        private const float PanelChrome = 30f + 4f + 8f;
        private const float CardHeight = 58f;
        private const float CardGap = 4f;
        private const float QuirkHeight = 36f;
        private const float QuirkGap = 4f;
        private const float NewGlowDuration = 2.5f;

        private static readonly Texture2D TitleTex = CoreWidgets.Icon("sparkle");
        private static readonly Texture2D MenuTex = CoreWidgets.Icon("users-three");
        private static readonly Texture2D GiftTex = CoreWidgets.Icon("gift");
        private static readonly Texture2D StarTex = CoreWidgets.Icon("star");
        private static readonly Texture2D RerollTex = CoreWidgets.Icon("arrows-clockwise");
        private static readonly Texture2D DismissTex = CoreWidgets.Icon("x");
        private static readonly Texture2D WantsTex = CoreWidgets.Icon("heart");
        private static readonly Texture2D QuirksTex = CoreWidgets.Icon("smiley");

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
            var height = TitleHeight + Pad + ReadoutHeight + Pad + PanelChrome + rows * (CardHeight + CardGap) + Pad;
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
            var main = new Rect(0f, 0f, MainWidth, frame.height);
            DrawTitleBar(new Rect(0f, 0f, frame.width, TitleHeight));

            var body = new Rect(main.x + Pad, TitleHeight + Pad, main.width - Pad * 2f, frame.height - TitleHeight - Pad * 2f);
            DrawPointsReadout(new Rect(body.x, body.y, body.width, ReadoutHeight), data);

            var panels = new Rect(body.x, body.y + ReadoutHeight + Pad, body.width, body.height - ReadoutHeight - Pad);
            var wantsWidth = Mathf.Floor(panels.width * WantsShare);
            DrawWants(new Rect(panels.x, panels.y, wantsWidth - Pad / 2f, panels.height), data);
            DrawQuirks(new Rect(panels.x + wantsWidth + Pad / 2f, panels.y, panels.width - wantsWidth - Pad / 2f, panels.height), data);

            if (tab.RewardsOpen)
            {
                DrawRewards(new Rect(MainWidth, TitleHeight + Pad, frame.width - MainWidth - Pad, frame.height - TitleHeight - Pad * 2f));
            }
        }

        // Title bar: "<Pawn>'s wants", then the rewards and Characters buttons beside Core's close and settings buttons.
        private void DrawTitleBar(Rect bar)
        {
            var points = WantsAndQuirksUtility.RewardPointsToSpend(pawn);
            var rewardsLabel = points > 0 ? points.ToString() : null;
            var rewardsWidth = CoreWidgets.HeaderToolWidth(rewardsLabel);
            var buttonsLeft = bar.xMax - 10f - 24f - (UIRethemedCoreMod.Settings.settingsGear ? 28f : 0f);
            var menuRect = new Rect(buttonsLeft - 4f - 24f, Mathf.Round(bar.center.y - 12f), 24f, 24f);
            var rewardsRect = new Rect(menuRect.x - 4f - rewardsWidth, menuRect.y, rewardsWidth, 24f);

            CoreWidgets.InspectTitleBar(bar, TitleTex, "WQ_PawnWants".Translate(pawn.LabelShortCap), Age, rewardsRect.x, Corners.Top);
            CoreWidgets.HeaderButtons(bar, () => Failsafe.Later(tab.Close), LoadedModManager.GetMod<WantsAndQuirksMod>());

            if (CoreWidgets.HeaderButton(menuRect, MenuTex, Theme.HighlightGray, DefsOf.WQ_CharactersMenu.LabelCap, DefsOf.WQ_CharactersMenu.description, 14f))
            {
                Failsafe.Later(ITab_Pawn_WantsAndQuirks.OpenCharactersMenu);
            }

            var open = tab.RewardsOpen;
            var enabled = open || points > 0;
            var tip = open ? "WQ_UIR_CloseRewards".Translate().Resolve() : points > 0 ? "WQ_RewardsButtonTip".Translate(points).Resolve() : "WQ_NoRewardPointsToSpend".Translate().Resolve();
            var accent = open ? Theme.HighlightGreen : Looks.Reward;
            if (CoreWidgets.HeaderTool(rewardsRect, GiftTex, "WQ_Rewards".Translate(), tip, open || points > 0, accent, enabled, rewardsLabel))
            {
                Failsafe.Later(() => tab.ToggleRewards(pawn));
            }
        }

        // Character points toward the next reward point: the pawn's, or the colony's when points aren't per pawn.
        private void DrawPointsReadout(Rect rect, PawnWantsData data)
        {
            var perPawn = WantsAndQuirksMod.settings.pawnSpecificRewardPoints;
            var current = perPawn ? data.characterPoints : State.characterPoints;
            var needed = perPawn ? WantsAndQuirksUtility.GetPawnCharacterPointsNeeded(data) : WantsAndQuirksUtility.GetGlobalCharacterPointsNeeded();
            var label = (perPawn ? "WQ_CharacterPoints" : "WQ_UIR_ColonyCharacterPoints").Translate() + "  " + CoreWidgets.Amount(current) + " / " + CoreWidgets.Amount(needed);
            CoreWidgets.Readout(rect, StarTex, Looks.Points, label, Mathf.Clamp01((float)current / needed), false);
            CoreWidgets.Tip(rect, "WQ_CharacterPoints".Translate(), "WQ_UIR_PointsTip".Translate(CoreWidgets.Amount(needed), WantsAndQuirksUtility.RewardPointsToSpend(pawn)));
        }

        private void DrawWants(Rect rect, PawnWantsData data)
        {
            var count = data.activeWants.Count + "/" + WantsAndQuirksMod.settings.maxActiveWants;
            var inner = CoreWidgets.Panel(rect, "WQ_Wants".Translate(), WantsTex, out var titleBar, CoreWidgets.Width(count) + 8f);
            CoreWidgets.Line(new Rect(titleBar.xMax - 10f - CoreWidgets.Width(count) - 4f, titleBar.y, CoreWidgets.Width(count) + 4f, titleBar.height), count, Theme.TextDim, GameFont.Small, TextAnchor.MiddleRight);

            if (data.activeWants.Count == 0)
            {
                CoreWidgets.Empty(inner, "WQ_NoActiveWants".Translate());
                return;
            }

            var contentHeight = data.activeWants.Count * (CardHeight + CardGap) - CardGap;
            var view = Theme.BeginScroll(inner, ref wantsScroll, contentHeight);
            if (Event.current.type == EventType.Repaint)
            {
                reorderGroup = ReorderableWidget.NewGroup((from, to) =>
                {
                    var item = data.activeWants[from];
                    data.activeWants.Insert(to, item);
                    data.activeWants.RemoveAt(from < to ? from : from + 1);
                }, ReorderableDirection.Vertical, inner);
            }
            var y = 0f;
            for (int i = 0; i < data.activeWants.Count; i++)
            {
                var card = new Rect(view.x, y, view.width, CardHeight);
                if (DrawWantCard(card, data, data.activeWants[i]))
                {
                    break;
                }
                ReorderableWidget.Reorderable(reorderGroup, card);
                y += CardHeight + CardGap;
            }
            Theme.EndScroll();
        }

        // One want: icon square, title and reward tag, progress or how long it has been wanted, reroll and dismiss. Returns true when dismissed.
        private bool DrawWantCard(Rect card, PawnWantsData data, ActiveWant want)
        {
            var mental = want.def.isMentalBreakWant;
            CoreWidgets.Card(card);

            if (!want.hasBeenViewed)
            {
                want.hasBeenViewed = true;
                want.glowStartRealTime = Time.realtimeSinceStartup;
            }
            if (want.glowStartRealTime >= 0f)
            {
                var glowAge = Time.realtimeSinceStartup - want.glowStartRealTime;
                if (glowAge < NewGlowDuration)
                {
                    CoreWidgets.EdgeGlow(card, Looks.NewGlow, 0.6f, 1f - glowAge / NewGlowDuration, 0, 1);
                }
            }
            if (mental)
            {
                CoreWidgets.EdgeGlow(card, Looks.MentalBreak);
            }

            var accent = mental ? Looks.MentalBreak : Looks.Progress;
            var iconRect = new Rect(card.x + 8f, Mathf.Round(card.center.y - 20f), 40f, 40f);
            Texture icon = want.Icon;
            if (want is ActiveWantWithPawnTarget pt && pt.targetPawn != null)
            {
                icon = PortraitsCache.Get(pt.targetPawn, new Vector2(40f, 40f), Rot4.South, cameraZoom: 1.2f);
            }
            CoreWidgets.IconSquare(iconRect, icon, accent);

            var buttonsWidth = mental ? 0f : 80f;
            var textX = iconRect.xMax + 10f;
            var textRight = card.xMax - 8f - buttonsWidth;
            var titleLine = new Rect(textX, card.y + 6f, textRight - textX, 22f);

            var tagLabel = mental ? "WQ_UIR_MentalBreakTag".Translate().Resolve() : "+" + CoreWidgets.Amount(want.def.reward);
            var tagLeft = CoreWidgets.MiniTag(textRight, titleLine.center.y, tagLabel, mental ? Looks.MentalBreak : Looks.Reward,
                mental ? "WQ_CausedByMentalBreak".Translate().Resolve() : "WQ_OnCompletion".Translate() + " " + "WQ_CharacterPointsReward".Translate(want.def.reward));
            CoreWidgets.Line(new Rect(titleLine.x, titleLine.y, tagLeft - titleLine.x - 4f, titleLine.height), want.LabelCap, Theme.TextBright);

            var detailLine = new Rect(textX, card.y + 32f, textRight - textX, 20f);
            var hasProgress = want.def.Worker.TryGetProgress(pawn, want, out var current, out var target) && target > 0f;
            var waited = Find.TickManager.TicksGame - want.assignedTick;
            var waitedText = "WQ_UIR_Waiting".Translate(CoreWidgets.Duration(waited)).Resolve();
            if (hasProgress)
            {
                var countText = current.ToString("0.#") + " / " + target.ToString("0.#");
                var countWidth = CoreWidgets.Width(countText) + 4f;
                var bar = new Rect(detailLine.x, Mathf.Round(detailLine.center.y - 3f), detailLine.width - countWidth - 8f, 6f);
                CoreWidgets.Gauge(bar, current / target, accent);
                CoreWidgets.Line(new Rect(detailLine.xMax - countWidth, detailLine.y, countWidth, detailLine.height), countText, Theme.TextDim, GameFont.Small, TextAnchor.MiddleRight);
            }
            else
            {
                CoreWidgets.Line(detailLine, waitedText, Theme.TextFaint);
            }

            var tipBody = want.Description;
            if (hasProgress)
            {
                tipBody += "\n\n" + "WQ_WantProgress".Translate(current.ToString("0.#"), target.ToString("0.#"));
            }
            tipBody += "\n\n" + waitedText;
            CoreWidgets.Tip(new Rect(card.x, card.y, textRight - card.x, card.height), want.LabelCap, tipBody, null, accent);

            if (mental)
                return false;

            var buttonY = Mathf.Round(card.center.y - 12f);
            var dismissRect = new Rect(card.xMax - 8f - 24f, buttonY, 24f, 24f);
            var maxRerolls = WantsAndQuirksMod.settings.rerollsPerWant;
            if (maxRerolls > 0)
            {
                var left = maxRerolls - want.rerollCount;
                var label = left.ToString();
                var width = CoreWidgets.HeaderToolWidth(label);
                var rerollRect = new Rect(dismissRect.x - 4f - width, buttonY, width, 24f);
                var rerollTip = left > 0 ? "WQ_RerollWantWithCount".Translate(left).Resolve() : "WQ_NoRerollsLeft".Translate().Resolve();
                if (CoreWidgets.HeaderTool(rerollRect, RerollTex, "WQ_UIR_Reroll".Translate(), rerollTip, false, Theme.HighlightBlue, left > 0, label))
                {
                    WantsAndQuirksUtility.RerollWant(pawn, data, want);
                    DefsOf.WQ_RerollSound.PlayOneShotOnCamera();
                }
            }
            if (CoreWidgets.IconButton(dismissRect, DismissTex, "WQ_DismissWant".Translate(), null, false, null, true, 14f))
            {
                data.activeWants.Remove(want);
                return true;
            }
            return false;
        }

        private void DrawQuirks(Rect rect, PawnWantsData data)
        {
            var inner = CoreWidgets.Panel(rect, "WQ_Quirks".Translate(), QuirksTex);
            if (data.quirks.Count == 0)
            {
                CoreWidgets.Empty(inner, "WQ_UIR_NoQuirks".Translate());
                return;
            }
            var contentHeight = data.quirks.Count * (QuirkHeight + QuirkGap) - QuirkGap;
            var view = Theme.BeginScroll(inner, ref quirksScroll, contentHeight);
            var y = 0f;
            for (int i = 0; i < data.quirks.Count; i++)
            {
                var quirk = data.quirks[i];
                var row = new Rect(view.x, y, view.width, QuirkHeight);
                CoreWidgets.Card(row);
                var iconRect = new Rect(row.x + 5f, Mathf.Round(row.center.y - 13f), 26f, 26f);
                var tex = WantsAndQuirksUtility.QuirkIcon(quirk, 26f);
                if (tex != null)
                {
                    CoreWidgets.DrawIcon(iconRect, tex);
                }
                var removeRect = new Rect(row.xMax - 4f - 22f, Mathf.Round(row.center.y - 11f), 22f, 22f);
                CoreWidgets.Line(new Rect(iconRect.xMax + 8f, row.y, removeRect.x - iconRect.xMax - 12f, row.height), quirk.LabelCap, Theme.TextBright);
                CoreWidgets.Tip(new Rect(row.x, row.y, removeRect.x - row.x, row.height), quirk.LabelCap, quirk.Description);
                if (CoreWidgets.IconButton(removeRect, DismissTex, "WQ_UIR_RemoveQuirk".Translate(), null, false, null, true, 12f))
                {
                    var removed = quirk;
                    var owner = pawn;
                    CoreWidgets.Confirm("WQ_ConfirmRemoveQuirk".Translate(removed.LabelCap), () =>
                    {
                        removed.def.Worker.OnRemoved(owner, removed);
                        owner.GetWantsData().quirks.Remove(removed);
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    });
                }
                y += QuirkHeight + QuirkGap;
            }
            Theme.EndScroll();
        }

        // The reward bubbles beside the tab, in the theme's rarity colors; a click gives the reward to this pawn.
        private void DrawRewards(Rect rect)
        {
            var points = WantsAndQuirksUtility.RewardPointsToSpend(pawn);
            var pointsText = "WQ_RewardPoints".Translate(points).Resolve();
            var inner = CoreWidgets.Panel(rect, "WQ_Rewards".Translate(), GiftTex, out var titleBar, CoreWidgets.Width(pointsText) + 8f);
            CoreWidgets.Line(new Rect(titleBar.xMax - 10f - CoreWidgets.Width(pointsText) - 4f, titleBar.y, CoreWidgets.Width(pointsText) + 4f, titleBar.height), pointsText, Looks.Points, GameFont.Small, TextAnchor.MiddleRight);
            var field = tab.Bubbles;
            field.rarityColor = Looks.Rarity;
            var owner = pawn;
            field.Draw(inner, true, node => tab.ClaimForPawn(owner, node), node => WantsAndQuirksUtility.RewardBlockedReason(owner, node));
        }
    }
}
