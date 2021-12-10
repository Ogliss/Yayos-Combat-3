using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace yayoCombat
{
	[StaticConstructorOnStartup]
	internal static class GizmoTexture
	{
		public static readonly Texture2D AmmoReload = ContentFinder<Texture2D>.Get("yy_ammo_reload", true);
	}

	[HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
	internal class Pawn_DraftController_GetGizmos
	{
		[HarmonyPostfix]
		static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
		{
			var pawn = __instance?.pawn;
			if (pawn != null
				&& pawn.Faction?.IsPlayer == true
				&& pawn.Drafted
				&& !pawn.WorkTagIsDisabled(WorkTags.Violent))
			{
				foreach (ThingWithComps thing in pawn.equipment.AllEquipmentListForReading)
				{
					CompReloadable comp = thing.TryGetComp<CompReloadable>();
					if (comp != null)
					{
						yield return new Command_Action()
						{
							defaultLabel = "reload_Weapon".Translate(),
							defaultDesc = "reload_WeaponDesc".Translate(),
							disabled = pawn.Downed,
							disabledReason = "pawn_downed".Translate(),
							icon = GizmoTexture.AmmoReload,

							action = () => reloadUtility.tryAutoReload(comp, true),
						};
					}
					break;
				}
			}

			foreach (var gizmo in __result)
				yield return gizmo;
		}
	}
}
