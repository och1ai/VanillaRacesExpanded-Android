using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using System;

namespace VREAndroids
{
    [HarmonyPatch(typeof(PawnDiedOrDownedThoughtsUtility), "AppendThoughts_ForHumanlike")]
    public static class PawnDiedOrDownedThoughtsUtility_AppendThoughts_ForHumanlike_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn victim, DamageInfo? dinfo)
        {
            
            if (Utils.IdeoTreatsAndroidAsTool(Faction.OfPlayerSilentFail?.ideos?.primaryIdeo, victim))
            {
                return false;
            }
            return true;
        }
    }
}
