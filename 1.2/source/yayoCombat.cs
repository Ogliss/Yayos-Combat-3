using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using HugsLib;
using HugsLib.Settings;
using System.Linq;

namespace yayoCombat
{
    public class yayoCombat : ModBase
    {
        public override string ModIdentifier => "YayoCombat3";
        public static bool using_dualWeld = false;

        static yayoCombat()
        {
            if (ModsConfig.ActiveModsInLoadOrder.Any(mod => mod.PackageId.ToLower().Contains("DualWield".ToLower())))
            {
                using_dualWeld = true;
            }
        }


        private SettingHandle<bool> refillMechAmmoSetting;
        public static bool refillMechAmmo = true;

        private SettingHandle<bool> ammoSetting;
        public static bool ammo = false;

        private SettingHandle<float> ammoGenSetting;
        public static float ammoGen = 1f;

        private SettingHandle<float> maxAmmoSetting;
        public static float maxAmmo = 1f;

        private SettingHandle<int> enemyAmmoSetting;
        public static int enemyAmmo = 70;
        public static float s_enemyAmmo = 0.70f;

        private SettingHandle<int> supplyAmmoDistSetting;
        public static int supplyAmmoDist = 4;



        private SettingHandle<bool> advAniSetting;
        public static bool advAni = true;

        private SettingHandle<float> meleeDelaySetting;
        public static float meleeDelay = 0.7f;

        private SettingHandle<float> meleeRandomSetting;
        public static float meleeRandom = 1.3f;

        private SettingHandle<bool> ani_twirlSetting;
        public static bool ani_twirl = true;

        private SettingHandle<bool> handProtectSetting;
        public static bool handProtect = true;

        private SettingHandle<bool> advArmorSetting;
        public static bool advArmor = true;


        private SettingHandle<int> armorEfSetting;
        public static int armorEf = 50;
        public static float s_armorEf = 0.5f;

        private SettingHandle<float> unprotectDmgSetting;
        public static float unprotectDmg = 1.1f;




        private SettingHandle<bool> advShootAccSetting;
        public static bool advShootAcc = true;

        // 명중률
        // accuracy
        private SettingHandle<int> accEfSetting;
        public static int accEf = 60;
        public static float s_accEf = 0.6f;

        private SettingHandle<int> missBulletHitSetting;
        public static int missBulletHit = 50;
        public static float s_missBulletHit = 0.5f;

        private SettingHandle<bool> mechAccSetting;
        public static bool mechAcc = true;

        private SettingHandle<bool> turretAccSetting;
        public static bool turretAcc = true;

        private SettingHandle<int> baseSkillSetting;
        public static int baseSkill = 5;

        private SettingHandle<bool> colonistAccSetting;
        public static bool colonistAcc = false;

        private SettingHandle<float> bulletSpeedSetting;
        public static float bulletSpeed = 3f;

        private SettingHandle<float> maxBulletSpeedSetting;
        public static float maxBulletSpeed = 200f;

        private SettingHandle<bool> enemyRocketSetting;
        public static bool enemyRocket = false;

        enum en_ammoType { normal, fire, emp };

        public static List<ThingDef> ar_customAmmoDef = new List<ThingDef>();

        /*
        private SettingHandle<bool> yayoJokeSetting;
        public static bool yayoJoke = false;
        */

