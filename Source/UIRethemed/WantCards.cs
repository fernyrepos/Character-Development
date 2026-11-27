using System.Runtime.CompilerServices;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;
using Verse.Sound;
using WantsAndQuirks;

namespace WantsAndQuirksUIRethemed
{
    // Want cards and quirk rows, shared by the themed wants tab and the themed Characters menu.
    [StaticConstructorOnStartup]
    internal static class WantCards
    {
        public const float CardHeight = 58f;
        public const float CardGap = 4f;
        public const float QuirkHeight = 36f;
        public const float QuirkGap = 4f;
        private const float ControlSize = 24f;
        private const float ControlGap = 4f;
        private const float NewGlowDuration = 2.5f;

        public static readonly Texture2D StarTex = CoreWidgets.Icon("star");
        public static readonly Texture2D RerollTex = CoreWidgets.Icon("arrows-clockwise");
        public static readonly Texture2D DismissTex = CoreWidgets.Icon("x");
        public static readonly Texture2D WarningTex = CoreWidgets.WarningTex;

        // Wants lists the player has reordered by hand; they keep that order until a new want arrives. Game lifetime: the data is the key.
        private static readonly ConditionalWeakTable<PawnWantsData, object> handOrdered = new ConditionalWeakTable<PawnWantsData, object>();
        private static readonly object Marker = new object();

        public static void MarkHandOrdered(PawnWantsData data)
        {
            handOrdered.Remove(data);
            handOrdered.Add(data, Marker);
        }

        // Sorts the wants lowest reward first unless the player has reordered them; a new want brings the sort back. Stable, in place.
        public static void SortByReward(PawnWantsData data)
        {
            var wants = data.activeWants;
            for (int i = 0; i < wants.Count; i++)
            {
                if (!wants[i].hasBeenViewed)
                {
                    handOrdered.Remove(data);
                    break;
                }
            }
            if (handOrdered.TryGetValue(data, out _))
                return;
            for (int i = 1; i < wants.Count; i++)
            {
                var want = wants[i];
                var j = i - 1;
                while (j >= 0 && wants[j].def.reward > want.def.reward)
                {
                    wants[j + 1] = wants[j];
                    j--;
                }
                wants[j + 1] = want;
            }
        }

        // One want: icon square, title, progress or how long it has been wanted, then reward, reroll and dismiss as one row of same-size controls. Returns true when dismissed.
        public static bool Draw(Rect card, Pawn pawn, PawnWantsData data, ActiveWant want)
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

            // Controls, right to left: dismiss, reroll, reward
            var controlY = Mathf.Round(card.center.y - ControlSize / 2f);
            var right = card.xMax - 8f;
            var dismissed = false;
            if (!mental)
            {
                var dismissRect = new Rect(right - ControlSize, controlY, ControlSize, ControlSize);
                if (CoreWidgets.HeaderTool(dismissRect, DismissTex, "WQ_DismissWant".Translate(), null, false, Theme.HighlightRed, true, null))
                {
                    dismissed = true;
                }
                right = dismissRect.x - ControlGap;

                var maxRerolls = WantsAndQuirksMod.settings.rerollsPerWant;
                if (maxRerolls > 0)
                {
                    var left = Mathf.Max(0, maxRerolls - want.rerollCount);
                    var label = left.ToString();
                    var width = CoreWidgets.HeaderToolWidth(label);
                    var rerollRect = new Rect(right - width, controlY, width, ControlSize);
                    var rerollTip = left > 0 ? "WQ_RerollWantWithCount".Translate(left).Resolve() : "WQ_NoRerollsLeft".Translate().Resolve();
                    if (CoreWidgets.HeaderTool(rerollRect, RerollTex, "WQ_UIR_Reroll".Translate(), rerollTip, false, Theme.HighlightBlue, left > 0, label) && !dismissed)
                    {
                        WantsAndQuirksUtility.RerollWant(pawn, data, want);
                        DefsOf.WQ_RerollSound.PlayOneShotOnCamera();
                    }
                    right = rerollRect.x - ControlGap;
                }
            }
            var tagLabel = mental ? "WQ_UIR_MentalBreakTag".Translate().Resolve() : "+" + CoreWidgets.Amount(want.def.reward);
            var tagWidth = CoreWidgets.HeaderToolWidth(tagLabel);
            var tagRect = new Rect(right - tagWidth, controlY, tagWidth, ControlSize);
            LitChip(tagRect, mental ? WarningTex : StarTex, tagLabel, mental ? Looks.MentalBreak : Looks.Reward);
            CoreWidgets.Tip(tagRect, mental ? "WQ_CausedByMentalBreak".Translate().Resolve() : "WQ_OnCompletion".Translate().Resolve(),
                mental ? null : "WQ_CharacterPointsReward".Translate(want.def.reward).Resolve());

            var textX = iconRect.xMax + 10f;
            var textRight = tagRect.x - 10f;
            var titleLine = new Rect(textX, card.y + 7f, textRight - textX, 22f);
            CoreWidgets.Line(titleLine, want.LabelCap, Theme.TextBright);

