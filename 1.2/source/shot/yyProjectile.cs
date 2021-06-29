using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace yayoCombat
{
	//[HarmonyPatch(typeof(Projectile))]
	//[HarmonyPatch("StartingTicksToImpact", PropertyMethod.Getter)]
	
	[HarmonyPatch(typeof(Projectile))]
	[HarmonyPatch("StartingTicksToImpact", MethodType.Getter)]
	static class yayoStartingTicksToImpact
    {
        public static bool Prefix(Projectile __instance, Vector3 ___origin, Vector3 ___destination, ref float __result)
        {
			if (__instance.def.projectile.flyOverhead || (__instance.def.projectile.speed <= 23f && __instance.def.projectile.explosionDelay <= 0)) return true;

			Vector3 origin = ___origin;

			Vector3 destination = ___destination;

			//float num = (this.origin - this.destination).magnitude / this.def.projectile.SpeedTilesPerTick;
			float speed = __instance.def.projectile.SpeedTilesPerTick * yayoCombat.bulletSpeed;
			if (speed >= yayoCombat.maxBulletSpeed * 0.01f)
			{
				speed = yayoCombat.maxBulletSpeed * 0.01f;
			}

			float num = (origin - destination).magnitude / speed;

			

            if (num <= 0f)
            {
				num = 0.001f;
			}
			

            __result = num;
            return false;
        }
    }
    

}
