using HarmonyLib;
using RimWorld;
using Verse;
using System;


namespace VREAndroids
{
    [HarmonyPatch(typeof(TaleUtility), "Notify_PawnDied")]
    public static class TaleUtility_Notify_PawnDied_Patch_Patch
    {
        // A recoverable android destruction records no death tales. Most importantly this skips the
        // "killed a colonist" tale, so whoever destroyed the android is not socially judged (the -10
        // "killed colonist" opinion) for taking down what is really just a repairable machine, and no
        // spurious death record lingers. Real deaths (subcore gone, or AndroidRealDeath forcing it) log
        // tales normally. The VRE_AndroidDied precept event below still fires either way.
        public static bool Prefix(Pawn victim)
        {
            if (!Utils.forcingAndroidRealDeath && victim != null && victim.IsAndroid()
                && Utils.HasSubcore(victim, out _))
            {
                return false;
            }
            return true;
        }

        public static void Postfix(Pawn victim, DamageInfo? dinfo)
        {
            if (ModsConfig.IdeologyActive && Utils.IsAndroid(victim)) {

                Pawn pawn = dinfo?.Instigator as Pawn;
                if (pawn != null)
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(VREA_DefOf.VRE_AndroidDied, new SignalArgs(pawn.Named(HistoryEventArgsNames.Doer))), true);
                }
                else
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(VREA_DefOf.VRE_AndroidDied));
                }

            }
            


        }
    }
}
