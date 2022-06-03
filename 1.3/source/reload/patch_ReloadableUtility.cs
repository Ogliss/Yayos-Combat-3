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
using System.Reflection.Emit;

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
		static void Prefix(Pawn_EquipmentTracker __instance, ThingOwner<ThingWithComps> ___equipment, IntVec3 pos, bool forbid = true)
		{
			if (yayoCombat.ammo && __instance.pawn.Faction?.IsPlayer != true)
			{
				foreach (var thing in ___equipment)
					reloadUtility.TryThingEjectAmmoDirect(thing, true, __instance.pawn);
			}
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
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			CodeInstruction returnInstruction = null;
			// Original code
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ret)
					returnInstruction = instruction;
				else
					yield return instruction;
			}

			// New code
			yield return new CodeInstruction(OpCodes.Ldloc_0);
			yield return new CodeInstruction(OpCodes.Ldloc_1);
			yield return new CodeInstruction(OpCodes.Ldloc_2);
			yield return new CodeInstruction(OpCodes.Ldarg_2);
			yield return new CodeInstruction(OpCodes.Call, typeof(Patch_ThingSetMaker_TraderStock_Generate).GetMethod(nameof(Patch_ThingSetMaker_TraderStock_Generate.AddAmmo), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
			yield return returnInstruction;
		}

		static void AddAmmo(TraderKindDef traderKindDef, Faction makingFaction, int forTile, List<Thing> outThings)
		{
			var isWeaponsTrader = traderKindDef.stockGenerators.FirstOrDefault(s => s is StockGenerator_WeaponsRanged) != null;
			var isExoticTrader = !isWeaponsTrader && traderKindDef.stockGenerators.FirstOrDefault(s => s is StockGenerator_Tag tag && tag.tradeTag == "ExoticMisc") != null;
			if (traderKindDef.defName.ToLower().Contains("bulkgoods")
				|| isWeaponsTrader
				|| isExoticTrader
				|| Rand.Value <= 0.33f)
			{
				TechLevel tech = TechLevel.Spacer;
				if (makingFaction?.def != null)
					tech = makingFaction.def.techLevel;

				if (tech >= TechLevel.Neolithic)
				{
					float amount = 400f;
					float min = 0.25f;
					float max = 1.50f;

					Thing thing;

					// 원시 이상
					var rnd = Rand.Value;
					if (!isExoticTrader
						&& (tech >= TechLevel.Neolithic && tech <= TechLevel.Medieval	// 100% for Neolithic & Medieval
						||  tech <= TechLevel.Industrial && rnd <= 0.2f					//  20% for Industrial
						||  rnd <= 0.1f))                                               //  10% for Post-Industrial
					{
						var primitiveAmount = amount;
						if (tech > TechLevel.Medieval)
							primitiveAmount *= 0.5f;

						var count = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * primitiveAmount);
						if (count > 20)
						{
							thing = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_primitive"));
							thing.stackCount = count;
							thing.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
							outThings.Add(thing);
						}

						count = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * primitiveAmount * 0.40f);
						if (count > 20)
						{
							thing = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_primitive_fire"));
							thing.stackCount = count;
							thing.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
							outThings.Add(thing);
						}

						count = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * primitiveAmount * 0.25f);
						if (count > 20)
						{
							thing = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_primitive_emp"));
							thing.stackCount = count;
							thing.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
							outThings.Add(thing);
						}
					}


					// 산업 이상
					rnd = Rand.Value;
					if (!isExoticTrader
						&& (tech == TechLevel.Industrial								// 100% for Industrial
						||  tech > TechLevel.Industrial && rnd <= 0.8f					//  80% for Post-Industrial
						||  rnd <= 0.2f))                                               //  20% for Pre-Industrial
					{
						var industrialAmount = amount;
						if (tech < TechLevel.Industrial)
							industrialAmount /= (int)TechLevel.Industrial - (int)tech;

						var count = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * industrialAmount);
						if (count > 20)
						{
							thing = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_industrial"));
							thing.stackCount = count;
							thing.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
							outThings.Add(thing);
						}

						count = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * industrialAmount * 0.40f);
						if (count > 20)
						{
							thing = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_industrial_fire"));
							thing.stackCount = count;
							thing.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
							outThings.Add(thing);
						}

						count = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * industrialAmount * 0.25f);
						if (count > 20)
						{
							thing = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_industrial_emp"));
							thing.stackCount = count;
							thing.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
							outThings.Add(thing);
						}
					}


					// 우주 이상
					rnd = Rand.Value;
					if (isExoticTrader
						|| tech >= TechLevel.Spacer										// 100% for Spacer & Post-Spacer
						|| tech == TechLevel.Industrial && rnd <= 0.5f					//  50% for Industrial
						|| rnd <= 0.1f)													//  10% for Pre-Industrial
					{
						var spacerAmount = amount;
						if (tech < TechLevel.Spacer)
							spacerAmount /= (int)TechLevel.Spacer - (int)tech;

						var count = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * spacerAmount);
						if (count > 20)
						{
							thing = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_spacer"));
							thing.stackCount = count;
							thing.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
							outThings.Add(thing);
						}

						count = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * spacerAmount * 0.40f);
						if (count > 20)
						{
							thing = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_spacer_fire"));
							thing.stackCount = count;
							thing.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
							outThings.Add(thing);
						}

						count = Mathf.RoundToInt(Rand.Range(min, max) * yayoCombat.ammoGen * spacerAmount * 0.25f);
						if (count > 20)
						{
							thing = ThingMaker.MakeThing(ThingDef.Named("yy_ammo_spacer_emp"));
							thing.stackCount = count;
							thing.PostGeneratedForTrader(traderKindDef, forTile, makingFaction);
							outThings.Add(thing);
						}
					}
				}
			}
		}
	}

	

	// 상인이 탄약 거래 가능하도록 허용
	[HarmonyPatch(typeof(TraderKindDef), "WillTrade")]
	internal class Patch_TraderKindDef_WillTrade
	{
		[HarmonyPostfix]
		static bool Postfix(bool __result, TraderKindDef __instance, ThingDef td)
		{
			if (yayoCombat.ammo && __instance.defName != "Empire_Caravan_TributeCollector")
			{
				if (td.tradeTags?.Contains("Ammo") == true)
					__result = true;
			}
			return __result;
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
		[HarmonyPostfix]
		static void Postfix(CompReloadable __instance)
		{
			if (!yayoCombat.ammo || __instance.Wearer == null) 
				return;

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
		}
	}

	// 사냥을 위한 무기 보유 여부를 탄약과 함께 계산
	[HarmonyPatch(typeof(WorkGiver_HunterHunt), "HasHuntingWeapon")]
	internal class Patch_WorkGiver_HasHuntingWeapon
	{
		[HarmonyPostfix]
		static bool Postfix(bool __result, Pawn p)
		{
			if (yayoCombat.ammo && __result)
			{
				var comp = p.equipment.Primary.GetComp<CompReloadable>();
				if (comp != null)
					__result = comp.CanBeUsed;
			}
			return __result;
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
		[HarmonyPostfix]
		static IEnumerable<Pair<CompReloadable, Thing>> Postfix(IEnumerable<Pair<CompReloadable, Thing>> __result, Pawn pawn, List<Thing> potentialAmmo)
		{
			foreach(var pair in __result)
				yield return pair;

			if (!yayoCombat.ammo)
				yield break;

			if (pawn.equipment != null)
			{
				foreach (var thing in pawn.equipment.AllEquipmentListForReading)
				{
					var comp = thing.TryGetComp<CompReloadable>();
					if (comp?.AmmoDef != null)
					{
						foreach (var ammoThing in potentialAmmo)
						{
							if (ammoThing?.def == comp.Props.ammoDef)
								yield return new Pair<CompReloadable, Thing>(comp, ammoThing);
						}
					}
				}
			}
		}
	}


	[HarmonyPatch(typeof(ReloadableUtility), "FindSomeReloadableComponent")]
	internal class Patch_ReloadableUtility_FindSomeReloadableComponent
	{
		[HarmonyPostfix]
		static CompReloadable Postfix(CompReloadable __result, Pawn pawn, bool allowForcedReload)
		{
			if (yayoCombat.ammo && __result == null)
			{
				foreach (var thing in pawn.equipment.AllEquipmentListForReading)
				{
					var compReloadable = thing.TryGetComp<CompReloadable>();
					if (compReloadable?.NeedsReload(allowForcedReload) == true)
					{
						__result = compReloadable;
						break;
					}
				}
			}
			return __result;
		}
	}

	[HarmonyPatch(typeof(ReloadableUtility), "WearerOf")]
	internal class Patch_ReloadableUtility_WearerOf
	{
		[HarmonyPostfix]
		static Pawn Postfix(Pawn __result, CompReloadable comp)
		{
			if (yayoCombat.ammo && __result == null)
			{
				if (comp.ParentHolder is Pawn_EquipmentTracker equipmentTracker)
					__result = equipmentTracker.pawn;
				// could also check "is Pawn_InventoryTracker inventoryTracker", might cause problems though?
			}
			return __result;
		}
	}
}