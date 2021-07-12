using System;
using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    [HarmonyPatch(typeof(DrugPolicy), "ExposeData")]
    internal class DrugPolicy_ExposeData
    {
    //    private static AccessTools.FieldRef<DrugPolicy, List<DrugPolicyEntry>> s_entriesInt = AccessTools.FieldRefAccess<DrugPolicy, List<DrugPolicyEntry>>("entriesInt");

        [HarmonyPriority(1000)]
        //[HarmonyPostfix]
        static bool Prefix(DrugPolicy __instance, ref List<DrugPolicyEntry> ___entriesInt)
        {
            if (!yayoCombat.ammo) return true;

            if (___entriesInt == null)
                ___entriesInt = new List<DrugPolicyEntry>();

            /*
            List<ThingDef> defsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
            
            // 탄약
            List<DrugPolicyEntry> ar_tmp0 = new List<DrugPolicyEntry>();
            foreach (ThingDef t in defsListForReading)
            {
                if (t.category == ThingCategory.Item && t.FirstThingCategory == ThingCategoryDef.Named("yy_ammo_category") && !___entriesInt.Exists(e => e.drug == t))
                {
                    ar_tmp0.Add(new DrugPolicyEntry()
                    {
                        drug = t,
                        allowedForAddiction = false
                    });
                }

            }
            ar_tmp0.RemoveAll(e => e?.drug?.GetCompProperties<CompProperties_Drug>() == null);
            ar_tmp0.SortBy<DrugPolicyEntry, float>((Func<DrugPolicyEntry, float>)(e => (float)e.drug.techLevel + (e.drug.defName.Contains("fire") ? 0.1f : e.drug.defName.Contains("emp") ? 0.2f : 0f)));


            // 약품
            List<DrugPolicyEntry> ar_tmp1 = new List<DrugPolicyEntry>();
            foreach (ThingDef t in defsListForReading)
            {
                if (t.category == ThingCategory.Item && t.FirstThingCategory == ThingCategoryDefOf.Medicine && !___entriesInt.Exists(e => e.drug == t))
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
                if (t.category == ThingCategory.Item && t.IsDrug && !___entriesInt.Exists(e => e.drug == t))
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
                if (t.category == ThingCategory.Item && t.FirstThingCategory == ThingCategoryDefOf.FoodMeals && !___entriesInt.Exists(e => e.drug == t))
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
            */


            Scribe_Values.Look<int>(ref __instance.uniqueId, "uniqueId", 0, false);
            Scribe_Values.Look<string>(ref __instance.label, "label", null, false);
            Scribe_Collections.Look<DrugPolicyEntry>(ref ___entriesInt, "drugs", LookMode.Deep, Array.Empty<object>());

            if (Scribe.mode == LoadSaveMode.PostLoadInit && ___entriesInt != null)
            {
                if (___entriesInt.RemoveAll((DrugPolicyEntry x) => x == null || x.drug == null) != 0)
                {
                    Log.Error("Some DrugPolicyEntries were null after loading.", false);
                }
            }

            return false;
        }
    }
    



}