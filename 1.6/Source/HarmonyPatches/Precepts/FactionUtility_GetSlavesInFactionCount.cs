using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using System;

namespace VREAndroids
{
    [HarmonyPatch(typeof(FactionUtility), "GetSlavesInFactionCount")]
    public static class FactionUtility_GetSlavesInFactionCount_Patch
    {
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(Faction faction, ref int __result)
        {
            var primaryIdeo = Faction.OfPlayerSilentFail?.ideos?.primaryIdeo;
            if (primaryIdeo == null)
            {
                return;
            }
            int num = __result;
            foreach (Pawn item in PawnsFinder.AllMaps_SpawnedPawnsInFaction(faction))
            {
                if (item.IsSlave && Utils.IdeoTreatsAndroidAsTool(primaryIdeo, item))
                {
                    num--;
                }
            }
            __result = num;
           
        }
    }
}
