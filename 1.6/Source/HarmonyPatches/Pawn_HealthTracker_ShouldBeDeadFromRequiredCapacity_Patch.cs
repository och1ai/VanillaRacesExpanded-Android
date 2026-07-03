using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // Losing consciousness doesn't kill an android the way it kills an organic: as long as its brain
    // (and subcore) is intact, running low on power or blood just downs it - it can be recharged,
    // repaired or hauled to a stand. Only when the brain itself is gone does consciousness loss mean a
    // true, permanent death. Other death causes still apply normally: a vital organ or the torso being
    // destroyed, or a blood-typed android whose wounds bleed all the way out (blood loss is lethal at
    // full severity), all destroy the android - recoverably, via its subcore.
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
