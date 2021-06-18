using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;
using System.Linq;
using Verse.AI;
using System.Reflection;
using System.Text;

namespace yayoCombat
{
    // 탄약에 따른 무기가격 조정 : 기존엔 기존 무기 가격에서 탄약가격이 뺄셈이 되었음. 그걸 기존 가격에 탄약가격을 덧셈하도록 변경
    // Adjustment of weapon price according to ammunition: 
    // In the past, the price of ammunition was subtracted from the price of existing weapons. Changed it to add ammo price to the original price
    [HarmonyPatch(typeof(StatPart_ReloadMarketValue), "TransformAndExplain")]
    public class Patch_StatPart_ReloadMarketValue_TransformAndExplain
    {
        public static bool Prefix(StatRequest req, ref float val, StringBuilder explanation)
        {
            if (req != null && req.Thing != null && req.Thing.def != null)

                if (req.Thing.def.IsRangedWeapon)
                {
                    CompReloadable compReloadable = req.Thing.TryGetComp<CompReloadable>();
                    if (compReloadable != null)
                    {
                        if (compReloadable.AmmoDef != null && compReloadable.RemainingCharges != 0)
                        {
                            int num = compReloadable.RemainingCharges;
                            float chargesPrice = compReloadable.AmmoDef.BaseMarketValue * (float)num;
                            val += chargesPrice;
                            explanation?.AppendLine("StatsReport_ReloadMarketValue".Translate(NamedArgumentUtility.Named(compReloadable.AmmoDef, "AMMO"), num.Named("COUNT")) + ": " + chargesPrice.ToStringMoneyOffset());
                        }

                        return false;
                    }
                }

            return true;
        }
    }

    // 탄약 카테고리 보이기
    // Show ammo categories
    [HarmonyPatch(typeof(ThingFilter), "SetFromPreset")]
    internal class patch_ThingFilter_SetFromPreset
    {
        [HarmonyPostfix]
        static bool Prefix(ThingFilter __instance, StorageSettingsPreset preset)
        {
            if (!yayoCombat.ammo) return true;

            if (preset == StorageSettingsPreset.DefaultStockpile)
            {
                __instance.SetAllow(ThingCategoryDef.Named("yy_ammo_category"), true);
            }
            return true;
        }
    }

    // 소집상태에서 탄약0일 장전 시도하기
    // Attempt to reload ammo for day 0 while in muster
    [HarmonyPatch(typeof(Pawn), "Tick")]
    internal class patch_Pawn_TickRare
    {
        [HarmonyPriority(0)]
        static void Postfix(Pawn __instance)
        {
            if (!yayoCombat.ammo) return;
            if (!__instance.Drafted) return;
            if (Find.TickManager.TicksGame % 60 != 0) return;
            if (!(__instance.CurJobDef == JobDefOf.Wait_Combat || __instance.CurJobDef == JobDefOf.AttackStatic) || __instance.equipment == null) return;

            List<ThingWithComps> ar = __instance.equipment.AllEquipmentListForReading;

            foreach (ThingWithComps t in ar)
            {
                CompReloadable cp = t.TryGetComp<CompReloadable>();
                
                if (cp != null)
                {
                    reloadUtility.tryAutoReload(cp);
                    return;
                }
            }

        }
    }

