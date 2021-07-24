using UnityEngine;
using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace yayoCombat
{
    [HarmonyPatch(typeof(PawnRenderer), "DrawEquipment")]
    internal class PawnRenderer_DrawEquipment
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo drawEquipmentAiming = AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming");
        //    MethodInfo preDrawEquipmentAiming = AccessTools.Method(typeof(PawnRenderer_DrawEquipmentAiming), "Prefixx");
        //    IEnumerable<CodeInstruction> newInstructions =  instructions.MethodReplacer(drawEquipmentAiming, preDrawEquipmentAiming);
            var instructionsList = new List<CodeInstruction>(instructions);
            int instCount = instructionsList.Count - 1;
            bool aimedPatched = false;
            bool eastPatched = false;
            bool westPatched = false;
            bool northPatched = false;
            bool southPatched = false;
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.4f)
                {
                    instruction = new CodeInstruction(OpCodes.Ldc_R4, 0.4f);
                }
                if (instruction.opcode == OpCodes.Stloc_2 && instructionsList[i + 1].opcode == OpCodes.Ldarg_1)
                {
                    Log.Message(i + ": Draw Angle Aiming opcode: " + instruction.opcode + " operand: " + instruction.operand);
                }
                if (!aimedPatched && instruction.opcode == OpCodes.Stloc_3)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PawnRenderer_DrawEquipment).GetMethod("ModyifiedAimingPos"));
                    aimedPatched = true;
                    Log.Message(i + ": Draw Loc Aiming opcode: " + instruction.opcode + " operand: " + instruction.operand);
                }
                if (!southPatched && instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 5)
                {
                    Log.Message(i + ": Draw Loc South opcode: " + instruction.opcode + " operand: " + instruction.operand);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 143f);
                    yield return new CodeInstruction(OpCodes.Stloc_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PawnRenderer_DrawEquipment).GetMethod("ModyifiedDrawPos"));
                    southPatched = true;
                }
                if (!northPatched && instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 6)
                {
                    Log.Message(i + ": Draw Loc North opcode: " + instruction.opcode + " operand: " + instruction.operand);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 143f);
                    yield return new CodeInstruction(OpCodes.Stloc_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PawnRenderer_DrawEquipment).GetMethod("ModyifiedDrawPos"));
                    northPatched = true;
                }
                if (!eastPatched && instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 7)
                {
                    Log.Message(i + ": Draw Loc East opcode: " + instruction.opcode + " operand: " + instruction.operand);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 143f);
                    yield return new CodeInstruction(OpCodes.Stloc_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PawnRenderer_DrawEquipment).GetMethod("ModyifiedDrawPos"));
                    eastPatched = true;
                }
                if (!westPatched && instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 8)
                {
                    Log.Message(i + ": Draw Loc West opcode: " + instruction.opcode + " operand: " + instruction.operand);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 217f);
                    yield return new CodeInstruction(OpCodes.Stloc_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PawnRenderer_DrawEquipment).GetMethod("ModyifiedDrawPos"));
                    westPatched = true;
                }
                if (i + 3 < instCount && instructionsList[i + 3].OperandIs(drawEquipmentAiming))
                {
                //    Log.Message(i + ": 3 opcode: " + instruction.opcode + " operand: " + instruction.operand);
                }
                if (i + 2 < instCount && instructionsList[i + 2].OperandIs(drawEquipmentAiming))
                {
                //    Log.Message(i + ": 2 opcode: " + instruction.opcode + " operand: " + instruction.operand);
                }
                if (i + 1 < instCount && instructionsList[i + 1].OperandIs(drawEquipmentAiming))
                {
                    if (instruction.opcode == OpCodes.Ldc_R4)
                    {
                    //    instruction = new CodeInstruction(OpCodes.Ldloc_2);
                    }
                //    Log.Message(i + ": 1 opcode: " + instruction.opcode + " operand: " + instruction.operand);
                }
                


                //   if (instruction.OperandIs(drawEquipmentAiming))
                //   {
                //       instruction = new CodeInstruction(instruction.opcode, AccessTools.Method(typeof(PawnRenderer_DrawEquipment), "PreDrawEquipmentAiming"));
                //   }
                   yield return instruction;

            }
        //    return instructionsList;
        }
        //    [HarmonyPrefix]
        public static Vector3 ModyifiedDrawPos(Vector3 originalLoc, PawnRenderer __instance, ref float originalAngle)
        {
            Vector3 drawLoc = originalLoc;
            if (!yayoCombat.advAni)
            {
                return drawLoc;
            }
            Pawn pawn = __instance.pawn;

            if (pawn.Dead || !pawn.Spawned)
            {
                return drawLoc;
            }
            if (pawn.equipment == null || pawn.equipment.Primary == null)
            {
                return drawLoc;
            }
            if (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon)
            {
                return drawLoc;
            }
        //    drawLoc = pawn.DrawPos;
            float y = 0.0005f;
            float x = 0.0f;
            float z = 0.1f;
            // duelWeld
            ThingWithComps offHandEquip = null;
            Stance_Busy mainHandStance = pawn.stances.curStance as Stance_Busy;
            Stance_Busy offHandStance = null;
            Vector3 offsetMainHand = Vector3.zero;
            Vector3 offsetOffHand = Vector3.zero;
            LocalTargetInfo focusTarg = null;
            float aimAngle = originalAngle;
            float mainHandAngle = originalAngle;
            float offHandAngle = originalAngle;
            Vector3 drawLocOffhand = originalLoc;
            if (yayoCombat.using_dualWeld)
            {
                if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps result))
                {
                    offHandEquip = result;
                }
                if (offHandEquip != null)
                {
                    if (pawn.GetStancesOffHand() != null)
                    {
                        offHandStance = pawn.GetStancesOffHand().curStance as Stance_Busy;
                    }
                    if (focusTarg == null && offHandStance != null && !offHandStance.neverAimWeapon)
                    {
                        focusTarg = offHandStance.focusTarg;
                        if (focusTarg != null)
                        {
                            aimAngle = PawnRenderer_override.GetAimingRotation(pawn, focusTarg);
                        }
                    }
                    mainHandAngle = aimAngle;
                    offHandAngle = aimAngle;
                    bool mainHandAiming = PawnRenderer_override.CurrentlyAiming(mainHandStance);
                    bool offHandAiming = offHandStance != null && PawnRenderer_override.CurrentlyAiming(offHandStance);

                    //bool currentlyAiming = (mainStance != null && !mainStance.neverAimWeapon && mainStance.focusTarg.IsValid) || stancesOffHand.curStance is Stance_Busy ohs && !ohs.neverAimWeapon && ohs.focusTarg.IsValid;
                    //When wielding offhand weapon, facing south, and not aiming, draw differently 
                    SetAnglesAndOffsets(pawn.equipment.Primary, offHandEquip, aimAngle, pawn, ref offsetMainHand, ref offsetOffHand, ref mainHandAngle, ref offHandAngle, mainHandAiming, offHandAiming);

                    if ((offHandAiming || mainHandAiming) && focusTarg != null)
                    {
                        offHandAngle = PawnRenderer_override.GetAimingRotation(pawn, focusTarg);
                        offsetOffHand.y += 0.1f;
                        drawLocOffhand = pawn.DrawPos + offsetOffHand;
                    }
                    else
                    {
                        drawLocOffhand += offsetOffHand;
                    }
                }

            }

            if (pawn.Rotation == Rot4.West)
            {
                y = -0.1f + -0.3787879f;
                x = -0.05f;
            }
            // 보조무기 // Offhand
            if (offHandEquip != null)
            {
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
                PawnRenderer_override.AnimateEquipment(__instance, drawLocOffhand, out drawLocOffhand, offHandAngle, out offHandAngle, offHandEquip, offHandStance, new Vector3(x, y, z), true);
                __instance.DrawEquipmentAiming(offHandEquip, drawLocOffhand, offHandAngle);
            }
            // 주무기 // Mainhand
            if (offHandEquip == null || offHandEquip != pawn.equipment.Primary)
            {
                PawnRenderer_override.AnimateEquipment(__instance, drawLoc, out drawLoc, mainHandAngle, out mainHandAngle, pawn.equipment.Primary, mainHandStance, new Vector3(-x, y, -z));
                originalAngle = mainHandAngle;
            }
            return drawLoc;
        }
         
        public static Vector3 ModyifiedAimingPos(Vector3 originalLoc, PawnRenderer __instance, ref float originalAngle)
        {
            Vector3 drawLoc = originalLoc;
            if (!yayoCombat.advAni)
            {
                return drawLoc;
            }
            Pawn pawn = __instance.pawn;

            if (pawn.Dead || !pawn.Spawned)
            {
                return drawLoc;
            }
            if (pawn.equipment == null || pawn.equipment.Primary == null)
            {
                return drawLoc;
            }
            if (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon)
            {
                return drawLoc;
            }
        //    drawLoc = pawn.DrawPos;
            float y = 0.0005f;
            float x = 0.0f;
            float z = -0.0f;
            // duelWeld
            ThingWithComps mainHandEquip = pawn.equipment.Primary;
            ThingWithComps offHandEquip = null;
            Stance_Busy mainHandStance = pawn.stances.curStance as Stance_Busy;
            Stance_Busy offHandStance = null;
            Vector3 offsetMainHand = Vector3.zero;
            Vector3 offsetOffHand = Vector3.zero;
            LocalTargetInfo focusTarg = null;
            float aimAngle = originalAngle;
            float mainHandAngle = originalAngle;
            float offHandAngle = originalAngle;
            Vector3 drawLocOffhand = originalLoc;
            if (yayoCombat.using_dualWeld)
            {
                if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps result))
                {
                    offHandEquip = result;
                }
                if (offHandEquip != null)
                {
                    if (pawn.GetStancesOffHand() != null)
                    {
                        offHandStance = pawn.GetStancesOffHand().curStance as Stance_Busy;
                    }
                    if (focusTarg == null && offHandStance != null && !offHandStance.neverAimWeapon)
                    {
                        focusTarg = offHandStance.focusTarg;
                        if (focusTarg != null)
                        {
                            aimAngle = PawnRenderer_override.GetAimingRotation(pawn, focusTarg);
                        }
                    }
                    mainHandAngle = aimAngle;
                    offHandAngle = aimAngle;
                    bool mainHandAiming = PawnRenderer_override.CurrentlyAiming(mainHandStance);
                    bool offHandAiming = offHandStance != null && PawnRenderer_override.CurrentlyAiming(offHandStance);

                    //bool currentlyAiming = (mainStance != null && !mainStance.neverAimWeapon && mainStance.focusTarg.IsValid) || stancesOffHand.curStance is Stance_Busy ohs && !ohs.neverAimWeapon && ohs.focusTarg.IsValid;
                    //When wielding offhand weapon, facing south, and not aiming, draw differently 
                    SetAnglesAndOffsets(pawn.equipment.Primary, offHandEquip, aimAngle, pawn, ref offsetMainHand, ref offsetOffHand, ref mainHandAngle, ref offHandAngle, mainHandAiming, offHandAiming);

                    if ((offHandAiming || mainHandAiming) && focusTarg != null)
                    {
                        offHandAngle = PawnRenderer_override.GetAimingRotation(pawn, focusTarg);
                        offsetOffHand.y += 0.1f;
                        drawLocOffhand = pawn.DrawPos + offsetOffHand;
                    }
                    else
                    {
                        drawLocOffhand += offsetOffHand;
                    }
                }

            }

            if (pawn.Rotation == Rot4.West)
            {
                y = -0.1f + -0.3787879f;
            //    x = -0.05f;
            }
            // 보조무기 // Offhand
            if (offHandEquip != null)
            {
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
                PawnRenderer_override.AnimationOffsetsAttack(pawn, offHandEquip, offHandStance, out Vector3 drawoOffsetOffhand, offHandAngle, out float offHandAngleOffset, true);
            //    PawnRenderer_override.AnimateEquipment(__instance, drawLocOffhand, out drawLocOffhand, offHandAngle, out offHandAngle, offHandEquip, offHandStance, new Vector3(x, y, z), true);
                __instance.DrawEquipmentAiming(offHandEquip, drawLocOffhand + drawoOffsetOffhand, offHandAngle + offHandAngleOffset);
            }
            // 주무기 // Mainhand
            if (offHandEquip == null || offHandEquip != mainHandEquip)
            {
                PawnRenderer_override.AnimationOffsetsAttack(pawn, mainHandEquip, mainHandStance, out Vector3 drawOffset, mainHandAngle, out float mainHandAngleOffset);
                originalAngle += mainHandAngleOffset;
                drawLoc += drawOffset;
                //   PawnRenderer_override.AnimateEquipment(__instance, drawLoc, out drawLoc, mainHandAngle, out mainHandAngle, pawn.equipment.Primary, mainHandStance, new Vector3(-x, y, -z));
                //    originalAngle = mainHandAngle;
            }
            return drawLoc;
        }
        
        static bool APrefix(PawnRenderer __instance, Vector3 rootLoc, Pawn ___pawn)
        {
            if (!yayoCombat.advAni)
            {
                return true;
            }
            Pawn pawn = ___pawn;
            Vector3 drawLoc = rootLoc;
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
            float y = 0.0005f;
            float x = 0.0f;
            float z = 0.1f;
            // duelWeld
            ThingWithComps offHandEquip = null;
            Stance_Busy mainHandStance = pawn.stances.curStance as Stance_Busy;
            Stance_Busy offHandStance = null;
            Vector3 offsetMainHand = Vector3.zero;
            Vector3 offsetOffHand = Vector3.zero;
            LocalTargetInfo focusTarg = null;
            float aimAngle = 143f;
            if (mainHandStance != null && !mainHandStance.neverAimWeapon)
            {
                focusTarg = mainHandStance.focusTarg;
                if (focusTarg != null)
                {
                    aimAngle = PawnRenderer_override.GetAimingRotation(pawn, focusTarg);
                }
            }
            float mainHandAngle = aimAngle;
            float offHandAngle = aimAngle;
            if (yayoCombat.using_dualWeld)
            {
                if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps result))
                {
                    offHandEquip = result;
                }
                if (offHandEquip != null)
                {
                    if (pawn.GetStancesOffHand() != null)
                    {
                        offHandStance = pawn.GetStancesOffHand().curStance as Stance_Busy;
                    }
                    if (focusTarg == null && offHandStance != null && !offHandStance.neverAimWeapon)
                    {
                        focusTarg = offHandStance.focusTarg;
                        if (focusTarg != null)
                        {
                            aimAngle = PawnRenderer_override.GetAimingRotation(pawn, focusTarg);
                        }
                    }
                    mainHandAngle = aimAngle;
                    offHandAngle = aimAngle;
                    bool mainHandAiming = PawnRenderer_override.CurrentlyAiming(mainHandStance);
                    bool offHandAiming = offHandStance != null && PawnRenderer_override.CurrentlyAiming(offHandStance);

                    //bool currentlyAiming = (mainStance != null && !mainStance.neverAimWeapon && mainStance.focusTarg.IsValid) || stancesOffHand.curStance is Stance_Busy ohs && !ohs.neverAimWeapon && ohs.focusTarg.IsValid;
                    //When wielding offhand weapon, facing south, and not aiming, draw differently 
                    SetAnglesAndOffsets(pawn.equipment.Primary, offHandEquip, aimAngle, ___pawn, ref offsetMainHand, ref offsetOffHand, ref mainHandAngle, ref offHandAngle, mainHandAiming, offHandAiming);

                    if ((offHandAiming || mainHandAiming) && focusTarg != null)
                    {
                        offHandAngle = PawnRenderer_override.GetAimingRotation(___pawn, focusTarg);
                        offsetOffHand.y += 0.1f;
                        drawLoc = ___pawn.DrawPos + offsetOffHand;
                    }
                    else
                    {
                        drawLoc += offsetOffHand;
                    }
                }

            }

            if (pawn.Rotation == Rot4.West)
            {
                y = -0.1f + -0.3787879f;
                x = -0.05f;
            }
            // 주무기 // Mainhand
            if (offHandEquip == null || offHandEquip != pawn.equipment.Primary)
            {
                PawnRenderer_override.animateEquip(__instance, pawn, rootLoc + offsetMainHand, mainHandAngle, pawn.equipment.Primary, mainHandStance, new Vector3(-x, y, -z));
            }
            // 보조무기 // Offhand
            if (offHandEquip != null)
            {
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
                PawnRenderer_override.animateEquip(__instance, pawn, drawLoc, offHandAngle, offHandEquip, offHandStance, new Vector3(x, y, z), true);
            }

            return false;
        }


        private static void SetAnglesAndOffsets(Thing eq, ThingWithComps offHandEquip, float aimAngle, Pawn pawn, ref Vector3 offsetMainHand, ref Vector3 offsetOffHand, ref float mainHandAngle, ref float offHandAngle, bool mainHandAiming, bool offHandAiming)
        {
            if (yayoCombat.using_dualWeld)
            {
                DualWield.Harmony.PawnRenderer_DrawEquipmentAiming.SetAnglesAndOffsets(eq, offHandEquip, aimAngle, pawn, ref offsetMainHand, ref offsetOffHand, ref mainHandAngle, ref offHandAngle, mainHandAiming, offHandAiming);
            }
            /*
            bool offHandIsMelee = IsMeleeWeapon(offHandEquip);
            bool mainHandIsMelee = IsMeleeWeapon(pawn.equipment.Primary);
            float meleeAngleFlipped = Base.meleeMirrored ? 360 - Base.meleeAngle : Base.meleeAngle;
            float rangedAngleFlipped = Base.rangedMirrored ? 360 - Base.rangedAngle : Base.rangedAngle;

            if (pawn.Rotation == Rot4.East)
            {
                offsetOffHand.y = -1f;
                offsetOffHand.z = 0.1f;
            }
            else if (pawn.Rotation == Rot4.West)
            {
                offsetMainHand.y = -1f;
                //zOffsetMain = 0.25f;
                offsetOffHand.z = -0.1f;
            }
            else if (pawn.Rotation == Rot4.North)
            {
                if (!mainHandAiming && !offHandAiming)
                {
                    offsetMainHand.x = mainHandIsMelee ? Base.meleeXOffset : Base.rangedXOffset;
                    offsetOffHand.x = offHandIsMelee ? -Base.meleeXOffset : -Base.rangedXOffset;
                    offsetMainHand.z = mainHandIsMelee ? Base.meleeZOffset : Base.rangedZOffset;
                    offsetOffHand.z = offHandIsMelee ? -Base.meleeZOffset : -Base.rangedZOffset;
                    offHandAngle = offHandIsMelee ? Base.meleeAngle : Base.rangedAngle;
                    mainHandAngle = mainHandIsMelee ? meleeAngleFlipped : rangedAngleFlipped;

                }
                else
                {
                    offsetOffHand.x = -0.1f;
                }
            }
            else
            {
                if (!mainHandAiming && !offHandAiming)
                {
                    offsetMainHand.y = 1f;
                    offsetMainHand.x = mainHandIsMelee ? -Base.meleeXOffset : -Base.rangedXOffset;
                    offsetOffHand.x = offHandIsMelee ? Base.meleeXOffset : Base.rangedXOffset;
                    offsetMainHand.z = mainHandIsMelee ? -Base.meleeZOffset : -Base.rangedZOffset;
                    offsetOffHand.z = offHandIsMelee ? Base.meleeZOffset : Base.rangedZOffset;
                    offHandAngle = offHandIsMelee ? meleeAngleFlipped : rangedAngleFlipped;
                    mainHandAngle = mainHandIsMelee ? Base.meleeAngle : Base.rangedAngle;
                }
                else
                {
                    offsetOffHand.x = 0.1f;
                }
            }
            if (!pawn.Rotation.IsHorizontal)
            {
                if (Base.customRotations.Value.inner.TryGetValue((offHandEquip.def.defName), out Record offHandValue))
                {
                    offHandAngle += pawn.Rotation == Rot4.North ? offHandValue.extraRotation : -offHandValue.extraRotation;
                    //offHandAngle %= 360;
                }
                if (Base.customRotations.Value.inner.TryGetValue((eq.def.defName), out Record mainHandValue))
                {
                    mainHandAngle += pawn.Rotation == Rot4.North ? -mainHandValue.extraRotation : mainHandValue.extraRotation;
                    //mainHandAngle %= 360;
                }
            }
            */
        }
    }
}