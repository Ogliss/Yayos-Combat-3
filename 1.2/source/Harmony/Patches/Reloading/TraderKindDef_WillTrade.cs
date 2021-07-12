using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 상인이 탄약 거래 가능하도록 허용
    [HarmonyPatch(typeof(TraderKindDef), "WillTrade")]
    internal class TraderKindDef_WillTrade
    {
        [HarmonyPostfix]
        static bool Prefix(ref bool __result, TraderKindDef __instance, ThingDef td)
        {
            if (!yayoCombat.ammo) return true;
            if (__instance.defName == "Empire_Caravan_TributeCollector") return true; // 제국 수집 상인

            if (td.tradeTags != null && td.tradeTags.Contains("Ammo"))
            {
                __result = true;
                return false;
            }
            else
            {
                return true;
            }


        }
    }




}