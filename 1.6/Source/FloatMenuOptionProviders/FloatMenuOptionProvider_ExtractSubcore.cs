using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    // Right-click float-menu option: with a colonist selected, clicking a dead android offers to extract
    // its subcore, assigning the job directly to that colonist (bypassing the designation step). Mirrors
    // Altered Carbon's cortical-stack extraction option on dead pawns.
    public class FloatMenuOptionProvider_ExtractSubcore : FloatMenuOptionProvider
    {
        public override bool Drafted => true;

        public override bool Undrafted => true;

        public override bool Multiselect => false;

        public override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
        {
            Pawn pawn = context.FirstSelectedPawn;
            if (pawn == null || clickedThing is not Corpse corpse || !Utils.HasSubcore(corpse.InnerPawn, out _))
            {
                return null;
            }
            string label = "VREA.DesignatorExtractSubcore".Translate();
            if (!pawn.CanReach(corpse, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);
            }
            if (!pawn.CanReserve(corpse))
            {
                return new FloatMenuOption(label + ": " + "Reserved".Translate().CapitalizeFirst(), null);
            }
            Action action = delegate
            {
                Job job = JobMaker.MakeJob(VREA_DefOf.VREA_ExtractSubcore, corpse);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            };
            return new FloatMenuOption(label, action, MenuOptionPriority.Default, null, corpse);
        }
    }
}
