using System;
using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 우클릭 탄약꺼내기 메뉴
    // Right-click eject ammunition menu

    [HarmonyPatch(typeof(ThingWithComps), "GetFloatMenuOptions")]
    internal class ThingWithComps_GetFloatMenuOptions
    {
        [HarmonyPriority(0)]
        static void Postfix(ref IEnumerable<FloatMenuOption> __result, ThingWithComps __instance, Pawn selPawn)
        {
            if (!yayoCombat.ammo) return;

            CompReloadable cp = __instance.TryGetComp<CompReloadable>();

            if (selPawn.IsColonist && cp != null && cp.AmmoDef != null && !cp.Props.destroyOnEmpty && cp.RemainingCharges > 0)
            {
                __result = new List<FloatMenuOption>() { new FloatMenuOption("eject_Ammo".Translate(), new Action(cleanWeapon), MenuOptionPriority.High) };
            }

            void cleanWeapon() => ReloadUtility.EjectAmmo(selPawn, __instance);
            
        }
    }




}