using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 상인 소지품에 탄약 생성
    [HarmonyPatch(typeof(ThingSetMaker_TraderStock), "Generate")]
    internal class ThingSetMaker_TraderStock_Generate
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




}