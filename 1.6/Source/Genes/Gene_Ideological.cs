using RimWorld;
using Verse;

namespace VREAndroids
{
    // Belief-modelling subroutine. An android only follows an ideoligion while it carries this gene:
    // adding it assigns one (random for now - the customization UI will let the player pick), removing
    // it drops the android back to no ideoligion. Roles then work through the normal ideoligion UI.
    public class Gene_Ideological : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            Utils.SyncAndroidIdeo(pawn);
        }

        public override void PostRemove()
        {
            base.PostRemove();
            Utils.SyncAndroidIdeo(pawn);
        }
    }
}
