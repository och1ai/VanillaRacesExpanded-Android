using HarmonyLib;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(Hediff_Injury), "BleedRate", MethodType.Getter)]
    public static class Hediff_Injury_BleedRate_Patch
    {
        // How much the coagulation subroutine cuts a wound's bleed rate.
        public const float CoagulationBleedFactor = 0.35f;

        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(ref float __result, Hediff_Injury __instance)
        {
            // Bloodless androids have no circulating fluid, so their wounds never bleed.
            // Normal-blood and neutroamine-blood androids bleed via the vanilla logic (red
            // blood, or blue neutroamine filth supplied by the gene's custom-blood extension).
            if (__instance.pawn.HasActiveGene(VREA_DefOf.VREA_Bloodless))
            {
                __result = 0f;
                return false;
            }
            return true;
        }

        // The coagulation subroutine seals fluid lines fast, so wounds bleed far slower. Runs after
        // vanilla has computed the base bleed rate and simply scales it down.
        public static void Postfix(ref float __result, Hediff_Injury __instance)
        {
            if (__result > 0f && __instance.pawn.HasActiveGene(VREA_DefOf.VREA_Coagulation))
            {
                __result *= CoagulationBleedFactor;
            }
        }
    }
}
