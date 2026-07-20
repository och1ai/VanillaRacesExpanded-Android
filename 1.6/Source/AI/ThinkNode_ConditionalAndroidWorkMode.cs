using RimWorld;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    // Vanilla's ThinkNode_ConditionalWorkMode requires RaceProps.IsMechanoid, so it can never match a
    // humanlike android. This is the same test for an overseen "mechlike" android: it matches when a
    // mechanitor has taken oversight and set the android's control group to this work mode.
    public class ThinkNode_ConditionalAndroidWorkMode : ThinkNode_Conditional
    {
        public MechWorkModeDef workMode;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var copy = (ThinkNode_ConditionalAndroidWorkMode)base.DeepCopy(resolve);
            copy.workMode = workMode;
            return copy;
        }

        public override bool Satisfied(Pawn pawn)
        {
            if (!MechOversightUtil.IsOversightAndroid(pawn) || pawn.Faction != Faction.OfPlayer)
            {
                return false;
            }
            // GetMechWorkMode null-chains through the overseer and control group, so an unassigned or
            // unclaimed android simply matches nothing and falls through to its normal behaviour.
            return pawn.GetOverseer() != null && pawn.GetMechWorkMode() == workMode;
        }
    }
}
