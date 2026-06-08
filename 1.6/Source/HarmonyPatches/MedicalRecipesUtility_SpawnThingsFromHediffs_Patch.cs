using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(MedicalRecipesUtility), "SpawnThingsFromHediffs")]
    public static class MedicalRecipesUtility_SpawnThingsFromHediffs_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            if (pawn.IsAndroid())
            {
                SpawnThingsFromHediffs(pawn, part, pos, map);
                return false;
            }
            return true;
        }

        public static void SpawnThingsFromHediffs(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            if (!pawn.health.hediffSet.GetNotMissingParts().Contains(part))
            {
                return;
            }
            foreach (Hediff item in pawn.health.hediffSet.hediffs.Where((Hediff x) => x.Part == part))
            {
                if (item.def.spawnThingOnRemoved != null)
                {
                    if (item is Hediff_AndroidPowerCore core)
                    {
                        // A reactor below a quarter charge is junk; a battery always drops as a
                        // (rechargeable) battery carrying whatever charge is left.
                        var itemToSpawn = item.def.spawnThingOnRemoved;
                        if (core is Hediff_AndroidReactor && core.Energy < 0.25f)
                        {
                            itemToSpawn = VREA_DefOf.VREA_SpentReactor;
                        }
                        var thing = GenSpawn.Spawn(itemToSpawn, pos, map);
                        if (thing is Reactor reactor)
                        {
                            reactor.curEnergy = core.Energy;
                        }
                    }
                    else
                    {
                        GenSpawn.Spawn(item.def.spawnThingOnRemoved, pos, map);
                    }
                }
            }
        }
    }
}
