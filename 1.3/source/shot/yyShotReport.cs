using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace yayoCombat
{
	internal class yyShotReport
	{
		[HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
		public class yayoTryCastShot
		{
			[HarmonyPostfix]
			//[HarmonyPriority(0)]
			public static bool Prefix(ref bool __result, Verb_LaunchProjectile __instance, LocalTargetInfo ___currentTarget, bool ___canHitNonTargetPawnsNow)
			{
				LocalTargetInfo currentTarget = ___currentTarget;
				bool canHitNonTargetPawnsNow = ___canHitNonTargetPawnsNow;


                if (yayoCombat.advShootAcc &&
					(yayoCombat.turretAcc || __instance.CasterIsPawn) &&
					(yayoCombat.mechAcc || (__instance.CasterIsPawn && !__instance.CasterPawn.RaceProps.IsMechanoid)) &&
					(!yayoCombat.colonistAcc || (__instance.CasterIsPawn && __instance.CasterPawn.IsColonist))
					)
				{

                    //Log.Message("yayo");
                    // yayo combat code -----------------------------------------------------------------------------------------

					if (currentTarget.HasThing && currentTarget.Thing.Map != __instance.caster.Map)
					{
						__result = false;
						return false;
					}
					ThingDef projectile = __instance.Projectile;

					if (projectile == null)
					{
						__result = false;
						return false;
					}
					ShootLine shootLine;
					bool flag = __instance.TryFindShootLineFromTo(__instance.caster.Position, currentTarget, out shootLine);
					if (__instance.verbProps.stopBurstWithoutLos && !flag)
					{
						__result = false;
						return false;
					}
					if (__instance.EquipmentSource != null)
					{
						CompChangeableProjectile comp = __instance.EquipmentSource.GetComp<CompChangeableProjectile>();
						if (comp != null)
						{
							comp.Notify_ProjectileLaunched();
						}
                        CompReloadable comp2 = __instance.EquipmentSource.GetComp<CompReloadable>();
                        if (comp2 != null)
                        {
                            comp2.UsedOnce();
                        }
                    }
					Thing launcher = __instance.caster;
					Thing equipment = __instance.EquipmentSource;
					CompMannable compMannable = __instance.caster.TryGetComp<CompMannable>();
					if (compMannable != null && compMannable.ManningPawn != null)
					{
						launcher = compMannable.ManningPawn;
						equipment = __instance.caster;
					}

					Vector3 drawPos = __instance.caster.DrawPos;
					Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, shootLine.Source, __instance.caster.Map, WipeMode.Vanish);
                    // 1
                    if (__instance.verbProps.forcedMissRadius > 0.5f)
					{
						float num = VerbUtility.CalculateAdjustedForcedMiss(__instance.verbProps.forcedMissRadius, currentTarget.Cell - __instance.caster.Position);
						if (num > 0.5f)
						{
							int max = GenRadial.NumCellsInRadius(num);
							int num2 = Rand.Range(0, max);
							if (num2 > 0)
							{
								IntVec3 c = currentTarget.Cell + GenRadial.RadialPattern[num2];
								ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
								//if (Rand.Chance(0.5f))
								if (Rand.Chance(yayoCombat.s_missBulletHit))
								{
									projectileHitFlags = ProjectileHitFlags.All;
								}
								if (!canHitNonTargetPawnsNow)
								{
									projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
								}
								projectile2.Launch(launcher, drawPos, c, currentTarget, projectileHitFlags, __instance.preventFriendlyFire, equipment, null);
								__result = true;
								return false;
							}
						}
					}
					ShotReport shotReport = ShotReport.HitReportFor(__instance.caster, __instance, currentTarget);
					Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
					ThingDef targetCoverDef = (randomCoverToMissInto == null) ? null : randomCoverToMissInto.def;

					// yayo
					
					bool isEquip = (__instance.EquipmentSource != null) && (__instance.EquipmentSource.def.equipmentType != EquipmentType.None);

					float missR = (1f - shotReport.AimOnTargetChance_IgnoringPosture * 0.5f);
                    if (missR < 0f) missR = 0f;

					float factorStat = 0.95f;
					float factorSkill = 0.3f;
					if (__instance.CasterIsPawn)
					{
						if (__instance.CasterPawn.skills != null)
						{
							factorSkill = (float)__instance.CasterPawn.skills.GetSkill(SkillDefOf.Shooting).levelInt / 20f;

						}
						else
						{
							factorSkill = (float)yayoCombat.baseSkill / 20f;
						}
						factorStat = 1f - __instance.caster.GetStatValue(StatDefOf.ShootingAccuracyPawn, true) * factorSkill;
					}
					else
					{
						// turret
						factorSkill = (float)yayoCombat.baseSkill / 20f;
						factorStat = 1f - (factorStat * factorSkill);
					}



                    float dist = (currentTarget.Cell - __instance.caster.Position).LengthHorizontal;

                    float factorEquip = 1f - __instance.verbProps.GetHitChanceFactor(__instance.EquipmentSource, dist);

					float factorGas = 1f;

					float factorWeather = 1f;
					if (!__instance.caster.Position.Roofed(__instance.caster.Map) || !currentTarget.Cell.Roofed(__instance.caster.Map))
					{
						factorWeather = __instance.caster.Map.weatherManager.CurWeatherAccuracyMultiplier;
					}
					else
					{
						factorWeather = 1f;
					}

					float factorAir = factorGas * factorWeather;

                    /*
                    Log.Message("-------------------");
                    Log.Message($"missR_a : {missR}");
                    Log.Message($"AimOnTargetChance_IgnoringPosture : {shotReport.AimOnTargetChance_IgnoringPosture}");
                    Log.Message($"skill : {factorSkill}");
                    Log.Message($"stat : {factorStat}");
                    Log.Message($"equip : {factorEquip}");
                    Log.Message($"air : {factorWeather}");
                    */

                    // yayo

                    if (isEquip)
					{
                        //missR *= ((0.25f * factorStat) + 0.55f * factorEquip * factorStat + 0.2f) * factorAir + (1f - factorAir);
                        //missR *= ((0.25f * factorStat) + (yayoCombat.s_equipAccEf + (yayoCombat.s_statAccEf * factorStat)) * factorEquip + 0.2f) * factorAir + (1f - factorAir);
                        //missR *= ((yayoCombat.s_accEff * factorStat + (yayoCombat.s_statAccEf * factorStat)) * factorEquip + (1f - yayoCombat.s_accEff)) * factorAir + (1f - factorAir);
                        missR *= (yayoCombat.s_accEf * factorStat + (1f - yayoCombat.s_accEf)) * factorAir + (1f - factorAir);
                    }

					// 근거리일 경우에 명중률 상승
					// Accuracy increases at close range
					if (dist < 10f)
                    {
                        missR -= Mathf.Clamp((10f - dist) * 0.07f, 0f, 0.3f);
                    }

					missR = missR * 0.95f + 0.05f;
					Mathf.Clamp(missR, 0.05f, 0.95f);

					//Log.Message($"missR_b : {missR}");


					// yayo

					// 2
					// 쓰레기 방향으로 나갈 확률 
					// Probability of going in the direction of garbage
					if (Rand.Chance(missR)) 
					{
						// 쓰레기 방향으로 발사
						// fire in the direction of garbage
						shootLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget);
						ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
						//if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
						if (Rand.Chance(yayoCombat.s_missBulletHit) && canHitNonTargetPawnsNow)
						{
							projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
						}
                        projectile2.Launch(launcher, drawPos, shootLine.Dest, currentTarget, projectileHitFlags2, __instance.preventFriendlyFire, equipment, targetCoverDef);
						__result = true;
						return false;
					}

                    // 3
					if (currentTarget.Thing != null && currentTarget.Thing.def.category == ThingCategory.Pawn && !Rand.Chance(shotReport.PassCoverChance)) 
					{
						//shootLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget);
						ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
						if (canHitNonTargetPawnsNow)
						{
							projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
						}
                        //projectile2.Launch(launcher, drawPos, shootLine.Dest, currentTarget, projectileHitFlags3, equipment, targetCoverDef);
                        projectile2.Launch(launcher, drawPos, randomCoverToMissInto, currentTarget, projectileHitFlags3, __instance.preventFriendlyFire, equipment, targetCoverDef);
						__result = true;
						return false;
					}
					ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
					if (canHitNonTargetPawnsNow)
					{
						projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
					}
					if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
					{
						projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
					}
					if (currentTarget.Thing != null)
					{
                        projectile2.Launch(launcher, drawPos, currentTarget, currentTarget, projectileHitFlags4, __instance.preventFriendlyFire, equipment, targetCoverDef);
					}
					else
					{
                        projectile2.Launch(launcher, drawPos, shootLine.Dest, currentTarget, projectileHitFlags4, __instance.preventFriendlyFire, equipment, targetCoverDef);
					}
					__result = true;
					return false;
                }
				else
				{
					//Log.Message("vanilla");
					// vanilla code
					return true;
				}
			}
		}
	}
    
}
