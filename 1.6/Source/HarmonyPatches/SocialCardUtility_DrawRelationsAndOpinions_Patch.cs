using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    // A "machine" android - emotionless (no emotion-simulator hardware, not awakened) and without the
    // ideological subroutine - has neither relationships nor an ideoligion, so its Social tab is drawn
    // as just the interaction log filling the whole card: no relations section, no ideoligion/role
    // section, and no empty gaps where they used to be.
    [HarmonyPatch(typeof(SocialCardUtility), "DrawSocialCard")]
    public static class SocialCardUtility_DrawSocialCard_Patch
    {
        public static bool Prefix(Rect rect, Pawn pawn)
        {
            if (!pawn.SocialTabLogOnly())
            {
                return true;
            }
            Widgets.BeginGroup(rect);
            Text.Font = GameFont.Small;
            float top = Prefs.DevMode ? 20f : 15f;
            Rect logRect = new Rect(0f, top, rect.width, rect.height - top).ContractedBy(10f);
            InteractionCardUtility.DrawInteractionsLog(logRect, pawn, Find.PlayLog.AllEntries, 12);
            Widgets.EndGroup();
            return false;
        }
    }

    [HarmonyPatch(typeof(SocialCardUtility), "DrawRelationsAndOpinions")]
    public static class SocialCardUtility_DrawRelationsAndOpinions_Patch
    {
        // For the remaining cases (e.g. an ideological android that still lacks emotion simulators),
        // keep hiding just the relations/opinions list while leaving the rest of the card intact.
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn selPawnForSocialInfo)
        {
            if (selPawnForSocialInfo.Emotionless())
            {
                return false;
            }
            return true;
        }
    }
}
