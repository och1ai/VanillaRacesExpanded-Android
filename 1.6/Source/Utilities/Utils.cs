using HarmonyLib;
using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace VREAndroids
{
    public static class Utils
    {
        public static bool HasAndroidPartThingVariant(this BodyPartDef part)
        {
            return part != BodyPartDefOf.Torso && part != VREA_DefOf.Brain
                && part != BodyPartDefOf.Head && part != VREA_DefOf.Skull;
        }

        private static HashSet<ThingDef> androidBeds;
        public static bool IsAndroidBed(this ThingDef thingDef)
        {
            if (androidBeds is null)
            {
                androidBeds = new HashSet<ThingDef>
                {
                    VREA_DefOf.VREA_NeutroCasket, VREA_DefOf.VREA_AndroidStand, VREA_DefOf.VREA_AndroidStandSpot
                };
            }
            return androidBeds.Contains(thingDef);
        }
        [DebugAction("Pawns", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
        private static void AwakenAndroid(Pawn p)
        {
            var gene = p.genes?.GetGene(VREA_DefOf.VREA_SyntheticBody) as Gene_SyntheticBody;
            if (gene != null && p.IsAwakened() is false) 
            {
                gene.Awaken("VREA.AndroidAwakening".Translate(p.Named("PAWN")), "VREA.AndroidAwakeningHighMood".Translate(p.Named("PAWN")));
            }
        }

        public static HashSet<GeneDef> allAndroidGenes = new HashSet<GeneDef>();
        private static List<GeneDef> cachedGeneDefsInOrder = null;

        public static List<GeneDef> AndroidGenesGenesInOrder
        {
            get
            {
                if (cachedGeneDefsInOrder == null)
                {
                    cachedGeneDefsInOrder = new List<GeneDef>();
                    foreach (GeneDef allDef in allAndroidGenes)
                    {
                        if (allDef.endogeneCategory != EndogeneCategory.Melanin)
                        {
                            cachedGeneDefsInOrder.Add(allDef);
                        }
                    }
                    cachedGeneDefsInOrder.SortBy((GeneDef x) => 0f - x.displayCategory.displayPriorityInXenotype, (GeneDef x) => x.displayCategory.label, (GeneDef x) => x.displayOrderInCategory);
                }
                return cachedGeneDefsInOrder;
            }
        }

        public static bool IsAndroidGene(this GeneDef geneDef)
        {
            var result = allAndroidGenes.Contains(geneDef);
            return result;
        }

        public static string ToName(this GeneDef geneDef)
        {
            return geneDef.defName + " - " + geneDef.GetHashCode();
        }

        public static bool CanBeRemovedFromAndroid(this GeneDef geneDef)
        {
            if (geneDef is AndroidGeneDef androidGeneDef && androidGeneDef.isCoreComponent)
            {
                return false;
            }
            return true;
        }

        public static bool IsAwakened(this Pawn pawn)
        {
            return pawn.genes.GenesListForReading.Select(x => x.def).OfType<AndroidGeneDef>()
            .Any(x => x.removeWhenAwakened) is false;
        }
        public static bool CanBeRemovedFromAndroidAwakened(this GeneDef geneDef)
        {
            if (geneDef is AndroidGeneDef androidGeneDef && androidGeneDef.isCoreComponent is true && androidGeneDef.removeWhenAwakened is false)
            {
                return false;
            }
            return true;
        }
        public static bool HasActiveGene(this Pawn pawn, GeneDef geneDef)
        {
            if (pawn.genes is null) return false;
            return pawn.genes.GetGene(geneDef)?.Active ?? false;
        }

        public static bool IsHardware(this GeneDef geneDef)
        {
            if (geneDef.IsAndroidGene() is false)
                return false;
            return geneDef.IsSubroutine() is false;
        }
        public static bool IsSubroutine(this GeneDef geneDef)
        {
            return geneDef.displayCategory == VREA_DefOf.VREA_Subroutine;
        }

        // Shared exclusion tag for the mutually-exclusive blood hardware (neutroamine / hemogenic
        // / bloodless). Exactly one is chosen when the body is built.
        public const string BloodExclusionTag = "AndroidBlood";

        public static bool IsBloodGene(this GeneDef geneDef)
        {
            return geneDef.exclusionTags != null && geneDef.exclusionTags.Contains(BloodExclusionTag);
        }

        public static Gene ActiveBloodGene(this Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return null;
            }
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                if (gene.Active && gene.def.IsBloodGene())
                {
                    return gene;
                }
            }
            return null;
        }

        private static HashSet<BodyPartDef> cachedBloodOrganParts;
        // Body parts whose synthetic organ depends on the android's blood type (heart, kidney).
        public static bool IsBloodOrganPart(BodyPartDef part)
        {
            if (cachedBloodOrganParts == null)
            {
                cachedBloodOrganParts = new HashSet<BodyPartDef>();
                foreach (var geneDef in allAndroidGenes)
                {
                    var ext = geneDef.GetModExtension<BloodOrgansExtension>();
                    if (ext != null)
                    {
                        foreach (var organ in ext.organs)
                        {
                            if (organ.part != null)
                            {
                                cachedBloodOrganParts.Add(organ.part);
                            }
                        }
                    }
                }
            }
            return cachedBloodOrganParts.Contains(part);
        }

        // Shared exclusion tag for the mutually-exclusive power hardware (reactor / battery).
        // Exactly one is chosen when the body is built.
        public const string PowerExclusionTag = "AndroidPower";

        public static bool IsPowerGene(this GeneDef geneDef)
        {
            return geneDef.exclusionTags != null && geneDef.exclusionTags.Contains(PowerExclusionTag);
        }

        // Reconciles an android's ideoligion with the ideological subroutine: an android only has an
        // ideoligion while it carries the gene. With the gene it is assigned one (its faction's, or a
        // random one) if it has none; without the gene its ideoligion is cleared. No-op without Ideology.
        public static void SyncAndroidIdeo(Pawn android)
        {
            if (!ModsConfig.IdeologyActive || android?.ideo == null || VREA_DefOf.VREA_Ideological == null)
            {
                return;
            }
            bool hasGene = android.genes?.GetGene(VREA_DefOf.VREA_Ideological) != null;
            if (hasGene)
            {
                if (android.Ideo == null)
                {
                    Ideo ideo = android.Faction?.ideos?.PrimaryIdeo;
                    if (ideo == null && Find.IdeoManager != null)
                    {
                        var all = Find.IdeoManager.IdeosListForReading;
                        if (all.Count > 0)
                        {
                            ideo = all.RandomElement();
                        }
                    }
                    if (ideo != null)
                    {
                        android.ideo.SetIdeo(ideo);
                    }
                }
            }
            else if (!android.IsAwakened() && android.Ideo != null)
            {
                // No subroutine and not awakened: it may not follow an ideoligion. An awakened android
                // keeps whatever ideoligion it has even after the subroutine is removed.
                android.ideo.SetIdeo(null);
            }
        }

        // Shared exclusion tag for the mutually-exclusive chassis hardware (reinforced / delicate).
        // Unlike blood and power it is fully optional: an android may have zero or one. It behaves
        // like blood at the printer (pick at most one, swaps replace) and is locked afterwards.
        public const string ChassisExclusionTag = "AndroidChassis";

        public static bool IsChassisGene(this GeneDef geneDef)
        {
            return geneDef.exclusionTags != null && geneDef.exclusionTags.Contains(ChassisExclusionTag);
        }

        public static Gene ActivePowerGene(this Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return null;
            }
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                if (gene.Active && gene.def.IsPowerGene())
                {
                    return gene;
                }
            }
            return null;
        }

        // The hardware a component gene depends on (requires at least one of), or null if it has none.
        public static List<GeneDef> RequiredHardware(this GeneDef geneDef)
        {
            var req = (geneDef as AndroidGeneDef)?.requiresOneOf;
            return (req != null && req.Count > 0) ? req : null;
        }

        // True when this gene's hardware requirement (if any) is met by the current selection.
        public static bool RequirementSatisfiedBy(this GeneDef geneDef, ICollection<GeneDef> selectedGenes)
        {
            var req = geneDef.RequiredHardware();
            if (req == null)
            {
                return true;
            }
            foreach (var r in req)
            {
                if (selectedGenes.Contains(r))
                {
                    return true;
                }
            }
            return false;
        }

        // Removes any gene the pawn carries more than one copy of, keeping the first. AddGene(def, true)
        // never dedups (only the endogene path checks), so a component chosen twice - or a duplicate
        // left over from the old single-instance removal loop - would otherwise pile up (e.g. two "skin
        // color" genes). Safe to run after any bulk gene rewrite.
        public static void RemoveDuplicateGenes(Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return;
            }
            var seen = new HashSet<GeneDef>();
            foreach (var gene in pawn.genes.GenesListForReading.ToList())
            {
                if (!seen.Add(gene.def))
                {
                    pawn.genes.RemoveGene(gene);
                }
            }
        }

        // The first selected gene this one is declared to conflict with, or null if there is no clash.
        public static GeneDef ConflictInSelection(this GeneDef geneDef, ICollection<GeneDef> selectedGenes)
        {
            var names = (geneDef as AndroidGeneDef)?.conflictsWith;
            if (names == null || names.Count == 0)
            {
                return null;
            }
            foreach (var g in selectedGenes)
            {
                if (g != geneDef && names.Contains(g.defName))
                {
                    return g;
                }
            }
            return null;
        }

        private static HashSet<BodyPartDef> cachedPowerCoreParts;
        // Body part(s) that hold the android's power core (the stomach by default).
        public static bool IsPowerCorePart(BodyPartDef part)
        {
            if (cachedPowerCoreParts == null)
            {
                cachedPowerCoreParts = new HashSet<BodyPartDef>();
                foreach (var geneDef in allAndroidGenes)
                {
                    var ext = geneDef.GetModExtension<PowerCoreExtension>();
                    if (ext?.part != null)
                    {
                        cachedPowerCoreParts.Add(ext.part);
                    }
                }
            }
            return cachedPowerCoreParts.Contains(part);
        }

        // Reconciles the android's power core with its active power gene: removes any core that
        // doesn't match and installs the gene's core on its part. Never overwrites manual implants.
        public static void SyncPowerCore(Pawn pawn, GeneDef powerGeneOverride = null)
        {
            if (pawn?.health == null)
            {
                return;
            }
            // powerGeneOverride is used when called from a gene's PostAdd, where the gene may not be
            // flagged Active yet and ActivePowerGene() would miss it.
            var powerGeneDef = powerGeneOverride ?? pawn.ActivePowerGene()?.def;
            var ext = powerGeneDef?.GetModExtension<PowerCoreExtension>();
            if (ext?.coreHediff == null)
            {
                return;
            }
            // Strip any power core that isn't the one this gene installs.
            var toRemove = pawn.health.hediffSet.hediffs
                .Where(h => h is Hediff_AndroidPowerCore && h.def != ext.coreHediff)
                .ToList();
            foreach (var stale in toRemove)
            {
                pawn.health.RemoveHediff(stale);
            }
            // A core with no part (the battery / capacitor array) is a whole-body system: its charge is
            // never tied to a destructible organ, so damage can't knock it out - it works like a mech's
            // energy. A core with a part (the reactor) is a physical, destructible/replaceable implant.
            if (ext.part == null)
            {
                if (pawn.health.hediffSet.hediffs.Any(h => h.def == ext.coreHediff) is false)
                {
                    pawn.health.AddHediff(ext.coreHediff);
                }
                return;
            }
            // Install the correct core on each matching part record if not already present.
            foreach (var record in pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def == ext.part).ToList())
            {
                bool alreadyHasCore = pawn.health.hediffSet.hediffs
                    .Any(h => h.Part == record && h.def == ext.coreHediff);
                bool hasManualAddedPart = pawn.health.hediffSet.hediffs
                    .Any(h => h.Part == record && h is Hediff_AddedPart && h is Hediff_AndroidPowerCore is false);
                if (alreadyHasCore is false && hasManualAddedPart is false)
                {
                    pawn.health.AddHediff(ext.coreHediff, record);
                }
            }
        }

        // Blood- and power-aware counterpart: circulatory organs depend on the active blood gene
        // (null when that blood type has none, e.g. bloodless) and the power core depends on the
        // active power gene (reactor vs battery); everything else uses the generic counterpart.
        public static HediffDef GetAndroidCounterPartFor(BodyPartDef part, Pawn pawn)
        {
            if (IsBloodOrganPart(part))
            {
                return pawn.ActiveBloodGene()?.def.GetModExtension<BloodOrgansExtension>()?.GetOrgan(part);
            }
            if (IsPowerCorePart(part))
            {
                return pawn.ActivePowerGene()?.def.GetModExtension<PowerCoreExtension>()?.coreHediff;
            }
            return part.GetAndroidCounterPart();
        }

        private static HashSet<HediffDef> cachedBloodOrganHediffs;
        public static bool IsBloodOrganHediff(HediffDef def)
        {
            if (cachedBloodOrganHediffs == null)
            {
                cachedBloodOrganHediffs = new HashSet<HediffDef>();
                foreach (var geneDef in allAndroidGenes)
                {
                    var ext = geneDef.GetModExtension<BloodOrgansExtension>();
                    if (ext != null)
                    {
                        foreach (var organ in ext.organs)
                        {
                            if (organ.hediff != null)
                            {
                                cachedBloodOrganHediffs.Add(organ.hediff);
                            }
                        }
                    }
                }
            }
            return cachedBloodOrganHediffs.Contains(def);
        }

        // Reconciles an android's circulatory organs with its active blood type: removes organs
        // that don't match and installs the correct ones (none for parts the blood type omits).
        // A part listed once applies to all its records (e.g. both kidneys get a neutrofilter);
        // listed multiple times, the organs map to successive records (e.g. one kidney gets a
        // fluid reprocessor, the other a heatsink). Manual implants are never overwritten.
        public static void SyncBloodOrgans(Pawn pawn, GeneDef bloodGeneOverride = null)
        {
            if (pawn?.health == null)
            {
                return;
            }
            // bloodGeneOverride is used when called from a gene's PostAdd, where the gene may not
            // be flagged Active yet and ActiveBloodGene() would miss it.
            var bloodGeneDef = bloodGeneOverride ?? pawn.ActiveBloodGene()?.def;
            var ext = bloodGeneDef?.GetModExtension<BloodOrgansExtension>();
            if (cachedBloodOrganParts == null)
            {
                IsBloodOrganPart(null);
            }
            var allowed = new HashSet<HediffDef>();
            if (ext != null)
            {
                foreach (var organ in ext.organs)
                {
                    if (organ.hediff != null)
                    {
                        allowed.Add(organ.hediff);
                    }
                }
            }
            // 1) Strip any circulatory organ that doesn't belong to this blood type (by hediff, so
            //    it works regardless of body-part-record matching).
            var toRemove = pawn.health.hediffSet.hediffs
                .Where(h => h is Hediff_AndroidPart && IsBloodOrganHediff(h.def) && allowed.Contains(h.def) is false)
                .ToList();
            foreach (var stale in toRemove)
            {
                pawn.health.RemoveHediff(stale);
            }
            // 2) Install the correct organs on each circulatory part record.
            foreach (var partDef in cachedBloodOrganParts)
            {
                var desired = new List<HediffDef>();
                if (ext != null)
                {
                    foreach (var organ in ext.organs)
                    {
                        if (organ.part == partDef && organ.hediff != null)
                        {
                            desired.Add(organ.hediff);
                        }
                    }
                }
                var records = pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def == partDef).ToList();
                for (int i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    HediffDef want = desired.Count == 0 ? null
                        : (desired.Count == 1 ? desired[0] : (i < desired.Count ? desired[i] : null));
                    if (want == null)
                    {
                        continue;
                    }
                    bool alreadyHasAddedPart = pawn.health.hediffSet.hediffs
                        .Any(h => h.Part == record && h is Hediff_AddedPart);
                    if (alreadyHasAddedPart is false)
                    {
                        pawn.health.AddHediff(want, record);
                    }
                }
            }
        }

        public static bool IsNonAwakenedAndroidType(this XenotypeDef def)
        {
            return def.genes.Any(x => x == VREA_DefOf.VREA_PsychologyDisabled);
        }

        public static Dictionary<BodyPartDef, HediffDef> cachedCounterParts = new Dictionary<BodyPartDef, HediffDef>();
        public static HediffDef GetAndroidCounterPart(this BodyPartDef bodyPart)
        {
            if (!cachedCounterParts.TryGetValue(bodyPart, out HediffDef hediffDef))
            {
                cachedCounterParts[bodyPart] = hediffDef = GetAndroidCounterPartInt(bodyPart);
            }
            return hediffDef;
        }
        private static HediffDef GetAndroidCounterPartInt(BodyPartDef bodyPart)
        {
            foreach (var recipe in DefDatabase<RecipeDef>.AllDefs)
            {
                if (recipe.addsHediff != null && recipe.appliedOnFixedBodyParts != null && recipe.appliedOnFixedBodyParts.Contains(bodyPart)
                    && typeof(Hediff_AndroidPart).IsAssignableFrom(recipe.addsHediff.hediffClass))
                {
                    return recipe.addsHediff;
                }
            }
            return null;
        }

        public static bool AndroidCanCatch(HediffDef hediffDef)
        {
            var extension = hediffDef.GetModExtension<AndroidSettingsExtension>();
            if (extension != null)
            {
                return extension.androidCanCatchIt;
            }
            if (hediffDef.tags != null)
            {
                if (hediffDef.tags.Contains("Sterilized"))
                {
                    return false;
                }
            }
            if (VREA_DefOf.VREA_AndroidSettings.androidsShouldNotReceiveHediffs.Contains(hediffDef.defName))
            {
                return false;
            }
            if (typeof(Hediff_Addiction).IsAssignableFrom(hediffDef.hediffClass)
                || typeof(Hediff_ChemicalDependency).IsAssignableFrom(hediffDef.hediffClass)
                || DefDatabase<ChemicalDef>.AllDefs.Any(x => x.toleranceHediff == hediffDef)
                || typeof(Hediff_High).IsAssignableFrom(hediffDef.hediffClass)
                || typeof(Hediff_Hangover).IsAssignableFrom(hediffDef.hediffClass)
                || hediffDef.chronic || hediffDef.CompProps<HediffCompProperties_Immunizable>() != null
                || hediffDef.makesSickThought)
            {
                return false;
            }
            return true;
        }

        public static Dictionary<Pawn_GeneTracker, bool> cachedPawnTypes = new();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAndroid(this Pawn pawn)
        {
            if (pawn is null)
            {
                Log.Error("Checking for null pawn. It shouldn't happen.");
                return false;
            }
            if (pawn.genes is null) return false;
            if (!cachedPawnTypes.TryGetValue(pawn.genes, out var isAndroid))
            {
                if (pawn.genes.xenogenes.Count == 0 && pawn.genes.endogenes.Count == 0) 
                    return false;
                cachedPawnTypes[pawn.genes] = isAndroid = pawn.genes.GenesListForReading.Any(x => x.def.CanBeRemovedFromAndroid() is false);
            }
            return isAndroid;
        }

        // The android's installed power core (battery or reactor), or null if it has none.
        public static Hediff_AndroidPowerCore GetPowerCore(this Pawn pawn)
        {
            return pawn?.health?.hediffSet?.hediffs.OfType<Hediff_AndroidPowerCore>().FirstOrDefault();
        }

        public static bool HasSubcore(this Pawn pawn, out Hediff_AndroidSubcore subcore)
        {
            subcore = pawn?.health?.hediffSet?.hediffs.OfType<Hediff_AndroidSubcore>().FirstOrDefault();
            return subcore != null;
        }

        // Set true while a deliberate subcore extraction is destroying an android, so the death letter
        // stays quiet - the player ordered the extraction and does not need a "destroyed" notice.
        public static bool extractingSubcore;

        // Set true while forcing the *real* death of an android (its subcore was destroyed), so the
        // normal "recoverable death" thought suppression is bypassed for this one moment.
        public static bool forcingAndroidRealDeath;

        // Set true while an android is being downed by running out of power, so the generic
        // "a capacitor array caused X..." combat-log line is swapped for a clearer low-power notice.
        public static bool suppressAndroidDownLog;

        // The permanent death of an android whose subcore has just been destroyed: friends and lovers now
        // grieve as for any real death, and the player is told the android was killed for good. As long
        // as the subcore survives, an android's "death" is only a recoverable destruction.
        // Fires the colony's grief for a truly, permanently dead android and (by default) the "android
        // killed" letter. Pass sendLetter: false when a kill notice was already shown for this death
        // (e.g. a head/brain kill, where NotifyPlayerOfKilled already posted it) so it is not doubled.
        public static void AndroidRealDeath(Pawn pawn, bool sendLetter = true)
        {
            if (pawn == null)
            {
                return;
            }
            try
            {
                forcingAndroidRealDeath = true;
                PawnDiedOrDownedThoughtsUtility.TryGiveThoughts(pawn, null, PawnDiedOrDownedThoughtsKind.Died);
            }
            finally
            {
                forcingAndroidRealDeath = false;
            }
            if (sendLetter && (pawn.Faction == Faction.OfPlayer || PawnUtility.ShouldSendNotificationAbout(pawn)))
            {
                Find.LetterStack.ReceiveLetter("VREA.AndroidKilled".Translate() + ": " + pawn.LabelShortCap,
                    "VREA.AndroidKilledDesc".Translate(pawn.Named("PAWN")), LetterDefOf.NegativeEvent);
            }
        }

        // The permanent death of an android known only by a stored subcore (no body): just notify the
        // player their android is gone for good.
        public static void AndroidRealDeathFromData(AndroidPersonaData data)
        {
            if (data == null || !data.ContainsData)
            {
                return;
            }
            // If the body this persona came from still exists, run the full grief through it so friends
            // and lovers actually mourn (and get the notice).
            if (data.sourcePawn != null && !data.sourcePawn.Discarded && data.sourcePawn.relations != null)
            {
                AndroidRealDeath(data.sourcePawn);
                return;
            }
            if (data.faction != Faction.OfPlayer)
            {
                return;
            }
            Find.LetterStack.ReceiveLetter("VREA.AndroidKilled".Translate() + ": " + data.ShortName,
                "VREA.AndroidKilledNoBodyDesc".Translate(data.name.ToStringFull), LetterDefOf.NegativeEvent);
        }

        // The standard materials to build an android body with the given hardware/subroutine genes -
        // the same cost the creation window charges. Used for reprints (which supply their own subcore,
        // so it is excluded there).
        public static List<ThingDefCount> AndroidMaterialCost(IEnumerable<GeneDef> genes, bool includeSubcore)
        {
            var geneList = genes?.ToList() ?? new List<GeneDef>();
            var items = new List<ThingDefCount>();
            if (includeSubcore)
            {
                items.Add(new ThingDefCount(VREA_DefOf.VREA_AndroidSubcore, 1));
            }
            items.Add(new ThingDefCount(ThingDefOf.Plasteel, 125));
            items.Add(new ThingDefCount(ThingDefOf.ComponentSpacer, 7));
            items.Add(geneList.Contains(VREA_DefOf.VREA_BatteryPowered)
                ? new ThingDefCount(ThingDefOf.ComponentIndustrial, 3)
                : new ThingDefCount(ThingDefOf.Uranium, 20));
            if (geneList.Contains(VREA_DefOf.VREA_NeutroCirculation))
            {
                items.Add(new ThingDefCount(VREA_DefOf.Neutroamine, 25));
            }
            else if (geneList.Contains(VREA_DefOf.VREA_NormalBlood))
            {
                items.Add(new ThingDefCount(ThingDefOf.HemogenPack, 4));
            }
            return items;
        }

        // The filth an android sprays when wounded (or when its head is torn off to pull the subcore):
        // neutroamine for neutroamine blood, red for the hemogenic default, and nothing for a dry
        // bloodless frame.
        public static ThingDef SubcoreBloodDef(this Pawn pawn)
        {
            if (pawn.HasActiveGene(VREA_DefOf.VREA_Bloodless))
            {
                return null;
            }
            if (pawn.HasActiveGene(VREA_DefOf.VREA_NeutroCirculation))
            {
                return VREA_DefOf.VREA_Filth_Neutroamine;
            }
            return ThingDefOf.Filth_Blood;
        }

        // Tears the android's head off and sprays its blood around the body when the subcore is pulled.
        // On a living android this severs the head and kills it; on a corpse it is purely the visual.
        public static void BlowOffHeadForSubcore(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }
            Map map = pawn.MapHeld;
            IntVec3 pos = pawn.PositionHeld;
            ThingDef blood = pawn.SubcoreBloodDef();
            if (blood != null && map != null && pos.IsValid)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(pos, 1.6f, true))
                {
                    if (cell.InBounds(map) && Rand.Chance(0.6f))
                    {
                        FilthMaker.TryMakeFilth(cell, map, blood, pawn.LabelShort, Rand.RangeInclusive(1, 3));
                    }
                }
            }
            BodyPartRecord head = pawn.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(p => p.def == BodyPartDefOf.Head);
            if (head != null)
            {
                pawn.health.AddHediff(HediffDefOf.MissingBodyPart, head);
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        public static void RecheckHediffs(Pawn pawn)
        {
            if (pawn.IsAndroid())
            {
                TrySwapHediff(pawn, HediffDefOf.Hypothermia, VREA_DefOf.VREA_Freezing);
                TrySwapHediff(pawn, HediffDefOf.Heatstroke, VREA_DefOf.VREA_Overheating);
            }
            else
            {
                TrySwapHediff(pawn, VREA_DefOf.VREA_Freezing, HediffDefOf.Hypothermia);
                TrySwapHediff(pawn, VREA_DefOf.VREA_Overheating, HediffDefOf.Heatstroke);
            }
        }

        public static void TrySwapHediff(Pawn pawn, HediffDef from, HediffDef to)
        {
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(from);
            if (hediff != null)
            {
                var newHediff = HediffMaker.MakeHediff(to, pawn, hediff.part);
                newHediff.Severity = hediff.Severity;
                pawn.health.RemoveHediff(hediff);
                pawn.health.AddHediff(newHediff);
            }
        }
        public static void RecheckGenes(Pawn_GeneTracker __instance)
        {
            if (__instance.pawn.IsAndroid())
            {
                for (var i = 0; i < __instance.endogenes.Count; i++)
                {
                    var gene = __instance.endogenes[i];
                    if (gene.def.IsAndroidGene() is false)
                    {
                        var androidVariant = DefDatabase<GeneDef>.GetNamedSilentFail("VREA_" + gene.def.defName);
                        if (androidVariant != null)
                        {
                            __instance.endogenes[i] = GeneMaker.MakeGene(androidVariant, __instance.pawn);
                        }
                    }
                }

                for (var i = 0; i < __instance.xenogenes.Count; i++)
                {
                    var gene = __instance.xenogenes[i];
                    if (gene.def.IsAndroidGene() is false)
                    {
                        var androidVariant = DefDatabase<GeneDef>.GetNamedSilentFail("VREA_" + gene.def.defName);
                        if (androidVariant != null)
                        {
                            __instance.xenogenes[i] = GeneMaker.MakeGene(androidVariant, __instance.pawn);
                        }
                    }
                }
            }
            else
            {
                for (var i = 0; i < __instance.endogenes.Count; i++)
                {
                    var gene = __instance.endogenes[i];
                    if (gene.def.IsAndroidGene())
                    {
                        var humanVariant = DefDatabase<GeneDef>.GetNamedSilentFail(gene.def.defName.Replace("VREA_", ""));
                        if (humanVariant != null)
                        {
                            __instance.endogenes[i] = GeneMaker.MakeGene(humanVariant, __instance.pawn);
                        }
                    }
                }

                for (var i = 0; i < __instance.xenogenes.Count; i++)
                {
                    var gene = __instance.xenogenes[i];
                    if (gene.def.IsAndroidGene())
                    {
                        var humanVariant = DefDatabase<GeneDef>.GetNamedSilentFail(gene.def.defName.Replace("VREA_", ""));
                        if (humanVariant != null)
                        {
                            __instance.xenogenes[i] = GeneMaker.MakeGene(humanVariant, __instance.pawn);
                        }
                    }
                }
            }
        }

        public static void TryAssignBackstory(Pawn pawn, string spawnCategory)
        {
            if (pawn.story.Childhood?.spawnCategories is null || pawn.story.Childhood.spawnCategories.Contains(spawnCategory) is false)
            {
                pawn.story.Childhood = DefDatabase<BackstoryDef>.AllDefs.Where(x => x.spawnCategories?.Contains(spawnCategory) ?? false).RandomElement();
            }
        }

        private static List<GeneDef> skinColorGenes;

        public static List<GeneDef> SkinColorAndroidGenesInOrder
        {
            get
            {
                if (skinColorGenes == null)
                {
                    skinColorGenes = new List<GeneDef>();
                    foreach (GeneDef allDef in DefDatabase<GeneDef>.AllDefs)
                    {
                        if ((allDef.endogeneCategory == EndogeneCategory.Melanin || !(allDef.minMelanin >= 0f)) && allDef.skinColorBase.HasValue)
                        {
                            if (allDef.IsAndroidGene())
                            {
                                skinColorGenes.Add(allDef);
                            }
                        }
                    }
                    skinColorGenes.SortBy((GeneDef x) => x.minMelanin);
                }
                return skinColorGenes;
            }
        }

        private static List<GeneDef> cachedHairColorGenes;

        public static List<GeneDef> HairColorAndroidGenes
        {
            get
            {
                if (cachedHairColorGenes == null)
                {
                    cachedHairColorGenes = DefDatabase<GeneDef>.AllDefs.Where((GeneDef x) => x.hairColorOverride.HasValue && x.IsAndroidGene()).ToList();
                }
                return cachedHairColorGenes;
            }
        }


        public static bool IsAndroid(this Pawn pawn, out Gene_SyntheticBody gene_SyntheticBody)
        {
            if (pawn is null)
            {
                Log.Error("Checking for null pawn. It shouldn't happen.");
                gene_SyntheticBody = null;
                return false;
            }
            gene_SyntheticBody = pawn.genes?.GetGene(VREA_DefOf.VREA_SyntheticBody) as Gene_SyntheticBody;
            return gene_SyntheticBody != null;
        }

        public static void TrySpawnWaste(this Pawn pawn, IntVec3 pos, Map map)
        {
            if (pawn.HasActiveGene(VREA_DefOf.VREA_ZeroWaste) is false)
            {
                var wasteCount = pawn.HasActiveGene(VREA_DefOf.VREA_ExtraWaste) ? 15 : 5;
                var wastepack = ThingMaker.MakeThing(ThingDefOf.Wastepack);
                wastepack.stackCount = wasteCount;
                GenSpawn.Spawn(wastepack, pos, map);
            }
        }

        public static bool IsAndroidType(this XenotypeDef def)
        {
            return def.genes.Count > 0 && def.genes.Any(x => x is AndroidGeneDef androidGeneDef && androidGeneDef.isCoreComponent);
        }
        public static bool IsAndroidType(this CustomXenotype def)
        {
            return def.genes.Count > 0 && def.genes.Any(x => x is AndroidGeneDef androidGeneDef && androidGeneDef.isCoreComponent);
        }
        public static RecipeDef RecipeForAndroid(this RecipeDef originalRecipe)
        {
            if (originalRecipe.workSkill != SkillDefOf.Crafting)
            {
                var recipe = originalRecipe.Clone() as RecipeDef;
                recipe.effectWorking = VREA_DefOf.ButcherMechanoid;
                recipe.soundWorking = VREA_DefOf.Recipe_Machining;
                recipe.workSpeedStat = VREA_DefOf.ButcheryMechanoidSpeed;
                recipe.workSkill = SkillDefOf.Crafting;
                if (recipe.skillRequirements != null)
                {
                    recipe.skillRequirements = new List<SkillRequirement>();
                    foreach (var skillReq in originalRecipe.skillRequirements)
                    {
                        if (skillReq.skill == SkillDefOf.Medicine)
                        {
                            recipe.skillRequirements.Add(new SkillRequirement { minLevel = skillReq.minLevel, skill = SkillDefOf.Crafting });
                        }
                        else
                        {
                            recipe.skillRequirements.Add(new SkillRequirement { minLevel = skillReq.minLevel, skill = skillReq.skill });
                        }
                    }
                }
                recipe.ingredients = recipe.ingredients.Where(x => (x.filter?.categories != null
                    && x.filter.categories.Contains("Medicine")) is false).ToList();
                return recipe;
            }
            return originalRecipe;
        }

        // Whether a pawn is allowed to hold an ideoligion at all. An android may only do so if it
        // carries the ideological subroutine or has awakened; everything else (all non-androids,
        // ideological or awakened androids) is unrestricted. Used to gate conversion, social
        // interactions and any other ideoligion assignment.
        public static bool CanHoldIdeoligion(this Pawn pawn)
        {
            if (pawn == null || !pawn.IsAndroid() || pawn.IsAwakened())
            {
                return true;
            }
            return VREA_DefOf.VREA_Ideological != null && pawn.HasActiveGene(VREA_DefOf.VREA_Ideological);
        }

        // True for a "machine" android whose Social tab should show only the interaction log - no
        // relations, no ideoligion/role section: emotionless (no emotion-simulator hardware, not
        // awakened) and without the ideological subroutine.
        public static bool SocialTabLogOnly(this Pawn pawn)
        {
            return pawn != null && pawn.IsAndroid() && pawn.Emotionless()
                && (VREA_DefOf.VREA_Ideological == null || !pawn.HasActiveGene(VREA_DefOf.VREA_Ideological));
        }

        // Relationships (and the whole relations section) need either the emotion-simulators hardware
        // or an awakened mind: an android with neither reads and behaves as emotionless. Emotion
        // simulators still functionally overrides psychology disabled (a psychology-disabled android
        // with the hardware is not emotionless). Non-androids are never emotionless.
        public static bool Emotionless(this Pawn pawn)
        {
            return pawn.IsAndroid() && !pawn.HasActiveGene(VREA_DefOf.VREA_EmotionSimulators) && !pawn.IsAwakened();
        }
        public static object Clone(this object obj)
        {
            var cloneMethod = obj.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return cloneMethod.Invoke(obj, null);
        }
    }
}