            var detailLine = new Rect(textX, card.y + 31f, textRight - textX, 20f);
            var hasProgress = want.def.Worker.TryGetProgress(pawn, want, out var current, out var target) && target > 0f;
            var waitedText = "WQ_UIR_Waiting".Translate(CoreWidgets.Duration(Find.TickManager.TicksGame - want.assignedTick)).Resolve();
            if (hasProgress)
            {
                var countText = current.ToString("0.#") + " / " + target.ToString("0.#");
                var countWidth = CoreWidgets.Width(countText) + 4f;
                var bar = new Rect(detailLine.x, Mathf.Round(detailLine.center.y - 3f), Mathf.Max(0f, detailLine.width - countWidth - 8f), 6f);
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

            if (dismissed)
            {
                data.activeWants.Remove(want);
            }
            return dismissed;
        }

        // A label that looks like a lit header tool but takes no clicks.
        public static void LitChip(Rect rect, Texture icon, string label, Color accent)
        {
            Rounded.Fill(rect, Color.Lerp(Theme.BGLight, accent, 0.55f));
            Rounded.Border(rect, accent);
            var iconRect = new Rect(rect.x + 8f, Mathf.Round(rect.center.y - 7f), 14f, 14f);
            CoreWidgets.DrawIcon(iconRect, icon);
            var x = iconRect.xMax + 6f;
            CoreWidgets.Line(new Rect(x, rect.y, rect.xMax - 8f - x + 2f, rect.height), label, Color.white);
        }

        // One quirk: icon, name, remove (with the same confirmation as the classic tab).
        public static void DrawQuirk(Rect row, Pawn pawn, Quirk quirk)
        {
            CoreWidgets.Card(row);
            var iconRect = new Rect(row.x + 5f, Mathf.Round(row.center.y - 13f), 26f, 26f);
            var tex = WantsAndQuirksUtility.QuirkIcon(quirk, 26f);
            if (tex != null)
            {
                CoreWidgets.DrawIcon(iconRect, tex);
            }
            var removeRect = new Rect(row.xMax - 6f - ControlSize, Mathf.Round(row.center.y - ControlSize / 2f), ControlSize, ControlSize);
            CoreWidgets.Line(new Rect(iconRect.xMax + 8f, row.y, removeRect.x - iconRect.xMax - 12f, row.height), quirk.LabelCap, Theme.TextBright);
            CoreWidgets.Tip(new Rect(row.x, row.y, removeRect.x - row.x, row.height), quirk.LabelCap, quirk.Description);
            if (CoreWidgets.HeaderTool(removeRect, DismissTex, "WQ_UIR_RemoveQuirk".Translate(), null, false, Theme.HighlightRed, true, null))
            {
                var removed = quirk;
                var owner = pawn;
                Failsafe.Later(() => CoreWidgets.Confirm("WQ_ConfirmRemoveQuirk".Translate(removed.LabelCap), () =>
                {
                    removed.def.Worker.OnRemoved(owner, removed);
                    owner.GetWantsData().quirks.Remove(removed);
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }));
            }
        }

        // The scrolling list of want cards, with drag to reorder. Returns the reorder group so callers can tell drags apart.
        public static void DrawWantList(Rect inner, Pawn pawn, PawnWantsData data, ref Vector2 scroll, ref int reorderGroup)
        {
            SortByReward(data);
            var contentHeight = data.activeWants.Count * (CardHeight + CardGap) - CardGap;
            var view = Theme.BeginScroll(inner, ref scroll, contentHeight);
            if (Event.current.type == EventType.Repaint)
            {
                var wantsData = data;
                reorderGroup = ReorderableWidget.NewGroup((from, to) =>
                {
                    var item = wantsData.activeWants[from];
                    wantsData.activeWants.Insert(to, item);
                    wantsData.activeWants.RemoveAt(from < to ? from : from + 1);
                    MarkHandOrdered(wantsData);
                }, ReorderableDirection.Vertical, inner);
            }
            var y = 0f;
            for (int i = 0; i < data.activeWants.Count; i++)
            {
                var card = new Rect(view.x, y, view.width, CardHeight);
                if (Draw(card, pawn, data, data.activeWants[i]))
                {
                    break;
                }
                ReorderableWidget.Reorderable(reorderGroup, card);
                y += CardHeight + CardGap;
            }
            Theme.EndScroll();
        }

        public static void DrawQuirkList(Rect inner, Pawn pawn, PawnWantsData data, ref Vector2 scroll)
        {
            var contentHeight = data.quirks.Count * (QuirkHeight + QuirkGap) - QuirkGap;
            var view = Theme.BeginScroll(inner, ref scroll, contentHeight);
            var y = 0f;
            for (int i = 0; i < data.quirks.Count; i++)
            {
                DrawQuirk(new Rect(view.x, y, view.width, QuirkHeight), pawn, data.quirks[i]);
                y += QuirkHeight + QuirkGap;
            }
            Theme.EndScroll();
        }
    }
}
