using HarmonyLib;
using Verse;

namespace VREAndroids
{
    // Shows the android's power level in the inspect pane like a mechanoid's "Mech energy: 50%
    // (-10% / day)" line.
    [HarmonyPatch(typeof(Pawn), "GetInspectString")]
    public static class Pawn_GetInspectString_Patch
    {
        public static void Postfix(Pawn __instance, ref string __result)
        {
            if (__instance.IsAndroid() is false)
            {
                return;
            }
            var core = __instance.GetPowerCore();
            var need = __instance.needs?.TryGetNeed<Need_ReactorPower>();
            if (core == null || need == null)
            {
                return;
            }
            string line = "VREA.AndroidEnergy".Translate() + ": " + need.CurLevelPercentage.ToStringPercent()
                + " (-" + "PerDay".Translate(core.DrainPerDay.ToStringPercent()) + ")";
            __result = __result.NullOrEmpty() ? line : __result + "\n" + line;
        }
    }
}
