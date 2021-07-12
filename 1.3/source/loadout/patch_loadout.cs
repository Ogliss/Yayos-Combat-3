using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;

using System.Reflection;

namespace yayoCombat
{

    // 약물정책 UI
    [HarmonyPatch(typeof(Dialog_ManageDrugPolicies), "DoEntryRow")]
    internal class patch_Dialog_ManageDrugPolicies_DoEntryRow
    {
        
        [HarmonyPostfix]
        static bool Prefix(Dialog_ManageDrugPolicies __instance, Rect rect, DrugPolicyEntry entry)
        {
            if (!yayoCombat.ammo) return true;

            float addictionWidth;
            float allowJoyWidth;
            float scheduledWidth;
            float drugNameWidth;
            float frequencyWidth;
            float moodThresholdWidth;
            float joyThresholdWidth;
            float takeToInventoryWidth;
            CalculateColumnsWidths(rect, out addictionWidth, out allowJoyWidth, out scheduledWidth, out drugNameWidth, out frequencyWidth, out moodThresholdWidth, out joyThresholdWidth, out takeToInventoryWidth);
            Verse.Text.Anchor = TextAnchor.MiddleLeft;
            
            Rect r = new Rect(rect.x + 10f, rect.y, 30f, 30f);
            float rx = r.size.x + 20f;
            float x1 = rect.x + rx;
            drugNameWidth -= rx;

            Widgets.DrawTextureFitted(r, entry.drug.uiIcon, 1f);
            Widgets.Label(new Rect(x1, rect.y, drugNameWidth, rect.height).ContractedBy(4f), entry.drug.LabelCap);
            Widgets.InfoCardButton(x1 + drugNameWidth - 35f, rect.y + (float)(((double)rect.height - 24.0) / 2.0), (Def)entry.drug);
            float x2 = x1 + drugNameWidth - 10f;

            // yayo
            if (entry.drug.IsDrug)
            {
                Widgets.TextFieldNumeric<int>(new Rect(x2, rect.y, takeToInventoryWidth, rect.height).ContractedBy(4f), ref entry.takeToInventory, ref entry.takeToInventoryTempBuffer, max: 15f);
            }
            else
            {
                Widgets.TextFieldNumeric<int>(new Rect(x2, rect.y, takeToInventoryWidth + 118f, rect.height).ContractedBy(4f), ref entry.takeToInventory, ref entry.takeToInventoryTempBuffer, max: 5000f);
            }
            //


            float x3 = x2 + takeToInventoryWidth + 10f;
            if (entry.drug.IsAddictiveDrug)
                Widgets.Checkbox(x3, rect.y, ref entry.allowedForAddiction, paintable: true);
            float x4 = x3 + addictionWidth;
            if (entry.drug.IsPleasureDrug)
                Widgets.Checkbox(x4, rect.y, ref entry.allowedForJoy, paintable: true);
            float x5 = x4 + allowJoyWidth;
            if (entry.drug.IsDrug) // 약물일 경우에만 스케쥴 관리 표시
                Widgets.Checkbox(x5, rect.y, ref entry.allowScheduled, paintable: true);
            float x6 = x5 + scheduledWidth;
            float num;
            if (entry.allowScheduled)
            {
                entry.daysFrequency = Widgets.FrequencyHorizontalSlider(new Rect(x6, rect.y, frequencyWidth, rect.height).ContractedBy(4f), entry.daysFrequency, 0.1f, 25f, true);
                float x7 = x6 + frequencyWidth;
                string label1 = (double)entry.onlyIfMoodBelow >= 1.0 ? (string)"NoDrugUseRequirement".Translate() : entry.onlyIfMoodBelow.ToStringPercent();
                entry.onlyIfMoodBelow = Widgets.HorizontalSlider(new Rect(x7, rect.y, moodThresholdWidth, rect.height).ContractedBy(4f), entry.onlyIfMoodBelow, 0.01f, 1f, true, label1);
                float x8 = x7 + moodThresholdWidth;
                string label2 = (double)entry.onlyIfJoyBelow >= 1.0 ? (string)"NoDrugUseRequirement".Translate() : entry.onlyIfJoyBelow.ToStringPercent();
                entry.onlyIfJoyBelow = Widgets.HorizontalSlider(new Rect(x8, rect.y, joyThresholdWidth, rect.height).ContractedBy(4f), entry.onlyIfJoyBelow, 0.01f, 1f, true, label2);
                num = x8 + joyThresholdWidth;
            }
            else
                num = x6 + (frequencyWidth + moodThresholdWidth + joyThresholdWidth);
            Verse.Text.Anchor = TextAnchor.UpperLeft;
            return false;

        }

