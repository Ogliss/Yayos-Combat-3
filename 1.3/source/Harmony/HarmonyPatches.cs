using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;

namespace yayoCombat
{
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public class Patch_DefGenerator_GenerateImpliedDefs_PreResolve
    {
        public static void Prefix()
        {
            yayoCombat.patchDef1();
        }

    }
}
