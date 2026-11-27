using RimWorld;
using Verse;

namespace WantsAndQuirks
{
    [DefOf]
    public static class DefsOf
    {
        public static RewardDef WQ_Quirk_EarlyBird;
        public static RewardDef WQ_Quirk_NightOwl;
        public static RewardDef WQ_Quirk_LikesClothing;
        public static RewardDef WQ_Quirk_LikesWeapon;
        public static RewardDef WQ_Quirk_LikesPerson;
        public static RewardDef WQ_Quirk_DislikesPerson;
        public static RewardDef WQ_Quirk_LovesPerson;
        public static RewardDef WQ_Quirk_LikesRecreationBuilding;
        public static ThoughtDef WQ_Thought_LikesRecreationBuilding;
        [MayRequire("vanillaexpanded.vcooking")]
        public static JoyKindDef VCE_Confectionery;
        public static SoundDef WQ_WantCompleted;
        public static SoundDef WQ_RerollSound;
        public static SoundDef WQ_BubbleClick;
        public static SoundDef WQ_NewWantBell;
        public static MainButtonDef WQ_CharactersMenu;
        static DefsOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DefsOf));
    }
}
