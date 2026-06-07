using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(RecipeDefGenerator), "DrugAdministerDefs")]
    public static class RecipeDefGenerator_DrugAdministerDefs_Patch
    {
        public static IEnumerable<RecipeDef> Postfix(IEnumerable<RecipeDef> __result)
        {
            foreach (var def in __result)
            {
                yield return def;
            }
            RecipeDef recipeDef = new RecipeDef();
            var item = VREA_DefOf.Neutroamine;
            recipeDef.defName = "VREA_Administer_" + item.defName;
            recipeDef.label = "RecipeAdminister".Translate(item.label);
            recipeDef.jobString = "RecipeAdministerJobString".Translate(item.label);
            recipeDef.workerClass = typeof(Recipe_AdministerNeutroamineForAndroid);
            recipeDef.targetsBodyPart = false;
            recipeDef.anesthetize = false;
            recipeDef.surgerySuccessChanceFactor = 99999f;
            recipeDef.modContentPack = item.modContentPack;
            recipeDef.workAmount = 120;
            IngredientCount ingredientCount = new IngredientCount();
            ingredientCount.SetBaseCount(100f);
            ingredientCount.filter.SetAllow(item, allow: true);
            recipeDef.ingredients.Add(ingredientCount);
            recipeDef.fixedIngredientFilter.SetAllow(item, allow: true);
            recipeDef.recipeUsers = new List<ThingDef>();
            foreach (ThingDef item2 in DefDatabase<ThingDef>.AllDefs.Where((ThingDef d) => d.category == ThingCategory.Pawn
            && d.race.Humanlike))
            {
                recipeDef.recipeUsers.Add(item2);
            }
            yield return recipeDef;
        }
    }
}
