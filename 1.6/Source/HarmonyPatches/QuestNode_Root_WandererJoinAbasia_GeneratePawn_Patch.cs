using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch]
    public static class QuestNode_Root_WandererJoinAbasia_GeneratePawn_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(QuestNode_Root_WandererJoinAbasia), "GeneratePawn_NewTemp")
                   ?? AccessTools.Method(typeof(QuestNode_Root_WandererJoinAbasia), "GeneratePawn");
        }

        public static void Prefix()
        {
            PawnGenerator_XenotypesAvailableFor_Patch.preventAndroidGeneration = true;
        }
        public static void Finalizer()
        {
            PawnGenerator_XenotypesAvailableFor_Patch.preventAndroidGeneration = false;
        }
    }
}