using System;
using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using Verse;


namespace yayoCombat
{
    // 에러 로그 방지
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), "ChooseMeleeVerb")]
    internal class Pawn_MeleeVerbs_ChooseMeleeVerb_Patch
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