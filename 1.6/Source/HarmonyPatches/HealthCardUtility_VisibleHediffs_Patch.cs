using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace VREAndroids
{
    [HarmonyPatch(typeof(HealthCardUtility), "VisibleHediffs")]
    public static class HealthCardUtility_VisibleHediffs_Patch
    {
        public static IEnumerable<Hediff> Postfix(IEnumerable<Hediff> __result, Pawn pawn, bool showBloodLoss)
        {
            if (pawn.IsAndroid())
            {
                CacheMissingPartsCommonAncestors(pawn.health.hediffSet);
                foreach (var hediff in VisibleHediffsForAndroid(pawn, showBloodLoss))
                {
                    yield return hediff;
                }
            }
            else
            {
                foreach (var r in __result)
                {
                    yield return r;
                }
            }
        }

        private static void CacheMissingPartsCommonAncestors(HediffSet set)
        {
            if (set.cachedMissingPartsCommonAncestors == null)
            {
                set.cachedMissingPartsCommonAncestors = new List<Hediff_MissingPart>();
            }
            else
            {
                set.cachedMissingPartsCommonAncestors.Clear();
            }
            set.missingPartsCommonAncestorsQueue.Clear();
            set.missingPartsCommonAncestorsQueue.Enqueue(set.pawn.def.race.body.corePart);
            while (set.missingPartsCommonAncestorsQueue.Count != 0)
            {
                BodyPartRecord bodyPartRecord = set.missingPartsCommonAncestorsQueue.Dequeue();
                if (set.PartOrAnyAncestorHasDirectlyAddedParts(bodyPartRecord))
                {
                    continue;
                }
                Hediff_MissingPart firstHediffMatchingPart = set.GetFirstHediffMatchingPart<Hediff_MissingPart>(bodyPartRecord);
                if (firstHediffMatchingPart != null)
                {
                    set.cachedMissingPartsCommonAncestors.Add(firstHediffMatchingPart);
                    continue;
                }
                for (int i = 0; i < bodyPartRecord.parts.Count; i++)
                {
                    set.missingPartsCommonAncestorsQueue.Enqueue(bodyPartRecord.parts[i]);
                }
            }
        }

        private static IEnumerable<Hediff> VisibleHediffsForAndroid(Pawn pawn, bool showBloodLoss)
        {
            if (!HealthCardUtility.showAllHediffs)
            {
                List<Hediff_MissingPart> mpca = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
                for (int i = 0; i < mpca.Count; i++)
                {
                    yield return mpca[i];
                }
                IEnumerable<Hediff> enumerable = pawn.health.hediffSet.hediffs.Where(delegate (Hediff d)
                {
                    if (d is Hediff_MissingPart)
                    {
                        return false;
                    }
                    if (!d.Visible)
                    {
                        return false;
                    }
                    // The capacitor array is distributed mechanite power storage spread through the whole
                    // body, not a discrete organ, so it isn't listed as a health entry (its charge shows
                    // on the needs bar instead). The reactor stays visible - it is a physical part.
                    if (d is Hediff_AndroidBattery)
                    {
                        return false;
                    }
                    return (showBloodLoss || d.def != VREA_DefOf.VREA_NeutroLoss) ? true : false;
                });
                foreach (Hediff item in enumerable)
                {
                    yield return item;
                }
                yield break;
            }
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                yield return hediff;
            }
        }
    }
}
