using RimWorld;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace yayoCombat
{
    [HarmonyPatch(typeof(ReloadableUtility), "WearerOf")]
    internal class ReloadableUtility_WearerOf
    {
        [HarmonyPostfix]
        static bool Prefix(ref Pawn __result, CompReloadable comp)
        {
            if (!yayoCombat.ammo) return true;

            // comp.ParentHolder is Pawn_ApparelTracker parentHolder ? parentHolder.pawn : (Pawn) null;
            __result = comp.ParentHolder is Pawn_EquipmentTracker parentHolder ? parentHolder.pawn : comp.ParentHolder is Pawn_ApparelTracker parentHolder2 ? parentHolder2.pawn : (Pawn)null;
            return false;
        }
    }
}