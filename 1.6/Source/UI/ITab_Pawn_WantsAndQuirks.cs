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

        private static Color BgColor = new ColorInt(28, 30, 31).ToColor;
        private static Color WantBgColor = new ColorInt(79, 82, 84).ToColor;
        private static Color QuirkContainerColor = new ColorInt(38, 37, 35).ToColor;
        private static Color QuirkBgColor = new ColorInt(70, 68, 66).ToColor;
        private static Color PointsColor = new ColorInt(166, 187, 194).ToColor;
        private static Color MentalBreakRectColor = new ColorInt(57, 45, 45).ToColor;
        private static Color MentalBreakTextColor = new ColorInt(184, 133, 134).ToColor;

        public ITab_Pawn_WantsAndQuirks()
        {
            labelKey = "WQ_Wants";
            size = new Vector2(600f, 413f);
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

        public override void FillTab()
        {
            var pawn = SelPawn;
            size = new Vector2(600f, 413f);
            var data = pawn.GetWantsData();
            var rect = new Rect(0f, 0f, size.x, size.y);

            Widgets.DrawBoxSolid(rect, BgColor);
            rect = rect.ContractedBy(10f);

            var leftRect = new Rect(rect.x, rect.y, rect.width * 0.70f, rect.height);
            var rightRect = new Rect(leftRect.xMax, rect.y, rect.width * 0.30f, rect.height);

            DrawWants(leftRect, pawn, data);
            DrawQuirks(rightRect, pawn, data);
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
                var needed = WantsAndQuirksMod.settings.pointsNeededForReward;
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new Rect(rect.x, curY, rect.width, 30f), "WQ_CharacterPoints".Translate());
                curY += 30f;

                var barRect = new Rect(rect.x, curY, rect.width, 24f);
                MainTabWindow_Characters.DrawCharacterPointsTracker(barRect, data.characterPoints, needed, pawn);
                curY += 30f;

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x, curY, rect.width, 20f), "WQ_PawnUnlockedModifiers".Translate(data.rewardPoints));
                GUI.color = Color.white;
                curY += 24f;
            }

            if (data.activeWants.Count == 0)
            {
                Text.Font = GameFont.Small;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(rect.x, curY, rect.width, 30f), "WQ_NoActiveWants".Translate());
                GUI.color = Color.white;
                return;
            }

            var outRect = new Rect(rect.x, curY, rect.width, rect.height - (curY - rect.y));
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, data.activeWants.Count * 85f);
            viewRect.height -= 5f;
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
                var wantRect = new Rect(0f, listY, viewRect.width, 80f);

                Widgets.DrawBoxSolid(wantRect, want.def.isMentalBreakWant ? MentalBreakRectColor : WantBgColor);

                var iconRect = new Rect(wantRect.x + 10f, wantRect.y + 20f, 50f, 50f);
                GUI.color = new Color(1f, 1f, 1f, 0.8f);
                var tex = want.Icon;
                if (want is ActiveWantWithPawnTarget pt && pt.targetPawn != null)
                {
                    tex = PortraitsCache.Get(pt.targetPawn, new Vector2(50f, 50f), Rot4.South, cameraZoom: 1.2f);
                }
                if (tex != null)
                {
                    GUI.DrawTexture(iconRect, tex);
                }
                GUI.color = Color.white;

                var textRect = new Rect(iconRect.xMax + 15f, wantRect.y + 5f, wantRect.width - 70f - 30f, 22f);
                Text.Font = GameFont.Small;
                Widgets.Label(textRect, $"<i>{want.LabelCap}</i>");

                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                var descRect = new Rect(textRect.x, textRect.yMax, textRect.width, 32f);
                Widgets.Label(descRect, want.Description);
                GUI.color = Color.white;

                var infoRect = new Rect(textRect.x, descRect.yMax, textRect.width, 25f);
                if (want.def.isMentalBreakWant)
                {
                    GUI.color = MentalBreakTextColor;
                    Widgets.Label(infoRect, "WQ_CausedByMentalBreak".Translate());
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = PointsColor;
                    Widgets.Label(infoRect, "WQ_OnCompletion".Translate() + " " + "WQ_CharacterPointsReward".Translate(want.def.reward));
                    GUI.color = Color.white;
                }
                Text.Anchor = TextAnchor.UpperLeft;

                var btnRect = new Rect(wantRect.xMax - 25f, wantRect.y + 5f, 20f, 20f);
                if (!want.def.isMentalBreakWant)
                {
                    if (WantsAndQuirksMod.settings.rerollsPerWant > 0 && want.rerollCount < WantsAndQuirksMod.settings.rerollsPerWant)
                    {
                        var rerollRect = new Rect(btnRect.x - 25f, btnRect.y, 20f, 20f);
                        if (Widgets.ButtonImage(rerollRect, ContentFinder<Texture2D>.Get("UI/Reroll")))
                        {
                            WantsAndQuirksUtility.RerollWant(pawn, data, want);
                            DefsOf.WQ_RerollSound.PlayOneShotOnCamera();
                        }
                        TooltipHandler.TipRegion(rerollRect, "WQ_RerollWantWithCount".Translate(WantsAndQuirksMod.settings.rerollsPerWant - want.rerollCount));
                    }

                    Text.Font = GameFont.Medium;
                    GUI.color = Color.gray;
                    if (Widgets.ButtonText(btnRect, "X", drawBackground: false))
                    {
                        data.activeWants.RemoveAt(i);
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        break;
                    }
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }

                ReorderableWidget.Reorderable(reorderableGroupID, wantRect);

                if (i == data.activeWants.Count - 1)
                {
                    listY += 80f;
                }
                else
                {
                    listY += 85f;
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawQuirks(Rect rect, Pawn pawn, PawnWantsData data)
        {
            var curY = rect.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, curY, rect.width, 30f), "WQ_Quirks".Translate());
            curY += 30f;
            Text.Font = GameFont.Small;

            var redirectBtnRect = new Rect(rect.x, curY, rect.width, 24f);
            if (Widgets.ButtonText(redirectBtnRect, "WQ_AddQuirks".Translate()))
            {
                Find.MainTabsRoot.SetCurrentTab(DefsOf.WQ_CharactersMenu);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            curY += 28f;

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
    }
}
