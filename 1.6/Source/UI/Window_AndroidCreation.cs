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

        // Blood type can be chosen freely while building the body.
        protected override bool CanSwapBlood => true;
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
                new ThingDefCount(ThingDefOf.Uranium, 30),
                new ThingDefCount(ThingDefOf.ComponentSpacer, 7),
            };
        }
    }
}