    // 적 사망 시 무기에서 탄약 아이템 분리
    // Detach ammo items from weapons when an enemy dies
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "DropAllEquipment")]
    internal class patch_Pawn_EquipmentTracker_DropAllEquipment
    {
        [HarmonyPostfix]
        static bool Prefix(Pawn_EquipmentTracker __instance, ThingOwner<ThingWithComps> ___equipment, IntVec3 pos, bool forbid = true)
        {
            if (!yayoCombat.ammo) return true;


            ThingOwner<ThingWithComps> equipment = ___equipment;
            for (int index = equipment.Count - 1; index >= 0; --index)
            {
                bool isEnemy = __instance.pawn.Faction != null && !__instance.pawn.Faction.IsPlayer;
                ThingWithComps t = equipment[index];
                if (__instance.TryDropEquipment(t, out ThingWithComps _, pos, forbid))
                {
                    if (isEnemy)
                    {
                        reloadUtility.TryThingEjectAmmoDirect((Thing)t, true);
                    }
                }
                
            }
                

            return false;
        }
    }

    // 게임 로드 시 comp_reloadable.verbTracker로 인한 중복 에러 방지
    // Avoid duplicate errors caused by comp_reloadable.verbTracker when loading games
    [HarmonyPatch(typeof(CompReloadable), "PostExposeData")]
    internal class patch_CompReloadable_PostExposeData
    {
        [HarmonyPostfix]
        static bool Prefix(CompReloadable __instance, ref int ___remainingCharges)
        {
            if (!yayoCombat.ammo) return true;
            if (!__instance.parent.def.IsWeapon) return true;

            //__instance.PostExposeData();

            int remainingCharges = Traverse.Create(__instance).Field("remainingCharges").GetValue<int>();
            Scribe_Values.Look<int>(ref remainingCharges, "remainingCharges", -999);
            Traverse.Create(__instance).Field("remainingCharges").SetValue(remainingCharges);
            /*
            VerbTracker verbTracker = null;
            Scribe_Deep.Look<VerbTracker>(ref verbTracker, "verbTracker", (object)__instance);
            Traverse.Create(__instance).Field("verbTracker").SetValue(verbTracker);
            */
            if (Scribe.mode != LoadSaveMode.PostLoadInit || remainingCharges != -999)
            {
                return false;
            }

            Traverse.Create(__instance).Field("remainingCharges").SetValue(__instance.MaxCharges);

            return false;

        }
    }


    // 우클릭 탄약꺼내기 메뉴
    // Right-click eject ammunition menu

    [HarmonyPatch(typeof(ThingWithComps), "GetFloatMenuOptions")]
    internal class patch_ThingWithComps_GetFloatMenuOptions
    {
        [HarmonyPriority(0)]
        static void Postfix(ref IEnumerable<FloatMenuOption> __result, ThingWithComps __instance, Pawn selPawn)
        {
            if (!yayoCombat.ammo) return;

            CompReloadable cp = __instance.TryGetComp<CompReloadable>();

            if (selPawn.IsColonist && cp != null && cp.AmmoDef != null && !cp.Props.destroyOnEmpty && cp.RemainingCharges > 0)
            {
                __result = new List<FloatMenuOption>() { new FloatMenuOption("eject_Ammo".Translate(), new Action(cleanWeapon), MenuOptionPriority.High) };
            }

            void cleanWeapon() => reloadUtility.EjectAmmo(selPawn, __instance);
            
        }
    }



    // 터렛 무기 재장전 콤프 제거
    [HarmonyPatch(typeof(Building_TurretGun), "MakeGun")]
    internal class patch_Building_TurretGun_MakeGun
    {
        [HarmonyPostfix]
        static bool Prefix(Building_TurretGun __instance)
        {
            if (!yayoCombat.ammo) return true;

            ThingDef gunDef = __instance.def.building.turretGunDef;
            bool flag = false;
            for(int i = 0; i < gunDef.comps.Count; i++)
            {
                if (gunDef.comps[i].compClass == typeof(CompReloadable))
                {
                    gunDef.comps.Remove(gunDef.comps[i]);
                    flag = true;
                }
            }

            if (flag)
            {
                __instance.def.building.turretGunDef = gunDef;
            }
            



            return true;


        }
    }



    // 상인 소지품에 탄약 생성
    [HarmonyPatch(typeof(ThingSetMaker_TraderStock), "Generate")]
    internal class patch_ThingSetMaker_TraderStock_Generate
    {
        [HarmonyPostfix]
        static bool Prefix(ThingSetMaker_TraderStock __instance, ThingSetMakerParams parms, List<Thing> outThings)
        {
            if (!yayoCombat.ammo) return true;

            bool hasStockGenerator_WeaponsRanged = false;

            TraderKindDef trader = parms.traderDef ?? DefDatabase<TraderKindDef>.AllDefsListForReading.RandomElement<TraderKindDef>();

            if (trader != null && trader.defName == "Empire_Caravan_TributeCollector") return true; // 제국 수집 상인

            Faction makingFaction = parms.makingFaction;
            int forTile = !parms.tile.HasValue ? (Find.AnyPlayerHomeMap == null ? (Find.CurrentMap == null ? -1 : Find.CurrentMap.Tile) : Find.AnyPlayerHomeMap.Tile) : parms.tile.Value;
            for (int index = 0; index < trader.stockGenerators.Count; ++index)
            {
                if(trader.stockGenerators[index] is StockGenerator_WeaponsRanged)
                {
                    hasStockGenerator_WeaponsRanged = true;
                }
                foreach (Thing thing in trader.stockGenerators[index].GenerateThings(forTile, parms.makingFaction))
                {
                    if (!thing.def.tradeability.TraderCanSell())
                    {
                        Log.Error(trader.ToString() + " generated carrying " + (object)thing + " which can't be sold by traders. Ignoring...");
                    }
                    else
                    {
                        thing.PostGeneratedForTrader(trader, forTile, makingFaction);
                        outThings.Add(thing);
                    }
                }
            }

            if (hasStockGenerator_WeaponsRanged || Rand.Value <= 0.2f)
            {
                TechLevel tech = TechLevel.Spacer;
                if (makingFaction != null && makingFaction.def != null)
                {
                    tech = makingFaction.def.techLevel;
                }
                Thing t = new Thing();

                float amount = 300f;
                float min = 0.4f;
                float max = 1.6f;


                if (tech >= TechLevel.Neolithic)
                {
                    // 원시 이상
                    if ((tech >= TechLevel.Neolithic && tech <= TechLevel.Medieval) || Rand.Value <= 0.3f)
                    {
                        t = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_primitive"));
                        t.stackCount = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * amount);
                        t.PostGeneratedForTrader(trader, forTile, makingFaction);
                        outThings.Add(t);
                    }


                    // 산업 이상
                    if (tech >= TechLevel.Industrial || Rand.Value <= 0.2f)
                    {
                        t = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_industrial"));
                        t.stackCount = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * amount);
                        t.PostGeneratedForTrader(trader, forTile, makingFaction);
                        outThings.Add(t);
                    }
                    if (tech >= TechLevel.Industrial || Rand.Value <= 0.2f)
                    {
                        t = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_industrial_fire"));
                        t.stackCount = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * amount * 0.5f);
                        t.PostGeneratedForTrader(trader, forTile, makingFaction);
                        outThings.Add(t);
                    }
                    if (tech >= TechLevel.Industrial || Rand.Value <= 0.2f)
                    {
                        t = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_industrial_emp"));
                        t.stackCount = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * amount * 0.25f);
                        t.PostGeneratedForTrader(trader, forTile, makingFaction);
                        outThings.Add(t);
                    }


                    // 우주 이상
                    if (tech >= TechLevel.Spacer || Rand.Value <= 0.2f)
                    {
                        t = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_spacer"));
                        t.stackCount = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * amount);
                        t.PostGeneratedForTrader(trader, forTile, makingFaction);
                        outThings.Add(t);
                    }
                    if (tech >= TechLevel.Spacer || Rand.Value <= 0.2f)
                    {
                        t = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_spacer_fire"));
                        t.stackCount = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * amount * 0.5f);
                        t.PostGeneratedForTrader(trader, forTile, makingFaction);
                        outThings.Add(t);
                    }
                    if (tech >= TechLevel.Spacer || Rand.Value <= 0.2f)
                    {
                        t = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_spacer_emp"));
                        t.stackCount = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * amount * 0.25f);
                        t.PostGeneratedForTrader(trader, forTile, makingFaction);
                        outThings.Add(t);
                    }

                }
                

                

                
            }

            return false;


        }
    }

    

    // 상인이 탄약 거래 가능하도록 허용
    [HarmonyPatch(typeof(TraderKindDef), "WillTrade")]
    internal class patch_TraderKindDef_WillTrade
    {
        [HarmonyPostfix]
        static bool Prefix(ref bool __result, TraderKindDef __instance, ThingDef td)
        {
            if (!yayoCombat.ammo) return true;
            if (__instance.defName == "Empire_Caravan_TributeCollector") return true; // 제국 수집 상인

            if (td.tradeTags != null && td.tradeTags.Contains("Ammo"))
            {
                __result = true;
                return false;
            }
            else
            {
                return true;
            }


        }
    }

    

    // 추가적인 탄약표시 기즈모 설정

    [HarmonyPatch(typeof(CompReloadable), "CreateVerbTargetCommand")]
    internal class patch_CompReloadable_CreateVerbTargetCommand
    {
        [HarmonyPostfix]
        static bool Prefix(ref Command_Reloadable __result, CompReloadable __instance, Thing gear, Verb verb)
        {
            if (!yayoCombat.ammo) return true;

            if (gear.def.IsWeapon)
            {
                Command_Reloadable commandReloadable = new Command_Reloadable(__instance);
                commandReloadable.defaultDesc = gear.def.description;
                //commandReloadable.hotKey = __instance.Props.hotKey;
                commandReloadable.defaultLabel = verb.verbProps.label;
                commandReloadable.verb = verb;
                if (verb.verbProps.defaultProjectile != null && verb.verbProps.commandIcon == null)
                {
                    commandReloadable.icon = verb.verbProps.defaultProjectile.uiIcon;
                    commandReloadable.iconAngle = verb.verbProps.defaultProjectile.uiIconAngle;
                    commandReloadable.iconOffset = verb.verbProps.defaultProjectile.uiIconOffset;
                    commandReloadable.overrideColor = new Color?(verb.verbProps.defaultProjectile.graphicData.color);
                }
                else
                {
                    commandReloadable.icon = (UnityEngine.Object)verb.UIIcon != (UnityEngine.Object)BaseContent.BadTex ? verb.UIIcon : gear.def.uiIcon;
                    commandReloadable.iconAngle = gear.def.uiIconAngle;
                    commandReloadable.iconOffset = gear.def.uiIconOffset;
                    commandReloadable.defaultIconColor = gear.DrawColor;
                }
                if (!__instance.Wearer.IsColonistPlayerControlled || !__instance.Wearer.Drafted)
                    commandReloadable.Disable();
                else if (verb.verbProps.violent && __instance.Wearer.WorkTagIsDisabled(WorkTags.Violent))
                    commandReloadable.Disable((string)("IsIncapableOfViolenceLower".Translate((NamedArgument)__instance.Wearer.LabelShort, (NamedArgument)(Thing)__instance.Wearer).CapitalizeFirst() + "."));
                else if (!__instance.CanBeUsed)
                    commandReloadable.Disable(__instance.DisabledReason(__instance.MinAmmoNeeded(false), __instance.MaxAmmoNeeded(false)));

                __result = commandReloadable;
                return false;
            }
            else
            {
                Command_Reloadable commandReloadable = new Command_Reloadable(__instance);
                commandReloadable.defaultDesc = gear.def.description;
                commandReloadable.hotKey = __instance.Props.hotKey;
                commandReloadable.defaultLabel = verb.verbProps.label;
                commandReloadable.verb = verb;
                if (verb.verbProps.defaultProjectile != null && verb.verbProps.commandIcon == null)
                {
                    commandReloadable.icon = verb.verbProps.defaultProjectile.uiIcon;
                    commandReloadable.iconAngle = verb.verbProps.defaultProjectile.uiIconAngle;
                    commandReloadable.iconOffset = verb.verbProps.defaultProjectile.uiIconOffset;
                    commandReloadable.overrideColor = new Color?(verb.verbProps.defaultProjectile.graphicData.color);
                }
                else
                {
                    commandReloadable.icon = (UnityEngine.Object)verb.UIIcon != (UnityEngine.Object)BaseContent.BadTex ? verb.UIIcon : gear.def.uiIcon;
                    commandReloadable.iconAngle = gear.def.uiIconAngle;
                    commandReloadable.iconOffset = gear.def.uiIconOffset;
                    commandReloadable.defaultIconColor = gear.DrawColor;
                }
                if (!__instance.Wearer.IsColonistPlayerControlled)
                    commandReloadable.Disable();
                else if (verb.verbProps.violent && __instance.Wearer.WorkTagIsDisabled(WorkTags.Violent))
                    commandReloadable.Disable((string)("IsIncapableOfViolenceLower".Translate((NamedArgument)__instance.Wearer.LabelShort, (NamedArgument)(Thing)__instance.Wearer).CapitalizeFirst() + "."));
                else if (!__instance.CanBeUsed)
                    commandReloadable.Disable(__instance.DisabledReason(__instance.MinAmmoNeeded(false), __instance.MaxAmmoNeeded(false)));

                __result = commandReloadable;
                return false;
            }

            


        }
    }

    // 추가적인 탄약표시 기즈모 생성
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "GetGizmos")]
    internal class patch_ThingWithComps_GetGizmos
    {
        [HarmonyPostfix]
        static bool Prefix(ref IEnumerable<Gizmo> __result, Pawn_EquipmentTracker __instance)
        {
            if (!yayoCombat.ammo) return true;

            List<Gizmo> ar_tmp = new List<Gizmo>();

            if (PawnAttackGizmoUtility.CanShowEquipmentGizmos())
            {
                // 기존 verb 기본기즈모 표시
                List<ThingWithComps> list = __instance.AllEquipmentListForReading;
                for (int i = 0; i < list.Count; ++i)
                {
                    foreach (Command verbsCommand in list[i].GetComp<CompEquippable>().GetVerbsCommands())
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

                list = (List<ThingWithComps>)null;
            }



            __result = ar_tmp;
            return false;
        }
    }




    // 남은 탄약이 0 일경우 사냥 중지, 탄약 줍기 ai
    [HarmonyPatch(typeof(CompReloadable), "UsedOnce")]
    internal class patch_CompReloadable_UsedOnce
    {
        [HarmonyPostfix]
        static bool Prefix(CompReloadable __instance)
        {
            if (!yayoCombat.ammo) return true;

            int remainingCharges0 = __instance.RemainingCharges;
            if (remainingCharges0 > 0)
            {
                Traverse.Create(__instance).Field("remainingCharges").SetValue(remainingCharges0 - 1);
            }
            //if (__instance.VerbTracker.PrimaryVerb.caster == null) return false;

            
            if (!__instance.Props.destroyOnEmpty || __instance.RemainingCharges != 0 || __instance.parent.Destroyed)
            {
                
            }
            else
            {
                __instance.parent.Destroy(DestroyMode.Vanish);
            }

            //

            if (__instance.Wearer == null) return false;

            // 남은 탄약이 0 일경우 게임튕김 방지를 위해 사냥 중지
            if (__instance.RemainingCharges == 0)
            {
                if (__instance.Wearer.CurJobDef == JobDefOf.Hunt)
                {
                    __instance.Wearer.jobs.StopAll();
                }
            }


            // 알아서 장전 ai
            reloadUtility.tryAutoReload(__instance);

            return false;
        }
    }

    // 사냥을 위한 무기 보유 여부를 탄약과 함께 계산
    [HarmonyPatch(typeof(WorkGiver_HunterHunt), "HasHuntingWeapon")]
    internal class patch_WorkGiver_HasHuntingWeapon
    {
        [HarmonyPostfix]
        static bool Prefix(ref bool __result, Pawn p)
        {
            if (!yayoCombat.ammo) return true;

            if (p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon && (p.equipment.PrimaryEq.PrimaryVerb.HarmsHealth() && !p.equipment.PrimaryEq.PrimaryVerb.UsesExplosiveProjectiles()))
            {
                if (p.equipment.Primary.GetComp<CompReloadable>() != null)
                {
                    __result = p.equipment.Primary.GetComp<CompReloadable>().CanBeUsed;

                }
                else
                {
                    __result = true;
                }
            }
            else
            {
                __result = false;
            }

            return false;
        }
    }



    // 적 생성 시 탄약 보유 설정
    [HarmonyPatch(typeof(PawnWeaponGenerator), "TryGenerateWeaponFor")]
    internal class patch_PawnWeaponGenerator_TryGenerateWeaponFor
    {
        private static FieldInfo f_allWeaponPairs = AccessTools.Field(typeof(PawnWeaponGenerator), "allWeaponPairs");
        private static AccessTools.FieldRef<List<ThingStuffPair>> s_allWeaponPairs = AccessTools.StaticFieldRefAccess<List<ThingStuffPair>>(f_allWeaponPairs);
        private static FieldInfo f_workingWeapons = AccessTools.Field(typeof(PawnWeaponGenerator), "workingWeapons");
        private static AccessTools.FieldRef<List<ThingStuffPair>> s_workingWeapons = AccessTools.StaticFieldRefAccess<List<ThingStuffPair>>(f_workingWeapons);

        [HarmonyPriority(0)]
        static bool Prefix(Pawn pawn, PawnGenerationRequest request)
        {
            if (!yayoCombat.ammo) return true;


            List<ThingStuffPair> allWeaponPairs = s_allWeaponPairs.Invoke();
            List<ThingStuffPair> workingWeapons = s_workingWeapons.Invoke();

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
                    thingWithComps.TryGetComp<CompBiocodable>()?.CodeFor(pawn);
                pawn.equipment.AddEquipment(thingWithComps);

            }
            workingWeapons.Clear();

            return false;

        }
    }


    



    // 아이템 생성 시 기본 탄약 수
    [HarmonyPatch(typeof(CompReloadable), "PostPostMake")]
    internal class patch_CompReloadable_PostPostMake
    {
        [HarmonyPriority(0)]
        static void Postfix(CompReloadable __instance)
        {
            if (yayoCombat.ammo && __instance.parent.def.IsWeapon)
            {


                if (GenTicks.TicksGame <= 5)
                {
                    foreach (ThingComp c in from comp in __instance.parent.AllComps
                                            where
                                                comp != null
                                                 && comp is CompReloadable
                                            select comp)
                    {
                        CompReloadable comp = c as CompReloadable;
                        if (comp != null)
                        {
                            // 시작아이템 총알 보유량
                            Traverse.Create(comp).Field("remainingCharges").SetValue(Mathf.RoundToInt((float)comp.MaxCharges * yayoCombat.s_enemyAmmo));
                        }

                    }
                }
                else
                {
                    // 생산된 아이템 총알 보유량
                    Traverse.Create(__instance).Field("remainingCharges").SetValue(0);
                }



            }
        }
    }


    [HarmonyPatch(typeof(ReloadableUtility), "FindPotentiallyReloadableGear")]
    internal class patch_ReloadableUtility_FindPotentiallyReloadableGear
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


    
    
    


    [HarmonyPatch(typeof(ReloadableUtility), "FindSomeReloadableComponent")]
    internal class patch_ReloadableUtility_FindSomeReloadableComponent
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

    [HarmonyPatch(typeof(ReloadableUtility), "WearerOf")]
    internal class patch_ReloadableUtility_WearerOf
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