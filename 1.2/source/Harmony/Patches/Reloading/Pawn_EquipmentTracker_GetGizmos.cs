using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 추가적인 탄약표시 기즈모 생성
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "GetGizmos")]
    internal class Pawn_EquipmentTracker_GetGizmos
    {
        [HarmonyPostfix]
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_EquipmentTracker __instance)
        {
            if (!yayoCombat.ammo) return __result;
            List<Gizmo> ar_tmp = new List<Gizmo>();
            if (PawnAttackGizmoUtility.CanShowEquipmentGizmos())
            {
                // 기존 verb 기본기즈모 표시
                List<ThingWithComps> list = __instance.AllEquipmentListForReading;
                for (int i = 0; i < list.Count; ++i)
                {
                    CompEquippable equippable = list[i].GetComp<CompEquippable>();
                    if (equippable == null)
                    {
                        continue;
                    }
                    foreach (Command verbsCommand in equippable.GetVerbsCommands())
                    {
                        switch (i)
                        {
                            case 0:
                                verbsCommand.hotKey = KeyBindingDefOf.Misc1;
                                break;
                            case 1:
                                verbsCommand.hotKey = KeyBindingDefOf.Misc2;
                                break;
                            case 2:
                                verbsCommand.hotKey = KeyBindingDefOf.Misc3;
                                break;
                        }
                        ar_tmp.Add((Gizmo)verbsCommand);
                    }
                }
                // 추가 탄약 기즈모
                List<ThingComp> comps = new List<ThingComp>();
                for (int i = 0; i < list.Count; i++)
                {
                    comps = list[i].AllComps;
                    for (int j = 0; j < comps.Count; ++j)
                    {
                        foreach (Gizmo gizmo in comps[j].CompGetWornGizmosExtra())
                        {
                            ar_tmp.Add(gizmo);
                        }
                    }
                }
            }
            return ar_tmp;
        }
    }
}