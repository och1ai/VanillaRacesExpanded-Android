using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    [HarmonyPatch(typeof(RestUtility), "FindBedFor",
    new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool), typeof(GuestStatus?) },
    new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
    public static class RestUtility_FindBedFor_Patch
    {
        public static bool Prefix(Pawn sleeper, Pawn traveler, ref Building_Bed __result, out List<ThingDef> __state)
        {
            __state = null;
            if (!sleeper.IsAndroid())
            {
                return true;
            }
            // Androids are always taken to an android stand first (to recharge / free memory), ahead of
            // any medical bed, hospital bed or sleeping spot.
            Building_AndroidStand stand = FindStandFor(sleeper, traveler);
            if (stand != null)
            {
                __result = stand;
                return false;
            }
            // No stand available: fall back to vanilla bed selection, but bump the neutro casket to the
            // front of the medical list so it wins over ordinary hospital beds.
            __state = RestUtility.bedDefsBestToWorst_Medical.ListFullCopy();
            RestUtility.bedDefsBestToWorst_Medical.RemoveAll(x => x == VREA_DefOf.VREA_NeutroCasket);
            RestUtility.bedDefsBestToWorst_Medical.Insert(0, VREA_DefOf.VREA_NeutroCasket);
            return true;
        }

        public static void Postfix(List<ThingDef> __state)
        {
            if (__state != null)
            {
                RestUtility.bedDefsBestToWorst_Medical = __state;
            }
        }

        // The android stand this sleeper should be taken to: stands are unowned free-for-all chargers,
        // so any stand the carrier can actually reach and reserve will do.
        private static Building_AndroidStand FindStandFor(Pawn sleeper, Pawn traveler)
        {
            Pawn reacher = traveler ?? sleeper;
            foreach (var stand in Building_AndroidStand.stands)
            {
                if (stand.Map == null || stand.Map != sleeper.MapHeld || stand.Faction != Faction.OfPlayer)
                {
                    continue;
                }
                if (stand.IsForbidden(reacher) || !reacher.CanReserveAndReach(stand, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }
                return stand;
            }
            return null;
        }
    }
}
