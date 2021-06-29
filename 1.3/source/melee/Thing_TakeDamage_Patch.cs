using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;
using System.Linq;


namespace yayoCombat
{

    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Thing_TakeDamage_Patch
    {
        [HarmonyPrefix]
        static void Prefix(ref DamageInfo dinfo)
        {
            if (yayoCombat.advAni && dinfo.Amount > 0f && dinfo.Weapon != null && dinfo.Weapon.IsMeleeWeapon)
            {
                // 공격 데미지
                // attack damage
                float amount = Mathf.Max(1f, Mathf.RoundToInt(dinfo.Amount * yayoCombat.meleeDelay));
                dinfo.SetAmount(amount);
            }
        }
    }



}