using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // Losing consciousness doesn't kill an android the way it kills an organic: as long as its brain
    // (and subcore) is intact, running out of power or bleeding out just downs it - it can be recharged,
    // repaired or hauled to a stand. Only when the brain itself is gone does consciousness loss mean a
    // true, permanent death. Other required capacities (blood pumping, breathing...) still kill normally,
    // so destroying a vital organ or the torso does destroy the android (recoverably, via its subcore).
    [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDeadFromRequiredCapacity")]
    public static class Pawn_HealthTracker_ShouldBeDeadFromRequiredCapacity_Patch
    {
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(ref PawnCapacityDef __result, Pawn ___pawn)
        {
            if (__result == PawnCapacityDefOf.Consciousness && ___pawn.IsAndroid()
                && ___pawn.health.hediffSet.GetBrain() != null)
            {
                __result = null;
            }
        }
    }
}
