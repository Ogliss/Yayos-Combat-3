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

            float y = 0.0005f;
            float x = 0.1f;
            float z = 0.1f;
            if (pawn.Rotation == Rot4.West)
            {
                y = -0.1f + -0.3787879f;
                x = -0.05f;
            }
            // 주무기 // Mainhand
            Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;
            PawnRenderer_override.animateEquip(__instance, pawn, rootLoc, pawn.equipment.Primary, stance_Busy, new Vector3(-x, y, -z));
            // 보조무기 // Offhand
            if (offHandEquip != null)
            {
                Stance_Busy offHandStance = null;
                if (pawn.GetStancesOffHand() != null)
                {
                    offHandStance = pawn.GetStancesOffHand().curStance as Stance_Busy;
                }
                if (pawn.Rotation == Rot4.East)
                {
                    y = -0.05f + -0.3787879f;
                }
                else
                if (pawn.Rotation == Rot4.West)
                {
                    y = 0.05f;
                }
                else y = 0f;
                PawnRenderer_override.animateEquip(__instance, pawn, rootLoc, offHandEquip, offHandStance, new Vector3(x, y, z), true);
            }

            return false;
        }
    }
}