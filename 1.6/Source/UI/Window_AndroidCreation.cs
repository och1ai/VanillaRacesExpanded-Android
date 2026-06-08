using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VREAndroids
{
    [HotSwappable]
    public class Window_AndroidCreation : Window_CreateAndroidBase
    {
        public Building_AndroidCreationStation station;

        public Pawn creator;
        public Window_AndroidCreation(Building_AndroidCreationStation station, Pawn creator, Action callback) : base(callback)
        {
            this.station = station;
            this.creator = creator;
        }

        public override string Header => "VREA.CreateAndroid".Translate();
        public override string AcceptButtonLabel => "VREA.CreateAndroid".Translate();

        // Blood type and power source can be chosen freely while building the body.
        protected override bool CanSwapBlood => true;
        protected override bool CanSwapPower => true;
        protected override void AcceptInner()
        {
            CustomXenotype customXenotype = new CustomXenotype();
            customXenotype.name = xenotypeName?.Trim();
            customXenotype.genes.AddRange(selectedGenes);
            customXenotype.inheritable = false;
            customXenotype.iconDef = iconDef;
            station.curAndroidProject = customXenotype;
            station.totalWorkAmount = selectedGenes.Sum(x => x.biostatCpx * 2000);
            station.currentWorkAmountDone = 0;
            station.requiredItems = requiredItems;
            if (creator != null)
            {
                var workgiver = new WorkGiver_CreateAndroid();
                var job = workgiver.JobOnThing(creator, station);
                if (job != null)
                {
                    creator.jobs.TryTakeOrderedJob(job);
                }
            }
        }


        protected override TaggedString AndroidName()
        {
            return "VREA.AndroidtypeName".Translate();
        }
        public override void DrawSearchRect(Rect rect)
        {
            base.DrawSearchRect(rect);
            if (Widgets.ButtonText(new Rect(rect.xMax - ButSize.x, rect.y, ButSize.x, ButSize.y), "VREA.SaveAndroidtype".Translate()))
            {
                CustomXenotype customXenotype = new CustomXenotype();
                customXenotype.name = xenotypeName?.Trim();
                customXenotype.genes.AddRange(selectedGenes);
                customXenotype.inheritable = false;
                customXenotype.iconDef = iconDef;
                Find.WindowStack.Add(new Dialog_AndroidProjectList_Save(customXenotype));
            }
            if (Widgets.ButtonText(new Rect(rect.xMax - ButSize.x * 2f - 4f, rect.y, ButSize.x, ButSize.y), "VREA.LoadAndroidtype".Translate()))
            {
                Find.WindowStack.Add(new Dialog_AndroidProjectList_Load(delegate (CustomXenotype xenotype)
                {
                    xenotypeName = xenotype.name;
                    xenotypeNameLocked = true;
                    selectedGenes.Clear();
                    selectedGenes = Utils.AndroidGenesGenesInOrder
                        .Where(x => x.CanBeRemovedFromAndroid() is false && x.IsBloodGene() is false).ToList();
                    selectedGenes.AddRange(xenotype.genes);
                    selectedGenes = selectedGenes.Distinct().ToList();
                    iconDef = xenotype.IconDef;
                    OnGenesChanged();
                }));
            }
        }

        public override void OnGenesChanged()
        {
            base.OnGenesChanged();
            requiredItems = new List<ThingDefCount>
            {
                new ThingDefCount(VREA_DefOf.VREA_PersonaSubcore, 1),
                new ThingDefCount(ThingDefOf.Plasteel, 125),
                new ThingDefCount(ThingDefOf.ComponentSpacer, 7),
            };
            // The chosen power source is the only source of the android's uranium cost: a reactor
            // needs uranium, a battery just a few components.
            if (SelectedGenes.Contains(VREA_DefOf.VREA_BatteryPowered))
            {
                requiredItems.Add(new ThingDefCount(ThingDefOf.ComponentIndustrial, 3));
            }
            else
            {
                requiredItems.Add(new ThingDefCount(ThingDefOf.Uranium, 20));
            }
            // The chosen blood fills the android's reservoir up front, so it spawns full: neutroamine
            // blood needs 25 neutroamine, hemogenic blood 4 hemogen packs, bloodless needs none.
            if (SelectedGenes.Contains(VREA_DefOf.VREA_NeutroCirculation))
            {
                requiredItems.Add(new ThingDefCount(VREA_DefOf.Neutroamine, 25));
            }
            else if (SelectedGenes.Contains(VREA_DefOf.VREA_NormalBlood))
            {
                requiredItems.Add(new ThingDefCount(ThingDefOf.HemogenPack, 4));
            }
        }
    }
}
