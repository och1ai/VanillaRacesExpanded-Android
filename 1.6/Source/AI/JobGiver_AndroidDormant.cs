using RimWorld;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    // Shared helper for the two low-power work modes.
    public static class AndroidDormancy
    {
        // Park the android and put it to sleep where it stands. While it is asleep in a low-power work
        // mode a battery trickle-charges and a reactor stops spending (see IsDormantForPower).
        public static Job SleepInPlaceJob(Pawn pawn)
        {
            var job = JobMaker.MakeJob(JobDefOf.LayDown, pawn.Position);
            job.forceSleep = true;
            return job;
        }

        // An android that runs a real sleep cycle beds down like an organic.
        public static Job BedJobIfSleeper(Pawn pawn)
        {
            if (!pawn.HasActiveGene(VREA_DefOf.VREA_SleepNeed))
            {
                return null;
            }
            Building_Bed bed = RestUtility.FindBedFor(pawn);
            if (bed == null)
            {
                return null;
            }
            var job = JobMaker.MakeJob(JobDefOf.LayDown, bed);
            job.forceSleep = true;
            return job;
        }
    }

    // "Dormant self-charge" work mode. Deliberately ignores charging stands: the android powers down where
    // it is and runs on its own trickle. A sleep-cycle android goes to its bed instead.
    public class JobGiver_AndroidDormant : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            return AndroidDormancy.BedJobIfSleeper(pawn) ?? AndroidDormancy.SleepInPlaceJob(pawn);
        }
    }

    // "Recharge" work mode. A battery android walks to an android stand and tops up from the grid; a
    // reactor android has nothing to plug into, so it just sleeps without spending charge.
    public class JobGiver_AndroidRecharge : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            var core = pawn.GetPowerCore();
            if (core != null && core.CanRecharge)
            {
                var stand = JobGiver_ChargeAndroid.FindChargingStandFor(pawn);
                if (stand != null)
                {
                    return JobMaker.MakeJob(VREA_DefOf.VREA_ChargeAndroid, stand);
                }
                // No stand available - fall back to powering down rather than idling at full drain.
            }
            return AndroidDormancy.BedJobIfSleeper(pawn) ?? AndroidDormancy.SleepInPlaceJob(pawn);
        }
    }
}
