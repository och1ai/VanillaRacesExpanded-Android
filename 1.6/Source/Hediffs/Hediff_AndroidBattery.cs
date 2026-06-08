using RimWorld;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    [HotSwappable]
    public class Hediff_AndroidBattery : Hediff_AndroidPowerCore
    {
        // The default android power core: drains over time and must be topped up at a charging
        // station drawing from the power grid.
        public override bool CanRecharge => true;

        public const int BatteryTickRate = 60;

        // A full battery lasts ~3 days of operation at baseline efficiency (drain factor 1.0); the
        // efficiency multiplier scales this exactly like the reactor does.
        public override float LifespanDays => 3f;

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn.IsHashIntervalTick(BatteryTickRate, delta))
            {
                var baseDrainSpeed = (1f / (GenDate.TicksPerDay * LifespanDays)) * PowerEfficiencyDrainMultiplier;
                baseDrainSpeed *= BatteryTickRate;
                Energy = Mathf.Max(0, Energy - baseDrainSpeed);
            }
        }
    }
}
