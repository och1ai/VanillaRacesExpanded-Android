using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(PawnUtility), "GetPosture")]
    public static class PawnUtility_GetPosture_Patch
    {
        // Render flags. They must be [ThreadStatic]: the pre-draw pass runs on parallel worker threads
        // for many pawns at once, so a plain static bool would be flipped by another pawn's render mid-
        // way and make the posture flicker between standing and lying (the android "vibrating" on the
        // stand). Per-thread flags keep each pawn's render self-consistent.
        [ThreadStatic]
        public static bool isPawnRendering;

        // The body being regrown in an android printer is drawn upright and facing front, as if the
        // platform is holding it up, rather than lying dead on the floor.
        [ThreadStatic]
        public static Pawn forceStandingPawn;

        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn p, ref PawnPosture __result)
        {
            if (forceStandingPawn != null && p == forceStandingPawn)
            {
                __result = PawnPosture.Standing;
                return false;
            }
            if (isPawnRendering && p.pather?.moving is false && p.Spawned)
            {
                List<Thing> thingList = p.Position.GetThingList(p.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i] is Building_AndroidStand)
                    {
                        __result = PawnPosture.Standing;
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
