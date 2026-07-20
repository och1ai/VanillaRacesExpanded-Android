using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VREAndroids
{
    // A mechlike android without an overseer is not a loose mechanoid: it never goes feral (it just stands
    // dormant until a mechanitor connects), so lumping it into the vanilla "Uncontrolled mechs" alert -
    // which warns about mechs defecting to the mechanoid faction - is wrong and alarming. Androids are
    // filtered out of that alert and get their own, calmer one instead.
    [HarmonyPatch(typeof(Alert_SubjectHasNowOverseer), "Targets", MethodType.Getter)]
    public static class Alert_SubjectHasNowOverseer_Targets_Patch
    {
        public static void Postfix(ref List<GlobalTargetInfo> __result)
        {
            __result?.RemoveAll(t => t.Thing is Pawn pawn && MechOversightUtil.IsOversightAndroid(pawn));
        }
    }

    // "Uncontrolled androids": mechlike androids waiting for a mechanitor to take oversight. Alerts are
    // discovered automatically from Alert subclasses, so no def is needed.
    public class Alert_UncontrolledAndroids : Alert
    {
        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();

        private List<GlobalTargetInfo> Targets
        {
            get
            {
                targets.Clear();
                foreach (Pawn pawn in PawnsFinder.AllMaps_SpawnedPawnsInFaction(Faction.OfPlayer))
                {
                    if (MechOversightUtil.IsOversightAndroid(pawn) && pawn.GetOverseer() == null)
                    {
                        targets.Add(pawn);
                    }
                }
                return targets;
            }
        }

        public Alert_UncontrolledAndroids()
        {
            defaultLabel = "VREA.AlertUncontrolledAndroids".Translate();
            defaultPriority = AlertPriority.Medium;
            requireBiotech = true;
        }

        public override TaggedString GetExplanation()
        {
            return "VREA.AlertUncontrolledAndroidsDesc".Translate() + ":\n"
                + targets.Select(t => t.Thing.LabelCap).ToLineList("  - ");
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(Targets);
        }
    }
}
