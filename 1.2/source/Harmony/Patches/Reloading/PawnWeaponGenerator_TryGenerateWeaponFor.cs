using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;
using System.Linq;
using System.Reflection;

namespace yayoCombat
{
    // 적 생성 시 탄약 보유 설정
    [HarmonyPatch(typeof(PawnWeaponGenerator), "TryGenerateWeaponFor")]
    internal class PawnWeaponGenerator_TryGenerateWeaponFor
    {
        /*
        private static FieldInfo f_allWeaponPairs = AccessTools.Field(typeof(PawnWeaponGenerator), "allWeaponPairs");
        private static AccessTools.FieldRef<List<ThingStuffPair>> s_allWeaponPairs = AccessTools.StaticFieldRefAccess<List<ThingStuffPair>>(f_allWeaponPairs);
        private static FieldInfo f_workingWeapons = AccessTools.Field(typeof(PawnWeaponGenerator), "workingWeapons");
        private static AccessTools.FieldRef<List<ThingStuffPair>> s_workingWeapons = AccessTools.StaticFieldRefAccess<List<ThingStuffPair>>(f_workingWeapons);
        */
        [HarmonyPriority(0)]
        static bool Prefix(Pawn pawn, PawnGenerationRequest request)
        {
            if (!yayoCombat.ammo) return true;


            List<ThingStuffPair> allWeaponPairs = PawnWeaponGenerator.allWeaponPairs;
            List<ThingStuffPair> workingWeapons = PawnWeaponGenerator.workingWeapons;

            workingWeapons.Clear();

            if (pawn.kindDef.weaponTags == null || pawn.kindDef.weaponTags.Count == 0 || (!pawn.RaceProps.ToolUser || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) || pawn.WorkTagIsDisabled(WorkTags.Violent))
                return false;

            float randomInRange = pawn.kindDef.weaponMoney.RandomInRange;
            for (int index = 0; index < allWeaponPairs.Count; ++index)
            {
                ThingStuffPair w = allWeaponPairs[index];
                if ((double)w.Price <= (double)randomInRange && (pawn.kindDef.weaponTags == null || pawn.kindDef.weaponTags.Any<string>((Predicate<string>)(tag => w.thing.weaponTags.Contains(tag)))) && ((double)w.thing.generateAllowChance >= 1.0 || Rand.ChanceSeeded(w.thing.generateAllowChance, pawn.thingIDNumber ^ (int)w.thing.shortHash ^ 28554824)))
                    workingWeapons.Add(w);
            }

            if (workingWeapons.Count == 0)
                return false;
            pawn.equipment.DestroyAllEquipment();
            ThingStuffPair result;

            if (workingWeapons.TryRandomElementByWeight<ThingStuffPair>((Func<ThingStuffPair, float>)(w => w.Commonality * w.Price), out result))
            {
                ThingWithComps thingWithComps = (ThingWithComps)ThingMaker.MakeThing(result.thing, result.stuff);

                // yayo
                if (pawn != null)
                {
                    foreach (ThingComp c in from comp in thingWithComps.AllComps
                                            where
                                                comp != null
                                                 && comp is CompReloadable
                                            select comp)
                    {
                        CompReloadable comp = c as CompReloadable;
                        if (comp != null)
                        {
                            if (pawn.Faction != null && pawn.Faction.IsPlayer)
                            {
                                // 정착민일 경우
                                Traverse.Create(comp).Field("remainingCharges").SetValue(Mathf.Min(comp.MaxCharges, Mathf.RoundToInt((float)comp.MaxCharges * yayoCombat.s_enemyAmmo * Rand.Range(0.7f, 1.3f))));
                            }
                            else
                            {
                                // 정착민이 아닐경우 총알 보유량
                                if(yayoCombat.s_enemyAmmo <= 1f)
                                {
                                    // 최대치 제한
                                    Traverse.Create(comp).Field("remainingCharges").SetValue(Mathf.Min(comp.MaxCharges, Mathf.RoundToInt((float)comp.MaxCharges * yayoCombat.s_enemyAmmo * Rand.Range(0.7f, 1.3f))));
                                }
                                else
                                {
                                    // 최대치 초과 가능
                                    Traverse.Create(comp).Field("remainingCharges").SetValue(Mathf.RoundToInt((float)comp.MaxCharges * yayoCombat.s_enemyAmmo * Rand.Range(0.7f, 1.3f)));
                                    /*
                                    Map m = pawn.Map;
                                    Thing thing = ThingMaker.MakeThing(comp.AmmoDef);
                                    thing.stackCount = Rand.Range(30, 30);
                                    */
                                    //pawn.inventory.innerContainer.AddItem(thing);
                                    //GenPlace.TryPlaceThing(thing, this.parent.InteractionCell, m, ThingPlaceMode.Near);
                                    

                                }
                                
                            }
                            
                        }

                    }
                }

                PawnGenerator.PostProcessGeneratedGear((Thing)thingWithComps, pawn);
                if ((double)Rand.Value < ((double)request.BiocodeWeaponChance > 0.0 ? (double)request.BiocodeWeaponChance : (double)pawn.kindDef.biocodeWeaponChance))
                    thingWithComps.TryGetComp<CompBiocodableWeapon>()?.CodeFor(pawn);
                pawn.equipment.AddEquipment(thingWithComps);

            }
            workingWeapons.Clear();

            return false;

        }
    }




}