using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace yayoCombat
{
    class reloadUtility
    {
        internal static void EjectAmmo(Pawn pawn, ThingWithComps t)
        {
            if (!pawn.IsColonist && pawn.equipment.Primary == null)
                return;
            Job job = new Job(yayoCombat_Defs.JobDefOf.EjectAmmo, (LocalTargetInfo)(Thing)t);
            job.count = 1;
            pawn.jobs.TryTakeOrderedJob(job);
        }
        internal static void EjectAmmoAction(Pawn p, CompReloadable cp)
        {
            int n = 0;
            while (cp.RemainingCharges > 0)
            {
                cp.UsedOnce();
                n++;
            }
            Thing t = null;
            
            while(n > 0)
            {
                t = ThingMaker.MakeThing(cp.AmmoDef);
                t.stackCount = Mathf.Min(t.def.stackLimit, n) * cp.Props.ammoCountPerCharge;
                n -= t.stackCount;
                GenPlace.TryPlaceThing(t, p.Position, p.Map, ThingPlaceMode.Near);
            }
            cp.Props.soundReload.PlayOneShot((SoundInfo)new TargetInfo(p.Position, p.Map));

        }

        internal static void TryThingEjectAmmoDirect(Thing w, bool forbidden = false, Pawn pawn = null)
        {
            if (!w.def.IsWeapon) 
                return;
            
            if (w.TryGetComp<CompReloadable>() == null) 
                return;

            CompReloadable cp = w.TryGetComp<CompReloadable>();
            int n = 0;

            while (cp.RemainingCharges > 0)
            {
                cp.UsedOnce();
                n++;
            }

            while (n > 0)
            {
                Thing t = ThingMaker.MakeThing(cp.AmmoDef);
                t.stackCount = Mathf.Min(t.def.stackLimit, n) * cp.Props.ammoCountPerCharge;
                t.SetForbidden(forbidden);
                n -= t.stackCount;

                if (pawn != null)
                    GenPlace.TryPlaceThing(t, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                else
                    GenPlace.TryPlaceThing(t, w.Position, w.Map, ThingPlaceMode.Near);
            }
        }

        static public Thing getEjectableWeapon(IntVec3 c, Map m)
        {
            foreach (Thing t in c.GetThingList(m))
            {
                if (t.TryGetComp<CompReloadable>()?.RemainingCharges > 0)
                {
                    return t;
                }
            }
            return null;
        }

        static public void tryAutoReload(CompReloadable cp)
        {
            if (cp.RemainingCharges <= 0)
            {
                Pawn p = cp.Wearer;
                List<Thing> ar_inven = p.inventory.innerContainer.ToList<Thing>();
                List<Thing> ar_ammo = new List<Thing>();
                for (int i = 0; i < ar_inven.Count; i++)
                {
                    if (ar_inven[i].def == cp.AmmoDef)
                    {
                        ar_ammo.Add(ar_inven[i]);
                    }
                }
                if (ar_ammo.Count == 0 && !p.RaceProps.Humanlike && yayoCombat.refillMechAmmo)
                {
                    Thing ammo = ThingMaker.MakeThing(cp.AmmoDef);
                    ammo.stackCount = cp.MaxAmmoNeeded(true);
                    p.inventory.innerContainer.TryAdd(ammo);
                    ar_ammo.Add(ammo);
                }
                if (ar_ammo.Count > 0)
                {
                    List<Thing> ar_dropThing = new List<Thing>();
                    int need = cp.MaxAmmoNeeded(true);
                    for (int i = ar_ammo.Count - 1; i >= 0; i--)
                    {
                        // 떨어뜨리기
                        // drop
                        int count = Mathf.Min(need, ar_ammo[i].stackCount);
                        Thing dropThing = null;
                        if (!p.inventory.innerContainer.TryDrop(ar_ammo[i], p.Position, p.Map, ThingPlaceMode.Direct, count, out dropThing))
                        {
                            p.inventory.innerContainer.TryDrop(ar_ammo[i], p.Position, p.Map, ThingPlaceMode.Near, count, out dropThing);
                        }
                        if (count > 0)
                        {
                            need -= count;
                            ar_dropThing.Add(dropThing);
                        }
                        if (need <= 0)
                        {
                            break;
                        }
                    }

                    if (ar_dropThing.Count > 0)
                    {
                        // 줍기
                        // pick up
                        Job j = JobMaker.MakeJob(JobDefOf.Reload, (LocalTargetInfo)(Thing)cp.parent);
                        j.targetQueueB = ar_dropThing.Select<Thing, LocalTargetInfo>((Func<Thing, LocalTargetInfo>)(t => new LocalTargetInfo(t))).ToList<LocalTargetInfo>();
                        j.count = ar_dropThing.Sum<Thing>((Func<Thing, int>)(t => t.stackCount));
                        j.count = Math.Min(j.count, cp.MaxAmmoNeeded(true));
                        p.jobs.TryTakeOrderedJob(j);
                        p.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, (LocalTargetInfo)p.Position));
                    }
                }
                else if (yayoCombat.supplyAmmoDist >= 0)
                {
                    // 인벤에 탄약이 없을경우 바닥의 탄약아이템 줍기
                    // If there is no ammo in your inventory, pick up the ammo item on the floor.
                    IntRange desiredQuantity = new IntRange(cp.MinAmmoNeeded(false), cp.MaxAmmoNeeded(false));
                    List<Thing> enoughAmmo = RefuelWorkGiverUtility.FindEnoughReservableThings(p, p.Position, desiredQuantity, (Predicate<Thing>)(t => t.def == cp.AmmoDef && IntVec3Utility.DistanceTo(p.Position, t.Position) <= yayoCombat.supplyAmmoDist));

                    if (enoughAmmo != null && p.jobs.jobQueue.ToList<QueuedJob>().Count <= 0)
                    {
                        p.jobs.TryTakeOrderedJob(JobGiver_Reload.MakeReloadJob(cp, enoughAmmo));
                        p.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, (LocalTargetInfo)p.Position));
                    }
                }
            }
        }
    }
}
