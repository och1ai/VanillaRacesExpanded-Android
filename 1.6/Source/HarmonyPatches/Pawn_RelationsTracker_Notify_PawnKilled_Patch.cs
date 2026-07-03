using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // A recoverable android destruction (its subcore survives) must not run the relationship "death"
    // handling: no grief pushed onto lovers/friends, and - importantly - its relationships are kept
    // intact so a resurrection or reprint carries them over. Only a real death (the subcore is gone, or
    // AndroidRealDeath is forcing it) processes relationships like any organic's.
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "Notify_PawnKilled")]
    public static class Pawn_RelationsTracker_Notify_PawnKilled_Patch
    {
        public static bool Prefix(Pawn ___pawn)
        {
            if (!Utils.forcingAndroidRealDeath && ___pawn != null && ___pawn.IsAndroid()
                && Utils.HasSubcore(___pawn, out _))
            {
                return false;
            }
            return true;
        }
    }
}
