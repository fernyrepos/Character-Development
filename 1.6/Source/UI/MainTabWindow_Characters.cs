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
        private float physicsTemperature = 0f;
        private static Texture2D BubbleTex = ContentFinder<Texture2D>.Get("UI/Bubble");
        private static Color CenterBgColor = new ColorInt(53, 52, 52).ToColor;
        private static Color PawnRowColor = new ColorInt(40, 40, 40).ToColor;
        private static Color PawnBgColor = new ColorInt(91, 91, 91).ToColor;
        private static Color ProgressBarBgColor = new ColorInt(62, 58, 58).ToColor;
        private static Color ProgressBarFillColor = new ColorInt(63, 90, 114).ToColor;
        private static Color LegendaryBubbleColor = new Color(0.8f, 0.7f, 0.3f, 0.85f);
        private static Color RareBubbleColor = new Color(0.6f, 0.4f, 0.8f, 0.85f);
        private static Color UncommonBubbleColor = new Color(0.3f, 0.6f, 0.8f, 0.85f);
        private static Color CommonBubbleColor = new Color(0.4f, 0.4f, 0.4f, 0.85f);
        private RewardNode draggedNode;
        private Vector2 dragStartMousePos;
        private bool wasDraggingNode;
        private bool hasSignificantDrag;
        private List<Pawn> tempPawns = new List<Pawn>();

        public override Vector2 RequestedTabSize => new Vector2(1200f, 500f);

        public override void PreOpen()
        {
            base.PreOpen();
            ValidateRewardNodes();
            RemoveWantsWithNullPawns();
            if (State.rewardNodes.Count == 0)
            {
                WantsAndQuirksUtility.GenerateGlobalRewardBubbles();
            }
            InitPhysics();
        }

        private void ValidateRewardNodes()
        {
            foreach (var node in State.rewardNodes.Where(n => n.def.requiresPawn && n.pawnTarget == null).ToList())
            {
                State.rewardNodes.Remove(node);
                var replacement = WantsAndQuirksUtility.GenerateSingleRewardBubble(State.rewardNodes);
                if (replacement != null)
                {
                    State.rewardNodes.Add(replacement);
                }
            }
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

            if (wasDraggingNode && hasSignificantDrag)
            {
                physicsTemperature = Mathf.Max(physicsTemperature, 100f);
            }

            PhysicsTick(Time.deltaTime);

            var nodes = State.rewardNodes;
            foreach (var node in nodes)
            {
                if (node == draggedNode)
                {
                    node.drawPos = node.pos;
                    node.dampVelocity = Vector2.zero;
                }
                else
                {
                    node.drawPos = Vector2.SmoothDamp(node.drawPos, node.pos, ref node.dampVelocity, 0.05f);
                }
            }

            if (physicsTemperature <= 0.5f)
            {
                foreach (var node in nodes)
                {
                    node.drawPos = node.pos;
                }
            }
        }

        public void InitPhysics()
        {
            physicsTemperature = 200f;
            var nodes = State.rewardNodes;
            var nodeCount = nodes.Count;
            if (nodeCount == 0)
                return;

            for (int i = 0; i < 5000; i++)
            {
                var velSum = PhysicsTick(0.04f, true);

                if (i > 150 && (velSum / nodeCount) < 0.001f)
                {
                    break;
                }
            }

            physicsTemperature = 0f;
            foreach (var node in nodes)
            {
                node.velocity = Vector2.zero;
                node.drawPos = node.pos;
            }
        }

        private float PhysicsTick(float dt, bool ignoreSettings = false)
        {
            if (physicsTemperature < 0.01f)
            { physicsTemperature = 0f; return 0f; }

            var velocitySum = 0f;
            var nodes = State.rewardNodes;
            var nodeCount = nodes.Count;

            var k_rep = 500f * 1.634f;
            var baseRep = (k_rep * k_rep) * 0.2f;

            for (int i = 0; i < nodeCount; i++)
            {
                var node = nodes[i];
                if (node == draggedNode)
                {
                    node.velocity = Vector2.zero;
                    continue;
                }

                var nx = node.pos.x;
                var ny = node.pos.y;

                if (float.IsNaN(nx) || float.IsNaN(ny))
                {
                    nx = Rand.Range(-10f, 10f);
                    ny = Rand.Range(-10f, 10f);
                    node.pos = new Vector2(nx, ny);
                    nx = node.pos.x;
                    ny = node.pos.y;
                }

                var repX = 0f;
                var repY = 0f;
                var nodeRadius = GetRadius(node.def.rarity);

                for (int j = 0; j < nodeCount; j++)
                {
                    if (i == j)
                        continue;
                    var other = nodes[j];
                    var dx = nx - other.pos.x;
                    var dy = ny - other.pos.y;
                    var distSq = dx * dx + dy * dy;

                    if (distSq < 1f)
                    {
                        dx = Rand.Value - 0.5f;
                        dy = Rand.Value - 0.5f;
                        if (dx == 0f && dy == 0f)
                            dx = 0.01f;
                        distSq = dx * dx + dy * dy;
                    }

                    var forceMagSq = baseRep * 1f / distSq;
                    forceMagSq *= 0.15f;

                    var minDist = nodeRadius + GetRadius(other.def.rarity) + 15f;
                    var minDistSq = minDist * minDist;

                    if (distSq < minDistSq)
                    {
                        var dist = Mathf.Sqrt(distSq);
                        forceMagSq += (minDist - dist) * 2000f / dist;
                    }

                    repX += dx * forceMagSq;
                    repY += dy * forceMagSq;
                }

                var cx = -nx * 10f;
                var cy = -ny * 10f;

                var vx = node.velocity.x;
                var vy = node.velocity.y;

                vx = (vx + (repX + cx) * dt) * 0.75f;
                vy = (vy + (repY + cy) * dt) * 0.75f;

                var speedSq = vx * vx + vy * vy;
                var physTempSq = physicsTemperature * physicsTemperature;

                if (speedSq > physTempSq)
                {
                    var speed = Mathf.Sqrt(speedSq);
                    var shrink = physicsTemperature / speed;
                    vx *= shrink;
                    vy *= shrink;
                    speedSq = physTempSq;
                }
                else if (speedSq < 0.0025f && !ignoreSettings)
                {
                    vx = 0f;
                    vy = 0f;
                    speedSq = 0f;
                }

                node.velocity = new Vector2(vx, vy);
                velocitySum += speedSq;
            }

            for (int i = 0; i < nodeCount; i++)
            {
                var node = nodes[i];
                if (node != draggedNode)
                {
                    node.pos.x += node.velocity.x * dt * 8f;
                    node.pos.y += node.velocity.y * dt * 8f;
                }
            }

            if (ignoreSettings)
                physicsTemperature *= 0.995f;
            else
                physicsTemperature *= 0.98f;

            return velocitySum;
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
            var center = new Vector2(physicsRect.width / 2f, physicsRect.height / 2f);

            GUI.BeginGroup(physicsRect);
            var nodes = State.rewardNodes;

            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                var node = nodes[i];
                var r = GetRadius(node.def.rarity);
                var nodeCenter = new Vector2(center.x + node.drawPos.x, center.y + node.drawPos.y);
                var nodeRect = new Rect(nodeCenter.x - r, nodeCenter.y - r, r * 2f, r * 2f);

                if (Mouse.IsOver(nodeRect))
                {
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        draggedNode = node;
                        wasDraggingNode = true;
                        hasSignificantDrag = false;
                        dragStartMousePos = Event.current.mousePosition;
                        Event.current.Use();
                        break;
                    }
                }
            }

            if (wasDraggingNode && draggedNode != null)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    hasSignificantDrag = true;
                    draggedNode.pos = Event.current.mousePosition - center;
                    draggedNode.drawPos = draggedNode.pos;
                    draggedNode.velocity = Vector2.zero;
                    draggedNode.dampVelocity = Vector2.zero;
                    physicsTemperature = Mathf.Max(physicsTemperature, 100f);
                    Event.current.Use();
                }
                if (Event.current.rawType == EventType.MouseUp)
                {
                    if (!hasSignificantDrag && Vector2.Distance(Event.current.mousePosition, dragStartMousePos) < 5f)
                    {
                        ClaimReward(draggedNode);
                    }
                    draggedNode = null;
                    wasDraggingNode = false;
                    hasSignificantDrag = false;
                    Event.current.Use();
                }
            }

            foreach (var node in nodes)
            {
                var r = GetRadius(node.def.rarity);
                var nodeCenter = new Vector2(center.x + node.drawPos.x, center.y + node.drawPos.y);
                var nodeRect = new Rect(nodeCenter.x - r, nodeCenter.y - r, r * 2f, r * 2f);

                GUI.color = GetBubbleColor(node.def.rarity);
                if (Mouse.IsOver(nodeRect))
                {
                    GUI.color = Color.Lerp(GUI.color, Color.white, 0.3f);
                }
                GUI.DrawTexture(nodeRect, BubbleTex);
                GUI.color = Color.white;

                var iconSize = r * (0.6f + (r / 140f));
                var iconRect = new Rect(nodeCenter.x - iconSize / 2f, nodeCenter.y - iconSize * 0.65f, iconSize, iconSize);
                if (node.def.requiresItem && node.item != null && node.item.uiIcon != null)
                {
                    GUI.DrawTexture(iconRect, node.item.uiIcon);
                }
                else if (node.def.requiresPawn && node.pawnTarget != null)
                {
                    GUI.DrawTexture(iconRect, PortraitsCache.Get(node.pawnTarget, new Vector2(iconSize, iconSize), Rot4.South, cameraZoom: 1.2f));
                }
                else if (node.def.Icon != BaseContent.WhiteTex && node.def.Icon != null)
                {
                    GUI.DrawTexture(iconRect, node.def.Icon);
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                var textRect = new Rect(nodeRect.x - 15, nodeCenter.y - 5, nodeRect.width + 30, (nodeRect.height / 2f) + 10);
                Text.Font = GameFont.Tiny;
                Widgets.Label(textRect, node.LabelCap);
                TooltipHandler.TipRegion(nodeRect, $"{node.LabelCap}\n\n{node.Description}");
                Text.Anchor = TextAnchor.UpperLeft;
            }
            GUI.EndGroup();

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

        private float GetRadius(RewardRarity rarity)
        {
            switch (rarity)
            {
                case RewardRarity.Legendary:
                    return 63f;
                case RewardRarity.Rare:
                    return 49;
                case RewardRarity.Uncommon:
                    return 42;
                case RewardRarity.Common:
                default:
                    return 32;
            }
        }

        private Color GetBubbleColor(RewardRarity rarity)
        {
            switch (rarity)
            {
                case RewardRarity.Legendary:
                    return LegendaryBubbleColor;
                case RewardRarity.Rare:
                    return RareBubbleColor;
                case RewardRarity.Uncommon:
                    return UncommonBubbleColor;
                case RewardRarity.Common:
                default:
                    return CommonBubbleColor;
            }
        }

        private void ClaimReward(RewardNode node)
        {
            if (State.rewardPoints <= 0)
            {
                Messages.Message("WQ_NotEnoughRewardPoints".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            var allCandidates = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction
                .Where(p => p.CanHaveWants() && node.def.Worker.CanBestowOn(p, node.item, node.pawnTarget)).ToList();

            List<Pawn> recipients;
recipients = allCandidates.Where(p => p.GetWantsData().rewardPoints > 0).ToList();
if (recipients.Count == 0 && allCandidates.Count > 0)
{
    Messages.Message("WQ_NoPawnRewardPoints".Translate(), MessageTypeDefOf.RejectInput, false);
    return;
}
            }
            else
            {
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
