using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // An android's own destruction never grieves the colony (no "my friend died", "witnessed ally's
    // death", etc.): like an Altered Carbon sleeve, the person survives in the subcore and can be
    // resurrected or reprinted. Grief - and only the relation/colony thoughts, never the witnessed-
    // death ones - comes solely from AndroidRealDeath, which fires when the subcore itself is destroyed
    // (a head/brain kill, the corpse being destroyed, or the popped subcore item being destroyed).
    // AndroidRealDeath sets forcingAndroidRealDeath, so its thoughts are let through.
    [HarmonyPatch(typeof(PawnDiedOrDownedThoughtsUtility), "TryGiveThoughts",
        new Type[] { typeof(Pawn), typeof(DamageInfo?), typeof(PawnDiedOrDownedThoughtsKind) })]
    public static class PawnDiedOrDownedThoughtsUtility_TryGiveThoughts_Patch
    {
        public static bool Prefix(Pawn victim)
        {
            if (!Utils.forcingAndroidRealDeath && victim != null && victim.IsAndroid())
            {
                return false;
            }
            return true;
        }
    }
}
