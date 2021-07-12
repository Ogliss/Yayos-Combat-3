using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace yayoCombat
{
    public static class ArmorUtility
    {
        public static float GetPostArmorDamage(Pawn pawn, float amount, float armorPenetration, BodyPartRecord part, ref DamageDef damageDef, ref bool deflectedByMetalArmor, ref bool diminishedByMetalArmor, DamageInfo dinfo)
        {
            deflectedByMetalArmor = false;
            diminishedByMetalArmor = false;
            bool forcedDefl = false;
            if (damageDef.armorCategory == null)
            {
                return amount;
            }
            StatDef armorRatingStat = damageDef.armorCategory.armorRatingStat;
            if (pawn.apparel != null)
            {
                List<Apparel> wornApparel = pawn.apparel.WornApparel;
                for (int i = wornApparel.Count - 1; i >= 0; i--)
                {
                    Apparel apparel = wornApparel[i];
                    if (apparel.def.apparel.CoversBodyPart(part))
                    {
                        float num = amount;
                        bool flag;
                        ApplyArmor(ref amount, armorPenetration, apparel.GetStatValue(armorRatingStat, true), apparel, ref damageDef, pawn, out flag, dinfo, out forcedDefl);
                        if (amount < 0.001f)
                        {
                            deflectedByMetalArmor = flag;
                            if (forcedDefl) deflectedByMetalArmor = true;
                            return 0f;
                        }
                        if (amount < num && flag)
                        {
                            diminishedByMetalArmor = true;
                        }
                    }
                }
            }
            float num2 = amount;
            bool flag2;
            ApplyArmor(ref amount, armorPenetration, pawn.GetStatValue(armorRatingStat, true), null, ref damageDef, pawn, out flag2, dinfo, out forcedDefl);
            if (amount < 0.001f)
            {
                deflectedByMetalArmor = flag2;
                if (forcedDefl) deflectedByMetalArmor = true;
                return 0f;
            }
            if (amount < num2 && flag2)
            {
                diminishedByMetalArmor = true;
            }
            if (forcedDefl) deflectedByMetalArmor = true;
            return amount;
        }

        public static void ApplyArmor(ref float damAmount, float armorPenetration, float armorRating, Thing armorThing, ref DamageDef damageDef, Pawn pawn, out bool metalArmor, DamageInfo dinfo, out bool forcedDefl)
        {
            bool hasAmor = false;
            bool isMechanoid = pawn.RaceProps.IsMechanoid;
            forcedDefl = false;

            if (armorThing != null)
            {
                hasAmor = true;
                metalArmor = (armorThing.def.apparel.useDeflectMetalEffect || (armorThing.Stuff != null && armorThing.Stuff.IsMetal));
            }
            else
            {
                metalArmor = isMechanoid;
            }

            float real_armorPenetration = armorPenetration;

            // 무기와 방어구 기술 수준에 따른 방어관통력 변화
            if(hasAmor && dinfo.Weapon != null)
            {
                if(armorThing.def.techLevel >= TechLevel.Spacer || isMechanoid)
                {
                    if (dinfo.Weapon.IsMeleeWeapon)
                    {
                        if(dinfo.Weapon.techLevel <= TechLevel.Medieval)
                        {
                            real_armorPenetration *= 0.5f;
                        }
                    }
                    else
                    {
                        if (dinfo.Weapon.techLevel <= TechLevel.Medieval)
                        {
                            real_armorPenetration *= 0.35f;
                        }
                    }
                }
                else if(armorThing.def.techLevel >= TechLevel.Industrial)
                {
                    if (dinfo.Weapon.IsMeleeWeapon)
                    {
                        if (dinfo.Weapon.techLevel <= TechLevel.Neolithic)
                        {
                            real_armorPenetration *= 0.75f;
                        }
                    }
                    else
                    {
                        if (dinfo.Weapon.techLevel <= TechLevel.Medieval)
                        {
                            real_armorPenetration *= 0.5f;
                        }
                    }
                }
            }
            //Log.Message($"real_armorPenetration : {real_armorPenetration}");

            float leftArmor = Mathf.Max(armorRating - real_armorPenetration, 0f); // 방어력 - 방어관통 => 방어차이
            float armorDmg = (real_armorPenetration - armorRating * 0.15f) * 5f;
            //Log.Message($"armorDmg : {armorDmg}");
            armorDmg = Mathf.Clamp01(armorDmg);

            float randomZeroOne = Rand.Value; // 랜덤값 0~1
            float leftArmor2 = leftArmor; // 방어차이값 저장

            if (hasAmor)
            {
                // 갑옷이 데미지 입음
                //float f = damAmount * (0.4f + armorDmg * 0.7f);
                float f = damAmount * (0.2f + armorDmg * 0.5f);
                //Log.Message($"damAmount_f : {f}");
                armorThing.TakeDamage(new DamageInfo(damageDef, (float)GenMath.RoundRandom(f), 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
            }


            // 데미지를 안입을 확률 계산
            float armorHpPer = 1f;

            if (hasAmor)
            {
                armorHpPer = (float)armorThing.HitPoints / armorThing.MaxHitPoints;
            }
            else
            {
                armorHpPer = (float)pawn.health.summaryHealth.SummaryHealthPercent;
            }


            //if (hasAmor || isMechanoid)
            if (true)
            {
                
                float defenceR = 1f;
                defenceR = Mathf.Max(armorRating * 0.9f - real_armorPenetration, 0f);


                float getHitR = 1f - yayoCombat.s_armorEf;
                //Log.Message($"armorHpPer : {armorHpPer}");
                //Log.Message($"getHitR : {getHitR}");
                //Log.Message($"defenceR : {defenceR}");
                if (randomZeroOne * getHitR < defenceR * armorHpPer) // 남은 갑옷 값이 높을 수록 높은 확률로
                {
                    /*
                    if (isMechanoid)
                    {
                        // 메카노이드일 경우
                        forcedDefl = true;
                        damAmount = (float)GenMath.RoundRandom(damAmount * (0.1f + armorDmg * 0.18f)); // 데미지 감소
                        if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp) // 날카로움 데미지를 둔탁함으로 변경
                        {
                            damageDef = DamageDefOf.Blunt;
                        }
                        
                    }
                    else
                    {
                        // 인간, 갑옷 착용의 경우
                        damAmount = 0f; // 데미지 0
                    }
                    */

                    if (Rand.Value < Mathf.Min(leftArmor2, 0.9f)) // 남은 갑옷 값이 높을 수록 높은 확률로
                    {
                        damAmount = 0f;
                    }
                    else
                    {
                        if (hasAmor)
                        {
                            forcedDefl = true;
                            damAmount = (float)GenMath.RoundRandom(damAmount * (0.25f + armorDmg * 0.25f)); // 데미지 감소
                            if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp) // 날카로움 데미지를 둔탁함으로 변경
                            {
                                damageDef = DamageDefOf.Blunt;
                            }
                        }
                        else
                        {
                            forcedDefl = true;
                            damAmount = (float)GenMath.RoundRandom(damAmount * (0.25f + armorDmg * 0.5f)); // 데미지 감소
                            if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp) // 날카로움 데미지를 둔탁함으로 변경
                            {
                                damageDef = DamageDefOf.Blunt;
                            }
                        }
                            

                    }
                    

                    return;
                }
            }
            else
            {
                if (randomZeroOne < leftArmor  * 0.5f) // 남은 갑옷 값 * 0.5 이 높을 수록 높은 확률로
                {
                    damAmount = 0f; // 데미지 0
                    return;
                }
            }

            if (randomZeroOne < leftArmor2 * (0.5f + armorHpPer * 0.5f)) // 남은 갑옷 값이 높을 수록 높은 확률로
            {
                damAmount = (float)GenMath.RoundRandom(damAmount * 0.5f); // 데미지 절반
                if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp) // 날카로움 데미지를 둔탁함으로 변경
                {
                    damageDef = DamageDefOf.Blunt;
                }
                return;
            }
            // 모든 방어 실패시 데미지 증폭
            //damAmount *= yayoCombat.unprotectDmg;
        }

        public const float MaxArmorRating = 2f;

        public const float DeflectThresholdFactor = 0.5f;
    }
}
