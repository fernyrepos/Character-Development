using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WantsAndQuirks
{
    [HotSwappable]
    public class ITab_Pawn_WantsAndQuirks : ITab
    {
        private Vector2 wantsScrollPos;
        private Vector2 quirksScrollPos;
        private int reorderableGroupID;
        private bool rewardsOpen;
        private Pawn lastPawn;
        private readonly RewardBubbleField bubbles = new RewardBubbleField();

        private static Color BgColor = new ColorInt(28, 30, 31).ToColor;
        private static Color WantBgColor = new ColorInt(79, 82, 84).ToColor;
        private static Color QuirkContainerColor = new ColorInt(38, 37, 35).ToColor;
        private static Color QuirkBgColor = new ColorInt(70, 68, 66).ToColor;
        private static Color PointsColor = new ColorInt(166, 187, 194).ToColor;
        private static Color MentalBreakRectColor = new ColorInt(57, 45, 45).ToColor;
        private static Color MentalBreakTextColor = new ColorInt(184, 133, 134).ToColor;
        private static Color ProgressFillColor = new ColorInt(126, 206, 214).ToColor.ToTransparent(0.3f);
        private static Color NewWantGlowColor = new Color(1f, 0.85f, 0.35f, 1f);
        private static Color RowButtonBgColor = new Color(0f, 0f, 0f, 0.3f);
        private const float NewWantGlowDuration = 2.5f;
        private const float MainWidth = 600f;
        private const float RewardsPanelWidth = 450f;
        private const float WantRowHeight = 34f;
        private const float WantRowGap = 4f;
        private const float RowButtonSize = 22f;

        public ITab_Pawn_WantsAndQuirks()
        {
            labelKey = "WQ_Wants";
            size = new Vector2(MainWidth, 413f);
        }

        public override bool IsVisible
        {
            get
            {
                if (!WantsAndQuirksMod.settings.enableCharactersMenu)
                    return false;
                var pawn = SelPawn;
                return pawn != null && pawn.CanHaveWants() && (pawn.Faction == Faction.OfPlayer || pawn.IsSlaveOfColony);
            }
        }

        public override void UpdateSize()
        {
            var height = WantsAndQuirksMod.settings.pawnSpecificRewardPoints ? 467f : 413f;
            size = new Vector2(rewardsOpen ? MainWidth + RewardsPanelWidth : MainWidth, height);
        }

        public override void TabUpdate()
        {
            base.TabUpdate();
            if (rewardsOpen)
            {
                bubbles.Update();
            }
        }

        public override void FillTab()
        {
            var pawn = SelPawn;
            if (pawn != lastPawn)
            {
                lastPawn = pawn;
                rewardsOpen = false;
            }
            var data = pawn.GetWantsData();
            var rect = new Rect(0f, 0f, size.x, size.y);

            Widgets.DrawBoxSolid(rect, BgColor);
            var mainRect = new Rect(0f, 0f, MainWidth, size.y).ContractedBy(10f);

            var leftRect = new Rect(mainRect.x, mainRect.y, mainRect.width * 0.70f, mainRect.height);
            var rightRect = new Rect(leftRect.xMax, mainRect.y, mainRect.width * 0.30f, mainRect.height);

            DrawWants(leftRect, pawn, data);
            DrawQuirks(rightRect, pawn, data);
            if (rewardsOpen)
            {
                DrawRewards(new Rect(MainWidth, 0f, RewardsPanelWidth, size.y).ContractedBy(10f), pawn);
            }
        }

        private void DrawWants(Rect rect, Pawn pawn, PawnWantsData data)
        {
            var curY = rect.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, curY, rect.width, 30f), "WQ_PawnWants".Translate(pawn));
            curY += 30f;

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), "WQ_WantsSubtitle".Translate());
            GUI.color = Color.white;
            curY += 28f;

            if (WantsAndQuirksMod.settings.pawnSpecificRewardPoints)
            {
                var needed = WantsAndQuirksUtility.GetPawnCharacterPointsNeeded(data);
                var barRect = new Rect(rect.x, curY, rect.width - 10f, 24f);
                MainTabWindow_Characters.DrawCharacterPointsTracker(barRect, data.characterPoints, needed, pawn);
                curY += 30f;

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x, curY, rect.width, 20f), "WQ_PawnUnlockedModifiers".Translate(data.rewardPoints));
                GUI.color = Color.white;
                curY += 24f;
            }

            Text.Font = GameFont.Small;
            if (data.activeWants.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x, curY, rect.width, 30f), "WQ_NoActiveWants".Translate());
                GUI.color = Color.white;
                return;
            }

            var outRect = new Rect(rect.x, curY, rect.width, rect.height - (curY - rect.y));
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, data.activeWants.Count * (WantRowHeight + WantRowGap) - WantRowGap);
            Widgets.BeginScrollView(outRect, ref wantsScrollPos, viewRect);

            if (Event.current.type == EventType.Repaint)
            {
                reorderableGroupID = ReorderableWidget.NewGroup((int from, int to) =>
                {
                    var item = data.activeWants[from];
                    data.activeWants.Insert(to, item);
                    data.activeWants.RemoveAt((from < to) ? from : (from + 1));
                }, ReorderableDirection.Vertical, outRect);
            }

            var listY = 0f;
            for (int i = 0; i < data.activeWants.Count; i++)
            {
                var want = data.activeWants[i];
                var rowRect = new Rect(0f, listY, viewRect.width, WantRowHeight);
                if (DrawWantRow(rowRect, pawn, data, want))
                {
                    break;
                }
                ReorderableWidget.Reorderable(reorderableGroupID, rowRect);
                listY += WantRowHeight + WantRowGap;
            }

            Widgets.EndScrollView();
        }

        // One want as a single row: icon and title over a progress fill, details in the tooltip, reroll and dismiss at the end. Returns true when the want was dismissed.
        private bool DrawWantRow(Rect rowRect, Pawn pawn, PawnWantsData data, ActiveWant want)
        {
            if (!want.hasBeenViewed)
            {
                want.hasBeenViewed = true;
                want.glowStartRealTime = Time.realtimeSinceStartup;
            }

            if (want.glowStartRealTime >= 0f)
            {
                var glowAge = Time.realtimeSinceStartup - want.glowStartRealTime;
                if (glowAge < NewWantGlowDuration)
                {
                    var glowT = glowAge / NewWantGlowDuration;
                    var glowColor = new Color(NewWantGlowColor.r, NewWantGlowColor.g, NewWantGlowColor.b, (1f - glowT) * 0.85f);
                    Widgets.DrawBoxSolid(rowRect.ExpandedBy(3f), glowColor);
                }
            }

            var isMentalBreak = want.def.isMentalBreakWant;
            Widgets.DrawBoxSolid(rowRect, isMentalBreak ? MentalBreakRectColor : WantBgColor);

            var hasProgress = want.def.Worker.TryGetProgress(pawn, want, out var progCurrent, out var progTarget) && progTarget > 0f;
            if (hasProgress)
            {
                var fill = Mathf.Clamp01(progCurrent / progTarget);
                Widgets.DrawBoxSolid(new Rect(rowRect.x, rowRect.y, rowRect.width * fill, rowRect.height), ProgressFillColor);
            }

            var buttonsWidth = isMentalBreak ? 0f : RowButtonSize * 2f + 14f;
            var titleArea = new Rect(rowRect.x, rowRect.y, rowRect.width - buttonsWidth, rowRect.height);

            var iconRect = new Rect(rowRect.x + 5f, rowRect.y + (rowRect.height - 26f) / 2f, 26f, 26f);
            var tex = want.Icon;
            if (want is ActiveWantWithPawnTarget pt && pt.targetPawn != null)
            {
                tex = PortraitsCache.Get(pt.targetPawn, new Vector2(26f, 26f), Rot4.South, cameraZoom: 1.2f);
            }
            if (tex != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.8f);
                GUI.DrawTexture(iconRect, tex);
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            var labelRect = new Rect(iconRect.xMax + 8f, rowRect.y, titleArea.xMax - iconRect.xMax - 12f, rowRect.height);
            if (isMentalBreak)
            {
                GUI.color = MentalBreakTextColor;
            }
            Widgets.Label(labelRect, $"<i>{want.LabelCap.Truncate(labelRect.width)}</i>");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            if (Mouse.IsOver(titleArea))
            {
                Widgets.DrawHighlight(titleArea);
                TooltipHandler.TipRegion(titleArea, WantTooltip(want, hasProgress, progCurrent, progTarget));
            }

            if (isMentalBreak)
                return false;

            var y = rowRect.y + (rowRect.height - RowButtonSize) / 2f;
            var dismissRect = new Rect(rowRect.xMax - RowButtonSize - 6f, y, RowButtonSize, RowButtonSize);
            var rerollRect = new Rect(dismissRect.x - RowButtonSize - 8f, y, RowButtonSize, RowButtonSize);

            var maxRerolls = WantsAndQuirksMod.settings.rerollsPerWant;
            if (maxRerolls > 0)
            {
                var rerollsLeft = maxRerolls - want.rerollCount;
                if (RowButton(rerollRect, ContentFinder<Texture2D>.Get("UI/Reroll"), rerollsLeft > 0,
                    rerollsLeft > 0 ? "WQ_RerollWantWithCount".Translate(rerollsLeft) : "WQ_NoRerollsLeft".Translate(), rerollsLeft))
                {
                    WantsAndQuirksUtility.RerollWant(pawn, data, want);
                    DefsOf.WQ_RerollSound.PlayOneShotOnCamera();
                }
            }

            if (RowButton(dismissRect, TexButton.CloseXSmall, true, "WQ_DismissWant".Translate()))
            {
                data.activeWants.Remove(want);
                SoundDefOf.Click.PlayOneShotOnCamera();
                return true;
            }
            return false;
        }

        private static string WantTooltip(ActiveWant want, bool hasProgress, float progCurrent, float progTarget)
        {
            var tip = want.LabelCap.AsTipTitle() + "\n\n" + want.Description;
            if (hasProgress)
            {
                tip += "\n\n" + "WQ_WantProgress".Translate(progCurrent.ToString("0.#"), progTarget.ToString("0.#"));
            }
            if (want.def.isMentalBreakWant)
            {
                tip += "\n\n" + "WQ_CausedByMentalBreak".Translate().Colorize(MentalBreakTextColor);
            }
            else
            {
                tip += "\n\n" + ("WQ_OnCompletion".Translate() + " " + "WQ_CharacterPointsReward".Translate(want.def.reward)).Colorize(PointsColor);
            }
            return tip;
        }

        // A square icon button on a dark backing; inert and greyed when not active. A count above zero is drawn in its corner.
        private static bool RowButton(Rect rect, Texture2D icon, bool active, string tooltip, int count = 0)
        {
            Widgets.DrawBoxSolid(rect, RowButtonBgColor);
            if (active && Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }
            TooltipHandler.TipRegion(rect, tooltip);
            var clicked = false;
            if (active)
            {
                clicked = Widgets.ButtonImage(rect.ContractedBy(3f), icon);
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.3f);
                GUI.DrawTexture(rect.ContractedBy(3f), icon);
                GUI.color = Color.white;
            }
            if (count > 0)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.LowerRight;
                Widgets.Label(new Rect(rect.x, rect.y, rect.width + 6f, rect.height + 4f), count.ToString());
                Text.Anchor = TextAnchor.UpperLeft;
            }
            return clicked;
        }

        private void DrawQuirks(Rect rect, Pawn pawn, PawnWantsData data)
        {
            var curY = rect.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, curY, rect.width, 30f), "WQ_Quirks".Translate());
            Text.Font = GameFont.Small;
            DrawRewardsButton(new Rect(rect.xMax - 85f, curY + 3f, 85f, 24f), pawn);
            curY += 34f;

            var listRect = new Rect(rect.x, curY, rect.width, rect.height - (curY - rect.y));
            Widgets.DrawBoxSolid(listRect, QuirkContainerColor);

            var viewRect = new Rect(0f, 0f, listRect.width - 16f, data.quirks.Count * 45f);
            Widgets.BeginScrollView(listRect, ref quirksScrollPos, viewRect);

            var listY = 0f;
            for (int i = 0; i < data.quirks.Count; i++)
            {
                var quirk = data.quirks[i];
                var quirkRect = new Rect(5f, listY + 5f, viewRect.width, 40f);

                Widgets.DrawBoxSolid(quirkRect, QuirkBgColor);

                var iconRect = new Rect(quirkRect.x + 4f, quirkRect.y + 4f, 32f, 32f);
                Texture tex = quirk.def.requiresItem && quirk.item?.uiIcon != null ? quirk.item.uiIcon : quirk.def.Icon;
                if (quirk.def.requiresPawn && quirk.pawnTarget != null)
                {
                    tex = PortraitsCache.Get(quirk.pawnTarget, new Vector2(32f, 32f), Rot4.South, cameraZoom: 1.2f);
                }
                if (tex != null)
                {
                    GUI.DrawTexture(iconRect, tex);
                }

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(iconRect.xMax + 10f, quirkRect.y, quirkRect.width - 32f - 40f, quirkRect.height), quirk.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                if (Mouse.IsOver(quirkRect))
                {
                    TooltipHandler.TipRegion(quirkRect, quirk.Description);
                }

                var btnRect = new Rect(quirkRect.xMax - 20f, quirkRect.y + 2f, 20f, 20f);
                Text.Font = GameFont.Medium;
                GUI.color = Color.gray;
                if (Widgets.ButtonText(btnRect, "x", drawBackground: false))
                {
                    var qIndex = i;
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("WQ_ConfirmRemoveQuirk".Translate(quirk.LabelCap), () =>
                    {
                        quirk.def.Worker.OnRemoved(pawn, quirk);
                        data.quirks.RemoveAt(qIndex);
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }));
                    break;
                }
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                listY += 45f;
            }

            Widgets.EndScrollView();
        }

        // Lit while there is a reward point to spend; greyed and inert otherwise, except that it can always close an open panel.
        private void DrawRewardsButton(Rect rect, Pawn pawn)
        {
            var points = WantsAndQuirksUtility.RewardPointsToSpend(pawn);
            var active = points > 0 || rewardsOpen;
            if (points > 0)
            {
                var pulse = 0.55f + 0.3f * Mathf.Sin(Time.realtimeSinceStartup * 3f);
                Widgets.DrawBoxSolid(rect.ExpandedBy(2f), new Color(NewWantGlowColor.r, NewWantGlowColor.g, NewWantGlowColor.b, pulse));
            }
            TooltipHandler.TipRegion(rect, points > 0 ? "WQ_RewardsButtonTip".Translate(points) : "WQ_NoRewardPointsToSpend".Translate());
            if (!active)
            {
                GUI.color = Color.gray;
            }
            if (Widgets.ButtonText(rect, "WQ_Rewards".Translate(), active: active) && active)
            {
                rewardsOpen = !rewardsOpen;
                if (rewardsOpen)
                {
                    RewardBubbleField.PrepareNodes();
                    bubbles.InitPhysics();
                }
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            GUI.color = Color.white;
        }

        private void DrawRewards(Rect rect, Pawn pawn)
        {
            var points = WantsAndQuirksUtility.RewardPointsToSpend(pawn);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 30f), "WQ_Rewards".Translate());
            Text.Font = GameFont.Small;
            GUI.color = PointsColor;
            Widgets.Label(new Rect(rect.x, rect.y + 30f, rect.width, 24f), "WQ_RewardPoints".Translate(points));
            GUI.color = Color.white;

            var fieldRect = new Rect(rect.x, rect.y + 58f, rect.width, rect.height - 58f);
            Widgets.DrawBoxSolid(fieldRect, QuirkContainerColor);
            bubbles.Draw(fieldRect, true, node => ClaimForPawn(pawn, node), node => BlockedReason(pawn, node));
        }

        private string BlockedReason(Pawn pawn, RewardNode node)
        {
            if (!node.def.Worker.CanBestowOn(pawn, node.item, node.pawnTarget))
                return "WQ_CannotReceiveReward".Translate(pawn.LabelShort);
            if (WantsAndQuirksUtility.RewardPointsToSpend(pawn) <= 0)
                return "WQ_NoRewardPointsToSpend".Translate();
            return null;
        }

        private void ClaimForPawn(Pawn pawn, RewardNode node)
        {
            var reason = BlockedReason(pawn, node);
            if (reason != null)
            {
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                return;
            }
            WantsAndQuirksUtility.BestowReward(pawn, node);
            if (WantsAndQuirksUtility.RewardPointsToSpend(pawn) <= 0)
            {
                rewardsOpen = false;
            }
            else
            {
                bubbles.InitPhysics();
            }
        }
    }
}
