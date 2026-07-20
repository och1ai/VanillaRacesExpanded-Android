using RimWorld;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    [HotSwappable]
    public class Hediff_AndroidReactor : Hediff_AndroidPowerCore
    {
        // A reactor powers itself; it is replaced when spent, never recharged at a station.
        public override bool CanRecharge => false;

        // ~2 years of operation at baseline efficiency.
        public override float LifespanDays => GenDate.DaysPerYear * 2f;

        public override void PostRemoved()
        {
            base.PostRemoved();
            if (pawn.MapHeld != null)
            {
                pawn.TrySpawnWaste(pawn.PositionHeld, pawn.MapHeld);
            }
        }

        public const int AndroidReactorTickRate = 60;

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn.IsHashIntervalTick(AndroidReactorTickRate, delta))
            {
                // Sleeping under mechanitor oversight (dormant self-charge, or "recharge" - a reactor has
                // nothing to plug into): the reactor idles without spending any charge.
                if (MechOversightUtil.IsDormantForPower(pawn))
                {
                    return;
                }
                // A reactor lasts ~2 years of operation at baseline efficiency (drain factor 1.0).
                var baseDrainSpeed = (1f / (GenDate.TicksPerYear * 2f)) * PowerEfficiencyDrainMultiplier;
                baseDrainSpeed *= AndroidReactorTickRate;
                if (pawn.HasActiveGene(VREA_DefOf.VREA_SolarPowered))
                {
                    var mapHeld = pawn.MapHeld;
                    if (mapHeld != null && (mapHeld.gameConditionManager.ElectricityDisabled(mapHeld)
                        || Find.World.gameConditionManager.ElectricityDisabled(mapHeld)))
                    {
                        Energy = Mathf.Min(1, Energy + baseDrainSpeed);
                        return;
                    }
                    else if (mapHeld != null && pawn.Position.InSunlight(mapHeld))
                    {
                        return;
                    }
                }
                if (pawn.HasActiveGene(VREA_DefOf.VREA_RainVulnerability) && pawn.Spawned && pawn.Position.Roofed(pawn.Map) is false
                    && pawn.Map.weatherManager.RainRate >= 0.01f)
                {
                    baseDrainSpeed *= 2f;
                }

                Energy = Mathf.Max(0, Energy - baseDrainSpeed);
            }
        }
    }
}
