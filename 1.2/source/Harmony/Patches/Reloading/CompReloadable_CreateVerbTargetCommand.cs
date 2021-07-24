using RimWorld;
using UnityEngine;
using HarmonyLib;
using Verse;

namespace yayoCombat
{
    // 추가적인 탄약표시 기즈모 설정
    [HarmonyPatch(typeof(CompReloadable), "CreateVerbTargetCommand")]
    internal class CompReloadable_CreateVerbTargetCommand
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
                    commandReloadable.icon = verb.UIIcon != BaseContent.BadTex ? verb.UIIcon : gear.def.uiIcon;
                    commandReloadable.iconAngle = gear.def.uiIconAngle;
                    commandReloadable.iconOffset = gear.def.uiIconOffset;
                    commandReloadable.defaultIconColor = gear.DrawColor;
                }
                if (!__instance.Wearer.IsColonistPlayerControlled)
                    commandReloadable.Disable();
                else if (verb.verbProps.violent && __instance.Wearer.WorkTagIsDisabled(WorkTags.Violent))
                    commandReloadable.Disable((string)("IsIncapableOfViolenceLower".Translate(__instance.Wearer.LabelShort, __instance.Wearer).CapitalizeFirst() + "."));
                else if (!__instance.CanBeUsed)
                    commandReloadable.Disable(__instance.DisabledReason(__instance.MinAmmoNeeded(false), __instance.MaxAmmoNeeded(false)));

                __result = commandReloadable;
                return false;
            }
        }
    }
}