using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;

using System.Reflection;

namespace yayoCombat
{
    // 약물정책 UI
    [HarmonyPatch(typeof(Dialog_ManageDrugPolicies), "DoEntryRow")]
    internal class Dialog_ManageDrugPolicies_DoEntryRow
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
}