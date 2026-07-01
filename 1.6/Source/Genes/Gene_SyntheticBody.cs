using RimWorld;
using System;
using System.Linq;
using Verse;

namespace VREAndroids
{
    public class Gene_SyntheticBody : Gene
    {
        public NameTriple storedTripleName;

        public bool autoRepair = true;
        public override void PostAdd()
        {
            base.PostAdd();
            foreach (var bodyPart in this.pawn.def.race.body.AllParts.OrderByDescending(x => x.Index))
            {
                // Circulatory organs (heart/kidney) depend on the blood type and are installed by
                // Gene_AndroidBlood; the power core (reactor/battery) depends on the power gene and
                // is installed by Gene_AndroidPower. Skip both here.
                if (Utils.IsBloodOrganPart(bodyPart.def) || Utils.IsPowerCorePart(bodyPart.def))
                {
                    continue;
                }
                var hediffDef = bodyPart.def.GetAndroidCounterPart();
                if (hediffDef != null && this.pawn.health.hediffSet.GetNotMissingParts().Contains(bodyPart)
                    && this.pawn.health.hediffSet.hediffs.Any(h => h.Part == bodyPart && h is Hediff_AddedPart) is false)
                {
                    var hediff = HediffMaker.MakeHediff(hediffDef, pawn, bodyPart);
                    try
                    {
                        pawn.health.hediffSet.AddDirect(hediff);
                    }
                    catch (Exception ex)
                    {
                        Log.Message("[VREA] Error adding " + hediff + " to " + pawn + ", exception: " + ex.ToString());
                    }
                }
            }
            // Install the android's subcore in the brain: it stores the android's identity so it can be
            // recovered and reprinted or resurrected - but, sitting in the brain, it is destroyed along
            // with the head, which is what makes decapitation a true, permanent kill. Guarded so an
            // android only ever carries one.
            if (pawn.health.hediffSet.hediffs.OfType<Hediff_AndroidSubcore>().Any() is false)
            {
                BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
                pawn.health.AddHediff(VREA_DefOf.VREA_AndroidSubcoreImplant, brain);
            }
            MeditationFocusTypeAvailabilityCache.ClearFor(pawn);
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn.IsHashIntervalTick(GenDate.TicksPerHour, delta) && pawn.IsAwakened() is false 
                && pawn.HasActiveGene(VREA_DefOf.VREA_AntiAwakeningProtocols) is false && Rand.Chance(0.5f))
            {
                if (pawn.needs.mood.CurLevel <= 0.05f)
                {
                    Awaken("VREA.AndroidAwakening".Translate(pawn.Named("PAWN")), "VREA.AndroidAwakeningLowMood".Translate(pawn.Named("PAWN")));
                    var gene = pawn.genes.GetGene(VREA_DefOf.VREA_CombatIncapability);
                    if (gene != null)
                    {
                        pawn.genes.RemoveGene(gene);
                    }
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                }
                if (pawn.needs.mood.CurLevel >= 0.8f)
                {
                    Awaken("VREA.AndroidAwakening".Translate(pawn.Named("PAWN")), "VREA.AndroidAwakeningHighMood".Translate(pawn.Named("PAWN")));
                    InspirationDef randomAvailableInspirationDef = pawn.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                    if (randomAvailableInspirationDef != null)
                    {
                        pawn.mindState.inspirationHandler.TryStartInspiration(randomAvailableInspirationDef, "LetterInspirationBeginThanksToHighMoodPart".Translate());
                    }
                }
            }
        }

        public void Awaken(TaggedString title, TaggedString description)
        {
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                var letter = (ChoiceLetter_AndroidAwakened)LetterMaker.MakeLetter(VREA_DefOf.VREA_AndroidAwakenedLetter);
                letter.Label = title;
                letter.Text = description;
                letter.ConfigureAwakenedLetter(pawn, 8, 6, 4);
                Find.LetterStack.ReceiveLetter(letter);
            }
            else
            {
                PawnGenerator.GenerateTraits(pawn, new PawnGenerationRequest(pawn.kindDef, pawn.Faction));
            }

            foreach (var gene in pawn.genes.GenesListForReading.ToList())
            {
                if (gene.def is AndroidGeneDef geneDef && geneDef.removeWhenAwakened)
                {
                    pawn.genes.RemoveGene(gene);
                }
            }
            if (storedTripleName != null)
            {
                pawn.Name = storedTripleName;
            }
            MoteMaker.MakeColonistActionOverlay(pawn, VREA_DefOf.VREA_AndroidAwakenedMote);
            VREA_DefOf.VREA_AndroidAwakenedEffect.SpawnAttached(pawn, pawn.Map);
            pawn.needs.AddOrRemoveNeedsAsAppropriate();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref storedTripleName, "storedTripleName");
            Scribe_Values.Look(ref autoRepair, "autoRepair", true);
        }
    }
}
