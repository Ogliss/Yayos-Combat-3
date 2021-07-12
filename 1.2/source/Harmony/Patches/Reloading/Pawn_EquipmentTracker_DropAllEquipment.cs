using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 적 사망 시 무기에서 탄약 아이템 분리
    // Detach ammo items from weapons when an enemy dies
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "DropAllEquipment")]
    internal class Pawn_EquipmentTracker_DropAllEquipment
    {
        [HarmonyPostfix]
        static bool Prefix(Pawn_EquipmentTracker __instance, ThingOwner<ThingWithComps> ___equipment, IntVec3 pos, bool forbid = true)
        {
            if (!yayoCombat.ammo) return true;


            ThingOwner<ThingWithComps> equipment = ___equipment;
            for (int index = equipment.Count - 1; index >= 0; --index)
            {
                bool isEnemy = __instance.pawn.Faction != null && !__instance.pawn.Faction.IsPlayer;
                ThingWithComps t = equipment[index];
                if (__instance.TryDropEquipment(t, out ThingWithComps _, pos, forbid))
                {
                    if (isEnemy)
                    {
                        ReloadUtility.TryThingEjectAmmoDirect((Thing)t, true);
                    }
                }
                
            }
                

            return false;
        }
    }




}