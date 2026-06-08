using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VREAndroids
{
    [StaticConstructorOnStartup]
    public class Building_AndroidCreationStation : Building
    {
        public CompPowerTrader compPower;

        public CustomXenotype curAndroidProject;

        public UnfinishedAndroid unfinishedAndroid;

        public float currentWorkAmountDone;

        public float totalWorkAmount;

        public List<ThingDefCount> requiredItems;
        public IEnumerable<IngredientCount> RequiredIngredients()
        {
            var ingredientCountList = new List<IngredientCount>();
            foreach (var data in requiredItems) 
                ingredientCountList.Add(new ThingDefCountClass(data.thingDef, data.count).ToIngredientCount());
            return ingredientCountList;
        }

        public void DoWork(Pawn crafter, int delta, out bool workDone)
        {
            var workAmount = crafter.GetStatValue(StatDefOf.WorkSpeedGlobal) * delta;
            currentWorkAmountDone += workAmount;
            if (currentWorkAmountDone >= totalWorkAmount)
                workDone = true;
            else
                workDone = false;
            unfinishedAndroid.workLeft = totalWorkAmount - currentWorkAmountDone;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPower = this.TryGetComp<CompPowerTrader>();
        }

        public bool ReadyForAssembling(Pawn crafter, out string failReason)
        {
            failReason = null;
            if (!compPower.PowerOn) failReason = "NoPower".Translate();
            if (curAndroidProject is null) return false;
            return failReason is null;
        }
        public void CreateUnfinishedAndroid(List<Thing> resources)
        {
            unfinishedAndroid = ThingMaker.MakeThing(VREA_DefOf.VREA_UnfinishedAndroid) as UnfinishedAndroid;
            unfinishedAndroid.resources = new List<Thing>();
            unfinishedAndroid.station = this;
            foreach (Thing thing in resources)
            {
                unfinishedAndroid.resources.Add(thing);
                thing.DeSpawn();
            }
            GenSpawn.Spawn(unfinishedAndroid, Position, Map);
        }

        public void FinishAndroidProject()
        {
            var android = PawnGenerator.GeneratePawn(new PawnGenerationRequest(VREA_DefOf.VREA_AndroidBasic, Faction.OfPlayer, 
                allowDowned: true, allowAddictions: false));
            android.apparel.wornApparel.Clear();
            android.equipment.equipment.Clear();
            android.inventory.innerContainer.Clear();

            android.ageTracker.AgeBiologicalTicks = 0;
            android.ageTracker.AgeChronologicalTicks = 0;
            android.genes.xenotypeName = curAndroidProject.name;
            android.genes.iconDef = curAndroidProject.IconDef;
            foreach (var gene in Utils.allAndroidGenes)
            {
                var existingGene = android.genes.GetGene(gene);
                if (existingGene != null)
                {
                    android.genes.RemoveGene(existingGene);
                }
            }

            foreach (GeneDef gene in curAndroidProject.genes.OrderByDescending(x => x.CanBeRemovedFromAndroid() is false).ToList())
            {
                android.genes.AddGene(gene, true);
            }
            // The body was generated as the default blood type and power source, so reconcile its
            // circulatory organs and power core with what was actually chosen for this android.
            Utils.SyncBloodOrgans(android);
            Utils.SyncPowerCore(android);
            // The android is built with a full reservoir (the blood/neutroamine was paid up front at
            // creation), so it spawns with no neutroloss. Neutroloss only builds up later from
            // bleeding wounds.
            curAndroidProject = null;
            GenSpawn.Spawn(android, Position, Map);
            currentWorkAmountDone = 0;
            totalWorkAmount = 0;
            unfinishedAndroid?.Destroy();
            unfinishedAndroid = null;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var opt in base.GetFloatMenuOptions(selPawn))
            {
                yield return opt;
            }
            if (this.curAndroidProject is null)
            {
                yield return new FloatMenuOption("VREA.CreateAndroid".Translate(), delegate
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
            if (curAndroidProject != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "VREA.CancelAndroid".Translate(),
                    defaultDesc = "VREA.CancelAndroidDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Gizmos/CancelAnAndroid"),
                    action = delegate
                    {
                        if (this.unfinishedAndroid != null)
                        {
                            this.unfinishedAndroid.CancelProject();
                        }
                        else
                        {
                            curAndroidProject = null;
                        }
                    }
                };

                if (DebugSettings.godMode)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: finish project",
                        action = FinishAndroidProject
                    };
                }
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "VREA.CreateAndroid".Translate(),
                    defaultDesc = "VREA.CreateAndroidDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Gizmos/CreateAnAndroid"),
                    action = delegate
                    {
                        CallAndroidCreationWindow(null);
                    }
                };
            }
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
            Scribe_Collections.Look(ref requiredItems, "requiredItems", LookMode.Deep);
        }
    }
}
