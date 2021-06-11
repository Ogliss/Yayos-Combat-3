using DualWield.Storage;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace DualWield
{
    static class Ext_Pawn_EquipmentTracker
    {
        public static bool TryGetOffHandEquipment(this Pawn_EquipmentTracker instance, out ThingWithComps result)
        {
            result = null;
            if (instance.pawn.HasMissingArmOrHand())
            {
                return false;
            }
            ExtendedDataStorage store = Base.Instance.GetExtendedDataStorage();
            foreach (ThingWithComps twc in instance.AllEquipmentListForReading)
            {
                if (store.TryGetExtendedDataFor(twc, out ExtendedThingWithCompsData ext) && ext.isOffHand)
                {
                    result = twc;
                    return true;
                }
            }
            return false;
        }
    }
}
