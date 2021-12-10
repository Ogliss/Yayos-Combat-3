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
			if (yayoCombat.ammo
				&& pawn != null
				&& pawn.Faction?.IsPlayer == true
				&& pawn.Drafted
				&& !pawn.WorkTagIsDisabled(WorkTags.Violent))
			{
				foreach (ThingWithComps thing in pawn.equipment.AllEquipmentListForReading)
				{
					CompReloadable comp = thing.TryGetComp<CompReloadable>();
					if (comp != null)
					{
						bool disabled = false;
						string disableReason = null;

						if (pawn.Downed) // should actually never happen, since pawns can't be drafted when downed
						{
							disabled = true;
							disableReason = "pawnDowned".Translate();
						}
						else if (comp.RemainingCharges >= comp.MaxCharges)
						{
							disabled = true;
							disableReason = "ammoFull".Translate();
						}

						yield return new Command_Action()
						{
							defaultLabel = "reloadWeapon_title".Translate(),
							defaultDesc = "reloadWeapon_desc".Translate(),
							disabled = disabled,
							disabledReason = disableReason,
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
