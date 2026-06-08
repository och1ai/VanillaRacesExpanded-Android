using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // The android stand draws extra power while charging, so its power line reads e.g.
    // "Power needed: 40 W (200 W when active)".
    [HarmonyPatch(typeof(CompPowerTrader), "CompInspectStringExtra")]
    public static class CompPowerTrader_CompInspectStringExtra_Patch
    {
        public static void Postfix(CompPowerTrader __instance, ref string __result)
        {
            if (__instance.parent is Building_AndroidStand && __instance.PowerOn && __result.NullOrEmpty() is false)
            {
                // CompInspectStringExtra leaves a trailing newline; trim it so the note stays on the
                // power line: "Power needed: 40 W (200 W when active)".
                __result = __result.TrimEndNewlines()
                    + " (" + "PowerActiveNeeded".Translate(JobDriver_ChargeAndroid.ChargingPowerConsumption.ToString("F0")) + ")";
            }
        }
    }
}
