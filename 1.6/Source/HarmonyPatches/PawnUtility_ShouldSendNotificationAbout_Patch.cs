using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // Mutes player notifications about a pawn while a throwaway designer-preview android is being
    // generated/edited (it isn't a real colonist, so its gene churn shouldn't spam the message log).
    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.ShouldSendNotificationAbout))]
    public static class PawnUtility_ShouldSendNotificationAbout_Patch
    {
        public static bool Prefix(ref bool __result)
        {
            if (Utils.suppressAndroidNotifications)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
