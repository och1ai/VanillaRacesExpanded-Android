using HarmonyLib;
using Verse;

namespace VREAndroids
{
    // Androids are built tough, like mechanoids: they don't bleed out or drop dead once accumulated
    // damage crosses the usual lethal threshold. They get downed and can be repaired instead. A real
    // death only comes from destroying a vital organ, the torso, or the brain (see the required-capacity
    // and corpse handling).
    [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDeadFromLethalDamageThreshold")]
    public static class Pawn_HealthTracker_ShouldBeDeadFromLethalDamageThreshold_Patch
    {
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(ref bool __result, Pawn ___pawn)
        {
            if (__result && ___pawn.IsAndroid())
            {
                __result = false;
            }
        }
    }
}
