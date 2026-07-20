using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(HealthCardUtility), "DrawOverviewTab")]
    public static class HealthCardUtility_DrawOverviewTab_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(ref float __result, Rect rect, Pawn pawn, float curY)
        {
            if (pawn.IsAndroid(out var gene))
            {
                __result = DrawOverviewTabAndroid(rect, pawn, gene, curY);
                return false;
            }
            return true;
        }

        private static float DrawOverviewTabAndroid(Rect leftRect, Pawn pawn, Gene_SyntheticBody gene, float curY)
        {
            curY += 4f;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            string str = (string)"VREA.AndroidSummaryWithGender".Translate(pawn.Named("PAWN"));
            Rect rect = new Rect(0f, curY, leftRect.width, 34f);
            Widgets.Label(rect, str.CapitalizeFirst());
            GUI.color = Color.white;
            curY += 34f;
            // (The delayed-shutdown countdown is drawn in the pawn's inspect pane, just above the android
            // energy readout - see Pawn_GetInspectString_Patch - rather than crowding the overview tab.)
            if (pawn.IsColonist && !pawn.Dead)
            {
                bool selfTend = pawn.playerSettings.selfTend;
                Rect rect3 = new Rect(0f, curY, leftRect.width, 24f);
                Widgets.CheckboxLabeled(rect3, "VREA.SelfRepair".Translate(), ref pawn.playerSettings.selfTend);
                if (pawn.playerSettings.selfTend && !selfTend)
                {
                    if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Crafting))
                    {
                        pawn.playerSettings.selfTend = false;
                        Messages.Message("VREA.MessageCannotSelfRepairEver".Translate(pawn.LabelShort, pawn), MessageTypeDefOf.RejectInput, historical: false);
                    }
                    else if (pawn.workSettings.GetPriority(WorkTypeDefOf.Crafting) == 0)
                    {
                        Messages.Message("VREA.MessageSelfRepairUnsatisfied".Translate(pawn.LabelShort, pawn), MessageTypeDefOf.CautionInput, historical: false);
                    }
                }
                if (Mouse.IsOver(rect3))
                {
                    TooltipHandler.TipRegion(rect3, "VREA.SelfRepairTip".Translate(Faction.OfPlayer.def.pawnsPlural, 0.7f.ToStringPercent()).CapitalizeFirst());
                }
                curY += 28f;
                rect3 = new Rect(0f, curY, leftRect.width, 24f);
                Widgets.CheckboxLabeled(rect3, "CommandAutoRepair".Translate(), ref gene.autoRepair);
                if (Mouse.IsOver(rect3))
                {
                    TooltipHandler.TipRegion(rect3, "VREA.CommandAutoRepairDesc".Translate());
                }
                curY += 28f;
            }

            Text.Font = GameFont.Small;
            if (!pawn.Dead)
            {
                IEnumerable<PawnCapacityDef> source = (pawn.def.race.Humanlike ? DefDatabase<PawnCapacityDef>.AllDefs.Where((PawnCapacityDef x) => x.showOnHumanlikes) : ((!pawn.def.race.Animal) ? DefDatabase<PawnCapacityDef>.AllDefs.Where((PawnCapacityDef x) => x.showOnMechanoids) : DefDatabase<PawnCapacityDef>.AllDefs.Where((PawnCapacityDef x) => x.showOnAnimals)));
                {
                    foreach (PawnCapacityDef item in source.OrderBy((PawnCapacityDef act) => act.listOrder))
                    {
                        if (PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, item))
                        {
                            PawnCapacityDef activityLocal = item;
                            Pair<string, Color> efficiencyLabel = HealthCardUtility.GetEfficiencyLabel(pawn, item);
                            Func<string> textGetter = () => (!pawn.Dead) ? HealthCardUtility.GetPawnCapacityTip(pawn, activityLocal) : "";
                            HealthCardUtility.DrawLeftRow(leftRect, ref curY, (item.labelMechanoids.NullOrEmpty() is false 
                                ? item.labelMechanoids : item.label).CapitalizeFirst(), efficiencyLabel.First, 
                                efficiencyLabel.Second, new TipSignal(textGetter, pawn.thingIDNumber ^ item.index));
                        }
                    }
                    return curY;
                }
            }
            return curY;
        }
    }
}
