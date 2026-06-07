using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace VREAndroids
{
    public class FloatMenuOptionProvider_RefuelWithNeutroamine : FloatMenuOptionProvider
    {

        public override bool Drafted => true;

        public override bool Undrafted => true;

        public override bool Multiselect => true;

        public override bool CanSelfTarget => true;

        public override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
        {
            if (!context.FirstSelectedPawn.IsAndroid())
            {
                return null;
            }


            var neutroloss = context.FirstSelectedPawn.health.hediffSet.GetFirstHediffOfDef(VREA_DefOf.VREA_NeutroLoss);
            if (neutroloss != null)
            {


                if (clickedThing.def == VREA_DefOf.Neutroamine)
                {
                    return new FloatMenuOption("VREA.RefuelWithNeutroamine".Translate(), delegate
                    {
                        var job = JobMaker.MakeJob(VREA_DefOf.VREA_RefuelWithNeutroamine, clickedThing);
                        var amount = Mathf.CeilToInt(neutroloss.Severity * Recipe_AdministerNeutroamineForAndroid.NeutroaminePerFullReservoir);
                        amount = Mathf.Min(clickedThing.stackCount, amount);
                        job.count = amount;
                        context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job);
                    });

                }
            }

            return null;
        }
    }
}
