using UnityEngine;
using HarmonyLib;
using Verse;


namespace yayoCombat
{
    [HarmonyPatch(typeof(PawnRenderer), "DrawEquipment")]
    internal class patch_DrawEquipment
    {

        [HarmonyPrefix]
        static bool Prefix(PawnRenderer __instance, Vector3 rootLoc, Pawn ___pawn)
        {
            if (!yayoCombat.advAni)
            {
                return true;
            }
            Pawn pawn = ___pawn;
            if (pawn.Dead || !pawn.Spawned)
            {
                return false;
            }
            if (pawn.equipment == null || pawn.equipment.Primary == null)
            {
                return false;
            }
            if (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon)
            {
                return false;
            }
            


            // duelWeld
            ThingWithComps offHandEquip = null;
            if (yayoCombat.using_dualWeld)
            {
                if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps result))
                {
                    offHandEquip = result;
                }

                /*
                try
                {
                    ((Action)(() =>
                    {
                        // do
                        
                    }))();
                }
                catch (TypeLoadException) { }
                */
            }

            // 주무기
            Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;
            PawnRenderer_override.animateEquip(__instance, pawn, rootLoc, pawn.equipment.Primary, stance_Busy, new Vector3(0f, 0f, 0.0005f));

            // 보조무기
            if (offHandEquip != null)
            {
                Stance_Busy offHandStance = null;
                if (pawn.GetStancesOffHand() != null)
                {
                    offHandStance = pawn.GetStancesOffHand().curStance as Stance_Busy;
                }
                PawnRenderer_override.animateEquip(__instance, pawn, rootLoc, offHandEquip, offHandStance, new Vector3(0.1f, 0.1f, 0f), true);
            }

            return false;
        }
    }
}