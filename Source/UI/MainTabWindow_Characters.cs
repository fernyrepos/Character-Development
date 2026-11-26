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
        private readonly RewardBubbleField bubbles = new RewardBubbleField();
        private List<Pawn> tempPawns = new List<Pawn>();

        public override Vector2 RequestedTabSize => new Vector2(1200f, 500f);

        public override void PreOpen()
        {
            base.PreOpen();
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
            var leftRect = new Rect(inRect.x, inRect.y, 300f, inRect.height);
            var centerRect = new Rect(leftRect.xMax, inRect.y, inRect.width - 600f, inRect.height);
            var rightRect = new Rect(centerRect.xMax, inRect.y, 300f, inRect.height);

            DrawLeftPanel(leftRect);
            DrawCenterPanel(centerRect);
            DrawRightPanel(rightRect);
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
            var listRect = new Rect(innerRect.x, curY, innerRect.width, innerRect.height - (curY - innerRect.y) + 12);
            var viewRect = new Rect(0, 0, listRect.width - 16f, (tempPawns.Count * 38f) + 8);

            Widgets.DrawBoxSolid(listRect, PawnBgColor);
            Widgets.BeginScrollView(listRect, ref pawnListScrollPos, viewRect);
            var pY = 5f;

            for (int i = 0; i < tempPawns.Count; i++)
            {
                var p = tempPawns[i];
                var rowRect = new Rect(5, pY, viewRect.width, 35f);
                Widgets.DrawBoxSolid(rowRect, PawnRowColor);
                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                if (Widgets.ButtonInvisible(rowRect))
                {
                    CameraJumper.TryJumpAndSelect(p);
                    InspectPaneUtility.OpenTab(typeof(ITab_Pawn_WantsAndQuirks));
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }

                GUI.DrawTexture(new Rect(rowRect.x + 2f, rowRect.y + 2f, 35f, 35f), PortraitsCache.Get(p, new Vector2(35f, 35f), Rot4.South, cameraZoom: 1.3f));

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(rowRect.x + 40f, rowRect.y, 150f, 35f), p.LabelShort);

                Text.Anchor = TextAnchor.MiddleRight;
                if (WantsAndQuirksMod.settings.pawnSpecificRewardPoints)
                {
                    var pData = p.GetWantsData();
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(new Rect(rowRect.xMax - 130f, rowRect.y, 120f, 18f), "WQ_WantsCount".Translate(pData.activeWants.Count));
                    Widgets.Label(new Rect(rowRect.xMax - 130f, rowRect.y + 17f, 120f, 18f), "WQ_PawnRewardPoints".Translate(pData.rewardPoints));
                    Text.Font = GameFont.Small;
                }
                else
                {
                    Widgets.Label(new Rect(rowRect.xMax - 100f, rowRect.y, 90f, 35f), "WQ_WantsCount".Translate(p.GetWantsData().activeWants.Count));
                }
                Text.Anchor = TextAnchor.UpperLeft;

                pY += 38f;
            }
            Widgets.EndScrollView();
        }

        private void DrawCenterPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, CenterBgColor);
            var curY = rect.y;
            if (!WantsAndQuirksMod.settings.pawnSpecificRewardPoints)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), "WQ_AvailableRewards".Translate(State.rewardPoints));
                curY += 24f;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            var physicsRect = new Rect(rect.x, curY, rect.width, rect.height - 50f - (curY - rect.y));
            bubbles.Draw(physicsRect, false, ClaimReward);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(new Rect(rect.x, rect.yMax - 50f, rect.width, 50f), "WQ_ClaimInstruction".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawRightPanel(Rect rect)
        {
            var innerRect = new Rect(rect.x + 20, rect.y + 10, rect.width - 20, rect.height - 10);
            Text.Font = GameFont.Small;
            Widgets.Label(innerRect, "WQ_QuirksInfoText".Translate());
        }

        private void ClaimReward(RewardNode node)
        {
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
