using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    // When the colony's primary ideoligion treats an android as a mere tool (the "androids: tools" precept,
    // or "respected (only awakened)" for a non-awakened android), the android is shown like an owned
    // slave - but tinted blue instead of the slave's amber, marking it as property rather than a person.
    [HarmonyPatch(typeof(PawnNameColorUtility), nameof(PawnNameColorUtility.PawnNameColorOf))]
    public static class PawnNameColorUtility_PawnNameColorOf_Patch
    {
        // A clear, cold blue that reads as the "slave amber" equivalent for androids.
        public static readonly Color ToolAndroidColor = new Color(0.4f, 0.65f, 1f);

        public static void Postfix(Pawn pawn, ref Color __result)
        {
            if (pawn == null)
            {
                return;
            }
            if (pawn.IsTreatedAsToolByColony())
            {
                __result = ToolAndroidColor;
                return;
            }
            // A mechlike android is still a colonist in the bar, not a mech: vanilla would tint it with the
            // "uncontrolled player mech" colour whenever it has no overseer / no bandwidth. Keep it white.
            if (MechOversightUtil.IsOversightAndroid(pawn) && pawn.Faction == Faction.OfPlayer)
            {
                __result = Color.white;
            }
        }
    }
}
