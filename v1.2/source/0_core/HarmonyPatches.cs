using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;

namespace yayoCombat
{

	
	public class HarmonyPatches : Mod
    {

		public HarmonyPatches(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.yayo.combat");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
		
		}

    }


    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public class Patch_DefGenerator_GenerateImpliedDefs_PreResolve
    {
        public static void Prefix()
        {
            yayoCombat.patchDef1();
        }

    }


}
