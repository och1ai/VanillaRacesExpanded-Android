using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VREAndroids
{
    // Adds an "extract subcore" toggle gizmo onto a dead android's body (using the subcore item's own
    // icon). Toggling it queues the extraction designation, exactly like the Orders designator, so the
    // recovery is discoverable straight from the selected corpse - the same way a mechlink or cortical
    // stack can be pulled from a dead pawn.
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetGizmos))]
    public static class ThingWithComps_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, ThingWithComps __instance)
        {
            foreach (var gizmo in gizmos)
            {
                yield return gizmo;
            }
            if (__instance is Corpse corpse && corpse.Spawned && corpse.Map != null
                && Utils.HasSubcore(corpse.InnerPawn, out _))
            {
                DesignationDef designation = VREA_DefOf.VREA_ExtractSubcoreDesignation;
                yield return new Command_Toggle
                {
                    defaultLabel = "VREA.DesignatorExtractSubcore".Translate(),
                    defaultDesc = "VREA.DesignatorExtractSubcoreDesc".Translate(),
                    icon = VREA_DefOf.VREA_AndroidSubcore.uiIcon,
                    isActive = () => corpse.Map.designationManager.DesignationOn(corpse, designation) != null,
                    toggleAction = delegate
                    {
                        DesignationManager manager = corpse.Map.designationManager;
                        Designation existing = manager.DesignationOn(corpse, designation);
                        if (existing != null)
                        {
                            manager.RemoveDesignation(existing);
                        }
                        else
                        {
                            manager.AddDesignation(new Designation(corpse, designation));
                        }
                    }
                };
            }
        }
    }
}
