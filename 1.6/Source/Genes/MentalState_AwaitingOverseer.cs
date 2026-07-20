using RimWorld;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    // The dormant state of a mechanitor-oversight android that has no overseer. It stands frozen (like the
    // solar-flare freeze) and recovers the instant a mechanitor connects to it.
    public class MentalState_AwaitingOverseer : MentalState
    {
        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }

        public override void PreStart()
        {
            base.PreStart();
            if (pawn.stances != null)
            {
                pawn.stances.SetStance(new Stance_Stand(999999999, pawn.Position + pawn.Rotation.FacingCell, null));
            }
        }

        public override void MentalStateTick(int delta)
        {
            base.MentalStateTick(delta);
            if (pawn.GetOverseer() != null)
            {
                RecoverFromState();
                return;
            }
            if (pawn.Spawned && pawn.stances != null && pawn.stances.curStance is not Stance_Stand)
            {
                pawn.stances.SetStance(new Stance_Stand(999999999, pawn.Position + pawn.Rotation.FacingCell, null));
            }
        }

        public override void PostEnd()
        {
            base.PostEnd();
            if (!pawn.Spawned)
            {
                return;
            }
            var gene = pawn.genes?.GetFirstGeneOfType<Gene_MechOversight>();
            if (gene != null)
            {
                pawn.Map.overlayDrawer.Disable(pawn, ref gene.overlayPowerOff);
            }
            if (pawn.stances != null)
            {
                pawn.stances.SetStance(new Stance_Mobile());
            }
        }
    }
}
