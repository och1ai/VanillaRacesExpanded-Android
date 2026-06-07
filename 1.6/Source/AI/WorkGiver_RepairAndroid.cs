using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    public class WorkGiver_RepairAndroid : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
        }
        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return HasJobOn(pawn, t, forced);
        }
        public static bool HasJobOn(Pawn pawn, Thing t, bool forced)
        {
            Pawn pawn2 = (Pawn)t;
            if (pawn2 is null || pawn2.IsAndroid(out var gene) is false)
            {
                return false;
            }
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Crafting))
            {
                return false;
            }
            if (gene.autoRepair is false)
            {
                return false;
            }
            if (GoodLayingStatusForTend(pawn2, pawn, forced) is false)
            {
                return false;
            }
            if (pawn != pawn2)
            {
                // Don't repair an android that is busy repairing itself.
                if (pawn2.CurJobDef == VREA_DefOf.VREA_RepairAndroid)
                {
                    return false;
                }
                if (pawn2.HostileTo(pawn))
                {
                    return false;
                }
                if (t.IsForbidden(pawn))
                {
                    return false;
                }
                // Only one crafter repairs a given android at a time. This avoids several free
                // crafters being handed the same patient and then failing to reserve it.
                List<Pawn> factionPawns = pawn2.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
                for (int i = 0; i < factionPawns.Count; i++)
                {
                    Pawn other = factionPawns[i];
                    if (other != pawn && other.CurJobDef == VREA_DefOf.VREA_RepairAndroid
                        && other.CurJob.targetA.Thing == pawn2)
                    {
                        return false;
                    }
                }
                if (!pawn.CanReserve(t, 1, -1, null, forced)
                    || !pawn.CanReach(t, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    return false;
                }
            }
            if (!JobDriver_RepairAndroid.CanRepairAndroid(pawn2))
            {
                return false;
            }
            return true;
        }

        public static bool GoodLayingStatusForTend(Pawn patient, Pawn doctor, bool forced)
        {
            if (patient == doctor)
            {
                // Self-repair only requires self-repair (self-tend) to be enabled.
                return patient.playerSettings != null && patient.playerSettings.selfTend;
            }
            // Auto-repair targets androids that are downed or resting in a bed/stand. Repairing
            // an android that is up and about is done on demand via the right-click "Repair"
            // order or by prioritizing the work (both pass forced = true), which avoids idle
            // androids being swarmed by every free crafter.
            return forced || patient.Downed || patient.InBed();
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(VREA_DefOf.VREA_RepairAndroid, t);
        }
    }
}
