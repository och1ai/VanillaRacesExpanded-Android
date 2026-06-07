using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    public class JobDriver_RefuelWithNeutroamine : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_General.Wait(120).WithProgressBarToilDelay(TargetIndex.A);
            var toil = ToilMaker.MakeToil();
            toil.initAction = delegate
            {
                var neutroloss = pawn.health.hediffSet.GetFirstHediffOfDef(VREA_DefOf.VREA_NeutroLoss);
                if (neutroloss != null)
                {
                    pawn.carryTracker.CarriedThing.Destroy();
                    neutroloss.Severity -= job.count / Recipe_AdministerNeutroamineForAndroid.NeutroaminePerFullReservoir;
                    if (neutroloss.Severity <= 0.01f)
                    {
                        neutroloss.Severity = 0;
                    }
                }
            };
            yield return toil;
        }
    }
}
