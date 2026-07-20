using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
    public static class Pawn_HealthTracker_ShouldBeDowned_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(ref bool __result, Pawn_HealthTracker __instance)
        {
            if (__instance.pawn.IsAndroid())
            {
                if (__instance.pawn.genes.GetGene(VREA_DefOf.VREA_SyntheticBody) != null
                    && __instance.hediffSet.hediffs.OfType<Hediff_AndroidPowerCore>().Any() is false)
                {
                    // Out of power: no reserve grace, it drops immediately.
                    __result = true;
                }
                else
                {
                    bool wouldBeDowned = __instance.capacities.CanBeAwake is false
                        || __instance.capacities.CapableOf(PawnCapacityDefOf.Moving) is false;
                    __result = ApplyDeactivationReserve(__instance.pawn, wouldBeDowned);
                }
                return false;
            }
            return true;
        }

        // Emergency-power-reserve subroutine: if the android would be downed by critical body damage but
        // still has an intact head, it keeps functioning for two hours before deactivating. A destroyed
        // head (or no such subroutine) downs it right away, as normal.
        private static bool ApplyDeactivationReserve(Pawn pawn, bool wouldBeDowned)
        {
            if (!(pawn.genes?.GetGene(VREA_DefOf.VREA_DelayedDeactivation) is Gene_DelayedDeactivation reserve))
            {
                return wouldBeDowned;
            }
            if (!wouldBeDowned)
            {
                reserve.ResetCountdown();
                return false;
            }
            if (pawn.health.hediffSet.GetBrain() == null)
            {
                reserve.ResetCountdown();
                return true;
            }
            return reserve.RunReserveAndShouldDeactivate();
        }
    }

    // Downing is only half the story: a destroyed torso kills outright through ShouldBeDead (the core-part
    // efficiency check), which never goes near ShouldBeDowned. The delayed-shutdown subroutine is supposed
    // to cover a critical failure in *any* region but the head, so the same two-hour reserve has to gate
    // death as well - otherwise the android dies instantly the moment its torso is destroyed.
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.ShouldBeDead))]
    public static class Pawn_HealthTracker_ShouldBeDead_Patch
    {
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(Pawn_HealthTracker __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            Pawn pawn = __instance.pawn;
            if (pawn == null || !pawn.IsAndroid()
                || !(pawn.genes?.GetGene(VREA_DefOf.VREA_DelayedDeactivation) is Gene_DelayedDeactivation reserve))
            {
                return;
            }
            // Losing the head/brain bypasses the reserve entirely - that is an instant shutdown.
            if (pawn.health.hediffSet.GetBrain() == null)
            {
                reserve.ResetCountdown();
                return;
            }
            // Anywhere else: run on reserve power, and only let the death through once it expires.
            if (!reserve.RunReserveAndShouldDeactivate())
            {
                __result = false;
            }
        }
    }
}
