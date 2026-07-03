using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    // Shrink the Social tab down to just the interaction log for a "machine" android (the card only
    // draws the log for those, so the full 510px height left a big empty gap). UpdateSize runs while
    // TabRect is computed, before the tab window is drawn, so changing the height here resizes the
    // window cleanly. Reset to the vanilla height for every other pawn since the tab is a shared
    // singleton reused across selections.
    [HarmonyPatch(typeof(InspectTabBase), "UpdateSize")]
    public static class InspectTabBase_UpdateSize_Patch
    {
        // Roughly the height the log occupies under the relations section on an ordinary pawn's card.
        private const float LogOnlyHeight = 185f;
        private const float DefaultHeight = 510f;

        private static readonly AccessTools.FieldRef<InspectTabBase, Vector2> SizeRef =
            AccessTools.FieldRefAccess<InspectTabBase, Vector2>("size");
        private static readonly MethodInfo SelPawnGetter =
            AccessTools.PropertyGetter(typeof(ITab_Pawn_Social), "SelPawnForSocialInfo");

        public static void Postfix(InspectTabBase __instance)
        {
            if (!(__instance is ITab_Pawn_Social) || SelPawnGetter == null)
            {
                return;
            }
            var pawn = SelPawnGetter.Invoke(__instance, null) as Pawn;
            ref Vector2 size = ref SizeRef(__instance);
            size.y = pawn.SocialTabLogOnly() ? LogOnlyHeight : DefaultHeight;
        }
    }
}
