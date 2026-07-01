using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    public enum PrintMode
    {
        Print,
        Resurrect,
        Reprint
    }

    [StaticConstructorOnStartup]
    public class Building_AndroidCreationStation : Building, IThingHolder
    {
        public CompPowerTrader compPower;

        public CustomXenotype curAndroidProject;

        public UnfinishedAndroid unfinishedAndroid;

        public float currentWorkAmountDone;

        public float totalWorkAmount;

        // A single print takes twice as long as a scyther in a mech gestator
        // (scyther = 2 cycles x 120000 forming ticks = 4 days), i.e. 8 days.
        public const int GestationTicks = 480000;

        // What the current print will produce, and the inputs for the resurrect/reprint modes.
        public PrintMode printMode;
        public AndroidPersonaData reprintPersona;
        // The specific body/subcore a queued resurrect/reprint still needs a colonist to haul in.
        public Thing pendingInput;
        private ThingOwner innerContainer;

        private Effecter printingEffecter;

        public Building_AndroidCreationStation()
        {
            innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
        }

        public List<ThingDefCount> requiredItems;
        public IEnumerable<IngredientCount> RequiredIngredients()
        {
            var ingredientCountList = new List<IngredientCount>();
            foreach (var data in requiredItems)
                ingredientCountList.Add(new ThingDefCountClass(data.thingDef, data.count).ToIngredientCount());
            return ingredientCountList;
        }

        // The printer is occupied whenever a print of any mode has been queued.
        public bool IsBusy => curAndroidProject != null || unfinishedAndroid != null || pendingInput != null;

        // A print has been queued but its materials/body have not been delivered yet.
        public bool HasPendingJob => unfinishedAndroid == null && (curAndroidProject != null || pendingInput != null);

        public Corpse HeldCorpse => innerContainer.OfType<Corpse>().FirstOrDefault();

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, innerContainer);
        }

        public void DoWork(Pawn crafter, int delta, out bool workDone)
        {
            var workAmount = crafter.GetStatValue(StatDefOf.WorkSpeedGlobal) * delta;
            currentWorkAmountDone += workAmount;
            workDone = currentWorkAmountDone >= totalWorkAmount;
            unfinishedAndroid.workLeft = totalWorkAmount - currentWorkAmountDone;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPower = this.TryGetComp<CompPowerTrader>();
        }

        // Once a print is under way (the unfinished android exists), the printer builds it on its own -
        // like a mech gestator, no crafter has to stand and work it. Growth only advances while powered.
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (unfinishedAndroid == null || (compPower != null && !compPower.PowerOn))
            {
                StopPrintingEffect();
                return;
            }
            // Welding sparks play on the printer for as long as an android is being printed.
            if (printingEffecter == null)
            {
                printingEffecter = EffecterDefOf.ConstructMetal.Spawn();
            }
            printingEffecter.EffectTick(this, this);
            currentWorkAmountDone += delta;
            if (currentWorkAmountDone >= totalWorkAmount)
            {
                FinishAndroidProject();
            }
        }

        private void StopPrintingEffect()
        {
            printingEffecter?.Cleanup();
            printingEffecter = null;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            StopPrintingEffect();
            base.DeSpawn(mode);
        }

        public float PrintProgress => totalWorkAmount > 0f ? Mathf.Clamp01(currentWorkAmountDone / totalWorkAmount) : 0f;

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (unfinishedAndroid != null)
            {
                if (!text.NullOrEmpty())
                {
                    text += "\n";
                }
                text += "VREA.PrintingProgress".Translate((PrintProgress * 100f).ToString("F0"));
                if (compPower != null && !compPower.PowerOn)
                {
                    text += " (" + "VREA.PrintingPaused".Translate() + ")";
                }
            }
            return text;
        }

        public bool ReadyForAssembling(Pawn crafter, out string failReason)
        {
            failReason = null;
            if (!compPower.PowerOn) failReason = "NoPower".Translate();
            if (curAndroidProject is null && pendingInput is null) return false;
            return failReason is null;
        }

        // Queues a resurrection: a colonist must haul in the dead body (subcore still inside) plus the
        // steel, then the printer regrows it.
        public void QueueResurrect(Corpse corpse)
        {
            pendingInput = corpse;
            printMode = PrintMode.Resurrect;
            requiredItems = new List<ThingDefCount> { new ThingDefCount(ThingDefOf.Steel, 50) };
        }

        // Queues a reprint from a recovered subcore: a colonist hauls in that subcore plus the standard
        // android materials, then the printer prints a fresh body carrying the stored identity.
        public void QueueReprint(AndroidSubcore subcore)
        {
            pendingInput = subcore;
            printMode = PrintMode.Reprint;
            requiredItems = Utils.AndroidMaterialCost(subcore.personaData?.androidGenes, includeSubcore: false);
        }

        // Called by the delivery job once a colonist has brought everything a queued print needs. Routes
        // the delivered things by mode and starts the print.
        public void DeliverAndStart(List<Thing> placedThings)
        {
            switch (printMode)
            {
                case PrintMode.Resurrect:
                {
                    Corpse corpse = placedThings.OfType<Corpse>().FirstOrDefault();
                    if (corpse == null) { pendingInput = null; printMode = PrintMode.Print; return; }
                    corpse.DeSpawn();
                    innerContainer.TryAddOrTransfer(corpse, canMergeWithExistingStacks: false);
                    SpawnUnfinishedMarker();
                    StoreResources(placedThings.Where(t => t != corpse));
                    break;
                }
                case PrintMode.Reprint:
                {
                    AndroidSubcore subcore = placedThings.OfType<AndroidSubcore>().FirstOrDefault(s => s.HasData);
                    if (subcore == null) { pendingInput = null; printMode = PrintMode.Print; return; }
                    reprintPersona = subcore.personaData;
                    // Hand the persona to the printer and empty the item so consuming it is not a death.
                    subcore.personaData = null;
                    subcore.Destroy();
                    SpawnUnfinishedMarker();
                    StoreResources(placedThings.Where(t => t != subcore));
                    break;
                }
                default:
                    printMode = PrintMode.Print;
                    SpawnUnfinishedMarker();
                    StoreResources(placedThings);
                    break;
            }
            pendingInput = null;
        }

        private void StoreResources(IEnumerable<Thing> resources)
        {
            unfinishedAndroid.resources = new List<Thing>();
            foreach (Thing thing in resources)
            {
                unfinishedAndroid.resources.Add(thing);
                if (thing.Spawned)
                {
                    thing.DeSpawn();
                }
            }
        }

        // Spawns the visible work-in-progress placeholder on the printer and (re)starts the print clock.
        private void SpawnUnfinishedMarker()
        {
            unfinishedAndroid = ThingMaker.MakeThing(VREA_DefOf.VREA_UnfinishedAndroid) as UnfinishedAndroid;
            unfinishedAndroid.station = this;
            currentWorkAmountDone = 0;
            totalWorkAmount = GestationTicks;
            GenSpawn.Spawn(unfinishedAndroid, Position, Map);
        }

        // Builds and fully configures a fresh android for a print project, but does not spawn it.
        public Pawn GenerateAndroidPawn(CustomXenotype project)
        {
            Pawn android = MakeBlankAndroid();
            android.genes.xenotypeName = project.name;
            android.genes.iconDef = project.IconDef;
            ClearAndroidGenes(android);
            foreach (GeneDef gene in project.genes.OrderByDescending(x => x.CanBeRemovedFromAndroid() is false).ToList())
            {
                android.genes.AddGene(gene, true);
            }
            Utils.SyncBloodOrgans(android);
            Utils.SyncPowerCore(android);
            if (android.HasSubcore(out var subcore))
            {
                subcore.personaData.CopyFromPawn(android);
            }
            return android;
        }

        // Builds a fresh body from a stored persona (a reprint): rebuilds the stored hardware/subroutines
        // and restores the android's full identity.
        public Pawn GenerateReprintedPawn(AndroidPersonaData persona)
        {
            Pawn android = MakeBlankAndroid();
            ClearAndroidGenes(android);
            if (persona.androidGenes != null)
            {
                foreach (GeneDef gene in persona.androidGenes.OrderByDescending(x => x.CanBeRemovedFromAndroid() is false).ToList())
                {
                    android.genes.AddGene(gene, true);
                }
            }
            if (!persona.xenotypeName.NullOrEmpty()) android.genes.xenotypeName = persona.xenotypeName;
            if (persona.iconDef != null) android.genes.iconDef = persona.iconDef;
            Utils.SyncBloodOrgans(android);
            Utils.SyncPowerCore(android);
            persona.OverwritePawn(android);
            if (android.HasSubcore(out var subcore))
            {
                subcore.personaData = persona;
            }
            return android;
        }

        private Pawn MakeBlankAndroid()
        {
            var android = PawnGenerator.GeneratePawn(new PawnGenerationRequest(VREA_DefOf.VREA_AndroidBasic, Faction.OfPlayer,
                allowDowned: true, allowAddictions: false));
            android.apparel.wornApparel.Clear();
            android.equipment.equipment.Clear();
            android.inventory.innerContainer.Clear();
            // Printed androids come out as fully-formed adults so the game treats them as ordinary pawns,
            // but their chronological age stays 0 - they were just made.
            android.ageTracker.AgeBiologicalTicks = (long)(Rand.Range(20f, 50f) * GenDate.TicksPerYear);
            android.ageTracker.AgeChronologicalTicks = 0;
            return android;
        }

        private static void ClearAndroidGenes(Pawn android)
        {
            foreach (var gene in Utils.allAndroidGenes)
            {
                var existingGene = android.genes.GetGene(gene);
                if (existingGene != null)
                {
                    android.genes.RemoveGene(existingGene);
                }
            }
        }

        // The charge a battery android wakes with after being resurrected.
        private const float ResurrectBatteryCharge = 0.5f;

        // After a resurrection, restore the android's power. A battery is inherent, so it is re-installed
        // (in case the body lost it) and wakes with a partial charge. A reactor instead keeps whatever
        // charge it had - its hediff survives the death and resurrection - and if it was destroyed with
        // the body it is not regrown: the android stays unpowered (downed) until a new one is installed.
        private void RestorePowerAfterResurrection(Pawn android)
        {
            if (android.ActivePowerGene()?.def == VREA_DefOf.VREA_BatteryPowered)
            {
                Utils.SyncPowerCore(android);
                var core = android.GetPowerCore();
                if (core != null)
                {
                    core.Energy = ResurrectBatteryCharge;
                }
            }
        }

        public void FinishAndroidProject()
        {
            // Snapshot and clear state up front so that, if the finish ever throws, it cannot re-fire
            // every tick and spam broken androids.
            PrintMode mode = printMode;
            CustomXenotype project = curAndroidProject;
            AndroidPersonaData persona = reprintPersona;
            Corpse corpse = innerContainer.OfType<Corpse>().FirstOrDefault();
            UnfinishedAndroid finished = unfinishedAndroid;
            curAndroidProject = null;
            reprintPersona = null;
            unfinishedAndroid = null;
            printMode = PrintMode.Print;
            currentWorkAmountDone = 0;
            totalWorkAmount = 0;
            StopPrintingEffect();
            finished?.Destroy();

            switch (mode)
            {
                case PrintMode.Resurrect:
                    if (corpse?.InnerPawn == null)
                    {
                        return;
                    }
                    Pawn dead = corpse.InnerPawn;
                    ResurrectionUtility.TryResurrect(dead);
                    if (!dead.Dead)
                    {
                        if (dead.Faction != Faction.OfPlayer)
                        {
                            dead.SetFaction(Faction.OfPlayer);
                        }
                        RestorePowerAfterResurrection(dead);
                    }
                    if (!dead.Spawned)
                    {
                        GenSpawn.Spawn(dead, Position, Map);
                    }
                    break;
                case PrintMode.Reprint:
                    if (persona == null)
                    {
                        return;
                    }
                    GenSpawn.Spawn(GenerateReprintedPawn(persona), Position, Map);
                    break;
                default:
                    if (project == null)
                    {
                        return;
                    }
                    GenSpawn.Spawn(GenerateAndroidPawn(project), Position, Map);
                    break;
            }
        }

        // Aborts the current print, returning any consumed inputs to the map.
        public void CancelPrint()
        {
            StopPrintingEffect();
            // A queued job whose inputs were never hauled just leaves the body/subcore where it is.
            pendingInput = null;
            if (innerContainer.Count > 0)
            {
                innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
            }
            if (reprintPersona != null)
            {
                var subcore = (AndroidSubcore)ThingMaker.MakeThing(VREA_DefOf.VREA_AndroidSubcore);
                subcore.personaData = reprintPersona;
                GenPlace.TryPlaceThing(subcore, Position, Map, ThingPlaceMode.Near);
                reprintPersona = null;
            }
            printMode = PrintMode.Print;
            currentWorkAmountDone = 0;
            totalWorkAmount = 0;
            if (unfinishedAndroid != null)
            {
                unfinishedAndroid.CancelProject();
            }
            else
            {
                curAndroidProject = null;
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var opt in base.GetFloatMenuOptions(selPawn))
            {
                yield return opt;
            }
            if (!IsBusy)
            {
                yield return new FloatMenuOption("VREA.PrintAndroid".Translate(), delegate
                {
                    CallAndroidCreationWindow(selPawn);
                });
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }
            if (IsBusy)
            {
                yield return new Command_Action
                {
                    defaultLabel = "VREA.CancelAndroid".Translate(),
                    defaultDesc = "VREA.CancelAndroidDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Gizmos/CancelAnAndroid"),
                    action = CancelPrint
                };
                if (DebugSettings.godMode)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: finish print instantly",
                        action = FinishAndroidProject
                    };
                }
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "VREA.PrintAndroid".Translate(),
                defaultDesc = "VREA.PrintAndroidDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Gizmos/CreateAnAndroid"),
                action = delegate
                {
                    CallAndroidCreationWindow(null);
                }
            };
            yield return new Command_Action
            {
                defaultLabel = "VREA.ResurrectAndroid".Translate(),
                defaultDesc = "VREA.ResurrectAndroidDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Gizmos/CreateAnAndroid"),
                action = OpenResurrectMenu
            };
            yield return new Command_Action
            {
                defaultLabel = "VREA.ReprintAndroid".Translate(),
                defaultDesc = "VREA.ReprintAndroidDesc".Translate(),
                icon = VREA_DefOf.VREA_AndroidSubcore.uiIcon,
                action = OpenReprintMenu
            };
        }

        // Lists the dead android bodies on the map to load for resurrection.
        private void OpenResurrectMenu()
        {
            var options = new List<FloatMenuOption>();
            foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
            {
                // Only bodies that still hold their subcore can be resurrected; once the subcore is
                // extracted, the android can only be rebuilt from that subcore via a reprint.
                if (thing is Corpse corpse && corpse.InnerPawn != null && corpse.InnerPawn.IsAndroid()
                    && Utils.HasSubcore(corpse.InnerPawn, out _))
                {
                    Corpse localCorpse = corpse;
                    options.Add(new FloatMenuOption(corpse.LabelCap, delegate { QueueResurrect(localCorpse); },
                        iconThing: localCorpse, iconColor: Color.white));
                }
            }
            if (!options.Any())
            {
                options.Add(new FloatMenuOption("VREA.NoAndroidBodies".Translate(), null));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        // Lists the recovered subcores on the map to load for a reprint.
        private void OpenReprintMenu()
        {
            var options = new List<FloatMenuOption>();
            foreach (Thing thing in Map.listerThings.ThingsOfDef(VREA_DefOf.VREA_AndroidSubcore))
            {
                if (thing is AndroidSubcore subcore && subcore.HasData)
                {
                    AndroidSubcore localSubcore = subcore;
                    options.Add(new FloatMenuOption(subcore.LabelCap, delegate { QueueReprint(localSubcore); },
                        iconThing: localSubcore, iconColor: Color.white));
                }
            }
            if (!options.Any())
            {
                options.Add(new FloatMenuOption("VREA.NoSubcores".Translate(), null));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        public void CallAndroidCreationWindow(Pawn creator)
        {
            Find.WindowStack.Add(new Window_AndroidCreation(this, creator, null));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref unfinishedAndroid, "unfinishedAndroid");
            Scribe_Deep.Look(ref curAndroidProject, "curAndroidProject");
            Scribe_Values.Look(ref currentWorkAmountDone, "currentWorkAmountDone");
            Scribe_Values.Look(ref totalWorkAmount, "totalWorkAmount");
            Scribe_Values.Look(ref printMode, "printMode");
            Scribe_References.Look(ref pendingInput, "pendingInput");
            Scribe_Deep.Look(ref reprintPersona, "reprintPersona");
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Collections.Look(ref requiredItems, "requiredItems", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                innerContainer ??= new ThingOwner<Thing>(this, oneStackOnly: false);
            }
        }
    }
}
