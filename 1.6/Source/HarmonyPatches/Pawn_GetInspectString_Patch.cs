using HarmonyLib;
using RimWorld;
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
            // Delayed-shutdown reserve: a red countdown sitting directly above the energy readout.
            if (__instance.genes?.GetGene(VREA_DefOf.VREA_DelayedDeactivation) is Gene_DelayedDeactivation reserve
                && reserve.CountingDown)
            {
                string warning = "VREA.DeactivatingIn".Translate(reserve.TicksLeft.ToStringTicksToPeriod())
                    .CapitalizeFirst().Colorize(ColorLibrary.RedReadable);
                __result = __result.NullOrEmpty() ? warning : __result + "\n" + warning;
            }
            var core = __instance.GetPowerCore();
            var need = __instance.needs?.TryGetNeed<Need_ReactorPower>();
            if (core == null || need == null)
            {
                return;
            }
            string line;
            if (core is Hediff_AndroidBattery && core.Severity >= 1f)
            {
                // Out of power and dormant, trickle self-charging - mirrors a mech's "Dormant self-charging".
                line = "VREA.AndroidEnergy".Translate() + ": " + need.CurLevelPercentage.ToStringPercent()
                    + " (+" + "PerDay".Translate(Hediff_AndroidBattery.SlowRechargePerDay.ToStringPercent()) + ")"
                    + "\n" + "VREA.AndroidDormantCharging".Translate();
            }
            else
            {
                line = "VREA.AndroidEnergy".Translate() + ": " + need.CurLevelPercentage.ToStringPercent()
                    + " (-" + "PerDay".Translate(core.DrainPerDay.ToStringPercent()) + ")";
            }
            __result = __result.NullOrEmpty() ? line : __result + "\n" + line;
        }
    }
}
