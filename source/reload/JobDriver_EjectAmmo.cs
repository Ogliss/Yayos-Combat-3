using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace yayoCombat
{
    public class JobDriver_EjectAmmo : JobDriver
    {
        private ThingWithComps Gear => (ThingWithComps)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.Reserve((LocalTargetInfo)(Thing)Gear, job);
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            JobDriver_EjectAmmo f = this;
            Thing gear = f.Gear;
            Pawn actor = this.GetActor();
            CompReloadable comp = gear != null ? gear.TryGetComp<CompReloadable>() : (CompReloadable)null;
            f.FailOn<JobDriver_EjectAmmo>((Func<bool>)(() => comp == null));
            //f.FailOn<JobDriver_EjectAmmo>((Func<bool>)(() => comp.AmmoDef == null));
            //f.FailOn<JobDriver_EjectAmmo>((Func<bool>)(() => comp.Props.destroyOnEmpty));
            f.FailOn<JobDriver_EjectAmmo>((Func<bool>)(() => comp.RemainingCharges <= 0));
            f.FailOnDestroyedOrNull<JobDriver_EjectAmmo>(TargetIndex.A);
            f.FailOnIncapable<JobDriver_EjectAmmo>(PawnCapacityDefOf.Manipulation);
            Toil getNextIngredient = Toils_General.Label();

            yield return getNextIngredient;
            foreach (Toil toil in f.EjectAsMuchAsPossible(comp))
                yield return toil;
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden<Toil>(TargetIndex.A).FailOnSomeonePhysicallyInteracting<Toil>(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden<Toil>(TargetIndex.A);
            yield return Toils_Jump.JumpIf(getNextIngredient, (Func<bool>)(() => !this.job.GetTargetQueue(TargetIndex.A).NullOrEmpty<LocalTargetInfo>()));
            foreach (Toil toil in f.EjectAsMuchAsPossible(comp))
                yield return toil;
            yield return new Toil()
            {
                initAction = (Action)(() =>
                {
                    Thing carriedThing = this.pawn.carryTracker.CarriedThing;
                    if (carriedThing == null || carriedThing.Destroyed)
                        return;
                    this.pawn.carryTracker.TryDropCarriedThing(this.pawn.Position, ThingPlaceMode.Near, out Thing _);
                }),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private IEnumerable<Toil> EjectAsMuchAsPossible(CompReloadable comp)
        {
            Toil done = Toils_General.Label();
            yield return Toils_Jump.JumpIf(done, (Func<bool>)(() => this.pawn.carryTracker.CarriedThing == null || this.pawn.carryTracker.CarriedThing.stackCount < comp.MinAmmoNeeded(true)));
            yield return Toils_General.Wait(comp.Props.baseReloadTicks).WithProgressBarToilDelay(TargetIndex.A);
            yield return new Toil()
            {
                initAction = (Action)(() => reloadUtility.EjectAmmoAction(GetActor(), comp)),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return done;
        }
    }
}