        public override void DefsLoaded()
        {
            ammoSetting = Settings.GetHandle<bool>("ammo", "ammo_title".Translate(), "ammo_desc".Translate(), false);
            ammo = ammoSetting.Value;

            refillMechAmmoSetting = Settings.GetHandle<bool>("refillMechAmmo", "refillMechAmmo_title".Translate(), "refillMechAmmo_desc".Translate(), true);
            refillMechAmmo = refillMechAmmoSetting.Value;

            ammoGenSetting = Settings.GetHandle<float>("ammoGen", "ammoGen_title".Translate(), "ammoGen_desc".Translate(), 1f);
            ammoGen = ammoGenSetting.Value;

            maxAmmoSetting = Settings.GetHandle<float>("maxAmmo", "maxAmmo_title".Translate(), "maxAmmo_desc".Translate(), 1f);
            maxAmmo = maxAmmoSetting.Value;

            enemyAmmoSetting = Settings.GetHandle<int>("enemyAmmo", "enemyAmmo_title".Translate(), "enemyAmmo_desc".Translate(), 70);
            enemyAmmo = enemyAmmoSetting.Value;
            s_enemyAmmo = (float)enemyAmmo / 100f;

            supplyAmmoDistSetting = Settings.GetHandle<int>("supplyAmmoDist", "supplyAmmoDist_title".Translate(), "supplyAmmoDist_desc".Translate(), 4);
            supplyAmmoDist = supplyAmmoDistSetting.Value;

            advAniSetting = Settings.GetHandle<bool>("advAni", "advAni_title".Translate(), "advAni_desc".Translate(), true);
            advAni = advAniSetting.Value;

            meleeDelaySetting = Settings.GetHandle<float>("meleeDelay", "meleeDelay_title".Translate(), "meleeDelay_desc".Translate(), 0.7f);
            meleeDelay = meleeDelaySetting.Value;

            meleeRandomSetting = Settings.GetHandle<float>("meleeRandom", "meleeRandom_title".Translate(), "meleeRandom_desc".Translate(), 1.3f);
            meleeRandom = meleeRandomSetting.Value;

            ani_twirlSetting = Settings.GetHandle<bool>("ani_twirl", "ani_twirl_title".Translate(), "ani_twirl_desc".Translate(), true);
            ani_twirl = ani_twirlSetting.Value;

            //
            handProtectSetting = Settings.GetHandle<bool>("handProtect", "handProtect_title".Translate(), "handProtect_desc".Translate(), true);
            handProtect = handProtectSetting.Value;

            //

            advArmorSetting = Settings.GetHandle<bool>("advArmor", "advArmor_title".Translate(), "advArmor_desc".Translate(), true);
            advArmor = advArmorSetting.Value;

            armorEfSetting = Settings.GetHandle<int>("armorEf", "armorEf_title".Translate(), "armorEf_desc".Translate(), 50);
            armorEf = armorEfSetting.Value;
            s_armorEf = (float)accEf / 100f;

            unprotectDmgSetting = Settings.GetHandle<float>("unprotectDmg", "unprotectDmg_title".Translate(), "unprotectDmg_desc".Translate(), 1.1f);
            unprotectDmg = unprotectDmgSetting.Value;
            unprotectDmgSetting.NeverVisible = true;

            advShootAccSetting = Settings.GetHandle<bool>("advShootAcc", "advShootAcc_title".Translate(), "advShootAcc_desc".Translate(), true);
            advShootAcc = advShootAccSetting.Value;

            accEfSetting = Settings.GetHandle<int>("accEf", "accEf_title".Translate(), "accEf_desc".Translate(), 60);
            accEf = accEfSetting.Value;
            s_accEf = (float)accEf / 100f;

            /*
            statAccSetting = Settings.GetHandle<int>("statAcc", "statAcc_title".Translate(), "statAcc_desc".Translate(), 0);
            statAcc = statAccSetting.Value;
            s_statAccEf = accEf * (float)statAcc / 100f;
            s_equipAccEf = accEf - s_statAccEf;
            */

            missBulletHitSetting = Settings.GetHandle<int>("missBulletHit", "missBulletHit_title".Translate(), "missBulletHit_desc".Translate(), 50);
            missBulletHit = missBulletHitSetting.Value;
            s_missBulletHit = (float)missBulletHit / 100f;

            mechAccSetting = Settings.GetHandle<bool>("mechAcc", "mechAcc_title".Translate(), "mechAcc_desc".Translate(), true);
            mechAcc = mechAccSetting.Value;

            turretAccSetting = Settings.GetHandle<bool>("turretAcc", "turretAcc_title".Translate(), "turretAcc_desc".Translate(), true);
            turretAcc = turretAccSetting.Value;

            baseSkillSetting = Settings.GetHandle<int>("baseSkill", "baseSkill_title".Translate(), "baseSkill_desc".Translate(), 5);
            baseSkill = baseSkillSetting.Value;

            colonistAccSetting = Settings.GetHandle<bool>("colonistAcc", "colonistAcc_title".Translate(), "colonistAcc_desc".Translate(), false);
            colonistAcc = colonistAccSetting.Value;


            bulletSpeedSetting = Settings.GetHandle<float>("bulletSpeed", "bulletSpeed_title".Translate(), "bulletSpeed_desc".Translate(), 3f);
            bulletSpeed = bulletSpeedSetting.Value;

            maxBulletSpeedSetting = Settings.GetHandle<float>("maxBulletSpeed", "maxBulletSpeed_title".Translate(), "maxBulletSpeed_desc".Translate(), 200f);
            maxBulletSpeed = maxBulletSpeedSetting.Value;

            enemyRocketSetting = Settings.GetHandle<bool>("enemyRocket", "useRocket_title".Translate(), "useRocket_desc".Translate(), false);
            enemyRocket = enemyRocketSetting.Value;

            /*
            yayoJokeSetting = Settings.GetHandle<bool>("yayoJoke", "yayoJoke_title".Translate(), "yayoJoke_desc".Translate(), false);
            yayoJoke = yayoJokeSetting.Value;
            */

            patchDef2();
        }

