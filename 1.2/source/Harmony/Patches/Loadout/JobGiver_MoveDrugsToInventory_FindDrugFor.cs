using RimWorld;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace yayoCombat
{
    // 탄약을 인벤에 챙길 수 있도록 2
    [HarmonyPatch(typeof(JobGiver_MoveDrugsToInventory), "FindDrugFor")]
    internal class JobGiver_MoveDrugsToInventory_FindDrugFor
    {

        [HarmonyPostfix]
        static bool Prefix(ref Thing __result, JobGiver_MoveDrugsToInventory __instance, Pawn pawn, ThingDef drugDef)
        {
            if (!yayoCombat.ammo) return true;
            if (drugDef.IsDrug)
            {
                __result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(drugDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn));
            }
            else
            {
                __result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(drugDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f
                    , x => !x.IsForbidden(pawn) && pawn.CanReserve((LocalTargetInfo)x));
            }
            
            return false;

        }
    }
}