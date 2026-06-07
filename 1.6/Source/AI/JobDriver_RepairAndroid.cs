using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    public class JobDriver_RepairAndroid : JobDriver
    {
        protected int ticksToNextRepair;
        protected Pawn Patient => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        protected int TicksPerHeal => 200;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Repair is open to every crafter, so a rare same-tick race for the same patient can
            // slip past the work giver; fail the reservation quietly instead of logging an error.
            return pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed: false);
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            var gotoToil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // Pure repair: no tending pass. Bleeding wounds are simply repaired away (bleeding
            // ones first), then injuries, missing parts and scars. Speed scales with the
            // repairer's crafting skill and work speed.
            Toil repairToil = Toils_General.Wait(int.MaxValue);
            repairToil.WithEffect(EffecterDefOf.MechRepairing, TargetIndex.A);
            repairToil.PlaySustainerOrSound(SoundDefOf.RepairMech_Touch);
            repairToil.AddPreInitAction(delegate
            {
                ticksToNextRepair = RepairInterval();
            });
            repairToil.handlingFacing = true;
            repairToil.tickIntervalAction = delegate(int delta)
            {
                ticksToNextRepair -= delta;
                if (ticksToNextRepair <= 0)
                {
                    RepairTick(Patient);
                    ticksToNextRepair = RepairInterval();
                }
                pawn.rotationTracker.FaceTarget(Patient);
                if (pawn.skills != null)
                {
                    pawn.skills.Learn(SkillDefOf.Crafting, 0.05f * delta);
                }
            };

            if (pawn != Patient)
            {
                repairToil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            }
            repairToil.AddEndCondition(() => CanRepairAndroid(Patient) ? JobCondition.Ongoing : JobCondition.Succeeded);
            repairToil.activeSkill = () => SkillDefOf.Crafting;
            if (pawn != Patient)
            {
                yield return gotoToil;
            }
            yield return repairToil;
            AddFinishAction(delegate
            {
                if (Patient != null && Patient != pawn && Patient.CurJob != null
                    && (Patient.CurJob.def == JobDefOf.Wait || Patient.CurJob.def == JobDefOf.Wait_MaintainPosture))
                {
                    Patient.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            });
        }

        // Ticks between each repair point, faster with higher crafting skill and work speed.
        // Self-repair is slower (0.7x), matching the self-repair tooltip.
        private int RepairInterval()
        {
            float speed = pawn.GetStatValue(StatDefOf.GeneralLaborSpeed);
            SkillRecord crafting = pawn.skills?.GetSkill(SkillDefOf.Crafting);
            float skillFactor = crafting != null ? Mathf.Lerp(0.5f, 2f, Mathf.Clamp01(crafting.Level / 20f)) : 1f;
            float factor = Mathf.Max(0.1f, speed * skillFactor);
            if (pawn == Patient)
            {
                factor *= 0.7f;
            }
            return Mathf.Max(1, Mathf.RoundToInt(TicksPerHeal / factor));
        }

        public override void Notify_DamageTaken(DamageInfo dinfo)
        {
            base.Notify_DamageTaken(dinfo);
            if (dinfo.Def.ExternalViolenceFor(pawn) && pawn.Faction != Faction.OfPlayer && pawn == Patient)
            {
                pawn.jobs.CheckForJobOverride();
            }
        }

        public static bool CanRepairAndroid(Pawn android)
        {
            if (android.InMentalState)
            {
                return false;
            }
            if (android.IsBurning())
            {
                return false;
            }
            if (android.IsAttacking())
            {
                return false;
            }
            // Androids are repaired like mechanoids: injuries, missing body parts and
            // permanent damage (scars) are all fixable, so nothing on them is permanent.
            return GetHediffToHeal(android) != null
                || GetMissingPartToRestore(android) != null
                || GetPermanentHediffToRemove(android) != null;
        }

        // Bleeding wounds are repaired first (so bleeding stops fastest), then the smallest
        // remaining injury.
        public static Hediff GetHediffToHeal(Pawn android)
        {
            Hediff bleeding = null;
            float maxBleed = 0f;
            Hediff smallest = null;
            float minSeverity = float.PositiveInfinity;
            foreach (Hediff hediff in android.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_Injury injury && injury.IsPermanent() is false)
                {
                    float bleed = injury.BleedRate;
                    if (bleed > maxBleed)
                    {
                        maxBleed = bleed;
                        bleeding = injury;
                    }
                    if (injury.Severity < minSeverity)
                    {
                        minSeverity = injury.Severity;
                        smallest = injury;
                    }
                }
            }
            return bleeding ?? smallest;
        }

        // The closest-to-core missing part that can be regrown (its parent still exists).
        public static Hediff_MissingPart GetMissingPartToRestore(Pawn android)
        {
            HediffSet hediffSet = android.health.hediffSet;
            foreach (Hediff hediff in hediffSet.hediffs)
            {
                if (hediff is Hediff_MissingPart missingPart && missingPart.def.keepOnBodyPartRestoration is false
                    && missingPart.Part != null)
                {
                    BodyPartRecord parent = missingPart.Part.parent;
                    if (parent == null || hediffSet.GetNotMissingParts().Contains(parent))
                    {
                        return missingPart;
                    }
                }
            }
            return null;
        }

        // Permanent injuries (scars) so androids carry no permanent damage once repaired.
        public static Hediff GetPermanentHediffToRemove(Pawn android)
        {
            foreach (Hediff hediff in android.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_Injury injury && injury.IsPermanent())
                {
                    return injury;
                }
            }
            return null;
        }

        public static void RepairTick(Pawn android)
        {
            Hediff hediffToHeal = GetHediffToHeal(android);
            if (hediffToHeal != null)
            {
                // Each wound is fully repaired in a single pass (no progressive shrinking and
                // no tending), so its bleeding stops at once. Parts are handled afterwards.
                hediffToHeal.Heal(hediffToHeal.Severity + 1f);
                return;
            }
            Hediff_MissingPart missingPart = GetMissingPartToRestore(android);
            if (missingPart != null)
            {
                RestoreMissingPart(android, missingPart.Part);
                return;
            }
            Hediff permanent = GetPermanentHediffToRemove(android);
            if (permanent != null)
            {
                android.health.RemoveHediff(permanent);
            }
        }

        // Regrows a missing part and re-synthesizes the android counterparts across the whole
        // restored subtree, leaving any manually installed implant in place rather than
        // overwriting it. RestorePart only clears the top part for androids (the mod's
        // RestorePartRecursiveInt patch is non-recursive), so the children are handled here,
        // mirroring Recipe_InstallAndroidPart.
        public static void RestoreMissingPart(Pawn android, BodyPartRecord part)
        {
            android.health.RestorePart(part);
            ReSynthesizeSubtree(android, part);
            android.health.hediffSet.DirtyCache();
        }

        private static void ReSynthesizeSubtree(Pawn android, BodyPartRecord part)
        {
            List<Hediff> hediffs = android.health.hediffSet.hediffs;
            bool hasAddedPart = false;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                Hediff hediff = hediffs[i];
                if (hediff.Part != part)
                {
                    continue;
                }
                if (hediff is Hediff_MissingPart && hediff.def.keepOnBodyPartRestoration is false)
                {
                    hediffs.RemoveAt(i);
                    hediff.PostRemoved();
                }
                else if (hediff is Hediff_AddedPart)
                {
                    hasAddedPart = true;
                }
            }
            if (hasAddedPart is false)
            {
                HediffDef counterpart = Utils.GetAndroidCounterPartFor(part.def, android);
                if (counterpart != null)
                {
                    android.health.AddHediff(counterpart, part);
                }
            }
            for (int i = 0; i < part.parts.Count; i++)
            {
                ReSynthesizeSubtree(android, part.parts[i]);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksToNextRepair, "ticksToNextRepair", 0);
        }
    }
}
