using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 탄약을 인벤에 챙길 수 있도록
    // 인벤에 탄약이 있더라도 수량이 부족하면 채우도록
    [HarmonyPatch(typeof(Pawn_DrugPolicyTracker), "AllowedToTakeToInventory")]
    internal class Pawn_DrugPolicyTracker_AllowedToTakeToInventory
    {
        
        [HarmonyPostfix]
        static bool Prefix(ref bool __result, Pawn_DrugPolicyTracker __instance, ThingDef thingDef)
        {
            if (!yayoCombat.ammo) return true;

            if (thingDef.FirstThingCategory == ThingCategoryDef.Named("yy_ammo_category")
                || yayoCombat.ar_customAmmoDef.Contains(thingDef)
                || thingDef.FirstThingCategory == ThingCategoryDefOf.Medicine
                || thingDef.FirstThingCategory == ThingCategoryDefOf.FoodMeals
                )
            {
                DrugPolicyEntry drugPolicyEntry = __instance.CurrentPolicy[thingDef];
                //__result = !drugPolicyEntry.allowScheduled && drugPolicyEntry.takeToInventory > 0 && !__instance.pawn.inventory.innerContainer.Contains(thingDef);
                __result = !drugPolicyEntry.allowScheduled && drugPolicyEntry.takeToInventory > 0 && drugPolicyEntry.takeToInventory > __instance.pawn.inventory.innerContainer.TotalStackCountOfDef(thingDef);
                return false;
            }
            else
            {
                if (!thingDef.IsIngestible)
                {
                    Log.Error(thingDef.ToString() + " is not ingestible.");
                    __result = false;
                    return false;
                }
                if (!thingDef.IsDrug)
                {
                    Log.Error("AllowedToTakeScheduledEver on non-drug " + (object)thingDef);
                    __result = false;
                    return false;
                }
                if (thingDef.IsNonMedicalDrug && __instance.pawn.IsTeetotaler())
                {
                    __result = false;
                    return false;
                }
                DrugPolicyEntry drugPolicyEntry = __instance.CurrentPolicy[thingDef];
                __result = !drugPolicyEntry.allowScheduled && drugPolicyEntry.takeToInventory > 0 && !__instance.pawn.inventory.innerContainer.Contains(thingDef);
                return false;

            }
            
            
        }
    }
}