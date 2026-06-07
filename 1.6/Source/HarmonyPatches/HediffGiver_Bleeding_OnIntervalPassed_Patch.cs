using HarmonyLib;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(HediffGiver_Bleeding), "OnIntervalPassed")]
    public static class HediffGiver_Bleeding_OnIntervalPassed_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn pawn, Hediff cause)
        {
            // Bloodless androids never bleed and never accrue any blood/neutro loss.
            if (pawn.HasActiveGene(VREA_DefOf.VREA_Bloodless))
            {
                return false;
            }
            if (pawn.HasActiveGene(VREA_DefOf.VREA_NeutroCirculation))
            {
                HediffSet hediffSet = pawn.health.hediffSet;
                if (hediffSet.BleedRateTotal >= 0)
                {
                    HealthUtility.AdjustSeverity(pawn, VREA_DefOf.VREA_NeutroLoss, hediffSet.BleedRateTotal * 0.001f);
                }
                return false;
            }
            return true;
        }
    }
}
