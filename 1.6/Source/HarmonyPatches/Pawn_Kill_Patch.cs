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
