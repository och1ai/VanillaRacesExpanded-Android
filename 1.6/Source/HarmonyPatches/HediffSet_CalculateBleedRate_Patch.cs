using HarmonyLib;
using Verse;

namespace VREAndroids
{
    // Bloodless androids have no circulating fluid, so the whole-body bleed rate is forced to
    // zero. This is the authoritative figure behind the "bleeding to death" timer and blood loss,
    // so it guarantees they never bleed regardless of individual wounds.
    [HarmonyPatch(typeof(HediffSet), "CalculateBleedRate")]
    public static class HediffSet_CalculateBleedRate_Patch
    {
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(HediffSet __instance, ref float __result)
        {
            if (__result > 0f && __instance.pawn.HasActiveGene(VREA_DefOf.VREA_Bloodless))
            {
                __result = 0f;
            }
        }
    }
}
