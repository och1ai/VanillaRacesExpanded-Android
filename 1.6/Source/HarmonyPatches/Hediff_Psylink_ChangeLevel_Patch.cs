using HarmonyLib;
using System;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(Hediff_Psylink), "ChangeLevel", new Type[]
    {
        typeof(int)
    })]
    public static class Hediff_Psylink_ChangeLevel_Patch
    {
        // A machine mind can't hold psychic power - but an awakened android has shed its dullness and is as
        // sensitive as any organic, so it may carry a psylink like anyone else.
        [HarmonyPriority(int.MaxValue)]
        private static bool Prefix(Hediff_Psylink __instance)
        {
            if (__instance.pawn.IsAndroid() && !__instance.pawn.IsAwakened())
            {
                return false;
            }
            return true;
        }
    }
}
