using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    public class Recipe_AdministerNeutroamineForAndroid : Recipe_AdministerIngestible
    {
        // Neutroamine needed to refill a full reservoir (1.0 severity of neutroloss), i.e. how much
        // neutroamine an android's body holds. Neutroamine blood is the cheap option: 40 (was 100).
        public const float NeutroaminePerFullReservoir = 40f;

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

    // Extract neutroamine from a neutroamine-blood android's reservoir into neutroamine items - the mirror
    // of vanilla "extract hemogen pack". Each operation pulls a fixed proportion of the reservoir; the bill
    // can be repeated, and (like extract hemogen) it blocks and warns when the android is already low, since
    // draining the reservoir dry would kill it.
    public class Recipe_ExtractNeutroamine : Recipe_Surgery
    {
        // Proportion of a full reservoir pulled per operation (same 0.45 as extract hemogen pack).
        public const float ExtractSeverity = 0.45f;

        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (thing is Pawn pawn && (pawn.IsAndroid() is false
                || pawn.HasActiveGene(VREA_DefOf.VREA_NeutroCirculation) is false))
            {
                return false;
            }
            return base.AvailableOnNow(thing, part);
        }

        public override bool CompletableEver(Pawn surgeryTarget)
            => base.CompletableEver(surgeryTarget) && HasEnoughNeutroamine(surgeryTarget);

        public override void CheckForWarnings(Pawn medPawn)
        {
            base.CheckForWarnings(medPawn);
            if (!HasEnoughNeutroamine(medPawn))
            {
                Messages.Message("VREA.MessageCannotExtractNeutroamine".Translate(medPawn.Named("PAWN")),
                    medPawn, MessageTypeDefOf.NeutralEvent, historical: false);
            }
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (!HasEnoughNeutroamine(pawn))
            {
                Messages.Message("VREA.MessageNotEnoughNeutroamineToExtract".Translate(pawn.Named("PAWN")),
                    pawn, MessageTypeDefOf.NeutralEvent);
                return;
            }
            var loss = pawn.health.hediffSet.GetFirstHediffOfDef(VREA_DefOf.VREA_NeutroLoss)
                ?? pawn.health.AddHediff(VREA_DefOf.VREA_NeutroLoss);
            loss.Severity += ExtractSeverity;
            OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
        }

        public override void OnSurgerySuccess(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            int amount = Mathf.RoundToInt(ExtractSeverity * Recipe_AdministerNeutroamineForAndroid.NeutroaminePerFullReservoir);
            var neutroamine = ThingMaker.MakeThing(VREA_DefOf.Neutroamine);
            neutroamine.stackCount = amount;
            if (!GenPlace.TryPlaceThing(neutroamine, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near))
            {
                Log.Error("Could not drop extracted neutroamine near " + pawn.PositionHeld);
            }
        }

        // Enough only if this extraction won't drain the reservoir past empty (a full drain kills the android).
        private bool HasEnoughNeutroamine(Pawn pawn)
        {
            var loss = pawn.health.hediffSet.GetFirstHediffOfDef(VREA_DefOf.VREA_NeutroLoss);
            return (loss?.Severity ?? 0f) < 1f - ExtractSeverity;
        }
    }
}
