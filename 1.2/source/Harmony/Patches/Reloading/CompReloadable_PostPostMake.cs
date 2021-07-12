using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;
using System.Linq;

namespace yayoCombat
{
    // 아이템 생성 시 기본 탄약 수
    [HarmonyPatch(typeof(CompReloadable), "PostPostMake")]
    internal class CompReloadable_PostPostMake
    {
        [HarmonyPriority(0)]
        static void Postfix(CompReloadable __instance)
        {
            if (yayoCombat.ammo && __instance.parent.def.IsWeapon)
            {


                if (GenTicks.TicksGame <= 5)
                {
                    foreach (ThingComp c in from comp in __instance.parent.AllComps
                                            where
                                                comp != null
                                                 && comp is CompReloadable
                                            select comp)
                    {
                        CompReloadable comp = c as CompReloadable;
                        if (comp != null)
                        {
                            // 시작아이템 총알 보유량
                            Traverse.Create(comp).Field("remainingCharges").SetValue(Mathf.RoundToInt((float)comp.MaxCharges * yayoCombat.s_enemyAmmo));
                        }

                    }
                }
                else
                {
                    // 생산된 아이템 총알 보유량
                    Traverse.Create(__instance).Field("remainingCharges").SetValue(0);
                }



            }
        }
    }




}