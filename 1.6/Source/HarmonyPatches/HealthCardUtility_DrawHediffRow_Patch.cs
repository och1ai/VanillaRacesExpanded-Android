using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(HealthCardUtility), "DrawHediffRow")]
    public static class HealthCardUtility_DrawHediffRow_Patch
    {
        public static Pawn curPawn;
        public static void Prefix(Pawn pawn)
        {
            curPawn = pawn;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions, ILGenerator ilg)
        {
            var codes = codeInstructions.MethodReplacer(AccessTools.PropertyGetter(typeof(BodyPartRecord), nameof(BodyPartRecord.LabelCap)),
                AccessTools.Method(typeof(HealthCardUtility_DrawHediffRow_Patch), nameof(GetAndroidCounterPart))).ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
            }
        }

        public static string GetAndroidCounterPart(BodyPartRecord bodyPartRecord)
        {
            if (curPawn.IsAndroid())
            {
                // Prefer the android organ actually installed on this part so the displayed type
                // matches it (e.g. heatsink on a bloodless android), not the generic neutroamine
                // counterpart. Fall back to the blood-aware counterpart.
                var installed = curPawn.health.hediffSet.hediffs
                    .FirstOrDefault(h => h.Part == bodyPartRecord && h is Hediff_AndroidPart);
                var counterPart = installed?.def ?? Utils.GetAndroidCounterPartFor(bodyPartRecord.def, curPawn);
                if (counterPart != null)
                {
                    return bodyPartRecord.AndroidPartLabel(counterPart).CapitalizeFirst();
                }
            }
            return bodyPartRecord.LabelCap;
        }

        public static void Postfix()
        {
            curPawn = null;
        }
    }
}
