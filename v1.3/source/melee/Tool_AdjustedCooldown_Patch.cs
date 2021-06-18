using System;
using UnityEngine;
using HarmonyLib;
using Verse;
using System.Linq;

namespace yayoCombat
{
    [HarmonyPatch(typeof(Tool), "AdjustedCooldown", new Type[] { typeof(Thing) })]
    internal class Tool_AdjustedCooldown_Patch
    {
        [HarmonyPriority(0)]
        static void Postfix(ref float __result, Thing ownerEquipment)
        {

            if (yayoCombat.advAni && ownerEquipment != null && __result > 0 && ownerEquipment.ParentHolder != null && ownerEquipment.ParentHolder.ParentHolder != null)
            {
                if (ownerEquipment.ParentHolder.ParentHolder is Pawn && ownerEquipment.def != null && ownerEquipment.def.IsMeleeWeapon)
                {
                    // 공격 쿨다운
                    float multiply = yayoCombat.meleeDelay * (1 + (Rand.Value - 0.5f) * yayoCombat.meleeRandom);
                    __result = Mathf.Max(__result * multiply, 0.2f);
                }
            }
        }
    }



}