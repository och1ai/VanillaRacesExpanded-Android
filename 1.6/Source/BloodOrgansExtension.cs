using System.Collections.Generic;
using Verse;

namespace VREAndroids
{
    // Placed on a blood-type hardware gene to declare which synthetic circulatory organs that
    // blood uses (e.g. neutroamine -> neutropump/neutrofilter, hemogenic -> hemopump/hemofilter).
    // A blood type with no entry for a part (e.g. bloodless) gets no organ there.
    public class BloodOrgansExtension : DefModExtension
    {
        public List<BloodOrgan> organs = new List<BloodOrgan>();

        public HediffDef GetOrgan(BodyPartDef part)
        {
            for (int i = 0; i < organs.Count; i++)
            {
                if (organs[i].part == part)
                {
                    return organs[i].hediff;
                }
            }
            return null;
        }
    }

    public class BloodOrgan
    {
        public BodyPartDef part;
        public HediffDef hediff;
    }
}
