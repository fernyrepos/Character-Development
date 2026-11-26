using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WantsAndQuirks
{
    // The reward bubbles (State.rewardNodes) with their physics, drag and click handling, shared by the Characters menu and the wants tab.
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class RewardBubbleField
    {
        private static Texture2D BubbleTex = ContentFinder<Texture2D>.Get("UI/Bubble");
        private static Color LegendaryBubbleColor = new Color(0.8f, 0.7f, 0.3f, 0.85f);
        private static Color RareBubbleColor = new Color(0.6f, 0.4f, 0.8f, 0.85f);
        private static Color UncommonBubbleColor = new Color(0.3f, 0.6f, 0.8f, 0.85f);
        private static Color CommonBubbleColor = new Color(0.4f, 0.4f, 0.4f, 0.85f);
        private static Color NewRewardGlowColor = new Color(1f, 0.85f, 0.35f, 1f);
        private static Color BlockedBubbleTint = new Color(1f, 1f, 1f, 0.35f);
        private const float NewRewardGlowDuration = 2.5f;
        private const float LabelOverhang = 15f;

        private float physicsTemperature = 0f;
        private RewardNode draggedNode;
        private Vector2 dragStartMousePos;
        private bool wasDraggingNode;
        private bool hasSignificantDrag;
        // Another look's rarity colors; null keeps the classic ones.
        public Func<RewardRarity, Color> rarityColor;
        private float fitScale = 1f;
        private bool snapFitScale = true;

        // Drops bubbles whose target pawn is gone and fills an empty pool.
        public static void PrepareNodes()
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
            if (State.rewardNodes.Count == 0)
            {
                WantsAndQuirksUtility.GenerateGlobalRewardBubbles();
            }
        }

        public void Update()
        {
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
            snapFitScale = true;
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

        // Draws the bubbles centered in rect. With fitToRect the field is scaled down (never up) so every bubble and label stays inside.
        // blockedReason returns null for a claimable bubble, or the tooltip line explaining why it can't be claimed.
        public void Draw(Rect rect, bool fitToRect, Action<RewardNode> onClick, Func<RewardNode, string> blockedReason = null)
        {
            var center = new Vector2(rect.width / 2f, rect.height / 2f);
            if (fitToRect)
            {
                if (snapFitScale)
                {
                    fitScale = TargetFitScale(center);
                    snapFitScale = false;
                }
                else if (Event.current.type == EventType.Repaint)
                {
                    fitScale = Mathf.Lerp(fitScale, TargetFitScale(center), 0.2f);
                }
            }
            else
            {
                fitScale = 1f;
            }
            var scale = fitScale;

            GUI.BeginGroup(rect);
            var nodes = State.rewardNodes;

            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                var node = nodes[i];
                var nodeRect = NodeRect(node, center, scale, out _, out _);

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
                    draggedNode.pos = (Event.current.mousePosition - center) / scale;
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
                        onClick(draggedNode);
                    }
                    draggedNode = null;
                    wasDraggingNode = false;
                    hasSignificantDrag = false;
                    Event.current.Use();
                }
            }

            foreach (var node in nodes)
            {
                var nodeRect = NodeRect(node, center, scale, out var nodeCenter, out var r);
                var blocked = blockedReason?.Invoke(node);

                if (!node.hasBeenViewed)
                {
                    node.hasBeenViewed = true;
                    node.glowStartRealTime = Time.realtimeSinceStartup;
                }

                if (node.glowStartRealTime >= 0f)
                {
                    var glowAge = Time.realtimeSinceStartup - node.glowStartRealTime;
                    if (glowAge < NewRewardGlowDuration)
                    {
                        var glowT = glowAge / NewRewardGlowDuration;
                        var glowRadius = r * (1f + glowT * 0.6f);
                        var glowRect = new Rect(nodeCenter.x - glowRadius, nodeCenter.y - glowRadius, glowRadius * 2f, glowRadius * 2f);
                        GUI.color = new Color(NewRewardGlowColor.r, NewRewardGlowColor.g, NewRewardGlowColor.b, (1f - glowT) * 0.8f);
                        GUI.DrawTexture(glowRect, BubbleTex);
                        GUI.color = Color.white;
                    }
                }

                var tint = blocked != null ? BlockedBubbleTint : Color.white;
                GUI.color = (rarityColor != null ? rarityColor(node.def.rarity) : GetBubbleColor(node.def.rarity)) * tint;
                if (blocked == null && Mouse.IsOver(nodeRect))
                {
                    GUI.color = Color.Lerp(GUI.color, Color.white, 0.3f);
                }
                GUI.DrawTexture(nodeRect, BubbleTex);
                GUI.color = tint;

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
                var textRect = new Rect(nodeRect.x - LabelOverhang, nodeCenter.y - 5, nodeRect.width + LabelOverhang * 2f, (nodeRect.height / 2f) + 10);
                Text.Font = GameFont.Tiny;
                Widgets.Label(textRect, node.LabelCap);
                GUI.color = Color.white;
                var tip = $"{node.LabelCap}\n\n{node.Description}";
                if (blocked != null)
                {
                    tip += "\n\n" + blocked.Colorize(ColoredText.WarningColor);
                }
                TooltipHandler.TipRegion(nodeRect, tip);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            GUI.EndGroup();
            Text.Font = GameFont.Small;
        }

        private Rect NodeRect(RewardNode node, Vector2 center, float scale, out Vector2 nodeCenter, out float r)
        {
            r = GetRadius(node.def.rarity) * scale;
            nodeCenter = new Vector2(center.x + node.drawPos.x * scale, center.y + node.drawPos.y * scale);
            return new Rect(nodeCenter.x - r, nodeCenter.y - r, r * 2f, r * 2f);
        }

        // Largest scale (at most 1) at which every bubble and its label fits within halfSize of the center.
        private float TargetFitScale(Vector2 halfSize)
        {
            var maxX = 0f;
            var maxY = 0f;
            foreach (var node in State.rewardNodes)
            {
                var r = GetRadius(node.def.rarity);
                maxX = Mathf.Max(maxX, Mathf.Abs(node.drawPos.x) + r + LabelOverhang);
                maxY = Mathf.Max(maxY, Mathf.Abs(node.drawPos.y) + r);
            }
            var scale = 1f;
            if (maxX > 0f)
                scale = Mathf.Min(scale, halfSize.x / maxX);
            if (maxY > 0f)
                scale = Mathf.Min(scale, halfSize.y / maxY);
            return Mathf.Max(scale, 0.1f);
        }

        public static float GetRadius(RewardRarity rarity)
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

        private static Color GetBubbleColor(RewardRarity rarity)
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
    }
}
