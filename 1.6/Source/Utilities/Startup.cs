using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace VREAndroids
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            foreach (var race in DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.race != null && x.race.Humanlike))
            {
                race.recipes ??= new List<RecipeDef>();
                race.recipes.Add(VREA_DefOf.VREA_RemoveArtificialPart);
                race.recipes.Add(VREA_DefOf.VREA_ExtractSubcoreSurgery);
            }
        }
    }
}
