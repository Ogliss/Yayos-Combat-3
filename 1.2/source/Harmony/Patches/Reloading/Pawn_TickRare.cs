using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 소집상태에서 탄약0일 장전 시도하기
    // Attempt to reload ammo for day 0 while in muster
    [HarmonyPatch(typeof(Pawn), "Tick")]
    internal class Pawn_TickRare
    {
        [HarmonyPriority(0)]
        static void Postfix(Pawn __instance)
        {
            if (!yayoCombat.ammo) return;
            if (!__instance.Drafted) return;
            if (Find.TickManager.TicksGame % 60 != 0) return;
            if (!(__instance.CurJobDef == JobDefOf.Wait_Combat || __instance.CurJobDef == JobDefOf.AttackStatic) || __instance.equipment == null) return;

            List<ThingWithComps> ar = __instance.equipment.AllEquipmentListForReading;

            foreach (ThingWithComps t in ar)
            {
                CompReloadable cp = t.TryGetComp<CompReloadable>();
                
                if (cp != null)
                {
                    ReloadUtility.tryAutoReload(cp);
                    return;
                }
            }

        }
    }




}