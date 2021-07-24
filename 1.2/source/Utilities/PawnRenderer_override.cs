using UnityEngine;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    static public class PawnRenderer_override
    {
        // PawnRenderer_override.DrawEquipmentAiming
        public static void DrawEquipmentAiming(this PawnRenderer instance, Thing eq, Vector3 drawLoc, float aimAngle, Pawn pawn, Stance_Busy stance_Busy = null, bool pffhand = false)
        {
            Pawn p = pawn;
            bool Offhand = false;
            if (!yayoCombat.advAni)
            {
                instance.DrawEquipmentAiming(eq, drawLoc, aimAngle);
                return;
            }

            float num = aimAngle - 90f;
            Mesh mesh;

            bool isMeleeAtk = false;
            bool flip = false;

            if (stance_Busy != null)
            {
            //  Log.Message((Offhand ? "Offhand" : "Mainhand") + " " + pawn.Rotation.ToStringHuman());
            }
            bool flag = true;
            if (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon) flag = false;

            if (flag && CurrentlyAiming(stance_Busy))
            {
                if (pawn.Rotation == Rot4.West)
                {
                    flip = true;
                }

                if (!eq.def.IsRangedWeapon || stance_Busy.verb.IsMeleeAttack)
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
                    num -= 270f;
                    num -= eq.def.equippedAngleOffset;
                }
                else
                {
                    mesh =  MeshPool.plane10;
                    num += eq.def.equippedAngleOffset;
                }
            }
            else
            {
                if (aimAngle > 20f && aimAngle < 160f)
                {
                    mesh = Offhand ? MeshPool.plane10Flip : MeshPool.plane10;
                    num += eq.def.equippedAngleOffset;
                }
                else if ((aimAngle > 200f && aimAngle < 340f) || flip)
                {
                    mesh = Offhand && (pawn.Rotation == Rot4.East || pawn.Rotation == Rot4.East) ? MeshPool.plane10 : MeshPool.plane10Flip;
                    num -= 180f;
                    num -= eq.def.equippedAngleOffset;
                }
                else
                {
                    mesh = MeshPool.plane10;
                    num += eq.def.equippedAngleOffset;
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
            return;
        }

        public static void AnimationOffsetsAttack(Pawn pawn, Thing weapon, Stance_Busy stance_Busy, out Vector3 drawOffset, float aimAngle, out float angleOffset, bool isSub = false, bool log = false)
        {
            if (weapon.def.IsRangedWeapon && !stance_Busy.verb.IsMeleeAttack)
            {
                AnimationOffsetsAttackRanged(pawn, weapon, stance_Busy, out drawOffset, aimAngle, out angleOffset, isSub, log);
            }
            else
            {
                AnimationOffsetsAttackMelee(pawn, weapon, stance_Busy, out drawOffset, aimAngle, out angleOffset, isSub, log);
            }
        }
        
        public static void AnimationOffsetsAttackMelee(Pawn pawn, Thing weapon, Stance_Busy stance_Busy, out Vector3 drawOffset, float aimAngle, out float angleOffset, bool isSub = false, bool log = false)
        {
            drawOffset = Vector3.zero;
            angleOffset = 0f;
            // Random attack type determination
            int atkType = (pawn.LastAttackTargetTick + weapon.thingIDNumber) % 10000 % 1000 % 100 % 3;
        //    atkType = isSub ? 2 : atkType;
            // Angle according to attack type
            string atktype;
            float addX = 0.35f;
            float addZ = 0f;
            switch (atkType)
            {
                default:
                    // Normal Attack
                    //  addZ = 0.25f;//
                    angleOffset = 25f;
                    atktype = "Normal Attack";
                    break;
                case 1:
                    // Low Attack
                    angleOffset = 35f;
                    addZ = -0.25f;//
                    atktype = "Low Attack";
                    break;
                case 2:
                    // 머리찌르기 
                    // High Attack
                    //  addAngle = isSub ? (pawn.Rotation == Rot4.West ? -25f : 25f) : -25f;
                    angleOffset = 20f;
                    addZ = 0.15f;//
                    atktype = "High Attack";
                    break;
            }

            if (pawn.Rotation == Rot4.West)
            {
                angleOffset = -angleOffset;
                addX = -addX;
            }
            //if (log) Log.Message("C");
            // 원거리 무기일경우 각도보정 // Angle correction for ranged weapons
            if (weapon.def.IsRangedWeapon)
            {
                angleOffset -= isSub ? -35f : 35f;
            }
            SimpleCurve
                curveAxisZ = new SimpleCurve
                {
                    {
                        new CurvePoint(0.1f, 0f),
                        true
                    },
                    {
                        new CurvePoint(0.25f, addZ * 0.5f),
                        true
                    },
                    {
                        new CurvePoint(0.5f, addZ),
                        true
                    }
                };
            SimpleCurve
                curveAxisX = new SimpleCurve
                {
                    {
                        new CurvePoint(0.1f, 0f),
                        true
                    },
                    {
                        new CurvePoint(0.25f, addX * 0.5f),
                        true
                    },
                    {
                        new CurvePoint(0.5f, addX),
                        true
                    }
                };
            SimpleCurve
                curveRotation = new SimpleCurve
                {
                    {
                        new CurvePoint(0.1f, 0f),
                        true
                    },
                    {
                        new CurvePoint(0.25f, angleOffset * 0.5f),
                        true
                    },
                    {
                        new CurvePoint(0.75f, angleOffset),
                        true
                    }
                };

            if (stance_Busy != null && stance_Busy.ticksLeft > 0f && stance_Busy.verb != null)
            {
                Rand.PushState(stance_Busy.ticksLeft);
                try
                {
                    /*
                    FloatRange range = new FloatRange(YayoCombat.meleeDelay * (0.5f * YayoCombat.meleeRandom), YayoCombat.meleeDelay * (1.5f * YayoCombat.meleeRandom));
                    float multiply = range.min;
                    */
                    float multiply = 1;
                    int baseCooldown = Mathf.Max(stance_Busy.verb.verbProps.AdjustedCooldown_NewTmp(stance_Busy.verb.tool, pawn, weapon.def, weapon.Stuff) * multiply, 0.2f).SecondsToTicks();
                    int ticksSinceLastShot = Mathf.Max(baseCooldown - stance_Busy.ticksLeft, 0);

                    if ((float)ticksSinceLastShot < baseCooldown)
                    {
                        float time = Mathf.InverseLerp(0, baseCooldown, stance_Busy.ticksLeft);
                        drawOffset = new Vector3(curveAxisX.Evaluate(time), 0f, curveAxisZ.Evaluate(time));
                        angleOffset = curveRotation.Evaluate(time);
                        aimAngle += angleOffset;
                        //    drawOffset = drawOffset.RotatedBy(angleOffset);
                    }
                }
                finally
                {
                    Rand.PopState();
                }
            }

        }
        
        public static void AnimationOffsetsAttackRanged(Pawn pawn, Thing weapon, Stance_Busy stance_Busy, out Vector3 drawOffset, float aimAngle, out float angleOffset, bool isSub = false, bool log = false)
        {
            drawOffset = Vector3.zero;
            angleOffset = 0f;
            bool isMechanoid = pawn.RaceProps.IsMechanoid;
            // 원거리용 // for Ranged
            //if (log) Log.Message((pawn.LastAttackTargetTick + thing.thingIDNumber).ToString());
            int ticksToNextBurstShot = stance_Busy.verb.ticksToNextBurstShot;
            int atkType = (pawn.LastAttackTargetTick + weapon.thingIDNumber) % 10000 % 1000 % 100 % 5; // 랜덤 공격타입 결정 // Random attack type determination
            Stance_Cooldown Stance_Cooldown = pawn.stances.curStance as Stance_Cooldown;
            Stance_Warmup Stance_Warmup = pawn.stances.curStance as Stance_Warmup;
            if (ticksToNextBurstShot > 10)
            {
                ticksToNextBurstShot = 10;
            }
            //atkType = 2; // 공격타입 테스트  // attack type test
            float ani_burst = (float)ticksToNextBurstShot;
            float ani_cool = (float)stance_Busy.ticksLeft;
            float ani = 0f;
            if (!isMechanoid)
            {
                ani = Mathf.Max(ani_cool, 25f) * 0.001f;
            }
            if (ticksToNextBurstShot > 0)
            {
                ani = ani_burst * 0.02f;
            }
            float addX = 0;
            float addZ = 0;
            // 준비동작 애니메이션 // preparation animation
            if (!isMechanoid)
            {
                float wiggle_slow = 0f;
                if (!isSub)
                {
                    wiggle_slow = Mathf.Sin(ani_cool * 0.035f) * 0.05f;
                }
                else
                {
                    wiggle_slow = Mathf.Sin(ani_cool * 0.035f + 0.5f) * 0.05f;
                }
                switch (atkType)
                {
                    case 0:
                        // 회전 // rotation
                        if (stance_Busy.ticksLeft > 1)
                        {
                            addZ += wiggle_slow;
                        }
                        break;
                    case 1:
                        // 재장전 // reload
                        if (ticksToNextBurstShot == 0)
                        {
                            if (stance_Busy.ticksLeft > 78)
                            {

                            }
                            else if (stance_Busy.ticksLeft > 48 && Stance_Warmup == null)
                            {
                                float wiggle = Mathf.Sin(ani_cool * 0.1f) * 0.05f;
                                addX += wiggle - 0.2f;
                                addZ += wiggle + 0.2f;
                                angleOffset += wiggle + 30f + ani_cool * 0.5f;
                            }
                            else if (stance_Busy.ticksLeft > 40 && Stance_Warmup == null)
                            {
                                float wiggle = Mathf.Sin(ani_cool * 0.1f) * 0.05f;
                                float wiggle_fast = Mathf.Sin(ani_cool) * 0.05f;
                                addX += wiggle_fast + 0.05f;
                                addZ += wiggle - 0.05f;
                                angleOffset += wiggle_fast * 100f - 15f;

                            }
                            else if (stance_Busy.ticksLeft > 1)
                            {
                                addZ += wiggle_slow;
                            }

                        }
                        break;
                    default:
                        if (stance_Busy.ticksLeft > 1)
                        {
                            addZ += wiggle_slow;
                        }
                        break;
                }
            }

            if (pawn.Rotation == Rot4.West)
            {
                drawOffset = new Vector3(addZ, 0, 0.0f + addX - ani);//.RotatedBy(aimAngle);
            }
            else
            {
                drawOffset = new Vector3(-addZ, 0, 0.0f + addX - ani);//.RotatedBy(aimAngle);
            }


            float reboundFactor = 70f;
            if (pawn.Rotation == Rot4.South)
            {
                angleOffset = ani * reboundFactor - angleOffset;
            }
            if (pawn.Rotation == Rot4.North)
            {
                angleOffset = ani * reboundFactor - angleOffset;
            }
            if (pawn.Rotation == Rot4.East)
            {
                angleOffset = ani * reboundFactor - angleOffset;
            }
            if (pawn.Rotation == Rot4.West)
            {
                angleOffset = ani * reboundFactor - angleOffset;
            }
        }

        public static void AnimationOffsetsIdle(Pawn pawn, Thing weapon, bool useTwirl, Vector3 offset, out Vector3 drawOffset, float aimAngle, out float angleOffset, bool isSub = false, bool log = false)
        {
            drawOffset = Vector3.zero;
            angleOffset = 0f;
            int tick = Mathf.Abs(pawn.HashOffsetTicks() % 1000000000);
            tick %= 100000000;
            tick %= 10000000;
            tick %= 1000000;
            tick %= 100000;
            tick %= 10000;
            tick %= 1000;
            float wiggle;
            if (!isSub)
            {
                wiggle = Mathf.Sin((float)tick * 0.05f);
            }
            else
            {
                wiggle = Mathf.Sin((float)tick * 0.05f + 1.5f);
            }
            float aniAngle = -5f;


            if (useTwirl)
            {
                if (!isSub)
                {
                    if (tick < 80 && tick >= 40)
                    {
                        angleOffset += (float)tick * 36f;
                        drawOffset += new Vector3(0f, 0f, 0.1f);
                    }
                }
                else
                {
                    if (tick < 40)
                    {
                        angleOffset += (float)(tick - 40) * -36f;
                        drawOffset += new Vector3(0f, 0f, 0.1f);
                    }
                }
            }
            float angle = aimAngle;
            if (pawn.Rotation == Rot4.South)
            {
                drawOffset = new Vector3(0f, offset.z, wiggle * 0.05f);
                if (isSub)
                {
                    aniAngle *= -1f;
                }
                drawOffset.y += 0.03787879f;
            }
            if (pawn.Rotation == Rot4.North)
            {
                drawOffset = new Vector3(0f, offset.z, wiggle * 0.05f);
                if (isSub)
                {
                    aniAngle *= -1f;
                }
            }
            if (pawn.Rotation == Rot4.East)
            {
                drawOffset = new Vector3(0f, offset.z, wiggle * 0.05f);
                drawOffset.y += 0.03787879f;
            }
            if (pawn.Rotation == Rot4.West)
            {
                angle = 360 - aimAngle;
                drawOffset = new Vector3(0f, offset.z, wiggle * 0.05f);
                drawOffset.y += 0.03787879f;
            }
            angleOffset = (angleOffset + angle + wiggle * aniAngle) - angle;
            drawOffset = drawOffset.RotatedBy(angleOffset);

        }

        public static void AnimateEquipment(PawnRenderer __instance, Vector3 inLoc, out Vector3 outLoc, float inAngle, out float outAngle, ThingWithComps thing, Stance_Busy stance_Busy, Vector3 offset, bool isSub = false)
        {
            Pawn pawn = __instance.pawn;
            outAngle = inAngle;
            outLoc = inLoc;
            Vector3 rootLoc2 = inLoc;
            bool isMechanoid = pawn.RaceProps.IsMechanoid;
            bool log = Prefs.DevMode && Find.Selector.SingleSelectedThing == pawn;
            // 설정과 무기 무게에 따른 회전 애니메이션 사용 여부 // Whether to use rotation animations based on settings and weapon weight
            bool useTwirl = yayoCombat.ani_twirl && !pawn.RaceProps.IsMechanoid && thing.def.BaseMass < 5f;

            if (stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
            {
                if (thing.def.IsRangedWeapon && !stance_Busy.verb.IsMeleeAttack)
                {
                    // 원거리용 // for Ranged
                    //if (log) Log.Message((pawn.LastAttackTargetTick + thing.thingIDNumber).ToString());
                    int ticksToNextBurstShot = stance_Busy.verb.ticksToNextBurstShot;
                    int atkType = (pawn.LastAttackTargetTick + thing.thingIDNumber) % 10000 % 1000 % 100 % 5; // 랜덤 공격타입 결정 // Random attack type determination
                    Stance_Cooldown Stance_Cooldown = pawn.stances.curStance as Stance_Cooldown;
                    Stance_Warmup Stance_Warmup = pawn.stances.curStance as Stance_Warmup;
                    if (ticksToNextBurstShot > 10)
                    {
                        ticksToNextBurstShot = 10;
                    }
                    //atkType = 2; // 공격타입 테스트  // attack type test
                    float ani_burst = (float)ticksToNextBurstShot;
                    float ani_cool = (float)stance_Busy.ticksLeft;
                    float ani = 0f;
                    if (!isMechanoid)
                    {
                        ani = Mathf.Max(ani_cool, 25f) * 0.001f;
                    }
                    if (ticksToNextBurstShot > 0)
                    {
                        ani = ani_burst * 0.02f;
                    }
                    float addAngle = 0f;
                    float addX = offset.x;
                    float addZ = offset.z;
                    // 준비동작 애니메이션 // preparation animation
                    if (!isMechanoid)
                    {
                        float wiggle_slow = 0f;
                        if (!isSub)
                        {
                            wiggle_slow = Mathf.Sin(ani_cool * 0.035f) * 0.05f;
                        }
                        else
                        {
                            wiggle_slow = Mathf.Sin(ani_cool * 0.035f + 0.5f) * 0.05f;
                        }
                        switch (atkType)
                        {
                            case 0:
                                // 회전 // rotation
                                if (useTwirl)
                                {
                                    /*
                                    if (stance_Busy.ticksLeft < 35 && stance_Busy.ticksLeft > 10 && ticksToNextBurstShot == 0 && Stance_Warmup == null)
                                    {
                                        addAngle += ani_cool * 50f + 180f;
                                    }
                                    else if (stance_Busy.ticksLeft > 1)
                                    {
                                        addY += wiggle_slow;
                                    }
                                    */
                                }
                                else
                                {
                                    if (stance_Busy.ticksLeft > 1)
                                    {
                                        addZ += wiggle_slow;
                                    }
                                }
                                break;
                            case 1:
                                // 재장전 // reload
                                if (ticksToNextBurstShot == 0)
                                {
                                    if (stance_Busy.ticksLeft > 78)
                                    {

                                    }
                                    else if (stance_Busy.ticksLeft > 48 && Stance_Warmup == null)
                                    {
                                        float wiggle = Mathf.Sin(ani_cool * 0.1f) * 0.05f;
                                        addX += wiggle - 0.2f;
                                        addZ += wiggle + 0.2f;
                                        addAngle += wiggle + 30f + ani_cool * 0.5f;
                                    }
                                    else if (stance_Busy.ticksLeft > 40 && Stance_Warmup == null)
                                    {
                                        float wiggle = Mathf.Sin(ani_cool * 0.1f) * 0.05f;
                                        float wiggle_fast = Mathf.Sin(ani_cool) * 0.05f;
                                        addX += wiggle_fast + 0.05f;
                                        addZ += wiggle - 0.05f;
                                        addAngle += wiggle_fast * 100f - 15f;

                                    }
                                    else if (stance_Busy.ticksLeft > 1)
                                    {
                                        addZ += wiggle_slow;
                                    }

                                }
                                break;
                            default:
                                if (stance_Busy.ticksLeft > 1)
                                {
                                    addZ += wiggle_slow;
                                }
                                break;
                        }
                    }

                    Vector3 drawLoc = Vector3.zero;

                    if (pawn.Rotation == Rot4.West)
                    {
                        drawLoc = rootLoc2 + new Vector3(addZ, offset.y, 0.0f + addX - ani).RotatedBy(inAngle);
                    }
                    else
                    {
                        drawLoc = rootLoc2 + new Vector3(-addZ, offset.y, 0.0f + addX - ani).RotatedBy(inAngle);
                    }

                    if (offset.y >= 0f)
                    {
                        drawLoc.y += 0.03787879f;
                    }

                    // 반동 계수 // recoil coefficient
                    float reboundFactor = 70f;
                    if (pawn.Rotation == Rot4.South)
                    {
                        outAngle = inAngle - ani * reboundFactor - addAngle;
                        //    __instance.DrawEquipmentAiming(thing, drawLoc, inAngle - ani * reboundFactor - addAngle, pawn);//, stance_Busy, isSub);
                        //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num - ani * reboundFactor - addAngle });
                    }
                    if (pawn.Rotation == Rot4.North)
                    {
                        outAngle = inAngle - ani * reboundFactor - addAngle;
                        //    __instance.DrawEquipmentAiming(thing, drawLoc, inAngle - ani * reboundFactor - addAngle, pawn);//, stance_Busy, isSub);
                        //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num - ani * reboundFactor - addAngle });
                    }
                    if (pawn.Rotation == Rot4.East)
                    {
                        outAngle = inAngle - ani * reboundFactor - addAngle;
                        //    __instance.DrawEquipmentAiming(thing, drawLoc, inAngle - ani * reboundFactor - addAngle, pawn);//, stance_Busy, isSub);
                        //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num - ani * reboundFactor - addAngle });
                    }
                    if (pawn.Rotation == Rot4.West)
                    {
                        outAngle = inAngle - ani * reboundFactor - addAngle;
                        //    __instance.DrawEquipmentAiming(thing, drawLoc, inAngle - ani * reboundFactor - addAngle, pawn);//, stance_Busy, isSub);
                        //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num + ani * reboundFactor + addAngle });
                    }
                    outLoc = drawLoc;
                    return;
                }
                else
                {
                    // 근접용 // for Melee
                    //if (log) Log.Message("A");
                    float addAngle = 0f;
                    int atkType = (pawn.LastAttackTargetTick + thing.thingIDNumber) % 10000 % 1000 % 100 % 3; // 랜덤 공격타입 결정 // Random attack type determination
                    //if (log) Log.Message("B");
                    //atkType = 1; // 공격 타입 테스트 // attack type test
                    // 공격 타입에 따른 각도 // Angle according to attack type
                    switch (atkType)
                    {
                        // 낮을수록 위로, 높을수록 아래로 휘두름 // The lower the swing, the higher the swing
                        default:
                            // 평범 // common
                            addAngle = 0f;
                            break;
                        case 1:
                            // 내려찍기 // take down
                            addAngle = 25f;
                            break;
                        case 2:
                            // 머리찌르기 // head stab
                            addAngle = -25f;
                            break;
                    }
                    //if (log) Log.Message("C");
                    // 원거리 무기일경우 각도보정 // Angle correction for ranged weapons
                    if (thing.def.IsRangedWeapon)
                    {
                        addAngle -= 35f;
                    }
                    //if (log) Log.Message("D");
                    float readyZ = isSub && pawn.Rotation == Rot4.West ? -0.2f : 0.2f;
                    //if (log) Log.Message("E");
                    float num2 = inAngle;// GetAimingRotation(pawn, stance_Busy.focusTarg);
                    if (stance_Busy.ticksLeft > 0)
                    {
                        //if (log) Log.Message("F");
                        // 애니메이션 // animation
                        float ani = Mathf.Min((float)stance_Busy.ticksLeft, 60f);
                        float ani2 = ani * 0.0075f; // 0.45f -> 0f
                        float addZ = offset.x;
                        float addX = offset.z;
                        switch (atkType)
                        {
                            default: // 평범한 공격 // ordinary attack
                                // 높을 수록 무기를 적쪽으로 내밀음 // The higher it is, the more the weapon is pushed toward the enemy.
                                addZ += readyZ + 0.05f + ani2;
                                // 높을수록 무기를 아래까지 내려침 // The higher it is, the lower the weapon is.
                                addX += 0.45f - 0.5f - ani2 * 0.1f;
                                break;
                            case 1: // 내려찍기 // take down
                                // 높을 수록 무기를 적쪽으로 내밀음  // The higher it is, the more the weapon is pushed toward the enemy.
                                addZ += readyZ + 0.05f + ani2;
                                // 높을수록 무기를 아래까지 내려침, 애니메이션 반대방향 // The higher, the lower the weapon, the opposite of the animation.
                                addX += 0.45f - 0.35f + ani2 * 0.5f;
                                // 각도 고정값 + 각도 변화량 // Angle fixed value + angle change amount
                                ani = 30f + ani * 0.5f;
                                break;
                            case 2: // 머리찌르기 // head stab
                                // 높을 수록 무기를 적쪽으로 내밀음 // The higher it is, the more the weapon is pushed toward the enemy.
                                addZ += readyZ + 0.05f + ani2;
                                // 높을수록 무기를 아래까지 내려침 // The higher, the lower the weapon, the opposite of the animation.
                                addX += 0.45f - 0.35f - ani2;
                                break;
                        }
                        // 회전 애니메이션 // rotation animation
                        if (useTwirl && pawn.LastAttackTargetTick % 5 == 0 && stance_Busy.ticksLeft <= 25)
                        {
                            //addAngle += ani2 * 5000f;
                        }
                        // 캐릭터 방향에 따라 적용 // Applied according to character orientation
                        if (pawn.Rotation == Rot4.South)
                        {
                            Vector3 drawLoc = rootLoc2 + new Vector3(-addX, offset.y, addZ).RotatedBy(num2);
                            if (offset.y >= 0f)
                            {
                                drawLoc.y += 0.03787879f;
                            }
                            num2 += addAngle;
                            outLoc = drawLoc;
                            outAngle = inAngle + ani;
                            //    __instance.DrawEquipmentAiming(thing, drawLoc, inAngle + ani, pawn);//, stance_Busy, isSub);
                            //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num + ani });
                        }
                        if (pawn.Rotation == Rot4.North)
                        {
                            Vector3 drawLoc = rootLoc2 + new Vector3(-addX, offset.y, addZ).RotatedBy(num2);
                            if (offset.y >= 0f)
                            {
                                drawLoc.y += 0.03787879f;
                            }
                            num2 += addAngle;
                            outLoc = drawLoc;
                            outAngle = inAngle + ani;
                            //    __instance.DrawEquipmentAiming(thing, drawLoc, inAngle + ani, pawn);//, stance_Busy, isSub);
                            //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num + ani });
                        }
                        if (pawn.Rotation == Rot4.East)
                        {
                            Vector3 drawLoc = rootLoc2 + new Vector3(addX, offset.y, addZ).RotatedBy(num2);
                            if (offset.y >= 0f)
                            {
                                drawLoc.y += 0.03787879f;
                            }
                            num2 += addAngle;
                            outLoc = drawLoc;
                            outAngle = num2 + ani;
                            //    __instance.DrawEquipmentAiming(thing, drawLoc, num2 + ani, pawn);//, stance_Busy, isSub);
                            //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num + ani });
                        }
                        if (pawn.Rotation == Rot4.West)
                        {
                            Vector3 drawLoc = rootLoc2 + new Vector3(addX, offset.y, addZ).RotatedBy(num2);
                            if (offset.y >= 0f)
                            {
                                drawLoc.y += 0.03787879f;
                            }
                            num2 -= addAngle;
                            outLoc = drawLoc;
                            outAngle = num2 + ani;
                            //    __instance.DrawEquipmentAiming(thing, drawLoc, num2 + ani, pawn);//, stance_Busy, isSub);
                            //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num - ani });
                        }
                    }
                    else
                    {
                        Vector3 drawLoc = rootLoc2 + new Vector3(isSub && pawn.Rotation == Rot4.West ? -0.2f : 0.0f, offset.y, readyZ).RotatedBy(inAngle);
                        if (offset.y >= 0f)
                        {
                            drawLoc.y += 0.03787879f;
                        }
                        outLoc = drawLoc;
                        outAngle = inAngle;
                        //    __instance.DrawEquipmentAiming(thing, drawLoc, inAngle, pawn);//, stance_Busy, isSub);
                        //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num });
                    }
                    return;
                }
            }
            //    if (log) Log.Message("대기 : Waiting");
            // 대기 // Waiting
            if ((pawn.carryTracker == null || pawn.carryTracker.CarriedThing == null) && (pawn.Drafted || (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon) || (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon)))
            {
                int tick = Mathf.Abs(pawn.HashOffsetTicks() % 1000000000);
                tick %= 100000000;
                tick %= 10000000;
                tick %= 1000000;
                tick %= 100000;
                tick %= 10000;
                tick %= 1000;
                float wiggle;
                if (!isSub)
                {
                    wiggle = Mathf.Sin((float)tick * 0.05f);
                }
                else
                {
                    wiggle = Mathf.Sin((float)tick * 0.05f + 0.5f);
                }
                float aniAngle = -5f;
                float addAngle = 0f;
                
                if (useTwirl)
                {
                    if (!isSub)
                    {
                        if (tick < 80 && tick >= 40)
                        {
                            addAngle += (float)tick * 36f;
                        //    rootLoc2 += new Vector3(-0.2f, 0f, 0.1f);
                        }
                    }
                    else
                    {
                        if (tick < 40)
                        {
                            addAngle += (float)(tick - 40) * -36f;
                        //    rootLoc2 += new Vector3(0.2f, 0f, 0.1f);
                        }
                    }
                }
                
                Vector3 drawLoc = Vector3.zero;
                if (pawn.Rotation == Rot4.South)
                {
                    float angle = inAngle; //143f;
                    if (!isSub)
                    {
                        drawLoc = rootLoc2 + new Vector3(0f, offset.y, wiggle * 0.05f);
                        //    angle = 143f;
                    }
                    else
                    {
                        drawLoc = rootLoc2 + new Vector3(0f, offset.y, wiggle * 0.05f);
                        angle = 350f - inAngle;
                        aniAngle *= -1f;
                    }
                    if (offset.y >= 0f)
                    {
                        drawLoc.y += 0.03787879f;
                    }
                //    outAngle = addAngle + angle + wiggle * aniAngle;
                    outLoc = drawLoc;
                    //    __instance.DrawEquipmentAiming(thing, drawLoc, addAngle + angle + wiggle * aniAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc2, addAngle + angle + wiggle * aniAngle });
                    return;
                }
                if (pawn.Rotation == Rot4.North)
                {
                    float angle = inAngle; //143f;
                    if (!isSub)
                    {
                        drawLoc = rootLoc2 + new Vector3(0f, offset.y,  wiggle * 0.05f);
                        //    angle = 143f;
                    }
                    else
                    {
                        drawLoc = rootLoc2 + new Vector3(0f, offset.y,  wiggle * 0.05f);
                        angle = 350f - inAngle;
                        aniAngle *= -1f;
                    }
                    drawLoc.y += 0f;
                    outLoc = drawLoc;
                    outAngle = addAngle + angle + wiggle * aniAngle;
                    //    __instance.DrawEquipmentAiming(thing, drawLoc3, addAngle + angle + wiggle * aniAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc3, addAngle + angle + wiggle * aniAngle });
                    return;
                }
                if (pawn.Rotation == Rot4.East)
                {
                    float angle = inAngle; //143f;
                    if (!isSub)
                    {
                        drawLoc = rootLoc2 + new Vector3(0.0f, offset.y, wiggle * 0.05f);
                        //    angle = 143f;
                    }
                    else
                    {
                        drawLoc = rootLoc2 + new Vector3(0.0f, offset.y,  wiggle * 0.05f);
                        aniAngle *= -1f;
                    }
                    if (offset.y >= 0f)
                    {
                        drawLoc.y += 0.03787879f;
                    }
                    outLoc = drawLoc;
                    outAngle = addAngle + angle + wiggle * aniAngle;
                    //    __instance.DrawEquipmentAiming(thing, drawLoc4, addAngle + angle + wiggle * aniAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc4, addAngle + angle + wiggle * aniAngle });
                    return;
                }
                if (pawn.Rotation == Rot4.West)
                {
                    float angle = 350f - inAngle; //217f;
                    if (!isSub)
                    {
                        drawLoc = rootLoc2 + new Vector3(-0.0f, offset.y,  wiggle * 0.05f);
                        //    angle = 217f;
                    }
                    else
                    {
                        drawLoc = rootLoc2 + new Vector3(-0.0f, offset.y,  wiggle * 0.05f);
                        //    angle = 350f - num;
                        aniAngle *= -1f;
                    }
                    if (offset.y >= 0f)
                    {
                        drawLoc.y += 0.03787879f;
                    }
                    outLoc = drawLoc;
                    outAngle = addAngle + angle + wiggle * aniAngle;
                    //    __instance.DrawEquipmentAiming(thing, drawLoc5, addAngle + angle + wiggle * aniAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc5, addAngle + angle + wiggle * aniAngle });
                    return;
                }
            }
            return;
        }
    
        static public void animateEquip(PawnRenderer __instance, Pawn pawn, Vector3 rootLoc, float num, ThingWithComps thing, Stance_Busy stance_Busy, Vector3 offset, bool isSub = false)
        {
            Vector3 rootLoc2 = rootLoc;
            bool isMechanoid = pawn.RaceProps.IsMechanoid;
            bool log = Prefs.DevMode && Find.Selector.SingleSelectedThing == pawn;
            // 설정과 무기 무게에 따른 회전 애니메이션 사용 여부 // Whether to use rotation animations based on settings and weapon weight
            bool useTwirl = yayoCombat.ani_twirl && !pawn.RaceProps.IsMechanoid && thing.def.BaseMass < 5f;

            if (stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
            {
                if (thing.def.IsRangedWeapon && !stance_Busy.verb.IsMeleeAttack)
                {
                    // 원거리용 // for Ranged
                    //if (log) Log.Message((pawn.LastAttackTargetTick + thing.thingIDNumber).ToString());
                    int ticksToNextBurstShot = stance_Busy.verb.ticksToNextBurstShot;
                    int atkType = (pawn.LastAttackTargetTick + thing.thingIDNumber) % 10000 % 1000 % 100 % 5; // 랜덤 공격타입 결정 // Random attack type determination
                    Stance_Cooldown Stance_Cooldown = pawn.stances.curStance as Stance_Cooldown;
                    Stance_Warmup Stance_Warmup = pawn.stances.curStance as Stance_Warmup;
                    if (ticksToNextBurstShot > 10)
                    {
                        ticksToNextBurstShot = 10;
                    }
                    //atkType = 2; // 공격타입 테스트  // attack type test
                    float ani_burst = (float)ticksToNextBurstShot;
                    float ani_cool = (float)stance_Busy.ticksLeft;
                    float ani = 0f;
                    if (!isMechanoid)
                    {
                        ani = Mathf.Max(ani_cool, 25f) * 0.001f;
                    }
                    if (ticksToNextBurstShot > 0)
                    {
                        ani = ani_burst * 0.02f;
                    }
                    float addAngle = 0f;
                    float addX = offset.x;
                    float addZ = offset.z;
                    // 준비동작 애니메이션 // preparation animation
                    if (!isMechanoid)
                    {
                        float wiggle_slow = 0f;
                        if (!isSub)
                        {
                            wiggle_slow = Mathf.Sin(ani_cool * 0.035f) * 0.05f;
                        }
                        else
                        {
                            wiggle_slow = Mathf.Sin(ani_cool * 0.035f + 0.5f) * 0.05f;
                        }
                        switch (atkType)
                        {
                            case 0:
                                // 회전 // rotation
                                if (useTwirl)
                                {
                                    /*
                                    if (stance_Busy.ticksLeft < 35 && stance_Busy.ticksLeft > 10 && ticksToNextBurstShot == 0 && Stance_Warmup == null)
                                    {
                                        addAngle += ani_cool * 50f + 180f;
                                    }
                                    else if (stance_Busy.ticksLeft > 1)
                                    {
                                        addY += wiggle_slow;
                                    }
                                    */
                                }
                                else
                                {
                                    if (stance_Busy.ticksLeft > 1)
                                    {
                                        addZ += wiggle_slow;
                                    }
                                }
                                break;
                            case 1:
                                // 재장전 // reload
                                if (ticksToNextBurstShot == 0)
                                {
                                    if (stance_Busy.ticksLeft > 78)
                                    {

                                    }
                                    else if (stance_Busy.ticksLeft > 48 && Stance_Warmup == null)
                                    {
                                        float wiggle = Mathf.Sin(ani_cool * 0.1f) * 0.05f;
                                        addX += wiggle - 0.2f;
                                        addZ += wiggle + 0.2f;
                                        addAngle += wiggle + 30f + ani_cool * 0.5f;
                                    }
                                    else if (stance_Busy.ticksLeft > 40 && Stance_Warmup == null)
                                    {
                                        float wiggle = Mathf.Sin(ani_cool * 0.1f) * 0.05f;
                                        float wiggle_fast = Mathf.Sin(ani_cool) * 0.05f;
                                        addX += wiggle_fast + 0.05f;
                                        addZ += wiggle - 0.05f;
                                        addAngle += wiggle_fast * 100f - 15f;

                                    }
                                    else if (stance_Busy.ticksLeft > 1)
                                    {
                                        addZ += wiggle_slow;
                                    }

                                }
                                break;
                            default:
                                if (stance_Busy.ticksLeft > 1)
                                {
                                    addZ += wiggle_slow;
                                }
                                break;
                        }
                    }

                    Vector3 drawLoc = Vector3.zero;

                    if (pawn.Rotation == Rot4.West)
                    {
                        drawLoc = rootLoc2 + new Vector3(addZ, offset.y, 0.4f + addX - ani).RotatedBy(num);
                    }
                    else
                    {
                        drawLoc = rootLoc2 + new Vector3(-addZ, offset.y, 0.4f + addX - ani).RotatedBy(num);
                    }

                    if (offset.y >= 0f)
                    {
                        drawLoc.y += 0.03787879f;
                    }

                    // 반동 계수 // recoil coefficient
                    float reboundFactor = 70f;
                    if (pawn.Rotation == Rot4.South)
                    {
                        __instance.DrawEquipmentAiming(thing, drawLoc, num - ani * reboundFactor - addAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num - ani * reboundFactor - addAngle });
                    }
                    if (pawn.Rotation == Rot4.North)
                    {
                        __instance.DrawEquipmentAiming(thing, drawLoc, num - ani * reboundFactor - addAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num - ani * reboundFactor - addAngle });
                    }
                    if (pawn.Rotation == Rot4.East)
                    {
                        __instance.DrawEquipmentAiming(thing, drawLoc, num - ani * reboundFactor - addAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num - ani * reboundFactor - addAngle });
                    }
                    if (pawn.Rotation == Rot4.West)
                    {
                        __instance.DrawEquipmentAiming(thing, drawLoc, num - ani * reboundFactor - addAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num + ani * reboundFactor + addAngle });
                    }
                    return;
                }
                else
                {
                    // 근접용 // for Melee
                    //if (log) Log.Message("A");
                    float addAngle = 0f;
                    int atkType = (pawn.LastAttackTargetTick + thing.thingIDNumber) % 10000 % 1000 % 100 % 3; // 랜덤 공격타입 결정 // Random attack type determination
                    //if (log) Log.Message("B");
                    //atkType = 1; // 공격 타입 테스트 // attack type test
                    // 공격 타입에 따른 각도 // Angle according to attack type
                    switch (atkType)
                    {
                        // 낮을수록 위로, 높을수록 아래로 휘두름 // The lower the swing, the higher the swing
                        default:
                            // 평범 // common
                            addAngle = 0f;
                            break;
                        case 1:
                            // 내려찍기 // take down
                            addAngle = 25f;
                            break;
                        case 2:
                            // 머리찌르기 // head stab
                            addAngle = -25f; 
                            break;
                    }
                    //if (log) Log.Message("C");
                    // 원거리 무기일경우 각도보정 // Angle correction for ranged weapons
                    if (thing.def.IsRangedWeapon)
                    {
                        addAngle -= 35f;
                    }
                    //if (log) Log.Message("D");
                    float readyZ = isSub && pawn.Rotation == Rot4.West  ?- 0.2f : 0.2f;
                    //if (log) Log.Message("E");
                    float num2 = GetAimingRotation(pawn, stance_Busy.focusTarg);
                    if (stance_Busy.ticksLeft > 0)
                    {
                        //if (log) Log.Message("F");
                        // 애니메이션 // animation
                        float ani = Mathf.Min((float)stance_Busy.ticksLeft, 60f);
                        float ani2 = ani * 0.0075f; // 0.45f -> 0f
                        float addZ = offset.x;
                        float addX = offset.z;
                        switch (atkType)
                        {
                            default: // 평범한 공격 // ordinary attack
                                // 높을 수록 무기를 적쪽으로 내밀음 // The higher it is, the more the weapon is pushed toward the enemy.
                                addZ += readyZ + 0.05f + ani2;
                                // 높을수록 무기를 아래까지 내려침 // The higher it is, the lower the weapon is.
                                addX += 0.45f - 0.5f - ani2 * 0.1f; 
                                break;
                            case 1: // 내려찍기 // take down
                                // 높을 수록 무기를 적쪽으로 내밀음  // The higher it is, the more the weapon is pushed toward the enemy.
                                addZ += readyZ + 0.05f + ani2;
                                // 높을수록 무기를 아래까지 내려침, 애니메이션 반대방향 // The higher, the lower the weapon, the opposite of the animation.
                                addX += 0.45f - 0.35f + ani2 * 0.5f;
                                // 각도 고정값 + 각도 변화량 // Angle fixed value + angle change amount
                                ani = 30f + ani * 0.5f;
                                break;
                            case 2: // 머리찌르기 // head stab
                                // 높을 수록 무기를 적쪽으로 내밀음 // The higher it is, the more the weapon is pushed toward the enemy.
                                addZ += readyZ + 0.05f + ani2;
                                // 높을수록 무기를 아래까지 내려침 // The higher, the lower the weapon, the opposite of the animation.
                                addX += 0.45f - 0.35f - ani2;
                                break;
                        }
                        // 회전 애니메이션 // rotation animation
                        if (useTwirl && pawn.LastAttackTargetTick % 5 == 0 && stance_Busy.ticksLeft <= 25)
                        {
                            //addAngle += ani2 * 5000f;
                        }
                        // 캐릭터 방향에 따라 적용 // Applied according to character orientation
                        if (pawn.Rotation == Rot4.South)
                        {
                            Vector3 drawLoc = rootLoc2 + new Vector3(-addX, offset.y, addZ).RotatedBy(num2);
                            if (offset.y >= 0f)
                            {
                                drawLoc.y += 0.03787879f;
                            }
                            num2 += addAngle;
                            __instance.DrawEquipmentAiming(thing, drawLoc, num+ ani, pawn);//, stance_Busy, isSub);
                            //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num + ani });
                        }
                        if (pawn.Rotation == Rot4.North)
                        {
                            Vector3 drawLoc = rootLoc2 + new Vector3(-addX, offset.y, addZ).RotatedBy(num2);
                            if (offset.y >= 0f)
                            {
                                drawLoc.y += 0.03787879f;
                            }
                            num2 += addAngle;
                            __instance.DrawEquipmentAiming(thing, drawLoc, num + ani, pawn);//, stance_Busy, isSub);
                            //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num + ani });
                        }
                        if (pawn.Rotation == Rot4.East)
                        {
                            Vector3 drawLoc = rootLoc2 + new Vector3(addX, offset.y, addZ).RotatedBy(num2);
                            if (offset.y >= 0f)
                            {
                                drawLoc.y += 0.03787879f;
                            }
                            num2 += addAngle;
                            __instance.DrawEquipmentAiming(thing, drawLoc, num2 + ani, pawn);//, stance_Busy, isSub);
                            //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num + ani });
                        }
                        if (pawn.Rotation == Rot4.West)
                        {
                            Vector3 drawLoc = rootLoc2 + new Vector3(addX, offset.y, addZ).RotatedBy(num2);
                            if (offset.y >= 0f)
                            {
                                drawLoc.y += 0.03787879f;
                            }
                            num2 -= addAngle;
                            __instance.DrawEquipmentAiming(thing, drawLoc, num2 + ani, pawn);//, stance_Busy, isSub);
                            //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num - ani });
                        }
                    }
                    else
                    {
                        Vector3 drawLoc = rootLoc2 + new Vector3(isSub && pawn.Rotation == Rot4.West ? -0.2f : 0.0f, offset.y, readyZ).RotatedBy(num);
                        if (offset.y >= 0f)
                        {
                            drawLoc.y += 0.03787879f;
                        }
                        __instance.DrawEquipmentAiming(thing, drawLoc, num, pawn);//, stance_Busy, isSub);
                        //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc, num });
                    }
                    return;
                }
            }
        //    if (log) Log.Message("대기 : Waiting");
            // 대기 // Waiting
            if ((pawn.carryTracker == null || pawn.carryTracker.CarriedThing == null) && (pawn.Drafted || (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon) || (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon)))
            {
                int tick = Mathf.Abs(pawn.HashOffsetTicks() % 1000000000);
                tick = tick % 100000000;
                tick = tick % 10000000;
                tick = tick % 1000000;
                tick = tick % 100000;
                tick = tick % 10000;
                tick = tick % 1000;
                float wiggle = 0f;
                if (!isSub)
                {
                    wiggle = Mathf.Sin((float)tick * 0.05f);
                }
                else
                {
                    wiggle = Mathf.Sin((float)tick * 0.05f + 0.5f);
                }
                float aniAngle = -5f;
                float addAngle = 0f;
                if (useTwirl)
                {
                    if (!isSub)
                    {
                        if (tick < 80 && tick >= 40)
                        {
                            addAngle += (float)tick * 36f;
                            rootLoc2 += new Vector3(-0.2f, 0f, 0.1f);
                        }
                    }
                    else
                    {
                        if (tick < 40)
                        {
                            addAngle += (float)(tick - 40) * -36f;
                            rootLoc2 += new Vector3(0.2f, 0f, 0.1f);
                        }
                    }
                }
                if (pawn.Rotation == Rot4.South)
                {
                    Vector3 drawLoc2 = Vector3.zero;
                    float angle = num; //143f;
                    if (!isSub)
                    {
                        drawLoc2 = rootLoc2 + new Vector3(0f, offset.y, -0.22f + wiggle * 0.05f);
                        //    angle = 143f;
                    }
                    else
                    {
                        drawLoc2 = rootLoc2 + new Vector3(0f, offset.y, -0.22f + wiggle * 0.05f);
                        angle = 350f - num;
                        aniAngle *= -1f;
                    }
                    if (offset.y >= 0f)
                    {
                        drawLoc2.y += 0.03787879f;
                    }
                    __instance.DrawEquipmentAiming(thing, drawLoc2, addAngle + angle + wiggle * aniAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc2, addAngle + angle + wiggle * aniAngle });
                    return;
                }
                if (pawn.Rotation == Rot4.North)
                {
                    Vector3 drawLoc3 = Vector3.zero;
                    float angle = num; //143f;
                    if (!isSub)
                    {
                        drawLoc3 = rootLoc2 + new Vector3(0f, offset.y, -0.11f + wiggle * 0.05f);
                        //    angle = 143f;
                    }
                    else
                    {
                        drawLoc3 = rootLoc2 + new Vector3(0f, offset.y, -0.11f + wiggle * 0.05f);
                        angle = 350f - num;
                        aniAngle *= -1f;
                    }
                    drawLoc3.y += 0f;
                    __instance.DrawEquipmentAiming(thing, drawLoc3, addAngle + angle + wiggle * aniAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc3, addAngle + angle + wiggle * aniAngle });
                    return;
                }
                if (pawn.Rotation == Rot4.East)
                {
                    Vector3 drawLoc4 = Vector3.zero;
                    float angle = num; //143f;
                    if (!isSub)
                    {
                        drawLoc4 = rootLoc2 + new Vector3(0.2f, offset.y, -0.22f + wiggle * 0.05f);
                        //    angle = 143f;
                    }
                    else
                    {
                        drawLoc4 = rootLoc2 + new Vector3(0.2f, offset.y, -0.22f + wiggle * 0.05f);
                        aniAngle *= -1f;
                    }
                    if (offset.y >= 0f)
                    {
                        drawLoc4.y += 0.03787879f;
                    }
                    __instance.DrawEquipmentAiming(thing, drawLoc4, addAngle + angle + wiggle * aniAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc4, addAngle + angle + wiggle * aniAngle });
                    return;
                }
                if (pawn.Rotation == Rot4.West)
                {
                    Vector3 drawLoc5 = Vector3.zero;
                    float angle = 350f - num; //217f;
                    if (!isSub)
                    {
                        drawLoc5 = rootLoc2 + new Vector3(-0.2f, offset.y, -0.22f + wiggle * 0.05f);
                    //    angle = 217f;
                    }
                    else
                    {
                        drawLoc5 = rootLoc2 + new Vector3(-0.2f, offset.y, -0.22f + wiggle * 0.05f);
                    //    angle = 350f - num;
                        aniAngle *= -1f;
                    }
                    if (offset.y >= 0f)
                    {
                        drawLoc5.y += 0.03787879f;
                    }
                    __instance.DrawEquipmentAiming(thing, drawLoc5, addAngle + angle + wiggle * aniAngle, pawn);//, stance_Busy, isSub);
                    //    AccessTools.Method(typeof(PawnRenderer), "DrawEquipmentAiming").Invoke(__instance, new object[] { thing, drawLoc5, addAngle + angle + wiggle * aniAngle });
                    return;
                }
            }
            return;
        }


        public static float GetAimingRotation(Pawn pawn, LocalTargetInfo focusTarg)
        {
            Vector3 a;
            if (focusTarg.HasThing)
            {
                a = focusTarg.Thing.DrawPos;
            }
            else
            {
                a = focusTarg.Cell.ToVector3Shifted();
            }
            float num = 0f;
            if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            {
                num = (a - pawn.DrawPos).AngleFlat();
            }

            return num;
        }
        public static bool CurrentlyAiming(Stance_Busy stance)
        {
            return (stance != null && !stance.neverAimWeapon && stance.focusTarg.IsValid);
        }
        public static bool IsMeleeWeapon(ThingWithComps eq)
        {
            if (eq == null)
            {
                return false;
            }
            if (eq.TryGetComp<CompEquippable>() is CompEquippable ceq)
            {
                if (ceq.PrimaryVerb.IsMeleeAttack)
                {
                    return true;
                }
            }
            return false;
        }
    }
}