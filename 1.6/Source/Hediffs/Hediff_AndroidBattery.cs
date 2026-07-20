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

        // While an android is out of power (downed), its cells trickle-charge on their own at ~+1%/day,
        // so it will slowly come back online even if nobody helps - though colonists usually just haul it
        // to a stand to charge it far faster.
        public const float SlowRechargePerDay = 0.01f;

        // A full battery lasts ~3 days of operation at baseline efficiency (drain factor 1.0); the
        // efficiency multiplier scales this exactly like the reactor does.
        public override float LifespanDays => 3f;

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!pawn.IsHashIntervalTick(BatteryTickRate, delta))
            {
                return;
            }
            // Out of power / downed, or deliberately parked in a dormant low-power work mode by its
            // mechanitor: trickle-charge slowly on its own instead of draining.
            if (Severity >= 1f || MechOversightUtil.IsDormantForPower(pawn))
            {
                Energy = Mathf.Min(1f, Energy + (SlowRechargePerDay / GenDate.TicksPerDay) * BatteryTickRate);
            }
            else
            {
                // Operating: drain toward empty.
                var baseDrainSpeed = (1f / (GenDate.TicksPerDay * LifespanDays)) * PowerEfficiencyDrainMultiplier;
                baseDrainSpeed *= BatteryTickRate;
                Energy = Mathf.Max(0, Energy - baseDrainSpeed);
            }
        }
    }
}
