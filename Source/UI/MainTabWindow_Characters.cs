using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WantsAndQuirks
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class MainTabWindow_Characters : MainTabWindow
    {
        private Vector2 pawnListScrollPos;
        private static Color CenterBgColor = new ColorInt(53, 52, 52).ToColor;
        private static Color PawnRowColor = new ColorInt(40, 40, 40).ToColor;
        private static Color PawnBgColor = new ColorInt(91, 91, 91).ToColor;
        private static Color ProgressBarBgColor = new ColorInt(62, 58, 58).ToColor;
        private static Color ProgressBarFillColor = new ColorInt(63, 90, 114).ToColor;
        private static Color PointsColor = new ColorInt(166, 187, 194).ToColor;
        private readonly RewardBubbleField bubbles = new RewardBubbleField();
        private List<Pawn> tempPawns = new List<Pawn>();
        private Pawn selectedPawn;
        private const float ListWidth = 560f;
        private const float PawnRowHeight = 32f;
        private const float QuirkIconSize = 24f;

        private const float HelpWidth = 300f;

        public override Vector2 RequestedTabSize => new Vector2(1460f, 560f);

        public override void PreOpen()
        {
            base.PreOpen();
            selectedPawn = null;
            RemoveWantsWithNullPawns();
            RewardBubbleField.PrepareNodes();
            InitPhysics();
        }

        private void RemoveWantsWithNullPawns()
        {
            foreach (var p in Find.Maps.SelectMany(m => m.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)).Where(p => p.CanHaveWants()))
            {
                p.GetWantsData().activeWants.RemoveAll(w => w is ActiveWantWithPawnTarget tp && tp.targetPawn == null);
            }
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            bubbles.Update();
        }

        public void InitPhysics()
        {
            bubbles.InitPhysics();
        }

        public override void DoWindowContents(Rect inRect)
        {
            var leftRect = new Rect(inRect.x, inRect.y, ListWidth, inRect.height);
            var centerRect = new Rect(leftRect.xMax, inRect.y, inRect.width - ListWidth - HelpWidth, inRect.height);
            var rightRect = new Rect(centerRect.xMax, inRect.y, HelpWidth, inRect.height);

            DrawLeftPanel(leftRect);
            DrawCenterPanel(centerRect);
            DrawRightPanel(rightRect);
        }

        private void DrawRightPanel(Rect rect)
        {
            var innerRect = new Rect(rect.x + 20, rect.y + 10, rect.width - 20, rect.height - 10);
            Text.Font = GameFont.Small;
            Widgets.Label(innerRect, "WQ_QuirksInfoText".Translate());
        }

        private void DrawLeftPanel(Rect rect)
        {
            var innerRect = new Rect(rect.x, rect.y + 10, rect.width - 20, rect.height - 10);
            var curY = innerRect.y;

            if (!WantsAndQuirksMod.settings.pawnSpecificRewardPoints)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new Rect(innerRect.x, curY - 5, innerRect.width, 30f), "WQ_CharacterPoints".Translate());
                curY += 30f;

                var barRect = new Rect(innerRect.x, curY, innerRect.width, 24f);
                DrawCharacterPointsTracker(barRect, State.characterPoints, WantsAndQuirksUtility.GetGlobalCharacterPointsNeeded(), null);
                curY += 30f;

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(new Rect(innerRect.x, curY, innerRect.width, 40f), "WQ_ProgressBarDesc".Translate());
                curY += 40f;
            }

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(innerRect.x, curY, innerRect.width, 24f), "WQ_CharactersWithWants".Translate());
            curY += 24f;

            tempPawns.Clear();
            var maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                var pawns = maps[i].mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
                for (int j = 0; j < pawns.Count; j++)
                {
                    var p = pawns[j];
                    if (!p.CanHaveWants())
                        continue;
                    var pawnData = p.GetWantsData();
                    bool hasWants = pawnData.activeWants.Count > 0;
                    bool hasPoints = WantsAndQuirksMod.settings.pawnSpecificRewardPoints && pawnData.rewardPoints > 0;
                    if (hasWants || hasPoints)
                    {
                        tempPawns.Add(p);
                    }
                }
            }
            if (selectedPawn != null && !tempPawns.Contains(selectedPawn))
            {
                selectedPawn = null;
            }

            var listRect = new Rect(innerRect.x, curY, innerRect.width, innerRect.height - (curY - innerRect.y) + 12);
            var viewRect = new Rect(0, 0, listRect.width - 16f, (tempPawns.Count * (PawnRowHeight + 3f)) + 8);

            Widgets.DrawBoxSolid(listRect, PawnBgColor);
            Widgets.BeginScrollView(listRect, ref pawnListScrollPos, viewRect);
            var pY = 5f;

            for (int i = 0; i < tempPawns.Count; i++)
            {
                DrawPawnRow(new Rect(5, pY, viewRect.width - 5f, PawnRowHeight), tempPawns[i]);
                pY += PawnRowHeight + 3f;
            }
            Widgets.EndScrollView();
        }

        // One line per pawn: portrait, name and title, wants, reward points and quirk icons, each with its details in a tooltip.
        // A click selects or deselects the pawn; a double click jumps to them and opens their wants tab.
        private void DrawPawnRow(Rect rowRect, Pawn p)
        {
            var pData = p.GetWantsData();
            Widgets.DrawBoxSolid(rowRect, PawnRowColor);
            if (p == selectedPawn)
            {
                Widgets.DrawHighlightSelected(rowRect);
            }
            if (Mouse.IsOver(rowRect))
            {
                Widgets.DrawHighlight(rowRect);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (Event.current.clickCount == 2)
                    {
                        selectedPawn = p;
                        CameraJumper.TryJumpAndSelect(p);
                        InspectPaneUtility.OpenTab(typeof(ITab_Pawn_WantsAndQuirks));
                    }
                    else
                    {
                        selectedPawn = selectedPawn == p ? null : p;
                    }
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    Event.current.Use();
                }
            }

            GUI.DrawTexture(new Rect(rowRect.x + 2f, rowRect.y + 1f, 30f, 30f), PortraitsCache.Get(p, new Vector2(30f, 30f), Rot4.South, cameraZoom: 1.3f));

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            var x = rowRect.x + 38f;

            var title = p.story?.TitleShortCap;
            var name = title.NullOrEmpty() ? p.LabelShortCap : p.LabelShortCap + ", " + title;
            var nameRect = new Rect(x, rowRect.y, 170f, rowRect.height);
            Widgets.Label(nameRect, name.Truncate(nameRect.width));
            TooltipHandler.TipRegion(nameRect, p.LabelCap + (title.NullOrEmpty() ? "" : "\n" + p.story.TitleCap));
            x = nameRect.xMax + 6f;

            var wantsRect = new Rect(x, rowRect.y, 70f, rowRect.height);
            Widgets.Label(wantsRect, "WQ_WantsCount".Translate(pData.activeWants.Count));
            if (pData.activeWants.Count > 0)
            {
                TooltipHandler.TipRegion(wantsRect, string.Join("\n", pData.activeWants.Select(w => w.LabelCap)));
            }
            x = wantsRect.xMax + 6f;

            if (WantsAndQuirksMod.settings.pawnSpecificRewardPoints)
            {
                var pointsRect = new Rect(x, rowRect.y, 95f, rowRect.height);
                if (pData.rewardPoints > 0)
                {
                    GUI.color = PointsColor;
                }
                Widgets.Label(pointsRect, "WQ_PawnRewardPoints".Translate(pData.rewardPoints));
                GUI.color = Color.white;
                TooltipHandler.TipRegion(pointsRect, "WQ_CharacterPoints".Translate() + ": " + pData.characterPoints + " / " + WantsAndQuirksUtility.GetPawnCharacterPointsNeeded(pData));
                x = pointsRect.xMax + 6f;
            }
            Text.Anchor = TextAnchor.UpperLeft;

            var quirks = pData.quirks;
            var slots = Mathf.FloorToInt((rowRect.xMax - 4f - x) / (QuirkIconSize + 3f));
            var shown = quirks.Count > slots ? Mathf.Max(slots - 1, 0) : quirks.Count;
            var iconY = rowRect.y + (rowRect.height - QuirkIconSize) / 2f;
            for (int q = 0; q < shown; q++)
            {
                var quirk = quirks[q];
                var iconRect = new Rect(x, iconY, QuirkIconSize, QuirkIconSize);
                var tex = WantsAndQuirksUtility.QuirkIcon(quirk, QuirkIconSize);
                if (tex != null)
                {
                    GUI.DrawTexture(iconRect, tex);
                }
                TooltipHandler.TipRegion(iconRect, quirk.LabelCap.AsTipTitle() + "\n\n" + quirk.Description);
                x += QuirkIconSize + 3f;
            }
            if (shown < quirks.Count)
            {
                var moreRect = new Rect(x, rowRect.y, QuirkIconSize + 10f, rowRect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(moreRect, "+" + (quirks.Count - shown));
                Text.Anchor = TextAnchor.UpperLeft;
                TooltipHandler.TipRegion(moreRect, string.Join("\n", quirks.Skip(shown).Select(qk => qk.LabelCap)));
            }
        }

        private void DrawCenterPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, CenterBgColor);
            var curY = rect.y;
            if (selectedPawn != null || !WantsAndQuirksMod.settings.pawnSpecificRewardPoints)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperCenter;
                var header = selectedPawn != null
                    ? "WQ_GivingRewardsTo".Translate(selectedPawn.LabelShortCap, WantsAndQuirksUtility.RewardPointsToSpend(selectedPawn))
                    : "WQ_AvailableRewards".Translate(State.rewardPoints);
                if (selectedPawn != null)
                {
                    GUI.color = PointsColor;
                }
                Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), header);
                GUI.color = Color.white;
                curY += 24f;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            var physicsRect = new Rect(rect.x, curY, rect.width, rect.height - 50f - (curY - rect.y));
            if (selectedPawn != null)
            {
                bubbles.Draw(physicsRect, true, ClaimReward, node => BlockedReason(selectedPawn, node));
            }
            else
            {
                bubbles.Draw(physicsRect, true, ClaimReward);
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(new Rect(rect.x, rect.yMax - 50f, rect.width, 50f), "WQ_ClaimInstruction".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static string BlockedReason(Pawn pawn, RewardNode node) => WantsAndQuirksUtility.RewardBlockedReason(pawn, node);

        private void ClaimReward(RewardNode node)
        {
            if (selectedPawn != null)
            {
                var reason = BlockedReason(selectedPawn, node);
                if (reason != null)
                {
                    Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                    return;
                }
                WantsAndQuirksUtility.BestowReward(selectedPawn, node);
                InitPhysics();
                return;
            }

            var allCandidates = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction
                .Where(p => p.CanHaveWants() && node.def.Worker.CanBestowOn(p, node.item, node.pawnTarget)).ToList();

            List<Pawn> recipients;
            if (WantsAndQuirksMod.settings.pawnSpecificRewardPoints)
            {
                recipients = allCandidates.Where(p => p.GetWantsData().rewardPoints > 0).ToList();
                if (recipients.Count == 0 && allCandidates.Count > 0)
                {
                    Messages.Message("WQ_NoPawnRewardPoints".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }
            else
            {
                if (State.rewardPoints <= 0)
                {
                    Messages.Message("WQ_NotEnoughRewardPoints".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }
                recipients = allCandidates;
            }

            if (recipients.Count == 0)
            {
                Messages.Message("WQ_NoValidRecipients".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            DefsOf.WQ_BubbleClick.PlayOneShotOnCamera();
            Find.WindowStack.Add(new Dialog_BestowReward(node, this, recipients));
        }
        public static void DrawCharacterPointsTracker(Rect barRect, int points, int needed, Pawn pawn)
        {
            var fill = Mathf.Clamp01((float)points / needed);

            Widgets.DrawBoxSolid(barRect, ProgressBarBgColor);
            Widgets.DrawBoxSolid(new Rect(barRect.x, barRect.y, barRect.width * fill, barRect.height), ProgressBarFillColor);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(barRect.x + 5f, barRect.y, 100f, 24f), "0");
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(barRect.xMax - 105f, barRect.y, 100f, 24f), needed.ToString());
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, points.ToString());

            if (Prefs.DevMode && DebugSettings.godMode)
            {
                var lineHeight = Text.LineHeight;
                var rectPlus = new Rect(barRect.xMax - lineHeight, barRect.y - lineHeight, lineHeight, lineHeight);
                if (Widgets.ButtonImage(rectPlus.ContractedBy(4f), TexButton.Plus))
                {
                    WantsAndQuirksUtility.AddCharacterPoints(pawn, 100);
                }
                if (Mouse.IsOver(rectPlus))
                {
                    TooltipHandler.TipRegion(rectPlus, "+ 100");
                }
                var rectMinus = new Rect(rectPlus.xMin - lineHeight, barRect.y - lineHeight, lineHeight, lineHeight);
                if (Widgets.ButtonImage(rectMinus.ContractedBy(4f), TexButton.Minus))
                {
                    WantsAndQuirksUtility.AddCharacterPoints(pawn, -100);
                }
                if (Mouse.IsOver(rectMinus))
                {
                    TooltipHandler.TipRegion(rectMinus, "- 100");
                }
            }
        }
    }
}
