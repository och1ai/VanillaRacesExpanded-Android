using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    // Sends an android with a low, rechargeable battery to a powered android stand to charge.
    // Androids running on a reactor power themselves and never use this.
    public class JobGiver_ChargeAndroid : ThinkNode_JobGiver
    {
        // Matches the vanilla mechanoid default: they go recharge once their energy drops below 30%,
        // and (via the charge job) top all the way back up to 100%.
        public const float ChargeThreshold = 0.3f;

        public override float GetPriority(Pawn pawn)
        {
            var core = pawn.GetPowerCore();
            if (core == null || core.CanRecharge is false)
            {
                return 0f;
            }
            var need = pawn.needs.TryGetNeed<Need_ReactorPower>();
            if (need == null || need.CurLevelPercentage > ChargeThreshold)
            {
                return 0f;
            }
            // High priority like freeing memory space: a low battery means imminent shutdown, so the
            // android breaks off work to charge (it still won't override player draft orders).
            return 950f;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            var core = pawn.GetPowerCore();
            if (core == null || core.CanRecharge is false)
            {
                return null;
            }
            Building_AndroidStand stand = FindChargingStandFor(pawn);
            if (stand != null)
            {
                return JobMaker.MakeJob(VREA_DefOf.VREA_ChargeAndroid, stand);
            }
            return null;
        }

        // Like JobGiver_FreeMemorySpace.FindStandFor, but only powered stands can charge a battery
        // from the grid.
        public static Building_AndroidStand FindChargingStandFor(Pawn pawn)
        {
            foreach (var stand in Building_AndroidStand.stands)
            {
                if (IsPoweredAndUsable(stand, pawn) && stand.CompAssignableToPawn.AssignedPawns.Contains(pawn))
                {
                    return stand;
                }
            }
            foreach (var stand in Building_AndroidStand.stands)
            {
                if (IsPoweredAndUsable(stand, pawn) && stand.CompAssignableToPawn.AssignedPawns.Any() is false)
                {
                    stand.CompAssignableToPawn.TryAssignPawn(pawn);
                    return stand;
                }
            }
            return null;
        }

        private static bool IsPoweredAndUsable(Building_AndroidStand stand, Pawn pawn)
        {
            return stand.compPower != null && stand.compPower.PowerOn
                && stand.IsFullOfWaste is false
                && stand.CannotUseNowReason(pawn) is null
                && pawn.CanReserveAndReach(stand, PathEndMode.OnCell, Danger.Deadly);
        }
    }
}
