using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // Funerals and eulogies are for the dead, and a merely *destroyed* android is not dead - its subcore
    // survives, so it can be resurrected or reprinted. Holding a funeral for one it is about to walk out of
    // the assembler again makes no sense. So android death rites are limited to androids that were truly
    // *killed* (subcore gone, irrecoverable) and that actually followed an ideoligion in the first place.
    internal static class AndroidFuneralUtil
    {
        public static bool ShouldGetDeathRites(Pawn pawn)
        {
            if (pawn == null || !pawn.IsAndroid())
            {
                return true; // Non-androids are untouched.
            }
            // Subcore still present -> "android destroyed", recoverable. No rites.
            // (Pawn_Kill_Patch strips the subcore before the death notice for a real kill, so by the time
            // these triggers run this reads correctly.)
            if (pawn.HasSubcore(out _))
            {
                return false;
            }
            // Truly killed: rites only if it had an ideoligion to be buried under.
            return pawn.Ideo != null;
        }
    }

    [HarmonyPatch(typeof(RitualObligationTrigger_MemberDied),
        nameof(RitualObligationTrigger_MemberDied.Notify_MemberDied))]
    public static class RitualObligationTrigger_MemberDied_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn p)
        {
            return AndroidFuneralUtil.ShouldGetDeathRites(p);
        }
    }

    [HarmonyPatch(typeof(RitualObligationTrigger_MemberCorpseDestroyed),
        nameof(RitualObligationTrigger_MemberCorpseDestroyed.Notify_MemberCorpseDestroyed))]
    public static class RitualObligationTrigger_MemberCorpseDestroyed_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn p)
        {
            return AndroidFuneralUtil.ShouldGetDeathRites(p);
        }
    }
}
