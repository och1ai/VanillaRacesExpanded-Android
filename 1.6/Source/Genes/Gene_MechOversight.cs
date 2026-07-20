using RimWorld;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    // "Mechlike" subroutine: the standard mechanoid routine that delegates part of the android's
    // decision-making to a supervising mechanitor. The android is treated as a colony mech (see
    // MechOversight_Patches), but on its own it has no operator: with no overseer it stands dormant -
    // powered down and frozen, the same way the solar-flare-vulnerability gene freezes an android - until a
    // mechanitor connects to it.
    public class Gene_MechOversight : Gene
    {
        public OverlayHandle? overlayPowerOff;

        public override void PostAdd()
        {
            base.PostAdd();
            // Make sure the overseer-subject comp exists as soon as the gene is applied (e.g. reprogramming
            // an already-spawned android), not only on spawn.
            if (pawn != null)
            {
                MechOversightUtil.EnsureOverseerSubject(pawn);
            }
        }

        // Losing the subroutine (most often by awakening) severs the link: an awakened mind is nobody's
        // remote-controlled machine. Without this the android would keep its Overseer relation and stay
        // sitting in the mechanitor's control group with no gene backing it.
        public override void PostRemove()
        {
            base.PostRemove();
            if (pawn == null)
            {
                return;
            }
            if (pawn.Spawned && overlayPowerOff.HasValue)
            {
                pawn.Map.overlayDrawer.Disable(pawn, ref overlayPowerOff);
            }
            // Disconnect explicitly, and do NOT rely on GetOverseer: by the time PostRemove runs, the
            // gene-change may already have severed the Overseer relation (a non-mech can't be overseen), yet
            // never unassigned the android from the control group - so it lingers in "Group 1". Sweep every
            // player mechanitor and unassign this pawn from all their groups, dropping any leftover relation
            // too. Belt-and-suspenders, but guaranteed to clear it.
            foreach (Pawn mechanitor in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
            {
                var tracker = mechanitor.mechanitor;
                if (tracker == null)
                {
                    continue;
                }
                bool wasControlling = tracker.ControlledPawns.Contains(pawn)
                    || tracker.OverseenPawns.Contains(pawn);
                tracker.UnassignPawnFromAnyControlGroup(pawn);
                mechanitor.relations.TryRemoveDirectRelation(PawnRelationDefOf.Overseer, pawn);
                if (wasControlling)
                {
                    tracker.Notify_BandwidthChanged();
                }
            }
            // Drop the dormant state so it doesn't stay frozen waiting for an overseer it no longer needs.
            if (pawn.MentalStateDef == VREA_DefOf.VREA_AwaitingOverseer)
            {
                pawn.mindState?.mentalStateHandler?.CurState?.RecoverFromState();
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (!pawn.Spawned)
            {
                return;
            }
            MechOversightUtil.EnsureOverseerSubject(pawn);

            // No mechanitor has taken oversight -> stand dormant until one does.
            if (pawn.GetOverseer() == null)
            {
                if (pawn.MentalStateDef != VREA_DefOf.VREA_AwaitingOverseer)
                {
                    if (pawn.InMentalState)
                    {
                        pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
                    }
                    pawn.mindState.mentalStateHandler.TryStartMentalState(VREA_DefOf.VREA_AwaitingOverseer,
                        null, forced: true, forceWake: false, causedByMood: false, null, transitionSilently: true);
                }
                if (overlayPowerOff is null)
                {
                    overlayPowerOff = pawn.Map.overlayDrawer.Enable(pawn, OverlayTypes.PowerOff);
                }
            }
            // When it has an overseer, MentalState_AwaitingOverseer recovers itself and clears the overlay.
        }
    }
}
