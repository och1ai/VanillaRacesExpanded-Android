using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // The convert ability can't target an android that has no way to hold beliefs - one that is not
    // awakened and lacks the ideological subroutine. It simply isn't a valid target, so the ability
    // can't be aimed at it (matching the SetIdeo block that would refuse the conversion anyway).
    [HarmonyPatch(typeof(CompAbilityEffect_Convert), "Valid")]
    public static class CompAbilityEffect_Convert_Valid_Patch
    {
        public static void Postfix(LocalTargetInfo target, bool throwMessages, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            Pawn pawn = target.Pawn;
            if (pawn != null && pawn.IsAndroid() && !pawn.CanHoldIdeoligion())
            {
                if (throwMessages)
                {
                    Messages.Message("VREA.CannotConvertAndroid".Translate(pawn.Named("PAWN")), pawn,
                        MessageTypeDefOf.RejectInput, historical: false);
                }
                __result = false;
            }
        }
    }
}
