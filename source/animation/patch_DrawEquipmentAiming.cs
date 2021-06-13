using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;
using System.Linq;

namespace yayoCombat
{
    [HarmonyPatch(typeof(PawnRenderer), "DrawEquipmentAiming")]
    public static class patch_DrawEquipmentAiming
    {

        [HarmonyPrefix]
        static bool Prefix(PawnRenderer __instance, Thing eq, Vector3 drawLoc, float aimAngle, Pawn ___pawn)
        {
            if (!yayoCombat.advAni)
            {
                return true;
            }
            Pawn pawn = ___pawn;

            float num = aimAngle - 90f;
            Mesh mesh;

            bool isMeleeAtk = false;
            bool flip = false;
            Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;

            bool flag = true;
            if (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon) flag = false;

            if (flag && stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
            {
                if (pawn.Rotation == Rot4.West)
                {
                    flip = true;
                }

                if (!pawn.equipment.Primary.def.IsRangedWeapon || stance_Busy.verb.IsMeleeAttack)
                {
                    // 근접공격
                    // melee attack
                    isMeleeAtk = true;
                }
            }
            if (isMeleeAtk)
            {
                if (flip)
                {
                    mesh = MeshPool.plane10Flip;
                    num -= 180f;
                    num -= eq.def.equippedAngleOffset;
                }
                else
                {
                    mesh = MeshPool.plane10;
                    num += eq.def.equippedAngleOffset;
                }
            }
            else
            {
                if (aimAngle > 20f && aimAngle < 160f)
                {
                    mesh = MeshPool.plane10;
                    num += eq.def.equippedAngleOffset;
                }
                //else if ((aimAngle > 200f && aimAngle < 340f) || ignore)
                else if ((aimAngle > 200f && aimAngle < 340f) || flip)
                {
                    mesh = MeshPool.plane10Flip;
                    num -= 180f;
                    num -= eq.def.equippedAngleOffset;
                }
                else
                {
                    mesh = MeshPool.plane10;
                    num += eq.def.equippedAngleOffset;
                }
            }
            if (pawn.Rotation == Rot4.West || pawn.Rotation == Rot4.East)
            {
                if (yayoCombat.using_dualWeld && !isMeleeAtk)
                {
                    if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps result))
                    {
                        if (eq == result)
                        {
                            Stance_Busy offHandStance = null;
                            if (pawn.GetStancesOffHand() != null)
                            {
                                offHandStance = pawn.GetStancesOffHand().curStance as Stance_Busy;
                            }
                            bool flag2 = offHandStance != null && !offHandStance.neverAimWeapon && offHandStance.focusTarg.IsValid;
                            if (!flag2)
                            {
                                if (mesh == MeshPool.plane10Flip)
                                {
                                    mesh = MeshPool.plane10;
                                }
                                else
                                {
                                    mesh = MeshPool.plane10Flip;
                                }
                            }
                        }
                    }
                }
            }
            
            num %= 360f;
            Graphic_StackCount graphic_StackCount = eq.Graphic as Graphic_StackCount;
            Material matSingle;
            if (graphic_StackCount != null)
            {
                matSingle = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle;
            }
            else
            {
                matSingle = eq.Graphic.MatSingle;
            }
            Graphics.DrawMesh(mesh, drawLoc, Quaternion.AngleAxis(num, Vector3.up), matSingle, 0);
            return false;
        }
    }
}