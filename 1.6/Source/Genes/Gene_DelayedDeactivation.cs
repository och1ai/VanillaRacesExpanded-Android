using RimWorld;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    // "Emergency power reserve" subroutine. When a critical failure that would normally drop the android
    // happens anywhere but the head, a hardened backup capacitor keeps it running for two hours before it
    // finally deactivates (gets downed). A destroyed head bypasses the reserve entirely. The actual
    // "should the android be downed" decision lives in Pawn_HealthTracker_ShouldBeDowned_Patch; this gene
    // just holds the countdown so it persists across saves and can be shown on the health card.
    public class Gene_DelayedDeactivation : Gene
    {
        // Absolute game tick at which the reserve runs out and the android deactivates. -1 = not counting.
        public int deactivateAtTick = -1;

        // Two hours of reserve power.
        public const int GraceTicks = 2 * GenDate.TicksPerHour;

        public bool CountingDown => deactivateAtTick >= 0;

        // Never report a negative remainder once the reserve has run out.
        public int TicksLeft => deactivateAtTick < 0
            ? 0
            : Mathf.Max(0, deactivateAtTick - Find.TickManager.TicksGame);

        public bool Expired => deactivateAtTick >= 0 && Find.TickManager.TicksGame >= deactivateAtTick;

        // ShouldBeDead / ShouldBeDowned are only consulted when something changes the pawn's health, so an
        // android whose reserve simply times out with no further damage would never be re-evaluated - the
        // countdown would just run on into negative numbers and it would keep working. Once the reserve is
        // spent, poke the health tracker so the shutdown actually lands.
        public override void Tick()
        {
            base.Tick();
            // Once it is down or dead the shutdown has landed, so stop poking the health tracker.
            if (Expired && pawn != null && !pawn.Dead && !pawn.Downed)
            {
                pawn.health.CheckForStateChange(null, null);
            }
        }

        // Called from ShouldBeDowned when the android would be downed by critical body damage (brain still
        // intact). Starts the countdown the first time and reports whether the reserve has run out.
        // Returns true once the android should finally deactivate.
        public bool RunReserveAndShouldDeactivate()
        {
            int now = Find.TickManager.TicksGame;
            if (deactivateAtTick < 0)
            {
                deactivateAtTick = now + GraceTicks;
                return false;
            }
            return now >= deactivateAtTick;
        }

        public void ResetCountdown()
        {
            deactivateAtTick = -1;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref deactivateAtTick, "deactivateAtTick", -1);
        }
    }
}