        public override void SettingsChanged()
        {
            ammo = ammoSetting.Value;
            ammoGen = ammoGenSetting.Value;
            maxAmmo = maxAmmoSetting.Value;

            enemyAmmoSetting.Value = Mathf.Clamp(enemyAmmoSetting.Value, 0, 500);
            enemyAmmo = enemyAmmoSetting.Value;
            s_enemyAmmo = (float)enemyAmmo / 100f;

            supplyAmmoDist = Mathf.Clamp(supplyAmmoDistSetting.Value, -1, 100);

            advAni = advAniSetting.Value;
            meleeDelaySetting.Value = Mathf.Clamp(meleeDelaySetting.Value, 0.2f, 2f);
            meleeDelay = meleeDelaySetting.Value;
            meleeRandomSetting.Value = Mathf.Clamp(meleeRandomSetting.Value, 0f, 1.5f);
            meleeRandom = meleeRandomSetting.Value;
            ani_twirl = ani_twirlSetting.Value;

            handProtect = handProtectSetting.Value;

            advArmor = advArmorSetting.Value;

            armorEfSetting.Value = Mathf.Clamp(armorEfSetting.Value, 0, 100);
            armorEf = armorEfSetting.Value;
            s_armorEf = (float)armorEf / 100f;

            unprotectDmgSetting.Value = Mathf.Clamp(unprotectDmgSetting.Value, 0.1f, 2f);
            unprotectDmg = unprotectDmgSetting.Value;

            advShootAcc = advShootAccSetting.Value;

            accEfSetting.Value = Mathf.Clamp(accEfSetting.Value, 0, 100);
            accEf = accEfSetting.Value;
            s_accEf = (float)accEf / 100f;

            missBulletHitSetting.Value = Mathf.Clamp(missBulletHitSetting.Value, 0, 100);
            missBulletHit = missBulletHitSetting.Value;
            s_missBulletHit = (float)missBulletHit / 100f;

            mechAcc = mechAccSetting.Value;
            turretAcc = turretAccSetting.Value;
            colonistAcc = colonistAccSetting.Value;

            baseSkillSetting.Value = Mathf.Clamp(baseSkillSetting.Value, 0, 20);
            baseSkill = baseSkillSetting.Value;

            bulletSpeedSetting.Value = Mathf.Clamp(bulletSpeedSetting.Value, 0.01f, 100f);
            bulletSpeed = bulletSpeedSetting.Value;

            maxBulletSpeedSetting.Value = Mathf.Clamp(maxBulletSpeedSetting.Value, 1f, 10000f);
            maxBulletSpeed = maxBulletSpeedSetting.Value;

            enemyRocket = enemyRocketSetting.Value;

            //yayoJoke = yayoJokeSetting.Value;

        }

        static public void patchDef1()
        {
            //Log.Message($"# Yayo's Combat : init 1");
        }

