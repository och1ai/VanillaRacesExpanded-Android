using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch]
    public static class RecipeWorker_AvailableOnNow_Patch
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(RecipeWorker).GetMethod("AvailableOnNow", AccessTools.all);
            foreach (var type in typeof(RecipeWorker).AllSubclasses())
            {
                var method = type.GetMethod("AvailableOnNow", AccessTools.allDeclared);
                if (method != null)
                {
                    yield return method;
                }
            }
        }
        [HarmonyPriority(int.MinValue)]
        public static void Postfix(ref bool __result, RecipeWorker __instance, Thing __0)
        {
            if (__result && __0 is Pawn pawn && pawn.IsAndroid())
            {
                __result = RecipeIsAvailableOnAndroid(__instance, pawn);
            }
        }

        public static bool RecipeIsAvailableOnAndroid(RecipeWorker recipeWorker, Pawn pawn)
        {
            // Hemogenic androids carry ordinary red blood, so they support the vanilla blood
            // transfusion and hemogen extraction recipes just like a normal pawn.
            string recipeDefName = recipeWorker.recipe.defName;
            if ((recipeDefName == "BloodTransfusion" || recipeDefName == "ExtractHemogenPack")
                && pawn.HasActiveGene(VREA_DefOf.VREA_NormalBlood))
            {
                return true;
            }
            if (recipeWorker is Recipe_AdministerIngestible && recipeWorker is not Recipe_AdministerNeutroamineForAndroid)
            {
                return false;
            }
            if (recipeWorker is Recipe_RemoveBodyPart && recipeWorker is not Recipe_RemoveArtificialBodyPart)
            {
                return false;
            }
            if (recipeWorker is Recipe_InstallNaturalBodyPart)
            {
                return false;
            }
            if (recipeWorker.recipe.addsHediff != null && Utils.AndroidCanCatch(recipeWorker.recipe.addsHediff) is false)
            {
                return false;
            }
            if (VREA_DefOf.VREA_AndroidSettings.disallowedRecipes.Contains(recipeWorker.recipe.defName))
            {
                return false;
            }
            return true;
        }
    }
}
