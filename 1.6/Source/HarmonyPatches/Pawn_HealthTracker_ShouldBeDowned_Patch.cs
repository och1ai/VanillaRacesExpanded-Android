using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
    public static class Pawn_HealthTracker_ShouldBeDowned_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(ref bool __result, Pawn_HealthTracker __instance)
        {
            if (__instance.pawn.IsAndroid())
            {
                if (__instance.pawn.genes.GetGene(VREA_DefOf.VREA_SyntheticBody) != null
                    && __instance.hediffSet.hediffs.OfType<Hediff_AndroidPowerCore>().Any() is false)
                {
                    __result = true;
                }
                else
                {
                    __result = __instance.capacities.CanBeAwake is false 
                        || __instance.capacities.CapableOf(PawnCapacityDefOf.Moving) is false;
                }
                return false;
            }
            return true;
        } 
    }
}
