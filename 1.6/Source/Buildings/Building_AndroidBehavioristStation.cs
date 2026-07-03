using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VREAndroids
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Building_AndroidBehavioristStation : Building_Enterable, IThingHolderWithDrawnPawn, IThingHolder
    {
        public CompPowerTrader compPower;

        public bool initModification;


        public CustomXenotype curAndroidProject;

        public float currentWorkAmountDone;

        public float totalWorkAmount;

        public static readonly Texture2D CancelLoadingIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/EjectAnAndroid");

        public static readonly CachedTexture InsertPersonIcon = new CachedTexture("UI/Gizmos/ModifyAnAndroid");
        public float HeldPawnDrawPos_Y => DrawPos.y + 3f / 74f;
        public float HeldPawnBodyAngle => Rotation.AsAngle;
        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

        public bool PowerOn => this.TryGetComp<CompPowerTrader>().PowerOn;

        public override Vector3 PawnDrawOffset
        {
            get
            {
                if (this.Rotation == Rot4.West)
                {
                    return new Vector3(.57f, 0, -0.05f).RotatedBy(base.Rotation);
                }
                else if (this.Rotation == Rot4.South)
                {
                    return new Vector3(.47f, 0, -0.05f).RotatedBy(base.Rotation);
                }
                else if (this.Rotation == Rot4.North)
                {
                    return new Vector3(.47f, 0, 0.05f).RotatedBy(base.Rotation);
                }
                else if (this.Rotation == Rot4.East)
                {
                    return new Vector3(.47f, 0, -0.05f).RotatedBy(base.Rotation);
                }
                return new Vector3(.47f, 0, 0).RotatedBy(base.Rotation);
            }
        }
        public Pawn Occupant
        {
            get
            {
                for (int i = 0; i < innerContainer.Count; i++)
                {
                    Pawn result;
                    if ((result = innerContainer[i] as Pawn) != null)
                    {
                        return result;
                    }
                }
                return null;
            }
        }

        public SubcoreScannerState State
        {
            get
            {
                if (!initModification || !PowerOn)
                {
                    return SubcoreScannerState.Inactive;
                }
                if (Occupant == null)
                {
                    return SubcoreScannerState.WaitingForOccupant;
                }
                return SubcoreScannerState.Occupied;
            }
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPower = this.TryGetComp<CompPowerTrader>();
        }
        public override AcceptanceReport CanAcceptPawn(Pawn selPawn)
        {
            if (!selPawn.IsColonist && !selPawn.IsSlaveOfColony && !selPawn.IsPrisonerOfColony)
            {
                return false;
            }
            if (selPawn.IsAndroid() is false)
            {
                return false;
            }
            if (selPawn.IsAwakened() && selPawn.IsColonist && selPawn.IsPrisoner is false)
            {
                return "VREA.RefusesReprogramming".Translate();
            }
            if (selPawn.IsQuestLodger())
            {
                return "CryptosleepCasketGuestsNotAllowed".Translate();
            }
            if (selectedPawn != null && selectedPawn != selPawn)
            {
                return false;
            }
            if (!PowerOn)
            {
                return "CannotUseNoPower".Translate();
            }
            return true;
        }

        public override void TryAcceptPawn(Pawn pawn)
        {
            if ((bool)CanAcceptPawn(pawn))
            {
                bool num = pawn.DeSpawnOrDeselect();
                if (pawn.holdingOwner != null)
                {
                    pawn.holdingOwner.TryTransferToContainer(pawn, innerContainer);
                }
                else
                {
                    innerContainer.TryAdd(pawn);
                }
                if (num)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
                Find.WindowStack.Add(new Window_AndroidModification(this, pawn, null));
            }
        }
        public bool ReadyForModifying(Pawn crafter, out string failReason)
        {
            failReason = null;
            if (!compPower.PowerOn) failReason = "NoPower".Translate();
            if (curAndroidProject is null) return false;
            if (Occupant is null) return false;
            return failReason is null;
        }

        public void DoWork(Pawn crafter, int delta, out bool workDone)
        {
            var workAmount = crafter.GetStatValue(StatDefOf.ResearchSpeed) * delta;
            currentWorkAmountDone += workAmount;
            if (currentWorkAmountDone >= totalWorkAmount)
                workDone = true;
            else
                workDone = false;
        }

        public void FinishAndroidProject()
        {
            var android = Occupant;
            android.genes.xenotypeName = curAndroidProject.name;
            android.genes.iconDef = curAndroidProject.IconDef;
            // Strip every android gene the pawn currently has - all instances, iterating its own list,
            // so any accidental duplicate is cleared too (the old GetGene loop removed only one copy of
            // each def and could leave/grow duplicates). Then re-add exactly the chosen set once each.
            foreach (var gene in android.genes.GenesListForReading.Where(g => g.def.IsAndroidGene()).ToList())
            {
                android.genes.RemoveGene(gene);
            }

            foreach (GeneDef gene in curAndroidProject.genes.Distinct().OrderByDescending(x => x.CanBeRemovedFromAndroid() is false).ToList())
            {
                android.genes.AddGene(gene, true);
            }
            Utils.RemoveDuplicateGenes(android);
            curAndroidProject = null;
            EjectContents(); 
            innerContainer.ClearAndDestroyContents();
            initModification = false;
            currentWorkAmountDone = 0;
            totalWorkAmount = 0;
        }

        public void EjectContents()
        {
            Pawn occupant = Occupant;
            if (occupant == null)
            {
                innerContainer.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
            }
            else
            {
                for (int num = innerContainer.Count - 1; num >= 0; num--)
                {
                    if (innerContainer[num] is Pawn || innerContainer[num] is Corpse)
                    {
                        innerContainer.TryDrop(innerContainer[num], InteractionCell, base.Map, ThingPlaceMode.Near, 1, out var _);
                    }
                }
            }
            selectedPawn = null;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            AcceptanceReport acceptanceReport = CanAcceptPawn(selPawn);
            if (acceptanceReport.Accepted)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
                {
                    SelectPawn(selPawn);
                }), selPawn, this);
            }
            else if (!acceptanceReport.Reason.NullOrEmpty())
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + acceptanceReport.Reason.CapitalizeFirst(), null);
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip); 
            Occupant?.Drawer.renderer.RenderPawnAt(DrawPos + PawnDrawOffset, null, neverAimWeapon: true);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (base.SelectedPawn == null)
            {
                Command_Action command_Action2 = new Command_Action();
                command_Action2.defaultLabel = "VREA.ModifyAndroid".Translate();
                command_Action2.defaultDesc = "VREA.ModifyAndroidDesc".Translate();
                command_Action2.icon = InsertPersonIcon.Texture;
                command_Action2.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    var allPawnsSpawned = base.Map.mapPawns.AllPawnsSpawned;
                    for (int j = 0; j < allPawnsSpawned.Count; j++)
                    {
                        Pawn pawn = allPawnsSpawned[j];
                        AcceptanceReport acceptanceReport = CanAcceptPawn(pawn);
                        if (!acceptanceReport.Accepted)
                        {
                            if (!acceptanceReport.Reason.NullOrEmpty())
                            {
                                list.Add(new FloatMenuOption(pawn.LabelShortCap + ": " + acceptanceReport.Reason, null, pawn, Color.white));
                            }
                        }
                        else
                        {
                            list.Add(new FloatMenuOption(pawn.LabelShortCap, delegate
                            {
                                SelectPawn(pawn);
                            }, pawn, Color.white));
                        }
                    }
                    if (!list.Any())
                    {
                        list.Add(new FloatMenuOption("VREA.NoAndroidsPresent".Translate(), null));
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                };
                if (!PowerOn)
                {
                    command_Action2.Disable("NoPower".Translate().CapitalizeFirst());
                }
                yield return command_Action2;
            }
            if (initModification)
            {
                Command_Action command_Action3 = new Command_Action();
                command_Action3.defaultLabel = "VREA.CancelModifyingAndroid".Translate();
                command_Action3.defaultDesc = "VREA.CancelModifyingAndroidDesc".Translate();
                command_Action3.icon = CancelLoadingIcon;
                command_Action3.action = delegate
                {
                    CancelModification();
                };
                command_Action3.activateSound = SoundDefOf.Designate_Cancel;
                yield return command_Action3;
            }
        }

        public void CancelModification()
        {
            EjectContents();
            innerContainer.ClearAndDestroyContents();
            initModification = false;
            curAndroidProject = null;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (State != SubcoreScannerState.Inactive)
            {
                stringBuilder.AppendLineIfNotEmpty();
                stringBuilder.AppendLine("VREA.ModificationProgress".Translate((currentWorkAmountDone / totalWorkAmount).ToStringPercent()));
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initModification, "initModification", defaultValue: false);
            Scribe_Deep.Look(ref curAndroidProject, "curAndroidProject");
            Scribe_Values.Look(ref currentWorkAmountDone, "currentWorkAmountDone");
            Scribe_Values.Look(ref totalWorkAmount, "totalWorkAmount");
        }
    }
}
