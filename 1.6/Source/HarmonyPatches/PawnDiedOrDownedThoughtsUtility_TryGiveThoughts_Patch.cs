using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // While an android still has its subcore, its "death" is only a recoverable destruction (it can be
    // resurrected or reprinted), so - like an Altered Carbon sleeve whose stack survives - colonists do
    // not grieve for it. The real grief only comes when the subcore itself is destroyed.
    [HarmonyPatch(typeof(PawnDiedOrDownedThoughtsUtility), "TryGiveThoughts",
        new Type[] { typeof(Pawn), typeof(DamageInfo?), typeof(PawnDiedOrDownedThoughtsKind) })]
    public static class PawnDiedOrDownedThoughtsUtility_TryGiveThoughts_Patch
    {
        public static bool Prefix(Pawn victim)
        {
            if (!Utils.forcingAndroidRealDeath && victim != null && victim.IsAndroid()
                && Utils.HasSubcore(victim, out _))
            {
                return false;
            }
            return true;
        }
    }
}
