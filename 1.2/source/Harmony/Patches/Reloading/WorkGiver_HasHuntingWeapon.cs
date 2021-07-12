using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 사냥을 위한 무기 보유 여부를 탄약과 함께 계산
    [HarmonyPatch(typeof(WorkGiver_HunterHunt), "HasHuntingWeapon")]
    internal class WorkGiver_HasHuntingWeapon
    {
        [HarmonyPostfix]
        static bool Prefix(ref bool __result, Pawn p)
        {
            if (!yayoCombat.ammo) return true;

            if (p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon && (p.equipment.PrimaryEq.PrimaryVerb.HarmsHealth() && !p.equipment.PrimaryEq.PrimaryVerb.UsesExplosiveProjectiles()))
            {
                if (p.equipment.Primary.GetComp<CompReloadable>() != null)
                {
                    __result = p.equipment.Primary.GetComp<CompReloadable>().CanBeUsed;

                }
                else
                {
                    __result = true;
                }
            }
            else
            {
                __result = false;
            }

            return false;
        }
    }




}