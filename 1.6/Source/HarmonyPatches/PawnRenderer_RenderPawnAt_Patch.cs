using HarmonyLib;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnAt")]
    public static class PawnRenderer_RenderPawnAt_Patch
    {
        public static void Prefix(Pawn ___pawn)
        {
            PawnUtility_GetPosture_Patch.isPawnRendering = true;
        }
        public static void Postfix(Pawn ___pawn)
        {
            PawnUtility_GetPosture_Patch.isPawnRendering = false;
        }
    }

    // The body posture (which decides whether a pawn is drawn upright or lying down) is actually
    // resolved during the parallel pre-draw pass, not RenderPawnAt, so the render flag has to be set
    // here too - otherwise a downed/recharging android on a stand is drawn collapsed instead of upright.
    [HarmonyPatch(typeof(PawnRenderer), "ParallelPreRenderPawnAt")]
    public static class PawnRenderer_ParallelPreRenderPawnAt_Patch
    {
        public static void Prefix(Pawn ___pawn)
        {
            PawnUtility_GetPosture_Patch.isPawnRendering = true;
        }
        public static void Postfix(Pawn ___pawn)
        {
            PawnUtility_GetPosture_Patch.isPawnRendering = false;
        }
    }
}
