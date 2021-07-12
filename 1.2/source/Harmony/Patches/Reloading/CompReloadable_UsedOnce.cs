using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 남은 탄약이 0 일경우 사냥 중지, 탄약 줍기 ai
    [HarmonyPatch(typeof(CompReloadable), "UsedOnce")]
    internal class CompReloadable_UsedOnce
    {
        [HarmonyPostfix]
        static bool Prefix(CompReloadable __instance)
        {
            if (!yayoCombat.ammo) return true;

            int remainingCharges0 = __instance.RemainingCharges;
            if (remainingCharges0 > 0)
            {
                Traverse.Create(__instance).Field("remainingCharges").SetValue(remainingCharges0 - 1);
            }
            //if (__instance.VerbTracker.PrimaryVerb.caster == null) return false;

            
            if (!__instance.Props.destroyOnEmpty || __instance.RemainingCharges != 0 || __instance.parent.Destroyed)
            {
                
            }
            else
            {
                __instance.parent.Destroy(DestroyMode.Vanish);
            }

            //

            if (__instance.Wearer == null) return false;

            // 남은 탄약이 0 일경우 게임튕김 방지를 위해 사냥 중지
            if (__instance.RemainingCharges == 0)
            {
                if (__instance.Wearer.CurJobDef == JobDefOf.Hunt)
                {
                    __instance.Wearer.jobs.StopAll();
                }
            }


            // 알아서 장전 ai
            ReloadUtility.tryAutoReload(__instance);

            return false;
        }
    }




}