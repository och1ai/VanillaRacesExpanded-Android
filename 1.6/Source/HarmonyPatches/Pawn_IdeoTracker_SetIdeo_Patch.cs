using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // An android without the ideological subroutine can never be given an ideoligion. This blocks every
    // vector that would assign one - ideoligion conversion, social interactions, generation, the colony
    // ideoligion being applied on load - at the single choke point they all funnel through. Clearing to
    // null (dropping an ideoligion) is always allowed, and androids that DO carry the subroutine, as
    // well as all non-androids, are unaffected.
    [HarmonyPatch(typeof(Pawn_IdeoTracker), "SetIdeo")]
    public static class Pawn_IdeoTracker_SetIdeo_Patch
    {
        public static bool Prefix(Pawn ___pawn, Ideo ideo)
        {
            if (ideo != null && ___pawn != null && !___pawn.CanHoldIdeoligion())
            {
                return false;
            }
            return true;
        }
    }
}
