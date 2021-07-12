using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;
using System.Linq;


namespace yayoCombat
{

	[HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyDamageToPart")]
    [HarmonyPatch(new Type[] { typeof(DamageInfo), typeof(Pawn), typeof(DamageWorker.DamageResult) })]
    internal class patch_DamageWorker_AddInjury
    {
		[HarmonyPrefix]
		static bool Prefix(DamageWorker_AddInjury __instance, DamageInfo dinfo, Pawn pawn, DamageWorker.DamageResult result)
        {
            // yayo
            if (!yayoCombat.advArmor)
            {
                return true;
            }
            //

            BodyPartRecord exactPartFromDamageInfo = GetExactPartFromDamageInfo(dinfo, pawn);
            if (exactPartFromDamageInfo == null)
            {
                return false;
            }
            dinfo.SetHitPart(exactPartFromDamageInfo);
            float num = dinfo.Amount;
            bool flag = !dinfo.InstantPermanentInjury && !dinfo.IgnoreArmor;
            bool deflectedByMetalArmor = false;
            if (flag)
            {
                DamageDef def = dinfo.Def;
                bool diminishedByMetalArmor = false;
                num = ArmorUtility.GetPostArmorDamage(pawn, num, dinfo.ArmorPenetrationInt, dinfo.HitPart, ref def, ref deflectedByMetalArmor, ref diminishedByMetalArmor, dinfo);
                dinfo.Def = def;
                if (num < dinfo.Amount)
                {
                    result.diminished = true;
                    result.diminishedByMetalArmor = diminishedByMetalArmor;
                }
            }

            if (dinfo.Def.ExternalViolenceFor(pawn))
            {
                num *= pawn.GetStatValue(StatDefOf.IncomingDamageFactor, true);
            }

            // yayo
            if (deflectedByMetalArmor)
            {
                result.deflected = true;
                result.deflectedByMetalArmor = deflectedByMetalArmor;
            }
            //

            if (num <= 0f)
            {
                result.AddPart(pawn, dinfo.HitPart);
                
                return false;
            }
            if (IsHeadshot(dinfo, pawn))
            {
                result.headshot = true;
            }
            if (dinfo.InstantPermanentInjury && (HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart).CompPropsFor(typeof(HediffComp_GetsPermanent)) == null || dinfo.HitPart.def.permanentInjuryChanceFactor == 0f || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(dinfo.HitPart)))
            {
                return false;
            }
            if (!dinfo.AllowDamagePropagation)
            {
                FinalizeAndAddInjury(pawn, num, dinfo, result);
                return false;
            }
            /*
            foreach (string str in Traverse.Create(__instance).Methods())
            {
                Log.Message(str);
            }
            */

            //Traverse.Create(__instance).Method("ApplySpecialEffectsToPart", new object[] { pawn, num, dinfo, result });

            AccessTools.Method(typeof(DamageWorker_AddInjury), "ApplySpecialEffectsToPart").Invoke(__instance, new object[] { pawn, num, dinfo, result });

            return false;
			
		}





        static private BodyPartRecord GetExactPartFromDamageInfo(DamageInfo dinfo, Pawn pawn)
        {
            if (dinfo.HitPart == null)
            {
                BodyPartRecord bodyPartRecord = ChooseHitPart(dinfo, pawn);
                if (bodyPartRecord == null)
                {
                    Log.Warning("ChooseHitPart returned null (any part).");
                }
                return bodyPartRecord;
            }
            if (!pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null).Any((BodyPartRecord x) => x == dinfo.HitPart))
            {
                return null;
            }
            return dinfo.HitPart;
        }


        static private BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
        {
            return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, dinfo.Depth, null);
        }

        private static bool IsHeadshot(DamageInfo dinfo, Pawn pawn)
        {
            return !dinfo.InstantPermanentInjury && dinfo.HitPart.groups.Contains(BodyPartGroupDefOf.FullHead) && dinfo.Def == DamageDefOf.Bullet;
        }




        private static float FinalizeAndAddInjury(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            if (pawn.health.hediffSet.PartIsMissing(dinfo.HitPart))
            {
                return 0f;
            }
            HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart);
            Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
            hediff_Injury.Part = dinfo.HitPart;
            hediff_Injury.source = dinfo.Weapon;
            hediff_Injury.sourceBodyPartGroup = dinfo.WeaponBodyPartGroup;
            hediff_Injury.sourceHediffDef = dinfo.WeaponLinkedHediff;
            hediff_Injury.Severity = totalDamage;
            if (dinfo.InstantPermanentInjury)
            {
                HediffComp_GetsPermanent hediffComp_GetsPermanent = hediff_Injury.TryGetComp<HediffComp_GetsPermanent>();
                if (hediffComp_GetsPermanent != null)
                {
                    hediffComp_GetsPermanent.IsPermanent = true;
                }
                else
                {
                    Log.Error(string.Concat(new object[]
                    {
                        "Tried to create instant permanent injury on Hediff without a GetsPermanent comp: ",
                        hediffDefFromDamage,
                        " on ",
                        pawn
                    }));
                }
            }
            return FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, result);
        }

        private static float FinalizeAndAddInjury(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            HediffComp_GetsPermanent hediffComp_GetsPermanent = injury.TryGetComp<HediffComp_GetsPermanent>();
            if (hediffComp_GetsPermanent != null)
            {
                hediffComp_GetsPermanent.PreFinalizeInjury();
            }
            float partHealth = pawn.health.hediffSet.GetPartHealth(injury.Part);
            if (pawn.IsColonist && !dinfo.IgnoreInstantKillProtection && dinfo.Def.ExternalViolenceFor(pawn) && !Rand.Chance(Find.Storyteller.difficulty.allowInstantKillChance))
            {
                float num = (injury.def.lethalSeverity > 0f) ? (injury.def.lethalSeverity * 1.1f) : 1f;
                float min = 1f;
                float max = Mathf.Min(injury.Severity, partHealth);
                int num2 = 0;
                while (num2 < 7 && pawn.health.WouldDieAfterAddingHediff(injury))
                {
                    float num3 = Mathf.Clamp(partHealth - num, min, max);
                    if (DebugViewSettings.logCauseOfDeath)
                    {
                        Log.Message(string.Format("CauseOfDeath: attempt to prevent death for {0} on {1} attempt:{2} severity:{3}->{4} part health:{5}", new object[]
                        {
                            pawn.Name,
                            injury.Part.Label,
                            num2 + 1,
                            injury.Severity,
                            num3,
                            partHealth
                        }));
                    }
                    injury.Severity = num3;
                    num *= 2f;
                    min = 0f;
                    num2++;
                }
            }
            pawn.health.AddHediff(injury, null, new DamageInfo?(dinfo), result);
            float num4 = Mathf.Min(injury.Severity, partHealth);
            result.totalDamageDealt += num4;
            result.wounded = true;
            result.AddPart(pawn, injury.Part);
            result.AddHediff(injury);
            return num4;
        }






    }

    




    




}
