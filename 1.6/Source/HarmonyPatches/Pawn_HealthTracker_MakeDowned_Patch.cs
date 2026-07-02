using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // When an android drops because it ran out of power, replace the generic "a capacitor array caused
    // X to fall unconscious" combat-log line with a clearer low-power collapse notice.
    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Pawn_HealthTracker_MakeDowned_Patch
    {
        public static void Prefix(Pawn ___pawn, Hediff hediff)
        {
            Utils.suppressAndroidDownLog = ___pawn != null && ___pawn.IsAndroid() && hediff is Hediff_AndroidBattery;
        }

        public static void Postfix(Pawn ___pawn)
        {
            if (!Utils.suppressAndroidDownLog)
            {
                return;
            }
            // Clear the flag first so the custom entry below is not itself skipped by BattleLog_Add_Patch.
            Utils.suppressAndroidDownLog = false;
            if (___pawn != null && ___pawn.Spawned)
            {
                Find.BattleLog.Add(new BattleLogEntry_StateTransition(___pawn, VREA_DefOf.VREA_Transition_LowPower,
                    null, null, null));
            }
        }
    }

    // Skips the vanilla "downed by hediff" combat-log entry while an android is collapsing from low
    // power - the MakeDowned postfix logs a clearer message in its place.
    [HarmonyPatch(typeof(BattleLog), nameof(BattleLog.Add))]
    public static class BattleLog_Add_Patch
    {
        public static bool Prefix(LogEntry entry)
        {
            return !(Utils.suppressAndroidDownLog && entry is BattleLogEntry_StateTransition);
        }
    }
}
