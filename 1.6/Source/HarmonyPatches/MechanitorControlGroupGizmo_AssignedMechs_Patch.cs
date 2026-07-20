using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace VREAndroids
{
    // The control group's "Assigned mechs" tooltip builds its entries with
    //     from m in controlGroup.MechsForReading where m.needs?.energy != null
    // i.e. it filters on the mech energy need, which an android never has (it runs on the mod's
    // reactor-power need instead). A mechlike android is therefore correctly assigned to the group and
    // draws its icon, but is silently missing from that one list. The listing lives inside a
    // compiler-generated closure in GizmoOnGUI, so the lambda has to be located reflectively.
    //
    // This is purely cosmetic, so it is written to never matter if it fails:
    //  - Prepare() returns false when the closure cannot be found, so Harmony skips the whole class.
    //    (Returning an empty TargetMethods is NOT safe - Harmony raises "Undefined target method", which
    //    aborts the mod's entire patching run.)
    //  - the postfix only touches a string that actually contains the "Assigned mechs" header;
    //  - anything that throws leaves the tooltip exactly as vanilla produced it.
    [HarmonyPatch]
    public static class MechanitorControlGroupGizmo_AssignedMechs_Patch
    {
        private static readonly FieldInfo ControlGroupField =
            AccessTools.Field(typeof(MechanitorControlGroupGizmo), "controlGroup");

        private static MethodBase resolvedTarget;
        private static bool searched;

        // Any parameterless string-returning method on a compiler-generated type nested in the gizmo is a
        // tooltip-builder candidate. The name filter is intentionally loose (the exact
        // "<GizmoOnGUI>b__N" shape is a compiler detail); the header check in the postfix is what actually
        // guarantees we only modify the right string.
        private static MethodBase FindTooltipClosure()
        {
            // The tooltip delegate captures only `this` (disabled / disabledReason are fields on the
            // gizmo, not locals), so the compiler emits it as a private method on the gizmo type itself
            // rather than inside a nested display class. Search both shapes so either compilation works.
            var searchTypes = new List<Type> { typeof(MechanitorControlGroupGizmo) };
            searchTypes.AddRange(typeof(MechanitorControlGroupGizmo).GetNestedTypes(AccessTools.all));

            var candidates = new List<MethodBase>();
            foreach (Type type in searchTypes)
            {
                foreach (MethodInfo method in type.GetMethods(AccessTools.all))
                {
                    if (method.ReturnType == typeof(string) && method.GetParameters().Length == 0
                        && !method.IsAbstract && method.DeclaringType == type
                        // Compiler-generated lambdas are name-mangled with angle brackets; this also keeps
                        // us away from ordinary members such as ToString().
                        && method.Name.IndexOf('<') >= 0)
                    {
                        candidates.Add(method);
                    }
                }
            }
            if (candidates.Count == 0)
            {
                return null;
            }
            // Prefer the one generated from GizmoOnGUI when the name survives.
            return candidates.FirstOrDefault(m => m.Name.Contains("GizmoOnGUI")) ?? candidates[0];
        }

        public static bool Prepare()
        {
            if (!searched)
            {
                searched = true;
                try
                {
                    resolvedTarget = FindTooltipClosure();
                }
                catch (Exception e)
                {
                    resolvedTarget = null;
                    Log.Warning("[VREAndroids] Error locating the control-group tooltip closure: " + e);
                }
                if (resolvedTarget == null)
                {
                    Log.Warning("[VREAndroids] Could not find the control-group \"Assigned mechs\" tooltip "
                        + "closure; mechlike androids will not be listed there. Everything else is unaffected.");
                }
            }
            return resolvedTarget != null;
        }

        public static MethodBase TargetMethod()
        {
            return resolvedTarget;
        }

        public static void Postfix(object __instance, ref string __result)
        {
            try
            {
                if (__result.NullOrEmpty() || __instance == null || ControlGroupField == null)
                {
                    return;
                }
                // Only ever touch the tooltip that actually holds the assigned-mechs section.
                string header = "AssignedMechs".Translate().Resolve();
                if (header.NullOrEmpty() || !__result.Contains(header))
                {
                    return;
                }
                MechanitorControlGroup group = FindControlGroup(__instance);
                if (group == null)
                {
                    return;
                }
                List<Pawn> androids = group.MechsForReading
                    .Where(m => m?.needs?.energy == null && MechOversightUtil.IsOversightAndroid(m))
                    .ToList();
                if (androids.Count == 0)
                {
                    return;
                }
                var sb = new StringBuilder(__result);
                foreach (Pawn android in androids)
                {
                    sb.AppendLine().Append(" - ").Append(android.LabelCap);
                    var core = android.GetPowerCore();
                    if (core != null)
                    {
                        sb.Append(" (").Append(core.Energy.ToStringPercent()).Append(' ')
                          .Append("EnergyLower".Translate().Resolve()).Append(')');
                    }
                }
                __result = sb.ToString();
            }
            catch
            {
                // Cosmetic only - never let a tooltip break the control group gizmo.
            }
        }

        // Depending on how the compiler shaped the lambda, the instance is either the gizmo itself (when it
        // captures only `this`) or a display class holding the gizmo / group in a field. Accept all.
        private static MechanitorControlGroup FindControlGroup(object closure)
        {
            if (closure is MechanitorControlGroupGizmo ownerGizmo)
            {
                return ControlGroupField.GetValue(ownerGizmo) as MechanitorControlGroup;
            }
            foreach (FieldInfo field in closure.GetType().GetFields(AccessTools.all))
            {
                object value = field.GetValue(closure);
                if (value is MechanitorControlGroup group)
                {
                    return group;
                }
                if (value is MechanitorControlGroupGizmo gizmo)
                {
                    return ControlGroupField.GetValue(gizmo) as MechanitorControlGroup;
                }
            }
            return null;
        }
    }
}
