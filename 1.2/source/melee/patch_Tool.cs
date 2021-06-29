using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;
using System.Linq;


namespace yayoCombat
{

    [HarmonyPatch(typeof(Tool), "AdjustedCooldown")]
    internal class patch_Tool
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


    // 에러 로그 방지
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), "ChooseMeleeVerb")]
    internal class patch_Pawn_Pawn_MeleeVerbs_ChooseMeleeVerb
    {
        [HarmonyPostfix]
        static bool Prefix(Pawn_MeleeVerbs __instance, Thing target)
        {
            if (!yayoCombat.advAni) return true;

            bool terrainTools = Rand.Chance(0.04f);
            List<VerbEntry> availableVerbsList = __instance.GetUpdatedAvailableVerbsList(terrainTools);
            bool flag = false;
            VerbEntry result;
            if (availableVerbsList.TryRandomElementByWeight<VerbEntry>((Func<VerbEntry, float>)(ve => ve.GetSelectionWeight(target)), out result))
                flag = true;
            else if (terrainTools)
            {
                availableVerbsList = __instance.GetUpdatedAvailableVerbsList(false);
                flag = availableVerbsList.TryRandomElementByWeight<VerbEntry>((Func<VerbEntry, float>)(ve => ve.GetSelectionWeight(target)), out result);
            }
            if (flag)
            {
                AccessTools.Method(typeof(Pawn_MeleeVerbs), "SetCurMeleeVerb").Invoke(__instance, new object[] { result.verb, target });
            }
            else
            {
                AccessTools.Method(typeof(Pawn_MeleeVerbs), "SetCurMeleeVerb").Invoke(__instance, new object[] { (Verb)null, (Thing)null });
            }

            return false;
        }
    }



}