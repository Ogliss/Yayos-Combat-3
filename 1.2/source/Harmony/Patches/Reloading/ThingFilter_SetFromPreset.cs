using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 탄약 카테고리 보이기
    // Show ammo categories
    [HarmonyPatch(typeof(ThingFilter), "SetFromPreset")]
    internal class ThingFilter_SetFromPreset
    {
        [HarmonyPostfix]
        static bool Prefix(ThingFilter __instance, StorageSettingsPreset preset)
        {
            if (!yayoCombat.ammo) return true;

            if (preset == StorageSettingsPreset.DefaultStockpile)
            {
                __instance.SetAllow(ThingCategoryDef.Named("yy_ammo_category"), true);
            }
            return true;
        }
    }




}