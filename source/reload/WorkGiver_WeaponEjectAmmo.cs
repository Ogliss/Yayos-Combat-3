using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace yayoCombat
{
    /*
    public abstract class WorkGiver_WeaponEjectAmmo : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            return reloadUtility.getEjectableWeapon(c, pawn.Map) != null;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false) => pawn.GetLord() != null || base.ShouldSkip(pawn, forced);

        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            return JobMaker.MakeJob(yayoCombat_Defs.JobDefOf.EjectAmmo, reloadUtility.getEjectableWeapon(c, pawn.Map));
            
        }


        //

        public override bool AllowUnreachable => true;

        protected virtual bool ExtraRequirements(IPlantToGrowSettable settable, Pawn pawn) => true;

        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            Danger maxDanger = pawn.NormalMaxDanger();
            int i;
            List<Zone> zonesList = pawn.Map.zoneManager.AllZones;
            for (i = 0; i < zonesList.Count; ++i)
            {
                if (zonesList[i] is Zone_Stockpile z)
                {
                    if (z.cells.Count == 0)
                        Log.ErrorOnce("Grow zone has 0 cells: " + (object)z, -563487);
                    else if (!z.ContainsStaticFire && pawn.CanReach((LocalTargetInfo)z.Cells[0], PathEndMode.OnCell, maxDanger))
                    {
                        for (int j = 0; j < z.cells.Count; ++j)
                            yield return z.cells[j];
                        z = (Zone_Stockpile)null;
                    }
                }
            }
        }

    }
    */

}
