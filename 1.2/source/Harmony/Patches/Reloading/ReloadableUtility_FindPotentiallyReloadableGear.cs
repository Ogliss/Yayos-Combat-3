using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    [HarmonyPatch(typeof(ReloadableUtility), "FindPotentiallyReloadableGear")]
    internal class ReloadableUtility_FindPotentiallyReloadableGear
    {
        [HarmonyPostfix]
        static bool Prefix(ref IEnumerable<Pair<CompReloadable, Thing>> __result, Pawn pawn, List<Thing> potentialAmmo)
        {
            if (!yayoCombat.ammo) return true;

            List<Pair<CompReloadable, Thing>> ar_tmp = new List<Pair<CompReloadable, Thing>>();
            if (pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; ++i)
                {
                    CompReloadable comp = worn[i].TryGetComp<CompReloadable>();
                    if (comp?.AmmoDef != null)
                    {
                        for (int j = 0; j < potentialAmmo.Count; ++j)
                        {
                            Thing second = potentialAmmo[j];
                            if (second.def == comp.Props.ammoDef)
                                ar_tmp.Add(new Pair<CompReloadable, Thing>(comp, second));
                        }
                        comp = (CompReloadable)null;
                    }
                }
            }

            // yayo
            // 무기
            if (pawn.equipment != null)
            {
                List<ThingWithComps> worn = pawn.equipment.AllEquipmentListForReading;
                for (int i = 0; i < worn.Count; ++i)
                {
                    CompReloadable comp = worn[i].TryGetComp<CompReloadable>();
                    if (comp?.AmmoDef != null)
                    {
                        for (int j = 0; j < potentialAmmo.Count; ++j)
                        {
                            Thing second = potentialAmmo[j];
                            if (second.def == comp.Props.ammoDef)
                                ar_tmp.Add(new Pair<CompReloadable, Thing>(comp, second));
                        }
                        comp = (CompReloadable)null;
                    }
                }
            }

            __result = ar_tmp;
            return false;
        }
    }




}