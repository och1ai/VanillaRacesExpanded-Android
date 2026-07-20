using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // If an android dies with its subcore already gone - a head/brain kill that destroyed the core -
    // that is a true, permanent death, so route it through AndroidRealDeath (the "android killed"
    // notice and the colony's grief). Deaths that leave the subcore intact are recoverable and stay
    // silent; a deliberate surgical extraction (extractingSubcore) drops the core into an item, so the
    // person is not dead and this must not fire; AndroidRealDeath's own kill path is skipped too.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Pawn_Kill_Patch
    {
        // A brain hit alone leaves a recoverable "android destroyed". But losing the whole HEAD
        // (decapitation) or the whole BODY (torso) takes the shielded subcore with it - strip the subcore
        // before the death notice runs so it reads as a true, irrecoverable "android killed".
        public static void Prefix(Pawn __instance)
        {
            if (Utils.extractingSubcore || Utils.forcingAndroidRealDeath)
            {
                return;
            }
            if (__instance != null && __instance.IsAndroid() && __instance.HasSubcore(out var subcore)
                && HeadOrBodyDestroyed(__instance))
            {
                __instance.health.RemoveHediff(subcore);
            }
        }

        private static bool HeadOrBodyDestroyed(Pawn pawn)
        {
            bool headPresent = false;
            bool torsoPresent = false;
            foreach (var part in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (part.def.defName == "Head") headPresent = true;
                else if (part.def == BodyPartDefOf.Torso) torsoPresent = true;
            }
            return !headPresent || !torsoPresent;
        }

        public static void Postfix(Pawn __instance)
        {
            if (Utils.extractingSubcore || Utils.forcingAndroidRealDeath)
            {
                return;
            }
            if (__instance != null && __instance.IsAndroid() && !Utils.HasSubcore(__instance, out _))
            {
                // NotifyPlayerOfKilled already posted the "android killed" letter for this brain kill,
                // so only add the grief here - not a second letter.
                Utils.AndroidRealDeath(__instance, sendLetter: false);
            }
        }
    }
}
