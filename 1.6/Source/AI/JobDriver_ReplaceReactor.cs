using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    public class JobDriver_ReplaceReactor : JobDriver
    {
        public Reactor Reactor => TargetA.Thing as Reactor;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_General.Wait(120)
                .WithProgressBarToilDelay(TargetIndex.A)
                .WithEffect(() => VREA_DefOf.ButcherMechanoid, TargetIndex.A);
            var toil = ToilMaker.MakeToil();
            toil.initAction = delegate
            {
                var hediff = pawn.health.hediffSet.hediffs.OfType<Hediff_AndroidPowerCore>().FirstOrDefault();
                if (hediff != null)
                {
                    hediff.Energy = Reactor.curEnergy;
                }
                pawn.TrySpawnWaste(pawn.Position, pawn.Map);
                Reactor.Destroy();
            };
            yield return toil;
        }
    }
}
