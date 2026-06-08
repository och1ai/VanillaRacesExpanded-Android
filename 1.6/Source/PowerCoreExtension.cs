using Verse;

namespace VREAndroids
{
    // Placed on a power-source hardware gene (reactor powered / battery powered) to declare which
    // power-core hediff that gene installs and on which body part. Mirrors BloodOrgansExtension.
    public class PowerCoreExtension : DefModExtension
    {
        public BodyPartDef part;
        public HediffDef coreHediff;
    }
}
