using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // Bloodless androids have no circulating fluid. CanBleed is the vanilla gate behind the
    // whole-body bleed rate (HediffSet.CalculateBleedRate returns 0 when it is false) and the
    // "bleeding to death" timer, so forcing it off here guarantees they never bleed.
    [HarmonyPatch(typeof(Pawn_HealthTracker), "CanBleed", MethodType.Getter)]
    public static class Pawn_HealthTracker_CanBleed_Patch
    {
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(Pawn_HealthTracker __instance, ref bool __result)
        {
            if (__result && __instance.pawn.HasActiveGene(VREA_DefOf.VREA_Bloodless))
            {
                __result = false;
            }
        }
    }
}
