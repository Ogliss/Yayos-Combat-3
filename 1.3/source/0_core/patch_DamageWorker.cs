using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace yayoCombat
{
    /*  
    internal class patch_DamageWorker
    {
             
        [HarmonyPatch(typeof(DamageWorker), "Apply")]
        [HarmonyPatch(new Type[] { typeof(DamageInfo), typeof(Thing) })]
        public class patch
            {
            [HarmonyPrefix]
            public static bool Prefix(ref DamageWorker.DamageResult __result, DamageWorker __instance, DamageInfo dinfo, Thing victim)
            {
                

                DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
                if (victim.SpawnedOrAnyParentSpawned)
                {
                    ImpactSoundUtility.PlayImpactSound(victim, dinfo.Def.impactSoundType, victim.MapHeld);
                }
                if (victim.def.useHitPoints && dinfo.Def.harmsHealth)
                {
                    float num = dinfo.Amount;
                    if (victim.def.category == ThingCategory.Building)
                    {
                        num *= dinfo.Def.buildingDamageFactor;
                    }
                    if (victim.def.category == ThingCategory.Plant)
                    {
                        num *= dinfo.Def.plantDamageFactor;
                    }
                    damageResult.AddPart(victim, BodyPartRecord)
                    damageResult.totalDamageDealt = (float)Mathf.Min(victim.HitPoints, GenMath.RoundRandom(num));
                    victim.HitPoints -= Mathf.RoundToInt(damageResult.totalDamageDealt);
                    if (victim.HitPoints <= 0)
                    {
                        victim.HitPoints = 0;
                        victim.Kill(new DamageInfo?(dinfo), null);
                    }
                }
                __result = damageResult;
                return false;






            }
        }

        


    }
    */
}
