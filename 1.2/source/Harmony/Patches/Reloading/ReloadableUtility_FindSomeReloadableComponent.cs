using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    [HarmonyPatch(typeof(ReloadableUtility), "FindSomeReloadableComponent")]
    internal class ReloadableUtility_FindSomeReloadableComponent
    {
        [HarmonyPostfix]
        static bool Prefix(ref CompReloadable __result, Pawn pawn, bool allowForcedReload)
        {
            if (!yayoCombat.ammo) return true;

            List<ThingWithComps> ar_thing = pawn.equipment.AllEquipmentListForReading;
            for (int i = 0; i < ar_thing.Count; i++)
            {
                CompReloadable compReloadable = ar_thing[i].TryGetComp<CompReloadable>();
                if (compReloadable != null && compReloadable.NeedsReload(allowForcedReload))
                {
                    __result = compReloadable;
                    return false;
                }
            }
            return true;
        }
    }




}