using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WantsAndQuirks
{
    [HotSwappable]
    public class Dialog_BestowReward : Window
    {
        private readonly RewardNode node;
        private readonly MainTabWindow_Characters parentWindow;
        private readonly List<Pawn> recipients;
        private Vector2 scrollPos;

        public override Vector2 InitialSize => new Vector2(450f, 600f);

        public Dialog_BestowReward(RewardNode node, MainTabWindow_Characters parentWindow, List<Pawn> recipients)
        {
            this.node = node;
            this.parentWindow = parentWindow;
            this.recipients = recipients;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            closeOnCancel = true;
            closeOnAccept = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "WQ_BestowReward".Translate());
            Text.Font = GameFont.Small;

            var rewardName = node.LabelCap;
            var description = "WQ_BestowRewardDesc".Translate(rewardName.Colorize(ColorLibrary.SkyBlue));
            var descHeight = Text.CalcHeight(description, inRect.width);
            Widgets.Label(new Rect(0f, 35f, inRect.width, descHeight), description);

            var listTop = 35f + descHeight + 10f;
            var listRect = new Rect(0f, listTop, inRect.width, inRect.height - listTop - 50f);
            var rowHeight = 50f;
            var viewRect = new Rect(0f, 0f, listRect.width - 16f, recipients.Count * rowHeight);

            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);
            var curY = 0f;
            foreach (var p in recipients)
            {
                var rowRect = new Rect(0f, curY, viewRect.width, rowHeight - 4f);

                if (Widgets.ButtonInvisible(rowRect))
                {
                    State.rewardPoints--;
                    if (WantsAndQuirksMod.settings.pawnSpecificRewardPoints)
                    {
                        var pData = p.GetWantsData();
                        if (pData.rewardPoints > 0)
                        {
                            pData.rewardPoints--;
                        }
                    }
                    WantsAndQuirksUtility.AddQuirk(p, node.def, node.item, node.pawnTarget);
                    SoundDefOf.Quest_Succeded.PlayOneShotOnCamera();

                    if (WantsAndQuirksMod.settings.rerollBubblesOnSelection)
                    {
                        WantsAndQuirksUtility.GenerateGlobalRewardBubbles();
                    }
                    else
                    {
                        State.rewardNodes.Remove(node);
                        var replacement = WantsAndQuirksUtility.GenerateSingleRewardBubble(State.rewardNodes);
                        if (replacement != null)
                        {
                            State.rewardNodes.Add(replacement);
                        }
                    }
                    parentWindow.InitPhysics();
                    Close();
                    break;
                }

                Widgets.DrawOptionBackground(rowRect, false);

                var portraitRect = new Rect(rowRect.x + 4f, rowRect.y + 3f, 40f, 40f);
                var tex = PortraitsCache.Get(p, new Vector2(40f, 40f), Rot4.South);
                GUI.DrawTexture(portraitRect, tex);

                Text.Anchor = TextAnchor.MiddleLeft;
                var labelRect = new Rect(portraitRect.xMax + 10f, rowRect.y, rowRect.width - portraitRect.width - 20f, rowRect.height);
                Widgets.Label(labelRect, p.LabelShortCap);
                Text.Anchor = TextAnchor.UpperLeft;

                curY += rowHeight;
            }
            Widgets.EndScrollView();

            if (Widgets.ButtonText(new Rect(inRect.width - 100f, inRect.height - 40f, 100f, 35f), "CloseButton".Translate()))
            {
                Close();
            }
        }
    }
}
