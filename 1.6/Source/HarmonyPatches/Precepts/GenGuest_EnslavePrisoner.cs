using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using System;

namespace VREAndroids
{
    [HarmonyPatch(typeof(GenGuest), "EnslavePrisoner")]
    public static class GenGuest_EnslavePrisoner_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn warden, Pawn prisoner)
        {
            if (Utils.IdeoTreatsAndroidAsTool(warden.Ideo, prisoner))
            {
                if (!prisoner.IsSlave)
                {
                   
                    prisoner.guest.SetGuestStatus(warden.Faction, GuestStatus.Slave);
                    Messages.Message("MessagePrisonerEnslaved".Translate(prisoner, warden), new LookTargets(prisoner, warden), MessageTypeDefOf.NeutralEvent);
                    prisoner.apparel.UnlockAll();
                }

                return false;
            }
            return true;
        }
    }
}