        static void CalculateColumnsWidths(
              Rect rect,
              out float addictionWidth,
              out float allowJoyWidth,
              out float scheduledWidth,
              out float drugNameWidth,
              out float frequencyWidth,
              out float moodThresholdWidth,
              out float joyThresholdWidth,
              out float takeToInventoryWidth)
        {
            float num = rect.width - 108f;
            drugNameWidth = num * 0.3f;
            addictionWidth = 36f;
            allowJoyWidth = 36f;
            scheduledWidth = 36f;
            frequencyWidth = num * 0.35f;
            moodThresholdWidth = num * 0.15f;
            joyThresholdWidth = num * 0.15f;
            takeToInventoryWidth = num * 0.05f;
        }
    }



    // 탄약을 인벤에 챙길 수 있도록 2
    [HarmonyPatch(typeof(JobGiver_MoveDrugsToInventory), "FindDrugFor")]
    internal class patch_JobGiver_MoveDrugsToInventory_FindDrugFor
    {

        [HarmonyPostfix]
        static bool Prefix(ref Thing __result, JobGiver_MoveDrugsToInventory __instance, Pawn pawn, ThingDef drugDef)
        {
            if (!yayoCombat.ammo) return true;
            if (drugDef.IsDrug)
            {
                __result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(drugDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn));
            }
            else
            {
                __result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(drugDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f
                    , x => !x.IsForbidden(pawn) && pawn.CanReserve((LocalTargetInfo)x));
            }
            
            return false;

        }
    }






    // 탄약을 인벤에 챙길 수 있도록
    // 인벤에 탄약이 있더라도 수량이 부족하면 채우도록
    [HarmonyPatch(typeof(Pawn_DrugPolicyTracker), "AllowedToTakeToInventory")]
    internal class patch_Pawn_DrugPolicyTracker_AllowedToTakeToInventory
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




    [HarmonyPatch(typeof(DrugPolicy), "ExposeData")]
    internal class Patch_DrugPolicy
    {
        private static AccessTools.FieldRef<DrugPolicy, List<DrugPolicyEntry>> s_entriesInt = AccessTools.FieldRefAccess<DrugPolicy, List<DrugPolicyEntry>>("entriesInt");

        [HarmonyPriority(1000)]
        //[HarmonyPostfix]
        static bool Prefix(DrugPolicy __instance)
        {
            if (!yayoCombat.ammo) return true;

            if (s_entriesInt.Invoke(__instance) == null)
                s_entriesInt.Invoke(__instance) = new List<DrugPolicyEntry>();

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
            Scribe_Collections.Look<DrugPolicyEntry>(ref s_entriesInt.Invoke(__instance), "drugs", LookMode.Deep, Array.Empty<object>());

            if (Scribe.mode == LoadSaveMode.PostLoadInit && s_entriesInt.Invoke(__instance) != null)
            {
                if (s_entriesInt.Invoke(__instance).RemoveAll((DrugPolicyEntry x) => x == null || x.drug == null) != 0)
                {
                    Log.Error("Some DrugPolicyEntries were null after loading.");
                }
            }

            return false;
        }
    }


    
    // 리스트에 탄약, 약품 추가

    [HarmonyPatch(typeof(DrugPolicy), "InitializeIfNeeded")]
    internal class patch_DrugPolicy_InitializeIfNeeded
    {
        //private static FieldInfo f_entriesInt = AccessTools.Field(typeof(DrugPolicy), "entriesInt");
        private static AccessTools.FieldRef<DrugPolicy, List<DrugPolicyEntry>> s_entriesInt = AccessTools.FieldRefAccess<DrugPolicy, List <DrugPolicyEntry>>("entriesInt");

        [HarmonyPriority(0)]
        static bool Prefix(DrugPolicy __instance)
        {
            if (!yayoCombat.ammo) return true;

            if (s_entriesInt.Invoke(__instance) != null)
                return false;
            s_entriesInt.Invoke(__instance) = new List<DrugPolicyEntry>();
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




            s_entriesInt.Invoke(__instance).AddRange(ar_tmp0);
            s_entriesInt.Invoke(__instance).AddRange(ar_tmp1);
            s_entriesInt.Invoke(__instance).AddRange(ar_tmp2);
            s_entriesInt.Invoke(__instance).AddRange(ar_tmp3);


            return false;
        }
    }
    



}