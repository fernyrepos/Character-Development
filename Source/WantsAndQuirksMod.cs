using HarmonyLib;
using UnityEngine;
using Verse;

namespace WantsAndQuirks
{
    public class WantsAndQuirksMod : Mod
    {
        public static WantsAndQuirksSettings settings;
        public static Harmony harmony;

        public WantsAndQuirksMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<WantsAndQuirksSettings>();
            harmony = new Harmony("WantsAndQuirksMod");
            harmony.PatchAll(typeof(WantsAndQuirksMod).Assembly);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return Content.Name;
        }
    }
}
