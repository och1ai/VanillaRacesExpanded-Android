using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(FoodUtility), "WillEat", new Type[] { typeof(Pawn), typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool) })]
    public static class FoodUtility_WillEat_Thing_Patch
    {
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(ref bool __result, Pawn p, Thing food)
        {
            if (p.IsAndroid())
            {
                __result = false;
            }
            else if (food is Corpse corpse && corpse.InnerPawn.IsAndroid())
            {
                __result = false;
            }
        }
    }

    // An android's chassis is metal, not meat. Corpse.IngestibleNow already reports mechanoid corpses as
    // non-edible (their race isn't flesh); androids are a humanlike xenotype whose race IS flesh, so their
    // corpses would otherwise read as edible food. Force them non-ingestible too, so nothing (pawns,
    // animals, nutrient dispensers, food stockpiles) treats an android body as food. Butchering it at the
    // android butcher table for plasteel/steel/neutroamine is unaffected (that path doesn't use this).
    [HarmonyPatch(typeof(Corpse), nameof(Corpse.IngestibleNow), MethodType.Getter)]
    public static class Corpse_IngestibleNow_Patch
    {
        public static void Postfix(Corpse __instance, ref bool __result)
        {
            if (__result && __instance.InnerPawn != null && __instance.InnerPawn.IsAndroid())
            {
                __result = false;
            }
        }
    }
}
