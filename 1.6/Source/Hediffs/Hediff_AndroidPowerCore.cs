using RimWorld;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    // Shared energy core for an android. Concrete cores are either a battery (rechargeable at a
    // charging station) or a reactor (self-sufficient, not rechargeable). The core stores the
    // charge level, mirrors it onto the power need, and shuts the android down (severity 1 ->
    // consciousness/manipulation caps) when it runs out.
    public abstract class Hediff_AndroidPowerCore : Hediff_AndroidPart
    {
        private Need_ReactorPower needInt;
        public Need_ReactorPower NeedReactor => needInt ??= pawn?.needs?.TryGetNeed<Need_ReactorPower>();
        protected float curEnergy;
        public override bool ShouldRemove => false;

        // Battery cores can be topped up at a charging station; reactors power themselves and cannot.
        public abstract bool CanRecharge { get; }

        // How many days a full core lasts at baseline efficiency (drain factor 1.0).
        public abstract float LifespanDays { get; }

        // Fraction of full charge lost per day at the android's current efficiency. Shown in the
        // pawn inspect string like a mechanoid's "(-X% / day)".
        public float DrainPerDay => PowerEfficiencyDrainMultiplier / LifespanDays;

        public float Energy
        {
            get
            {
                return curEnergy;
            }
            set
            {
                curEnergy = Mathf.Clamp01(value);
                if (pawn != null)
                {
                    var need = NeedReactor;
                    if (need != null)
                    {
                        need.curLevelInt = curEnergy;
                    }
                    UpdateSeverity();
                }
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            Energy = 1f;
            Severity = 0;
        }

        // Sum of gene metabolic efficiency -> power drain multiplier. Affects battery and reactor
        // drain identically, so a more efficient hardware/subroutine loadout lasts proportionally
        // longer on a charge.
        public float PowerEfficiencyDrainMultiplier
        {
            get
            {
                int efficiency = 0;
                foreach (Gene item in pawn.genes.GenesListForReading)
                {
                    if (!item.Overridden)
                    {
                        efficiency += item.def.biostatMet;
                    }
                }
                return AndroidStatsTable.PowerEfficiencyToPowerDrainFactorCurve.Evaluate(efficiency);
            }
        }

        // The android runs out of power (downs) only when fully drained, but once down it stays down
        // until charged back past a small buffer - this hysteresis stops it from flickering awake for a
        // single tick every time a trickle of charge comes in.
        public const float WakeThreshold = 0.15f;

        protected void UpdateSeverity()
        {
            if (this.Severity >= 1f)
            {
                if (Energy >= WakeThreshold)
                {
                    this.Severity = 0f;
                }
            }
            else if (Energy <= 0f)
            {
                this.Severity = 1f;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref curEnergy, "curEnergy");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                UpdateSeverity();
            }
        }
    }
}