        public static void HandsAndFeetProtection()
        {
            foreach (ThingDef t in from thing in DefDatabase<ThingDef>.AllDefs
                                   where
                                       thing.apparel != null
                                       && thing.apparel.bodyPartGroups != null
                                       && thing.apparel.bodyPartGroups.Count > 0
                                       && (thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Hands) || thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Feet))
                                       && !thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso)
                                       && !thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead)
                                       && !thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead)
                                       && !thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Shoulders)
                                   select thing)
            {
                List<ApparelLayerDef> b = new List<ApparelLayerDef>();
                for (int i = 0; i < t.apparel.layers.Count; i++)
                {
                    switch (t.apparel.layers[i].defName)
                    {
                        case "OnSkin":
                            b.Add(yayoCombat_Defs.ApparelLayerDefOf.OnSkin_A);
                            break;
                        case "Shell":
                            b.Add(yayoCombat_Defs.ApparelLayerDefOf.Shell_A);
                            break;
                        case "Middle":
                            b.Add(yayoCombat_Defs.ApparelLayerDefOf.Middle_A);
                            break;
                        case "Belt":
                            b.Add(yayoCombat_Defs.ApparelLayerDefOf.Belt_A);
                            break;
                        case "Overhead":
                            b.Add(yayoCombat_Defs.ApparelLayerDefOf.Overhead_A);
                            break;
                    }

                }
                if (b.Count > 0)
                {
                    t.apparel.layers = b;
                }

            }


            foreach (ThingDef t in from thing in DefDatabase<ThingDef>.AllDefs
                                   where
                                        thing.apparel != null
                                        && thing.apparel.bodyPartGroups != null
                                        && thing.apparel.bodyPartGroups.Count > 0
                                   select thing)
            {
                List<BodyPartGroupDef> b = t.apparel.bodyPartGroups;
                if (b.Contains(BodyPartGroupDefOf.Arms) && !b.Contains(BodyPartGroupDefOf.Hands))
                {
                    b.Add(BodyPartGroupDefOf.Hands);
                }
                if (b.Contains(BodyPartGroupDefOf.Legs) && !b.Contains(BodyPartGroupDefOf.Feet))
                {
                    b.Add(BodyPartGroupDefOf.Feet);
                }


            }
        }

        public static void EnableAmmunitionSystem()
        {

            List<string> specialKeyword_lower = new List<string>() { "frost", "energy", "cryo", "gleam", "laser", "plasma", "beam", "magic"
                    , "thunder", "poison", "elec", "wave", "psy", "cold", "tox", "atom", "pulse", "tornado", "water", "liqu", "tele", "matter"
                };
            List<string> specialKeyword_lowerOver = new List<string>() { "ice"
                };
            List<string> specialKeyword_fullCase = new List<string>() { "Ice"
                };

            // 무기셋업
            // weapon setup
            foreach (ThingDef t in from t in DefDatabase<ThingDef>.AllDefs
                                   where
                                        t.IsRangedWeapon
                                        && t.Verbs != null
                                        && t.Verbs.Count >= 1
                                        && (t.modExtensions == null || !t.modExtensions.Exists(x => x.ToString() == "HeavyWeapons.HeavyWeapon"))
                                   select t)
            {

                if (t.techLevel <= TechLevel.Animal) continue;
                string ammoDefName = "";

                VerbProperties v = t.Verbs[0];

                if (v.verbClass == null
                    || v.verbClass == typeof(Verb_ShootOneUse)
                    || v.consumeFuelPerShot > 0f
                    || (t.weaponTags != null && (t.weaponTags.Contains("TurretGun") || t.weaponTags.Contains("Artillery") || t.weaponTags.Contains("yy_NoAmmo")))
                    )
                {
                    //Log.Message($"# {t.defName}");
                    continue;
                }

                t.menuHidden = false;

                CompProperties_Reloadable cp = new CompProperties_Reloadable();

                float shotPerSec = (float)v.burstShotCount / ((float)(v.ticksBetweenBurstShots * 0.016666f * (float)v.burstShotCount) + v.warmupTime + t.statBases.GetStatValueFromList(StatDefOf.RangedWeapon_Cooldown, 0f)); // 1초당 평균 발사 수
                                                                                                                                                                                                                               // 최대 충전 시, 전투 지속시간
                                                                                                                                                                                                                               // When fully charged, battle duration
                float fightTime = 60f * 1.5f;
                // 최대 충전 수
                // maximum number of charges
                cp.maxCharges = Mathf.Max(3, Mathf.RoundToInt(fightTime * shotPerSec * maxAmmo));
                // 1발당 자원 필요량
                // Resource requirements per shot
                cp.ammoCountPerCharge = 1;
                // 재장전 시간
                // reload time
                cp.baseReloadTicks = Mathf.RoundToInt(60);


                cp.soundReload = SoundDefOf.Standard_Reload;
                cp.hotKey = KeyBindingDefOf.Misc4;
                cp.chargeNoun = "ammo";
                cp.displayGizmoWhileUndrafted = true;


                en_ammoType ammoType = en_ammoType.normal;



                if (v.defaultProjectile != null && v.defaultProjectile.projectile != null && v.defaultProjectile.projectile.damageDef != null)
                {


                    if (t.weaponTags != null)
                    {
                        if (t.weaponTags.Contains("ammo_none")) continue;

                        string customAmmoCode = getContainStringByList("ammo_", t.weaponTags);

                        if (customAmmoCode != "")
                        {
                            string[] ar_customAmmoCode = customAmmoCode.Split('/');

                            ammoDefName = $"yy_{ar_customAmmoCode[0]}";
                            if (ThingDef.Named(ammoDefName) == null) ammoDefName = "";

                            int customAmmoCount;
                            if (ar_customAmmoCode.Length >= 2 && int.TryParse(ar_customAmmoCode[1], out customAmmoCount))
                            {
                                cp.maxCharges = Mathf.Max(1, Mathf.RoundToInt(customAmmoCount * maxAmmo));
                            }

                            int customConsume;
                            if (ar_customAmmoCode.Length >= 3 && int.TryParse(ar_customAmmoCode[2], out customConsume))
                            {
                                cp.ammoCountPerCharge = Mathf.Max(1, customConsume);
                            }
                        }

                    }

                    if (ammoDefName == "")
                    {
                        ammoDefName = "yy_ammo_";

                        ProjectileProperties pp = v.defaultProjectile.projectile;
                        if (new List<DamageDef>() { DamageDefOf.Bomb, DamageDefOf.Flame, DamageDefOf.Burn }.Contains(pp.damageDef))
                        {
                            // 발사체 타입 : 폭발, 화염
                            ammoType = en_ammoType.fire;
                            cp.ammoCountPerCharge = Mathf.Max(1, Mathf.RoundToInt(pp.explosionRadius));
                        }
                        else if (new List<DamageDef>() { DamageDefOf.Smoke }.Contains(pp.damageDef))
                        {
                            // 발사체 타입 : 연막
                            ammoType = en_ammoType.fire;
                            cp.ammoCountPerCharge = Mathf.Max(1, Mathf.RoundToInt(pp.explosionRadius / 3f));
                        }
                        else if (new List<DamageDef>() { DamageDefOf.EMP, DamageDefOf.Deterioration, DamageDefOf.Extinguish, DamageDefOf.Frostbite, DamageDefOf.Rotting, DamageDefOf.Stun, DamageDefOf.TornadoScratch }.Contains(pp.damageDef))
                        {
                            // 발사체 타입 : EMP
                            ammoType = en_ammoType.emp;
                            cp.ammoCountPerCharge = Mathf.Max(1, Mathf.RoundToInt(pp.explosionRadius / 3f));

                        }
                        else if (containCheckByList(t.defName.ToLower(), specialKeyword_lower)
                           || containCheckByList(t.defName, specialKeyword_fullCase)
                           || containCheckByList(pp.damageDef.defName.ToLower(), specialKeyword_lower)
                           || containCheckByList(pp.damageDef.defName, specialKeyword_fullCase)
                           ) // 데미지타입 키워드검색
                        {
                            // 특수타입 키워드가 포함된 무기 defname
                            ammoType = en_ammoType.emp;
                            cp.ammoCountPerCharge = Mathf.Max(1, Mathf.RoundToInt(pp.explosionRadius));
                        }
                        //else if (pp.damageDef.modContentPack.ToString().Contains("Ludeon")) // 발사체 타입 : 모든 바닐라 데미지 타입
                        else // 발사체 타입 : 그외 모든 데미지 타입 (모드 데미지타입 포함
                        {

                            if (pp.explosionRadius > 0f)
                            {
                                // 폭발하는 경우
                                cp.ammoCountPerCharge = Mathf.Max(1, Mathf.RoundToInt(pp.explosionRadius));

                                if (pp.damageDef.armorCategory != null)
                                {
                                    switch (pp.damageDef.armorCategory.defName) // 방어 타입에 따라 구분
                                    {
                                        case "Sharp":
                                            ammoType = en_ammoType.fire;
                                            break;
                                        case "Heat":
                                            ammoType = en_ammoType.fire;
                                            break;
                                        case "Blunt":
                                            ammoType = en_ammoType.fire;
                                            break;
                                        default: // 모드 추가 방어타입
                                            ammoType = en_ammoType.emp;
                                            break;
                                    }
                                }
                                else
                                {
                                    // 방어타입 없음
                                    ammoType = en_ammoType.emp;
                                }

                            }
                            else
                            {
                                // 폭발하지 않음
                                if (pp.damageDef.armorCategory != null)
                                {
                                    switch (pp.damageDef.armorCategory.defName) // 방어 타입에 따라 구분
                                    {
                                        case "Sharp":
                                            ammoType = en_ammoType.normal;
                                            break;
                                        case "Heat":
                                            ammoType = en_ammoType.fire;
                                            break;
                                        case "Blunt":
                                            ammoType = en_ammoType.normal;
                                            break;
                                        default: // 모드 추가 방어타입
                                            ammoType = en_ammoType.emp;
                                            break;
                                    }
                                }
                                else
                                {
                                    // 방어타입 없음
                                    ammoType = en_ammoType.emp;
                                    cp.ammoCountPerCharge = Mathf.Max(1, Mathf.RoundToInt(pp.explosionRadius));

                                }
                            }

                        }



                        if (t.techLevel >= TechLevel.Spacer)
                        {
                            // 우주 이상
                            ammoDefName += "spacer";

                        }
                        else if (t.techLevel >= TechLevel.Industrial)
                        {
                            // 산업 이상
                            ammoDefName += "industrial";
                        }
                        else
                        {
                            // 원시 이상
                            ammoDefName += "primitive";
                        }

                        switch (ammoType)
                        {
                            case en_ammoType.fire:
                                ammoDefName += "_fire";
                                break;
                            case en_ammoType.emp:
                                ammoDefName += "_emp";
                                break;
                        }


                    }


                    cp.ammoDef = ThingDef.Named(ammoDefName);

                }


                // 완전한 커스텀 탄약 Def
                if (t.weaponTags != null)
                {
                    string customAmmoCode2 = getContainStringByList("ammoDef_", t.weaponTags);
                    if (customAmmoCode2 != "")
                    {
                        string[] ar_customAmmoCode2 = customAmmoCode2.Split('/');
                        string[] ar_customAmmoDefCode = ar_customAmmoCode2[0].Split('_');
                        if (ar_customAmmoDefCode.Length >= 2)
                        {
                            cp.ammoDef = ThingDef.Named(ar_customAmmoDefCode[1]);

                            if (cp.ammoDef != null) ar_customAmmoDef.Add(cp.ammoDef);

                            int customAmmoCount;
                            if (ar_customAmmoCode2.Length >= 2 && int.TryParse(ar_customAmmoCode2[1], out customAmmoCount))
                            {
                                cp.maxCharges = Mathf.Max(1, Mathf.RoundToInt(customAmmoCount * maxAmmo));
                            }

                            int customConsume;
                            if (ar_customAmmoCode2.Length >= 3 && int.TryParse(ar_customAmmoCode2[2], out customConsume))
                            {
                                cp.ammoCountPerCharge = Mathf.Max(1, customConsume);
                            }

                        }
                    }
                }





                if (cp.ammoDef == null)
                {
                    if (t.techLevel >= TechLevel.Spacer)
                    {
                        // 우주 이상
                        cp.ammoDef = ThingDef.Named("yy_ammo_spacer");

                    }
                    else if (t.techLevel >= TechLevel.Industrial)
                    {
                        // 산업 이상
                        cp.ammoDef = ThingDef.Named("yy_ammo_industrial");
                    }
                    else
                    {
                        // 원시 이상
                        cp.ammoDef = ThingDef.Named("yy_ammo_primitive");
                    }

                }


                t.comps.Add(cp);

                /*
                // 디버그 로그
                if(cp.ammoDef.defName == "yy_ammo_primitive_fire" || cp.ammoDef.defName == "yy_ammo_primitive_emp")
                {
                    Log.Warning(cp.ammoDef.defName);
                }
                */

                /*
                // 디버그 로그
                if (v.defaultProjectile != null && v.defaultProjectile.projectile != null && v.defaultProjectile.projectile.damageDef != null)
                {
                    ProjectileProperties pp = v.defaultProjectile.projectile;
                    if (pp.damageDef.armorCategory != null)
                    {
                        Log.Message($"{t.label}({t.defName}) - [{cp.ammoDef.label}], dmgType:[{pp.damageDef}], armorType:[{pp.damageDef.armorCategory}], explosion:[{pp.explosionRadius}], ammoPerCharge:[{cp.ammoCountPerCharge}]");
                    }
                    else
                    {
                        Log.Message($"{t.label}({t.defName}) - [{cp.ammoDef.label}], dmgType:[{pp.damageDef}], explosion:[{pp.explosionRadius}], ammoPerCharge:[{cp.ammoCountPerCharge}]");
                    }

                }
                else
                {
                    Log.Message($"{t.label}({t.defName}) - [{cp.ammoDef.label}], ammoPerCharge:[{cp.ammoCountPerCharge}]");
                }
                */






            }

            // 레시피
            foreach (RecipeDef t in from thing in DefDatabase<RecipeDef>.AllDefs
                                    where
                                         thing.defName.Contains("yy_ammo")
                                    select thing)
            {
                if (t.products != null && t.products.Count > 0)
                {
                    t.products[0].count = Mathf.RoundToInt((float)t.products[0].count * ammoGen);
                }
            }
        }

        public static void DisableAmmunitionSystem()
        {
            // 탄약 시스템 사용안함
            // 기본 탄약 레시피 제거
            foreach (RecipeDef t in from thing in DefDatabase<RecipeDef>.AllDefs
                                    where
                                         thing.defName.Contains("yy_ammo")
                                    select thing)
            {
                t.recipeUsers = new List<ThingDef>();
                t.ResolveReferences();
            }

            foreach (ThingDef t in from thing in DefDatabase<ThingDef>.AllDefs
                                   where
                                        thing.defName.Contains("yy_ammo")
                                   select thing)
            {
                t.tradeability = Tradeability.None;
                t.tradeTags = null;
                t.ResolveReferences();
            }
        }

        public static void AdvancedArmour()
        {
            foreach (PawnKindDef p in from pawn in DefDatabase<PawnKindDef>.AllDefs
                                      where
                                           pawn.defaultFactionType == FactionDefOf.Mechanoid
                                      select pawn
                               )
            {
                p.race.SetStatBaseValue(StatDefOf.ArmorRating_Sharp, p.race.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) * 1.3f);
                p.combatPower *= 1.3f; // 메카노이드 수량 감소
            }
        }

        static public void patchDef2()
        {
            //Log.Message($"# Yayo's Combat : init 2");
            // 발, 손 보호
            // foot and hand protection
            if (handProtect)
            {
                try
                {
                    HandsAndFeetProtection();
                }
                catch (Exception)
                {
                    Log.Warning("Yayo's Comabt Hands and Feet Patch failed");
                    throw;
                }
            }

            // 탄약
            // ammunition
            if (ammo)
            {
                try
                {
                    EnableAmmunitionSystem();
                }
                catch (Exception)
                {

                    Log.Warning("Yayo's Comabt Enable Ammunition System Patch failed");
                    throw;
                }
            }
            else
            {
                try
                {
                    DisableAmmunitionSystem();
                }
                catch (Exception)
                {

                    Log.Warning("Yayo's Comabt Disable Ammunition System Patch failed");
                    throw;
                }
            }

            // 메카노이드 방어력 강화
            if (yayoCombat.advArmor)
            {
                try
                {
                    AdvancedArmour();
                }
                catch (Exception)
                {
                    Log.Warning("Yayo's Comabt Advanced Armour System Patch failed");
                    throw;
                }
            }
            //PawnKindDef.Named("Mech_Centipede").combatPower *= 1.3f;

            // 대전차 로켓
            ThingDef def_rocket = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_AntiArmor_Rocket");
            if (def_rocket != null)
            {
                if (enemyRocket)
                {

                }
                else
                {
                    def_rocket.weaponTags = new List<string>();
                }
            }


            /*
            // 야요 조크
            if (yayoJoke)
            {
                foreach (ThingDef t in from thing in DefDatabase<ThingDef>.AllDefs
                                       where
                                            thing.defName.Contains("yy_ammo")
                                            && !thing.defName.Contains("unfinish")
                                       select thing)
                {
                    GraphicData gd = new GraphicData();
                    gd.graphicClass = typeof(Graphic_StackCount);
                    gd.texPath = "Things/Item/Drug/Yayo";


                    gd.color = new Color(0.7f, 1f, 0.7f);
                    if (t.defName.Contains("fire"))
                    {
                        gd.color = new Color(1f, 0.7f, 0.7f);
                    }
                    else if (t.defName.Contains("emp"))
                    {
                        gd.color = new Color(0.7f, 0.7f, 1f);
                    }


                    gd.drawSize = new Vector2(0.75f, 0.75f);
                    t.graphicData = gd;


                }




                foreach (ThingDef t in from thing in DefDatabase<ThingDef>.AllDefs
                                       where
                                       thing.IsRangedWeapon
                                       && thing.Verbs != null
                                       && thing.Verbs.Count >= 1
                                       && thing.Verbs[0].defaultProjectile != null
                                       select thing)
                {
                    VerbProperties v = t.Verbs[0];

                    GraphicData gd = new GraphicData();
                    gd.graphicClass = typeof(Graphic_Single);
                    gd.texPath = "Things/Item/Drug/Yayo/Yayo_a";
                    gd.shaderType = ShaderTypeDefOf.Transparent;
                    gd.color = new Color(1f, 1f, 1f);
                    gd.drawSize = new Vector2(0.75f, 0.75f);

                    ThingDef bullet = ThingDef.Named(v.defaultProjectile.defName);
                    
                    
                    bullet.graphicData = gd;
                    //v.defaultProjectile.graphicData = gd;

                    
                    Graphic gp = new Graphic();
                    gp.data = gd;
                    gp.path = "Things/Item/Drug/Yayo/Yayo_a";
                    gp.drawSize = new Vector2(1f, 1f);
                    
                    v.defaultProjectile.graphic = gp;
                    v.defaultProjectile.graphic.path = "Things/Item/Drug/Yayo/Yayo_a";

                    bullet.graphicData = gd;
                    bullet.graphic = gp;
                    bullet.graphic.path = "Things/Item/Drug/Yayo/Yayo_a";


                    //DefGenerator.AddImpliedDef<ThingDef>(bullet);

                    v.defaultProjectile = bullet;
                    bullet.graphicData. = 
                    
                    //DefGenerator.AddImpliedDef<ThingDef>(t);
                    
                    //DefGenerator.GenerateImpliedDefs_PreResolve();
                }
                
            }
            */



        }



        static public bool containCheckByList(string origin, List<string> ar)
        {
            for(int i = 0; i < ar.Count; i++)
            {
                if (origin.Contains(ar[i]))
                {
                    return true;
                }
            }
            return false;

        }

        static public string getContainStringByList(string keyword, List<string> ar)
        {
            for (int i = 0; i < ar.Count; i++)
            {
                if (ar[i].Contains(keyword))
                {
                    return ar[i];
                }
            }
            return "";

        }







    }











}
