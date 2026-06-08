using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VREAndroids
{
    // An android with a rechargeable battery tops it up at a powered android stand, drawing extra
    // power from the grid while charging. Modeled on the vanilla mech charger and AC2's belt charge.
    public class JobDriver_ChargeAndroid : JobDriver
    {
        public Building_AndroidStand Stand => job.targetA.Thing as Building_AndroidStand;

        // Total grid draw of the stand while actively charging (vs. its idle standby draw).
        public const float ChargingPowerConsumption = 200f;

        // A fully drained battery recharges in ~3 in-game hours; a low android only needs a short
        // pit stop before returning to work.
        public const int FullChargeTicks = 7500;

        private Mote moteCharging;
        private Mote moteCablePulse;
        private Sustainer sustainerCharging;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOn(() => Stand == null || Stand.compPower == null || Stand.compPower.PowerOn is false);
            this.FailOn(() => Stand != null && Stand.IsFullOfWaste);
            this.FailOn(() => pawn.GetPowerCore()?.CanRecharge != true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            Toil toil = ToilMaker.MakeToil();
            toil.initAction = delegate
            {
                toil.actor.pather.StopDead();
                SoundDefOf.MechChargerStart.PlayOneShot(Stand);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.handlingFacing = true;
            toil.tickIntervalAction = delegate(int delta)
            {
                toil.actor.Rotation = Rot4.South;
                var core = pawn.GetPowerCore();
                if (core == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                float gained = (1f / FullChargeTicks) * delta;
                core.Energy = Mathf.Min(1f, core.Energy + gained);
                Stand.AddChargingWaste(gained, pawn);
                if (core.Energy >= 1f)
                {
                    EndJobWith(JobCondition.Succeeded);
                }
            };
            toil.tickAction = delegate
            {
                if (moteCharging == null || moteCharging.Destroyed)
                {
                    moteCharging = MoteMaker.MakeAttachedOverlay(pawn, ThingDefOf.Mote_MechCharging, Vector3.zero);
                }
                moteCharging?.Maintain();
                if (moteCablePulse == null || moteCablePulse.Destroyed)
                {
                    moteCablePulse = MoteMaker.MakeInteractionOverlay(ThingDefOf.Mote_ChargingCablesPulse, Stand,
                        new TargetInfo(pawn.Position, base.Map));
                }
                moteCablePulse?.Maintain();
                if (sustainerCharging == null || sustainerCharging.Ended)
                {
                    sustainerCharging = SoundDefOf.MechChargerCharging.TrySpawnSustainer(SoundInfo.InMap(Stand, MaintenanceType.PerTick));
                }
                sustainerCharging.Maintain();
                if (Stand?.compPower != null)
                {
                    Stand.compPower.PowerOutput = -ChargingPowerConsumption;
                }
            };
            AddFinishAction(delegate
            {
                if (Stand?.compPower != null)
                {
                    Stand.compPower.PowerOutput = -Stand.compPower.Props.basePowerConsumption;
                }
                if (sustainerCharging != null)
                {
                    sustainerCharging.End();
                    sustainerCharging = null;
                }
            });
            yield return toil;
        }
    }
}
