using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(Need), "ShowOnNeedList", MethodType.Getter)]
    public static class Need_ShowOnNeedList_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Need __instance, Pawn ___pawn, ref bool __result)
        {
            if (__instance is Need_Mood && ___pawn.HasActiveGene(VREA_DefOf.VREA_PsychologyDisabled))
            {
                __result = false;
                return false;
            }
            // A non-awakened android reads like a machine: the needs tab is trimmed to just energy
            // and memory (like a mechanoid). Mood still exists for the awakening mechanic, it is
            // only hidden. Once awakened the android shows the full set of needs.
            if (___pawn.IsAndroid() && ___pawn.IsAwakened() is false)
            {
                if (__instance.def != VREA_DefOf.VREA_ReactorPower && __instance.def != VREA_DefOf.VREA_MemorySpace)
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}
