using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[]
    {
        typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult)
    })]
    public static class Pawn_HealthTracker_AddHediff_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        private static bool Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, ref Hediff hediff, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageWorker.DamageResult result = null)
        {
            if (___pawn.IsAndroid())
            {
                return HandleHediffForAndroid(___pawn, ref hediff);
            }
            return true;
        }

        public static bool HandleHediffForAndroid(Pawn ___pawn, ref Hediff hediff)
        {
            if (ModCompatibility.MSE2Active)
            {
                foreach (var androidImplant in Utils.cachedCounterParts.Values.ToList())
                {
                    var extension = androidImplant.modExtensions?.FirstOrDefault(x => x.GetType() == ModCompatibility.ignoreSubPartsExtensionType);
                    if (extension != null)
                    {
                        androidImplant.modExtensions.Remove(extension);
                    }
                }
            }

            // Golden cube obsession: a non-awakened android's subcore has no desire to worship or hoard.
            // Awakened androids feel the pull like anyone else, so they are NOT immune.
            if ((hediff.def == HediffDefOf.CubeInterest || hediff.def == HediffDefOf.CubeWithdrawal
                || hediff.def == HediffDefOf.CubeComa) && !___pawn.IsAwakened())
            {
                return false;
            }
            // An awakened android is as psychically sensitive as an organic, so it may receive a psylink
            // even though the blanket android blocklist rejects the psychic amplifier.
            bool awakenedPsylink = hediff.def == HediffDefOf.PsychicAmplifier && ___pawn.IsAwakened();
            if (!awakenedPsylink && ___pawn.HasActiveGene(VREA_DefOf.VREA_SyntheticImmunity)
                && Utils.AndroidCanCatch(hediff.def) is false)
            {
                return false;
            }
            if (hediff is Hediff_MissingPart && hediff.Part != null && !___pawn.health.hediffSet.GetNotMissingParts().Contains(hediff.Part))
            {
                return false;
            }
            if (hediff.def == HediffDefOf.Hypothermia)
            {
                var newHediff = HediffMaker.MakeHediff(VREA_DefOf.VREA_Freezing, ___pawn, hediff.part);
                newHediff.Severity = hediff.Severity;
                hediff = newHediff;
            }
            else if (hediff.def == HediffDefOf.Heatstroke)
            {
                var newHediff = HediffMaker.MakeHediff(VREA_DefOf.VREA_Overheating, ___pawn, hediff.part);
                newHediff.Severity = hediff.Severity;
                hediff = newHediff;
            }
            return true;
        }
    }
}
