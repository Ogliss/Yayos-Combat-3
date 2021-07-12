using HarmonyLib;
using RimWorld;

namespace yayoCombat
{
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public class DefGenerator_GenerateImpliedDefs_PreResolve
    {
        public static void Prefix()
        {
            yayoCombat.patchDef1();
        }

    }
}
