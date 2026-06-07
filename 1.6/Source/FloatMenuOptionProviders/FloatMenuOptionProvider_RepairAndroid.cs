using RimWorld;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    // Lets the player order a crafter (or the android itself) to repair an android on demand,
    // wherever it is - repair is no longer limited to androids resting in a stand.
    public class FloatMenuOptionProvider_RepairAndroid : FloatMenuOptionProvider
    {
        public override bool Drafted => true;

        public override bool Undrafted => true;

        public override bool Multiselect => false;

        public override bool CanSelfTarget => true;

        public override bool RequiresManipulation => true;

        public override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
        {
            if (clickedThing is not Pawn android || android.IsAndroid() is false)
            {
                return null;
            }
            if (JobDriver_RepairAndroid.CanRepairAndroid(android) is false)
            {
                return null;
            }
            Pawn doer = context.FirstSelectedPawn;
            if (doer == null || doer.WorkTypeIsDisabled(WorkTypeDefOf.Crafting))
            {
                return null;
            }
            if (doer != android && doer.CanReserveAndReach(android, PathEndMode.InteractionCell, Danger.Deadly) is false)
            {
                return null;
            }
            return new FloatMenuOption("VREA.RepairAndroid".Translate(android.Named("PAWN")), delegate
            {
                Job job = JobMaker.MakeJob(VREA_DefOf.VREA_RepairAndroid, android);
                doer.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
    }
}
