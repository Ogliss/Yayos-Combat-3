using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 게임 로드 시 comp_reloadable.verbTracker로 인한 중복 에러 방지
    // Avoid duplicate errors caused by comp_reloadable.verbTracker when loading games
    [HarmonyPatch(typeof(CompReloadable), "PostExposeData")]
    internal class CompReloadable_PostExposeData
    {
        [HarmonyPostfix]
        static bool Prefix(CompReloadable __instance, ref int ___remainingCharges)
        {
            if (!yayoCombat.ammo) return true;
            if (!__instance.parent.def.IsWeapon) return true;

            //__instance.PostExposeData();

            int remainingCharges = Traverse.Create(__instance).Field("remainingCharges").GetValue<int>();
            Scribe_Values.Look<int>(ref remainingCharges, "remainingCharges", -999);
            Traverse.Create(__instance).Field("remainingCharges").SetValue(remainingCharges);
            /*
            VerbTracker verbTracker = null;
            Scribe_Deep.Look<VerbTracker>(ref verbTracker, "verbTracker", (object)__instance);
            Traverse.Create(__instance).Field("verbTracker").SetValue(verbTracker);
            */
            if (Scribe.mode != LoadSaveMode.PostLoadInit || remainingCharges != -999)
            {
                return false;
            }

            Traverse.Create(__instance).Field("remainingCharges").SetValue(__instance.MaxCharges);

            return false;

        }
    }




}