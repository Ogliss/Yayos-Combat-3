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
	internal class Patch_StatPart_ReloadMarketValue_TransformAndExplain
	{
		[HarmonyPrefix]
		static bool Prefix(StatRequest req, ref float val, StringBuilder explanation)
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
	internal class Patch_ThingFilter_SetFromPreset
	{
		[HarmonyPrefix]
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
	internal class Patch_Pawn_TickRare
	{
		[HarmonyPostfix]
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
	internal class Patch_Pawn_EquipmentTracker_DropAllEquipment
	{
		[HarmonyPrefix]
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
	internal class Patch_CompReloadable_PostExposeData
	{
		[HarmonyPrefix]
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
	internal class Patch_ThingWithComps_GetFloatMenuOptions
	{
		[HarmonyPostfix]
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
	internal class Patch_Building_TurretGun_MakeGun
	{
		[HarmonyPrefix]
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
	internal class Patch_ThingSetMaker_TraderStock_Generate
	{
		[HarmonyPrefix]
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
				Thing t;

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
	internal class Patch_TraderKindDef_WillTrade
	{
		[HarmonyPrefix]
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
	internal class Patch_CompReloadable_CreateVerbTargetCommand
	{
		[HarmonyPrefix]
		static bool Prefix(ref Command_Reloadable __result, CompReloadable __instance, Thing gear, Verb verb)
		{
			if (yayoCombat.ammo && gear.def.IsWeapon)
			{
				verb.caster = __instance.Wearer;

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
					if (verb.verbProps.defaultProjectile.graphicData != null)
					{
						commandReloadable.overrideColor = new Color?(verb.verbProps.defaultProjectile.graphicData.color);
					}
				}
				else
				{
					commandReloadable.icon = verb.UIIcon != BaseContent.BadTex ? verb.UIIcon : gear.def.uiIcon;
					commandReloadable.iconAngle = gear.def.uiIconAngle;
					commandReloadable.iconOffset = gear.def.uiIconOffset;
					commandReloadable.defaultIconColor = gear.DrawColor;
				}
				if (!__instance.Wearer.IsColonistPlayerControlled || !__instance.Wearer.Drafted)
					commandReloadable.Disable();
				else if (verb.verbProps.violent && __instance.Wearer.WorkTagIsDisabled(WorkTags.Violent))
					commandReloadable.Disable("IsIncapableOfViolenceLower".Translate(__instance.Wearer.LabelShort, __instance.Wearer).CapitalizeFirst() + ".");
				else if (!__instance.CanBeUsed)
					commandReloadable.Disable(__instance.DisabledReason(__instance.MinAmmoNeeded(false), __instance.MaxAmmoNeeded(false)));

				__result = commandReloadable;

				return false;
			}
			return true;
		}
	}

	// 추가적인 탄약표시 기즈모 생성
	[HarmonyPatch(typeof(Pawn_EquipmentTracker), "GetGizmos")]
	internal class Patch_Pawn_EquipmentTracker_GetGizmos
	{
		[HarmonyPostfix]
		static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_EquipmentTracker __instance)
		{
			foreach (var gizmo in __result)
				yield return gizmo;

			if (!yayoCombat.ammo)
				yield break;

			if (PawnAttackGizmoUtility.CanShowEquipmentGizmos())
			{
				foreach (var thing in __instance.AllEquipmentListForReading)
				{
					foreach (var comp in thing.AllComps)
					{
						foreach (var gizmo in comp.CompGetWornGizmosExtra())
						{
							yield return gizmo;
						}
					}
				}
			}
		}
	}




	// 남은 탄약이 0 일경우 사냥 중지, 탄약 줍기 ai
	[HarmonyPatch(typeof(CompReloadable), "UsedOnce")]
	internal class Patch_CompReloadable_UsedOnce
	{
		[HarmonyPrefix]
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
	internal class Patch_WorkGiver_HasHuntingWeapon
	{
		[HarmonyPrefix]
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
	[HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor")]
	internal class Patch_PawnGenerator_GenerateGearFor
	{
		[HarmonyPostfix]
		[HarmonyPriority(Priority.Last)]
		static void Postfix(Pawn pawn)
		{
			if (!yayoCombat.ammo) 
				return;

			var allWeaponsComps = new List<CompReloadable>();
			// get all generated equipped weapons
			if (pawn?.equipment?.AllEquipmentListForReading != null)
			{
				foreach (var thing in pawn.equipment.AllEquipmentListForReading)
				{
					var comp = thing.GetComp<CompReloadable>();
					if (comp != null && thing.def.IsWeapon)
						allWeaponsComps.Add(comp);
				}
			}
			// get all generated weapons in inventory
			if (pawn?.inventory?.innerContainer != null)
			{
				foreach (var thing in pawn.inventory.innerContainer)
				{
					var comp = thing.TryGetComp<CompReloadable>();
					if (comp != null && thing.def.IsWeapon)
						allWeaponsComps.Add(comp);
				}
			}

			// add ammo to all weapons found
			foreach (var comp in allWeaponsComps)
			{
				int charges;
				if (yayoCombat.s_enemyAmmo <= 1f || pawn.Faction != null && pawn.Faction.IsPlayer)
					charges = Mathf.Min(comp.MaxCharges, Mathf.RoundToInt(comp.MaxCharges * yayoCombat.s_enemyAmmo * Rand.Range(0.7f, 1.3f)));
				else
					charges = Mathf.RoundToInt(comp.MaxCharges * yayoCombat.s_enemyAmmo * Rand.Range(0.7f, 1.3f));
				comp.remainingCharges = charges;
			}
		}
	}


	



	// 아이템 생성 시 기본 탄약 수
	[HarmonyPatch(typeof(CompReloadable), "PostPostMake")]
	internal class Patch_CompReloadable_PostPostMake
	{
		[HarmonyPostfix]
		[HarmonyPriority(0)]
		static void Postfix(CompReloadable __instance)
		{
			if (yayoCombat.ammo && __instance.parent.def.IsWeapon)
			{
				if (GenTicks.TicksGame <= 5)
				{
					__instance.remainingCharges = Mathf.RoundToInt(__instance.MaxCharges * yayoCombat.s_enemyAmmo);
				}
				else
				{
					__instance.remainingCharges = 0;
				}
			}
		}
	}


	[HarmonyPatch(typeof(ReloadableUtility), "FindPotentiallyReloadableGear")]
	internal class Patch_ReloadableUtility_FindPotentiallyReloadableGear
	{
		[HarmonyPrefix]
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
					}
				}
			}

			__result = ar_tmp;
			return false;
		}
	}


	
	
	


	[HarmonyPatch(typeof(ReloadableUtility), "FindSomeReloadableComponent")]
	internal class Patch_ReloadableUtility_FindSomeReloadableComponent
	{
		[HarmonyPrefix]
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
	internal class Patch_ReloadableUtility_WearerOf
	{
		[HarmonyPrefix]
		static bool Prefix(ref Pawn __result, CompReloadable comp)
		{
			if (!yayoCombat.ammo) return true;

			// comp.ParentHolder is Pawn_ApparelTracker parentHolder ? parentHolder.pawn : (Pawn) null;
			__result = comp.ParentHolder is Pawn_EquipmentTracker parentHolder ? parentHolder.pawn : comp.ParentHolder is Pawn_ApparelTracker parentHolder2 ? parentHolder2.pawn : (Pawn)null;
			return false;
		}
	}
}