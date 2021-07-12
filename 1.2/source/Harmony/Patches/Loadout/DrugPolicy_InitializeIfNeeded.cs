using System;
using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 리스트에 탄약, 약품 추가

    [HarmonyPatch(typeof(DrugPolicy), "InitializeIfNeeded")]
    internal class DrugPolicy_InitializeIfNeeded
    {
        //private static FieldInfo f_entriesInt = AccessTools.Field(typeof(DrugPolicy), "entriesInt");
     //   private static AccessTools.FieldRef<DrugPolicy, List<DrugPolicyEntry>> s_entriesInt = AccessTools.FieldRefAccess<DrugPolicy, List <DrugPolicyEntry>>("entriesInt");

        [HarmonyPriority(0)]
        static bool Prefix(DrugPolicy __instance, ref List<DrugPolicyEntry> ___entriesInt)
        {
            if (!yayoCombat.ammo) return true;

            if (___entriesInt != null)
                return false;
            ___entriesInt = new List<DrugPolicyEntry>();
            List<ThingDef> defsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;

            // 탄약
            List<DrugPolicyEntry> ar_tmp0 = new List<DrugPolicyEntry>();
            foreach (ThingDef t in defsListForReading)
            {
                if (t.category == ThingCategory.Item && (t.FirstThingCategory == ThingCategoryDef.Named("yy_ammo_category") || yayoCombat.ar_customAmmoDef.Contains(t)))
                {
                    ar_tmp0.Add(new DrugPolicyEntry()
                    {
                        drug = t,
                        allowedForAddiction = false
                    });
                }

            }
            ar_tmp0.SortBy<DrugPolicyEntry, float>((Func<DrugPolicyEntry, float>)(e => (float)e.drug.techLevel + (e.drug.defName.Contains("fire") ? 0.1f : e.drug.defName.Contains("emp") ? 0.2f : 0f)));


            // 약품
            List<DrugPolicyEntry> ar_tmp1 = new List<DrugPolicyEntry>();
            foreach (ThingDef t in defsListForReading)
            {
                if (t.category == ThingCategory.Item && t.FirstThingCategory == ThingCategoryDefOf.Medicine)
                {
                    ar_tmp1.Add(new DrugPolicyEntry()
                    {
                        drug = t,
                        allowedForAddiction = false
                    });
                }

            }
            ar_tmp1.SortByDescending<DrugPolicyEntry, float>((Func<DrugPolicyEntry, float>)(e => e.drug.BaseMarketValue));


            // 마약
            List<DrugPolicyEntry> ar_tmp2 = new List<DrugPolicyEntry>();
            foreach (ThingDef t in defsListForReading)
            {
                if (t.category == ThingCategory.Item && t.IsDrug)
                {
                    ar_tmp2.Add(new DrugPolicyEntry()
                    {
                        drug = t,
                        allowedForAddiction = true
                    });
                }

            }
            ar_tmp2.SortBy<DrugPolicyEntry, float>((Func<DrugPolicyEntry, float>)(e => e.drug.GetCompProperties<CompProperties_Drug>() != null ? e.drug.GetCompProperties<CompProperties_Drug>().listOrder : 0f));



            // 음식
            List<DrugPolicyEntry> ar_tmp3 = new List<DrugPolicyEntry>();
            foreach (ThingDef t in defsListForReading)
            {
                if (t.category == ThingCategory.Item && t.FirstThingCategory == ThingCategoryDefOf.FoodMeals)
                {
                    ar_tmp3.Add(new DrugPolicyEntry()
                    {
                        drug = t,
                        allowedForAddiction = false
                    });
                }

            }
            ar_tmp3.SortByDescending<DrugPolicyEntry, float>((Func<DrugPolicyEntry, float>)(e => e.drug.BaseMarketValue));

            ___entriesInt.AddRange(ar_tmp0);
            ___entriesInt.AddRange(ar_tmp1);
            ___entriesInt.AddRange(ar_tmp2);
            ___entriesInt.AddRange(ar_tmp3);

            return false;
        }
    }
    



}