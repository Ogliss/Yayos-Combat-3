using RimWorld;
using HarmonyLib;
using Verse;
using System.Text;

namespace yayoCombat
{
    // 탄약에 따른 무기가격 조정 : 기존엔 기존 무기 가격에서 탄약가격이 뺄셈이 되었음. 그걸 기존 가격에 탄약가격을 덧셈하도록 변경
    // Adjustment of weapon price according to ammunition: 
    // In the past, the price of ammunition was subtracted from the price of existing weapons. Changed it to add ammo price to the original price
    [HarmonyPatch(typeof(StatPart_ReloadMarketValue), "TransformAndExplain")]
    public class StatPart_ReloadMarketValue_TransformAndExplain
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




}