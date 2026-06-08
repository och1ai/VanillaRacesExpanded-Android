using System.Linq;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // Blood-type hardware gene. On add, installs the synthetic circulatory organs declared by its
    // BloodOrgansExtension (e.g. neutroamine -> neutropump/neutrofilter, hemogenic ->
    // hemopump/hemofilter). Bloodless declares none, so it gets no circulatory organs.
    // The organs are added here rather than in Gene_SyntheticBody so they don't depend on the
    // order genes are applied during pawn generation.
    public class Gene_AndroidBlood : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            // Reconcile organs to this blood type: this both installs our organs and removes any
            // left over from a different blood type (e.g. when the body is generated as one type
            // and then has its blood swapped during creation). Pass our own def since this gene
            // may not be flagged Active yet during PostAdd.
            Utils.SyncBloodOrgans(pawn, def);
        }
    }
}
