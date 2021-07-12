using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 터렛 무기 재장전 콤프 제거
    [HarmonyPatch(typeof(Building_TurretGun), "MakeGun")]
    internal class Building_TurretGun_MakeGun
    {
        [HarmonyPostfix]
        static bool Prefix(Building_TurretGun __instance)
        {
            if (!yayoCombat.ammo) return true;

            ThingDef gunDef = __instance.def.building.turretGunDef;
            bool flag = false;
            for(int i = 0; i < gunDef.comps.Count; i++)
            {
                if (gunDef.comps[i].compClass == typeof(CompReloadable))
                {
                    gunDef.comps.Remove(gunDef.comps[i]);
                    flag = true;
                }
            }

            if (flag)
            {
                __instance.def.building.turretGunDef = gunDef;
            }
            



            return true;


        }
    }




}