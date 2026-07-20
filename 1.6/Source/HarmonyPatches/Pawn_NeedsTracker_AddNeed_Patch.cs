using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "AddNeed")]
    public static class Pawn_NeedsTracker_AddNeed_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn ___pawn, NeedDef nd)
        {
            if (___pawn.IsAndroid())
            {
                // Let the sleep-cycle subroutine add the Rest need even though Rest is normally excluded.
                if (nd == NeedDefOf.Rest && ___pawn.HasActiveGene(VREA_DefOf.VREA_SleepNeed))
                {
                    return true;
                }
                if (VREA_DefOf.VREA_AndroidSettings.excludedNeedsForAndroids.Contains(nd.defName))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
