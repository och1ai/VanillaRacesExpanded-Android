using RimWorld;
using Verse;

namespace VREAndroids
{
    // Power-source hardware gene (reactor powered / battery powered). On add, installs the power
    // core declared by its PowerCoreExtension, replacing any core left from a different power gene.
    // Done here rather than in Gene_SyntheticBody so it doesn't depend on gene application order.
    public class Gene_AndroidPower : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            // Pass our own def: this gene may not be flagged Active yet during PostAdd.
            Utils.SyncPowerCore(pawn, def);
        }
    }
}
