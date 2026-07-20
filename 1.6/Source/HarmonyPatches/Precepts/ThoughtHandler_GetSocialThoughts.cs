using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using System;

namespace VREAndroids
{
    [HarmonyPatch(typeof(ThoughtHandler), "GetSocialThoughts", new Type[]
    {
        typeof(Pawn), typeof(List<ISocialThought>)
    })]
    public static class ThoughtHandler_GetSocialThoughts_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn otherPawn, List<ISocialThought> outThoughts, ThoughtHandler __instance)
        {
            if (!__instance.pawn.IsAndroid() && Utils.IdeoTreatsAndroidAsTool(__instance.pawn.Ideo, otherPawn))
            {
                return false;
            }
            return true;
        }
    }
}
