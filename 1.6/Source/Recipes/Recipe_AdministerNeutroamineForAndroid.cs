using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    public class Recipe_AdministerNeutroamineForAndroid : Recipe_AdministerIngestible
    {
        // Neutroamine needed to refill a full reservoir (1.0 severity of neutroloss). Neutroamine
        // blood is the cheap option: 25 instead of the old 100.
        public const float NeutroaminePerFullReservoir = 25f;

        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            // Only neutroamine-blood androids are refuelled with neutroamine. Normal-blood
            // androids top up with hemogen instead, and bloodless ones have no reservoir.
            if (thing is Pawn pawn && pawn.HasActiveGene(VREA_DefOf.VREA_NeutroCirculation) is false)
            {
                return false;
            }
            return base.AvailableOnNow(thing, part);
        }

        public override float GetIngredientCount(IngredientCount ing, Bill bill)
        {
            Pawn pawn = bill.billStack?.billGiver as Pawn;
            if (pawn == null)
            {
                return base.GetIngredientCount(ing, bill);
            }
            Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(VREA_DefOf.VREA_NeutroLoss);
            if (firstHediffOfDef == null)
            {
                return base.GetIngredientCount(ing, bill);
            }
            return Mathf.Min(bill.Map.listerThings.ThingsOfDef(VREA_DefOf.Neutroamine).Sum((Thing x) => x.stackCount), firstHediffOfDef.Severity * NeutroaminePerFullReservoir);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            var neutroamine = ingredients.Where(x => x.def == VREA_DefOf.Neutroamine).Sum(x => x.stackCount);
            var neutroloss = pawn.health.hediffSet.GetFirstHediffOfDef(VREA_DefOf.VREA_NeutroLoss);
            if (neutroloss != null)
            {
                neutroloss.Severity -= neutroamine / NeutroaminePerFullReservoir;
                if (neutroloss.Severity <= 0.01f)
                {
                    neutroloss.Severity = 0;
                }
            }
            ingredients.ForEach(x => x.Destroy());
        }
    }
}
